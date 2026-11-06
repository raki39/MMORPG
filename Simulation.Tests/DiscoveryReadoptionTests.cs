using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

/// <summary>
/// D-067a: a meta de descobrir uma zona, largada, so volta com mudanca concreta. Subir de nivel conta quando o
/// nivel novo alcanca um requisito que faltava para revelar a zona; para uma zona que ja era acessivel, so rota,
/// conhecimento (pista) ou progresso pertinente (missao que passa por um lugar que a revela). Nada e definitivo.
/// </summary>
public class DiscoveryReadoptionTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private const string Covil = "covil_do_rei_goblin";
    private static string Key(string zone) => GoalTypes.DiscoverZone + "|" + zone;

    private static (WorldState W, Person P, Character Ch) World(ContentDatabase c, params string[] known)
    {
        var e = WorldFactory.Create(c, new WorldConfig { Seed = 91, PopulationSize = 80 });
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(2));
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0);
        var ch = w.GetCharacter(p.CharacterId);
        ch.GoalList.Clear();
        ch.ActiveQuestList.Clear();
        ch.Known.Clear();
        ch.Hints.Clear();
        foreach (var z in known) ch.Known.Add(z);
        p.ObjectiveList.Clear();
        return (w, p, ch);
    }

    /// <summary>Larga a meta de descobrir a zona sem progresso (bloqueio com o retrato do momento).</summary>
    private static void Abandon(WorldState w, Person p, Character ch, string zone)
    {
        ch.GoalList.Add(new PersonalGoal(GoalTypes.DiscoverZone, zone, 1, 0, 500, "pref_exploration", 500, w.Clock.Tick, -10_000_000));
        w.Behavior.Goals.Review(p, ch, new DeterministicRng(1));
        Assert.Contains(Key(zone), ch.BlockedTargets);
        Assert.True(ch.ZoneBlocks.ContainsKey(zone));
        w.Events.Drain();
    }

    private static string? Reason(WorldState w) =>
        w.Events.Drain().Where(x => x.Type == "goal_reconsidered").Select(x => x.Causes[0].Factor).SingleOrDefault();

    [Fact]
    public void ZonaJaAcessivel_SubirDeNivelNaoLibera_PistaLibera()
    {
        var (w, p, ch) = World(C, "vila_aurora", "floresta_sussurrante", "colinas_do_vale", "mina_velha", "forte_do_vau");
        var goals = w.Behavior.Goals;
        ch.Level = 16;
        Assert.True(goals.Reveal(Covil).Level <= ch.Level);                       // ja dava para revela-la
        Abandon(w, p, ch, Covil);
        for (int lvl = 17; lvl <= 22; lvl++)
        {
            ch.Level = lvl;
            goals.OnLevelUp(p, ch);
            goals.RefreshZoneBlocks(ch);
            Assert.Contains(Key(Covil), ch.BlockedTargets);                        // o mesmo estado nao repete o ciclo
            for (ulong k = 2; k < 8; k++)
                Assert.DoesNotContain(goals.CandidateWeights(p, ch, new DeterministicRng(k)), x => x.Type == GoalTypes.DiscoverZone && x.Target == Covil);
        }
        Assert.Null(Reason(w));
        ch.Hints.Add(Covil + "/enfrentar_rei");                                     // conhecimento novo
        goals.RefreshZoneBlocks(ch);
        Assert.DoesNotContain(Key(Covil), ch.BlockedTargets);
        Assert.Equal("knowledge", Reason(w));
    }

    [Fact]
    public void NivelQueAlcancaORequisitoQueFaltava_Libera()
    {
        var (w, p, ch) = World(C, "vila_aurora");
        var goals = w.Behavior.Goals;
        var z = C.Regions.Keys.OrderBy(x => x, StringComparer.Ordinal)
            .Select(id => (Id: id, R: goals.Reveal(id))).First(x => x.Id != "vila_aurora" && x.R.Level >= 5 && x.R.Enter.Count + x.R.Explore.Count > 0);
        ch.Level = z.R.Level - 1;                                                    // faltava nivel para a acao que a revela
        Abandon(w, p, ch, z.Id);
        goals.RefreshZoneBlocks(ch);
        Assert.Contains(Key(z.Id), ch.BlockedTargets);
        ch.Level = z.R.Level;
        goals.OnLevelUp(p, ch);
        goals.RefreshZoneBlocks(ch);
        Assert.DoesNotContain(Key(z.Id), ch.BlockedTargets);
        Assert.Equal("level_requirement", Reason(w));
    }

    [Fact]
    public void RotaNova_Libera()
    {
        var (w, p, ch) = World(C, "vila_aurora", "forte_do_vau");
        var goals = w.Behavior.Goals;
        ch.Level = 16;
        Assert.Contains("colinas_do_vale", goals.Reveal(Covil).Explore);             // explorar nas Colinas revela o Covil
        Assert.Contains("mina_velha", goals.Reveal(Covil).Enter);                    // entrar na Mina tambem
        Assert.Equal(3, goals.Reveal(Covil).Level);                                  // Colinas: faixa 3, exploracao sem minimo
        Abandon(w, p, ch, Covil);
        ch.Known.Add("colinas_do_vale");
        goals.RefreshZoneBlocks(ch);
        Assert.DoesNotContain(Key(Covil), ch.BlockedTargets);
        Assert.Equal("route", Reason(w));
    }

    [Fact]
    public void MissaoQuePassaPorUmLugarQueRevela_Libera()
    {
        var (w, p, ch) = World(C, "vila_aurora", "floresta_sussurrante", "colinas_do_vale", "mina_velha", "forte_do_vau");
        var goals = w.Behavior.Goals;
        ch.Level = 16;
        Abandon(w, p, ch, Covil);
        var q = C.Quests.Values.OrderBy(x => x.Id, StringComparer.Ordinal).First(x => x.Steps.Any(s => s.RegionId == "mina_velha"));
        ch.ActiveQuestList.Add(new ActiveQuest(q.Id, w.Clock.Tick, ch.PlayedTicks));
        goals.RefreshZoneBlocks(ch);
        Assert.DoesNotContain(Key(Covil), ch.BlockedTargets);
        Assert.Equal("quest_on_the_way", Reason(w));
    }

    [Fact]
    public void ZonaDescobertaPorOutroCaminho_SaiDoBloqueio()
    {
        var (w, p, ch) = World(C, "vila_aurora", "colinas_do_vale", "mina_velha");
        ch.Level = 16;
        Abandon(w, p, ch, Covil);
        ch.Known.Add(Covil);
        w.Behavior.Goals.RefreshZoneBlocks(ch);
        Assert.DoesNotContain(Key(Covil), ch.BlockedTargets);
        Assert.False(ch.ZoneBlocks.ContainsKey(Covil));
    }

    [Fact]
    public void Desligado_TodaSubidaLibera_ComoNoD066()
    {
        var off = TestSupport.FreshPresetSet();
        off.Population.Goals.ZoneUnblockByRequirement = false;
        var c = ContentDatabase.Create(off);
        var (w, p, ch) = World(c, "vila_aurora", "colinas_do_vale", "mina_velha");
        ch.Level = 16;
        ch.GoalList.Add(new PersonalGoal(GoalTypes.DiscoverZone, Covil, 1, 0, 500, "pref_exploration", 500, w.Clock.Tick, -10_000_000));
        w.Behavior.Goals.Review(p, ch, new DeterministicRng(1));
        Assert.Contains(Key(Covil), ch.BlockedTargets);
        Assert.Empty(ch.ZoneBlocks);
        ch.Level = 17;
        w.Behavior.Goals.OnLevelUp(p, ch);
        Assert.DoesNotContain(Key(Covil), ch.BlockedTargets);
    }
}

using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

/// <summary>
/// Metas de missao (D-065): so para missao ativa ou que pode ser aceita agora; avanco parcial da
/// etapa e o aceite contam como progresso (como o XP nas metas de nivel).
/// </summary>
public class QuestGoalTests
{
    private static ContentDatabase C => TestSupport.Preset;

    private static (WorldState W, Person P, Character Ch) World()
    {
        var e = WorldFactory.Create(C, new WorldConfig { Seed = 61, PopulationSize = 80 });
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(2));
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0);
        var ch = w.GetCharacter(p.CharacterId);
        ch.GoalList.Clear();
        return (w, p, ch);
    }

    [Fact]
    public void Meta_SoParaMissaoAtivaOuQueDaParaAceitar()
    {
        var (w, p, ch) = World();
        // Vagas cheias com missoes que nao sao principais.
        ch.ActiveQuestList.Clear();
        foreach (var q in C.Quests.Values.Where(q => q.Category != QuestCategories.Main).Take(C.QuestSettings.MaxActivePerCharacter))
            ch.ActiveQuestList.Add(new ActiveQuest(q.Id, w.Clock.Tick, ch.PlayedTicks));
        foreach (var z in C.Regions.Keys) ch.Known.Add(z);
        for (ulong s = 1; s <= 20; s++)
            foreach (var x in w.Behavior.Goals.CandidateWeights(p, ch, new DeterministicRng(s)).Where(x => x.Type == GoalTypes.CompleteQuest))
                Assert.Contains(ch.ActiveQuestList, a => a.QuestId == x.Target);   // nunca uma oferta que nao cabe
        // Com vaga, a oferta principal volta a ser candidata.
        ch.ActiveQuestList.RemoveAt(0);
        ch.ActiveQuestList.RemoveAt(0);
        Assert.Contains(w.Behavior.Goals.CandidateWeights(p, ch, new DeterministicRng(1)),
            x => x.Type == GoalTypes.CompleteQuest && C.Quests[x.Target].Category == QuestCategories.Main && ch.ActiveQuestList.All(a => a.QuestId != x.Target));
    }

    [Fact]
    public void AvancoParcialDaEtapa_ContaComoProgresso()
    {
        var (w, p, ch) = World();
        var q = C.Quests.Values.First(q => q.Steps.Count > 0 && q.Steps[0].Minutes > 1 && C.Regions.ContainsKey(q.Steps[0].RegionId));
        ch.ActiveQuestList.Clear();
        ch.ActiveQuestList.Add(new ActiveQuest(q.Id, w.Clock.Tick, ch.PlayedTicks));
        var g = new PersonalGoal(GoalTypes.CompleteQuest, q.Id, 1, 0, 500, "patience", 500, w.Clock.Tick, -1000);
        ch.GoalList.Add(g);
        var region = C.Regions[q.Steps[0].RegionId];
        w.Behavior.Quests.OnActivityDone(ch, region, region.FindActivity(q.Steps[0].ActivityId)!, 1);
        Assert.Equal(ch.PlayedTicksAt(w.Clock.Tick), g.LastProgressPlayedTicks);
        Assert.Equal(0, g.Progress);                    // nao conta etapa
        Assert.Equal(1, ch.ActiveQuests[0].StepMinutes);
    }

    [Fact]
    public void AceitarAMissaoDaMeta_ContaComoProgresso()
    {
        var (w, p, ch) = World();
        ch.ActiveQuestList.Clear();
        var board = C.QuestBoards.Values.OrderBy(b => b.Id, StringComparer.Ordinal).First(b => w.Behavior.Quests.Offers(ch, b).Any());
        var offer = w.Behavior.Quests.Offers(ch, board).First();
        var g = new PersonalGoal(GoalTypes.CompleteQuest, offer.Id, 1, 0, 500, "patience", 500, w.Clock.Tick, -1000);
        ch.GoalList.Add(g);
        var region = C.Regions[board.RegionId];
        w.Behavior.Quests.OnActivityDone(ch, region, region.FindActivity(board.ActivityId)!, 5);
        Assert.Contains(ch.ActiveQuests, a => a.QuestId == offer.Id);
        Assert.Equal(ch.PlayedTicksAt(w.Clock.Tick), g.LastProgressPlayedTicks);
    }

    [Fact]
    public void MetaLargada_NaoVoltaNaMesmaRevisao()
    {
        var (w, p, ch) = World();
        for (ulong k = 1; k <= 30; k++)
        {
            ch.GoalList.Clear();
            var g = new PersonalGoal(GoalTypes.PracticeActivity, ActivityTypes.Hunt, 120, 0, 500, "pref_pve", 500, w.Clock.Tick, -10_000_000);
            ch.GoalList.Add(g);
            w.Behavior.Goals.Review(p, ch, new DeterministicRng(k));   // sem progresso: largada
            Assert.DoesNotContain(ch.Goals, x => x.Type == GoalTypes.PracticeActivity && x.Target == ActivityTypes.Hunt);
        }
    }
}

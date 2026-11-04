using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

/// <summary>Atributos vivos e objetivos pessoais (D-036).</summary>
public class TraitGoalTests
{
    private static SimulationEngine Opened(ulong seed = 42, int population = 100)
    {
        var e = TestSupport.NewEngine(seed, population);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        return e;
    }

    private static (WorldState W, Person P, Character Ch) Someone(int days = 8)
    {
        var e = Opened();
        e.AdvanceTo(SimTime.Days(days));
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0 && !x.IsOnline && x.LastPlayedDay >= 0);
        return (w, p, w.GetCharacter(p.CharacterId));
    }

    private static ActivityDefinition FirstOfType(ContentDatabase c, string type) =>
        c.Regions.Values.OrderBy(r => r.Id, StringComparer.Ordinal).SelectMany(r => r.AllActivities()).First(a => a.Type == type);

    [Fact]
    public void Inatividade_MaisDeUmaSemana_ReduzMecanica_EDuasSemanas_Conhecimento()
    {
        var (w, p, _) = Someone();
        var traits = w.Behavior.Traits;
        int mech = p.Traits.MechanicalSkill, know = p.Traits.GameKnowledge;
        int mechLo = TraitDynamics.Floor(p, "mechanicalSkill");
        int knowLo = TraitDynamics.Floor(p, "gameKnowledge");

        traits.OnDaily(p, p.LastPlayedDay + 7);   // uma semana exata: nada muda
        Assert.Equal(mech, p.Traits.MechanicalSkill);
        traits.OnDaily(p, p.LastPlayedDay + 8);
        Assert.Equal(Math.Max(mechLo, mech - TraitTuning.MechanicalDecayPerDay), p.Traits.MechanicalSkill);
        Assert.Equal(know, p.Traits.GameKnowledge);  // conhecimento so cai depois de duas semanas
        traits.OnDaily(p, p.LastPlayedDay + 15);
        Assert.Equal(Math.Max(knowLo, know - TraitTuning.KnowledgeDecayPerDay), p.Traits.GameKnowledge);
    }

    [Fact]
    public void Pratica_EmCombate_AumentaMecanica_DentroDoLimite()
    {
        var (w, p, ch) = Someone();
        var hunt = FirstOfType(w.Content, ActivityTypes.Hunt);
        p.Practice.Clear();
        p.Traits.MechanicalSkill = p.BaseTraits.MechanicalSkill;
        int before = p.Traits.MechanicalSkill;
        int hi = TraitDynamics.Ceiling(p, "mechanicalSkill");
        w.Behavior.Traits.OnActivity(p, ch, hunt, 10 * TraitTuning.MechanicalPerCombatMinutes, 0, 0, false);
        Assert.InRange(p.Traits.MechanicalSkill - before, 1, 17);   // ~10 pontos, conforme o talento
        w.Behavior.Traits.OnActivity(p, ch, hunt, 10_000_000, 0, 0, false);
        Assert.Equal(hi, p.Traits.MechanicalSkill);   // nunca passa do potencial
    }

    [Fact]
    public void Explorar_E_Descobrir_AumentamConhecimento()
    {
        var (w, p, ch) = Someone();
        p.Practice.Clear();
        p.Traits.GameKnowledge = p.BaseTraits.GameKnowledge;
        int know = p.Traits.GameKnowledge;
        w.Behavior.Traits.OnRegionDiscovered(p);
        Assert.Equal(know + TraitTuning.KnowledgePerDiscovery, p.Traits.GameKnowledge);
        int k2 = p.Traits.GameKnowledge;
        w.Behavior.Traits.OnActivity(p, ch, FirstOfType(w.Content, ActivityTypes.Explore), 6 * TraitTuning.KnowledgePerExploreMinutes, 0, 0, false);
        Assert.True(p.Traits.GameKnowledge > k2);
    }

    [Fact]
    public void AbaterJogador_AumentaPvp()
    {
        var (w, p, _) = Someone();
        int pvp = p.Traits.Pvp;
        w.Behavior.Traits.OnPvpKill(p);
        Assert.Equal(Math.Min(TraitDynamics.Ceiling(p, "pvp"), pvp + 3), p.Traits.Pvp);
    }

    [Fact]
    public void Talento_DefineOPotencial_EAVelocidadeDeAprender()
    {
        var (w, p, ch) = Someone();
        var hunt = FirstOfType(w.Content, ActivityTypes.Hunt);
        int Gain(int talent)
        {
            p.Traits.Talent = talent;
            p.Practice.Clear();
            p.Traits.MechanicalSkill = p.BaseTraits.MechanicalSkill;
            w.Behavior.Traits.OnActivity(p, ch, hunt, 40 * TraitTuning.MechanicalPerCombatMinutes, 0, 0, false);
            return p.Traits.MechanicalSkill - p.BaseTraits.MechanicalSkill;
        }
        int genius = Gain(950), noob = Gain(80);
        Assert.True(genius > noob, $"genio {genius} vs noob {noob}");
        p.Traits.Talent = 950;
        int capGenius = TraitDynamics.Ceiling(p, "mechanicalSkill");
        p.Traits.Talent = 80;
        int capNoob = TraitDynamics.Ceiling(p, "mechanicalSkill");
        Assert.True(capGenius > capNoob + 200 || capGenius == 1000);
        // Gostos e personalidade nao dependem do talento.
        int pveNoob = TraitDynamics.Ceiling(p, "pve");
        p.Traits.Talent = 950;
        Assert.Equal(pveNoob, TraitDynamics.Ceiling(p, "pve"));
    }

    [Fact]
    public void MudancaVisivel_NaEscala_GeraEventoComCausa()
    {
        var (w, p, _) = Someone();
        w.Events.Drain();
        int delta = w.Behavior.Traits.Adjust(p, "pve", p.Traits.Pve > 500 ? -120 : 120, "teste");
        Assert.NotEqual(0, delta);
        var ev = w.Events.Drain().Single(x => x.Type == "trait_changed");
        Assert.StartsWith("pve:", ev.Detail);
        Assert.Equal("teste", ev.Causes[0].Factor);
    }

    [Fact]
    public void Mecanica_RendeMaisXp_EmCombate_ENaoNaColeta()
    {
        var hi = new PersonTraits { MechanicalSkill = 900, GameKnowledge = 500 };
        var lo = new PersonTraits { MechanicalSkill = 100, GameKnowledge = 500 };
        Assert.True(ActivityScoring.TraitXpPermille(hi, ActivityTypes.Hunt) > ActivityScoring.TraitXpPermille(lo, ActivityTypes.Hunt));
        Assert.Equal(ActivityScoring.TraitXpPermille(hi, ActivityTypes.Gather), ActivityScoring.TraitXpPermille(lo, ActivityTypes.Gather));
        // Conhecimento rende um pouco em tudo.
        var know = new PersonTraits { MechanicalSkill = 500, GameKnowledge = 1000 };
        Assert.True(ActivityScoring.TraitXpPermille(know, ActivityTypes.Gather) > 1000);
    }

    [Fact]
    public void Objetivos_SaoAdotados_ESeguemOsTracos()
    {
        var e = Opened(7, 300);
        var discover = new Dictionary<bool, (int Goals, int Discover)> { [true] = (0, 0), [false] = (0, 0) };
        for (int d = 0; d < 21; d++)
        {
            e.AdvanceTo(SimTime.Days(d + 1));
            foreach (var ev in e.World.Events.Drain().Where(x => x.Type == "goal_set"))
            {
                var ch = e.World.GetCharacter((int)ev.Entities[0].Id);
                var t = e.World.GetPerson(ch.PersonId).BaseTraits;
                if (t.Exploration is > 350 and < 650) continue;
                bool explorer = t.Exploration >= 650;
                var (g, disc) = discover[explorer];
                // O motivo do objetivo (1a causa) e o traco que mais pesou na escolha.
                discover[explorer] = (g + 1, disc + (ev.Causes[0].Factor == "exploration" ? 1 : 0));
            }
        }
        Assert.True(discover[true].Goals > 20 && discover[false].Goals > 20, $"amostra pequena: {discover[true].Goals}/{discover[false].Goals}");
        // Exploradores escolhem objetivos motivados pela exploracao com mais frequencia: tendencia, nao regra.
        Assert.True(discover[true].Discover * 1000 / discover[true].Goals > discover[false].Discover * 1000 / discover[false].Goals,
            $"exploradores {discover[true].Discover}/{discover[true].Goals} vs outros {discover[false].Discover}/{discover[false].Goals}");
    }

    [Fact]
    public void Objetivo_DaBonusSoAsOpcoesQueAjudam_ELimitado()
    {
        var (w, p, ch) = Someone();
        ch.GoalList.Clear();
        ch.GoalList.Add(new PersonalGoal(GoalTypes.PracticeActivity, ActivityTypes.Gather, 300, 0, 1000, "trade", 600, w.Clock.Tick, 0));
        var region = w.Content.Regions.Values.First(r => r.AllActivities().Any(a => a.Type == ActivityTypes.Gather));
        var gather = region.AllActivities().First(a => a.Type == ActivityTypes.Gather);
        var hunt = FirstOfType(w.Content, ActivityTypes.Hunt);
        var goals = w.Behavior.Goals;
        int bonus = goals.Bonus(ch, region, gather, true, 800);
        Assert.Equal(GoalTuning.WeightGoal, bonus);
        Assert.Equal(0, goals.Bonus(ch, region, hunt, true, 800));
        // Comprometimento menor, influencia menor.
        ch.GoalList.Clear();
        ch.GoalList.Add(new PersonalGoal(GoalTypes.PracticeActivity, ActivityTypes.Gather, 300, 0, 500, "trade", 600, w.Clock.Tick, 0));
        Assert.Equal(GoalTuning.WeightGoal / 2, goals.Bonus(ch, region, gather, true, 800));
    }

    [Fact]
    public void Objetivo_DeNivel_ConcluiEReforcaOMotivo()
    {
        var (w, p, ch) = Someone();
        ch.GoalList.Clear();
        var g = new PersonalGoal(GoalTypes.ReachLevel, "", ch.Level + 1, ch.Level, 800, "pve", p.Traits.Pve, w.Clock.Tick, 0);
        ch.GoalList.Add(g);
        int completed = ch.GoalsCompleted, pve = p.Traits.Pve;
        ch.Level++;
        w.Events.Drain();
        w.Behavior.Goals.OnLevelUp(p, ch);
        Assert.DoesNotContain(g, ch.Goals);
        Assert.Equal(completed + 1, ch.GoalsCompleted);
        Assert.Contains(w.Events.Drain(), x => x.Type == "goal_completed");
        Assert.Equal(Math.Min(TraitDynamics.Ceiling(p, "pve"), pve + TraitTuning.GoalMotiveReinforce), p.Traits.Pve);
    }

    [Fact]
    public void Objetivo_SemProgresso_EAbandonado()
    {
        var (w, p, ch) = Someone();
        ch.GoalList.Clear();
        var g = new PersonalGoal(GoalTypes.PracticeActivity, ActivityTypes.Gather, 300, 0, 800, "trade", 600, w.Clock.Tick, ch.PlayedTicksAt(w.Clock.Tick));
        ch.GoalList.Add(g);
        ch.PlayedTicks += 20 * SimTime.TicksPerHour;   // 20 h de jogo sem avancar (mais que o maximo de paciencia)
        int abandoned = ch.GoalsAbandoned;
        w.Behavior.Goals.Review(p, ch, new DeterministicRng(123));
        Assert.DoesNotContain(g, ch.Goals);
        Assert.Equal(abandoned + 1, ch.GoalsAbandoned);
    }

    [Fact]
    public void AtributosEObjetivos_SaoDeterministicos_EEntramNoHash()
    {
        string Run()
        {
            var e = Opened(11, 80);
            e.AdvanceTo(SimTime.Days(14));
            Assert.True(e.World.Characters.Sum(c => c.GoalsSet) > 0);
            Assert.Contains(e.World.Persons, p => PersonTraits.DynamicKeys.Any(k => p.Traits.Get(k) != p.BaseTraits.Get(k)));
            return StateHasher.Compute(e.World);
        }
        Assert.Equal(Run(), Run());
    }
}

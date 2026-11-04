using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

/// <summary>Plano da sessao, continuidade e tedio contextual (D-026/D-037).</summary>
public class SessionPlanTests
{
    private static SimulationEngine Run(ulong seed, int population, int days)
    {
        var e = TestSupport.NewEngine(seed, population);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(days));
        return e;
    }

    private static SessionSettingsDefinition S => TestSupport.Preset.Session;

    private static ScoredOption Option(string activityId, params (string Key, int Value)[] factors)
    {
        var region = TestSupport.Preset.Regions.Values.OrderBy(r => r.Id, StringComparer.Ordinal).First();
        var a = region.AllActivities().First();
        var list = factors.Select(f => new DecisionFactor(f.Key, f.Value)).ToList();
        return new ScoredOption(region, a, TravelPlan.Unreachable, list.Sum(f => f.Contribution), list);
    }

    [Fact]
    public void Tedio_CresceNoTipo_AliviaOsOutros_ESeDissipaEntreSessoes()
    {
        var ch = new Character(1, 1, "x", "mago");
        var t = new PersonTraits { Exploration = 500, Pve = 500, Trade = 500 };
        SessionRules.AccrueBoredom(ch, t, ActivityTypes.Hunt, 120, S);
        int hunt = ch.BoredomByType[ActivityTypes.Hunt];
        Assert.True(hunt > 0);
        SessionRules.AccrueBoredom(ch, t, ActivityTypes.Gather, 30, S);
        Assert.True(ch.BoredomByType[ActivityTypes.Hunt] < hunt);   // fazer outra coisa alivia
        int before = ch.BoredomByType[ActivityTypes.Hunt];
        SessionRules.FadeBoredomBetweenSessions(ch, S);
        Assert.Equal(before * S.BoredomKeptBetweenSessions / 1000, ch.BoredomByType.GetValueOrDefault(ActivityTypes.Hunt));
    }

    [Fact]
    public void Tedio_MaisRapidoParaQuemGostaDeVariedade_EMaisLentoParaQuemGostaDoTipo()
    {
        int Gain(PersonTraits t)
        {
            var ch = new Character(1, 1, "x", "mago");
            SessionRules.AccrueBoredom(ch, t, ActivityTypes.Hunt, 90, S);
            return ch.BoredomByType.GetValueOrDefault(ActivityTypes.Hunt);
        }
        Assert.True(Gain(new PersonTraits { Exploration = 900, Pve = 500 }) > Gain(new PersonTraits { Exploration = 100, Pve = 500 }));
        Assert.True(Gain(new PersonTraits { Exploration = 500, Pve = 900 }) < Gain(new PersonTraits { Exploration = 500, Pve = 100 }));
    }

    [Fact]
    public void Paciencia_AliviaOPesoDoTedio()
    {
        var ch = new Character(1, 1, "x", "mago");
        ch.Boredom[ActivityTypes.Hunt] = 800;
        int impatient = ActivityScoring.BoredomPenalty(ch, new PersonTraits { Patience = 0 }, ActivityTypes.Hunt, S);
        int patient = ActivityScoring.BoredomPenalty(ch, new PersonTraits { Patience = 1000 }, ActivityTypes.Hunt, S);
        Assert.Equal(800 * S.BoredomWeight / 1000, impatient);
        Assert.True(patient < impatient);
        Assert.Equal(0, ActivityScoring.BoredomPenalty(ch, new PersonTraits(), ActivityTypes.Gather, S));
    }

    [Fact]
    public void MotivoDaTroca_EOFatorComMaiorDiferenca_SemContarRuido()
    {
        var focus = Option("a", ("affinity", 300), ("boredom", -200), ("noise", 40));
        var other = Option("b", ("affinity", 250), ("goal", 120), ("noise", -40));
        Assert.Equal("boredom", SessionRules.MainDifference(other, focus));   // +200 contra +120 da meta
        Assert.Equal("affinity", SessionRules.MainReason(focus));
    }

    [Fact]
    public void Sessoes_FormamPlano_EContinuamSoDentroDaMargem()
    {
        var e = Run(5, 150, 6);
        var events = e.World.Events.Drain().ToList();
        Assert.Contains(events, x => x.Type == "session_plan");
        var changes = events.Where(x => x.Type == "plan_changed").ToList();
        Assert.NotEmpty(changes);
        Assert.All(changes, x => Assert.False(string.IsNullOrEmpty(x.Causes[0].Factor)));
        // Acontecimentos relevantes liberam a revisao (ex.: etapa de missao, nivel).
        Assert.Contains(changes, x => x.Causes[0].Factor is "quest_step" or "level_up" or "quest_completed");
        // Quem seguiu o plano tinha alternativa melhor por menos que a margem do compromisso.
        var continued = events.Where(x => x.Type == "activity_chosen" && x.Causes.Count > 0 && x.Causes[0].Factor == "plan_continuity").ToList();
        Assert.NotEmpty(continued);
        Assert.All(continued, x => Assert.InRange(x.Causes[0].WeightPermille, 0, S.CommitMargin - 1));
    }

    [Fact]
    public void ExplorarSemNadaNovo_PerdeParteDaGraca()
    {
        var c = TestSupport.Preset;
        var ch = new Character(1, 1, "x", "mago");
        var region = c.Regions.Values.First(r => r.AllActivities().Any(a => a.Type == ActivityTypes.Explore));
        Assert.True(ActivityScoring.ExploreRevealsSomething(ch, region, c));
        foreach (var id in c.Regions.Keys) ch.Known.Add(id);
        Assert.False(ActivityScoring.ExploreRevealsSomething(ch, region, c));
    }

    [Fact]
    public void PlanoETedio_SaoDeterministicos_EEntramNoHash()
    {
        string Hash()
        {
            var e = Run(9, 80, 5);
            Assert.Contains(e.World.Characters, c => c.BoredomByType.Count > 0);
            Assert.True(e.World.Characters.Sum(c => c.PlansFormed) > 0);
            return StateHasher.Compute(e.World);
        }
        Assert.Equal(Hash(), Hash());
    }

    [Fact]
    public void ParametrosDaSessao_InvalidosSaoErro()
    {
        var set = TestSupport.FreshPresetSet();
        set.Population.Session.PatienceRelief = 2000;
        set.Population.Session.SwitchMargin = -1;
        var errors = ContentValidator.Validate(set).Where(i => i.Severity == IssueSeverity.Error).ToList();
        Assert.Contains(errors, x => x.Path == "session.patienceRelief");
        Assert.Contains(errors, x => x.Path == "session.switchMargin");
    }
}

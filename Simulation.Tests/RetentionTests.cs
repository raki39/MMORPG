using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Events;
using Simulation.Core.Population;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

/// <summary>Retencao: engajamento na rotina, sessoes flexiveis, inatividade, desistencia e retorno (D-039).</summary>
public class RetentionTests
{
    private static SimulationEngine Opened(ContentDatabase? content = null, ulong seed = 42, int population = 100)
    {
        var e = content == null ? TestSupport.NewEngine(seed, population)
            : WorldFactory.Create(content, new WorldConfig { Seed = seed, PopulationSize = population });
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        return e;
    }

    private static RetentionSettingsDefinition R => TestSupport.Preset.Retention;

    /// <summary>Avanca ate a proxima meia-noite e devolve os eventos do periodo.</summary>
    private static List<SimEvent> NextMidnight(SimulationEngine e)
    {
        var cal = e.World.Clock.Calendar;
        e.AdvanceTo(cal.StartOfDay(cal.DayIndex(e.World.Clock.Tick) + 1) + 1);
        return e.World.Events.Drain().ToList();
    }

    private static Person Offline(SimulationEngine e) =>
        e.World.Persons.First(p => p.CharacterId > 0 && p.State == LifecycleState.Offline);

    [Fact]
    public void Engajamento_MudaAChanceDeJogar_ComPisoETeto()
    {
        Assert.Equal(1000, BehaviorSystem.PlayFactor(R.EngagementNeutral, R));
        Assert.True(BehaviorSystem.PlayFactor(R.EngagementNeutral - 200, R) < 1000);
        Assert.True(BehaviorSystem.PlayFactor(R.EngagementNeutral + 200, R) > 1000);
        var strong = new RetentionSettingsDefinition { EngagementNeutral = 500, EngagementEffect = 5000, MinPlayFactor = 150, MaxPlayFactor = 1300 };
        Assert.Equal(150, BehaviorSystem.PlayFactor(0, strong));
        Assert.Equal(1300, BehaviorSystem.PlayFactor(1000, strong));
    }

    [Fact]
    public void SemJogarPorDuasSemanas_ViraInativo_EAoVoltarRegistraORetorno()
    {
        var e = Opened();
        e.AdvanceTo(SimTime.Days(20) - SimTime.Minutes(1));   // 23h59: ninguem mais comeca a jogar hoje
        var w = e.World;
        var p = Offline(e);
        p.LastPlayedDay = 20 - R.InactiveAfterDays;
        var events = NextMidnight(e);
        Assert.Equal(LifecycleState.Inactive, p.State);
        Assert.Contains(events, x => x.Type == "player_inactive" && x.Entities[0].Id == p.Id);
        // Inativo que volta a jogar registra o retorno (forcado aqui por uma sessao agendada).
        w.Scheduler.Schedule(w.Clock.Tick + SimTime.Minutes(5), BehaviorSystem.EvSessionStart, p.Id, 30);
        e.AdvanceTo(w.Clock.Tick + SimTime.Minutes(10));
        Assert.Equal(LifecycleState.Online, p.State);
        Assert.Contains(w.Events.Drain(), x => x.Type == "player_returned" && x.Causes[0].Factor == "came_back");
        Assert.Equal(1, p.TimesReturned);
    }

    [Fact]
    public void InativoPorMuitoTempo_Desiste()
    {
        var e = Opened();
        e.AdvanceTo(SimTime.Days(R.QuitAfterInactiveDays + 5) - SimTime.Minutes(1));
        var w = e.World;
        var p = Offline(e);
        p.State = LifecycleState.Inactive;
        p.LastPlayedDay = 5;
        var events = NextMidnight(e);
        Assert.Equal(LifecycleState.Quit, p.State);
        var quit = events.Single(x => x.Type == "player_quit" && x.Entities[0].Id == p.Id);
        Assert.Equal("long_inactivity", quit.Causes[0].Factor);
    }

    [Fact]
    public void IntencaoBaixa_LevaADesistir_ComOQueMaisIncomodava_EDesistenteNaoJoga()
    {
        var e = Opened();
        e.AdvanceTo(SimTime.Days(5));
        var w = e.World;
        var p = Offline(e);
        List<SimEvent> all = new();
        for (int d = 0; d < 60 && p.State != LifecycleState.Quit; d++)
        {
            p.Mood.Satisfaction = 0;
            p.Mood.Frustration = 1000;
            p.Drivers["novelty"] = -900;
            p.Drivers["boredom"] = -400;
            all.AddRange(NextMidnight(e));
        }
        Assert.Equal(LifecycleState.Quit, p.State);
        var quit = all.Single(x => x.Type == "player_quit" && x.Entities[0].Id == p.Id);
        Assert.Equal("low_intention", quit.Causes[0].Factor);
        Assert.Equal("novelty", quit.Causes[1].Factor);   // o que mais incomodava
        // Desistente nao conecta mais.
        e.AdvanceTo(w.Clock.Tick + SimTime.Days(10));
        Assert.DoesNotContain(w.Events.Drain(), x => x.Type == "session_started" && x.Entities[0].Id == p.Id);
        Assert.False(p.IsOnline);
    }

    [Fact]
    public void Desistente_PodeVoltarPelaSaudade_ComRecomeco()
    {
        var set = TestSupport.FreshPresetSet();
        set.Population.Retention.QuitReturnChancePerDay = 1000;
        set.Population.Retention.QuitReturnMinDays = 2;
        var e = Opened(ContentDatabase.Create(set));
        e.AdvanceTo(SimTime.Days(5));
        var w = e.World;
        var p = Offline(e);
        long today = w.Clock.Calendar.DayIndex(w.Clock.Tick);
        p.State = LifecycleState.Quit;
        p.QuitDay = today - 3;
        p.Traits.Loyalty = 1000;
        p.Mood.Frustration = 700;
        w.Behavior.Experience.Remember(p, "main_quest", "saudade", 1000, 1000);
        var events = NextMidnight(e);
        Assert.NotEqual(LifecycleState.Quit, p.State);
        var back = events.Single(x => x.Type == "player_returned" && x.Entities[0].Id == p.Id);
        Assert.Equal("nostalgia", back.Causes[0].Factor);
        Assert.Equal(0, p.Mood.Frustration);
        Assert.True(p.Mood.Satisfaction >= set.Population.Retention.ReturnSatisfaction);
    }

    [Fact]
    public void SessoesBoas_SeEstendemUmaVez()
    {
        var e = Opened(seed: 3, population: 150);
        e.AdvanceTo(SimTime.Days(8));
        var events = e.World.Events.Drain().ToList();
        var ext = events.Where(x => x.Type == "session_extended").ToList();
        Assert.NotEmpty(ext);
        Assert.All(ext, x => Assert.True(x.Causes[0].WeightPermille >= R.ExtendScoreAbove));
    }

    [Fact]
    public void Retencao_EDeterministica_EEntraNoHash()
    {
        string Hash()
        {
            var e = Opened(seed: 17, population: 80);
            e.AdvanceTo(SimTime.Days(12));
            return StateHasher.Compute(e.World);
        }
        Assert.Equal(Hash(), Hash());
    }

    [Fact]
    public void ParametrosDeRetencao_InvalidosSaoErro()
    {
        var set = TestSupport.FreshPresetSet();
        set.Population.Retention.QuitAfterInactiveDays = 5;   // menor que inactiveAfterDays
        set.Population.Retention.ExtendChance = 2000;
        var errors = ContentValidator.Validate(set).Where(i => i.Severity == IssueSeverity.Error).ToList();
        Assert.Contains(errors, x => x.Path == "retention.quitAfterInactiveDays");
        Assert.Contains(errors, x => x.Path == "retention.extendChance");
    }
}

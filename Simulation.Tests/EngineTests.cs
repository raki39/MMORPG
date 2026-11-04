using Simulation.Core.Commands;
using Simulation.Core.Engine;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

public class EngineTests
{
    [Fact]
    public void Comando_SemTick_EhAplicadoNoProximoLimite()
    {
        var engine = TestSupport.NewEngine();
        engine.AdvanceTo(500);
        var env = engine.Submit(new SetServerOpenCommand(true));
        Assert.Equal(501, env.ScheduledTick);
        Assert.False(engine.World.Server.IsOpen);

        engine.Step();
        Assert.Equal(CommandStatus.Applied, env.Status);
        Assert.Equal(501, env.AppliedTick);
        Assert.True(engine.World.Server.IsOpen);
        Assert.Equal(501, engine.World.Server.LastOpenedTick);
    }

    [Fact]
    public void Comando_NoPassado_EhRecusado()
    {
        var engine = TestSupport.NewEngine();
        engine.AdvanceTo(100);
        Assert.Throws<ArgumentOutOfRangeException>(() => engine.Submit(new SetServerOpenCommand(true), 100));
    }

    [Fact]
    public void ComandoInvalido_EhRejeitadoERegistrado()
    {
        var engine = TestSupport.NewEngine();
        engine.Submit(new SetServerOpenCommand(false), 10);
        engine.AdvanceTo(20);
        var env = Assert.Single(engine.World.Commands.History);
        Assert.Equal(CommandStatus.Rejected, env.Status);
        Assert.Equal("O servidor já está fechado.", env.RejectionReason);
        Assert.Equal(10, env.AppliedTick);
    }

    [Fact]
    public void ComandosNoMesmoTick_SeguemASequencia()
    {
        var engine = TestSupport.NewEngine();
        var open = engine.Submit(new SetServerOpenCommand(true), 50);
        var close = engine.Submit(new SetServerOpenCommand(false), 50);
        engine.AdvanceTo(60);
        Assert.Equal(CommandStatus.Applied, open.Status);
        Assert.Equal(CommandStatus.Applied, close.Status);
        Assert.False(engine.World.Server.IsOpen);
    }

    [Fact]
    public void ComandoAgendadoAntes_DeOutroSubmetidoDepois_RodaPrimeiro()
    {
        var engine = TestSupport.NewEngine();
        // Submetido primeiro, mas agendado para depois: a ordem e pelo tick, nao pela submissao.
        var late = engine.Submit(new SetServerOpenCommand(false), 200);
        var early = engine.Submit(new SetServerOpenCommand(true), 100);
        engine.AdvanceTo(300);
        Assert.Equal(100, early.AppliedTick);
        Assert.Equal(CommandStatus.Applied, early.Status);
        Assert.Equal(CommandStatus.Applied, late.Status);
        Assert.False(engine.World.Server.IsOpen);
    }

    [Fact]
    public void EventoDeAbertura_RegistraCausaNoComando()
    {
        var engine = TestSupport.NewEngine();
        var env = engine.Submit(new SetServerOpenCommand(true), 10);
        engine.AdvanceTo(10);
        var ev = Assert.Single(engine.World.Events.Drain());
        Assert.Equal("server_opened", ev.Type);
        Assert.Equal(10, ev.Tick);
        var cause = Assert.Single(ev.Causes);
        Assert.Equal(env.Sequence, cause.SourceCommandSequence);
    }

    [Fact]
    public void FechamentoDiario_RegistraCadaDia()
    {
        var engine = TestSupport.NewEngine();
        engine.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        engine.AdvanceTo(SimTime.Days(7));
        var h = engine.World.History;
        Assert.Equal(7, h.Count);
        Assert.Equal(Enumerable.Range(0, 7).Select(i => (long)i), h.Select(d => d.DayIndex));
        Assert.Equal(new DateTime(2027, 3, 1), h[0].Date);
        Assert.Equal(DayKind.Weekend, h[5].DayKind);
        Assert.Equal(1, h[0].CommandsApplied);
        Assert.Equal(0, h[1].CommandsApplied);
        Assert.All(h, d => Assert.Equal(100, d.PersonsTotal));
        Assert.True(h[0].ServerOpen);
    }

    private static string RunScripted(ulong seed, bool noSkip, long ticks)
    {
        var engine = TestSupport.NewEngine(seed);
        engine.DisableTickSkipping = noSkip;
        engine.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        engine.Submit(new SetServerOpenCommand(true), SimTime.Hours(9));
        engine.Submit(new SetServerOpenCommand(false), SimTime.Days(1) + SimTime.Hours(3));
        engine.AdvanceTo(ticks);
        return StateHasher.Compute(engine.World);
    }

    [Fact]
    public void Determinismo_MesmaSeedEComandos_MesmoHash()
    {
        long t = SimTime.Days(3);
        Assert.Equal(RunScripted(42, false, t), RunScripted(42, false, t));
    }

    [Fact]
    public void Determinismo_SeedDiferente_HashDiferente()
    {
        long t = SimTime.Days(1);
        Assert.NotEqual(RunScripted(42, false, t), RunScripted(43, false, t));
    }

    [Fact]
    public void SaltoDeTicksOciosos_EquivaleAProcessarTickATick()
    {
        long t = SimTime.Days(2);
        var skip = TestSupport.NewEngine();
        var full = TestSupport.NewEngine();
        full.DisableTickSkipping = true;
        foreach (var e in new[] { skip, full })
        {
            e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
            e.Submit(new SetServerOpenCommand(false), SimTime.Days(1) + 7);
            e.AdvanceTo(t);
        }
        Assert.Equal(StateHasher.Compute(full.World), StateHasher.Compute(skip.World));
        Assert.Equal(t, full.TicksExecuted);
        Assert.True(skip.TicksExecuted < 10);
    }

    [Fact]
    public void StepRepetido_EquivaleAAdvanceTo()
    {
        var a = TestSupport.NewEngine();
        var b = TestSupport.NewEngine();
        a.Submit(new SetServerOpenCommand(true), 5);
        b.Submit(new SetServerOpenCommand(true), 5);
        for (int i = 0; i < 1000; i++) a.Step();
        b.AdvanceTo(1000);
        Assert.Equal(StateHasher.Compute(a.World), StateHasher.Compute(b.World));
    }

    [Fact]
    public void Replay_DoRegistroDeComandos_ReproduzOEstado()
    {
        long t = SimTime.Days(2);
        var original = TestSupport.NewEngine();
        original.AdvanceTo(SimTime.Hours(7));
        original.Submit(new SetServerOpenCommand(true));                        // submetido "ao vivo"
        original.AdvanceTo(SimTime.Hours(20));
        original.Submit(new SetServerOpenCommand(true), SimTime.Hours(21));     // sera rejeitado
        original.Submit(new SetServerOpenCommand(false), SimTime.Days(1) + 100);
        original.AdvanceTo(t);

        var replay = TestSupport.NewEngine();
        foreach (var env in original.World.Commands.History)
            replay.Submit(env.Command, env.ScheduledTick);
        replay.AdvanceTo(t);

        Assert.Equal(StateHasher.Compute(original.World), StateHasher.Compute(replay.World));
    }

    [Fact]
    public void Hash_EhSensivelAoEstadoDasPessoas()
    {
        var a = TestSupport.NewEngine();
        var b = TestSupport.NewEngine();
        Assert.Equal(StateHasher.Compute(a.World), StateHasher.Compute(b.World));
        b.World.GetPerson(50).Mood.Satisfaction += 1;
        Assert.NotEqual(StateHasher.Compute(a.World), StateHasher.Compute(b.World));
    }

    [Fact]
    public void RelogioNaoVoltaNoTempo()
    {
        var engine = TestSupport.NewEngine();
        engine.AdvanceTo(100);
        Assert.Throws<ArgumentOutOfRangeException>(() => engine.AdvanceTo(99));
    }
}

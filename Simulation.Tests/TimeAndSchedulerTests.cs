using Simulation.Core.Scheduling;
using Simulation.Core.Time;

namespace Simulation.Tests;

public class TimeAndSchedulerTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]     // duracao positiva dura ao menos 1 tick
    [InlineData(49, 1)]
    [InlineData(50, 1)]
    [InlineData(149, 1)]
    [InlineData(150, 2)]   // meio tick arredonda para cima
    [InlineData(2000, 20)]
    [InlineData(6000, 60)]
    public void MillisecondsToTicks_PoliticaDeArredondamento(long ms, long ticks)
    {
        Assert.Equal(ticks, SimTime.MillisecondsToTicks(ms));
    }

    [Fact]
    public void MillisecondsToTicks_NegativoEhErro()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SimTime.MillisecondsToTicks(-1));
    }

    [Fact]
    public void Calendario_ConverteTicksEmDatas()
    {
        var cal = new CampaignCalendar(new DateTime(2027, 3, 1), new[] { new Holiday(3, 26, 2027, "Sexta-feira Santa"), new Holiday(12, 25, null, "Natal") });
        Assert.Equal(new DateTime(2027, 3, 1, 0, 0, 0), cal.ToDateTime(0));
        Assert.Equal(new DateTime(2027, 3, 1, 0, 0, 0, 100), cal.ToDateTime(1));
        Assert.Equal(new DateTime(2027, 3, 2), cal.ToDateTime(SimTime.TicksPerDay));
        Assert.Equal(new DateTime(2027, 3, 1, 20, 30, 0), cal.ToDateTime(SimTime.Hours(20) + SimTime.Minutes(30)));
        Assert.Equal(864_000, SimTime.TicksPerDay);

        Assert.Equal(DayKind.Weekday, cal.KindOf(new DateTime(2027, 3, 1)));
        Assert.Equal(DayKind.Weekend, cal.KindOf(new DateTime(2027, 3, 6)));
        Assert.Equal(DayKind.Holiday, cal.KindOf(new DateTime(2027, 3, 26)));
        Assert.Equal(DayKind.Weekday, cal.KindOf(new DateTime(2028, 3, 26).AddDays(1))); // feriado de data unica nao repete
        Assert.Equal(DayKind.Holiday, cal.KindOf(new DateTime(2031, 12, 25)));           // recorrente repete
    }

    [Fact]
    public void Agendador_OrdenaPorTickEDepoisPorInsercao()
    {
        var s = new EventScheduler();
        s.Schedule(50, "c");
        s.Schedule(10, "a1");
        s.Schedule(30, "b");
        s.Schedule(10, "a2");
        s.Schedule(10, "a3");

        var order = new List<string>();
        while (s.TryDequeueDue(long.MaxValue, out var ev)) order.Add(ev.Type);
        Assert.Equal(new[] { "a1", "a2", "a3", "b", "c" }, order);
    }

    [Fact]
    public void Agendador_SoEntregaEventosVencidos()
    {
        var s = new EventScheduler();
        s.Schedule(100, "x");
        Assert.False(s.TryDequeueDue(99, out _));
        Assert.Equal(100, s.PeekTick);
        Assert.True(s.TryDequeueDue(100, out var ev));
        Assert.Equal("x", ev.Type);
        Assert.Equal(long.MaxValue, s.PeekTick);
    }

    [Fact]
    public void Agendador_MuitosEventos_SaemOrdenados()
    {
        var s = new EventScheduler();
        var rng = new Simulation.Core.Random.DeterministicRng(3);
        for (int i = 0; i < 5000; i++) s.Schedule(rng.NextInt(1000), "e", payload: i);
        long lastTick = -1, lastSeq = -1;
        while (s.TryDequeueDue(long.MaxValue, out var ev))
        {
            Assert.True(ev.Tick > lastTick || (ev.Tick == lastTick && ev.Sequence > lastSeq));
            lastTick = ev.Tick;
            lastSeq = ev.Sequence;
        }
    }
}

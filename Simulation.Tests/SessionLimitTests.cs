using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Population;
using Simulation.Core.Time;

namespace Simulation.Tests;

/// <summary>
/// R2: o limite absoluto vale para a saida real. Nenhuma sessao invade preparo, deslocamento,
/// obrigacao incompativel ou compromisso; sobreposicao com o trabalho so na pausa ou na escapada
/// autorizadas do trabalho remoto; o sono so e consumido dentro do corte autorizado.
/// </summary>
public class SessionLimitTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private static RoutineSettingsDefinition RS => C.Routine;

    private sealed record Played(Person P, long Start, long End, string Kind);

    private static (List<Played> Sessions, Simulation.Core.World.WorldState W) Run(ulong seed, int population, int days)
    {
        var e = TestSupport.NewEngine(seed, population);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        var w = e.World;
        var open = new Dictionary<int, (long Start, string Kind)>();
        var list = new List<Played>();
        for (int d = 0; d < days; d++)
        {
            e.AdvanceTo(SimTime.Days(d + 1));
            foreach (var ev in w.Events.Drain())
            {
                if (ev.Type == "session_started")
                    open[(int)ev.Entities[0].Id] = (ev.Tick, ev.Causes.Count > 0 ? ev.Causes[0].Factor : "normal");
                else if (ev.Type == "session_ended" && open.Remove((int)ev.Entities[0].Id, out var s))
                    list.Add(new Played(w.GetPerson((int)ev.Entities[0].Id), s.Start, ev.Tick, s.Kind));
            }
        }
        return (list, w);
    }

    /// <summary>Dia acordado que contem o instante e o minuto nas coordenadas desse dia.</summary>
    private static (long Day, DailySchedule.DayLayout Layout, int Minute) WakingDay(Simulation.Core.World.WorldState w, Person p, long tick)
    {
        long cal = BehaviorSystem.LocalDay(p, tick);
        int minute = (int)((tick - BehaviorSystem.LocalDayStart(p, cal)) / SimTime.TicksPerMinute);
        var day = w.Behavior.LayoutOf(p, cal);
        if (minute < day.Wake) { cal--; minute += 1440; day = w.Behavior.LayoutOf(p, cal); }
        return (cal, day, minute);
    }

    [Fact]
    public void NenhumaSessao_InvadeDeslocamentoPreparoOuObrigacao_ESonoSoDentroDoCorteAutorizado()
    {
        var (sessions, w) = Run(31, 900, 28);
        Assert.True(sessions.Count > 2000, $"amostra {sessions.Count}");
        int cutChecked = 0;
        foreach (var s in sessions)
        {
            var (_, day, start) = WakingDay(w, s.P, s.Start);
            int end = start + (int)((s.End - s.Start) / SimTime.TicksPerMinute);
            foreach (var part in day.Parts.Where(x => x.Start < end && start < x.End))
            {
                // A unica sobreposicao permitida e com o trabalho remoto, na pausa ou na escapada.
                Assert.True(part.Kind == "remote" && s.Kind is "work_break" or "during_work",
                    $"{s.P.OccupationId}: sessão {s.Kind} {start / 60}:{start % 60:00}–{end / 60}:{end % 60:00} invade {part.Kind} {part.Start / 60}:{part.Start % 60:00}");
                if (s.Kind == "work_break")
                    Assert.Contains(day.Remote, r => r.Breaks.Any(b => start >= b.Start && end <= b.End));
            }
            if (end > day.Bed && s.Kind == "normal")
            {
                // Passou da hora de deitar: so dentro do teto por noite (a noite fecha com o sono minimo).
                Assert.True(end - day.Bed <= RS.MaxSleepCutMinutes, $"cortou {end - day.Bed} min de sono");
                cutChecked++;
            }
        }
        Assert.True(cutChecked > 5, $"sessões que avançaram sobre o sono: {cutChecked}");
    }

    [Fact]
    public void NoiteComCorteDeSono_MantemOSonoMinimo()
    {
        var (sessions, w) = Run(32, 700, 28);
        int nights = 0;
        foreach (var s in sessions.Where(x => x.Kind == "normal"))
        {
            var (d, day, _) = WakingDay(w, s.P, s.Start);
            long bed = BehaviorSystem.LocalDayStart(s.P, d) + day.Bed * SimTime.TicksPerMinute;
            if (s.End <= bed) continue;
            var rec = s.P.DayPlans.FirstOrDefault(r => r.LocalDay == d + 1);
            if (rec == null) continue;
            var next = w.Behavior.LayoutOf(s.P, d + 1);
            long wake = BehaviorSystem.LocalDayStart(s.P, d + 1) + next.Wake * SimTime.TicksPerMinute;
            if (wake > w.Clock.Tick) continue;   // noite ainda nao fechada
            Assert.True(rec.SleepMinutes >= RS.MinSleepMinutes || next.Wake - next.SleepStart < RS.MinSleepMinutes,
                $"{s.P.OccupationId}: dormiu {rec.SleepMinutes} min depois de jogar até tarde");
            nights++;
        }
        Assert.True(nights > 5, $"noites conferidas: {nights}");
    }

    [Fact]
    public void ChamadoDoTrabalho_ToleranciaPropriaENaoPassaDoLimite()
    {
        Assert.Equal(5, RS.WorkCallToleranceMinutes);
        var (sessions, _) = Run(33, 1500, 21);
        var slack = sessions.Where(x => x.Kind == "during_work").ToList();
        Assert.NotEmpty(slack);
        // A escapada nunca passa do fim planejado (limite absoluto = fim planejado: volta ao trabalho).
        Assert.All(slack, x => Assert.True((x.End - x.Start) / SimTime.TicksPerMinute <= 120));
    }

    [Fact]
    public void Probabilidade_CombinaOsFatoresELimitaSoNoFim_EAEstimativaUsaAMesmaPolitica()
    {
        // Um hardcore engajado e com habito alto teria 146% sem limite; a estimativa e o planejador ficam em 100%.
        var p = PopulationGenerator.Generate(1, C, 1);
        p.OccupationId = "sem_ocupacao";
        p.Traits.GameImportance = 950;
        p.Traits.Habit = 1000;
        var week = Enumerable.Range(0, 7).Select(d => DailySchedule.Build(p, C.Occupations["sem_ocupacao"], RS, d, false, false, false)).ToList();
        var est = DailySchedule.ExpectWeek(p, week, RS);
        Assert.All(est, x => Assert.InRange(x.ChancePermille, 0, 1000));
        Assert.All(est, x => Assert.True(x.ChancePermille >= 950));
        // Semana real: dias com chances diferentes recebem estimativas diferentes (nao uma media unica).
        var q = PopulationGenerator.Generate(2, C, 1);
        q.OccupationId = "comercial";
        q.Traits.GameImportance = 400;
        var qweek = Enumerable.Range(0, 7).Select(d => DailySchedule.Build(q, C.Occupations["comercial"], RS, d, false, false, false)).ToList();
        var qest = DailySchedule.ExpectWeek(q, qweek, RS);
        Assert.True(qest[5].ChancePermille > qest[1].ChancePermille);   // sabado > terca
        Assert.Equal(qest.Sum(x => x.ExpectedMinutes), DailySchedule.ExpectedWeeklyMinutes(q, C.Occupations["comercial"], RS));
    }
}

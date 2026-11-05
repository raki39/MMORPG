using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Time;

namespace Simulation.Tests;

/// <summary>
/// R4: modelo de vontade. Sessao base = sessao normal, ampliada pelo tempo livre ate x1,5; a
/// importancia so na prioridade; tempo desejado do dia flexivel, de todas as sessoes (inclusive no
/// trabalho); segunda sessao para qualquer perfil pela vontade restante; habito = traco x
/// regularidade recente com oportunidade; disposicao do dia reproduzivel; gosto de horario
/// separado do horario habitual.
/// </summary>
public class WillModelTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private static RoutineSettingsDefinition RS => C.Routine;

    private static DailySchedule.DayLayout Day(Person p, string occ, int dow, (int, int, string)? commitment = null) =>
        DailySchedule.Build(p, C.Occupations[occ], RS, dow, false, false, false, commitment);

    [Fact]
    public void SessaoPlanejada_ImportanciaNaoMudaADuracao_ETempoLivreAmpliaAteUmEMeio()
    {
        var p = PopulationGenerator.Generate(1, C, 1);
        p.Traits.SessionMinutes = 200;
        var free = Day(p, "sem_ocupacao", 5);
        var work = Day(p, "escala_5x2", 1);
        Assert.True(free.FreeMinutes >= RS.FullFreeMinutes);
        p.Traits.GameImportance = 100;
        int low = DailySchedule.DesiredMinutes(p, free, RS);
        p.Traits.GameImportance = 950;
        Assert.Equal(low, DailySchedule.DesiredMinutes(p, free, RS));   // importancia nao entra na duracao
        Assert.Equal(200 * RS.MaxPlannedAmplificationPermille / 1000, low);   // tempo livre cheio: x1,5
        Assert.Equal(1500, RS.MaxPlannedAmplificationPermille);
        int busy = DailySchedule.DesiredMinutes(p, work, RS);
        Assert.InRange(busy, 200, low - 1);   // dia de trabalho: entre a sessao normal e o teto
        // O tempo desejado do dia nao passa disso sem disposicao, humor ou acontecimento.
        Assert.Equal(low, DailySchedule.DayIntentMinutes(low, 0, 1000, 0, RS));
        Assert.True(DailySchedule.DayIntentMinutes(low, -150, 1000, 0, RS) < low);
        Assert.True(DailySchedule.DayIntentMinutes(low, 0, 1000, 800, RS) < low);   // cansado: menos
    }

    [Fact]
    public void DisposicaoDoDia_ReproduzivelPorPessoaEDia_PequenaECentrada()
    {
        var a = TestSupport.NewEngine(5, 200);
        var b = TestSupport.NewEngine(5, 200);
        var values = new List<int>();
        foreach (var p in a.World.Persons)
            for (long d = 1; d <= 40; d++)
            {
                int v = a.World.Behavior.Disposition(p, d);
                Assert.Equal(v, b.World.Behavior.Disposition(b.World.GetPerson(p.Id), d));   // mesma seed, mesmo valor
                Assert.Equal(v, a.World.Behavior.Disposition(p, d));                         // sem consumir outros sorteios
                Assert.InRange(v, -RS.DispositionSpreadPermille, RS.DispositionSpreadPermille);
                values.Add(v);
            }
        Assert.InRange(values.Average(), -10, 10);
        Assert.True(values.Distinct().Count() > 50);
        var first = a.World.Persons[0];
        Assert.True(Enumerable.Range(1, 40).Select(d => a.World.Behavior.Disposition(first, d)).Distinct().Count() > 10);   // muda de um dia para outro

        // A agenda guarda a disposicao usada no planejamento.
        var e = TestSupport.NewEngine(6, 300);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(8));
        var plans = e.World.Persons.SelectMany(p => p.DayPlans.Where(r => r.Reason != "came_back").Select(r => (p, r))).ToList();
        Assert.True(plans.Count > 300);
        Assert.All(plans, t => Assert.Equal(e.World.Behavior.Disposition(t.p, t.r.LocalDay), t.r.DispositionPermille));
    }

    private static DayPlanRecord R(long day, string reason, int sessions, int work = 0) =>
        new(day, 300, 1, sessions, reason, -1, 0, false, workSessions: work);

    [Fact]
    public void Habito_FaltaPorObrigacaoENeutra_EscolhaReduz_SessaoInterrompidaConta()
    {
        const int N = 7;
        // So dias sem oportunidade: nada a medir (neutro).
        Assert.Equal(500, DailySchedule.Regularity(Enumerable.Range(3, 7).Select(d => R(d, "no_free_time", 0)).ToList(), 10, N).Regularity);
        // Faltar por obrigacao nao quebra o habito de quem joga sempre que pode.
        var forced = new[] { R(7, "played", 1), R(8, "no_free_time", 0), R(9, "played", 1) };
        Assert.Equal(1000, DailySchedule.Regularity(forced, 10, N).Regularity);
        // Escolher outro lazer conta como falta.
        var chose = DailySchedule.Regularity(new[] { R(7, "played", 1), R(8, "other_leisure", 0), R(9, "played", 1) }, 10, N);
        Assert.True(chose.Regularity < 1000);
        Assert.Equal(1, chose.Missed);
        Assert.Equal(2, chose.Played);
        // Cansado e desanimado tambem sao faltas com oportunidade.
        Assert.Equal(0, DailySchedule.Regularity(new[] { R(8, "tired", 0), R(9, "low_engagement", 0) }, 10, N).Regularity);
        // Varios dias, mais peso aos recentes: faltar ontem pesa mais que faltar ha seis dias.
        var missYesterday = Enumerable.Range(4, 5).Select(d => R(d, "played", 1)).Append(R(9, "other_leisure", 0)).ToList();
        var missOld = Enumerable.Range(5, 5).Select(d => R(d, "played", 1)).Prepend(R(4, "other_leisure", 0)).ToList();
        Assert.True(DailySchedule.Regularity(missYesterday, 10, N).Regularity < DailySchedule.Regularity(missOld, 10, N).Regularity);
        // Fora da janela de dias nao conta; o proprio dia tambem nao.
        Assert.Equal(500, DailySchedule.Regularity(new[] { R(1, "other_leisure", 0), R(10, "other_leisure", 0) }, 10, N).Regularity);
        // So jogou no trabalho: escolheu jogar.
        Assert.Equal(1000, DailySchedule.Regularity(new[] { R(9, "other_leisure", 0, work: 1) }, 10, N).Regularity);
        // Sessao interrompida (jogou 5 min): a escolha foi jogar.
        var cut = R(9, "played", 1);
        cut.PlayedMinutes = 5;
        Assert.Equal(1000, DailySchedule.Regularity(new[] { cut }, 10, N).Regularity);
        // O fator e simetrico: regularidade total ganha, nenhuma perde, meio a meio neutro.
        Assert.Equal(1000 + RS.HabitStreakPermille, DailySchedule.HabitFactor(1000, 1000, RS));
        Assert.Equal(1000 - RS.HabitStreakPermille, DailySchedule.HabitFactor(1000, 0, RS));
        Assert.Equal(1000, DailySchedule.HabitFactor(800, 500, RS));
        Assert.Equal(1000, DailySchedule.HabitFactor(0, 1000, RS));   // sem habito, o historico nao pesa
    }

    [Fact]
    public void SegundaSessao_PossivelParaQualquerPerfil_PelaVontadeRestanteEOportunidade()
    {
        var p = PopulationGenerator.Generate(1, C, 1);
        p.Traits.SessionMinutes = 120;
        p.Traits.GameImportance = 350;   // casual
        // Sabado com visita a familia no meio: duas janelas.
        var day = Day(p, "sem_ocupacao", 5, (11 * 60, 15 * 60, "familia"));
        Assert.True(day.Windows.Count(w => w.End - w.Start >= RS.MinSessionMinutes) >= 2);
        int main = day.Windows.Select((w, i) => (w, i)).OrderByDescending(t => t.w.End - t.w.Start).First().i;
        var casual = DailySchedule.SecondSession(p, day, main, 100, RS);
        Assert.True(casual.Window >= 0 && casual.Window != main);
        Assert.InRange(casual.ChancePermille, 1, 999);
        Assert.InRange(casual.Minutes, RS.MinSessionMinutes, 100);   // nunca mais do que sobrou
        // Mesma vontade restante, mais prioridade ao jogo: mais chance (a duracao nao muda).
        p.Traits.GameImportance = 850;
        var eager = DailySchedule.SecondSession(p, day, main, 100, RS);
        Assert.True(eager.ChancePermille > casual.ChancePermille);
        Assert.Equal(casual.Minutes, eager.Minutes);
        // Sem vontade restante (ou so o custo de entrar de novo): nao ha segunda sessao.
        Assert.Equal(-1, DailySchedule.SecondSession(p, day, main, 0, RS).Window);
        Assert.Equal(-1, DailySchedule.SecondSession(p, day, main, RS.ReentryCostMinutes + RS.MinSessionMinutes - 1, RS).Window);
        // Sem outra janela: nao ha segunda sessao, mesmo com vontade sobrando.
        var single = Day(p, "sem_ocupacao", 5);
        Assert.Equal(-1, DailySchedule.SecondSession(p, single, 0, 300, RS).Window);
    }

    /// <summary>A agenda do dia nunca planeja mais do que a vontade do dia, descontado o que foi planejado no trabalho.</summary>
    [Fact]
    public void VontadeDoDia_LimitaOPlanejado_EDescontaOJogoNoTrabalho()
    {
        var e = WorldFactory.Create(C, new WorldConfig { Seed = 41, PopulationSize = 1500 });
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        var w = e.World;
        var workPlanned = new Dictionary<(int, long), int>();
        for (int d = 0; d < 21; d++)
        {
            e.AdvanceTo(SimTime.Days(d + 1));
            foreach (var ev in w.Events.Drain().Where(x => x.Type == "session_started" && x.Causes.Count > 0))
            {
                var p = w.GetPerson((int)ev.Entities[0].Id);
                var key = (p.Id, BehaviorSystem.LocalDay(p, ev.Tick));
                workPlanned[key] = workPlanned.GetValueOrDefault(key) + ev.Causes[0].WeightPermille;   // causa = minutos planejados
            }
        }
        int checkedDays = 0, withWork = 0;
        foreach (var p in w.Persons)
            foreach (var r in p.DayPlans.Where(r => r.Reason == "played" && r.DesiredMinutes > 0))
            {
                int work = workPlanned.GetValueOrDefault((p.Id, r.LocalDay));
                Assert.True(r.PlannedMinutes + work <= Math.Max(r.DesiredMinutes, RS.MinSessionMinutes + work),
                    $"{p.OccupationId}: planejou {r.PlannedMinutes} + {work} no trabalho com vontade de {r.DesiredMinutes}");
                checkedDays++;
                if (work > 0 && r.PlannedMinutes + work <= r.DesiredMinutes) withWork++;
            }
        Assert.True(checkedDays > 5000, $"dias {checkedDays}");
        Assert.True(withWork > 20, $"dias com jogo no trabalho descontado: {withWork}");
    }

    /// <summary>
    /// A sessao seguinte do mesmo dia desconta o que ja foi jogado; se a pessoa ja jogou o que
    /// queria, deixa a sessao de lado (prefere outro lazer).
    /// </summary>
    [Fact]
    public void SessaoSeguinte_DescontaOQueJaFoiJogado_EFicaDeLadoSeJaJogouOQueQueria()
    {
        var e = WorldFactory.Create(C, new WorldConfig { Seed = 77, PopulationSize = 400, NeutralMood = true });
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(4) + SimTime.Hours(1));
        var w = e.World;
        w.Events.Drain();
        long now = w.Clock.Tick;
        // Pessoas sem nada planejado hoje (nem no trabalho): a agenda de teste nao disputa com a delas.
        var idle = w.Persons.Where(x => x.State == LifecycleState.Offline
            && x.DayPlans.Any(r => r.LocalDay == BehaviorSystem.LocalDay(x, now) && r.Sessions == 0 && r.WorkSessions == 0)).Take(2).ToList();
        Assert.Equal(2, idle.Count);
        DayPlanRecord Plan(Person p, int desired)
        {
            long day = BehaviorSystem.LocalDay(p, now);
            p.PlanList.RemoveAll(r => r.LocalDay == day);
            var plan = new DayPlanRecord(day, 600, 2, 2, "played", 0, 140, true, desiredMinutes: desired);
            p.PlanList.Add(plan);
            w.Scheduler.Schedule(now + SimTime.Minutes(5), BehaviorSystem.EvSessionStart, p.Id, BehaviorSystem.SessionPayload(80, BehaviorSystem.SessionNormal, day));
            w.Scheduler.Schedule(now + SimTime.Hours(3), BehaviorSystem.EvSessionStart, p.Id, BehaviorSystem.SessionPayload(60, BehaviorSystem.SessionNormal, day));
            return plan;
        }
        var wants = Plan(idle[0], 100);   // sobra vontade depois da primeira
        var done = Plan(idle[1], 90);     // sobra menos que a sessao minima
        e.AdvanceTo(now + SimTime.Hours(6));
        var events = w.Events.Drain().ToList();
        List<(long Start, long End, string Detail)> Sessions(Person p)
        {
            var list = new List<(long, long, string)>();
            long start = -1;
            string detail = "";
            foreach (var ev in events.Where(x => x.Entities.Count > 0 && x.Entities[0].Id == p.Id))
            {
                if (ev.Type == "session_started") { start = ev.Tick; detail = ev.Detail ?? ""; }
                else if (ev.Type == "session_ended" && start >= 0) { list.Add((start, ev.Tick, detail)); start = -1; }
            }
            return list;
        }
        var a = Sessions(idle[0]);
        Assert.Equal(2, a.Count);
        Assert.Equal("80 min", a[0].Detail);
        int first = (int)((a[0].End - a[0].Start) / SimTime.TicksPerMinute);
        Assert.Equal($"{Math.Min(60, 100 - first)} min", a[1].Detail);
        Assert.Equal(2, wants.NormalSessionsStarted);
        Assert.Equal(first + (int)((a[1].End - a[1].Start) / SimTime.TicksPerMinute), wants.PlayedMinutes);
        var b = Sessions(idle[1]);
        Assert.Single(b);
        Assert.Equal(1, done.SkippedSessions);
        Assert.Equal(1, done.NormalSessionsStarted);
    }

    [Fact]
    public void GostoDeHorario_SeparadoDoHorarioHabitual()
    {
        var people = Enumerable.Range(1, 1500).Select(id => PopulationGenerator.Generate(id, C, 9)).ToList();
        // O gosto e o sorteado pelo perfil (nao muda com a rotina); o habitual e o viavel calculado.
        Assert.Equal(people.Select(p => p.Traits.PreferredStartHour), people.Select(p => p.BaseTraits.PreferredStartHour));
        Assert.All(people, p => Assert.InRange(p.HabitualStartHour, 0, 23));
        Assert.Contains(people, p => p.HabitualStartHour != p.Traits.PreferredStartHour);
        Assert.Contains(people, p => p.HabitualStartHour == p.Traits.PreferredStartHour);
        // Quem trabalha ate as 18:50 pode gostar de jogar as 17 h, mas costuma comecar depois de chegar.
        var shift = people.Where(p => p.OccupationId == "escala_5x2" && p.Traits.PreferredStartHour <= 17).ToList();
        Assert.NotEmpty(shift);
        Assert.All(shift, p => Assert.True(p.HabitualStartHour >= 19, $"gosta das {p.Traits.PreferredStartHour}h, costuma {p.HabitualStartHour}h"));
    }

    [Fact]
    public void Estimativa_UsaAMesmaPolitica_EDisposicaoComMediaZero()
    {
        var pts = DailySchedule.DispositionPoints(RS.DispositionSpreadPermille);
        Assert.Equal(1.0, pts.Sum(x => x.Weight), 6);
        Assert.Equal(0.0, pts.Sum(x => x.Value * x.Weight), 6);
        Assert.All(pts, x => Assert.InRange(x.Value, -RS.DispositionSpreadPermille, RS.DispositionSpreadPermille));
        var p = PopulationGenerator.Generate(3, C, 1);
        var week = Enumerable.Range(0, 7).Select(d => Day(p, p.OccupationId, d)).ToList();
        var est = DailySchedule.ExpectWeek(p, week, RS);
        Assert.All(est, x => Assert.InRange(x.ChancePermille, 0, 1000));
        Assert.Equal(est.Sum(x => x.ExpectedMinutes), DailySchedule.ExpectedWeeklyMinutes(p, C.Occupations[p.OccupationId], RS));
    }
}

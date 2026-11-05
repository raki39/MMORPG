using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Events;
using Simulation.Core.Population;
using Simulation.Core.Time;

namespace Simulation.Tests;

/// <summary>
/// Vontade de jogar (R4 corrigida, v3, D-051): decisoes separadas de comecar, continuar, parar e
/// voltar, comparando a vontade de jogar com as outras opcoes de lazer e a vontade de parar
/// (saciedade e cansaco). A saciedade cresce com o tempo jogado e cai nas pausas; a sessao normal
/// e uma preferencia, nao um limite. Habito pelo historico e disposicao do dia continuam da R4.
/// </summary>
public class WillModelTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private static RoutineSettingsDefinition RS => C.Routine;

    private static DailySchedule.DayLayout Day(Person p, string occ, int dow, (int, int, string)? commitment = null) =>
        DailySchedule.Build(p, C.Occupations[occ], RS, dow, false, false, false, commitment);

    private static List<SimEvent> Run(SimulationEngine e, int days)
    {
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        var all = new List<SimEvent>();
        for (int d = 0; d < days; d++)
        {
            e.AdvanceTo(SimTime.Days(d + 1));
            all.AddRange(e.World.Events.Drain());
        }
        return all;
    }

    private static string StartReason(SimEvent ev) => ev.Causes.Count > 1 ? ev.Causes[1].Factor : ev.Causes.Count > 0 ? ev.Causes[0].Factor : "";

    [Fact]
    public void SessaoNormal_EPreferencia_ImportanciaNaoMudaADuracao()
    {
        var p = PopulationGenerator.Generate(1, C, 1);
        p.Traits.SessionMinutes = 200;
        p.Traits.GameImportance = 100;
        Assert.Equal(200, DailySchedule.SessionMinutes(p, RS));
        p.Traits.GameImportance = 950;
        Assert.Equal(200, DailySchedule.SessionMinutes(p, RS));
        // Prioridade dentro do dia: importancia e tempo livre, sem o peso das obrigacoes (que fica so na chance do dia).
        var free = Day(p, "sem_ocupacao", 5);
        var work = Day(p, "escala_5x2", 1);
        Assert.True(DailySchedule.SessionPriority(p, free, RS) >= DailySchedule.SessionPriority(p, work, RS));
        Assert.True(DailySchedule.SessionPriority(p, work, RS) >= p.Traits.GameImportance);
        Assert.True(DailySchedule.BaseChance(p, work, RS) < DailySchedule.SessionPriority(p, work, RS));
    }

    [Fact]
    public void Decisao_SemSaciedadeNemCansaco_EAPropriaPrioridade_ESaciedadeReduz()
    {
        foreach (int prio in new[] { 200, 500, 900 })
        {
            int pull = DailySchedule.Pull(prio, 0, 1000, 0, 1000, RS);
            Assert.Equal(prio, DailySchedule.Choice(pull, prio, 0));
            Assert.True(DailySchedule.Choice(pull, prio, 400) < prio);              // saciedade: vontade de parar
            Assert.True(DailySchedule.Choice(pull, prio, DailySchedule.StopPressure(0, 1000, RS)) < prio);   // cansaco
        }
        // Experiencia em relacao ao que a pessoa costuma sentir: igual ao habitual nao muda nada.
        Assert.Equal(DailySchedule.Pull(600, 0, 1000, 0, 1000, RS), DailySchedule.Pull(600, 0, 1000, 0, 1000, RS));
        Assert.True(DailySchedule.Pull(600, 0, 1000, 200, 1000, RS) > DailySchedule.Pull(600, 0, 1000, 0, 1000, RS));
    }

    [Fact]
    public void Saciedade_CresceComOTempoJogado_CaiNaPausa_NuncaPassaDosLimites()
    {
        int s = 120;
        int tau = s * RS.SatietyGrowthSessionsPermille / 1000;
        Assert.Equal(500, DailySchedule.SatietyAfterPlay(0, tau, s, RS));                 // metade a cada tau
        Assert.Equal(250, DailySchedule.SatietyAfterPause(500, RS.SatietyRecoveryMinutes, RS));   // a pausa recupera
        Assert.True(DailySchedule.SatietyAfterPlay(0, 10, s, RS) > 0);                    // todo minuto jogado conta
        Assert.InRange(DailySchedule.SatietyAfterPlay(990, 10_000, s, RS), 990, 1000);
        Assert.Equal(0, DailySchedule.SatietyAfterPause(0, 300, RS));
        var e = TestSupport.NewEngine(12, 600);
        Run(e, 10);
        Assert.All(e.World.Persons, p => Assert.InRange(p.Satiety, 0, 1000));
        Assert.Contains(e.World.Persons, p => p.Satiety > 0);
    }

    [Fact]
    public void Comecar_UmSorteioSoPorDia_OPassoNaoMudaQuantoSeComecaCedo()
    {
        var p = PopulationGenerator.Generate(5, C, 1);
        var day = Day(p, "sem_ocupacao", 5);
        int target = 20 * 60, prio = 600, pull = DailySchedule.Pull(prio, 0, 1000, 0, 1000, RS);
        (double Early, double Median) Profile(int step)
        {
            var rs = new RoutineSettingsDefinition { StartStepMinutes = step, EarlyStartResistancePerHour = RS.EarlyStartResistancePerHour, FreeDayMorningMinutes = RS.FreeDayMorningMinutes, MinSessionMinutes = RS.MinSessionMinutes };
            var starts = Enumerable.Range(0, 1000).Select(i => DailySchedule.StartMinute(day, target, 120, pull, prio, 0, i * 1000, rs)).OrderBy(x => x).ToList();
            return (starts.Count(x => x < target) / 10.0, starts[500]);
        }
        var fine = Profile(1);
        foreach (int step in new[] { 5, 10, 30 })
        {
            var x = Profile(step);
            Assert.InRange(x.Early, fine.Early - 4, fine.Early + 4);     // a chance e por hora: o passo so arredonda
            Assert.InRange(x.Median, fine.Median - step - 15, fine.Median + step + 15);
        }
        // Sem resistencia e com toda a prioridade, comeca cedo; com resistencia enorme, so no horario de costume.
        var strong = new RoutineSettingsDefinition { EarlyStartResistancePerHour = 100_000, FreeDayMorningMinutes = 60, MinSessionMinutes = 20 };
        Assert.InRange(DailySchedule.StartMinute(day, target, 120, pull, prio, 0, 999_999, strong), target, target + strong.StartStepMinutes - 1);
        // Horario de costume depois de todo o tempo livre: comeca no ultimo momento em que ainda cabe a sessao.
        int late = DailySchedule.StartMinute(day, day.Bed + 120, 120, pull, prio, 0, 999_999, strong);
        Assert.Equal(day.Windows[^1].End - 120, late);
    }

    [Fact]
    public void Participacao_ENaChanceDoDia_NaoNoPassoDeAvaliacao()
    {
        double Played(int step)
        {
            var set = TestSupport.FreshPresetSet();
            set.Population.Routine.StartStepMinutes = step;
            var content = ContentDatabase.Create(set);
            var e = WorldFactory.Create(content, new WorldConfig { Seed = 21, PopulationSize = 800, NeutralMood = true });
            Run(e, 12);
            var plans = e.World.Persons.SelectMany(p => p.DayPlans).Where(r => r.Reason != "came_back").ToList();
            return plans.Count(r => r.Reason == "played") * 100.0 / plans.Count;
        }
        double a = Played(10), b = Played(30);
        Assert.InRange(b, a - 1.0, a + 1.0);
    }

    [Fact]
    public void Continuar_DentroDoTempoLivre_EFicarAcordadoSoPassandoDaHoraDeDormir()
    {
        var e = TestSupport.NewEngine(13, 1200);
        var events = Run(e, 21);
        var cont = events.Where(x => x.Type == "session_continued").ToList();
        Assert.True(cont.Count > 500, $"continuacoes: {cont.Count}");
        // Ficar acordado (P3) e so o que passa da hora de dormir: todo minuto acrescentado e sono cortado.
        var ext = events.Where(x => x.Type == "session_extended").ToList();
        Assert.All(ext, x => Assert.Equal(int.Parse(x.Detail!.Trim('+', ' ', 'm', 'i', 'n')), x.Causes.Single(c => c.Factor == "sleep_cut").WeightPermille));
        // Toda sessao termina com um motivo; a decisao de parar existe (nao continua ate o fim automaticamente).
        var ends = events.Where(x => x.Type == "session_ended").Select(x => x.Causes[0].Factor).ToList();
        Assert.Contains("satisfied", ends);
        Assert.Contains("window_end", ends);
    }

    [Fact]
    public void Voltar_DepoisDeUmaPausa_ComumNoDiaLivre_ECasualPodeJogarTresVezes()
    {
        var e = TestSupport.NewEngine(14, 1500);
        var events = Run(e, 28);
        var w = e.World;
        var starts = events.Where(x => x.Type == "session_started" && x.Causes.Count > 1 && x.Causes[0].Factor == "normal").ToList();
        var returns = starts.Where(x => StartReason(x) == "return").ToList();
        Assert.True(returns.Count > 300, $"voltas: {returns.Count}");
        // A volta vem depois de uma pausa de pelo menos o minimo configurado.
        var lastEnd = new Dictionary<long, long>();
        foreach (var ev in events)
        {
            if (ev.Entities.Count == 0) continue;
            long pid = ev.Entities[0].Id;
            if (ev.Type == "session_ended") lastEnd[pid] = ev.Tick;
            else if (ev.Type == "session_started" && StartReason(ev) == "return")
                Assert.True(ev.Tick - lastEnd[pid] >= RS.ReturnPauseMinMinutes * SimTime.TicksPerMinute, "volta sem pausa");
        }
        var casualDays = w.Persons.Where(p => p.ProfileId == "casual").SelectMany(p => p.DayPlans).Where(r => r.Reason == "played").ToList();
        Assert.Contains(casualDays, r => r.Sittings >= 3);   // permitido, nao forcado
        var free = w.Persons.SelectMany(p => p.DayPlans).Where(r => r.Reason == "played" && r.FreeDay).ToList();
        Assert.True(free.Count(r => r.Returns > 0) * 100 / free.Count >= 15, "voltar no dia livre deveria ser comum");
    }

    [Fact]
    public void JogoNoTrabalho_NaoDescontaMinutosDaSessaoDaNoite()
    {
        var e = TestSupport.NewEngine(15, 1500);
        var events = Run(e, 21);
        var w = e.World;
        int checkedSessions = 0;
        foreach (var ev in events.Where(x => x.Type == "session_started" && x.Causes.Count > 1 && x.Causes[0].Factor == "normal" && StartReason(x) is "usual_time" or "early_choice"))
        {
            var p = w.GetPerson((int)ev.Entities[0].Id);
            long cal = BehaviorSystem.LocalDay(p, ev.Tick);
            int minute = (int)((ev.Tick - BehaviorSystem.LocalDayStart(p, cal)) / SimTime.TicksPerMinute);
            var day = w.Behavior.LayoutOf(p, cal);
            if (minute < day.Wake) { cal--; minute += 1440; day = w.Behavior.LayoutOf(p, cal); }
            var win = day.Windows.FirstOrDefault(x => x.Start <= minute && minute < x.End);
            if (win == default) continue;
            int planned = int.Parse(ev.Detail!.Split(' ')[0]);
            Assert.Equal(Math.Min(DailySchedule.SessionMinutes(p, RS), win.End - minute), planned);
            checkedSessions++;
        }
        Assert.True(checkedSessions > 2000, $"sessoes: {checkedSessions}");
        // Mas jogar no trabalho pesa na vontade de comecar a noite (saciedade e experiencia).
        Assert.Contains(w.Persons.SelectMany(p => p.DayPlans), r => r.SkippedAfterWork > 0);
    }

    [Fact]
    public void DiaDeObrigacao_TambemUsaAJanelaAntesDoTrabalho()
    {
        var e = TestSupport.NewEngine(16, 2500);
        var events = Run(e, 21);
        var w = e.World;
        int before = 0;
        foreach (var ev in events.Where(x => x.Type == "session_started" && x.Causes.Count > 1 && x.Causes[0].Factor == "normal"))
        {
            var p = w.GetPerson((int)ev.Entities[0].Id);
            if (p.OccupationId is not ("turno_noturno" or "meio_periodo_tarde" or "estudante_tarde")) continue;
            long cal = BehaviorSystem.LocalDay(p, ev.Tick);
            int minute = (int)((ev.Tick - BehaviorSystem.LocalDayStart(p, cal)) / SimTime.TicksPerMinute);
            var day = w.Behavior.LayoutOf(p, cal);
            if (day.FreeDay || day.Blocks.Count == 0) continue;
            if (minute >= day.Wake && minute < day.Blocks[0].Start) before++;
        }
        Assert.True(before > 30, $"sessoes antes da obrigacao: {before}");
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
                Assert.Equal(v, b.World.Behavior.Disposition(b.World.GetPerson(p.Id), d));
                Assert.InRange(v, -RS.DispositionSpreadPermille, RS.DispositionSpreadPermille);
                values.Add(v);
            }
        Assert.InRange(values.Average(), -10, 10);
        var e = TestSupport.NewEngine(6, 300);
        Run(e, 8);
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
        Assert.Equal(500, DailySchedule.Regularity(Enumerable.Range(3, 7).Select(d => R(d, "no_free_time", 0)).ToList(), 10, N).Regularity);
        Assert.Equal(1000, DailySchedule.Regularity(new[] { R(7, "played", 1), R(8, "no_free_time", 0), R(9, "played", 1) }, 10, N).Regularity);
        var chose = DailySchedule.Regularity(new[] { R(7, "played", 1), R(8, "other_leisure", 0), R(9, "played", 1) }, 10, N);
        Assert.True(chose.Regularity < 1000);
        Assert.Equal(1, chose.Missed);
        Assert.Equal(1000, DailySchedule.Regularity(new[] { R(9, "other_leisure", 0, work: 1) }, 10, N).Regularity);
        var cut = R(9, "played", 1);
        cut.PlayedMinutes = 5;
        Assert.Equal(1000, DailySchedule.Regularity(new[] { cut }, 10, N).Regularity);
        Assert.Equal(1000 + RS.HabitStreakPermille, DailySchedule.HabitFactor(1000, 1000, RS));
        Assert.Equal(1000, DailySchedule.HabitFactor(800, 500, RS));
    }

    [Fact]
    public void GostoDeHorario_SeparadoDoHorarioHabitual()
    {
        var people = Enumerable.Range(1, 1500).Select(id => PopulationGenerator.Generate(id, C, 9)).ToList();
        Assert.Equal(people.Select(p => p.Traits.PreferredStartHour), people.Select(p => p.BaseTraits.PreferredStartHour));
        Assert.Contains(people, p => p.HabitualStartHour != p.Traits.PreferredStartHour);
        var shift = people.Where(p => p.OccupationId == "escala_5x2" && p.Traits.PreferredStartHour <= 17).ToList();
        Assert.NotEmpty(shift);
        Assert.All(shift, p => Assert.True(p.HabitualStartHour >= 19, $"gosta das {p.Traits.PreferredStartHour}h, costuma {p.HabitualStartHour}h"));
    }

    [Fact]
    public void Estimativa_MesmaRegra_Deterministica_ESomaDaSemana()
    {
        var p = PopulationGenerator.Generate(3, C, 1);
        var week = Enumerable.Range(0, 7).Select(d => Day(p, p.OccupationId, d)).ToList();
        var a = DailySchedule.ExpectWeek(p, week, RS);
        var b = DailySchedule.ExpectWeek(p, week, RS);
        Assert.Equal(a, b);
        Assert.All(a, x => Assert.InRange(x.ChancePermille, 0, 1000));
        Assert.Equal(a.Sum(x => x.ExpectedMinutes), DailySchedule.ExpectedWeeklyMinutes(p, C.Occupations[p.OccupationId], RS));
        Assert.Contains(a, x => x.Minutes > 0);
    }
}

using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Population;
using Simulation.Core.Time;

namespace Simulation.Tests;

/// <summary>
/// R1: contabilidade do dia. Cada minuto pertence a um unico trecho, o sono e contado uma vez com
/// inicio e fim reais, e o dia de amanha (feriado, folga, turno da noite) e conhecido.
/// </summary>
public class DayAccountingTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private static RoutineSettingsDefinition RS => C.Routine;

    private static Person Standard(string occupation, int bedtime = 1410, int need = 480)
    {
        var p = PopulationGenerator.Generate(1, C, 1);
        p.OccupationId = occupation;
        p.ShiftOffsetMinutes = 0;
        p.CommutePermille = 1000;
        p.DayOffIndex = -1;
        p.SleepNeedMinutes = need;
        p.BedtimeMinute = bedtime;
        p.SleepDeficitMinutes = 0;
        return p;
    }

    private static DailySchedule.DayLayout Day(Person p, int dow, bool holiday = false, bool prevHoliday = false, bool nextHoliday = false) =>
        DailySchedule.Build(p, C.Occupations[p.OccupationId], RS, dow, holiday, prevHoliday, nextHoliday);

    // ---------------- Sono contado uma vez ----------------

    [Theory]
    [InlineData(420, 60)]      // precisa de 8 h, dormiu 7 h: +60 min de deficit
    [InlineData(480, -20)]     // dormiu o necessario: nenhuma falta nova; so a recuperacao explicita de noite completa
    [InlineData(540, -80)]     // dormiu 9 h: paga a hora a mais e a recuperacao de noite completa
    public void DeficitDeUmaNoite_RegraExplicita(int slept, int expected)
    {
        Assert.Equal(20, RS.RecoveryNormalNightMinutes);
        Assert.Equal(expected, DailySchedule.DeficitChange(480, slept, RS));
    }

    [Fact]
    public void DeitarAs2_AcordarAs9_SaoSeteHoras()
    {
        var p = Standard("sem_ocupacao", bedtime: 26 * 60, need: 420);   // deita as 02:00 (26h do dia anterior)
        var day = Day(p, 2);
        Assert.Equal(2 * 60, day.SleepStart);   // 02:00 de hoje
        Assert.Equal(9 * 60, day.Wake);
        Assert.Equal(420, day.SleepMinutes);
    }

    /// <summary>
    /// Quem fica jogando depois da hora de deitar perde exatamente esse tempo de sono, contado uma
    /// unica vez ao acordar (nada e somado na saida e de novo na noite seguinte).
    /// </summary>
    [Fact]
    public void FicarJogandoDepoisDeDeitar_EntraUmaVezNoSonoReal()
    {
        var e = TestSupport.NewEngine(2, 800);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        var w = e.World;
        var lateLogouts = new List<(Person P, long Tick)>();
        var starts = new Dictionary<int, List<long>>();
        for (int d = 0; d < 28; d++)
        {
            e.AdvanceTo(SimTime.Days(d + 1));
            foreach (var ev in w.Events.Drain())
            {
                if (ev.Type == "stayed_up_late") lateLogouts.Add((w.GetPerson((int)ev.Entities[0].Id), ev.Tick));
                if (ev.Type == "session_started")
                {
                    int pid = (int)ev.Entities[0].Id;
                    if (!starts.TryGetValue(pid, out var l)) starts[pid] = l = new List<long>();
                    l.Add(ev.Tick);
                }
            }
        }
        e.AdvanceTo(SimTime.Days(29));
        int checkedNights = 0;
        foreach (var (p, tick) in lateLogouts)
        {
            // A noite que comecou nessa saida termina no "acordar" do dia local seguinte (ou do mesmo, se passou da meia-noite).
            for (long d = BehaviorSystem.LocalDay(p, tick); d <= BehaviorSystem.LocalDay(p, tick) + 1; d++)
            {
                var day = w.Behavior.LayoutOf(p, d);
                long start = BehaviorSystem.LocalDayStart(p, d), sleepStart = start + day.SleepStart * SimTime.TicksPerMinute, wake = start + day.Wake * SimTime.TicksPerMinute;
                if (tick < sleepStart || tick > wake) continue;
                var rec = p.DayPlans.FirstOrDefault(r => r.LocalDay == d);
                if (rec == null) continue;   // fora da agenda guardada
                if (starts.TryGetValue(p.Id, out var again) && again.Any(t => t > tick && t <= wake)) continue;   // voltou a jogar antes de acordar
                Assert.Equal((int)((wake - tick) / SimTime.TicksPerMinute), rec.SleepMinutes);
                checkedNights++;
            }
        }
        Assert.True(checkedNights > 20, $"noites conferidas: {checkedNights}");
    }

    // ---------------- Linha do tempo fechando 24 h ----------------

    [Fact]
    public void LinhaDoTempo_CadaMinutoUmaVez_EOsDiasSeEncaixam()
    {
        var people = Enumerable.Range(1, 600).Select(i => PopulationGenerator.Generate(i, C, 9)).ToList();
        foreach (var p in people)
        {
            var occ = C.Occupations[p.OccupationId];
            var week = Enumerable.Range(0, 7).Select(d => DailySchedule.Build(p, occ, RS, d, false, false, false)).ToList();
            for (int d = 0; d < 7; d++)
            {
                var day = week[d];
                // As partes acordadas nao se sobrepoem e as janelas livres nao tocam nenhuma delas.
                for (int i = 0; i + 1 < day.Parts.Count; i++)
                    Assert.True(day.Parts[i].End <= day.Parts[i + 1].Start, $"{p.OccupationId} d{d}: {day.Parts[i]} x {day.Parts[i + 1]}");
                foreach (var (ws, we) in day.Windows)
                    Assert.DoesNotContain(day.Parts, x => x.Start < we && ws < x.End);
                Assert.All(day.Parts, x => Assert.True(x.Start >= day.Wake && x.End <= day.Bed));
                // A linha do tempo cobre o dia acordado inteiro, de SleepStart a Bed, sem buraco.
                var tl = day.Timeline();
                Assert.Equal(day.SleepStart, tl[0].Start);
                Assert.Equal(day.Bed, tl[^1].End);
                for (int i = 0; i + 1 < tl.Count; i++) Assert.Equal(tl[i].End, tl[i + 1].Start);
                Assert.Equal(day.FreeMinutes, tl.Where(t => t.Kind == "free").Sum(t => t.End - t.Start));
                Assert.Equal(day.SleepMinutes, tl.Where(t => t.Kind == "sleep").Sum(t => t.End - t.Start));
                // Amanha comeca exatamente onde hoje termina: a semana fecha 7 x 24 h.
                Assert.Equal(day.Bed - 1440, week[(d + 1) % 7].SleepStart);
            }
            Assert.Equal(7 * 1440, week.Sum(x => x.Bed - x.SleepStart));
        }
    }

    [Fact]
    public void TrabalhaEEstuda_VaiDiretoDoTrabalhoParaAFaculdade_EComeNoCaminho()
    {
        var p = Standard("trabalho_estudo");
        var monday = Day(p, 0).Timeline().Where(t => t.Kind != "awake").Select(t => t.Kind).ToList();
        int work = monday.IndexOf("work");
        Assert.True(work >= 0);
        Assert.Equal(new[] { "work", "meal", "transfer", "wait", "study", "commute", "arrival" }, monday.Skip(work).Take(7).ToArray());
        Assert.DoesNotContain("free", monday.Skip(work).Take(5));   // nao passa em casa entre o trabalho e a aula
        var friday = Day(p, 4).Timeline().Select(t => t.Kind).ToList();   // sexta: sem aula, volta para casa
        Assert.Equal(new[] { "work", "commute", "arrival", "free" }, friday.SkipWhile(k => k != "work").Take(4).ToArray());
    }

    [Fact]
    public void HomeOffice_SemDeslocamento_NaoTemChegada_LivreAoFimDoExpediente()
    {
        var p = Standard("home_office");
        var day = Day(p, 1);
        Assert.Contains(day.Windows, x => x.Start == 18 * 60);   // termina as 18:00 e ja esta em casa
        Assert.DoesNotContain(day.Parts, x => x.Kind == "arrival");
    }

    // ---------------- Escala 6x1 ----------------

    [Fact]
    public void Escala6x1_SeisDiasDeTrabalhoEUmaFolga_QuePodeSerDomingo()
    {
        var people = Enumerable.Range(1, 4000).Select(i => PopulationGenerator.Generate(i, C, 11)).Where(p => p.OccupationId == "escala_6x1").ToList();
        Assert.True(people.Count > 100, $"amostra {people.Count}");
        foreach (var p in people)
            Assert.Equal(6, Enumerable.Range(0, 7).Count(d => DailySchedule.Obligations(p, C.Occupations["escala_6x1"], d, false).Count > 0));
        Assert.Contains(people, p => p.DayOffIndex == 6);   // folga no domingo
        Assert.Contains(people, p => p.DayOffIndex is >= 0 and <= 4);
        var autonomos = Enumerable.Range(1, 4000).Select(i => PopulationGenerator.Generate(i, C, 11)).Where(p => p.OccupationId == "autonomo").ToList();
        Assert.All(autonomos, p => Assert.Equal(5, Enumerable.Range(0, 7).Count(d => DailySchedule.Obligations(p, C.Occupations["autonomo"], d, false).Count > 0)));
    }

    // ---------------- O dia de amanha ----------------

    [Fact]
    public void FeriadoOuFolgaAmanha_DormeNaHoraNatural()
    {
        var p = Standard("comercial", bedtime: 25 * 60);   // gosta de deitar a 01:00
        Assert.True(Day(p, 1).Bed < 25 * 60);                     // amanha trabalha: deita cedo para dormir 8 h
        Assert.Equal(25 * 60, Day(p, 1, nextHoliday: true).Bed);  // amanha e feriado
        var esc = Standard("escala_6x1", bedtime: 25 * 60);
        esc.DayOffIndex = 2;   // folga na quarta
        Assert.Equal(25 * 60, Day(esc, 1).Bed);                   // terca a noite: amanha e folga
        Assert.True(Day(esc, 2).FreeDay);
    }

    [Fact]
    public void TurnoDaNoiteDeSexta_SabadoComecaDepoisDoSono_NaoInteiramenteLivre()
    {
        var p = Standard("turno_noturno");
        var friday = Day(p, 4);
        var saturday = Day(p, 5);
        Assert.True(friday.NightShiftAhead);
        Assert.Equal(friday.Bed - 1440, saturday.SleepStart);           // sabado: dorme depois de voltar do turno
        Assert.True(saturday.SleepStart >= 6 * 60);                       // ja e manha
        Assert.True(saturday.DaySleeper);
        Assert.True(saturday.FreeMinutes < 12 * 60, $"livre {saturday.FreeMinutes} min");   // o dia livre so comeca a tarde
        Assert.True(saturday.Wake >= saturday.SleepStart + p.SleepNeedMinutes);
    }

    [Fact]
    public void SaidaParaOTurno_NaoESono_EOPrimeiroTurnoDepoisDaFolga()
    {
        var p = Standard("turno_noturno");
        var sunday = Day(p, 6);
        var monday = Day(p, 0);
        Assert.False(sunday.NightShiftAhead);
        Assert.Equal(sunday.Bed - 1440, monday.SleepStart);              // noite normal de domingo para segunda
        Assert.True(monday.NightShiftAhead);
        Assert.True(monday.ShiftDeparture > 0 && monday.ShiftDeparture < 22 * 60);
        Assert.All(monday.Windows, x => Assert.True(x.End <= monday.ShiftDeparture));   // nenhuma janela depois da saida
        Assert.True(monday.Bed > 1440 + 6 * 60);                          // o sono so comeca depois do turno
        Assert.DoesNotContain(monday.Timeline(), t => t.Kind == "sleep" && t.Start >= monday.Wake);   // sair para o turno nao e dormir
    }
}

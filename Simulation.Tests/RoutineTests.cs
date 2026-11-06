using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

/// <summary>Obrigacoes, tempo livre, vontade de jogar e renda (P2, D-043).</summary>
public class RoutineTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private static RoutineSettingsDefinition RS => C.Routine;

    /// <summary>Pessoa padronizada: sem variacao de horario, dorme 23h30 e precisa de 8 h.</summary>
    private static Person Standard(string occupation)
    {
        var p = PopulationGenerator.Generate(1, C, 1);
        p.OccupationId = occupation;
        p.ShiftOffsetMinutes = 0;
        p.CommutePermille = 1000;
        p.DayOffIndex = -1;
        p.SleepNeedMinutes = 480;
        p.BedtimeMinute = 1410;
        p.FreeEveShiftMinutes = 0;   // sem vespera de folga (R6): estes testes medem a hora natural
        return p;
    }

    private static DailySchedule.DayLayout Day(Person p, int dow, bool holiday = false) =>
        DailySchedule.Build(p, C.Occupations[p.OccupationId], RS, dow, holiday, false, false);

    [Fact]
    [Trait("Classe", "Regra do preset")]
    public void Comercial_SoTemANoite_EDormeATempoDoDiaSeguinte()
    {
        var day = Day(Standard("comercial"), 0);   // segunda
        Assert.Equal(new[] { (1115, 1345) }, day.Windows.ToArray());   // 18h35 (chegou e comeu) a 22h25
        Assert.Equal(385, day.Wake);   // 06h25: a tempo das 8h com 50 min de deslocamento e preparo
        Assert.False(day.FreeDay);
        var friday = Day(Standard("comercial"), 4);   // sexta: sabado livre, dorme na hora natural
        Assert.Equal(1410, friday.Bed);
        Assert.True(Day(Standard("comercial"), 5).FreeDay);   // sabado
    }

    [Fact]
    [Trait("Classe", "Regra do preset")]
    public void Estudante_DaManha_TemATardeEANoiteLivres()
    {
        var day = Day(Standard("estudante_manha"), 1);
        Assert.Single(day.Windows);
        Assert.Equal(825, day.Windows[0].Start);   // 13h45
        Assert.True(day.Windows[0].End - day.Windows[0].Start >= 450);
    }

    [Fact]
    [Trait("Classe", "Regra do preset")]
    public void TurnoNoturno_DormeDeDia_EJogaAntesDoTurno()
    {
        var p = Standard("turno_noturno");
        var tuesday = Day(p, 1);
        Assert.True(tuesday.Wake >= 14 * 60, $"acorda {tuesday.Wake / 60}h");
        var before = Assert.Single(tuesday.Windows.Except(tuesday.OptionalWindows));
        Assert.True(before.End <= 22 * 60 - 30);   // sai para o turno
        Assert.All(tuesday.OptionalWindows, x => Assert.True(x.Start >= tuesday.Bed));   // depois do turno: so possibilidade (R6)
        var saturday = Day(p, 5);   // depois do turno de sexta: dorme de manha e joga a noite
        Assert.True(saturday.Wake >= 14 * 60);
        // Folga (R6): continua perto do ritmo dos turnos, a meio caminho da hora natural (23h30).
        int ws = DailySchedule.NightSleepStart(p, C.Occupations[p.OccupationId], RS)!.Value;
        Assert.Equal(1410 + (ws - 1410) * RS.NightShiftOffContinuityPermille / 1000, saturday.Bed);
    }

    [Fact]
    [Trait("Classe", "Regra do preset")]
    public void TrabalhaEEstuda_QuaseSemTempoNosDiasUteis()
    {
        var p = Standard("trabalho_estudo");
        foreach (int dow in new[] { 0, 1, 2, 3 })
            Assert.DoesNotContain(Day(p, dow).Windows, w => w.End - w.Start >= RS.MinSessionMinutes);
        Assert.Contains(Day(p, 4).Windows, w => w.End - w.Start >= 240);   // sexta a noite
        Assert.True(Day(p, 5).FreeDay);
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Feriado_SuspendeObrigacoes_SalvoQuemTrabalhaEmFeriado_EFolgaDaEscala()
    {
        Assert.True(Day(Standard("comercial"), 2, holiday: true).FreeDay);
        Assert.False(Day(Standard("escala_6x1"), 2, holiday: true).FreeDay);   // escala trabalha em feriado
        var escala = Standard("escala_6x1");
        escala.DayOffIndex = 2;
        Assert.True(Day(escala, 2).FreeDay);    // folga fixa na quarta
        Assert.False(Day(escala, 5).FreeDay);   // trabalha no sabado
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void SessaoMinimaMesmoNaJanela_EComecaMaisCedoSeNaoCouber()
    {
        var day = Day(Standard("comercial"), 0);   // janela 1115..1345
        Assert.True(DailySchedule.PlaceMain(day, 21 * 60 + 30, 180, RS, out _, out int start, out int minutes));
        Assert.Equal(1345 - 180, start);   // queria jogar 3 h: comeca mais cedo
        Assert.Equal(180, minutes);
        Assert.True(DailySchedule.PlaceMain(day, 19 * 60, 60, RS, out _, out start, out _));
        Assert.Equal(19 * 60, start);   // cabe no preferido
        var busy = Day(Standard("trabalho_estudo"), 0);
        Assert.False(DailySchedule.PlaceMain(busy, 21 * 60, 60, RS, out _, out _, out _));
    }

    /// <summary>
    /// Ninguem joga durante obrigacoes incompativeis (D-044). A unica sobreposicao valida e no
    /// trabalho remoto, marcada na sessao: pausa (dentro da pausa declarada) ou escapada (com
    /// flexibilidade da ocupacao). Deslocamento, trabalho presencial e estudo ficam protegidos.
    /// </summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void NinguemComecaASessaoDuranteUmaObrigacao()
    {
        var e = TestSupport.NewEngine(11, 400);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        var w = e.World;
        int checkedStarts = 0;
        for (int d = 0; d < 14; d++)
        {
            e.AdvanceTo(SimTime.Days(d + 1));
            foreach (var ev in w.Events.Drain().Where(x => x.Type == "session_started" && x.Tick > SimTime.Days(1)))
            {
                var p = w.GetPerson((int)ev.Entities[0].Id);
                long localDay = BehaviorSystem.LocalDay(p, ev.Tick);
                int minute = (int)((ev.Tick - BehaviorSystem.LocalDayStart(p, localDay)) / SimTime.TicksPerMinute);
                var day = w.Behavior.LayoutOf(p, localDay);
                string kind = ev.Causes.Count > 0 ? ev.Causes[0].Factor : "";
                foreach (var b in day.Blocks.Where(b => minute >= b.Start && minute < b.End))
                {
                    Assert.True(b.Remote, $"sessão dentro de obrigação presencial ({b.Label}) às {minute / 60}h{minute % 60:00}");
                    var work = day.Remote.Single(r => minute >= r.Start && minute < r.End);   // nunca no deslocamento
                    if (kind == "work_break") Assert.Contains(work.Breaks, br => minute >= br.Start && minute < br.End);
                    else
                    {
                        Assert.Equal("during_work", kind);
                        Assert.True(C.Occupations[p.OccupationId].PlayDuringWorkFlexibility > 0);
                    }
                }
                checkedStarts++;
            }
        }
        Assert.True(checkedStarts > 500, $"amostra pequena: {checkedStarts}");
    }

    [Fact]
    [Trait("Classe", "Tendência")]
    public void TempoLivreNaoEJogar_DedicacaoPesaMaisQueDisponibilidade()
    {
        var e = TestSupport.NewEngine(4, 1500);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(28));
        var w = e.World;
        var regs = w.Persons.Where(p => p.CharacterId > 0).ToList();
        double Days(IEnumerable<Person> ps) => ps.Average(p => (double)p.DayPlans.Count(d => d.Sessions > 0));
        var idleLow = regs.Where(p => p.OccupationId == "sem_ocupacao" && p.Traits.GameImportance < 400).ToList();
        var workerHigh = regs.Where(p => p.OccupationId == "comercial" && p.Traits.GameImportance > 750).ToList();
        Assert.True(idleLow.Count >= 5 && workerHigh.Count >= 5, $"amostra: {idleLow.Count}/{workerHigh.Count}");
        // Sem ocupacao e pouca importancia joga menos dias que o trabalhador que prioriza o jogo.
        Assert.True(Days(idleLow) < Days(workerHigh), $"{Days(idleLow):0.0} x {Days(workerHigh):0.0} dias com sessao em 14");
        // Quem trabalha e estuda quase nao tem tempo nos dias uteis: o motivo fica na agenda.
        var busy = regs.Where(p => p.OccupationId == "trabalho_estudo").ToList();
        Assert.NotEmpty(busy);
        Assert.Contains(busy.SelectMany(p => p.DayPlans), d => d.Reason == "no_free_time");
        Assert.All(regs, p => Assert.True(p.DayPlans.Count <= RS.PlanHistoryDays));
    }

    [Fact]
    [Trait("Classe", "Regra do preset")]
    public void Renda_PelaOcupacaoEPeloPais_ApoioPrincipalmenteParaQuemEstuda()
    {
        var people = Enumerable.Range(1, 3000).Select(i => PopulationGenerator.Generate(i, C, 3)).ToList();
        var students = people.Where(p => p.OccupationId is "estudante_manha" or "estudante_tarde").ToList();
        Assert.All(students, p => Assert.Equal(0, p.IncomeCentsPerMonth));
        Assert.True(students.Count(p => p.SupportCentsPerMonth > 0) * 100 / students.Count >= 85);
        double Avg(string occ, string loc) => people.Where(p => p.OccupationId == occ && p.ResidenceId == loc).DefaultIfEmpty().Average(p => p == null ? 0 : (double)p.IncomeCentsPerMonth);
        Assert.True(Avg("comercial", "eua") > Avg("comercial", "brasil") * 2);
        // Ocupacao independente do perfil: hardcore tambem trabalha.
        Assert.Contains(people, p => p.ProfileId == "hardcore" && p.OccupationId == "comercial");
        Assert.Contains(people, p => p.ProfileId == "casual" && p.OccupationId == "sem_ocupacao");
        Assert.All(people, p => Assert.True(p.ExpectedWeeklyMinutes >= 0));
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void OcupacoesInvalidas_SaoErro()
    {
        var set = TestSupport.FreshPresetSet();
        var occ = set.Occupations.First(o => o.Id == "comercial");
        occ.Blocks.Add(new ObligationBlockDefinition { Days = { "mon" }, Start = "10:00", End = "11:00", Label = "trabalho" });
        occ.Blocks.Add(new ObligationBlockDefinition { Days = { "quarta" }, Start = "25:00", End = "11:00", Label = "trabalho" });
        int i = set.Occupations.IndexOf(occ);
        var errors = ContentValidator.Validate(set).Where(x => x.Severity == IssueSeverity.Error).ToList();
        Assert.Contains(errors, x => x.Path == $"occupations[{i}].blocks[1]" && x.Message.Contains("Sobrepõe"));
        Assert.Contains(errors, x => x.Path == $"occupations[{i}].blocks[2].start");
        Assert.Contains(errors, x => x.Path == $"occupations[{i}].blocks[2].days");
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Rotina_EDeterministica_EEntraNoHash()
    {
        string Hash()
        {
            var e = TestSupport.NewEngine(8, 80);
            e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
            e.AdvanceTo(SimTime.Days(6));
            Assert.Contains(e.World.Persons, p => p.DayPlans.Count > 0);
            return StateHasher.Compute(e.World);
        }
        Assert.Equal(Hash(), Hash());
    }
}

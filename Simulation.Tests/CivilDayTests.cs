using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Population;
using Simulation.Core.Time;

namespace Simulation.Tests;

/// <summary>
/// Dia civil x ciclo de sono (conferencia da R1): o dia acordado e so organizacao interna; o
/// calendario e os relatorios usam o dia civil, e o recorte civil cobre a janela inteira, uma vez.
/// </summary>
public class CivilDayTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private static RoutineSettingsDefinition RS => C.Routine;

    /// <summary>Dias acordados de -1 a 7 (domingo anterior ate a segunda seguinte) com os feriados dados (por dia civil 0..6).</summary>
    private static List<DailySchedule.DayLayout> Days(Person p, Func<int, bool> holiday)
    {
        var occ = C.Occupations[p.OccupationId];
        bool H(int d) => d >= 0 && d < 7 && holiday(d);
        return Enumerable.Range(-1, 9).Select(d => DailySchedule.Build(p, occ, RS, ((d % 7) + 7) % 7, H(d), H(d - 1), H(d + 1))).ToList();
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void SemanaCivil_ComFeriadoETurnoNoturno_Fecha168hSemLacunaNemSobreposicao()
    {
        var people = Enumerable.Range(1, 800).Select(i => PopulationGenerator.Generate(i, C, 13)).ToList();
        foreach (var p in people)
        {
            var week = DailySchedule.CivilWindow(Days(p, d => d == 2), -1, 0, 7);   // feriado na quarta
            Assert.Equal(0, week[0].Start);
            Assert.Equal(7 * 1440, week[^1].End);
            for (int i = 0; i + 1 < week.Count; i++) Assert.Equal(week[i].End, week[i + 1].Start);
            Assert.Equal(7 * 1440, week.Sum(t => t.End - t.Start));
        }
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void TurnoDeSexta_AsHorasDepoisDaMeiaNoitePertencemAoSabadoCivil_EOSonoAparece()
    {
        var p = PopulationGenerator.Generate(1, C, 1);
        p.OccupationId = "turno_noturno"; p.ShiftOffsetMinutes = 0; p.CommutePermille = 1000; p.SleepNeedMinutes = 480; p.BedtimeMinute = 1410;
        // Recorte so do sabado civil (dia 5).
        var saturday = DailySchedule.CivilWindow(Days(p, _ => false), -1, 5, 1);
        Assert.Equal("work", saturday[0].Kind);                       // 00:00: ainda no turno de sexta
        Assert.Equal(0, saturday[0].Start);
        Assert.Contains(saturday, t => t.Kind == "commute");
        var sleep = saturday.Where(t => t.Kind == "sleep").ToList();
        Assert.NotEmpty(sleep);                                         // o sono depois do turno conta no sabado
        Assert.True(sleep.Sum(t => t.End - t.Start) >= p.SleepNeedMinutes);
        Assert.Equal(1440, saturday.Sum(t => t.End - t.Start));
    }

    /// <summary>
    /// Cada noite e fechada uma unica vez, com deficit minimo zero: o deficit final de cada pessoa e
    /// exatamente a soma das noites fechadas da agenda (regra explicita, com o piso e o teto a cada noite).
    /// </summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void CadaNoite_FechadaUmaVez_DeficitNuncaNegativo()
    {
        var e = TestSupport.NewEngine(4, 600);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(25) + SimTime.Hours(12));
        var w = e.World;
        int checkedPeople = 0;
        foreach (var p in w.Persons.Where(x => x.CharacterId > 0 && x.DayPlans.Count > 0 && x.DayPlans.All(r => r.Reason != "came_back")))
        {
            int deficit = 0;
            foreach (var rec in p.DayPlans)
            {
                var day = w.Behavior.LayoutOf(p, rec.LocalDay);
                long wake = BehaviorSystem.LocalDayStart(p, rec.LocalDay) + day.Wake * SimTime.TicksPerMinute;
                if (wake > w.Clock.Tick) continue;   // noite ainda em andamento: nao fechada
                deficit = Math.Clamp(deficit + DailySchedule.DeficitChange(p.SleepNeedMinutes, rec.SleepMinutes, RS), 0, DailySchedule.MaxDeficit(p, RS));
                Assert.Equal(deficit, rec.DeficitMinutes);
            }
            Assert.Equal(deficit, p.SleepDeficitMinutes);
            Assert.True(p.SleepDeficitMinutes >= 0);
            checkedPeople++;
        }
        Assert.True(checkedPeople > 100, $"pessoas conferidas: {checkedPeople}");
    }

    [Fact]
    [Trait("Classe", "Regra do preset")]
    public void Escala5x2_CincoDiasDeSegundaASexta_FolgaSempreNoFimDeSemana_MaisComumQueA6x1()
    {
        var people = Enumerable.Range(1, 6000).Select(i => PopulationGenerator.Generate(i, C, 17)).ToList();
        var five = people.Where(p => p.OccupationId == "escala_5x2").ToList();
        var six = people.Where(p => p.OccupationId == "escala_6x1").ToList();
        Assert.True(five.Count > six.Count * 3 / 2, $"5x2 {five.Count} x 6x1 {six.Count}");
        var occ = C.Occupations["escala_5x2"];
        foreach (var p in five)
        {
            for (int d = 0; d < 5; d++) Assert.NotEmpty(DailySchedule.Obligations(p, occ, d, false));   // segunda a sexta
            Assert.Empty(DailySchedule.Obligations(p, occ, 5, false));                                // sabado livre
            Assert.Empty(DailySchedule.Obligations(p, occ, 6, false));                                // domingo livre
        }
    }
}

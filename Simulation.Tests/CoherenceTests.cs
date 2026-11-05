using Simulation.Core.Behavior;
using Simulation.Core.Content;
using Simulation.Core.Population;

namespace Simulation.Tests;

/// <summary>
/// Coerencia entre os horarios da pessoa (D-045): turno, sono, hora preferida de jogar e
/// tempo livre precisam contar a mesma historia. Nada de jogar as 22 h e dormir as 19:49.
/// </summary>
public class CoherenceTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private static RoutineSettingsDefinition RS => C.Routine;

    private static readonly List<Person> People = Enumerable.Range(1, 4000).Select(i => PopulationGenerator.Generate(i, C, 7)).ToList();

    private static List<DailySchedule.DayLayout> Week(Person p) =>
        Enumerable.Range(0, 7).Select(d => DailySchedule.Build(p, C.Occupations[p.OccupationId], RS, d, false, false, false)).ToList();

    [Fact]
    public void TurnoDiurno_NinguemAcordaDeMadrugada()
    {
        foreach (var p in People.Where(p => !DailySchedule.HasNightShift(C.Occupations[p.OccupationId])))
            foreach (var day in Week(p).Where(d => !d.FreeDay))
                Assert.True(day.Wake >= ContentValidator.EarliestDayShiftWakeMinute,
                    $"{p.OccupationId} acorda às {day.Wake / 60:00}:{day.Wake % 60:00}");
    }

    [Fact]
    public void HoraPreferida_CaiNoTempoLivreDaRotina()
    {
        int all = 0, total = 0;
        foreach (var p in People)
        {
            // Dias do habito: obrigacao com tempo para a sessao (limitada a maior janela deles); senao dia livre.
            var (pool, session) = DailySchedule.PreferredPool(Week(p), Math.Max(RS.MinSessionMinutes, p.Traits.SessionMinutes), RS);
            if (pool.Count == 0) continue;
            int near = pool.Count(d => DailySchedule.StartsAtPreferred(d, p.HabitualStartHour, session, RS));
            // Na maioria dos dias com tempo livre, a sessao tipica comeca na hora preferida (ou logo
            // que a janela abre) e cabe inteira antes de dormir: nada de preferir 21 h e precisar comecar as 20:47.
            Assert.True(near * 2 > pool.Count, $"{p.OccupationId}: costuma {p.HabitualStartHour}h, perto em {near} de {pool.Count} dias; sessao {p.Traits.SessionMinutes}; " + string.Join(" / ", pool.Select(d => $"acorda {d.Wake} " + string.Join(",", d.Windows.Select(w => $"{w.Start}-{w.End}")))));
            total++;
            if (near == pool.Count) all++;
        }
        Assert.True(all * 100 >= total * 85, $"em todos os dias: {all} de {total}");
        Assert.Equal(People.Select(p => p.Traits.PreferredStartHour), People.Select(p => p.BaseTraits.PreferredStartHour));
    }

    /// <summary>
    /// Vontade e horario: quem prioriza o jogo chega do trabalho e ja joga; o casual chega e
    /// so joga mais tarde, por menos tempo (exemplo do criador).
    /// </summary>
    [Fact]
    public void QuemPriorizaOJogo_ComecaLogoQueChega_OCasualMaisTarde()
    {
        static double Delay(Person p)
        {
            var day = Week(p)[1];   // terca
            var w = day.Windows.OrderByDescending(x => x.End - x.Start).First();
            int pref = p.HabitualStartHour * 60;
            if (pref < day.Wake) pref += 1440;
            return Math.Max(0, pref - w.Start);
        }
        var workers = People.Where(p => p.OccupationId == "comercial").ToList();
        var eager = workers.Where(p => p.Traits.GameImportance >= 750).ToList();
        var casual = workers.Where(p => p.Traits.GameImportance <= 400).ToList();
        Assert.True(eager.Count >= 20 && casual.Count >= 20, $"amostra {eager.Count}/{casual.Count}");
        double de = eager.Average(Delay), dc = casual.Average(Delay);
        Assert.True(de <= 75, $"quem prioriza espera {de:0} min depois de chegar");
        Assert.True(de + 60 < dc, $"prioriza {de:0} min x casual {dc:0} min");
        Assert.True(casual.Average(p => p.Traits.SessionMinutes) < eager.Average(p => p.Traits.SessionMinutes));
    }

    /// <summary>Caso do criador: hardcore de meio periodo de manha, livre as 13:25, ja joga por volta das 14 h.</summary>
    [Fact]
    public void Hardcore_DeMeioPeriodoDeManha_JogaLogoDepoisDoAlmoco()
    {
        var eager = People.Where(p => p.OccupationId == "meio_periodo_manha" && p.Traits.GameImportance >= 700).ToList();
        Assert.True(eager.Count >= 10, $"amostra {eager.Count}");
        foreach (var p in eager)
        {
            var monday = Week(p)[0];
            var w = monday.Windows.OrderByDescending(x => x.End - x.Start).First();
            int delay = p.HabitualStartHour * 60 - w.Start;
            Assert.True(delay is >= -59 and <= 90, $"livre às {w.Start / 60}:{w.Start % 60:00}, costuma {p.HabitualStartHour}h");
        }
    }

    /// <summary>
    /// v3 (D-051): o tempo livre nao estica a sessao normal; ele aparece como mais tempo para
    /// continuar e voltar. A estimativa de quem prioriza o jogo e maior no dia com mais tempo livre.
    /// </summary>
    [Fact]
    public void MaisTempoLivre_MaisJogoEstimado_ParaQuemPriorizaOJogo()
    {
        var p = People.First(x => x.Traits.GameImportance >= 750);
        var part = DailySchedule.Build(p, C.Occupations["meio_periodo_manha"], RS, 1, false, false, false);
        var full = DailySchedule.Build(p, C.Occupations["comercial"], RS, 1, false, false, false);
        Assert.True(part.FreeMinutes > full.FreeMinutes);
        Assert.Equal(DailySchedule.SessionMinutes(p, RS), p.Traits.SessionMinutes);
        var rng1 = new Simulation.Core.Random.DeterministicRng(7);
        var rng2 = new Simulation.Core.Random.DeterministicRng(7);
        int a = 0, b = 0;
        for (int i = 0; i < 60; i++) { a += DailySchedule.SimulateDay(p, part, 0, rng1, RS).Minutes; b += DailySchedule.SimulateDay(p, full, 0, rng2, RS).Minutes; }
        Assert.True(a > b, $"meio periodo {a / 60} min/dia x comercial {b / 60}");
    }

    [Fact]
    public void HoraDeDormir_SeAcostumaAsObrigacoes_EODiaLivreNaoVaiMuitoMaisTarde()
    {
        foreach (var p in People)
        {
            var occ = C.Occupations[p.OccupationId];
            if (DailySchedule.HasNightShift(occ)) continue;
            var required = DailySchedule.RequiredBedtimes(p, occ, RS);
            if (required.Count == 0) continue;
            required.Sort();
            int typical = required[required.Count / 2];
            Assert.True(p.BedtimeMinute <= Math.Max(1200, typical + RS.BedtimeAdaptMinutes),
                $"{p.OccupationId}: deita {p.BedtimeMinute / 60 % 24:00}:{p.BedtimeMinute % 60:00} no dia livre, {typical / 60 % 24:00}:{typical % 60:00} antes de obrigação");
        }
    }

    [Fact]
    public void Exemplo_MeioPeriodo_SemMadrugada_ComJogoAntesDeDormir()
    {
        var part = People.Where(p => p.OccupationId.StartsWith("meio_periodo", StringComparison.Ordinal)).ToList();
        Assert.True(part.Count > 50, $"amostra {part.Count}");
        foreach (var p in part)
        {
            var week = Week(p);
            var monday = week[0];
            var (pool, session) = DailySchedule.PreferredPool(week, Math.Max(RS.MinSessionMinutes, p.Traits.SessionMinutes), RS);
            Assert.True(monday.Blocks[0].Start >= 6 * 60, $"{p.OccupationId} sai às {monday.Blocks[0].Start / 60}h");
            int pref = p.HabitualStartHour * 60;
            if (pref < monday.Wake) pref += 1440;
            Assert.True(pref < monday.Bed, $"costuma {p.HabitualStartHour}h e dorme às {monday.Bed / 60 % 24}h");
            // A sessao tipica cabe inteira depois da hora preferida (quem sai as 17 h nao "prefere" 21 h com sessao de 3 h).
            Assert.Contains(monday, pool);   // meio periodo sempre tem tempo para jogar no dia util
            Assert.True(DailySchedule.StartsAtPreferred(monday, p.HabitualStartHour, session, RS),
                $"{p.OccupationId}: costuma {p.HabitualStartHour}h, sessão de {p.Traits.SessionMinutes} min, dorme às {monday.Bed / 60 % 24}h");
        }
        Assert.Contains(part, p => p.OccupationId == "meio_periodo_manha");
        Assert.Contains(part, p => p.OccupationId == "meio_periodo_tarde");
    }

    [Fact]
    public void Validador_RecusaTurnoDiurnoQueExigeAcordarDeMadrugada()
    {
        var set = TestSupport.FreshPresetSet();
        int i = set.Occupations.FindIndex(o => o.Id == "comercial");
        set.Occupations[i].StartVariationMinutes = 240;   // 08:00 - 4 h: alguem sairia de casa as 03:00
        var errors = ContentValidator.Validate(set).Where(x => x.Severity == IssueSeverity.Error).ToList();
        Assert.Contains(errors, x => x.Path == $"occupations[{i}].blocks[0].start");
        Assert.DoesNotContain(ContentValidator.Validate(TestSupport.FreshPresetSet()), x => x.Severity == IssueSeverity.Error);
    }
}

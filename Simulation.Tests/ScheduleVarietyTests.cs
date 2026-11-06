using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Population;
using Simulation.Core.Scheduling;
using Simulation.Core.Time;

namespace Simulation.Tests;

/// <summary>
/// R6, variedade de horarios: cronotipo (tendencia individual, so em parte no sono e no horario
/// preferido), faixa continua que atravessa a meia-noite, vespera de folga, continuidade do turno da
/// noite, jogar depois do turno como possibilidade e sono deslocado (diferente de sono cortado).
/// </summary>
public class ScheduleVarietyTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private static RoutineSettingsDefinition RS => C.Routine;

    private static readonly Lazy<List<Person>> Sample = new(() =>
        Enumerable.Range(1, 5000).Select(id => PopulationGenerator.Generate(id, C, 31)).ToList());

    private static Person Standard(string occupation)
    {
        var p = PopulationGenerator.Generate(1, C, 1);
        p.OccupationId = occupation;
        p.ShiftOffsetMinutes = 0;
        p.CommutePermille = 1000;
        p.DayOffIndex = -1;
        p.SleepNeedMinutes = 480;
        p.BedtimeMinute = 1410;
        p.SleepDeficitMinutes = 0;
        p.FreeEveShiftMinutes = 0;
        return p;
    }

    private static DailySchedule.DayLayout Day(Person p, int dow, RoutineSettingsDefinition? rs = null, int nextCommit = -1) =>
        DailySchedule.Build(p, C.Occupations[p.OccupationId], rs ?? RS, dow, false, false, false, null, 0, nextCommit);

    private static double Spearman(IReadOnlyList<int> x, IReadOnlyList<int> y)
    {
        static double[] Rank(IReadOnlyList<int> v)
        {
            var order = Enumerable.Range(0, v.Count).OrderBy(i => v[i]).ToArray();
            var r = new double[v.Count];
            for (int i = 0; i < order.Length;)
            {
                int j = i;
                while (j + 1 < order.Length && v[order[j + 1]] == v[order[i]]) j++;
                for (int k = i; k <= j; k++) r[order[k]] = (i + j) / 2.0;
                i = j + 1;
            }
            return r;
        }
        var a = Rank(x); var b = Rank(y);
        double ma = a.Average(), mb = b.Average();
        double cov = a.Zip(b, (u, v) => (u - ma) * (v - mb)).Sum();
        return cov / Math.Sqrt(a.Sum(u => (u - ma) * (u - ma)) * b.Sum(v => (v - mb) * (v - mb)));
    }

    /// <summary>Cronotipo em fluxo proprio: desligar a influencia nao muda nenhum outro atributo.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Cronotipo_NaoMudaOsDemaisAtributos()
    {
        var set = TestSupport.FreshPresetSet();
        set.Population.Routine.ChronotypeBedtimeInfluencePermille = 0;
        set.Population.Routine.FreeEve.ChancePermille = 0;
        set.Population.Links.RemoveAll(l => l.Anchor == "traits.chronotype");
        var off = ContentDatabase.Create(set);
        for (int id = 1; id <= 1500; id++)
        {
            var a = PopulationGenerator.Generate(id, C, 21);
            var b = PopulationGenerator.Generate(id, off, 21);
            Assert.Equal(a.Traits.Chronotype, b.Traits.Chronotype);
            Assert.Equal(0, b.FreeEveShiftMinutes);
            foreach (var k in PersonTraits.DynamicKeys) Assert.Equal(a.Traits.Get(k), b.Traits.Get(k));
            foreach (var k in PersonPreferences.Keys) Assert.Equal(a.Preferences.Get(k), b.Preferences.Get(k));
            Assert.Equal((a.PersonalityId, a.OccupationId, a.ResidenceId, a.SleepNeedMinutes, a.Traits.SessionMinutes), (b.PersonalityId, b.OccupationId, b.ResidenceId, b.SleepNeedMinutes, b.Traits.SessionMinutes));
            Assert.Equal(a.Quirks.Select(q => q.Id), b.Quirks.Select(q => q.Id));
        }
    }

    /// <summary>
    /// O cronotipo pesa em parte: mais forte no sono que no horario preferido; os dois extremos
    /// existem (matinais e notivagos), e ser notivago nao obriga a preferir jogar de madrugada.
    /// </summary>
    [Fact]
    [Trait("Classe", "Estatistico")]
    public void Cronotipo_InfluenciaParcialEDoisExtremos()
    {
        var people = Sample.Value;
        var free = people.Where(p => p.OccupationId is "sem_ocupacao" or "aposentado").ToList();
        double pref = Spearman(people.Select(p => p.Traits.Chronotype).ToList(), people.Select(p => p.Traits.PreferredStartHour).ToList());
        double bed = Spearman(free.Select(p => p.Traits.Chronotype).ToList(), free.Select(p => p.BedtimeMinute).ToList());
        Assert.InRange(pref, 0.2, 0.7);
        Assert.True(bed > pref && bed < 0.99, $"sono {bed:0.00}, preferido {pref:0.00}");
        Assert.True(people.Count(p => p.Traits.PreferredStartHour < 12) > people.Count / 30, "matinais");
        Assert.True(people.Count(p => p.Traits.PreferredStartHour >= 24) > people.Count / 30, "depois da meia-noite");
        var owls = people.Where(p => p.Traits.Chronotype >= 800).ToList();
        Assert.Contains(owls, p => p.Traits.PreferredStartHour < 20);   // notivago que joga cedo: possivel
        Assert.True(free.Count(p => p.BedtimeMinute >= 1560) > 0 && free.Count(p => p.BedtimeMinute <= 1350) > 0, "dorme depois das 02h e antes das 22h30");
    }

    /// <summary>Obrigacoes continuam limitando: quem trabalha cedo nao fica com a hora natural de um notivago.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Cronotipo_ObrigacoesLimitam()
    {
        foreach (var p in Sample.Value.Where(p => p.OccupationId is "comercial" or "estudante_manha"))
        {
            var occ = C.Occupations[p.OccupationId];
            var required = DailySchedule.RequiredBedtimes(p, occ, RS);
            required.Sort();
            Assert.True(p.BedtimeMinute <= Math.Max(1200, required[required.Count / 2] + RS.BedtimeAdaptMinutes));
        }
    }

    /// <summary>Faixa continua: 03h vem depois das 23h; horario antes de acordar, longe da noite, vale o acordar.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void HorarioContinuo_AtravessaAMeiaNoite()
    {
        var owl = new DailySchedule.DayLayout { Wake = 600, Bed = 1620 };     // acorda 10h, dorme 03h
        Assert.Equal(1620, DailySchedule.AwakeMinute(180, owl));               // 03h = depois das 23h
        Assert.Equal(1500, DailySchedule.AwakeMinute(25 * 60, owl));           // 25 h = 01h
        Assert.Equal(1380, DailySchedule.AwakeMinute(1380, owl));
        var lark = new DailySchedule.DayLayout { Wake = 450, Bed = 1380 };    // acorda 7h30, dorme 23h
        Assert.Equal(420, DailySchedule.AwakeMinute(420, lark));               // 7h: cedo demais hoje, vale o acordar
        Assert.Equal(1500, DailySchedule.AwakeMinute(60, lark));               // 01h fica perto da noite
        var night = new DailySchedule.DayLayout { Wake = 950, Bed = 1860 };   // turno da noite: acorda 15h50
        Assert.Equal(1860, DailySchedule.AwakeMinute(420, night));             // 7h = depois do turno
        Assert.Equal(840, DailySchedule.AwakeMinute(840, night));              // 14h: ja passou, joga ao acordar
    }

    /// <summary>Vespera de folga: deslocamento individual, so antes do dia livre, respeitando o compromisso.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void VesperaDeFolga_DeslocamentoIndividual()
    {
        var p = Standard("comercial");
        int thursday = Day(p, 3).Bed, friday = Day(p, 4).Bed, sunday = Day(p, 6).Bed;
        Assert.Equal(p.BedtimeMinute, friday);             // sem deslocamento: a hora natural
        p.FreeEveShiftMinutes = 90;
        Assert.Equal(thursday, Day(p, 3).Bed);             // amanha tem trabalho
        Assert.Equal(p.BedtimeMinute + 90, Day(p, 4).Bed); // sabado livre
        Assert.Equal(sunday, Day(p, 6).Bed);               // segunda tem trabalho
        var saturday = Day(p, 5);
        Assert.Equal(Day(p, 4).Bed - 1440, saturday.SleepStart);
        Assert.Equal(saturday.SleepStart + p.SleepNeedMinutes, saturday.Wake);   // dorme o necessario: desloca, nao corta
        // Compromisso no sabado as 9h: acorda a tempo, com o sono necessario.
        var withCommit = Day(p, 4, nextCommit: 9 * 60);
        Assert.True(withCommit.Bed <= 1440 + 9 * 60 - RS.PrepMinutes - p.SleepNeedMinutes);
    }

    /// <summary>Turno da noite: na folga, a hora de dormir continua perto do ritmo dos turnos.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void TurnoDaNoite_FolgaComContinuidade()
    {
        var p = Standard("turno_noturno");
        var occ = C.Occupations[p.OccupationId];
        int ws = DailySchedule.NightSleepStart(p, occ, RS)!.Value;
        int saturdayBed = Day(p, 5).Bed;
        Assert.Equal(1410 + (ws - 1410) * RS.NightShiftOffContinuityPermille / 1000, saturdayBed);
        var set = TestSupport.FreshPresetSet();
        set.Population.Routine.NightShiftOffContinuityPermille = 0;
        Assert.Equal(1410, Day(p, 5, ContentDatabase.Create(set).Routine).Bed);   // sem continuidade: volta a hora natural
        // Antes do proximo turno ha sono suficiente (domingo dorme tarde, segunda trabalha as 22h).
        var monday = Day(p, 0);
        Assert.True(monday.Wake - monday.SleepStart >= p.SleepNeedMinutes);
    }

    /// <summary>Depois do turno: possibilidade, sem criar tempo livre nem mudar a chance do dia.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void DepoisDoTurno_PossibilidadeSemCriarTempoLivre()
    {
        var p = Standard("turno_noturno");
        var monday = Day(p, 0);
        Assert.True(monday.NightShiftAhead);
        var opt = Assert.Single(monday.OptionalWindows);
        Assert.Equal(monday.Bed, opt.Start);                       // o sono planejado comeca na chegada
        Assert.True(opt.End - opt.Start <= RS.PostShiftWindowMinutes);
        Assert.Contains(opt, monday.Windows);
        var set = TestSupport.FreshPresetSet();
        set.Population.Routine.PostShiftWindowMinutes = 0;
        var without = Day(p, 0, ContentDatabase.Create(set).Routine);
        Assert.Empty(without.OptionalWindows);
        Assert.Equal(without.FreeMinutes, monday.FreeMinutes);   // nao conta como tempo livre
        Assert.Equal(DailySchedule.BaseChance(p, without, RS), DailySchedule.BaseChance(p, monday, RS));
        Assert.Equal(DailySchedule.SessionPriority(p, without, RS), DailySchedule.SessionPriority(p, monday, RS));
        Assert.Equal(without.Bed, monday.Bed);
    }

    /// <summary>
    /// Deslocar x cortar: quem foi dormir tarde num dia livre acorda mais tarde e nao ganha deficit;
    /// quem precisa acordar para a obrigacao acorda na hora e o que faltou vira deficit.
    /// </summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void SonoDeslocado_DiferenteDeSonoCortado()
    {
        var e = TestSupport.NewEngine(13, 80);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(2));
        var w = e.World;
        bool shiftedCase = false, cutCase = false;
        foreach (var p in w.Persons.Where(x => x.CharacterId > 0).OrderBy(x => x.Id))
        {
            if (shiftedCase && cutCase) break;
            if (p.IsOnline) continue;
            long now = w.Clock.Tick;
            long day = BehaviorSystem.LocalDay(p, now) + 1;   // amanha (local), ainda nao planejado
            var lay = w.Behavior.LayoutOf(p, day);
            long start = BehaviorSystem.LocalDayStart(p, day);
            long plannedSleep = start + lay.SleepStart * SimTime.TicksPerMinute, plannedWake = start + lay.Wake * SimTime.TicksPerMinute;
            if (plannedSleep <= now + SimTime.Hours(1)) continue;
            int latest = DailySchedule.LatestWake(lay, w.Content.Routine);
            bool free = (latest == int.MaxValue || latest - lay.Wake >= p.SleepNeedMinutes) && lay.Bed - lay.Wake >= p.SleepNeedMinutes + 120;
            bool tight = latest != int.MaxValue && latest - lay.Wake <= 5;
            if ((free && shiftedCase) || (tight && cutCase) || (!free && !tight)) continue;
            // Simula ter ficado acordado ate 3 h antes do acordar planejado (sem sessao): a saida do jogo foi tarde.
            e.AdvanceTo(plannedWake - SimTime.Hours(3));
            if (p.IsOnline) continue;
            p.LastLogoutTick = w.Clock.Tick;
            int deficit = p.SleepDeficitMinutes;
            e.AdvanceTo(plannedWake + 1);
            if (free)
            {
                Assert.True(p.SleepUntilTick > plannedWake, "acorda mais tarde");
                e.AdvanceTo(p.SleepUntilTick + 1);
                var rec = p.DayPlans.LastOrDefault(r => r.LocalDay == day);
                Assert.True(p.SleepDeficitMinutes <= deficit, $"deficit {deficit} -> {p.SleepDeficitMinutes}");
                if (rec != null) Assert.True(rec.SleepShiftMinutes > 0);
                shiftedCase = true;
            }
            else
            {
                Assert.Equal(0, p.SleepUntilTick);
                Assert.True(p.SleepDeficitMinutes > deficit || deficit >= DailySchedule.MaxDeficit(p, w.Content.Routine), "o que faltou vira deficit");
                cutCase = true;
            }
        }
        Assert.True(shiftedCase && cutCase, $"casos: deslocado {shiftedCase}, cortado {cutCase}");
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Agenda_PrimeiroEventoDeUmTipo()
    {
        var s = new EventScheduler();
        s.Schedule(50, "b", 1);
        s.Schedule(30, "a", 2);
        s.Schedule(20, "b", 3);
        s.Schedule(20, "b", 4);
        Assert.Equal(3, s.EarliestOf("b")!.TargetId);   // empate: a sequencia
        Assert.Equal(2, s.EarliestOf("a")!.TargetId);
        Assert.Null(s.EarliestOf("c"));
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Validador_Cronotipo()
    {
        var set = TestSupport.FreshPresetSet();
        set.Population.Traits.Chronotype = new TraitRange { Mean = 500, Spread = 300 };
        set.Population.Links.RemoveAll(l => l.Anchor == "traits.chronotype");
        Assert.Contains(ContentValidator.Validate(set), i => i.Severity == IssueSeverity.Error && i.Path == "traits.chronotype");
        var ok = TestSupport.FreshPresetSet();
        ok.Population.Routine.PostShiftWindowMinutes = 500;
        Assert.Contains(ContentValidator.Validate(ok), i => i.Severity == IssueSeverity.Error && i.Path == "routine.postShiftWindowMinutes");
    }
}

using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Events;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

/// <summary>Sono, dificuldade de parar (P3) e jogar no trabalho remoto (D-044).</summary>
public class SleepWorkTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private static RoutineSettingsDefinition RS => C.Routine;

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
        return p;
    }

    private static DailySchedule.DayLayout Day(Person p, int dow) =>
        DailySchedule.Build(p, C.Occupations[p.OccupationId], RS, dow, false, false, false);

    private static (SimulationEngine E, List<SimEvent> Events) Run(ulong seed, int population, int days)
    {
        var e = TestSupport.NewEngine(seed, population);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        var all = new List<SimEvent>();
        for (int d = 0; d < days; d++)
        {
            e.AdvanceTo(SimTime.Days(d + 1));
            all.AddRange(e.World.Events.Drain());
        }
        return (e, all);
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void ManhaLivre_RecuperaODeficit_DiaDeTrabalhoNao()
    {
        var p = Standard("comercial");
        p.SleepDeficitMinutes = 200;
        var saturday = Day(p, 5);
        Assert.Equal(Math.Min(200, RS.RecoveryFreeNightMinutes), saturday.RecoveryMinutes);   // dorme mais
        var monday = Day(p, 0);
        Assert.Equal(0, monday.RecoveryMinutes);   // o despertador manda
    }

    [Fact]
    [Trait("Classe", "Regra do preset")]
    public void TrabalhaEEstuda_DormeMenosQueOPrecisoNosDiasUteis_EAcumulaDeficit()
    {
        var p = Standard("trabalho_estudo");
        var monday = Day(p, 0);
        var tuesday = Day(p, 1);
        int sleep = tuesday.Wake + 1440 - monday.Bed;
        Assert.True(sleep < p.SleepNeedMinutes, $"dorme {sleep} min, precisa de {p.SleepNeedMinutes}");
        var (e, _) = Run(6, 1500, 21);
        var regs = e.World.Persons.Where(x => x.CharacterId > 0).ToList();
        double Avg(string occ) => regs.Where(x => x.OccupationId == occ).Average(x => (double)x.SleepDeficitMinutes);
        Assert.True(Avg("trabalho_estudo") > Avg("comercial") + 30, $"{Avg("trabalho_estudo"):0} x {Avg("comercial"):0}");
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Extensao_TemLimites_ConsomeSonoSoNoFimDoDia_ENuncaInvadeObrigacoes()
    {
        var (e, events) = Run(2, 800, 28);
        var w = e.World;
        var ext = events.Where(x => x.Type == "session_extended").ToList();
        Assert.True(ext.Count > 50, $"poucas extensoes: {ext.Count}");
        Assert.All(ext, x => Assert.InRange(int.Parse(x.Detail!.Trim('+', ' ', 'm', 'i', 'n')), 10, RS.ExtendMaxMinutes));
        Assert.All(ext.SelectMany(x => x.Causes).Where(c => c.Factor == "sleep_cut"), c => Assert.InRange(c.WeightPermille, 1, RS.MaxSleepCutMinutes));
        var late = events.Where(x => x.Type == "stayed_up_late").ToList();
        Assert.NotEmpty(late);
        foreach (var ev in late)
        {
            var p = w.GetPerson((int)ev.Entities[0].Id);
            // A atividade em curso pode terminar dentro da tolerancia de saida (mais um trecho de viagem).
            Assert.InRange(ev.Causes[1].WeightPermille, 1, RS.MaxSleepCutMinutes + C.Behavior.LogoutToleranceMinutes + 30);
            // Nunca invade a obrigacao seguinte: sai antes do primeiro bloco do dia local em que saiu.
            long localDay = BehaviorSystem.LocalDay(p, ev.Tick);
            int minute = (int)((ev.Tick - BehaviorSystem.LocalDayStart(p, localDay)) / SimTime.TicksPerMinute);
            var day = w.Behavior.LayoutOf(p, localDay);
            Assert.DoesNotContain(day.Blocks, b => minute >= b.Start && minute < b.End);
        }
        Assert.All(w.Persons, p => Assert.True(p.SleepDeficitMinutes <= DailySchedule.MaxDeficit(p, RS)));
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void CenarioControlado_NaoEstende_ESessaoDoTrabalhoNuncaEstende()
    {
        var e = WorldFactory.Create(C, new WorldConfig { Seed = 2, PopulationSize = 300, NeutralMood = true });
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(10));
        Assert.DoesNotContain(e.World.Events.Drain(), x => x.Type is "session_extended" or "stayed_up_late");
    }

    [Fact]
    [Trait("Classe", "Tendência")]
    public void Cansado_JogaMenos_ComMotivoNaAgenda()
    {
        var e = TestSupport.NewEngine(9, 800);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(3));
        var w = e.World;
        var brazilians = w.Persons.Where(p => p.CharacterId > 0 && p.ResidenceId == "brasil").OrderBy(p => p.Id).ToList();
        var tired = brazilians.Where((p, i) => i % 2 == 0).ToList();
        var rested = brazilians.Where((p, i) => i % 2 == 1).ToList();
        for (int d = 3; d < 17; d++)
        {
            e.AdvanceTo(SimTime.Days(d) + SimTime.Hours(23));
            foreach (var p in tired) p.SleepDeficitMinutes = DailySchedule.MaxDeficit(p, RS);   // sem dormir direito
            foreach (var p in rested) p.SleepDeficitMinutes = 0;
            e.AdvanceTo(SimTime.Days(d + 1) + 1);
        }
        double Played(List<Person> g) => g.Average(p => (double)p.DayPlans.Count(x => x.Sessions > 0));
        Assert.True(Played(tired) < Played(rested), $"cansados {Played(tired):0.0} x descansados {Played(rested):0.0}");
        Assert.Contains(tired.SelectMany(p => p.DayPlans), x => x.Reason == "tired");
        Assert.DoesNotContain(rested.SelectMany(p => p.DayPlans), x => x.Reason == "tired");
    }

    [Fact]
    [Trait("Classe", "Tendência")]
    public void ImportanciaTendenciaEAutocontrole_SaoSeparados()
    {
        var people = Enumerable.Range(1, 3000).Select(i => PopulationGenerator.Generate(i, C, 5)).ToList();
        // O perfil puxa as medias, mas dentro do perfil os tres tracos variam de forma independente.
        static double Corr(List<Person> g, Func<Person, int> a, Func<Person, int> b)
        {
            double ma = g.Average(x => (double)a(x)), mb = g.Average(x => (double)b(x));
            double cov = g.Sum(x => (a(x) - ma) * (b(x) - mb));
            double va = g.Sum(x => (a(x) - ma) * (a(x) - ma)), vb = g.Sum(x => (b(x) - mb) * (b(x) - mb));
            return cov / Math.Sqrt(va * vb);
        }
        foreach (var g in people.GroupBy(p => p.ProfileId, StringComparer.Ordinal).Where(g => g.Count() >= 200))
        {
            var list = g.ToList();
            Assert.InRange(Corr(list, x => x.Traits.GameImportance, x => x.Traits.ExtendTendency), -0.12, 0.12);
            Assert.InRange(Corr(list, x => x.Traits.ExtendTendency, x => x.Traits.SelfControl), -0.12, 0.12);
            Assert.InRange(Corr(list, x => x.Traits.GameImportance, x => x.Traits.SelfControl), -0.12, 0.12);
        }
        // Combinacoes que so existem porque os tracos sao separados.
        Assert.Contains(people, p => p.Traits.GameImportance >= 700 && p.Traits.ExtendTendency <= 450);   // prioriza o jogo, mas para na hora
        Assert.Contains(people, p => p.Traits.GameImportance <= 400 && p.Traits.ExtendTendency >= 550);   // pouco importa, mas se empolga
        Assert.Contains(people, p => p.Traits.ExtendTendency >= 650 && p.Traits.SelfControl >= 650);   // puxado, mas disciplinado
        Assert.All(people, p => Assert.InRange(p.SleepNeedMinutes, 300, 660));
    }

    [Fact]
    [Trait("Classe", "Regra do preset")]
    public void TrabalhoRemoto_PausaEEscapada_SoParaQuemPode_ENaoTodoDia()
    {
        var (e, events) = Run(4, 1500, 28);
        var w = e.World;
        var work = events.Where(x => x.Type == "session_started" && x.Causes.Count > 0 && x.Causes[0].Factor is "work_break" or "during_work").ToList();
        Assert.Contains(work, x => x.Causes[0].Factor == "work_break");
        Assert.Contains(work, x => x.Causes[0].Factor == "during_work");
        foreach (var ev in work)
        {
            var occ = C.Occupations[w.GetPerson((int)ev.Entities[0].Id).OccupationId];
            Assert.Contains(occ.Blocks, b => b.Remote);
            if (ev.Causes[0].Factor == "during_work") Assert.True(occ.PlayDuringWorkFlexibility > 0);
        }
        // Nem todos nem todo dia: escapadas em poucos dias do home office.
        var homeOffice = w.Persons.Where(p => p.CharacterId > 0 && p.OccupationId == "home_office").ToList();
        int days = homeOffice.Sum(p => p.DayPlans.Count(d => !d.FreeDay)), slackDays = homeOffice.Sum(p => p.DayPlans.Count(d => d.WorkOverlapMinutes > 0));
        Assert.True(slackDays * 100 < days * 15, $"{slackDays} de {days} dias com escapada");
        Assert.Contains(homeOffice, p => p.DayPlans.All(d => d.WorkOverlapMinutes == 0));
        // Chamado do trabalho: termina a atividade ou larga e sai.
        foreach (var ev in events.Where(x => x.Type == "work_interruption"))
            Assert.Contains(ev.Causes[0].Factor, new[] { "finished_activity", "left_activity" });
    }

    [Fact]
    [Trait("Classe", "Tendência")]
    public void SessaoInterrompivel_EvitaAtividadeCaraDeLargar()
    {
        var (e, _) = Run(3, 120, 5);
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0 && !x.IsOnline);
        var ch = w.GetCharacter(p.CharacterId);
        ch.SessionKind = BehaviorSystem.SessionDuringWork;
        var opts = ActivityScoring.Evaluate(ch, p.Traits, p.Preferences, C, w.Behavior.Routes, w.OccupancyOf, w.Clock.Tick, new DeterministicRng(1), w.Behavior.Quests, w.Behavior.Goals, p.PlaceImpressions);
        int Penalty(string type) => opts.Where(o => o.Activity.Type == type).SelectMany(o => o.Factors).Where(f => f.Key == "interruption").Select(f => f.Contribution).DefaultIfEmpty(0).Min();
        Assert.True(Penalty(ActivityTypes.Hunt) < Penalty(ActivityTypes.Trade));
        ch.SessionKind = BehaviorSystem.SessionNormal;
        var normal = ActivityScoring.Evaluate(ch, p.Traits, p.Preferences, C, w.Behavior.Routes, w.OccupancyOf, w.Clock.Tick, new DeterministicRng(1), w.Behavior.Quests, w.Behavior.Goals, p.PlaceImpressions);
        Assert.DoesNotContain(normal.SelectMany(o => o.Factors), f => f.Key == "interruption");
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void PausasEFlexibilidade_InvalidasSaoErro()
    {
        var set = TestSupport.FreshPresetSet();
        int i = set.Occupations.FindIndex(o => o.Id == "comercial");
        set.Occupations[i].PlayDuringWorkFlexibility = 500;   // sem bloco remoto
        set.Occupations[i].Blocks[0].Breaks.Add(new BreakDefinition { Start = "12:00", End = "13:00" });
        int h = set.Occupations.FindIndex(o => o.Id == "home_office");
        set.Occupations[h].Blocks[0].Breaks.Add(new BreakDefinition { Start = "19:00", End = "20:00" });   // fora do bloco
        var errors = ContentValidator.Validate(set).Where(x => x.Severity == IssueSeverity.Error).ToList();
        Assert.Contains(errors, x => x.Path == $"occupations[{i}].playDuringWorkFlexibility");
        Assert.Contains(errors, x => x.Path == $"occupations[{i}].blocks[0].breaks");
        Assert.Contains(errors, x => x.Path.StartsWith($"occupations[{h}].blocks[0].breaks[", StringComparison.Ordinal));
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void SonoETrabalho_SaoDeterministicos_EEntramNoHash()
    {
        string Hash()
        {
            var (e, _) = Run(12, 120, 8);
            return StateHasher.Compute(e.World);
        }
        Assert.Equal(Hash(), Hash());
    }
}

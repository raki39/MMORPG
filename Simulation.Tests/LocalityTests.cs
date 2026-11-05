using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

/// <summary>Localidade, fuso e calendario da residencia (P1, D-042).</summary>
public class LocalityTests
{
    private static SimulationEngine Opened(ulong seed = 42, int population = 300)
    {
        var e = TestSupport.NewEngine(seed, population);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        return e;
    }

    [Fact]
    public void Localidades_DoConteudo_ComServidorNoBrasil()
    {
        var c = TestSupport.Preset;
        Assert.Equal(new[] { "argentina", "brasil", "china", "eua" }, c.LocalitiesOrdered.Select(l => l.Id).ToArray());
        Assert.Equal("brasil", c.ServerLocality.Id);
        Assert.Equal(480, c.Localities["china"].UtcOffsetMinutes);
    }

    [Fact]
    public void Pessoas_TemResidenciaNacionalidadeEIdiomas_PelosPesos()
    {
        var c = TestSupport.Preset;
        var people = Enumerable.Range(1, 3000).Select(i => PopulationGenerator.Generate(i, c, 9)).ToList();
        double Share(string id) => people.Count(p => p.ResidenceId == id) * 1000.0 / people.Count;
        Assert.InRange(Share("brasil"), 720, 800);
        Assert.InRange(Share("china"), 50, 95);
        Assert.Contains(people, p => p.NationalityId != p.ResidenceId);   // estrangeiros existem
        Assert.True(people.Count(p => p.NationalityId != p.ResidenceId) < people.Count / 10);
        // O idioma nativo da nacionalidade sempre esta entre os idiomas.
        Assert.All(people, p => Assert.Contains(c.Localities[p.NationalityId].Languages[0].Id, p.Languages));
        Assert.All(people.Where(p => p.ResidenceId == "china"), p => Assert.Equal(660, p.LocalOffsetMinutes));
        // Nome da nacionalidade quando ela tem os seus (brasileiros usam os nomes gerais).
        var chinese = people.First(p => p.NationalityId == "china");
        Assert.Contains(chinese.Name.Split(' ')[0], c.Localities["china"].Names.FirstNames);
        var brazilian = people.First(p => p.NationalityId == "brasil");
        Assert.Contains(brazilian.Name.Split(' ')[0], c.Names.FirstNames);
        // Deterministico.
        var again = PopulationGenerator.Generate(77, c, 9);
        Assert.Equal(people[76].ResidenceId, again.ResidenceId);
        Assert.Equal(people[76].Languages, again.Languages);
        Assert.Equal(people[76].Name, again.Name);
    }

    [Fact]
    public void FeriadosDependemDaResidencia()
    {
        var e = Opened(population: 10);
        var w = e.World;
        var date = new DateTime(2027, 5, 25);   // Revolucao de Maio (Argentina)
        Assert.Equal(DayKind.Holiday, w.LocalityCalendars["argentina"].KindOf(date));
        Assert.Equal(DayKind.Weekday, w.LocalityCalendars["brasil"].KindOf(date));
        Assert.Equal(DayKind.Holiday, w.LocalityCalendars["brasil"].KindOf(new DateTime(2027, 4, 21)));   // Tiradentes
        Assert.Equal(DayKind.Weekday, w.LocalityCalendars["eua"].KindOf(new DateTime(2027, 4, 21)));
        // O servidor usa o calendario da localidade dele.
        Assert.Equal(DayKind.Holiday, w.Clock.Calendar.KindOf(new DateTime(2027, 4, 21)));
    }

    /// <summary>
    /// Fuso (P1): o chines segue a rotina do horario local, como o brasileiro segue o dele; no
    /// relogio do servidor isso aparece deslocado (a noite local cai na manha do servidor). Com o
    /// modelo de vontade da v3 (D-051) ha sessoes de tarde e voltas, entao compara-se a distribuicao
    /// local das duas localidades, nao uma faixa fixa.
    /// </summary>
    [Fact]
    [Trait("Classe", "Tendência")]
    public void Chineses_JogamNaNoiteLocal_QueEManhaNoServidor()
    {
        var e = Opened(seed: 3, population: 600);
        var local = new Dictionary<string, List<int>> { ["china"] = new(), ["brasil"] = new() };
        var server = new Dictionary<string, List<int>> { ["china"] = new(), ["brasil"] = new() };
        for (int d = 0; d < 14; d++)
        {
            e.AdvanceTo(SimTime.Days(d + 1));
            foreach (var ev in e.World.Events.Drain().Where(x => x.Type == "session_started"))
            {
                var p = e.World.GetPerson((int)ev.Entities[0].Id);
                if (!local.ContainsKey(p.ResidenceId) || ev.Tick < SimTime.Days(3)) continue;
                var at = e.World.Clock.Calendar.ToDateTime(ev.Tick);
                var atLocal = e.World.Clock.Calendar.ToDateTime(ev.Tick + p.LocalOffsetMinutes * SimTime.TicksPerMinute);
                if (p.ResidenceId == "china") Assert.Equal((at.Hour + 11) % 24, atLocal.Hour);
                server[p.ResidenceId].Add(at.Hour);
                local[p.ResidenceId].Add(atLocal.Hour);
            }
        }
        Assert.True(local["china"].Count > 20, $"amostra pequena: {local["china"].Count}");
        int Share(List<int> v, Func<int, bool> f) => v.Count(f) * 100 / Math.Max(1, v.Count);
        // No horario local, chineses e brasileiros jogam nos mesmos periodos (maioria a tarde e a noite).
        int chinaEvening = Share(local["china"], h => h >= 15), brazilEvening = Share(local["brasil"], h => h >= 15);
        Assert.True(chinaEvening >= 50, $"sessoes chinesas fora da tarde e noite local: {chinaEvening}%");
        Assert.InRange(chinaEvening, brazilEvening - 12, brazilEvening + 12);
        // No relogio do servidor, a noite chinesa cai entre a madrugada e o almoco, bem mais que a brasileira.
        int chinaMorning = Share(server["china"], h => h >= 3 && h < 14), brazilMorning = Share(server["brasil"], h => h >= 3 && h < 14);
        Assert.True(chinaMorning >= brazilMorning + 20, $"china {chinaMorning}% x brasil {brazilMorning}% na manha do servidor");
    }

    [Fact]
    public void PlanejamentoDiario_NaMeiaNoiteLocal_DeCadaLocalidade()
    {
        var e = Opened(population: 50);
        var w = e.World;
        var plans = w.Scheduler.SnapshotOrdered().Where(x => x.Type == BehaviorSystem.EvDailySessions).ToList();
        Assert.Equal(w.Content.LocalitiesOrdered.Count, plans.Count);
        var china = plans.Single(x => w.Content.LocalitiesOrdered[(int)x.TargetId].Id == "china");
        Assert.Equal(SimTime.TicksPerDay - SimTime.Hours(11), china.Tick);   // meia-noite em Pequim = 13h no servidor
        var eua = plans.Single(x => w.Content.LocalitiesOrdered[(int)x.TargetId].Id == "eua");
        Assert.Equal(SimTime.TicksPerDay + SimTime.Hours(2), eua.Tick);
    }

    [Fact]
    public void LocalidadesInvalidas_SaoErro()
    {
        var set = TestSupport.FreshPresetSet();
        set.World.ServerLocalityId = "lua";
        set.Localities[0].UtcOffsetMinutes = 2000;
        set.Localities[1].Languages[0].Permille = 500;
        var errors = ContentValidator.Validate(set).Where(i => i.Severity == IssueSeverity.Error).ToList();
        Assert.Contains(errors, x => x.Path == "serverLocalityId");
        Assert.Contains(errors, x => x.Path == "localities[0].utcOffsetMinutes");
        Assert.Contains(errors, x => x.Path == "localities[1].languages[0].permille");
    }

    [Fact]
    public void LocalidadeEntraNoHash_EEDeterministica()
    {
        string Hash()
        {
            var e = Opened(seed: 21, population: 80);
            e.AdvanceTo(SimTime.Days(6));
            return StateHasher.Compute(e.World);
        }
        Assert.Equal(Hash(), Hash());
    }

    [Fact]
    public void InicioMedio_EMediaCircular_ELocalEServidorDiferemPeloFuso()
    {
        // Duas sessoes: 23h30 e 0h30. A media aritmetica daria 12h; a circular, meia-noite.
        var (ax, ay) = Simulation.Infrastructure.Validation.ValidationCollector.ClockVector(23 * 60 + 30);
        var (bx, by) = Simulation.Infrastructure.Validation.ValidationCollector.ClockVector(30);
        double mid = Simulation.Infrastructure.Validation.ValidationCollector.CircularMeanHour(ax + bx, ay + by);
        Assert.True(mid < 0.01 || mid > 23.99, $"{mid}");
        // Na campanha: chineses (UTC+8) contra o servidor (UTC-3) = 11 h exatas.
        var e = Opened(seed: 3, population: 600);
        var collector = new Simulation.Infrastructure.Validation.ValidationCollector(e.World.Content, warmupDays: 2);
        for (int d = 0; d < 10; d++)
        {
            e.AdvanceTo(SimTime.Days(d + 1));
            collector.Consume(e.World.Events.Drain(), e.World);
        }
        var m = collector.Finish(e.World, 3, 10, Array.Empty<Simulation.Infrastructure.Validation.TargetDefinition>());
        var china = m.Localities.Single(l => l.Id == "china");
        double diff = (china.MeanLocalStartHour - china.MeanServerStartHour + 24) % 24;
        Assert.InRange(diff, 10.99, 11.01);
    }

    [Fact]
    public void HashSemIdentificacaoDoConteudo_IgnoraSoOHashDosArquivos()
    {
        string[] Run(string sourceHash)
        {
            var set = TestSupport.FreshPresetSet();
            set.SourceHash = sourceHash;
            var e = WorldFactory.Create(ContentDatabase.Create(set), new WorldConfig { Seed = 5, PopulationSize = 40 });
            e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
            e.AdvanceTo(SimTime.Days(3));
            return new[] { StateHasher.Compute(e.World), StateHasher.ComputeWithoutContentId(e.World) };
        }
        var a = Run("conteudo-a");
        var b = Run("conteudo-b");
        Assert.NotEqual(a[0], b[0]);   // o hash original continua identificando o conteudo
        Assert.Equal(a[1], b[1]);      // o estado completo normalizado e o mesmo
    }
}

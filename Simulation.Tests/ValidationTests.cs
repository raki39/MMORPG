using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Time;
using Simulation.Infrastructure.Validation;
using Simulation.Runner;

namespace Simulation.Tests;

/// <summary>Validacao estatistica (plano 17.1) e as medidas usadas no relatorio de validacao.</summary>
public class ValidationTests
{
    private readonly Xunit.Abstractions.ITestOutputHelper _out;
    public ValidationTests(Xunit.Abstractions.ITestOutputHelper output) => _out = output;

    private const int WindowFrom = 19 * 60, WindowTo = 23 * 60;

    /// <summary>
    /// Plano 19.4: um perfil "trabalhador" que prefere 19h-23h deve concentrar nessa faixa a
    /// proporcao esperada das sessoes em dias uteis. A proporcao esperada sai da configuracao
    /// do cenario (hora preferida 21 +- 2 e variacao de +-75 min), calculada exatamente abaixo;
    /// a tolerancia e de 4 erros-padrao contados por pessoa (a hora preferida e fixa por pessoa,
    /// entao sessoes da mesma pessoa nao sao independentes).
    /// </summary>
    [Fact]
    public void Trabalhador_19a23h_ConcentraAsSessoesDeDiaUtil()
    {
        var set = TestSupport.FreshPresetSet();
        var casual = set.Population.Profiles.First(p => p.Id == "casual");
        casual.Id = "trabalhador";
        casual.Name = "Trabalhador";
        casual.PreferredStartHour = new TraitRange { Mean = 21, Spread = 2 };
        casual.DaysPerWeek = new TraitRange { Mean = 5, Spread = 1 };
        set.Population.Profiles = new List<PopulationProfileDefinition> { casual };
        var content = ContentDatabase.Create(set);

        double expected = ExpectedShare(mean: 21, spread: 2, jitter: Simulation.Core.Behavior.BehaviorTuning.SessionStartJitterMinutes);
        int persons = 0, inWindow = 0, total = 0;
        var perSeed = new List<double>();
        foreach (ulong seed in new ulong[] { 1, 2, 3, 4, 5 })
        {
            var e = WorldFactory.Create(content, new WorldConfig { Seed = seed, PopulationSize = 200 });
            e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
            var collector = new ValidationCollector(content, warmupDays: 7);
            for (int d = 0; d < 28; d++)
            {
                e.AdvanceTo(SimTime.Days(d + 1));
                collector.Consume(e.World.Events.Drain(), e.World);
            }
            var starts = collector.WeekdaySessionStarts;
            int hit = starts.Count(s => s.Minute >= WindowFrom && s.Minute < WindowTo);
            perSeed.Add(hit / (double)starts.Count);
            inWindow += hit;
            total += starts.Count;
            persons += e.World.Characters.Count;
        }

        Assert.True(total > 2000, $"amostra pequena: {total} sessões");
        double observed = inWindow / (double)total;
        double tolerance = 4 * Math.Sqrt(expected * (1 - expected) / persons);
        _out.WriteLine($"esperado {expected:P2} · observado {observed:P2} · tolerância ±{tolerance:P2} · {persons} pessoas · {total} sessões · por seed: {string.Join(" ", perSeed.Select(x => x.ToString("P1")))}");
        Assert.True(Math.Abs(observed - expected) <= tolerance,
            $"observado {observed:P1} x esperado {expected:P1} (tolerância ±{tolerance:P1}, {persons} pessoas, {total} sessões)");
        // Cada seed isolada tambem fica perto (tolerancia maior: 1/5 das pessoas).
        foreach (var s in perSeed)
            Assert.True(Math.Abs(s - expected) <= tolerance * Math.Sqrt(5), $"seed fora: {s:P1} x {expected:P1}");
    }

    /// <summary>
    /// Proporcao exata de inicios em [19h, 23h) pela regra do calendario: hora preferida =
    /// media + (soma de 3 sorteios em [-spread, spread]) / 3 (divisao inteira), e inicio =
    /// hora*60 + sorteio uniforme em [-jitter, jitter], limitado ao dia.
    /// </summary>
    private static double ExpectedShare(int mean, int spread, int jitter)
    {
        double sum = 0;
        int combos = 0;
        for (int a = -spread; a <= spread; a++)
            for (int b = -spread; b <= spread; b++)
                for (int c = -spread; c <= spread; c++)
                {
                    int hour = Math.Clamp(mean + (a + b + c) / 3, 0, 23);
                    int hits = 0;
                    for (int j = -jitter; j <= jitter; j++)
                    {
                        int m = Math.Clamp(hour * 60 + j, 0, 24 * 60 - 1);
                        if (m >= WindowFrom && m < WindowTo) hits++;
                    }
                    sum += hits / (double)(2 * jitter + 1);
                    combos++;
                }
        return sum / combos;
    }

    [Fact]
    public void TempoJogado_FechaComAsSessoesEAsAtividades()
    {
        var e = TestSupport.NewEngine(7, 150);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        var played = new Dictionary<long, long>();
        var open = new Dictionary<long, long>();
        for (int d = 0; d < 21; d++)
        {
            e.AdvanceTo(SimTime.Days(d + 1));
            foreach (var ev in e.World.Events.Drain())
            {
                long pid = ev.Entities.Count > 0 ? ev.Entities[0].Id : 0;
                if (ev.Type == "session_started") open[pid] = ev.Tick;
                else if (ev.Type == "session_ended" && open.Remove(pid, out long s))
                    played[pid] = played.GetValueOrDefault(pid) + ev.Tick - s;
            }
        }
        var w = e.World;
        Assert.True(w.Characters.Count > 100);
        foreach (var c in w.Characters)
        {
            // Tempo conectado fechado = soma das sessoes encerradas (eventos).
            Assert.Equal(played.GetValueOrDefault(c.PersonId), c.PlayedTicks);
            // O tempo em atividades nunca passa do tempo conectado; o resto e deslocamento.
            Assert.True(c.ActivityTicksByType.Values.Sum() <= c.PlayedTicksAt(w.Clock.Tick), c.Name);
            // Um registro por nivel ganho, em ordem crescente de tempo jogado.
            Assert.Equal(c.Level - 1, c.PlayedTicksAtLevel.Count);
            for (int i = 1; i < c.PlayedTicksAtLevel.Count; i++)
                Assert.True(c.PlayedTicksAtLevel[i] >= c.PlayedTicksAtLevel[i - 1]);
        }
        Assert.Contains(w.Characters, c => c.ActivityTicksByType.ContainsKey("hunt"));
    }

    [Fact]
    public void Metas_ComErro_SaoRejeitadasComCaminho()
    {
        var file = new TargetsFile
        {
            SchemaVersion = 1,
            Targets =
            {
                new TargetDefinition { Id = "a", Name = "A", Metric = "inventada", Min = 0, Max = 1 },
                new TargetDefinition { Id = "a", Name = "B", Metric = TargetMetrics.HoursToLevel, Min = 5, Max = 1 },
                new TargetDefinition { Id = "c", Name = "C", Metric = TargetMetrics.ActivityShare, Types = { "voar" }, Profile = "ninguem" },
            },
        };
        var errors = TargetsLoader.Validate(file, TestSupport.Preset);
        Assert.Contains(errors, x => x.Contains("targets[0].metric"));
        Assert.Contains(errors, x => x.Contains("targets[1].id") && x.Contains("repetido"));
        Assert.Contains(errors, x => x.Contains("targets[1].level"));
        Assert.Contains(errors, x => x.Contains("targets[1]") && x.Contains("maior que max"));
        Assert.Contains(errors, x => x.Contains("targets[2].types") && x.Contains("voar"));
        Assert.Contains(errors, x => x.Contains("targets[2].profile"));
    }

    [Fact]
    public void MetasDoPreset_SaoValidas()
    {
        var r = TargetsLoader.Load(Path.Combine(TestSupport.PresetFolder, TargetsLoader.FileName), TestSupport.Preset);
        Assert.True(r.Success, string.Join("\n", r.Errors));
        Assert.NotEmpty(r.File.Targets);
    }

    [Fact]
    public void ValidacaoComVariasSeeds_EhReproduzivel()
    {
        RunResult Run(string dir) => ScenarioRunner.Run(new RunOptions
        {
            WorldFolder = TestSupport.PresetFolder,
            Days = 10,
            PopulationOverride = 80,
            Seeds = new List<ulong> { 1, 2, 3 },
            OutDir = dir,
        });
        string a = TestSupport.NewTempDir(), b = TestSupport.NewTempDir();
        Assert.Equal(ScenarioRunner.ExitOk, Run(a).ExitCode);
        Assert.Equal(ScenarioRunner.ExitOk, Run(b).ExitCode);
        string md = File.ReadAllText(Path.Combine(a, "validation.md"));
        Assert.Contains("## Metas", md);
        Assert.Contains("Atividades pouco usadas", md);
        // Mesmas seeds -> mesmos valores, mesmo rodando em paralelo.
        Assert.Equal(File.ReadAllText(Path.Combine(a, "validation.csv")), File.ReadAllText(Path.Combine(b, "validation.csv")));
    }

    [Fact]
    public void Diagnostico_ExplicaAtividadeBloqueadaPorGrupo()
    {
        var covil = TestSupport.Preset.Regions["covil_do_rei_goblin"];
        var boss = covil.AllActivities().First(a => a.Type == ActivityTypes.Boss);
        string text = ValidationReport.Diagnose(covil, boss, new List<int> { 5, 9, 16 });
        Assert.Contains("grupo de 5", text);
        Assert.Contains("M6", text);
    }
}

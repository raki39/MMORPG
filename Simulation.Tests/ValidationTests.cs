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
    /// Regra do cenario (P2, D-043): trabalhador em horario comercial joga depois do expediente
    /// e antes de dormir. Com hora preferida 21 +- 2, os inicios de dia util caem entre 18h e
    /// 23h (hora local) e nenhum cai durante o trabalho.
    /// </summary>
    [Fact]
    public void TrabalhadorComercial_JogaDepoisDoExpediente_EAntesDeDormir()
    {
        var set = TestSupport.FreshPresetSet();
        var casual = set.Population.Profiles.First(p => p.Id == "casual");
        casual.Id = "trabalhador";
        casual.Name = "Trabalhador";
        casual.PreferredStartHour = new TraitRange { Mean = 21, Spread = 2 };
        casual.GameImportance = new TraitRange { Mean = 800, Spread = 100 };
        set.Population.Profiles = new List<PopulationProfileDefinition> { casual };
        set.Occupations = set.Occupations.Where(o => o.Id == "comercial").ToList();
        var content = ContentDatabase.Create(set);

        int inWindow = 0, total = 0;
        foreach (ulong seed in new ulong[] { 1, 2, 3 })
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
            inWindow += starts.Count(s => s.Minute >= 18 * 60 && s.Minute < 23 * 60);
            total += starts.Count;
            Assert.DoesNotContain(starts, s => s.Minute >= 8 * 60 + 60 && s.Minute < 17 * 60 - 60);   // nunca no meio do expediente
        }
        Assert.True(total > 1500, $"amostra pequena: {total} sessões");
        _out.WriteLine($"inícios de dia útil entre 18h e 23h: {inWindow * 100.0 / total:0.0}% de {total}");
        Assert.True(inWindow * 100 / total >= 90, $"{inWindow}/{total} entre 18h e 23h");
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
        Assert.Contains(errors, x => x.Contains("targets[0].kind"));   // classe obrigatoria (D-039a)
    }

    [Fact]
    public void MetasDoPreset_TemClasse_EARotinaERegraDoCenario()
    {
        var r = TargetsLoader.Load(Path.Combine(TestSupport.PresetFolder, TargetsLoader.FileName), TestSupport.Preset);
        Assert.All(r.File.Targets, t => Assert.Contains(t.Kind, TargetKinds.All));
        Assert.All(r.File.Targets.Where(t => t.Metric is TargetMetrics.SessionsPerWeek or TargetMetrics.HoursPerWeek),
            t => Assert.Equal(TargetKinds.Scenario, t.Kind));
        Assert.All(r.File.Targets.Where(t => t.Metric is TargetMetrics.QuitShare or TargetMetrics.InactiveShare or TargetMetrics.ReturnShare),
            t => Assert.Equal(TargetKinds.Future, t.Kind));
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

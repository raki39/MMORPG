using Simulation.Runner;

namespace Simulation.Tests;

public class RunnerTests
{
    private static RunOptions Options(string outDir, int days = 3) => new()
    {
        WorldFolder = TestSupport.PresetFolder,
        Days = days,
        Seed = 42,
        OutDir = outDir,
    };

    [Fact]
    public void Cenario_GeraTodosOsRelatorios()
    {
        var dir = TestSupport.NewTempDir();
        var result = ScenarioRunner.Run(Options(dir));
        Assert.Equal(ScenarioRunner.ExitOk, result.ExitCode);
        foreach (var f in new[] { "daily.csv", "events.csv", "commands.csv", "population.csv", "summary.md", "metadata.json" })
            Assert.True(File.Exists(Path.Combine(dir, f)), f);

        var daily = File.ReadAllLines(Path.Combine(dir, "daily.csv"));
        Assert.Equal(4, daily.Length); // cabecalho + 3 dias
        Assert.EndsWith(result.FinalStateHash, daily[^1]);

        var commands = File.ReadAllLines(Path.Combine(dir, "commands.csv"));
        Assert.Contains(commands, l => l.Contains(",applied,"));
        Assert.Contains(commands, l => l.Contains(",rejected,"));

        var summary = File.ReadAllText(Path.Combine(dir, "summary.md"));
        Assert.Contains(result.FinalStateHash, summary);
        Assert.Contains("Métricas indisponíveis", summary);
        Assert.Equal(101, File.ReadAllLines(Path.Combine(dir, "population.csv")).Length);
    }

    [Fact]
    public void MesmaExecucao_ProduzMesmosDados()
    {
        var a = TestSupport.NewTempDir();
        var b = TestSupport.NewTempDir();
        var ra = ScenarioRunner.Run(Options(a));
        var rb = ScenarioRunner.Run(Options(b));
        Assert.Equal(ra.FinalStateHash, rb.FinalStateHash);
        foreach (var f in new[] { "daily.csv", "events.csv", "commands.csv", "population.csv" })
            Assert.Equal(File.ReadAllText(Path.Combine(a, f)), File.ReadAllText(Path.Combine(b, f)));
    }

    [Fact]
    public void ModoTickATick_ProduzMesmoHash()
    {
        var fast = ScenarioRunner.Run(Options(TestSupport.NewTempDir(), days: 1));
        var opt = Options(TestSupport.NewTempDir(), days: 1);
        opt.DisableTickSkipping = true;
        var slow = ScenarioRunner.Run(opt);
        Assert.Equal(fast.FinalStateHash, slow.FinalStateHash);
    }

    [Fact]
    public void ConteudoInvalido_RetornaCodigo2()
    {
        var dir = TestSupport.NewTempDir();
        TestSupport.CopyDirectory(TestSupport.PresetFolder, dir);
        File.WriteAllText(Path.Combine(dir, "world.json"), "{ \"schemaVersion\": 1, \"world\": { \"id\": \"x\" ");
        var result = ScenarioRunner.Run(new RunOptions { WorldFolder = dir, Days = 1 });
        Assert.Equal(ScenarioRunner.ExitInvalidContent, result.ExitCode);
        Assert.Contains(result.Messages, m => m.Contains("world.json"));
    }

    [Fact]
    public void CenarioInvalido_RetornaCodigo3()
    {
        var dir = TestSupport.NewTempDir();
        TestSupport.CopyDirectory(TestSupport.PresetFolder, dir);
        File.WriteAllText(Path.Combine(dir, "scenario.json"),
            "{ \"schemaVersion\": 1, \"populationSize\": 5, \"commands\": [ { \"at\": \"amanhã\", \"type\": \"set_server_open\", \"args\": { \"open\": true } } ] }");
        var result = ScenarioRunner.Run(new RunOptions { WorldFolder = dir, Days = 1 });
        Assert.Equal(ScenarioRunner.ExitInvalidScenario, result.ExitCode);
        Assert.Contains(result.Messages, m => m.Contains("commands[0].at"));
    }
}

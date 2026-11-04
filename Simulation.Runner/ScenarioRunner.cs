using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Simulation.Core.Engine;
using Simulation.Core.Events;
using Simulation.Core.Random;
using Simulation.Core.Time;
using Simulation.Core.World;
using Simulation.Infrastructure.Content;
using Simulation.Infrastructure.Reports;
using Simulation.Infrastructure.Scenarios;

namespace Simulation.Runner;

public sealed class RunOptions
{
    public string WorldFolder { get; set; } = "Content/presets/inicial";
    public int Days { get; set; } = 30;
    public ulong Seed { get; set; } = 42;
    public string? OutDir { get; set; }
    public int? PopulationOverride { get; set; }
    /// <summary>Caminho do cenario; nulo = scenario.json da pasta do mundo, se existir.</summary>
    public string? ScenarioPath { get; set; }
    public bool NoScenario { get; set; }
    public bool DisableTickSkipping { get; set; }
}

public sealed class RunResult
{
    public int ExitCode { get; init; }
    public List<string> Messages { get; init; } = new();
    public RunMetadata? Metadata { get; init; }
    public string FinalStateHash => Metadata?.FinalStateHash ?? "";
}

/// <summary>Executa um cenario headless completo e escreve os relatorios.</summary>
public static class ScenarioRunner
{
    public const int ExitOk = 0;
    public const int ExitUsage = 1;
    public const int ExitInvalidContent = 2;
    public const int ExitInvalidScenario = 3;

    public static RunResult Run(RunOptions opt)
    {
        var msgs = new List<string>();
        var wall = Stopwatch.StartNew();

        if (opt.Days < 1) return Fail(ExitUsage, msgs, "--days deve ser >= 1.");

        var content = ContentLoader.LoadFolder(opt.WorldFolder);
        foreach (var w in content.Warnings) msgs.Add(w.ToString());
        if (!content.Success)
        {
            foreach (var e in content.Errors) msgs.Add(e.ToString());
            return Fail(ExitInvalidContent, msgs, $"Conteúdo inválido em '{content.Folder}'.");
        }

        ScenarioLoadResult? scenario = null;
        string scenarioPath = opt.ScenarioPath ?? Path.Combine(content.Folder, ScenarioLoader.FileName);
        if (!opt.NoScenario && (opt.ScenarioPath != null || File.Exists(scenarioPath)))
        {
            scenario = ScenarioLoader.Load(scenarioPath);
            if (!scenario.Success)
            {
                msgs.AddRange(scenario.Errors);
                return Fail(ExitInvalidScenario, msgs, "Cenário inválido.");
            }
        }

        int population = opt.PopulationOverride ?? scenario?.Scenario.PopulationSize ?? 100;
        var engine = WorldFactory.Create(content.Database!, new WorldConfig { Seed = opt.Seed, PopulationSize = population });
        var profiler = new StopwatchProfiler();
        engine.Profiler = profiler;
        engine.DisableTickSkipping = opt.DisableTickSkipping;

        if (scenario != null)
        {
            long lastTick = SimTime.Days(opt.Days);
            foreach (var c in scenario.Commands.OrderBy(c => c.Tick))
            {
                if (c.Tick > lastTick)
                {
                    msgs.Add($"Aviso: comando '{c.Command.Type}' no tick {c.Tick} fica após o fim da execução e não será aplicado.");
                    continue;
                }
                engine.Submit(c.Command, c.Tick);
            }
        }

        var world = engine.World;
        var events = new List<SimEvent>();
        var hashes = new List<DailyHash>();
        var simWatch = new Stopwatch();
        var hashWatch = new Stopwatch();

        for (int day = 0; day < opt.Days; day++)
        {
            simWatch.Start();
            engine.AdvanceTo(SimTime.Days(day + 1));
            simWatch.Stop();

            // Fora do tick: drenar eventos e calcular hash do dia.
            events.AddRange(world.Events.Drain());
            hashWatch.Start();
            hashes.Add(new DailyHash(day, StateHasher.Compute(world)));
            hashWatch.Stop();
        }

        var meta = new RunMetadata
        {
            SimulatorVersion = SimulatorVersion(),
            RngAlgorithm = DeterministicRng.AlgorithmId,
            StateHashFormat = StateHasher.FormatVersion,
            TickMilliseconds = SimTime.TickMilliseconds,
            WorldId = world.Content.World.Id,
            WorldVersion = world.Content.World.Version,
            WorldName = world.Content.World.Name,
            ContentFolder = ToRepoRelative(content.Folder),
            ContentHash = content.ContentHash,
            ScenarioFile = scenario != null ? ToRepoRelative(Path.GetFullPath(scenarioPath)) : "",
            ScenarioHash = scenario?.ScenarioHash ?? "",
            Seed = opt.Seed,
            PopulationSize = population,
            Days = opt.Days,
            StartDate = world.Content.World.StartDate,
            TickSkipping = !opt.DisableTickSkipping,
            TicksSimulated = world.Clock.Tick,
            TicksExecuted = engine.TicksExecuted,
            SimulationTimeMs = simWatch.Elapsed.TotalMilliseconds,
            HashTimeMs = hashWatch.Elapsed.TotalMilliseconds,
            SimulatedDaysPerSecond = simWatch.Elapsed.TotalSeconds > 0 ? opt.Days / simWatch.Elapsed.TotalSeconds : 0,
            FinalStateHash = hashes[^1].Hash,
            Runtime = RuntimeInformation.FrameworkDescription,
            OperatingSystem = RuntimeInformation.OSDescription,
            ProcessorCount = Environment.ProcessorCount,
            GeneratedAtUtc = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
            SystemTimings = profiler.Snapshot(),
        };

        if (opt.OutDir != null)
        {
            var reportWatch = Stopwatch.StartNew();
            meta.WallTimeMs = wall.Elapsed.TotalMilliseconds;
            RunReportWriter.WriteAll(opt.OutDir, meta, world, events, hashes);
            meta.ReportTimeMs = reportWatch.Elapsed.TotalMilliseconds;
            meta.WallTimeMs = wall.Elapsed.TotalMilliseconds;
            // Reescreve com os tempos finais (escrita e rapida e idempotente).
            RunReportWriter.WriteAll(opt.OutDir, meta, world, events, hashes);
        }
        else
        {
            meta.WallTimeMs = wall.Elapsed.TotalMilliseconds;
        }

        msgs.Add($"OK: {opt.Days} dias, {population} pessoas, seed {opt.Seed}. Hash final {meta.FinalStateHash[..16]}. " +
                 $"Simulação {meta.SimulationTimeMs:F1} ms; total {meta.WallTimeMs:F1} ms." +
                 (opt.OutDir != null ? $" Relatórios em {Path.GetFullPath(opt.OutDir)}" : ""));
        return new RunResult { ExitCode = ExitOk, Messages = msgs, Metadata = meta };
    }

    private static RunResult Fail(int code, List<string> msgs, string message)
    {
        msgs.Add("ERRO: " + message);
        return new RunResult { ExitCode = code, Messages = msgs };
    }

    public static string SimulatorVersion()
    {
        var asm = typeof(SimulationEngine).Assembly;
        var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        return info ?? asm.GetName().Version?.ToString() ?? "desconhecida";
    }

    private static string ToRepoRelative(string fullPath)
    {
        var rel = Path.GetRelativePath(Directory.GetCurrentDirectory(), fullPath);
        return rel.Replace('\\', '/');
    }
}

/// <summary>Perfilador do host: mede tempo real por sistema (apenas diagnostico).</summary>
public sealed class StopwatchProfiler : ISimulationProfiler
{
    private readonly Dictionary<string, (long Calls, long Ticks)> _totals = new(StringComparer.Ordinal);

    public IDisposable Measure(string system) => new Scope(this, system, Stopwatch.GetTimestamp());

    private void Add(string system, long elapsed)
    {
        _totals.TryGetValue(system, out var t);
        _totals[system] = (t.Calls + 1, t.Ticks + elapsed);
    }

    public List<SystemTiming> Snapshot() => _totals
        .OrderBy(k => k.Key, StringComparer.Ordinal)
        .Select(k => new SystemTiming
        {
            System = k.Key,
            Calls = k.Value.Calls,
            TotalMs = k.Value.Ticks * 1000.0 / Stopwatch.Frequency,
        })
        .ToList();

    private sealed class Scope : IDisposable
    {
        private readonly StopwatchProfiler _owner;
        private readonly string _system;
        private readonly long _start;

        public Scope(StopwatchProfiler owner, string system, long start)
        {
            _owner = owner;
            _system = system;
            _start = start;
        }

        public void Dispose() => _owner.Add(_system, Stopwatch.GetTimestamp() - _start);
    }
}

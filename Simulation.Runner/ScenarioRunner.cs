using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Events;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Time;
using Simulation.Core.World;
using Simulation.Infrastructure.Content;
using Simulation.Infrastructure.Reports;
using Simulation.Infrastructure.Scenarios;
using Simulation.Infrastructure.Validation;

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
    /// <summary>Varias seeds (modo validacao); nulo = so Seed.</summary>
    public List<ulong>? Seeds { get; set; }
    /// <summary>Arquivo de metas; nulo = targets.json da pasta do mundo, se existir.</summary>
    public string? TargetsPath { get; set; }
    public bool NoTargets { get; set; }
    /// <summary>Cenario controlado (R4): celulas perfil x ocupacao; nulo = populacao normal.</summary>
    public List<Simulation.Core.Population.ControlledCell>? Controlled { get; set; }
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

    /// <summary>Previa nominal da curva de progressao (D-027); com --out grava progression.md.</summary>
    public static int PrintProgression(RunOptions opt)
    {
        var content = ContentLoader.LoadFolder(opt.WorldFolder);
        if (!content.Success)
        {
            foreach (var e in content.Errors) Console.Error.WriteLine(e);
            return ExitInvalidContent;
        }
        string md = ProgressionPreview.ToMarkdown(content.Database!);
        Console.WriteLine(md);
        if (!string.IsNullOrEmpty(opt.OutDir))
        {
            Directory.CreateDirectory(opt.OutDir);
            File.WriteAllText(Path.Combine(opt.OutDir, "progression.md"), md, new System.Text.UTF8Encoding(false));
        }
        return ExitOk;
    }

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

        var targets = LoadTargets(opt, content, msgs, out bool targetsFailed);
        if (targetsFailed) return Fail(ExitInvalidScenario, msgs, "Metas inválidas.");

        int population = opt.PopulationOverride ?? scenario?.Scenario.PopulationSize ?? 100;
        if (opt.Controlled is { Count: > 0 })
            return RunControlled(opt, content, scenario, opt.PopulationOverride ?? 100, msgs, wall);
        if (opt.Seeds is { Count: > 0 })
            return RunBatch(opt, content, scenario, targets, population, msgs, wall);

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
        var collector = targets != null ? new ValidationCollector(content.Database!, targets.File.WarmupDays) : null;
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
            int before = events.Count;
            events.AddRange(world.Events.Drain());
            collector?.Consume(events.Skip(before), world);
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
            FinalStateHashWithoutContentId = StateHasher.ComputeWithoutContentId(world),
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
            if (collector != null)
            {
                var metrics = collector.Finish(world, opt.Seed, opt.Days, targets!.File.Targets);
                metrics.FinalStateHash = meta.FinalStateHash;
                metrics.SimulationSeconds = simWatch.Elapsed.TotalSeconds;
                ValidationReport.Write(opt.OutDir, Context(opt, content, targets, population, wall), targets.File, content.Database!, new[] { metrics });
            }
            meta.ReportTimeMs = reportWatch.Elapsed.TotalMilliseconds;
            meta.WallTimeMs = wall.Elapsed.TotalMilliseconds;
            // Reescreve com os tempos finais (escrita e rapida e idempotente).
            RunReportWriter.WriteAll(opt.OutDir, meta, world, events, hashes);
        }
        else
        {
            meta.WallTimeMs = wall.Elapsed.TotalMilliseconds;
        }

        msgs.Add($"OK: {opt.Days} dias, {population} pessoas, seed {opt.Seed}. Hash final {meta.FinalStateHash[..16]} (sem a identificação do conteúdo: {meta.FinalStateHashWithoutContentId[..16]}). " +
                 $"Simulação {meta.SimulationTimeMs:F1} ms; total {meta.WallTimeMs:F1} ms." +
                 (opt.OutDir != null ? $" Relatórios em {Path.GetFullPath(opt.OutDir)}" : ""));
        return new RunResult { ExitCode = ExitOk, Messages = msgs, Metadata = meta };
    }

    private static TargetsLoadResult? LoadTargets(RunOptions opt, ContentLoadResult content, List<string> msgs, out bool failed)
    {
        failed = false;
        if (opt.NoTargets) return null;
        string path = opt.TargetsPath ?? Path.Combine(content.Folder, TargetsLoader.FileName);
        if (opt.TargetsPath == null && !File.Exists(path)) return null;
        var t = TargetsLoader.Load(path, content.Database!);
        if (!t.Success)
        {
            msgs.AddRange(t.Errors);
            failed = true;
            return null;
        }
        return t;
    }

    private static ValidationContext Context(RunOptions opt, ContentLoadResult content, TargetsLoadResult targets, int population, Stopwatch wall) => new()
    {
        WorldFolder = ToRepoRelative(content.Folder),
        ContentHash = content.ContentHash,
        TargetsFile = ToRepoRelative(Path.GetFullPath(targets.Path)),
        TargetsHash = targets.Hash,
        SimulatorVersion = SimulatorVersion(),
        Population = population,
        Days = opt.Days,
        WallSeconds = wall.Elapsed.TotalSeconds,
    };

    /// <summary>
    /// Modo validacao: roda varias seeds em paralelo (cada uma com seu proprio motor e
    /// conteudo carregado), coleta medidas sem guardar eventos e escreve validation.md/.csv.
    /// </summary>
    private static RunResult RunBatch(RunOptions opt, ContentLoadResult content, ScenarioLoadResult? scenario,
        TargetsLoadResult? targets, int population, List<string> msgs, Stopwatch wall)
    {
        if (targets == null) return Fail(ExitUsage, msgs, "--seeds precisa de metas (targets.json na pasta do mundo ou --targets).");
        if (opt.OutDir == null) return Fail(ExitUsage, msgs, "--seeds precisa de --out.");

        var seeds = opt.Seeds!;
        var results = new RunMetrics[seeds.Count];
        // Regras do cenario (rotina) sao medidas numa rodada controlada com o humor neutralizado
        // (D-039a); as demais metas e secoes vem da campanha completa.
        var scenarioTargets = targets.File.Targets.Where(t => t.Kind == TargetKinds.Scenario).ToList();
        var jobs = Enumerable.Range(0, seeds.Count).Select(i => (Index: i, Neutral: false))
            .Concat(scenarioTargets.Count > 0 ? Enumerable.Range(0, seeds.Count).Select(i => (Index: i, Neutral: true)) : Enumerable.Empty<(int, bool)>())
            .ToList();
        var controlled = new RunMetrics?[seeds.Count];
        var people = new string[seeds.Count];
        Parallel.For(0, jobs.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) }, j =>
        {
            var (i, neutral) = jobs[j];
            ulong seed = seeds[i];
            var db = ContentLoader.LoadFolder(opt.WorldFolder).Database!;
            var engine = WorldFactory.Create(db, new WorldConfig { Seed = seed, PopulationSize = population, NeutralMood = neutral });
            engine.DisableTickSkipping = opt.DisableTickSkipping;
            if (scenario != null)
                foreach (var c in scenario.Commands.OrderBy(c => c.Tick).Where(c => c.Tick <= SimTime.Days(opt.Days)))
                    engine.Submit(c.Command, c.Tick);
            var collector = new ValidationCollector(db, targets.File.WarmupDays);
            var tracker = neutral ? null : new ProgressionTracker(db);
            var watch = Stopwatch.StartNew();
            for (int day = 0; day < opt.Days; day++)
            {
                engine.AdvanceTo(SimTime.Days(day + 1));
                var evs = engine.World.Events.Drain();
                tracker?.Consume(evs, engine.World);
                collector.Consume(evs, engine.World);
            }
            watch.Stop();
            var m = collector.Finish(engine.World, seed, opt.Days, neutral ? scenarioTargets : targets.File.Targets);
            m.FinalStateHash = StateHasher.Compute(engine.World);
            m.SimulationSeconds = watch.Elapsed.TotalSeconds;
            if (neutral) controlled[i] = m; else { results[i] = m; people[i] = PeopleRows(engine.World, collector, tracker!, seed, opt.Days, targets.File.WarmupDays); }
        });
        WritePeople(opt.OutDir, people);
        for (int i = 0; i < seeds.Count; i++)
        {
            if (controlled[i] is not { } c) continue;
            foreach (var t in scenarioTargets)
            {
                results[i].Values[t.Id] = c.Values.GetValueOrDefault(t.Id, double.NaN);
                results[i].Notes.Remove(t.Id);
                if (c.Notes.TryGetValue(t.Id, out var note)) results[i].Notes[t.Id] = note;
                results[i].ControlledProfiles.Clear();
                results[i].ControlledProfiles.AddRange(c.Profiles);
            }
        }

        var ctx = Context(opt, content, targets, population, wall);
        ValidationReport.Write(opt.OutDir, ctx, targets.File, content.Database!, results);
        var outcomes = ValidationReport.Outcomes(targets.File, results);
        string Cls(string kind) => $"{outcomes.Count(o => o.Target.Kind == kind && o.Status == TargetStatus.Inside)}/{outcomes.Count(o => o.Target.Kind == kind)}";
        msgs.Add($"OK: {results.Length} seeds × {opt.Days} dias × {population} pessoas em {wall.Elapsed.TotalSeconds:F1} s. " +
                 $"Metas dentro da faixa: regras do cenário {Cls(TargetKinds.Scenario)}, provisórias {Cls(TargetKinds.Provisional)}, dependentes de sistemas futuros {Cls(TargetKinds.Future)} (acompanhamento). " +
                 $"Relatório em {Path.GetFullPath(Path.Combine(opt.OutDir, "validation.md"))}");
        return new RunResult { ExitCode = ExitOk, Messages = msgs };
    }

    /// <summary>
    /// Cenario controlado (R4): em cada seed, "perCell" pessoas iguais em cada celula perfil x
    /// ocupacao, com o humor neutro (sem extensoes, inatividade nem desistencia). Mede so as
    /// regras de rotina e de vontade; escreve controlled.md e controlled.csv.
    /// </summary>
    private static RunResult RunControlled(RunOptions opt, ContentLoadResult content, ScenarioLoadResult? scenario, int perCell, List<string> msgs, Stopwatch wall)
    {
        if (opt.OutDir == null) return Fail(ExitUsage, msgs, "--controlled precisa de --out.");
        var db0 = content.Database!;
        foreach (var c in opt.Controlled!)
        {
            if (!db0.Profiles.ContainsKey(c.Profile)) return Fail(ExitUsage, msgs, $"Perfil desconhecido: {c.Profile}");
            if (!db0.Occupations.ContainsKey(c.Occupation)) return Fail(ExitUsage, msgs, $"Ocupação desconhecida: {c.Occupation}");
        }
        var seeds = opt.Seeds is { Count: > 0 } ? opt.Seeds : new List<ulong> { opt.Seed };
        var runs = new DistributionData[seeds.Count];
        int population = perCell * opt.Controlled!.Count;
        Parallel.For(0, seeds.Count, new ParallelOptions { MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount / 2) }, i =>
        {
            var db = ContentLoader.LoadFolder(opt.WorldFolder).Database!;
            var engine = WorldFactory.Create(db, new WorldConfig { Seed = seeds[i], PopulationSize = population, NeutralMood = true, ControlledCells = opt.Controlled });
            if (scenario != null)
                foreach (var c in scenario.Commands.OrderBy(c => c.Tick).Where(c => c.Tick <= SimTime.Days(opt.Days)))
                    engine.Submit(c.Command, c.Tick);
            var collector = new ValidationCollector(db, 7);
            for (int day = 0; day < opt.Days; day++)
            {
                engine.AdvanceTo(SimTime.Days(day + 1));
                collector.Consume(engine.World.Events.Drain(), engine.World);
            }
            runs[i] = collector.Finish(engine.World, seeds[i], opt.Days, Array.Empty<TargetDefinition>()).Distribution;
        });
        ControlledReport.Write(opt.OutDir, opt.Controlled!, seeds, opt.Days, perCell, SimulatorVersion(), runs);
        msgs.Add($"OK: cenário controlado, {opt.Controlled!.Count} células × {perCell} pessoas × {seeds.Count} seeds × {opt.Days} dias em {wall.Elapsed.TotalSeconds:F1} s. " +
                 $"Relatório em {Path.GetFullPath(Path.Combine(opt.OutDir, "controlled.md"))}");
        return new RunResult { ExitCode = ExitOk, Messages = msgs };
    }

    private static readonly string[] PeopleActivityTypes =
    {
        ActivityTypes.Hunt, ActivityTypes.Boss, ActivityTypes.Quest, ActivityTypes.Explore, ActivityTypes.Gather, ActivityTypes.Trade,
        ActivityTypes.Socialize, ActivityTypes.Train, ActivityTypes.Repair, ActivityTypes.Patrol, ActivityTypes.Ambush,
    };
    private static readonly string[] PeopleTraitKeys =
        { "pve", "pvp", "trade", "exploration", "socializing", "sociability", "aggressiveness", "variety", "competitiveness" };

    private static readonly string PeopleHeader =
        "seed,person_id,profile,occupation,state,registered,game_importance,session_minutes,habit,expected_h_week,played_h_week,free_h_week,plan_days,class,level,"
        + string.Join(",", PeopleTraitKeys.Select(k => "t_" + k)) + ","
        + string.Join(",", PersonPreferences.Keys.Select(k => "pref_" + k)) + ","
        + string.Join(",", PeopleActivityTypes.Select(k => "min_" + k)) + ","
        + "target_level,h_to_target," + string.Join(",", PeopleActivityTypes.Select(k => "pre_min_" + k)) + ","
        + "unlock_set,unlock_done,discover_set,discover_done,discover_set_non_exploration,personality,personality_initial,personality_changes,min_idle,min_played,traits\n";

    /// <summary>
    /// Uma linha por pessoa da campanha (people.csv): atributos de dedicacao, horas estimadas na
    /// criacao, horas jogadas por semana na janela de medicao (mesma conta da validacao; zero para
    /// quem nao jogou) e tempo livre por semana na agenda guardada.
    /// </summary>
    private static string PeopleRows(WorldState w, ValidationCollector collector, ProgressionTracker tracker, ulong seed, int days, int warmupDays)
    {
        double weeks = Math.Max(0, days - warmupDays) / 7.0;
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var sb = new System.Text.StringBuilder();
        var chars = w.Characters.ToDictionary(c => c.Id);
        foreach (var p in w.Persons.OrderBy(x => x.Id))
        {
            chars.TryGetValue(p.CharacterId, out var ch);
            var plans = p.DayPlans.GroupBy(r => r.LocalDay).Select(g => g.Last()).ToList();
            double free = plans.Count == 0 ? double.NaN : plans.Sum(r => (double)r.FreeMinutes) / plans.Count * 7 / 60;
            double played = weeks > 0 ? collector.PlayedTicksInWindow(p.Id) / (double)SimTime.TicksPerHour / weeks : double.NaN;
            sb.Append(string.Join(",", new[]
            {
                seed.ToString(inv), p.Id.ToString(inv), p.ProfileId, p.OccupationId, p.State.ToString(), p.CharacterId != 0 ? "1" : "0",
                p.Traits.GameImportance.ToString(inv), p.Traits.SessionMinutes.ToString(inv), p.Traits.Habit.ToString(inv),
                (p.ExpectedWeeklyMinutes / 60.0).ToString("0.00", inv), played.ToString("0.00", inv), free.ToString("0.00", inv), plans.Count.ToString(inv),
                ch?.ClassId ?? "", (ch?.Level ?? 0).ToString(inv),
            }
            .Concat(PeopleTraitKeys.Select(k => p.Traits.Get(k).ToString(inv)))
            .Concat(PersonPreferences.Keys.Select(k => p.Preferences.Get(k).ToString(inv)))
            .Concat(PeopleActivityTypes.Select(k => ch == null ? "0" : (ch.ActivityTicksByType.GetValueOrDefault(k) / SimTime.TicksPerMinute).ToString(inv)))
            .Concat(tracker.Columns(ch, p.CharacterId, PeopleActivityTypes))
            .Append(p.PersonalityId)
            .Append(p.PersonalityHistory.Count > 0 ? p.PersonalityHistory[0].From : p.PersonalityId)
            .Append(p.PersonalityHistory.Count.ToString(inv))
            // Deslocamento = conectado - atividades - espera (idle), como na meta "andando ou esperando".
            .Append(ch == null ? "0" : (ch.ActivityTicksByType.GetValueOrDefault("idle") / SimTime.TicksPerMinute).ToString(inv))
            .Append(ch == null ? "0" : (ch.PlayedTicks / SimTime.TicksPerMinute).ToString(inv))
            .Append(string.Join("|", p.Quirks.Select(q => q.Id)))));
            sb.Append('\n');
        }
        return sb.ToString();
    }

    /// <summary>
    /// Medicao da progressao inicial (D-055), so para o relatorio por pessoa: horas ate o
    /// nivel-alvo, minutos por tipo antes dele (foto do fim do dia em que chegou) e metas de
    /// desbloqueio e de descoberta adotadas e cumpridas. Nao muda a simulacao.
    /// </summary>
    private sealed class ProgressionTracker
    {
        private readonly int _target;
        private readonly Dictionary<int, Dictionary<string, long>> _before = new();
        private readonly Dictionary<int, int[]> _goals = new();   // unlock set, unlock done, discover set, discover done, discover nao-exploracao

        public ProgressionTracker(ContentDatabase db) => _target = db.EarlyProgression.TargetLevel;

        public void Consume(IReadOnlyList<SimEvent> evs, WorldState w)
        {
            foreach (var e in evs)
            {
                if (e.Type is not ("goal_set" or "goal_completed") || e.Entities.Count == 0 || e.Detail == null) continue;
                int cid = (int)e.Entities[0].Id;
                if (!_goals.TryGetValue(cid, out var g)) _goals[cid] = g = new int[5];
                bool set = e.Type == "goal_set";
                if (e.Detail.StartsWith("unlock_level|", StringComparison.Ordinal)) g[set ? 0 : 1]++;
                else if (e.Detail.StartsWith("discover_zone|", StringComparison.Ordinal))
                {
                    g[set ? 2 : 3]++;
                    if (set && e.Causes.Count > 0 && e.Causes[0].Factor != "pref_exploration") g[4]++;
                }
            }
            foreach (var c in w.Characters)
                if (c.Level >= _target && !_before.ContainsKey(c.Id))
                    _before[c.Id] = new Dictionary<string, long>(c.ActivityTicksByType, StringComparer.Ordinal);
        }

        public IEnumerable<string> Columns(Character? ch, int characterId, string[] types)
        {
            var inv = System.Globalization.CultureInfo.InvariantCulture;
            yield return _target.ToString(inv);
            yield return ch != null && ch.PlayedTicksAtLevel.Count >= _target - 1
                ? (ch.PlayedTicksAtLevel[_target - 2] / (double)SimTime.TicksPerHour).ToString("0.00", inv) : "";
            _before.TryGetValue(characterId, out var before);
            foreach (var k in types)
                yield return before == null ? "" : (before.GetValueOrDefault(k) / SimTime.TicksPerMinute).ToString(inv);
            var g = _goals.GetValueOrDefault(characterId) ?? new int[5];
            foreach (var x in g) yield return x.ToString(inv);
        }
    }

    private static void WritePeople(string outDir, string[] rows)
    {
        Directory.CreateDirectory(outDir);
        var sb = new System.Text.StringBuilder(PeopleHeader);
        foreach (var r in rows) sb.Append(r);
        File.WriteAllText(Path.Combine(outDir, "people.csv"), sb.ToString(), new System.Text.UTF8Encoding(false));
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

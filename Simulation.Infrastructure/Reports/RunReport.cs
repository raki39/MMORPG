using System.Globalization;
using System.Text;
using System.Text.Json;
using Simulation.Core.Commands;
using Simulation.Core.Events;
using Simulation.Core.Population;
using Simulation.Core.World;

namespace Simulation.Infrastructure.Reports;

/// <summary>Metadados obrigatorios de toda execucao (plano 13.6).</summary>
public sealed class RunMetadata
{
    public string SimulatorVersion { get; set; } = "";
    public string RngAlgorithm { get; set; } = "";
    public int StateHashFormat { get; set; }
    public int TickMilliseconds { get; set; }
    public string WorldId { get; set; } = "";
    public int WorldVersion { get; set; }
    public string WorldName { get; set; } = "";
    public string ContentFolder { get; set; } = "";
    public string ContentHash { get; set; } = "";
    public string ScenarioFile { get; set; } = "";
    public string ScenarioHash { get; set; } = "";
    public ulong Seed { get; set; }
    public int PopulationSize { get; set; }
    public int Days { get; set; }
    public string StartDate { get; set; } = "";
    public bool TickSkipping { get; set; }
    public long TicksSimulated { get; set; }
    public long TicksExecuted { get; set; }
    public double WallTimeMs { get; set; }
    public double SimulationTimeMs { get; set; }
    public double HashTimeMs { get; set; }
    public double ReportTimeMs { get; set; }
    public double SimulatedDaysPerSecond { get; set; }
    public string FinalStateHash { get; set; } = "";
    /// <summary>Estado completo sem a identificacao do conteudo (comparar mudancas que nao deveriam alterar a simulacao).</summary>
    public string FinalStateHashWithoutContentId { get; set; } = "";
    public string Runtime { get; set; } = "";
    public string OperatingSystem { get; set; } = "";
    public int ProcessorCount { get; set; }
    public string GeneratedAtUtc { get; set; } = "";
    public List<SystemTiming> SystemTimings { get; set; } = new();
}

public sealed class SystemTiming
{
    public string System { get; set; } = "";
    public long Calls { get; set; }
    public double TotalMs { get; set; }
}

/// <summary>Hash do estado ao fim de cada dia (calculado fora do tick).</summary>
public sealed class DailyHash
{
    public DailyHash(long dayIndex, string hash)
    {
        DayIndex = dayIndex;
        Hash = hash;
    }

    public long DayIndex { get; }
    public string Hash { get; }
}

/// <summary>
/// Escreve CSVs (dados verificaveis) e resumo Markdown (leitura) de uma execucao.
/// Metricas que dependem de sistemas ainda nao implementados aparecem como indisponiveis,
/// nunca com numeros ficticios.
/// </summary>
public static class RunReportWriter
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    /// <summary>Metricas previstas no plano e o marco em que passam a existir.</summary>
    public static readonly (string Metric, string Milestone)[] UnavailableMetrics =
    {
        ("Abandono, retorno e satisfação", "M2 (próxima etapa)"),
        ("Grupos para conteúdo de grupo (boss, recrutamento)", "M6"),
        ("Combate segmentado (dano, cura, mortes)", "M3"),
        ("Fontes e consumos de ouro", "M3/M5"),
        ("Preços negociados (médio/mediano), volume e liquidez", "M5"),
        ("Cesta de inflação", "M5"),
        ("Relações, grupos e guildas", "M6"),
        ("Finanças empresariais (receita, despesas, caixa)", "M7"),
    };

    public static void WriteAll(string outDir, RunMetadata meta, WorldState world,
        IReadOnlyList<SimEvent> events, IReadOnlyList<DailyHash> hashes)
    {
        Directory.CreateDirectory(outDir);
        WriteDaily(Path.Combine(outDir, "daily.csv"), world, hashes);
        WriteEvents(Path.Combine(outDir, "events.csv"), world, events);
        WriteCommands(Path.Combine(outDir, "commands.csv"), world.Commands.History);
        WritePopulation(Path.Combine(outDir, "population.csv"), world.Persons);
        WriteCharacters(Path.Combine(outDir, "characters.csv"), world);
        File.WriteAllText(Path.Combine(outDir, "metadata.json"),
            JsonSerializer.Serialize(meta, new JsonSerializerOptions { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
            new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(outDir, "summary.md"), BuildSummary(meta, world, events, hashes), new UTF8Encoding(false));
    }

    public static void WriteDaily(string path, WorldState world, IReadOnlyList<DailyHash> hashes)
    {
        var hashByDay = hashes.ToDictionary(h => h.DayIndex, h => h.Hash);
        var sb = new StringBuilder();
        sb.Append("day,date,day_kind,closed_at_tick,persons_total,potential_interested,registered,online,server_open,commands_applied,commands_rejected,events_emitted,registrations,sessions,active_players,peak_online,activities_chosen,level_ups,play_minutes,state_hash\n");
        foreach (var d in world.History)
        {
            sb.Append(string.Join(",",
                d.DayIndex.ToString(Inv), d.Date.ToString("yyyy-MM-dd", Inv), d.DayKind.ToString().ToLowerInvariant(),
                d.ClosedAtTick.ToString(Inv), d.PersonsTotal.ToString(Inv), d.PotentialInterested.ToString(Inv),
                d.Registered.ToString(Inv), d.Online.ToString(Inv), d.ServerOpen ? "1" : "0",
                d.CommandsApplied.ToString(Inv), d.CommandsRejected.ToString(Inv), d.EventsEmitted.ToString(Inv),
                d.Registrations.ToString(Inv), d.SessionsStarted.ToString(Inv), d.ActivePlayers.ToString(Inv), d.PeakOnline.ToString(Inv),
                d.ActivitiesChosen.ToString(Inv), d.LevelUps.ToString(Inv), d.PlayMinutes.ToString(Inv),
                hashByDay.TryGetValue(d.DayIndex, out var h) ? h : ""));
            sb.Append('\n');
        }
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    public static void WriteEvents(string path, WorldState world, IReadOnlyList<SimEvent> events)
    {
        var sb = new StringBuilder("event_id,tick,datetime,type,importance,entities,causes,detail\n");
        foreach (var e in events)
        {
            string causes = string.Join(";", e.Causes.Select(c =>
                c.Factor + ":" + c.WeightPermille.ToString(Inv) +
                (c.SourceEventId.HasValue ? ":ev" + c.SourceEventId.Value.ToString(Inv) : "") +
                (c.SourceCommandSequence.HasValue ? ":cmd" + c.SourceCommandSequence.Value.ToString(Inv) : "")));
            sb.Append(string.Join(",",
                e.Id.ToString(Inv), e.Tick.ToString(Inv), world.Clock.Calendar.ToDateTime(e.Tick).ToString("yyyy-MM-dd HH:mm:ss.f", Inv),
                Csv(e.Type), e.Importance.ToString().ToLowerInvariant(),
                Csv(string.Join(";", e.Entities.Select(x => x.ToString()))), Csv(causes), Csv(e.Detail ?? "")));
            sb.Append('\n');
        }
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    public static void WriteCommands(string path, IReadOnlyList<CommandEnvelope> commands)
    {
        var sb = new StringBuilder("sequence,submitted_tick,scheduled_tick,applied_tick,type,payload,status,rejection_reason\n");
        foreach (var c in commands)
        {
            sb.Append(string.Join(",",
                c.Sequence.ToString(Inv), c.SubmittedAtTick.ToString(Inv), c.ScheduledTick.ToString(Inv), c.AppliedTick.ToString(Inv),
                Csv(c.Command.Type), Csv(c.Command.Describe()), c.Status.ToString().ToLowerInvariant(), Csv(c.RejectionReason ?? "")));
            sb.Append('\n');
        }
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    private static readonly (string Name, Func<PersonTraits, int> Get)[] TraitColumns =
    {
        ("pve", t => t.Pve), ("pvp", t => t.Pvp), ("trade", t => t.Trade), ("exploration", t => t.Exploration),
        ("socializing", t => t.Socializing), ("sociability", t => t.Sociability), ("aggressiveness", t => t.Aggressiveness),
        ("patience", t => t.Patience), ("loyalty", t => t.Loyalty), ("risk_aversion", t => t.RiskAversion),
        ("mechanical_skill", t => t.MechanicalSkill), ("game_knowledge", t => t.GameKnowledge), ("coordination", t => t.Coordination),
        ("cheat_propensity", t => t.CheatPropensity), ("opportunism", t => t.Opportunism),
        ("leisure_budget_cents_month", t => t.LeisureBudgetCentsPerMonth), ("willingness_to_pay", t => t.WillingnessToPay),
        ("paid_advantage_sensitivity", t => t.PaidAdvantageSensitivity), ("preferred_start_hour", t => t.PreferredStartHour),
        ("session_minutes", t => t.SessionMinutes), ("game_importance", t => t.GameImportance), ("habit", t => t.Habit),
        ("competitiveness", t => t.Competitiveness), ("extend_tendency", t => t.ExtendTendency), ("self_control", t => t.SelfControl),
        ("talent", t => t.Talent), ("variety", t => t.Variety),
    };

    public static void WritePopulation(string path, IReadOnlyList<Person> persons)
    {
        var sb = new StringBuilder("person_id,name,profile,appearance_seed,state," + string.Join(",", TraitColumns.Select(c => c.Name))
            + "," + string.Join(",", PersonPreferences.Keys.Select(k => "pref_" + k)) + "\n");
        foreach (var p in persons)
        {
            sb.Append(string.Join(",",
                new[] { p.Id.ToString(Inv), Csv(p.Name), p.ProfileId, p.AppearanceSeed.ToString("x16", Inv), p.State.ToString() }
                .Concat(TraitColumns.Select(c => c.Get(p.Traits).ToString(Inv)))
                .Concat(PersonPreferences.Keys.Select(k => p.Preferences.Get(k).ToString(Inv)))));
            sb.Append('\n');
        }
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    public static void WriteCharacters(string path, WorldState world)
    {
        var sb = new StringBuilder("character_id,person_id,nickname,class,level,experience,status,region,known_regions,decisions,last_choice\n");
        foreach (var c in world.Characters)
        {
            string last = c.LastDecision == null ? "" : c.LastDecision.RegionId + "/" + c.LastDecision.ActivityId;
            sb.Append(string.Join(",", c.Id.ToString(Inv), c.PersonId.ToString(Inv), Csv(c.Name), c.ClassId, c.Level.ToString(Inv),
                c.Experience.ToString(Inv), c.Status.ToString().ToLowerInvariant(), c.RegionId,
                Csv(string.Join(";", c.KnownRegions)), c.DecisionCount.ToString(Inv), Csv(last)));
            sb.Append('\n');
        }
        File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
    }

    private static void AppendBehavior(StringBuilder sb, WorldState world, IReadOnlyList<SimEvent> events)
    {
        sb.AppendLine("## Comportamento");
        sb.AppendLine();
        var chosen = events.Where(e => e.Type == "activity_chosen" && e.Detail != null).ToList();
        if (chosen.Count == 0)
        {
            sb.AppendLine("Nenhuma escolha de atividade registrada.");
            sb.AppendLine();
            return;
        }
        sb.AppendLine($"{N(chosen.Count)} escolhas de atividade. Distribuição por zona e atividade (escolhas, não tempo gasto):");
        sb.AppendLine();
        sb.AppendLine("| Zona | Atividade | Tipo | Escolhas | Participação |");
        sb.AppendLine("|---|---|---|---:|---:|");
        foreach (var g in chosen.GroupBy(e => e.Detail!).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal))
        {
            var parts = g.Key.Split('/');
            var region = world.Content.Regions.TryGetValue(parts[0], out var r) ? r : null;
            var act = region?.FindActivity(parts[1]);
            sb.AppendLine($"| {region?.Name ?? parts[0]} | {act?.Name ?? parts[1]} | `{act?.Type}` | {N(g.Count())} | {Pct(g.Count(), chosen.Count)} |");
        }
        sb.AppendLine();

        if (world.Characters.Count > 0)
        {
            sb.AppendLine("| Nível | Personagens |");
            sb.AppendLine("|---:|---:|");
            foreach (var g in world.Characters.GroupBy(c => c.Level).OrderBy(g => g.Key))
                sb.AppendLine($"| {g.Key} | {N(g.Count())} |");
            sb.AppendLine();
            sb.AppendLine("| Classe | Personagens |");
            sb.AppendLine("|---|---:|");
            foreach (var g in world.Characters.GroupBy(c => c.ClassId).OrderBy(g => g.Key, StringComparer.Ordinal))
                sb.AppendLine($"| {(world.Content.Classes.TryGetValue(g.Key, out var cl) ? cl.Name : g.Key)} | {N(g.Count())} |");
            sb.AppendLine();
        }

        sb.AppendLine("Exemplos de decisões com os fatores calculados (permille ponderado):");
        sb.AppendLine();
        foreach (var c in world.Characters.Where(c => c.LastDecision != null).Take(5))
        {
            var d = c.LastDecision!;
            var region = world.Content.Regions[d.RegionId];
            var act = region.FindActivity(d.ActivityId)!;
            string factors = string.Join(", ", d.Factors.Where(f => f.Key != "noise").OrderByDescending(f => Math.Abs(f.Contribution))
                .Select(f => $"{f.Key} {(f.Contribution >= 0 ? "+" : "")}{f.Contribution}"));
            sb.AppendLine($"- **{c.Name}** (nível {c.Level}) escolheu *{act.Name}* em {region.Name} entre {d.OptionsConsidered} opções: {factors}.");
        }
        sb.AppendLine();
    }

    public static string BuildSummary(RunMetadata m, WorldState world, IReadOnlyList<SimEvent> events, IReadOnlyList<DailyHash> hashes)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Relatório de execução — {m.WorldName}");
        sb.AppendLine();
        sb.AppendLine("> Execução headless do `Simulation.Runner`. Todos os números abaixo vêm do estado simulado; métricas de sistemas ainda não implementados aparecem como indisponíveis.");
        sb.AppendLine();
        sb.AppendLine("## Metadados");
        sb.AppendLine();
        sb.AppendLine("| Campo | Valor |");
        sb.AppendLine("|---|---|");
        Row(sb, "Versão do simulador", m.SimulatorVersion);
        Row(sb, "Mundo (conteúdo)", $"`{m.WorldId}` v{m.WorldVersion}");
        Row(sb, "Pasta de conteúdo", $"`{m.ContentFolder}`");
        Row(sb, "Hash do conteúdo", $"`{Short(m.ContentHash)}`");
        Row(sb, "Cenário", m.ScenarioFile.Length > 0 ? $"`{m.ScenarioFile}` (`{Short(m.ScenarioHash)}`)" : "nenhum");
        Row(sb, "Seed", m.Seed.ToString(Inv));
        Row(sb, "População", N(m.PopulationSize) + " pessoas");
        Row(sb, "Duração simulada", $"{N(m.Days)} dias a partir de {m.StartDate}");
        Row(sb, "Tick lógico", $"{m.TickMilliseconds} ms");
        Row(sb, "RNG", $"`{m.RngAlgorithm}`");
        Row(sb, "Formato do hash de estado", $"v{m.StateHashFormat}");
        Row(sb, "Ambiente", $"{m.Runtime} · {m.OperatingSystem} · {m.ProcessorCount} núcleos lógicos");
        Row(sb, "Gerado em (UTC)", m.GeneratedAtUtc);
        sb.AppendLine();

        sb.AppendLine("## Desempenho medido");
        sb.AppendLine();
        sb.AppendLine("| Medida | Valor |");
        sb.AppendLine("|---|---|");
        Row(sb, "Tempo total (parede)", Ms(m.WallTimeMs));
        Row(sb, "Simulação (sem hash e relatórios)", Ms(m.SimulationTimeMs));
        Row(sb, "Hash de estado diário", Ms(m.HashTimeMs));
        Row(sb, "Escrita de relatórios", Ms(m.ReportTimeMs));
        Row(sb, "Velocidade", m.SimulatedDaysPerSecond.ToString("N1", PtBr) + " dias simulados por segundo (só simulação)");
        Row(sb, "Ticks simulados", N(m.TicksSimulated));
        Row(sb, "Ticks executados", N(m.TicksExecuted) + (m.TickSkipping ? " (salto de ticks ociosos ligado)" : " (tick a tick)"));
        sb.AppendLine();
        if (m.SystemTimings.Count > 0)
        {
            sb.AppendLine("| Sistema | Chamadas | Tempo total |");
            sb.AppendLine("|---|---:|---:|");
            foreach (var s in m.SystemTimings)
                sb.AppendLine($"| `{s.System}` | {N(s.Calls)} | {Ms(s.TotalMs)} |");
            sb.AppendLine();
        }
        sb.AppendLine("Medições de uma única execução nesta máquina; não são benchmark nem promessa de capacidade.");
        sb.AppendLine();

        sb.AppendLine("## Resultado");
        sb.AppendLine();
        sb.AppendLine($"- Hash final do estado: `{m.FinalStateHash}`");
        sb.AppendLine($"- Hash final sem a identificação do conteúdo: `{m.FinalStateHashWithoutContentId}`");
        sb.AppendLine($"- Servidor aberto ao fim: {(world.Server.IsOpen ? "sim" : "não")}");
        sb.AppendLine($"- Eventos registrados: {N(events.Count)} · Comandos: {N(world.Commands.History.Count)} ({N(world.Commands.History.Count(c => c.Status == CommandStatus.Rejected))} rejeitados)");
        sb.AppendLine();

        sb.AppendLine("## Série diária");
        sb.AppendLine();
        sb.AppendLine("| Dia | Data | Tipo | Registros | Cadastrados | Ativos no dia | Pico online | Sessões | Escolhas | Subidas de nível | Horas jogadas | Hash |");
        sb.AppendLine("|---:|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---|");
        var hashByDay = hashes.ToDictionary(h => h.DayIndex, h => h.Hash);
        var days = world.History;
        IEnumerable<DailyRecord> shown = days.Count <= 31 ? days : days.Take(7).Concat(days.Skip(days.Count - 7));
        long lastShown = -1;
        foreach (var d in shown)
        {
            if (lastShown >= 0 && d.DayIndex != lastShown + 1)
                sb.AppendLine("| … | | | | | | | | | |");
            sb.AppendLine($"| {d.DayIndex} | {d.Date.ToString("dd/MM/yyyy ddd", PtBr)} | {DayKindPt(d.DayKind)} | {N(d.Registrations)} | {N(d.Registered)} | {N(d.ActivePlayers)} | {N(d.PeakOnline)} | {N(d.SessionsStarted)} | {N(d.ActivitiesChosen)} | {N(d.LevelUps)} | {N(d.PlayMinutes / 60)} | `{(hashByDay.TryGetValue(d.DayIndex, out var h) ? Short(h) : "-")}` |");
            lastShown = d.DayIndex;
        }
        sb.AppendLine();
        sb.AppendLine("Tabela completa em `daily.csv`.");
        sb.AppendLine();

        AppendBehavior(sb, world, events);

        sb.AppendLine("## População gerada");
        sb.AppendLine();
        sb.AppendLine("| Perfil | Pessoas | Participação |");
        sb.AppendLine("|---|---:|---:|");
        foreach (var g in world.Persons.GroupBy(p => p.ProfileId).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            string name = world.Content.Profiles.TryGetValue(g.Key, out var prof) ? prof.Name : g.Key;
            sb.AppendLine($"| {name} | {N(g.Count())} | {Pct(g.Count(), world.Persons.Count)} |");
        }
        sb.AppendLine();
        if (world.Persons.Count > 0)
        {
            sb.AppendLine("| Parâmetro | Mín. | P10 | Mediana | P90 | Máx. |");
            sb.AppendLine("|---|---:|---:|---:|---:|---:|");
            foreach (var col in TraitColumns)
            {
                var vals = world.Persons.Select(p => col.Get(p.Traits)).OrderBy(v => v).ToArray();
                sb.AppendLine($"| `{col.Name}` | {N(vals[0])} | {N(Pctl(vals, 10))} | {N(Pctl(vals, 50))} | {N(Pctl(vals, 90))} | {N(vals[^1])} |");
            }
            sb.AppendLine();
            sb.AppendLine("Valores comportamentais em permille (0–1000). Lista individual em `population.csv`.");
            sb.AppendLine();
        }

        sb.AppendLine("## Eventos e comandos");
        sb.AppendLine();
        if (events.Count == 0) sb.AppendLine("Nenhum evento registrado.");
        foreach (var g in events.GroupBy(e => e.Type).OrderBy(g => g.Key, StringComparer.Ordinal))
            sb.AppendLine($"- `{g.Key}`: {N(g.Count())}");
        sb.AppendLine();
        foreach (var c in world.Commands.History)
        {
            string when = world.Clock.Calendar.ToDateTime(c.ScheduledTick).ToString("dd/MM/yyyy HH:mm", PtBr);
            string status = c.Status == CommandStatus.Applied ? "aplicado" : c.Status == CommandStatus.Rejected ? $"rejeitado — {c.RejectionReason}" : "pendente";
            sb.AppendLine($"- #{c.Sequence} `{c.Command.Type}` ({c.Command.Describe()}) em {when}: {status}");
        }
        sb.AppendLine();

        sb.AppendLine("## Métricas indisponíveis nesta versão");
        sb.AppendLine();
        sb.AppendLine("| Métrica | Disponível a partir de |");
        sb.AppendLine("|---|---|");
        foreach (var (metric, ms) in UnavailableMetrics)
            sb.AppendLine($"| {metric} | {ms} |");
        sb.AppendLine();
        return sb.ToString();
    }

    private static string DayKindPt(Simulation.Core.Time.DayKind k) => k switch
    {
        Simulation.Core.Time.DayKind.Weekend => "fim de semana",
        Simulation.Core.Time.DayKind.Holiday => "feriado",
        _ => "dia útil",
    };

    private static int Pctl(int[] sorted, int p)
    {
        int idx = (int)Math.Round((p / 100.0) * (sorted.Length - 1), MidpointRounding.AwayFromZero);
        return sorted[idx];
    }

    private static void Row(StringBuilder sb, string k, string v) => sb.AppendLine($"| {k} | {v} |");
    private static string N(long v) => v.ToString("N0", PtBr);
    private static string Ms(double ms) => ms >= 1000 ? (ms / 1000).ToString("N2", PtBr) + " s" : ms.ToString("N1", PtBr) + " ms";
    private static string Pct(int part, int total) => total == 0 ? "sem dados" : ((double)part / total).ToString("P1", PtBr);
    private static string Short(string hash) => hash.Length > 16 ? hash.Substring(0, 16) : hash;

    private static string Csv(string s)
    {
        if (s.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0) return s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }
}

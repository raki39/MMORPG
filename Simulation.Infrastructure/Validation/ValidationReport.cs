using System.Globalization;
using System.Text;
using Simulation.Core.Behavior;
using Simulation.Core.Content;
using Simulation.Core.Time;

namespace Simulation.Infrastructure.Validation;

/// <summary>Situacao de uma meta considerando todas as seeds.</summary>
public enum TargetStatus { Inside, Unstable, Below, Above, NoData }

public sealed class TargetOutcome
{
    public TargetDefinition Target { get; init; } = new();
    public double Mean { get; init; }
    public double Min { get; init; }
    public double Max { get; init; }
    public TargetStatus Status { get; init; }
    public string Notes { get; init; } = "";
}

/// <summary>Contexto da validacao (para o cabecalho do relatorio).</summary>
public sealed class ValidationContext
{
    public string WorldFolder { get; init; } = "";
    public string ContentHash { get; init; } = "";
    public string TargetsFile { get; init; } = "";
    public string TargetsHash { get; init; } = "";
    public string SimulatorVersion { get; init; } = "";
    public int Population { get; init; }
    public int Days { get; init; }
    public double WallSeconds { get; set; }
}

/// <summary>
/// Relatorio de validacao: metas x medidas em varias seeds (plano 17.1), perfis,
/// uso do tempo e atividades pouco usadas com diagnostico.
/// </summary>
public static class ValidationReport
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public static List<TargetOutcome> Outcomes(TargetsFile targets, IReadOnlyList<RunMetrics> runs)
    {
        var list = new List<TargetOutcome>();
        foreach (var t in targets.Targets)
        {
            var values = runs.Select(r => r.Values.GetValueOrDefault(t.Id, double.NaN)).Where(v => !double.IsNaN(v)).ToList();
            var notes = runs.Select(r => r.Notes.GetValueOrDefault(t.Id, "")).Where(n => n.Length > 0).Distinct().ToList();
            if (values.Count == 0)
            {
                list.Add(new TargetOutcome { Target = t, Mean = double.NaN, Min = double.NaN, Max = double.NaN, Status = TargetStatus.NoData, Notes = string.Join("; ", notes) });
                continue;
            }
            double mean = values.Average(), min = values.Min(), max = values.Max();
            TargetStatus status =
                mean < t.Min ? TargetStatus.Below :
                mean > t.Max ? TargetStatus.Above :
                min >= t.Min && max <= t.Max ? TargetStatus.Inside : TargetStatus.Unstable;
            var reached = runs.Where(r => r.Reached.ContainsKey(t.Id)).Select(r => r.Reached[t.Id]).ToList();
            if (reached.Count > 0)
            {
                double lo = reached.Min(), hi = reached.Max();
                string range = Math.Round(lo) == Math.Round(hi) ? $"{lo:F0}%" : $"{lo:F0}–{hi:F0}%";
                notes.Insert(0, $"{range} chegaram" + (lo < 50 ? " (mediana só de quem chegou: subestima o tempo real)" : ""));
            }
            if (values.Count < runs.Count) notes.Add($"sem amostra em {runs.Count - values.Count} seed(s)");
            list.Add(new TargetOutcome { Target = t, Mean = mean, Min = min, Max = max, Status = status, Notes = string.Join("; ", notes) });
        }
        return list;
    }

    public static void Write(string outDir, ValidationContext ctx, TargetsFile targets, ContentDatabase content, IReadOnlyList<RunMetrics> runs)
    {
        Directory.CreateDirectory(outDir);
        var outcomes = Outcomes(targets, runs);
        File.WriteAllText(Path.Combine(outDir, "validation.md"), BuildMarkdown(ctx, targets, content, runs, outcomes), new UTF8Encoding(false));
        File.WriteAllText(Path.Combine(outDir, "validation.csv"), BuildCsv(targets, runs), new UTF8Encoding(false));
    }

    public static string BuildCsv(TargetsFile targets, IReadOnlyList<RunMetrics> runs)
    {
        var sb = new StringBuilder("target_id,metric,profile,seed,value,min,max\n");
        foreach (var t in targets.Targets)
            foreach (var r in runs)
                sb.Append(Inv, $"{t.Id},{t.Metric},{t.Profile},{r.Seed},{Num(r.Values.GetValueOrDefault(t.Id, double.NaN))},{t.Min.ToString(Inv)},{t.Max.ToString(Inv)}\n");
        return sb.ToString();
    }

    public static string BuildMarkdown(ValidationContext ctx, TargetsFile targets, ContentDatabase content,
        IReadOnlyList<RunMetrics> runs, List<TargetOutcome> outcomes)
    {
        var sb = new StringBuilder();
        sb.Append("# Validação da simulação\n\n");
        sb.Append("> Gerado pelo `Simulation.Runner` com várias seeds. As faixas vêm de `targets.json` e são **hipóteses de design ajustáveis**, não regras do mundo. ");
        sb.Append("Uma meta só conta como \"dentro\" quando todas as seeds caem na faixa.\n\n");

        sb.Append("| Campo | Valor |\n|---|---|\n");
        sb.Append($"| Conteúdo | `{ctx.WorldFolder}` (`{Short(ctx.ContentHash)}`) |\n");
        sb.Append($"| Metas | `{ctx.TargetsFile}` (`{Short(ctx.TargetsHash)}`) |\n");
        sb.Append($"| Simulador | {ctx.SimulatorVersion} |\n");
        sb.Append($"| Execuções | {runs.Count} seeds ({string.Join(", ", runs.Select(r => r.Seed))}) · {N(ctx.Population)} pessoas · {ctx.Days} dias |\n");
        sb.Append($"| Janela das medidas semanais | a partir do dia {targets.WarmupDays} (exclui a semana de lançamento) |\n");
        sb.Append($"| Tempo total | {ctx.WallSeconds.ToString("N1", PtBr)} s |\n\n");

        int inside = outcomes.Count(o => o.Status == TargetStatus.Inside);
        sb.Append($"## Metas\n\n**{inside} de {outcomes.Count} dentro da faixa.**\n\n");
        sb.Append("| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |\n|---|---|---|---:|---|---|\n");
        foreach (var o in outcomes)
        {
            var t = o.Target;
            sb.Append($"| {StatusText(o.Status)} | {t.Name} | {Fmt(t.Min)}–{Fmt(t.Max)} {t.Unit} | {Fmt(o.Mean)} | {Fmt(o.Min)}–{Fmt(o.Max)} | {Esc(o.Notes)} |\n");
        }
        sb.Append("\nPor que cada faixa:\n\n");
        foreach (var t in targets.Targets.Where(t => t.Rationale.Length > 0))
            sb.Append($"- **{t.Name}:** {t.Rationale}\n");

        // ---- Perfis ----
        sb.Append("\n## Perfis (média das seeds)\n\n");
        sb.Append("| Perfil | Pessoas | Sessões/semana | Horas/semana | Nível mediano no fim | Nível p90 | Sessões em dia útil iniciadas entre 18h e 23h |\n|---|---:|---:|---:|---:|---:|---:|\n");
        foreach (var id in runs[0].Profiles.Select(p => p.Id))
        {
            var ps = runs.Select(r => r.Profiles.First(p => p.Id == id)).ToList();
            sb.Append($"| {ps[0].Name} | {Fmt(ps.Average(p => p.Persons))} | {Fmt(Avg(ps.Select(p => p.SessionsPerWeek)))} | {Fmt(Avg(ps.Select(p => p.HoursPerWeek)))} | " +
                      $"{Fmt(Avg(ps.Select(p => p.MedianLevel)))} | {Fmt(Avg(ps.Select(p => p.P90Level)))} | {Fmt(Avg(ps.Select(p => p.PrimeTimeStartPct)))}% |\n");
        }

        // ---- Atributos do jogador (D-036) ----
        const double unitsPerPoint = 1000.0 / 19.0;
        sb.Append("\n## Evolução dos atributos do jogador (média das seeds)\n\n");
        sb.Append($"Variação média desde o início, em **pontos da escala 1–20** (1 ponto ≈ {unitsPerPoint.ToString("N1", PtBr)} unidades do Core), ao fim de {ctx.Days} dias. Valores de `population.json` (`dynamics`).\n\n");
        sb.Append("| Perfil | Mecânica | Conhecimento | PvE | Exploração | Comércio | Aversão a risco | Metas concluídas | Metas abandonadas |\n|---|---:|---:|---:|---:|---:|---:|---:|---:|\n");
        foreach (var id in runs[0].Profiles.Select(p => p.Id))
        {
            var ps = runs.Select(r => r.Profiles.First(p => p.Id == id)).ToList();
            string D(string k) => (ps.Average(p => p.TraitDelta.TryGetValue(k, out var v) ? v : 0) / unitsPerPoint).ToString("+0.00;-0.00;0.00", PtBr);
            sb.Append($"| {ps[0].Name} | {D("mechanicalSkill")} | {D("gameKnowledge")} | {D("pve")} | {D("exploration")} | {D("trade")} | {D("riskAversion")} | {Fmt(ps.Average(p => p.GoalsCompleted))} | {Fmt(ps.Average(p => p.GoalsAbandoned))} |\n");
        }
        var dyn = content.Dynamics;
        sb.Append("\nInatividade (queda, sem modificadores):\n\n| Dias parado | Mecânica | Conhecimento |\n|---:|---:|---:|\n");
        foreach (int days in new[] { 7, 14, 30, 60, 90 })
        {
            double mech = Math.Max(0, days - dyn.MechanicalDecayAfterDays) * dyn.MechanicalDecayPerDay / unitsPerPoint;
            double know = Math.Max(0, days - dyn.KnowledgeDecayAfterDays) * dyn.KnowledgeDecayPerDay / unitsPerPoint;
            sb.Append($"| {days} | {(-mech).ToString("0.00;-0.00;0.00", PtBr)} | {(-know).ToString("0.00;-0.00;0.00", PtBr)} |\n");
        }

        // ---- Retencao (D-039) ----
        sb.Append("\n## Retenção (média das seeds, no fim)\n\n");
        sb.Append("| Perfil | Desistiram | Inativos | Voltaram alguma vez |\n|---|---:|---:|---:|\n");
        foreach (var id in runs[0].Profiles.Select(p => p.Id))
        {
            var ps = runs.Select(r => r.Profiles.First(p => p.Id == id)).ToList();
            sb.Append($"| {ps[0].Name} | {Fmt(ps.Average(p => p.QuitPct))}% | {Fmt(ps.Average(p => p.InactivePct))}% | {Fmt(ps.Average(p => p.ReturnedPct))}% |\n");
        }
        var quitDays = runs.SelectMany(r => r.QuitDays).Select(d => (double)d).ToList();
        var quitLevels = runs.SelectMany(r => r.QuitLevels).Select(d => (double)d).ToList();
        if (quitDays.Count > 0)
            sb.Append($"\nDesistências: dia mediano {Fmt(Percentile(quitDays, 0.5))}, nível mediano {Fmt(Percentile(quitLevels, 0.5))}.");
        long rated2 = runs.Sum(r => r.RatedSessions);
        if (rated2 > 0)
            sb.Append($" Sessões estendidas por estarem indo bem: {Pct(runs.Sum(r => r.SessionsExtended), rated2)}; encerradas por frustração: {Pct(runs.Sum(r => r.RageQuits), rated2)}.");
        sb.Append("\n");
        var qr = new SortedDictionary<string, long>(StringComparer.Ordinal);
        foreach (var r in runs) foreach (var kv in r.QuitReasons) qr[kv.Key] = qr.GetValueOrDefault(kv.Key) + kv.Value;
        if (qr.Count > 0)
        {
            long allQ = Math.Max(1, qr.Values.Sum());
            sb.Append("\nMotivo principal das desistências (o que mais incomodava; \"long_inactivity/\" = sumiu por muito tempo):\n\n| Motivo | Desistências |\n|---|---:|\n");
            foreach (var kv in qr.OrderByDescending(k => k.Value).ThenBy(k => k.Key, StringComparer.Ordinal))
                sb.Append($"| {kv.Key} | {Pct(kv.Value, allQ)} |\n");
        }

        // ---- Humor e memoria (D-038) ----
        sb.Append("\n## Humor e memória (média das seeds, no fim)\n\n");
        sb.Append("Satisfação, frustração, novidade e engajamento em permille (0–1000); progresso em permille do XP de referência do nível.\n\n");
        sb.Append("| Perfil | Satisfação | Insatisfeitos (< 400) | Frustração | Novidade | Engajamento | Intenção de continuar | Expectativa | Progresso da última sessão | Memórias |\n|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|\n");
        foreach (var id in runs[0].Profiles.Select(p => p.Id))
        {
            var ps = runs.Select(r => r.Profiles.First(p => p.Id == id)).ToList();
            string Mv(string k) => Fmt(ps.Average(p => p.Mood.TryGetValue(k, out var v) ? v : double.NaN));
            sb.Append($"| {ps[0].Name} | {Mv("satisfaction")} | {Mv("unhappy_pct")}% | {Mv("frustration")} | {Mv("novelty")} | {Mv("engagement")} | {Mv("return_intention")} | {Mv("expectation")} | {Mv("progress_ratio")} | {Mv("memories")} |\n");
        }
        long rated = runs.Sum(r => r.RatedSessions);
        if (rated > 0)
            sb.Append($"\nSessões ótimas: {Pct(runs.Sum(r => r.GreatSessions), rated)} · ruins: {Pct(runs.Sum(r => r.BadSessions), rated)} (após a semana de lançamento).\n");
        var complaints = new SortedDictionary<string, long>(StringComparer.Ordinal);
        foreach (var r in runs) foreach (var kv in r.TopComplaints) complaints[kv.Key] = complaints.GetValueOrDefault(kv.Key) + kv.Value;
        long allC = Math.Max(1, complaints.Values.Sum());
        sb.Append("\nO que mais incomoda cada pessoa (motivo mais negativo acumulado):\n\n| Motivo | Pessoas |\n|---|---:|\n");
        foreach (var kv in complaints.OrderByDescending(k => k.Value).ThenBy(k => k.Key, StringComparer.Ordinal))
            sb.Append($"| {kv.Key} | {Pct(kv.Value, allC)} |\n");

        // ---- Uso do tempo ----
        sb.Append("\n## Uso do tempo conectado (todas as seeds)\n\n");
        var time = new SortedDictionary<string, long>(StringComparer.Ordinal);
        foreach (var r in runs)
            foreach (var kv in r.TimeByType) time[kv.Key] = time.GetValueOrDefault(kv.Key) + kv.Value;
        long totalTime = Math.Max(1, time.Values.Sum());
        var choicesByType = new Dictionary<string, long>(StringComparer.Ordinal);
        long totalChoices = 0;
        foreach (var r in runs)
            foreach (var kv in r.Choices)
            {
                string type = ActivityOf(content, kv.Key)?.Type ?? "?";
                choicesByType[type] = choicesByType.GetValueOrDefault(type) + kv.Value;
                totalChoices += kv.Value;
            }
        sb.Append("| Tipo | Tempo | Escolhas |\n|---|---:|---:|\n");
        foreach (var kv in time.OrderByDescending(k => k.Value).ThenBy(k => k.Key, StringComparer.Ordinal))
        {
            string ch = choicesByType.TryGetValue(kv.Key, out long c) ? Pct(c, Math.Max(1, totalChoices)) : "—";
            sb.Append($"| {TypeName(kv.Key)} | {Pct(kv.Value, totalTime)} | {ch} |\n");
        }

        // ---- Continuidade (D-026/D-037) ----
        sb.Append("\n## Continuidade das sessões (média das seeds)\n\n");
        sb.Append("| Medida | Valor |\n|---|---:|\n");
        double C(string k) => runs.Average(r => r.Continuity.TryGetValue(k, out var v) ? v : double.NaN);
        sb.Append($"| Escolhas por sessão | {Fmt(C("choices_per_session"))} |\n");
        sb.Append($"| Continuou na mesma atividade (% das escolhas após a primeira) | {Fmt(C("continue_pct"))}% |\n");
        sb.Append($"| Trocas de atividade por hora conectada | {Fmt(C("switches_per_hour"))} |\n");
        sb.Append($"| Trocas de zona por sessão | {Fmt(C("zone_switches_per_session"))} |\n");
        sb.Append($"| Ciclos seguidos na mesma atividade (média) | {Fmt(C("run_cycles"))} |\n");
        var reasons = new SortedDictionary<string, long>(StringComparer.Ordinal);
        foreach (var r in runs) foreach (var kv in r.SwitchReasons) reasons[kv.Key] = reasons.GetValueOrDefault(kv.Key) + kv.Value;
        if (reasons.Count > 0)
        {
            long allR = Math.Max(1, reasons.Values.Sum());
            sb.Append("\nMotivos das trocas de plano (fator principal):\n\n| Motivo | Trocas |\n|---|---:|\n");
            foreach (var kv in reasons.OrderByDescending(k => k.Value).ThenBy(k => k.Key, StringComparer.Ordinal))
                sb.Append($"| {kv.Key} | {Pct(kv.Value, allR)} |\n");
        }

        // ---- Atividades pouco usadas ----
        var allLevels = runs.SelectMany(r => r.FinalLevels).ToList();
        var choices = new SortedDictionary<string, long>(StringComparer.Ordinal);
        foreach (var r in runs)
            foreach (var kv in r.Choices) choices[kv.Key] = choices.GetValueOrDefault(kv.Key) + kv.Value;
        long all = Math.Max(1, choices.Values.Sum());
        var rare = choices.Where(kv => kv.Value * 1000 / all < targets.RareActivityPermille)
            .OrderBy(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).ToList();
        sb.Append($"\n## Atividades pouco usadas (menos de {(targets.RareActivityPermille / 10.0).ToString("0.#", PtBr)}% das escolhas)\n\n");
        if (rare.Count == 0) sb.Append("Nenhuma.\n");
        else
        {
            sb.Append("| Zona | Atividade | Tipo | Escolhas | Diagnóstico |\n|---|---|---|---:|---|\n");
            foreach (var kv in rare)
            {
                var (region, act) = Split(content, kv.Key);
                if (region == null || act == null) continue;
                sb.Append($"| {region.Name} | {act.Name} | `{act.Type}` | {Pct(kv.Value, all)} | {Esc(Diagnose(region, act, allLevels))} |\n");
            }
        }

        // ---- Execucoes ----
        sb.Append("\n## Execuções\n\n| Seed | Hash final | Simulação |\n|---:|---|---:|\n");
        foreach (var r in runs)
            sb.Append($"| {r.Seed} | `{Short(r.FinalStateHash)}` | {r.SimulationSeconds.ToString("N1", PtBr)} s |\n");
        sb.Append("\nMedições de tempo desta máquina, não benchmark.\n");
        return sb.ToString();
    }

    /// <summary>Explica por que uma atividade quase nao e escolhida, a partir das regras atuais.</summary>
    public static string Diagnose(RegionDefinition region, ActivityDefinition a, List<int> finalLevels)
    {
        var reasons = new List<string>();
        if (a.MinGroupSize > 1) reasons.Add($"exige grupo de {a.MinGroupSize}; grupos chegam no M6, então hoje ninguém pode fazer");
        if (a.Type == ActivityTypes.Recruit) reasons.Add("recrutamento depende do sistema de grupos (M6)");
        if (a.RequiredItemIds.Count > 0) reasons.Add("exige item; inventário chega no M5");
        if (a.ClassIds.Count > 0) reasons.Add($"só para {string.Join(", ", a.ClassIds)}");
        int minLevel = Math.Max(a.MinLevel, 1);
        if (minLevel > 1 && finalLevels.Count > 0)
        {
            double pct = finalLevels.Count(l => l >= minLevel) * 100.0 / finalLevels.Count;
            if (pct < 50) reasons.Add($"nível mínimo {minLevel}: só {pct.ToString("0", PtBr)}% dos personagens chegaram até o fim");
        }
        if (region.IsCity && a.Type == ActivityTypes.Quest)
            reasons.Add("pegar missão no quadro não dá XP (D-027); as quests estruturadas por cidade ainda não existem");
        else if (region.IsCity)
            reasons.Add("atividade da cidade: não dá XP e ainda não existe economia nem desgaste, então a nota não tem termo de recompensa");
        if (region.Pvp == PvpRules.Open) reasons.Add("zona PvP: o risco extra afasta quem tem aversão a risco");
        if (reasons.Count == 0) reasons.Add("pouco atraente na fórmula atual (rever na conferência da fórmula)");
        return string.Join("; ", reasons) + ".";
    }

    private static ActivityDefinition? ActivityOf(ContentDatabase content, string key) => Split(content, key).Activity;

    private static (RegionDefinition? Region, ActivityDefinition? Activity) Split(ContentDatabase content, string key)
    {
        int i = key.IndexOf('/');
        if (i < 0 || !content.Regions.TryGetValue(key[..i], out var r)) return (null, null);
        return (r, r.FindActivity(key[(i + 1)..]));
    }

    private static string StatusText(TargetStatus s) => s switch
    {
        TargetStatus.Inside => "✅ dentro",
        TargetStatus.Unstable => "⚠️ instável",
        TargetStatus.Below => "❌ abaixo",
        TargetStatus.Above => "❌ acima",
        _ => "— sem dados",
    };

    private static string TypeName(string type) => type switch
    {
        TargetMetrics.Travel => "Deslocamento",
        TargetMetrics.Idle => "Espera (sem opção viável)",
        ActivityTypes.Hunt => "Caça",
        ActivityTypes.Gather => "Coleta",
        ActivityTypes.Quest => "Missão",
        ActivityTypes.Explore => "Exploração",
        ActivityTypes.Trade => "Comércio",
        ActivityTypes.Repair => "Conserto",
        ActivityTypes.Train => "Treino",
        ActivityTypes.Socialize => "Socializar",
        ActivityTypes.Recruit => "Recrutar",
        ActivityTypes.Patrol => "Patrulha",
        ActivityTypes.Ambush => "Emboscada",
        ActivityTypes.Boss => "Chefe",
        _ => type,
    };

    private static double Avg(IEnumerable<double> values)
    {
        var v = values.Where(x => !double.IsNaN(x)).ToList();
        return v.Count == 0 ? double.NaN : v.Average();
    }

    private static double Percentile(List<double> v, double q) => ValidationCollector.Percentile(v, q);

    private static string Fmt(double v) => double.IsNaN(v) ? "—" : v.ToString(Math.Abs(v) >= 100 ? "N0" : "N1", PtBr);
    private static string Num(double v) => double.IsNaN(v) ? "" : v.ToString("0.###", Inv);
    private static string N(long v) => v.ToString("N0", PtBr);
    private static string Pct(long part, long total) => (part / (double)total).ToString("P1", PtBr);
    private static string Short(string hash) => hash.Length > 16 ? hash[..16] : hash;
    private static string Esc(string s) => s.Replace("|", "\\|");
}

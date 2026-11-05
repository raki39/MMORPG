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

        sb.Append("## Metas\n\n");
        sb.Append("As metas têm três classes (D-039a); o total \"x de y\" sozinho não diz se dá para avançar:\n\n");
        sb.Append("- **Regras do cenário:** rotina calibrada, medida numa rodada controlada com o humor neutralizado. Falhar aqui indica erro.\n");
        sb.Append("- **Provisórias:** balanceamento que será recalibrado (progressão, divisão do tempo, missões).\n");
        sb.Append("- **Dependem de sistemas futuros:** só acompanhamento, não reprovam (retenção, missões e coleta dependem de combate, economia e social).\n\n");
        foreach (var (kind, title) in new[] { (TargetKinds.Scenario, "Regras do cenário"), (TargetKinds.Provisional, "Provisórias"), (TargetKinds.Future, "Dependem de sistemas futuros (acompanhamento)") })
        {
            var group = outcomes.Where(o => o.Target.Kind == kind).ToList();
            if (group.Count == 0) continue;
            sb.Append($"### {title}: {group.Count(o => o.Status == TargetStatus.Inside)} de {group.Count} dentro\n\n");
            sb.Append("| Situação | Meta | Faixa | Média | Mín–máx entre seeds | Observação |\n|---|---|---|---:|---|---|\n");
            foreach (var o in group)
            {
                var t = o.Target;
                sb.Append($"| {StatusText(o.Status)} | {t.Name} | {Fmt(t.Min)}–{Fmt(t.Max)} {t.Unit} | {Fmt(o.Mean)} | {Fmt(o.Min)}–{Fmt(o.Max)} | {Esc(o.Notes)} |\n");
            }
            sb.Append("\n");
        }
        sb.Append("\nPor que cada faixa:\n\n");
        foreach (var t in targets.Targets.Where(t => t.Rationale.Length > 0))
            sb.Append($"- **{t.Name}:** {t.Rationale}\n");

        // ---- Perfis ----
        if (runs.All(r => r.ControlledProfiles.Count > 0))
        {
            sb.Append("\n## Rotina pura (rodada controlada, humor neutralizado)\n\n| Perfil | Sessões/semana | Horas/semana |\n|---|---:|---:|\n");
            foreach (var id in runs[0].ControlledProfiles.Select(p => p.Id))
            {
                var ps = runs.Select(r => r.ControlledProfiles.First(p => p.Id == id)).ToList();
                sb.Append($"| {ps[0].Name} | {Fmt(Avg(ps.Select(p => p.SessionsPerWeek)))} | {Fmt(Avg(ps.Select(p => p.HoursPerWeek)))} |\n");
            }
        }
        sb.Append("\n## Perfis na campanha completa (média das seeds)\n\n");
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

        // ---- Rotina por ocupacao (P2) ----
        if (runs[0].Occupations.Count > 0)
        {
            sb.Append("\n## Rotina por ocupação (média das seeds, campanha completa)\n\nObrigações e sono definem o tempo livre; a vontade de jogar decide se e quanto. Tempo livre medido na agenda dos últimos dias. Renda e apoio em reais por mês (sem efeito até o M7).\n\n");
            sb.Append("| Ocupação | Pessoas | Sessões/semana | Horas/semana | Livre em dia com obrigação | Livre em dia livre | Dias sem tempo | Renda | Apoio |\n|---|---:|---:|---:|---:|---:|---:|---:|---:|\n");
            foreach (var id in runs[0].Occupations.Select(o => o.Id))
            {
                var os = runs.Select(r => r.Occupations.First(o => o.Id == id)).ToList();
                string Min(Func<OccupationStats, double> f) { double v = Avg(os.Select(f)); return double.IsNaN(v) ? "—" : $"{v / 60:0.0} h"; }
                sb.Append($"| {os[0].Name} | {Fmt(os.Average(o => o.Persons))} | {Fmt(Avg(os.Select(o => o.SessionsPerWeek)))} | {Fmt(Avg(os.Select(o => o.HoursPerWeek)))} | " +
                          $"{Min(o => o.FreeMinutesBusyDay)} | {Min(o => o.FreeMinutesFreeDay)} | {Fmt(Avg(os.Select(o => o.NoFreeTimePct)))}% | R$ {Fmt(Avg(os.Select(o => o.MeanIncomeReais)))} | R$ {Fmt(Avg(os.Select(o => o.MeanSupportReais)))} |\n");
            }
            var dayReasons = new SortedDictionary<string, long>(StringComparer.Ordinal);
            foreach (var r in runs) foreach (var kv in r.DayReasons) dayReasons[kv.Key] = dayReasons.GetValueOrDefault(kv.Key) + kv.Value;
            long allDays = Math.Max(1, dayReasons.Values.Sum());
            sb.Append("\nComo terminaram os dias da agenda:\n\n| Dia | Parcela |\n|---|---:|\n");
            foreach (var kv in dayReasons.OrderByDescending(k => k.Value).ThenBy(k => k.Key, StringComparer.Ordinal))
                sb.Append($"| {DayReason(kv.Key)} | {Pct(kv.Value, allDays)} |\n");
        }

        // ---- Sono e dificuldade de parar (P3) ----
        if (runs[0].Occupations.Count > 0)
        {
            double SW(string key) => runs.Average(r => r.SleepWork.GetValueOrDefault(key));
            sb.Append("\n## Sono e dificuldade de parar (média das seeds, campanha completa)\n\n");
            sb.Append("Importância do jogo, tendência a prolongar e autocontrole são traços separados. A extensão tem limites explícitos (vezes por sessão, minutos, sono mínimo e teto do déficit); só corta sono no fim do dia, nunca uma obrigação. Sono cortado vira déficit; manhã livre e noites normais recuperam.\n\n");
            double ext = SW("ext_enjoying") + SW("ext_goal_close");
            sb.Append($"- Extensões na janela: {Fmt(ext)} ({Fmt(SW("ext_enjoying"))} gostando da sessão, {Fmt(SW("ext_goal_close"))} perto de concluir a meta); {Fmt(SW("ext_sleep_cut"))} avançaram sobre a hora de dormir.\n");
            double late = SW("late_nights");
            sb.Append($"- Noites encurtadas pelo jogo: {Fmt(late)}, {(late > 0 ? Fmt(SW("late_minutes") / late) : "—")} min a menos em média.\n\n");
            sb.Append("| Perfil | Extensões/semana | Sono cortado/semana | Déficit no fim | Cansados |\n|---|---:|---:|---:|---:|\n");
            foreach (var id in runs[0].Profiles.Select(p => p.Id))
            {
                var ps = runs.Select(r => r.Profiles.First(p => p.Id == id)).ToList();
                sb.Append($"| {ps[0].Name} | {Fmt(Avg(ps.Select(p => p.ExtensionsPerWeek)))} | {Fmt(Avg(ps.Select(p => p.SleepCutMinutesPerWeek)))} min | {Fmt(Avg(ps.Select(p => p.MeanDeficitMinutes)) / 60)} h | {Fmt(Avg(ps.Select(p => p.TiredPct)))}% |\n");
            }
            sb.Append("\nSono da noite anterior pela agenda (o sono cortado pelo jogo vai direto para o déficit). Em dia livre a pessoa dorme mais para recuperar. O déficit no fim depende do dia da semana em que a campanha termina.\n\n");
            sb.Append("| Ocupação | Precisa | Sono em dia com obrigação | Sono em dia livre | Déficit no fim | Cansados |\n|---|---:|---:|---:|---:|---:|\n");
            foreach (var id in runs[0].Occupations.Select(o => o.Id))
            {
                var os = runs.Select(r => r.Occupations.First(o => o.Id == id)).ToList();
                string H(Func<OccupationStats, double> f) { double v = Avg(os.Select(f)); return double.IsNaN(v) ? "—" : $"{Fmt(v / 60)} h"; }
                sb.Append($"| {os[0].Name} | {H(o => o.MeanSleepNeedMinutes)} | {H(o => o.MeanSleepBusyMinutes)} | {H(o => o.MeanSleepFreeMinutes)} | {H(o => o.MeanDeficitMinutes)} | {Fmt(Avg(os.Select(o => o.TiredPct)))}% |\n");
            }

            // ---- Jogar no horario de trabalho (D-044) ----
            sb.Append("\n## Jogar no horário de trabalho (média das seeds)\n\n");
            sb.Append("Só no trabalho remoto. Pausa: jogar no intervalo permitido. Escapada: jogar no expediente, rara, pela flexibilidade do trabalho, importância do jogo, falta de autocontrole e folga do dia; sessão curta e interrompível. Deslocamento, trabalho presencial e estudo ficam protegidos. Sem consequência profissional por enquanto.\n\n");
            double sessions = Math.Max(1, SW("sessions"));
            sb.Append($"- Sessões na pausa: {Fmt(SW("break_sessions"))} ({Pct((long)SW("break_sessions"), (long)sessions)} das sessões); escapadas: {Fmt(SW("slack_sessions"))} ({Pct((long)SW("slack_sessions"), (long)sessions)}).\n");
            sb.Append($"- Chamados do trabalho durante a escapada: {Fmt(SW("interrupt_finished_activity") + SW("interrupt_left_activity"))} ({Fmt(SW("interrupt_finished_activity"))} terminaram a atividade, {Fmt(SW("interrupt_left_activity"))} largaram e saíram).\n\n");
            sb.Append("| Ocupação | Pausa/semana | Escapadas/semana | Dias de trabalho com escapada |\n|---|---:|---:|---:|\n");
            foreach (var id in runs[0].Occupations.Select(o => o.Id))
            {
                if (!content.Occupations.TryGetValue(id, out var occ) || !occ.Blocks.Any(b => b.Remote)) continue;
                var os = runs.Select(r => r.Occupations.First(o => o.Id == id)).ToList();
                sb.Append($"| {os[0].Name} | {Fmt(Avg(os.Select(o => o.BreakSessionsPerWeek)))} | {Fmt(Avg(os.Select(o => o.SlackSessionsPerWeek)))} | {Fmt(Avg(os.Select(o => o.SlackDayPct)))}% |\n");
            }
        }

        // ---- Distribuicoes (R3) ----
        if (runs.All(r => r.Distribution.Sessions.Count > 0)) sb.Append(DistributionSection(content, runs));

        // ---- Localidades (P1) ----
        if (runs[0].Localities.Count > 0)
        {
            sb.Append("\n## Localidades (média das seeds)\n\nCada pessoa joga no horário e nos feriados de onde mora; o relógio do jogo é o do servidor. O início médio é uma média circular das horas (a meia-noite não distorce), então local e servidor diferem exatamente pelo fuso.\n\n");
            sb.Append("| Localidade | Fuso | Pessoas | Estrangeiros | Sessões | Início médio (local) | Início médio (servidor) |\n|---|---|---:|---:|---:|---:|---:|\n");
            foreach (var id in runs[0].Localities.Select(l => l.Id))
            {
                var ls = runs.Select(r => r.Localities.First(l => l.Id == id)).ToList();
                int off = ls[0].UtcOffsetMinutes;
                string tz = $"UTC{(off >= 0 ? "+" : "−")}{Math.Abs(off) / 60}";
                sb.Append($"| {ls[0].Name} | {tz} | {Fmt(ls.Average(l => l.Persons))} | {Fmt(ls.Average(l => l.ForeignNationals))} | {Fmt(ls.Average(l => (double)l.Sessions))} | {Hour(CircAvg(ls.Select(l => l.MeanLocalStartHour)))} | {Hour(CircAvg(ls.Select(l => l.MeanServerStartHour)))} |\n");
            }
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
        sb.Append("Caça, chefe, patrulha e emboscada ainda são atividades abstratas (tempo e XP, sem luta simulada) até o M3; aí o tempo se divide em viagem, procura, luta, recuperação e recompensa.\n\n");
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

    /// <summary>
    /// Distribuicoes (R3): referencia medida antes de mudar a vontade de jogar (R4). Junta as seeds
    /// (amostras somadas), na janela de medicao, campanha completa.
    /// </summary>
    private static string DistributionSection(ContentDatabase content, IReadOnlyList<RunMetrics> runs)
    {
        var sb = new StringBuilder();
        var all = runs.SelectMany(r => r.Distribution.Sessions).ToList();
        var normal = all.Where(s => s.Kind == "normal").ToList();
        var days = runs.SelectMany(r => r.Distribution.Days).ToList();
        double weeks = runs[0].Distribution.Weeks;
        string P(List<double> v, double q) => Fmt(ValidationCollector.Percentile(v, q));
        string ProfileName(string id) => content.Profiles.TryGetValue(id, out var pr) ? pr.Name : id;
        sb.Append("\n## Distribuições (R3)\n\n");
        sb.Append("Medição da campanha completa (com humor), seeds somadas, na janela de medição. A referência antes da R4 (modelo de vontade) está em `docs/baselines/pre-R4/`, com o mesmo relatório e o cenário controlado (pessoas iguais por célula perfil × ocupação), que separa o efeito das regras da mistura da população.\n\n");

        // Duracao das sessoes
        sb.Append("**Duração das sessões (minutos, sessões normais).** Inclui extensões e o fim real; pausa e escapada do trabalho à parte.\n\n");
        sb.Append("| Perfil | Sessões | p10 | p25 | Mediana | p75 | p90 | Máx. | Média |\n|---|---:|---:|---:|---:|---:|---:|---:|---:|\n");
        foreach (var g in normal.GroupBy(s => s.Profile).OrderBy(g => ProfileName(g.Key), StringComparer.Ordinal))
        {
            var v = g.Select(s => (double)s.Minutes).ToList();
            sb.Append($"| {ProfileName(g.Key)} | {N(v.Count)} | {P(v, 0.1)} | {P(v, 0.25)} | {P(v, 0.5)} | {P(v, 0.75)} | {P(v, 0.9)} | {Fmt(v.Max())} | {Fmt(v.Average())} |\n");
        }
        foreach (var (kind, label) in new[] { ("work_break", "Pausa no trabalho"), ("during_work", "Escapada no expediente") })
        {
            var v = all.Where(s => s.Kind == kind).Select(s => (double)s.Minutes).ToList();
            if (v.Count > 0) sb.Append($"| {label} | {N(v.Count)} | {P(v, 0.1)} | {P(v, 0.25)} | {P(v, 0.5)} | {P(v, 0.75)} | {P(v, 0.9)} | {Fmt(v.Max())} | {Fmt(v.Average())} |\n");
        }

        // Horas por semana: perfil x ocupacao
        var profiles = content.ProfilesOrdered.Select(p => p.Id).ToList();
        var persons = new Dictionary<(string, string), int>();
        foreach (var r in runs) foreach (var kv in r.Distribution.Persons) persons[kv.Key] = persons.GetValueOrDefault(kv.Key) + kv.Value;
        var minutesBy = all.GroupBy(s => (s.Profile, s.Occupation)).ToDictionary(g => g.Key, g => g.Sum(s => (double)s.Minutes));
        sb.Append("\n**Horas por semana, perfil × ocupação** (por pessoa registrada; \"—\" com menos de 10 pessoas na soma das seeds).\n\n");
        sb.Append("| Ocupação | " + string.Join(" | ", profiles.Select(ProfileName)) + " |\n|---|" + string.Concat(profiles.Select(_ => "---:|")) + "\n");
        foreach (var o in content.OccupationsOrdered)
        {
            sb.Append($"| {o.Name} |");
            foreach (var pr in profiles)
            {
                int n = persons.GetValueOrDefault((pr, o.Id));
                double h = n < 10 ? double.NaN : minutesBy.GetValueOrDefault((pr, o.Id)) / 60.0 / n / weeks;
                sb.Append($" {Fmt(h)} |");
            }
            sb.Append("\n");
        }

        // Sessoes por dia e intervalo
        // Agrupamentos por seed (a mesma pessoa e o mesmo dia existem em cada seed).
        var perDay = runs.SelectMany((r, i) => r.Distribution.Sessions.Select(x => (Run: i, S: x)))
            .GroupBy(t => (t.Run, t.S.PersonId, t.S.LocalDay)).Select(g => g.Select(t => t.S).ToList()).ToList();
        int one = perDay.Count(g => g.Count == 1), two = perDay.Count(g => g.Count == 2), more = perDay.Count(g => g.Count >= 3);
        var gaps = new List<double>();
        foreach (var g in perDay.Where(g => g.Count > 1))
        {
            var ordered = g.OrderBy(s => s.Start).ToList();
            for (int i = 1; i < ordered.Count; i++) gaps.Add((ordered[i].Start - ordered[i - 1].End) / (double)SimTime.TicksPerMinute);
        }
        sb.Append($"\n**Sessões por dia de jogo:** {Pct(one, perDay.Count)} com uma, {Pct(two, perDay.Count)} com duas, {Pct(more, perDay.Count)} com três ou mais. ");
        sb.Append(gaps.Count > 0 ? $"Intervalo entre sessões do mesmo dia: mediana {P(gaps, 0.5)} min, p90 {P(gaps, 0.9)} min.\n" : "\n");

        // Uso do tempo livre
        var playedByDay = runs.SelectMany((r, i) => r.Distribution.Sessions.Select(x => (Run: i, S: x)))
            .GroupBy(t => (t.Run, t.S.PersonId, t.S.LocalDay)).ToDictionary(g => g.Key, g => g.Sum(t => t.S.Minutes));
        var daysR = runs.SelectMany((r, i) => r.Distribution.Days.Select(d => (Run: i, D: d))).ToList();
        int Played((int Run, DaySample D) t) => playedByDay.GetValueOrDefault((t.Run, t.D.PersonId, t.D.LocalDay));
        string Use(IEnumerable<(int Run, DaySample D)> ds)
        {
            var list = ds.ToList();
            long free = list.Sum(t => (long)t.D.FreeMinutes);
            long played = list.Sum(t => (long)Played(t));
            return free == 0 ? "—" : Pct(played, free);
        }
        {
            // v3: voltas depois de pausas, motivos de inicio e de fim, e as sessoes mais longas com os motivos.
            var played = runs.SelectMany(r => r.Distribution.Days).Where(d => d.Reason == "played").ToList();
            if (played.Count > 0)
            {
                int withReturn = played.Count(d => d.Returns > 0), declined = played.Sum(d => d.DeclinedReturns), skippedWork = played.Sum(d => d.SkippedAfterWork);
                var bySittings = played.Where(d => d.Sittings > 0).GroupBy(d => Math.Min(3, d.Sittings)).ToDictionary(g => g.Key, g => g.Count());
                int withSitting = Math.Max(1, played.Count(d => d.Sittings > 0));
                sb.Append($"**Voltas depois de uma pausa (v3):** em {Pct(withReturn, played.Count)} dos dias com jogo. Sessões normais por dia com jogo: ");
                sb.Append($"uma {Pct(bySittings.GetValueOrDefault(1), withSitting)}, duas {Pct(bySittings.GetValueOrDefault(2), withSitting)}, três ou mais {Pct(bySittings.GetValueOrDefault(3), withSitting)}. ");
                sb.Append($"Voltas consideradas e recusadas: {declined.ToString("N0", PtBr)}. Sessão da noite deixada de lado depois de jogar no trabalho: {skippedWork.ToString("N0", PtBr)} dias.\n\n");
            }
            var normalS = runs.SelectMany(r => r.Distribution.Sessions).Where(x => x.Kind == "normal").ToList();
            if (normalS.Count > 0)
            {
                string StartPt(string r) => r switch { "usual_time" => "No horário de costume", "early_choice" => "Antes do horário, por vontade", "return" => "Voltou depois de uma pausa", "first" => "Primeira (cadastro ou volta ao jogo)", _ => "Outro" };
                string EndPt(string r) => r switch
                {
                    "satisfied" => "Decidiu parar (saciedade, cansaço, outras opções)", "window_end" => "Fim do tempo livre (obrigação, preparo, compromisso)",
                    "bedtime" => "Hora de dormir", "stayed_up" => "Depois de ficar acordado jogando", "frustration" => "Frustração", "server_closed" => "Servidor fechado",
                    "session_time" => "Fim planejado (sem decisão)", _ => r,
                };
                sb.Append("**Por que as sessões começaram e terminaram** (sessões normais, seeds somadas).\n\n| Início | Parcela | | Fim | Parcela |\n|---|---:|---|---|---:|\n");
                var starts = normalS.GroupBy(x => x.StartReason).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).ToList();
                var ends = normalS.GroupBy(x => x.EndReason).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).ToList();
                for (int i = 0; i < Math.Max(starts.Count, ends.Count); i++)
                {
                    string l = i < starts.Count ? $"{StartPt(starts[i].Key)} | {Pct(starts[i].Count(), normalS.Count)}" : " | ";
                    string r = i < ends.Count ? $"{EndPt(ends[i].Key)} | {Pct(ends[i].Count(), normalS.Count)}" : " | ";
                    sb.Append($"| {l} | | {r} |\n");
                }
                sb.Append('\n');
            }
            var longest = normalS.OrderByDescending(x => x.Minutes).ThenBy(x => x.PersonId).Take(8).ToList();
            if (longest.Count > 0)
            {
                sb.Append("**Sessões mais longas e por quê** (as 8 maiores sessões normais, seeds somadas). Resto: terminar a atividade ou a viagem.\n\n");
                sb.Append("| Perfil | Ocupação | Duração | Ao começar | Continuou | Ficou acordado | Resto | Disposição | Tempo livre do dia |\n|---|---|---:|---:|---:|---:|---:|---:|---:|\n");
                foreach (var x in longest)
                {
                    int rest = x.Minutes - x.Planned - x.Continued - x.ExtendedMinutes;
                    sb.Append($"| {ProfileName(x.Profile)} | {(content.Occupations.TryGetValue(x.Occupation, out var oc) ? oc.Name : x.Occupation)} | {x.Minutes} min | {x.Planned} min | {x.Continued} min | {x.ExtendedMinutes} min | {rest} min | ");
                    sb.Append($"{(x.Disposition >= 0 ? "+" : "−")}{Math.Abs(x.Disposition) / 10}% | {(x.FreeMinutes >= 0 ? Fmt(x.FreeMinutes / 60.0) + " h" : "—")} |\n");
                }
                sb.Append('\n');
            }
        }
        sb.Append("\n**Quanto do tempo livre vira jogo** (minutos jogados ÷ minutos livres da agenda; o jogo no trabalho e as extensões entram no jogado).\n\n| Perfil | Uso |\n|---|---:|\n");
        foreach (var pr in profiles) sb.Append($"| {ProfileName(pr)} | {Use(daysR.Where(t => t.D.Profile == pr))} |\n");
        sb.Append("\n| Ocupação | Uso |\n|---|---:|\n");
        foreach (var o in content.OccupationsOrdered) sb.Append($"| {o.Name} | {Use(daysR.Where(t => t.D.Occupation == o.Id))} |\n");

        // Composicao do tempo jogado
        double total = all.Sum(s => (double)s.Minutes);
        double ext = all.Sum(s => (double)s.ExtendedMinutes);
        double work = all.Where(s => s.Kind is "work_break" or "during_work").Sum(s => (double)s.Minutes);
        var cuts = runs.SelectMany(r => r.Distribution.SleepCutPerPeriod).Select(x => (double)x).ToList();
        sb.Append($"\n**Composição do tempo jogado:** {Fmt(total / 60)} h no total; extensões {Pct((long)ext, (long)total)}; pausa e escapada do trabalho {Pct((long)work, (long)total)}; sono cortado pelo jogo {Fmt(cuts.Sum() / 60)} h ({Pct((long)cuts.Sum(), (long)total)} do tempo jogado).\n");

        // Dias sem jogo
        var noPlay = days.Where(d => d.Sessions == 0).ToList();
        int workOnly = noPlay.Count(d => d.WorkSessions > 0);
        sb.Append($"\n**Dias sem sessão principal** ({Pct(noPlay.Count, days.Count)} dos dias da agenda):\n\n| Motivo | Parcela dos dias sem sessão |\n|---|---:|\n");
        foreach (var (key, label) in new[] { ("no_free_time", "Sem oportunidade (nenhuma janela comporta uma sessão)"), ("other_leisure", "Escolha: tinha tempo, preferiu outro lazer"), ("tired", "Cansaço"), ("low_engagement", "Desânimo (engajamento baixo)"), ("inactive", "Inativo") })
        {
            int n = noPlay.Count(d => d.Reason == key && d.WorkSessions == 0);
            if (n > 0) sb.Append($"| {label} | {Pct(n, noPlay.Count)} |\n");
        }
        if (workOnly > 0) sb.Append($"| Jogou só no horário de trabalho | {Pct(workOnly, noPlay.Count)} |\n");

        // Efeito separado do tempo livre
        sb.Append("\n**Efeito do tempo livre, separado** (dias da agenda por faixa de tempo livre): a frequência (chance de jogar) e a duração (minutos planejados quando joga) medidas à parte.\n\n");
        sb.Append("| Tempo livre no dia | Dias | Jogou | Duração planejada quando joga | Jogado por dia |\n|---|---:|---:|---:|---:|\n");
        foreach (var (lo, hi, label) in new[] { (0, 120, "até 2 h"), (120, 240, "2–4 h"), (240, 360, "4–6 h"), (360, 600, "6–10 h"), (600, 100000, "10 h ou mais") })
        {
            var b = daysR.Where(t => t.D.FreeMinutes >= lo && t.D.FreeMinutes < hi).ToList();
            if (b.Count == 0) continue;
            var withPlay = b.Where(t => t.D.Sessions > 0).ToList();
            double perDayPlayed = b.Average(t => (double)Played(t));
            sb.Append($"| {label} | {N(b.Count)} | {Pct(withPlay.Count, b.Count)} | {(withPlay.Count == 0 ? "—" : Fmt(withPlay.Average(t => (double)t.D.PlannedMinutes)) + " min")} | {Fmt(perDayPlayed)} min |\n");
        }

        // Corte de sono
        sb.Append("\n**Corte de sono (observações do criador, R2/R3).**\n\n");
        sb.Append("- **Onde pode cortar:** só a janela que termina **por causa do sono** avança sobre ele. Janela que termina por obrigação, preparo, deslocamento ou compromisso não avança (teste).\n");
        sb.Append("- **Teto compartilhado:** os 180 minutos por período de sono valem para a soma de todas as sessões que afetam aquele período (teste).\n");
        if (cuts.Count > 0)
            sb.Append($"- **Medido:** {N(cuts.Count)} períodos de sono encurtados pelo jogo; corte por período: mediana {P(cuts, 0.5)} min, p90 {P(cuts, 0.9)} min, máximo {Fmt(cuts.Max())} min.\n");
        return sb.ToString();
    }

    private static double Avg(IEnumerable<double> values)
    {
        var v = values.Where(x => !double.IsNaN(x)).ToList();
        return v.Count == 0 ? double.NaN : v.Average();
    }

    private static double Percentile(List<double> v, double q) => ValidationCollector.Percentile(v, q);

    private static string DayReason(string key) => key switch
    {
        "played" => "Jogou",
        "no_free_time" => "Sem tempo livre",
        "other_leisure" => "Tinha tempo, preferiu outro lazer",
        "low_engagement" => "Sem vontade (engajamento baixo)",
        "inactive" => "Inativo",
        "came_back" => "Voltou ao jogo",
        "tired" => "Cansado (dormiu pouco)",
        _ => key,
    };

    private static double CircAvg(IEnumerable<double> hours)
    {
        double x = 0, y = 0;
        int n = 0;
        foreach (var h in hours)
        {
            if (double.IsNaN(h)) continue;
            var (vx, vy) = ValidationCollector.ClockVector((int)Math.Round(h * 60));
            x += vx; y += vy; n++;
        }
        return n == 0 ? double.NaN : ValidationCollector.CircularMeanHour(x, y);
    }

    private static string Hour(double h)
    {
        if (double.IsNaN(h)) return "—";
        int m = (int)Math.Round(h * 60) % 1440;
        return $"{m / 60:00}h{m % 60:00}";
    }

    private static string Fmt(double v) => double.IsNaN(v) ? "—" : v.ToString(Math.Abs(v) >= 100 ? "N0" : "N1", PtBr);
    private static string Num(double v) => double.IsNaN(v) ? "" : v.ToString("0.######", Inv);   // CSV: precisao total para analise
    private static string N(long v) => v.ToString("N0", PtBr);
    private static string Pct(long part, long total) => (part / (double)total).ToString("P1", PtBr);
    private static string Short(string hash) => hash.Length > 16 ? hash[..16] : hash;
    private static string Esc(string s) => s.Replace("|", "\\|");
}

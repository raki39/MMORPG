using System.Globalization;
using System.Text;
using Simulation.Core.Population;

namespace Simulation.Infrastructure.Validation;

/// <summary>
/// Relatorio do cenario controlado (R4): em cada celula perfil x ocupacao, pessoas iguais (tracos
/// na media) e o humor neutro. Mostra o efeito das regras de rotina e de vontade sem a mistura
/// de perfis e ocupacoes da populacao; serve para comparar versoes das regras.
/// </summary>
public static class ControlledReport
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public sealed record CellResult(ControlledCell Cell, int Persons, double HoursPerWeek, double WorkHoursPerWeek, double PlayDaysPct,
        double OpportunityPct, double TwoSessionsPct, double FreeUsedPct, double SessionP50, double SessionP90, double SessionMax,
        double PlannedFreeDay, double PlannedBusyDay, int Sessions);

    public static IReadOnlyList<CellResult> Compute(IReadOnlyList<ControlledCell> cells, IReadOnlyList<DistributionData> runs)
    {
        var list = new List<CellResult>();
        foreach (var cell in cells)
        {
            bool In(string profile, string occ) => profile == cell.Profile && occ == cell.Occupation;
            var ses = runs.SelectMany((r, i) => r.Sessions.Where(s => In(s.Profile, s.Occupation)).Select(s => (Run: i, S: s))).ToList();
            var days = runs.SelectMany((r, i) => r.Days.Where(d => In(d.Profile, d.Occupation)).Select(d => (Run: i, D: d))).ToList();
            int persons = runs.Sum(r => r.Persons.GetValueOrDefault((cell.Profile, cell.Occupation)));
            double weeks = runs.Count > 0 ? runs[0].Weeks : 0;
            double perPersonWeek = persons > 0 && weeks > 0 ? persons * weeks : double.NaN;
            var normal = ses.Where(t => t.S.Kind == "normal").ToList();
            var minutes = normal.Select(t => (double)t.S.Minutes).ToList();
            double hours = ses.Sum(t => (double)t.S.Minutes) / 60.0 / perPersonWeek;
            double work = ses.Where(t => t.S.Kind != "normal").Sum(t => (double)t.S.Minutes) / 60.0 / perPersonWeek;
            var perDay = normal.GroupBy(t => (t.Run, t.S.PersonId, t.S.LocalDay)).Select(g => g.Count()).ToList();
            var played = ses.GroupBy(t => (t.Run, t.S.PersonId, t.S.LocalDay)).ToDictionary(g => g.Key, g => g.Sum(t => t.S.Minutes));
            long free = days.Sum(t => (long)t.D.FreeMinutes);
            long used = days.Sum(t => (long)played.GetValueOrDefault((t.Run, t.D.PersonId, t.D.LocalDay)));
            double Median(IEnumerable<double> v) { var l = v.ToList(); return l.Count == 0 ? double.NaN : ValidationCollector.Percentile(l, 0.5); }
            list.Add(new CellResult(cell, persons, hours, work,
                days.Count == 0 ? double.NaN : days.Count(t => t.D.Sessions > 0 || t.D.WorkSessions > 0) * 100.0 / days.Count,
                days.Count == 0 ? double.NaN : days.Count(t => t.D.Reason != "no_free_time") * 100.0 / days.Count,
                perDay.Count == 0 ? double.NaN : perDay.Count(c => c >= 2) * 100.0 / perDay.Count,
                free == 0 ? double.NaN : used * 100.0 / free,
                minutes.Count == 0 ? double.NaN : ValidationCollector.Percentile(minutes, 0.5),
                minutes.Count == 0 ? double.NaN : ValidationCollector.Percentile(minutes, 0.9),
                minutes.Count == 0 ? double.NaN : minutes.Max(),
                Median(days.Where(t => t.D.FreeDay && t.D.Sessions > 0).Select(t => (double)t.D.PlannedMinutes)),
                Median(days.Where(t => !t.D.FreeDay && t.D.Sessions > 0).Select(t => (double)t.D.PlannedMinutes)),
                normal.Count));
        }
        return list;
    }

    public static void Write(string outDir, IReadOnlyList<ControlledCell> cells, IReadOnlyList<ulong> seeds, int days, int perCell, string version,
        IReadOnlyList<DistributionData> runs)
    {
        Directory.CreateDirectory(outDir);
        var rows = Compute(cells, runs);
        string F(double v, string fmt = "0") => double.IsNaN(v) || double.IsInfinity(v) ? "—" : v.ToString(fmt, PtBr);
        var sb = new StringBuilder();
        sb.Append("# Cenário controlado\n\n");
        sb.Append($"Simulador {version} · seeds {string.Join(", ", seeds)} · {days} dias (7 de aquecimento) · {perCell} pessoas por célula.\n\n");
        sb.Append("Cada célula tem pessoas iguais: perfil e ocupação fixos, traços e rotina na média do conteúdo, residência na localidade do servidor. ");
        sb.Append("Humor neutro: sem extensões, inatividade nem desistência. O que muda entre as pessoas é só o sorteio do dia a dia, ");
        sb.Append("então as diferenças entre células e entre versões vêm das regras, não da mistura da população.\n\n");
        sb.Append("| Perfil | Ocupação | h/semana | no trabalho | dias com jogo | dias com oportunidade | 2+ sessões | tempo livre usado | sessão p50 | p90 | máx. | planejado dia livre | planejado dia útil |\n");
        sb.Append("|---|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|---:|\n");
        foreach (var r in rows)
            sb.Append($"| {r.Cell.Profile} | {r.Cell.Occupation} | {F(r.HoursPerWeek, "0.0")} | {F(r.WorkHoursPerWeek, "0.0")} | {F(r.PlayDaysPct, "0.0")}% | {F(r.OpportunityPct, "0.0")}% | " +
                      $"{F(r.TwoSessionsPct, "0.0")}% | {F(r.FreeUsedPct, "0.0")}% | {F(r.SessionP50)} | {F(r.SessionP90)} | {F(r.SessionMax)} | {F(r.PlannedFreeDay)} | {F(r.PlannedBusyDay)} |\n");
        sb.Append("\nh/semana: todo o tempo jogado por pessoa (inclui pausa e escapada do trabalho, coluna à parte). ");
        sb.Append("Dias e tempo livre: os últimos 30 dias da agenda. Sessão: só as sessões normais, em minutos. ");
        sb.Append("Planejado: mediana do total planejado nos dias com jogo.\n");
        File.WriteAllText(Path.Combine(outDir, "controlled.md"), sb.ToString(), new UTF8Encoding(false));

        var csv = new StringBuilder("profile,occupation,persons,hours_week,work_hours_week,play_days_pct,opportunity_pct,two_sessions_pct,free_used_pct,session_p50,session_p90,session_max,planned_free_day,planned_busy_day,sessions\n");
        string N(double v) => double.IsNaN(v) ? "" : v.ToString("0.######", Inv);
        foreach (var r in rows)
            csv.Append($"{r.Cell.Profile},{r.Cell.Occupation},{r.Persons},{N(r.HoursPerWeek)},{N(r.WorkHoursPerWeek)},{N(r.PlayDaysPct)},{N(r.OpportunityPct)},{N(r.TwoSessionsPct)},{N(r.FreeUsedPct)},{N(r.SessionP50)},{N(r.SessionP90)},{N(r.SessionMax)},{N(r.PlannedFreeDay)},{N(r.PlannedBusyDay)},{r.Sessions}\n");
        File.WriteAllText(Path.Combine(outDir, "controlled.csv"), csv.ToString(), new UTF8Encoding(false));
    }
}

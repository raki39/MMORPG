using System.Globalization;
using System.Text;
using Simulation.Core.Behavior;
using Simulation.Core.Content;

namespace Simulation.Infrastructure.Validation;

/// <summary>Linha da previa: um nivel da curva.</summary>
public sealed record ProgressionRow(int Level, long XpToNext, int StagePermille, long XpPerHour, double Hours, double CumulativeHours);

/// <summary>
/// Previa nominal da progressao (D-027): quanto tempo conectado cada nivel leva para um
/// personagem de referencia que caca no proprio nivel e passa uma parte fixa do tempo
/// conectado ganhando XP (o resto e viagem, cidade e organizacao). Nao substitui a medida
/// das execucoes; serve para ver o efeito de mudar curva, faixas e XP sem rodar dias.
/// </summary>
public static class ProgressionPreview
{
    public const int DefaultEfficiencyPermille = 600;
    public const string ReferenceType = ActivityTypes.Hunt;

    public static List<ProgressionRow> Build(ContentDatabase content, int efficiencyPermille = DefaultEfficiencyPermille)
    {
        var p = content.Progression;
        var rows = new List<ProgressionRow>();
        double total = 0;
        for (int level = 1; level < p.LevelCap; level++)
        {
            // Atividade de referencia: caca com desafio igual ao nivel.
            var reference = new ActivityDefinition { Type = ReferenceType, ChallengeLevel = level };
            long milliPerMin = ProgressionRules.MilliXpPerMinute(content, reference, level);
            long perHour = milliPerMin * 60 * efficiencyPermille / 1000 / 1000;
            long need = ProgressionRules.XpToNextLevel(p, level);
            double hours = perHour > 0 ? need / (double)perHour : double.PositiveInfinity;
            total += hours;
            rows.Add(new ProgressionRow(level, need, ProgressionRules.StageRate(p, level), perHour, hours, total));
        }
        return rows;
    }

    public static string ToMarkdown(ContentDatabase content, int efficiencyPermille = DefaultEfficiencyPermille)
    {
        var br = CultureInfo.GetCultureInfo("pt-BR");
        var p = content.Progression;
        var sb = new StringBuilder();
        sb.AppendLine("# Prévia da progressão (nominal)");
        sb.AppendLine();
        sb.AppendLine($"- Teto: nível {p.LevelCap}. Curva: XP para sair do nível n = {Curve(p.XpCurve)}.");
        sb.AppendLine($"- Personagem de referência: caça no próprio nível e ganha XP em {efficiencyPermille / 10}% do tempo conectado (o resto é viagem, cidade e organização).");
        sb.AppendLine("- É uma estimativa para calibrar, não a medida da simulação; a medida real sai de `--seeds` (validation.md).");
        sb.AppendLine();
        sb.AppendLine("| Nível | XP para subir | Taxa da faixa | XP por hora conectada | Tempo no nível | Total conectado |");
        sb.AppendLine("|---:|---:|---:|---:|---:|---:|");
        foreach (var r in Build(content, efficiencyPermille))
            sb.AppendLine($"| {r.Level} → {r.Level + 1} | {r.XpToNext.ToString("N0", br)} | ×{(r.StagePermille / 1000.0).ToString("0.##", br)} | {r.XpPerHour.ToString("N0", br)} | {Duration(r.Hours, br)} | {r.CumulativeHours.ToString("0.0", br)} h |");
        return sb.ToString();
    }

    private static string Curve(XpCurveDefinition c)
    {
        var parts = new List<string>();
        if (c.Cubic != 0) parts.Add($"{c.Cubic}·n³");
        if (c.Quadratic != 0) parts.Add($"{c.Quadratic}·n²");
        if (c.Linear != 0) parts.Add($"{c.Linear}·n");
        if (c.Constant != 0) parts.Add(c.Constant.ToString(CultureInfo.InvariantCulture));
        return parts.Count == 0 ? "0" : string.Join(" + ", parts);
    }

    private static string Duration(double hours, CultureInfo br) =>
        double.IsInfinity(hours) ? "—" : hours < 1 ? $"{Math.Round(hours * 60)} min" : $"{hours.ToString("0.0", br)} h";
}

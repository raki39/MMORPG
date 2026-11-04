using Simulation.Core.Content;
using Simulation.Core.Time;

namespace GodotClient.Ui;

/// <summary>Formatacao pt-BR independente da cultura do sistema.</summary>
public static class PtBr
{
    private static readonly string[] WeekDays = { "dom.", "seg.", "ter.", "qua.", "qui.", "sex.", "sáb." };

    public static string DateTime(System.DateTime d) =>
        $"{WeekDays[(int)d.DayOfWeek]} {d.Day:00}/{d.Month:00}/{d.Year} · {d.Hour:00}:{d.Minute:00}";

    public static string ShortDate(System.DateTime d) => $"{WeekDays[(int)d.DayOfWeek]} {d.Day:00}/{d.Month:00}";

    public static string Function(string f) => f switch
    {
        ZoneFunctions.City => "Cidade",
        ZoneFunctions.Hunting => "Zona de caça",
        _ => f,
    };

    public static string Death(string d) => d switch
    {
        DeathRules.None => "Sem perda",
        DeathRules.Partial => "Perda parcial",
        DeathRules.FullLoot => "Full loot",
        _ => d,
    };

    public static string Control(string c) => c switch
    {
        ControlRules.Neutral => "Neutra",
        ControlRules.Contestable => "Disputável",
        _ => c,
    };

    public static string Date(System.DateTime d) => $"{WeekDays[(int)d.DayOfWeek]} {d.Day:00}/{d.Month:00}/{d.Year}";

    public static string Biome(string biome) => biome switch
    {
        "planicie" => "Planície",
        "floresta" => "Floresta",
        "caverna" => "Caverna",
        "ruinas" => "Ruínas",
        _ => biome,
    };

    public static string Int(long v)
    {
        var s = Math.Abs(v).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var parts = new List<string>();
        for (int i = s.Length; i > 0; i -= 3) parts.Insert(0, s.Substring(Math.Max(0, i - 3), Math.Min(3, i)));
        return (v < 0 ? "-" : "") + string.Join(".", parts);
    }

    public static string Decimal(double v, int places = 1) =>
        v.ToString("F" + places, System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');

    public static string DayKind(Simulation.Core.Time.DayKind k) => k switch
    {
        Simulation.Core.Time.DayKind.Weekend => "fim de semana",
        Simulation.Core.Time.DayKind.Holiday => "feriado",
        _ => "dia útil",
    };

    public static string Pvp(string rule) => rule switch
    {
        PvpRules.None => "Sem PvP",
        PvpRules.Consensual => "PvP consensual",
        PvpRules.Open => "PvP aberto",
        _ => rule,
    };

    public static string ActivityType(string type) => type switch
    {
        ActivityTypes.Hunt => "Caça",
        ActivityTypes.Gather => "Coleta",
        ActivityTypes.Boss => "Boss",
        ActivityTypes.Quest => "Missão",
        ActivityTypes.Explore => "Exploração",
        ActivityTypes.Trade => "Comércio",
        ActivityTypes.Repair => "Reparo",
        ActivityTypes.Train => "Treino",
        ActivityTypes.Socialize => "Social",
        ActivityTypes.Recruit => "Recrutamento",
        ActivityTypes.Patrol => "Patrulha",
        ActivityTypes.Ambush => "Emboscada",
        _ => type,
    };
}

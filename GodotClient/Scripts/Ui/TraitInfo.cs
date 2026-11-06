using Godot;
using Simulation.Core.Content;
using Simulation.Core.Population;

namespace GodotClient.Ui;

/// <summary>
/// Personalidade principal (P4a) e traits (P4b) na interface: icones padronizados, cores,
/// textos de situacao e o emblema. Um icone Lucide por personalidade e por trait, sem repetir.
/// </summary>
public static class TraitInfo
{
    /// <summary>Icone de cada personalidade principal (13, todos diferentes).</summary>
    public static string PersonalityIcon(string id) => id switch
    {
        "sociavel" => "users",
        "reservado" => "user",
        "agressivo" => "flame",
        "pacifico" => "bird",
        "paciente" => "hourglass",
        "inquieto" => "zap",
        "leal" => "anchor",
        "independente" => "signpost",
        "cauteloso" => "shield",
        "destemido" => "mountain-snow",
        "versatil" => "shuffle",
        "metodico" => "repeat",
        _ => "scale",   // Equilibrado
    };

    /// <summary>Icone de cada trait (30, todos diferentes e diferentes dos das personalidades).</summary>
    public static string QuirkIcon(QuirkDefinition? q) => q?.Id switch
    {
        "persistente" => "biceps-flexed",
        "impulsivo" => "dices",
        "colecionador" => "gem",
        "otimizador" => "gauge",
        "resiliente" => "sprout",
        "pavio_curto" => "bomb",
        "saudosista" => "history",
        "rancoroso" => "angry",
        "estudioso" => "graduation-cap",
        "esquecido" => "eraser",
        "memoria_firme" => "brain",
        "observador" => "binoculars",
        "indeciso" => "circle-help",
        "maratonista" => "infinity",
        "procrastinador" => "snail",
        "protetor_novatos" => "shield-plus",
        "competidor_honrado" => "handshake",
        "provocador" => "megaphone",
        "lider_nato" => "crown",
        "companheiro_fiel" => "user-check",
        "lobo_solitario" => "user-x",
        "mercador_cauteloso" => "piggy-bank",
        "cacador_barganhas" => "badge-percent",
        "gastador_compulsivo" => "shopping-cart",
        "acumulador" => "archive",
        "frio_sob_pressao" => "snowflake",
        "nervoso_pvp" => "siren",
        "artesao_meticuloso" => "ruler",
        "traumatizado" => "ghost",
        "matador_novatos" => "crosshair",
        _ => q?.Polarity == QuirkPolarities.Negative ? "circle-alert" : "sparkles",   // trait nova no conteudo
    };

    public static bool IsPositive(QuirkDefinition q) => q.Polarity == QuirkPolarities.Positive;
    public static Color PolarityColor(QuirkDefinition q) => IsPositive(q) ? UiTokens.Success : UiTokens.Danger;
    public static string PolarityLabel(QuirkDefinition q) => IsPositive(q) ? "Positiva" : "Negativa";

    /// <summary>Nome do sistema de que o efeito depende.</summary>
    public static string SystemName(string system) => system switch
    {
        QuirkSystems.Combat => "combate (M3)",
        QuirkSystems.Economy => "economia (M5)",
        QuirkSystems.Groups => "grupos (M6)",
        QuirkSystems.Pvp => "PvP (Etapa 3)",
        QuirkSystems.Crafting => "fabricação",
        _ => "",
    };

    /// <summary>
    /// Situacao do efeito (P4b: todos desligados). Efeitos da pessoa entram na P4c; os demais com
    /// o sistema de que dependem.
    /// </summary>
    public static string Status(QuirkDefinition q) => q.System.Length == 0
        ? "Efeito a partir da P4c"
        : $"Sem efeito até {SystemShort(q.System)}";

    private static string SystemShort(string system) => system switch
    {
        QuirkSystems.Combat => "o M3",
        QuirkSystems.Economy => "o M5",
        QuirkSystems.Groups => "o M6",
        QuirkSystems.Pvp => "a Etapa 3",
        QuirkSystems.Crafting => "a fabricação",
        _ => "",
    };

    private static string AttributeName(string key) => key switch
    {
        "selfControl" => "Autocontrole",
        "extendTendency" => "Tendência a prolongar",
        _ => AttributeInfo.ByKey.TryGetValue(key, out var e) ? e.Name : key,
    };

    /// <summary>"Mais provavel com" e "menos provavel com", a partir das afinidades do conteudo.</summary>
    public static (List<string> More, List<string> Less) Affinities(ContentDatabase c, QuirkDefinition q)
    {
        var more = new List<string>();
        var less = new List<string>();
        foreach (var a in q.Affinities)
        {
            int dot = a.Key.IndexOf('.');
            string kind = dot > 0 ? a.Key[..dot] : "", key = dot > 0 ? a.Key[(dot + 1)..] : a.Key;
            string text = kind switch
            {
                "traits" => $"{AttributeName(key)} {(a.Direction == "low" ? "baixa" : "alta")}",
                "preferences" => $"preferência {(PreferenceInfo.ByKey.TryGetValue(key, out var pr) ? pr.Name : key)} {(a.Direction == "low" ? "baixa" : "alta")}",
                "profile" => $"perfil {(c.Profiles.TryGetValue(key, out var pf) ? pf.Name : key)}",
                "personality" => $"personalidade {PlayerModal.PersonalityOf(c, key).Label}",
                _ => a.Key,
            };
            (a.StrengthPermille >= 0 ? more : less).Add(text);
        }
        return (more, less);
    }

    /// <summary>Tooltip estruturado de uma trait.</summary>
    public static string Tooltip(ContentDatabase c, QuirkDefinition q)
    {
        var (more, less) = Affinities(c, q);
        var lines = new List<string>
        {
            $"{q.Name}|{PolarityLabel(q)}|#{PolarityColor(q).ToHtml(false)}",
            $"^{(q.Acquisition == QuirkAcquisitions.Acquired ? "Traço adquirido" : "Traço de nascença")}",
            "#Efeito",
            q.Effect,
        };
        if (q.Tradeoff.Length > 0) { lines.Add("#Contrapartida"); lines.Add(q.Tradeoff); }
        lines.Add("#Situação");
        lines.Add(q.System.Length == 0
            ? "Desligado nesta versão: os efeitos dos traços entram na P4c. O traço já existe e não muda a simulação."
            : $"Depende de {SystemName(q.System)}. Até lá o traço existe, mas não muda a simulação.");
        if (more.Count > 0) lines.Add("~Mais provável com " + string.Join(", ", more) + ".");
        if (less.Count > 0) lines.Add("~Menos provável com " + string.Join(", ", less) + ".");
        if (q.Excludes.Count > 0)
            lines.Add("~Não aparece junto com " + string.Join(", ", q.Excludes.Select(x => c.Quirks.TryGetValue(x, out var o) ? o.Name : x)) + ".");
        if (q.ExcludesPersonalities.Count > 0)
            lines.Add("~Não aparece em quem é " + string.Join(", ", q.ExcludesPersonalities.Select(x => PlayerModal.PersonalityOf(c, x).Label)) + ".");
        return string.Join("\n", lines);
    }
}

/// <summary>
/// Emblema padronizado de personalidade e de trait: mesmo traco de icone (Lucide 1,6 px), mesma
/// grade e o mesmo desenho em todo o jogo.
/// - Personalidade: circulo dourado.
/// - Trait: quadrado arredondado, verde (positiva) ou vermelho (negativa).
/// - Sem efeito ainda (Pending): borda e icone esmaecidos.
/// Desenho estatico (sem redesenho por quadro).
/// </summary>
public partial class TraitBadge : Control
{
    private readonly string _icon;
    private readonly Color _color;
    private readonly bool _round;
    private readonly bool _pending;
    private readonly int _size;

    public TraitBadge(string icon, Color color, int size, bool round = false, bool pending = false)
    {
        _icon = icon;
        _color = color;
        _round = round;
        _pending = pending;
        _size = size;
        CustomMinimumSize = new Vector2(size, size);
        SizeFlagsVertical = SizeFlags.ShrinkBegin;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        float s = _size;
        float fillA = _pending ? 0.07f : 0.14f, borderA = _pending ? 0.38f : 0.75f, iconA = _pending ? 0.6f : 1f;
        var fill = new Color(_color, fillA);
        var border = new Color(_color, borderA);
        if (_round)
        {
            var c = new Vector2(s / 2f, s / 2f);
            DrawCircle(c, s / 2f - 0.5f, fill);
            DrawArc(c, s / 2f - 1f, 0, Mathf.Tau, 48, border, 1.5f, true);
        }
        else
        {
            var box = new StyleBoxFlat { BgColor = fill, BorderColor = border, AntiAliasing = true };
            box.SetBorderWidthAll(1);
            box.SetCornerRadiusAll(Mathf.RoundToInt(s * 0.22f));
            DrawStyleBox(box, new Rect2(0, 0, s, s));
        }
        int iconSize = Mathf.RoundToInt(s * (_round ? 0.5f : 0.55f));
        var tex = Icons.Get(_icon, iconSize);
        float o = Mathf.Round((s - iconSize) / 2f);
        DrawTextureRect(tex, new Rect2(o, o, iconSize, iconSize), false, new Color(_color, iconA));
    }
}

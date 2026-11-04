using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Monta o Theme global a partir dos tokens. Componentes novos registram estilos aqui,
/// com estados normal, hover, foco, pressionado/selecionado e desabilitado.
/// </summary>
public static class ThemeFactory
{
    public static Theme Build()
    {
        var t = new Theme { DefaultFont = UiFonts.Regular, DefaultFontSize = UiTokens.FontBody };

        // Paineis: vidro escuro com borda fina e sombra suave.
        t.SetStylebox("panel", "PanelContainer", Glass(UiTokens.S16));
        t.SetStylebox("panel", "Panel", Box(UiTokens.Surface, 0));

        // Texto
        t.SetColor("font_color", "Label", UiTokens.TextPrimary);
        t.SetColor("font_shadow_color", "Label", new Color(0, 0, 0, 0));

        // Botoes
        t.SetFont("font", "Button", UiFonts.Medium);
        t.SetStylebox("normal", "Button", Box(UiTokens.SurfaceRaised, UiTokens.S12, border: UiTokens.Border));
        t.SetStylebox("hover", "Button", Box(UiTokens.SurfaceHover, UiTokens.S12, border: UiTokens.BorderStrong));
        t.SetStylebox("pressed", "Button", Box(UiTokens.Accent, UiTokens.S12, border: UiTokens.AccentSoft));
        t.SetStylebox("hover_pressed", "Button", Box(UiTokens.AccentSoft, UiTokens.S12, border: UiTokens.AccentSoft));
        t.SetStylebox("disabled", "Button", Box(new Color(UiTokens.SurfaceRaised, 0.5f), UiTokens.S12, border: UiTokens.Border));
        t.SetStylebox("focus", "Button", Focus());
        t.SetColor("font_color", "Button", UiTokens.TextPrimary);
        t.SetColor("font_hover_color", "Button", UiTokens.TextPrimary);
        t.SetColor("font_pressed_color", "Button", UiTokens.Background);
        t.SetColor("font_hover_pressed_color", "Button", UiTokens.Background);
        t.SetColor("font_focus_color", "Button", UiTokens.TextPrimary);
        t.SetColor("font_disabled_color", "Button", UiTokens.TextDisabled);

        // Barras de atributo
        var barBg = Box(new Color(UiTokens.Background, 0.9f), 0, radius: 4);
        barBg.ContentMarginTop = barBg.ContentMarginBottom = 0;
        t.SetStylebox("background", "ProgressBar", barBg);
        var barFill = Box(UiTokens.Accent, 0, radius: 4);
        barFill.ContentMarginTop = barFill.ContentMarginBottom = 0;
        t.SetStylebox("fill", "ProgressBar", barFill);

        // Rolagem fina
        t.SetStylebox("panel", "ScrollContainer", new StyleBoxEmpty());
        var grabber = Box(UiTokens.BorderStrong, 0, radius: 4);
        t.SetStylebox("grabber", "VScrollBar", grabber);
        t.SetStylebox("grabber_highlight", "VScrollBar", Box(UiTokens.TextSecondary, 0, radius: 4));
        t.SetStylebox("grabber_pressed", "VScrollBar", Box(UiTokens.Accent, 0, radius: 4));
        var track = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), ContentMarginLeft = 3, ContentMarginRight = 3 };
        t.SetStylebox("scroll", "VScrollBar", track);

        // Separadores
        t.SetStylebox("separator", "HSeparator", new StyleBoxLine { Color = UiTokens.Border, Thickness = 1 });
        t.SetConstant("separation", "HSeparator", UiTokens.S16);

        // Campos de formulario
        var field = Box(new Color(UiTokens.Background, 0.85f), 10, border: UiTokens.BorderStrong, radius: 7);
        field.ContentMarginTop = field.ContentMarginBottom = 7;
        var fieldFocus = Box(new Color(UiTokens.Background, 0.95f), 10, border: UiTokens.Accent, radius: 7);
        fieldFocus.ContentMarginTop = fieldFocus.ContentMarginBottom = 7;
        t.SetStylebox("normal", "LineEdit", field);
        t.SetStylebox("focus", "LineEdit", fieldFocus);
        t.SetStylebox("read_only", "LineEdit", Box(new Color(UiTokens.Background, 0.5f), 10, border: UiTokens.Border, radius: 7));
        t.SetColor("font_color", "LineEdit", UiTokens.TextPrimary);
        t.SetColor("font_placeholder_color", "LineEdit", UiTokens.TextDisabled);
        t.SetColor("caret_color", "LineEdit", UiTokens.Accent);
        t.SetColor("selection_color", "LineEdit", new Color(UiTokens.Accent, 0.35f));
        t.SetStylebox("normal", "OptionButton", field);
        t.SetStylebox("hover", "OptionButton", Box(UiTokens.SurfaceHover, 10, border: UiTokens.BorderStrong, radius: 7));
        t.SetStylebox("pressed", "OptionButton", fieldFocus);
        t.SetStylebox("focus", "OptionButton", fieldFocus);
        t.SetColor("font_color", "OptionButton", UiTokens.TextPrimary);
        t.SetColor("font_hover_color", "OptionButton", UiTokens.TextPrimary);
        t.SetColor("font_pressed_color", "OptionButton", UiTokens.TextPrimary);
        var popup = Box(UiTokens.SurfaceRaised, 6, border: UiTokens.BorderStrong, radius: 8);
        t.SetStylebox("panel", "PopupMenu", popup);
        t.SetStylebox("hover", "PopupMenu", Box(new Color(UiTokens.Accent, 0.25f), 6, radius: 6));
        t.SetColor("font_color", "PopupMenu", UiTokens.TextPrimary);
        t.SetColor("font_hover_color", "PopupMenu", UiTokens.TextPrimary);
        t.SetColor("font_color", "CheckBox", UiTokens.TextPrimary);
        t.SetColor("font_hover_color", "CheckBox", UiTokens.TextPrimary);
        t.SetColor("font_pressed_color", "CheckBox", UiTokens.TextPrimary);
        t.SetStylebox("normal", "CheckBox", new StyleBoxEmpty());
        t.SetStylebox("hover", "CheckBox", new StyleBoxEmpty());
        t.SetStylebox("pressed", "CheckBox", new StyleBoxEmpty());
        t.SetStylebox("hover_pressed", "CheckBox", new StyleBoxEmpty());
        t.SetStylebox("focus", "CheckBox", new StyleBoxEmpty());

        // Tooltip
        t.SetStylebox("panel", "TooltipPanel", Box(UiTokens.SurfaceRaised, UiTokens.S12, border: UiTokens.BorderStrong));
        t.SetColor("font_color", "TooltipLabel", UiTokens.TextPrimary);
        t.SetFont("font", "TooltipLabel", UiFonts.Regular);
        t.SetFontSize("font_size", "TooltipLabel", UiTokens.FontSmall);
        return t;
    }

    public static StyleBoxFlat Box(Color bg, int padding, Color? border = null, int radius = UiTokens.CornerRadius)
    {
        var sb = new StyleBoxFlat
        {
            BgColor = bg,
            ContentMarginLeft = padding,
            ContentMarginRight = padding,
            ContentMarginTop = padding * 0.55f,
            ContentMarginBottom = padding * 0.55f,
            AntiAliasing = true,
        };
        sb.SetCornerRadiusAll(radius);
        if (border.HasValue)
        {
            sb.BorderColor = border.Value;
            sb.SetBorderWidthAll(1);
        }
        return sb;
    }

    /// <summary>Painel flutuante: vidro escuro, borda fina, sombra.</summary>
    public static StyleBoxFlat Glass(int padding, int radius = UiTokens.PanelRadius)
    {
        var sb = Box(UiTokens.SurfaceGlass, padding, border: UiTokens.Border, radius: radius);
        sb.ContentMarginTop = padding;
        sb.ContentMarginBottom = padding;
        sb.ShadowColor = UiTokens.Shadow;
        sb.ShadowSize = 18;
        sb.ShadowOffset = new Vector2(0, 6);
        return sb;
    }

    private static StyleBoxFlat Focus()
    {
        var sb = new StyleBoxFlat { DrawCenter = false, BorderColor = UiTokens.AccentSoft, AntiAliasing = true };
        sb.SetBorderWidthAll(2);
        sb.SetCornerRadiusAll(UiTokens.CornerRadius);
        return sb;
    }

    /// <summary>Etiqueta compacta (chip) com fundo translucido na cor indicada.</summary>
    public static PanelContainer Chip(string text, Color color, bool dot = false)
    {
        var chip = new PanelContainer();
        var sb = Box(new Color(color, 0.14f), 10, border: new Color(color, 0.45f), radius: 999);
        sb.ContentMarginTop = 3;
        sb.ContentMarginBottom = 3;
        chip.AddThemeStyleboxOverride("panel", sb);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        if (dot)
        {
            row.AddChild(new DotIcon(color) { CustomMinimumSize = new Vector2(9, 9), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
        }
        var label = Text(text, UiTokens.FontCaption, color, UiFonts.SemiBold);
        label.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(label);
        chip.AddChild(row);
        return chip;
    }

    public static Label Text(string text, int size = UiTokens.FontBody, Color? color = null, Font? font = null)
    {
        var l = new Label { Text = text };
        l.AddThemeFontSizeOverride("font_size", size);
        if (font != null) l.AddThemeFontOverride("font", font);
        if (color.HasValue) l.AddThemeColorOverride("font_color", color.Value);
        return l;
    }

    /// <summary>Titulo de secao: caixa alta, pequeno, espacado.</summary>
    public static Label Section(string text)
    {
        var l = Text(text.ToUpperInvariant(), UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.SemiBold);
        return l;
    }
}

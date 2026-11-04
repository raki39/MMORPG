using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Tema global compacto (D-022): controles de 28 px, cantos de 3 px, bordas finas,
/// rotulos condensados em caixa alta, numeros tabulares. Nada de pilulas ou botoes gigantes.
/// </summary>
public static class ThemeFactory
{
    public static Theme Build()
    {
        var t = new Theme { DefaultFont = UiFonts.Regular, DefaultFontSize = UiTokens.FontBody };

        t.SetStylebox("panel", "PanelContainer", Panel(10));
        t.SetStylebox("panel", "Panel", Box(UiTokens.Surface, 0));
        t.SetColor("font_color", "Label", UiTokens.TextPrimary);

        // Botoes compactos
        t.SetFont("font", "Button", UiFonts.SemiBold);
        t.SetFontSize("font_size", "Button", UiTokens.FontSmall);
        t.SetStylebox("normal", "Button", Btn(UiTokens.SurfaceRaised, UiTokens.BorderStrong));
        t.SetStylebox("hover", "Button", Btn(UiTokens.SurfaceHover, UiTokens.AccentSoft));
        t.SetStylebox("pressed", "Button", Btn(new Color(UiTokens.Accent, 0.22f), UiTokens.Accent));
        t.SetStylebox("hover_pressed", "Button", Btn(new Color(UiTokens.Accent, 0.30f), UiTokens.AccentSoft));
        t.SetStylebox("disabled", "Button", Btn(new Color(UiTokens.SurfaceRaised, 0.45f), UiTokens.Border));
        t.SetStylebox("focus", "Button", new StyleBoxEmpty());
        t.SetColor("font_color", "Button", UiTokens.TextPrimary);
        t.SetColor("font_hover_color", "Button", UiTokens.TextPrimary);
        t.SetColor("font_pressed_color", "Button", UiTokens.AccentSoft);
        t.SetColor("font_hover_pressed_color", "Button", UiTokens.AccentSoft);
        t.SetColor("font_disabled_color", "Button", UiTokens.TextDisabled);

        // Barras
        var barBg = Box(new Color(UiTokens.Background, 0.9f), 0, radius: 1);
        barBg.ContentMarginTop = barBg.ContentMarginBottom = 0;
        t.SetStylebox("background", "ProgressBar", barBg);
        var barFill = Box(UiTokens.Accent, 0, radius: 1);
        barFill.ContentMarginTop = barFill.ContentMarginBottom = 0;
        t.SetStylebox("fill", "ProgressBar", barFill);

        // Rolagem fina
        t.SetStylebox("panel", "ScrollContainer", new StyleBoxEmpty());
        t.SetStylebox("grabber", "VScrollBar", Box(UiTokens.BorderStrong, 0, radius: 2));
        t.SetStylebox("grabber_highlight", "VScrollBar", Box(UiTokens.TextSecondary, 0, radius: 2));
        t.SetStylebox("grabber_pressed", "VScrollBar", Box(UiTokens.Accent, 0, radius: 2));
        t.SetStylebox("scroll", "VScrollBar", new StyleBoxFlat { BgColor = new Color(1, 1, 1, 0.02f), ContentMarginLeft = 3, ContentMarginRight = 3, CornerRadiusTopLeft = 4, CornerRadiusTopRight = 4, CornerRadiusBottomLeft = 4, CornerRadiusBottomRight = 4 });

        t.SetStylebox("separator", "HSeparator", new StyleBoxLine { Color = UiTokens.Border, Thickness = 1 });
        t.SetConstant("separation", "HSeparator", 10);

        // Campos
        var field = Box(UiTokens.Background, 8, border: UiTokens.BorderStrong, radius: 3);
        field.ContentMarginTop = field.ContentMarginBottom = 4;
        var fieldFocus = Box(UiTokens.Background, 8, border: UiTokens.Accent, radius: 3);
        fieldFocus.ContentMarginTop = fieldFocus.ContentMarginBottom = 4;
        foreach (var type in new[] { "LineEdit", "OptionButton" })
        {
            t.SetStylebox("normal", type, field);
            t.SetStylebox("focus", type, fieldFocus);
            t.SetColor("font_color", type, UiTokens.TextPrimary);
            t.SetFontSize("font_size", type, UiTokens.FontSmall);
        }
        t.SetStylebox("hover", "OptionButton", Box(UiTokens.SurfaceHover, 8, border: UiTokens.BorderStrong, radius: 3));
        t.SetStylebox("pressed", "OptionButton", fieldFocus);
        t.SetColor("font_hover_color", "OptionButton", UiTokens.TextPrimary);
        t.SetColor("font_placeholder_color", "LineEdit", UiTokens.TextDisabled);
        t.SetColor("caret_color", "LineEdit", UiTokens.Accent);
        t.SetColor("selection_color", "LineEdit", new Color(UiTokens.Accent, 0.35f));
        t.SetStylebox("panel", "PopupMenu", Box(UiTokens.SurfaceRaised, 4, border: UiTokens.BorderStrong, radius: 3));
        t.SetStylebox("hover", "PopupMenu", Box(new Color(UiTokens.Accent, 0.22f), 4, radius: 2));
        t.SetColor("font_color", "PopupMenu", UiTokens.TextPrimary);
        t.SetColor("font_hover_color", "PopupMenu", UiTokens.TextPrimary);
        t.SetFontSize("font_size", "PopupMenu", UiTokens.FontSmall);
        foreach (var s in new[] { "normal", "hover", "pressed", "hover_pressed", "focus" }) t.SetStylebox(s, "CheckBox", new StyleBoxEmpty());
        t.SetColor("font_color", "CheckBox", UiTokens.TextPrimary);
        t.SetColor("font_hover_color", "CheckBox", UiTokens.TextPrimary);
        t.SetColor("font_pressed_color", "CheckBox", UiTokens.TextPrimary);
        t.SetFontSize("font_size", "CheckBox", UiTokens.FontSmall);

        // Tooltip padrao (os ricos usam RichTip).
        var tip = new StyleBoxFlat
        {
            BgColor = new Color("#141920"), BorderColor = UiTokens.BorderStrong,
            ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 11, ContentMarginBottom = 12,
            ShadowColor = new Color(0, 0, 0, 0.55f), ShadowSize = 12, ShadowOffset = new Vector2(0, 4), AntiAliasing = true,
        };
        tip.SetBorderWidthAll(1);
        tip.SetCornerRadiusAll(4);
        t.SetStylebox("panel", "TooltipPanel", tip);
        t.SetColor("font_color", "TooltipLabel", UiTokens.TextLabel);
        t.SetFont("font", "TooltipLabel", UiFonts.Medium);
        t.SetFontSize("font_size", "TooltipLabel", UiTokens.FontBody);
        return t;
    }

    private static StyleBoxFlat Btn(Color bg, Color border)
    {
        var sb = Box(bg, 10, border: border, radius: 3);
        sb.ContentMarginTop = 4;
        sb.ContentMarginBottom = 4;
        return sb;
    }

    public static StyleBoxFlat Box(Color bg, int padding, Color? border = null, int radius = UiTokens.CornerRadius)
    {
        var sb = new StyleBoxFlat
        {
            BgColor = bg,
            ContentMarginLeft = padding, ContentMarginRight = padding,
            ContentMarginTop = padding * 0.6f, ContentMarginBottom = padding * 0.6f,
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

    /// <summary>Painel flutuante plano: fundo quase opaco, borda fina, sombra curta.</summary>
    public static StyleBoxFlat Panel(int padding, int radius = UiTokens.PanelRadius)
    {
        var sb = Box(UiTokens.SurfaceGlass, padding, border: UiTokens.Border, radius: radius);
        sb.ContentMarginTop = sb.ContentMarginBottom = padding;
        sb.ShadowColor = new Color(0, 0, 0, 0.45f);
        sb.ShadowSize = 8;
        sb.ShadowOffset = new Vector2(0, 3);
        return sb;
    }

    /// <summary>Compatibilidade: paineis antigos usam Glass; agora e o painel plano.</summary>
    public static StyleBoxFlat Glass(int padding, int radius = UiTokens.PanelRadius) => Panel(padding, radius);

    /// <summary>Etiqueta neutra (tag): fundo escuro, borda fina, texto claro; cor so no ponto.</summary>
    public static PanelContainer Chip(string text, Color color, bool dot = false)
    {
        var chip = new PanelContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        var sb = new StyleBoxFlat
        {
            BgColor = UiTokens.SurfaceRaised, BorderColor = UiTokens.Border,
            ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 2, ContentMarginBottom = 2,
        };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(3);
        chip.AddThemeStyleboxOverride("panel", sb);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        row.AddChild(new ColorRect { Color = color, CustomMinimumSize = new Vector2(6, 6), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore });
        var label = Text(text, UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Medium);
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

    /// <summary>Rotulo de secao: condensado, caixa alta.</summary>
    public static Label Section(string text) =>
        Text(text, UiTokens.FontBody, UiTokens.Accent, UiFonts.SemiBold);
}

/// <summary>
/// Tooltip estruturado. Texto: 1a linha = titulo; linhas "Rotulo|valor" viram pares
/// alinhados; linhas iniciadas por "~" viram nota discreta; demais, texto normal.
/// </summary>
public static class RichTip
{
    /// <summary>
    /// Dica para _MakeCustomTooltip. O Godot 4.7 pede a dica mesmo com o texto vazio; um
    /// controle invisivel faz ele nao mostrar nada (antes aparecia uma moldura vazia).
    /// </summary>
    public static Control Make(string text) =>
        string.IsNullOrWhiteSpace(text) ? new Control { Visible = false } : Build(text);

    /// <summary>
    /// Conteudo da dica (o fundo e a borda vem do TooltipPanel do tema): titulo de 14 px,
    /// pares rotulo/valor de 13 px e notas legiveis com quebra de linha.
    /// </summary>
    public static Control Build(string text)
    {
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 5);
        var lines = text.Split('\n');
        bool notesStarted = false;
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            if (i == 0)
            {
                var title = ThemeFactory.Text(line, 14, UiTokens.TextPrimary, UiFonts.SemiBold);
                col.AddChild(title);
                continue;
            }
            if (line.StartsWith("~"))
            {
                if (!notesStarted && col.GetChildCount() > 1)
                    col.AddChild(new ColorRect { Color = UiTokens.Divider, CustomMinimumSize = new Vector2(0, 1) });
                notesStarted = true;
                var note = ThemeFactory.Text(line[1..].Trim(), UiTokens.FontSmall, new Color("#A9B2BE"));
                note.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                note.CustomMinimumSize = new Vector2(Math.Min(340, Math.Max(220, line.Length * 6)), 0);
                col.AddChild(note);
                continue;
            }
            var pair = line.Split('|');
            if (pair.Length == 2)
            {
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 20);
                var k = ThemeFactory.Text(pair[0], UiTokens.FontSmall, UiTokens.TextSecondary, UiFonts.Medium);
                k.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
                row.AddChild(k);
                row.AddChild(ThemeFactory.Text(pair[1], UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Numbers));
                col.AddChild(row);
            }
            else
            {
                var l = ThemeFactory.Text(line, UiTokens.FontSmall, UiTokens.TextLabel);
                l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                l.CustomMinimumSize = new Vector2(Math.Min(340, Math.Max(200, line.Length * 6)), 0);
                col.AddChild(l);
            }
        }
        col.CustomMinimumSize = new Vector2(220, 0);
        return col;
    }
}

/// <summary>Painel com tooltip estruturado (RichTip).</summary>
public partial class TipPanel : PanelContainer
{
    public override GodotObject _MakeCustomTooltip(string forText) => RichTip.Make(forText);
}

/// <summary>Botao com tooltip estruturado (RichTip).</summary>
public partial class TipButton : Button
{
    public override GodotObject _MakeCustomTooltip(string forText) => RichTip.Make(forText);
}

/// <summary>Rotulo com tooltip estruturado (RichTip).</summary>
public partial class TipLabel : Label
{
    public TipLabel() => MouseFilter = MouseFilterEnum.Stop;
    public override GodotObject _MakeCustomTooltip(string forText) => RichTip.Make(forText);
}

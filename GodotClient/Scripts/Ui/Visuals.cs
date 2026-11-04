using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Card das paginas (D-034): degrade vertical discreto, brilho suave no canto, borda fina
/// que acende ao passar o mouse. Tudo desenhado em poucas primitivas (sem texturas).
/// </summary>
public partial class CardPanel : PanelContainer
{
    private bool _hover;
    public Color Glow { get; set; } = UiTokens.Accent;

    public CardPanel()
    {
        MouseFilter = MouseFilterEnum.Pass;
        AddThemeStyleboxOverride("panel", new StyleBoxEmpty { ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 13, ContentMarginBottom = 15 });
    }

    public override void _Process(double delta)
    {
        bool h = IsVisibleInTree() && GetGlobalRect().HasPoint(GetGlobalMousePosition());
        if (h == _hover) return;
        _hover = h;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float w = Size.X, h = Size.Y;
        if (w < 4 || h < 4) return;
        // Retangulo arredondado com degrade (cor por vertice conforme a altura).
        var pts = Visuals.RoundedRect(new Rect2(0, 0, w, h), 4);
        var cols = new Color[pts.Length];
        for (int i = 0; i < pts.Length; i++) cols[i] = UiTokens.CardTop.Lerp(UiTokens.CardBottom, pts[i].Y / h);
        DrawPolygon(pts, cols);
        // Brilho no canto superior direito (leque com alfa decrescente).
        float r = Mathf.Min(220, Mathf.Min(w * 0.55f, h * 0.95f));
        var fan = new Vector2[18];
        var fc = new Color[18];
        fan[0] = new Vector2(w - 1, 1);
        fc[0] = new Color(Glow, _hover ? 0.075f : 0.05f);
        for (int i = 0; i < 17; i++)
        {
            float a = Mathf.Pi / 2 + i / 16f * Mathf.Pi / 2;
            fan[i + 1] = new Vector2(w - 1 + Mathf.Cos(a) * r, 1 - Mathf.Sin(a) * -r * -1);
            fan[i + 1] = new Vector2(w - 1 + Mathf.Cos(a) * r, 1 + Mathf.Sin(a) * r);
            fc[i + 1] = new Color(Glow, 0);
        }
        DrawPolygon(fan, fc);
        // Filete claro no topo e borda.
        DrawLine(new Vector2(4, 0.5f), new Vector2(w - 4, 0.5f), new Color(1, 1, 1, 0.035f), 1);
        var border = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), BorderColor = _hover ? UiTokens.BorderStrong : UiTokens.Border, AntiAliasing = true };
        border.SetBorderWidthAll(1);
        border.SetCornerRadiusAll(4);
        DrawStyleBox(border, new Rect2(0, 0, w, h));
    }
}

/// <summary>Fundo das paginas: base escura e um brilho radial muito suave da cor da marca.</summary>
public partial class PageBackdrop : Control
{
    public PageBackdrop()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, Size), UiTokens.Background);
        Glow(new Vector2(Size.X * 0.82f, -Size.Y * 0.1f), Size.X * 0.55f, new Color(UiTokens.Accent, 0.055f));
        Glow(new Vector2(Size.X * 0.05f, Size.Y * 1.05f), Size.X * 0.4f, new Color(UiTokens.Info, 0.03f));
    }

    private void Glow(Vector2 c, float r, Color col)
    {
        const int n = 48;
        var pts = new Vector2[n + 1];
        var cols = new Color[n + 1];
        pts[0] = c;
        cols[0] = col;
        for (int i = 0; i < n; i++)
        {
            float a = i * Mathf.Tau / n;
            pts[i + 1] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            cols[i + 1] = new Color(col, 0);
        }
        // Leque fechado em triangulos (o poligono nao e convexo no ultimo ponto).
        var tri = new List<Vector2>();
        var tc = new List<Color>();
        for (int i = 0; i < n; i++)
        {
            int j = i + 1, k = i + 1 == n ? 1 : i + 2;
            tri.Add(pts[0]); tri.Add(pts[j]); tri.Add(pts[k]);
            tc.Add(cols[0]); tc.Add(cols[j]); tc.Add(cols[k]);
        }
        RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), Enumerable.Range(0, tri.Count).ToArray(), tri.ToArray(), tc.ToArray());
    }
}

public static class Visuals
{
    /// <summary>Pontos de um retangulo arredondado (convexo), sentido horario.</summary>
    public static Vector2[] RoundedRect(Rect2 r, float radius, int seg = 4)
    {
        var pts = new List<Vector2>();
        void Corner(Vector2 c, float start)
        {
            for (int i = 0; i <= seg; i++)
            {
                float a = start + i * (Mathf.Pi / 2) / seg;
                pts.Add(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * radius);
            }
        }
        Corner(new Vector2(r.Position.X + radius, r.Position.Y + radius), Mathf.Pi);
        Corner(new Vector2(r.End.X - radius, r.Position.Y + radius), -Mathf.Pi / 2);
        Corner(new Vector2(r.End.X - radius, r.End.Y - radius), 0);
        Corner(new Vector2(r.Position.X + radius, r.End.Y - radius), Mathf.Pi / 2);
        return pts.ToArray();
    }

    /// <summary>Caixa de leitura (tooltip desenhado dentro dos graficos).</summary>
    public static void ReadoutBox(CanvasItem g, Rect2 box)
    {
        var sb = new StyleBoxFlat { BgColor = new Color("#0E1116"), BorderColor = UiTokens.BorderStrong, ShadowColor = new Color(0, 0, 0, 0.45f), ShadowSize = 6, AntiAliasing = true };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(3);
        g.DrawStyleBox(sb, box);
    }

    /// <summary>Etiqueta de valor (pilula) na cor da serie.</summary>
    public static void ValuePill(CanvasItem g, Vector2 leftMid, string text, Color col, float maxRight)
    {
        var font = UiFonts.Numbers;
        const int fs = 11;
        float tw = font.GetStringSize(text, HorizontalAlignment.Left, -1, fs).X;
        var box = new Rect2(Mathf.Round(leftMid.X), Mathf.Round(leftMid.Y - 9), Mathf.Round(tw + 12), 18);
        if (box.End.X > maxRight) box.Position = new Vector2(Mathf.Round(maxRight - box.Size.X), box.Position.Y);
        var sb = new StyleBoxFlat { BgColor = new Color(col, 0.16f), BorderColor = new Color(col, 0.55f), AntiAliasing = true };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(9);
        g.DrawStyleBox(sb, box);
        g.DrawString(font, new Vector2(box.Position.X + 6, box.Position.Y + 13), text, HorizontalAlignment.Left, -1, fs, col.Lightened(0.15f));
    }
}

/// <summary>Barra fina com trilho, alinhada ao pixel (sem meio-pixel borrado).</summary>
public partial class ThinBar : Control
{
    private readonly float _t;
    private readonly Color _color;
    public ThinBar(float t, Color color) { _t = Mathf.Clamp(t, 0, 1); _color = color; MouseFilter = MouseFilterEnum.Ignore; }

    public override void _Draw()
    {
        float h = Mathf.Round(Mathf.Min(Size.Y, 4)), y = Mathf.Round((Size.Y - h) / 2f), w = Mathf.Round(Size.X);
        DrawRect(new Rect2(0, y, w, h), UiTokens.Track);
        if (_t > 0) DrawRect(new Rect2(0, y, Mathf.Max(2, Mathf.Round(w * _t)), h), _color);
    }
}

/// <summary>Icone "i" discreto (circulo fino) com tooltip estruturado.</summary>
public partial class InfoGlyph : Control
{
    private bool _hover;
    public InfoGlyph()
    {
        MouseFilter = MouseFilterEnum.Stop;
        MouseDefaultCursorShape = CursorShape.Help;
        MouseEntered += () => { _hover = true; QueueRedraw(); };
        MouseExited += () => { _hover = false; QueueRedraw(); };
    }
    public override GodotObject _MakeCustomTooltip(string forText) => RichTip.Build(forText);

    public override void _Draw()
    {
        var c = (Size / 2f).Round();
        var col = _hover ? UiTokens.TextPrimary : new Color("#5E6772");
        DrawArc(c, 6.5f, 0, Mathf.Tau, 32, col, 1.2f, true);
        DrawRect(new Rect2(c.X - 0.75f, c.Y - 1.5f, 1.5f, 5), col);
        DrawRect(new Rect2(c.X - 0.75f, c.Y - 4, 1.5f, 1.5f), col);
    }
}

/// <summary>Medidor em anel (0..1) com valor no centro e legenda, estilo nota de desempenho.</summary>
public partial class RingGauge : Control
{
    public float Value { get; set; }
    public string Center { get; set; } = "";
    public string Caption { get; set; } = "";
    public Color Color { get; set; } = UiTokens.Accent;
    public float Thickness { get; set; } = 6;

    public RingGauge() => MouseFilter = MouseFilterEnum.Pass;

    public override void _Draw()
    {
        float r = Mathf.Round(Mathf.Min(Size.X, Size.Y) / 2f - Thickness / 2f - 1);
        var c = (Size / 2f).Round();
        DrawArc(c, r, 0, Mathf.Tau, 96, UiTokens.Track, Thickness, true);
        float t = Mathf.Clamp(Value, 0, 1);
        if (t > 0) DrawArc(c, r, -Mathf.Pi / 2, -Mathf.Pi / 2 + Mathf.Tau * t, Math.Max(8, (int)(96 * t)), Color, Thickness, true);
        int fs = (int)Mathf.Clamp(r * 0.5f, 13, 24);
        float y = Caption.Length > 0 ? c.Y + fs * 0.3f - 4 : c.Y + fs * 0.36f;
        DrawString(UiFonts.Numbers, new Vector2(0, Mathf.Round(y)), Center, HorizontalAlignment.Center, Size.X, fs, UiTokens.TextPrimary);
        if (Caption.Length > 0)
            DrawString(UiFonts.Medium, new Vector2(0, Mathf.Round(y + 14)), Caption, HorizontalAlignment.Center, Size.X, 11, UiTokens.TextSecondary);
    }
}

/// <summary>Rosca de participacao (fatias com cor) e total no centro.</summary>
public partial class DonutChart : Control
{
    public IReadOnlyList<(float Value, Color Color)> Slices { get; set; } = Array.Empty<(float, Color)>();
    public string Center { get; set; } = "";
    public string Caption { get; set; } = "";

    public DonutChart() => MouseFilter = MouseFilterEnum.Ignore;

    public override void _Draw()
    {
        float thick = 12, r = Mathf.Round(Mathf.Min(Size.X, Size.Y) / 2f - thick / 2f - 1);
        var c = (Size / 2f).Round();
        float total = Slices.Sum(s => s.Value);
        DrawArc(c, r, 0, Mathf.Tau, 96, UiTokens.Track, thick, true);
        if (total > 0)
        {
            float a = -Mathf.Pi / 2;
            foreach (var (v, col) in Slices)
            {
                float span = Mathf.Tau * v / total;
                if (span > 0.03f) DrawArc(c, r, a + 0.018f, a + span - 0.018f, Math.Max(6, (int)(96 * span / Mathf.Tau)), col, thick, true);
                a += span;
            }
        }
        DrawString(UiFonts.Numbers, new Vector2(0, c.Y + 3), Center, HorizontalAlignment.Center, Size.X, 17, UiTokens.TextPrimary);
        if (Caption.Length > 0) DrawString(UiFonts.Medium, new Vector2(0, c.Y + 19), Caption, HorizontalAlignment.Center, Size.X, 11, UiTokens.TextSecondary);
    }
}

/// <summary>
/// Grafico de colunas (estilo "forma" de um jogo de gestao): colunas largas com topo
/// arredondado, cor por desempenho, media tracejada com rotulo e leitura ao passar o mouse.
/// </summary>
public partial class ColumnChart : Control
{
    public IReadOnlyList<float> Values { get; set; } = Array.Empty<float>();
    public IReadOnlyList<string> Labels { get; set; } = Array.Empty<string>();
    public Func<float, Color> ColorOf { get; set; } = _ => UiTokens.Success;
    public string Unit { get; set; } = "";
    public string EmptyText { get; set; } = "Sem dados ainda.";
    private int _hover = -1;
    private const float Left = 34, Bottom = 20, Top = 8;

    public ColumnChart()
    {
        MouseFilter = MouseFilterEnum.Pass;
        MouseExited += () => { _hover = -1; QueueRedraw(); };
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseMotion m || Values.Count == 0) return;
        float slot = (Size.X - Left) / Values.Count;
        int i = (int)Mathf.Floor((m.Position.X - Left) / slot);
        int h = i >= 0 && i < Values.Count && m.Position.Y < Size.Y - Bottom ? i : -1;
        if (h != _hover) { _hover = h; QueueRedraw(); }
    }

    public override void _Draw()
    {
        var plot = new Rect2(Left, Top, Size.X - Left, Size.Y - Top - Bottom);
        float baseY = Mathf.Floor(plot.End.Y) + 0.5f;
        if (Values.Count == 0 || Values.All(v => v <= 0))
        {
            DrawLine(new Vector2(plot.Position.X, baseY), new Vector2(plot.End.X, baseY), UiTokens.BorderStrong, 1);
            DrawString(UiFonts.Regular, new Vector2(plot.Position.X, Mathf.Round(plot.GetCenter().Y)), EmptyText, HorizontalAlignment.Center, plot.Size.X, 12, UiTokens.TextDisabled);
            return;
        }
        float max = Mathf.Max(1, Values.Max());
        float nice = max <= 60 ? 60 : max <= 120 ? 120 : max <= 240 ? 240 : max <= 360 ? 360 : Mathf.Ceil(max / 120f) * 120;
        for (int g = 0; g <= 3; g++)
        {
            float y = Mathf.Floor(plot.End.Y - plot.Size.Y * g / 3f) + 0.5f;
            DrawLine(new Vector2(plot.Position.X, y), new Vector2(plot.End.X, y), g == 0 ? UiTokens.BorderStrong : UiTokens.Grid, 1);
            DrawString(UiFonts.Regular, new Vector2(0, Mathf.Round(y + 4)), (nice * g / 3f).ToString("0"), HorizontalAlignment.Right, Left - 8, 11, UiTokens.AxisText);
        }
        float slot = plot.Size.X / Values.Count, w = Mathf.Round(Mathf.Clamp(slot * 0.62f, 4, 34));
        int step = Math.Max(1, (int)Mathf.Ceil(Values.Count / 7f));
        for (int i = 0; i < Values.Count; i++)
        {
            float h = Mathf.Round(plot.Size.Y * Mathf.Clamp(Values[i] / nice, 0, 1));
            var col = ColorOf(Values[i]);
            if (_hover == i) col = col.Lightened(0.18f);
            float x = Mathf.Round(plot.Position.X + slot * i + (slot - w) / 2f);
            if (h >= 2)
            {
                var sb = new StyleBoxFlat { BgColor = col, CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2, AntiAliasing = false };
                DrawStyleBox(sb, new Rect2(x, Mathf.Floor(plot.End.Y) - h, w, h));
            }
            else DrawRect(new Rect2(x, Mathf.Floor(plot.End.Y) - 2, w, 2), UiTokens.Track);
            bool label = i < Labels.Count && ((Values.Count - 1 - i) % step == 0);
            if (label || _hover == i)
                DrawString(UiFonts.Regular, new Vector2(x - 20, Size.Y - 4), i < Labels.Count ? Labels[i] : "", HorizontalAlignment.Center, w + 40, 11, _hover == i ? UiTokens.TextPrimary : UiTokens.AxisText);
        }
        var played = Values.Where(v => v > 0).ToList();
        float avg = played.Count == 0 ? 0 : played.Average();
        float ay = Mathf.Floor(plot.End.Y - plot.Size.Y * Mathf.Clamp(avg / nice, 0, 1)) + 0.5f;
        DrawDashedLine(new Vector2(plot.Position.X, ay), new Vector2(plot.End.X, ay), new Color(UiTokens.AccentSoft, 0.75f), 1, 4);
        if (_hover >= 0)
        {
            string title = _hover < Labels.Count ? Labels[_hover] : "";
            string value = $"{Values[_hover]:0}{Unit}";
            float tw = Mathf.Max(UiFonts.SemiBold.GetStringSize(title, HorizontalAlignment.Left, -1, 12).X, UiFonts.Numbers.GetStringSize(value, HorizontalAlignment.Left, -1, 12).X) + 20;
            float cx = plot.Position.X + slot * _hover + slot / 2;
            var box = new Rect2(Mathf.Round(Mathf.Clamp(cx - tw / 2, 0, Size.X - tw)), 0, Mathf.Round(tw), 40);
            Visuals.ReadoutBox(this, box);
            DrawString(UiFonts.SemiBold, new Vector2(box.Position.X + 10, 16), title, HorizontalAlignment.Left, -1, 12, UiTokens.TextPrimary);
            DrawString(UiFonts.Numbers, new Vector2(box.Position.X + 10, 32), value, HorizontalAlignment.Left, -1, 12, ColorOf(Values[_hover]).Lightened(0.2f));
        }
    }
}

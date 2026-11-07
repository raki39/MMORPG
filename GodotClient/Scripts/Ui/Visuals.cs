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
        TextureRepeat = TextureRepeatEnum.Enabled;
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
        DrawTextureRect(CardTexture.Get(), new Rect2(2, 2, w - 4, h - 4), true, new Color(1, 1, 1, 1));
        // Brilho no canto superior direito (leque com alfa decrescente).
        float r = Mathf.Min(220, Mathf.Min(w * 0.55f, h * 0.95f));
        var fan = new Vector2[18];
        var fc = new Color[18];
        fan[0] = new Vector2(w - 1, 1);
        fc[0] = new Color(Glow, _hover ? 0.075f : 0.05f);
        for (int i = 0; i < 17; i++)
        {
            float a = Mathf.Pi / 2 + i / 16f * Mathf.Pi / 2;
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
    public override GodotObject _MakeCustomTooltip(string forText) => RichTip.Make(forText);

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
    /// <summary>Escala para contagens (multiplos de 3) em vez de minutos.</summary>
    public bool CountScale { get; set; }
    public bool ShowAverage { get; set; } = true;
    /// <summary>Cor por indice (tem prioridade sobre ColorOf).</summary>
    public Func<int, Color>? ColorAt { get; set; }
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
        float nice = CountScale ? Mathf.Max(3, Mathf.Ceil(max / 3f) * 3)
            : max <= 60 ? 60 : max <= 120 ? 120 : max <= 240 ? 240 : max <= 360 ? 360 : Mathf.Ceil(max / 120f) * 120;
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
            var col = ColorAt?.Invoke(i) ?? ColorOf(Values[i]);
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
        if (ShowAverage) DrawDashedLine(new Vector2(plot.Position.X, ay), new Vector2(plot.End.X, ay), new Color(UiTokens.AccentSoft, 0.75f), 1, 4);
        if (_hover >= 0)
        {
            string title = _hover < Labels.Count ? Labels[_hover] : "";
            string value = $"{Values[_hover]:0}{Unit}";
            float tw = Mathf.Max(UiFonts.SemiBold.GetStringSize(title, HorizontalAlignment.Left, -1, 12).X, UiFonts.Numbers.GetStringSize(value, HorizontalAlignment.Left, -1, 12).X) + 20;
            float cx = plot.Position.X + slot * _hover + slot / 2;
            var box = new Rect2(Mathf.Round(Mathf.Clamp(cx - tw / 2, 0, Size.X - tw)), 0, Mathf.Round(tw), 40);
            Visuals.ReadoutBox(this, box);
            DrawString(UiFonts.SemiBold, new Vector2(box.Position.X + 10, 16), title, HorizontalAlignment.Left, -1, 12, UiTokens.TextPrimary);
            DrawString(UiFonts.Numbers, new Vector2(box.Position.X + 10, 32), value, HorizontalAlignment.Left, -1, 12, (ColorAt?.Invoke(_hover) ?? ColorOf(Values[_hover])).Lightened(0.2f));
        }
    }
}

/// <summary>Nota em anel pequeno (ex.: 4/5), como o relatorio de olheiro de um jogo de gestao.</summary>
public partial class RingRating : Control
{
    private readonly int _score, _max;
    private readonly Color _color;

    public RingRating(int score, int max, Color color, int size = 40)
    {
        _score = score; _max = max; _color = color;
        CustomMinimumSize = new Vector2(size, size);
        SizeFlagsVertical = SizeFlags.ShrinkCenter;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        var c = (Size / 2f).Round();
        float r = Mathf.Round(Mathf.Min(Size.X, Size.Y) / 2f - 3);
        DrawArc(c, r, 0, Mathf.Tau, 72, UiTokens.Track, 3, true);
        float t = _max <= 0 ? 0 : Mathf.Clamp(_score / (float)_max, 0, 1);
        if (t > 0) DrawArc(c, r, -Mathf.Pi / 2, -Mathf.Pi / 2 + Mathf.Tau * t, Math.Max(8, (int)(72 * t)), _color, 3, true);
        int fs = Size.X >= 52 ? 20 : 15;
        string s = _score.ToString();
        float w = UiFonts.Numbers.GetStringSize(s, HorizontalAlignment.Left, -1, fs).X;
        float sw = UiFonts.Medium.GetStringSize("/" + _max, HorizontalAlignment.Left, -1, 9).X;
        float x = Mathf.Round(c.X - (w + sw) / 2);
        DrawString(UiFonts.Numbers, new Vector2(x, Mathf.Round(c.Y + fs * 0.36f)), s, HorizontalAlignment.Left, -1, fs, UiTokens.TextPrimary);
        DrawString(UiFonts.Medium, new Vector2(x + w, Mathf.Round(c.Y + fs * 0.36f)), "/" + _max, HorizontalAlignment.Left, -1, 9, UiTokens.TextSecondary);
    }
}

/// <summary>Meio-medidor do cabecalho (estilo nota central): arco com degrade, valor e rotulo.</summary>
public partial class SemiGauge : Control
{
    public float Value { get; set; }
    public string Center { get; set; } = "";
    public string Caption { get; set; } = "";
    public string Verdict { get; set; } = "";
    public Color Color { get; set; } = UiTokens.Accent;

    public SemiGauge() { MouseFilter = MouseFilterEnum.Stop; CustomMinimumSize = new Vector2(150, 78); }
    public override GodotObject _MakeCustomTooltip(string forText) => RichTip.Make(forText);

    public override void _Draw()
    {
        var c = new Vector2(Mathf.Round(Size.X / 2f), Mathf.Round(Size.Y - 14));
        float r = Mathf.Min(Size.X / 2f - 8, Size.Y - 22);
        DrawArc(c, r, Mathf.Pi, Mathf.Tau, 64, UiTokens.Track, 6, true);
        float t = Mathf.Clamp(Value, 0, 1);
        if (t > 0)
        {
            // Arco em segmentos com cor crescente (degrade ao longo do arco).
            int seg = Math.Max(2, (int)(48 * t));
            for (int i = 0; i < seg; i++)
            {
                float a0 = Mathf.Pi + Mathf.Pi * t * i / seg, a1 = Mathf.Pi + Mathf.Pi * t * (i + 1) / seg + 0.01f;
                var col = new Color(Color, 0.45f).Lerp(Color, (i + 1f) / seg);
                DrawArc(c, r, a0, a1, 3, col, 6, true);
            }
        }
        DrawString(UiFonts.Medium, new Vector2(0, Mathf.Round(c.Y - r + 22)), Verdict, HorizontalAlignment.Center, Size.X, 11, Color.Lightened(0.15f));
        DrawString(UiFonts.Numbers, new Vector2(0, Mathf.Round(c.Y - 2)), Center, HorizontalAlignment.Center, Size.X, 22, UiTokens.TextPrimary);
        DrawString(UiFonts.Medium, new Vector2(0, Mathf.Round(c.Y + 13)), Caption, HorizontalAlignment.Center, Size.X, 11, UiTokens.TextSecondary);
    }
}

/// <summary>Escala de 1 a 5 em pontos (equivalente as estrelas), com metade.</summary>
public partial class Pips : Control
{
    private readonly float _value;
    private readonly Color _color;
    public Pips(float value, Color color) { _value = value; _color = color; CustomMinimumSize = new Vector2(5 * 12, 12); SizeFlagsVertical = SizeFlags.ShrinkCenter; MouseFilter = MouseFilterEnum.Ignore; }

    public override void _Draw()
    {
        for (int i = 0; i < 5; i++)
        {
            var c = new Vector2(6 + i * 12, Mathf.Round(Size.Y / 2));
            DrawCircle(c, 4, UiTokens.Track);
            float f = Mathf.Clamp(_value - i, 0, 1);
            if (f >= 0.75f) DrawCircle(c, 4, _color);
            else if (f >= 0.25f) DrawArc(c, 2, Mathf.Pi / 2, Mathf.Pi * 1.5f, 8, _color, 4, true);
        }
    }
}

/// <summary>Indicador com o mesmo fundo dos cards e tooltip estruturado.</summary>
public partial class StatTile : CardPanel
{
    public override GodotObject _MakeCustomTooltip(string forText) => RichTip.Make(forText);
}

/// <summary>Emblema quadrado com icone (missoes, paginas de lista): degrade, borda e faixa no topo.</summary>
public partial class EmblemBox : Control
{
    private readonly string _icon;
    private readonly Color _color;
    public EmblemBox(string icon, Color color) { _icon = icon; _color = color; MouseFilter = MouseFilterEnum.Ignore; SizeFlagsVertical = SizeFlags.ShrinkBegin; }

    public override void _Draw()
    {
        var rect = new Rect2(Vector2.One, Size - Vector2.One * 2);
        var top = new Color("#141922").Lerp(_color, 0.16f);
        var bottom = new Color("#0B0E13");
        DrawPolygon(new[] { rect.Position, new Vector2(rect.End.X, rect.Position.Y), rect.End, new Vector2(rect.Position.X, rect.End.Y) }, new[] { top, top, bottom, bottom });
        DrawRect(rect, new Color(_color, 0.45f), false, 1f);
        DrawRect(new Rect2(rect.Position, new Vector2(rect.Size.X, 3)), _color);
        int sz = (int)(Mathf.Min(Size.X, Size.Y) * 0.44f);
        DrawTextureRect(Icons.Get(_icon, sz, 2f), new Rect2(((Size - new Vector2(sz, sz)) / 2f).Round(), new Vector2(sz, sz)), false, _color.Lightened(0.25f));
    }
}

/// <summary>Marcador numerado de etapa com linha de ligacao (linha do tempo vertical).</summary>
public partial class StepBadge : Control
{
    private readonly int _n;
    private readonly Color _color;
    private readonly bool _last;
    public StepBadge(int n, Color color, bool last) { _n = n; _color = color; _last = last; MouseFilter = MouseFilterEnum.Ignore; }

    public override void _Draw()
    {
        var c = new Vector2(Mathf.Round(Size.X / 2), Mathf.Round(Size.Y / 2));
        if (!_last) DrawLine(new Vector2(c.X + 0.5f, c.Y + 13), new Vector2(c.X + 0.5f, Size.Y + 2), UiTokens.BorderStrong, 1);
        if (_n > 1) DrawLine(new Vector2(c.X + 0.5f, -2), new Vector2(c.X + 0.5f, c.Y - 13), UiTokens.BorderStrong, 1);
        DrawCircle(c, 12, UiTokens.CardBottom);
        DrawArc(c, 12, 0, Mathf.Tau, 40, _last ? UiTokens.Accent : new Color(_color, 0.8f), 1.5f, true);
        string t = _n.ToString();
        float w = UiFonts.Numbers.GetStringSize(t, HorizontalAlignment.Left, -1, 12).X;
        DrawString(UiFonts.Numbers, new Vector2(Mathf.Round(c.X - w / 2), c.Y + 4), t, HorizontalAlignment.Left, -1, 12, UiTokens.TextPrimary);
    }
}

/// <summary>
/// Linha de uma grade de 12 colunas: largura da coluna = (largura - 11 vaos) / 12; um item de
/// N colunas mede N colunas + (N - 1) vaos. Assim as bordas coincidem entre linhas diferentes.
/// </summary>
public partial class SpanRow : Container
{
    public const int Columns = 12;
    public int Gap { get; set; } = 12;
    private readonly Dictionary<Control, int> _spans = new();

    public void Add(Control item, int span)
    {
        _spans[item] = Math.Clamp(span, 1, Columns);
        AddChild(item);
    }

    private IEnumerable<Control> Items => GetChildren().OfType<Control>().Where(c => c.Visible);
    public List<Control> VisibleItems => Items.ToList();

    /// <summary>
    /// Alturas dos cards empilhados dadas pela pagina em telas (ScreenPage, D-068): cada card vira uma linha
    /// propria, com o y relativo a esta linha. Nulo: empilha pela altura minima de cada card.
    /// </summary>
    public List<(float Y, float H)>? StackRects { get; set; }

    private bool _stacked;

    /// <summary>
    /// Largura minima = a do maior card (a linha nunca alarga a pagina alem da tela); se os
    /// cards nao cabem lado a lado na largura atual, empilham (altura = soma).
    /// </summary>
    public override Vector2 _GetMinimumSize()
    {
        float h = 0, w = 0, sum = 0;
        foreach (var c in Items)
        {
            var m = c.GetCombinedMinimumSize();
            w = Mathf.Max(w, m.X);
            h = _stacked ? h + m.Y + (h > 0 ? Gap : 0) : Mathf.Max(h, m.Y);
            sum += m.Y;
        }
        return new Vector2(w, h);
    }

    /// <summary>Os cards cabem lado a lado se cada um recebe ao menos a sua largura minima.</summary>
    public bool FitsSideBySide(float width) => Fits(width);

    private bool Fits(float width)
    {
        float col = (width - Gap * (Columns - 1)) / Columns;
        foreach (var c in Items)
        {
            int span = _spans.TryGetValue(c, out var s) ? s : 1;
            if (span * col + (span - 1) * Gap < c.GetCombinedMinimumSize().X - 0.5f) return false;
        }
        return true;
    }

    public override void _Notification(int what)
    {
        if (what != NotificationSortChildren) return;
        bool stacked = !Fits(Size.X);
        if (stacked != _stacked) { _stacked = stacked; CallDeferred(Control.MethodName.UpdateMinimumSize); }
        if (_stacked && StackRects is { } rects && rects.Count == Items.Count())
        {
            int i = 0;
            foreach (var c in Items) { FitChildInRect(c, new Rect2(0, rects[i].Y, Size.X, rects[i].H)); i++; }
            return;
        }
        if (_stacked)
        {
            float y = 0;
            foreach (var c in Items)
            {
                float hh = c.GetCombinedMinimumSize().Y;
                FitChildInRect(c, new Rect2(0, y, Size.X, hh));
                y += hh + Gap;
            }
            return;
        }
        float col = (Size.X - Gap * (Columns - 1)) / Columns;
        int at = 0;
        foreach (var c in Items)
        {
            int span = _spans.TryGetValue(c, out var s) ? s : 1;
            float x = Mathf.Round(at * (col + Gap));
            float right = Mathf.Round((at + span) * (col + Gap) - Gap);
            FitChildInRect(c, new Rect2(x, 0, right - x, Size.Y));
            at += span;
        }
    }
}

/// <summary>
/// Conteiner de uma linha que nao alarga quem o contem (D-068): a altura minima e a do filho, a largura
/// minima e zero; o que passar da largura e cortado (o filho define reticencias e tooltip).
/// </summary>
/// <summary>
/// Lista que cabe no card (D-068): mostra, em ordem, so os itens inteiros que cabem na altura recebida e esconde
/// o resto, sem rolagem. Offset pula os primeiros (paginas). A altura minima e a de um item, para o card nunca
/// rolar por causa da lista. Depois de cada arranjo avisa quantos mostrou (Laid), para o paginador do titulo.
/// </summary>
public partial class FitList : Container
{
    public int Separation { get; set; } = 6;
    public int Offset { get; set; }
    /// <summary>Itens do comeco sempre mostrados (contam na altura minima); so os seguintes somem quando nao cabem.</summary>
    public int Keep { get; set; }
    /// <summary>O ultimo item mostrado fica com toda a altura que sobra (ex.: um grafico que cresce).</summary>
    public bool StretchLast { get; set; }
    public int Shown { get; private set; }
    public int Total => GetChildCount();
    public event Action? Laid;

    public FitList()
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        MouseFilter = MouseFilterEnum.Pass;
    }

    private List<Control> Items() => GetChildren().OfType<Control>().ToList();

    /// <summary>
    /// Inicio da pagina anterior: volta item por item enquanto couber na altura do card, para a pagina anterior
    /// ser inteira (e nao recuar so o tanto da pagina atual, que pode ser a ultima e mais curta).
    /// </summary>
    public int PreviousOffset()
    {
        var items = Items();
        int start = Math.Min(Offset, items.Count);
        float y = 0;
        for (int i = start - 1; i >= 0; i--)
        {
            float h = items[i].GetCombinedMinimumSize().Y;
            float need = (start < Offset ? Separation : 0) + h;
            if (start < Offset && y + need > Size.Y + 0.5f) break;
            y += need;
            start = i;
        }
        return start;
    }

    public override Vector2 _GetMinimumSize()
    {
        var items = Items();
        float w = 0;
        foreach (var c in items) w = Mathf.Max(w, c.GetCombinedMinimumSize().X);
        float h = 0;
        if (items.Count > 0 && Keep > 0)
        {
            int n = Math.Min(Keep, items.Count);
            for (int i = 0; i < n; i++) h += items[i].GetCombinedMinimumSize().Y;
            h += Separation * (n - 1);
        }
        else if (items.Count > 0) h = items[Math.Clamp(Offset, 0, items.Count - 1)].GetCombinedMinimumSize().Y;
        return new Vector2(w, h);
    }

    public override void _Notification(int what)
    {
        if (what != NotificationSortChildren) return;
        var items = Items();
        Offset = Math.Clamp(Offset, 0, Math.Max(0, items.Count - 1));
        float y = 0;
        int shown = 0;
        bool full = false;
        Control? last = null;
        float lastY = 0;
        for (int i = 0; i < items.Count; i++)
        {
            var c = items[i];
            bool show = false;
            if (i >= Offset && !full)
            {
                float h = c.GetCombinedMinimumSize().Y;
                float gap = shown > 0 ? Separation : 0;
                if (shown == 0 || i < Keep || y + gap + h <= Size.Y + 0.5f)
                {
                    y += gap;
                    FitChildInRect(c, new Rect2(0, y, Size.X, h));
                    last = c;
                    lastY = y;
                    y += h;
                    shown++;
                    show = true;
                }
                else full = true;
            }
            if (c.Visible != show) c.Visible = show;
        }
        if (StretchLast && last != null && Size.Y > lastY) FitChildInRect(last, new Rect2(0, lastY, Size.X, Size.Y - lastY));
        if (shown != Shown) { Shown = shown; }
        Laid?.Invoke();
    }
}

public partial class ClipRow : Container
{
    public ClipRow() { ClipContents = true; MouseFilter = MouseFilterEnum.Pass; }

    public override Vector2 _GetMinimumSize()
    {
        float h = 0;
        foreach (var c in GetChildren().OfType<Control>()) if (c.Visible) h = Mathf.Max(h, c.GetCombinedMinimumSize().Y);
        return new Vector2(0, h);
    }

    public override void _Notification(int what)
    {
        if (what != NotificationSortChildren) return;
        foreach (var c in GetChildren().OfType<Control>())
        {
            var m = c.GetCombinedMinimumSize();
            FitChildInRect(c, new Rect2(0, Mathf.Round((Size.Y - m.Y) / 2f), Mathf.Max(m.X, 0), m.Y));
        }
    }
}

/// <summary>
/// Textura sutil dos cards (grao fino + hachura diagonal leve), gerada uma vez por codigo,
/// deterministica (hash inteiro, sem RNG). Branca com alfa baixo: so da acabamento ao fundo.
/// </summary>
public static class CardTexture
{
    private static Texture2D? _tex;

    public static Texture2D Get()
    {
        if (_tex != null) return _tex;
        const int n = 128;
        var img = Image.CreateEmpty(n, n, false, Image.Format.Rgba8);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                uint h = (uint)(x * 374761393 + y * 668265263);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                float grain = (h & 1023) / 1023f;
                float a = grain * 0.022f;
                if ((x + y) % 7 == 0) a += 0.009f;          // hachura diagonal
                if ((x - y + n * 4) % 23 == 0) a += 0.0f; // contra-hachura bem leve
                img.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        _tex = ImageTexture.CreateFromImage(img);
        return _tex;
    }
}

/// <summary>Barra de atributo com o potencial (parte clara ate o teto), estilo jogo de gestao.</summary>
public partial class PotentialBar : Control
{
    private readonly float _t, _potential;
    private readonly Color _color;
    public PotentialBar(float t, float potential, Color color) { _t = Mathf.Clamp(t, 0, 1); _potential = Mathf.Clamp(potential, 0, 1); _color = color; MouseFilter = MouseFilterEnum.Ignore; }

    public override void _Draw()
    {
        float h = Mathf.Round(Mathf.Min(Size.Y, 4)), y = Mathf.Round((Size.Y - h) / 2f), w = Mathf.Round(Size.X);
        DrawRect(new Rect2(0, y, w, h), UiTokens.Track);
        if (_potential > _t) DrawRect(new Rect2(0, y, Mathf.Round(w * _potential), h), new Color(UiTokens.Accent, 0.30f));
        if (_t > 0) DrawRect(new Rect2(0, y, Mathf.Max(2, Mathf.Round(w * _t)), h), _color);
    }
}

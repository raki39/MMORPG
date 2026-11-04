using Godot;

namespace GodotClient.Ui;

/// <summary>Mini grafico de linha com area (indicadores da barra superior).</summary>
public partial class Sparkline : Control
{
    public IReadOnlyList<float> Values { get; set; } = Array.Empty<float>();
    public Color LineColor { get; set; } = UiTokens.Info;

    public Sparkline() => MouseFilter = MouseFilterEnum.Ignore;

    public override void _Draw()
    {
        if (Values.Count < 2) return;
        float max = Mathf.Max(1, Values.Max()), min = Mathf.Min(0, Values.Min());
        var pts = new Vector2[Values.Count];
        for (int i = 0; i < Values.Count; i++)
            pts[i] = new Vector2(i * Size.X / (Values.Count - 1), Size.Y - 1 - (Values[i] - min) / (max - min) * (Size.Y - 2));
        var area = new List<Vector2>(pts) { new(Size.X, Size.Y), new(0, Size.Y) };
        DrawColoredPolygon(area.ToArray(), new Color(LineColor, 0.14f));
        DrawPolyline(pts, LineColor, 1.4f, true);
        DrawCircle(pts[^1], 2f, LineColor);
    }
}

/// <summary>Serie de um grafico de linhas.</summary>
public sealed class ChartSeries
{
    public string Name { get; init; } = "";
    public Color Color { get; init; } = UiTokens.Info;
    public IReadOnlyList<float> Values { get; init; } = Array.Empty<float>();
}

/// <summary>
/// Grafico de linhas (kit visual, D-034): grade fina continua, rotulos de 11 px, linha de 2 px
/// com pontos, area em degrade suave, valor do ultimo ponto em pilula e leitura ao passar o
/// mouse (linha guia, ponto destacado e caixa com os valores). Dados vem de amostras reais.
/// </summary>
public partial class LineChart : Control
{
    public IReadOnlyList<ChartSeries> Series { get; set; } = Array.Empty<ChartSeries>();
    public IReadOnlyList<string> XLabels { get; set; } = Array.Empty<string>();
    public string EmptyText { get; set; } = "Coletando dados…";
    /// <summary>Sufixo das leituras (ex.: " h").</summary>
    public string Unit { get; set; } = "";
    /// <summary>Mostra um ponto em cada amostra (series curtas).</summary>
    public bool Dots { get; set; } = true;
    private int _hover = -1;

    public LineChart()
    {
        MouseFilter = MouseFilterEnum.Pass;
        MouseExited += () => { _hover = -1; QueueRedraw(); };
    }

    private int Count => Series.Count == 0 ? 0 : Series.Max(s => s.Values.Count);

    private Rect2 Plot(float left)
    {
        float top = Series.Count > 1 ? 26 : 10, bottom = 22, right = 52;
        return new Rect2(left, top, Mathf.Max(10, Size.X - left - right), Mathf.Max(10, Size.Y - top - bottom));
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseMotion m) return;
        int n = Count;
        if (n < 2) return;
        var plot = Plot(_left);
        int i = m.Position.X < plot.Position.X - 6 || m.Position.X > plot.End.X + 6 ? -1
            : Mathf.Clamp(Mathf.RoundToInt((m.Position.X - plot.Position.X) / plot.Size.X * (n - 1)), 0, n - 1);
        if (i != _hover) { _hover = i; QueueRedraw(); }
    }

    private float _left = 34;

    public override void _Draw()
    {
        var font = UiFonts.Regular;
        const int fs = 11;
        int n = Count;
        float max = 1;
        foreach (var s in Series) if (s.Values.Count > 0) max = Mathf.Max(max, s.Values.Max());
        max = NiceMax(max);
        const int div = 4;
        // Margem esquerda conforme o maior rotulo do eixo.
        _left = Mathf.Ceil(font.GetStringSize(Compact(max), HorizontalAlignment.Left, -1, fs).X) + 14;
        var plot = Plot(_left);
        for (int g = 0; g <= div; g++)
        {
            float y = Mathf.Floor(plot.End.Y - plot.Size.Y * g / div) + 0.5f;
            DrawLine(new Vector2(plot.Position.X, y), new Vector2(plot.End.X, y), g == 0 ? UiTokens.BorderStrong : UiTokens.Grid, 1);
            DrawString(font, new Vector2(0, Mathf.Round(y + 4)), Compact(max * g / div), HorizontalAlignment.Right, _left - 10, fs, UiTokens.AxisText);
        }
        if (Series.Count > 1)
        {
            float lx = plot.Position.X;
            foreach (var s in Series)
            {
                DrawRect(new Rect2(lx, 8, 10, 3), s.Color);
                DrawString(UiFonts.Medium, new Vector2(lx + 15, 14), s.Name, HorizontalAlignment.Left, -1, fs, UiTokens.TextSecondary);
                lx += 15 + UiFonts.Medium.GetStringSize(s.Name, HorizontalAlignment.Left, -1, fs).X + 18;
            }
        }
        if (n < 2)
        {
            DrawString(UiFonts.Regular, new Vector2(plot.Position.X, Mathf.Round(plot.GetCenter().Y)), EmptyText, HorizontalAlignment.Center, plot.Size.X, 12, UiTokens.TextDisabled);
            return;
        }
        float X(int i) => plot.Position.X + i * plot.Size.X / (n - 1);
        float Y(float v) => plot.End.Y - v / max * plot.Size.Y;
        // Rotulos do eixo X: no maximo ~7, sempre o ultimo, sem sobrepor.
        int step = Math.Max(1, (int)Mathf.Ceil(n / 7f));
        float lastRight = -999;
        for (int i = n - 1; i >= 0; i -= step)
        {
            if (i >= XLabels.Count) continue;
            float w = font.GetStringSize(XLabels[i], HorizontalAlignment.Left, -1, fs).X;
            float x = Mathf.Round(Mathf.Clamp(X(i) - w / 2, plot.Position.X - 4, Size.X - w));
            if (lastRight > -999 && x + w > lastRight - 8) continue;
            DrawString(font, new Vector2(x, Size.Y - 5), XLabels[i], HorizontalAlignment.Left, -1, fs, _hover == i ? UiTokens.TextPrimary : UiTokens.AxisText);
            lastRight = x;
        }
        if (_hover >= 0)
        {
            float hx = Mathf.Floor(X(_hover)) + 0.5f;
            DrawLine(new Vector2(hx, plot.Position.Y), new Vector2(hx, plot.End.Y), new Color(UiTokens.TextSecondary, 0.35f), 1);
        }
        foreach (var s in Series)
        {
            if (s.Values.Count < 2) continue;
            var pts = new Vector2[s.Values.Count];
            for (int i = 0; i < s.Values.Count; i++) pts[i] = new Vector2(X(i), Y(s.Values[i]));
            var poly = new Vector2[pts.Length + 2];
            var cols = new Color[pts.Length + 2];
            for (int i = 0; i < pts.Length; i++) { poly[i] = pts[i]; cols[i] = new Color(s.Color, 0.13f); }
            poly[^2] = new Vector2(pts[^1].X, plot.End.Y); cols[^2] = new Color(s.Color, 0.0f);
            poly[^1] = new Vector2(pts[0].X, plot.End.Y); cols[^1] = new Color(s.Color, 0.0f);
            DrawPolygon(poly, cols);
            DrawPolyline(pts, s.Color, 2f, true);
            bool dots = Dots && pts.Length <= 32;
            for (int i = 0; i < pts.Length; i++)
            {
                if (!dots && i != pts.Length - 1 && i != _hover) continue;
                float r = i == _hover ? 4.5f : 3f;
                DrawCircle(pts[i], r + 1.5f, UiTokens.CardBottom);
                DrawCircle(pts[i], r, s.Color);
            }
            Visuals.ValuePill(this, new Vector2(pts[^1].X + 8, pts[^1].Y), Compact(s.Values[^1]) + Unit, s.Color, Size.X - 1);
        }
        if (_hover >= 0)
        {
            var lines = new List<(string Text, Color Col)>();
            foreach (var s in Series) if (_hover < s.Values.Count) lines.Add(($"{s.Name}: {Compact(s.Values[_hover])}{Unit}", s.Color));
            string title = _hover < XLabels.Count ? XLabels[_hover] : "";
            float w = Mathf.Max(UiFonts.SemiBold.GetStringSize(title, HorizontalAlignment.Left, -1, 12).X,
                lines.Count == 0 ? 0 : lines.Max(l => UiFonts.Medium.GetStringSize(l.Text, HorizontalAlignment.Left, -1, 12).X + 14)) + 22;
            float bx = X(_hover) + 12 + w > Size.X ? X(_hover) - w - 12 : X(_hover) + 12;
            var box = new Rect2(Mathf.Round(bx), Mathf.Round(plot.Position.Y), Mathf.Round(w), 26 + lines.Count * 17);
            Visuals.ReadoutBox(this, box);
            DrawString(UiFonts.SemiBold, new Vector2(box.Position.X + 11, box.Position.Y + 18), title, HorizontalAlignment.Left, -1, 12, UiTokens.TextPrimary);
            for (int k = 0; k < lines.Count; k++)
            {
                float ly = box.Position.Y + 36 + k * 17;
                DrawRect(new Rect2(box.Position.X + 11, ly - 7, 7, 7), lines[k].Col);
                DrawString(UiFonts.Medium, new Vector2(box.Position.X + 25, ly), lines[k].Text, HorizontalAlignment.Left, -1, 12, UiTokens.TextLabel);
            }
        }
    }

    public static string Compact(float v) => v >= 10000 ? (v / 1000f).ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) + " mil"
        : v >= 100 ? v.ToString("0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"))
        : v.ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));

    private static float NiceMax(float v)
    {
        if (v <= 4) return 4;
        float mag = Mathf.Pow(10, Mathf.Floor(Mathf.Log(v) / Mathf.Log(10)));
        foreach (var f in new[] { 1f, 1.2f, 2f, 4f, 6f, 8f, 10f }) if (f * mag >= v) return f * mag;
        return 10 * mag;
    }
}

/// <summary>Lista de barras horizontais (rotulo, barra, valor) com tooltip por linha.</summary>
public static class BarList
{
    public static Control Build(IEnumerable<(string Label, float Value, Color Color, string Tooltip)> items, string unit = "")
    {
        var list = items.ToList();
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 3);
        float max = list.Count == 0 ? 1 : Mathf.Max(1, list.Max(i => i.Value));
        if (list.Count == 0) col.AddChild(ThemeFactory.Text("Sem dados ainda.", UiTokens.FontSmall, UiTokens.TextDisabled));
        foreach (var (label, value, color, tip) in list)
        {
            var row = new TipPanel { TooltipText = tip, MouseFilter = tip.Length > 0 ? Control.MouseFilterEnum.Stop : Control.MouseFilterEnum.Ignore };
            row.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
            row.AddChild(ModalFrame.Bar(label, value, max, color, $"{value:0}{unit}", 150));
            col.AddChild(row);
        }
        return col;
    }
}

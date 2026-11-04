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
/// Grafico de linhas com eixo Y, grade, rotulos de tempo, legenda e leitura ao passar o
/// mouse (linha vertical + valores no ponto). Dados vem de amostras reais.
/// </summary>
public partial class LineChart : Control
{
    public IReadOnlyList<ChartSeries> Series { get; set; } = Array.Empty<ChartSeries>();
    public IReadOnlyList<string> XLabels { get; set; } = Array.Empty<string>();
    public string EmptyText { get; set; } = "Coletando dados…";
    private float _hoverX = -1;

    public LineChart()
    {
        MouseFilter = MouseFilterEnum.Pass;
        MouseExited += () => { _hoverX = -1; QueueRedraw(); };
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseMotion m) { _hoverX = m.Position.X; QueueRedraw(); }
    }

    public override void _Draw()
    {
        // Grafico analitico: grade discreta, eixo com rotulos pequenos, area em degrade,
        // linha fina, valor do ultimo ponto e leitura ao passar o mouse.
        var font = UiFonts.Numbers;
        const int fs = 9;
        float left = 34, bottom = 18, top = 20, right = 44;
        var plot = new Rect2(left, top, Size.X - left - right, Size.Y - top - bottom);
        int n = Series.Count == 0 ? 0 : Series.Max(s => s.Values.Count);
        float max = 1;
        foreach (var s in Series) if (s.Values.Count > 0) max = Mathf.Max(max, s.Values.Max());
        max = NiceMax(max);
        // Divisoes que dao rotulos redondos (5 para maximos 5/10/50..., 4 para 2/4/20/40...).
        int div = Mathf.IsEqualApprox(max / Mathf.Pow(10, Mathf.Floor(Mathf.Log(max) / Mathf.Log(10))), 5f) || Mathf.IsEqualApprox(max / Mathf.Pow(10, Mathf.Floor(Mathf.Log(max) / Mathf.Log(10))), 1f) ? 5 : 4;
        var grid = new Color(UiTokens.Border, 0.9f);
        for (int g = 0; g <= div; g++)
        {
            float y = plot.End.Y - plot.Size.Y * g / div;
            if (g == 0) DrawLine(new Vector2(plot.Position.X, y), new Vector2(plot.End.X, y), UiTokens.BorderStrong, 1);
            else DrawDashedLine(new Vector2(plot.Position.X, y), new Vector2(plot.End.X, y), grid, 1, 3);
            string lbl = Compact(max * g / div);
            DrawString(font, new Vector2(0, y + 3), lbl, HorizontalAlignment.Right, left - 8, fs, UiTokens.TextDisabled);
        }
        // Legenda a esquerda do topo.
        float lx = plot.Position.X;
        foreach (var s in Series)
        {
            DrawRect(new Rect2(lx, 6, 8, 2), s.Color);
            DrawString(UiFonts.Medium, new Vector2(lx + 12, 11), s.Name, HorizontalAlignment.Left, -1, 10, UiTokens.TextSecondary);
            lx += 22 + UiFonts.Medium.GetStringSize(s.Name, HorizontalAlignment.Left, -1, 10).X + 14;
        }
        if (n < 2)
        {
            DrawString(UiFonts.Regular, new Vector2(plot.Position.X, plot.GetCenter().Y), EmptyText, HorizontalAlignment.Center, plot.Size.X, 11, UiTokens.TextDisabled);
            return;
        }
        float X(int i) => plot.Position.X + i * plot.Size.X / (n - 1);
        float Y(float v) => plot.End.Y - v / max * plot.Size.Y;
        int step = Math.Max(1, n / 6);
        for (int i = 0; i < XLabels.Count && i < n; i += step)
            DrawString(font, new Vector2(X(i) - 20, Size.Y - 4), XLabels[i], HorizontalAlignment.Center, 40, fs, UiTokens.TextDisabled);
        foreach (var s in Series)
        {
            if (s.Values.Count < 2) continue;
            var pts = new Vector2[s.Values.Count];
            for (int i = 0; i < s.Values.Count; i++) pts[i] = new Vector2(X(i), Y(s.Values[i]));
            // Area em degrade (opaca junto da linha, transparente no eixo).
            var poly = new Vector2[pts.Length + 2];
            var cols = new Color[pts.Length + 2];
            for (int i = 0; i < pts.Length; i++) { poly[i] = pts[i]; cols[i] = new Color(s.Color, 0.16f); }
            poly[^2] = new Vector2(pts[^1].X, plot.End.Y); cols[^2] = new Color(s.Color, 0.0f);
            poly[^1] = new Vector2(pts[0].X, plot.End.Y); cols[^1] = new Color(s.Color, 0.0f);
            DrawPolygon(poly, cols);
            DrawPolyline(pts, s.Color, 1.5f, true);
            DrawCircle(pts[^1], 2.5f, s.Color);
            DrawString(font, new Vector2(pts[^1].X + 6, pts[^1].Y + 3), Compact(s.Values[^1]), HorizontalAlignment.Left, -1, 10, s.Color);
        }
        if (_hoverX >= plot.Position.X && _hoverX <= plot.End.X)
        {
            int i = Mathf.Clamp(Mathf.RoundToInt((_hoverX - plot.Position.X) / plot.Size.X * (n - 1)), 0, n - 1);
            DrawLine(new Vector2(X(i), plot.Position.Y), new Vector2(X(i), plot.End.Y), new Color(UiTokens.TextSecondary, 0.5f), 1);
            var lines = new List<(string, Color)> { (i < XLabels.Count ? XLabels[i] : "", UiTokens.TextPrimary) };
            foreach (var s in Series) if (i < s.Values.Count) lines.Add(($"{s.Name}: {Compact(s.Values[i])}", s.Color));
            float w = lines.Max(l => UiFonts.Medium.GetStringSize(l.Item1, HorizontalAlignment.Left, -1, 11).X) + 18;
            float bx = X(i) + 10 + w > Size.X ? X(i) - w - 10 : X(i) + 10;
            var box = new Rect2(bx, plot.Position.Y + 4, w, lines.Count * 15 + 10);
            DrawStyleBox(ThemeFactory.Box(new Color("#0B0E12"), 0, border: UiTokens.BorderStrong, radius: 2), box);
            for (int k = 0; k < lines.Count; k++)
                DrawString(k == 0 ? UiFonts.SemiBold : UiFonts.Medium, new Vector2(box.Position.X + 9, box.Position.Y + 17 + k * 15), lines[k].Item1, HorizontalAlignment.Left, -1, 11, lines[k].Item2);
            foreach (var s in Series) if (i < s.Values.Count) { DrawCircle(new Vector2(X(i), Y(s.Values[i])), 4, new Color("#0B0E12")); DrawCircle(new Vector2(X(i), Y(s.Values[i])), 2.5f, s.Color); }
        }
    }

    private static string Compact(float v) => v >= 10000 ? (v / 1000f).ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) + " mil"
        : v >= 100 ? v.ToString("0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"))
        : v.ToString("0.#", System.Globalization.CultureInfo.GetCultureInfo("pt-BR"));

    private static float NiceMax(float v)
    {
        if (v <= 5) return 5;
        float mag = Mathf.Pow(10, Mathf.Floor(Mathf.Log(v) / Mathf.Log(10)));
        foreach (var f in new[] { 1f, 2f, 2.5f, 5f, 10f }) if (f * mag >= v) return f * mag;
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

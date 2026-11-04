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
        var font = UiFonts.Numbers;
        const int fs = 10;
        float left = 30, bottom = 16, top = 18, right = 6;
        var plot = new Rect2(left, top, Size.X - left - right, Size.Y - top - bottom);
        DrawRect(plot, new Color(UiTokens.Background, 0.5f));
        int n = Series.Count == 0 ? 0 : Series.Max(s => s.Values.Count);
        float max = 1;
        foreach (var s in Series) if (s.Values.Count > 0) max = Mathf.Max(max, s.Values.Max());
        max = NiceMax(max);
        for (int g = 0; g <= 4; g++)
        {
            float y = plot.End.Y - plot.Size.Y * g / 4f;
            DrawLine(new Vector2(plot.Position.X, y), new Vector2(plot.End.X, y), new Color(UiTokens.Border, g == 0 ? 1f : 0.6f), 1);
            string lbl = (max * g / 4f).ToString("0");
            DrawString(font, new Vector2(2, y + 4), lbl, HorizontalAlignment.Right, left - 6, fs, UiTokens.TextDisabled);
        }
        // Legenda.
        float lx = plot.Position.X;
        foreach (var s in Series)
        {
            DrawRect(new Rect2(lx, 5, 10, 3), s.Color);
            DrawString(UiFonts.Condensed, new Vector2(lx + 14, 11), s.Name.ToUpperInvariant(), HorizontalAlignment.Left, -1, fs, UiTokens.TextSecondary);
            lx += 18 + UiFonts.Condensed.GetStringSize(s.Name.ToUpperInvariant(), HorizontalAlignment.Left, -1, fs).X + 12;
        }
        if (n < 2)
        {
            DrawString(UiFonts.Regular, new Vector2(plot.Position.X, plot.GetCenter().Y), EmptyText, HorizontalAlignment.Center, plot.Size.X, 12, UiTokens.TextDisabled);
            return;
        }
        float X(int i) => plot.Position.X + i * plot.Size.X / (n - 1);
        float Y(float v) => plot.End.Y - v / max * plot.Size.Y;
        for (int i = 0; i < XLabels.Count && i < n; i += Math.Max(1, n / 6))
            DrawString(font, new Vector2(X(i) - 16, Size.Y - 3), XLabels[i], HorizontalAlignment.Center, 32, fs, UiTokens.TextDisabled);
        foreach (var s in Series)
        {
            if (s.Values.Count < 2) continue;
            var pts = new Vector2[s.Values.Count];
            for (int i = 0; i < s.Values.Count; i++) pts[i] = new Vector2(X(i), Y(s.Values[i]));
            var area = new List<Vector2>(pts) { new(pts[^1].X, plot.End.Y), new(pts[0].X, plot.End.Y) };
            DrawColoredPolygon(area.ToArray(), new Color(s.Color, 0.08f));
            DrawPolyline(pts, s.Color, 1.6f, true);
        }
        if (_hoverX >= plot.Position.X && _hoverX <= plot.End.X)
        {
            int i = Mathf.Clamp(Mathf.RoundToInt((_hoverX - plot.Position.X) / plot.Size.X * (n - 1)), 0, n - 1);
            DrawLine(new Vector2(X(i), plot.Position.Y), new Vector2(X(i), plot.End.Y), UiTokens.BorderStrong, 1);
            var lines = new List<(string, Color)> { (i < XLabels.Count ? XLabels[i] : "", UiTokens.TextPrimary) };
            foreach (var s in Series) if (i < s.Values.Count) lines.Add(($"{s.Name}: {s.Values[i]:0}", s.Color));
            float w = lines.Max(l => UiFonts.SemiBold.GetStringSize(l.Item1, HorizontalAlignment.Left, -1, 11).X) + 16;
            float bx = X(i) + 8 + w > Size.X ? X(i) - w - 8 : X(i) + 8;
            var box = new Rect2(bx, plot.Position.Y + 4, w, lines.Count * 15 + 8);
            DrawStyleBox(ThemeFactory.Box(UiTokens.SurfaceHeader, 0, border: UiTokens.BorderStrong, radius: 3), box);
            for (int k = 0; k < lines.Count; k++)
                DrawString(UiFonts.SemiBold, new Vector2(box.Position.X + 8, box.Position.Y + 16 + k * 15), lines[k].Item1, HorizontalAlignment.Left, -1, 11, lines[k].Item2);
            foreach (var s in Series) if (i < s.Values.Count) DrawCircle(new Vector2(X(i), Y(s.Values[i])), 3, s.Color);
        }
    }

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

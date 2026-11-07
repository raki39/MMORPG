using Godot;

namespace GodotClient.Ui;

/// <summary>Leitura com titulo e linhas coloridas, desenhada dentro dos graficos da rotina.</summary>
public static class Readout
{
    public static void Draw(CanvasItem g, Vector2 anchor, Vector2 area, string title, IReadOnlyList<(string Text, Color Color)> lines)
    {
        const int fs = 12;
        float w = UiFonts.SemiBold.GetStringSize(title, HorizontalAlignment.Left, -1, fs).X;
        foreach (var (t, _) in lines) w = Mathf.Max(w, UiFonts.Regular.GetStringSize(t, HorizontalAlignment.Left, -1, fs).X);
        w = Mathf.Round(w + 22);
        float h = 24 + lines.Count * 16;
        float x = anchor.X + 14 + w > area.X ? anchor.X - 14 - w : anchor.X + 14;
        float y = Mathf.Clamp(anchor.Y - h / 2, 0, Mathf.Max(0, area.Y - h));
        var box = new Rect2(Mathf.Round(Mathf.Max(0, x)), Mathf.Round(y), w, h);
        Visuals.ReadoutBox(g, box);
        g.DrawString(UiFonts.SemiBold, new Vector2(box.Position.X + 11, box.Position.Y + 17), title, HorizontalAlignment.Left, -1, fs, UiTokens.TextPrimary);
        for (int i = 0; i < lines.Count; i++)
            g.DrawString(UiFonts.Regular, new Vector2(box.Position.X + 11, box.Position.Y + 34 + i * 16), lines[i].Text, HorizontalAlignment.Left, -1, fs, lines[i].Color);
    }
}

/// <summary>Um grupo de barras (um dia): rotulo, linha de baixo (texto ou icone) e as barras lado a lado.</summary>
public sealed class BarGroup
{
    public string Label { get; init; } = "";
    /// <summary>Texto curto embaixo do rotulo (ex.: chance do dia).</summary>
    public string SubLabel { get; init; } = "";
    public Color SubColor { get; init; } = UiTokens.TextSecondary;
    /// <summary>Icone embaixo do rotulo (ex.: motivo do dia), tingido.</summary>
    public string Icon { get; init; } = "";
    public Color IconColor { get; init; } = UiTokens.TextSecondary;
    /// <summary>Hoje: rotulo dourado e sublinhado.</summary>
    public bool Highlight { get; init; }
    public bool LabelGreen { get; init; }
    /// <summary>Losango azul acima das barras (ex.: jogou no horario de trabalho).</summary>
    public bool Marker { get; init; }
    public IReadOnlyList<(int Minutes, Color Color)> Bars { get; init; } = Array.Empty<(int, Color)>();
    public string Title { get; init; } = "";
    public IReadOnlyList<(string Text, Color Color)> Lines { get; init; } = Array.Empty<(string, Color)>();
}

/// <summary>
/// Barras agrupadas por dia (horas no eixo): cada serie e uma barra solida lado a lado, com o
/// valor em cima quando cabe; embaixo, o dia e um texto curto ou um icone. Nada de texto
/// espremido entre as barras: o detalhe fica na leitura ao passar o mouse.
/// </summary>
public partial class GroupedBarChart : Control
{
    public IReadOnlyList<BarGroup> Groups { get; set; } = Array.Empty<BarGroup>();
    public string EmptyText { get; set; } = "Sem dados ainda.";
    private int _hover = -1;
    private const float Left = 34, Top = 18;
    private float Bottom => Groups.Any(g => g.Icon.Length > 0 || g.SubLabel.Length > 0) ? 44 : 24;

    public GroupedBarChart()
    {
        MouseFilter = MouseFilterEnum.Pass;
        MouseExited += () => { _hover = -1; QueueRedraw(); };
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseMotion m || Groups.Count == 0) return;
        float slot = (Size.X - Left) / Groups.Count;
        int i = (int)Mathf.Floor((m.Position.X - Left) / slot);
        int h = i >= 0 && i < Groups.Count ? i : -1;
        if (h != _hover) { _hover = h; QueueRedraw(); }
    }

    private static string HoursText(int minutes) => (minutes / 60.0).ToString(minutes >= 600 ? "0" : "0.0", System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) + "h";

    public override void _Draw()
    {
        var plot = new Rect2(Left, Top, Size.X - Left, Size.Y - Top - Bottom);
        float baseY = Mathf.Floor(plot.End.Y);
        if (Groups.Count == 0)
        {
            DrawLine(new Vector2(plot.Position.X, baseY + 0.5f), new Vector2(plot.End.X, baseY + 0.5f), UiTokens.BorderStrong, 1);
            DrawString(UiFonts.Regular, new Vector2(plot.Position.X, Mathf.Round(plot.GetCenter().Y)), EmptyText, HorizontalAlignment.Center, plot.Size.X, 12, UiTokens.TextDisabled);
            return;
        }
        float max = Mathf.Max(120, Groups.Max(g => g.Bars.Count == 0 ? 0 : g.Bars.Max(b => b.Minutes)));
        float stepMin = max <= 360 ? 120 : 240;
        float nice = Mathf.Ceil(max / stepMin) * stepMin;
        int steps = (int)(nice / stepMin);
        int every = steps > 5 ? 2 : 1;
        for (int g = 0; g <= steps; g += every)
        {
            float y = Mathf.Floor(plot.End.Y - plot.Size.Y * g / steps) + 0.5f;
            DrawLine(new Vector2(plot.Position.X, y), new Vector2(plot.End.X, y), g == 0 ? UiTokens.BorderStrong : UiTokens.Grid, 1);
            DrawString(UiFonts.Regular, new Vector2(0, Mathf.Round(y + 4)), $"{g * stepMin / 60:0}h", HorizontalAlignment.Right, Left - 8, 11, UiTokens.AxisText);
        }
        float slot = plot.Size.X / Groups.Count;
        int labelEvery = slot >= 44 ? 1 : 2;
        for (int i = 0; i < Groups.Count; i++)
        {
            var grp = Groups[i];
            int n = Math.Max(1, grp.Bars.Count);
            const float gap = 3;
            float bw = Mathf.Round(Mathf.Clamp((slot * 0.72f - gap * (n - 1)) / n, 4, 26));
            float groupW = bw * n + gap * (n - 1);
            float gx = Mathf.Round(plot.Position.X + slot * i + (slot - groupW) / 2f), cx = plot.Position.X + slot * i + slot / 2f;
            if (i == _hover) DrawRect(new Rect2(Mathf.Round(plot.Position.X + slot * i + 2), plot.Position.Y - 4, Mathf.Round(slot - 4), plot.Size.Y + 4), new Color(1, 1, 1, 0.03f));
            float topMost = baseY;
            for (int b = 0; b < grp.Bars.Count; b++)
            {
                var (minutes, color) = grp.Bars[b];
                float x = gx + b * (bw + gap);
                float h = Mathf.Round(plot.Size.Y * Mathf.Clamp(minutes / nice, 0, 1));
                var col = i == _hover ? color.Lightened(0.15f) : color;
                if (h >= 2)
                    DrawStyleBox(new StyleBoxFlat { BgColor = col, CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2, AntiAliasing = false }, new Rect2(x, baseY - h, bw, h));
                else if (b == 0) DrawRect(new Rect2(x, baseY - 2, bw, 2), UiTokens.Danger);   // zero na primeira serie
                topMost = Mathf.Min(topMost, baseY - h);
                // Valor em cima quando a barra comporta o texto.
                if (minutes > 0 && slot / n >= 30)
                    DrawString(UiFonts.Numbers, new Vector2(x + bw / 2 - 24, baseY - h - 5), HoursText(minutes), HorizontalAlignment.Center, 48, 11,
                        i == _hover ? UiTokens.TextPrimary : UiTokens.TextLabel);
            }
            if (grp.Marker)
            {
                var c = new Vector2(Mathf.Round(cx), topMost - (slot / n >= 30 ? 22 : 8));
                DrawColoredPolygon(new[] { c + new Vector2(0, -4), c + new Vector2(4, 0), c + new Vector2(0, 4), c + new Vector2(-4, 0) }, UiTokens.Info);
            }
            if (i % labelEvery != (Groups.Count - 1) % labelEvery && i != _hover) continue;
            var lc = grp.Highlight ? UiTokens.Accent : i == _hover ? UiTokens.TextPrimary : grp.LabelGreen ? UiTokens.Success : UiTokens.TextLabel;
            DrawString(UiFonts.SemiBold, new Vector2(cx - 40, baseY + 16), grp.Label, HorizontalAlignment.Center, 80, 12, lc);
            if (grp.Highlight) DrawRect(new Rect2(Mathf.Round(cx - 10), baseY + 19, 20, 2), UiTokens.Accent);
            if (grp.Icon.Length > 0)
                DrawTextureRect(Icons.Get(grp.Icon, 14), new Rect2(Mathf.Round(cx - 7), baseY + 24, 14, 14), false, grp.IconColor);
            else if (grp.SubLabel.Length > 0)
                DrawString(UiFonts.Numbers, new Vector2(cx - 40, baseY + 36), grp.SubLabel, HorizontalAlignment.Center, 80, 11, grp.SubColor);
        }
        if (_hover >= 0)
        {
            var g = Groups[_hover];
            Readout.Draw(this, new Vector2(plot.Position.X + slot * _hover + slot / 2, plot.GetCenter().Y), Size, g.Title, g.Lines);
        }
    }
}

/// <summary>Um dia do historico: tempo livre, tempo jogado, motivo (quando nao jogou) e detalhes.</summary>
public sealed class DayPoint
{
    public string Label { get; init; } = "";
    public bool FreeDay { get; init; }
    public int FreeMinutes { get; init; }
    public int PlayedMinutes { get; init; }
    /// <summary>Cor do motivo do dia (ponto no zero quando nao jogou).</summary>
    public Color ReasonColor { get; init; } = UiTokens.TextSecondary;
    /// <summary>Jogou no horario do trabalho remoto (pausa ou escapada).</summary>
    public bool Work { get; init; }
    /// <summary>Teve um compromisso no dia livre (festa, visita...): marcador roxo no alto.</summary>
    public bool Commitment { get; init; }
    public string Title { get; init; } = "";
    public IReadOnlyList<(string Text, Color Color)> Lines { get; init; } = Array.Empty<(string, Color)>();
}

/// <summary>
/// Tendencia dos ultimos dias: tempo livre como area azul suave, tempo jogado como linha dourada
/// com pontos; dia sem jogo fica no zero, na cor do motivo. Faixas verdes sutis marcam os dias
/// livres (o padrao da semana aparece sozinho). Leitura completa do dia ao passar o mouse.
/// </summary>
public partial class DayTrendChart : Control
{
    public IReadOnlyList<DayPoint> Days { get; set; } = Array.Empty<DayPoint>();
    private int _hover = -1;
    private const float Left = 34, Top = 12, Bottom = 22, Right = 10;

    public DayTrendChart()
    {
        MouseFilter = MouseFilterEnum.Pass;
        MouseExited += () => { _hover = -1; QueueRedraw(); };
    }

    private Rect2 Plot => new(Left, Top, Mathf.Max(10, Size.X - Left - Right), Mathf.Max(10, Size.Y - Top - Bottom));

    /// <summary>
    /// Curva cubica monotona (Fritsch-Carlson) pelos pontos, com X igualmente espacado: arredonda
    /// sem ultrapassar os valores vizinhos (nada abaixo do zero, nenhum pico inventado).
    /// </summary>
    public static Vector2[] Smooth(Vector2[] p, int sub = 10)
    {
        int n = p.Length;
        if (n < 3) return p;
        var d = new float[n - 1];
        for (int i = 0; i < n - 1; i++) d[i] = (p[i + 1].Y - p[i].Y) / Mathf.Max(0.001f, p[i + 1].X - p[i].X);
        var m = new float[n];
        m[0] = d[0];
        m[n - 1] = d[n - 2];
        for (int i = 1; i < n - 1; i++) m[i] = d[i - 1] * d[i] <= 0 ? 0 : (d[i - 1] + d[i]) / 2f;
        for (int i = 0; i < n - 1; i++)
        {
            if (Mathf.Abs(d[i]) < 1e-6f) { m[i] = 0; m[i + 1] = 0; continue; }
            float a = m[i] / d[i], b = m[i + 1] / d[i], s = a * a + b * b;
            if (s > 9)
            {
                float t = 3f / Mathf.Sqrt(s);
                m[i] = t * a * d[i];
                m[i + 1] = t * b * d[i];
            }
        }
        var outPts = new List<Vector2>((n - 1) * sub + 1);
        for (int i = 0; i < n - 1; i++)
        {
            float h = p[i + 1].X - p[i].X;
            for (int k = 0; k < sub; k++)
            {
                float t = k / (float)sub, t2 = t * t, t3 = t2 * t;
                float y = (2 * t3 - 3 * t2 + 1) * p[i].Y + (t3 - 2 * t2 + t) * h * m[i] + (-2 * t3 + 3 * t2) * p[i + 1].Y + (t3 - t2) * h * m[i + 1];
                outPts.Add(new Vector2(p[i].X + h * t, y));
            }
        }
        outPts.Add(p[n - 1]);
        return outPts.ToArray();
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseMotion m || Days.Count == 0) return;
        var plot = Plot;
        float slot = plot.Size.X / Days.Count;
        int i = (int)Mathf.Floor((m.Position.X - plot.Position.X) / slot);
        int h = i >= 0 && i < Days.Count ? i : -1;
        if (h != _hover) { _hover = h; QueueRedraw(); }
    }

    public override void _Draw()
    {
        var plot = Plot;
        float baseY = Mathf.Floor(plot.End.Y) + 0.5f;
        if (Days.Count == 0)
        {
            DrawLine(new Vector2(plot.Position.X, baseY), new Vector2(plot.End.X, baseY), UiTokens.BorderStrong, 1);
            DrawString(UiFonts.Regular, new Vector2(plot.Position.X, Mathf.Round(plot.GetCenter().Y)), "A agenda aparece depois do primeiro dia.", HorizontalAlignment.Center, plot.Size.X, 12, UiTokens.TextDisabled);
            return;
        }
        int n = Days.Count;
        float slot = plot.Size.X / n;
        float X(int i) => plot.Position.X + slot * i + slot / 2f;
        float max = Mathf.Max(240, Days.Max(d => Mathf.Max(d.FreeMinutes, d.PlayedMinutes)));
        float nice = Mathf.Ceil(max / 240f) * 240;
        float Y(float minutes) => plot.End.Y - plot.Size.Y * Mathf.Clamp(minutes / nice, 0, 1);
        // Dias livres: faixa verde bem sutil (fim de semana, folga, feriado).
        for (int i = 0; i < n; i++)
            if (Days[i].FreeDay)
                DrawRect(new Rect2(Mathf.Round(plot.Position.X + slot * i), plot.Position.Y, Mathf.Ceil(slot), plot.Size.Y), new Color(UiTokens.Success, 0.06f));
        int steps = (int)(nice / 240);
        int every = steps > 4 ? 2 : 1;
        for (int g = 0; g <= steps; g += every)
        {
            float y = Mathf.Floor(plot.End.Y - plot.Size.Y * g / steps) + 0.5f;
            DrawLine(new Vector2(plot.Position.X, y), new Vector2(plot.End.X, y), g == 0 ? UiTokens.BorderStrong : UiTokens.Grid, 1);
            DrawString(UiFonts.Regular, new Vector2(0, Mathf.Round(y + 4)), $"{g * 4}h", HorizontalAlignment.Right, Left - 8, 11, UiTokens.AxisText);
        }
        // Rotulos do eixo X: sem sobrepor, sempre o ultimo dia.
        int step = Math.Max(1, (int)Mathf.Ceil(44f / slot));
        for (int i = n - 1; i >= 0; i -= step)
            DrawString(UiFonts.Regular, new Vector2(X(i) - 24, Size.Y - 5), Days[i].Label, HorizontalAlignment.Center, 48, 11, i == _hover ? UiTokens.TextPrimary : UiTokens.AxisText);
        if (_hover >= 0)
        {
            float hx = Mathf.Floor(X(_hover)) + 0.5f;
            DrawLine(new Vector2(hx, plot.Position.Y), new Vector2(hx, plot.End.Y), new Color(UiTokens.TextSecondary, 0.35f), 1);
        }
        var free = new Vector2[n];
        var played = new Vector2[n];
        for (int i = 0; i < n; i++)
        {
            free[i] = new Vector2(X(i), Y(Days[i].FreeMinutes));
            played[i] = new Vector2(X(i), Y(Days[i].PlayedMinutes));
        }
        // Tempo livre: area azul com degrade e contorno fino; jogado: linha dourada. Curvas suaves
        // monotonas (passam pelos pontos sem inventar picos nem descer abaixo do zero).
        if (n >= 2)
        {
            var freeCurve = Smooth(free);
            var playedCurve = Smooth(played);
            int m = freeCurve.Length;
            var poly = new Vector2[m + 2];
            var cols = new Color[m + 2];
            for (int i = 0; i < m; i++) { poly[i] = freeCurve[i]; cols[i] = new Color(UiTokens.Info, 0.20f); }
            poly[m] = new Vector2(freeCurve[m - 1].X, plot.End.Y); cols[m] = new Color(UiTokens.Info, 0.03f);
            poly[m + 1] = new Vector2(freeCurve[0].X, plot.End.Y); cols[m + 1] = new Color(UiTokens.Info, 0.03f);
            DrawPolygon(poly, cols);
            DrawPolyline(freeCurve, new Color(UiTokens.Info, 0.85f), 1.5f, true);
            DrawPolyline(playedCurve, UiTokens.Accent, 2.2f, true);
        }
        // Compromisso do dia livre: marcador roxo no alto da coluna do dia.
        for (int i = 0; i < n; i++)
            if (Days[i].Commitment)
            {
                var c = new Vector2(Mathf.Round(X(i)), plot.Position.Y + 5);
                DrawCircle(c, 4.5f, UiTokens.CardBottom);
                DrawCircle(c, 3.2f, UiTokens.Exceptional);
            }
        // Pontos: dourado quando jogou; no zero, na cor do motivo, quando nao jogou.
        for (int i = 0; i < n; i++)
        {
            var d = Days[i];
            float r = i == _hover ? 4.5f : 3.2f;
            var col = d.PlayedMinutes > 0 ? UiTokens.Accent : d.ReasonColor;
            DrawCircle(played[i], r + 1.5f, UiTokens.CardBottom);
            DrawCircle(played[i], r, col);
            if (d.Work)
            {
                var c = played[i] + new Vector2(0, -11);
                DrawColoredPolygon(new[] { c + new Vector2(0, -4), c + new Vector2(4, 0), c + new Vector2(0, 4), c + new Vector2(-4, 0) }, UiTokens.Info);
            }
        }
        if (_hover >= 0)
        {
            var d = Days[_hover];
            Readout.Draw(this, new Vector2(X(_hover), plot.GetCenter().Y), Size, d.Title, d.Lines);
        }
    }
}

/// <summary>Sono de cada noite contra a necessidade (tracejado): curta em laranja, recuperacao em verde.</summary>
public partial class SleepChart : Control
{
    public IReadOnlyList<(int Minutes, string Label, string Title, IReadOnlyList<(string Text, Color Color)> Lines)> Nights { get; set; }
        = Array.Empty<(int, string, string, IReadOnlyList<(string, Color)>)>();
    public int NeedMinutes { get; set; } = 480;
    private int _hover = -1;
    private const float Left = 30, Bottom = 18, Top = 6;

    public SleepChart()
    {
        MouseFilter = MouseFilterEnum.Pass;
        MouseExited += () => { _hover = -1; QueueRedraw(); };
    }

    public static Color ColorFor(int minutes, int need) => minutes < need - 30 ? UiTokens.Warning : minutes > need + 30 ? UiTokens.Success : UiTokens.Info;

    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseMotion m || Nights.Count == 0) return;
        float slot = (Size.X - Left) / Nights.Count;
        int i = (int)Mathf.Floor((m.Position.X - Left) / slot);
        int h = i >= 0 && i < Nights.Count ? i : -1;
        if (h != _hover) { _hover = h; QueueRedraw(); }
    }

    public override void _Draw()
    {
        var plot = new Rect2(Left, Top, Size.X - Left, Size.Y - Top - Bottom);
        float top = 12 * 60;   // escala fixa de 0 a 12 h: compara pessoas e dias
        float Y(float minutes) => Mathf.Floor(plot.End.Y - plot.Size.Y * Mathf.Clamp(minutes / top, 0, 1));
        foreach (int hr in new[] { 0, 4, 8, 12 })
        {
            float y = Y(hr * 60) + 0.5f;
            DrawLine(new Vector2(plot.Position.X, y), new Vector2(plot.End.X, y), hr == 0 ? UiTokens.BorderStrong : UiTokens.Grid, 1);
            DrawString(UiFonts.Regular, new Vector2(0, Mathf.Round(y + 4)), $"{hr}h", HorizontalAlignment.Right, Left - 7, 11, UiTokens.AxisText);
        }
        if (Nights.Count == 0)
        {
            DrawString(UiFonts.Regular, new Vector2(plot.Position.X, Mathf.Round(plot.GetCenter().Y)), "Sem noites registradas ainda.", HorizontalAlignment.Center, plot.Size.X, 12, UiTokens.TextDisabled);
            return;
        }
        float slot = plot.Size.X / Nights.Count, cw = Mathf.Round(Mathf.Clamp(slot * 0.58f, 5, 22));
        for (int i = 0; i < Nights.Count; i++)
        {
            var n = Nights[i];
            float x = Mathf.Round(plot.Position.X + slot * i + (slot - cw) / 2f);
            float y = Y(n.Minutes);
            var col = ColorFor(n.Minutes, NeedMinutes);
            if (i == _hover) col = col.Lightened(0.18f);
            if (plot.End.Y - y >= 2)
                DrawStyleBox(new StyleBoxFlat { BgColor = col, CornerRadiusTopLeft = 2, CornerRadiusTopRight = 2, AntiAliasing = false }, new Rect2(x, y, cw, Mathf.Floor(plot.End.Y) - y));
            int labelStep = Math.Max(1, (int)Mathf.Ceil(26f / slot));
            if ((Nights.Count - 1 - i) % labelStep == 0 || i == _hover)
                DrawString(UiFonts.Regular, new Vector2(x - 16, Size.Y - 3), n.Label, HorizontalAlignment.Center, cw + 32, 11, i == _hover ? UiTokens.TextPrimary : UiTokens.AxisText);
        }
        float ny = Y(NeedMinutes) + 0.5f;
        DrawDashedLine(new Vector2(plot.Position.X, ny), new Vector2(plot.End.X, ny), new Color(UiTokens.TextPrimary, 0.7f), 1, 4);
        if (_hover >= 0)
        {
            var n = Nights[_hover];
            Readout.Draw(this, new Vector2(plot.Position.X + slot * _hover + slot / 2, plot.GetCenter().Y), Size, n.Title, n.Lines);
        }
    }
}

/// <summary>
/// Jornada do dia (Ocupacao, D-068): uma faixa de 0 a 24 h por dia mostrado (um dia com obrigacao e um dia
/// livre), com os trechos da rotina desta pessoa no horario local: sono, obrigacao, deslocamento, tempo livre e
/// pausa. O que nao tem trecho (preparo, refeicao, espera) fica no fundo. Embaixo, as horas e a legenda; o
/// mouse num trecho mostra o nome e o horario. Desenho estatico.
/// </summary>
public partial class DayTimeline : Control
{
    public sealed record Row(string Label, Color LabelColor, string Title, IReadOnlyList<(int Start, int End, string Kind)> Segments, bool FreeDay);

    public IReadOnlyList<Row> Rows { get; set; } = Array.Empty<Row>();
    /// <summary>Nome de um tipo de trecho ("work" → "Trabalho").</summary>
    public Func<string, string> NameOf { get; set; } = k => k;

    private const float LabelW = 38, RowH = 12, RowGap = 7, AxisH = 16, LegendH = 13;
    private static readonly Color Rest = new("#262B33");

    public DayTimeline()
    {
        MouseFilter = MouseFilterEnum.Stop;
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        TooltipText = "Jornada do dia";
    }

    /// <summary>Quantos dias cabem na altura recebida (no minimo um): com pouco espaco, so o dia com obrigacao.</summary>
    private int RowsShown => Math.Clamp((int)((Size.Y - AxisH - LegendH + RowGap) / (RowH + RowGap)), 1, Math.Max(1, Rows.Count));
    private float RowsHeight => RowsShown * RowH + (RowsShown - 1) * RowGap;

    public override Vector2 _GetMinimumSize() => new(220, RowH + AxisH + LegendH);

    /// <summary>Grupo de cor dos trechos da linha do tempo do dia (R1).</summary>
    private static string Group(string kind) => kind switch
    {
        "sleep" or "free" or "break" or "commitment" => kind,
        "commute" or "transfer" => "commute",
        "prep" or "arrival" or "meal" => "personal",
        "wait" or "awake" => "wait",
        _ => "obligation",   // trabalho, estudo, cuidados da casa e da familia, remoto
    };

    public static Color ColorOf(string kind, bool freeDay) => Group(kind) switch
    {
        "sleep" => new Color("#5867A8"),
        "commute" => new Color("#6B7380"),
        "personal" => new Color("#4A515C"),
        "wait" => new Color("#343A44"),
        "commitment" => UiTokens.Exceptional,
        "free" => freeDay ? UiTokens.Success : UiTokens.Info,
        "break" => new Color("#9CC9F2"),
        _ => new Color("#A9B0BA"),
    };

    private static string Clock(int minute) { int m = ((minute % 1440) + 1440) % 1440; return $"{m / 60:00}:{m % 60:00}"; }

    public override void _Draw()
    {
        float x0 = LabelW, w = Mathf.Max(1, Size.X - LabelW);
        var present = new HashSet<string>(StringComparer.Ordinal);
        bool rest = false;
        for (int i = 0; i < RowsShown; i++)
        {
            var r = Rows[i];
            float y = i * (RowH + RowGap);
            DrawString(UiFonts.SemiBold, new Vector2(0, y + RowH - 1), r.Label, HorizontalAlignment.Left, LabelW - 4, 12, r.LabelColor);
            DrawRect(new Rect2(x0, y, w, RowH), Rest);
            int covered = 0;
            foreach (var (s, e, k) in r.Segments)
            {
                int a = Math.Clamp(s, 0, 1440), b = Math.Clamp(e, 0, 1440);
                if (b <= a) continue;
                covered += b - a;
                present.Add(Group(k) == "free" ? (r.FreeDay ? "free_day" : "free") : Group(k));
                DrawRect(new Rect2(x0 + w * a / 1440f, y, Mathf.Max(1, w * (b - a) / 1440f), RowH), ColorOf(k, r.FreeDay));
            }
            if (covered < 1440) rest = true;
        }
        // Horas: marca a cada 3 h, rotulo a cada 6 h.
        float ay = RowsHeight;
        for (int h = 0; h <= 24; h += 3)
        {
            float x = Mathf.Round(x0 + w * h / 24f);
            DrawLine(new Vector2(x, ay + 2), new Vector2(x, ay + 5), UiTokens.BorderStrong, 1);
            if (h % 6 != 0) continue;
            string t = $"{h:00}h";
            float tw = UiFonts.Regular.GetStringSize(t, HorizontalAlignment.Left, -1, 10).X;
            float tx = Mathf.Clamp(x - tw / 2, x0, x0 + w - tw);
            DrawString(UiFonts.Regular, new Vector2(tx, ay + 15), t, HorizontalAlignment.Left, -1, 10, UiTokens.AxisText);
        }
        // Legenda numa linha, so com o que aparece; o que nao couber fica de fora (esta no tooltip).
        var legend = new List<(string Name, Color Color)>();
        if (present.Contains("sleep")) legend.Add(("Sono", ColorOf("sleep", false)));
        if (present.Contains("obligation")) legend.Add(("Obrigação", ColorOf("work", false)));
        if (present.Contains("commute")) legend.Add(("Deslocamento", ColorOf("commute", false)));
        if (present.Contains("personal")) legend.Add(("Cuidados pessoais", ColorOf("prep", false)));
        if (present.Contains("commitment")) legend.Add(("Compromisso", ColorOf("commitment", false)));
        if (present.Contains("free")) legend.Add(("Livre", ColorOf("free", false)));
        if (present.Contains("free_day")) legend.Add(("Livre (dia livre)", ColorOf("free", true)));
        if (present.Contains("break")) legend.Add(("Pausa (pode jogar)", ColorOf("break", false)));
        if (present.Contains("wait") || rest) legend.Add(("Espera e brechas", ColorOf("wait", false)));
        float lx = x0, ly = ay + AxisH + LegendH - 3;
        foreach (var (name, color) in legend)
        {
            float tw = UiFonts.Medium.GetStringSize(name, HorizontalAlignment.Left, -1, 11).X;
            if (lx + 14 + tw > x0 + w) break;
            DrawRect(new Rect2(lx, ly - 8, 8, 8), color);
            DrawString(UiFonts.Medium, new Vector2(lx + 12, ly), name, HorizontalAlignment.Left, -1, 11, UiTokens.TextSecondary);
            lx += 12 + tw + 16;
        }
    }

    public override string _GetTooltip(Vector2 at)
    {
        float x0 = LabelW, w = Mathf.Max(1, Size.X - LabelW);
        for (int i = 0; i < RowsShown; i++)
        {
            float y = i * (RowH + RowGap);
            if (at.Y < y - RowGap / 2 || at.Y > y + RowH + RowGap / 2) continue;
            var r = Rows[i];
            int minute = (int)Math.Clamp((at.X - x0) / w * 1440, 0, 1439);
            foreach (var (s, e, k) in r.Segments)
                if (minute >= s && minute < e)
                    return $"{NameOf(k)}|{Clock(s)}–{Clock(e)}\n^{r.Title}";
            return $"Espera e brechas\n^{r.Title}\n~Fora das obrigações e do tempo livre.";
        }
        return $"Jornada do dia\n~Das 0 às 24 h, no horário desta pessoa. Passe o mouse num trecho para ver o horário.";
    }

    public override GodotObject _MakeCustomTooltip(string forText) => RichTip.Make(forText);
}

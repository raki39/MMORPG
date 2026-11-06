using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Pagina em secoes de tela inteira (D-068). Cada secao ocupa exatamente a altura visivel do corpo da
/// pagina; dentro dela, as linhas dividem a altura por peso, cada uma com um minimo legivel, e os cards
/// preenchem a linha (a altura vem do layout, nunca do conteudo: o card e igual para qualquer jogador).
/// Se as linhas de uma secao nao cabem na tela (1366×768), a secao continua na tela seguinte; uma linha
/// de cards que empilha na largura estreita vira uma linha por card. A rolagem da pagina salta de tela
/// em tela (ModalFrame); o conteudo que passa do card rola dentro dele.
/// </summary>
public partial class ScreenPage : Container
{
    /// <summary>Vao entre linhas e entre telas (px).</summary>
    public int Gap { get; set; } = 12;
    public int ScreenGap { get; set; } = 16;

    private sealed class Row
    {
        public Control Node = null!;
        public int Weight;
        public int Min;
        public int Section;
    }

    private readonly List<Row> _rows = new();
    private int _section;
    private ScrollContainer? _scroll;
    private readonly List<float> _screenTops = new();

    /// <summary>Comeca uma secao nova (a proxima linha abre uma tela).</summary>
    public ScreenPage Section()
    {
        if (_rows.Count > 0) _section++;
        return this;
    }

    /// <summary>Linha da secao atual: peso na altura da tela e altura minima (px).</summary>
    public ScreenPage Add(Control row, int weight = 1, int min = 280)
    {
        row.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _rows.Add(new Row { Node = row, Weight = Math.Max(1, weight), Min = Math.Max(0, min), Section = _section });
        AddChild(row);
        return this;
    }

    /// <summary>Topo de cada tela (coordenadas da pagina), para a rolagem saltar de tela em tela.</summary>
    public IReadOnlyList<float> ScreenTops => _screenTops;

    /// <summary>Altura visivel: a do ScrollContainer que contem a pagina.</summary>
    private float View => _scroll != null && _scroll.Size.Y > 0 ? _scroll.Size.Y : Mathf.Max(Size.Y, 600);

    public override void _EnterTree()
    {
        for (Node? n = GetParent(); n != null; n = n.GetParent())
            if (n is ScrollContainer s) { _scroll = s; break; }
        if (_scroll != null) _scroll.Resized += OnViewResized;
    }

    public override void _ExitTree()
    {
        if (_scroll != null) _scroll.Resized -= OnViewResized;
        _scroll = null;
    }

    private void OnViewResized()
    {
        UpdateMinimumSize();
        QueueSort();
    }

    // Unidade de layout: uma linha, ou um card de uma linha empilhada (Card >= 0).
    private readonly record struct Unit(Row Row, int Card, float Min, int Weight);

    private sealed class Placed
    {
        public Row Row = null!;
        public int Card;
        public float Y, H;
    }

    private List<Unit> Units(float width)
    {
        var units = new List<Unit>();
        foreach (var r in _rows)
        {
            if (!r.Node.Visible) continue;
            if (r.Node is SpanRow sr && !sr.FitsSideBySide(width))
            {
                var cards = sr.VisibleItems;
                for (int i = 0; i < cards.Count; i++)
                    units.Add(new Unit(r, i, Mathf.Max(r.Min, cards[i].GetCombinedMinimumSize().Y), r.Weight));
            }
            else units.Add(new Unit(r, -1, Mathf.Max(r.Min, r.Node.GetCombinedMinimumSize().Y), r.Weight));
        }
        return units;
    }

    /// <summary>Distribui as unidades em telas e calcula y e altura de cada uma.</summary>
    private (List<Placed> Placed, float Height) Plan(float width)
    {
        float view = View;
        var units = Units(width);
        var screens = new List<List<Unit>>();
        float used = 0;
        int section = -1;
        foreach (var u in units)
        {
            bool fits = screens.Count > 0 && used + Gap + u.Min <= view + 0.5f;
            if (screens.Count == 0 || u.Row.Section != section || !fits)
            {
                screens.Add(new List<Unit>());
                used = 0;
                section = u.Row.Section;
            }
            else used += Gap;
            screens[^1].Add(u);
            used += u.Min;
        }
        var placed = new List<Placed>();
        _screenTops.Clear();
        float y = 0;
        foreach (var s in screens)
        {
            if (_screenTops.Count > 0) y += ScreenGap;
            _screenTops.Add(y);
            float mins = s.Sum(u => u.Min) + Gap * (s.Count - 1);
            float height = Mathf.Max(view, mins);
            var hs = Distribute(s, height - Gap * (s.Count - 1));
            float at = y;
            for (int i = 0; i < s.Count; i++)
            {
                placed.Add(new Placed { Row = s[i].Row, Card = s[i].Card, Y = at, H = hs[i] });
                at += hs[i] + Gap;
            }
            y += height;
        }
        return (placed, y);
    }

    /// <summary>Altura por peso, respeitando o minimo de cada linha; pixels inteiros, o resto na ultima.</summary>
    private static float[] Distribute(List<Unit> s, float available)
    {
        var h = new float[s.Count];
        var free = Enumerable.Range(0, s.Count).ToList();
        float left = available;
        bool changed = true;
        while (changed && free.Count > 0)
        {
            changed = false;
            int weights = free.Sum(i => s[i].Weight);
            foreach (int i in free.ToList())
            {
                float share = left * s[i].Weight / weights;
                if (share < s[i].Min) { h[i] = s[i].Min; left -= s[i].Min; free.Remove(i); changed = true; }
            }
        }
        if (free.Count > 0)
        {
            int weights = free.Sum(i => s[i].Weight);
            foreach (int i in free) h[i] = Mathf.Floor(left * s[i].Weight / weights);
            h[free[^1]] += available - h.Sum();
        }
        return h;
    }

    public override Vector2 _GetMinimumSize()
    {
        float w = 0;
        foreach (var r in _rows) if (r.Node.Visible) w = Mathf.Max(w, r.Node.GetCombinedMinimumSize().X);
        float width = Size.X > 0 ? Size.X : _scroll != null && _scroll.Size.X > 0 ? _scroll.Size.X - 14 : w;
        return new Vector2(w, Plan(Mathf.Max(width, w)).Height);
    }

    public override void _Notification(int what)
    {
        if (what != NotificationSortChildren) return;
        var (placed, _) = Plan(Size.X);
        foreach (var g in placed.GroupBy(p => p.Row))
        {
            var list = g.ToList();
            float top = list.Min(p => p.Y), bottom = list.Max(p => p.Y + p.H);
            if (g.Key.Node is SpanRow sr)
                sr.StackRects = list[0].Card >= 0 ? list.OrderBy(p => p.Card).Select(p => (p.Y - top, p.H)).ToList() : null;
            FitChildInRect(g.Key.Node, new Rect2(0, top, Size.X, bottom - top));
        }
    }
}

using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Pagina em secoes (D-068). Cada linha tem a altura natural do seu conteudo (listas inteiras, graficos na
/// altura preferida). A secao so ocupa a tela inteira quando tem conteudo para isso (pedido do criador): se o
/// natural passa da tela, a linha que precisa de menos fica no tamanho dela e a sobra vai para as que tem mais
/// (listas longas ganham paginas); se o natural fica perto da tela (FillThreshold) ou ha outra secao embaixo, a
/// secao estica ate a tela, para a de baixo nao aparecer cortada; senao, fica no tamanho natural, sem esticar
/// cards. A ultima tela, se for mais baixa que a janela, ganha folga embaixo: a rolagem para com ela no topo,
/// sem mostrar a tela de cima cortada.
/// Em tela baixa (1366×768) a secao mantem as suas linhas, com cards mais baixos e rolagem dentro do card;
/// so passa para a tela seguinte se uma linha ficaria abaixo do piso (180 px). Uma linha de cards que
/// empilha na largura estreita vira uma linha por card. A rolagem da pagina salta de tela em tela
/// (ModalFrame); o conteudo que passa do card rola dentro dele.
/// </summary>
public partial class ScreenPage : Container
{
    /// <summary>Vao entre linhas e entre telas (px).</summary>
    public int Gap { get; set; } = 12;
    public int ScreenGap { get; set; } = 16;
    /// <summary>Fracao da tela a partir da qual a ultima secao estica ate a tela (abaixo disso fica natural).</summary>
    public float FillThreshold { get; set; } = 0.9f;

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

    /// <summary>
    /// Linha da secao atual: peso na altura da tela e piso (px). Abaixo do piso a linha passa para a tela
    /// seguinte; acima dele, a secao fica junta e o conteudo que passar rola dentro do card.
    /// </summary>
    public ScreenPage Add(Control row, int weight = 1, int min = 180)
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
    private readonly record struct Unit(Row Row, int Card, float Min, float Natural, int Weight);

    /// <summary>
    /// Altura que o conteudo pede alem do minimo: listas que cabem (FitList) inteiras, graficos na altura
    /// preferida (meta preferred_height, posta por Grow), o conteudo inteiro dos cards de altura fixa.
    /// </summary>
    private static float Extra(Control c)
    {
        if (!c.Visible) return 0;
        float min = c.GetCombinedMinimumSize().Y;
        if (c is FitList f) return Mathf.Max(0, f.NaturalHeight - min);
        float own = c.HasMeta("preferred_height") ? Mathf.Max(0, (float)c.GetMeta("preferred_height") - min) : 0;
        if (c is ScrollContainer sc)
        {
            if (sc.GetChildCount() > 0 && sc.GetChild(0) is Control child)
                own = Mathf.Max(own, child.GetCombinedMinimumSize().Y + Extra(child) - min);
            return own;
        }
        float sum = 0, max = 0;
        foreach (var k in c.GetChildren().OfType<Control>())
        {
            float e = Extra(k);
            sum += e;
            max = Mathf.Max(max, e);
        }
        return Mathf.Max(own, c is VBoxContainer ? sum : max);
    }

    private static float Natural(Control c, float floor) => Mathf.Max(floor, c.GetCombinedMinimumSize().Y + Extra(c));

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
                {
                    float min = Mathf.Max(r.Min, cards[i].GetCombinedMinimumSize().Y);
                    units.Add(new Unit(r, i, min, Natural(cards[i], min), r.Weight));
                }
            }
            else
            {
                float min = Mathf.Max(r.Min, r.Node.GetCombinedMinimumSize().Y);
                units.Add(new Unit(r, -1, min, Natural(r.Node, min), r.Weight));
            }
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
        for (int k = 0; k < screens.Count; k++)
        {
            var s = screens[k];
            if (_screenTops.Count > 0) y += ScreenGap;
            _screenTops.Add(y);
            float gaps = Gap * (s.Count - 1);
            float avail = Mathf.Max(view, s.Sum(u => u.Min) + gaps) - gaps;
            // Estica ate a tela se houver outra tela embaixo (nao aparece cortada) ou se o natural ja quase a enche.
            bool fill = k < screens.Count - 1 || s.Sum(u => u.Natural) >= FillThreshold * avail;
            var hs = Distribute(s, avail, fill);
            float height = hs.Sum() + gaps;
            float at = y;
            for (int i = 0; i < s.Count; i++)
            {
                placed.Add(new Placed { Row = s[i].Row, Card = s[i].Card, Y = at, H = hs[i] });
                at += hs[i] + Gap;
            }
            y += height;
        }
        // Com mais de uma tela, a ultima comeca no topo da janela mesmo sendo mais baixa: folga embaixo, em vez
        // de a rolagem parar no fim e mostrar a tela de cima cortada.
        if (_screenTops.Count > 1) y = Mathf.Max(y, _screenTops[^1] + view);
        return (placed, y);
    }

    /// <summary>
    /// Altura de cada linha (pixels inteiros). Se o natural cabe: cada linha no seu natural e, com fill, a sobra
    /// dividida por peso. Se nao cabe: as linhas que pedem menos que a sua parte ficam no natural, as outras
    /// dividem o resto por peso, sem ficar abaixo do minimo.
    /// </summary>
    private static float[] Distribute(List<Unit> s, float available, bool fill)
    {
        var h = new float[s.Count];
        float need = s.Sum(u => u.Natural);
        if (need <= available)
        {
            for (int i = 0; i < s.Count; i++) h[i] = Mathf.Floor(s[i].Natural);
            if (fill)
            {
                float extra = available - h.Sum();
                int w = s.Sum(u => u.Weight);
                for (int i = 0; i < s.Count; i++) h[i] += Mathf.Floor(extra * s[i].Weight / w);
                h[^1] += available - h.Sum();
            }
            return h;
        }
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
                float fixedH = share >= s[i].Natural ? s[i].Natural : share < s[i].Min ? s[i].Min : -1;
                if (fixedH < 0) continue;
                h[i] = Mathf.Floor(fixedH);
                left -= h[i];
                free.Remove(i);
                changed = true;
                break;   // recalcula as partes com o que sobrou
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

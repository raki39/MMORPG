using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Pagina de dossie (D-034), no estilo das telas de um jogo de gestao: ocupa a area abaixo da
/// barra superior (o mapa some e o tempo pausa), cabecalho com identidade e faixa de dados,
/// abas de texto com as acoes a direita e corpo com cards. Navegacao (voltar/avancar e menu)
/// fica na barra superior. Esc volta ao mapa. Abas "ao vivo" se atualizam sem perder a rolagem.
/// </summary>
public partial class ModalFrame : Control
{
    public event Action? Closed;

    private readonly PanelContainer _panel;
    private readonly List<(string Name, Func<Control> Build, bool Live)> _tabs = new();
    private readonly List<Button> _tabButtons = new();
    private readonly HBoxContainer _tabBar;
    private readonly ScrollContainer _scroll;
    private readonly HBoxContainer _facts;
    private readonly Label _crumb;
    private int _current = -1;

    public VBoxContainer Header { get; }
    public HBoxContainer Actions { get; }
    public int CurrentTab => _current;
    public int TabCount => _tabs.Count;

    /// <summary>Trilha discreta acima do titulo (ex.: "Mundo › Vila Aurora › Praça").</summary>
    public string Breadcrumb { set => _crumb.Text = value; }

    /// <summary>Retrato ou emblema a esquerda do cabecalho (ocupa a altura toda).</summary>
    public HBoxContainer Art { get; }
    /// <summary>Destaque a direita da faixa de dados (ex.: medidor central).</summary>
    public HBoxContainer Highlight { get; }

    public ModalFrame(Color accent)
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        OffsetTop = UiTokens.TopBarHeight;
        MouseFilter = MouseFilterEnum.Stop;
        AddChild(new PageBackdrop());

        _panel = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        _panel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_panel);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 0);
        _panel.AddChild(col);

        // Cabecalho (kit D-034): retrato a esquerda ocupando a altura; a direita, trilha,
        // identidade + faixa de dados + destaque e, embaixo, as abas alinhadas ao nome.
        var head = new PanelContainer();
        head.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(UiTokens.Surface, 0.78f), BorderColor = UiTokens.Divider, BorderWidthBottom = 1,
            ContentMarginLeft = 24, ContentMarginRight = 24, ContentMarginTop = 14, ContentMarginBottom = 0,
        });
        col.AddChild(head);
        var headRow = new HBoxContainer();
        headRow.AddThemeConstantOverride("separation", 20);
        head.AddChild(headRow);
        Art = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkBegin };
        headRow.AddChild(Art);
        var headCol = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        headCol.AddThemeConstantOverride("separation", 6);
        headRow.AddChild(headCol);
        _crumb = ThemeFactory.Text("", UiTokens.FontCaption, UiTokens.TextDisabled, UiFonts.Medium);
        headCol.AddChild(_crumb);
        var top = new HBoxContainer();
        top.AddThemeConstantOverride("separation", 28);
        headCol.AddChild(top);
        Header = new VBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter, CustomMinimumSize = new Vector2(280, 0) };
        Header.AddThemeConstantOverride("separation", 2);
        top.AddChild(Header);
        _facts = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _facts.AddThemeConstantOverride("separation", 0);
        top.AddChild(_facts);
        Highlight = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        top.AddChild(Highlight);

        var tabsRow = new HBoxContainer();
        tabsRow.AddThemeConstantOverride("separation", 12);
        headCol.AddChild(tabsRow);
        _tabBar = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _tabBar.AddThemeConstantOverride("separation", 2);
        tabsRow.AddChild(_tabBar);
        Actions = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        Actions.AddThemeConstantOverride("separation", 6);
        tabsRow.AddChild(Actions);

        _scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = SizeFlags.ExpandFill };
        var body = new MarginContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("margin_left", 16);
        body.AddThemeConstantOverride("margin_right", 16);
        body.AddThemeConstantOverride("margin_top", 16);
        body.AddThemeConstantOverride("margin_bottom", 16);
        body.AddChild(_scroll);
        col.AddChild(body);
    }

    /// <summary>Dado-chave do cabecalho: valor em cima, rotulo embaixo, separado por filete.</summary>
    public void Fact(string label, string value, Color? color = null, string tooltip = "", string icon = "")
    {
        if (_facts.GetChildCount() > 0)
            _facts.AddChild(new ColorRect { Color = UiTokens.Divider, CustomMinimumSize = new Vector2(1, 34), SizeFlagsVertical = SizeFlags.ShrinkCenter });
        var cell = new TipPanel { TooltipText = tooltip, MouseFilter = tooltip.Length > 0 ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore };
        cell.AddThemeStyleboxOverride("panel", new StyleBoxEmpty { ContentMarginLeft = 18, ContentMarginRight = 18 });
        var c = new VBoxContainer();
        c.AddThemeConstantOverride("separation", 1);
        var v = new HBoxContainer();
        v.AddThemeConstantOverride("separation", 7);
        if (icon.Length > 0) v.AddChild(new Icon(icon, 16, color ?? UiTokens.TextSecondary));
        else if (color is { } dot && dot != UiTokens.TextPrimary && dot != UiTokens.TextSecondary)
            v.AddChild(new ColorRect { Color = dot, CustomMinimumSize = new Vector2(7, 7), SizeFlagsVertical = SizeFlags.ShrinkCenter });
        v.AddChild(ThemeFactory.Text(value, 15, UiTokens.TextPrimary, UiFonts.Numbers));
        c.AddChild(v);
        c.AddChild(ThemeFactory.Text(label, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium));
        cell.AddChild(c);
        _facts.AddChild(cell);
    }

    public override void _Ready()
    {
        Modulate = new Color(1, 1, 1, 0);
        CreateTween().TweenProperty(this, "modulate:a", 1f, 0.12f);
    }

    public void AddTab(string name, Func<Control> build, bool live = false)
    {
        int index = _tabs.Count;
        _tabs.Add((name, build, live));
        var b = new Button { Text = name, ToggleMode = true, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 42) };
        b.AddThemeFontOverride("font", UiFonts.Medium);
        b.AddThemeFontSizeOverride("font_size", 14);
        StyleBoxFlat Tab(bool active, bool hover) => new()
        {
            BgColor = new Color(0, 0, 0, 0),
            BorderColor = active ? UiTokens.Accent : hover ? UiTokens.BorderStrong : new Color(0, 0, 0, 0), BorderWidthBottom = 2,
            ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 8, ContentMarginBottom = 10,
        };
        b.AddThemeStyleboxOverride("normal", Tab(false, false));
        b.AddThemeStyleboxOverride("hover", Tab(false, true));
        b.AddThemeStyleboxOverride("pressed", Tab(true, false));
        b.AddThemeStyleboxOverride("hover_pressed", Tab(true, true));
        b.AddThemeColorOverride("font_color", UiTokens.TextLabel);
        b.AddThemeColorOverride("font_hover_color", UiTokens.TextPrimary);
        b.AddThemeColorOverride("font_pressed_color", UiTokens.Accent);
        b.AddThemeColorOverride("font_hover_pressed_color", UiTokens.AccentSoft);
        b.Pressed += () => SelectTab(index);
        _tabButtons.Add(b);
        _tabBar.AddChild(b);
        if (_current < 0) SelectTab(0);
    }

    public void SelectTab(int index)
    {
        _current = index;
        for (int i = 0; i < _tabButtons.Count; i++) _tabButtons[i].SetPressedNoSignal(i == index);
        Rebuild(resetScroll: true);
    }

    public void RefreshLive()
    {
        if (_current >= 0 && _tabs[_current].Live) Rebuild(resetScroll: false);
    }

    public void Rebuild(bool resetScroll)
    {
        int scroll = _scroll.ScrollVertical;
        foreach (var c in _scroll.GetChildren()) { _scroll.RemoveChild(c); c.QueueFree(); }
        var content = _tabs[_current].Build();
        content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        _scroll.AddChild(content);
        if (!resetScroll) CallDeferred(nameof(RestoreScroll), scroll);
    }

    private void RestoreScroll(int value) => _scroll.ScrollVertical = value;

    /// <summary>Rola o corpo da pagina (capturas automaticas).</summary>
    public void ScrollTo(int value) => CallDeferred(nameof(RestoreScroll), value);

    /// <summary>Sai da pagina (o cliente volta ao mapa).</summary>
    public void Close()
    {
        Closed?.Invoke();
        QueueFree();
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            Close();
            GetViewport().SetInputAsHandled();
        }
    }

    // ---------- Blocos reutilizaveis ----------

    /// <summary>
    /// Card de dados (kit D-034): CardPanel com degrade e hover, titulo dourado de 14 px,
    /// conteudo opcional a direita do titulo e icone de informacao. accent: compatibilidade.
    /// </summary>
    public static PanelContainer Card(string title, Control body, Color? accent = null, string info = "", Control? headerRight = null)
    {
        var card = new CardPanel { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 12);
        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", 10);
        var t = ThemeFactory.Text(title, 14, UiTokens.Accent, UiFonts.SemiBold);
        t.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        t.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        head.AddChild(t);
        if (headerRight != null) head.AddChild(headerRight);
        if (info.Length > 0) head.AddChild(InfoIcon(info));
        col.AddChild(head);
        col.AddChild(body);
        card.AddChild(col);
        return card;
    }

    /// <summary>Subtitulo dentro de um card: texto claro seminegrito e filete embaixo.</summary>
    public static Control Section(string title, Control? right = null)
    {
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 7);
        var row = new HBoxContainer();
        var t = ThemeFactory.Text(title, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.SemiBold);
        t.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(t);
        if (right != null) row.AddChild(right);
        col.AddChild(row);
        col.AddChild(new ColorRect { Color = UiTokens.Divider, CustomMinimumSize = new Vector2(0, 1) });
        return col;
    }

    /// <summary>Linha clicavel com destaque ao passar o mouse.</summary>
    public static Button HoverRow(Action press, string tip = "")
    {
        var b = new TipButton { FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ExpandFill, TooltipText = tip, MouseDefaultCursorShape = CursorShape.PointingHand };
        b.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
        var hover = new StyleBoxFlat { BgColor = new Color(1, 1, 1, 0.035f), BorderColor = UiTokens.Accent, BorderWidthLeft = 2 };
        hover.SetCornerRadiusAll(2);
        b.AddThemeStyleboxOverride("hover", hover);
        b.AddThemeStyleboxOverride("pressed", hover);
        b.Pressed += press;
        return b;
    }

    /// <summary>Filete horizontal.</summary>
    public static Control Divider() => new ColorRect { Color = UiTokens.Divider, CustomMinimumSize = new Vector2(0, 1), MouseFilter = MouseFilterEnum.Ignore };

    /// <summary>Icone "i" com explicacao estruturada ao passar o mouse.</summary>
    public static Control InfoIcon(string tooltip) =>
        new InfoGlyph { TooltipText = tooltip, CustomMinimumSize = new Vector2(16, 16), SizeFlagsVertical = SizeFlags.ShrinkCenter };

    public static Button Action(string text, string tip, Action onPress, bool primary = false, bool enabled = true)
    {
        var b = new TipButton { Text = text, TooltipText = tip, Disabled = !enabled, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 28) };
        b.AddThemeFontOverride("font", UiFonts.Medium);
        b.AddThemeFontSizeOverride("font_size", 12);
        if (primary)
        {
            b.AddThemeStyleboxOverride("normal", ThemeFactory.Box(UiTokens.Accent, 12, radius: 3));
            b.AddThemeStyleboxOverride("hover", ThemeFactory.Box(UiTokens.AccentSoft, 12, radius: 3));
            b.AddThemeStyleboxOverride("pressed", ThemeFactory.Box(UiTokens.AccentSoft, 12, radius: 3));
            b.AddThemeColorOverride("font_color", UiTokens.Background);
            b.AddThemeColorOverride("font_hover_color", UiTokens.Background);
            b.AddThemeColorOverride("font_pressed_color", UiTokens.Background);
            b.AddThemeFontOverride("font", UiFonts.SemiBold);
        }
        b.Pressed += onPress;
        return b;
    }

    /// <summary>Linha rotulo / barra fina / valor.</summary>
    public static HBoxContainer Bar(string label, float value, float max, Color color, string valueText, float labelWidth = 150)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        if (labelWidth > 0)
        {
            var l = ThemeFactory.Text(label, UiTokens.FontSmall, UiTokens.TextLabel, UiFonts.Medium);
            l.CustomMinimumSize = new Vector2(labelWidth, 0);
            l.ClipText = true;
            row.AddChild(l);
        }
        row.AddChild(new ThinBar(max <= 0 ? 0 : value / max, color) { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter, CustomMinimumSize = new Vector2(0, 4) });
        var v = ThemeFactory.Text(valueText, UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Numbers);
        v.CustomMinimumSize = new Vector2(64, 0);
        v.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(v);
        return row;
    }

    /// <summary>Indicador: numero grande em cima, rotulo embaixo; cor so no filete lateral.</summary>
    public static PanelContainer Stat(string value, string caption, Color? color = null, string tooltip = "", bool fill = false)
    {
        // fill = ocupa a celula (grades estreitas, como o painel lateral).
        var tile = new StatTile
        {
            SizeFlagsHorizontal = fill ? SizeFlags.ExpandFill : SizeFlags.Fill, CustomMinimumSize = new Vector2(fill ? 0 : 150, 0),
            TooltipText = tooltip, MouseFilter = tooltip.Length > 0 ? MouseFilterEnum.Stop : MouseFilterEnum.Pass,
        };
        tile.AddThemeStyleboxOverride("panel", new StyleBoxEmpty { ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 10, ContentMarginBottom = 11 });
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 1);
        var v = new HBoxContainer();
        v.AddThemeConstantOverride("separation", 9);
        if (color is { } c && c != UiTokens.TextPrimary)
            v.AddChild(new ColorRect { Color = c, CustomMinimumSize = new Vector2(3, 18), SizeFlagsVertical = SizeFlags.ShrinkCenter });
        v.AddChild(ThemeFactory.Text(value, 22, UiTokens.TextPrimary, UiFonts.Numbers));
        col.AddChild(v);
        col.AddChild(ThemeFactory.Text(caption, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium));
        tile.AddChild(col);
        return tile;
    }

    /// <summary>Linha de dado: rotulo a esquerda, valor a direita.</summary>
    public static Control KeyValue(string key, string value, Color? color = null)
    {
        var row = new HBoxContainer();
        var k = ThemeFactory.Text(key, UiTokens.FontSmall, UiTokens.TextSecondary);
        k.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(k);
        row.AddChild(ThemeFactory.Text(value, UiTokens.FontBody, color ?? UiTokens.TextPrimary, UiFonts.Numbers));
        return row;
    }

    public static HBoxContainer Row(int separation = 10, params Control[] items)
    {
        var r = new HBoxContainer();
        r.AddThemeConstantOverride("separation", separation);
        foreach (var i in items) r.AddChild(i);
        return r;
    }

    public static VBoxContainer Column(int separation = 10, params Control[] items)
    {
        var c = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        c.AddThemeConstantOverride("separation", separation);
        foreach (var i in items) c.AddChild(i);
        return c;
    }

    /// <summary>
    /// Linha de cards numa grade de 12 colunas (kit D-034): cada card ocupa N colunas e as
    /// divisas caem sempre nas mesmas linhas verticais da pagina, de uma linha para outra.
    /// Os cards da linha ficam com a mesma altura.
    /// </summary>
    public static SpanRow Grid(int separation, params (Control Item, int Span)[] items)
    {
        var r = new SpanRow { Gap = separation, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var (item, span) in items) r.Add(item, span);
        return r;
    }
}

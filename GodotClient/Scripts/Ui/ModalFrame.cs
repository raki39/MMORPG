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

    public ModalFrame(Color accent)
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        OffsetTop = UiTokens.TopBarHeight;
        MouseFilter = MouseFilterEnum.Stop;

        _panel = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
        _panel.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = UiTokens.Background });
        _panel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(_panel);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 0);
        _panel.AddChild(col);

        // Cabecalho: identidade + faixa de dados na mesma linha (como a ficha de um atleta).
        var head = new PanelContainer();
        head.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = UiTokens.Surface, BorderColor = UiTokens.Border, BorderWidthBottom = 1,
            ContentMarginLeft = 20, ContentMarginRight = 20, ContentMarginTop = 10, ContentMarginBottom = 0,
        });
        col.AddChild(head);
        var headCol = new VBoxContainer();
        headCol.AddThemeConstantOverride("separation", 8);
        head.AddChild(headCol);
        _crumb = ThemeFactory.Text("", UiTokens.FontCaption, UiTokens.TextDisabled, UiFonts.Medium);
        headCol.AddChild(_crumb);
        var headRow = new HBoxContainer();
        headRow.AddThemeConstantOverride("separation", 24);
        headCol.AddChild(headRow);
        Header = new VBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter, CustomMinimumSize = new Vector2(300, 0) };
        Header.AddThemeConstantOverride("separation", 4);
        headRow.AddChild(Header);
        _facts = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        _facts.AddThemeConstantOverride("separation", 0);
        headRow.AddChild(_facts);

        // Abas de texto a esquerda, acoes a direita.
        var tabsRow = new HBoxContainer();
        tabsRow.AddThemeConstantOverride("separation", 12);
        headCol.AddChild(tabsRow);
        _tabBar = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _tabBar.AddThemeConstantOverride("separation", 4);
        tabsRow.AddChild(_tabBar);
        Actions = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        Actions.AddThemeConstantOverride("separation", 6);
        tabsRow.AddChild(Actions);

        _scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = SizeFlags.ExpandFill };
        var body = new MarginContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("margin_left", 16);
        body.AddThemeConstantOverride("margin_right", 16);
        body.AddThemeConstantOverride("margin_top", 14);
        body.AddThemeConstantOverride("margin_bottom", 14);
        body.AddChild(_scroll);
        col.AddChild(body);
    }

    /// <summary>Dado-chave do cabecalho: valor em cima, rotulo embaixo, separado por filete.</summary>
    public void Fact(string label, string value, Color? color = null, string tooltip = "")
    {
        if (_facts.GetChildCount() > 0)
            _facts.AddChild(new ColorRect { Color = UiTokens.Border, CustomMinimumSize = new Vector2(1, 30), SizeFlagsVertical = SizeFlags.ShrinkCenter });
        var cell = new TipPanel { TooltipText = tooltip, MouseFilter = tooltip.Length > 0 ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore };
        cell.AddThemeStyleboxOverride("panel", new StyleBoxEmpty { ContentMarginLeft = 16, ContentMarginRight = 16 });
        var c = new VBoxContainer();
        c.AddThemeConstantOverride("separation", 0);
        var v = new HBoxContainer();
        v.AddThemeConstantOverride("separation", 6);
        // Cor so como marcador pequeno (significado), o valor fica neutro.
        if (color is { } dot && dot != UiTokens.TextPrimary && dot != UiTokens.TextSecondary)
            v.AddChild(new ColorRect { Color = dot, CustomMinimumSize = new Vector2(6, 6), SizeFlagsVertical = SizeFlags.ShrinkCenter });
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
        var b = new Button { Text = name, ToggleMode = true, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 36) };
        b.AddThemeFontOverride("font", UiFonts.Medium);
        b.AddThemeFontSizeOverride("font_size", 13);
        StyleBoxFlat Tab(bool active, bool hover) => new()
        {
            BgColor = new Color(0, 0, 0, 0),
            BorderColor = active ? UiTokens.Accent : hover ? UiTokens.BorderStrong : new Color(0, 0, 0, 0), BorderWidthBottom = 2,
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 6, ContentMarginBottom = 8,
        };
        b.AddThemeStyleboxOverride("normal", Tab(false, false));
        b.AddThemeStyleboxOverride("hover", Tab(false, true));
        b.AddThemeStyleboxOverride("pressed", Tab(true, false));
        b.AddThemeStyleboxOverride("hover_pressed", Tab(true, true));
        b.AddThemeColorOverride("font_color", UiTokens.TextSecondary);
        b.AddThemeColorOverride("font_hover_color", UiTokens.TextPrimary);
        b.AddThemeColorOverride("font_pressed_color", UiTokens.AccentSoft);
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
    /// Card de dados (estilo FM): painel um tom acima do fundo, titulo na cor de destaque,
    /// icone de informacao com explicacao (tooltip) e corpo. O accent fica por compatibilidade.
    /// </summary>
    public static PanelContainer Card(string title, Control body, Color? accent = null, string info = "")
    {
        var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var sb = new StyleBoxFlat { BgColor = UiTokens.SurfaceRaised, BorderColor = UiTokens.Border, ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 10, ContentMarginBottom = 12 };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(4);
        card.AddThemeStyleboxOverride("panel", sb);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 10);
        var head = new HBoxContainer();
        var t = ThemeFactory.Text(title, 13, UiTokens.Accent, UiFonts.SemiBold);
        t.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        t.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        head.AddChild(t);
        if (info.Length > 0) head.AddChild(InfoIcon(info));
        col.AddChild(head);
        col.AddChild(body);
        card.AddChild(col);
        return card;
    }

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
            var l = ThemeFactory.Text(label, UiTokens.FontSmall, new Color(UiTokens.TextPrimary, 0.9f));
            l.CustomMinimumSize = new Vector2(labelWidth, 0);
            l.ClipText = true;
            row.AddChild(l);
        }
        row.AddChild(new ThinBar(max <= 0 ? 0 : value / max, color) { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter, CustomMinimumSize = new Vector2(0, 4) });
        var v = ThemeFactory.Text(valueText, UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Numbers);
        v.CustomMinimumSize = new Vector2(60, 0);
        v.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(v);
        return row;
    }

    /// <summary>Indicador: numero grande em cima, rotulo embaixo; cor so no filete lateral.</summary>
    public static PanelContainer Stat(string value, string caption, Color? color = null, string tooltip = "", bool fill = false)
    {
        // fill = ocupa a celula (grades estreitas, como o painel lateral).
        var tile = new TipPanel
        {
            SizeFlagsHorizontal = fill ? SizeFlags.ExpandFill : SizeFlags.Fill, CustomMinimumSize = new Vector2(fill ? 0 : 140, 0),
            TooltipText = tooltip, MouseFilter = tooltip.Length > 0 ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore,
        };
        var sb = new StyleBoxFlat
        {
            BgColor = UiTokens.SurfaceRaised, BorderColor = UiTokens.Border,
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 9,
        };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(4);
        tile.AddThemeStyleboxOverride("panel", sb);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 0);
        var v = new HBoxContainer();
        v.AddThemeConstantOverride("separation", 8);
        if (color is { } c && c != UiTokens.TextPrimary)
            v.AddChild(new ColorRect { Color = c, CustomMinimumSize = new Vector2(3, 16), SizeFlagsVertical = SizeFlags.ShrinkCenter });
        v.AddChild(ThemeFactory.Text(value, 20, UiTokens.TextPrimary, UiFonts.Numbers));
        col.AddChild(v);
        col.AddChild(ThemeFactory.Text(caption, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium));
        tile.AddChild(col);
        return tile;
    }

    /// <summary>Linha de dado: rotulo a esquerda, valor a direita (tabela de 2 colunas).</summary>
    public static Control KeyValue(string key, string value, Color? color = null)
    {
        var row = new HBoxContainer();
        var k = ThemeFactory.Text(key, UiTokens.FontSmall, UiTokens.TextSecondary);
        k.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(k);
        row.AddChild(ThemeFactory.Text(value, UiTokens.FontSmall, color ?? UiTokens.TextPrimary, UiFonts.Numbers));
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

    /// <summary>Linha de cards com larguras proporcionais (pesos) e mesma altura.</summary>
    public static HBoxContainer Grid(int separation, params (Control Item, float Weight)[] items)
    {
        var r = new HBoxContainer();
        r.AddThemeConstantOverride("separation", separation);
        foreach (var (item, weight) in items)
        {
            item.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            item.SizeFlagsStretchRatio = weight;
            r.AddChild(item);
        }
        return r;
    }
}

using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Ficha em estilo "dossie" de jogo de gestao (D-022): painel largo e denso, cabecalho
/// compacto com faixa de dados-chave, acoes compactas, abas planas e secoes com faixa de
/// titulo. Esc ou clique fora fecha. Abas "ao vivo" se atualizam sem perder a rolagem.
/// </summary>
public partial class ModalFrame : Control
{
    public event Action? Closed;

    private readonly Control _root;
    private readonly PanelContainer _panel;
    private readonly List<(string Name, Func<Control> Build, bool Live)> _tabs = new();
    private readonly List<Button> _tabButtons = new();
    private readonly HBoxContainer _tabBar;
    private readonly ScrollContainer _scroll;
    private readonly HBoxContainer _facts;
    private int _current = -1;
    private int _settleFrames = 4;

    public VBoxContainer Header { get; }
    public HBoxContainer Actions { get; }
    public int CurrentTab => _current;

    public ModalFrame(Color accent)
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        var dim = new ColorRect { Color = new Color(0.01f, 0.015f, 0.02f, 0.72f), MouseFilter = MouseFilterEnum.Stop };
        dim.SetAnchorsPreset(LayoutPreset.FullRect);
        dim.GuiInput += e => { if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) Close(); };
        AddChild(dim);

        _root = new Control { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_root);
        _panel = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
        var sb = new StyleBoxFlat
        {
            BgColor = UiTokens.Surface, BorderColor = UiTokens.BorderStrong,
            ShadowColor = new Color(0, 0, 0, 0.6f), ShadowSize = 24, ShadowOffset = new Vector2(0, 8),
        };
        sb.SetBorderWidthAll(1);
        sb.BorderWidthTop = 3;
        sb.BorderColor = UiTokens.BorderStrong;
        sb.SetCornerRadiusAll(4);
        _panel.AddThemeStyleboxOverride("panel", sb);
        _panel.ClipChildren = ClipChildrenMode.AndDraw;
        _root.AddChild(_panel);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 0);
        _panel.AddChild(col);

        // Faixa de cor do contexto no topo.
        col.AddChild(new ColorRect { Color = accent, CustomMinimumSize = new Vector2(0, 3) });

        // Cabecalho: identidade a esquerda, acoes a direita.
        var head = new PanelContainer();
        var hsb = new StyleBoxFlat { BgColor = UiTokens.SurfaceHeader, ContentMarginLeft = 20, ContentMarginRight = 14, ContentMarginTop = 14, ContentMarginBottom = 12 };
        head.AddThemeStyleboxOverride("panel", hsb);
        col.AddChild(head);
        var headRow = new HBoxContainer();
        headRow.AddThemeConstantOverride("separation", 16);
        head.AddChild(headRow);
        Header = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(480, 0) };
        Header.AddThemeConstantOverride("separation", 4);
        headRow.AddChild(Header);
        var right = new VBoxContainer();
        right.AddThemeConstantOverride("separation", 8);
        headRow.AddChild(right);
        var closeRow = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        var close = new Button { Text = "✕  FECHAR", TooltipText = "Fechar (Esc)", FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 24) };
        close.AddThemeFontOverride("font", UiFonts.CondensedBold);
        close.AddThemeFontSizeOverride("font_size", 11);
        close.AddThemeStyleboxOverride("normal", ThemeFactory.Box(new Color(0, 0, 0, 0), 8, radius: 2));
        close.AddThemeStyleboxOverride("hover", ThemeFactory.Box(new Color(UiTokens.Danger, 0.25f), 8, border: UiTokens.Danger, radius: 2));
        close.AddThemeColorOverride("font_color", UiTokens.TextSecondary);
        close.Pressed += Close;
        closeRow.AddChild(close);
        right.AddChild(closeRow);
        Actions = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End };
        Actions.AddThemeConstantOverride("separation", 6);
        right.AddChild(Actions);

        // Faixa de dados-chave (estilo barra de informacoes).
        var factsBar = new PanelContainer();
        var fsb = new StyleBoxFlat { BgColor = UiTokens.SurfaceRaised, BorderColor = UiTokens.Border, BorderWidthTop = 1, BorderWidthBottom = 1, ContentMarginLeft = 20, ContentMarginRight = 20, ContentMarginTop = 6, ContentMarginBottom = 6 };
        factsBar.AddThemeStyleboxOverride("panel", fsb);
        _facts = new HBoxContainer();
        _facts.AddThemeConstantOverride("separation", 0);
        factsBar.AddChild(_facts);
        col.AddChild(factsBar);

        // Abas planas.
        var tabsBg = new PanelContainer();
        tabsBg.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = UiTokens.Surface, BorderColor = UiTokens.Border, BorderWidthBottom = 1, ContentMarginLeft = 14, ContentMarginRight = 14 });
        _tabBar = new HBoxContainer();
        _tabBar.AddThemeConstantOverride("separation", 2);
        tabsBg.AddChild(_tabBar);
        col.AddChild(tabsBg);

        _scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = SizeFlags.ExpandFill };
        var body = new MarginContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("margin_left", 16);
        body.AddThemeConstantOverride("margin_right", 14);
        body.AddThemeConstantOverride("margin_top", 14);
        body.AddThemeConstantOverride("margin_bottom", 14);
        body.AddChild(_scroll);
        col.AddChild(body);
    }

    /// <summary>Celula da faixa de dados-chave (rotulo pequeno + valor). Tooltip estruturado opcional.</summary>
    public void Fact(string label, string value, Color? color = null, string tooltip = "")
    {
        if (_facts.GetChildCount() > 0)
            _facts.AddChild(new ColorRect { Color = UiTokens.Border, CustomMinimumSize = new Vector2(1, 0), SizeFlagsVertical = SizeFlags.Fill });
        var cell = new TipPanel { TooltipText = tooltip, MouseFilter = tooltip.Length > 0 ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore };
        cell.AddThemeStyleboxOverride("panel", new StyleBoxEmpty { ContentMarginLeft = 12, ContentMarginRight = 14 });
        var c = new VBoxContainer();
        c.AddThemeConstantOverride("separation", -1);
        c.AddChild(ThemeFactory.Text(label.ToUpperInvariant(), 10, UiTokens.TextSecondary, UiFonts.CondensedSemiBold));
        c.AddChild(ThemeFactory.Text(value, 14, color ?? UiTokens.TextPrimary, UiFonts.Numbers));
        cell.AddChild(c);
        _facts.AddChild(cell);
    }

    public override void _Ready()
    {
        Layout();
        GetViewport().SizeChanged += Layout;
        _root.Modulate = new Color(1, 1, 1, 0);
        var tw = CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tw.TweenProperty(_root, "modulate:a", 1f, 0.14f);
    }

    private void Layout()
    {
        var vp = GetViewportRect().Size;
        var size = new Vector2(Mathf.Min(1240, vp.X - 48), Mathf.Min(800, vp.Y - 48));
        _root.Size = size;
        _root.Position = (vp - size) / 2f;
        _panel.Size = size;
    }

    public override void _Process(double delta)
    {
        if (_settleFrames <= 0) return;
        _settleFrames--;
        Layout();
    }

    public void AddTab(string name, Func<Control> build, bool live = false)
    {
        int index = _tabs.Count;
        _tabs.Add((name, build, live));
        var b = new Button { Text = name.ToUpperInvariant(), ToggleMode = true, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 34) };
        b.AddThemeFontOverride("font", UiFonts.CondensedBold);
        b.AddThemeFontSizeOverride("font_size", 13);
        StyleBoxFlat Tab(bool active, bool hover) => new()
        {
            BgColor = hover ? UiTokens.SurfaceHover : new Color(0, 0, 0, 0),
            BorderColor = UiTokens.Accent, BorderWidthBottom = active ? 2 : 0,
            ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 6, ContentMarginBottom = 6,
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

    // ---------- Blocos reutilizaveis (densos) ----------

    /// <summary>Secao com faixa de titulo escura (estilo painel de dados).</summary>
    public static PanelContainer Card(string title, Control body, Color? accent = null)
    {
        var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var sb = new StyleBoxFlat { BgColor = UiTokens.SurfaceRaised, BorderColor = UiTokens.Border };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(3);
        card.AddThemeStyleboxOverride("panel", sb);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 0);
        var bar = new PanelContainer();
        var bsb = new StyleBoxFlat
        {
            BgColor = UiTokens.SurfaceHeader, BorderColor = accent ?? UiTokens.Border, BorderWidthLeft = accent.HasValue ? 3 : 0, BorderWidthBottom = 0,
            ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 5, ContentMarginBottom = 5,
        };
        bsb.CornerRadiusTopLeft = 3; bsb.CornerRadiusTopRight = 3;
        bar.AddThemeStyleboxOverride("panel", bsb);
        bar.AddChild(ThemeFactory.Section(title));
        col.AddChild(bar);
        var m = new MarginContainer();
        m.AddThemeConstantOverride("margin_left", 10);
        m.AddThemeConstantOverride("margin_right", 10);
        m.AddThemeConstantOverride("margin_top", 8);
        m.AddThemeConstantOverride("margin_bottom", 10);
        m.AddChild(body);
        col.AddChild(m);
        card.AddChild(col);
        return card;
    }

    public static Button Action(string text, string tip, Action onPress, bool primary = false, bool enabled = true)
    {
        var b = new TipButton { Text = text, TooltipText = tip, Disabled = !enabled, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 26) };
        b.AddThemeFontSizeOverride("font_size", 12);
        if (primary)
        {
            b.AddThemeStyleboxOverride("normal", ThemeFactory.Box(new Color(UiTokens.Accent, 0.9f), 10, border: UiTokens.AccentSoft, radius: 3));
            b.AddThemeStyleboxOverride("hover", ThemeFactory.Box(UiTokens.AccentSoft, 10, border: UiTokens.AccentSoft, radius: 3));
            b.AddThemeColorOverride("font_color", UiTokens.Background);
            b.AddThemeColorOverride("font_hover_color", UiTokens.Background);
        }
        b.Pressed += onPress;
        return b;
    }

    /// <summary>Linha rotulo / barra fina / valor.</summary>
    public static HBoxContainer Bar(string label, float value, float max, Color color, string valueText, float labelWidth = 150)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        if (labelWidth > 0)
        {
            var l = ThemeFactory.Text(label, UiTokens.FontSmall, UiTokens.TextPrimary);
            l.CustomMinimumSize = new Vector2(labelWidth, 0);
            row.AddChild(l);
        }
        var bar = new ProgressBar
        {
            MinValue = 0, MaxValue = Math.Max(1, max), Value = Math.Clamp(value, 0, max), ShowPercentage = false,
            SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter, CustomMinimumSize = new Vector2(0, 5),
        };
        var fill = ThemeFactory.Box(color, 0, radius: 1);
        fill.ContentMarginTop = fill.ContentMarginBottom = 0;
        bar.AddThemeStyleboxOverride("fill", fill);
        row.AddChild(bar);
        var v = ThemeFactory.Text(valueText, UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Numbers);
        v.CustomMinimumSize = new Vector2(64, 0);
        v.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(v);
        return row;
    }

    /// <summary>Indicador compacto: rotulo condensado em cima, numero grande, cor opcional.</summary>
    public static PanelContainer Stat(string value, string caption, Color? color = null, string tooltip = "")
    {
        var tile = new TipPanel { SizeFlagsHorizontal = SizeFlags.ExpandFill, TooltipText = tooltip };
        var sb = new StyleBoxFlat
        {
            BgColor = UiTokens.SurfaceRaised, BorderColor = UiTokens.Border,
            ContentMarginLeft = 10, ContentMarginRight = 10, ContentMarginTop = 6, ContentMarginBottom = 7,
        };
        sb.BorderWidthTop = 2;
        sb.BorderColor = color ?? UiTokens.BorderStrong;
        sb.SetCornerRadiusAll(3);
        tile.AddThemeStyleboxOverride("panel", sb);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", -1);
        col.AddChild(ThemeFactory.Text(caption.ToUpperInvariant(), 10, UiTokens.TextSecondary, UiFonts.CondensedSemiBold));
        col.AddChild(ThemeFactory.Text(value, 18, color ?? UiTokens.TextPrimary, UiFonts.Numbers));
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
}

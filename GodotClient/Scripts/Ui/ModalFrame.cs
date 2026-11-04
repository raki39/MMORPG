using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Modal grande (ficha) com acabamento de jogo: fundo escurecido, painel com brilho na cor do
/// contexto, banner de cabecalho com arte procedural, abas com indicador deslizante e corpo
/// rolavel com transicao. Esc ou clique fora fecha. Abas "ao vivo" se atualizam sem perder a rolagem.
/// </summary>
public partial class ModalFrame : Control
{
    public event Action? Closed;

    private readonly Color _accent;
    private readonly Control _root;
    private readonly PanelContainer _panel;
    private readonly List<(string Name, Func<Control> Build, bool Live)> _tabs = new();
    private readonly List<Button> _tabButtons = new();
    private readonly HBoxContainer _tabBar;
    private readonly ColorRect _indicator;
    private readonly ScrollContainer _scroll;
    private int _current = -1;

    public VBoxContainer Header { get; }
    public HBoxContainer Actions { get; }
    public int CurrentTab => _current;

    public ModalFrame(Color accent)
    {
        _accent = accent;
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        var dim = new ColorRect { Color = new Color(0.01f, 0.015f, 0.02f, 0.70f), MouseFilter = MouseFilterEnum.Stop };
        dim.SetAnchorsPreset(LayoutPreset.FullRect);
        dim.GuiInput += e => { if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) Close(); };
        AddChild(dim);

        _root = new Control { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_root);

        _panel = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
        // Sombra profunda levemente tingida pela cor do contexto (brilho sutil e arredondado).
        var sb = new StyleBoxFlat
        {
            BgColor = new Color("#10161E"), BorderColor = new Color(accent, 0.42f),
            ShadowColor = new Color(0, 0, 0, 0.7f).Lerp(new Color(accent, 0.7f), 0.18f), ShadowSize = 40, ShadowOffset = new Vector2(0, 12), AntiAliasing = true,
        };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(18);
        _panel.AddThemeStyleboxOverride("panel", sb);
        // Recorta o conteudo (banner) pelos cantos arredondados do painel.
        _panel.ClipChildren = ClipChildrenMode.AndDraw;
        _root.AddChild(_panel);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 0);
        _panel.AddChild(col);

        // Banner do cabecalho: arte procedural atras do conteudo.
        var head = new PanelContainer();
        head.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        col.AddChild(head);
        head.AddChild(new HeaderArt(accent) { MouseFilter = MouseFilterEnum.Ignore });
        var headRow = new HBoxContainer();
        headRow.AddThemeConstantOverride("separation", 18);
        head.AddChild(Margin(headRow, 30, 22, 24, 18));
        Header = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(560, 0) };
        Header.AddThemeConstantOverride("separation", 6);
        headRow.AddChild(Header);
        var close = new Button { Text = "✕", TooltipText = "Fechar (Esc)", CustomMinimumSize = new Vector2(38, 38), FocusMode = FocusModeEnum.None, SizeFlagsVertical = SizeFlags.ShrinkBegin };
        var cn = ThemeFactory.Box(new Color(UiTokens.Background, 0.55f), 0, border: UiTokens.BorderStrong, radius: 19);
        var ch = ThemeFactory.Box(new Color(UiTokens.Danger, 0.85f), 0, border: UiTokens.Danger, radius: 19);
        close.AddThemeStyleboxOverride("normal", cn);
        close.AddThemeStyleboxOverride("hover", ch);
        close.AddThemeStyleboxOverride("pressed", ch);
        close.Pressed += Close;
        headRow.AddChild(close);

        Actions = new HBoxContainer();
        Actions.AddThemeConstantOverride("separation", 8);
        col.AddChild(Margin(Actions, 30, 22, 14, 0));

        // Abas com indicador deslizante.
        var tabsHolder = new Control { CustomMinimumSize = new Vector2(0, 44) };
        _tabBar = new HBoxContainer();
        _tabBar.AddThemeConstantOverride("separation", 6);
        _tabBar.SetAnchorsPreset(LayoutPreset.FullRect);
        tabsHolder.AddChild(_tabBar);
        _indicator = new ColorRect { Color = accent.Lerp(UiTokens.Accent, 0.5f), Size = new Vector2(0, 3), MouseFilter = MouseFilterEnum.Ignore };
        tabsHolder.AddChild(_indicator);
        col.AddChild(Margin(tabsHolder, 24, 22, 12, 0));
        col.AddChild(new ColorRect { Color = UiTokens.Border, CustomMinimumSize = new Vector2(0, 1) });

        _scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = SizeFlags.ExpandFill };
        var body = Margin(_scroll, 30, 22, 20, 24);
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        col.AddChild(body);
    }

    private static MarginContainer Margin(Control child, int l, int r, int t, int b)
    {
        var m = new MarginContainer();
        m.AddThemeConstantOverride("margin_left", l);
        m.AddThemeConstantOverride("margin_right", r);
        m.AddThemeConstantOverride("margin_top", t);
        m.AddThemeConstantOverride("margin_bottom", b);
        m.AddChild(child);
        return m;
    }

    public override void _Ready()
    {
        Layout();
        GetViewport().SizeChanged += Layout;
        _root.Modulate = new Color(1, 1, 1, 0);
        _root.Scale = new Vector2(0.965f, 0.965f);
        _root.Position += new Vector2(0, 10);
        var tw = CreateTween().SetParallel().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tw.TweenProperty(_root, "modulate:a", 1f, 0.18f);
        tw.TweenProperty(_root, "scale", Vector2.One, 0.22f);
        tw.TweenProperty(_root, "position:y", _root.Position.Y - 10, 0.22f);
    }

    private void Layout()
    {
        var vp = GetViewportRect().Size;
        var size = new Vector2(Mathf.Min(1100, vp.X - 64), Mathf.Min(760, vp.Y - 56));
        _root.Size = size;
        _root.Position = (vp - size) / 2f;
        _root.PivotOffset = size / 2f;
        _panel.Size = size;
    }

    // Textos com quebra calculam a altura minima so depois de ter largura: reaplica o
    // tamanho nos primeiros quadros para o painel nao crescer alem da tela.
    private int _settleFrames = 4;

    public override void _Process(double delta)
    {
        if (_settleFrames <= 0) return;
        _settleFrames--;
        Layout();
        CallDeferred(nameof(MoveIndicator), false);
    }

    public void AddTab(string name, Func<Control> build, bool live = false)
    {
        int index = _tabs.Count;
        _tabs.Add((name, build, live));
        var b = new Button { Text = name, ToggleMode = true, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 40) };
        b.AddThemeFontOverride("font", UiFonts.SemiBold);
        b.AddThemeFontSizeOverride("font_size", 14);
        var clear = new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 6, ContentMarginBottom = 10 };
        var hover = new StyleBoxFlat { BgColor = new Color(UiTokens.SurfaceHover, 0.55f), ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 6, ContentMarginBottom = 10 };
        hover.SetCornerRadiusAll(8);
        b.AddThemeStyleboxOverride("normal", clear);
        b.AddThemeStyleboxOverride("hover", hover);
        b.AddThemeStyleboxOverride("pressed", clear);
        b.AddThemeStyleboxOverride("hover_pressed", hover);
        b.AddThemeColorOverride("font_color", UiTokens.TextSecondary);
        b.AddThemeColorOverride("font_hover_color", UiTokens.TextPrimary);
        b.AddThemeColorOverride("font_pressed_color", UiTokens.TextPrimary);
        b.AddThemeColorOverride("font_hover_pressed_color", UiTokens.TextPrimary);
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
        CallDeferred(nameof(MoveIndicator), true);
    }

    private void MoveIndicator(bool animate)
    {
        if (_current < 0 || _current >= _tabButtons.Count) return;
        var b = _tabButtons[_current];
        var target = new Vector2(b.Position.X + 12, b.Size.Y + 1);
        var size = new Vector2(Mathf.Max(0, b.Size.X - 24), 3);
        if (!animate || _indicator.Size.X < 1)
        {
            _indicator.Position = target;
            _indicator.Size = size;
            return;
        }
        var tw = CreateTween().SetParallel().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tw.TweenProperty(_indicator, "position", target, 0.22f);
        tw.TweenProperty(_indicator, "size", size, 0.22f);
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
        if (resetScroll)
        {
            content.Modulate = new Color(1, 1, 1, 0);
            var tw = CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
            tw.TweenProperty(content, "modulate:a", 1f, 0.2f);
        }
        else CallDeferred(nameof(RestoreScroll), scroll);
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

    // ---------- Blocos reutilizaveis ----------

    /// <summary>Titulo de secao com losango na cor de destaque.</summary>
    public static Control SectionTitle(string title, Color? accent = null)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        row.AddChild(ThemeFactory.Text("◆", 10, accent ?? UiTokens.Accent));
        row.AddChild(ThemeFactory.Section(title));
        return row;
    }

    public static PanelContainer Card(string title, Control body, Color? accent = null)
    {
        var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color("#151D28"), BorderColor = UiTokens.Border,
            ShadowColor = new Color(0, 0, 0, 0.32f), ShadowSize = 10, ShadowOffset = new Vector2(0, 4),
            ContentMarginLeft = 18, ContentMarginRight = 18, ContentMarginTop = 16, ContentMarginBottom = 16, AntiAliasing = true,
        };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(12);
        if (accent.HasValue) { sb.BorderWidthTop = 3; sb.BorderColor = new Color(accent.Value, 0.75f); }
        card.AddThemeStyleboxOverride("panel", sb);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 12);
        col.AddChild(SectionTitle(title, accent));
        col.AddChild(body);
        card.AddChild(col);
        return card;
    }

    public static Button Action(string text, string tip, Action onPress, bool primary = false, bool enabled = true)
    {
        var b = new Button { Text = text, TooltipText = tip, Disabled = !enabled, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 38) };
        b.AddThemeFontOverride("font", UiFonts.SemiBold);
        if (primary)
        {
            var n = ThemeFactory.Box(UiTokens.Accent, 16, border: UiTokens.AccentSoft, radius: 9);
            n.ShadowColor = new Color(UiTokens.Accent, 0.35f);
            n.ShadowSize = 8;
            var h = ThemeFactory.Box(UiTokens.AccentSoft, 16, border: UiTokens.AccentSoft, radius: 9);
            h.ShadowColor = new Color(UiTokens.Accent, 0.5f);
            h.ShadowSize = 12;
            b.AddThemeStyleboxOverride("normal", n);
            b.AddThemeStyleboxOverride("hover", h);
            b.AddThemeStyleboxOverride("pressed", h);
            b.AddThemeColorOverride("font_color", UiTokens.Background);
            b.AddThemeColorOverride("font_hover_color", UiTokens.Background);
            b.AddThemeColorOverride("font_pressed_color", UiTokens.Background);
        }
        else
        {
            b.AddThemeStyleboxOverride("normal", ThemeFactory.Box(new Color(UiTokens.SurfaceRaised, 0.75f), 16, border: UiTokens.BorderStrong, radius: 9));
            b.AddThemeStyleboxOverride("hover", ThemeFactory.Box(UiTokens.SurfaceHover, 16, border: UiTokens.AccentSoft, radius: 9));
        }
        b.Pressed += onPress;
        return b;
    }

    public static HBoxContainer Bar(string label, float value, float max, Color color, string valueText, float labelWidth = 170)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        if (labelWidth > 0)
        {
            var l = ThemeFactory.Text(label, UiTokens.FontSmall, UiTokens.TextPrimary);
            l.CustomMinimumSize = new Vector2(labelWidth, 0);
            row.AddChild(l);
        }
        var bar = new ProgressBar
        {
            MinValue = 0, MaxValue = Math.Max(1, max), Value = Math.Clamp(value, 0, max), ShowPercentage = false,
            SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter, CustomMinimumSize = new Vector2(0, 8),
        };
        var fill = ThemeFactory.Box(color, 0, radius: 4);
        fill.ContentMarginTop = fill.ContentMarginBottom = 0;
        bar.AddThemeStyleboxOverride("fill", fill);
        row.AddChild(bar);
        var v = ThemeFactory.Text(valueText, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers);
        v.CustomMinimumSize = new Vector2(70, 0);
        v.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(v);
        return row;
    }

    /// <summary>Indicador numerico com barra lateral colorida.</summary>
    public static PanelContainer Stat(string value, string caption, Color? color = null)
    {
        var tile = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var c = color ?? UiTokens.Info;
        var sb = new StyleBoxFlat
        {
            BgColor = new Color("#121A24"), BorderColor = new Color(c, 0.85f), BorderWidthLeft = 3,
            ContentMarginLeft = 14, ContentMarginRight = 12, ContentMarginTop = 10, ContentMarginBottom = 10,
            ShadowColor = new Color(0, 0, 0, 0.25f), ShadowSize = 6, ShadowOffset = new Vector2(0, 2), AntiAliasing = true,
        };
        sb.SetCornerRadiusAll(9);
        tile.AddThemeStyleboxOverride("panel", sb);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 0);
        col.AddChild(ThemeFactory.Text(value, 22, color ?? UiTokens.TextPrimary, UiFonts.Numbers));
        col.AddChild(ThemeFactory.Text(caption, UiTokens.FontCaption, UiTokens.TextSecondary));
        tile.AddChild(col);
        return tile;
    }

    public static HBoxContainer Row(int separation = 14, params Control[] items)
    {
        var r = new HBoxContainer();
        r.AddThemeConstantOverride("separation", separation);
        foreach (var i in items) r.AddChild(i);
        return r;
    }

    public static VBoxContainer Column(int separation = 14, params Control[] items)
    {
        var c = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        c.AddThemeConstantOverride("separation", separation);
        foreach (var i in items) c.AddChild(i);
        return c;
    }
}

/// <summary>Arte do banner: gradiente na cor do contexto, feixes de luz, pontilhado e filete inferior.</summary>
public partial class HeaderArt : Control
{
    private readonly Color _accent;

    public HeaderArt(Color accent)
    {
        _accent = accent;
        SetAnchorsPreset(LayoutPreset.FullRect);
    }

    public override void _Draw()
    {
        var s = Size;
        // Gradiente horizontal suave (cores por vertice, sem faixas).
        var a0 = new Color(_accent, 0.30f);
        var a1 = new Color(_accent, 0.10f);
        var a2 = new Color(_accent, 0.02f);
        DrawPolygon(new[] { new Vector2(0, 0), new Vector2(s.X * 0.45f, 0), new Vector2(s.X * 0.45f, s.Y), new Vector2(0, s.Y) }, new[] { a0, a1, a1, a0 });
        DrawPolygon(new[] { new Vector2(s.X * 0.45f, 0), new Vector2(s.X, 0), new Vector2(s.X, s.Y), new Vector2(s.X * 0.45f, s.Y) }, new[] { a1, a2, a2, a1 });
        // Feixes diagonais.
        for (int i = 0; i < 5; i++)
        {
            float x = s.X * (0.18f + i * 0.17f);
            var pts = new[] { new Vector2(x, 0), new Vector2(x + 60, 0), new Vector2(x - 40, s.Y), new Vector2(x - 100, s.Y) };
            DrawColoredPolygon(pts, new Color(1, 1, 1, 0.018f + 0.01f * (i % 2)));
        }
        // Pontilhado discreto.
        for (float y = 10; y < s.Y; y += 14)
            for (float x = s.X * 0.55f + (y % 28 == 10 ? 0 : 7); x < s.X; x += 14)
                DrawCircle(new Vector2(x, y), 1f, new Color(1, 1, 1, 0.035f));
        // Cantos superiores arredondados nao sao recortados pelo pai: mascara simples nas bordas.
        DrawLine(new Vector2(0, s.Y - 1), new Vector2(s.X, s.Y - 1), new Color(_accent, 0.45f), 1f);
        DrawLine(new Vector2(0, s.Y), new Vector2(s.X * 0.35f, s.Y), new Color(_accent, 0.9f), 2f);
    }
}

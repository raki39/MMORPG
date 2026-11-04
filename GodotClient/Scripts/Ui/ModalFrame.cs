using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Modal grande (ficha): fundo escurecido, painel central em vidro, cabecalho, abas e corpo
/// rolavel. Abre com animacao curta; Esc ou clique fora fecha. As abas se reconstroem sob
/// demanda (Refresh) para mostrar dados ao vivo sem refazer o resto.
/// </summary>
public partial class ModalFrame : Control
{
    public event Action? Closed;

    private readonly PanelContainer _panel;
    private readonly List<(string Name, Func<Control> Build, bool Live)> _tabs = new();
    private readonly List<Button> _tabButtons = new();
    private readonly HBoxContainer _tabBar;
    private readonly ScrollContainer _scroll;
    private int _current = -1;

    public VBoxContainer Header { get; }
    public HBoxContainer Actions { get; }
    public int CurrentTab => _current;

    public ModalFrame(Color accent)
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;

        var dim = new ColorRect { Color = new Color(0.01f, 0.015f, 0.02f, 0.62f), MouseFilter = MouseFilterEnum.Stop };
        dim.SetAnchorsPreset(LayoutPreset.FullRect);
        dim.GuiInput += e => { if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) Close(); };
        AddChild(dim);

        _panel = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
        var sb = ThemeFactory.Glass(0, 16);
        sb.BgColor = new Color(UiTokens.Surface, 0.97f);
        sb.BorderColor = new Color(accent, 0.45f);
        sb.ShadowSize = 40;
        sb.ShadowColor = new Color(0, 0, 0, 0.6f);
        _panel.AddThemeStyleboxOverride("panel", sb);
        AddChild(_panel);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 0);
        _panel.AddChild(col);

        // Faixa do cabecalho com brilho na cor de destaque.
        var head = new PanelContainer();
        var hb = new StyleBoxFlat
        {
            BgColor = new Color(accent, 0.10f),
            ContentMarginLeft = 28, ContentMarginRight = 20, ContentMarginTop = 22, ContentMarginBottom = 16,
            CornerRadiusTopLeft = 16, CornerRadiusTopRight = 16,
            BorderColor = new Color(accent, 0.35f), BorderWidthBottom = 1,
        };
        head.AddThemeStyleboxOverride("panel", hb);
        col.AddChild(head);
        var headRow = new HBoxContainer();
        headRow.AddThemeConstantOverride("separation", 18);
        head.AddChild(headRow);
        Header = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        Header.AddThemeConstantOverride("separation", 6);
        headRow.AddChild(Header);
        var right = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Begin };
        right.AddThemeConstantOverride("separation", 10);
        headRow.AddChild(right);
        var close = new Button { Text = "✕", TooltipText = "Fechar (Esc)", CustomMinimumSize = new Vector2(36, 36), FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ShrinkEnd };
        close.Pressed += Close;
        right.AddChild(close);
        Actions = new HBoxContainer();
        Actions.AddThemeConstantOverride("separation", 8);

        var actionsRow = new MarginContainer();
        actionsRow.AddThemeConstantOverride("margin_left", 28);
        actionsRow.AddThemeConstantOverride("margin_right", 20);
        actionsRow.AddThemeConstantOverride("margin_top", 12);
        actionsRow.AddChild(Actions);
        col.AddChild(actionsRow);

        var tabsMargin = new MarginContainer();
        tabsMargin.AddThemeConstantOverride("margin_left", 22);
        tabsMargin.AddThemeConstantOverride("margin_right", 22);
        tabsMargin.AddThemeConstantOverride("margin_top", 12);
        col.AddChild(tabsMargin);
        _tabBar = new HBoxContainer();
        _tabBar.AddThemeConstantOverride("separation", 4);
        tabsMargin.AddChild(_tabBar);

        var sep = new ColorRect { Color = UiTokens.Border, CustomMinimumSize = new Vector2(0, 1) };
        col.AddChild(sep);

        var bodyMargin = new MarginContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        bodyMargin.AddThemeConstantOverride("margin_left", 28);
        bodyMargin.AddThemeConstantOverride("margin_right", 20);
        bodyMargin.AddThemeConstantOverride("margin_top", 18);
        bodyMargin.AddThemeConstantOverride("margin_bottom", 22);
        col.AddChild(bodyMargin);
        _scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = SizeFlags.ExpandFill };
        bodyMargin.AddChild(_scroll);
    }

    public override void _Ready()
    {
        Layout();
        GetViewport().SizeChanged += Layout;
        _panel.Modulate = new Color(1, 1, 1, 0);
        _panel.Scale = new Vector2(0.97f, 0.97f);
        var tw = CreateTween().SetParallel().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tw.TweenProperty(_panel, "modulate:a", 1f, 0.16f);
        tw.TweenProperty(_panel, "scale", Vector2.One, 0.18f);
    }

    private void Layout()
    {
        var vp = GetViewportRect().Size;
        var size = new Vector2(Mathf.Min(1060, vp.X - 64), Mathf.Min(740, vp.Y - 64));
        _panel.Size = size;
        _panel.Position = (vp - size) / 2f;
        _panel.PivotOffset = size / 2f;
    }

    public void AddTab(string name, Func<Control> build, bool live = false)
    {
        int index = _tabs.Count;
        _tabs.Add((name, build, live));
        var b = new Button { Text = name, ToggleMode = true, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 38) };
        b.AddThemeFontOverride("font", UiFonts.SemiBold);
        b.AddThemeFontSizeOverride("font_size", 14);
        b.AddThemeStyleboxOverride("normal", TabBox(false));
        b.AddThemeStyleboxOverride("hover", TabBox(false, hover: true));
        b.AddThemeStyleboxOverride("pressed", TabBox(true));
        b.AddThemeStyleboxOverride("hover_pressed", TabBox(true));
        b.AddThemeColorOverride("font_color", UiTokens.TextSecondary);
        b.AddThemeColorOverride("font_pressed_color", UiTokens.AccentSoft);
        b.AddThemeColorOverride("font_hover_pressed_color", UiTokens.AccentSoft);
        b.Pressed += () => SelectTab(index);
        _tabButtons.Add(b);
        _tabBar.AddChild(b);
        if (_current < 0) SelectTab(0);
    }

    private static StyleBoxFlat TabBox(bool active, bool hover = false)
    {
        var sb = new StyleBoxFlat
        {
            BgColor = hover ? new Color(UiTokens.SurfaceHover, 0.6f) : new Color(0, 0, 0, 0),
            ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 6, ContentMarginBottom = 8,
            BorderColor = UiTokens.Accent, BorderWidthBottom = active ? 3 : 0,
            CornerRadiusTopLeft = 6, CornerRadiusTopRight = 6,
        };
        return sb;
    }

    public void SelectTab(int index)
    {
        _current = index;
        for (int i = 0; i < _tabButtons.Count; i++) _tabButtons[i].SetPressedNoSignal(i == index);
        Rebuild(resetScroll: true);
    }

    /// <summary>Reconstroi a aba atual se ela for "ao vivo" (mantem a rolagem).</summary>
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

    // ---------- Blocos reutilizaveis do corpo ----------

    public static PanelContainer Card(string title, Control body, Color? accent = null)
    {
        var card = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var sb = ThemeFactory.Box(new Color(UiTokens.SurfaceRaised, 0.55f), 16, border: UiTokens.Border, radius: 10);
        sb.ContentMarginTop = 14; sb.ContentMarginBottom = 14;
        if (accent.HasValue) { sb.BorderWidthTop = 2; sb.BorderColor = new Color(accent.Value, 0.7f); }
        card.AddThemeStyleboxOverride("panel", sb);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 10);
        col.AddChild(ThemeFactory.Section(title));
        col.AddChild(body);
        card.AddChild(col);
        return card;
    }

    public static Button Action(string text, string tip, Action onPress, bool primary = false, bool enabled = true)
    {
        var b = new Button { Text = text, TooltipText = tip, Disabled = !enabled, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 36) };
        if (primary)
        {
            b.AddThemeStyleboxOverride("normal", ThemeFactory.Box(UiTokens.Accent, 14, border: UiTokens.AccentSoft));
            b.AddThemeStyleboxOverride("hover", ThemeFactory.Box(UiTokens.AccentSoft, 14, border: UiTokens.AccentSoft));
            b.AddThemeColorOverride("font_color", UiTokens.Background);
            b.AddThemeColorOverride("font_hover_color", UiTokens.Background);
        }
        b.Pressed += onPress;
        return b;
    }

    /// <summary>Barra com rotulo, valor e cor (0..max).</summary>
    public static HBoxContainer Bar(string label, float value, float max, Color color, string valueText, float labelWidth = 170)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        var l = ThemeFactory.Text(label, UiTokens.FontSmall, UiTokens.TextPrimary);
        l.CustomMinimumSize = new Vector2(labelWidth, 0);
        row.AddChild(l);
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
        v.CustomMinimumSize = new Vector2(64, 0);
        v.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(v);
        return row;
    }

    public static PanelContainer Stat(string value, string caption, Color? color = null)
    {
        var tile = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        var sb = ThemeFactory.Box(new Color(UiTokens.Background, 0.45f), 12, border: UiTokens.Border, radius: 8);
        sb.ContentMarginTop = sb.ContentMarginBottom = 10;
        tile.AddThemeStyleboxOverride("panel", sb);
        var c = new VBoxContainer();
        c.AddThemeConstantOverride("separation", 0);
        c.AddChild(ThemeFactory.Text(value, 20, color ?? UiTokens.TextPrimary, UiFonts.Numbers));
        c.AddChild(ThemeFactory.Text(caption, UiTokens.FontCaption, UiTokens.TextSecondary));
        tile.AddChild(c);
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

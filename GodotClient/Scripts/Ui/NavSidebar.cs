using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Menu principal lateral (D-035): marca no topo, secoes (Mundo, Comunidade, Empresa,
/// Relatorios) com itens de icone e nome, subitens em sanfona, itens futuros apagados com o
/// marco, Configuracoes e o botao de recolher no rodape. Recolhido vira uma faixa de icones.
/// </summary>
public partial class NavSidebar : PanelContainer
{
    public const int ExpandedWidth = 224, CollapsedWidth = 60;

    public sealed class Entry
    {
        public string Kind { get; init; } = "";
        public string Icon { get; init; } = "";
        public string Label { get; init; } = "";
        public string Tip { get; init; } = "";
        /// <summary>Marco em que o item passa a existir (vazio = ja existe).</summary>
        public string Milestone { get; init; } = "";
        public Action? Go { get; init; }
        public Func<List<(string Label, string Icon, Action Go)>>? Subs { get; init; }
    }

    private readonly List<(string Section, List<Entry> Items)> _sections;
    private readonly Entry _settings;
    private readonly VBoxContainer _list;
    private readonly VBoxContainer _footer;
    private readonly Control _brand;
    private string _active = "map";
    private string _open = "";

    public bool Expanded { get; private set; } = true;
    public int Width => Expanded ? ExpandedWidth : CollapsedWidth;
    /// <summary>Mudou a largura (recolher/abrir).</summary>
    public event Action? Toggled;

    public NavSidebar(List<(string Section, List<Entry> Items)> sections, Entry settings, string worldName)
    {
        _sections = sections;
        _settings = settings;
        MouseFilter = MouseFilterEnum.Stop;
        AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color("#090B0F"), BorderColor = UiTokens.Divider, BorderWidthRight = 1 });
        SetAnchorsPreset(LayoutPreset.LeftWide);
        OffsetRight = ExpandedWidth;
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 0);
        AddChild(col);
        _brand = new PanelContainer { CustomMinimumSize = new Vector2(0, UiTokens.TopBarHeight), TooltipText = worldName };
        ((PanelContainer)_brand).AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), BorderColor = UiTokens.Divider, BorderWidthBottom = 1, ContentMarginLeft = 18, ContentMarginRight = 12 });
        col.AddChild(_brand);
        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = SizeFlags.ExpandFill };
        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _list.AddThemeConstantOverride("separation", 2);
        var pad = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        foreach (var (k, v) in new[] { ("margin_left", 8), ("margin_right", 8), ("margin_top", 8), ("margin_bottom", 8) }) pad.AddThemeConstantOverride(k, v);
        pad.AddChild(_list);
        scroll.AddChild(pad);
        col.AddChild(scroll);
        var foot = new MarginContainer();
        foreach (var (k, v) in new[] { ("margin_left", 8), ("margin_right", 8), ("margin_top", 6), ("margin_bottom", 10) }) foot.AddThemeConstantOverride(k, v);
        _footer = new VBoxContainer();
        _footer.AddThemeConstantOverride("separation", 2);
        foot.AddChild(_footer);
        col.AddChild(new ColorRect { Color = UiTokens.Divider, CustomMinimumSize = new Vector2(0, 1) });
        col.AddChild(foot);
        Rebuild();
    }

    /// <summary>Secao ativa (destaque no item).</summary>
    public string Active
    {
        get => _active;
        set
        {
            if (_active == value) return;
            _active = value;
            Rebuild();
        }
    }

    public void SetExpanded(bool expanded)
    {
        if (Expanded == expanded) return;
        Expanded = expanded;
        OffsetRight = Width;
        Rebuild();
        Toggled?.Invoke();
    }

    public void Toggle() => SetExpanded(!Expanded);

    private void Rebuild()
    {
        foreach (var c in _list.GetChildren()) { _list.RemoveChild(c); c.QueueFree(); }
        foreach (var c in _footer.GetChildren()) { _footer.RemoveChild(c); c.QueueFree(); }
        var brand = (PanelContainer)_brand;
        foreach (var c in brand.GetChildren()) { brand.RemoveChild(c); c.QueueFree(); }
        var b = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        b.AddThemeConstantOverride("separation", 9);
        b.AddChild(ThemeFactory.Text("◆", 15, UiTokens.Accent));
        if (Expanded) b.AddChild(ThemeFactory.Text("MMORPG TYCOON", 14, UiTokens.AccentSoft, UiFonts.Display));
        brand.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), BorderColor = UiTokens.Divider, BorderWidthBottom = 1, ContentMarginLeft = Expanded ? 20 : 23, ContentMarginRight = 8 });
        brand.AddChild(b);

        bool first = true;
        foreach (var (section, items) in _sections)
        {
            if (Expanded)
            {
                var cap = ThemeFactory.Text(section, UiTokens.FontCaption, UiTokens.TextDisabled, UiFonts.SemiBold);
                var m = new MarginContainer();
                m.AddThemeConstantOverride("margin_left", 12);
                m.AddThemeConstantOverride("margin_top", first ? 6 : 16);
                m.AddThemeConstantOverride("margin_bottom", 4);
                m.AddChild(cap);
                _list.AddChild(m);
            }
            else if (!first)
            {
                var m = new MarginContainer();
                m.AddThemeConstantOverride("margin_top", 8);
                m.AddThemeConstantOverride("margin_bottom", 8);
                m.AddThemeConstantOverride("margin_left", 10);
                m.AddThemeConstantOverride("margin_right", 10);
                m.AddChild(new ColorRect { Color = UiTokens.Divider, CustomMinimumSize = new Vector2(0, 1) });
                _list.AddChild(m);
            }
            first = false;
            foreach (var e in items) AddItem(_list, e);
        }
        AddItem(_footer, _settings);
        var toggle = Row(Expanded ? "panel-left-close" : "panel-left-open", Expanded ? "Recolher menu" : "", false, false, "", 0);
        toggle.TooltipText = Expanded ? "Recolher o menu (N)" : "Abrir o menu (N)";
        toggle.Pressed += Toggle;
        _footer.AddChild(toggle);
    }

    private void AddItem(VBoxContainer into, Entry e)
    {
        bool future = e.Milestone.Length > 0;
        bool active = e.Kind == _active;
        bool hasSubs = e.Subs != null && !future;
        bool open = hasSubs && _open == e.Kind && Expanded;
        var row = Row(e.Icon, e.Label, active, future, future ? e.Milestone : hasSubs ? (open ? "chevron-up" : "chevron-down") : "", 40);
        row.TooltipText = future ? $"{e.Label}\n~Chega no {e.Milestone}." : Expanded ? e.Tip : $"{e.Label}\n~{e.Tip.Split('\n').Last().TrimStart('~')}";
        if (!future)
            row.Pressed += () =>
            {
                if (hasSubs) _open = _open == e.Kind && _active == e.Kind ? "" : e.Kind;
                e.Go?.Invoke();
                Rebuild();
            };
        into.AddChild(row);
        if (!open) return;
        foreach (var (label, icon, go) in e.Subs!())
        {
            var sub = SubRow(label, icon);
            var g = go;
            sub.Pressed += () => g();
            into.AddChild(sub);
        }
    }

    /// <summary>Linha de item: icone, nome e, a direita, seta do submenu ou etiqueta do marco.</summary>
    private Button Row(string icon, string label, bool active, bool future, string trailing, int height)
    {
        var b = new TipButton { FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(0, height > 0 ? height : 36), SizeFlagsHorizontal = SizeFlags.ExpandFill, Disabled = future };
        b.MouseDefaultCursorShape = future ? CursorShape.Arrow : CursorShape.PointingHand;
        StyleBoxFlat S(bool on, bool hover)
        {
            var s = new StyleBoxFlat
            {
                BgColor = on ? new Color(UiTokens.Accent, 0.09f) : hover ? new Color(1, 1, 1, 0.045f) : new Color(0, 0, 0, 0),
                BorderColor = UiTokens.Accent, BorderWidthLeft = on ? 3 : 0,
            };
            s.SetCornerRadiusAll(4);
            return s;
        }
        b.AddThemeStyleboxOverride("normal", S(active, false));
        b.AddThemeStyleboxOverride("hover", S(active, true));
        b.AddThemeStyleboxOverride("pressed", S(true, true));
        b.AddThemeStyleboxOverride("disabled", S(false, false));
        var r = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        r.SetAnchorsPreset(LayoutPreset.FullRect);
        r.OffsetLeft = Expanded ? 14 : 0;
        r.OffsetRight = Expanded ? -10 : 0;
        r.AddThemeConstantOverride("separation", 12);
        if (!Expanded) r.Alignment = BoxContainer.AlignmentMode.Center;
        var col = future ? UiTokens.TextDisabled : active ? UiTokens.Accent : UiTokens.TextSecondary;
        r.AddChild(new Icon(icon, 18, col));
        if (Expanded && label.Length > 0)
        {
            var l = ThemeFactory.Text(label, 14, future ? UiTokens.TextDisabled : active ? UiTokens.TextPrimary : UiTokens.TextLabel, UiFonts.Medium);
            l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            l.VerticalAlignment = VerticalAlignment.Center;
            l.MouseFilter = MouseFilterEnum.Ignore;
            r.AddChild(l);
            if (trailing.StartsWith("chevron")) r.AddChild(new Icon(trailing, 14, UiTokens.TextDisabled));
            else if (trailing.Length > 0)
            {
                var tag = ThemeFactory.Text(trailing, UiTokens.FontCaption, UiTokens.TextDisabled, UiFonts.SemiBold);
                tag.VerticalAlignment = VerticalAlignment.Center;
                tag.MouseFilter = MouseFilterEnum.Ignore;
                r.AddChild(tag);
            }
        }
        b.AddChild(r);
        return b;
    }

    private Button SubRow(string label, string icon)
    {
        var b = ModalFrame.HoverRow(() => { });
        b.CustomMinimumSize = new Vector2(0, 32);
        var r = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        r.SetAnchorsPreset(LayoutPreset.FullRect);
        r.OffsetLeft = 44;
        r.OffsetRight = -10;
        r.AddThemeConstantOverride("separation", 10);
        r.AddChild(new Icon(icon, 14, UiTokens.TextDisabled));
        var l = ThemeFactory.Text(label, UiTokens.FontBody, UiTokens.TextSecondary);
        l.VerticalAlignment = VerticalAlignment.Center;
        l.MouseFilter = MouseFilterEnum.Ignore;
        r.AddChild(l);
        b.AddChild(r);
        return b;
    }
}

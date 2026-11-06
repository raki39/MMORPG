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
    private HBoxContainer _tabsRow = null!;
    private bool _compactActions;
    private readonly HFlowContainer _tabBar;
    private readonly ScrollContainer _scroll;
    /// <summary>Faixa de dados numa linha so (D-068): ao lado do titulo se couber, senao numa linha abaixo.</summary>
    /// <summary>Etiquetas do cabecalho (D-068): ao lado do titulo se couberem, senao numa linha propria abaixo.</summary>
    private readonly HBoxContainer _tags;
    private readonly ClipRow _tagsClip;
    private readonly HBoxContainer _top;
    private readonly VBoxContainer _headCol;
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

    /// <summary>Largura atual do menu lateral (as paginas comecam depois dele).</summary>
    public static int ContentLeft { get; set; } = NavSidebar.ExpandedWidth;

    public ModalFrame(Color accent)
    {
        SetAnchorsPreset(LayoutPreset.FullRect);
        OffsetTop = UiTokens.TopBarHeight;
        OffsetLeft = ContentLeft;
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
        _headCol = headCol;
        _crumb = ThemeFactory.Text("", UiTokens.FontCaption, UiTokens.TextDisabled, UiFonts.Medium);
        headCol.AddChild(_crumb);
        var top = new HBoxContainer();
        top.AddThemeConstantOverride("separation", 28);
        headCol.AddChild(top);
        _top = top;
        Header = new VBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter, CustomMinimumSize = new Vector2(220, 0) };
        Header.AddThemeConstantOverride("separation", 2);
        top.AddChild(Header);
        // Etiquetas (D-068) logo depois do bloco do titulo; ficam com quase toda a sobra da linha (o destaque so
        // encosta a direita).
        _tags = new HBoxContainer();
        _tags.AddThemeConstantOverride("separation", 0);
        _tagsClip = new ClipRow { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter, SizeFlagsStretchRatio = 1000, Visible = false };
        _tagsClip.AddChild(_tags);
        top.AddChild(_tagsClip);
        // O destaque encosta a direita, com a faixa de dados ao lado do titulo ou numa linha abaixo.
        Highlight = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter, SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.End };
        top.AddChild(Highlight);
        // Sem largura ao lado do titulo, as etiquetas descem para uma linha propria (sempre uma linha so).
        top.Resized += () => CallDeferred(nameof(PlaceTags));


        var tabsRow = new HBoxContainer();
        tabsRow.AddThemeConstantOverride("separation", 12);
        headCol.AddChild(tabsRow);
        _tabsRow = tabsRow;
        // Telas estreitas (1366): se abas e acoes nao cabem numa linha, as acoes com icone viram so icone.
        tabsRow.Resized += () => CallDeferred(nameof(UpdateCompactActions));
        _tabBar = new HFlowContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        _tabBar.AddThemeConstantOverride("h_separation", 2);
        _tabBar.AddThemeConstantOverride("v_separation", 0);
        tabsRow.AddChild(_tabBar);
        Actions = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.End, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        Actions.AddThemeConstantOverride("separation", 6);
        tabsRow.AddChild(Actions);

        _scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = SizeFlags.ExpandFill };
        var body = new MarginContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        body.AddThemeConstantOverride("margin_left", 16);
        body.AddThemeConstantOverride("margin_right", 4);
        body.AddThemeConstantOverride("margin_top", 16);
        body.AddThemeConstantOverride("margin_bottom", 16);
        body.AddChild(_scroll);
        col.AddChild(body);
        _scroll.GuiInput += OnPageWheel;
    }

    // ---------- Pagina em telas (D-068) ----------

    private Tween? _snap;

    /// <summary>Pagina em telas da aba atual (conteudo raiz), se houver.</summary>
    private ScreenPage? CurrentPage() =>
        _scroll.GetChildCount() > 0 && _scroll.GetChild(0) is Control wrap && wrap.GetChildCount() > 0 ? wrap.GetChild(0) as ScreenPage : null;

    /// <summary>Tela mais proxima da rolagem atual.</summary>
    private static int NearestScreen(ScreenPage page, float scroll)
    {
        int best = 0;
        for (int i = 1; i < page.ScreenTops.Count; i++)
            if (Mathf.Abs(page.ScreenTops[i] - scroll) < Mathf.Abs(page.ScreenTops[best] - scroll)) best = i;
        return best;
    }

    /// <summary>
    /// Roda do mouse numa pagina em telas: salta para a tela anterior ou seguinte, inteira (um salto por
    /// gesto). Dentro de um card com conteudo maior, a roda rola o card primeiro (o card consome o evento).
    /// </summary>
    private void OnPageWheel(InputEvent e)
    {
        if (e is not InputEventMouseButton { Pressed: true } mb || mb.ButtonIndex is not (MouseButton.WheelUp or MouseButton.WheelDown)) return;
        if (CurrentPage() is not { ScreenTops.Count: > 1 }) return;
        _scroll.AcceptEvent();
        Snap(mb.ButtonIndex == MouseButton.WheelDown ? 1 : -1);
    }

    /// <summary>Salta uma tela (dir +1 ou -1), com animacao curta; ignora enquanto o salto anterior anda.</summary>
    private void Snap(int dir)
    {
        var page = CurrentPage();
        if (page == null || page.ScreenTops.Count < 2 || (_snap != null && _snap.IsRunning())) return;
        int cur = NearestScreen(page, _scroll.ScrollVertical);
        int next = Math.Clamp(cur + dir, 0, page.ScreenTops.Count - 1);
        if (next == cur && Mathf.Abs(page.ScreenTops[cur] - _scroll.ScrollVertical) < 1) return;
        _snap = CreateTween();
        _snap.TweenProperty(_scroll, "scroll_vertical", (int)page.ScreenTops[next], 0.22).SetTrans(Tween.TransitionType.Cubic).SetEase(Tween.EaseType.Out);
    }

    /// <summary>Capturas (--wheel=N): N giros da roda para baixo, pela mesma regra do mouse.</summary>
    public async void WheelForCapture(int turns)
    {
        for (int i = 0; i < turns; i++)
        {
            await ToSignal(GetTree().CreateTimer(0.35), SceneTreeTimer.SignalName.Timeout);
            Snap(1);
        }
    }

    /// <summary>Vai para a tela N da pagina (capturas: --section=N).</summary>
    public void ScrollToSection(int index)
    {
        _pendingSection = index;
        _pendingScrollFrames = 30;
        SetProcess(true);
    }

    private int _pendingSection = -1;

    /// <summary>Rolagem dentro dos cards de altura fixa, guardada pelo titulo entre atualizacoes ao vivo.</summary>
    private static void CollectCardScroll(Node n, Dictionary<string, int> into)
    {
        if (n is ScrollContainer s && s.HasMeta("card")) into[(string)s.GetMeta("card")] = s.ScrollVertical;
        foreach (var c in n.GetChildren()) CollectCardScroll(c, into);
    }

    private static void ApplyCardScroll(Node n, Dictionary<string, int> from)
    {
        if (n is ScrollContainer s && s.HasMeta("card") && from.TryGetValue((string)s.GetMeta("card"), out int v)) s.ScrollVertical = v;
        foreach (var c in n.GetChildren()) ApplyCardScroll(c, from);
    }

    private Dictionary<string, int> _cardScroll = new();
    private void RestoreCardScroll() { ApplyCardScroll(_scroll, _cardScroll); _cardScroll = new(); }

    /// <summary>Titulo do cabecalho (D-068): 24 px seminegrito, igual em todas as paginas.</summary>
    public static Label TitleText(string title) => ThemeFactory.Text(title, 24, UiTokens.TextPrimary, UiFonts.SemiBold);

    /// <summary>
    /// Bloco de identificacao do cabecalho (D-068): linhas em duas colunas alinhadas (o segundo texto de cada
    /// linha comeca no mesmo lugar). Celulas em ordem: linha 1 esquerda, linha 1 direita, linha 2...
    /// </summary>
    public static GridContainer InfoGrid(params Control[] cells)
    {
        var g = new GridContainer { Columns = 2 };
        g.AddThemeConstantOverride("h_separation", 22);
        g.AddThemeConstantOverride("v_separation", 1);
        foreach (var c in cells) g.AddChild(c);
        return g;
    }

    /// <summary>
    /// Trecho do bloco de identificacao: icone e texto, com explicacao no tooltip. maxWidth (px) limita o texto
    /// (reticencias), para o bloco ter a mesma largura em qualquer ficha.
    /// </summary>
    public static Control Info(string icon, string text, Color iconColor, Color textColor, string tip = "", Font? font = null, int maxWidth = 0)
    {
        var p = new TipPanel { TooltipText = tip, MouseFilter = tip.Length > 0 ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore };
        p.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        var r = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        r.AddThemeConstantOverride("separation", 7);
        if (icon.Length > 0) r.AddChild(new Icon(icon, 14, iconColor) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
        var f = font ?? UiFonts.Regular;
        var l = ThemeFactory.Text(text, UiTokens.FontBody, textColor, f);
        if (maxWidth > 0)
        {
            float natural = f.GetStringSize(text, HorizontalAlignment.Left, -1, UiTokens.FontBody).X + 2;
            l.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            l.CustomMinimumSize = new Vector2(Mathf.Min(natural, maxWidth), 0);
        }
        r.AddChild(l);
        p.AddChild(r);
        return p;
    }

    /// <summary>Largura de cada etiqueta (igual para todas e para qualquer pessoa).</summary>
    public int TagWidth { get; set; } = 185;

    /// <summary>
    /// Etiqueta (D-068): sem fundo, todas com a mesma largura e filetes entre elas; em cima o rotulo curto que
    /// diz o que e, embaixo o icone e o valor (valor longo com reticencias); a explicacao fica no tooltip.
    /// </summary>
    public void Tag(string icon, string caption, string value, Color iconColor, string tooltip)
    {
        if (_tags.GetChildCount() > 0)
            _tags.AddChild(new ColorRect { Color = UiTokens.Divider, CustomMinimumSize = new Vector2(1, 36), SizeFlagsVertical = SizeFlags.ShrinkCenter });
        var p = new TipPanel { TooltipText = tooltip, MouseFilter = MouseFilterEnum.Stop, CustomMinimumSize = new Vector2(TagWidth, 0) };
        p.AddThemeStyleboxOverride("panel", new StyleBoxEmpty { ContentMarginLeft = 12, ContentMarginRight = 8 });
        var col = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        col.AddThemeConstantOverride("separation", 2);
        col.AddChild(ThemeFactory.Text(caption, UiTokens.FontSmall, UiTokens.TextSecondary, UiFonts.Medium));
        var r = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        r.AddThemeConstantOverride("separation", 7);
        r.AddChild(new Icon(icon, 15, iconColor) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
        var v = ThemeFactory.Text(value, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.SemiBold);
        v.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        v.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        r.AddChild(v);
        col.AddChild(r);
        p.AddChild(col);
        _tags.AddChild(p);
        _tagsClip.Visible = true;
        CallDeferred(nameof(PlaceTags));
    }

    /// <summary>As etiquetas ficam ao lado do bloco do titulo se couberem; senao, numa linha propria logo abaixo.</summary>
    private void PlaceTags()
    {
        if (!IsInstanceValid(_tagsClip) || !_tagsClip.Visible) return;
        float need = _tags.GetCombinedMinimumSize().X;
        float side = _top.Size.X - Header.GetCombinedMinimumSize().X - Highlight.GetCombinedMinimumSize().X - 28 * 2 - 8;
        bool below = need > side;
        bool isBelow = _tagsClip.GetParent() == _headCol;
        if (below == isBelow) return;
        _tagsClip.GetParent().RemoveChild(_tagsClip);
        if (below) { _headCol.AddChild(_tagsClip); _headCol.MoveChild(_tagsClip, _top.GetIndex() + 1); }
        else { _top.AddChild(_tagsClip); _top.MoveChild(_tagsClip, Header.GetIndex() + 1); }
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
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 10,
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
        var cardScroll = new Dictionary<string, int>();
        if (!resetScroll) CollectCardScroll(_scroll, cardScroll);
        foreach (var c in _scroll.GetChildren()) { _scroll.RemoveChild(c); c.QueueFree(); }
        var content = _tabs[_current].Build();
        content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        // Folga entre o conteudo e a barra de rolagem.
        var wrap = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        wrap.AddThemeConstantOverride("margin_right", 14);
        wrap.AddChild(content);
        _scroll.AddChild(wrap);
        if (!resetScroll)
        {
            CallDeferred(nameof(RestoreScroll), scroll);
            _cardScroll = cardScroll;
            if (cardScroll.Count > 0) CallDeferred(nameof(RestoreCardScroll));
        }
    }

    private void RestoreScroll(int value) => _scroll.ScrollVertical = value;

    /// <summary>Rola o corpo da pagina (capturas automaticas).</summary>
    public void ScrollTo(int value)
    {
        // Capturas: reaplica por alguns quadros, ate a pagina terminar de crescer (cards empilhados em 1366).
        _pendingScroll = value;
        _pendingScrollFrames = 30;
        SetProcess(true);
    }

    private int _pendingScroll;
    private int _pendingScrollFrames;

    public override void _Process(double delta)
    {
        if (_pendingScrollFrames <= 0) { SetProcess(false); return; }
        _pendingScrollFrames--;
        if (_pendingSection >= 0 && CurrentPage() is { } page && page.ScreenTops.Count > 0)
            _scroll.ScrollVertical = (int)page.ScreenTops[Math.Min(_pendingSection, page.ScreenTops.Count - 1)];
        else _scroll.ScrollVertical = _pendingScroll;
    }

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

    /// <summary>
    /// Card de altura fixa (D-068), para a pagina em telas: a altura vem da linha, nunca do conteudo. O corpo
    /// preenche o card (graficos marcados com Grow crescem com ele); se passar da altura, rola dentro do card.
    /// </summary>
    public static PanelContainer FillCard(string title, Control body, string info = "", Control? headerRight = null)
    {
        var scroll = new ScrollContainer
        {
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            SizeFlagsVertical = SizeFlags.ExpandFill, SizeFlagsHorizontal = SizeFlags.ExpandFill, FollowFocus = false,
        };
        scroll.SetMeta("card", title);
        body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        body.SizeFlagsVertical = SizeFlags.ExpandFill;
        scroll.AddChild(body);
        var card = Card(title, scroll, info: info, headerRight: headerRight);
        if (card.GetChild(0) is Control col) col.SizeFlagsVertical = SizeFlags.ExpandFill;
        return card;
    }

    /// <summary>Marca o elemento (um grafico) para crescer com a altura do card fixo.</summary>
    public static T Grow<T>(T c) where T : Control
    {
        c.SizeFlagsVertical = SizeFlags.ExpandFill;
        return c;
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

    /// <summary>
    /// Botao de acao do cabecalho. Com icone, em tela estreita ele mostra so o icone (o rotulo fica no
    /// tooltip), para as abas caberem numa linha.
    /// </summary>
    public static Button Action(string text, string tip, Action onPress, bool primary = false, bool enabled = true, string icon = "")
    {
        var b = new TipButton { Text = text, TooltipText = tip, Disabled = !enabled, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 28) };
        b.AddThemeFontOverride("font", UiFonts.Medium);
        b.AddThemeFontSizeOverride("font_size", 12);
        b.SetMeta("label", text);
        if (icon.Length > 0 && Icons.Has(icon))
        {
            b.SetMeta("icon", icon);
            var ic = primary ? UiTokens.Background : UiTokens.TextLabel;
            foreach (var state in new[] { "icon_normal_color", "icon_hover_color", "icon_pressed_color", "icon_focus_color" }) b.AddThemeColorOverride(state, ic);
            b.AddThemeColorOverride("icon_disabled_color", UiTokens.TextDisabled);   // fundo desativado e escuro, inclusive no primario
        }
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

    /// <summary>Troca o rotulo de uma acao respeitando o modo compacto (so icone).</summary>
    public void SetActionLabel(Button b, string text)
    {
        b.SetMeta("label", text);
        if (!(_compactActions && b.HasMeta("icon"))) b.Text = text;
    }

    /// <summary>Compacta as acoes com icone quando abas + acoes (com rotulo) nao cabem na linha.</summary>
    private void UpdateCompactActions()
    {
        if (!IsInsideTree() || _tabsRow.Size.X <= 0) return;
        float tabs = 0;
        foreach (var t in _tabButtons) tabs += t.GetCombinedMinimumSize().X;
        tabs += 2 * Math.Max(0, _tabButtons.Count - 1);
        float full = 0;
        int n = 0;
        foreach (var child in Actions.GetChildren())
        {
            if (child is not Button b || !b.Visible) continue;
            string label = b.HasMeta("label") ? b.GetMeta("label").AsString() : b.Text;
            full += UiFonts.Medium.GetStringSize(label, HorizontalAlignment.Left, -1, 12).X + 28;
            n++;
        }
        full += 6 * Math.Max(0, n - 1);
        bool compact = tabs + 12 + full > _tabsRow.Size.X;
        if (compact == _compactActions) return;
        _compactActions = compact;
        foreach (var child in Actions.GetChildren())
        {
            if (child is not Button b || !b.HasMeta("icon")) continue;
            if (compact)
            {
                b.Text = "";
                b.Icon = Icons.Get(b.GetMeta("icon").AsString(), 16);
                b.CustomMinimumSize = new Vector2(32, 28);
            }
            else
            {
                b.Text = b.GetMeta("label").AsString();
                b.Icon = null;
                b.CustomMinimumSize = new Vector2(0, 28);
            }
        }
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

    /// <summary>
    /// Linha de dado: rotulo a esquerda, valor a direita. O valor corta com reticencias quando
    /// nao cabe: um texto longo (ex.: nome de atividade) nunca alarga o card e nunca obriga a
    /// grade a empilhar os cards da linha.
    /// </summary>
    public static Control KeyValue(string key, string value, Color? color = null)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        var k = ThemeFactory.Text(key, UiTokens.FontSmall, UiTokens.TextSecondary);
        row.AddChild(k);
        var v = ThemeFactory.Text(value, UiTokens.FontBody, color ?? UiTokens.TextPrimary, UiFonts.Numbers);
        v.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        v.HorizontalAlignment = HorizontalAlignment.Right;
        v.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        row.AddChild(v);
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

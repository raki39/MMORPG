using Godot;
using Simulation.Core.Content;
using Simulation.Core.Events;
using Simulation.Core.Population;
using GodotClient.Map;
using GodotClient.Sim;

namespace GodotClient.Ui;

/// <summary>
/// Painel lateral fixo de gestao (D-022): Visao geral (indicadores, grafico 24 h, atividades,
/// perfis), Zonas (tabela), Jogadores (tabela com busca), Eventos (feed com filtros) e Fixados (cartoes
/// fixados no mapa, D-068).
/// Atualiza ao vivo sem perder rolagem nem ordenacao.
/// </summary>
public partial class SidePanel : PanelContainer
{
    private readonly ClientContext _ctx;
    private readonly WorldMapView _map;
    private readonly MetricsHistory _metrics;
    private readonly Func<IReadOnlyList<(SimEvent Event, int PersonId)>> _feed;
    private readonly List<Button> _tabs = new();
    private readonly ScrollContainer _scroll;
    private int _tab;
    private string _search = "";
    private int _feedFilter;
    private LineEdit? _searchBox;

    private readonly DataTable<RegionDefinition> _zoneTable;
    private readonly DataTable<(int Id, CharacterView V)> _playerTable;

    public SidePanel(ClientContext ctx, WorldMapView map, MetricsHistory metrics, Func<IReadOnlyList<(SimEvent, int)>> feed)
    {
        _ctx = ctx;
        _map = map;
        _metrics = metrics;
        _feed = feed;
        MouseFilter = MouseFilterEnum.Stop;
        var sb = new StyleBoxFlat { BgColor = UiTokens.Surface, BorderColor = UiTokens.Border, BorderWidthLeft = 1 };
        AddThemeStyleboxOverride("panel", sb);

        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 0);
        AddChild(col);
        var tabsBg = new PanelContainer();
        tabsBg.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = UiTokens.SurfaceHeader, BorderColor = UiTokens.Border, BorderWidthBottom = 1, ContentMarginLeft = 6, ContentMarginRight = 6 });
        var tabRow = new HBoxContainer();
        tabRow.AddThemeConstantOverride("separation", 0);
        tabsBg.AddChild(tabRow);
        col.AddChild(tabsBg);
        foreach (var (name, i) in new[] { ("Visão geral", 0), ("Zonas", 1), ("Jogadores", 2), ("Eventos", 3), ("Fixados", 4) })
        {
            var b = new Button { Text = name, ToggleMode = true, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 38), SizeFlagsHorizontal = SizeFlags.ExpandFill };
            b.AddThemeFontOverride("font", UiFonts.Medium);
            b.AddThemeFontSizeOverride("font_size", 13);
            StyleBoxFlat T(bool on, bool hover) => new() { BgColor = hover ? UiTokens.SurfaceHover : new Color(0, 0, 0, 0), BorderColor = UiTokens.Accent, BorderWidthBottom = on ? 2 : 0 };
            b.AddThemeStyleboxOverride("normal", T(false, false));
            b.AddThemeStyleboxOverride("hover", T(false, true));
            b.AddThemeStyleboxOverride("pressed", T(true, false));
            b.AddThemeStyleboxOverride("hover_pressed", T(true, true));
            b.AddThemeColorOverride("font_color", UiTokens.TextLabel);
            b.AddThemeColorOverride("font_hover_color", UiTokens.TextPrimary);
            b.AddThemeColorOverride("font_pressed_color", UiTokens.Accent);
            b.AddThemeColorOverride("font_hover_pressed_color", UiTokens.AccentSoft);
            int idx = i;
            b.Pressed += () => { _tab = idx; SyncTabs(); Refresh(resetScroll: true); };
            _tabs.Add(b);
            tabRow.AddChild(b);
        }
        _scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, SizeFlagsVertical = SizeFlags.ExpandFill };
        var m = new MarginContainer { SizeFlagsVertical = SizeFlags.ExpandFill };
        foreach (var (k, v) in new[] { ("margin_left", 10), ("margin_right", 3), ("margin_top", 10), ("margin_bottom", 10) }) m.AddThemeConstantOverride(k, v);
        m.AddChild(_scroll);
        col.AddChild(m);

        _zoneTable = new DataTable<RegionDefinition>(new[]
        {
            new Column<RegionDefinition> { Title = "Zona", Text = z => z.Name, SortKey = z => z.Name, Tooltip = z => $"{z.Name}\nFunção|{PtBr.Function(z.Function)}\nÁreas|{z.Areas.Count}\nSaídas|{z.Connections.Count}\n~Clique para abrir o dossiê da zona." },
            new Column<RegionDefinition> { Title = "Jog.", Width = 52, Right = true, Numeric = true, Text = z => _map.PeopleInZone(z.Id).ToString(), SortKey = z => _map.PeopleInZone(z.Id), Color = z => _map.PeopleInZone(z.Id) > 0 ? UiTokens.Info : UiTokens.TextDisabled },
            new Column<RegionDefinition> { Title = "Perigo", Width = 62, Right = true, Numeric = true, Text = z => ((int)(WorldMapView.DangerOf(z) * 100)).ToString(), SortKey = z => WorldMapView.DangerOf(z), Color = z => WorldMapView.Ramp(WorldMapView.DangerOf(z)), Tooltip = z => $"Índice de perigo\nCombate|{PtBr.Pvp(z.Pvp)}\nMorte|{PtBr.Death(z.DeathRule)}\n~Combina risco das atividades, PvP e regra de morte." },
            new Column<RegionDefinition> { Title = "Nível", Width = 62, Right = true, Numeric = true, Text = z => z.IsCity ? "—" : $"{z.LevelMin}-{z.LevelMax}", SortKey = z => z.LevelMin },
        }, sortColumn: 1) { OnClick = z => _ctx.OpenZone(z.Id), Gap = 10 };
        _zoneTable.OnSortChanged = () => Refresh(false);

        _playerTable = new DataTable<(int Id, CharacterView V)>(new[]
        {
            new Column<(int Id, CharacterView V)> { Title = "Jogador", Text = r => (_ctx.Favorites.Contains(r.Id) ? "★ " : "") + r.V.Name, SortKey = r => r.V.Name, Color = r => UiTokens.ProfileColor(_ctx.Persons[r.Id - 1].ProfileId).Lightened(0.25f), Tooltip = r => $"{r.V.Name}\nPessoa|{_ctx.Persons[r.Id - 1].Name}\nPerfil|{ProfileName(r.Id)}\n~Clique para abrir o dossiê." },
            new Column<(int Id, CharacterView V)> { Title = "Nv", Width = 40, Right = true, Numeric = true, Text = r => r.V.Level.ToString(), SortKey = r => r.V.Level, Color = _ => UiTokens.Info },
            new Column<(int Id, CharacterView V)> { Title = "Fazendo", Width = 128, Text = r => StatusText.Of(r.V, _ctx.Content), SortKey = r => r.V.ActivityId, Color = _ => UiTokens.TextSecondary, Tooltip = r => $"{StatusText.Of(r.V, _ctx.Content)}\nZona|{_ctx.ZoneName(r.V.RegionId)}" },
        }, sortColumn: 1) { OnClick = r => _ctx.OpenPerson(r.Id), Gap = 10 };
        _playerTable.OnSortChanged = () => Refresh(false);

        SyncTabs();
    }

    private string ProfileName(int id) => _ctx.Content.Profiles.TryGetValue(_ctx.Persons[id - 1].ProfileId, out var p) ? p.Name : "";
    private string ClassName(string id) => _ctx.Content.Classes.TryGetValue(id, out var c) ? c.Name : id;

    private static Label Hint(string text)
    {
        var l = ThemeFactory.Text(text, UiTokens.FontCaption, UiTokens.TextDisabled);
        l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        l.CustomMinimumSize = new Vector2(100, 0);
        return l;
    }

    /// <summary>Abre uma aba (capturas e atalhos).</summary>
    public void ShowTab(int tab)
    {
        _tab = Math.Clamp(tab, 0, _tabs.Count - 1);
        SyncTabs();
        Refresh(resetScroll: true);
    }

    private void SyncTabs()
    {
        for (int i = 0; i < _tabs.Count; i++) _tabs[i].SetPressedNoSignal(i == _tab);
    }

    public void Refresh(bool resetScroll = false)
    {
        int scroll = _scroll.ScrollVertical;
        bool keepSearchFocus = _searchBox != null && IsInstanceValid(_searchBox) && _searchBox.HasFocus();
        int caret = keepSearchFocus ? _searchBox!.CaretColumn : 0;
        foreach (var c in _scroll.GetChildren()) { _scroll.RemoveChild(c); c.QueueFree(); }
        Control content = _tab switch { 1 => BuildZones(), 2 => BuildPlayers(), 3 => BuildEvents(), 4 => BuildPinned(), _ => BuildOverview() };
        content.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var wrap = new MarginContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        wrap.AddThemeConstantOverride("margin_right", 12);
        wrap.AddChild(content);
        _scroll.AddChild(wrap);
        if (keepSearchFocus && _searchBox != null) { _searchBox.GrabFocus(); _searchBox.CaretColumn = caret; }
        if (!resetScroll) CallDeferred(nameof(RestoreScroll), scroll);
    }

    private void RestoreScroll(int v) => _scroll.ScrollVertical = v;

    // ---------------- Visao geral ----------------

    private Control BuildOverview()
    {
        var snap = _ctx.Snapshot();
        var online = new List<CharacterView>();
        foreach (var v in snap.ByPerson) if (v != null && v.Status != CharacterStatus.Offline) online.Add(v);
        double avgLevel = snap.Registered == 0 ? 0 : snap.ByPerson.Where(v => v != null).Average(v => v!.Level);
        int zonesActive = _ctx.Content.Regions.Keys.Count(z => _map.PeopleInZone(z) > 0);

        var grid = new GridContainer { Columns = 3 };
        grid.AddThemeConstantOverride("h_separation", 6);
        grid.AddThemeConstantOverride("v_separation", 6);
        grid.AddChild(ModalFrame.Stat(snap.Online.ToString(), "Conectados", UiTokens.Info, $"Conectados agora\nTotal|{snap.Online}\nPico 24 h|{(_metrics.Online.Count == 0 ? 0 : _metrics.Online.Max()):0}\n~Pessoas com sessão aberta.", fill: true));
        grid.AddChild(ModalFrame.Stat(snap.Registered.ToString(), "Cadastrados", UiTokens.Success, $"Contas criadas\nCadastrados|{snap.Registered}\nInteressados|{snap.PersonsTotal}\n~Registro progressivo do lançamento.", fill: true));
        grid.AddChild(ModalFrame.Stat((snap.PersonsTotal - snap.Registered).ToString(), "Sem conta", UiTokens.Warning, "Interessados sem conta\n~Ainda podem se registrar.", fill: true));
        grid.AddChild(ModalFrame.Stat(PtBr.Decimal(avgLevel, 1), "Nível médio", UiTokens.AccentSoft, "Nível médio dos personagens\n~Curva provisória até o combate (M3).", fill: true));
        grid.AddChild(ModalFrame.Stat($"{zonesActive}/{_ctx.Content.Regions.Count}", "Zonas ativas", null, "Zonas com alguém agora", fill: true));
        grid.AddChild(ModalFrame.Stat(online.Count(v => v.Status == CharacterStatus.Traveling).ToString(), "Na estrada", null, "Jogadores viajando entre zonas ou até uma área", fill: true));
        foreach (var c in grid.GetChildren().OfType<Control>()) c.SizeFlagsHorizontal = SizeFlags.ExpandFill;

        var labels = _metrics.Ticks.Select(t => _ctx.Clock(t)).ToList();
        var city = _ctx.Content.World.StartingRegionId;
        var chart = new LineChart
        {
            CustomMinimumSize = new Vector2(0, 150),
            XLabels = labels,
            Series = new[]
            {
                new ChartSeries { Name = "Conectados", Color = UiTokens.Info, Values = _metrics.Online.ToList() },
                new ChartSeries { Name = _ctx.ZoneName(city), Color = UiTokens.Accent, Values = _metrics.OnlineByZone.TryGetValue(city, out var cs) ? cs.ToList() : new List<float>() },
            },
        };

        var acts = online.Where(v => v.Status == CharacterStatus.Doing && v.ActivityId.Length > 0)
            .GroupBy(v => v.TargetRegionId + "/" + v.ActivityId)
            .OrderByDescending(g => g.Count()).Take(6)
            .Select(g =>
            {
                var p = g.Key.Split('/');
                var a = _ctx.Content.Regions[p[0]].FindActivity(p[1]);
                return (_ctx.ActivityName(p[0], p[1]), (float)g.Count(), ActivityPalette.Color(a?.Type ?? ""),
                    $"{_ctx.ActivityName(p[0], p[1])}\nLugar|{_ctx.PlaceName(p[0], p[1])}\nZona|{_ctx.ZoneName(p[0])}\nAgora|{g.Count()}");
            });

        var profiles = online.GroupBy(v => _ctx.Persons[v.PersonId - 1].ProfileId).OrderByDescending(g => g.Count())
            .Select(g => (_ctx.Content.Profiles.TryGetValue(g.Key, out var pr) ? pr.Name : g.Key, (float)g.Count(), UiTokens.ProfileColor(g.Key),
                $"Perfil {(_ctx.Content.Profiles.TryGetValue(g.Key, out var p2) ? p2.Name : g.Key)}\nConectados|{g.Count()}\n~A cor do perfil é a do ponto no mapa."));

        return ModalFrame.Column(10,
            grid,
            ModalFrame.Card("Conectados — últimas 24 h", chart, UiTokens.Info),
            ModalFrame.Card("Fazendo agora", BarList.Build(acts), UiTokens.Accent),
            ModalFrame.Card("Perfis conectados", BarList.Build(profiles), UiTokens.Success));
    }

    // ---------------- Zonas ----------------

    private Control BuildZones() => ModalFrame.Column(8,
        Hint("Clique numa linha para ver o resumo. Clique no título da coluna para ordenar."),
        _zoneTable.Build(_ctx.Content.Regions.Values));

    // ---------------- Jogadores ----------------

    private Control BuildPlayers()
    {
        var snap = _ctx.Snapshot();
        var rows = new List<(int, CharacterView)>();
        for (int id = 1; id < snap.ByPerson.Length; id++)
        {
            var v = snap.ByPerson[id];
            if (v == null || v.Status == CharacterStatus.Offline) continue;
            if (_search.Length > 0)
            {
                string hay = $"{v.Name} {_ctx.Persons[id - 1].Name} {ClassName(v.ClassId)} {StatusText.Of(v, _ctx.Content)}";
                if (hay.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;
            }
            rows.Add((id, v));
        }
        _searchBox = new LineEdit { PlaceholderText = "Buscar jogador, classe, zona ou atividade…", Text = _search, ClearButtonEnabled = true };
        _searchBox.TextChanged += t => { _search = t.Trim(); Refresh(true); };
        return ModalFrame.Column(8, _searchBox,
            ThemeFactory.Text($"{rows.Count} conectados", UiTokens.FontCaption, UiTokens.TextSecondary),
            _playerTable.Build(rows));
    }

    // ---------------- Fixados ----------------

    /// <summary>
    /// Cartoes fixados no mapa (D-068): uma linha por cartao, na ordem em que foram fixados. Clique mostra o
    /// cartao (reabre se foi fechado); os botoes enquadram no mapa ou soltam.
    /// </summary>
    private Control BuildPinned()
    {
        var col = ModalFrame.Column(2);
        if (_ctx.Pinned.Count == 0)
        {
            col.AddChild(Hint("Nenhum cartão fixado. No mapa, abra o resumo de um jogador, zona ou área e clique no alfinete para mantê-lo aberto e listado aqui."));
            return col;
        }
        col.AddChild(ThemeFactory.Text(_ctx.Pinned.Count == 1 ? "1 cartão fixado" : $"{_ctx.Pinned.Count} cartões fixados", UiTokens.FontCaption, UiTokens.TextSecondary));
        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });
        var snap = _ctx.Snapshot();
        foreach (var key in _ctx.Pinned.ToArray())
        {
            var k = key.Split('#');
            Control emblem;
            string name, line;
            Color lineColor = UiTokens.TextSecondary;
            Action locate;
            if (k[0] == "person" && k.Length == 2 && int.TryParse(k[1], out int id) && id >= 1 && id <= _ctx.Persons.Count)
            {
                var p = _ctx.Persons[id - 1];
                var v = id < snap.ByPerson.Length ? snap.ByPerson[id] : null;
                var cls = v != null && _ctx.Content.Classes.TryGetValue(v.ClassId, out var c) ? c : null;
                emblem = new PortraitBadge(p.AppearanceSeed, UiTokens.ProfileColor(p.ProfileId), v?.Name ?? p.Name) { ClassIcon = PlayerModal.ClassIcon(cls) };
                name = v?.Name ?? p.Name;
                bool online = v != null && v.Status != CharacterStatus.Offline;
                line = v == null ? "Sem conta" : $"Nv {v.Level} · {StatusText.Of(v, _ctx.Content)}";
                lineColor = online ? UiTokens.TextLabel : UiTokens.TextDisabled;
                locate = () => _ctx.FocusPerson(id, true);
            }
            else if (k[0] == "zone" && k.Length == 2 && _ctx.Content.Regions.TryGetValue(k[1], out var z))
            {
                emblem = new ZoneEmblem(z);
                name = z.Name;
                int n = _map.PeopleInZone(z.Id);
                line = $"Zona · {(n == 1 ? "1 jogador" : $"{n} jogadores")}";
                locate = () => _ctx.FocusZone(z.Id);
            }
            else if (k[0] == "area" && k.Length == 3 && _ctx.Content.Regions.TryGetValue(k[1], out var za) && za.Areas.FirstOrDefault(a => a.Id == k[2]) is { } area)
            {
                emblem = new KindEmblem(area.Kind);
                name = area.Name;
                line = $"{AreaPalette.Name(area.Kind)} · {za.Name}";
                locate = () => _ctx.FocusArea(za.Id, area.Id);
            }
            else continue;

            string pinned = key;
            var row = ModalFrame.HoverRow(() => _ctx.ShowPinned(pinned), $"{name}\n~Clique para mostrar o cartão no mapa.");
            row.CustomMinimumSize = new Vector2(0, 46);
            var h = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            h.SetAnchorsPreset(LayoutPreset.FullRect);
            h.OffsetLeft = 6; h.OffsetRight = -2;
            h.AddThemeConstantOverride("separation", 10);
            emblem.CustomMinimumSize = new Vector2(34, 34);
            emblem.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            emblem.MouseFilter = MouseFilterEnum.Ignore;
            h.AddChild(emblem);
            var names = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
            names.AddThemeConstantOverride("separation", 0);
            var nl = ThemeFactory.Text(name, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.SemiBold);
            nl.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            names.AddChild(nl);
            var ll = ThemeFactory.Text(line, UiTokens.FontCaption, lineColor, UiFonts.Medium);
            ll.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            names.AddChild(ll);
            h.AddChild(names);
            h.AddChild(RowButton("locate-fixed", "Enquadrar no mapa", locate));
            h.AddChild(RowButton("x", "Soltar\n~Tira dos fixados e fecha o cartão.", () => _ctx.Unpin(pinned)));
            row.AddChild(h);
            col.AddChild(row);
            col.AddChild(ModalFrame.Divider());
        }
        return col;
    }

    private static Button RowButton(string icon, string tip, Action press)
    {
        var b = new TipButton { TooltipText = tip, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(26, 26), SizeFlagsVertical = SizeFlags.ShrinkCenter, Icon = Icons.Get(icon, 14), IconAlignment = HorizontalAlignment.Center, MouseDefaultCursorShape = CursorShape.PointingHand };
        b.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
        var hover = ThemeFactory.Box(UiTokens.SurfaceHover, 0, radius: 3);
        b.AddThemeStyleboxOverride("hover", hover);
        b.AddThemeStyleboxOverride("pressed", hover);
        foreach (var st in new[] { "icon_normal_color", "icon_hover_color", "icon_pressed_color", "icon_focus_color" }) b.AddThemeColorOverride(st, UiTokens.TextLabel);
        b.Pressed += press;
        return b;
    }

    // ---------------- Eventos ----------------

    private static readonly string[][] FeedFilters =
    {
        Array.Empty<string>(),
        new[] { "level_up", "region_discovered", "person_registered" },
        new[] { "session_started", "session_ended", "session_continued", "session_extended", "stayed_up_late", "work_interruption" },
        new[] { "activity_chosen" },
    };

    private Control BuildEvents()
    {
        var filters = new HBoxContainer();
        filters.AddThemeConstantOverride("separation", 4);
        string[] names = { "Tudo", "Progresso", "Sessões", "Decisões" };
        for (int i = 0; i < names.Length; i++)
        {
            int idx = i;
            var b = new Button { Text = names[i], ToggleMode = true, ButtonPressed = i == _feedFilter, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 24) };
            b.AddThemeFontSizeOverride("font_size", 11);
            b.Pressed += () => { _feedFilter = idx; Refresh(true); };
            filters.AddChild(b);
        }
        var list = ModalFrame.Column(0);
        int r = 0;
        foreach (var (e, pid) in _feed())
        {
            if (_feedFilter > 0 && !FeedFilters[_feedFilter].Contains(e.Type)) continue;
            if (r >= 120) break;
            var (text, color) = Describe(e, pid);
            var row = new TipButton { FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 22), TooltipText = $"{text}\nQuando|{PtBr.ShortDate(_ctx.Calendar.ToDateTime(e.Tick))} {_ctx.Clock(e.Tick)}\n~Clique para abrir o dossiê do jogador." };
            row.AddThemeStyleboxOverride("normal", new StyleBoxFlat { BgColor = r++ % 2 == 1 ? UiTokens.RowAlt : new Color(0, 0, 0, 0) });
            row.AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = new Color(UiTokens.Accent, 0.1f), BorderColor = UiTokens.Accent, BorderWidthLeft = 2 });
            var h = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            h.SetAnchorsPreset(LayoutPreset.FullRect);
            h.OffsetLeft = 4;
            h.AddThemeConstantOverride("separation", 6);
            var when = ThemeFactory.Text(_ctx.Clock(e.Tick), UiTokens.FontCaption, UiTokens.TextDisabled, UiFonts.Numbers);
            when.CustomMinimumSize = new Vector2(34, 0);
            when.VerticalAlignment = VerticalAlignment.Center;
            h.AddChild(when);
            h.AddChild(new ColorRect { Color = color, CustomMinimumSize = new Vector2(3, 12), SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore });
            var l = ThemeFactory.Text(text, UiTokens.FontSmall, UiTokens.TextPrimary);
            l.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            l.VerticalAlignment = VerticalAlignment.Center;
            l.ClipText = true;
            h.AddChild(l);
            foreach (var c in h.GetChildren().OfType<Control>()) c.MouseFilter = MouseFilterEnum.Ignore;
            row.AddChild(h);
            int p = pid;
            row.Pressed += () => { if (p > 0) _ctx.OpenPerson(p); };
            list.AddChild(row);
        }
        if (r == 0) list.AddChild(ThemeFactory.Text("Nenhum evento ainda.", UiTokens.FontSmall, UiTokens.TextDisabled));
        return ModalFrame.Column(8, filters, list);
    }

    private string QuestName(string? id) => id != null && _ctx.Content.Quests.TryGetValue(id, out var q) ? q.Name : id ?? "";

    private (string, Color) Describe(SimEvent e, int pid)
    {
        var v = pid > 0 && pid < _ctx.Snapshot().ByPerson.Length ? _ctx.Snapshot().ByPerson[pid] : null;
        string who = v?.Name ?? (pid > 0 ? _ctx.Persons[pid - 1].Name : "");
        switch (e.Type)
        {
            case "person_registered": return ($"{who} criou a conta ({e.Detail})", UiTokens.Accent);
            case "session_started":
                if (e.Causes.Count > 0 && e.Causes[0].Factor == "work_break") return ($"{who} jogou na pausa do trabalho", UiTokens.Success);
                if (e.Causes.Count > 0 && e.Causes[0].Factor == "during_work") return ($"{who} deu uma escapada no expediente", UiTokens.Warning);
                if (e.Causes.Count > 1 && e.Causes[1].Factor == "return") return ($"{who} voltou a jogar", UiTokens.Success);
                return ($"{who} conectou", UiTokens.Success);
            case "session_continued": return ($"{who} continuou jogando ({e.Detail})", UiTokens.Success.Darkened(0.1f));
            case "session_extended": return ($"{who} ficou acordado jogando ({e.Detail})", UiTokens.Warning);
            case "stayed_up_late": return ($"{who} foi dormir {e.Detail} mais tarde por causa do jogo", UiTokens.Warning);
            case "work_interruption": return ($"{who} saiu por um chamado do trabalho", UiTokens.Warning);
            case "session_ended": return ($"{who} desconectou", UiTokens.TextDisabled);
            case "level_up": return ($"{who} subiu para o {e.Detail}", UiTokens.Accent);
            case "region_discovered": return ($"{who} descobriu {_ctx.ZoneName(e.Detail ?? "")}", UiTokens.Info);
            case "activity_chosen":
                var p = (e.Detail ?? "/").Split('/');
                return ($"{who} → {_ctx.ActivityName(p[0], p[1])} · {_ctx.PlaceName(p[0], p[1])}", UiTokens.Info.Darkened(0.2f));
            case "quest_accepted": return ($"{who} aceitou {QuestName(e.Detail)}", UiTokens.Accent.Darkened(0.1f));
            case "quest_step_completed": return ($"{who} cumpriu etapa de {QuestName((e.Detail ?? "").Split('#')[0])}", UiTokens.Info.Darkened(0.2f));
            case "quest_completed": return ($"{who} entregou {QuestName(e.Detail)}", UiTokens.Success);
            case "goal_set": return ($"{who} adotou uma meta", UiTokens.Accent.Darkened(0.1f));
            case "goal_completed": return ($"{who} concluiu uma meta", UiTokens.Success);
            case "goal_abandoned": return ($"{who} desistiu de uma meta", UiTokens.TextSecondary);
            case "player_quit":
            {
                string why = e.Causes.Count > 1 ? PlayerModal.DriverPt(e.Causes[1].Factor).ToLowerInvariant() : "";
                return ($"{who} desistiu do jogo{(why.Length > 0 ? " — " + why : "")}", UiTokens.Danger);
            }
            case "player_returned": return ($"{who} voltou ao jogo ({e.Detail})", UiTokens.Success);
            case "player_inactive": return ($"{who} ficou inativo", UiTokens.Warning);
            case "session_great": return ($"{who} teve uma sessão ótima", UiTokens.Success.Darkened(0.1f));
            case "session_bad": return ($"{who} teve uma sessão ruim", UiTokens.Danger.Darkened(0.1f));
            case "trait_changed":
            {
                var parts = (e.Detail ?? "").Split(':');
                if (parts.Length != 3) return ($"{who} mudou um atributo", UiTokens.TextSecondary);
                bool up = string.CompareOrdinal(parts[2].PadLeft(2), parts[1].PadLeft(2)) > 0;
                return ($"{who}: {AttributeInfo.Name(parts[0])} {(up ? "subiu" : "caiu")} para {parts[2]}", up ? UiTokens.Success : UiTokens.Danger);
            }
            case "server_opened": return ("Servidor aberto ao público", UiTokens.Success);
            case "server_closed": return ("Servidor fechado", UiTokens.Danger);
            default:
            {
                var (text, color) = PlayerModal.Describe(_ctx, e);
                return (who.Length > 0 ? $"{who} · {text}" : text, color);
            }
        }
    }
}

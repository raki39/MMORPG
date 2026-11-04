using System.Globalization;
using Godot;
using Simulation.Core.Content;
using GodotClient.Map;
using GodotClient.Sim;
using GodotClient.Ui;

namespace GodotClient;

/// <summary>
/// HUD do cliente (D-034): barra superior de altura fixa (voltar/avancar, marca, menu com
/// submenus, indicadores, relogio, velocidade, configuracoes), painel lateral recolhivel e os
/// paineis sobre o mapa (camadas, controles, aviso). Tudo no kit visual v3.
/// </summary>
public partial class Main
{
    private bool _sideOpen = true;
    private Button? _sideToggle;
    private Icon? _sideToggleIcon;
    private Control? _kpiRow;
    private MenuDropdown? _dropdown;
    private Button? _dropdownOwner;
    private double _dropdownHover;
    private Button? _hoverMenu;
    private double _hoverMenuTime;

    private int SideWidth => _sideOpen ? UiTokens.SidePanelWidth : 0;

    // ---------- Area util do mapa ----------

    private Rect2 MapArea()
    {
        var size = GetViewport().GetVisibleRect().Size;
        return new Rect2(UiTokens.S16, UiTokens.TopBarHeight + UiTokens.S8, size.X - SideWidth - UiTokens.S16 * 2, size.Y - UiTokens.TopBarHeight - UiTokens.S16 - 40);
    }

    private void FitMap(bool instant)
    {
        var size = GetViewport().GetVisibleRect().Size;
        if (_kpiRow != null) _kpiRow.Visible = size.X >= 1500;
        _serverChip.Visible = size.X >= 1760;
        _map.FitTo(MapArea(), instant);
        PlaceMapHud();
    }

    private Rect2 QuickBounds()
    {
        var vp = GetViewport().GetVisibleRect().Size;
        return new Rect2(12, UiTokens.TopBarHeight + 12, vp.X - SideWidth - 24, vp.Y - UiTokens.TopBarHeight - 24);
    }

    // ---------- Barra superior ----------

    private void BuildTopBar(ContentDatabase content)
    {
        var bar = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop, ClipContents = true };
        bar.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color("#0A0C10"), BorderColor = UiTokens.Divider, BorderWidthBottom = 1,
            ShadowColor = new Color(0, 0, 0, 0.4f), ShadowSize = 10,
            ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 0, ContentMarginBottom = 0,
        });
        bar.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        bar.OffsetBottom = UiTokens.TopBarHeight;
        _ui.AddChild(bar);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 0);
        bar.AddChild(row);

        var nav = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        nav.AddThemeConstantOverride("separation", 4);
        _backButton = IconNav("chevron-left", "Voltar (Alt+← ou botão lateral do mouse)", () => Navigate(-1));
        _forwardButton = IconNav("chevron-right", "Avançar (Alt+→)", () => Navigate(+1));
        nav.AddChild(_backButton);
        nav.AddChild(_forwardButton);
        row.AddChild(nav);
        row.AddChild(Gap(18));

        var brand = new HBoxContainer { TooltipText = content.World.Name, MouseFilter = Control.MouseFilterEnum.Pass, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        brand.AddThemeConstantOverride("separation", 8);
        brand.AddChild(ThemeFactory.Text("◆", 14, UiTokens.Accent));
        brand.AddChild(ThemeFactory.Text("MMORPG TYCOON", 14, UiTokens.AccentSoft, UiFonts.Display));
        row.AddChild(brand);
        row.AddChild(Gap(22));
        row.AddChild(new ColorRect { Color = UiTokens.Divider, CustomMinimumSize = new Vector2(1, 26), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
        row.AddChild(Gap(10));

        // Menu principal com submenus (passe o mouse); o mapa e a unica tela em que o tempo passa.
        var menu = new HBoxContainer();
        menu.AddThemeConstantOverride("separation", 4);
        foreach (var (kind, name, tip, ico) in new[]
        {
            ("map", "Mapa", "Mapa do mundo ao vivo (M)\n~Única tela em que o tempo passa.", "map"),
            ("players", "Jogadores", "Todos os personagens (J)\n~O tempo pausa fora do mapa.", "users"),
            ("zones", "Zonas", "Zonas e áreas do mundo (Z)\n~O tempo pausa fora do mapa.", "castle"),
            ("quests", "Missões", "Catálogo de missões (Q)\n~O tempo pausa fora do mapa.", "scroll-text"),
        })
        {
            var b = MenuButton(name, tip, ico);
            string k = kind;
            b.Pressed += () => { CloseDropdown(); OpenSection(k); };
            b.MouseEntered += () => { _hoverMenu = b; _hoverMenuTime = 0; };
            b.MouseExited += () => { if (_hoverMenu == b) _hoverMenu = null; };
            _menu.Add((kind, b));
            menu.AddChild(b);
        }
        row.AddChild(menu);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        _serverChip = ThemeFactory.Chip("Servidor fechado", UiTokens.TextSecondary, dot: true);
        row.AddChild(_serverChip);
        row.AddChild(Gap(14));

        var kpis = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        kpis.AddThemeConstantOverride("separation", 0);
        Kpi(kpis, "online", "Conectados", "Conectados agora\n~Pessoas com sessão aberta. Variação: última hora.");
        Kpi(kpis, "registered", "Cadastrados", "Contas criadas\n~Registro progressivo durante o lançamento.");
        Kpi(kpis, "level", "Nível médio", "Nível médio dos personagens");
        row.AddChild(kpis);
        _kpiRow = kpis;
        _onlineLabel = new Label();
        _registeredLabel = new Label();
        row.AddChild(Gap(10));
        row.AddChild(new ColorRect { Color = UiTokens.Divider, CustomMinimumSize = new Vector2(1, 26), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
        row.AddChild(Gap(14));

        var clockRow = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        clockRow.AddThemeConstantOverride("separation", 10);
        clockRow.AddChild(new Icon("calendar-days", 18, UiTokens.TextSecondary));
        var clock = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        clock.AddThemeConstantOverride("separation", -2);
        _clockLabel = ThemeFactory.Text("00:00", 17, UiTokens.TextPrimary, UiFonts.Numbers);
        _dateLabel = ThemeFactory.Text("", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium);
        clock.AddChild(_clockLabel);
        clock.AddChild(_dateLabel);
        clockRow.AddChild(clock);
        row.AddChild(clockRow);
        row.AddChild(Gap(16));

        var seg = Segmented(out var segRow);
        seg.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        var group = new ButtonGroup();
        foreach (int s in SimulationHost.Speeds)
        {
            var b = SegButton(s == 0 ? "" : $"{s}×", s == 0 ? "Pausar (Espaço)" : $"Velocidade {s}× (tecla {Array.IndexOf(SimulationHost.Speeds, s)})", group);
            if (s == 0)
            {
                b.Icon = Icons.Get("pause", 14);
                b.IconAlignment = HorizontalAlignment.Center;
                b.AddThemeColorOverride("icon_pressed_color", UiTokens.Background);
                b.AddThemeColorOverride("icon_hover_pressed_color", UiTokens.Background);
            }
            int speed = s;
            b.Pressed += () => SetSpeed(speed);
            _speedButtons.Add(b);
            segRow.AddChild(b);
        }
        _speedSeg = seg;
        row.AddChild(seg);
        _returnButton = ModalFrame.Action("Voltar ao mapa", "Voltar ao mapa (Esc ou M)\n~O tempo volta a correr na velocidade de antes.", () => ShowMap(), primary: true);
        _returnButton.Visible = false;
        _returnButton.Icon = Icons.Get("play", 14);
        _returnButton.CustomMinimumSize = new Vector2(176, 36);
        _returnButton.AddThemeFontSizeOverride("font_size", 13);
        _returnButton.AddThemeColorOverride("icon_normal_color", UiTokens.Background);
        _returnButton.AddThemeColorOverride("icon_hover_color", UiTokens.Background);
        _returnButton.AddThemeConstantOverride("h_separation", 8);
        _returnButton.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        row.AddChild(_returnButton);
        row.AddChild(Gap(10));

        _speedLabel = ThemeFactory.Text("", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers);
        _speedLabel.CustomMinimumSize = new Vector2(80, 0);
        _speedLabel.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        row.AddChild(_speedLabel);
        _fpsLabel = ThemeFactory.Text("", UiTokens.FontCaption, UiTokens.TextDisabled, UiFonts.Numbers);
        _fpsLabel.CustomMinimumSize = new Vector2(48, 0);
        _fpsLabel.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        _fpsLabel.Visible = ClientSettings.ShowFps;
        row.AddChild(_fpsLabel);
        row.AddChild(IconNav("settings", "Configurações\n~Vídeo e gráficos, tela cheia, jogo e atalhos.", () => OpenSection("settings")));
    }

    private static Control Gap(int w) => new Control { CustomMinimumSize = new Vector2(w, 0), MouseFilter = Control.MouseFilterEnum.Ignore };

    private void OpenSection(string kind, int tab = 0)
    {
        switch (kind)
        {
            case "map": ShowMap(); break;
            case "players": OpenPlayersPage(); break;
            case "zones": OpenZonesPage(); break;
            case "quests": OpenQuestsPage(); break;
            case "settings": OpenSettingsPage(); break;
        }
        if (tab > 0 && _modal != null && IsInstanceValid(_modal) && tab < _modal.TabCount) _modal.SelectTab(tab);
    }

    /// <summary>Itens do submenu de cada secao (atalhos para abas e lugares especificos).</summary>
    private List<MenuDropdown.Item> SubItems(string kind)
    {
        var items = new List<MenuDropdown.Item>();
        switch (kind)
        {
            case "map":
                items.Add(new("map", "Mapa ao vivo", "Terreno e jogadores", () => { ShowMap(); SetLayer(MapLayer.Normal); }));
                items.Add(new("users", "Camada de lotação", "Jogadores em relação à capacidade", () => { ShowMap(); SetLayer(MapLayer.Crowding); }));
                items.Add(new("skull", "Camada de perigo", "Risco, PvP e regra de morte", () => { ShowMap(); SetLayer(MapLayer.Danger); }));
                items.Add(new("trending-up", "Camada de nível", "Faixa de nível do conteúdo", () => { ShowMap(); SetLayer(MapLayer.Level); }));
                items.Add(new("maximize", "Enquadrar o mundo", "Tecla Home", () => { ShowMap(); FitMap(false); }));
                break;
            case "players":
                items.Add(new("users", "Todos os jogadores", "Lista completa com busca", () => OpenSection("players", 0)));
                items.Add(new("activity", "Conectados agora", "Quem está jogando", () => OpenSection("players", 1)));
                items.Add(new("star", "Favoritos", "Jogadores que você acompanha", () => OpenSection("players", 2)));
                break;
            case "zones":
                items.Add(new("castle", "Todas as zonas", "Lotação, perigo e regras", () => OpenSection("zones", 0)));
                foreach (var z in _ctx.Content.Regions.Values.Where(z => z.IsCity).OrderBy(z => z.Name, StringComparer.Ordinal))
                {
                    string id = z.Id;
                    items.Add(new("landmark", z.Name, $"Cidade nível {z.CityLevel}", () => OpenZone(id)));
                }
                break;
            case "quests":
                items.Add(new("scroll-text", "Todas as missões", "Catálogo completo", () => OpenSection("quests", 0)));
                items.Add(new("crown", "Principais", "Uma vez por personagem", () => OpenSection("quests", 1)));
                items.Add(new("scroll-text", "Secundárias", "Repetíveis ou únicas", () => OpenSection("quests", 2)));
                items.Add(new("sun", "Diárias", "Renovam todo dia", () => OpenSection("quests", 3)));
                break;
        }
        return items;
    }

    private void OpenDropdown(Button owner)
    {
        if (_dropdownOwner == owner && _dropdown != null && IsInstanceValid(_dropdown)) return;
        CloseDropdown();
        string kind = _menu.First(m => m.Button == owner).Kind;
        _dropdown = new MenuDropdown(SubItems(kind), () => CloseDropdown());
        _dropdownOwner = owner;
        _ui.AddChild(_dropdown);
        var r = owner.GetGlobalRect();
        _dropdown.Position = new Vector2(Mathf.Round(r.Position.X), UiTokens.TopBarHeight - 1);
        _dropdownHover = 0;
    }

    private void CloseDropdown()
    {
        if (_dropdown != null && IsInstanceValid(_dropdown)) _dropdown.QueueFree();
        _dropdown = null;
        _dropdownOwner = null;
    }

    /// <summary>Abre o submenu apos um instante sobre o item; fecha ao sair do item e do submenu.</summary>
    private void UpdateDropdown(double delta)
    {
        var mouse = GetViewport().GetMousePosition();
        if (_hoverMenu != null)
        {
            _hoverMenuTime += delta;
            if (_hoverMenuTime > 0.12) OpenDropdown(_hoverMenu);
        }
        if (_dropdown == null || !IsInstanceValid(_dropdown) || _dropdownOwner == null || _map.IgnoreRealInput) return;
        bool inside = _dropdown.GetGlobalRect().Grow(4).HasPoint(mouse) || _dropdownOwner.GetGlobalRect().HasPoint(mouse);
        _dropdownHover = inside ? 0 : _dropdownHover + delta;
        if (_dropdownHover > 0.22) CloseDropdown();
    }

    private Button MenuButton(string text, string tip, string icon)
    {
        var b = new TipButton { Text = text, TooltipText = tip, ToggleMode = true, FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(0, UiTokens.TopBarHeight - 1), Icon = Icons.Get(icon, 16) };
        b.AddThemeConstantOverride("h_separation", 9);
        b.AddThemeColorOverride("icon_normal_color", UiTokens.TextSecondary);
        b.AddThemeColorOverride("icon_hover_color", UiTokens.TextPrimary);
        b.AddThemeColorOverride("icon_pressed_color", UiTokens.Accent);
        b.AddThemeColorOverride("icon_hover_pressed_color", UiTokens.Accent);
        b.AddThemeFontOverride("font", UiFonts.Medium);
        b.AddThemeFontSizeOverride("font_size", 14);
        StyleBoxFlat S(bool on, bool hover) => new()
        {
            BgColor = on ? new Color(UiTokens.Accent, 0.07f) : hover ? new Color(1, 1, 1, 0.035f) : new Color(0, 0, 0, 0),
            BorderColor = on ? UiTokens.Accent : hover ? UiTokens.BorderStrong : new Color(0, 0, 0, 0), BorderWidthBottom = 2,
            ContentMarginLeft = 18, ContentMarginRight = 34,
        };
        b.AddThemeStyleboxOverride("normal", S(false, false));
        b.AddThemeStyleboxOverride("hover", S(false, true));
        b.AddThemeStyleboxOverride("pressed", S(true, false));
        b.AddThemeStyleboxOverride("hover_pressed", S(true, true));
        b.AddThemeColorOverride("font_color", UiTokens.TextLabel);
        b.AddThemeColorOverride("font_hover_color", UiTokens.TextPrimary);
        b.AddThemeColorOverride("font_pressed_color", UiTokens.TextPrimary);
        b.AddThemeColorOverride("font_hover_pressed_color", UiTokens.TextPrimary);
        // Seta do submenu a direita do texto.
        var chevron = new Icon("chevron-down", 14, UiTokens.TextDisabled);
        chevron.SetAnchorsPreset(Control.LayoutPreset.RightWide);
        chevron.OffsetLeft = -28;
        chevron.OffsetRight = -12;
        b.AddChild(chevron);
        return b;
    }

    private static Button IconNav(string icon, string tip, Action press)
    {
        var b = new TipButton { TooltipText = tip, FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(30, 30), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        b.AddThemeStyleboxOverride("normal", ThemeFactory.Box(UiTokens.SurfaceRaised, 4, border: UiTokens.Border, radius: 4));
        b.AddThemeStyleboxOverride("hover", ThemeFactory.Box(UiTokens.SurfaceHover, 4, border: UiTokens.BorderStrong, radius: 4));
        b.AddThemeStyleboxOverride("pressed", ThemeFactory.Box(UiTokens.SurfaceHover, 4, border: UiTokens.Accent, radius: 4));
        b.AddThemeStyleboxOverride("disabled", ThemeFactory.Box(new Color(0, 0, 0, 0), 4, border: new Color(UiTokens.Border, 0.6f), radius: 4));
        var ic = new Icon(icon, 16, UiTokens.TextLabel);
        ic.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        b.AddChild(ic);
        b.Pressed += press;
        // Icone apagado quando desabilitado.
        b.Draw += () => ic.SelfModulate = b.Disabled ? UiTokens.TextDisabled : UiTokens.TextLabel;
        return b;
    }

    /// <summary>Indicador da barra superior: valor, variacao e rotulo (sem mini grafico).</summary>
    private void Kpi(HBoxContainer row, string key, string label, string tooltip)
    {
        var cell = new TipPanel { TooltipText = tooltip, MouseFilter = Control.MouseFilterEnum.Stop };
        cell.AddThemeStyleboxOverride("panel", new StyleBoxEmpty { ContentMarginLeft = 14, ContentMarginRight = 14 });
        var col = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        col.AddThemeConstantOverride("separation", -1);
        var valRow = new HBoxContainer();
        valRow.AddThemeConstantOverride("separation", 6);
        var value = ThemeFactory.Text("0", 16, UiTokens.TextPrimary, UiFonts.Numbers);
        var delta = ThemeFactory.Text("", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers);
        delta.VerticalAlignment = VerticalAlignment.Center;
        valRow.AddChild(value);
        valRow.AddChild(delta);
        col.AddChild(valRow);
        col.AddChild(ThemeFactory.Text(label, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium));
        cell.AddChild(col);
        if (row.GetChildCount() > 0) row.AddChild(new ColorRect { Color = UiTokens.Divider, CustomMinimumSize = new Vector2(1, 24), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
        row.AddChild(cell);
        _kpis.Add((key, value, delta));
    }

    private void UpdateKpis(SimSnapshot snap)
    {
        foreach (var (key, value, delta) in _kpis)
        {
            List<float> series = key switch { "online" => _metrics.Online, "registered" => _metrics.Registered, _ => _metrics.AvgLevel };
            float current = key switch { "online" => snap.Online, "registered" => snap.Registered, _ => series.Count > 0 ? series[^1] : 0 };
            value.Text = key == "level" ? PtBr.Decimal(current, 1) : PtBr.Int((long)current);
            float d = _metrics.DeltaOf(series);
            delta.Text = series.Count > 6 ? (d > 0 ? $"▲{PtBr.Decimal(d, key == "level" ? 1 : 0)}" : d < 0 ? $"▼{PtBr.Decimal(-d, key == "level" ? 1 : 0)}" : "") : "";
            delta.AddThemeColorOverride("font_color", d > 0 ? UiTokens.Success : d < 0 ? UiTokens.Danger : UiTokens.TextDisabled);
        }
    }

    private static PanelContainer Segmented(out HBoxContainer row)
    {
        var seg = new PanelContainer();
        var box = ThemeFactory.Box(new Color("#07090C"), 3, border: UiTokens.Border, radius: 6);
        box.ContentMarginTop = box.ContentMarginBottom = 3;
        seg.AddThemeStyleboxOverride("panel", box);
        row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 2);
        seg.AddChild(row);
        return seg;
    }

    private static Button SegButton(string text, string tip, ButtonGroup group)
    {
        var b = new TipButton { Text = text, ToggleMode = true, ButtonGroup = group, CustomMinimumSize = new Vector2(42, 30), FocusMode = Control.FocusModeEnum.None, TooltipText = tip };
        b.AddThemeFontOverride("font", UiFonts.Numbers);
        b.AddThemeFontSizeOverride("font_size", 13);
        b.AddThemeStyleboxOverride("normal", ThemeFactory.Box(new Color(0, 0, 0, 0), 10, radius: 4));
        b.AddThemeStyleboxOverride("hover", ThemeFactory.Box(UiTokens.SurfaceHover, 10, radius: 4));
        b.AddThemeStyleboxOverride("pressed", ThemeFactory.Box(UiTokens.Accent, 10, radius: 4));
        b.AddThemeStyleboxOverride("hover_pressed", ThemeFactory.Box(UiTokens.AccentSoft, 10, radius: 4));
        b.AddThemeColorOverride("font_color", UiTokens.TextLabel);
        b.AddThemeColorOverride("font_pressed_color", UiTokens.Background);
        b.AddThemeColorOverride("font_hover_pressed_color", UiTokens.Background);
        b.AddThemeColorOverride("icon_normal_color", UiTokens.TextLabel);
        return b;
    }

    private void SetSpeed(int speed)
    {
        if (_host == null) return;
        _host.Speed = speed;
        if (speed > 0) _lastNonZeroSpeed = speed;
        SyncSpeedButtons();
    }

    private void SyncSpeedButtons()
    {
        if (_host == null) return;
        for (int i = 0; i < _speedButtons.Count; i++)
            _speedButtons[i].SetPressedNoSignal(SimulationHost.Speeds[i] == _host.Speed);
    }

    // ---------- Painel lateral (recolhivel) ----------

    private void BuildSidePanel()
    {
        _sideOpen = ClientSettings.SidePanelOpen;
        _side = new SidePanel(_ctx, _map, _metrics, () => _feed);
        _side.SetAnchorsPreset(Control.LayoutPreset.RightWide);
        _side.OffsetLeft = _sideOpen ? -UiTokens.SidePanelWidth : 0;
        _side.OffsetRight = _sideOpen ? 0 : UiTokens.SidePanelWidth;
        _side.OffsetTop = UiTokens.TopBarHeight;
        _side.Visible = _sideOpen;
        _ui.AddChild(_side);
        _side.Refresh(true);

        _sideToggle = new TipButton { FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(34, 34), TooltipText = "Recolher ou abrir o painel lateral (P)" };
        _sideToggle.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
        var th = ThemeFactory.Box(UiTokens.SurfaceHover, 0, radius: 4);
        _sideToggle.AddThemeStyleboxOverride("hover", th);
        _sideToggle.AddThemeStyleboxOverride("pressed", th);
        _sideToggleIcon = new Icon(_sideOpen ? "panel-right-close" : "panel-right-open", 18, UiTokens.TextLabel);
        _sideToggleIcon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _sideToggle.AddChild(_sideToggleIcon);
        _sideToggle.Pressed += ToggleSidePanel;
        PlaceMapHud();
    }

    private void ToggleSidePanel()
    {
        if (_side == null) return;
        var oldArea = MapArea();
        _sideOpen = !_sideOpen;
        ClientSettings.SidePanelOpen = _sideOpen;
        if (!_args.ContainsKey("screenshot") && !_args.ContainsKey("measure-seconds")) ClientSettings.Save();
        _sideToggleIcon?.QueueFree();
        _sideToggleIcon = new Icon(_sideOpen ? "panel-right-close" : "panel-right-open", 18, UiTokens.TextLabel);
        _sideToggleIcon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        _sideToggle!.AddChild(_sideToggleIcon);
        float w = UiTokens.SidePanelWidth;
        if (_sideOpen) _side.Visible = true;
        var tw = CreateTween().SetParallel().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        tw.TweenProperty(_side, "offset_left", _sideOpen ? -w : 0f, 0.18);
        tw.TweenProperty(_side, "offset_right", _sideOpen ? 0f : w, 0.18);
        if (!_sideOpen) tw.Chain().TweenCallback(Callable.From(() => { if (!_sideOpen && _side != null) _side.Visible = false; }));
        _map.ShiftViewArea(oldArea, MapArea());
        PlaceMapHud();
    }

    /// <summary>Reposiciona os paineis do mapa conforme a largura do painel lateral.</summary>
    private void PlaceMapHud()
    {
        if (_mapControls != null) _mapControls.OffsetRight = -(SideWidth + 12);
        if (_emptyBanner != null)
        {
            _emptyBanner.OffsetLeft = -(SideWidth / 2);
            _emptyBanner.OffsetRight = -(SideWidth / 2);
        }
    }

    /// <summary>Caixa dos paineis do mapa: fundo dos cards, borda fina, sombra.</summary>
    private static StyleBoxFlat HudBox(int padding, Color? border = null, Color? bg = null)
    {
        var sb = new StyleBoxFlat
        {
            BgColor = bg ?? new Color(UiTokens.CardTop, 0.96f), BorderColor = border ?? UiTokens.Border,
            ShadowColor = new Color(0, 0, 0, 0.45f), ShadowSize = 8, ShadowOffset = new Vector2(0, 3),
            ContentMarginLeft = padding, ContentMarginRight = padding, ContentMarginTop = padding, ContentMarginBottom = padding,
            AntiAliasing = true,
        };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(5);
        return sb;
    }

    // ---------- Paineis do mapa ----------

    private static readonly (MapLayer Layer, string Name, string Tip, string Icon)[] Layers =
    {
        (MapLayer.Normal, "Mapa", "Terreno e biomas", "map"),
        (MapLayer.Crowding, "Lotação", "Jogadores presentes em relação à capacidade da zona", "users"),
        (MapLayer.Danger, "Perigo", "Risco das atividades, PvP e regra de morte", "skull"),
        (MapLayer.Level, "Nível", "Faixa de nível do conteúdo", "trending-up"),
    };

    private void BuildLayerPanel()
    {
        var panel = new CardPanel { MouseFilter = Control.MouseFilterEnum.Stop };
        panel.AddThemeStyleboxOverride("panel", new StyleBoxEmpty { ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 10, ContentMarginBottom = 12 });
        panel.Position = new Vector2(UiTokens.S16, UiTokens.TopBarHeight + 12);
        _ui.AddChild(panel);
        _layerPanel = panel;
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 10);
        panel.AddChild(col);
        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", 8);
        head.AddChild(new Icon("layers", 16, UiTokens.Accent));
        head.AddChild(ThemeFactory.Text("Camadas do mapa", UiTokens.FontBody, UiTokens.Accent, UiFonts.SemiBold));
        col.AddChild(head);
        var seg = Segmented(out var row);
        var group = new ButtonGroup();
        for (int i = 0; i < Layers.Length; i++)
        {
            var (l, name, tip, ico) = Layers[i];
            var b = SegButton(name, tip, group);
            b.CustomMinimumSize = new Vector2(84, 30);
            b.Icon = Icons.Get(ico, 14);
            b.AddThemeConstantOverride("h_separation", 6);
            b.AddThemeFontOverride("font", UiFonts.Medium);
            b.AddThemeColorOverride("icon_pressed_color", UiTokens.Background);
            b.AddThemeColorOverride("icon_hover_pressed_color", UiTokens.Background);
            b.Pressed += () => SetLayer(l);
            _layerButtons.Add(b);
            row.AddChild(b);
        }
        col.AddChild(seg);
        _layerLegend = new VBoxContainer();
        _layerLegend.AddThemeConstantOverride("separation", 4);
        col.AddChild(_layerLegend);
    }

    private void SetLayer(MapLayer layer)
    {
        _map.Layer = layer;
        for (int i = 0; i < Layers.Length; i++) _layerButtons[i].SetPressedNoSignal(Layers[i].Layer == layer);
        foreach (var c in _layerLegend.GetChildren()) c.QueueFree();
        switch (layer)
        {
            case MapLayer.Crowding: _layerLegend.AddChild(GradientLegend(WorldMapView.Ramp(0), WorldMapView.Ramp(0.5f), WorldMapView.Ramp(1), "vazia", "25%+ da capacidade")); break;
            case MapLayer.Danger: _layerLegend.AddChild(GradientLegend(WorldMapView.Ramp(0), WorldMapView.Ramp(0.5f), WorldMapView.Ramp(1), "segura", "PvP e alto risco")); break;
            case MapLayer.Level: _layerLegend.AddChild(GradientLegend(new Color("#3E6FB0"), new Color("#6C5FBB"), new Color("#9B4FC6"), "nível 1", $"nível {_host?.Content.Progression.LevelCap ?? 30}")); break;
            default:
                _layerLegend.AddChild(ThemeFactory.Text("Estradas ligam as saídas ao centro de cada zona.\nTrilhas tracejadas levam às atividades.", UiTokens.FontCaption, UiTokens.TextSecondary));
                break;
        }
    }

    private static Control GradientLegend(Color a, Color b, Color c, string left, string right)
    {
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 3);
        var tex = new GradientTexture2D { Gradient = new Gradient { Colors = new[] { a, b, c }, Offsets = new[] { 0f, 0.5f, 1f } }, Width = 256, Height = 6 };
        col.AddChild(new TextureRect { Texture = tex, CustomMinimumSize = new Vector2(0, 6), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale });
        var labels = new HBoxContainer();
        labels.AddChild(ThemeFactory.Text(left, UiTokens.FontCaption, UiTokens.TextSecondary));
        labels.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        labels.AddChild(ThemeFactory.Text(right, UiTokens.FontCaption, UiTokens.TextSecondary));
        col.AddChild(labels);
        return col;
    }

    private void BuildMapControls()
    {
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop };
        panel.AddThemeStyleboxOverride("panel", HudBox(4));
        panel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        panel.GrowHorizontal = Control.GrowDirection.Begin;
        panel.GrowVertical = Control.GrowDirection.Begin;
        panel.OffsetBottom = -UiTokens.S12;
        _ui.AddChild(panel);
        _mapControls = panel;
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 2);
        panel.AddChild(col);
        foreach (var (icon, tip, action) in new (string, string, Action)[]
        {
            ("plus", "Aproximar (roda do mouse ou +)", () => _map.ZoomBy(1.3f)),
            ("minus", "Afastar (roda do mouse ou −)", () => _map.ZoomBy(1f / 1.3f)),
            ("maximize", "Enquadrar o mundo (Home)", () => FitMap(false)),
        })
        {
            var b = new TipButton { TooltipText = tip, CustomMinimumSize = new Vector2(34, 34), FocusMode = Control.FocusModeEnum.None };
            b.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
            var hover = ThemeFactory.Box(UiTokens.SurfaceHover, 0, radius: 4);
            b.AddThemeStyleboxOverride("hover", hover);
            b.AddThemeStyleboxOverride("pressed", hover);
            var ic = new Icon(icon, 18, UiTokens.TextLabel);
            ic.SetAnchorsPreset(Control.LayoutPreset.FullRect);
            b.AddChild(ic);
            b.Pressed += action;
            col.AddChild(b);
        }
        // Recolher/abrir o painel lateral, separado dos controles de zoom.
        if (_sideToggle != null)
        {
            col.AddChild(new ColorRect { Color = UiTokens.Divider, CustomMinimumSize = new Vector2(0, 1) });
            col.AddChild(_sideToggle);
        }
        PlaceMapHud();
    }

    /// <summary>Aviso no mapa quando ninguem esta conectado (e nota de inicio de campanha).</summary>
    private void BuildEmptyBanner()
    {
        _emptyBanner = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop };
        var sb = HudBox(12);
        sb.ContentMarginTop = sb.ContentMarginBottom = 9;
        sb.BorderWidthLeft = 3;
        sb.BorderColor = UiTokens.Accent;
        _emptyBanner.AddThemeStyleboxOverride("panel", sb);
        _emptyBanner.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        _emptyBanner.GrowHorizontal = Control.GrowDirection.Both;
        _emptyBanner.GrowVertical = Control.GrowDirection.Begin;
        _emptyBanner.OffsetBottom = -UiTokens.S16;
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        row.AddChild(new Icon("info", 18, UiTokens.AccentSoft));
        _emptyText = ThemeFactory.Text("", UiTokens.FontBody, UiTokens.TextPrimary);
        _emptyText.VerticalAlignment = VerticalAlignment.Center;
        row.AddChild(_emptyText);
        _skipButton = ModalFrame.Action("Avançar até o próximo login", "Avançar o tempo\n~Processa a simulação minuto a minuto até alguém conectar (no máximo 1 dia). Nada é pulado.", () => _host?.SkipToNextLogin(), primary: true);
        row.AddChild(_skipButton);
        var close = ModalFrame.Action("OK", "Fechar este aviso", () => { _startNote = ""; if (_emptyBanner != null) _emptyBanner.Visible = false; });
        row.AddChild(close);
        _emptyBanner.AddChild(row);
        _ui.AddChild(_emptyBanner);
    }

    private void UpdateEmptyBanner(SimSnapshot snap)
    {
        if (_emptyBanner == null || _emptyText == null || _skipButton == null) return;
        bool empty = snap.Online == 0 && snap.ServerOpen;
        bool skipping = _host?.Skipping ?? false;
        if (empty)
        {
            var now = _host!.Calendar.ToDateTime(snap.Tick);
            _emptyText.Text = skipping
                ? "Avançando o tempo até alguém conectar…"
                : $"Ninguém conectado às {now.Hour:00}:{now.Minute:00}. Os jogadores entram pela rotina deles (a maioria entre 19h e 22h).";
            _skipButton.Visible = !skipping;
            _emptyBanner.Visible = true;
        }
        else if (_startNote.Length > 0)
        {
            _emptyText.Text = _startNote;
            _skipButton.Visible = false;
            _emptyBanner.Visible = true;
        }
        else _emptyBanner.Visible = false;
    }
}

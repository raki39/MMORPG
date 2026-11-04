using System.Globalization;
using Godot;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Events;
using Simulation.Infrastructure.Content;
using GodotClient.Map;
using GodotClient.Sim;
using GodotClient.Ui;

namespace GodotClient;

/// <summary>
/// Raiz do cliente: mapa de zonas em tela cheia, barra superior, camadas, legenda e
/// fichas (modais) de jogadores e zonas. Toda informacao vem do Core via snapshot/eventos.
///
/// Argumentos (depois de "--"):
///   --world=PASTA  --seed=N  --population=N  --speed=N  --layer=normal|lotacao|perigo|nivel
///   --hour=H | --sim-minutes=N    avanca a simulacao antes de comecar (capturas)
///   --open-person=ID | --open-zone=ID | --open-cluster   abre uma ficha ao iniciar
///   --tab=N               aba inicial da ficha aberta
///   --screenshot=ARQ --frames=N    captura e encerra (--zoom=F, --focus=ZONA, --mouse=XxY)
///   --measure-seconds=S   imprime FPS medio e encerra
/// </summary>
public partial class Main : Node
{
    private void ApplyStartZoom(float factor) => _map.ZoomBy(factor);

    // ---------- Historico de fichas (voltar / avancar) ----------
    private readonly List<(string Kind, string Id, int Tab)> _navHistory = new();
    private int _navAt = -1;
    private bool _navigating;

    private void PushHistory(string kind, string id)
    {
        if (_navigating) return;
        if (_navAt >= 0 && _navHistory[_navAt].Kind == kind && _navHistory[_navAt].Id == id) return;
        if (_navAt >= 0 && _modal != null && IsInstanceValid(_modal))
            _navHistory[_navAt] = (_navHistory[_navAt].Kind, _navHistory[_navAt].Id, _modal.CurrentTab);
        if (_navAt >= 0 && _navHistory[_navAt].Kind == kind && _navHistory[_navAt].Id == id) return;
        if (_navAt + 1 < _navHistory.Count) _navHistory.RemoveRange(_navAt + 1, _navHistory.Count - _navAt - 1);
        _navHistory.Add((kind, id, 0));
        if (_navHistory.Count > 60) _navHistory.RemoveAt(0);
        _navAt = _navHistory.Count - 1;
    }

    private void Navigate(int delta)
    {
        int to = _navAt + delta;
        if (to < 0 || to >= _navHistory.Count) return;
        if (_modal != null && IsInstanceValid(_modal))
            _navHistory[_navAt] = (_navHistory[_navAt].Kind, _navHistory[_navAt].Id, _modal.CurrentTab);
        _navigating = true;
        _navAt = to;
        var (kind, id, tab) = _navHistory[to];
        switch (kind)
        {
            case "map": ShowMap(); break;
            case "players": OpenPlayersPage(); break;
            case "zones": OpenZonesPage(); break;
            case "quests": OpenQuestsPage(); break;
            case "settings": OpenSettingsPage(); break;
            case "person": OpenPerson(int.Parse(id, CultureInfo.InvariantCulture)); break;
            case "zone": OpenZone(id); break;
            case "area": { var p = id.Split('#'); OpenArea(p[0], p[1]); break; }
            case "quest": OpenQuest(id); break;
        }
        _navigating = false;
        if (_modal != null && IsInstanceValid(_modal) && tab > 0 && tab < _modal.TabCount) _modal.SelectTab(tab);
        SyncNav();
    }
    private Vector2? _fakeMouse;

    private readonly Dictionary<string, string> _args = new(StringComparer.Ordinal);
    private SimulationHost? _host;
    private ClientContext _ctx = null!;
    private WorldMapView _map = null!;
    private Control _ui = null!;
    private Label _clockLabel = null!, _dateLabel = null!, _speedLabel = null!, _fpsLabel = null!, _onlineLabel = null!, _registeredLabel = null!;
    private Control? _kpiRow;
    private PanelContainer _serverChip = null!;
    private VBoxContainer _layerLegend = null!;
    private readonly List<Button> _speedButtons = new();
    private readonly List<Button> _layerButtons = new();
    private int _lastNonZeroSpeed = 1;
    private double _uiTimer, _modalTimer;
    private int _frames;
    private double _measureElapsed;
    private readonly List<double> _frameMs = new();
    private double _gpuMs, _renderCpuMs, _drawMs, _overlayMs, _mapProcessMs;
    private int _measureFrames;
    private bool _serverOpenShown;
    private ModalFrame? _modal;
    private ClusterPopover? _popover;
    private QuickCard? _quick;
    private ColorRect _terrain = null!;
    private Control? _layerPanel, _mapControls;
    private Control _speedSeg = null!;
    private Button _backButton = null!, _forwardButton = null!, _returnButton = null!;
    private readonly List<(string Kind, Button Button)> _menu = new();
    private int _speedBeforePage = 1;
    private bool _pausedByPage;
    private bool OnMap => _modal == null || !IsInstanceValid(_modal);
    private readonly Dictionary<int, List<SimEvent>> _history = new();
    private readonly Dictionary<int, int> _characterToPerson = new();
    private readonly MetricsHistory _metrics = new();
    private string _startNote = "";
    private PanelContainer? _emptyBanner;
    private Label? _emptyText;
    private Button? _skipButton;
    private readonly List<(SimEvent Event, int PersonId)> _feed = new();
    private SidePanel? _side;
    private readonly List<(string Key, Label Value, Label Delta, Sparkline Spark)> _kpis = new();

    public override void _Ready()
    {
        foreach (var a in OS.GetCmdlineUserArgs())
        {
            var kv = a.TrimStart('-').Split('=', 2);
            _args[kv[0]] = kv.Length > 1 ? kv[1] : "true";
        }

        ClientSettings.Load();
        ClientSettings.ApplyVideo(GetViewport(), _args.ContainsKey("screenshot") || _args.ContainsKey("measure-seconds"));
        var terrain = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore, Color = UiTokens.Background };
        _terrain = terrain;
        terrain.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(terrain);
        _map = new WorldMapView();
        AddChild(_map);
        _ui = new Control { Theme = ThemeFactory.Build(), MouseFilter = Control.MouseFilterEnum.Ignore };
        _ui.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        var layer = new CanvasLayer { Layer = 1 };
        layer.AddChild(_ui);
        AddChild(layer);

        string worldFolder = ResolveWorldFolder();
        var load = ContentLoader.LoadFolder(worldFolder);
        if (!load.Success)
        {
            ShowError($"Conteúdo inválido em {worldFolder}", load.Errors.Select(e => e.ToString()).Take(12));
            return;
        }
        var content = load.Database!;
        ulong seed = ulong.Parse(Arg("seed", "42"), CultureInfo.InvariantCulture);
        int population = int.Parse(Arg("population", "100"), CultureInfo.InvariantCulture);
        _host = new SimulationHost(content, seed, population);

        _ctx = new ClientContext
        {
            Content = content,
            Calendar = _host.Calendar,
            Layout = _host.Layout,
            Persons = _host.Persons,
            Snapshot = () => _host.Snapshot,
            Metrics = _metrics,
            PersonHistory = id => _history.TryGetValue(id, out var l) ? l : (IReadOnlyList<SimEvent>)Array.Empty<SimEvent>(),
            FocusPerson = (id, follow) => { _map.SelectPerson(id); _map.FocusPerson(id, follow); },
            FocusZone = id => { _map.SelectRegion(id); _map.FocusRegion(id); },
            OpenPerson = id => { if (OnMap) PeekPerson(id); else OpenPerson(id); },
            OpenZone = id => { if (OnMap) PeekZone(id); else OpenZone(id); },
            OpenArea = (z, a) => { if (OnMap) PeekArea(z, a); else OpenArea(z, a); },
            OpenQuest = OpenQuest,
            WatchPerson = id => _host?.WatchPerson(id),
            FocusArea = (z, a) => { _map.SelectArea(z + "#" + a); _map.FocusArea(z, a); },
        };

        _map.Setup(content, _host.Layout, _host.Persons, terrain);
        _map.RegionSelected += id => { if (id.Length > 0) PeekZone(id); else CloseOverlays(); };
        _map.PersonSelected += PeekPerson;
        _map.AreaSelected += PeekArea;
        _map.ClusterSelected += OpenCluster;

        BuildTopBar(content);
        BuildLayerPanel();
        BuildSidePanel();
        BuildEmptyBanner();
        BuildMapControls();

        _host.Submit(new SetServerOpenCommand(true));
        long preRoll = 0;
        if (_args.TryGetValue("hour", out var hour))
            preRoll = (long)(double.Parse(hour, CultureInfo.InvariantCulture) * Simulation.Core.Time.SimTime.TicksPerHour);
        if (_args.TryGetValue("sim-minutes", out var mins))
            preRoll = long.Parse(mins, CultureInfo.InvariantCulture) * Simulation.Core.Time.SimTime.TicksPerMinute;
        // Por padrao abre com a campanha em andamento (6o dia, sabado 19h): os jogadores
        // seguem a propria rotina e, no dia do lancamento a meia-noite, ainda nao ha ninguem.
        if (preRoll == 0 && !_args.ContainsKey("from-launch"))
        {
            preRoll = (5L * 24 * 60 + 19 * 60) * Simulation.Core.Time.SimTime.TicksPerMinute;
            _startNote = "Campanha em andamento: 6º dia após o lançamento. Os jogadores seguem a própria rotina (a maioria joga à noite).";
        }
        if (preRoll > 0) _host.AdvanceBeforeStart(preRoll);
        if (!ClientSettings.CampaignNote) _startNote = "";
        _host.Speed = int.Parse(Arg("speed", ClientSettings.StartSpeed.ToString(CultureInfo.InvariantCulture)), CultureInfo.InvariantCulture);
        _host.Start();
        CollectEvents();
        PushHistory("map", "");

        SyncSpeedButtons();
        SyncNav();
        GetViewport().SizeChanged += () => FitMap(false);
        FitMap(true);
        SetLayer(Arg("layer", "normal") switch { "lotacao" => MapLayer.Crowding, "perigo" => MapLayer.Danger, "nivel" => MapLayer.Level, _ => MapLayer.Normal });
        CallDeferred(nameof(OpenFromArgs));
    }

    private void OpenFromArgs()
    {
        _map.SetSnapshot(_host!.Snapshot);
        _map._Process(0);
        if (_args.TryGetValue("open-person", out var p))
        {
            int id = p == "auto"
                ? Enumerable.Range(1, _host.Snapshot.ByPerson.Length - 1).FirstOrDefault(i => _host.Snapshot.ByPerson[i] is { Status: Simulation.Core.Population.CharacterStatus.Doing } v && v.LastDecision != null)
                : int.TryParse(p, out var n) ? n : 0;
            if (id > 0) OpenPerson(id);
        }
        if (_args.TryGetValue("focus", out var fz) && _ctx.Content.Regions.ContainsKey(fz)) _map.FocusRegion(fz);
        // Capturas: ignoram o mouse real; --mouse=X,Y simula o mouse na tela (hover).
        if (_args.ContainsKey("screenshot") || _args.ContainsKey("measure-seconds")) _map.IgnoreRealInput = true;
        if (_args.ContainsKey("measure-redraw")) _map.ForceRedrawEveryFrame = true;
        if (_args.ContainsKey("measure-seconds"))
        {
            // Medicao: sem limite do vsync, com tempo de renderizacao (CPU e GPU) por quadro.
            DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
            Engine.MaxFps = 0;
            RenderingServer.ViewportSetMeasureRenderTime(GetViewport().GetViewportRid(), true);
        }
        if (_args.TryGetValue("mouse", out var ms) && ms.Split(',', 'x') is { Length: 2 } mp
            && float.TryParse(mp[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var mx)
            && float.TryParse(mp[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var my))
            _fakeMouse = new Vector2(mx, my);
        // Capturas: --zoom=F aproxima (F > 1) ou afasta (F < 1) a partir do enquadramento inicial.
        if (_args.TryGetValue("zoom", out var zf) && float.TryParse(zf, NumberStyles.Float, CultureInfo.InvariantCulture, out var zoomFactor) && zoomFactor > 0)
            CallDeferred(nameof(ApplyStartZoom), zoomFactor);
        if (_args.TryGetValue("open-zone", out var z) && _ctx.Content.Regions.ContainsKey(z)) OpenZone(z);
        if (_args.TryGetValue("open-area", out var ar) && ar.Split('#') is { Length: 2 } ap) OpenArea(ap[0], ap[1]);
        if (_args.TryGetValue("open-quest", out var oq)) OpenQuest(oq);
        if (_args.TryGetValue("page", out var pg)) { if (pg == "players") OpenPlayersPage(); else if (pg == "zones") OpenZonesPage(); else if (pg == "quests") OpenQuestsPage(); else if (pg == "settings") OpenSettingsPage(); }
        var center = GetViewport().GetVisibleRect().Size / 2f - new Vector2(UiTokens.SidePanelWidth / 2f, 0);
        if (_args.TryGetValue("peek-person", out var pp))
        {
            int id = pp == "auto"
                ? Enumerable.Range(1, _host.Snapshot.ByPerson.Length - 1).FirstOrDefault(i => _host.Snapshot.ByPerson[i] is { Status: Simulation.Core.Population.CharacterStatus.Doing } v && v.LastDecision != null)
                : int.TryParse(pp, out var n) ? n : 0;
            if (id > 0) PeekPerson(id, center);
        }
        if (_args.TryGetValue("peek-zone", out var pz) && _ctx.Content.Regions.ContainsKey(pz)) PeekZone(pz, center);
        if (_args.ContainsKey("open-cluster"))
        {
            var snap = _host.Snapshot;
            var best = Enumerable.Range(1, snap.ByPerson.Length - 1)
                .Where(i => snap.ByPerson[i] is { Status: Simulation.Core.Population.CharacterStatus.Doing } v && v.ActivityId.Length > 0)
                .GroupBy(i => snap.ByPerson[i]!.TargetRegionId + "/" + snap.ByPerson[i]!.ActivityId)
                .OrderByDescending(g => g.Count()).FirstOrDefault();
            if (best != null) OpenCluster(best.Key.Split('/')[0] + "#", GetViewport().GetVisibleRect().Size / 2f);
        }
        if (_modal != null && _args.TryGetValue("tab", out var tab) && int.TryParse(tab, out int ti)) _modal.SelectTab(ti);
    }

    private string Arg(string key, string fallback) => _args.TryGetValue(key, out var v) ? v : fallback;

    private string ResolveWorldFolder()
    {
        if (_args.TryGetValue("world", out var w)) return w;
        string rel = System.IO.Path.Combine("Content", "presets", "inicial");
        string exeDir = System.IO.Path.GetDirectoryName(OS.GetExecutablePath()) ?? "";
        string besideExe = System.IO.Path.Combine(exeDir, rel);
        if (System.IO.Directory.Exists(besideExe)) return besideExe;
        return System.IO.Path.GetFullPath(System.IO.Path.Combine(ProjectSettings.GlobalizePath("res://"), "..", rel));
    }

    private void FitMap(bool instant)
    {
        var size = GetViewport().GetVisibleRect().Size;
        if (_kpiRow != null) _kpiRow.Visible = size.X >= 1560;
        _serverChip.Visible = size.X >= 1760;
        var area = new Rect2(UiTokens.S16, UiTokens.TopBarHeight + UiTokens.S8, size.X - UiTokens.SidePanelWidth - UiTokens.S16 * 2, size.Y - UiTokens.TopBarHeight - UiTokens.S16 - 40);
        _map.FitTo(area, instant);
    }

    // ---------- Telas: mapa (tempo corre) e paginas (tempo pausa), D-034 ----------

    private void CloseOverlays()
    {
        if (_popover != null && IsInstanceValid(_popover)) _popover.QueueFree();
        _popover = null;
        if (_quick != null && IsInstanceValid(_quick)) _quick.QueueFree();
        _quick = null;
    }

    /// <summary>Abre uma pagina: esconde o mapa, pausa o tempo (lembra a velocidade) e registra no historico.</summary>
    private void ShowPage(string kind, string id, Func<ModalFrame> build)
    {
        if (_host == null) return;
        PushHistory(kind, id);
        CloseOverlays();
        bool fromMap = OnMap;
        if (!fromMap) _modal!.QueueFree();
        _host.WatchPerson(0);
        if (fromMap)
        {
            _speedBeforePage = _host.Speed;
            _pausedByPage = ClientSettings.PauseOffMap;
            if (_pausedByPage) _host.Speed = 0;
            SetMapVisible(false);
        }
        var page = build();
        _modal = page;
        page.Closed += () => { if (_modal == page) { _modal = null; ShowMap(); } };
        _ui.AddChild(page);
        SyncNav();
    }

    /// <summary>Volta ao mapa: retoma a velocidade de antes das paginas.</summary>
    private void ShowMap()
    {
        if (_host == null) return;
        bool wasPage = !OnMap;
        if (wasPage) { var m = _modal!; _modal = null; m.QueueFree(); }
        _host.WatchPerson(0);
        if (wasPage || !_map.Visible)
        {
            SetMapVisible(true);
            if (_pausedByPage) SetSpeed(_speedBeforePage);
        }
        PushHistory("map", "");
        SyncNav();
    }

    private void SetMapVisible(bool visible)
    {
        _map.Visible = visible;
        _map.ProcessMode = visible ? ProcessModeEnum.Inherit : ProcessModeEnum.Disabled;
        _terrain.Visible = visible;
        if (_side != null) _side.Visible = visible;
        if (_layerPanel != null) _layerPanel.Visible = visible;
        if (_mapControls != null) _mapControls.Visible = visible;
        if (_emptyBanner != null && !visible) _emptyBanner.Visible = false;
        _speedSeg.Visible = visible;
        _returnButton.Visible = !visible;
    }

    /// <summary>Estado da barra: voltar/avancar e item do menu ativo.</summary>
    private void SyncNav()
    {
        if (_backButton == null) return;
        _backButton.Disabled = _navAt <= 0;
        _forwardButton.Disabled = _navAt < 0 || _navAt >= _navHistory.Count - 1;
        string current = _navAt >= 0 ? _navHistory[_navAt].Kind : "map";
        string section = current switch { "person" => "players", "zone" or "area" => "zones", "quest" => "quests", _ => current };
        foreach (var (kind, b) in _menu) b.SetPressedNoSignal(kind == section);
    }

    private Rect2 QuickBounds()
    {
        var vp = GetViewport().GetVisibleRect().Size;
        return new Rect2(12, UiTokens.TopBarHeight + 12, vp.X - UiTokens.SidePanelWidth - 24, vp.Y - UiTokens.TopBarHeight - 24);
    }

    private Vector2 MouseOr(Vector2? at) => at ?? _fakeMouse ?? GetViewport().GetMousePosition();

    private void ShowQuick(QuickCard card)
    {
        CloseOverlays();
        _quick = card;
        card.Dismissed += () => { _map.SelectPerson(0); _map.SelectRegion(null); _map.SelectArea(null); };
        _ui.AddChild(card);
    }

    private void PeekPerson(int personId) => PeekPerson(personId, null);
    private void PeekPerson(int personId, Vector2? at)
    {
        if (_host == null || personId < 1 || personId > _host.Persons.Count) return;
        _map.SelectPerson(personId);
        _map.SelectRegion(null);
        _map.SelectArea(null);
        ShowQuick(QuickCard.Person(_ctx, personId, MouseOr(at), QuickBounds(), () => OpenPerson(personId)));
    }

    private void PeekZone(string zoneId) => PeekZone(zoneId, null);
    private void PeekZone(string zoneId, Vector2? at)
    {
        if (_host == null || !_ctx.Content.Regions.ContainsKey(zoneId)) return;
        _map.SelectRegion(zoneId);
        _map.SelectArea(null);
        _map.SelectPerson(0);
        ShowQuick(QuickCard.Zone(_ctx, _map, zoneId, MouseOr(at), QuickBounds(), () => OpenZone(zoneId)));
    }

    private void PeekArea(string zoneId, string areaId)
    {
        if (_host == null || !_ctx.Content.Regions.TryGetValue(zoneId, out var zr) || zr.Areas.All(a => a.Id != areaId)) return;
        _map.SelectArea(zoneId + "#" + areaId);
        _map.SelectPerson(0);
        ShowQuick(QuickCard.Area(_ctx, _map, zoneId, areaId, MouseOr(null), QuickBounds(), () => OpenArea(zoneId, areaId)));
    }

    private void OpenPerson(int personId)
    {
        if (_host == null || personId < 1 || personId > _host.Persons.Count) return;
        _map.SelectPerson(personId);
        ShowPage("person", personId.ToString(CultureInfo.InvariantCulture), () => new PlayerModal(_ctx, personId));
    }

    private void OpenZone(string zoneId)
    {
        if (_host == null || !_ctx.Content.Regions.ContainsKey(zoneId)) return;
        _map.SelectRegion(zoneId);
        ShowPage("zone", zoneId, () => new ZoneModal(_ctx, _map, zoneId));
    }

    private void OpenArea(string zoneId, string areaId)
    {
        if (_host == null || !_ctx.Content.Regions.TryGetValue(zoneId, out var zr) || zr.Areas.All(a => a.Id != areaId)) return;
        _map.SelectArea(zoneId + "#" + areaId);
        ShowPage("area", zoneId + "#" + areaId, () => new AreaModal(_ctx, _map, zoneId, areaId));
    }

    private void OpenQuest(string questId)
    {
        if (_host == null || !_ctx.Content.Quests.ContainsKey(questId)) return;
        ShowPage("quest", questId, () => new QuestModal(_ctx, questId));
    }

    private void OpenPlayersPage() => ShowPage("players", "", () => new PlayersPage(_ctx));
    private void OpenZonesPage() => ShowPage("zones", "", () => new ZonesPage(_ctx, _map));
    private void OpenQuestsPage() => ShowPage("quests", "", () => new QuestsPage(_ctx));
    private void OpenSettingsPage() => ShowPage("settings", "", () => new SettingsPage(GetViewport(), () => _fpsLabel.Visible = ClientSettings.ShowFps));

    private void OpenCluster(string placeKey, Vector2 at)
    {
        CloseOverlays();
        var parts = placeKey.Split('#');
        var zone = _ctx.Content.Regions[parts[0]];
        string title = parts[1].Length == 0 ? "Na encruzilhada" : zone.Areas.First(a => a.Id == parts[1]).Name;
        var people = _map.PeopleAt(placeKey);
        _popover = new ClusterPopover(_ctx, title, $"{zone.Name} · {people.Count} jogadores", people, at, GetViewport().GetVisibleRect().Size);
        _ui.AddChild(_popover);
    }

    // ---------- Eventos por pessoa ----------

    private void CollectEvents()
    {
        if (_host == null) return;
        var snap = _host.Snapshot;
        foreach (var v in snap.ByPerson) if (v != null) _characterToPerson[v.CharacterId] = v.PersonId;
        foreach (var e in _host.DrainEvents())
        {
            int pid = 0;
            foreach (var ent in e.Entities)
            {
                pid = ent.Kind == "person" ? (int)ent.Id : ent.Kind == "character" && _characterToPerson.TryGetValue((int)ent.Id, out var p) ? p : 0;
                if (pid != 0) break;
            }
            _feed.Insert(0, (e, pid));
            if (_feed.Count > 400) _feed.RemoveAt(_feed.Count - 1);
            if (pid == 0) continue;
            if (!_history.TryGetValue(pid, out var list)) _history[pid] = list = new List<SimEvent>();
            list.Insert(0, e);
            if (list.Count > 120) list.RemoveAt(list.Count - 1);
        }
    }

    /// <summary>Aviso no mapa quando ninguem esta conectado (e nota de inicio de campanha).</summary>
    private void BuildEmptyBanner()
    {
        _emptyBanner = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop };
        var sb = ThemeFactory.Panel(12);
        sb.BorderWidthLeft = 3;
        sb.BorderColor = UiTokens.Info;
        _emptyBanner.AddThemeStyleboxOverride("panel", sb);
        _emptyBanner.SetAnchorsPreset(Control.LayoutPreset.CenterBottom);
        _emptyBanner.GrowHorizontal = Control.GrowDirection.Both;
        _emptyBanner.GrowVertical = Control.GrowDirection.Begin;
        _emptyBanner.OffsetBottom = -UiTokens.S16;
        _emptyBanner.OffsetLeft = -(UiTokens.SidePanelWidth / 2);
        _emptyBanner.OffsetRight = -(UiTokens.SidePanelWidth / 2);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        _emptyText = ThemeFactory.Text("", UiTokens.FontSmall, UiTokens.TextPrimary);
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

    private void BuildSidePanel()
    {
        _side = new SidePanel(_ctx, _map, _metrics, () => _feed);
        _side.SetAnchorsPreset(Control.LayoutPreset.RightWide);
        _side.OffsetLeft = -UiTokens.SidePanelWidth;
        _side.OffsetTop = UiTokens.TopBarHeight;
        _ui.AddChild(_side);
        _side.Refresh(true);
    }

    /// <summary>Indicador da barra superior: rotulo, valor, variacao e mini grafico, com tooltip.</summary>
    private void Kpi(HBoxContainer row, string key, string label, Color color, string tooltip)
    {
        var cell = new TipPanel { TooltipText = tooltip, MouseFilter = Control.MouseFilterEnum.Stop };
        cell.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), BorderColor = UiTokens.Border, BorderWidthLeft = 1, ContentMarginLeft = 10, ContentMarginRight = 10 });
        var h = new HBoxContainer();
        h.AddThemeConstantOverride("separation", 8);
        var col = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        col.AddThemeConstantOverride("separation", -3);
        col.AddChild(ThemeFactory.Text(label.ToUpperInvariant(), 10, UiTokens.TextSecondary, UiFonts.CondensedSemiBold));
        var valRow = new HBoxContainer();
        valRow.AddThemeConstantOverride("separation", 5);
        var value = ThemeFactory.Text("0", 16, UiTokens.TextPrimary, UiFonts.Numbers);
        var delta = ThemeFactory.Text("", 10, UiTokens.TextSecondary, UiFonts.Numbers);
        delta.VerticalAlignment = VerticalAlignment.Bottom;
        valRow.AddChild(value);
        valRow.AddChild(delta);
        col.AddChild(valRow);
        h.AddChild(col);
        var spark = new Sparkline { CustomMinimumSize = new Vector2(64, 24), LineColor = color, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        h.AddChild(spark);
        cell.AddChild(h);
        row.AddChild(cell);
        _kpis.Add((key, value, delta, spark));
    }

    private void UpdateKpis(SimSnapshot snap)
    {
        foreach (var (key, value, delta, spark) in _kpis)
        {
            List<float> series = key switch { "online" => _metrics.Online, "registered" => _metrics.Registered, _ => _metrics.AvgLevel };
            float current = key switch { "online" => snap.Online, "registered" => snap.Registered, _ => series.Count > 0 ? series[^1] : 0 };
            value.Text = key == "level" ? PtBr.Decimal(current, 1) : PtBr.Int((long)current);
            float d = _metrics.DeltaOf(series);
            delta.Text = series.Count > 6 ? (d > 0 ? $"▲{PtBr.Decimal(d, key == "level" ? 1 : 0)}" : d < 0 ? $"▼{PtBr.Decimal(-d, key == "level" ? 1 : 0)}" : "=") : "";
            delta.AddThemeColorOverride("font_color", d > 0 ? UiTokens.Success : d < 0 ? UiTokens.Danger : UiTokens.TextDisabled);
            spark.Values = series.TakeLast(72).ToList();
            spark.QueueRedraw();
        }
    }

    // ---------- Barra superior ----------

    private void BuildTopBar(ContentDatabase content)
    {
        var bar = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop };
        var sb = ThemeFactory.Box(new Color(UiTokens.Surface, 0.94f), UiTokens.S16, radius: 0);
        sb.BorderColor = UiTokens.Border;
        sb.BorderWidthBottom = 1;
        sb.ShadowColor = new Color(0, 0, 0, 0.35f);
        sb.ShadowSize = 14;
        bar.AddThemeStyleboxOverride("panel", sb);
        bar.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        bar.OffsetBottom = UiTokens.TopBarHeight;
        _ui.AddChild(bar);

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", UiTokens.S16);
        bar.AddChild(row);

        var nav = new HBoxContainer { SizeFlagsVertical = Control.SizeFlags.ShrinkCenter };
        nav.AddThemeConstantOverride("separation", 2);
        _backButton = NavButton("‹", "Voltar (Alt+← ou botão lateral do mouse)", () => Navigate(-1));
        _forwardButton = NavButton("›", "Avançar (Alt+→)", () => Navigate(+1));
        nav.AddChild(_backButton);
        nav.AddChild(_forwardButton);
        row.AddChild(nav);

        var brand = new HBoxContainer { TooltipText = content.World.Name, MouseFilter = Control.MouseFilterEnum.Pass };
        brand.AddThemeConstantOverride("separation", UiTokens.S8);
        brand.AddChild(ThemeFactory.Text("◆", 14, UiTokens.Accent));
        brand.AddChild(ThemeFactory.Text("MMORPG TYCOON", 14, UiTokens.AccentSoft, UiFonts.Display));
        row.AddChild(brand);

        // Menu principal: o mapa e a unica tela em que o tempo passa.
        var menu = new HBoxContainer();
        menu.AddThemeConstantOverride("separation", 0);
        foreach (var (kind, name, tip) in new[]
        {
            ("map", "Mapa", "Mapa do mundo ao vivo (M)\n~Única tela em que o tempo passa."),
            ("players", "Jogadores", "Todos os personagens (J)\n~O tempo pausa fora do mapa."),
            ("zones", "Zonas", "Zonas e áreas do mundo (Z)\n~O tempo pausa fora do mapa."),
            ("quests", "Missões", "Catálogo de missões (Q)\n~O tempo pausa fora do mapa."),
        })
        {
            var b = MenuButton(name, tip);
            string k = kind;
            b.Pressed += () => OpenSection(k);
            _menu.Add((kind, b));
            menu.AddChild(b);
        }
        row.AddChild(menu);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        _serverChip = ThemeFactory.Chip("Servidor fechado", UiTokens.TextSecondary, dot: true);
        row.AddChild(_serverChip);

        var kpis = new HBoxContainer();
        kpis.AddThemeConstantOverride("separation", 0);
        Kpi(kpis, "online", "Conectados", UiTokens.Info, "Conectados agora\n~Pessoas com sessão aberta. Mini gráfico: últimas 12 h (amostras de 10 min).\n~Variação: última hora.");
        Kpi(kpis, "registered", "Cadastrados", UiTokens.Success, "Contas criadas\n~Registro progressivo durante o lançamento.");
        Kpi(kpis, "level", "Nível médio", UiTokens.AccentSoft, "Nível médio dos personagens\n~Curva provisória até o combate (M3).");
        row.AddChild(kpis);
        _kpiRow = kpis;
        _onlineLabel = new Label();
        _registeredLabel = new Label();

        var clock = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        clock.AddThemeConstantOverride("separation", -2);
        _clockLabel = ThemeFactory.Text("00:00", 17, UiTokens.TextPrimary, UiFonts.Numbers);
        _clockLabel.HorizontalAlignment = HorizontalAlignment.Right;
        _dateLabel = ThemeFactory.Text("", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers);
        _dateLabel.HorizontalAlignment = HorizontalAlignment.Right;
        clock.AddChild(_clockLabel);
        clock.AddChild(_dateLabel);
        row.AddChild(clock);

        var seg = Segmented(out var segRow);
        var group = new ButtonGroup();
        foreach (int s in SimulationHost.Speeds)
        {
            var b = SegButton(s == 0 ? "❚❚" : $"{s}×", s == 0 ? "Pausar (Espaço)" : $"Velocidade {s}× (tecla {Array.IndexOf(SimulationHost.Speeds, s)})", group);
            int speed = s;
            b.Pressed += () => SetSpeed(speed);
            _speedButtons.Add(b);
            segRow.AddChild(b);
        }
        _speedSeg = seg;
        row.AddChild(seg);
        _returnButton = ModalFrame.Action("▶  Voltar ao mapa", "Voltar ao mapa (Esc ou M)\n~O tempo volta a correr na velocidade de antes.", () => ShowMap(), primary: true);
        _returnButton.Visible = false;
        _returnButton.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        row.AddChild(_returnButton);

        _speedLabel = ThemeFactory.Text("", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers);
        _speedLabel.CustomMinimumSize = new Vector2(84, 0);
        row.AddChild(_speedLabel);
        _fpsLabel = ThemeFactory.Text("", UiTokens.FontCaption, UiTokens.TextDisabled, UiFonts.Numbers);
        _fpsLabel.CustomMinimumSize = new Vector2(46, 0);
        _fpsLabel.Visible = ClientSettings.ShowFps;
        row.AddChild(_fpsLabel);
        var gear = NavButton("", "Configurações\n~Vídeo e gráficos, tela cheia, jogo e atalhos.", () => OpenSection("settings"));
        gear.CustomMinimumSize = new Vector2(30, 30);
        gear.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        var icon = new GearIcon { MouseFilter = Control.MouseFilterEnum.Ignore };
        icon.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        gear.AddChild(icon);
        row.AddChild(gear);
    }

    private void OpenSection(string kind)
    {
        switch (kind)
        {
            case "map": ShowMap(); break;
            case "players": OpenPlayersPage(); break;
            case "zones": OpenZonesPage(); break;
            case "quests": OpenQuestsPage(); break;
            case "settings": OpenSettingsPage(); break;
        }
    }

    private static Button MenuButton(string text, string tip)
    {
        var b = new TipButton { Text = text, TooltipText = tip, ToggleMode = true, FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(0, UiTokens.TopBarHeight - 2) };
        b.AddThemeFontOverride("font", UiFonts.Medium);
        b.AddThemeFontSizeOverride("font_size", 13);
        StyleBoxFlat S(bool on, bool hover) => new()
        {
            BgColor = hover ? new Color(1, 1, 1, 0.03f) : new Color(0, 0, 0, 0), BorderColor = on ? UiTokens.Accent : new Color(0, 0, 0, 0), BorderWidthBottom = 2,
            ContentMarginLeft = 14, ContentMarginRight = 14,
        };
        b.AddThemeStyleboxOverride("normal", S(false, false));
        b.AddThemeStyleboxOverride("hover", S(false, true));
        b.AddThemeStyleboxOverride("pressed", S(true, false));
        b.AddThemeStyleboxOverride("hover_pressed", S(true, true));
        b.AddThemeColorOverride("font_color", UiTokens.TextSecondary);
        b.AddThemeColorOverride("font_hover_color", UiTokens.TextPrimary);
        b.AddThemeColorOverride("font_pressed_color", UiTokens.TextPrimary);
        b.AddThemeColorOverride("font_hover_pressed_color", UiTokens.TextPrimary);
        return b;
    }

    private static Button NavButton(string glyph, string tip, Action press)
    {
        var b = new TipButton { Text = glyph, TooltipText = tip, FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(28, 28) };
        b.AddThemeFontOverride("font", UiFonts.Bold);
        b.AddThemeFontSizeOverride("font_size", 17);
        b.AddThemeStyleboxOverride("normal", ThemeFactory.Box(UiTokens.SurfaceRaised, 4, border: UiTokens.Border, radius: 3));
        b.AddThemeStyleboxOverride("hover", ThemeFactory.Box(UiTokens.SurfaceHover, 4, border: UiTokens.BorderStrong, radius: 3));
        b.AddThemeStyleboxOverride("pressed", ThemeFactory.Box(UiTokens.SurfaceHover, 4, border: UiTokens.Accent, radius: 3));
        b.AddThemeStyleboxOverride("disabled", ThemeFactory.Box(new Color(0, 0, 0, 0), 4, border: new Color(UiTokens.Border, 0.6f), radius: 3));
        b.AddThemeColorOverride("font_color", UiTokens.TextPrimary);
        b.AddThemeColorOverride("font_disabled_color", UiTokens.TextDisabled);
        b.Pressed += press;
        return b;
    }

    private static PanelContainer Segmented(out HBoxContainer row)
    {
        var seg = new PanelContainer();
        var box = ThemeFactory.Box(new Color(UiTokens.Background, 0.7f), 3, border: UiTokens.Border, radius: 10);
        box.ContentMarginTop = box.ContentMarginBottom = 3;
        seg.AddThemeStyleboxOverride("panel", box);
        row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 2);
        seg.AddChild(row);
        return seg;
    }

    private static Button SegButton(string text, string tip, ButtonGroup group)
    {
        var b = new Button { Text = text, ToggleMode = true, ButtonGroup = group, CustomMinimumSize = new Vector2(44, 30), FocusMode = Control.FocusModeEnum.None, TooltipText = tip };
        b.AddThemeFontOverride("font", UiFonts.Numbers);
        b.AddThemeFontSizeOverride("font_size", 13);
        b.AddThemeStyleboxOverride("normal", ThemeFactory.Box(new Color(0, 0, 0, 0), 10, radius: 7));
        b.AddThemeStyleboxOverride("hover", ThemeFactory.Box(UiTokens.SurfaceHover, 10, radius: 7));
        b.AddThemeStyleboxOverride("pressed", ThemeFactory.Box(UiTokens.Accent, 10, radius: 7));
        b.AddThemeStyleboxOverride("hover_pressed", ThemeFactory.Box(UiTokens.AccentSoft, 10, radius: 3));
        b.AddThemeStyleboxOverride("pressed", ThemeFactory.Box(UiTokens.Accent, 10, radius: 3));
        b.AddThemeColorOverride("font_pressed_color", UiTokens.Background);
        b.AddThemeColorOverride("font_hover_pressed_color", UiTokens.Background);
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

    // ---------- Camadas ----------

    private static readonly (MapLayer Layer, string Name, string Tip)[] Layers =
    {
        (MapLayer.Normal, "Mapa", "Terreno e biomas"),
        (MapLayer.Crowding, "Lotação", "Jogadores presentes em relação à capacidade da zona"),
        (MapLayer.Danger, "Perigo", "Risco das atividades, PvP e regra de morte"),
        (MapLayer.Level, "Nível", "Faixa de nível do conteúdo"),
    };

    private void BuildLayerPanel()
    {
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop };
        panel.AddThemeStyleboxOverride("panel", ThemeFactory.Glass(12, 12));
        panel.Position = new Vector2(UiTokens.S16, UiTokens.TopBarHeight + UiTokens.S16);
        _ui.AddChild(panel);
        _layerPanel = panel;
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 8);
        panel.AddChild(col);
        col.AddChild(ThemeFactory.Section("Camadas do mapa"));
        var seg = Segmented(out var row);
        var group = new ButtonGroup();
        for (int i = 0; i < Layers.Length; i++)
        {
            var (l, name, tip) = Layers[i];
            var b = SegButton(name, tip, group);
            b.CustomMinimumSize = new Vector2(76, 30);
            b.AddThemeFontOverride("font", UiFonts.SemiBold);
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
        col.AddThemeConstantOverride("separation", 2);
        var tex = new GradientTexture2D { Gradient = new Gradient { Colors = new[] { a, b, c }, Offsets = new[] { 0f, 0.5f, 1f } }, Width = 256, Height = 8 };
        col.AddChild(new TextureRect { Texture = tex, CustomMinimumSize = new Vector2(0, 8), ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize, StretchMode = TextureRect.StretchModeEnum.Scale });
        var labels = new HBoxContainer();
        labels.AddChild(ThemeFactory.Text(left, UiTokens.FontCaption, UiTokens.TextSecondary));
        labels.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });
        labels.AddChild(ThemeFactory.Text(right, UiTokens.FontCaption, UiTokens.TextSecondary));
        col.AddChild(labels);
        return col;
    }

    // ---------- Legenda e controles ----------

    private void BuildLegend(ContentDatabase content)
    {
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop };
        panel.AddThemeStyleboxOverride("panel", ThemeFactory.Glass(UiTokens.S16));
        panel.SetAnchorsPreset(Control.LayoutPreset.BottomLeft);
        panel.GrowVertical = Control.GrowDirection.Begin;
        panel.OffsetLeft = UiTokens.S16;
        panel.OffsetBottom = -UiTokens.S16;
        _ui.AddChild(panel);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", UiTokens.S8);
        panel.AddChild(col);
        col.AddChild(ThemeFactory.Section("Jogadores por perfil"));
        var profiles = new HFlowContainer { CustomMinimumSize = new Vector2(520, 0) };
        profiles.AddThemeConstantOverride("h_separation", UiTokens.S16);
        profiles.AddThemeConstantOverride("v_separation", 4);
        foreach (var p in content.ProfilesOrdered)
        {
            int count = _host!.Persons.Count(x => x.ProfileId == p.Id);
            var item = new HBoxContainer();
            item.AddThemeConstantOverride("separation", 6);
            item.AddChild(new DotIcon(UiTokens.ProfileColor(p.Id)) { CustomMinimumSize = new Vector2(12, 12), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
            item.AddChild(ThemeFactory.Text(p.Name, UiTokens.FontSmall, UiTokens.TextPrimary));
            item.AddChild(ThemeFactory.Text(count.ToString(CultureInfo.InvariantCulture), UiTokens.FontSmall, UiTokens.TextSecondary, UiFonts.Numbers));
            profiles.AddChild(item);
        }
        col.AddChild(profiles);
        col.AddChild(ThemeFactory.Text("Grupos de 5+ no mesmo ponto viram uma bolha; clique para ver a lista.", UiTokens.FontCaption, UiTokens.TextDisabled));
    }

    private void BuildMapControls()
    {
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop };
        panel.AddThemeStyleboxOverride("panel", ThemeFactory.Glass(4, 10));
        panel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        panel.GrowHorizontal = Control.GrowDirection.Begin;
        panel.GrowVertical = Control.GrowDirection.Begin;
        panel.OffsetRight = -(UiTokens.SidePanelWidth + UiTokens.S12);
        panel.OffsetBottom = -UiTokens.S12;
        _ui.AddChild(panel);
        _mapControls = panel;
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 2);
        panel.AddChild(col);
        foreach (var (text, tip, action) in new (string, string, Action)[]
        {
            ("+", "Aproximar (roda do mouse ou +)", () => _map.ZoomBy(1.3f)),
            ("−", "Afastar (roda do mouse ou −)", () => _map.ZoomBy(1f / 1.3f)),
            ("⤢", "Enquadrar o mundo (Home)", () => FitMap(false)),
        })
        {
            var b = new Button { Text = text, TooltipText = tip, CustomMinimumSize = new Vector2(38, 38), FocusMode = Control.FocusModeEnum.None };
            b.AddThemeFontSizeOverride("font_size", 18);
            b.AddThemeStyleboxOverride("normal", ThemeFactory.Box(new Color(0, 0, 0, 0), 6, radius: 8));
            b.Pressed += action;
            col.AddChild(b);
        }
    }

    private void ShowError(string title, IEnumerable<string> lines)
    {
        var panel = new PanelContainer();
        var sb = ThemeFactory.Glass(UiTokens.S24);
        sb.BorderColor = UiTokens.Danger;
        panel.AddThemeStyleboxOverride("panel", sb);
        panel.SetAnchorsPreset(Control.LayoutPreset.Center);
        panel.GrowHorizontal = Control.GrowDirection.Both;
        panel.GrowVertical = Control.GrowDirection.Both;
        _ui.AddChild(panel);
        var col = new VBoxContainer();
        panel.AddChild(col);
        col.AddChild(ThemeFactory.Text(title, UiTokens.FontTitle, UiTokens.Danger, UiFonts.SemiBold));
        foreach (var l in lines) col.AddChild(ThemeFactory.Text(l, UiTokens.FontSmall));
        GD.PrintErr(title + "\n" + string.Join("\n", lines));
    }

    // ---------- Quadro a quadro ----------

    public override void _Process(double delta)
    {
        if (_host == null) { HandleAutomation(delta); return; }
        var snap = _host.Snapshot;
        _map.SetSnapshot(snap);
        CollectEvents();
        _host.CopyMetricsInto(_metrics);
        var now = _host.Calendar.ToDateTime(snap.Tick);
        _map.SetHour(now.Hour + now.Minute / 60.0 + now.Second / 3600.0);

        _uiTimer += delta;
        if (_uiTimer >= 0.2 || _frames < 3)
        {
            _uiTimer = 0;
            _clockLabel.Text = $"{now.Hour:00}:{now.Minute:00}";
            _dateLabel.Text = $"{PtBr.Date(now)} · {PtBr.DayKind(_host.Calendar.KindOf(now.Date))}";
            _speedLabel.Text = !OnMap ? "pausado fora do mapa" : snap.RequestedSpeed == 0 ? "pausado" : snap.EffectiveSpeed < 0 ? "medindo…" : $"efetiva {PtBr.Decimal(snap.EffectiveSpeed)}×";
            UpdateKpis(snap);
            if (OnMap) UpdateEmptyBanner(snap);
            _fpsLabel.Text = $"{Engine.GetFramesPerSecond():0} fps";
            UpdateServerChip(snap.ServerOpen);
        }
        _modalTimer += delta;
        if (_modalTimer >= 1.0)
        {
            _modalTimer = 0;
            if (_modal != null && IsInstanceValid(_modal)) _modal.RefreshLive();
            else _side?.Refresh();
            if (_quick != null && IsInstanceValid(_quick) && !_quick.GetGlobalRect().HasPoint(GetViewport().GetMousePosition())) _quick.Refresh();
        }
        HandleAutomation(delta);
    }

    private void UpdateServerChip(bool open)
    {
        if (open == _serverOpenShown && _frames > 2) return;
        _serverOpenShown = open;
        var parent = _serverChip.GetParent();
        int idx = _serverChip.GetIndex();
        _serverChip.QueueFree();
        _serverChip = ThemeFactory.Chip(open ? "Servidor aberto" : "Servidor fechado", open ? UiTokens.Success : UiTokens.TextSecondary, dot: true);
        parent.AddChild(_serverChip);
        parent.MoveChild(_serverChip, idx);
    }

    private void HandleAutomation(double delta)
    {
        _frames++;
        if (_fakeMouse is { } fm) _map.SimulateMouse(fm);
        if (_args.TryGetValue("screenshot", out var path) && _frames == int.Parse(Arg("frames", "45"), CultureInfo.InvariantCulture))
        {
            var img = GetViewport().GetTexture().GetImage();
            var full = System.IO.Path.GetFullPath(path);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
            var err = img.SavePng(full);
            GD.Print(err == Error.Ok ? $"CAPTURA_OK {full} {img.GetWidth()}x{img.GetHeight()}" : $"CAPTURA_ERRO {err}");
            Quit();
        }
        if (_args.TryGetValue("measure-seconds", out var secs))
        {
            if (_frames > 60)
            {
                _measureElapsed += delta;
                _measureFrames++;
                var rid = GetViewport().GetViewportRid();
                _frameMs.Add(delta * 1000);
                _gpuMs += RenderingServer.ViewportGetMeasuredRenderTimeGpu(rid);
                _renderCpuMs += RenderingServer.ViewportGetMeasuredRenderTimeCpu(rid);
                _drawMs += _map.LastDrawMs;
                _overlayMs += _map.LastOverlayMs;
                _mapProcessMs += _map.LastProcessMs;
            }
            if (_measureElapsed >= double.Parse(secs, CultureInfo.InvariantCulture))
            {
                int n = Math.Max(1, _measureFrames);
                _frameMs.Sort();
                double p95 = _frameMs.Count == 0 ? 0 : _frameMs[Math.Min(_frameMs.Count - 1, (int)(_frameMs.Count * 0.95))];
                GD.Print(string.Create(CultureInfo.InvariantCulture,
                    $"FPS_MEDIO {_measureFrames / _measureElapsed:F1} quadro_ms={_measureElapsed * 1000 / n:F2} p95_ms={p95:F2} gpu_ms={_gpuMs / n:F2} render_cpu_ms={_renderCpuMs / n:F2} mapa_draw_ms={_drawMs / n:F2} rotulos_ms={_overlayMs / n:F2} mapa_process_ms={_mapProcessMs / n:F2} populacao={_host?.Persons.Count ?? 0}"));
                Quit();
            }
        }
    }

    private void Quit()
    {
        _host?.Dispose();
        GetTree().Quit();
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (e is not InputEventKey k || !k.Pressed || k.Echo) return;
        if (k.Keycode == Key.F11 || (k.AltPressed && k.Keycode is Key.Enter or Key.KpEnter)) { ClientSettings.ToggleFullscreen(); GetViewport().SetInputAsHandled(); return; }
        if (k.AltPressed && k.Keycode is Key.Left or Key.Right) { Navigate(k.Keycode == Key.Left ? -1 : +1); GetViewport().SetInputAsHandled(); return; }
        if (k.Keycode is Key.M or Key.J or Key.Z or Key.Q && !k.CtrlPressed && GetViewport().GuiGetFocusOwner() is not LineEdit)
        {
            OpenSection(k.Keycode switch { Key.J => "players", Key.Z => "zones", Key.Q => "quests", _ => "map" });
            GetViewport().SetInputAsHandled();
            return;
        }
        if (!OnMap) return;
        switch (k.Keycode)
        {
            case Key.Space: SetSpeed(_host?.Speed == 0 ? _lastNonZeroSpeed : 0); break;
            case Key.Key1: SetSpeed(1); break;
            case Key.Key2: SetSpeed(2); break;
            case Key.Key3: SetSpeed(5); break;
            case Key.Key4: SetSpeed(10); break;
            case Key.Equal or Key.KpAdd: _map.ZoomBy(1.3f); break;
            case Key.Minus or Key.KpSubtract: _map.ZoomBy(1f / 1.3f); break;
            case Key.Home: FitMap(false); break;
            case Key.Escape: CloseOverlays(); _map.SelectPerson(0); _map.SelectRegion(null); break;
            default: return;
        }
        GetViewport().SetInputAsHandled();
    }

    public override void _Input(InputEvent e)
    {
        // Botoes laterais do mouse: voltar e avancar (em qualquer tela).
        if (e is InputEventMouseButton { Pressed: true } mb && mb.ButtonIndex is MouseButton.Xbutton1 or MouseButton.Xbutton2)
        {
            Navigate(mb.ButtonIndex == MouseButton.Xbutton1 ? -1 : +1);
            GetViewport().SetInputAsHandled();
        }
    }

    public override void _ExitTree() => _host?.Dispose();
}

/// <summary>Icone de engrenagem desenhado (sem depender de glifo da fonte).</summary>
public partial class GearIcon : Control
{
    public override void _Draw()
    {
        var c = Size / 2f;
        var col = UiTokens.TextSecondary;
        for (int i = 0; i < 8; i++)
        {
            float a = i * Mathf.Tau / 8f;
            var d = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            DrawLine(c + d * 4.5f, c + d * 7.5f, col, 2.6f, true);
        }
        DrawArc(c, 4.6f, 0, Mathf.Tau, 24, col, 2.2f, true);
    }
}

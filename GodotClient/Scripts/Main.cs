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
///   --open-person=ID | --open-zone=ID | --open-cluster   abre uma ficha ao iniciar (auto, goal, objectives, longplan...)
///   --tab=N               aba inicial da ficha aberta  (--scroll=PX ou --section=N: tela da pagina)
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
    private int _speedBeforePage = 1;
    private Vector2 _lastMouse;
    private long _lastRefreshTick = -1;
    private int _watchedSeenId;
    private bool _pausedByPage;
    private bool OnMap => _modal == null || !IsInstanceValid(_modal);
    private readonly Dictionary<int, List<SimEvent>> _history = new();
    private readonly Dictionary<int, int> _characterToPerson = new();
    private readonly Dictionary<int, List<SessionLog>> _sessions = new();
    private readonly Dictionary<int, long> _registeredAt = new();
    private readonly MetricsHistory _metrics = new();
    private string _startNote = "";
    private PanelContainer? _emptyBanner;
    private Label? _emptyText;
    private Button? _skipButton;
    private readonly List<(SimEvent Event, int PersonId)> _feed = new();
    private SidePanel? _side;
    private readonly List<(string Key, Label Value, Label Delta)> _kpis = new();

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
            PersonSessions = id => _sessions.TryGetValue(id, out var l) ? l : (IReadOnlyList<SessionLog>)Array.Empty<SessionLog>(),
            RegisteredAt = id => _registeredAt.TryGetValue(id, out var t) ? t : -1,
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

        BuildNav(content);
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

    /// <summary>Nome do foco de um plano ("Atividade · Lugar"), como a ficha mostra.</summary>
    private string PlanName(string focusKey)
    {
        var p = focusKey.Split('/');
        return p.Length == 2 ? $"{_ctx.ActivityName(p[0], p[1])} · {_ctx.PlaceName(p[0], p[1])}" : focusKey;
    }

    private void OpenFromArgs()
    {
        _map.SetSnapshot(_host!.Snapshot);
        _map._Process(0);
        if (_args.TryGetValue("open-person", out var p))
        {
            int id = p == "auto"
                ? Enumerable.Range(1, _host.Snapshot.ByPerson.Length - 1).FirstOrDefault(i => _host.Snapshot.ByPerson[i] is { Status: Simulation.Core.Population.CharacterStatus.Doing } v && v.LastDecision != null)
                : p == "objectives"   // capturas: quem mais mudou de objetivos (O3: concluidos, renovados, encerrados)
                    ? Enumerable.Range(1, _host.Snapshot.ByPerson.Length - 1).Where(i => _host.Snapshot.ByPerson[i] != null)
                        .OrderByDescending(i => _host.Snapshot.ByPerson[i]!.ObjectiveChanges).ThenBy(i => i).FirstOrDefault()
                : p == "goal"   // capturas: primeiro jogador com objetivo pessoal ativo
                    ? Enumerable.Range(1, _host.Snapshot.ByPerson.Length - 1).FirstOrDefault(i => _host.Snapshot.ByPerson[i] is { } v && v.Goals.Count > 0)
                    : p == "longplan"   // capturas: plano da sessao com o nome mais longo (cards nao podem empilhar)
                        ? Enumerable.Range(1, _host.Snapshot.ByPerson.Length - 1).Where(i => _host.Snapshot.ByPerson[i]?.Plan != null)
                            .OrderByDescending(i => PlanName(_host.Snapshot.ByPerson[i]!.Plan!.FocusKey).Length).FirstOrDefault()
                        : p.StartsWith("resident:", StringComparison.Ordinal)   // capturas: primeiro registrado que mora na localidade
                            ? Enumerable.Range(1, _host.Snapshot.ByPerson.Length - 1).FirstOrDefault(i => _host.Snapshot.ByPerson[i] != null && _ctx.Persons[i - 1].ResidenceId == p[9..])
                            : p.StartsWith("occ:", StringComparison.Ordinal)   // capturas: primeiro registrado com a ocupacao (e o perfil: occ:<ocupacao>:<perfil>)
                                ? Enumerable.Range(1, _host.Snapshot.ByPerson.Length - 1).FirstOrDefault(i => _host.Snapshot.ByPerson[i] != null
                                    && _ctx.Persons[i - 1].OccupationId == p[4..].Split(':')[0]
                                    && (p[4..].Split(':').Length < 2 || _ctx.Persons[i - 1].ProfileId == p[4..].Split(':')[1]))
                                : int.TryParse(p, out var n) ? n : 0;
            if (id > 0) OpenPerson(id);
            // Capturas: abre outra ficha e volta pelo historico (a evolucao deve carregar).
            if (id > 0 && _args.ContainsKey("back-test"))
            {
                int other = Enumerable.Range(1, _host.Snapshot.ByPerson.Length - 1).FirstOrDefault(i => i != id && _host.Snapshot.ByPerson[i] != null);
                if (other > 0) { OpenPerson(other); Navigate(-1); }
            }
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
        if (_args.TryGetValue("page", out var pg)) { if (pg == "players") OpenPlayersPage(); else if (pg == "zones") OpenZonesPage(); else if (pg == "quests") OpenQuestsPage(); else if (pg == "settings") OpenSettingsPage(); else if (pg == "icons") ShowPage("icons", "", () => new IconGallery()); }
        var center = GetViewport().GetVisibleRect().Size / 2f - new Vector2(UiTokens.SidePanelWidth / 2f, 0);
        if (_args.TryGetValue("peek-person", out var pp))
        {
            int id = pp == "auto"
                ? Enumerable.Range(1, _host.Snapshot.ByPerson.Length - 1).FirstOrDefault(i => _host.Snapshot.ByPerson[i] is { Status: Simulation.Core.Population.CharacterStatus.Doing } v && v.LastDecision != null)
                : int.TryParse(pp, out var n) ? n : 0;
            if (id > 0) PeekPerson(id, center);
        }
        if (_args.TryGetValue("side", out var sd) && sd == "off" && _sideOpen) ToggleSidePanel();
        if (_args.TryGetValue("nav", out var nv) && nv == "off") _nav?.SetExpanded(false);
        if (_args.ContainsKey("tip-demo"))
        {
            // Capturas: dica de exemplo com a moldura do tema (o mouse real nao e simulado).
            var demo = new PanelContainer { Position = new Vector2(700, 300) };
            demo.AddThemeStyleboxOverride("panel", _ui.Theme.GetStylebox("panel", "TooltipPanel"));
            demo.AddChild(RichTip.Build("Mecânica|13 / 20|#8B94A1\n^" + AttributeInfo.GroupOf("mechanicalSkill") + "\nPotencial|17 / 20 · talento alto\n#O que afeta\n"
                + AttributeInfo.Bullets(AttributeInfo.ByKey["mechanicalSkill"].Influences) + "\n#Como muda\n" + AttributeInfo.Bullets(AttributeInfo.ByKey["mechanicalSkill"].Changes)));
            _ui.AddChild(demo);
        }
        if (_args.TryGetValue("side-tab", out var stb) && int.TryParse(stb, out var sti)) { if (!_sideOpen) ToggleSidePanel(); _side?.ShowTab(sti); }
        if (_args.TryGetValue("warp", out var wp) && wp.Split('x') is { Length: 2 } wpp
            && float.TryParse(wpp[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var wx)
            && float.TryParse(wpp[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var wy))
            GetTree().CreateTimer(0.6).Timeout += () => Input.WarpMouse(new Vector2(wx, wy));
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
        if (_modal != null && _args.TryGetValue("scroll", out var sc) && int.TryParse(sc, out int sv)) _modal.ScrollTo(sv);
        if (_modal != null && _args.TryGetValue("section", out var sec) && int.TryParse(sec, out int sn)) _modal.ScrollToSection(sn);
        if (_modal != null && _args.TryGetValue("wheel", out var wh) && int.TryParse(wh, out int wn)) _modal.WheelForCapture(wn);
        // --pager=n,n,p: aperta as setas do primeiro paginador da pagina (n = seguintes, p = anteriores), um clique
        // a cada 15 quadros, 40 quadros depois de a ficha abrir (conferencia das paginas nas capturas; por quadros, nao por tempo).
        if (_args.TryGetValue("pager", out var pagerArg))
        {
            _pagerSteps = pagerArg.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(x => x == "p" ? "Anteriores" : "Seguintes").ToList();
            _pagerStart = _frames + 40;   // conta a partir de quando a ficha abriu
        }
    }

    private List<string> _pagerSteps = new();
    private int _pagerStart;

    /// <summary>Aperta o primeiro botao visivel e ativo com esse tooltip (capturas).</summary>
    private static bool PressFirst(Node n, string tip)
    {
        if (n is BaseButton b && b.TooltipText == tip && b.IsVisibleInTree() && !b.Disabled)
        {
            b.EmitSignal(BaseButton.SignalName.Pressed);
            return true;
        }
        foreach (var c in n.GetChildren())
            if (PressFirst(c, tip)) return true;
        return false;
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
        if (_side != null) _side.Visible = visible && _sideOpen;
        if (_sideToggle != null) _sideToggle.Visible = visible;
        if (_layerPanel != null) _layerPanel.Visible = visible;
        if (_mapControls != null) _mapControls.Visible = visible;
        if (_emptyBanner != null && !visible) _emptyBanner.Visible = false;
        _speedSeg.Visible = visible;
        _speedLabel.Visible = visible;
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
        if (_nav != null) _nav.Active = section;
        SetScreenTitle(section);
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
            if (list.Count > 300) list.RemoveAt(list.Count - 1);
            // Diario de sessoes por pessoa (graficos de forma e engajamento).
            if (e.Type == "session_started")
            {
                if (!_sessions.TryGetValue(pid, out var sl)) _sessions[pid] = sl = new List<SessionLog>();
                int planned = int.TryParse((e.Detail ?? "").Split(' ')[0], out var pm) ? pm : 0;
                sl.Add(new SessionLog(e.Tick, -1, planned));
                if (sl.Count > 90) sl.RemoveAt(0);
            }
            else if (e.Type == "session_ended" && _sessions.TryGetValue(pid, out var se) && se.Count > 0 && se[^1].End < 0)
                se[^1] = se[^1] with { End = e.Tick };
            else if (e.Type == "person_registered") _registeredAt[pid] = e.Tick;
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
        if (_modalTimer >= 1.0 || _frames == 8)
        {
            _modalTimer = 0;
            var mouse = GetViewport().GetMousePosition();
            bool reading = mouse == _lastMouse;   // mouse parado: pode estar lendo uma dica
            _lastMouse = mouse;
            var snapNow = _host.Snapshot;
            // Detalhe da pessoa observada (evolucao, humor, memorias): chega um pouco depois de a
            // ficha abrir. Com o tempo pausado e o mouse parado (ex.: logo apos clicar em voltar),
            // a chegada do detalhe precisa atualizar a ficha mesmo assim, e so conta como vista
            // depois de atualizar.
            int watchedId = snapNow.Watched?.PersonId ?? 0;
            if (watchedId == 0) _watchedSeenId = 0;
            bool detailArrived = watchedId != 0 && watchedId != _watchedSeenId;
            if (_modal != null && IsInstanceValid(_modal))
            {
                if (detailArrived || (snapNow.Tick != _lastRefreshTick && !reading))
                {
                    _lastRefreshTick = snapNow.Tick;
                    _watchedSeenId = watchedId;
                    _modal.RefreshLive();
                }
            }
            else if (!(reading && _side != null && _side.Visible && _side.GetGlobalRect().HasPoint(mouse))) _side?.Refresh();
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
        if (_pagerSteps.Count > 0 && _frames >= _pagerStart && (_frames - _pagerStart) % 15 == 0 && (_frames - _pagerStart) / 15 < _pagerSteps.Count
            && _modal != null && IsInstanceValid(_modal))
            PressFirst(_modal, _pagerSteps[(_frames - _pagerStart) / 15]);
        if (_args.TryGetValue("screenshot", out var path) && _frames == int.Parse(Arg("frames", "45"), CultureInfo.InvariantCulture))
        {
            var img = GetViewport().GetTexture().GetImage();
            var full = System.IO.Path.GetFullPath(path);
            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
            var err = img.SavePng(full);
            GD.Print(err == Error.Ok ? $"CAPTURA_OK {full} {img.GetWidth()}x{img.GetHeight()}" : $"CAPTURA_ERRO {err}");
            // Cards que precisariam de rolagem (D-068): todos os da pagina aberta, de todas as telas dela.
            var over = new List<string>();
            void Scan(Node n)
            {
                if (n is ScrollContainer s && s.HasMeta("card") && s.IsVisibleInTree() && s.GetChildCount() > 0 && s.GetChild(0) is Control body)
                {
                    float extra = body.GetCombinedMinimumSize().Y - s.Size.Y;
                    if (extra > 0.5f) over.Add($"{s.GetMeta("card").AsString()}: +{extra:0} px");
                }
                foreach (var c in n.GetChildren()) Scan(c);
            }
            Scan(GetTree().Root);
            System.IO.File.WriteAllLines(System.IO.Path.ChangeExtension(full, ".rolagem.txt"), over);
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
        if (k.Keycode == Key.N && !k.CtrlPressed && GetViewport().GuiGetFocusOwner() is not LineEdit) { _nav?.Toggle(); GetViewport().SetInputAsHandled(); return; }
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
            case Key.P: ToggleSidePanel(); break;
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

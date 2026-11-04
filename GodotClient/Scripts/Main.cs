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
///   --screenshot=ARQ --frames=N    captura e encerra
///   --measure-seconds=S   imprime FPS medio e encerra
/// </summary>
public partial class Main : Node
{
    private readonly Dictionary<string, string> _args = new(StringComparer.Ordinal);
    private SimulationHost? _host;
    private ClientContext _ctx = null!;
    private WorldMapView _map = null!;
    private Control _ui = null!;
    private Label _clockLabel = null!, _dateLabel = null!, _speedLabel = null!, _fpsLabel = null!, _onlineLabel = null!, _registeredLabel = null!;
    private Label? _worldNameLabel;
    private PanelContainer _serverChip = null!;
    private VBoxContainer _layerLegend = null!;
    private readonly List<Button> _speedButtons = new();
    private readonly List<Button> _layerButtons = new();
    private int _lastNonZeroSpeed = 1;
    private double _uiTimer, _modalTimer;
    private int _frames;
    private double _measureElapsed;
    private int _measureFrames;
    private bool _serverOpenShown;
    private ModalFrame? _modal;
    private ClusterPopover? _popover;
    private readonly Dictionary<int, List<SimEvent>> _history = new();
    private readonly Dictionary<int, int> _characterToPerson = new();

    public override void _Ready()
    {
        foreach (var a in OS.GetCmdlineUserArgs())
        {
            var kv = a.TrimStart('-').Split('=', 2);
            _args[kv[0]] = kv.Length > 1 ? kv[1] : "true";
        }

        var terrain = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore, Color = UiTokens.Background };
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
            PersonHistory = id => _history.TryGetValue(id, out var l) ? l : (IReadOnlyList<SimEvent>)Array.Empty<SimEvent>(),
            FocusPerson = (id, follow) => { _map.SelectPerson(id); _map.FocusPerson(id, follow); },
            FocusZone = id => { _map.SelectRegion(id); _map.FocusRegion(id); },
            OpenPerson = OpenPerson,
            OpenZone = OpenZone,
        };

        _map.Setup(content, _host.Layout, _host.Persons, terrain);
        _map.RegionSelected += id => { if (id.Length > 0) OpenZone(id); };
        _map.PersonSelected += OpenPerson;
        _map.ClusterSelected += OpenCluster;

        BuildTopBar(content);
        BuildLayerPanel();
        BuildLegend(content);
        BuildMapControls();

        _host.Submit(new SetServerOpenCommand(true));
        long preRoll = 0;
        if (_args.TryGetValue("hour", out var hour))
            preRoll = (long)(double.Parse(hour, CultureInfo.InvariantCulture) * Simulation.Core.Time.SimTime.TicksPerHour);
        if (_args.TryGetValue("sim-minutes", out var mins))
            preRoll = long.Parse(mins, CultureInfo.InvariantCulture) * Simulation.Core.Time.SimTime.TicksPerMinute;
        if (preRoll > 0) _host.AdvanceBeforeStart(preRoll);
        _host.Speed = int.Parse(Arg("speed", "1"), CultureInfo.InvariantCulture);
        _host.Start();
        CollectEvents();

        SyncSpeedButtons();
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
        if (_args.TryGetValue("open-zone", out var z) && _ctx.Content.Regions.ContainsKey(z)) OpenZone(z);
        if (_args.ContainsKey("open-cluster"))
        {
            var snap = _host.Snapshot;
            var best = Enumerable.Range(1, snap.ByPerson.Length - 1)
                .Where(i => snap.ByPerson[i] is { Status: Simulation.Core.Population.CharacterStatus.Doing } v && v.ActivityId.Length > 0)
                .GroupBy(i => snap.ByPerson[i]!.TargetRegionId + "/" + snap.ByPerson[i]!.ActivityId)
                .OrderByDescending(g => g.Count()).FirstOrDefault();
            if (best != null) OpenCluster(best.Key, GetViewport().GetVisibleRect().Size / 2f);
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
        if (_worldNameLabel != null) _worldNameLabel.Visible = size.X >= 1600;
        var area = new Rect2(UiTokens.S24, UiTokens.TopBarHeight + UiTokens.S16, size.X - UiTokens.S24 * 2, size.Y - UiTokens.TopBarHeight - UiTokens.S16 - 96);
        _map.FitTo(area, instant);
    }

    // ---------- Fichas ----------

    private void CloseOverlays()
    {
        if (_popover != null && IsInstanceValid(_popover)) _popover.QueueFree();
        _popover = null;
        if (_modal != null && IsInstanceValid(_modal)) _modal.QueueFree();
        _modal = null;
    }

    private void OpenPerson(int personId)
    {
        if (_host == null || personId < 1 || personId > _host.Persons.Count) return;
        CloseOverlays();
        _map.SelectPerson(personId);
        _modal = new PlayerModal(_ctx, personId);
        _modal.Closed += () => _modal = null;
        _ui.AddChild(_modal);
    }

    private void OpenZone(string zoneId)
    {
        if (_host == null || !_ctx.Content.Regions.ContainsKey(zoneId)) return;
        CloseOverlays();
        _map.SelectRegion(zoneId);
        _modal = new ZoneModal(_ctx, _map, zoneId);
        _modal.Closed += () => { _modal = null; _map.SelectRegion(null); };
        _ui.AddChild(_modal);
    }

    private void OpenCluster(string spotKey, Vector2 at)
    {
        CloseOverlays();
        var parts = spotKey.Split('/');
        var zone = _ctx.Content.Regions[parts[0]];
        string title = parts[1].Length == 0 ? "Na encruzilhada" : _ctx.ActivityName(parts[0], parts[1]);
        var people = _map.PeopleAt(spotKey);
        if (people.Count == 0)
        {
            var snap = _host!.Snapshot;
            people = Enumerable.Range(1, snap.ByPerson.Length - 1).Where(i => snap.ByPerson[i] is { } v && v.TargetRegionId + "/" + v.ActivityId == spotKey).ToList();
        }
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
            foreach (var ent in e.Entities)
            {
                int pid = ent.Kind == "person" ? (int)ent.Id : ent.Kind == "character" && _characterToPerson.TryGetValue((int)ent.Id, out var p) ? p : 0;
                if (pid == 0) continue;
                if (!_history.TryGetValue(pid, out var list)) _history[pid] = list = new List<SimEvent>();
                list.Insert(0, e);
                if (list.Count > 120) list.RemoveAt(list.Count - 1);
                break;
            }
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

        var brand = new HBoxContainer();
        brand.AddThemeConstantOverride("separation", UiTokens.S8);
        brand.AddChild(ThemeFactory.Text("◆", 16, UiTokens.Accent));
        brand.AddChild(ThemeFactory.Text("MMORPG TYCOON", UiTokens.FontTitle, UiTokens.AccentSoft, UiFonts.Display));
        row.AddChild(brand);
        _worldNameLabel = ThemeFactory.Text("· " + content.World.Name, UiTokens.FontBody, UiTokens.TextSecondary, UiFonts.Medium);
        row.AddChild(_worldNameLabel);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        _serverChip = ThemeFactory.Chip("Servidor fechado", UiTokens.TextSecondary, dot: true);
        row.AddChild(_serverChip);

        var players = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        players.AddThemeConstantOverride("separation", -2);
        _onlineLabel = ThemeFactory.Text("", UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Numbers);
        _registeredLabel = ThemeFactory.Text("", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers);
        players.AddChild(_onlineLabel);
        players.AddChild(_registeredLabel);
        row.AddChild(players);

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
        row.AddChild(seg);

        _speedLabel = ThemeFactory.Text("", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers);
        _speedLabel.CustomMinimumSize = new Vector2(84, 0);
        row.AddChild(_speedLabel);
        _fpsLabel = ThemeFactory.Text("", UiTokens.FontCaption, UiTokens.TextDisabled, UiFonts.Numbers);
        _fpsLabel.CustomMinimumSize = new Vector2(46, 0);
        row.AddChild(_fpsLabel);
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
        b.AddThemeStyleboxOverride("hover_pressed", ThemeFactory.Box(UiTokens.AccentSoft, 10, radius: 7));
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
            case MapLayer.Level: _layerLegend.AddChild(GradientLegend(new Color("#3E6FB0"), new Color("#6C5FBB"), new Color("#9B4FC6"), "nível 1", "nível 16+")); break;
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
        panel.OffsetRight = -UiTokens.S16;
        panel.OffsetBottom = -UiTokens.S16;
        _ui.AddChild(panel);
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
        var now = _host.Calendar.ToDateTime(snap.Tick);
        _map.SetHour(now.Hour + now.Minute / 60.0 + now.Second / 3600.0);

        _uiTimer += delta;
        if (_uiTimer >= 0.2 || _frames < 3)
        {
            _uiTimer = 0;
            _clockLabel.Text = $"{now.Hour:00}:{now.Minute:00}";
            _dateLabel.Text = $"{PtBr.Date(now)} · {PtBr.DayKind(_host.Calendar.KindOf(now.Date))}";
            _speedLabel.Text = snap.RequestedSpeed == 0 ? "pausado" : snap.EffectiveSpeed < 0 ? "medindo…" : $"efetiva {PtBr.Decimal(snap.EffectiveSpeed)}×";
            _onlineLabel.Text = $"● {PtBr.Int(snap.Online)} conectados";
            _registeredLabel.Text = $"{PtBr.Int(snap.Registered)} cadastrados · {PtBr.Int(snap.PersonsTotal)} interessados";
            _fpsLabel.Text = $"{Engine.GetFramesPerSecond():0} fps";
            UpdateServerChip(snap.ServerOpen);
        }
        _modalTimer += delta;
        if (_modalTimer >= 0.5)
        {
            _modalTimer = 0;
            if (_modal != null && IsInstanceValid(_modal)) _modal.RefreshLive();
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
            if (_frames > 30) { _measureElapsed += delta; _measureFrames++; }
            if (_measureElapsed >= double.Parse(secs, CultureInfo.InvariantCulture))
            {
                GD.Print($"FPS_MEDIO {_measureFrames / _measureElapsed:F1} quadros={_measureFrames} segundos={_measureElapsed:F2} populacao={_host?.Persons.Count ?? 0}");
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

    public override void _ExitTree() => _host?.Dispose();
}

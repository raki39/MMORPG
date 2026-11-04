using System.Globalization;
using Godot;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Infrastructure.Content;
using GodotClient.Fixtures;
using GodotClient.Map;
using GodotClient.Sim;
using GodotClient.Ui;

namespace GodotClient;

/// <summary>
/// Raiz do cliente. Monta HUD por codigo com os tokens, conecta mapa, host da simulacao
/// e fixture de movimento (demonstracao ate o M2).
///
/// Argumentos (depois de "--" na linha de comando do Godot):
///   --world=PASTA  --seed=N  --population=N  --speed=N
///   --select=ID_REGIAO | --person=ID   selecao inicial
///   --hour=H             avanca a simulacao ate a hora H do primeiro dia (capturas)
///   --sim-minutes=N      avanca a simulacao N minutos antes de comecar
///   --screenshot=ARQ     salva PNG apos --frames=N quadros e encerra
///   --measure-seconds=S  imprime FPS medio apos S segundos e encerra
/// </summary>
public partial class Main : Node
{
    private readonly Dictionary<string, string> _args = new(StringComparer.Ordinal);
    private SimulationHost? _host;
    private DemoMovementFixture? _fixture;
    private WorldMapView _map = null!;
    private ColorRect _terrain = null!;
    private Control _ui = null!;
    private Label _clockLabel = null!, _dateLabel = null!, _speedLabel = null!, _fpsLabel = null!, _populationLabel = null!;
    private Label? _worldNameLabel;
    private PanelContainer _serverChip = null!;
    private VBoxContainer _inspector = null!;
    private Label? _inspectorCount;
    private readonly List<Button> _speedButtons = new();
    private int _lastNonZeroSpeed = 1;
    private double _uiTimer;
    private int _frames;
    private double _measureElapsed;
    private int _measureFrames;
    private bool _serverOpenShown;

    public override void _Ready()
    {
        foreach (var a in OS.GetCmdlineUserArgs())
        {
            var kv = a.TrimStart('-').Split('=', 2);
            _args[kv[0]] = kv.Length > 1 ? kv[1] : "true";
        }

        // Camadas: terreno (shader) -> mapa -> HUD.
        _terrain = new ColorRect { MouseFilter = Control.MouseFilterEnum.Ignore, Color = UiTokens.Background };
        _terrain.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(_terrain);
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
        _fixture = new DemoMovementFixture(content, _host.Persons, seed);
        _map.Setup(content, _fixture, _host.Persons, _terrain);
        _map.RegionSelected += OnRegionSelected;
        _map.PersonSelected += OnPersonSelected;

        BuildTopBar(content);
        BuildInspector();
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

        SyncSpeedButtons();
        GetViewport().SizeChanged += () => FitMap(false);
        FitMap(true);

        if (_args.TryGetValue("select", out var sel) && content.Regions.ContainsKey(sel))
        {
            _map.SelectRegion(sel);
            OnRegionSelected(sel);
        }
        if (_args.TryGetValue("person", out var pid) && int.TryParse(pid, out int personId) && personId >= 1 && personId <= _host.Persons.Count)
        {
            _map.SelectPerson(personId);
            OnPersonSelected(personId);
        }
    }

    private string Arg(string key, string fallback) => _args.TryGetValue(key, out var v) ? v : fallback;

    private string ResolveWorldFolder()
    {
        if (_args.TryGetValue("world", out var w)) return w;
        string rel = System.IO.Path.Combine("Content", "presets", "inicial");
        string exeDir = System.IO.Path.GetDirectoryName(OS.GetExecutablePath()) ?? "";
        string besideExe = System.IO.Path.Combine(exeDir, rel);
        if (System.IO.Directory.Exists(besideExe)) return besideExe;
        string projectDir = ProjectSettings.GlobalizePath("res://");
        return System.IO.Path.GetFullPath(System.IO.Path.Combine(projectDir, "..", rel));
    }

    private Rect2 MapArea()
    {
        var size = GetViewport().GetVisibleRect().Size;
        return new Rect2(0, UiTokens.TopBarHeight, size.X - UiTokens.InspectorWidth - UiTokens.S24, size.Y - UiTokens.TopBarHeight);
    }

    private void FitMap(bool instant)
    {
        var size = GetViewport().GetVisibleRect().Size;
        if (_worldNameLabel != null) _worldNameLabel.Visible = size.X >= 1600;
        var area = MapArea();
        // Margem extra embaixo para a legenda.
        _map.FitTo(new Rect2(area.Position + new Vector2(UiTokens.S24, UiTokens.S24),
            area.Size - new Vector2(UiTokens.S24 * 2, UiTokens.S24 * 2 + 90)), instant);
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
        row.AddThemeConstantOverride("separation", UiTokens.S12);
        bar.AddChild(row);

        // Marca
        var brand = new HBoxContainer();
        brand.AddThemeConstantOverride("separation", UiTokens.S8);
        var mark = new Label { Text = "◆" };
        mark.AddThemeColorOverride("font_color", UiTokens.Accent);
        mark.AddThemeFontSizeOverride("font_size", 16);
        brand.AddChild(mark);
        brand.AddChild(ThemeFactory.Text("MMORPG TYCOON", UiTokens.FontTitle, UiTokens.AccentSoft, UiFonts.Display));
        row.AddChild(brand);
        _worldNameLabel = ThemeFactory.Text("· " + content.World.Name, UiTokens.FontBody, UiTokens.TextSecondary, UiFonts.Medium);
        row.AddChild(_worldNameLabel);
        var demo = ThemeFactory.Chip("DEMONSTRAÇÃO", UiTokens.Warning, dot: true);
        demo.TooltipText = DemoMovementFixture.Label + ".\nO movimento das pessoas no mapa é fictício; sessões e escolhas reais chegam no M2.";
        demo.MouseFilter = Control.MouseFilterEnum.Stop;
        row.AddChild(demo);
        row.AddChild(new Control { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill });

        _serverChip = ThemeFactory.Chip("Servidor fechado", UiTokens.TextSecondary, dot: true);
        row.AddChild(_serverChip);

        // Relogio: hora grande + data menor.
        var clock = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        clock.AddThemeConstantOverride("separation", -2);
        _clockLabel = ThemeFactory.Text("00:00", 17, UiTokens.TextPrimary, UiFonts.Numbers);
        _clockLabel.HorizontalAlignment = HorizontalAlignment.Right;
        _dateLabel = ThemeFactory.Text("", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers);
        _dateLabel.HorizontalAlignment = HorizontalAlignment.Right;
        clock.AddChild(_clockLabel);
        clock.AddChild(_dateLabel);
        row.AddChild(clock);

        // Controle segmentado de velocidade.
        var seg = new PanelContainer();
        var segBox = ThemeFactory.Box(new Color(UiTokens.Background, 0.7f), 3, border: UiTokens.Border, radius: 10);
        segBox.ContentMarginTop = segBox.ContentMarginBottom = 3;
        seg.AddThemeStyleboxOverride("panel", segBox);
        var segRow = new HBoxContainer();
        segRow.AddThemeConstantOverride("separation", 2);
        seg.AddChild(segRow);
        var group = new ButtonGroup();
        foreach (int s in SimulationHost.Speeds)
        {
            var b = new Button
            {
                Text = s == 0 ? "❚❚" : $"{s}×",
                ToggleMode = true,
                ButtonGroup = group,
                CustomMinimumSize = new Vector2(44, 30),
                FocusMode = Control.FocusModeEnum.None,
                TooltipText = s == 0 ? "Pausar (Espaço)" : $"Velocidade {s}× (tecla {Array.IndexOf(SimulationHost.Speeds, s)})",
            };
            b.AddThemeFontOverride("font", UiFonts.Numbers);
            b.AddThemeFontSizeOverride("font_size", 13);
            b.AddThemeStyleboxOverride("normal", ThemeFactory.Box(new Color(0, 0, 0, 0), 8, radius: 7));
            b.AddThemeStyleboxOverride("hover", ThemeFactory.Box(UiTokens.SurfaceHover, 8, radius: 7));
            b.AddThemeStyleboxOverride("pressed", ThemeFactory.Box(UiTokens.Accent, 8, radius: 7));
            b.AddThemeStyleboxOverride("hover_pressed", ThemeFactory.Box(UiTokens.AccentSoft, 8, radius: 7));
            int speed = s;
            b.Pressed += () => SetSpeed(speed);
            _speedButtons.Add(b);
            segRow.AddChild(b);
        }
        row.AddChild(seg);

        var stats = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        stats.AddThemeConstantOverride("separation", -2);
        _populationLabel = ThemeFactory.Text("", UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Numbers);
        _speedLabel = ThemeFactory.Text("", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers);
        stats.AddChild(_populationLabel);
        stats.AddChild(_speedLabel);
        stats.CustomMinimumSize = new Vector2(96, 0);
        row.AddChild(stats);

        _fpsLabel = ThemeFactory.Text("", UiTokens.FontCaption, UiTokens.TextDisabled, UiFonts.Numbers);
        _fpsLabel.CustomMinimumSize = new Vector2(48, 0);
        row.AddChild(_fpsLabel);
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

    // ---------- Inspecao ----------

    private void BuildInspector()
    {
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop };
        panel.AddThemeStyleboxOverride("panel", ThemeFactory.Glass(UiTokens.S24));
        panel.SetAnchorsPreset(Control.LayoutPreset.RightWide);
        panel.OffsetLeft = -UiTokens.InspectorWidth - UiTokens.S16;
        panel.OffsetRight = -UiTokens.S16;
        panel.OffsetTop = UiTokens.TopBarHeight + UiTokens.S16;
        panel.OffsetBottom = -UiTokens.S16;
        _ui.AddChild(panel);

        var scroll = new ScrollContainer { HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled };
        panel.AddChild(scroll);
        _inspector = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        _inspector.AddThemeConstantOverride("separation", UiTokens.S8);
        scroll.AddChild(_inspector);
        ShowEmptyInspector();
    }

    private void ClearInspector()
    {
        foreach (var c in _inspector.GetChildren()) c.QueueFree();
        _inspectorCount = null;
    }

    private void ShowEmptyInspector()
    {
        ClearInspector();
        _inspector.AddChild(ThemeFactory.Section("Inspeção"));
        _inspector.AddChild(ThemeFactory.Text("Explore o mundo", UiTokens.FontHeading, UiTokens.TextPrimary, UiFonts.Display));
        var hint = ThemeFactory.Text("Clique em uma região para ver serviços, rotas e atividades oferecidas, ou em uma pessoa para abrir a ficha.",
            UiTokens.FontBody, UiTokens.TextSecondary);
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _inspector.AddChild(hint);
        _inspector.AddChild(new HSeparator());
        _inspector.AddChild(ThemeFactory.Section("Atalhos"));
        foreach (var (k, d) in new[] { ("Espaço", "pausar / continuar"), ("1 – 4", "velocidade"), ("Roda", "zoom"), ("Arrastar", "mover o mapa"), ("Duplo clique", "focar região"), ("Esc", "limpar seleção") })
            _inspector.AddChild(KeyRow(k, d));
    }

    private static HBoxContainer KeyRow(string key, string desc)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", UiTokens.S12);
        var keyChip = new PanelContainer();
        var sb = ThemeFactory.Box(UiTokens.SurfaceRaised, 8, border: UiTokens.BorderStrong, radius: 6);
        sb.ContentMarginTop = 2; sb.ContentMarginBottom = 2;
        sb.BorderWidthBottom = 2;
        keyChip.AddThemeStyleboxOverride("panel", sb);
        keyChip.AddChild(ThemeFactory.Text(key, UiTokens.FontCaption, UiTokens.TextPrimary, UiFonts.SemiBold));
        keyChip.CustomMinimumSize = new Vector2(96, 0);
        row.AddChild(keyChip);
        row.AddChild(ThemeFactory.Text(desc, UiTokens.FontSmall, UiTokens.TextSecondary));
        return row;
    }

    private void OnRegionSelected(string regionId)
    {
        if (_host == null) return;
        if (string.IsNullOrEmpty(regionId) || !_host.Content.Regions.TryGetValue(regionId, out var r))
        {
            ShowEmptyInspector();
            return;
        }

        ClearInspector();
        bool city = r.Kind == RegionKinds.City;
        _inspector.AddChild(ThemeFactory.Section(city ? "Cidade" : "Área"));
        _inspector.AddChild(ThemeFactory.Text(r.Name, UiTokens.FontHeading, UiTokens.TextPrimary, UiFonts.Display));

        var chips = new HFlowContainer();
        chips.AddThemeConstantOverride("h_separation", UiTokens.S8);
        chips.AddThemeConstantOverride("v_separation", UiTokens.S8);
        chips.AddChild(ThemeFactory.Chip(city ? "Zona segura" : PtBr.Biome(r.Biome), city ? UiTokens.Success : UiTokens.BiomeColor(r.Biome).Lightened(0.3f), dot: true));
        chips.AddChild(ThemeFactory.Chip(PtBr.Pvp(r.Pvp), r.Pvp == PvpRules.Open ? UiTokens.Danger : UiTokens.TextSecondary));
        chips.AddChild(ThemeFactory.Chip($"Níveis {r.LevelMin}–{r.LevelMax}", UiTokens.Info));
        _inspector.AddChild(chips);

        _inspector.AddChild(new HSeparator());
        _inspector.AddChild(ThemeFactory.Section("Agora"));
        var stat = new HBoxContainer();
        stat.AddThemeConstantOverride("separation", UiTokens.S8);
        _inspectorCount = ThemeFactory.Text("0", 28, UiTokens.TextPrimary, UiFonts.Numbers);
        stat.AddChild(_inspectorCount);
        var statText = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        statText.AddThemeConstantOverride("separation", -2);
        statText.AddChild(ThemeFactory.Text("pessoas no local", UiTokens.FontSmall, UiTokens.TextPrimary));
        statText.AddChild(ThemeFactory.Text($"de {PtBr.Int(r.Capacity)} de capacidade · demonstração", UiTokens.FontCaption, UiTokens.TextSecondary));
        stat.AddChild(statText);
        _inspector.AddChild(stat);
        UpdateInspectorCount();

        _inspector.AddChild(new HSeparator());
        _inspector.AddChild(ThemeFactory.Section("Rotas"));
        foreach (var c in r.Connections)
        {
            string name = _host.Content.Regions.TryGetValue(c.To, out var other) ? other.Name : c.To;
            var link = new Button
            {
                Text = $"{name}   ·   {c.TravelMinutes} min",
                Alignment = HorizontalAlignment.Left,
                CustomMinimumSize = new Vector2(0, UiTokens.ControlHeight),
                TooltipText = $"Ir para {name}",
                FocusMode = Control.FocusModeEnum.None,
            };
            string target = c.To;
            link.Pressed += () => { _map.SelectRegion(target); _map.FocusRegion(target); OnRegionSelected(target); };
            _inspector.AddChild(link);
        }

        _inspector.AddChild(new HSeparator());
        _inspector.AddChild(ThemeFactory.Section($"Atividades oferecidas · {r.Activities.Count}"));
        if (r.Activities.Count == 0)
            _inspector.AddChild(ThemeFactory.Text("Nenhuma atividade: jogadores não terão motivo para vir.", UiTokens.FontBody, UiTokens.Warning));
        foreach (var a in r.Activities)
            _inspector.AddChild(ActivityCard(a));

        _inspector.AddChild(new HSeparator());
        var observe = new Button
        {
            Text = "Observar em 3D",
            Disabled = true,
            TooltipText = "Disponível a partir do M4 (observação 3D).",
            CustomMinimumSize = new Vector2(0, UiTokens.ControlHeight + 6),
        };
        _inspector.AddChild(observe);
    }

    private static PanelContainer ActivityCard(ActivityDefinition a)
    {
        var card = new PanelContainer();
        var sb = ThemeFactory.Box(new Color(UiTokens.SurfaceRaised, 0.7f), 12, border: UiTokens.Border, radius: 8);
        sb.ContentMarginTop = sb.ContentMarginBottom = 9;
        sb.BorderWidthLeft = 3;
        sb.BorderColor = ActivityColor(a.Type);
        card.AddThemeStyleboxOverride("panel", sb);
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 2);
        var head = new HBoxContainer();
        var title = ThemeFactory.Text(a.Name, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Medium);
        title.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        head.AddChild(title);
        head.AddChild(ThemeFactory.Text(PtBr.ActivityType(a.Type).ToUpperInvariant(), UiTokens.FontCaption, ActivityColor(a.Type), UiFonts.SemiBold));
        box.AddChild(head);
        string group = a.MinGroupSize == a.MaxGroupSize ? $"grupo {a.MinGroupSize}" : $"grupo {a.MinGroupSize}–{a.MaxGroupSize}";
        string risk = a.BaseRiskPermille > 0 ? $" · risco {a.BaseRiskPermille / 10}%" : "";
        var meta = ThemeFactory.Text($"nível {a.MinLevel}+ · {group} · ~{a.TypicalMinutes} min{risk}", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers);
        meta.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        box.AddChild(meta);
        card.AddChild(box);
        return card;
    }

    private static Color ActivityColor(string type) => type switch
    {
        ActivityTypes.Hunt or ActivityTypes.Boss => UiTokens.Danger,
        ActivityTypes.Ambush or ActivityTypes.Patrol => UiTokens.Warning,
        ActivityTypes.Gather or ActivityTypes.Explore => UiTokens.Success,
        ActivityTypes.Trade or ActivityTypes.Repair => UiTokens.Accent,
        _ => UiTokens.Info,
    };

    private void OnPersonSelected(int personId)
    {
        if (_host == null || personId < 1 || personId > _host.Persons.Count) { ShowEmptyInspector(); return; }
        var p = _host.Persons[personId - 1];
        var profileName = _host.Content.Profiles.TryGetValue(p.ProfileId, out var pr) ? pr.Name : p.ProfileId;
        var color = UiTokens.ProfileColor(p.ProfileId);
        var t = p.Traits;

        ClearInspector();
        _inspector.AddChild(ThemeFactory.Section($"Pessoa #{p.Id}"));
        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", UiTokens.S12);
        head.AddChild(new PortraitBadge(p.AppearanceSeed, color) { CustomMinimumSize = new Vector2(56, 56) });
        var names = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        names.AddThemeConstantOverride("separation", 2);
        names.AddChild(ThemeFactory.Text(p.Name, 21, UiTokens.TextPrimary, UiFonts.SemiBold));
        var chips = new HBoxContainer();
        chips.AddThemeConstantOverride("separation", 6);
        chips.AddChild(ThemeFactory.Chip(profileName, color, dot: true));
        chips.AddChild(ThemeFactory.Chip("Potencial interessado", UiTokens.TextSecondary));
        names.AddChild(chips);
        head.AddChild(names);
        _inspector.AddChild(head);

        var note = ThemeFactory.Text("Parâmetros reais gerados pelo Core. A posição no mapa ainda é demonstração.", UiTokens.FontCaption, UiTokens.TextSecondary);
        note.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _inspector.AddChild(note);

        _inspector.AddChild(new HSeparator());
        _inspector.AddChild(ThemeFactory.Section("Rotina"));
        var routine = new HBoxContainer();
        routine.AddThemeConstantOverride("separation", UiTokens.S8);
        routine.AddChild(StatTile($"{t.PreferredStartHour:00}h", "início preferido"));
        routine.AddChild(StatTile($"{t.SessionMinutes} min", "sessão típica"));
        routine.AddChild(StatTile($"{t.DaysPerWeek}/7", "dias por semana"));
        _inspector.AddChild(routine);

        AddTraitGroup("Estilo de jogo", new[] { ("PvE", t.Pve), ("PvP", t.Pvp), ("Comércio", t.Trade), ("Exploração", t.Exploration), ("Social", t.Socializing) });
        AddTraitGroup("Personalidade", new[] { ("Sociabilidade", t.Sociability), ("Agressividade", t.Aggressiveness), ("Paciência", t.Patience), ("Lealdade", t.Loyalty), ("Aversão a risco", t.RiskAversion) });
        AddTraitGroup("Capacidade", new[] { ("Habilidade mecânica", t.MechanicalSkill), ("Conhecimento do jogo", t.GameKnowledge), ("Coordenação", t.Coordination) });

        _inspector.AddChild(new HSeparator());
        _inspector.AddChild(ThemeFactory.Section("Finanças"));
        var money = new HBoxContainer();
        money.AddThemeConstantOverride("separation", UiTokens.S8);
        money.AddChild(StatTile($"R$ {PtBr.Decimal(t.LeisureBudgetCentsPerMonth / 100.0, 0)}", "lazer por mês"));
        money.AddChild(StatTile($"{t.WillingnessToPay / 10}%", "disposição a pagar"));
        _inspector.AddChild(money);
    }

    private void AddTraitGroup(string title, (string Name, int Value)[] traits)
    {
        _inspector.AddChild(new HSeparator());
        _inspector.AddChild(ThemeFactory.Section(title));
        foreach (var (name, value) in traits)
        {
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", UiTokens.S8);
            var label = ThemeFactory.Text(name, UiTokens.FontSmall, UiTokens.TextPrimary);
            label.CustomMinimumSize = new Vector2(150, 0);
            row.AddChild(label);
            var bar = new ProgressBar
            {
                MinValue = 0, MaxValue = 1000, Value = value, ShowPercentage = false,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
                CustomMinimumSize = new Vector2(0, 7),
                TooltipText = $"{name}: {value} de 1000",
            };
            var fill = ThemeFactory.Box(TraitColor(value), 0, radius: 4);
            fill.ContentMarginTop = fill.ContentMarginBottom = 0;
            bar.AddThemeStyleboxOverride("fill", fill);
            row.AddChild(bar);
            var num = ThemeFactory.Text((value / 10).ToString(CultureInfo.InvariantCulture), UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers);
            num.CustomMinimumSize = new Vector2(26, 0);
            num.HorizontalAlignment = HorizontalAlignment.Right;
            row.AddChild(num);
            _inspector.AddChild(row);
        }
    }

    private static Color TraitColor(int v) => v >= 700 ? UiTokens.Accent : v >= 400 ? UiTokens.Info : new Color(UiTokens.TextSecondary, 0.8f);

    private static PanelContainer StatTile(string value, string caption)
    {
        var tile = new PanelContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var sb = ThemeFactory.Box(new Color(UiTokens.SurfaceRaised, 0.7f), 10, border: UiTokens.Border, radius: 8);
        sb.ContentMarginTop = sb.ContentMarginBottom = 8;
        tile.AddThemeStyleboxOverride("panel", sb);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 0);
        col.AddChild(ThemeFactory.Text(value, 17, UiTokens.TextPrimary, UiFonts.Numbers));
        col.AddChild(ThemeFactory.Text(caption, UiTokens.FontCaption, UiTokens.TextSecondary));
        tile.AddChild(col);
        return tile;
    }

    private void UpdateInspectorCount()
    {
        if (_inspectorCount == null || _map.SelectedRegionId == null) return;
        _inspectorCount.Text = PtBr.Int(_map.CountIn(_map.SelectedRegionId));
    }

    // ---------- Legenda e controles do mapa ----------

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
        col.AddChild(ThemeFactory.Section("População por perfil"));

        var profiles = new HFlowContainer { CustomMinimumSize = new Vector2(520, 0) };
        profiles.AddThemeConstantOverride("h_separation", UiTokens.S16);
        profiles.AddThemeConstantOverride("v_separation", 4);
        foreach (var p in content.ProfilesOrdered)
        {
            int count = _host!.Persons.Count(x => x.ProfileId == p.Id);
            profiles.AddChild(LegendItem(UiTokens.ProfileColor(p.Id), p.Name, count.ToString(CultureInfo.InvariantCulture)));
        }
        col.AddChild(profiles);
    }

    private static HBoxContainer LegendItem(Color color, string text, string count)
    {
        var item = new HBoxContainer();
        item.AddThemeConstantOverride("separation", 6);
        item.AddChild(new DotIcon(color) { CustomMinimumSize = new Vector2(12, 12), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter });
        item.AddChild(ThemeFactory.Text(text, UiTokens.FontSmall, UiTokens.TextPrimary));
        item.AddChild(ThemeFactory.Text(count, UiTokens.FontSmall, UiTokens.TextSecondary, UiFonts.Numbers));
        return item;
    }

    private void BuildMapControls()
    {
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Stop };
        var sb = ThemeFactory.Glass(4, 10);
        panel.AddThemeStyleboxOverride("panel", sb);
        panel.SetAnchorsPreset(Control.LayoutPreset.BottomRight);
        panel.GrowHorizontal = Control.GrowDirection.Begin;
        panel.GrowVertical = Control.GrowDirection.Begin;
        panel.OffsetRight = -(UiTokens.InspectorWidth + UiTokens.S16 * 2);
        panel.OffsetBottom = -UiTokens.S16;
        _ui.AddChild(panel);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 2);
        panel.AddChild(col);
        foreach (var (text, tip, action) in new (string, string, Action)[]
        {
            ("+", "Aproximar (roda do mouse)", () => _map.ZoomBy(1.3f)),
            ("−", "Afastar (roda do mouse)", () => _map.ZoomBy(1f / 1.3f)),
            ("⤢", "Enquadrar o mundo", () => FitMap(false)),
        })
        {
            var b = new Button { Text = text, TooltipText = tip, CustomMinimumSize = new Vector2(38, 38), FocusMode = Control.FocusModeEnum.None };
            b.AddThemeFontSizeOverride("font_size", 18);
            b.AddThemeStyleboxOverride("normal", ThemeFactory.Box(new Color(0, 0, 0, 0), 6, radius: 8));
            b.Pressed += action;
            col.AddChild(b);
        }
    }

    // ---------- Erro ----------

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
        if (_host == null || _fixture == null) { HandleAutomation(delta); return; }

        var snap = _host.Snapshot;
        _fixture.Update(snap.Tick);
        var now = _host.Calendar.ToDateTime(snap.Tick);
        _map.SetHour(now.Hour + now.Minute / 60.0 + now.Second / 3600.0);

        _uiTimer += delta;
        if (_uiTimer >= 0.2 || _frames < 3)
        {
            _uiTimer = 0;
            _clockLabel.Text = $"{now.Hour:00}:{now.Minute:00}";
            _dateLabel.Text = $"{PtBr.Date(now)} · {PtBr.DayKind(_host.Calendar.KindOf(now.Date))}";
            _speedLabel.Text = snap.RequestedSpeed == 0 ? "pausado"
                : snap.EffectiveSpeed < 0 ? "medindo…"
                : $"efetiva {PtBr.Decimal(snap.EffectiveSpeed)}×";
            _populationLabel.Text = $"{PtBr.Int(snap.PersonsTotal)} pessoas";
            _fpsLabel.Text = $"{Engine.GetFramesPerSecond():0} fps";
            UpdateServerChip(snap.ServerOpen);
            UpdateInspectorCount();
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
        if (_args.TryGetValue("screenshot", out var path))
        {
            int frames = int.Parse(Arg("frames", "45"), CultureInfo.InvariantCulture);
            if (_frames == frames)
            {
                var img = GetViewport().GetTexture().GetImage();
                var full = System.IO.Path.GetFullPath(path);
                System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
                var err = img.SavePng(full);
                GD.Print(err == Error.Ok ? $"CAPTURA_OK {full} {img.GetWidth()}x{img.GetHeight()}" : $"CAPTURA_ERRO {err}");
                Quit();
            }
        }
        if (_args.TryGetValue("measure-seconds", out var secs))
        {
            double limit = double.Parse(secs, CultureInfo.InvariantCulture);
            if (_frames > 30)
            {
                _measureElapsed += delta;
                _measureFrames++;
            }
            if (_measureElapsed >= limit)
            {
                double fps = _measureFrames / _measureElapsed;
                GD.Print($"FPS_MEDIO {fps:F1} quadros={_measureFrames} segundos={_measureElapsed:F2} populacao={_host?.Persons.Count ?? 0}");
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
            case Key.Escape:
                _map.SelectRegion(null);
                _map.SelectPerson(0);
                ShowEmptyInspector();
                break;
            default: return;
        }
        GetViewport().SetInputAsHandled();
    }

    public override void _ExitTree() => _host?.Dispose();
}

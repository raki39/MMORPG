using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Simulation.Core.Content;
using Simulation.Core.Population;
using Simulation.Infrastructure.Content;
using GodotClient.Map;

namespace GodotClient.Ui;

/// <summary>
/// Ficha da zona: situacao ao vivo, jogadores presentes, regras e configuracao. A aba de
/// configuracao edita um RASCUNHO validado pelo Core; nada muda no mundo ativo ate a
/// publicacao por atualizacao (M7) ou modo sandbox.
/// </summary>
public partial class ZoneModal : ModalFrame
{
    private readonly ClientContext _ctx;
    private readonly WorldMapView _map;
    private readonly RegionDefinition _zone;
    private ZoneDraft _draft;
    private VBoxContainer? _issuesBox;

    public ZoneModal(ClientContext ctx, WorldMapView map, string zoneId)
        : base(ZoneAccent(ctx.Content.Regions[zoneId]))
    {
        _ctx = ctx;
        _map = map;
        _zone = ctx.Content.Regions[zoneId];
        _draft = ctx.Drafts.TryGetValue(zoneId, out var d) ? d : ZoneDraft.From(_zone);
        BuildHeader();
        Breadcrumb = $"Mundo › {_zone.Name}";
        AddTab("Visão geral", BuildOverview, live: true);
        AddTab("Jogadores", BuildPlayers, live: true);
        AddTab("Regras", BuildRules);
        AddTab("Configuração", BuildConfig);
    }

    private static Color ZoneAccent(RegionDefinition z) =>
        z.IsCity ? UiTokens.Accent : z.Pvp == PvpRules.Open ? UiTokens.Danger : UiTokens.Success;

    private void BuildHeader()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 18);
        var emblem = new ZoneEmblem(_zone) { CustomMinimumSize = new Vector2(52, 52), SizeFlagsVertical = SizeFlags.ShrinkCenter };
        emblem.CustomMinimumSize = new Vector2(84, 84);
        Art.AddChild(emblem);
        var col = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        col.AddThemeConstantOverride("separation", 2);
        col.AddChild(ThemeFactory.Text(_zone.Name, 28, UiTokens.TextPrimary, UiFonts.SemiBold));
        string sub = _zone.IsCity ? $"{PtBr.Function(_zone.Function)} nível {_zone.CityLevel} · {PtBr.Biome(_zone.Biome)} · todos os níveis"
            : $"{PtBr.Function(_zone.Function)} · {PtBr.Biome(_zone.Biome)} · níveis {_zone.LevelMin}–{_zone.LevelMax}";
        col.AddChild(ThemeFactory.Text(sub, UiTokens.FontBody, UiTokens.TextSecondary));
        if (_ctx.Drafts.TryGetValue(_zone.Id, out var d) && d.Saved && d.DiffersFrom(_zone))
            col.AddChild(ThemeFactory.Chip("Rascunho não publicado", UiTokens.Warning, dot: true));
        row.AddChild(col);
        Header.AddChild(row);

        int people = _map.PeopleInZone(_zone.Id);
        Fact("Jogadores", people.ToString(), UiTokens.Info, "Jogadores na zona agora\n~Inclui quem está andando pelas estradas da zona.");
        Fact("Lotação", $"{people * 100 / Math.Max(1, _zone.Capacity)}%", WorldMapView.Ramp(Mathf.Clamp(people / (float)Math.Max(1, _zone.Capacity) * 4f, 0, 1)), $"Lotação\nJogadores|{people}\nCapacidade|{_zone.Capacity}");
        Fact("Níveis", _zone.IsCity ? "Todos" : $"{_zone.LevelMin}–{_zone.LevelMax}", UiTokens.AccentSoft);
        Fact("Combate", PtBr.Pvp(_zone.Pvp), _zone.Pvp == PvpRules.Open ? UiTokens.Danger : UiTokens.Success);
        Fact("Morte", PtBr.Death(_zone.DeathRule), _zone.DeathRule == DeathRules.FullLoot ? UiTokens.Danger : _zone.DeathRule == DeathRules.Partial ? UiTokens.Warning : UiTokens.TextSecondary);
        Fact("Controle", PtBr.Control(_zone.Control));
        Fact("Perigo", ((int)(WorldMapView.DangerOf(_zone) * 100)).ToString(), WorldMapView.Ramp(WorldMapView.DangerOf(_zone)), "Índice de perigo\n~Risco das atividades, PvP e regra de morte.");
        Fact("Áreas", _zone.Areas.Count.ToString());

        Actions.AddChild(Action("Aproximar no mapa", "Centraliza e aproxima a zona", () => { _ctx.FocusZone(_zone.Id); Close(); }, primary: true));
        Actions.AddChild(Action("Observar em 3D", "Disponível a partir do M4", () => { }, enabled: false));
    }

    // ---------------- Visao geral (kit D-034) ----------------

    private Control BuildOverview()
    {
        var snap = _ctx.Snapshot();
        var here = new List<Sim.CharacterView>();
        for (int id = 1; id < snap.ByPerson.Length; id++)
            if (snap.ByPerson[id] is { } v && v.Status != CharacterStatus.Offline && v.RegionId == _zone.Id) here.Add(v);
        int people = _map.PeopleInZone(_zone.Id);

        var row1 = Grid(12,
            (Card("Agora na zona", NowBody(people), info: "Situação ao vivo\n~Lotação contra a capacidade, índice de perigo e o que as pessoas estão fazendo aqui."), 3),
            (Card($"Áreas · {_zone.Areas.Count}", AreasBody(), info: "Áreas da zona\n~Clique numa área para abrir a página dela. O número à direita é quem está lá agora."), 6),
            (Card("Regras e acesso", RulesBody(), info: "Regras da zona\n~Combate, morte e controle valem para a zona inteira. As saídas levam às zonas vizinhas pela estrada."), 3));

        var series = _ctx.Metrics.OnlineByZone.TryGetValue(_zone.Id, out var zs) ? zs.ToList() : new List<float>();
        var chart = new LineChart
        {
            CustomMinimumSize = new Vector2(0, 190), Dots = false,
            XLabels = _ctx.Metrics.Ticks.Select(t => _ctx.Clock(t)).ToList(),
            Series = new[] { new ChartSeries { Name = "Jogadores na zona", Color = UiTokens.Info, Values = series } },
        };
        var row2 = Grid(12,
            (Card("Movimento · últimas 24 h", chart, info: "Jogadores na zona\n~Amostras a cada 10 minutos de jogo."), 6),
            (Card("Níveis de quem está aqui", LevelsBody(here), info: "Distribuição de níveis\n~Verde: dentro da faixa da zona. Laranja: fora dela."), 3),
            (Card("Quem está aqui", WhoBody(here), info: "Jogadores conectados na zona\n~Os de nível mais alto primeiro. Lista completa na aba Jogadores."), 3));
        return Column(12, row1, row2);
    }

    private Control NowBody(int people)
    {
        float load = people / (float)Math.Max(1, _zone.Capacity);
        float danger = WorldMapView.DangerOf(_zone);
        var rings = Row(18,
            Column(4, new RingGauge { CustomMinimumSize = new Vector2(84, 84), SizeFlagsHorizontal = SizeFlags.ShrinkCenter, Value = Mathf.Clamp(load * 4f, 0, 1), Color = WorldMapView.Ramp(Mathf.Clamp(load * 4f, 0, 1)), Center = $"{load * 100:0}%", Caption = "lotação" }),
            Column(4, new RingGauge { CustomMinimumSize = new Vector2(84, 84), SizeFlagsHorizontal = SizeFlags.ShrinkCenter, Value = danger, Color = WorldMapView.Ramp(danger), Center = $"{danger * 100:0}", Caption = "perigo" }),
            Column(2, ThemeFactory.Text(people.ToString(), 28, UiTokens.TextPrimary, UiFonts.Numbers), ThemeFactory.Text("jogadores agora", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium),
                ThemeFactory.Text($"capacidade {_zone.Capacity}", UiTokens.FontCaption, UiTokens.TextDisabled)));
        foreach (var c in rings.GetChildren().OfType<Control>()) c.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        var col = Column(12, rings, Section("Fazendo agora"));
        var acts = _zone.AllActivities().Select(a => (A: a, N: _map.PeopleAtSpot(_zone.Id, a.Id))).Where(x => x.N > 0).OrderByDescending(x => x.N).Take(5).ToList();
        if (acts.Count == 0) col.AddChild(ThemeFactory.Text("Ninguém em atividade aqui agora.", UiTokens.FontSmall, UiTokens.TextDisabled));
        int max = acts.Count == 0 ? 1 : acts.Max(x => x.N);
        foreach (var (a, n) in acts)
        {
            var r = new HBoxContainer();
            r.AddThemeConstantOverride("separation", 10);
            r.AddChild(new Icon(PlayerModal.TypeIcon(a.Type), 16, ActivityPalette.Color(a.Type)));
            var c = Column(4);
            var top = new HBoxContainer();
            var nm = ThemeFactory.Text(a.Name, UiTokens.FontSmall, UiTokens.TextLabel, UiFonts.Medium);
            nm.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            top.AddChild(nm);
            top.AddChild(ThemeFactory.Text(n.ToString(), UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Numbers));
            c.AddChild(top);
            c.AddChild(new ThinBar(n / (float)max, ActivityPalette.Color(a.Type)) { CustomMinimumSize = new Vector2(0, 3) });
            r.AddChild(c);
            col.AddChild(r);
        }
        return col;
    }

    private Control AreasBody()
    {
        var col = Column(2);
        foreach (var area in _zone.Areas.OrderByDescending(a => _map.PeopleInArea(_zone.Id, a.Id).Count).ThenBy(a => a.Name, StringComparer.Ordinal))
        {
            string zid = _zone.Id, aid = area.Id;
            int n = _map.PeopleInArea(_zone.Id, area.Id).Count;
            var b = HoverRow(() => _ctx.OpenArea(zid, aid), $"{area.Name}\nTipo|{AreaPalette.Name(area.Kind)}\nAgora|{n}\nTamanho|{area.Radius * 20} m\n~{area.Description}");
            b.CustomMinimumSize = new Vector2(0, 52);
            var r = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            r.SetAnchorsPreset(LayoutPreset.FullRect);
            r.OffsetLeft = 8; r.OffsetRight = -10;
            r.AddThemeConstantOverride("separation", 12);
            r.AddChild(new KindEmblem(area.Kind) { CustomMinimumSize = new Vector2(34, 34), SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore });
            var info = Column(1);
            info.MouseFilter = MouseFilterEnum.Ignore;
            info.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            info.AddChild(ThemeFactory.Text(area.Name, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Medium));
            string tasks = area.Activities.Count == 0
                ? (AreaKinds.IsPortal(area.Kind) ? $"Leva a {area.LeadsTo} (em breve)" : AreaPalette.Name(area.Kind))
                : $"{AreaPalette.Name(area.Kind)} · " + string.Join(" · ", area.Activities.Select(a => a.Name));
            var sub = ThemeFactory.Text(tasks, UiTokens.FontSmall, UiTokens.TextSecondary);
            sub.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            sub.CustomMinimumSize = new Vector2(120, 0);
            info.AddChild(sub);
            r.AddChild(info);
            var count = ThemeFactory.Text(n.ToString(), 18, n > 0 ? UiTokens.TextPrimary : UiTokens.TextDisabled, UiFonts.Numbers);
            count.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            r.AddChild(count);
            r.AddChild(new Icon("chevron-right", 14, UiTokens.TextDisabled));
            b.AddChild(r);
            col.AddChild(b);
        }
        if (_zone.Activities.Count > 0)
            col.AddChild(ThemeFactory.Text("Na zona inteira: " + string.Join(" · ", _zone.Activities.Select(a => $"{a.Name} ({_map.PeopleAtSpot(_zone.Id, a.Id)})")), UiTokens.FontSmall, UiTokens.TextSecondary));
        return col;
    }

    private Control RulesBody()
    {
        Control Line(string icon, string key, string value, Color color)
        {
            var r = new HBoxContainer();
            r.AddThemeConstantOverride("separation", 10);
            r.AddChild(new Icon(icon, 16, color));
            var k = ThemeFactory.Text(key, UiTokens.FontSmall, UiTokens.TextSecondary);
            k.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            r.AddChild(k);
            r.AddChild(ThemeFactory.Text(value, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Medium));
            return r;
        }
        var col = Column(9,
            Line("swords", "Combate", PtBr.Pvp(_zone.Pvp), _zone.Pvp == PvpRules.Open ? UiTokens.Danger : UiTokens.Success),
            Line("skull", "Morte", PtBr.Death(_zone.DeathRule), _zone.DeathRule == DeathRules.FullLoot ? UiTokens.Danger : _zone.DeathRule == DeathRules.Partial ? UiTokens.Warning : UiTokens.TextSecondary),
            Line("flag", "Controle", PtBr.Control(_zone.Control), _zone.Control == ControlRules.Contestable ? UiTokens.Warning : UiTokens.TextSecondary),
            Line("trending-up", "Níveis", _zone.IsCity ? "Todos" : $"{_zone.LevelMin}–{_zone.LevelMax}", UiTokens.Accent),
            Line("users", "Capacidade", _zone.Capacity.ToString(), UiTokens.Info),
            Line("trees", "Bioma", PtBr.Biome(_zone.Biome), UiTokens.BiomeColor(_zone.Biome).Lightened(0.3f)));
        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 2) });
        col.AddChild(Section("Saídas"));
        foreach (var c in _zone.Connections)
        {
            var other = _ctx.Content.Regions[c.To];
            var L = _ctx.Layout;
            int dist = L.LegLength(L.Hub(_zone.Id), L.Gate(_zone.Id, c.To)) + L.Gate(_zone.Id, c.To).DistanceTo(L.Gate(c.To, _zone.Id))
                       + L.LegLength(L.Gate(c.To, _zone.Id), L.Hub(c.To));
            int minutes = (dist + Simulation.Core.Behavior.BehaviorTuning.WalkSpeed - 1) / Simulation.Core.Behavior.BehaviorTuning.WalkSpeed;
            string target = c.To;
            var b = HoverRow(() => _ctx.OpenZone(target), $"Saída para {other.Name}\nCaminhada até o centro de lá|~{minutes} min\nDistância|{dist * 10} m pela estrada\nJogadores lá|{_map.PeopleInZone(c.To)}\nCombate|{PtBr.Pvp(other.Pvp)}\n~Clique para abrir a zona.");
            b.CustomMinimumSize = new Vector2(0, 30);
            var r = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            r.SetAnchorsPreset(LayoutPreset.FullRect);
            r.OffsetLeft = 6; r.OffsetRight = -6;
            r.AddThemeConstantOverride("separation", 10);
            r.AddChild(new Icon("arrow-up-right", 14, UiTokens.AccentSoft));
            var nm = ThemeFactory.Text(other.Name, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Medium);
            nm.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            nm.VerticalAlignment = VerticalAlignment.Center;
            r.AddChild(nm);
            var t = ThemeFactory.Text($"~{minutes} min", UiTokens.FontSmall, UiTokens.TextSecondary, UiFonts.Numbers);
            t.VerticalAlignment = VerticalAlignment.Center;
            r.AddChild(t);
            b.AddChild(r);
            col.AddChild(b);
        }
        return col;
    }

    private Control LevelsBody(List<Sim.CharacterView> here)
    {
        if (here.Count == 0) return ThemeFactory.Text("Ninguém conectado nesta zona agora.", UiTokens.FontSmall, UiTokens.TextDisabled);
        int lo = Math.Max(1, Math.Min(here.Min(v => v.Level), _zone.IsCity ? 99 : _zone.LevelMin) - 1);
        int hi = Math.Max(here.Max(v => v.Level), _zone.IsCity ? 0 : _zone.LevelMax) + 1;
        hi = Math.Min(hi, _ctx.Content.Progression.LevelCap);
        var values = Enumerable.Range(lo, hi - lo + 1).Select(l => (float)here.Count(v => v.Level == l)).ToList();
        bool In(int l) => _zone.IsCity || (l >= _zone.LevelMin && l <= _zone.LevelMax);
        var chart = new ColumnChart
        {
            CustomMinimumSize = new Vector2(0, 160), CountScale = true, ShowAverage = false, Unit = " jogadores",
            Values = values, Labels = Enumerable.Range(lo, hi - lo + 1).Select(l => $"Nv {l}").ToList(),
        };
        var levels = Enumerable.Range(lo, hi - lo + 1).ToList();
        chart.ColorAt = i => In(levels[i]) ? UiTokens.Success : UiTokens.Warning;
        int inside = here.Count(v => In(v.Level));
        return Column(12, chart, Row(0,
            Mini($"{here.Average(v => v.Level):0.#}".Replace('.', ','), "Nível médio"),
            Mini($"{inside * 100 / here.Count}%", "Na faixa da zona"),
            Mini(here.Max(v => v.Level).ToString(), "Mais alto")));
    }

    private static Control Mini(string value, string caption)
    {
        var c = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        c.AddThemeConstantOverride("separation", 0);
        c.AddChild(ThemeFactory.Text(value, 20, UiTokens.TextPrimary, UiFonts.Numbers));
        c.AddChild(ThemeFactory.Text(caption, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium));
        return c;
    }

    private Control WhoBody(List<Sim.CharacterView> here)
    {
        var col = Column(2);
        if (here.Count == 0) { col.AddChild(ThemeFactory.Text("Ninguém conectado nesta zona agora.", UiTokens.FontSmall, UiTokens.TextDisabled)); return col; }
        foreach (var v in here.OrderByDescending(x => x.Level).ThenBy(x => x.Name, StringComparer.Ordinal).Take(6))
        {
            var p = _ctx.Persons[v.PersonId - 1];
            int pid = v.PersonId;
            var b = HoverRow(() => _ctx.OpenPerson(pid), $"{v.Name}\n~Clique para abrir a ficha.");
            b.CustomMinimumSize = new Vector2(0, 40);
            var r = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            r.SetAnchorsPreset(LayoutPreset.FullRect);
            r.OffsetLeft = 6; r.OffsetRight = -6;
            r.AddThemeConstantOverride("separation", 10);
            r.AddChild(new PortraitBadge(p.AppearanceSeed, UiTokens.ProfileColor(p.ProfileId), v.Name) { CustomMinimumSize = new Vector2(28, 28), SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore });
            var info = Column(0);
            info.MouseFilter = MouseFilterEnum.Ignore;
            info.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            string cls = _ctx.Content.Classes.TryGetValue(v.ClassId, out var c) ? c.Name : v.ClassId;
            info.AddChild(ThemeFactory.Text($"{v.Name}", UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Medium));
            var st = ThemeFactory.Text(StatusText.Of(v, _ctx.Content), UiTokens.FontCaption, UiTokens.TextSecondary);
            st.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            st.CustomMinimumSize = new Vector2(100, 0);
            info.AddChild(st);
            r.AddChild(info);
            var lv = ThemeFactory.Text($"Nv {v.Level} · {cls}", UiTokens.FontSmall, UiTokens.TextLabel, UiFonts.Numbers);
            lv.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            r.AddChild(lv);
            b.AddChild(r);
            col.AddChild(b);
        }
        if (here.Count > 6) col.AddChild(ThemeFactory.Text($"+{here.Count - 6} na aba Jogadores", UiTokens.FontCaption, UiTokens.TextSecondary));
        return col;
    }

    private Control AreaCard(AreaDefinition area)
    {
        var col = AreaPalette.Color(area.Kind);
        var b = new Button { FocusMode = FocusModeEnum.None, SizeFlagsHorizontal = SizeFlags.ExpandFill, TooltipText = $"Abrir {area.Name}" };
        var n = ThemeFactory.Box(new Color("#121A24"), 0, border: new Color(col, 0.35f), radius: 10);
        n.BorderWidthLeft = 3;
        var h = ThemeFactory.Box(new Color("#18222F"), 0, border: new Color(col, 0.9f), radius: 10);
        h.BorderWidthLeft = 3;
        h.ShadowColor = new Color(col, 0.18f);
        h.ShadowSize = 10;
        b.AddThemeStyleboxOverride("normal", n);
        b.AddThemeStyleboxOverride("hover", h);
        b.AddThemeStyleboxOverride("pressed", h);
        var row = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        row.SetAnchorsPreset(LayoutPreset.FullRect);
        row.OffsetLeft = 14; row.OffsetRight = -14; row.OffsetTop = 10; row.OffsetBottom = -10;
        row.AddThemeConstantOverride("separation", 14);
        row.AddChild(new KindEmblem(area.Kind) { CustomMinimumSize = new Vector2(48, 48), MouseFilter = MouseFilterEnum.Ignore, SizeFlagsVertical = SizeFlags.ShrinkCenter });
        var info = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
        info.AddThemeConstantOverride("separation", 3);
        var title = ThemeFactory.Text(area.Name, 16, UiTokens.TextPrimary, UiFonts.SemiBold);
        title.MouseFilter = MouseFilterEnum.Ignore;
        info.AddChild(title);
        string tasks = area.Activities.Count == 0
            ? (AreaKinds.IsPortal(area.Kind) ? $"Leva a: {area.LeadsTo} (em breve)" : "Sem tarefas")
            : string.Join("  ·  ", area.Activities.Select(a => $"{a.Name} ({_map.PeopleAtSpot(_zone.Id, a.Id)})"));
        var sub = ThemeFactory.Text($"{AreaPalette.Name(area.Kind)}  —  {tasks}", UiTokens.FontSmall, UiTokens.TextSecondary);
        sub.MouseFilter = MouseFilterEnum.Ignore;
        sub.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        info.AddChild(sub);
        row.AddChild(info);
        int here = _map.PeopleInArea(_zone.Id, area.Id).Count;
        var count = ThemeFactory.Text(here.ToString(), 24, here > 0 ? UiTokens.Info : UiTokens.TextDisabled, UiFonts.Numbers);
        count.MouseFilter = MouseFilterEnum.Ignore;
        count.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        row.AddChild(count);
        var arrow = ThemeFactory.Text("›", 24, col);
        arrow.MouseFilter = MouseFilterEnum.Ignore;
        arrow.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        row.AddChild(arrow);
        b.AddChild(row);
        b.CustomMinimumSize = new Vector2(0, 74);
        string zid = _zone.Id, aid = area.Id;
        b.Pressed += () => { _ctx.OpenArea(zid, aid); };
        return b;
    }

    private Control TaskLine(ActivityDefinition a)
    {
        int n = _map.PeopleAtSpot(_zone.Id, a.Id);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        row.AddChild(new GlyphIcon(a.Type) { CustomMinimumSize = new Vector2(30, 30) });
        var info = Column(2);
        info.AddChild(ThemeFactory.Text(a.Name, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Medium));
        info.AddChild(ThemeFactory.Text($"{PtBr.ActivityType(a.Type)} · nível {a.MinLevel}+ · ~{a.TypicalMinutes} min", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers));
        row.AddChild(info);
        row.AddChild(ThemeFactory.Text($"{n} agora", UiTokens.FontSmall, n > 0 ? UiTokens.Info : UiTokens.TextDisabled, UiFonts.Numbers));
        return row;
    }

    // ---------------- Jogadores ----------------

    private Control BuildPlayers()
    {
        var snap = _ctx.Snapshot();
        var here = new List<(int Id, Sim.CharacterView V)>();
        for (int id = 1; id < snap.ByPerson.Length; id++)
            if (snap.ByPerson[id] is { } v && v.Status != CharacterStatus.Offline && v.RegionId == _zone.Id) here.Add((id, v));
        var col = Column(6);
        col.AddChild(ThemeFactory.Text($"{here.Count} jogadores conectados nesta zona", UiTokens.FontBody, UiTokens.TextSecondary));
        foreach (var (id, v) in here.OrderByDescending(x => x.V.Level).ThenBy(x => x.V.Name, StringComparer.Ordinal))
            col.AddChild(PlayerRow.Build(_ctx, id, v, () => { _ctx.OpenPerson(id); }));
        return col;
    }

    // ---------------- Regras ----------------

    private Control BuildRules()
    {
        Control Rule(string name, string value, string desc, Color color)
        {
            var c = Column(4);
            c.AddChild(Row(10, ThemeFactory.Text(name, UiTokens.FontSmall, UiTokens.TextSecondary, UiFonts.SemiBold), ThemeFactory.Chip(value, color)));
            var d = ThemeFactory.Text(desc, UiTokens.FontSmall, UiTokens.TextPrimary);
            d.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            c.AddChild(d);
            return c;
        }
        var col = Column(16,
            Rule("Função", PtBr.Function(_zone.Function), _zone.IsCity
                ? "Serviços, missões e convivência. Cidades são sempre seguras e sem perdas."
                : "Conteúdo de caça e coleta. Atrai jogadores do nível indicado.", ZoneAccent(_zone)),
            Rule("Combate", PtBr.Pvp(_zone.Pvp), _zone.Pvp switch
            {
                PvpRules.Open => "Qualquer jogador pode atacar outro. Jogadores avessos a risco tendem a evitar a zona.",
                PvpRules.Consensual => "PvP apenas entre quem aceitou o confronto.",
                _ => "Não há dano entre jogadores.",
            }, _zone.Pvp == PvpRules.Open ? UiTokens.Danger : UiTokens.Success),
            Rule("Morte", PtBr.Death(_zone.DeathRule), _zone.DeathRule switch
            {
                DeathRules.FullLoot => "Ao morrer, o personagem perde o que carrega (detalhes no M9).",
                DeathRules.Partial => "Ao morrer, parte do que carrega é perdida (regras completas no M9).",
                _ => "Morrer não causa perda de itens.",
            }, _zone.DeathRule == DeathRules.None ? UiTokens.TextSecondary : UiTokens.Warning),
            Rule("Controle", PtBr.Control(_zone.Control), _zone.Control == ControlRules.Contestable
                ? "Guildas poderão disputar o domínio da zona (territórios no M9)."
                : "Nenhuma guilda pode dominar esta zona.", _zone.Control == ControlRules.Contestable ? UiTokens.Warning : UiTokens.TextSecondary),
            Rule("Escala", $"{_ctx.Layout.ZoneRadius * 2 * 10} m de diâmetro", "Todas as zonas têm a mesma escala. Andar de uma saída à outra leva tempo real de simulação.", UiTokens.Info));
        return Card("Regras desta zona", col);
    }

    // ---------------- Configuracao (rascunho) ----------------

    private Control BuildConfig()
    {
        var form = Column(12);
        var name = new LineEdit { Text = _draft.Name, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        name.TextChanged += t => { _draft.Name = t; Validate(); };
        form.AddChild(Field("Nome", name));
        form.AddChild(Field("Bioma (visual)", Options(new[] { "planicie", "floresta", "caverna", "ruinas" }, PtBr.Biome, _draft.Biome, v => _draft.Biome = v)));
        form.AddChild(Field("Função", Options(ZoneFunctions.All, PtBr.Function, _draft.Function, v => _draft.Function = v)));
        form.AddChild(Field("Combate", Options(PvpRules.All, PtBr.Pvp, _draft.Pvp, v => _draft.Pvp = v)));
        form.AddChild(Field("Regra de morte", Options(DeathRules.All, PtBr.Death, _draft.DeathRule, v => _draft.DeathRule = v)));
        form.AddChild(Field("Controle", Options(ControlRules.All, PtBr.Control, _draft.Control, v => _draft.Control = v)));
        form.AddChild(Field("Níveis", Row(8, Spin(1, 60, _draft.LevelMin, v => _draft.LevelMin = v), ThemeFactory.Text("até", UiTokens.FontSmall, UiTokens.TextSecondary), Spin(1, 60, _draft.LevelMax, v => _draft.LevelMax = v))));
        form.AddChild(Field("Capacidade", Spin(1, 5000, _draft.Capacity, v => _draft.Capacity = v)));

        var acts = Column(6);
        void Check(ActivityDefinition a)
        {
            var cb = new CheckBox { Text = $"{a.Name}  ·  {PtBr.ActivityType(a.Type)}", ButtonPressed = _draft.Offered.Contains(a.Id), FocusMode = FocusModeEnum.None };
            string id = a.Id;
            cb.Toggled += on => { if (on) _draft.Offered.Add(id); else _draft.Offered.Remove(id); Validate(); };
            acts.AddChild(cb);
        }
        foreach (var area in _zone.Areas)
        {
            acts.AddChild(Row(8, new KindEmblem(area.Kind) { CustomMinimumSize = new Vector2(22, 22) },
                ThemeFactory.Text(area.Name, UiTokens.FontSmall, AreaPalette.Color(area.Kind), UiFonts.SemiBold)));
            foreach (var a in area.Activities) Check(a);
        }
        if (_zone.Activities.Count > 0)
        {
            acts.AddChild(ThemeFactory.Text("Zona inteira", UiTokens.FontSmall, UiTokens.TextSecondary, UiFonts.SemiBold));
            foreach (var a in _zone.Activities) Check(a);
        }

        _issuesBox = Column(6);
        var buttons = Row(8,
            Action("Salvar rascunho", "Guarda o rascunho nesta sessão", () => { _draft.Saved = true; _ctx.Drafts[_zone.Id] = _draft; Validate(); }, primary: true),
            Action("Descartar", "Volta aos valores publicados", () => { _ctx.Drafts.Remove(_zone.Id); _draft = ZoneDraft.From(_zone); SelectTab(3); }),
            Action("Publicar no mundo", "A publicação entra pelo sistema de atualizações (M7) ou pelo modo sandbox", () => { }, enabled: false));

        var info = ThemeFactory.Text("Você está editando um rascunho. O mundo ativo não muda até a publicação por atualização (M7) ou pelo modo sandbox. A validação abaixo é a mesma usada pelo Core.",
            UiTokens.FontSmall, UiTokens.TextSecondary);
        info.AutowrapMode = TextServer.AutowrapMode.WordSmart;

        var left = Column(14, Card("Propriedades", form, UiTokens.Accent));
        var right = Column(14, Card("Atividades oferecidas", acts), Card("Validação", _issuesBox), info, buttons);
        CallDeferred(nameof(Validate));
        return Row(16, left, right);
    }

    private static Control Field(string label, Control input)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        var l = ThemeFactory.Text(label, UiTokens.FontSmall, UiTokens.TextSecondary, UiFonts.SemiBold);
        l.CustomMinimumSize = new Vector2(130, 0);
        row.AddChild(l);
        input.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(input);
        return row;
    }

    private OptionButton Options(string[] values, Func<string, string> label, string current, Action<string> set)
    {
        var ob = new OptionButton { FocusMode = FocusModeEnum.None };
        for (int i = 0; i < values.Length; i++)
        {
            ob.AddItem(label(values[i]), i);
            if (values[i] == current) ob.Select(i);
        }
        ob.ItemSelected += i => { set(values[(int)i]); Validate(); };
        return ob;
    }

    private SpinBox Spin(int min, int max, int value, Action<int> set)
    {
        var sb = new SpinBox { MinValue = min, MaxValue = max, Value = value, Rounded = true, CustomMinimumSize = new Vector2(110, 0) };
        sb.ValueChanged += v => { set((int)v); Validate(); };
        return sb;
    }

    /// <summary>Valida o rascunho aplicado a uma copia do conteudo, com o validador do Core.</summary>
    private void Validate()
    {
        if (_issuesBox == null || !IsInstanceValid(_issuesBox)) return;
        foreach (var c in _issuesBox.GetChildren()) c.QueueFree();

        var opts = new JsonSerializerOptions(ContentLoader.JsonOptions) { UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip };
        var copy = JsonSerializer.Deserialize<ContentSet>(JsonSerializer.Serialize(_ctx.Content.Set, opts), opts)!;
        int idx = copy.Regions.FindIndex(r => r.Id == _zone.Id);
        var z = copy.Regions[idx];
        z.Name = _draft.Name; z.Biome = _draft.Biome; z.Function = _draft.Function; z.Pvp = _draft.Pvp;
        z.DeathRule = _draft.DeathRule; z.Control = _draft.Control; z.LevelMin = _draft.LevelMin; z.LevelMax = _draft.LevelMax;
        z.Capacity = _draft.Capacity;
        z.Activities.RemoveAll(a => !_draft.Offered.Contains(a.Id));
        foreach (var ar in z.Areas) ar.Activities.RemoveAll(a => !_draft.Offered.Contains(a.Id));

        string prefix = $"regions[{idx}]";
        var issues = ContentValidator.Validate(copy)
            .Where(i => i.Path.StartsWith(prefix, StringComparison.Ordinal) || i.File != ContentValidator.RegionsFile)
            .ToList();
        if (issues.Count == 0)
        {
            _issuesBox.AddChild(ThemeFactory.Text(_draft.DiffersFrom(_zone) ? "✓ Rascunho válido. Pronto para entrar numa atualização." : "✓ Sem alterações em relação ao publicado.",
                UiTokens.FontSmall, UiTokens.Success, UiFonts.SemiBold));
            return;
        }
        foreach (var i in issues)
        {
            var l = ThemeFactory.Text($"{(i.Severity == IssueSeverity.Error ? "✕" : "!")} {i.Message}  ({i.Path.Replace(prefix, "zona")})",
                UiTokens.FontSmall, i.Severity == IssueSeverity.Error ? UiTokens.Danger : UiTokens.Warning);
            l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            _issuesBox.AddChild(l);
        }
    }
}

/// <summary>Linha de jogador reutilizavel (listas da zona e das aglomeracoes).</summary>
public static class PlayerRow
{
    public static Button Build(ClientContext ctx, int personId, Sim.CharacterView v, Action onClick)
    {
        var p = ctx.Persons[personId - 1];
        var b = new Button { FocusMode = Control.FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 46), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        b.AddThemeStyleboxOverride("normal", ThemeFactory.Box(new Color(UiTokens.SurfaceRaised, 0.35f), 10, radius: 8));
        b.AddThemeStyleboxOverride("hover", ThemeFactory.Box(UiTokens.SurfaceHover, 10, border: UiTokens.BorderStrong, radius: 8));
        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        row.OffsetLeft = 10; row.OffsetRight = -10;
        row.AddThemeConstantOverride("separation", 10);
        var portrait = new PortraitBadge(p.AppearanceSeed, UiTokens.ProfileColor(p.ProfileId)) { CustomMinimumSize = new Vector2(34, 34), SizeFlagsVertical = Control.SizeFlags.ShrinkCenter, MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddChild(portrait);
        var col = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore };
        col.AddThemeConstantOverride("separation", -2);
        string cls = ctx.Content.Classes.TryGetValue(v.ClassId, out var c) ? c.Name : v.ClassId;
        var name = ThemeFactory.Text($"{v.Name}   ·   Nv {v.Level} {cls}", UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.SemiBold);
        name.MouseFilter = Control.MouseFilterEnum.Ignore;
        var st = ThemeFactory.Text(StatusText.Of(v, ctx.Content), UiTokens.FontCaption, UiTokens.TextSecondary);
        st.MouseFilter = Control.MouseFilterEnum.Ignore;
        col.AddChild(name);
        col.AddChild(st);
        row.AddChild(col);
        if (ctx.Favorites.Contains(personId))
        {
            var star = ThemeFactory.Text("★", 14, UiTokens.Accent);
            star.MouseFilter = Control.MouseFilterEnum.Ignore;
            row.AddChild(star);
        }
        b.AddChild(row);
        b.Pressed += onClick;
        return b;
    }
}

/// <summary>Icone redondo de tipo de atividade (controle).</summary>
public partial class GlyphIcon : Control
{
    private readonly string _type;
    public GlyphIcon(string type) => _type = type;

    public override void _Draw()
    {
        var c = Size / 2f;
        float r = Mathf.Min(Size.X, Size.Y) / 2f - 1;
        var col = ActivityPalette.Color(_type);
        DrawCircle(c, r, new Color(UiTokens.Background, 0.9f));
        DrawArc(c, r, 0, Mathf.Tau, 28, col, 1.8f, true);
        ActivityPalette.DrawGlyph(this, _type, c, r * 0.52f, col);
    }
}

/// <summary>Emblema da zona: medalhao com a cor do bioma e o anel da regra de combate.</summary>
public partial class ZoneEmblem : Control
{
    private readonly RegionDefinition _zone;
    public ZoneEmblem(RegionDefinition zone) => _zone = zone;

    public override void _Draw()
    {
        // Icone plano: cor do bioma, faixa superior na cor da regra (cidade, PvP ou bioma).
        var rect = new Rect2(Vector2.One, Size - Vector2.One * 2);
        var biome = UiTokens.BiomeColor(_zone.Biome);
        var rim = _zone.IsCity ? UiTokens.Accent : _zone.Pvp == PvpRules.Open ? UiTokens.Danger : biome.Lightened(0.3f);
        DrawRect(rect, biome.Darkened(0.72f));
        DrawRect(rect, new Color(rim, 0.6f), false, 1f);
        DrawRect(new Rect2(rect.Position, new Vector2(rect.Size.X, 2)), rim);
        var main = _zone.AllActivities().FirstOrDefault()?.Type ?? ActivityTypes.Explore;
        if (_zone.IsCity) main = ActivityTypes.Trade;
        ActivityPalette.DrawGlyph(this, main, Size / 2f, Mathf.Min(Size.X, Size.Y) * 0.22f, new Color(UiTokens.TextPrimary, 0.9f));
    }
}

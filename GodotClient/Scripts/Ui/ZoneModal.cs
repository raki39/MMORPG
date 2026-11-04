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
        var emblem = new ZoneEmblem(_zone) { CustomMinimumSize = new Vector2(84, 84) };
        row.AddChild(emblem);
        var col = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        col.AddThemeConstantOverride("separation", 6);
        col.AddChild(ThemeFactory.Text(_zone.Name, 30, UiTokens.TextPrimary, UiFonts.Display));
        var chips = new HFlowContainer();
        chips.AddThemeConstantOverride("h_separation", 8);
        chips.AddThemeConstantOverride("v_separation", 6);
        chips.AddChild(ThemeFactory.Chip(PtBr.Function(_zone.Function), ZoneAccent(_zone), dot: true));
        chips.AddChild(ThemeFactory.Chip(_zone.IsCity ? "Todos os níveis" : $"Níveis {_zone.LevelMin}–{_zone.LevelMax}", UiTokens.Info));
        chips.AddChild(ThemeFactory.Chip(PtBr.Pvp(_zone.Pvp), _zone.Pvp == PvpRules.Open ? UiTokens.Danger : UiTokens.Success));
        chips.AddChild(ThemeFactory.Chip(PtBr.Death(_zone.DeathRule), _zone.DeathRule == DeathRules.None ? UiTokens.TextSecondary : UiTokens.Warning));
        chips.AddChild(ThemeFactory.Chip(PtBr.Control(_zone.Control), _zone.Control == ControlRules.Contestable ? UiTokens.Warning : UiTokens.TextSecondary));
        chips.AddChild(ThemeFactory.Chip(PtBr.Biome(_zone.Biome), UiTokens.BiomeColor(_zone.Biome).Lightened(0.35f)));
        if (_ctx.Drafts.TryGetValue(_zone.Id, out var d) && d.Saved && d.DiffersFrom(_zone))
            chips.AddChild(ThemeFactory.Chip("Rascunho não publicado", UiTokens.Warning, dot: true));
        col.AddChild(chips);
        row.AddChild(col);
        Header.AddChild(row);

        Actions.AddChild(Action("Aproximar no mapa", "Centraliza e aproxima a zona", () => { _ctx.FocusZone(_zone.Id); Close(); }, primary: true));
        Actions.AddChild(Action("Observar em 3D", "Disponível a partir do M4", () => { }, enabled: false));
    }

    // ---------------- Visao geral ----------------

    private Control BuildOverview()
    {
        int people = _map.PeopleInZone(_zone.Id);
        var stats = Row(10,
            Stat(people.ToString(), "jogadores agora"),
            Stat(_zone.Capacity.ToString(), "capacidade nominal"),
            Stat($"{people * 100 / Math.Max(1, _zone.Capacity)}%", "lotação", WorldMapView.Ramp(Mathf.Clamp(people / (float)Math.Max(1, _zone.Capacity) * 4f, 0, 1))),
            Stat($"{(int)(WorldMapView.DangerOf(_zone) * 100)}", "índice de perigo", WorldMapView.Ramp(WorldMapView.DangerOf(_zone))));

        var areas = Column(10);
        if (_zone.Areas.Count == 0)
            areas.AddChild(ThemeFactory.Text("Esta zona ainda não tem áreas.", UiTokens.FontBody, UiTokens.Warning));
        foreach (var area in _zone.Areas) areas.AddChild(AreaCard(area));
        if (_zone.Activities.Count > 0)
        {
            var whole = Column(6);
            foreach (var a in _zone.Activities)
                whole.AddChild(TaskLine(a));
            areas.AddChild(Card("Na zona inteira (sem lugar fixo)", whole));
        }

        var exits = Column(8);
        foreach (var c in _zone.Connections)
        {
            var other = _ctx.Content.Regions[c.To];
            int dist = _ctx.Layout.Hub(_zone.Id).DistanceTo(_ctx.Layout.Gate(_zone.Id, c.To)) + _ctx.Layout.Gate(_zone.Id, c.To).DistanceTo(_ctx.Layout.Gate(c.To, _zone.Id));
            int minutes = (dist + Simulation.Core.Behavior.BehaviorTuning.WalkSpeed - 1) / Simulation.Core.Behavior.BehaviorTuning.WalkSpeed;
            var b = new Button
            {
                Text = $"→  {other.Name}     ·  ~{minutes} min do centro até a fronteira  ·  {_map.PeopleInZone(c.To)} jogadores lá",
                Alignment = HorizontalAlignment.Left, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(0, 38),
            };
            string target = c.To;
            b.Pressed += () => { Close(); _ctx.OpenZone(target); };
            exits.AddChild(b);
        }
        var note = ThemeFactory.Text("Zonas vizinhas sem saída declarada não se ligam: o caminho passa por outras zonas.", UiTokens.FontCaption, UiTokens.TextDisabled);
        exits.AddChild(note);

        return Column(16, stats, Card($"Áreas · {_zone.Areas.Count}", areas, ZoneAccent(_zone)), Card("Saídas", exits));
    }


    /// <summary>Cartao clicavel de area: emblema, nome, tipo, tarefas e quem esta la.</summary>
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
        b.Pressed += () => { Close(); _ctx.OpenArea(zid, aid); };
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
            col.AddChild(PlayerRow.Build(_ctx, id, v, () => { Close(); _ctx.OpenPerson(id); }));
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
        var c = Size / 2f;
        float r = Mathf.Min(Size.X, Size.Y) / 2f - 3;
        var biome = UiTokens.BiomeColor(_zone.Biome);
        for (int i = 0; i <= 10; i++)
        {
            float t = i / 10f;
            DrawCircle(c + new Vector2(-r * 0.25f, -r * 0.3f) * t, r * (1 - t * 0.8f), biome.Darkened(0.6f).Lerp(biome.Lightened(0.1f), t * t));
        }
        var rim = _zone.IsCity ? UiTokens.Accent : _zone.Pvp == PvpRules.Open ? UiTokens.Danger : biome.Lightened(0.45f);
        DrawArc(c, r, 0, Mathf.Tau, 48, rim, 3f, true);
        var main = _zone.AllActivities().FirstOrDefault()?.Type ?? ActivityTypes.Explore;
        if (_zone.IsCity) main = ActivityTypes.Trade;
        ActivityPalette.DrawGlyph(this, main, c, r * 0.42f, new Color(UiTokens.TextPrimary, 0.92f));
    }
}

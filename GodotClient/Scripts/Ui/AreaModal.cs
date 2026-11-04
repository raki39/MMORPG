using Godot;
using Simulation.Core.Behavior;
using Simulation.Core.Content;
using Simulation.Core.Population;
using GodotClient.Map;

namespace GodotClient.Ui;

/// <summary>
/// Ficha da area: o lugar, as tarefas que ela oferece (requisitos, criaturas, recompensas,
/// quem esta fazendo agora) e os jogadores presentes. Masmorras e esconderijos mostram para
/// onde levam (instancias e hideouts chegam em marcos futuros).
/// </summary>
public partial class AreaModal : ModalFrame
{
    private readonly ClientContext _ctx;
    private readonly WorldMapView _map;
    private readonly RegionDefinition _zone;
    private readonly AreaDefinition _area;

    public AreaModal(ClientContext ctx, WorldMapView map, string zoneId, string areaId)
        : base(AreaPalette.Color(ctx.Content.Regions[zoneId].Areas.First(a => a.Id == areaId).Kind))
    {
        _ctx = ctx;
        _map = map;
        _zone = ctx.Content.Regions[zoneId];
        _area = _zone.Areas.First(a => a.Id == areaId);
        BuildHeader();
        AddTab(_area.Activities.Count > 0 ? $"Tarefas · {_area.Activities.Count}" : "Visão geral", BuildTasks, live: true);
        AddTab("Jogadores", BuildPlayers, live: true);
        AddTab("Sobre o lugar", BuildAbout);
    }

    private Color Accent => AreaPalette.Color(_area.Kind);

    private void BuildHeader()
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 20);
        row.AddChild(new KindEmblem(_area.Kind) { CustomMinimumSize = new Vector2(88, 88) });
        var col = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        col.AddThemeConstantOverride("separation", 6);
        col.AddChild(ThemeFactory.Text(_area.Name, 30, UiTokens.TextPrimary, UiFonts.Display));
        var desc = ThemeFactory.Text(_area.Description, UiTokens.FontBody, UiTokens.TextSecondary);
        desc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        desc.CustomMinimumSize = new Vector2(520, 0);
        col.AddChild(desc);
        var chips = new HFlowContainer();
        chips.AddThemeConstantOverride("h_separation", 8);
        chips.AddThemeConstantOverride("v_separation", 6);
        chips.AddChild(ThemeFactory.Chip(AreaPalette.Name(_area.Kind), Accent, dot: true));
        chips.AddChild(ThemeFactory.Chip(_zone.Name, UiTokens.TextSecondary));
        chips.AddChild(ThemeFactory.Chip($"{_area.Radius * 20} m de diâmetro", UiTokens.Info));
        chips.AddChild(ThemeFactory.Chip(PtBr.Pvp(_zone.Pvp), _zone.Pvp == PvpRules.Open ? UiTokens.Danger : UiTokens.Success));
        if (_zone.DeathRule != DeathRules.None)
            chips.AddChild(ThemeFactory.Chip(_zone.DeathRule == DeathRules.FullLoot ? "☠ Full loot" : "Perda parcial", _zone.DeathRule == DeathRules.FullLoot ? UiTokens.Danger : UiTokens.Warning, dot: true));
        if (AreaKinds.IsPortal(_area.Kind) && _area.LeadsTo.Length > 0)
            chips.AddChild(ThemeFactory.Chip($"Leva a: {_area.LeadsTo}", Accent, dot: true));
        col.AddChild(chips);
        row.AddChild(col);
        Header.AddChild(row);

        Actions.AddChild(Action("Aproximar no mapa", "Centraliza a área", () => { _ctx.FocusArea(_zone.Id, _area.Id); Close(); }, primary: true));
        Actions.AddChild(Action("Ficha da zona", $"Abre {_zone.Name}", () => { Close(); _ctx.OpenZone(_zone.Id); }));
        if (AreaKinds.IsPortal(_area.Kind))
            Actions.AddChild(Action(_area.Kind == AreaKinds.Dungeon ? "Entrar na masmorra" : "Entrar no esconderijo",
                "Instâncias e esconderijos chegam em um marco futuro", () => { }, enabled: false));
        Actions.AddChild(Action("Observar em 3D", "Disponível a partir do M4", () => { }, enabled: false));
    }

    // ---------------- Tarefas ----------------

    private Control BuildTasks()
    {
        var people = _map.PeopleInArea(_zone.Id, _area.Id).Count;
        var stats = Row(10,
            Stat(people.ToString(), "jogadores aqui agora", UiTokens.Info),
            Stat(_area.Activities.Count.ToString(), "tarefas oferecidas", Accent),
            Stat(_area.Activities.Count == 0 ? "–" : $"{_area.Activities.Min(a => a.MinLevel)}+", "nível mínimo", UiTokens.AccentSoft),
            Stat(_area.Activities.Count == 0 ? "–" : $"{_area.Activities.Max(a => a.BaseRiskPermille) / 10}%", "risco máximo", WorldMapView.Ramp(_area.Activities.Count == 0 ? 0 : _area.Activities.Max(a => a.BaseRiskPermille) / 1000f)));

        var col = Column(16, stats);
        if (AreaKinds.IsPortal(_area.Kind))
        {
            var t = ThemeFactory.Text(_area.Kind == AreaKinds.Dungeon
                    ? $"Esta entrada leva a {_area.LeadsTo}, uma instância separada do mundo aberto. Masmorras instanciadas chegam em um marco futuro; por enquanto a entrada já faz parte do mapa e pode ter tarefas próprias."
                    : $"Aqui uma guilda poderá erguer seu esconderijo ({_area.LeadsTo}): um espaço seguro dentro de uma zona perigosa. Territórios e esconderijos chegam no M9.",
                UiTokens.FontBody, UiTokens.TextPrimary);
            t.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            col.AddChild(Card(_area.Kind == AreaKinds.Dungeon ? "Masmorra" : "Esconderijo de guilda", t, Accent));
        }
        if (_area.Activities.Count == 0 && !AreaKinds.IsPortal(_area.Kind))
            col.AddChild(Card("Tarefas", ThemeFactory.Text("Este lugar ainda não oferece tarefas.", UiTokens.FontBody, UiTokens.TextSecondary)));
        foreach (var a in _area.Activities) col.AddChild(TaskCard(a));
        return col;
    }

    private Control TaskCard(ActivityDefinition a)
    {
        int doing = _map.PeopleAtSpot(_zone.Id, a.Id);
        var body = Column(12);

        var head = new HBoxContainer();
        head.AddThemeConstantOverride("separation", 14);
        head.AddChild(new GlyphIcon(a.Type) { CustomMinimumSize = new Vector2(42, 42) });
        var titles = Column(2);
        titles.AddChild(ThemeFactory.Text(a.Name, 18, UiTokens.TextPrimary, UiFonts.SemiBold));
        string group = a.MinGroupSize == a.MaxGroupSize ? $"grupo de {a.MinGroupSize}" : $"grupo de {a.MinGroupSize} a {a.MaxGroupSize}";
        titles.AddChild(ThemeFactory.Text($"{PtBr.ActivityType(a.Type)} · nível {a.MinLevel}+ · {group} · ~{a.TypicalMinutes} min · risco {a.BaseRiskPermille / 10}%",
            UiTokens.FontSmall, UiTokens.TextSecondary, UiFonts.Numbers));
        head.AddChild(titles);
        var live = Column(0);
        live.AddChild(ThemeFactory.Text(doing.ToString(), 26, doing > 0 ? UiTokens.Info : UiTokens.TextDisabled, UiFonts.Numbers));
        live.AddChild(ThemeFactory.Text(doing == 1 ? "fazendo agora" : "fazendo agora", UiTokens.FontCaption, UiTokens.TextSecondary));
        live.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        head.AddChild(live);
        body.AddChild(head);

        int xp = BehaviorTuning.XpPerMinute(a.Type);
        var rewards = new HFlowContainer();
        rewards.AddThemeConstantOverride("h_separation", 8);
        rewards.AddThemeConstantOverride("v_separation", 6);
        if (xp > 0) rewards.AddChild(ThemeFactory.Chip($"≈ {xp * a.TypicalMinutes} XP por ciclo", UiTokens.Success, dot: true));
        foreach (var itemId in a.ItemIds)
            if (_ctx.Content.Items.TryGetValue(itemId, out var it))
                rewards.AddChild(ThemeFactory.Chip($"{it.Name} · {it.BaseValueCopper} cobre", ActivityPalette.Color(ActivityTypes.Gather)));
        if (rewards.GetChildCount() > 0) body.AddChild(rewards);

        foreach (var cid in a.CreatureIds)
        {
            if (!_ctx.Content.Creatures.TryGetValue(cid, out var cr)) continue;
            var cbox = new PanelContainer();
            var sb = ThemeFactory.Box(new Color(UiTokens.Background, 0.55f), 14, border: UiTokens.Border, radius: 9);
            sb.ContentMarginTop = sb.ContentMarginBottom = 10;
            cbox.AddThemeStyleboxOverride("panel", sb);
            var cc = Column(6);
            cc.AddChild(Row(10,
                ThemeFactory.Text(cr.Name, UiTokens.FontBody, cr.Kind == CreatureKinds.Boss ? UiTokens.Accent : UiTokens.TextPrimary, UiFonts.SemiBold),
                ThemeFactory.Chip($"Nv {cr.Level}", UiTokens.Info),
                ThemeFactory.Chip($"{cr.Health} PV", UiTokens.Success),
                ThemeFactory.Chip($"{cr.Damage} dano", UiTokens.Danger),
                ThemeFactory.Chip($"{cr.Experience} XP", UiTokens.AccentSoft),
                ThemeFactory.Chip($"{cr.GoldMinCopper}–{cr.GoldMaxCopper} cobre", UiTokens.Accent)));
            if (cr.Loot.Count > 0)
            {
                var loot = new HFlowContainer();
                loot.AddThemeConstantOverride("h_separation", 6);
                loot.AddThemeConstantOverride("v_separation", 4);
                loot.AddChild(ThemeFactory.Text("Saque:", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.SemiBold));
                foreach (var l in cr.Loot)
                {
                    string name = _ctx.Content.Items.TryGetValue(l.ItemId, out var it) ? it.Name : l.ItemId;
                    string rarity = _ctx.Content.Items.TryGetValue(l.ItemId, out var it2) ? it2.Rarity : "common";
                    loot.AddChild(ThemeFactory.Chip($"{name} {PtBr.Decimal(l.ChancePermille / 10.0, l.ChancePermille < 10 ? 1 : 0)}%", RarityColor(rarity)));
                }
                cc.AddChild(loot);
            }
            cbox.AddChild(cc);
            body.AddChild(cbox);
        }
        if (a.MinGroupSize > 1)
            body.AddChild(ThemeFactory.Text($"Exige grupo de {a.MinGroupSize}: os jogadores simulados só fazem isso quando existirem grupos (M6).", UiTokens.FontCaption, UiTokens.Warning));

        return Card(PtBr.ActivityType(a.Type), body, ActivityPalette.Color(a.Type));
    }

    private static Color RarityColor(string rarity) => rarity switch
    {
        "uncommon" => new Color("#7FD8A6"),
        "rare" => new Color("#6FB7FF"),
        "epic" => new Color("#C49BFF"),
        _ => UiTokens.TextSecondary,
    };

    // ---------------- Jogadores ----------------

    private Control BuildPlayers()
    {
        var snap = _ctx.Snapshot();
        var ids = _map.PeopleInArea(_zone.Id, _area.Id);
        var col = Column(6);
        col.AddChild(ThemeFactory.Text(ids.Count == 0 ? "Ninguém neste lugar agora." : $"{ids.Count} jogadores neste lugar", UiTokens.FontBody, UiTokens.TextSecondary));
        foreach (var id in ids.OrderByDescending(i => snap.ByPerson[i]?.Level ?? 0))
            if (snap.ByPerson[id] is { } v)
            {
                int pid = id;
                col.AddChild(PlayerRow.Build(_ctx, id, v, () => { Close(); _ctx.OpenPerson(pid); }));
            }
        return col;
    }

    // ---------------- Sobre ----------------

    private Control BuildAbout()
    {
        var t = Column(10,
            ThemeFactory.Text($"Tipo: {AreaPalette.Name(_area.Kind)}", UiTokens.FontBody, UiTokens.TextPrimary),
            ThemeFactory.Text($"Tamanho: {_area.Radius * 20} m de diâmetro. O tamanho define o espaço, não a quantidade de monstros ou recursos (configurada à parte).", UiTokens.FontSmall, UiTokens.TextSecondary),
            ThemeFactory.Text($"Regras herdadas da zona {_zone.Name}: {PtBr.Pvp(_zone.Pvp)} · {PtBr.Death(_zone.DeathRule)} · {PtBr.Control(_zone.Control)}.", UiTokens.FontSmall, UiTokens.TextSecondary),
            ThemeFactory.Text("Para fazer uma tarefa, o personagem sai da estrada e vai até este lugar.", UiTokens.FontSmall, UiTokens.TextSecondary));
        foreach (var c in t.GetChildren().OfType<Label>()) c.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return Card("Sobre o lugar", t, Accent);
    }
}

/// <summary>Emblema grande de tipo de area (medalhao com brilho).</summary>
public partial class KindEmblem : Control
{
    private readonly string _kind;
    public KindEmblem(string kind) => _kind = kind;

    public override void _Draw()
    {
        var c = Size / 2f;
        float r = Mathf.Min(Size.X, Size.Y) / 2f - 4;
        var col = AreaPalette.Color(_kind);
        for (int i = 4; i >= 1; i--) DrawCircle(c, r + i * 3, new Color(col, 0.04f));
        for (int i = 0; i <= 10; i++)
        {
            float t = i / 10f;
            DrawCircle(c + new Vector2(-r * 0.25f, -r * 0.3f) * t, r * (1 - t * 0.8f), new Color("#0D131A").Lerp(col.Darkened(0.55f), t * t));
        }
        DrawArc(c, r, 0, Mathf.Tau, 48, col, 3f, true);
        DrawArc(c, r - 5, Mathf.Pi * 1.1f, Mathf.Pi * 1.45f, 16, new Color(1, 1, 1, 0.25f), 2f, true);
        AreaPalette.DrawGlyph(this, _kind, c, r * 0.48f, col.Lightened(0.15f));
    }
}

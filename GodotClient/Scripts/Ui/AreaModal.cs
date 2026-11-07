using Godot;
using Simulation.Core.Behavior;
using Simulation.Core.Content;
using Simulation.Core.Population;
using GodotClient.Map;
using GodotClient.Sim;

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
        Breadcrumb = $"Mundo › {_zone.Name} › {_area.Name}";
        _board = ctx.Content.QuestBoards.Values.FirstOrDefault(b => b.RegionId == zoneId && b.AreaId == areaId);
        // Paginas em telas (D-068): quem esta aqui e o "sobre o lugar" entraram na Visao geral.
        if (_board != null) AddTab($"Missões · {ctx.Content.QuestsByBoard[_board.Id].Count}", BuildQuests, live: true);
        AddTab("Visão geral", BuildOverview, live: true);
    }

    private Color Accent => AreaPalette.Color(_area.Kind);
    private readonly QuestBoardDefinition? _board;
    private DataTable<QuestDefinition>? _questTable;

    // ---------------- Missoes (area de quest, D-031) ----------------

    private Control BuildQuests()
    {
        var quests = _ctx.Content.QuestsByBoard[_board!.Id];
        var stats = _ctx.Snapshot().QuestStats;
        QuestStatsView? S(QuestDefinition q) => stats.TryGetValue(q.Id, out var s) ? s : null;
        var summary = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        summary.AddThemeConstantOverride("h_separation", 8);
        summary.AddThemeConstantOverride("v_separation", 8);
        summary.AddChild(Stat(quests.Count(q => q.Category == QuestCategories.Main).ToString(), "principais", QuestModal.CategoryColor(QuestCategories.Main), fill: true));
        summary.AddChild(Stat(quests.Count(q => q.Category == QuestCategories.Secondary).ToString(), "secundárias", QuestModal.CategoryColor(QuestCategories.Secondary), fill: true));
        summary.AddChild(Stat(quests.Count(q => q.Category == QuestCategories.Daily).ToString(), "diárias", QuestModal.CategoryColor(QuestCategories.Daily), $"Limite: {_board.DailyLimit} diárias por dia por personagem", fill: true));
        summary.AddChild(Stat(_board.DailyLimit.ToString(), "diárias por dia", QuestModal.CategoryColor(QuestCategories.Daily), "Limite do quadro por personagem", fill: true));
        summary.AddChild(Stat(PtBr.Int(quests.Sum(q => S(q)?.Completed ?? 0)), "entregas no quadro", UiTokens.Success, fill: true));
        summary.AddChild(Stat(PtBr.Int(quests.Sum(q => S(q)?.Active ?? 0)), "em andamento", UiTokens.Info, fill: true));
        _questTable ??= new DataTable<QuestDefinition>(new[]
        {
            new Column<QuestDefinition> { Title = "Tipo", Width = 86, Text = q => QuestModal.CategoryName(q.Category), SortKey = q => CategoryOrder(q.Category), Color = q => QuestModal.CategoryColor(q.Category) },
            new Column<QuestDefinition> { Title = "Missão", Text = q => q.Name, SortKey = q => q.Name, Tooltip = q => q.Name + "\n~" + q.Description },
            new Column<QuestDefinition> { Title = "Nível", Width = 58, Right = true, Numeric = true, Text = q => q.MaxLevel > 0 ? $"{q.MinLevel}–{q.MaxLevel}" : $"{q.MinLevel}+", SortKey = q => q.MinLevel, Color = _ => UiTokens.Info },
            new Column<QuestDefinition> { Title = "Etapas", Width = 52, Right = true, Numeric = true, Text = q => (q.Steps.Count + 1).ToString(), SortKey = q => q.Steps.Count },
            new Column<QuestDefinition> { Title = "Requer", Width = 150, Text = q => q.PrerequisiteQuestIds.Count == 0 ? "—" : string.Join(", ", q.PrerequisiteQuestIds.Select(p => _ctx.Content.Quests.TryGetValue(p, out var pq) ? pq.Name : p)), Color = _ => UiTokens.TextSecondary },
            new Column<QuestDefinition> { Title = "Concl.", Width = 52, Right = true, Numeric = true, Text = q => PtBr.Int(S(q)?.UniqueCompleters ?? 0), SortKey = q => S(q)?.UniqueCompleters ?? 0, Color = _ => UiTokens.Success, Tooltip = _ => "Personagens que concluíram" },
            new Column<QuestDefinition> { Title = "Agora", Width = 48, Right = true, Numeric = true, Text = q => PtBr.Int(S(q)?.Active ?? 0), SortKey = q => S(q)?.Active ?? 0, Color = _ => UiTokens.Info, Tooltip = _ => "Fazendo agora" },
        }, sortColumn: 2, sortDesc: false) { OnClick = q => { _ctx.OpenQuest(q.Id); } };
        _questTable.OnSortChanged = () => Rebuild(false);
        var note = ThemeFactory.Text("Aceitar e entregar são ações rápidas aqui no quadro; as etapas acontecem no mundo. Clique numa missão para ver a ficha completa.", UiTokens.FontCaption, UiTokens.TextSecondary);
        note.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        var rows = Fit(0, "quadro");
        var table = _questTable.Build(quests, rows);
        return new ScreenPage().Add(Grid(12,
            (FillCard(_board.Name, table, info: "Missões deste quadro\n~Clique numa missão para abrir a ficha. Ordene pelas colunas.", headerRight: Pager(rows, "quadro", "missão", "missões")), 9),
            (FillCard("Resumo do quadro", Column(14, summary, Divider(), note), info: "Números do quadro desde o lançamento"), 3)));
    }

    private static int CategoryOrder(string c) => c == QuestCategories.Main ? 0 : c == QuestCategories.Secondary ? 1 : 2;

    private void BuildHeader()
    {
        // Cabecalho (D-068): emblema; titulo, o texto que identifica em duas colunas alinhadas e a descricao do
        // lugar; ao lado, as regras e o tamanho em etiquetas.
        Art.AddChild(new KindEmblem(_area.Kind) { CustomMinimumSize = new Vector2(92, 92), SizeFlagsVertical = SizeFlags.ShrinkBegin });
        Header.AddChild(TitleText(_area.Name));
        Header.CustomMinimumSize = new Vector2(390, 0);   // largura fixa: as etiquetas comecam no mesmo lugar em toda area
        int people = _map.PeopleInArea(_zone.Id, _area.Id).Count;
        Header.AddChild(InfoGrid(
            Info("layers", AreaPalette.Name(_area.Kind), Accent, UiTokens.TextLabel, "", UiFonts.Medium, 176),
            Info("trending-up", _area.LevelMin > 0 ? $"Faixa {_area.LevelMin}–{_area.LevelMax}" : "Todos os níveis", UiTokens.AccentSoft, UiTokens.AccentSoft,
                "Faixa de nível recomendada\n~Orientação: o XP vem do nível do desafio de cada tarefa.", UiFonts.SemiBold, 150),
            Info("map-pin", _zone.Name, UiTokens.TextSecondary, UiTokens.TextLabel, $"{_zone.Name}\n~Clique em \"Ficha da zona\" para ver a zona inteira.", null, 176),
            Info("users", $"{people} jogadores agora", people > 0 ? UiTokens.Info : UiTokens.TextSecondary, UiTokens.TextLabel, "", null, 150)));
        if (_area.Description.Length > 0)
            Header.AddChild(Info("", _area.Description, UiTokens.TextSecondary, UiTokens.TextSecondary, _area.Description, null, 390));

        TagWidth = 170;
        bool pvp = _zone.Pvp == PvpRules.Open;
        Tag("swords", "Combate", PtBr.Pvp(_zone.Pvp), pvp ? UiTokens.Danger : UiTokens.Success, $"Regra da zona {_zone.Name}.");
        Tag("skull", "Morte", PtBr.Death(_zone.DeathRule), _zone.DeathRule == DeathRules.FullLoot ? UiTokens.Danger : _zone.DeathRule == DeathRules.Partial ? UiTokens.Warning : UiTokens.TextSecondary,
            "Regra de morte da zona\n~O que se perde ao morrer aqui.");
        Tag("list", "Tarefas", _area.Activities.Count.ToString(), UiTokens.AccentSoft, "Tarefas oferecidas neste lugar.");
        Tag("ruler", "Tamanho", $"{_area.Radius * 20} m", UiTokens.TextSecondary, "Diâmetro da área\n~Configurável no conteúdo (editor de áreas futuro).");
        if (AreaKinds.IsPortal(_area.Kind) && _area.LeadsTo.Length > 0)
            Tag("door-open", "Leva a", _area.LeadsTo, Accent, "Destino da passagem.");
        Actions.AddChild(Action("Aproximar no mapa", "Centraliza a área", () => { _ctx.FocusArea(_zone.Id, _area.Id); Close(); }, primary: true));
        Actions.AddChild(Action("Ficha da zona", $"Abre {_zone.Name}", () => { _ctx.OpenZone(_zone.Id); }));
        if (AreaKinds.IsPortal(_area.Kind))
            Actions.AddChild(Action(_area.Kind == AreaKinds.Dungeon ? "Entrar na masmorra" : "Entrar no esconderijo",
                "Instâncias e esconderijos chegam em um marco futuro", () => { }, enabled: false));
        Actions.AddChild(Action("Observar em 3D", "Disponível a partir do M4", () => { }, enabled: false));
    }

    // ---------------- Visao geral ----------------

    /// <summary>
    /// Uma tela: as tarefas do lugar e o que acontece agora; embaixo, quem esta aqui e o que o lugar e. Com mais
    /// tarefas, a linha de cima fica maior.
    /// </summary>
    private Control BuildOverview()
    {
        int top = 56 + Math.Min(3, _area.Activities.Count) * 4;
        var ids = _map.PeopleInArea(_zone.Id, _area.Id);
        var who = Fit(6, "area_jogadores");
        var snap = _ctx.Snapshot();
        if (ids.Count == 0) who.AddChild(ThemeFactory.Text("Ninguém neste lugar agora.", UiTokens.FontBody, UiTokens.TextSecondary).AsPlaceholder());
        foreach (var id in ids.OrderByDescending(i => snap.ByPerson[i]?.Level ?? 0).ThenBy(i => i))
            if (snap.ByPerson[id] is { } v)
            {
                int pid = id;
                who.AddChild(PlayerRow.Build(_ctx, id, v, () => { _ctx.OpenPerson(pid); }));
            }
        return new ScreenPage()
            .Add(Grid(12,
                (FillCard("Tarefas", TasksBody(), info: "O que se faz aqui\n~Nível, grupo, duração, risco, recompensa de referência e as criaturas de cada tarefa.", headerRight: Caption(_area.Activities.Count.ToString())), 8),
                (FillCard("Agora no lugar", NowBody(ids.Count), info: "Situação ao vivo do lugar"), 4)), weight: top)
            .Add(Grid(12,
                (FillCard("Quem está aqui", who, info: "Jogadores neste lugar agora\n~Os de nível mais alto primeiro. Clique para abrir a ficha.", headerRight: Pager(who, "area_jogadores", "jogador", "jogadores")), 8),
                (FillCard("Sobre o lugar", AboutBody(), info: "O lugar e as regras que ele herda da zona"), 4)), weight: 100 - top);
    }

    private Control TasksBody()
    {
        var col = Column(14);
        if (AreaKinds.IsPortal(_area.Kind))
        {
            var t = ThemeFactory.Text(_area.Kind == AreaKinds.Dungeon
                    ? $"Esta entrada leva a {_area.LeadsTo}, uma instância separada do mundo aberto. Masmorras instanciadas chegam em um marco futuro; por enquanto a entrada já faz parte do mapa e pode ter tarefas próprias."
                    : $"Aqui uma guilda poderá erguer seu esconderijo ({_area.LeadsTo}): um espaço seguro dentro de uma zona perigosa. Territórios e esconderijos chegam no M9.",
                UiTokens.FontBody, UiTokens.TextPrimary);
            t.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            col.AddChild(Column(8, Section(_area.Kind == AreaKinds.Dungeon ? "Masmorra" : "Esconderijo de guilda"), t));
        }
        if (_area.Activities.Count == 0 && !AreaKinds.IsPortal(_area.Kind))
            col.AddChild(ThemeFactory.Text("Este lugar ainda não oferece tarefas.", UiTokens.FontBody, UiTokens.TextSecondary));
        for (int i = 0; i < _area.Activities.Count; i++)
        {
            if (i > 0) col.AddChild(Divider());
            col.AddChild(TaskCard(_area.Activities[i]));
        }
        return col;
    }

    /// <summary>Agora no lugar: numeros do lugar e quantos estao em cada tarefa.</summary>
    private Control NowBody(int people)
    {
        var tiles = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        tiles.AddThemeConstantOverride("h_separation", 8);
        tiles.AddThemeConstantOverride("v_separation", 8);
        tiles.AddChild(Stat(people.ToString(), "jogadores aqui agora", UiTokens.Info, fill: true));
        tiles.AddChild(Stat(_area.Activities.Count.ToString(), "tarefas oferecidas", Accent, fill: true));
        tiles.AddChild(Stat(_area.Activities.Count == 0 ? "–" : $"{_area.Activities.Min(a => a.MinLevel)}+", "nível mínimo", UiTokens.AccentSoft, fill: true));
        tiles.AddChild(Stat(_area.Activities.Count == 0 ? "–" : $"{_area.Activities.Max(a => a.BaseRiskPermille) / 10}%", "risco máximo",
            WorldMapView.Ramp(_area.Activities.Count == 0 ? 0 : _area.Activities.Max(a => a.BaseRiskPermille) / 1000f), fill: true));
        var col = Column(12, tiles);
        if (_area.Activities.Count > 0)
        {
            col.AddChild(Section("Fazendo agora"));
            int max = Math.Max(1, _area.Activities.Max(a => _map.PeopleAtSpot(_zone.Id, a.Id)));
            foreach (var a in _area.Activities.OrderByDescending(a => _map.PeopleAtSpot(_zone.Id, a.Id)).ThenBy(a => a.Name, StringComparer.Ordinal))
            {
                int n = _map.PeopleAtSpot(_zone.Id, a.Id);
                col.AddChild(Bar(a.Name, n, max, ActivityPalette.Color(a.Type), n.ToString(), 150));
            }
        }
        return col;
    }

    private Control AboutBody()
    {
        var col = Column(8,
            KeyValue("Tipo", AreaPalette.Name(_area.Kind)),
            KeyValue("Tamanho", $"{_area.Radius * 20} m de diâmetro"),
            KeyValue("Zona", _zone.Name),
            KeyValue("Combate", PtBr.Pvp(_zone.Pvp), _zone.Pvp == PvpRules.Open ? UiTokens.Danger : null),
            KeyValue("Morte", PtBr.Death(_zone.DeathRule)),
            KeyValue("Controle", PtBr.Control(_zone.Control)));
        foreach (var text in new[]
        {
            "O tamanho define o espaço, não a quantidade de monstros ou recursos (configurada à parte).",
            "Combate, morte e controle são as regras da zona, herdadas pelo lugar.",
            "Para fazer uma tarefa, o personagem sai da estrada e vai até este lugar.",
        })
        {
            var t = ThemeFactory.Text(text, UiTokens.FontCaption, UiTokens.TextSecondary);
            t.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            col.AddChild(t);
        }
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

        int challenge = ProgressionRules.ChallengeLevel(_ctx.Content, a);
        // XP de referencia: personagem no proprio nivel do desafio.
        long xp = challenge == 0 ? 0 : ProgressionRules.XpFor(_ctx.Content, a, challenge, a.TypicalMinutes);
        var rewards = new HFlowContainer();
        rewards.AddThemeConstantOverride("h_separation", 8);
        rewards.AddThemeConstantOverride("v_separation", 6);
        if (challenge > 0) rewards.AddChild(ThemeFactory.Chip($"Desafio nível {challenge}", UiTokens.Info));
        if (xp > 0) rewards.AddChild(ThemeFactory.Chip($"≈ {xp} XP por ciclo", UiTokens.Success, dot: true));
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

        return body;
    }

    private static Color RarityColor(string rarity) => rarity switch
    {
        "uncommon" => new Color("#7FD8A6"),
        "rare" => new Color("#6FB7FF"),
        "epic" => new Color("#C49BFF"),
        _ => UiTokens.TextSecondary,
    };

}

/// <summary>Emblema do tipo de area: a moldura padrao com o icone do jogo e a cor do tipo (etapa 4).</summary>
public partial class KindEmblem : EmblemBox
{
    public KindEmblem(string kind) : base(GameIcons.Area(kind), AreaPalette.Color(kind)) { }
}

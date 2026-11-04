using Godot;
using Simulation.Core.Behavior;
using Simulation.Core.Content;
using Simulation.Core.Population;
using GodotClient.Map;
using GodotClient.Sim;

namespace GodotClient.Ui;

/// <summary>Base das paginas de lista do menu (D-034): titulo, faixa de dados e tabela com busca.</summary>
public abstract partial class ListPage : ModalFrame
{
    protected readonly ClientContext Ctx;
    private string _search = "";

    protected ListPage(ClientContext ctx, string title, string subtitle, string icon = "list") : base(UiTokens.Accent)
    {
        Ctx = ctx;
        Art.AddChild(new EmblemBox(icon, UiTokens.Accent) { CustomMinimumSize = new Vector2(72, 72) });
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 2);
        col.AddChild(ThemeFactory.Text(title, 28, UiTokens.TextPrimary, UiFonts.SemiBold));
        col.AddChild(ThemeFactory.Text(subtitle, UiTokens.FontBody, UiTokens.TextSecondary));
        Header.AddChild(col);
        Breadcrumb = title;
    }

    private readonly Dictionary<string, int> _filters = new(StringComparer.Ordinal);

    /// <summary>Opcao escolhida no filtro (0 = todos).</summary>
    protected int Filter(string name) => _filters.TryGetValue(name, out var i) ? i : 0;

    /// <summary>
    /// Corpo padrao: busca, filtros (listas de opcoes; a primeira e "todos") e tabela.
    /// So a tabela e refeita ao digitar ou filtrar; busca e filtros sobrevivem a ordenacao.
    /// </summary>
    protected Control Searchable(string placeholder, Func<string, Control> table, string note = "", params (string Name, IReadOnlyList<string> Options)[] filters)
    {
        var holder = new VBoxContainer();
        var search = new LineEdit { PlaceholderText = placeholder, ClearButtonEnabled = true, Text = _search, CustomMinimumSize = new Vector2(320, 32), SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
        void Fill()
        {
            foreach (var c in holder.GetChildren()) { holder.RemoveChild(c); c.QueueFree(); }
            holder.AddChild(table(_search));
        }
        search.TextChanged += t => { _search = t.Trim(); Fill(); };
        Fill();
        var top = Row(10, search);
        if (filters.Length > 0)
        {
            top.AddChild(new Control { CustomMinimumSize = new Vector2(6, 0) });
            top.AddChild(new Icon("filter", 16, UiTokens.TextSecondary));
            foreach (var (name, options) in filters)
            {
                var o = new OptionButton { FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(170, 32), TooltipText = name };
                foreach (var opt in options) o.AddItem(opt);
                o.Selected = Math.Clamp(Filter(name), 0, Math.Max(0, o.ItemCount - 1));
                string key = name;
                o.ItemSelected += i => { _filters[key] = (int)i; Fill(); };
                top.AddChild(o);
            }
            var clear = Action("Limpar", "Limpar busca e filtros", () => { _filters.Clear(); _search = ""; Rebuild(false); });
            clear.CustomMinimumSize = new Vector2(0, 32);
            top.AddChild(clear);
        }
        if (note.Length > 0)
        {
            var n = ThemeFactory.Text(note, UiTokens.FontCaption, UiTokens.TextDisabled);
            n.VerticalAlignment = VerticalAlignment.Center;
            n.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            n.HorizontalAlignment = HorizontalAlignment.Right;
            top.AddChild(n);
        }
        return Column(10, top, Card(Title, holder));
    }

    protected abstract string Title { get; }

    protected static bool Match(string search, params string[] fields) =>
        search.Length == 0 || fields.Any(f => f.Contains(search, StringComparison.OrdinalIgnoreCase));
}

/// <summary>Pagina "Jogadores": todos os personagens com filtros e ordenacao.</summary>
public partial class PlayersPage : ListPage
{
    private const int MaxRows = 300;
    private readonly DataTable<(int Id, CharacterView V)> _table;
    protected override string Title => "Personagens";

    public PlayersPage(ClientContext ctx) : base(ctx, "Jogadores", "Todos os personagens do servidor · clique para abrir a ficha", "users")
    {
        var snap = ctx.Snapshot();
        var chars = snap.ByPerson.Where(v => v != null).Select(v => v!).ToList();
        Fact("Cadastrados", PtBr.Int(snap.Registered), UiTokens.Success, "Contas criadas no MMORPG");
        Fact("Conectados", PtBr.Int(snap.Online), UiTokens.Info, "Sessões abertas agora");
        Fact("Sem conta", PtBr.Int(snap.PersonsTotal - snap.Registered), UiTokens.Warning, "Interessados que ainda não criaram conta");
        Fact("Inativos", PtBr.Int(snap.Inactive), UiTokens.Warning, "Inativos\n~Mais de 14 dias sem jogar. Voltam com chance menor, conforme a intenção de continuar; inativos por muito tempo desistem.");
        Fact("Desistiram", PtBr.Int(snap.Quit), UiTokens.Danger, "Desistentes\n~Deixaram o jogo pelo que mais incomodava (veja a ficha). Podem voltar pela saudade; atualizações e convites chegam no M6 e no M7.");
        Fact("Nível médio", chars.Count == 0 ? "—" : PtBr.Decimal(chars.Average(c => c.Level), 1));
        Fact("Nível máximo", chars.Count == 0 ? "—" : chars.Max(c => c.Level).ToString());
        Fact("Favoritos", ctx.Favorites.Count.ToString(), UiTokens.Accent);

        string Cls(string id) => ctx.Content.Classes.TryGetValue(id, out var c) ? c.Name : id;
        string Prof(int id) => ctx.Content.Profiles.TryGetValue(ctx.Persons[id - 1].ProfileId, out var p) ? p.Name : "";
        string Pers(int id) => PlayerModal.PersonalityOf(ctx.Persons[id - 1].Traits).Label;
        _table = new DataTable<(int Id, CharacterView V)>(new[]
        {
            new Column<(int Id, CharacterView V)> { Title = "Personagem", Text = r => (ctx.Favorites.Contains(r.Id) ? "★ " : "") + r.V.Name, SortKey = r => r.V.Name, Tooltip = r => $"{r.V.Name}\nPessoa|{ctx.Persons[r.Id - 1].Name}\n~Clique para abrir a ficha." },
            new Column<(int Id, CharacterView V)> { Title = "Pessoa", Width = 150, Text = r => ctx.Persons[r.Id - 1].Name, SortKey = r => ctx.Persons[r.Id - 1].Name, Color = _ => UiTokens.TextSecondary },
            new Column<(int Id, CharacterView V)> { Title = "Perfil", Width = 100, Text = r => Prof(r.Id), SortKey = r => Prof(r.Id), Color = r => UiTokens.ProfileColor(ctx.Persons[r.Id - 1].ProfileId).Lightened(0.15f) },
            new Column<(int Id, CharacterView V)> { Title = "Personalidade", Width = 110, Text = r => Pers(r.Id), SortKey = r => Pers(r.Id), Color = _ => UiTokens.TextLabel,
                Tooltip = r => $"{Pers(r.Id)}\n~{PlayerModal.PersonalityOf(ctx.Persons[r.Id - 1].Traits).Text}" },
            new Column<(int Id, CharacterView V)> { Title = "Classe", Width = 90, Text = r => Cls(r.V.ClassId), SortKey = r => r.V.ClassId },
            new Column<(int Id, CharacterView V)> { Title = "Nível", Width = 54, Right = true, Numeric = true, Text = r => r.V.Level.ToString(), SortKey = r => r.V.Level * 1_000_000L + r.V.Experience },
            new Column<(int Id, CharacterView V)> { Title = "Situação", Width = 250, Text = r => StatusText.Of(r.V, ctx.Content), SortKey = r => (int)r.V.Status, Color = r => r.V.State == LifecycleState.Quit ? UiTokens.Danger : r.V.State == LifecycleState.Inactive ? UiTokens.Warning : r.V.Status == CharacterStatus.Offline ? UiTokens.TextDisabled : UiTokens.TextPrimary },
            new Column<(int Id, CharacterView V)> { Title = "Zona", Width = 150, Text = r => ctx.ZoneName(r.V.RegionId), SortKey = r => ctx.ZoneName(r.V.RegionId), Color = _ => UiTokens.TextSecondary },
            new Column<(int Id, CharacterView V)> { Title = "Zonas", Width = 54, Right = true, Numeric = true, Text = r => r.V.KnownRegions.ToString(), SortKey = r => r.V.KnownRegions, Tooltip = _ => "Zonas conhecidas" },
            new Column<(int Id, CharacterView V)> { Title = "Missões", Width = 62, Right = true, Numeric = true, Text = r => r.V.CompletedQuests.Sum(c => c.Times).ToString(), SortKey = r => r.V.CompletedQuests.Sum(c => c.Times), Tooltip = _ => "Missões entregues" },
            new Column<(int Id, CharacterView V)> { Title = "XP de missões", Width = 96, Right = true, Numeric = true, Text = r => r.V.XpFromQuests + r.V.XpFromActivities == 0 ? "—" : $"{r.V.XpFromQuests * 100 / (r.V.XpFromQuests + r.V.XpFromActivities)}%", SortKey = r => r.V.XpFromQuests * 1000 / Math.Max(1, r.V.XpFromQuests + r.V.XpFromActivities) },
        }, sortColumn: 5) { OnClick = r => ctx.OpenPerson(r.Id) };
        _table.OnSortChanged = () => Rebuild(false);

        AddTab("Todos", () => Body(_ => true));
        AddTab($"Conectados · {snap.Online}", () => Body(v => v.Status != CharacterStatus.Offline));
        AddTab($"Favoritos · {ctx.Favorites.Count}", () => Body(v => ctx.Favorites.Contains(v.PersonId)));
        AddTab($"Inativos · {snap.Inactive}", () => Body(v => v.State == LifecycleState.Inactive));
        AddTab($"Desistiram · {snap.Quit}", () => Body(v => v.State == LifecycleState.Quit));
    }

    private Control Body(Func<CharacterView, bool> filter)
    {
        // Opcoes dos filtros (a primeira de cada lista e "todos").
        var profiles = Ctx.Content.ProfilesOrdered.ToList();
        var classes = Ctx.Content.Classes.Values.OrderBy(c => c.Name, StringComparer.Ordinal).ToList();
        var pers = Ctx.Persons.Select(p => PlayerModal.PersonalityOf(p.Traits).Label).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
        var zones = Ctx.Content.Regions.Values.OrderBy(z => z.Name, StringComparer.Ordinal).ToList();
        return Searchable("Buscar por nome, pessoa, classe, perfil ou zona…", search =>
        {
            var snap = Ctx.Snapshot();
            int fp = Filter("Perfil"), fc = Filter("Classe"), fq = Filter("Personalidade"), fz = Filter("Zona");
            var rows = Enumerable.Range(1, snap.ByPerson.Length - 1)
                .Where(i => snap.ByPerson[i] is { } v && filter(v))
                .Select(i => (Id: i, V: snap.ByPerson[i]!))
                .Where(r => fp == 0 || Ctx.Persons[r.Id - 1].ProfileId == profiles[fp - 1].Id)
                .Where(r => fc == 0 || r.V.ClassId == classes[fc - 1].Id)
                .Where(r => fq == 0 || PlayerModal.PersonalityOf(Ctx.Persons[r.Id - 1].Traits).Label == pers[fq - 1])
                .Where(r => fz == 0 || r.V.RegionId == zones[fz - 1].Id)
                .Where(r => Match(search, r.V.Name, Ctx.Persons[r.Id - 1].Name, Ctx.ZoneName(r.V.RegionId),
                    Ctx.Content.Classes.TryGetValue(r.V.ClassId, out var c) ? c.Name : "", Ctx.Content.Profiles.TryGetValue(Ctx.Persons[r.Id - 1].ProfileId, out var p) ? p.Name : ""))
                .ToList();
            var col = Column(6, _table.Build(rows.Take(MaxRows)));
            col.AddChild(ThemeFactory.Text(rows.Count > MaxRows ? $"Mostrando {MaxRows} de {PtBr.Int(rows.Count)} pela ordem atual; refine com a busca ou os filtros." : $"{PtBr.Int(rows.Count)} personagens", UiTokens.FontCaption, UiTokens.TextDisabled));
            return col;
        }, "O tempo está pausado nesta tela.",
            ("Perfil", new[] { "Todos os perfis" }.Concat(profiles.Select(p => p.Name)).ToList()),
            ("Classe", new[] { "Todas as classes" }.Concat(classes.Select(c => c.Name)).ToList()),
            ("Personalidade", new[] { "Todas as personalidades" }.Concat(pers).ToList()),
            ("Zona", new[] { "Todas as zonas" }.Concat(zones.Select(z => z.Name)).ToList()));
    }
}

/// <summary>Pagina "Zonas": todas as zonas com lotacao, perigo e regras.</summary>
public partial class ZonesPage : ListPage
{
    private readonly WorldMapView _map;
    private readonly DataTable<RegionDefinition> _table;
    protected override string Title => "Zonas do mundo";

    public ZonesPage(ClientContext ctx, WorldMapView map) : base(ctx, "Zonas", $"{ctx.Content.World.Name} · clique para abrir a ficha da zona", "castle")
    {
        _map = map;
        var zones = ctx.Content.Regions.Values.ToList();
        Fact("Zonas", zones.Count.ToString());
        Fact("Cidades", zones.Count(z => z.IsCity).ToString(), UiTokens.Accent);
        Fact("Com PvP", zones.Count(z => z.Pvp == PvpRules.Open).ToString(), UiTokens.Danger);
        Fact("Áreas", zones.Sum(z => z.Areas.Count).ToString());
        Fact("Ocupadas agora", zones.Count(z => map.PeopleInZone(z.Id) > 0).ToString(), UiTokens.Info);

        _table = new DataTable<RegionDefinition>(new[]
        {
            new Column<RegionDefinition> { Title = "Zona", Text = z => z.Name, SortKey = z => z.Name, Tooltip = z => $"{z.Name}\nÁreas|{z.Areas.Count}\nCapacidade|{z.Capacity}\n~Clique para abrir a ficha." },
            new Column<RegionDefinition> { Title = "Função", Width = 110, Text = z => PtBr.Function(z.Function), SortKey = z => z.Function, Color = _ => UiTokens.TextSecondary },
            new Column<RegionDefinition> { Title = "Bioma", Width = 90, Text = z => PtBr.Biome(z.Biome), SortKey = z => z.Biome, Color = _ => UiTokens.TextSecondary },
            new Column<RegionDefinition> { Title = "Níveis", Width = 60, Right = true, Numeric = true, Text = z => z.IsCity ? "todos" : $"{z.LevelMin}–{z.LevelMax}", SortKey = z => z.LevelMin },
            new Column<RegionDefinition> { Title = "Jogadores", Width = 76, Right = true, Numeric = true, Text = z => _map.PeopleInZone(z.Id).ToString(), SortKey = z => _map.PeopleInZone(z.Id) },
            new Column<RegionDefinition> { Title = "Lotação", Width = 66, Right = true, Numeric = true, Text = z => $"{_map.PeopleInZone(z.Id) * 100 / Math.Max(1, z.Capacity)}%", SortKey = z => _map.PeopleInZone(z.Id) / (float)Math.Max(1, z.Capacity), Tooltip = z => $"Lotação\nJogadores|{_map.PeopleInZone(z.Id)}\nCapacidade|{z.Capacity}" },
            new Column<RegionDefinition> { Title = "Perigo", Width = 60, Right = true, Numeric = true, Text = z => ((int)(WorldMapView.DangerOf(z) * 100)).ToString(), SortKey = z => WorldMapView.DangerOf(z), Color = z => WorldMapView.Ramp(WorldMapView.DangerOf(z)) },
            new Column<RegionDefinition> { Title = "Combate", Width = 130, Text = z => PtBr.Pvp(z.Pvp), SortKey = z => z.Pvp, Color = z => z.Pvp == PvpRules.Open ? UiTokens.Danger : UiTokens.TextSecondary },
            new Column<RegionDefinition> { Title = "Morte", Width = 130, Text = z => PtBr.Death(z.DeathRule), SortKey = z => z.DeathRule, Color = z => z.DeathRule == DeathRules.FullLoot ? UiTokens.Danger : UiTokens.TextSecondary },
            new Column<RegionDefinition> { Title = "Áreas", Width = 50, Right = true, Numeric = true, Text = z => z.Areas.Count.ToString(), SortKey = z => z.Areas.Count },
            new Column<RegionDefinition> { Title = "Saídas", Width = 54, Right = true, Numeric = true, Text = z => z.Connections.Count.ToString(), SortKey = z => z.Connections.Count },
        }, sortColumn: 3, sortDesc: false) { OnClick = z => ctx.OpenZone(z.Id) };
        _table.OnSortChanged = () => Rebuild(false);
        var functions = zones.Select(z => z.Function).Distinct().OrderBy(f => PtBr.Function(f), StringComparer.Ordinal).ToList();
        var pvps = zones.Select(z => z.Pvp).Distinct().OrderBy(p => PtBr.Pvp(p), StringComparer.Ordinal).ToList();
        AddTab("Todas", () => Searchable("Buscar zona, função ou bioma…", s =>
        {
            int ff = Filter("Função"), fp = Filter("Combate");
            return _table.Build(Ctx.Content.Regions.Values.Where(z => (ff == 0 || z.Function == functions[ff - 1]) && (fp == 0 || z.Pvp == pvps[fp - 1])
                && Match(s, z.Name, PtBr.Function(z.Function), PtBr.Biome(z.Biome))));
        }, "",
            ("Função", new[] { "Todas as funções" }.Concat(functions.Select(PtBr.Function)).ToList()),
            ("Combate", new[] { "Qualquer combate" }.Concat(pvps.Select(PtBr.Pvp)).ToList())));
    }
}

/// <summary>Pagina "Missões": catalogo de quests com os numeros da campanha.</summary>
public partial class QuestsPage : ListPage
{
    private readonly DataTable<QuestDefinition> _table;
    protected override string Title => "Catálogo de missões";

    public QuestsPage(ClientContext ctx) : base(ctx, "Missões", "Catálogo por quadro de missões · clique para abrir a ficha", "scroll-text")
    {
        var stats = ctx.Snapshot().QuestStats;
        var quests = ctx.Content.Quests.Values.ToList();
        QuestStatsView? S(QuestDefinition q) => stats.TryGetValue(q.Id, out var s) ? s : null;
        Fact("Missões", quests.Count.ToString());
        Fact("Quadros", ctx.Content.QuestBoards.Count.ToString());
        Fact("Entregas", PtBr.Int(stats.Values.Sum(s => s.Completed)), UiTokens.Success, "Entregas desde o lançamento");
        Fact("Em andamento", PtBr.Int(stats.Values.Sum(s => s.Active)), UiTokens.Info);
        Fact("XP concedido", PtBr.Int(stats.Values.Sum(s => s.XpGranted)), UiTokens.Accent);

        string Board(QuestDefinition q) => ctx.Content.QuestBoards.TryGetValue(q.BoardId, out var b) ? b.Name : q.BoardId;
        string Zone(QuestDefinition q) => ctx.Content.QuestBoards.TryGetValue(q.BoardId, out var b) ? ctx.ZoneName(b.RegionId) : "";
        _table = new DataTable<QuestDefinition>(new[]
        {
            new Column<QuestDefinition> { Title = "Missão", Text = q => q.Name, SortKey = q => q.Name, Tooltip = q => $"{q.Name}\n~{q.Description}" },
            new Column<QuestDefinition> { Title = "Categoria", Width = 90, Text = q => QuestModal.CategoryName(q.Category), SortKey = q => q.Category, Color = q => q.Category == QuestCategories.Main ? UiTokens.AccentSoft : UiTokens.TextSecondary },
            new Column<QuestDefinition> { Title = "Quadro", Width = 170, Text = Board, SortKey = q => Board(q), Color = _ => UiTokens.TextSecondary },
            new Column<QuestDefinition> { Title = "Zona", Width = 130, Text = Zone, SortKey = q => Zone(q), Color = _ => UiTokens.TextSecondary },
            new Column<QuestDefinition> { Title = "Nível", Width = 54, Right = true, Numeric = true, Text = q => q.MaxLevel > 0 ? $"{q.MinLevel}–{q.MaxLevel}" : $"{q.MinLevel}+", SortKey = q => q.MinLevel },
            new Column<QuestDefinition> { Title = "Etapas", Width = 54, Right = true, Numeric = true, Text = q => (q.Steps.Count + 1).ToString(), SortKey = q => q.Steps.Count },
            new Column<QuestDefinition> { Title = "XP", Width = 60, Right = true, Numeric = true, Text = q => PtBr.Int(QuestSystem.RewardXp(ctx.Content, Math.Max(q.MinLevel, q.RecommendedLevel), q)), SortKey = q => QuestSystem.RewardXp(ctx.Content, Math.Max(q.MinLevel, q.RecommendedLevel), q) },
            new Column<QuestDefinition> { Title = "Aceitas", Width = 62, Right = true, Numeric = true, Text = q => PtBr.Int(S(q)?.Accepted ?? 0), SortKey = q => S(q)?.Accepted ?? 0 },
            new Column<QuestDefinition> { Title = "Entregues", Width = 72, Right = true, Numeric = true, Text = q => PtBr.Int(S(q)?.Completed ?? 0), SortKey = q => S(q)?.Completed ?? 0 },
            new Column<QuestDefinition> { Title = "Fazendo", Width = 62, Right = true, Numeric = true, Text = q => PtBr.Int(S(q)?.Active ?? 0), SortKey = q => S(q)?.Active ?? 0 },
            new Column<QuestDefinition> { Title = "Conclusão", Width = 76, Right = true, Numeric = true, Text = q => S(q) is { Accepted: > 0 } s ? $"{s.Completed * 100 / s.Accepted}%" : "—", SortKey = q => S(q) is { Accepted: > 0 } s ? s.Completed * 1000 / s.Accepted : -1, Tooltip = _ => "Entregas ÷ aceites" },
        }, sortColumn: 4, sortDesc: false) { OnClick = q => ctx.OpenQuest(q.Id) };
        _table.OnSortChanged = () => Rebuild(false);
        var boards = ctx.Content.QuestBoards.Values.OrderBy(b => b.Name, StringComparer.Ordinal).ToList();
        foreach (var (name, cat) in new[] { ("Todas", ""), ("Principais", QuestCategories.Main), ("Secundárias", QuestCategories.Secondary), ("Diárias", QuestCategories.Daily) })
        {
            string c = cat;
            int n = c.Length == 0 ? quests.Count : quests.Count(q => q.Category == c);
            AddTab($"{name} · {n}", () => Searchable("Buscar missão, quadro ou zona…", s =>
            {
                int fb = Filter("Quadro"), fs = Filter("Situação");
                return _table.Build(Ctx.Content.Quests.Values.Where(q => (c.Length == 0 || q.Category == c)
                    && (fb == 0 || q.BoardId == boards[fb - 1].Id)
                    && (fs == 0 || (fs == 1 ? (S(q)?.Active ?? 0) > 0 : fs == 2 ? (S(q)?.Completed ?? 0) > 0 : (S(q)?.Accepted ?? 0) == 0))
                    && Match(s, q.Name, Board(q), Zone(q))));
            }, "",
                ("Quadro", new[] { "Todos os quadros" }.Concat(boards.Select(b => b.Name)).ToList()),
                ("Situação", new List<string> { "Qualquer situação", "Com gente fazendo", "Já entregue", "Nunca aceita" })));
        }
    }
}

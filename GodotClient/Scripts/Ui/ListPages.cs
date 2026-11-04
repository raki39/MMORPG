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

    protected ListPage(ClientContext ctx, string title, string subtitle) : base(UiTokens.Accent)
    {
        Ctx = ctx;
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 2);
        col.AddChild(ThemeFactory.Text(title, 24, UiTokens.TextPrimary, UiFonts.Title));
        col.AddChild(ThemeFactory.Text(subtitle, UiTokens.FontBody, UiTokens.TextSecondary));
        Header.AddChild(col);
        Breadcrumb = title;
    }

    /// <summary>Corpo padrao: busca + tabela (so a tabela e refeita ao digitar).</summary>
    protected Control Searchable(string placeholder, Func<string, Control> table, string note = "")
    {
        var holder = new VBoxContainer();
        var search = new LineEdit { PlaceholderText = placeholder, ClearButtonEnabled = true, Text = _search, CustomMinimumSize = new Vector2(360, 30), SizeFlagsHorizontal = SizeFlags.ShrinkBegin };
        void Fill()
        {
            foreach (var c in holder.GetChildren()) { holder.RemoveChild(c); c.QueueFree(); }
            holder.AddChild(table(_search));
        }
        search.TextChanged += t => { _search = t.Trim(); Fill(); };
        Fill();
        var top = Row(12, search);
        if (note.Length > 0) top.AddChild(ThemeFactory.Text(note, UiTokens.FontCaption, UiTokens.TextDisabled));
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

    public PlayersPage(ClientContext ctx) : base(ctx, "Jogadores", "Todos os personagens do servidor · clique para abrir a ficha")
    {
        var snap = ctx.Snapshot();
        var chars = snap.ByPerson.Where(v => v != null).Select(v => v!).ToList();
        Fact("Cadastrados", PtBr.Int(snap.Registered), UiTokens.Success, "Contas criadas no MMORPG");
        Fact("Conectados", PtBr.Int(snap.Online), UiTokens.Info, "Sessões abertas agora");
        Fact("Sem conta", PtBr.Int(snap.PersonsTotal - snap.Registered), UiTokens.Warning, "Interessados que ainda não criaram conta");
        Fact("Nível médio", chars.Count == 0 ? "—" : PtBr.Decimal(chars.Average(c => c.Level), 1));
        Fact("Nível máximo", chars.Count == 0 ? "—" : chars.Max(c => c.Level).ToString());
        Fact("Favoritos", ctx.Favorites.Count.ToString(), UiTokens.Accent);

        string Cls(string id) => ctx.Content.Classes.TryGetValue(id, out var c) ? c.Name : id;
        string Prof(int id) => ctx.Content.Profiles.TryGetValue(ctx.Persons[id - 1].ProfileId, out var p) ? p.Name : "";
        _table = new DataTable<(int Id, CharacterView V)>(new[]
        {
            new Column<(int Id, CharacterView V)> { Title = "Personagem", Text = r => (ctx.Favorites.Contains(r.Id) ? "★ " : "") + r.V.Name, SortKey = r => r.V.Name, Tooltip = r => $"{r.V.Name}\nPessoa|{ctx.Persons[r.Id - 1].Name}\n~Clique para abrir a ficha." },
            new Column<(int Id, CharacterView V)> { Title = "Pessoa", Width = 150, Text = r => ctx.Persons[r.Id - 1].Name, SortKey = r => ctx.Persons[r.Id - 1].Name, Color = _ => UiTokens.TextSecondary },
            new Column<(int Id, CharacterView V)> { Title = "Perfil", Width = 100, Text = r => Prof(r.Id), SortKey = r => Prof(r.Id), Color = _ => UiTokens.TextSecondary },
            new Column<(int Id, CharacterView V)> { Title = "Classe", Width = 90, Text = r => Cls(r.V.ClassId), SortKey = r => r.V.ClassId },
            new Column<(int Id, CharacterView V)> { Title = "Nível", Width = 50, Right = true, Numeric = true, Text = r => r.V.Level.ToString(), SortKey = r => r.V.Level * 1_000_000L + r.V.Experience },
            new Column<(int Id, CharacterView V)> { Title = "Situação", Width = 250, Text = r => StatusText.Of(r.V, ctx.Content), SortKey = r => (int)r.V.Status, Color = r => r.V.Status == CharacterStatus.Offline ? UiTokens.TextDisabled : UiTokens.TextPrimary },
            new Column<(int Id, CharacterView V)> { Title = "Zona", Width = 150, Text = r => ctx.ZoneName(r.V.RegionId), SortKey = r => ctx.ZoneName(r.V.RegionId), Color = _ => UiTokens.TextSecondary },
            new Column<(int Id, CharacterView V)> { Title = "Zonas", Width = 54, Right = true, Numeric = true, Text = r => r.V.KnownRegions.ToString(), SortKey = r => r.V.KnownRegions, Tooltip = _ => "Zonas conhecidas" },
            new Column<(int Id, CharacterView V)> { Title = "Missões", Width = 62, Right = true, Numeric = true, Text = r => r.V.CompletedQuests.Sum(c => c.Times).ToString(), SortKey = r => r.V.CompletedQuests.Sum(c => c.Times), Tooltip = _ => "Missões entregues" },
            new Column<(int Id, CharacterView V)> { Title = "XP de missões", Width = 96, Right = true, Numeric = true, Text = r => r.V.XpFromQuests + r.V.XpFromActivities == 0 ? "—" : $"{r.V.XpFromQuests * 100 / (r.V.XpFromQuests + r.V.XpFromActivities)}%", SortKey = r => r.V.XpFromQuests * 1000 / Math.Max(1, r.V.XpFromQuests + r.V.XpFromActivities) },
        }, sortColumn: 4) { OnClick = r => ctx.OpenPerson(r.Id) };
        _table.OnSortChanged = () => Rebuild(false);

        AddTab("Todos", () => Body(_ => true));
        AddTab($"Conectados · {snap.Online}", () => Body(v => v.Status != CharacterStatus.Offline));
        AddTab($"Favoritos · {ctx.Favorites.Count}", () => Body(v => ctx.Favorites.Contains(v.PersonId)));
    }

    private Control Body(Func<CharacterView, bool> filter) => Searchable("Buscar por nome, pessoa, classe, perfil ou zona…", search =>
    {
        var snap = Ctx.Snapshot();
        var rows = Enumerable.Range(1, snap.ByPerson.Length - 1)
            .Where(i => snap.ByPerson[i] is { } v && filter(v))
            .Select(i => (Id: i, V: snap.ByPerson[i]!))
            .Where(r => Match(search, r.V.Name, Ctx.Persons[r.Id - 1].Name, Ctx.ZoneName(r.V.RegionId),
                Ctx.Content.Classes.TryGetValue(r.V.ClassId, out var c) ? c.Name : "", Ctx.Content.Profiles.TryGetValue(Ctx.Persons[r.Id - 1].ProfileId, out var p) ? p.Name : ""))
            .ToList();
        var col = Column(6, _table.Build(rows.Take(MaxRows)));
        if (rows.Count > MaxRows) col.AddChild(ThemeFactory.Text($"Mostrando {MaxRows} de {PtBr.Int(rows.Count)} pela ordem atual; refine com a busca.", UiTokens.FontCaption, UiTokens.TextDisabled));
        return col;
    }, "O tempo está pausado nesta tela.");
}

/// <summary>Pagina "Zonas": todas as zonas com lotacao, perigo e regras.</summary>
public partial class ZonesPage : ListPage
{
    private readonly WorldMapView _map;
    private readonly DataTable<RegionDefinition> _table;
    protected override string Title => "Zonas do mundo";

    public ZonesPage(ClientContext ctx, WorldMapView map) : base(ctx, "Zonas", $"{ctx.Content.World.Name} · clique para abrir a ficha da zona")
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
        AddTab("Todas", () => Searchable("Buscar zona, função ou bioma…", s => _table.Build(Ctx.Content.Regions.Values.Where(z => Match(s, z.Name, PtBr.Function(z.Function), PtBr.Biome(z.Biome))))));
    }
}

/// <summary>Pagina "Missões": catalogo de quests com os numeros da campanha.</summary>
public partial class QuestsPage : ListPage
{
    private readonly DataTable<QuestDefinition> _table;
    protected override string Title => "Catálogo de missões";

    public QuestsPage(ClientContext ctx) : base(ctx, "Missões", "Catálogo por quadro de missões · clique para abrir a ficha")
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
        foreach (var (name, cat) in new[] { ("Todas", ""), ("Principais", QuestCategories.Main), ("Secundárias", QuestCategories.Secondary), ("Diárias", QuestCategories.Daily) })
        {
            string c = cat;
            int n = c.Length == 0 ? quests.Count : quests.Count(q => q.Category == c);
            AddTab($"{name} · {n}", () => Searchable("Buscar missão, quadro ou zona…", s =>
                _table.Build(Ctx.Content.Quests.Values.Where(q => (c.Length == 0 || q.Category == c) && Match(s, q.Name, Board(q), Zone(q))))));
        }
    }
}

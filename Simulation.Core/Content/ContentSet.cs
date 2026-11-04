namespace Simulation.Core.Content;

/// <summary>Conteudo bruto como lido dos arquivos, antes de validar.</summary>
public sealed class ContentSet
{
    public const int SupportedSchemaVersion = 1;

    public WorldDefinition World { get; set; } = new();
    public List<RegionDefinition> Regions { get; set; } = new();
    public List<ClassDefinition> Classes { get; set; } = new();
    public List<AbilityDefinition> Abilities { get; set; } = new();
    public List<ItemDefinition> Items { get; set; } = new();
    public List<CreatureDefinition> Creatures { get; set; } = new();
    public PopulationDefinition Population { get; set; } = new();
    public ProgressionDefinition Progression { get; set; } = new();
    public QuestCatalog Quests { get; set; } = new();

    /// <summary>Hash do conteudo de origem (calculado por quem carregou os arquivos).</summary>
    public string SourceHash { get; set; } = "";
}

/// <summary>
/// Conteudo validado e indexado por ID estavel. Imutavel por convencao apos a criacao;
/// rascunhos e atualizacoes (M7) produzirao novas instancias.
/// </summary>
public sealed class ContentDatabase
{
    private ContentDatabase(ContentSet set)
    {
        Set = set;
        Regions = Index(set.Regions);
        Classes = Index(set.Classes);
        Abilities = Index(set.Abilities);
        Items = Index(set.Items);
        Creatures = Index(set.Creatures);
        Profiles = Index(set.Population.Profiles);
        Quests = Index(set.Quests.Quests);
        QuestBoards = Index(set.Quests.Boards);
        var byActivity = new SortedDictionary<string, QuestBoardDefinition>(StringComparer.Ordinal);
        foreach (var b in set.Quests.Boards) byActivity.TryAdd(b.RegionId + "/" + b.ActivityId, b);
        BoardByActivity = byActivity;
        var byBoard = new SortedDictionary<string, IReadOnlyList<QuestDefinition>>(StringComparer.Ordinal);
        foreach (var b in set.Quests.Boards)
            byBoard[b.Id] = set.Quests.Quests.Where(q => q.BoardId == b.Id).OrderBy(q => q.Id, StringComparer.Ordinal).ToList();
        QuestsByBoard = byBoard;
    }

    public IReadOnlyDictionary<string, QuestDefinition> Quests { get; }
    public IReadOnlyDictionary<string, QuestBoardDefinition> QuestBoards { get; }
    /// <summary>Quadro pela atividade de aceitar/entregar ("regiao/atividade").</summary>
    public IReadOnlyDictionary<string, QuestBoardDefinition> BoardByActivity { get; }
    public IReadOnlyDictionary<string, IReadOnlyList<QuestDefinition>> QuestsByBoard { get; }
    public QuestSettings QuestSettings => Set.Quests.Settings;

    public ContentSet Set { get; }
    public WorldDefinition World => Set.World;
    public string SourceHash => Set.SourceHash;
    public NamePoolDefinition Names => Set.Population.Names;
    /// <summary>Evolucao dos atributos do jogador (D-036).</summary>
    public TraitDynamicsDefinition Dynamics => Set.Population.Dynamics;
    /// <summary>Metas de curto prazo (D-036).</summary>
    public GoalSettingsDefinition GoalSettings => Set.Population.Goals;
    /// <summary>Plano da sessao e tedio (D-037).</summary>
    public SessionSettingsDefinition Session => Set.Population.Session;
    /// <summary>Experiencia e humor (D-038).</summary>
    public MoodSettingsDefinition Mood => Set.Population.Mood;
    /// <summary>Memoria (D-038).</summary>
    public MemorySettingsDefinition Memory => Set.Population.Memory;
    /// <summary>Retencao (D-039).</summary>
    public RetentionSettingsDefinition Retention => Set.Population.Retention;
    public ProgressionDefinition Progression => Set.Progression;

    public IReadOnlyDictionary<string, RegionDefinition> Regions { get; }
    public IReadOnlyDictionary<string, ClassDefinition> Classes { get; }
    public IReadOnlyDictionary<string, AbilityDefinition> Abilities { get; }
    public IReadOnlyDictionary<string, ItemDefinition> Items { get; }
    public IReadOnlyDictionary<string, CreatureDefinition> Creatures { get; }
    public IReadOnlyDictionary<string, PopulationProfileDefinition> Profiles { get; }

    /// <summary>Perfis em ordem ordinal de ID (ordem estavel para sorteio ponderado).</summary>
    public IReadOnlyList<PopulationProfileDefinition> ProfilesOrdered => Profiles.Values.ToList();

    /// <summary>Valida e cria a base. Lanca se houver erros; use Validate antes para obter a lista.</summary>
    public static ContentDatabase Create(ContentSet set)
    {
        var issues = ContentValidator.Validate(set);
        var errors = issues.Where(i => i.Severity == IssueSeverity.Error).ToList();
        if (errors.Count > 0)
            throw new ContentValidationException(errors);
        return new ContentDatabase(set);
    }

    private static IReadOnlyDictionary<string, T> Index<T>(IEnumerable<T> items) where T : IContentDefinition
    {
        var dict = new SortedDictionary<string, T>(StringComparer.Ordinal);
        foreach (var item in items) dict.Add(item.Id, item);
        return dict;
    }
}

public sealed class ContentValidationException : Exception
{
    public ContentValidationException(IReadOnlyList<ValidationIssue> issues)
        : base("Conteúdo inválido:\n" + string.Join("\n", issues.Select(i => i.ToString())))
    {
        Issues = issues;
    }

    public IReadOnlyList<ValidationIssue> Issues { get; }
}

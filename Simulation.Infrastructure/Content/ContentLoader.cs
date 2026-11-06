using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Simulation.Core.Content;

namespace Simulation.Infrastructure.Content;

public sealed class ContentLoadResult
{
    public ContentSet? Set { get; init; }
    public ContentDatabase? Database { get; init; }
    public List<ValidationIssue> Issues { get; init; } = new();
    /// <summary>SHA-256 dos arquivos de conteudo (nomes + texto normalizado em LF).</summary>
    public string ContentHash { get; init; } = "";
    public string Folder { get; init; } = "";
    public bool Success => Database != null;
    public IEnumerable<ValidationIssue> Errors => Issues.Where(i => i.Severity == IssueSeverity.Error);
    public IEnumerable<ValidationIssue> Warnings => Issues.Where(i => i.Severity == IssueSeverity.Warning);
}

/// <summary>
/// Carrega uma pasta de conteudo (preset ou rascunho) em JSON e valida.
/// Campos desconhecidos sao erro (pega erros de digitacao no autor).
/// </summary>
public static class ContentLoader
{
    public static readonly string[] ContentFiles =
    {
        ContentValidator.WorldFile,
        ContentValidator.LocalitiesFile,
        ContentValidator.OccupationsFile,
        ContentValidator.RegionsFile,
        ContentValidator.ClassesFile,
        ContentValidator.AbilitiesFile,
        ContentValidator.ItemsFile,
        ContentValidator.CreaturesFile,
        ContentValidator.PopulationFile,
        ContentValidator.ProgressionFile,
        ContentValidator.QuestsFile,
        ContentValidator.TraitsFile,
    };

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
    };

    public static ContentLoadResult LoadFolder(string folder)
    {
        var issues = new List<ValidationIssue>();
        var full = Path.GetFullPath(folder);
        if (!Directory.Exists(full))
        {
            issues.Add(new ValidationIssue(IssueSeverity.Error, folder, "", "Pasta de conteúdo não encontrada."));
            return new ContentLoadResult { Issues = issues, Folder = full };
        }

        var texts = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var name in ContentFiles)
        {
            var path = Path.Combine(full, name);
            if (!File.Exists(path))
                issues.Add(new ValidationIssue(IssueSeverity.Error, name, "", "Arquivo obrigatório ausente."));
            else
                texts[name] = File.ReadAllText(path, Encoding.UTF8);
        }

        string hash = ComputeHash(texts);
        if (issues.Count > 0)
            return new ContentLoadResult { Issues = issues, ContentHash = hash, Folder = full };

        var set = new ContentSet { SourceHash = hash };
        set.World = Parse<WorldFile>(texts, ContentValidator.WorldFile, issues)?.World ?? new WorldDefinition();
        set.Regions = Parse<RegionsFile>(texts, ContentValidator.RegionsFile, issues)?.Regions ?? new();
        set.Localities = Parse<LocalitiesFile>(texts, ContentValidator.LocalitiesFile, issues)?.Localities ?? new();
        set.Occupations = Parse<OccupationsFile>(texts, ContentValidator.OccupationsFile, issues)?.Occupations ?? new();
        set.Classes = Parse<ClassesFile>(texts, ContentValidator.ClassesFile, issues)?.Classes ?? new();
        set.Abilities = Parse<AbilitiesFile>(texts, ContentValidator.AbilitiesFile, issues)?.Abilities ?? new();
        set.Items = Parse<ItemsFile>(texts, ContentValidator.ItemsFile, issues)?.Items ?? new();
        set.Creatures = Parse<CreaturesFile>(texts, ContentValidator.CreaturesFile, issues)?.Creatures ?? new();
        var pop = Parse<PopulationFile>(texts, ContentValidator.PopulationFile, issues);
        if (pop != null) set.Population = new PopulationDefinition { Profiles = pop.Profiles, Names = pop.Names, Dynamics = pop.Dynamics, Goals = pop.Goals, Session = pop.Session, Mood = pop.Mood, Memory = pop.Memory, Retention = pop.Retention, Behavior = pop.Behavior, Routine = pop.Routine, Dedication = pop.Dedication, Traits = pop.Traits, Preferences = pop.Preferences, Links = pop.Links, EarlyProgression = pop.EarlyProgression, Personality = pop.Personality };
        set.Progression = Parse<ProgressionFile>(texts, ContentValidator.ProgressionFile, issues)?.Progression ?? new ProgressionDefinition();
        var quests = Parse<QuestsFile>(texts, ContentValidator.QuestsFile, issues);
        if (quests != null) set.Quests = new QuestCatalog { Settings = quests.Settings, Boards = quests.Boards, Quests = quests.Quests };
        var traits = Parse<TraitsFile>(texts, ContentValidator.TraitsFile, issues);
        if (traits != null) set.Quirks = new QuirkCatalogDefinition { Generation = traits.Generation, Effects = traits.Effects, Acquisition = traits.Acquisition, Traits = traits.Traits };

        if (issues.Any(i => i.Severity == IssueSeverity.Error))
            return new ContentLoadResult { Set = set, Issues = issues, ContentHash = hash, Folder = full };

        issues.AddRange(ContentValidator.Validate(set));
        ContentDatabase? db = issues.Any(i => i.Severity == IssueSeverity.Error) ? null : ContentDatabase.Create(set);
        return new ContentLoadResult { Set = set, Database = db, Issues = issues, ContentHash = hash, Folder = full };
    }

    private static T? Parse<T>(IDictionary<string, string> texts, string file, List<ValidationIssue> issues)
        where T : VersionedFile
    {
        try
        {
            var value = JsonSerializer.Deserialize<T>(texts[file], JsonOptions);
            if (value == null)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, file, "", "Arquivo vazio."));
                return null;
            }
            if (value.SchemaVersion != ContentSet.SupportedSchemaVersion)
            {
                issues.Add(new ValidationIssue(IssueSeverity.Error, file, "schemaVersion",
                    $"Versão de esquema {value.SchemaVersion} não suportada (esperado {ContentSet.SupportedSchemaVersion})."));
                return null;
            }
            return value;
        }
        catch (JsonException ex)
        {
            string where = ex.LineNumber.HasValue ? $"linha {ex.LineNumber + 1}, coluna {ex.BytePositionInLine + 1}" : "posição desconhecida";
            issues.Add(new ValidationIssue(IssueSeverity.Error, file, ex.Path ?? "", $"JSON inválido ({where}): {ex.Message}"));
            return null;
        }
    }

    /// <summary>Hash estavel: independe de CRLF/LF e da ordem de leitura.</summary>
    public static string ComputeHash(IDictionary<string, string> texts)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var kv in texts.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            sha.AppendData(Encoding.UTF8.GetBytes(kv.Key + "\n"));
            sha.AppendData(Encoding.UTF8.GetBytes(kv.Value.Replace("\r\n", "\n").TrimStart('﻿')));
            sha.AppendData(new byte[] { 0 });
        }
        return Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();
    }
}

// Formatos dos arquivos: envelope com schemaVersion + dados.
public sealed class OccupationsFile : VersionedFile
{
    public List<OccupationDefinition> Occupations { get; set; } = new();
}

public sealed class LocalitiesFile : VersionedFile
{
    public List<LocalityDefinition> Localities { get; set; } = new();
}

public abstract class VersionedFile
{
    public int SchemaVersion { get; set; }
}

public sealed class WorldFile : VersionedFile { public WorldDefinition World { get; set; } = new(); }
public sealed class RegionsFile : VersionedFile { public List<RegionDefinition> Regions { get; set; } = new(); }
public sealed class ClassesFile : VersionedFile { public List<ClassDefinition> Classes { get; set; } = new(); }
public sealed class AbilitiesFile : VersionedFile { public List<AbilityDefinition> Abilities { get; set; } = new(); }
public sealed class ItemsFile : VersionedFile { public List<ItemDefinition> Items { get; set; } = new(); }
public sealed class CreaturesFile : VersionedFile { public List<CreatureDefinition> Creatures { get; set; } = new(); }

public sealed class QuestsFile : VersionedFile
{
    public QuestSettings Settings { get; set; } = new();
    public List<QuestBoardDefinition> Boards { get; set; } = new();
    public List<QuestDefinition> Quests { get; set; } = new();
}

public sealed class ProgressionFile : VersionedFile { public ProgressionDefinition Progression { get; set; } = new(); }

/// <summary>traits.json (P4b): traits da pessoa (no codigo, Quirk).</summary>
public sealed class TraitsFile : VersionedFile
{
    public QuirkGenerationDefinition Generation { get; set; } = new();
    public QuirkEffectsDefinition Effects { get; set; } = new();
    public QuirkAcquisitionDefinition Acquisition { get; set; } = new();
    public List<QuirkDefinition> Traits { get; set; } = new();
}

public sealed class PopulationFile : VersionedFile
{
    public List<PopulationProfileDefinition> Profiles { get; set; } = new();
    public NamePoolDefinition Names { get; set; } = new();
    public TraitDynamicsDefinition Dynamics { get; set; } = new();
    public GoalSettingsDefinition Goals { get; set; } = new();
    public SessionSettingsDefinition Session { get; set; } = new();
    public MoodSettingsDefinition Mood { get; set; } = new();
    public MemorySettingsDefinition Memory { get; set; } = new();
    public RetentionSettingsDefinition Retention { get; set; } = new();
    public BehaviorSettingsDefinition Behavior { get; set; } = new();
    public RoutineSettingsDefinition Routine { get; set; } = new();
    public DedicationSettingsDefinition Dedication { get; set; } = new();
    public PopulationTraitsDefinition Traits { get; set; } = new();
    public PreferencesDefinition Preferences { get; set; } = new();
    public List<TraitLinkDefinition> Links { get; set; } = new();
    public EarlyProgressionDefinition EarlyProgression { get; set; } = new();
    public PersonalitySettingsDefinition Personality { get; set; } = new();
}

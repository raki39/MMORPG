using System.Text;
using System.Text.Json;
using Simulation.Core.Content;
using Simulation.Infrastructure.Content;

namespace Simulation.Infrastructure.Validation;

/// <summary>
/// Metas de referencia para validar uma campanha (targets.json, ao lado do conteudo).
/// Nao faz parte do conteudo publicado: e uma ferramenta de calibracao do runner.
/// As faixas sao hipoteses de design ajustaveis, nao regras do mundo.
/// </summary>
public sealed class TargetsFile
{
    public int SchemaVersion { get; set; }
    public string Description { get; set; } = "";
    /// <summary>Dias iniciais ignorados nas medidas semanais (semana de lancamento).</summary>
    public int WarmupDays { get; set; } = 7;
    /// <summary>Atividades abaixo desta parcela das escolhas (permille) entram na lista de pouco usadas.</summary>
    public int RareActivityPermille { get; set; } = 10;
    public List<TargetDefinition> Targets { get; set; } = new();
}

public sealed class TargetDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Metric { get; set; } = "";
    /// <summary>Perfil de populacao (vazio = todos).</summary>
    public string Profile { get; set; } = "";
    /// <summary>Nivel (metricas hours_to_level e reached_level).</summary>
    public int Level { get; set; }
    /// <summary>Tipos de atividade somados (activity_share). "travel" = deslocamento; "idle" = espera.</summary>
    public List<string> Types { get; set; } = new();
    /// <summary>weekday ou weekend (dau_ratio).</summary>
    public string DayKind { get; set; } = "";
    public double Min { get; set; }
    public double Max { get; set; }
    public string Unit { get; set; } = "";
    /// <summary>Por que a faixa foi escolhida (aparece no relatorio).</summary>
    public string Rationale { get; set; } = "";
    /// <summary>
    /// Classe (D-039a): "scenario" = regra do cenario (rotina calibrada; medida com humor
    /// neutralizado; falhar indica erro); "provisional" = balanceamento a recalibrar;
    /// "future" = depende de sistemas que ainda nao existem (so acompanhamento).
    /// </summary>
    public string Kind { get; set; } = "";
}

public static class TargetKinds
{
    public const string Scenario = "scenario";
    public const string Provisional = "provisional";
    public const string Future = "future";
    public static readonly string[] All = { Scenario, Provisional, Future };
}

public static class TargetMetrics
{
    public const string HoursToLevel = "hours_to_level";
    public const string ReachedLevel = "reached_level";
    public const string SessionsPerWeek = "sessions_per_week";
    public const string HoursPerWeek = "hours_per_week";
    public const string ActivityShare = "activity_share";
    public const string DauRatio = "dau_ratio";
    public const string WeekendLift = "weekend_lift";
    public const string PeakOnlineRatio = "peak_online_ratio";
    /// <summary>% do XP ganho que veio de quests (o resto vem de atividades).</summary>
    public const string QuestXpShare = "quest_xp_share";
    /// <summary>Quests concluidas por personagem (media).</summary>
    public const string QuestsPerCharacter = "quests_per_character";
    /// <summary>% dos cadastrados que estao desistentes no fim (D-039).</summary>
    public const string QuitShare = "quit_share";
    /// <summary>% dos cadastrados inativos no fim (D-039).</summary>
    public const string InactiveShare = "inactive_share";
    /// <summary>% de quem ficou inativo ou desistiu e voltou ao menos uma vez (D-039).</summary>
    public const string ReturnShare = "return_share";

    public static readonly string[] All =
    {
        HoursToLevel, ReachedLevel, SessionsPerWeek, HoursPerWeek, ActivityShare, DauRatio, WeekendLift, PeakOnlineRatio,
        QuestXpShare, QuestsPerCharacter, QuitShare, InactiveShare, ReturnShare,
    };

    /// <summary>Pseudo-tipos aceitos em activity_share alem dos tipos de atividade.</summary>
    public const string Travel = "travel";
    public const string Idle = "idle";
}

public sealed class TargetsLoadResult
{
    public TargetsFile File { get; init; } = new();
    public List<string> Errors { get; init; } = new();
    public string Hash { get; init; } = "";
    public string Path { get; init; } = "";
    public bool Success => Errors.Count == 0;
}

public static class TargetsLoader
{
    public const string FileName = "targets.json";

    public static TargetsLoadResult Load(string path, ContentDatabase content)
    {
        if (!File.Exists(path)) return new TargetsLoadResult { Errors = { $"Metas não encontradas: {path}" }, Path = path };
        string text = File.ReadAllText(path, Encoding.UTF8);
        string hash = ContentLoader.ComputeHash(new Dictionary<string, string> { [FileName] = text });
        TargetsFile? file;
        try
        {
            file = JsonSerializer.Deserialize<TargetsFile>(text, ContentLoader.JsonOptions);
        }
        catch (JsonException ex)
        {
            return new TargetsLoadResult { Errors = { $"{FileName}: JSON inválido (linha {ex.LineNumber + 1}): {ex.Message}" }, Hash = hash, Path = path };
        }
        if (file == null) return new TargetsLoadResult { Errors = { $"{FileName}: arquivo vazio." }, Hash = hash, Path = path };
        var errors = Validate(file, content);
        return new TargetsLoadResult { File = file, Errors = errors, Hash = hash, Path = path };
    }

    public static List<string> Validate(TargetsFile file, ContentDatabase content)
    {
        var errors = new List<string>();
        if (file.SchemaVersion != 1) errors.Add($"{FileName}: schemaVersion {file.SchemaVersion} não suportada.");
        if (file.WarmupDays < 0) errors.Add($"{FileName} :: warmupDays — não pode ser negativo.");
        if (file.RareActivityPermille is < 0 or > 1000) errors.Add($"{FileName} :: rareActivityPermille — use 0 a 1000.");

        var knownTypes = new HashSet<string>(ActivityTypes.All, StringComparer.Ordinal) { TargetMetrics.Travel, TargetMetrics.Idle };
        var ids = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < file.Targets.Count; i++)
        {
            var t = file.Targets[i];
            string p = $"{FileName} :: targets[{i}]";
            if (string.IsNullOrWhiteSpace(t.Id)) errors.Add($"{p}.id — obrigatório.");
            else if (!ids.Add(t.Id)) errors.Add($"{p}.id — '{t.Id}' repetido.");
            if (string.IsNullOrWhiteSpace(t.Name)) errors.Add($"{p}.name — obrigatório.");
            if (!TargetKinds.All.Contains(t.Kind))
                errors.Add($"{p}.kind — use {string.Join(", ", TargetKinds.All)} (regra do cenário, provisória ou dependente de sistema futuro).");
            if (!TargetMetrics.All.Contains(t.Metric))
                errors.Add($"{p}.metric — '{t.Metric}' desconhecida; use {string.Join(", ", TargetMetrics.All)}.");
            if (t.Profile.Length > 0 && !content.Profiles.ContainsKey(t.Profile))
                errors.Add($"{p}.profile — perfil '{t.Profile}' não existe no conteúdo.");
            if (t.Metric is TargetMetrics.HoursToLevel or TargetMetrics.ReachedLevel && t.Level < 2)
                errors.Add($"{p}.level — obrigatório (2 ou mais) para {t.Metric}.");
            if (t.Metric == TargetMetrics.ActivityShare)
            {
                if (t.Types.Count == 0) errors.Add($"{p}.types — obrigatório para activity_share.");
                foreach (var ty in t.Types)
                    if (!knownTypes.Contains(ty)) errors.Add($"{p}.types — tipo '{ty}' desconhecido.");
            }
            if (t.Metric == TargetMetrics.DauRatio && t.DayKind is not ("weekday" or "weekend"))
                errors.Add($"{p}.dayKind — use weekday ou weekend.");
            if (t.Min > t.Max) errors.Add($"{p} — min ({t.Min}) maior que max ({t.Max}).");
        }
        return errors;
    }
}

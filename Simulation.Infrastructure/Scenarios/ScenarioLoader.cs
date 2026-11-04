using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Simulation.Core.Commands;
using Simulation.Core.Time;
using Simulation.Infrastructure.Content;

namespace Simulation.Infrastructure.Scenarios;

/// <summary>
/// Cenario de execucao headless: tamanho da populacao e comandos agendados.
/// Fica em scenario.json ao lado do conteudo, mas nao faz parte do conteudo publicado.
/// </summary>
public sealed class ScenarioFile
{
    public int SchemaVersion { get; set; }
    public string Description { get; set; } = "";
    public int PopulationSize { get; set; } = 100;
    public List<ScenarioCommand> Commands { get; set; } = new();
}

public sealed class ScenarioCommand
{
    /// <summary>Instante no formato "D{dia} HH:mm" (dia 0 = data inicial), ex.: "D0 08:00".</summary>
    public string At { get; set; } = "";
    public string Type { get; set; } = "";
    public JsonElement Args { get; set; }
}

public sealed class ScheduledScenarioCommand
{
    public ScheduledScenarioCommand(long tick, ISimCommand command)
    {
        Tick = tick;
        Command = command;
    }

    public long Tick { get; }
    public ISimCommand Command { get; }
}

public sealed class ScenarioLoadResult
{
    public ScenarioFile Scenario { get; init; } = new();
    public List<ScheduledScenarioCommand> Commands { get; init; } = new();
    public List<string> Errors { get; init; } = new();
    public string ScenarioHash { get; init; } = "";
    public bool Success => Errors.Count == 0;
}

public static class ScenarioLoader
{
    public const string FileName = "scenario.json";
    private static readonly Regex AtPattern = new(@"^D(\d+) (\d{2}):(\d{2})$", RegexOptions.CultureInvariant);

    public static ScenarioLoadResult Load(string path)
    {
        var errors = new List<string>();
        if (!File.Exists(path))
            return new ScenarioLoadResult { Errors = { $"Cenário não encontrado: {path}" } };

        string text = File.ReadAllText(path, Encoding.UTF8);
        string hash = ContentLoader.ComputeHash(new Dictionary<string, string> { [FileName] = text });
        ScenarioFile? file;
        try
        {
            file = JsonSerializer.Deserialize<ScenarioFile>(text, ContentLoader.JsonOptions);
        }
        catch (JsonException ex)
        {
            return new ScenarioLoadResult { Errors = { $"{FileName}: JSON inválido (linha {ex.LineNumber + 1}): {ex.Message}" }, ScenarioHash = hash };
        }
        if (file == null) return new ScenarioLoadResult { Errors = { $"{FileName}: arquivo vazio." }, ScenarioHash = hash };
        if (file.SchemaVersion != 1) errors.Add($"{FileName}: schemaVersion {file.SchemaVersion} não suportada.");
        if (file.PopulationSize < 0) errors.Add($"{FileName}: populationSize não pode ser negativo.");

        var cmds = new List<ScheduledScenarioCommand>();
        for (int i = 0; i < file.Commands.Count; i++)
        {
            var c = file.Commands[i];
            string p = $"{FileName} :: commands[{i}]";
            if (!TryParseAt(c.At, out long tick))
            {
                errors.Add($"{p}.at — '{c.At}' inválido; use \"D{{dia}} HH:mm\", ex.: \"D0 08:00\".");
                continue;
            }
            if (tick == 0)
            {
                errors.Add($"{p}.at — comandos precisam ser após o início (D0 00:00 já está processado).");
                continue;
            }
            var cmd = CommandFactory.Create(c.Type, c.Args, out string? err);
            if (cmd == null) errors.Add($"{p} — {err}");
            else cmds.Add(new ScheduledScenarioCommand(tick, cmd));
        }
        return new ScenarioLoadResult { Scenario = file, Commands = cmds, Errors = errors, ScenarioHash = hash };
    }

    public static bool TryParseAt(string at, out long tick)
    {
        tick = 0;
        var m = AtPattern.Match(at ?? "");
        if (!m.Success) return false;
        long day = long.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        int hh = int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
        int mm = int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
        if (hh > 23 || mm > 59) return false;
        tick = SimTime.Days(day) + SimTime.Hours(hh) + SimTime.Minutes(mm);
        return true;
    }
}

/// <summary>Cria comandos do Core a partir de tipo + argumentos JSON (cenarios, replays).</summary>
public static class CommandFactory
{
    public static ISimCommand? Create(string type, JsonElement args, out string? error)
    {
        error = null;
        switch (type)
        {
            case SetServerOpenCommand.TypeId:
                if (args.ValueKind != JsonValueKind.Object || !args.TryGetProperty("open", out var open) ||
                    (open.ValueKind != JsonValueKind.True && open.ValueKind != JsonValueKind.False))
                {
                    error = "set_server_open exige args { \"open\": true|false }.";
                    return null;
                }
                return new SetServerOpenCommand(open.GetBoolean());
            default:
                error = $"Tipo de comando '{type}' desconhecido.";
                return null;
        }
    }
}

using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Infrastructure.Content;

namespace Simulation.Tests;

internal static class TestSupport
{
    private static readonly Lazy<string> RepoRootLazy = new(() =>
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "MMORPG.sln")))
            dir = dir.Parent;
        return dir?.FullName ?? throw new InvalidOperationException("Raiz do repositório (MMORPG.sln) não encontrada.");
    });

    public static string RepoRoot => RepoRootLazy.Value;
    public static string PresetFolder => Path.Combine(RepoRoot, "Content", "presets", "inicial");

    private static readonly Lazy<ContentDatabase> PresetLazy = new(() =>
    {
        var r = ContentLoader.LoadFolder(PresetFolder);
        if (!r.Success) throw new InvalidOperationException(string.Join("\n", r.Errors));
        return r.Database!;
    });

    /// <summary>Preset inicial validado (compartilhado; nao alterar).</summary>
    public static ContentDatabase Preset => PresetLazy.Value;

    /// <summary>Conjunto novo e mutavel do preset (para testes de validacao).</summary>
    public static ContentSet FreshPresetSet()
    {
        var r = ContentLoader.LoadFolder(PresetFolder);
        return r.Set!;
    }

    public static SimulationEngine NewEngine(ulong seed = 42, int population = 100) =>
        WorldFactory.Create(Preset, new WorldConfig { Seed = seed, PopulationSize = population });

    public static string NewTempDir()
    {
        var path = Path.Combine(Path.GetTempPath(), "mmorpg-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    public static void CopyDirectory(string from, string to)
    {
        Directory.CreateDirectory(to);
        foreach (var f in Directory.GetFiles(from))
            File.Copy(f, Path.Combine(to, Path.GetFileName(f)), overwrite: true);
    }
}

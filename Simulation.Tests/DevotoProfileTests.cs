using Simulation.Core.Content;
using Simulation.Core.Population;

namespace Simulation.Tests;

/// <summary>
/// Perfil Devoto (R5, D-052): raro (cerca de 2% depois da normalizacao dos pesos), importancia e
/// habito muito altos, sessao normal longa, so estudantes e sem ocupacao (pesos do proprio perfil).
/// Nenhuma regra propria: o comportamento sai dos parametros e das decisoes de todos.
/// </summary>
public class DevotoProfileTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private static readonly List<Person> People = Enumerable.Range(1, 20_000).Select(id => PopulationGenerator.Generate(id, C, 31)).ToList();

    [Fact]
    [Trait("Classe", "Regra do preset")]
    public void Devoto_PorVoltaDeDoisPorCento_SoEstudantesESemOcupacao()
    {
        var devotos = People.Where(p => p.ProfileId == "devoto").ToList();
        double share = devotos.Count * 100.0 / People.Count;
        Assert.InRange(share, 1.6, 2.4);
        var allowed = C.Profiles["devoto"].OccupationWeights.Keys.ToHashSet();
        Assert.All(devotos, p => Assert.Contains(p.OccupationId, allowed));
        foreach (var occ in allowed) Assert.Contains(devotos, p => p.OccupationId == occ);
        // Os outros perfis continuam com o peso geral (ocupacoes fora da lista do devoto aparecem).
        Assert.Contains(People, p => p.ProfileId == "hardcore" && !allowed.Contains(p.OccupationId));
        // Variacao individual: nem todos iguais.
        Assert.True(devotos.Select(p => p.Traits.SessionMinutes).Distinct().Count() > 20);
        Assert.True(devotos.Select(p => p.Traits.SelfControl).Distinct().Count() > 20);
        Assert.InRange(devotos.Average(p => p.Traits.GameImportance), 900, 960);
        Assert.InRange(devotos.Average(p => p.Traits.SessionMinutes), 330, 390);
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void OcupacoesDoPerfil_InvalidasSaoErro()
    {
        var set = TestSupport.FreshPresetSet();
        set.Population.Profiles.First(p => p.Id == "devoto").OccupationWeights = new Dictionary<string, int> { ["nao_existe"] = 5 };
        Assert.Contains(ContentValidator.Validate(set), e => e.Severity == IssueSeverity.Error && e.Path.Contains("occupationWeights"));
        var set2 = TestSupport.FreshPresetSet();
        set2.Population.Profiles.First(p => p.Id == "devoto").OccupationWeights = new Dictionary<string, int> { ["sem_ocupacao"] = 0 };
        Assert.Contains(ContentValidator.Validate(set2), e => e.Severity == IssueSeverity.Error && e.Path.Contains("occupationWeights"));
    }

    /// <summary>Sem bonus escondido: o codigo da simulacao nao trata o devoto (nem outro perfil) pelo id.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Simulacao_NaoTemRegraPorIdDePerfil()
    {
        var core = Path.Combine(TestSupport.RepoRoot, "Simulation.Core");
        foreach (var file in Directory.EnumerateFiles(core, "*.cs", SearchOption.AllDirectories).Where(f => !f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")))
        {
            // So o codigo conta: comentarios podem citar perfis como exemplo.
            string code = string.Join("\n", File.ReadAllLines(file).Where(l => !l.TrimStart().StartsWith("//", StringComparison.Ordinal)));
            foreach (var id in C.ProfilesOrdered.Select(p => p.Id))
                Assert.False(code.Contains($"\"{id}\"", StringComparison.Ordinal), $"{Path.GetFileName(file)} trata o perfil \"{id}\" pelo id");
        }
    }
}

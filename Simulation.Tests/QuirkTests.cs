using Simulation.Core.Content;
using Simulation.Core.Population;

namespace Simulation.Tests;

/// <summary>
/// Traits (P4b): catalogo em traits.json, sorteio na criacao (quantidade pela tabela, 40% de
/// negativas por vaga, coerencia limitada, exclusoes com traits e personalidade) e fluxo proprio.
/// </summary>
public class QuirkTests
{
    private static ContentDatabase C => TestSupport.Preset;

    private static readonly Lazy<List<Person>> Sample = new(() =>
        Enumerable.Range(1, 10_000).Select(id => PopulationGenerator.Generate(id, C, 77)).ToList());

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Catalogo_CarregadoDoPreset()
    {
        Assert.Equal(30, C.Quirks.Count);
        Assert.Equal(25, C.QuirksOrdered.Count(q => q.Drawn));
        Assert.All(C.QuirksOrdered.Where(q => q.Acquisition == QuirkAcquisitions.Acquired), q => Assert.False(q.Drawn));
        Assert.Equal(new[] { 80, 250, 500, 100, 40, 20, 10 }, C.QuirkCatalog.Generation.CountWeights);
        Assert.Equal(400, C.QuirkCatalog.Generation.NegativeChancePermille);
    }

    /// <summary>10 mil pessoas: quantidade pela tabela e cerca de 40% de negativas por vaga.</summary>
    [Fact]
    [Trait("Classe", "Estatistico")]
    public void Sorteio_QuantidadeEClassificacao()
    {
        var people = Sample.Value;
        var w = C.QuirkCatalog.Generation.CountWeights;
        int total = w.Sum();
        for (int k = 0; k < w.Count; k++)
        {
            double expected = w[k] * 100.0 / total;
            double observed = people.Count(p => p.Quirks.Count == k) * 100.0 / people.Count;
            Assert.True(Math.Abs(observed - expected) <= 2.0, $"{k} traits: {observed:0.0}% (esperado {expected:0.0}%)");
        }
        var all = people.SelectMany(p => p.Quirks).ToList();
        double negative = all.Count(q => C.Quirks[q.Id].Polarity == QuirkPolarities.Negative) * 100.0 / all.Count;
        Assert.InRange(negative, 37.5, 42.5);
        // So positivas ou so negativas continuam possiveis (sem proporcao fixa por pessoa).
        Assert.Contains(people, p => p.Quirks.Count >= 2 && p.Quirks.All(q => C.Quirks[q.Id].Polarity == QuirkPolarities.Negative));
        Assert.Contains(people, p => p.Quirks.Count >= 2 && p.Quirks.All(q => C.Quirks[q.Id].Polarity == QuirkPolarities.Positive));
    }

    /// <summary>Nenhuma combinacao excluida, nenhuma repetida, nada fora do sorteio.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Sorteio_RespeitaExclusoes()
    {
        foreach (var p in Sample.Value)
        {
            var ids = p.Quirks.Select(q => q.Id).ToList();
            Assert.Equal(ids.Count, ids.Distinct(StringComparer.Ordinal).Count());
            foreach (var id in ids)
            {
                var q = C.Quirks[id];
                Assert.True(q.Drawn, $"{id} nao entra no sorteio");
                Assert.DoesNotContain(p.PersonalityId, q.ExcludesPersonalities);
                foreach (var other in ids.Where(x => x != id)) Assert.DoesNotContain(other, q.Excludes);
                Assert.Equal(QuirkAcquisitions.Innate, p.Quirks.First(x => x.Id == id).Source);
            }
        }
    }

    /// <summary>Coerencia: as afinidades pesam na chance, sem tornar nada obrigatorio.</summary>
    [Fact]
    [Trait("Classe", "Estatistico")]
    public void Sorteio_CoerenteComAPessoa()
    {
        var people = Sample.Value;
        double Rate(string quirk, Func<Person, bool> group)
        {
            var g = people.Where(group).ToList();
            return g.Count(p => p.Quirks.Any(q => q.Id == quirk)) / (double)Math.Max(1, g.Count);
        }
        Assert.True(Rate("pavio_curto", p => p.PersonalityId == "agressivo") > 2 * Rate("pavio_curto", p => p.PersonalityId == "pacifico"));
        Assert.True(Rate("maratonista", p => p.ProfileId is "hardcore" or "devoto") > 1.5 * Rate("maratonista", p => p.ProfileId == "casual"));
        Assert.True(Rate("observador", p => p.Preferences.Exploration >= 700) > Rate("observador", p => p.Preferences.Exploration <= 300));
        // Exclusao com a personalidade e, ao mesmo tempo, combinacao incomum ainda possivel.
        Assert.Equal(0, Rate("lobo_solitario", p => p.PersonalityId == "sociavel"));
        Assert.True(Rate("pavio_curto", p => p.PersonalityId == "pacifico") > 0);
    }

    /// <summary>Fluxo proprio: ligar as traits nao muda nenhum outro atributo de ninguem.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Traits_NaoMudamOsDemaisAtributos()
    {
        var set = TestSupport.FreshPresetSet();
        set.Quirks.Generation.Enabled = false;
        var off = ContentDatabase.Create(set);
        for (int id = 1; id <= 1500; id++)
        {
            var a = PopulationGenerator.Generate(id, C, 21);
            var b = PopulationGenerator.Generate(id, off, 21);
            Assert.Empty(b.Quirks);
            foreach (var k in PersonTraits.DynamicKeys) Assert.Equal(a.Traits.Get(k), b.Traits.Get(k));
            foreach (var k in PersonPreferences.Keys) Assert.Equal(a.Preferences.Get(k), b.Preferences.Get(k));
            Assert.Equal((a.PersonalityId, a.OccupationId, a.ResidenceId, a.BedtimeMinute, a.HabitualStartHour, a.Traits.SelfControl),
                (b.PersonalityId, b.OccupationId, b.ResidenceId, b.BedtimeMinute, b.HabitualStartHour, b.Traits.SelfControl));
        }
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Sorteio_DeterministicoESemTraitsNoCenarioControlado()
    {
        var a = PopulationGenerator.Generate(42, C, 5);
        var b = PopulationGenerator.Generate(42, C, 5);
        Assert.Equal(a.Quirks.Select(q => q.Id), b.Quirks.Select(q => q.Id));
        var cell = new ControlledCell("casual", C.Occupations.Keys.First());
        Assert.Empty(PopulationGenerator.Generate(42, C, 5, cell).Quirks);
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Afinidade_TermosEMultiplicadorLimitado()
    {
        var p = PopulationGenerator.Generate(3, C, 9);
        var hc = new QuirkAffinityDefinition { Key = "profile." + p.ProfileId, StrengthPermille = 800 };
        Assert.Equal(1800, QuirkSystem.AffinityTerm(C, p, hc));
        Assert.Equal(1000, QuirkSystem.AffinityTerm(C, p, new QuirkAffinityDefinition { Key = "profile.nenhum", StrengthPermille = 800 }));
        var q = new QuirkDefinition { Id = "x", Affinities = { hc, hc, hc } };   // 1,8^3 = 5,8: limitado a 3
        Assert.Equal(C.QuirkCatalog.Generation.MaxMultiplierPermille, QuirkSystem.Multiplier(C, p, q));
        var low = new QuirkAffinityDefinition { Key = "profile." + p.ProfileId, StrengthPermille = -1000 };
        Assert.Equal(C.QuirkCatalog.Generation.MinMultiplierPermille, QuirkSystem.Multiplier(C, p, new QuirkDefinition { Id = "y", Affinities = { low } }));
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Validador_Traits()
    {
        static bool Err(Action<ContentSet> change, string path)
        {
            var set = TestSupport.FreshPresetSet();
            change(set);
            return ContentValidator.Validate(set).Any(i => i.Severity == IssueSeverity.Error && i.File == ContentValidator.TraitsFile && i.Path.Contains(path));
        }
        QuirkDefinition Q(ContentSet s, string id) => s.Quirks.Traits.First(q => q.Id == id);
        Assert.False(Err(_ => { }, ""));
        Assert.True(Err(s => Q(s, "persistente").Polarity = "neutra", "polarity"));
        Assert.True(Err(s => Q(s, "matador_novatos").Drawn = true, "drawn"));
        Assert.True(Err(s => Q(s, "persistente").Excludes.Add("voar"), "excludes"));
        Assert.True(Err(s => Q(s, "persistente").ExcludesPersonalities.Add("zangado"), "excludesPersonalities"));
        Assert.True(Err(s => Q(s, "persistente").Affinities.Add(new QuirkAffinityDefinition { Key = "traits.talent", StrengthPermille = 300 }), "affinities"));
        Assert.True(Err(s => Q(s, "persistente").Affinities.Add(new QuirkAffinityDefinition { Key = "traits.patience", Direction = "meio", StrengthPermille = 300 }), "direction"));
        Assert.True(Err(s => Q(s, "persistente").System = "M9", "system"));
        Assert.True(Err(s => s.Quirks.Generation.MaxTotal = 3, "countWeights"));
        Assert.True(Err(s => s.Quirks.Generation.NegativeChancePermille = 1200, "negativeChancePermille"));
    }
}

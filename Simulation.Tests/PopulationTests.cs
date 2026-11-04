using Simulation.Core.Engine;
using Simulation.Core.Population;

namespace Simulation.Tests;

public class PopulationTests
{
    private static List<Person> Generate(int count, ulong seed) =>
        Enumerable.Range(1, count).Select(id => PopulationGenerator.Generate(id, TestSupport.Preset, seed)).ToList();

    private static int[] Vector(Person p)
    {
        var t = p.Traits;
        return new[]
        {
            t.Pve, t.Pvp, t.Trade, t.Exploration, t.Socializing, t.Sociability, t.Aggressiveness, t.Patience, t.Loyalty,
            t.RiskAversion, t.MechanicalSkill, t.GameKnowledge, t.Coordination, t.CheatPropensity, t.Opportunism,
            t.LeisureBudgetCentsPerMonth, t.WillingnessToPay, t.PaidAdvantageSensitivity, t.PreferredStartHour,
            t.SessionMinutes, t.DaysPerWeek, t.WeekendBoost,
        };
    }

    [Fact]
    public void MesmaSeedEId_GeramMesmaPessoa()
    {
        var a = PopulationGenerator.Generate(17, TestSupport.Preset, 42);
        var b = PopulationGenerator.Generate(17, TestSupport.Preset, 42);
        Assert.Equal(a.Name, b.Name);
        Assert.Equal(a.ProfileId, b.ProfileId);
        Assert.Equal(a.AppearanceSeed, b.AppearanceSeed);
        Assert.Equal(Vector(a), Vector(b));
    }

    [Fact]
    public void PessoaNaoDependeDoTamanhoDaPopulacao()
    {
        var small = TestSupport.NewEngine(seed: 42, population: 10).World;
        var large = TestSupport.NewEngine(seed: 42, population: 100).World;
        for (int id = 1; id <= 10; id++)
            Assert.Equal(Vector(small.GetPerson(id)), Vector(large.GetPerson(id)));
    }

    [Fact]
    public void ParametrosFicamNasFaixasDocumentadas()
    {
        foreach (var p in Generate(2000, 1))
        {
            var t = p.Traits;
            foreach (int v in new[] { t.Pve, t.Pvp, t.Trade, t.Exploration, t.Socializing, t.Sociability, t.Aggressiveness,
                         t.Patience, t.Loyalty, t.RiskAversion, t.MechanicalSkill, t.GameKnowledge, t.Coordination,
                         t.CheatPropensity, t.Opportunism, t.WillingnessToPay, t.PaidAdvantageSensitivity, t.WeekendBoost })
                Assert.InRange(v, 0, 1000);
            Assert.InRange(t.PreferredStartHour, 0, 23);
            Assert.InRange(t.SessionMinutes, 10, 720);
            Assert.InRange(t.DaysPerWeek, 0, 7);
            Assert.True(t.LeisureBudgetCentsPerMonth >= 0);
            Assert.Equal(LifecycleState.PotentialInterested, p.State);
        }
    }

    [Theory]
    [InlineData(1UL)]
    [InlineData(2UL)]
    [InlineData(3UL)]
    public void DistribuicaoDePerfis_SegueOsPesos(ulong seed)
    {
        // n = 2000; desvio padrao da proporcao <= 1,1 p.p.; tolerancia de 4 p.p.
        var persons = Generate(2000, seed);
        int totalWeight = TestSupport.Preset.Profiles.Values.Sum(p => p.Weight);
        foreach (var prof in TestSupport.Preset.Profiles.Values)
        {
            double expected = prof.Weight / (double)totalWeight;
            double actual = persons.Count(p => p.ProfileId == prof.Id) / 2000.0;
            Assert.InRange(actual, expected - 0.04, expected + 0.04);
        }
    }

    [Fact]
    public void Diversidade_NinguemEhClone()
    {
        var persons = Generate(500, 42);
        var distinct = persons.Select(p => string.Join(",", Vector(p))).Distinct().Count();
        Assert.Equal(500, distinct);
        Assert.True(persons.Select(p => p.AppearanceSeed).Distinct().Count() == 500, "Aparência deve ser única por pessoa.");
    }

    [Fact]
    public void CorrelacaoControlada_NemTodoCompetitivoEhAgressivo()
    {
        var persons = Generate(3000, 42);
        double r = Pearson(persons.Select(p => (double)p.Traits.Pvp).ToArray(), persons.Select(p => (double)p.Traits.Aggressiveness).ToArray());
        // Mistura de perfis cria correlacao positiva, mas longe de identidade.
        Assert.InRange(r, 0.15, 0.85);

        var pvpLovers = persons.Where(p => p.Traits.Pvp >= 700).ToList();
        Assert.True(pvpLovers.Count > 100);
        Assert.Contains(pvpLovers, p => p.Traits.Aggressiveness < 400);

        // Frequente nao implica habilidoso.
        var frequent = persons.Where(p => p.Traits.DaysPerWeek >= 6).ToList();
        Assert.Contains(frequent, p => p.Traits.MechanicalSkill < 450);
    }

    private static double Pearson(double[] x, double[] y)
    {
        double mx = x.Average(), my = y.Average();
        double sxy = 0, sxx = 0, syy = 0;
        for (int i = 0; i < x.Length; i++)
        {
            sxy += (x[i] - mx) * (y[i] - my);
            sxx += (x[i] - mx) * (x[i] - mx);
            syy += (y[i] - my) * (y[i] - my);
        }
        return sxy / Math.Sqrt(sxx * syy);
    }
}

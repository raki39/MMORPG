using Simulation.Core.Random;

namespace Simulation.Tests;

public class RandomTests
{
    [Theory]
    [InlineData("", 0xcbf29ce484222325UL)]
    [InlineData("a", 0xaf63dc4c8601ec8cUL)]
    [InlineData("foobar", 0x85944171f73967e8UL)]
    public void Fnv1a64_ConfereComVetoresDeReferencia(string text, ulong expected)
    {
        Assert.Equal(expected, StableHash.Fnv1a64(text));
    }

    [Fact]
    public void SplitMix64_ConfereComVetorDeReferencia()
    {
        // Saidas de referencia do splitmix64 (Vigna) para seed 1234567; viram o estado inicial do xoshiro.
        var s = new DeterministicRng(1234567UL).State;
        Assert.Equal(6457827717110365317UL, s.S0);
        Assert.Equal(3203168211198807973UL, s.S1);
        Assert.Equal(9817491932198370423UL, s.S2);
        Assert.Equal(4593380528125082431UL, s.S3);
    }

    [Fact]
    public void Xoshiro256StarStar_ConfereComImplementacaoDeReferencia()
    {
        // Estado {1,2,3,4}: saidas calculadas a mao a partir do algoritmo de referencia.
        var rng = new DeterministicRng(new RngState(1, 2, 3, 4));
        Assert.Equal(11520UL, rng.NextUInt64());
        Assert.Equal(0UL, rng.NextUInt64());
        Assert.Equal(1509978240UL, rng.NextUInt64());
    }

    [Fact]
    public void MesmaSeed_MesmaSequencia()
    {
        var a = new DeterministicRng(42);
        var b = new DeterministicRng(42);
        for (int i = 0; i < 1000; i++) Assert.Equal(a.NextUInt64(), b.NextUInt64());
    }

    [Fact]
    public void EstadoSalvo_ContinuaIdentico()
    {
        var a = new DeterministicRng(7);
        for (int i = 0; i < 37; i++) a.NextUInt64();
        var b = new DeterministicRng(a.State);
        for (int i = 0; i < 100; i++) Assert.Equal(a.NextInt(1000), b.NextInt(1000));
    }

    [Fact]
    public void Fluxos_SaoIndependentesEEstaveis()
    {
        var s1 = new RngStreams(42);
        var s2 = new RngStreams(42);
        // Consumir um fluxo nao altera outro.
        for (int i = 0; i < 50; i++) s1.Get("combate").NextUInt64();
        Assert.Equal(s2.Get("mercado").NextUInt64(), s1.Get("mercado").NextUInt64());
        Assert.NotEqual(RngStreams.DeriveSeed(42, "combate"), RngStreams.DeriveSeed(42, "mercado"));
        Assert.NotEqual(RngStreams.DeriveSeed(42, "pessoa", 1), RngStreams.DeriveSeed(42, "pessoa", 2));
        Assert.NotEqual(RngStreams.DeriveSeed(42, "pessoa", 1), RngStreams.DeriveSeed(43, "pessoa", 1));
    }

    [Fact]
    public void NextInt_RespeitaLimitesEEhAproximadamenteUniforme()
    {
        var rng = new DeterministicRng(123);
        var counts = new int[10];
        const int n = 100_000;
        for (int i = 0; i < n; i++)
        {
            int v = rng.NextInt(10);
            Assert.InRange(v, 0, 9);
            counts[v]++;
        }
        // Esperado 10.000 por balde; desvio padrao ~95. Tolerancia de 6 desvios.
        foreach (int c in counts) Assert.InRange(c, 9_430, 10_570);
    }

    [Fact]
    public void ChancePermille_Extremos()
    {
        var rng = new DeterministicRng(1);
        for (int i = 0; i < 100; i++)
        {
            Assert.False(rng.ChancePermille(0));
            Assert.True(rng.ChancePermille(1000));
        }
    }

    [Fact]
    public void PickWeighted_IgnoraPesoZeroERespeitaProporcao()
    {
        var rng = new DeterministicRng(5);
        var counts = new int[3];
        for (int i = 0; i < 30_000; i++) counts[rng.PickWeighted(new[] { 1, 0, 2 })]++;
        Assert.Equal(0, counts[1]);
        Assert.InRange(counts[2] / (double)counts[0], 1.85, 2.15);
    }

    [Fact]
    public void Ruido_FicaDentroDaDispersao()
    {
        var rng = new DeterministicRng(9);
        for (int i = 0; i < 10_000; i++) Assert.InRange(rng.NextCenteredNoise(200), -200, 200);
    }
}

namespace Simulation.Core.Random;

/// <summary>Estado serializavel de um gerador (para saves e hash de estado).</summary>
public readonly struct RngState : IEquatable<RngState>
{
    public RngState(ulong s0, ulong s1, ulong s2, ulong s3)
    {
        S0 = s0; S1 = s1; S2 = s2; S3 = s3;
    }

    public ulong S0 { get; }
    public ulong S1 { get; }
    public ulong S2 { get; }
    public ulong S3 { get; }

    public bool Equals(RngState other) => S0 == other.S0 && S1 == other.S1 && S2 == other.S2 && S3 == other.S3;
    public override bool Equals(object? obj) => obj is RngState o && Equals(o);
    public override int GetHashCode() => HashCode.Combine(S0, S1, S2, S3);
}

/// <summary>
/// Gerador pseudoaleatorio deterministico: xoshiro256** 1.0 (Blackman e Vigna),
/// semeado por SplitMix64. Algoritmo e versao fixos (D-004); qualquer mudanca aqui
/// quebra reprodutibilidade de saves e precisa de nova versao de algoritmo.
/// Apenas operacoes inteiras: sem float/double no dominio.
/// </summary>
public sealed class DeterministicRng
{
    public const string AlgorithmId = "xoshiro256**-1.0/splitmix64";

    private ulong _s0, _s1, _s2, _s3;

    public DeterministicRng(ulong seed)
    {
        ulong sm = seed;
        _s0 = NextSplitMix(ref sm);
        _s1 = NextSplitMix(ref sm);
        _s2 = NextSplitMix(ref sm);
        _s3 = NextSplitMix(ref sm);
    }

    public DeterministicRng(RngState state)
    {
        if (state.S0 == 0 && state.S1 == 0 && state.S2 == 0 && state.S3 == 0)
            throw new ArgumentException("Estado xoshiro nao pode ser todo zero.", nameof(state));
        _s0 = state.S0; _s1 = state.S1; _s2 = state.S2; _s3 = state.S3;
    }

    public RngState State => new RngState(_s0, _s1, _s2, _s3);

    private static ulong NextSplitMix(ref ulong x)
    {
        x += 0x9E3779B97F4A7C15UL;
        return StableHash.Mix64(x);
    }

    private static ulong Rotl(ulong x, int k) => (x << k) | (x >> (64 - k));

    public ulong NextUInt64()
    {
        ulong result = Rotl(_s1 * 5, 7) * 9;
        ulong t = _s1 << 17;
        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = Rotl(_s3, 45);
        return result;
    }

    public uint NextUInt32() => (uint)(NextUInt64() >> 32);

    /// <summary>Inteiro uniforme em [0, maxExclusive). Metodo de Lemire, sem vies.</summary>
    public int NextInt(int maxExclusive)
    {
        if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        uint range = (uint)maxExclusive;
        ulong m = (ulong)NextUInt32() * range;
        uint low = (uint)m;
        if (low < range)
        {
            uint threshold = (uint)(-(int)range) % range;
            while (low < threshold)
            {
                m = (ulong)NextUInt32() * range;
                low = (uint)m;
            }
        }
        return (int)(m >> 32);
    }

    /// <summary>Inteiro uniforme em [minInclusive, maxExclusive).</summary>
    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        long span = (long)maxExclusive - minInclusive;
        if (span > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Intervalo grande demais.");
        return minInclusive + NextInt((int)span);
    }

    /// <summary>Inteiro uniforme em [minInclusive, maxInclusive].</summary>
    public int NextIntInclusive(int minInclusive, int maxInclusive) => NextInt(minInclusive, maxInclusive + 1);

    /// <summary>Verdadeiro com probabilidade permille/1000.</summary>
    public bool ChancePermille(int permille)
    {
        if (permille <= 0) return false;
        if (permille >= 1000) return true;
        return NextInt(1000) < permille;
    }

    /// <summary>
    /// Ruido aproximadamente normal (media 0) em [-spread, spread], pela media de 3 uniformes.
    /// Distribuicao triangular-suave; suficiente para diversidade de atributos.
    /// </summary>
    public int NextCenteredNoise(int spread)
    {
        if (spread < 0) throw new ArgumentOutOfRangeException(nameof(spread));
        if (spread == 0) return 0;
        int sum = NextIntInclusive(-spread, spread) + NextIntInclusive(-spread, spread) + NextIntInclusive(-spread, spread);
        return sum / 3;
    }

    /// <summary>Escolha ponderada por pesos inteiros nao negativos (ordem dos pesos importa).</summary>
    public int PickWeighted(IReadOnlyList<int> weights)
    {
        long total = 0;
        foreach (int w in weights)
        {
            if (w < 0) throw new ArgumentException("Peso negativo.", nameof(weights));
            total += w;
        }
        if (total <= 0) throw new ArgumentException("Soma de pesos deve ser positiva.", nameof(weights));
        if (total > int.MaxValue) throw new ArgumentException("Soma de pesos grande demais.", nameof(weights));
        int roll = NextInt((int)total);
        for (int i = 0; i < weights.Count; i++)
        {
            roll -= weights[i];
            if (roll < 0) return i;
        }
        throw new InvalidOperationException("Inalcancavel.");
    }
}

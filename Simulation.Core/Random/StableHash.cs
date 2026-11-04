using System.Text;

namespace Simulation.Core.Random;

/// <summary>
/// Hash estavel entre execucoes e plataformas (string.GetHashCode e aleatorizado no .NET).
/// FNV-1a 64 bits sobre os bytes UTF-8 da string.
/// </summary>
public static class StableHash
{
    public const ulong FnvOffsetBasis = 14695981039346656037UL;
    public const ulong FnvPrime = 1099511628211UL;

    public static ulong Fnv1a64(string text)
    {
        ulong hash = FnvOffsetBasis;
        foreach (byte b in Encoding.UTF8.GetBytes(text))
        {
            hash ^= b;
            hash *= FnvPrime;
        }
        return hash;
    }

    /// <summary>Finalizador do SplitMix64: espalha bits de uma entrada de 64 bits.</summary>
    public static ulong Mix64(ulong z)
    {
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}

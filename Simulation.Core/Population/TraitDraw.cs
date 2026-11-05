namespace Simulation.Core.Population;

using Simulation.Core.Random;

/// <summary>
/// Sorteio de atributos no formato triangular e dedicacao compartilhada (experimento).
/// Tudo em inteiros. Uma "posicao" e um quantil em [0, Scale): a posicao p vira o valor cuja
/// probabilidade acumulada e p / Scale.
/// </summary>
public static class TraitDraw
{
    /// <summary>Resolucao das posicoes (um bilionesimo).</summary>
    public const long Scale = 1_000_000_000;

    /// <summary>Posicao uniforme em [0, Scale) com tres sorteios de 0 a 999 (tres digitos cada).</summary>
    public static long Position(DeterministicRng rng)
    {
        long a = rng.NextInt(1000), b = rng.NextInt(1000), c = rng.NextInt(1000);
        return a * 1_000_000 + b * 1_000 + c;
    }

    /// <summary>
    /// Mistura a posicao de dedicacao (shared) com a posicao propria do atributo (own) e devolve
    /// uma posicao ainda uniforme. Com w = influencia/1000, a soma s = w*shared + (1-w)*own tem
    /// distribuicao trapezoidal (soma de duas uniformes de larguras w e 1-w); aplicar a funcao
    /// acumulada F dessa soma (transformacao integral de probabilidade) devolve uma uniforme.
    /// Com a = min(w, 1-w) e b = max(w, 1-w):
    ///   F(s) = s^2 / (2ab)            para s menor que a;
    ///   F(s) = (s - a/2) / b          para s entre a e b;
    ///   F(s) = 1 - (1-s)^2 / (2ab)    para s maior que b.
    /// Influencia 0 devolve own; 1000 devolve shared. A transformacao e monotona e simetrica.
    /// </summary>
    public static long Combine(long shared, long own, int influencePermille)
    {
        if (influencePermille <= 0) return own;
        if (influencePermille >= 1000) return shared;
        long w = influencePermille;
        long s = (w * shared + (1000 - w) * own) / 1000;
        long a = Math.Min(w, 1000 - w), b = Math.Max(w, 1000 - w);
        long lowEnd = a * 1_000_000, highStart = b * 1_000_000;   // a e b em unidades de Scale
        long f;
        if (s < lowEnd) f = s * s / (2000 * a * b);
        else if (s <= highStart) f = (s - a * 500_000) * 1000 / b;
        else { long r = Scale - s; f = Scale - r * r / (2000 * a * b); }
        return Math.Clamp(f, 0, Scale - 1);
    }

    /// <summary>
    /// Valor da distribuicao triangular (min, mode, max) na posicao dada (inversa da acumulada),
    /// arredondado ao inteiro mais proximo:
    ///   x = min + raiz(u (max-min)(mode-min))        se u menor que (mode-min)/(max-min);
    ///   x = max - raiz((1-u)(max-min)(max-mode))      caso contrario.
    /// </summary>
    public static int Triangular(long position, int min, int mode, int max)
    {
        long range = (long)max - min;
        if (range <= 0) return min;
        long left = (long)mode - min, right = (long)max - mode;
        long p = Math.Clamp(position, 0, Scale - 1);
        // Raiz em milesimos: raiz(u * R * L) * 1000 = raiz(p * R * L / 1000), pois Scale = 10^9.
        if ((Int128)p * range < (Int128)left * Scale)
        {
            long milli = (long)ISqrt((UInt128)((Int128)p * range * left / 1000));
            return (int)Math.Clamp(min + (milli + 500) / 1000, min, max);
        }
        long milliR = (long)ISqrt((UInt128)((Int128)(Scale - p) * range * right / 1000));
        return (int)Math.Clamp(max - (milliR + 500) / 1000, min, max);
    }

    /// <summary>Mediana da triangular: o valor na posicao do meio (nao confundir com a moda).</summary>
    public static int Median(int min, int mode, int max) => Triangular(Scale / 2, min, mode, max);

    /// <summary>Raiz quadrada inteira (parte inteira), pelo metodo de Newton.</summary>
    public static UInt128 ISqrt(UInt128 n)
    {
        if (n < 2) return n;
        int bits = 128 - (int)UInt128.LeadingZeroCount(n);
        UInt128 x = (UInt128)1 << ((bits + 1) / 2);
        while (true)
        {
            UInt128 y = (x + n / x) >> 1;
            if (y >= x) return x;
            x = y;
        }
    }
}

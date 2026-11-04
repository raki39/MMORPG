namespace Simulation.Core.Time;

/// <summary>
/// Unidades de tempo da simulacao. Um tick logico = 100 ms de tempo simulado (D-005).
/// O tick e independente de tempo real, FPS e camera.
/// </summary>
public static class SimTime
{
    public const int TickMilliseconds = 100;
    public const long TicksPerSecond = 1000 / TickMilliseconds;
    public const long TicksPerMinute = TicksPerSecond * 60;
    public const long TicksPerHour = TicksPerMinute * 60;
    public const long TicksPerDay = TicksPerHour * 24;
    public const long TicksPerWeek = TicksPerDay * 7;

    /// <summary>
    /// Converte milissegundos autorais (ex.: castTimeMs) em ticks.
    /// Politica: arredonda para o tick mais proximo (meio tick arredonda para cima);
    /// qualquer duracao positiva dura pelo menos 1 tick. Negativos sao invalidos.
    /// </summary>
    public static long MillisecondsToTicks(long milliseconds)
    {
        if (milliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(milliseconds), "Duracao negativa nao e permitida.");
        if (milliseconds == 0)
            return 0;
        long ticks = (milliseconds + TickMilliseconds / 2) / TickMilliseconds;
        return ticks == 0 ? 1 : ticks;
    }

    public static long Days(long days) => days * TicksPerDay;
    public static long Hours(long hours) => hours * TicksPerHour;
    public static long Minutes(long minutes) => minutes * TicksPerMinute;
}

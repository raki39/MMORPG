namespace Simulation.Core.Time;

/// <summary>Relogio logico da campanha. So avanca pelo motor da simulacao.</summary>
public sealed class SimClock
{
    public SimClock(CampaignCalendar calendar, long tick = 0)
    {
        Calendar = calendar;
        Tick = tick;
    }

    public CampaignCalendar Calendar { get; }

    /// <summary>Ultimo tick processado.</summary>
    public long Tick { get; private set; }

    public DateTime Now => Calendar.ToDateTime(Tick);
    public long DayIndex => Calendar.DayIndex(Tick);
    public DayKind DayKind => Calendar.KindOfTick(Tick);

    internal void AdvanceTo(long tick)
    {
        if (tick < Tick)
            throw new InvalidOperationException($"O relogio nao volta no tempo ({Tick} -> {tick}).");
        Tick = tick;
    }
}

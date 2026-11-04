namespace Simulation.Core.Time;

public enum DayKind
{
    Weekday = 0,
    Weekend = 1,
    Holiday = 2,
}

/// <summary>Feriado recorrente (todo ano no mesmo dia/mes) ou de data fixa.</summary>
public sealed class Holiday
{
    public Holiday(int month, int day, int? year, string name)
    {
        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));
        if (day < 1 || day > 31) throw new ArgumentOutOfRangeException(nameof(day));
        Month = month;
        Day = day;
        Year = year;
        Name = name;
    }

    public int Month { get; }
    public int Day { get; }
    /// <summary>Nulo = recorrente todo ano.</summary>
    public int? Year { get; }
    public string Name { get; }

    public bool Matches(DateTime date) =>
        date.Month == Month && date.Day == Day && (Year == null || Year == date.Year);
}

/// <summary>
/// Calendario da campanha: data inicial (tick 0 = 00:00 dessa data) e feriados.
/// Usa DateTime apenas como aritmetica de calendario; nunca le o relogio do computador.
/// </summary>
public sealed class CampaignCalendar
{
    private readonly List<Holiday> _holidays;

    public CampaignCalendar(DateTime startDate, IEnumerable<Holiday> holidays)
    {
        StartDate = new DateTime(startDate.Year, startDate.Month, startDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
        _holidays = holidays.ToList();
    }

    public DateTime StartDate { get; }
    public IReadOnlyList<Holiday> Holidays => _holidays;

    public DateTime ToDateTime(long tick)
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        // 1 tick = 100 ms = 1_000_000 ticks de DateTime (100 ns).
        return StartDate.AddTicks(checked(tick * SimTime.TickMilliseconds * TimeSpan.TicksPerMillisecond));
    }

    public long DayIndex(long tick) => tick / SimTime.TicksPerDay;

    public long StartOfDay(long dayIndex) => dayIndex * SimTime.TicksPerDay;

    public Holiday? HolidayOn(DateTime date)
    {
        foreach (var h in _holidays)
            if (h.Matches(date)) return h;
        return null;
    }

    public DayKind KindOf(DateTime date)
    {
        if (HolidayOn(date) != null) return DayKind.Holiday;
        return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday
            ? DayKind.Weekend
            : DayKind.Weekday;
    }

    public DayKind KindOfTick(long tick) => KindOf(ToDateTime(tick).Date);
}

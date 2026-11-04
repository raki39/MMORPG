using Simulation.Core.Content;
using Simulation.Core.Events;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Infrastructure.Validation;

/// <summary>Medidas de uma execucao (uma seed). Valores de meta: NaN = sem amostra.</summary>
public sealed class RunMetrics
{
    public ulong Seed { get; init; }
    public int Population { get; init; }
    public int Days { get; init; }
    public string FinalStateHash { get; set; } = "";
    public double SimulationSeconds { get; set; }
    public Dictionary<string, double> Values { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, string> Notes { get; } = new(StringComparer.Ordinal);
    /// <summary>% de personagens que chegaram ao nivel (metas hours_to_level).</summary>
    public Dictionary<string, double> Reached { get; } = new(StringComparer.Ordinal);
    /// <summary>Escolhas por "zona/atividade" (todas as atividades do conteudo, inclusive zero).</summary>
    public SortedDictionary<string, long> Choices { get; } = new(StringComparer.Ordinal);
    /// <summary>Tempo conectado por tipo (ticks), com "travel" (deslocamento) e "idle" (espera).</summary>
    public SortedDictionary<string, long> TimeByType { get; } = new(StringComparer.Ordinal);
    public List<ProfileStats> Profiles { get; } = new();
    /// <summary>Nivel de cada personagem no fim (para diagnosticar atividades com nivel minimo).</summary>
    public List<int> FinalLevels { get; } = new();
}

public sealed class ProfileStats
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public int Persons { get; init; }
    public double SessionsPerWeek { get; init; }
    public double HoursPerWeek { get; init; }
    public double MedianLevel { get; init; }
    public double P90Level { get; init; }
    /// <summary>% das sessoes em dia util que comecam no horario nobre (18h-23h).</summary>
    public double PrimeTimeStartPct { get; init; }
}

/// <summary>
/// Coleta medidas durante a execucao a partir dos eventos drenados por dia e do estado
/// final. Nao altera a simulacao; serve ao relatorio de validacao (plano 17.1).
/// </summary>
public sealed class ValidationCollector
{
    private readonly int _warmupDays;
    private readonly long _windowStart;
    private readonly Dictionary<long, long> _openSession = new();
    private readonly Dictionary<long, int> _sessionsInWindow = new();
    private readonly Dictionary<long, long> _playedInWindow = new();
    private readonly Dictionary<long, (int Prime, int Total)> _weekdayStarts = new();
    public const int PrimeTimeFromMinute = 18 * 60, PrimeTimeToMinute = 23 * 60;
    private readonly SortedDictionary<string, long> _choices = new(StringComparer.Ordinal);

    public ValidationCollector(ContentDatabase content, int warmupDays)
    {
        _warmupDays = warmupDays;
        _windowStart = SimTime.Days(warmupDays);
        foreach (var r in content.Regions.Values)
            foreach (var a in r.AllActivities())
                _choices[r.Id + "/" + a.Id] = 0;
    }

    /// <summary>Inicio da janela de medicao (apos a semana de lancamento).</summary>
    public long WindowStartTick => _windowStart;

    /// <summary>Lista de sessoes iniciadas em dia util na janela: (pessoa, minuto do dia).</summary>
    public List<(long PersonId, int Minute)> WeekdaySessionStarts { get; } = new();

    public void Consume(IEnumerable<SimEvent> events, WorldState w)
    {
        var cal = w.Clock.Calendar;
        foreach (var e in events)
        {
            switch (e.Type)
            {
                case "session_started":
                {
                    long pid = e.Entities[0].Id;
                    _openSession[pid] = e.Tick;
                    if (e.Tick < _windowStart) break;
                    _sessionsInWindow[pid] = _sessionsInWindow.GetValueOrDefault(pid) + 1;
                    if (cal.KindOfTick(e.Tick) == DayKind.Weekday)
                    {
                        var at = cal.ToDateTime(e.Tick);
                        int minute = at.Hour * 60 + at.Minute;
                        WeekdaySessionStarts.Add((pid, minute));
                        bool prime = minute >= PrimeTimeFromMinute && minute < PrimeTimeToMinute;
                        var s = _weekdayStarts.GetValueOrDefault(pid);
                        _weekdayStarts[pid] = (s.Prime + (prime ? 1 : 0), s.Total + 1);
                    }
                    break;
                }
                case "session_ended":
                {
                    long pid = e.Entities[0].Id;
                    if (_openSession.Remove(pid, out long start)) AddPlayed(pid, start, e.Tick);
                    break;
                }
                case "activity_chosen":
                    if (e.Detail != null) _choices[e.Detail] = _choices.GetValueOrDefault(e.Detail) + 1;
                    break;
            }
        }
    }

    private void AddPlayed(long pid, long start, long end)
    {
        long from = Math.Max(start, _windowStart);
        if (end > from) _playedInWindow[pid] = _playedInWindow.GetValueOrDefault(pid) + (end - from);
    }

    public RunMetrics Finish(WorldState w, ulong seed, int days, IEnumerable<TargetDefinition> targets)
    {
        long end = w.Clock.Tick;
        foreach (var kv in _openSession) AddPlayed(kv.Key, kv.Value, end);
        _openSession.Clear();

        var m = new RunMetrics { Seed = seed, Population = w.Persons.Count, Days = days };
        foreach (var kv in _choices) m.Choices[kv.Key] = kv.Value;

        double weeks = Math.Max(0, days - _warmupDays) / 7.0;
        var chars = w.Characters.ToList();
        foreach (var c in chars)
        {
            m.FinalLevels.Add(c.Level);
            long played = c.PlayedTicksAt(end);
            long sum = 0;
            foreach (var kv in c.ActivityTicksByType)
            {
                m.TimeByType[kv.Key] = m.TimeByType.GetValueOrDefault(kv.Key) + kv.Value;
                sum += kv.Value;
            }
            m.TimeByType[TargetMetrics.Travel] = m.TimeByType.GetValueOrDefault(TargetMetrics.Travel) + Math.Max(0, played - sum);
        }

        foreach (var profile in w.Content.ProfilesOrdered)
        {
            var group = chars.Where(c => w.GetPerson(c.PersonId).ProfileId == profile.Id).ToList();
            var levels = group.Select(c => (double)c.Level).ToList();
            int prime = 0, total = 0;
            foreach (var c in group)
            {
                var s = _weekdayStarts.GetValueOrDefault(c.PersonId);
                prime += s.Prime;
                total += s.Total;
            }
            m.Profiles.Add(new ProfileStats
            {
                Id = profile.Id,
                Name = profile.Name,
                Persons = group.Count,
                SessionsPerWeek = PerPersonWeekly(group.Select(c => (long)c.PersonId), _sessionsInWindow, weeks, 1),
                HoursPerWeek = PerPersonWeekly(group.Select(c => (long)c.PersonId), _playedInWindow, weeks, SimTime.TicksPerHour),
                MedianLevel = Percentile(levels, 0.5),
                P90Level = Percentile(levels, 0.9),
                PrimeTimeStartPct = total > 0 ? prime * 100.0 / total : double.NaN,
            });
        }

        foreach (var t in targets)
        {
            var (value, note, reached) = Evaluate(t, w, chars, end, weeks);
            m.Values[t.Id] = value;
            if (note.Length > 0) m.Notes[t.Id] = note;
            if (!double.IsNaN(reached)) m.Reached[t.Id] = reached;
        }
        return m;
    }

    private (double Value, string Note, double Reached) Evaluate(TargetDefinition t, WorldState w,
        List<Simulation.Core.Population.Character> all, long end, double weeks)
    {
        var chars = t.Profile.Length == 0 ? all : all.Where(c => w.GetPerson(c.PersonId).ProfileId == t.Profile).ToList();
        var persons = chars.Select(c => (long)c.PersonId);
        var history = w.History.Where(d => d.DayIndex >= _warmupDays && d.Registered > 0).ToList();

        if (t.Metric == TargetMetrics.HoursToLevel)
        {
            var reached = chars.Where(c => c.PlayedTicksAtLevel.Count >= t.Level - 1)
                .Select(c => c.PlayedTicksAtLevel[t.Level - 2] / (double)SimTime.TicksPerHour).ToList();
            if (chars.Count == 0 || reached.Count == 0) return (double.NaN, "ninguém chegou", 0);
            return (Percentile(reached, 0.5), "", reached.Count * 100.0 / chars.Count);
        }
        var (v, n) = EvaluateOther(t, w, chars, end, weeks, persons, history);
        return (v, n, double.NaN);
    }

    private (double Value, string Note) EvaluateOther(TargetDefinition t, WorldState w,
        List<Simulation.Core.Population.Character> chars, long end, double weeks, IEnumerable<long> persons,
        List<DailyRecord> history)
    {
        switch (t.Metric)
        {
            case TargetMetrics.ReachedLevel:
                return chars.Count == 0 ? (double.NaN, "") : (chars.Count(c => c.Level >= t.Level) * 100.0 / chars.Count, "");
            case TargetMetrics.SessionsPerWeek:
                return (PerPersonWeekly(persons, _sessionsInWindow, weeks, 1), "");
            case TargetMetrics.HoursPerWeek:
                return (PerPersonWeekly(persons, _playedInWindow, weeks, SimTime.TicksPerHour), "");
            case TargetMetrics.ActivityShare:
            {
                long played = 0, part = 0;
                foreach (var c in chars)
                {
                    long p = c.PlayedTicksAt(end);
                    long sum = c.ActivityTicksByType.Values.Sum();
                    played += p;
                    foreach (var ty in t.Types)
                        part += ty == TargetMetrics.Travel ? Math.Max(0, p - sum) : c.ActivityTicksByType.GetValueOrDefault(ty);
                }
                return played == 0 ? (double.NaN, "") : (part * 100.0 / played, "");
            }
            case TargetMetrics.DauRatio:
            {
                bool weekday = t.DayKind == "weekday";
                var days = history.Where(d => (d.DayKind == DayKind.Weekday) == weekday).ToList();
                return days.Count == 0 ? (double.NaN, "") : (days.Average(d => d.ActivePlayers * 100.0 / d.Registered), $"{days.Count} dias");
            }
            case TargetMetrics.WeekendLift:
            {
                var wd = history.Where(d => d.DayKind == DayKind.Weekday).ToList();
                var we = history.Where(d => d.DayKind != DayKind.Weekday).ToList();
                if (wd.Count == 0 || we.Count == 0) return (double.NaN, "");
                double a = wd.Average(d => (double)d.ActivePlayers);
                return a == 0 ? (double.NaN, "") : (we.Average(d => (double)d.ActivePlayers) / a, "");
            }
            case TargetMetrics.PeakOnlineRatio:
                return history.Count == 0 ? (double.NaN, "") : (history.Average(d => d.PeakOnline * 100.0 / d.Registered), "");
            default:
                return (double.NaN, "métrica desconhecida");
        }
    }

    private static double PerPersonWeekly(IEnumerable<long> persons, Dictionary<long, int> counts, double weeks, long divisor)
        => PerPersonWeekly(persons, counts.ToDictionary(k => k.Key, k => (long)k.Value), weeks, divisor);

    private static double PerPersonWeekly(IEnumerable<long> persons, Dictionary<long, long> totals, double weeks, long divisor)
    {
        var list = persons.ToList();
        if (list.Count == 0 || weeks <= 0) return double.NaN;
        return list.Average(p => totals.GetValueOrDefault(p) / (double)divisor / weeks);
    }

    /// <summary>Percentil por posicao mais proxima (sem interpolacao); NaN se vazio.</summary>
    public static double Percentile(List<double> values, double q)
    {
        if (values.Count == 0) return double.NaN;
        var sorted = values.OrderBy(v => v).ToList();
        int idx = (int)Math.Ceiling(q * sorted.Count) - 1;
        return sorted[Math.Clamp(idx, 0, sorted.Count - 1)];
    }
}

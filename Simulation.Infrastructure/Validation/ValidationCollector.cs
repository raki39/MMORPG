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
    /// <summary>Continuidade das sessoes (D-026/D-037): trocas, permanencia e motivos.</summary>
    public SortedDictionary<string, double> Continuity { get; } = new(StringComparer.Ordinal);
    /// <summary>Motivos das trocas de plano (fator principal), contagem.</summary>
    public SortedDictionary<string, long> SwitchReasons { get; } = new(StringComparer.Ordinal);
    /// <summary>O que mais incomoda cada pessoa registrada (motivo mais negativo), contagem (D-038).</summary>
    public SortedDictionary<string, long> TopComplaints { get; } = new(StringComparer.Ordinal);
    /// <summary>Desistencias (D-039): motivo principal (o que mais incomodava) e dia, por evento.</summary>
    public SortedDictionary<string, long> QuitReasons { get; } = new(StringComparer.Ordinal);
    public List<int> QuitDays { get; } = new();
    public List<int> QuitLevels { get; } = new();
    public long SessionsExtended { get; set; }
    public long RageQuits { get; set; }
    /// <summary>Notas das sessoes (D-038): sessoes otimas e ruins na janela.</summary>
    public long GreatSessions { get; set; }
    public long BadSessions { get; set; }
    public long RatedSessions { get; set; }
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
    /// <summary>Variacao media dos atributos do jogador desde o inicio (unidades do Core), D-036.</summary>
    public Dictionary<string, double> TraitDelta { get; init; } = new(StringComparer.Ordinal);
    public double GoalsCompleted { get; init; }
    public double GoalsAbandoned { get; init; }
    /// <summary>Retencao no fim (D-039): % desistentes, inativos e quem voltou.</summary>
    public double QuitPct { get; init; }
    public double InactivePct { get; init; }
    public double ReturnedPct { get; init; }
    /// <summary>Humor medio no fim (D-038): satisfacao, frustracao, novidade, engajamento etc.</summary>
    public Dictionary<string, double> Mood { get; init; } = new(StringComparer.Ordinal);
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
    // Continuidade (D-026): ultima escolha da sessao por personagem e tamanho da sequencia atual.
    private readonly Dictionary<long, (string Key, int Run)> _lastChoice = new();
    private long _sessionsWithChoice, _continued, _switched, _zoneSwitched;
    private readonly List<int> _runs = new();
    private readonly SortedDictionary<string, long> _switchReasons = new(StringComparer.Ordinal);
    private long _great, _bad, _rated, _extended, _rage;
    private readonly SortedDictionary<string, long> _quitReasons = new(StringComparer.Ordinal);
    private readonly List<int> _quitDays = new(), _quitLevels = new();

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
                    long cid = w.GetPerson((int)pid).CharacterId;
                    if (_lastChoice.Remove(cid, out var prev) && prev.Run > 0 && e.Tick >= _windowStart) _runs.Add(prev.Run);
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
                    if (e.Tick >= _windowStart) _rated++;
                    if (e.Tick >= _windowStart && e.Causes.Count > 0 && e.Causes[0].Factor == "frustration") _rage++;
                    break;
                }
                case "activity_chosen":
                {
                    if (e.Detail == null) break;
                    _choices[e.Detail] = _choices.GetValueOrDefault(e.Detail) + 1;
                    if (e.Tick < _windowStart) break;
                    long cid = e.Entities[0].Id;
                    if (!_lastChoice.TryGetValue(cid, out var last) || last.Key.Length == 0)
                    {
                        _sessionsWithChoice++;
                        _lastChoice[cid] = (e.Detail, 1);
                    }
                    else if (last.Key == e.Detail)
                    {
                        _continued++;
                        _lastChoice[cid] = (e.Detail, last.Run + 1);
                    }
                    else
                    {
                        _switched++;
                        if (!string.Equals(Zone(last.Key), Zone(e.Detail), StringComparison.Ordinal)) _zoneSwitched++;
                        _runs.Add(last.Run);
                        _lastChoice[cid] = (e.Detail, 1);
                    }
                    break;
                }
                case "player_quit":
                {
                    string why = e.Causes.Count > 1 ? e.Causes[1].Factor : e.Causes.Count > 0 ? e.Causes[0].Factor : "?";
                    if (e.Causes.Count > 0 && e.Causes[0].Factor == "long_inactivity") why = "long_inactivity/" + why;
                    _quitReasons[why] = _quitReasons.GetValueOrDefault(why) + 1;
                    _quitDays.Add((int)(e.Tick / SimTime.TicksPerDay));
                    var c = w.GetCharacter((int)e.Entities[1].Id);
                    _quitLevels.Add(c.Level);
                    break;
                }
                case "session_extended":
                    if (e.Tick >= _windowStart) _extended++;
                    break;
                case "session_great":
                    if (e.Tick >= _windowStart) _great++;
                    break;
                case "session_bad":
                    if (e.Tick >= _windowStart) _bad++;
                    break;
                case "plan_changed":
                    if (e.Tick >= _windowStart && e.Causes.Count > 0)
                        _switchReasons[e.Causes[0].Factor] = _switchReasons.GetValueOrDefault(e.Causes[0].Factor) + 1;
                    break;
            }
        }
    }

    private static string Zone(string key) => key.Split('/')[0];

    private static Dictionary<string, double> MoodOf(List<Simulation.Core.Population.Person> ps)
    {
        double Avg(Func<Simulation.Core.Population.Person, int> f) => ps.Count == 0 ? double.NaN : ps.Average(p => (double)f(p));
        return new Dictionary<string, double>(StringComparer.Ordinal)
        {
            ["satisfaction"] = Avg(p => p.Mood.Satisfaction),
            ["frustration"] = Avg(p => p.Mood.Frustration),
            ["novelty"] = Avg(p => p.Mood.Novelty),
            ["engagement"] = Avg(p => p.Mood.Engagement),
            ["return_intention"] = Avg(p => p.Mood.ReturnIntention),
            ["expectation"] = Avg(p => p.Mood.ProgressExpectation),
            ["progress_ratio"] = Avg(p => p.Mood.LastProgressRatio),
            ["last_score"] = Avg(p => p.Mood.LastSessionScore),
            ["memories"] = Avg(p => p.Memories.Count),
            ["unhappy_pct"] = ps.Count == 0 ? double.NaN : ps.Count(p => p.Mood.Satisfaction < 400) * 100.0 / ps.Count,
        };
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
        foreach (var kv in _lastChoice) if (kv.Value.Run > 0) _runs.Add(kv.Value.Run);
        _lastChoice.Clear();
        double playedHours = _playedInWindow.Values.Sum() / (double)SimTime.TicksPerHour;
        long decisions = _continued + _switched;
        m.Continuity["switches_per_hour"] = playedHours > 0 ? _switched / playedHours : double.NaN;
        m.Continuity["continue_pct"] = decisions > 0 ? _continued * 100.0 / decisions : double.NaN;
        m.Continuity["zone_switches_per_session"] = _sessionsWithChoice > 0 ? _zoneSwitched / (double)_sessionsWithChoice : double.NaN;
        m.Continuity["choices_per_session"] = _sessionsWithChoice > 0 ? (decisions + _sessionsWithChoice) / (double)_sessionsWithChoice : double.NaN;
        m.Continuity["run_cycles"] = _runs.Count > 0 ? _runs.Average() : double.NaN;
        foreach (var kv in _switchReasons) m.SwitchReasons[kv.Key] = kv.Value;

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
                TraitDelta = new[] { "mechanicalSkill", "gameKnowledge", "pve", "exploration", "trade", "riskAversion" }
                    .ToDictionary(k => k, k => group.Count == 0 ? 0 : group.Average(c => (double)(w.GetPerson(c.PersonId).Traits.Get(k) - w.GetPerson(c.PersonId).BaseTraits.Get(k))), StringComparer.Ordinal),
                GoalsCompleted = group.Count == 0 ? 0 : group.Average(c => (double)c.GoalsCompleted),
                GoalsAbandoned = group.Count == 0 ? 0 : group.Average(c => (double)c.GoalsAbandoned),
                Mood = MoodOf(group.Select(c => w.GetPerson(c.PersonId)).ToList()),
                QuitPct = group.Count == 0 ? double.NaN : group.Count(c => w.GetPerson(c.PersonId).State == Simulation.Core.Population.LifecycleState.Quit) * 100.0 / group.Count,
                InactivePct = group.Count == 0 ? double.NaN : group.Count(c => w.GetPerson(c.PersonId).State == Simulation.Core.Population.LifecycleState.Inactive) * 100.0 / group.Count,
                ReturnedPct = group.Count == 0 ? double.NaN : group.Count(c => w.GetPerson(c.PersonId).TimesReturned > 0) * 100.0 / group.Count,
            });
        }

        foreach (var c in chars)
        {
            var p = w.GetPerson(c.PersonId);
            var worst = p.MoodDrivers.Where(kv => kv.Value < 0).OrderBy(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).FirstOrDefault();
            string key = worst.Key ?? "nenhum";
            m.TopComplaints[key] = m.TopComplaints.GetValueOrDefault(key) + 1;
        }
        foreach (var kv in _quitReasons) m.QuitReasons[kv.Key] = kv.Value;
        m.QuitDays.AddRange(_quitDays);
        m.QuitLevels.AddRange(_quitLevels);
        m.SessionsExtended = _extended;
        m.RageQuits = _rage;
        m.GreatSessions = _great;
        m.BadSessions = _bad;
        m.RatedSessions = _rated;

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
            case TargetMetrics.QuestXpShare:
            {
                long q = chars.Sum(c => c.XpFromQuests), a = chars.Sum(c => c.XpFromActivities);
                return q + a == 0 ? (double.NaN, "") : (q * 100.0 / (q + a), "");
            }
            case TargetMetrics.QuestsPerCharacter:
                return chars.Count == 0 ? (double.NaN, "") : (chars.Average(c => (double)c.QuestHistory.Values.Sum(r => r.Completions)), "");
            case TargetMetrics.QuitShare:
                return chars.Count == 0 ? (double.NaN, "") : (chars.Count(c => w.GetPerson(c.PersonId).State == Simulation.Core.Population.LifecycleState.Quit) * 100.0 / chars.Count, "");
            case TargetMetrics.InactiveShare:
                return chars.Count == 0 ? (double.NaN, "") : (chars.Count(c => w.GetPerson(c.PersonId).State == Simulation.Core.Population.LifecycleState.Inactive) * 100.0 / chars.Count, "");
            case TargetMetrics.ReturnShare:
            {
                var left = chars.Select(c => w.GetPerson(c.PersonId)).Where(p => p.TimesInactive + p.TimesQuit > 0).ToList();
                return left.Count == 0 ? (double.NaN, "ninguém saiu") : (left.Count(p => p.TimesReturned > 0) * 100.0 / left.Count, $"{left.Count} saíram");
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

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
    /// <summary>Rotina por ocupacao (P2).</summary>
    public List<OccupationStats> Occupations { get; } = new();
    /// <summary>Motivo de cada dia da agenda (P2): played, no_free_time, other_leisure, low_engagement, inactive...</summary>
    public SortedDictionary<string, long> DayReasons { get; } = new(StringComparer.Ordinal);
    /// <summary>Por localidade (P1): pessoas registradas, sessoes e hora media de inicio local e do servidor.</summary>
    public List<LocalityStats> Localities { get; } = new();
    /// <summary>Perfis da rodada controlada (humor neutralizado), quando houver (D-039a).</summary>
    public List<ProfileStats> ControlledProfiles { get; } = new();
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
    /// <summary>Distribuicoes (R3).</summary>
    public DistributionData Distribution { get; } = new();
    /// <summary>Sono e trabalho (P3/D-044), contagens na janela: extensoes por motivo e com sono cortado,
    /// noites encurtadas e minutos cortados, sessoes na pausa e escapadas, chamados do trabalho.</summary>
    public SortedDictionary<string, double> SleepWork { get; } = new(StringComparer.Ordinal);
    public long RageQuits { get; set; }
    /// <summary>Notas das sessoes (D-038): sessoes otimas e ruins na janela.</summary>
    public long GreatSessions { get; set; }
    public long BadSessions { get; set; }
    public long RatedSessions { get; set; }
}

/// <summary>Uma sessao fechada na janela de medicao (R3): quem, quando, quanto e quanto foi estendido.</summary>
/// <summary>
/// Uma sessao (R3/v3). Planned: minutos ao comecar (a sessao normal ou o que cabia); Continued: minutos
/// acrescentados pela decisao de continuar; ExtendedMinutes: ficou acordado jogando (corte de sono);
/// StartReason e EndReason: os motivos do inicio e do fim; Disposition e FreeMinutes: da agenda do dia.
/// </summary>
public sealed record SessionSample(int PersonId, string Profile, string Occupation, string Kind, long Start, long End, int ExtendedMinutes, long LocalDay,
    int Planned = 0, int Continued = 0, int Disposition = 0, int FreeMinutes = -1, string StartReason = "", string EndReason = "")
{
    public int Minutes => (int)((End - Start) / SimTime.TicksPerMinute);
}

/// <summary>Um dia da agenda (R3), na janela de medicao.</summary>
public sealed record DaySample(int PersonId, string Profile, string Occupation, long LocalDay, int FreeMinutes, int PlannedMinutes, int Sessions, int WorkSessions, string Reason, bool FreeDay = false,
    int Sittings = 0, int Returns = 0, int DeclinedReturns = 0, int SkippedAfterWork = 0);

/// <summary>Medicao de distribuicoes (R3): referencia antes de mudar a vontade de jogar (R4).</summary>
public sealed class DistributionData
{
    public List<SessionSample> Sessions { get; } = new();
    public List<DaySample> Days { get; } = new();
    /// <summary>Pessoas registradas por (perfil, ocupacao).</summary>
    public Dictionary<(string Profile, string Occupation), int> Persons { get; } = new();
    public double Weeks { get; set; }
    /// <summary>Sono cortado pelo jogo por periodo de sono (minutos somados das sessoes do periodo).</summary>
    public List<int> SleepCutPerPeriod { get; } = new();
}

public sealed class OccupationStats
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public int Persons { get; init; }
    public double SessionsPerWeek { get; init; }
    public double HoursPerWeek { get; init; }
    /// <summary>Minutos livres medios em dias com obrigacao e em dias livres (agenda dos ultimos dias).</summary>
    public double FreeMinutesBusyDay { get; init; }
    public double FreeMinutesFreeDay { get; init; }
    /// <summary>% dos dias da agenda sem janela que comporte uma sessao.</summary>
    public double NoFreeTimePct { get; init; }
    public double MeanIncomeReais { get; init; }
    public double MeanSupportReais { get; init; }
    /// <summary>Sono (P3): necessidade media, sono medio da agenda em dia com obrigacao e em dia livre (recuperacao),
    /// deficit medio no fim e % cansados (motivo "tired" possivel).</summary>
    public double MeanSleepNeedMinutes { get; init; }
    public double MeanSleepBusyMinutes { get; init; }
    public double MeanSleepFreeMinutes { get; init; }
    public double MeanDeficitMinutes { get; init; }
    public double TiredPct { get; init; }
    /// <summary>Trabalho remoto (D-044): sessoes na pausa e escapadas por pessoa por semana; % dos dias com obrigacao com escapada.</summary>
    public double BreakSessionsPerWeek { get; init; }
    public double SlackSessionsPerWeek { get; init; }
    public double SlackDayPct { get; init; }
}

public sealed class LocalityStats
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public int UtcOffsetMinutes { get; init; }
    public int Persons { get; init; }
    public int ForeignNationals { get; init; }
    public long Sessions { get; init; }
    public double MeanLocalStartHour { get; init; }
    public double MeanServerStartHour { get; init; }
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
    /// <summary>Dificuldade de parar e sono (P3): extensoes e minutos de sono cortados por pessoa por semana; deficit medio e % cansados no fim.</summary>
    public double ExtensionsPerWeek { get; init; }
    public double SleepCutMinutesPerWeek { get; init; }
    public double MeanDeficitMinutes { get; init; }
    public double TiredPct { get; init; }
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
    /// <summary>Por localidade: sessoes e soma dos vetores das horas de inicio (media circular: a meia-noite nao distorce).</summary>
    private readonly SortedDictionary<string, (long Sessions, double LocalX, double LocalY, double ServerX, double ServerY)> _localities = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, long> _quitReasons = new(StringComparer.Ordinal);
    private readonly List<int> _quitDays = new(), _quitLevels = new();
    // Sono e trabalho (P3/D-044), na janela.
    private readonly Dictionary<long, int> _extByPerson = new(), _breakByPerson = new(), _slackByPerson = new();
    private readonly Dictionary<long, long> _cutByPerson = new();
    private readonly SortedDictionary<string, double> _sleepWork = new(StringComparer.Ordinal);
    // R3: sessoes abertas (inicio, tipo, minutos estendidos) e fechadas na janela; corte por periodo de sono.
    private readonly Dictionary<long, (long Start, string Kind, int Extended, int Planned, int Continued, string Why)> _r3Open = new();
    private readonly List<SessionSample> _r3Sessions = new();
    private readonly Dictionary<(long Person, long LocalDay), int> _r3CutByNight = new();
    private void CountSleepWork(string key, double amount = 1) => _sleepWork[key] = _sleepWork.GetValueOrDefault(key) + amount;

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
                    _r3Open[pid] = (e.Tick, e.Causes.Count > 0 ? e.Causes[0].Factor : "normal", 0,
                        int.TryParse((e.Detail ?? "").Split(' ')[0], out int plannedMin) ? plannedMin : 0, 0, e.Causes.Count > 1 ? e.Causes[1].Factor : "");
                    long cid = w.GetPerson((int)pid).CharacterId;
                    if (_lastChoice.Remove(cid, out var prev) && prev.Run > 0 && e.Tick >= _windowStart) _runs.Add(prev.Run);
                    if (e.Tick < _windowStart) break;
                    _sessionsInWindow[pid] = _sessionsInWindow.GetValueOrDefault(pid) + 1;
                    if (e.Causes.Count > 0 && e.Causes[0].Factor == "work_break") { _breakByPerson[pid] = _breakByPerson.GetValueOrDefault(pid) + 1; CountSleepWork("break_sessions"); }
                    if (e.Causes.Count > 0 && e.Causes[0].Factor == "during_work") { _slackByPerson[pid] = _slackByPerson.GetValueOrDefault(pid) + 1; CountSleepWork("slack_sessions"); }
                    // Horario e dia local da pessoa (P1): fuso e feriados da residencia.
                    var person = w.GetPerson((int)pid);
                    var at = cal.ToDateTime(e.Tick + person.LocalOffsetMinutes * SimTime.TicksPerMinute);
                    var server = cal.ToDateTime(e.Tick);
                    var lstat = _localities.GetValueOrDefault(person.ResidenceId);
                    var (lx, ly) = ClockVector(at.Hour * 60 + at.Minute);
                    var (sx, sy) = ClockVector(server.Hour * 60 + server.Minute);
                    _localities[person.ResidenceId] = (lstat.Sessions + 1, lstat.LocalX + lx, lstat.LocalY + ly, lstat.ServerX + sx, lstat.ServerY + sy);
                    if (w.CalendarOf(person).KindOf(at.Date) == DayKind.Weekday)
                    {
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
                    if (_r3Open.Remove(pid, out var r3) && r3.Start >= _windowStart)
                    {
                        var who = w.GetPerson((int)pid);
                        long sday = Simulation.Core.Behavior.BehaviorSystem.LocalDay(who, r3.Start);
                        // Agenda do dia da sessao (a de antes da meia-noite, para quem comecou de madrugada).
                        var rec = who.DayPlans.LastOrDefault(r => r.LocalDay == sday && r.Reason == "played")
                                  ?? who.DayPlans.LastOrDefault(r => r.LocalDay == sday - 1 && r.Reason == "played");
                        _r3Sessions.Add(new SessionSample((int)pid, who.ProfileId, who.OccupationId, r3.Kind, r3.Start, e.Tick, r3.Extended, sday,
                            r3.Planned, r3.Continued, rec?.DispositionPermille ?? 0, rec?.FreeMinutes ?? -1, r3.Why, e.Causes.Count > 0 ? e.Causes[0].Factor : ""));
                    }
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
                case "session_continued":
                    if (_r3Open.TryGetValue(e.Entities[0].Id, out var openC) && int.TryParse((e.Detail ?? "").Trim('+', ' ', 'm', 'i', 'n'), out int contMin))
                        _r3Open[e.Entities[0].Id] = (openC.Start, openC.Kind, openC.Extended, openC.Planned, openC.Continued + contMin, openC.Why);
                    break;
                case "session_extended":
                    if (e.Tick < _windowStart) break;
                    _extended++;
                    _extByPerson[e.Entities[0].Id] = _extByPerson.GetValueOrDefault(e.Entities[0].Id) + 1;
                    if (_r3Open.TryGetValue(e.Entities[0].Id, out var openR3) && int.TryParse((e.Detail ?? "").Trim('+', ' ', 'm', 'i', 'n'), out int extMin))
                        _r3Open[e.Entities[0].Id] = (openR3.Start, openR3.Kind, openR3.Extended + extMin, openR3.Planned, openR3.Continued, openR3.Why);
                    if (e.Causes.Count > 0) CountSleepWork("ext_" + e.Causes[0].Factor);
                    if (e.Causes.Any(c => c.Factor == "sleep_cut")) CountSleepWork("ext_sleep_cut");
                    break;
                case "stayed_up_late":
                {
                    if (e.Tick < _windowStart) break;
                    int cut = e.Causes.Count > 1 ? e.Causes[1].WeightPermille : 0;
                    _cutByPerson[e.Entities[0].Id] = _cutByPerson.GetValueOrDefault(e.Entities[0].Id) + cut;
                    var sleeper = w.GetPerson((int)e.Entities[0].Id);
                    // O periodo de sono e a noite que comeca no dia acordado da saida (local, antes do amanhecer = dia anterior).
                    long night = Simulation.Core.Behavior.BehaviorSystem.LocalDay(sleeper, e.Tick - 6 * SimTime.TicksPerHour);
                    _r3CutByNight[(e.Entities[0].Id, night)] = _r3CutByNight.GetValueOrDefault((e.Entities[0].Id, night)) + cut;
                    CountSleepWork("late_nights");
                    CountSleepWork("late_minutes", cut);
                    break;
                }
                case "work_interruption":
                    if (e.Tick >= _windowStart && e.Causes.Count > 0) CountSleepWork("interrupt_" + e.Causes[0].Factor);
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

    /// <summary>Cansado o bastante para "tired" ser o motivo de nao jogar (P3).</summary>
    private static bool Tired(WorldState w, Simulation.Core.Population.Person p) =>
        Simulation.Core.Behavior.DailySchedule.Fatigue(p, w.Content.Routine) >= w.Content.Routine.TiredReasonFatigue;

    /// <summary>Minuto do dia como vetor no circulo de 24 h (relatorio; nao entra na simulacao).</summary>
    public static (double X, double Y) ClockVector(int minuteOfDay)
    {
        double a = minuteOfDay / 1440.0 * 2 * Math.PI;
        return (Math.Cos(a), Math.Sin(a));
    }

    /// <summary>
    /// Media circular das horas (0 a 24): a media aritmetica de horas do relogio erra quando ha
    /// horarios dos dois lados da meia-noite. Assim, a diferenca entre as medias local e do
    /// servidor e exatamente o fuso.
    /// </summary>
    public static double CircularMeanHour(double sumX, double sumY)
    {
        double h = Math.Atan2(sumY, sumX) / (2 * Math.PI) * 24;
        return h < 0 ? h + 24 : h;
    }

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
                ExtensionsPerWeek = PerPersonWeekly(group.Select(c => (long)c.PersonId), _extByPerson, weeks, 1),
                SleepCutMinutesPerWeek = PerPersonWeekly(group.Select(c => (long)c.PersonId), _cutByPerson, weeks, 1),
                MeanDeficitMinutes = group.Count == 0 ? double.NaN : group.Average(c => (double)w.GetPerson(c.PersonId).SleepDeficitMinutes),
                TiredPct = group.Count == 0 ? double.NaN : group.Count(c => Tired(w, w.GetPerson(c.PersonId))) * 100.0 / group.Count,
            });
        }

        foreach (var c in chars)
        {
            var p = w.GetPerson(c.PersonId);
            var worst = p.MoodDrivers.Where(kv => kv.Value < 0).OrderBy(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).FirstOrDefault();
            string key = worst.Key ?? "nenhum";
            m.TopComplaints[key] = m.TopComplaints.GetValueOrDefault(key) + 1;
        }
        foreach (var o in w.Content.OccupationsOrdered)
        {
            var ps = chars.Select(c => w.GetPerson(c.PersonId)).Where(p => p.OccupationId == o.Id).ToList();
            var ids = ps.Select(p => (long)p.Id).ToList();
            var busyDays = ps.SelectMany(p => p.DayPlans).Where(d => !d.FreeDay && d.Reason != "came_back").ToList();
            var freeDays = ps.SelectMany(p => p.DayPlans).Where(d => d.FreeDay && d.Reason != "came_back").ToList();
            var allDays = busyDays.Concat(freeDays).ToList();
            m.Occupations.Add(new OccupationStats
            {
                Id = o.Id, Name = o.Name, Persons = ps.Count,
                SessionsPerWeek = PerPersonWeekly(ids, _sessionsInWindow, weeks, 1),
                HoursPerWeek = PerPersonWeekly(ids, _playedInWindow, weeks, SimTime.TicksPerHour),
                FreeMinutesBusyDay = busyDays.Count == 0 ? double.NaN : busyDays.Average(d => (double)d.FreeMinutes),
                FreeMinutesFreeDay = freeDays.Count == 0 ? double.NaN : freeDays.Average(d => (double)d.FreeMinutes),
                NoFreeTimePct = allDays.Count == 0 ? double.NaN : allDays.Count(d => d.Reason == "no_free_time") * 100.0 / allDays.Count,
                MeanIncomeReais = ps.Count == 0 ? double.NaN : ps.Average(p => p.IncomeCentsPerMonth / 100.0),
                MeanSupportReais = ps.Count == 0 ? double.NaN : ps.Average(p => p.SupportCentsPerMonth / 100.0),
                MeanSleepNeedMinutes = ps.Count == 0 ? double.NaN : ps.Average(p => (double)p.SleepNeedMinutes),
                MeanSleepBusyMinutes = busyDays.Count == 0 ? double.NaN : busyDays.Average(d => (double)d.SleepMinutes),
                MeanSleepFreeMinutes = freeDays.Count == 0 ? double.NaN : freeDays.Average(d => (double)d.SleepMinutes),
                MeanDeficitMinutes = ps.Count == 0 ? double.NaN : ps.Average(p => (double)p.SleepDeficitMinutes),
                TiredPct = ps.Count == 0 ? double.NaN : ps.Count(p => Tired(w, p)) * 100.0 / ps.Count,
                BreakSessionsPerWeek = PerPersonWeekly(ids, _breakByPerson, weeks, 1),
                SlackSessionsPerWeek = PerPersonWeekly(ids, _slackByPerson, weeks, 1),
                SlackDayPct = busyDays.Count == 0 ? double.NaN : busyDays.Count(d => d.WorkOverlapMinutes > 0) * 100.0 / busyDays.Count,
            });
        }
        foreach (var p in chars.Select(c => w.GetPerson(c.PersonId)))
            foreach (var d in p.DayPlans)
                m.DayReasons[d.Reason] = m.DayReasons.GetValueOrDefault(d.Reason) + 1;
        foreach (var l in w.Content.LocalitiesOrdered)
        {
            var residents = chars.Select(c => w.GetPerson(c.PersonId)).Where(p => p.ResidenceId == l.Id).ToList();
            var st = _localities.GetValueOrDefault(l.Id);
            m.Localities.Add(new LocalityStats
            {
                Id = l.Id, Name = l.Name, UtcOffsetMinutes = l.UtcOffsetMinutes, Persons = residents.Count,
                ForeignNationals = residents.Count(p => p.NationalityId != p.ResidenceId), Sessions = st.Sessions,
                MeanLocalStartHour = st.Sessions == 0 ? double.NaN : CircularMeanHour(st.LocalX, st.LocalY),
                MeanServerStartHour = st.Sessions == 0 ? double.NaN : CircularMeanHour(st.ServerX, st.ServerY),
            });
        }
        foreach (var kv in _quitReasons) m.QuitReasons[kv.Key] = kv.Value;
        m.QuitDays.AddRange(_quitDays);
        m.QuitLevels.AddRange(_quitLevels);
        m.SessionsExtended = _extended;
        // ---- R3: distribuicoes ----
        var dist = m.Distribution;
        dist.Weeks = weeks;
        dist.Sessions.AddRange(_r3Sessions);
        dist.SleepCutPerPeriod.AddRange(_r3CutByNight.Values);
        long windowDay = _windowStart / SimTime.TicksPerDay;
        foreach (var c in chars)
        {
            var p = w.GetPerson(c.PersonId);
            var key = (p.ProfileId, p.OccupationId);
            dist.Persons[key] = dist.Persons.GetValueOrDefault(key) + 1;
            foreach (var d in p.DayPlans)
                if (d.LocalDay >= windowDay)
                    dist.Days.Add(new DaySample(p.Id, p.ProfileId, p.OccupationId, d.LocalDay, d.FreeMinutes, d.PlannedMinutes, d.Sessions, d.WorkSessions, d.Reason, d.FreeDay, d.Sittings, d.Returns, d.DeclinedReturns, d.SkippedAfterWork));
        }
        foreach (var kv in _sleepWork) m.SleepWork[kv.Key] = kv.Value;
        m.SleepWork["sessions"] = _sessionsInWindow.Values.Sum();
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

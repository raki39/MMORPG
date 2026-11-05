using Simulation.Core.Content;
using Simulation.Core.Events;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Scheduling;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Core.Behavior;

/// <summary>
/// Comportamento individual (M2): registro, sessoes pela rotina, escolha de atividades
/// oferecidas pelos locais conhecidos, viagem, execucao, progressao e saida.
/// Tudo por eventos agendados (sem Update por pessoa). Cada sorteio usa um fluxo derivado
/// de (seed, pessoa, contador), entao o resultado nao depende da ordem global de eventos
/// alem da ordem deterministica do agendador.
/// </summary>
public sealed class BehaviorSystem
{
    public const string EvRegister = "person_register";
    public const string EvSessionStart = "session_start";
    public const string EvSessionEnd = "session_end";
    public const string EvArrive = "travel_arrive";
    public const string EvActivityEnd = "activity_end";
    public const string EvDailySessions = "daily_sessions";

    private readonly WorldState _w;
    private readonly RouteFinder _routes;
    private readonly QuestSystem _quests;
    private readonly TraitDynamics _traits;
    private readonly GoalSystem _goals;
    private readonly ExperienceSystem _exp;

    public BehaviorSystem(WorldState world)
    {
        _exp = new ExperienceSystem(world);
        _w = world;
        _routes = new RouteFinder(world.Content, new ZoneLayout(world.Content));
        _quests = new QuestSystem(world, GainExperience);
        _traits = new TraitDynamics(world);
        _goals = new GoalSystem(world, _quests, _traits);
        _quests.StepCompleted = (ch, questId) =>
        {
            RequestPlanReview(ch, "quest_step");
            _goals.OnQuestStep(ch, questId);
        };
        _quests.Completed = (ch, questId) =>
        {
            var p = _w.GetPerson(ch.PersonId);
            RequestPlanReview(ch, "quest_completed");
            var md = _w.Content.Mood;
            bool main = _w.Content.Quests.TryGetValue(questId, out var qd) && qd.Category == QuestCategories.Main;
            bool first = ch.QuestHistory.TryGetValue(questId, out var rec) && rec.Completions == 1;
            _exp.OnAchievement(p, ch, main ? "main_quest" : "quest", questId, main ? md.MainQuestPoints : md.QuestPoints,
                first ? md.NoveltyNewQuest : 0, main ? _w.Content.Memory.MainQuestIntensity : 0);
            _traits.OnQuestCompleted(p);
            _goals.OnQuestCompleted(p, ch, questId);
        };
        _goals.Ended = (ch, g, completed, reason) =>
        {
            RequestPlanReview(ch, completed ? "goal_completed" : "goal_abandoned");
            var p = _w.GetPerson(ch.PersonId);
            var md = _w.Content.Mood;
            string subject = g.Type + "|" + g.Target + "|" + g.TargetValue;
            if (completed) _exp.OnAchievement(p, ch, "goal_completed", subject, md.GoalCompletedPoints, 0, _w.Content.Memory.GoalCompletedIntensity);
            else _exp.OnSetback(p, ch, "goal_abandoned", subject, md.GoalAbandonedPoints, md.GoalAbandonedFrustration, _w.Content.Memory.GoalAbandonedIntensity);
        };
    }

    public RouteFinder Routes => _routes;
    public QuestSystem Quests => _quests;
    public TraitDynamics Traits => _traits;
    public GoalSystem Goals => _goals;
    public ExperienceSystem Experience => _exp;

    public IEnumerable<(string Type, Action<ScheduledEvent> Handler)> Handlers()
    {
        yield return (EvRegister, OnRegister);
        yield return (EvSessionStart, OnSessionStart);
        yield return (EvSessionEnd, OnSessionEnd);
        yield return (EvArrive, OnArrive);
        yield return (EvActivityEnd, OnActivityEnd);
        yield return (EvDailySessions, OnDailySessions);
    }

    private long Now => _w.Clock.Tick;
    private CampaignCalendar Cal => _w.Clock.Calendar;

    // ---- Horario local (P1) ----
    private static long FloorDiv(long a, long b) => a >= 0 ? a / b : (a - b + 1) / b;
    private static long OffsetTicks(Person p) => p.LocalOffsetMinutes * SimTime.TicksPerMinute;
    /// <summary>Dia local da pessoa no instante (indice de dias desde o inicio, no fuso dela).</summary>
    public static long LocalDay(Person p, long tick) => FloorDiv(tick + OffsetTicks(p), SimTime.TicksPerDay);
    /// <summary>Instante (do servidor) em que comeca o dia local da pessoa.</summary>
    public static long LocalDayStart(Person p, long localDay) => localDay * SimTime.TicksPerDay - OffsetTicks(p);
    /// <summary>Tipo do dia local pela residencia (feriados de onde mora).</summary>
    private DayKind LocalKind(Person p, long localDay) =>
        _w.CalendarOf(p).KindOf(Cal.StartDate.AddDays(localDay));

    // ---- Rotina de vida (P2) ----
    private OccupationDefinition? OccOf(Person p) => _w.Content.Occupations.TryGetValue(p.OccupationId, out var o) ? o : null;
    private static int Dow(DateTime d) => ((int)d.DayOfWeek + 6) % 7;

    /// <summary>Dia local montado: obrigacoes, sono e janelas livres (feriados da residencia).</summary>
    public DailySchedule.DayLayout LayoutOf(Person p, long localDay)
    {
        var cal = _w.CalendarOf(p);
        var date = Cal.StartDate.AddDays(localDay);
        return DailySchedule.Build(p, OccOf(p), _w.Content.Routine, Dow(date),
            cal.HolidayOn(date) != null, cal.HolidayOn(date.AddDays(-1)) != null, cal.HolidayOn(date.AddDays(1)) != null);
    }

    private int PreferredMinute(Person p, DeterministicRng rng) =>
        p.Traits.PreferredStartHour * 60 + rng.NextIntInclusive(-_w.Content.Behavior.SessionStartJitterMinutes, _w.Content.Behavior.SessionStartJitterMinutes);

    private int DurationJitter(DeterministicRng rng) =>
        rng.NextIntInclusive(1000 - _w.Content.Behavior.SessionDurationJitterPermille, 1000 + _w.Content.Behavior.SessionDurationJitterPermille);

    /// <summary>
    /// Primeira sessao a partir de um dia local (registro e retorno): na janela livre que melhor
    /// serve, depois de "notBefore"; se o dia nao tem janela (ex.: trabalha e estuda), o proximo que tiver.
    /// </summary>
    private (long At, int Minutes) FirstSessionOf(Person p, long localDay, DeterministicRng rng, long notBefore)
    {
        var rs = _w.Content.Routine;
        int preferred = PreferredMinute(p, rng);
        int jitter = DurationJitter(rng);
        for (long d = localDay; d < localDay + 8; d++)
        {
            var day = LayoutOf(p, d);
            long dayStart = LocalDayStart(p, d);
            int desired = DailySchedule.DesiredMinutes(p, day, rs, jitter);
            if (DailySchedule.PlaceMain(day, preferred, desired, rs, out _, out int start, out int minutes)
                && dayStart + start * SimTime.TicksPerMinute > notBefore)
                return (dayStart + start * SimTime.TicksPerMinute, minutes);
            // Hoje a janela preferida ja passou: a proxima janela ainda aberta do mesmo dia.
            foreach (var (s, e) in day.Windows)
            {
                long ws = Math.Max(dayStart + s * SimTime.TicksPerMinute, notBefore + 1);
                long we = dayStart + e * SimTime.TicksPerMinute;
                if (we - ws >= rs.MinSessionMinutes * SimTime.TicksPerMinute)
                    return (ws, (int)Math.Min(desired, (we - ws) / SimTime.TicksPerMinute));
            }
        }
        return (notBefore + 1 + SimTime.TicksPerHour, p.Traits.SessionMinutes);
    }

    private DeterministicRng Rng(string stream, long entity) =>
        new(RngStreams.DeriveSeed(_w.Seed, stream, entity));

    // ---------------- Abertura e registro ----------------

    /// <summary>Servidor aberto: interessados se registram ao longo da primeira semana.</summary>
    public void OnServerOpened()
    {
        foreach (var p in _w.Persons)
        {
            if (p.State != LifecycleState.PotentialInterested) continue;
            var rng = Rng("behavior.register", p.Id);
            int day = rng.PickWeighted(_w.Content.Behavior.RegistrationDayWeights);
            long localDay = LocalDay(p, Now) + day;
            long at = FirstSessionOf(p, localDay, rng, Now).At;   // no tempo livre da pessoa (P2)
            _w.Scheduler.Schedule(at, EvRegister, p.Id);
        }
    }

    /// <summary>Servidor fechado: todos saem imediatamente.</summary>
    public void OnServerClosed()
    {
        foreach (var p in _w.Persons)
            if (p.IsOnline) Logout(_w.GetCharacter(p.CharacterId), "server_closed");
    }

    private void OnRegister(ScheduledEvent ev)
    {
        var p = _w.GetPerson((int)ev.TargetId);
        if (p.State != LifecycleState.PotentialInterested) return;
        if (!_w.Server.IsOpen)
        {
            _w.Scheduler.Schedule(Now + SimTime.TicksPerDay, EvRegister, p.Id);
            return;
        }
        var rng = Rng("behavior.character", p.Id);
        var account = _w.AddAccount(p.Id, Now);
        string classId = ChooseClass(p.Traits, rng);
        string nick = MakeNickname(rng);
        string start = _w.Content.World.StartingRegionId;
        var ch = _w.AddCharacter(account.Id, p.Id, nick, classId, start);
        ch.Position = _routes.Layout.Hub(start);
        account.ActiveCharacterId = ch.Id;
        p.AccountId = account.Id;
        p.CharacterId = ch.Id;
        p.State = LifecycleState.Registered;
        // Humor inicial (D-038): boa vontade, tudo novo, expectativa pelo perfil.
        var md = _w.Content.Mood;
        p.Mood.Satisfaction = md.InitialSatisfaction;
        p.Mood.Novelty = md.InitialNovelty;
        p.Mood.ProgressExpectation = ExperienceSystem.InitialExpectation(p.Traits, md);
        _exp.UpdateEngagement(p, ch);
        // Conhece a cidade inicial e as zonas ligadas a ela (as saidas mostram para onde levam).
        ch.Known.Add(start);
        foreach (var c in _w.Content.Regions[start].Connections) ch.Known.Add(c.To);

        _w.Today.Registrations++;
        _w.Emit("person_registered", EventImportance.Normal,
            new[] { new EntityRef("person", p.Id), new EntityRef("character", ch.Id) },
            new[] { new EventCause("launch_interest", 1000) }, $"{nick} · {classId}");

        // Primeira sessao comeca no ato do registro.
        // Primeira sessao no ato do registro, limitada a janela livre em que ele cai (P2).
        var today = LayoutOf(p, LocalDay(p, Now));
        int nowMin = (int)((Now - LocalDayStart(p, LocalDay(p, Now))) / SimTime.TicksPerMinute);
        int firstMinutes = DailySchedule.DesiredMinutes(p, today, _w.Content.Routine, DurationJitter(rng));
        foreach (var (s, e) in today.Windows)
            if (s <= nowMin && nowMin < e) firstMinutes = Math.Max(_w.Content.Routine.MinSessionMinutes, Math.Min(firstMinutes, e - nowMin));
        StartSession(p, firstMinutes);
    }

    private string ChooseClass(PersonTraits t, DeterministicRng rng)
    {
        var classes = _w.Content.Classes.Values.ToList();
        var weights = classes.Select(c => c.Role switch
        {
            ClassRoles.Tank => 100 + t.Aggressiveness * 4 / 10 + t.Coordination * 3 / 10 + t.Loyalty * 3 / 10,
            ClassRoles.Damage => 100 + t.Pvp * 4 / 10 + t.MechanicalSkill * 4 / 10 + t.Pve * 2 / 10,
            ClassRoles.Support => 100 + t.Sociability * 5 / 10 + t.Patience * 3 / 10 + t.Socializing * 2 / 10,
            _ => 300,
        }).ToList();
        return classes[rng.PickWeighted(weights)].Id;
    }

    private string MakeNickname(DeterministicRng rng)
    {
        var n = _w.Content.Names;
        return n.NickPrefixes[rng.NextInt(n.NickPrefixes.Count)] + n.NickSuffixes[rng.NextInt(n.NickSuffixes.Count)];
    }

    // ---------------- Sessoes ----------------

    /// <summary>
    /// Meia-noite local de uma localidade (P1): cada pessoa registrada que mora la decide se e
    /// quando joga hoje, no horario e nos feriados de onde mora. Inatividade e humor contam pelo
    /// dia do servidor.
    /// </summary>
    private void OnDailySessions(ScheduledEvent ev)
    {
        var locality = _w.Content.LocalitiesOrdered[(int)ev.TargetId];
        long localDay = ev.Payload;
        long day = Cal.DayIndex(Now);
        var rt = _w.Content.Retention;
        foreach (var p in _w.Persons)
        {
            if (p.State is LifecycleState.PotentialInterested or LifecycleState.Banned || p.CharacterId == 0) continue;
            if (p.ResidenceId.Length > 0 ? p.ResidenceId != locality.Id : (int)ev.TargetId != 0) continue;
            long dayStart = LocalDayStart(p, localDay);
            var ch = _w.GetCharacter(p.CharacterId);
            var rng = Rng("behavior.session", p.Id * 100_000L + day);
            if (p.State == LifecycleState.Quit)
            {
                // Desistente (D-039): so a saudade dos bons momentos traz de volta (atualizacoes e convites no M6/M7).
                _exp.OnDaily(p, ch);
                if (!TryReturnFromQuit(p, ch, day, rng)) continue;
                var (backAt, backMinutes) = FirstSessionOf(p, localDay, rng, Now);
                _w.Scheduler.Schedule(backAt, EvSessionStart, p.Id, backMinutes);
                RecordPlan(p, new DayPlanRecord(localDay, 0, 0, 1, "came_back", (int)((backAt - dayStart) / SimTime.TicksPerMinute), backMinutes, false));
                continue;
            }
            else
            {
                _traits.OnDaily(p, day);   // inatividade (D-036), antes de decidir se joga hoje
                _exp.OnDaily(p, ch);       // humor e memoria (D-038)
                if (!_w.NeutralMood && !p.IsOnline && ReviewRetention(p, ch, day, rng)) continue;   // inativo ou desistiu (D-039)
            }
            PlanDay(p, localDay, dayStart, rng);
        }
        _w.Scheduler.Schedule(Now + SimTime.TicksPerDay, EvDailySessions, ev.TargetId, localDay + 1);
    }

    /// <summary>
    /// Agenda do dia (P2): janelas livres pelas obrigacoes e pelo sono; a vontade de jogar
    /// (importancia do jogo, tempo livre, habito e humor) decide se joga; a sessao vai para a
    /// janela que serve ao horario preferido. Pode haver uma segunda sessao em outra janela.
    /// O motivo do dia fica na agenda (estado), sem gerar evento.
    /// </summary>
    private void PlanDay(Person p, long localDay, long dayStart, DeterministicRng rng)
    {
        var rs = _w.Content.Routine;
        var rt = _w.Content.Retention;
        var day = LayoutOf(p, localDay);
        p.OpenWindows.RemoveAll(x => x.End < Now);
        foreach (var (s, e) in day.Windows)
            p.OpenWindows.Add((dayStart + s * SimTime.TicksPerMinute, dayStart + e * SimTime.TicksPerMinute));

        int chance = DailySchedule.BaseChance(p, day, rs);
        int streak = p.Traits.Habit * rs.HabitStreakPermille / 1000;
        chance = Math.Clamp(chance + (p.LastPlayedLocalDay == localDay - 1 ? streak : -streak), 0, 1000);
        if (!_w.NeutralMood) chance = chance * PlayFactor(p.Mood.Engagement, rt) / 1000;   // o humor muda a rotina (D-039)
        if (p.State == LifecycleState.Inactive) chance = chance * rt.InactiveReturnPermille / 1000 * p.Mood.ReturnIntention / 1000;

        int preferred = PreferredMinute(p, rng);
        int desired = DailySchedule.DesiredMinutes(p, day, rs, DurationJitter(rng));
        bool fits = DailySchedule.PlaceMain(day, preferred, desired, rs, out int window, out int start, out int minutes);
        bool wants = rng.ChancePermille(chance);
        if (!fits) { RecordPlan(p, new DayPlanRecord(localDay, day.FreeMinutes, day.Windows.Count, 0, "no_free_time", -1, 0, day.FreeDay)); return; }
        if (!wants)
        {
            string reason = p.State == LifecycleState.Inactive ? "inactive"
                : !_w.NeutralMood && p.Mood.Engagement < rt.EngagementNeutral - rs.LowEngagementGap ? "low_engagement" : "other_leisure";
            RecordPlan(p, new DayPlanRecord(localDay, day.FreeMinutes, day.Windows.Count, 0, reason, -1, 0, day.FreeDay));
            return;
        }
        _w.Scheduler.Schedule(dayStart + start * SimTime.TicksPerMinute, EvSessionStart, p.Id, minutes);
        int sessions = 1, planned = minutes;
        int imp = p.Traits.GameImportance;
        if (rs.MaxSessionsPerDay > 1 && imp > rs.SecondSessionFromImportance)
        {
            int c2 = (imp - rs.SecondSessionFromImportance) * rs.SecondSessionPermilleAtMax / Math.Max(1, 1000 - rs.SecondSessionFromImportance);
            int other = -1;
            for (int i = 0; i < day.Windows.Count; i++)
            {
                if (i == window || day.Windows[i].End - day.Windows[i].Start < rs.MinSessionMinutes) continue;
                if (other < 0 || day.Windows[i].End - day.Windows[i].Start > day.Windows[other].End - day.Windows[other].Start) other = i;
            }
            if (other >= 0 && rng.ChancePermille(c2))
            {
                var (s2, e2) = day.Windows[other];
                int m2 = Math.Min(e2 - s2, Math.Max(rs.MinSessionMinutes, p.Traits.SessionMinutes * rs.SecondSessionDurationPermille / 1000));
                _w.Scheduler.Schedule(dayStart + s2 * SimTime.TicksPerMinute, EvSessionStart, p.Id, m2);
                sessions = 2;
                planned += m2;
            }
        }
        RecordPlan(p, new DayPlanRecord(localDay, day.FreeMinutes, day.Windows.Count, sessions, "played", start, planned, day.FreeDay));
    }

    private void RecordPlan(Person p, DayPlanRecord plan)
    {
        p.PlanList.Add(plan);
        while (p.PlanList.Count > _w.Content.Routine.PlanHistoryDays) p.PlanList.RemoveAt(0);
    }

    /// <summary>Fator da chance de jogar pelo engajamento (permille), com piso e teto.</summary>
    public static int PlayFactor(int engagement, RetentionSettingsDefinition rt) =>
        Math.Clamp(1000 + (engagement - rt.EngagementNeutral) * rt.EngagementEffect / 1000, rt.MinPlayFactor, rt.MaxPlayFactor);

    /// <summary>
    /// Meia-noite (D-039): quem esta sem jogar ha muito vira inativo; inativo ha muito, ou com
    /// intencao de continuar baixa, desiste (com as causas: o que mais incomodou). Devolve true
    /// se desistiu agora.
    /// </summary>
    private bool ReviewRetention(Person p, Character ch, long day, DeterministicRng rng)
    {
        var rt = _w.Content.Retention;
        long last = Math.Max(p.LastPlayedDay, p.LastReturnDay);
        long off = last < 0 ? 0 : day - last;
        if (p.State != LifecycleState.Inactive && off >= rt.InactiveAfterDays)
        {
            p.State = LifecycleState.Inactive;
            p.TimesInactive++;
            _w.Today.BecameInactive++;
            _w.Emit("player_inactive", EventImportance.Low, new[] { new EntityRef("person", p.Id), new EntityRef("character", ch.Id) },
                new[] { new EventCause("days_without_playing", (int)off) }, off.ToString());
        }
        int ri = p.Mood.ReturnIntention;
        bool quit = false;
        string reason = "";
        if (p.State == LifecycleState.Inactive && off >= rt.QuitAfterInactiveDays) { quit = true; reason = "long_inactivity"; }
        // Carencia depois de voltar: as novas sessoes decidem, nao o humor de quando saiu (D-039a).
        else if (ri < rt.QuitBelow && (p.LastReturnDay < 0 || day - p.LastReturnDay >= rt.QuitGraceDaysAfterReturn)
            && rng.ChancePermille(rt.QuitChancePerDay * (rt.QuitBelow - ri) / Math.Max(1, rt.QuitBelow))) { quit = true; reason = "low_intention"; }
        if (!quit) return false;
        p.State = LifecycleState.Quit;
        p.QuitDay = day;
        p.TimesQuit++;
        _w.Today.Quits++;
        var causes = new List<EventCause> { new(reason, 1000) };
        foreach (var kv in p.MoodDrivers.Where(kv => kv.Value < 0).OrderBy(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).Take(3))
            causes.Add(new EventCause(kv.Key, kv.Value));
        _w.Emit("player_quit", EventImportance.Normal, new[] { new EntityRef("person", p.Id), new EntityRef("character", ch.Id) },
            causes.ToArray(), $"nível {ch.Level}");
        return true;
    }

    /// <summary>
    /// Desistente (D-039a): pode voltar pela saudade de boas experiencias realmente registradas,
    /// que ainda nao motivaram uma volta, e pela lealdade. Cada volta por saudade gasta essas
    /// memorias, aumenta o intervalo minimo e reduz a chance seguinte (sem ciclos rapidos).
    /// Volta com o humor que tem (a frustracao ja se dissipou com o tempo); atualizacoes,
    /// convites e campanhas com streamers serao outros motivos (M6/M7).
    /// </summary>
    public static int Nostalgia(Person p) =>
        Math.Min(1000, p.Memories.Where(m => m.Valence > 0 && m.Tick > p.NostalgiaSpentTick).Sum(m => m.Intensity));

    private bool TryReturnFromQuit(Person p, Character ch, long day, DeterministicRng rng)
    {
        var rt = _w.Content.Retention;
        if (p.QuitDay < 0 || day - p.QuitDay < rt.QuitReturnMinDays * (1 + p.NostalgiaReturns)) return false;
        int nostalgia = Nostalgia(p);
        int chance = rt.QuitReturnChancePerDay * nostalgia / 1000 * (500 + p.Traits.Loyalty) / 1000 / (1 + p.NostalgiaReturns);
        if (!rng.ChancePermille(chance)) return false;
        p.State = LifecycleState.Offline;
        p.TimesReturned++;
        p.NostalgiaReturns++;
        p.NostalgiaSpentTick = Now;
        p.LastReturnDay = day;
        _w.Today.Returns++;
        _exp.UpdateEngagement(p, ch);
        _w.Emit("player_returned", EventImportance.Normal, new[] { new EntityRef("person", p.Id), new EntityRef("character", ch.Id) },
            new[] { new EventCause("nostalgia", nostalgia), new EventCause("days_away", (int)(day - p.QuitDay)) }, $"{day - p.QuitDay} dias");
        return true;
    }

    private void OnSessionStart(ScheduledEvent ev)
    {
        var p = _w.GetPerson((int)ev.TargetId);
        if (!_w.Server.IsOpen || p.CharacterId == 0 || p.State == LifecycleState.Quit) return;
        StartSession(p, (int)ev.Payload);
    }

    private void StartSession(Person p, int minutes)
    {
        var ch = _w.GetCharacter(p.CharacterId);
        long end = Now + minutes * SimTime.TicksPerMinute;
        if (p.IsOnline)
        {
            // Ja conectado: a sessao apenas se estende.
            if (end > ch.SessionEndTick)
            {
                ch.SessionEndTick = end;
                ch.LogoutPending = false;
                _w.Scheduler.Schedule(end, EvSessionEnd, p.Id);
            }
            return;
        }
        if (p.State == LifecycleState.Inactive)
        {
            // Inativo que voltou a jogar (D-039).
            p.TimesReturned++;
            p.LastReturnDay = Cal.DayIndex(Now);
            _w.Today.Returns++;
            _w.Emit("player_returned", EventImportance.Normal, new[] { new EntityRef("person", p.Id), new EntityRef("character", ch.Id) },
                new[] { new EventCause("came_back", 1000), new EventCause("days_away", (int)(Cal.DayIndex(Now) - p.LastPlayedDay)) },
                $"{Cal.DayIndex(Now) - p.LastPlayedDay} dias");
        }
        p.State = LifecycleState.Online;
        p.LastPlayedLocalDay = LocalDay(p, Now);
        _w.OnlineCount++;
        _w.Today.SessionsStarted++;
        _w.Today.PeakOnline = Math.Max(_w.Today.PeakOnline, _w.OnlineCount);
        long day = Cal.DayIndex(Now);
        if (p.LastPlayedDay != day) { p.LastPlayedDay = day; _w.Today.ActivePlayers++; }

        ch.Status = CharacterStatus.Idle;
        ch.SessionStartTick = Now;
        ch.SessionEndTick = end;
        ch.LogoutPending = false;
        ch.SessionExtended = false;
        // Plano novo a cada sessao; o tedio se dissipa em parte fora do jogo (D-037).
        ch.Plan = null;
        ch.PlanReviewReason = "";
        SessionRules.FadeBoredomBetweenSessions(ch, _w.Content.Session);
        _exp.OnSessionStart(ch);
        _w.Scheduler.Schedule(end, EvSessionEnd, p.Id);
        _w.Emit("session_started", EventImportance.Low, new[] { new EntityRef("person", p.Id) }, detail: $"{minutes} min");
        // Objetivos pessoais (D-036): revisa ao conectar, antes da primeira decisao.
        _goals.Review(p, ch, Rng("behavior.goals", ch.Id * 1_000_000L + ch.DecisionCount));
        Decide(ch, p);
    }

    private void OnSessionEnd(ScheduledEvent ev)
    {
        var p = _w.GetPerson((int)ev.TargetId);
        if (!p.IsOnline) return;
        var ch = _w.GetCharacter(p.CharacterId);
        if (ch.SessionEndTick != ev.Tick) return; // sessao foi estendida

        // Sessao flexivel (D-039; plano 4.3): indo bem e engajado, fica mais um pouco (uma vez, com teto).
        var rt = _w.Content.Retention;
        int room = RoomToExtend(p, ch);
        if (!_w.NeutralMood && !ch.SessionExtended && p.Mood.Engagement >= rt.EngagementNeutral && SessionScoreSoFar(ch) >= rt.ExtendScoreAbove
            && room >= 10 && Rng("behavior.session_flex", ch.Id * 1_000_000L + ch.DecisionCount).ChancePermille(rt.ExtendChance))
        {
            int planned = (int)((ch.SessionEndTick - ch.SessionStartTick) / SimTime.TicksPerMinute);
            int extra = Math.Min(Math.Max(10, planned * rt.ExtendPermille / 1000), room);
            ch.SessionExtended = true;
            ch.SessionEndTick += extra * SimTime.TicksPerMinute;
            _w.Scheduler.Schedule(ch.SessionEndTick, EvSessionEnd, p.Id);
            _w.Emit("session_extended", EventImportance.Low, new[] { new EntityRef("person", p.Id) },
                new[] { new EventCause("enjoying", SessionScoreSoFar(ch)) }, $"+{extra} min");
            return;
        }

        switch (ch.Status)
        {
            case CharacterStatus.Doing when ch.ActivityEndTick - Now <= _w.Content.Behavior.LogoutToleranceMinutes * SimTime.TicksPerMinute:
            case CharacterStatus.Traveling:
                ch.LogoutPending = true; // termina a atividade ou a etapa da viagem e sai
                break;
            default:
                Logout(ch, "session_time");
                break;
        }
    }

    private void Logout(Character ch, string reason)
    {
        var p = _w.GetPerson(ch.PersonId);
        if (ch.Status == CharacterStatus.Doing || ch.Status == CharacterStatus.Traveling)
            _w.AddOccupancy(ch.TargetRegionId, ch.ActivityId, -1);
        if (ch.Status == CharacterStatus.Traveling)
        {
            // Sai no meio do caminho: fica onde estava neste instante.
            ch.Position = ch.PositionAt(Now);
            ch.Waypoints.Clear();
        }
        // Atividade interrompida pela saida: o tempo gasto conta (sem recompensa).
        if (ch.Status == CharacterStatus.Doing) AddActivityTime(ch, ActivityTypeOf(ch), Now - ch.ActivityStartTick);
        _exp.OnSessionEnd(p, ch, Now);   // nota da sessao, satisfacao e frustracao (D-038)
        _w.Today.PlayMinutes += (Now - ch.SessionStartTick) / SimTime.TicksPerMinute;
        ch.PlayedTicks += Now - ch.SessionStartTick;
        ch.Status = CharacterStatus.Offline;
        ch.ActivityId = "";
        ch.TargetRegionId = "";
        ch.LogoutPending = false;
        ch.Plan = null;
        ch.PlanReviewReason = "";
        p.State = LifecycleState.Offline;
        _w.OnlineCount--;
        _w.Emit("session_ended", EventImportance.Low, new[] { new EntityRef("person", p.Id) },
            new[] { new EventCause(reason, 1000) });
    }

    /// <summary>Minutos de janela livre depois do fim previsto da sessao (P2): a extensao nao invade obrigacoes nem o sono.</summary>
    private int RoomToExtend(Person p, Character ch)
    {
        long end = ch.SessionEndTick;
        long limit = -1;
        foreach (var (s, e) in p.OpenWindows)
            if (s <= end && end <= e) limit = Math.Max(limit, e);
        return limit < 0 ? 0 : (int)((limit - end) / SimTime.TicksPerMinute);
    }

    /// <summary>Nota parcial da sessao: experiencias e acontecimentos ate agora (sem progresso e novidade).</summary>
    private int SessionScoreSoFar(Character ch)
    {
        var L = ch.SessionLedger;
        long sum = 0;
        foreach (var v in L.Sums.Values) sum += v;
        int avg = L.Minutes > 0 ? (int)(sum / L.Minutes) : 0;
        return Math.Clamp(500 + avg + Math.Clamp(L.Points, -_w.Content.Mood.EventClamp, _w.Content.Mood.EventClamp), 0, 1000);
    }

    // ---------------- Decisao ----------------

    private void Decide(Character ch, Person p)
    {
        var rng = Rng("behavior.decision", ch.Id * 1_000_000L + ch.DecisionCount);
        ch.DecisionCount++;
        var options = ActivityScoring.Evaluate(ch, p.Traits, _w.Content, _routes, _w.OccupancyOf, Now, rng, _quests, _goals, p.PlaceImpressions);

        if (options.Count == 0)
        {
            var md = _w.Content.Mood;
            _exp.OnSetback(p, ch, "nothing_to_do", ch.RegionId, md.NothingToDoPoints, md.NothingToDoFrustration, _w.Content.Memory.NothingToDoIntensity);
            // Nada viavel: espera e repensa.
            ch.Status = CharacterStatus.Doing;
            ch.TargetRegionId = ch.RegionId;
            ch.ActivityId = "";
            ch.ActivityStartTick = Now;
            ch.ActivityEndTick = Now + _w.Content.Behavior.IdleRethinkMinutes * SimTime.TicksPerMinute;
            _w.Scheduler.Schedule(ch.ActivityEndTick, EvActivityEnd, ch.Id);
            return;
        }

        // Plano da sessao (D-037): continua o foco salvo vantagem clara de outra opcao;
        // acontecimentos relevantes liberam a revisao. Viagem ja feita nao conta (sem custo afundado).
        var top = options[0];
        var best = top;
        var focus = ch.Plan == null ? null : options.FirstOrDefault(o => o.Key == ch.Plan.FocusKey);
        bool continued = false;
        int gap = 0;
        var ss = _w.Content.Session;
        if (focus != null && focus != top && ch.PlanReviewReason.Length == 0)
        {
            int margin = Now < ch.Plan!.CommitUntilTick ? ss.CommitMargin : ss.SwitchMargin;
            gap = top.Utility - focus.Utility;
            if (gap < margin) { best = focus; continued = true; }
        }
        if (ch.Plan == null) FormPlan(ch, p, best, null);
        else if (best.Key != ch.Plan.FocusKey)
            FormPlan(ch, p, best, ch.PlanReviewReason.Length > 0 ? ch.PlanReviewReason
                : focus == null ? "focus_unavailable" : SessionRules.MainDifference(best, focus));
        else ch.Plan.Cycles++;
        ch.PlanReviewReason = "";

        ch.LastDecision = new DecisionRecord(Now, best.Region.Id, best.Activity.Id, best.Utility, options.Count,
            best.Factors, continued ? top.Key : options.FirstOrDefault(o => o != best)?.Key ?? "")
        { ContinuedPlan = continued, PlanGap = continued ? gap : 0 };
        if (best.Key == ch.LastActivityKey) ch.RepeatCount++;
        else { ch.RepeatCount = 0; ch.LastActivityKey = best.Key; }
        _w.Today.ActivitiesChosen++;

        var causes = best.Factors.Where(f => f.Key != "noise" && f.Contribution != 0)
            .OrderByDescending(f => Math.Abs(f.Contribution)).ThenBy(f => f.Key, StringComparer.Ordinal).Take(4)
            .Select(f => new EventCause(f.Key, f.Contribution)).ToList();
        if (continued) causes.Insert(0, new EventCause("plan_continuity", gap));
        _w.Emit("activity_chosen", EventImportance.Low,
            new[] { new EntityRef("character", ch.Id) }, causes.ToArray(), $"{best.Region.Id}/{best.Activity.Id}");

        ch.TargetRegionId = best.Region.Id;
        ch.ActivityId = best.Activity.Id;
        _w.AddOccupancy(best.Region.Id, best.Activity.Id, +1);

        ch.Waypoints.Clear();
        ch.Waypoints.AddRange(best.Plan.Waypoints);
        // Ja no ponto da atividade: comeca direto.
        while (ch.Waypoints.Count > 0 && ch.Waypoints[0].Pos.Equals(ch.Position) && ch.Waypoints[0].EntersZone.Length == 0)
            ch.Waypoints.RemoveAt(0);
        if (ch.Waypoints.Count == 0) StartActivity(ch);
        else NextLeg(ch);
    }

    /// <summary>
    /// Forma (inicio da sessao, reason = null) ou troca o plano da sessao. O compromisso minimo
    /// cresce com a paciencia. Eventos: session_plan e plan_changed, com o motivo como 1a causa.
    /// </summary>
    private void FormPlan(Character ch, Person p, ScoredOption option, string? reason)
    {
        var ss = _w.Content.Session;
        string main = SessionRules.MainReason(option);
        long commit = Now + (ss.CommitBaseMinutes + p.Traits.Patience * ss.CommitExtraMinutesAtMaxPatience / 1000) * SimTime.TicksPerMinute;
        ch.Plan = new SessionPlan(option.Key, option.Activity.Type, reason ?? main, Now, commit);
        if (reason == null)
        {
            ch.PlansFormed++;
            _w.Emit("session_plan", EventImportance.Low, new[] { new EntityRef("character", ch.Id) },
                new[] { new EventCause(main, 1000) }, option.Key);
        }
        else
        {
            ch.PlanChanges++;
            _w.Emit("plan_changed", EventImportance.Low, new[] { new EntityRef("character", ch.Id) },
                new[] { new EventCause(reason, 1000), new EventCause(main, 500) }, option.Key);
        }
    }

    /// <summary>Acontecimento relevante: a proxima decisao revisa o plano sem margem (o primeiro motivo vale).</summary>
    private void RequestPlanReview(Character ch, string reason)
    {
        if (ch.Status != CharacterStatus.Offline && ch.Plan != null && ch.PlanReviewReason.Length == 0)
            ch.PlanReviewReason = reason;
    }

    /// <summary>Inicia o proximo trecho: distancia / velocidade, no minimo 1 tick.</summary>
    private void NextLeg(Character ch)
    {
        var wp = ch.Waypoints[0];
        ch.Waypoints.RemoveAt(0);
        long dist = _routes.Layout.LegLength(ch.Position, wp.Pos);   // pela estrada (D-030)
        long ticks = Math.Max(1, (dist * SimTime.TicksPerMinute + _w.Content.Behavior.WalkSpeed - 1) / _w.Content.Behavior.WalkSpeed);
        ch.Status = CharacterStatus.Traveling;
        ch.MoveTo = wp.Pos;
        ch.MoveEntersZone = wp.EntersZone;
        ch.MoveStartTick = Now;
        ch.MoveEndTick = Now + ticks;
        _w.Scheduler.Schedule(ch.MoveEndTick, EvArrive, ch.Id);
    }

    private void OnArrive(ScheduledEvent ev)
    {
        var ch = _w.GetCharacter((int)ev.TargetId);
        if (ch.Status != CharacterStatus.Traveling || ch.MoveEndTick != ev.Tick) return;
        ch.Position = ch.MoveTo;
        if (ch.MoveEntersZone.Length > 0)
        {
            ch.RegionId = ch.MoveEntersZone;
            ch.MoveEntersZone = "";
            // Ao entrar numa zona, as saidas revelam as zonas ligadas a ela.
            foreach (var c in _w.Content.Regions[ch.RegionId].Connections)
                if (ch.Known.Add(c.To))
                {
                    _w.Emit("region_discovered", EventImportance.Low, new[] { new EntityRef("character", ch.Id) },
                        new[] { new EventCause("zone_exit_seen", 1000) }, c.To);
                    Discovered(ch, c.To);
                }
        }
        if (ch.LogoutPending) { Logout(ch, "session_time"); return; }
        if (ch.Waypoints.Count > 0) NextLeg(ch);
        else StartActivity(ch);
    }

    private void StartActivity(Character ch)
    {
        var a = _w.Content.Regions[ch.TargetRegionId].FindActivity(ch.ActivityId)!;
        var rng = Rng("behavior.duration", ch.Id * 1_000_000L + ch.DecisionCount);
        int minutes = Math.Max(1, a.TypicalMinutes * rng.NextIntInclusive(_w.Content.Behavior.ActivityDurationMin, _w.Content.Behavior.ActivityDurationMax) / 1000);
        ch.Status = CharacterStatus.Doing;
        ch.ActivityStartTick = Now;
        ch.ActivityEndTick = Now + minutes * SimTime.TicksPerMinute;
        _w.Scheduler.Schedule(ch.ActivityEndTick, EvActivityEnd, ch.Id);
    }

    private void OnActivityEnd(ScheduledEvent ev)
    {
        var ch = _w.GetCharacter((int)ev.TargetId);
        if (ch.Status != CharacterStatus.Doing || ch.ActivityEndTick != ev.Tick) return;
        var p = _w.GetPerson(ch.PersonId);
        string doneType = ActivityTypeOf(ch);
        bool firstTimeType = !ch.ActivityTicks.ContainsKey(doneType);
        bool rageQuit = false;
        AddActivityTime(ch, doneType, ev.Tick - ch.ActivityStartTick);

        if (ch.ActivityId.Length > 0)
        {
            var region = _w.Content.Regions[ch.TargetRegionId];
            var a = region.FindActivity(ch.ActivityId)!;
            _w.AddOccupancy(region.Id, a.Id, -1);
            int minutes = (int)((ev.Tick - ch.ActivityStartTick) / SimTime.TicksPerMinute);
            int levelBefore = ch.Level;
            AwardExperience(ch, a, region, minutes);
            _quests.OnActivityDone(ch, region, a, minutes);
            // Atributos vivos e objetivos (D-036): pratica, gosto, risco enfrentado.
            int challenge = ProgressionRules.ChallengeLevel(_w.Content, a);
            int fit = ProgressionRules.MilliXpPerMinute(_w.Content, a, levelBefore) > 0 ? ProgressionRules.Efficiency(_w.Content.Progression, levelBefore, challenge) : 0;
            _traits.OnActivity(p, ch, a, minutes, fit, ActivityScoring.RiskOf(region, a, challenge, levelBefore), firstTimeType);
            _goals.OnActivity(p, ch, a, minutes);
            ch.ZoneMinutes[region.Id] = (ch.ZoneMinutes.TryGetValue(region.Id, out int zm) ? zm : 0) + minutes;
            SessionRules.AccrueBoredom(ch, p.Traits, a.Type, minutes, _w.Content.Session);
            int valence = _exp.OnActivity(p, ch, region, a, minutes, fit, fit > 0, ActivityScoring.RiskOf(region, a, challenge, levelBefore), firstTimeType);
            // Frustracao alta e mais uma experiencia ruim: pode largar a sessao na hora (D-039).
            var rt = _w.Content.Retention;
            rageQuit = !_w.NeutralMood && valence < 0 && p.Mood.Frustration >= rt.RageQuitFrustration
                && Rng("behavior.session_flex", ch.Id * 1_000_000L + ch.DecisionCount + 500_000).ChancePermille(rt.RageQuitChance);
            // Explorar revela mais longe: as zonas ligadas as vizinhas.
            if (a.Type == ActivityTypes.Explore)
                foreach (var c in region.Connections)
                    foreach (var c2 in _w.Content.Regions[c.To].Connections)
                        if (ch.Known.Add(c2.To))
                        {
                            _w.Emit("region_discovered", EventImportance.Normal, new[] { new EntityRef("character", ch.Id) },
                                new[] { new EventCause("exploration", 1000) }, c2.To);
                            Discovered(ch, c2.To);
                        }
        }
        ch.ActivityId = "";
        ch.TargetRegionId = "";
        // Atividade encerrada e contabilizada: a saida abaixo nao pode conta-la de novo.
        ch.Status = CharacterStatus.Idle;

        if (rageQuit) { Logout(ch, "frustration"); return; }
        if (ch.LogoutPending || Now >= ch.SessionEndTick) { Logout(ch, "session_time"); return; }
        Decide(ch, p);
    }

    /// <summary>Zona revelada: conhecimento, gosto por explorar e objetivos (D-036).</summary>
    private void Discovered(Character ch, string zoneId)
    {
        var p = _w.GetPerson(ch.PersonId);
        RequestPlanReview(ch, "zone_discovered");
        var md = _w.Content.Mood;
        int curiosity = 500 + p.Traits.Exploration;   // exploradores valorizam mais a descoberta
        _exp.OnAchievement(p, ch, "discovery", zoneId, md.DiscoveryPoints * curiosity / 1000, md.NoveltyPerDiscovery,
            _w.Content.Memory.DiscoveryIntensity * curiosity / 1000);
        _traits.OnRegionDiscovered(p);
        _goals.OnDiscovered(p, ch, zoneId);
    }

    /// <summary>Tipo da atividade atual; "idle" quando esperando sem opcao viavel.</summary>
    private string ActivityTypeOf(Character ch) =>
        ch.ActivityId.Length == 0 ? "idle" : _w.Content.Regions[ch.TargetRegionId].FindActivity(ch.ActivityId)!.Type;

    private static void AddActivityTime(Character ch, string type, long ticks)
    {
        if (ticks <= 0) return;
        ch.ActivityTicks.TryGetValue(type, out long t);
        ch.ActivityTicks[type] = t + ticks;
    }

    private void AwardExperience(Character ch, ActivityDefinition a, RegionDefinition region, int minutes)
    {
        long xp = ProgressionRules.XpFor(_w.Content, a, ch.Level, minutes);
        // Rendimento pelos tracos (D-036): conhecimento do jogo e, em combate, mecanica.
        xp = xp * ActivityScoring.TraitXpPermille(_w.GetPerson(ch.PersonId).Traits, a.Type) / 1000;
        if (xp <= 0) return;
        ch.XpFromActivities += xp;
        GainExperience(ch, xp, a.Type);
    }

    /// <summary>Soma XP (de atividade ou de quest) e sobe de nivel quantas vezes couber.</summary>
    private void GainExperience(Character ch, long xp, string source)
    {
        var p = _w.Content.Progression;
        if (xp <= 0 || ch.Level >= p.LevelCap) return;
        int before = ch.Level;
        ch.Experience += xp;
        while (ch.Level < p.LevelCap && ch.Experience >= ProgressionRules.XpToNextLevel(p, ch.Level))
        {
            ch.Experience -= ProgressionRules.XpToNextLevel(p, ch.Level);
            ch.Level++;
            ch.LevelTicks.Add(ch.PlayedTicksAt(Now));
            _w.Today.LevelUps++;
            var md = _w.Content.Mood;
            var mm = _w.Content.Memory;
            _exp.OnAchievement(_w.GetPerson(ch.PersonId), ch, "level_milestone", ch.Level.ToString(), md.LevelUpPoints, md.NoveltyPerLevel,
                ch.Level % mm.LevelMilestoneEvery == 0 ? mm.LevelMilestoneIntensity : 0);
            _w.Emit("level_up", EventImportance.Normal, new[] { new EntityRef("character", ch.Id) },
                new[] { new EventCause(source, 1000) }, $"nível {ch.Level}");
        }
        if (ch.Level > before)
        {
            RequestPlanReview(ch, "level_up");
            _goals.OnLevelUp(_w.GetPerson(ch.PersonId), ch);
        }
    }
}

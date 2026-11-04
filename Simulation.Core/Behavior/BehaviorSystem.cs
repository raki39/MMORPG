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

    public BehaviorSystem(WorldState world)
    {
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
            _traits.OnQuestCompleted(p);
            _goals.OnQuestCompleted(p, ch, questId);
        };
        _goals.Ended = (ch, g, completed, reason) => RequestPlanReview(ch, completed ? "goal_completed" : "goal_abandoned");
    }

    public RouteFinder Routes => _routes;
    public QuestSystem Quests => _quests;
    public TraitDynamics Traits => _traits;
    public GoalSystem Goals => _goals;

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
            int day = rng.PickWeighted(BehaviorTuning.RegistrationDayWeights);
            long dayStart = Cal.StartOfDay(Cal.DayIndex(Now) + day);
            long at = dayStart + SessionStartMinute(p, rng, Cal.KindOf(Cal.ToDateTime(dayStart).Date)) * SimTime.TicksPerMinute;
            if (at <= Now) at = Now + 1 + rng.NextInt((int)SimTime.TicksPerHour);
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
        // Conhece a cidade inicial e as zonas ligadas a ela (as saidas mostram para onde levam).
        ch.Known.Add(start);
        foreach (var c in _w.Content.Regions[start].Connections) ch.Known.Add(c.To);

        _w.Today.Registrations++;
        _w.Emit("person_registered", EventImportance.Normal,
            new[] { new EntityRef("person", p.Id), new EntityRef("character", ch.Id) },
            new[] { new EventCause("launch_interest", 1000) }, $"{nick} · {classId}");

        // Primeira sessao comeca no ato do registro.
        StartSession(p, SessionMinutes(p, rng, Cal.KindOfTick(Now)));
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

    /// <summary>Meia-noite: cada pessoa registrada decide se e quando joga hoje.</summary>
    private void OnDailySessions(ScheduledEvent ev)
    {
        long day = ev.Payload;
        long dayStart = Cal.StartOfDay(day);
        var kind = Cal.KindOf(Cal.ToDateTime(dayStart).Date);
        foreach (var p in _w.Persons)
        {
            if (p.State != LifecycleState.Registered && p.State != LifecycleState.Offline && p.State != LifecycleState.Online) continue;
            _traits.OnDaily(p, day);   // inatividade (D-036), antes de decidir se joga hoje
            var rng = Rng("behavior.session", p.Id * 100_000L + day);
            int chance = p.Traits.DaysPerWeek * 1000 / 7;
            chance = kind == DayKind.Weekday
                ? chance * (1000 - p.Traits.WeekendBoost * 2 / 7) / 1000
                : chance * (1000 + p.Traits.WeekendBoost) / 1000;
            if (!rng.ChancePermille(chance)) continue;
            int minute = SessionStartMinute(p, rng, kind);
            int duration = SessionMinutes(p, rng, kind);
            _w.Scheduler.Schedule(dayStart + minute * SimTime.TicksPerMinute, EvSessionStart, p.Id, duration);
        }
        _w.Scheduler.Schedule(Cal.StartOfDay(day + 1), EvDailySessions, payload: day + 1);
    }

    private static int SessionStartMinute(Person p, DeterministicRng rng, DayKind kind)
    {
        int minute = p.Traits.PreferredStartHour * 60;
        if (kind != DayKind.Weekday) minute -= p.Traits.WeekendBoost * 240 / 1000; // fim de semana: mais cedo
        minute += rng.NextIntInclusive(-BehaviorTuning.SessionStartJitterMinutes, BehaviorTuning.SessionStartJitterMinutes);
        return Math.Clamp(minute, 0, 24 * 60 - 1);
    }

    private static int SessionMinutes(Person p, DeterministicRng rng, DayKind kind)
    {
        int j = BehaviorTuning.SessionDurationJitterPermille;
        int minutes = p.Traits.SessionMinutes * rng.NextIntInclusive(1000 - j, 1000 + j) / 1000;
        if (kind != DayKind.Weekday) minutes = minutes * (1000 + p.Traits.WeekendBoost / 2) / 1000;
        return Math.Max(10, minutes);
    }

    private void OnSessionStart(ScheduledEvent ev)
    {
        var p = _w.GetPerson((int)ev.TargetId);
        if (!_w.Server.IsOpen || p.CharacterId == 0) return;
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
        p.State = LifecycleState.Online;
        _w.OnlineCount++;
        _w.Today.SessionsStarted++;
        _w.Today.PeakOnline = Math.Max(_w.Today.PeakOnline, _w.OnlineCount);
        long day = Cal.DayIndex(Now);
        if (p.LastPlayedDay != day) { p.LastPlayedDay = day; _w.Today.ActivePlayers++; }

        ch.Status = CharacterStatus.Idle;
        ch.SessionStartTick = Now;
        ch.SessionEndTick = end;
        ch.LogoutPending = false;
        // Plano novo a cada sessao; o tedio se dissipa em parte fora do jogo (D-037).
        ch.Plan = null;
        ch.PlanReviewReason = "";
        SessionRules.FadeBoredomBetweenSessions(ch, _w.Content.Session);
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

        switch (ch.Status)
        {
            case CharacterStatus.Doing when ch.ActivityEndTick - Now <= BehaviorTuning.LogoutToleranceMinutes * SimTime.TicksPerMinute:
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

    // ---------------- Decisao ----------------

    private void Decide(Character ch, Person p)
    {
        var rng = Rng("behavior.decision", ch.Id * 1_000_000L + ch.DecisionCount);
        ch.DecisionCount++;
        var options = ActivityScoring.Evaluate(ch, p.Traits, _w.Content, _routes, _w.OccupancyOf, Now, rng, _quests, _goals);

        if (options.Count == 0)
        {
            // Nada viavel: espera e repensa.
            ch.Status = CharacterStatus.Doing;
            ch.TargetRegionId = ch.RegionId;
            ch.ActivityId = "";
            ch.ActivityStartTick = Now;
            ch.ActivityEndTick = Now + BehaviorTuning.IdleRethinkMinutes * SimTime.TicksPerMinute;
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
        long ticks = Math.Max(1, (dist * SimTime.TicksPerMinute + BehaviorTuning.WalkSpeed - 1) / BehaviorTuning.WalkSpeed);
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
        int minutes = Math.Max(1, a.TypicalMinutes * rng.NextIntInclusive(BehaviorTuning.ActivityDurationMin, BehaviorTuning.ActivityDurationMax) / 1000);
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

        if (ch.LogoutPending || Now >= ch.SessionEndTick) { Logout(ch, "session_time"); return; }
        Decide(ch, p);
    }

    /// <summary>Zona revelada: conhecimento, gosto por explorar e objetivos (D-036).</summary>
    private void Discovered(Character ch, string zoneId)
    {
        var p = _w.GetPerson(ch.PersonId);
        RequestPlanReview(ch, "zone_discovered");
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

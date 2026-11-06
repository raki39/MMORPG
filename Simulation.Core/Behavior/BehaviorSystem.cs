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
    public const string EvWorkInterrupt = "work_interrupt";
    /// <summary>A pessoa acorda (R1): fecha a conta do sono da noite com o inicio e o fim reais.</summary>
    public const string EvWake = "wake";
    /// <summary>Depois de uma pausa, a pessoa reconsidera voltar a jogar (v3, D-051).</summary>
    public const string EvConsiderReturn = "consider_return";

    /// <summary>Por que a sessao normal comecou (v3): no horario de costume, antes dele por vontade, de volta depois de uma pausa, primeira (cadastro ou volta).</summary>
    public const int StartUsual = 0, StartEarly = 1, StartReturn = 2, StartFirst = 3;
    public static string StartReasonName(int reason) => reason switch { StartEarly => "early_choice", StartReturn => "return", StartFirst => "first", _ => "usual_time" };

    /// <summary>Tipos de sessao (D-044): normal, pausa permitida do trabalho remoto, escapada no expediente.</summary>
    public const int SessionNormal = 0, SessionBreak = 1, SessionDuringWork = 2;

    /// <summary>Carga do evento de sessao: minutos, tipo, motivo do inicio (v3) e o dia da agenda a que pertence (-1 = nenhum).</summary>
    public static long SessionPayload(int minutes, int kind, long planDay = -1, int reason = StartUsual) =>
        (planDay + 1) * 100_000_000L + reason * 10_000_000L + kind * 1_000_000L + minutes;

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
        yield return (EvWorkInterrupt, OnWorkInterrupt);
        yield return (EvWake, OnWake);
        yield return (EvConsiderReturn, OnConsiderReturn);
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
        bool hol = cal.HolidayOn(date) != null, prevHol = cal.HolidayOn(date.AddDays(-1)) != null, nextHol = cal.HolidayOn(date.AddDays(1)) != null;
        var rs = _w.Content.Routine;
        // A recuperacao do dia e decidida uma vez (R2) e fica guardada: o dia montado nao muda quando o
        // deficit muda depois. Dia passado nunca planejado: sem recuperacao; dia futuro: pelo deficit atual.
        int? recovery = p.RecoveryByDay.TryGetValue(localDay, out int fixedRecovery) ? fixedRecovery
            : localDay < LocalDay(p, Now) ? 0 : null;
        var occ = OccOf(p);
        var next = date.AddDays(1);
        var c = CommitmentOf(p, localDay, DailySchedule.Blocks(p, occ, Dow(date), hol).Count == 0, cal.KindOf(date) != DayKind.Weekday || Dow(date) == p.DayOffIndex);
        // Compromisso de amanha (R6): a hora de dormir de hoje respeita o sono necessario antes dele.
        var nc = CommitmentOf(p, localDay + 1, DailySchedule.Blocks(p, occ, Dow(next), nextHol).Count == 0, cal.KindOf(next) != DayKind.Weekday || Dow(next) == p.DayOffIndex);
        return DailySchedule.Build(p, occ, rs, Dow(date), hol, prevHol, nextHol, c, recovery, nc?.Start ?? -1);
    }

    /// <summary>
    /// Compromisso ocasional de um dia livre (D-046): sorteio proprio por pessoa e dia (o mesmo
    /// dia da sempre o mesmo resultado), chance baixa e maior para quem e sociavel.
    /// </summary>
    private (int Start, int End, string Id)? CommitmentOf(Person p, long localDay, bool freeDay, bool eligibleDay)
    {
        var rs = _w.Content.Routine;
        if (!freeDay || !eligibleDay || rs.Commitments.Count == 0 || rs.CommitmentChancePermille <= 0) return null;
        var rng = Rng("routine.commitment", p.Id * 100_000L + localDay);
        int soc = Math.Clamp(p.Traits.Sociability, 0, 1000);
        if (!rng.ChancePermille(rs.CommitmentChancePermille * (600 + soc * 800 / 1000) / 1000)) return null;
        var kind = rs.Commitments[rng.PickWeighted(rs.Commitments.Select(k => Math.Max(0, k.Weight) * (k.Social ? 500 + soc : 1000)).ToList())];
        int from = ContentValidator.ParseClock(kind.StartFrom), to = ContentValidator.ParseClock(kind.StartTo);
        int start = rng.NextIntInclusive(from, Math.Max(from, to)) / 5 * 5;
        int minutes = rng.NextIntInclusive(kind.MinMinutes, Math.Max(kind.MinMinutes, kind.MaxMinutes)) / 5 * 5;
        return (start, start + minutes, kind.Id);
    }

    private int PreferredMinute(Person p, DeterministicRng rng) =>
        p.HabitualStartHour * 60 + rng.NextIntInclusive(-_w.Content.Behavior.SessionStartJitterMinutes, _w.Content.Behavior.SessionStartJitterMinutes);

    /// <summary>
    /// Disposicao do dia (R4): variacao pequena (+-permille) por pessoa e dia local, num fluxo
    /// proprio (o mesmo dia da sempre o mesmo valor, sem mexer nos outros sorteios).
    /// </summary>
    public int Disposition(Person p, long localDay)
    {
        int d = Rng("routine.disposition", p.Id * 100_000L + localDay).NextCenteredNoise(Math.Max(0, _w.Content.Routine.DispositionSpreadPermille));
        // Maratonista (P4c): depois de uma maratona, o dia seguinte comeca com menos disposicao.
        if (p.MarathonDaysSet.Contains(localDay - 1) && QuirkSystem.Active(_w.Content, p, QuirkIds.Maratonista))
            d -= _w.Content.QuirkCatalog.Effects.Maratonista.NextDayDispositionPenaltyPermille;
        return d;
    }

    /// <summary>Agenda de um dia local (a mais recente primeiro); null se nao houver.</summary>
    private static DayPlanRecord? PlanOf(Person p, long localDay)
    {
        for (int i = p.PlanList.Count - 1; i >= 0; i--)
            if (p.PlanList[i].LocalDay == localDay) return p.PlanList[i];
        return null;
    }

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
            int desired = DailySchedule.SessionMinutes(p, rs, jitter);
            if (DailySchedule.PlaceMain(day, preferred, desired, rs, out _, out int start, out int minutes)
                && dayStart + start * SimTime.TicksPerMinute > notBefore)
            {
                FixRecovery(p, d);   // o dia escolhido fica com a recuperacao usada aqui
                return (dayStart + start * SimTime.TicksPerMinute, minutes);
            }
            // Hoje a janela preferida ja passou: a proxima janela ainda aberta do mesmo dia.
            foreach (var (s, e) in day.Windows)
            {
                long ws = Math.Max(dayStart + s * SimTime.TicksPerMinute, notBefore + 1);
                long we = dayStart + e * SimTime.TicksPerMinute;
                if (we - ws >= rs.MinSessionMinutes * SimTime.TicksPerMinute)
                {
                    FixRecovery(p, d);
                    return (ws, (int)Math.Min(desired, (we - ws) / SimTime.TicksPerMinute));
                }
            }
        }
        return (notBefore + 1 + SimTime.TicksPerHour, p.Traits.SessionMinutes);
    }

    /// <summary>Fixa a recuperacao da manha do dia (se ainda nao fixada) e descarta as muito antigas.</summary>
    private void FixRecovery(Person p, long localDay)
    {
        if (!p.RecoveryByDay.ContainsKey(localDay))
            p.RecoveryByDay[localDay] = Math.Min(p.SleepDeficitMinutes, _w.Content.Routine.RecoveryFreeNightMinutes);
        while (p.RecoveryByDay.Count > 0 && p.RecoveryByDay.Keys.First() < localDay - _w.Content.Routine.PlanHistoryDays - 7)
            p.RecoveryByDay.Remove(p.RecoveryByDay.Keys.First());
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
        string classId = ChooseClass(p.Traits, p.Preferences, rng);
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
        int firstMinutes = DailySchedule.SessionMinutes(p, _w.Content.Routine, DurationJitter(rng));
        foreach (var (s, e) in today.Windows)
            if (s <= nowMin && nowMin < e) firstMinutes = Math.Max(_w.Content.Routine.MinSessionMinutes, Math.Min(firstMinutes, e - nowMin));
        StartSession(p, firstMinutes, SessionNormal, -1, StartFirst);
    }

    /// <summary>
    /// Classe pelo papel: tanque pela personalidade (agressividade, coordenacao, lealdade); dano
    /// pelas preferencias PvP e PvE e pela mecanica; suporte pela sociabilidade, paciencia e pela
    /// preferencia social.
    /// </summary>
    private string ChooseClass(PersonTraits t, PersonPreferences pr, DeterministicRng rng)
    {
        var classes = _w.Content.Classes.Values.ToList();
        var weights = classes.Select(c => c.Role switch
        {
            ClassRoles.Tank => 100 + t.Aggressiveness * 4 / 10 + t.Coordination * 3 / 10 + t.Loyalty * 3 / 10,
            ClassRoles.Damage => 100 + pr.Pvp * 4 / 10 + t.MechanicalSkill * 4 / 10 + pr.Pve * 2 / 10,
            ClassRoles.Support => 100 + t.Sociability * 5 / 10 + t.Patience * 3 / 10 + pr.Social * 2 / 10,
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
                _w.Scheduler.Schedule(backAt, EvSessionStart, p.Id, SessionPayload(backMinutes, SessionNormal, -1, StartFirst));
                RecordPlan(p, new DayPlanRecord(localDay, 0, 0, 1, "came_back", (int)((backAt - dayStart) / SimTime.TicksPerMinute), backMinutes, false));
                continue;
            }
            else
            {
                _traits.OnDaily(p, day);   // inatividade (D-036), antes de decidir se joga hoje
                // Personalidade principal (P4a): muda so com alvo confirmado por dias seguidos.
                if (PersonalitySystem.OnDaily(_w.Content, p, day) is { } pc)
                    _w.Emit("personality_changed", EventImportance.Normal, new[] { new EntityRef("person", p.Id) },
                        new[] { new EventCause(pc.Attribute, pc.Value) }, $"{pc.From}|{pc.To}|{pc.PositionPermille}");
                // Traits (P4d): aquisicao e perda por evidencia, so em dia jogado (jogou no dia local anterior).
                if (QuirkSystem.OnDaily(_w.Content, p, localDay, p.LastPlayedLocalDay == localDay - 1) is { } qc)
                {
                    var rule = QuirkSystem.RuleOf(_w.Content, qc.Id);
                    _w.Emit(qc.Change == "acquired" ? "trait_acquired" : "trait_lost", EventImportance.Normal, new[] { new EntityRef("person", p.Id) },
                        new[] { new EventCause(qc.Reason, qc.For), new EventCause("evidence_against", qc.Against), new EventCause("play_day", qc.PlayDay) },
                        $"{qc.Id}|{qc.PreviousSource}");
                }
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
        FixRecovery(p, localDay);
        var day = LayoutOf(p, localDay);
        // Sono (R1): a conta real fecha ao acordar (EvWake). Aqui so a projecao da noite em
        // andamento, para o cansaco de hoje: comeca na hora de deitar ou na saida do jogo, se for depois.
        long sleepStartTick = dayStart + day.SleepStart * SimTime.TicksPerMinute, wakeTick = dayStart + day.Wake * SimTime.TicksPerMinute;
        long realStart = sleepStartTick;
        if (p.IsOnline) realStart = Math.Max(realStart, _w.GetCharacter(p.CharacterId).SessionEndTick);
        else if (p.LastLogoutTick > realStart && p.LastLogoutTick < wakeTick) realStart = p.LastLogoutTick;
        int sleep = (int)Math.Max(0, (wakeTick - realStart) / SimTime.TicksPerMinute);
        int projectedDeficit = Math.Clamp(p.SleepDeficitMinutes + DailySchedule.DeficitChange(p.SleepNeedMinutes, sleep, rs), 0, DailySchedule.MaxDeficit(p, rs));
        if (wakeTick > Now) _w.Scheduler.Schedule(wakeTick, EvWake, p.Id, localDay);
        p.OpenWindows.RemoveAll(x => x.End < Now);
        foreach (var (s, e) in day.Windows)
            p.OpenWindows.Add((dayStart + s * SimTime.TicksPerMinute, dayStart + e * SimTime.TicksPerMinute, e >= day.Bed && !day.NightShiftAhead));
        int fatigue = Math.Min(1000, projectedDeficit * 1000 / Math.Max(1, rs.FatigueFullDeficitMinutes));
        var (workSessions, overlap, _) = p.State == LifecycleState.Inactive ? (0, 0, 0) : PlanWorkTime(p, day, dayStart, localDay, rng);

        // Probabilidade do dia (R2/R4): a base (0..1000, importancia = prioridade do jogo entre os
        // lazeres) vezes os fatores (permille, 1000 = x1), combinados e limitada a 0..1000 uma unica
        // vez no fim. Mesma politica na estimativa (DailySchedule.ExpectWeek).
        // - habito: o traco vezes a regularidade dos ultimos dias com oportunidade (faltar por
        //   obrigacao e neutro; sessao interrompida conta como jogo);
        // - disposicao do dia: sorteio pequeno, reproduzivel, por pessoa e dia;
        // - humor, inatividade e cansaco.
        int disposition = Disposition(p, localDay);
        int regularity = DailySchedule.Regularity(p.PlanList, localDay, rs.HabitHistoryDays).Regularity;
        int moodFactor = _w.NeutralMood ? 1000 : PlayFactor(p.Mood.Engagement, rt);   // o humor muda a rotina (D-039)
        long combined = DailySchedule.BaseChance(p, day, rs);
        combined = combined * DailySchedule.HabitFactor(p.Traits.Habit, regularity, rs) / 1000;
        combined = combined * (1000 + disposition) / 1000;
        combined = combined * moodFactor / 1000;
        if (p.State == LifecycleState.Inactive) combined = combined * rt.InactiveReturnPermille / 1000 * p.Mood.ReturnIntention / 1000;
        combined = combined * (1000 - fatigue * rs.FatigueChancePenalty / 1000) / 1000;   // cansaco (P3)
        int chance = (int)Math.Clamp(combined, 0, 1000);

        // Quando comecar (v3, D-051): a decisao de jogar hoje e o sorteio acima (um por dia); o horario
        // vem de um sorteio proprio do dia, com a vontade de comecar valendo por hora no tempo livre
        // e resistencia antes do horario de costume (o gosto no dia livre; o habitual no dia de
        // obrigacao). Toda janela livre conta, inclusive a de antes do trabalho.
        int session = DailySchedule.SessionMinutes(p, rs);
        int target = (day.FreeDay ? p.Traits.PreferredStartHour : p.HabitualStartHour) * 60
            + rng.NextIntInclusive(-_w.Content.Behavior.SessionStartJitterMinutes, _w.Content.Behavior.SessionStartJitterMinutes);
        int prio = DailySchedule.SessionPriority(p, day, rs);
        int pull = DailySchedule.Pull(prio, disposition, moodFactor, 0, 1000, rs);
        int u = Rng("routine.start", p.Id * 100_000L + localDay).NextInt(1_000_000);
        int start = DailySchedule.StartMinute(day, target, session, pull, prio, DailySchedule.StopPressure(0, fatigue, rs), u, rs);
        bool wants = rng.ChancePermille(chance);
        if (start < 0) { RecordPlan(p, new DayPlanRecord(localDay, day.FreeMinutes, day.Windows.Count, 0, "no_free_time", -1, 0, day.FreeDay, sleep, projectedDeficit, workSessions, overlap, CommitId(day), CommitStart(day), CommitEnd(day), day.RecoveryMinutes, prio, disposition, regularity)); return; }
        if (!wants)
        {
            string reason = p.State == LifecycleState.Inactive ? "inactive"
                : fatigue >= rs.TiredReasonFatigue ? "tired"
                : !_w.NeutralMood && p.Mood.Engagement < rt.EngagementNeutral - rs.LowEngagementGap ? "low_engagement" : "other_leisure";
            RecordPlan(p, new DayPlanRecord(localDay, day.FreeMinutes, day.Windows.Count, 0, reason, -1, 0, day.FreeDay, sleep, projectedDeficit, workSessions, overlap, CommitId(day), CommitStart(day), CommitEnd(day), day.RecoveryMinutes, prio, disposition, regularity));
            return;
        }
        // Procrastinador (P4c): adia o comeco dentro da janela; sem a sessao minima depois do atraso,
        // nao comeca nessa oportunidade e reconsidera na proxima janela valida (de novo com atraso).
        if (QuirkSystem.Active(_w.Content, p, QuirkIds.Procrastinador))
        {
            int delayed = Procrastinate(p, day, start, localDay);
            if (delayed < 0)
            {
                QuirkSystem.Count(p, "procrastinador_dias_sem_comecar");
                RecordPlan(p, new DayPlanRecord(localDay, day.FreeMinutes, day.Windows.Count, 0, "procrastinated", -1, 0, day.FreeDay, sleep, projectedDeficit, workSessions, overlap, CommitId(day), CommitStart(day), CommitEnd(day), day.RecoveryMinutes, prio, disposition, regularity));
                return;
            }
            start = delayed;
        }
        // A primeira sessao vai ate a sessao normal (ou o fim da janela); ai a pessoa decide se continua.
        int windowEnd = day.Windows.First(w => w.Start <= start && start < w.End).End;
        int minutes = Math.Min(session, windowEnd - start);
        int startReason = start < target ? StartEarly : StartUsual;
        _w.Scheduler.Schedule(dayStart + start * SimTime.TicksPerMinute, EvSessionStart, p.Id, SessionPayload(minutes, SessionNormal, localDay, startReason));
        RecordPlan(p, new DayPlanRecord(localDay, day.FreeMinutes, day.Windows.Count, 1, "played", start, minutes, day.FreeDay, sleep, projectedDeficit, workSessions, overlap, CommitId(day), CommitStart(day), CommitEnd(day), day.RecoveryMinutes, prio, disposition, regularity));
    }

    /// <summary>
    /// Procrastinador (P4c): o comeco atrasa (sorteio proprio por pessoa, dia e tentativa) dentro da
    /// janela; se nao sobra a sessao minima, tenta a proxima janela do dia. -1 = nenhuma serviu.
    /// Nunca cria tempo: so usa as janelas livres ja existentes.
    /// </summary>
    private int Procrastinate(Person p, DailySchedule.DayLayout day, int start, long localDay)
    {
        var e = _w.Content.QuirkCatalog.Effects.Procrastinador;
        var rs = _w.Content.Routine;
        int from = start, tries = 0;
        foreach (var (ws, we) in day.Windows.OrderBy(w => w.Start))
        {
            if (we <= from) continue;
            int t0 = Math.Max(ws, from);
            int delay = Rng("quirk.procrastinate", p.Id * 100_000L + localDay * 10 + tries++).NextIntInclusive(e.MinDelayMinutes, Math.Max(e.MinDelayMinutes, e.MaxDelayMinutes));
            int t = t0 + delay;
            if (we - t >= rs.MinSessionMinutes)
            {
                QuirkSystem.Count(p, "procrastinador_minutos", t - start);
                _w.Emit("session_delayed", EventImportance.Low, new[] { new EntityRef("person", p.Id) },
                    new[] { new EventCause(QuirkIds.Factor(QuirkIds.Procrastinador), t - start) }, $"{t - start} min");
                return t;
            }
            QuirkSystem.Count(p, "procrastinador_janelas_perdidas");
        }
        return -1;
    }

    /// <summary>
    /// Acordar (R1): a noite que terminou e contada uma unica vez, do inicio real (hora de deitar
    /// planejada, ou a saida do jogo se foi depois) ate agora. O deficit muda pela regra explicita
    /// (DeficitChange) e o registro do dia guarda o sono e o deficit reais.
    /// </summary>
    private void OnWake(ScheduledEvent ev)
    {
        var p = _w.GetPerson((int)ev.TargetId);
        var rs = _w.Content.Routine;
        long localDay = ev.Payload;
        var day = LayoutOf(p, localDay);
        long dayStart = LocalDayStart(p, localDay);
        long planned = dayStart + day.SleepStart * SimTime.TicksPerMinute;
        long plannedWake = dayStart + day.Wake * SimTime.TicksPerMinute;
        long start = planned;
        if (p.IsOnline) start = Now;   // ainda jogando ao amanhecer: nao dormiu
        else if (p.LastLogoutTick > planned && p.LastLogoutTick <= Now) start = p.LastLogoutTick;
        int slept = (int)Math.Max(0, (Now - start) / SimTime.TicksPerMinute);
        // Deslocar o sono (R6): foi dormir mais tarde e nada obriga a acordar agora: acorda mais tarde,
        // ate completar o sono necessario ou ate a obrigacao/compromisso de hoje. Uma vez por noite.
        if (rs.SleepShift && !p.IsOnline && p.SleepShiftedDay != localDay && slept < p.SleepNeedMinutes && Now == plannedWake)
        {
            p.SleepShiftedDay = localDay;
            int latest = DailySchedule.LatestWake(day, rs);
            long target = Now + (long)(p.SleepNeedMinutes - slept) * SimTime.TicksPerMinute;
            if (latest != int.MaxValue) target = Math.Min(target, dayStart + latest * SimTime.TicksPerMinute);
            target = Math.Min(target, dayStart + (day.Bed - rs.MinWindowMinutes) * SimTime.TicksPerMinute);
            if (target - Now >= 5 * SimTime.TicksPerMinute)
            {
                p.SleepUntilTick = target;
                _w.Scheduler.Schedule(target, EvWake, p.Id, localDay);
                return;
            }
        }
        int shifted = (int)Math.Max(0, (Now - plannedWake) / SimTime.TicksPerMinute);
        // O que avancou sobre o sono planejado, menos o que foi recuperado acordando mais tarde.
        int cut = Math.Max(0, (int)Math.Max(0, (start - planned) / SimTime.TicksPerMinute) - shifted);
        p.SleepUntilTick = 0;
        p.SleepDeficitMinutes = Math.Clamp(p.SleepDeficitMinutes + DailySchedule.DeficitChange(p.SleepNeedMinutes, slept, rs), 0, DailySchedule.MaxDeficit(p, rs));
        if (cut > 0) { p.LastSleepCutMinutes = cut; p.SleepCutTotalMinutes += cut; }
        foreach (var rec in p.PlanList)
            if (rec.LocalDay == localDay) { rec.SleepMinutes = slept; rec.DeficitMinutes = p.SleepDeficitMinutes; rec.SleepShiftMinutes = shifted; }
        // Dormiu: a saciedade de ontem nao passa para hoje (v3). Quem amanheceu jogando segue com a sua.
        if (!p.IsOnline) { p.Satiety = 0; p.SatietyTick = Now; p.Unfinished = false; }
    }

    /// <summary>
    /// Trabalho remoto (D-044): pausa permitida (jogar no almoco, pela importancia do jogo) e,
    /// raramente, uma escapada no expediente (flexibilidade do trabalho x importancia do jogo x
    /// falta de autocontrole x folga do dia). Sessoes curtas, interrompiveis por chamado do trabalho.
    /// Nunca em blocos presenciais, deslocamento ou estudo. Devolve (sessoes, minutos da escapada sobre
    /// o trabalho, minutos planejados no expediente); estes saem do tempo desejado do dia (R4).
    /// </summary>
    private (int Sessions, int Overlap, int Minutes) PlanWorkTime(Person p, DailySchedule.DayLayout day, long dayStart, long localDay, DeterministicRng rng)
    {
        var rs = _w.Content.Routine;
        var occ = OccOf(p);
        if (occ == null || day.Remote.Count == 0) return (0, 0, 0);
        int imp = p.Traits.GameImportance, sessions = 0, overlap = 0, total = 0;
        foreach (var work in day.Remote)
        {
            foreach (var (bs, be) in work.Breaks)
            {
                if (be - bs < rs.MinSessionMinutes + 5 || !rng.ChancePermille(rs.BreakSessionChancePermille * imp / 1000)) continue;
                int m = Math.Min(be - bs - 5, Math.Max(rs.MinSessionMinutes, p.Traits.SessionMinutes / 2));
                _w.Scheduler.Schedule(dayStart + (bs + 2) * SimTime.TicksPerMinute, EvSessionStart, p.Id, SessionPayload(m, SessionBreak, localDay));
                sessions++;
                total += m;
            }
            if (occ.PlayDuringWorkFlexibility <= 0) continue;
            int workload = rng.NextIntInclusive(0, 1000);   // carga do trabalho hoje
            int chance = rs.SlackChanceAtMax * occ.PlayDuringWorkFlexibility / 1000 * imp / 1000
                * (1000 - p.Traits.SelfControl) / 1000 * (1000 - workload) / 1000;
            if (!rng.ChancePermille(chance)) continue;
            int minutes = Math.Clamp(rs.SlackMinutes.Mean + rng.NextCenteredNoise(rs.SlackMinutes.Spread), 5, 120);
            int from = work.Start + rs.SlackEdgeMinutes, to = work.End - rs.SlackEdgeMinutes - minutes;
            if (to <= from) continue;
            int start = rng.NextIntInclusive(from, to);
            foreach (var (bs, be) in work.Breaks)   // escapada nao cai na pausa (seria pausa)
                if (start < be && bs < start + minutes) start = Math.Min(be, to);
            if (start + minutes > work.End - rs.SlackEdgeMinutes) continue;
            _w.Scheduler.Schedule(dayStart + start * SimTime.TicksPerMinute, EvSessionStart, p.Id, SessionPayload(minutes, SessionDuringWork, localDay));
            if (rng.ChancePermille(rs.WorkInterruptChancePermille))
                _w.Scheduler.Schedule(dayStart + (start + rng.NextIntInclusive(3, Math.Max(3, minutes - 1))) * SimTime.TicksPerMinute, EvWorkInterrupt, p.Id);
            sessions++;
            overlap += minutes;
            total += minutes;
        }
        return (sessions, overlap, total);
    }

    /// <summary>
    /// Chamado do trabalho durante a escapada (D-044): termina a atividade se falta pouco (regra
    /// de saida); senao larga e sai. Sem punicao profissional por enquanto.
    /// </summary>
    private void OnWorkInterrupt(ScheduledEvent ev)
    {
        var p = _w.GetPerson((int)ev.TargetId);
        if (!p.IsOnline) return;
        var ch = _w.GetCharacter(p.CharacterId);
        if (ch.SessionKind != SessionDuringWork) return;
        bool finishing = ch.Status == CharacterStatus.Doing
            && ch.ActivityEndTick - Now <= _w.Content.Routine.WorkCallToleranceMinutes * SimTime.TicksPerMinute
            && ch.ActivityEndTick <= ch.SessionHardEndTick;
        _w.Emit("work_interruption", EventImportance.Low, new[] { new EntityRef("person", p.Id), new EntityRef("character", ch.Id) },
            new[] { new EventCause(finishing ? "finished_activity" : "left_activity", 1000) }, ch.ActivityId.Length > 0 ? ActivityTypeOf(ch) : "");
        if (finishing) ch.LogoutPending = true;
        else Logout(ch, "work_interruption");
    }

    private static string CommitId(DailySchedule.DayLayout d) => d.Commitments.Count > 0 ? d.Commitments[0].Id : "";
    private static int CommitStart(DailySchedule.DayLayout d) => d.Commitments.Count > 0 ? d.Commitments[0].Start : 0;
    private static int CommitEnd(DailySchedule.DayLayout d) => d.Commitments.Count > 0 ? d.Commitments[0].End : 0;

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
        bool nostalgic = QuirkSystem.Active(_w.Content, p, QuirkIds.Saudosista);   // Saudosista (P4c): volta mais
        if (nostalgic) chance = chance * _w.Content.QuirkCatalog.Effects.Saudosista.QuitReturnPermille / 1000;
        if (!rng.ChancePermille(chance)) return false;
        p.State = LifecycleState.Offline;
        p.TimesReturned++;
        p.NostalgiaReturns++;
        p.NostalgiaSpentTick = Now;
        p.LastReturnDay = day;
        _w.Today.Returns++;
        _exp.UpdateEngagement(p, ch);
        _w.Emit("player_returned", EventImportance.Normal, new[] { new EntityRef("person", p.Id), new EntityRef("character", ch.Id) },
            nostalgic ? new[] { new EventCause("nostalgia", nostalgia), new EventCause("days_away", (int)(day - p.QuitDay)), new EventCause(QuirkIds.Factor(QuirkIds.Saudosista), 1000) }
                : new[] { new EventCause("nostalgia", nostalgia), new EventCause("days_away", (int)(day - p.QuitDay)) }, $"{day - p.QuitDay} dias");
        if (nostalgic) QuirkSystem.Count(p, "saudosista_voltou");
        return true;
    }

    private void OnSessionStart(ScheduledEvent ev)
    {
        var p = _w.GetPerson((int)ev.TargetId);
        if (!_w.Server.IsOpen || p.CharacterId == 0 || p.State == LifecycleState.Quit) return;
        if (p.SleepUntilTick > Now)
        {
            // Dormindo mais (sono deslocado, R6): comeca depois de acordar e se preparar, se ainda houver janela.
            long at = p.SleepUntilTick + _w.Content.Routine.PrepMinutes * SimTime.TicksPerMinute;
            long windowEnd = -1;
            foreach (var (s, e, _) in p.OpenWindows)
                if (s <= at && at < e && e > windowEnd) windowEnd = e;
            int planned = (int)(ev.Payload % 1_000_000);
            int left = windowEnd < 0 ? 0 : (int)Math.Min(planned, (windowEnd - at) / SimTime.TicksPerMinute);   // a sessao continua dentro da janela
            if (left >= _w.Content.Routine.MinSessionMinutes) _w.Scheduler.Schedule(at, EvSessionStart, p.Id, ev.Payload - planned + left);
            return;
        }
        StartSession(p, (int)(ev.Payload % 1_000_000), (int)(ev.Payload / 1_000_000 % 10), ev.Payload / 100_000_000 - 1, (int)(ev.Payload / 10_000_000 % 10));
    }

    private void StartSession(Person p, int minutes, int kind = SessionNormal, long planDay = -1, int startReason = StartUsual)
    {
        var ch = _w.GetCharacter(p.CharacterId);
        var rs = _w.Content.Routine;
        SatietyToNow(p);
        var plan = planDay >= 0 ? PlanOf(p, planDay) : null;
        // Jogou no trabalho hoje (v3): a saciedade que sobrou e a experiencia de la pesam na vontade de
        // comecar a noite (sem descontar minutos da sessao). Sem saciedade, nada muda.
        if (plan != null && kind == SessionNormal && !p.IsOnline && plan.Sittings == 0 && startReason != StartReturn && p.Satiety > 0)
        {
            int pull = PullNow(p, plan, LastExperienceDelta(p), 1000, true);
            int keep = (int)((long)DailySchedule.Choice(pull, plan.SessionPriority, DailySchedule.StopPressure(p.Satiety, 0, rs)) * 1000
                / Math.Max(1, DailySchedule.Choice(pull, plan.SessionPriority, 0)));
            if (!Rng("routine.go", p.Id * 100_000L + planDay).ChancePermille(keep)) { plan.SkippedAfterWork++; return; }
        }
        long end = Now + minutes * SimTime.TicksPerMinute;
        if (p.IsOnline)
        {
            if (kind != SessionNormal) return;   // ja conectado: a escapada nao se soma
            // Ja conectado: a sessao apenas se estende.
            if (end > ch.SessionEndTick)
            {
                ch.SessionEndTick = end;
                ch.SessionHardEndTick = Math.Max(ch.SessionHardEndTick, HardEndFor(p, SessionNormal, end));
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
        p.Unfinished = false;
        if (plan != null && kind == SessionNormal) { plan.Sittings++; if (startReason == StartReturn) plan.Returns++; }
        _w.OnlineCount++;
        _w.Today.SessionsStarted++;
        _w.Today.PeakOnline = Math.Max(_w.Today.PeakOnline, _w.OnlineCount);
        long day = Cal.DayIndex(Now);
        if (p.LastPlayedDay != day) { p.LastPlayedDay = day; _w.Today.ActivePlayers++; }

        ch.Status = CharacterStatus.Idle;
        ch.SessionStartTick = Now;
        ch.RepeatMinutes = 0;   // a repeticao do Inquieto conta dentro da sessao
        ch.LastHesitationPair = "";   // Indeciso (P4c): a mesma dupla pode fazer hesitar de novo em outra sessao
        ch.SessionEndTick = end;
        ch.LogoutPending = false;
        ch.SessionKind = kind;
        ch.SessionPlanDay = planDay;
        ch.StopReason = "";
        ch.Continues = 0;
        ch.ContinuedMinutes = 0;
        ch.SessionHardEndTick = HardEndFor(p, kind, end);
        ch.SessionExtensions = 0;
        ch.ExtendedMinutes = 0;
        ch.SleepCutStartTick = 0;
        ch.ExtendMotive = "";
        // Plano novo a cada sessao; o tedio se dissipa em parte fora do jogo (D-037).
        ch.Plan = null;
        ch.PlanReviewReason = "";
        SessionRules.FadeBoredomBetweenSessions(ch, _w.Content.Session);
        _exp.OnSessionStart(ch);
        _w.Scheduler.Schedule(end, EvSessionEnd, p.Id);
        _w.Emit("session_started", EventImportance.Low, new[] { new EntityRef("person", p.Id) },
            kind == SessionBreak ? new[] { new EventCause("work_break", minutes) }
            : kind == SessionDuringWork ? new[] { new EventCause("during_work", minutes) }
            : new[] { new EventCause("normal", 1000), new EventCause(StartReasonName(startReason), 1000) },
            $"{minutes} min");
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

        if (ch.SessionKind == SessionNormal && TryContinue(p, ch)) return;
        if (ch.SessionKind == SessionNormal && TryStayUp(p, ch)) return;

        // Adiar a saida (terminar a atividade ou o trecho de viagem) so cabe dentro do limite absoluto (R2).
        switch (ch.Status)
        {
            case CharacterStatus.Doing when ch.ActivityEndTick - Now <= _w.Content.Behavior.LogoutToleranceMinutes * SimTime.TicksPerMinute
                                            && ch.ActivityEndTick <= ch.SessionHardEndTick:
            case CharacterStatus.Traveling when ch.MoveEndTick <= ch.SessionHardEndTick:
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
        SatietyToNow(p);                 // todo o tempo jogado conta, inclusive o interrompido (v3)
        p.LastLogoutTick = Now;
        if (reason == "session_time" && ch.StopReason.Length > 0) reason = ch.StopReason;
        if (reason == "frustration") p.RageQuits++;
        // Maratonista (P4c): sessao com mais do dobro da normal marca o dia (o seguinte comeca com menos disposicao).
        if (ch.SessionKind == SessionNormal && QuirkSystem.Active(_w.Content, p, QuirkIds.Maratonista)
            && (Now - ch.SessionStartTick) / SimTime.TicksPerMinute * 1000 >= (long)DailySchedule.SessionMinutes(p, _w.Content.Routine) * _w.Content.QuirkCatalog.Effects.Maratonista.MarathonSessionsPermille)
        {
            p.MarathonDaysSet.Add(LocalDay(p, Now));
            while (p.MarathonDaysSet.Count > 14) p.MarathonDaysSet.Remove(p.MarathonDaysSet.Min);
            QuirkSystem.Count(p, "maratonista_maratonas");
        }
        long planDay = ch.SessionPlanDay;
        int endedKind = ch.SessionKind;
        if (planDay >= 0 && PlanOf(p, planDay) is { } plan)
            plan.PlayedMinutes += (int)((Now - ch.SessionStartTick) / SimTime.TicksPerMinute);
        if (endedKind == SessionDuringWork && reason == "work_interruption") p.Unfinished = true;   // vontade de terminar (v3)
        ch.SessionPlanDay = -1;
        // Corte de sono compartilhado (R3): quanto desta sessao avancou sobre o periodo de sono da janela.
        if (ch.SessionKind == SessionNormal)
        {
            long bedTick = -1;
            foreach (var (s, e, atBed) in p.OpenWindows)
                if (atBed && s <= ch.SessionStartTick && ch.SessionStartTick < e) bedTick = e;
            if (bedTick > 0 && Now > bedTick)
            {
                int cut = (int)((Now - Math.Max(bedTick, ch.SessionStartTick)) / SimTime.TicksPerMinute);
                if (p.SleepCutBedTick != bedTick) { p.SleepCutBedTick = bedTick; p.SleepCutUsedMinutes = 0; }
                p.SleepCutUsedMinutes += cut;
            }
        }
        if (ch.SleepCutStartTick > 0 && Now > ch.SleepCutStartTick)
        {
            // Ficou acordado jogando (P3): informa o motivo; o sono perdido entra no deficit uma vez,
            // quando a pessoa acorda (R1).
            int cut = (int)((Now - ch.SleepCutStartTick) / SimTime.TicksPerMinute);
            _w.Emit("stayed_up_late", EventImportance.Normal, new[] { new EntityRef("person", p.Id), new EntityRef("character", ch.Id) },
                new[] { new EventCause(ch.ExtendMotive.Length > 0 ? ch.ExtendMotive : "enjoying", 1000), new EventCause("sleep_cut", cut) }, $"{cut} min");
        }
        ch.SleepCutStartTick = 0;
        ch.SessionKind = SessionNormal;
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
        bool rageFuse = reason == "frustration" && QuirkSystem.Active(_w.Content, p, QuirkIds.PavioCurto);
        _w.Emit("session_ended", EventImportance.Low, new[] { new EntityRef("person", p.Id) },
            rageFuse ? new[] { new EventCause(reason, 1000), new EventCause(QuirkIds.Factor(QuirkIds.PavioCurto), 1000) } : new[] { new EventCause(reason, 1000) });
        // Parou por vontade, pelo fim da janela ou frustrado: se o dia ainda tem tempo, pode voltar depois de uma pausa (v3).
        if (endedKind == SessionNormal && planDay >= 0 && reason is "satisfied" or "window_end" or "frustration") ScheduleReturn(p, planDay, rageFuse);
    }

    /// <summary>
    /// Limite absoluto de uma sessao (R2). Pausa ou escapada do trabalho: o fim planejado (volta ao
    /// trabalho). Sessao normal: o fim da janela livre onde ela termina; se essa janela acaba na
    /// hora de dormir, mais o corte de sono autorizado. Nunca entra em preparo, deslocamento ou obrigacao.
    /// </summary>
    private long HardEndFor(Person p, int kind, long plannedEnd)
    {
        if (kind != SessionNormal) return plannedEnd;
        long hard = -1;
        bool atBed = false;
        foreach (var (s, e, bed) in p.OpenWindows)
            if (s <= plannedEnd && plannedEnd <= e && e > hard) { hard = e; atBed = bed; }
        if (hard < 0) return plannedEnd;
        return atBed ? hard + AllowedSleepCutMinutes(p, hard) * SimTime.TicksPerMinute : hard;
    }

    /// <summary>
    /// Corte de sono autorizado depois da hora de deitar (R2): o menor entre o teto por noite, o que
    /// cabe no teto do deficit (contando a falta que a noite ja teria) e o que mantem o sono minimo
    /// antes do proximo acordar planejado. A proxima obrigacao nunca muda.
    /// </summary>
    private int AllowedSleepCutMinutes(Person p, long bedTick)
    {
        var rs = _w.Content.Routine;
        long cal = LocalDay(p, bedTick - 1);
        for (long d = cal; d >= cal - 1; d--)
        {
            var day = LayoutOf(p, d);
            if (LocalDayStart(p, d) + day.Bed * SimTime.TicksPerMinute != bedTick) continue;
            var next = LayoutOf(p, d + 1);
            int plannedSleep = next.Wake - next.SleepStart;
            // Deslocar o sono (R6): se nada obriga a acordar na hora planejada, parte do que passar da
            // hora de deitar e recuperado acordando mais tarde; so o resto vira deficit.
            int room = 0;
            if (rs.SleepShift)
            {
                int latest = DailySchedule.LatestWake(next, rs);
                room = Math.Max(0, Math.Min(latest == int.MaxValue ? p.SleepNeedMinutes : latest - next.Wake, next.Bed - rs.MinWindowMinutes - next.Wake));
                room = Math.Min(room, p.SleepNeedMinutes);
            }
            int effective = plannedSleep + room;
            int byDeficit = DailySchedule.MaxDeficit(p, rs) - p.SleepDeficitMinutes + (effective - p.SleepNeedMinutes);
            // O teto por noite e compartilhado: o que outra sessao ja cortou deste mesmo periodo sai do total.
            int used = p.SleepCutBedTick == bedTick ? p.SleepCutUsedMinutes : 0;
            return Math.Max(0, Math.Min(rs.MaxSleepCutMinutes, Math.Min(byDeficit, effective - rs.MinSleepMinutes)) - used);
        }
        return 0;
    }

    /// <summary>Janela livre que contem o fim previsto da sessao: minutos que sobram e se ela termina na hora de dormir.</summary>
    private (int Room, bool AtBed, long End) WindowAfter(Person p, Character ch)
    {
        long end = ch.SessionEndTick;
        long limit = -1;
        bool atBed = false;
        foreach (var (s, e, bed) in p.OpenWindows)
            if (s <= end && end <= e && e > limit) { limit = e; atBed = bed; }
        return limit < 0 ? (0, false, end) : ((int)((limit - end) / SimTime.TicksPerMinute), atBed, limit);
    }

    /// <summary>
    /// Continuar (v3, D-051): ao chegar a sessao normal (e depois a cada bloco), a pessoa decide se
    /// continua jogando dentro do tempo livre. A vontade de jogar (prioridade x disposicao x humor x
    /// experiencia da sessao x tendencia a prolongar) e comparada com as outras opcoes de lazer e a
    /// vontade de parar (saciedade e cansaco). Parar e sempre possivel; o fim da janela e o limite.
    /// </summary>
    private bool TryContinue(Person p, Character ch)
    {
        var rs = _w.Content.Routine;
        var (room, atBed, _) = WindowAfter(p, ch);
        if (room < rs.ContinueMinMinutes) { if (ch.StopReason.Length == 0) ch.StopReason = atBed ? "bedtime" : "window_end"; return false; }
        SatietyToNow(p);
        var plan = ch.SessionPlanDay >= 0 ? PlanOf(p, ch.SessionPlanDay) : null;
        int prio = plan?.SessionPriority ?? Math.Clamp(p.Traits.GameImportance, 0, 1000);
        int exp = _w.NeutralMood ? 0 : SessionScoreSoFar(ch) - p.Mood.Satisfaction;
        int pull = PullNow(p, plan, exp, DailySchedule.TendencyFactor(p, rs), false);
        int chance = DailySchedule.Choice(pull, prio, DailySchedule.StopPressure(p.Satiety, DailySchedule.Fatigue(p, rs), rs));
        if (!Rng("routine.continue", ch.Id * 1_000_000L + ch.DecisionCount * 16 + ch.Continues).ChancePermille(chance))
        {
            ch.StopReason = "satisfied";
            return false;
        }
        int block = Math.Min(rs.ContinueBlockMinutes, room);
        ch.Continues++;
        ch.ContinuedMinutes += block;
        ch.SessionEndTick += block * SimTime.TicksPerMinute;
        _w.Scheduler.Schedule(ch.SessionEndTick, EvSessionEnd, p.Id);
        _w.Emit("session_continued", EventImportance.Low, new[] { new EntityRef("person", p.Id) },
            new[] { new EventCause("continue", chance), new EventCause("satiety", p.Satiety) }, $"+{block} min");
        return true;
    }

    /// <summary>
    /// Ficar acordado jogando (P3, so no fim do dia): quando o tempo livre acaba na hora de dormir,
    /// a pessoa pode passar dela, com motivo explicito (gostando da sessao ou perto da meta), pela
    /// tendencia a prolongar e contra o autocontrole e o cansaco. Nunca antes de uma obrigacao, de
    /// um turno da noite ou dentro do tempo livre (isso e a decisao de continuar); sempre dentro do
    /// corte de sono autorizado (R2/R3).
    /// </summary>
    private bool TryStayUp(Person p, Character ch)
    {
        var rs = _w.Content.Routine;
        var rt = _w.Content.Retention;
        if (_w.NeutralMood || ch.SessionKind != SessionNormal || ch.SessionExtensions >= rs.ExtendMaxTimesPerSession) return false;
        var (room, atBed, windowEnd) = WindowAfter(p, ch);
        if (!atBed && ch.SleepCutStartTick == 0) return false;            // termina por obrigacao, preparo ou compromisso
        if (room >= rs.ContinueMinMinutes) return false;                  // ainda ha tempo livre: decisao de continuar
        string motive = SessionScoreSoFar(ch) >= rt.ExtendScoreAbove ? "enjoying" : GoalClose(ch) ? "goal_close" : "";
        if (motive.Length == 0) return false;
        int fatigue = DailySchedule.Fatigue(p, rs);
        var t = p.Traits;
        int chance = rt.ExtendChance * t.ExtendTendency / 1000 * (1000 - t.SelfControl * rs.SelfControlWeight / 1000) / 1000 * (1000 - fatigue) / 1000;
        if (!Rng("behavior.session_flex", ch.Id * 1_000_000L + ch.DecisionCount + ch.SessionExtensions).ChancePermille(chance)) return false;
        int planned = (int)((ch.SessionEndTick - ch.SessionStartTick) / SimTime.TicksPerMinute);
        int extra = Math.Min(Math.Max(10, planned * rt.ExtendPermille / 1000 * (rs.ExtendTendencyBase + t.ExtendTendency) / 1000),
            rs.ExtendMaxMinutes - ch.ExtendedMinutes);
        int toHard = (int)Math.Max(0, (ch.SessionHardEndTick - ch.SessionEndTick) / SimTime.TicksPerMinute);
        int total = Math.Min(extra, toHard);
        if (total < 10) return false;
        long cutFrom = Math.Max(windowEnd, ch.SessionEndTick);
        if (ch.SleepCutStartTick == 0) ch.SleepCutStartTick = cutFrom;
        ch.SessionExtensions++;
        ch.ExtendedMinutes += total;
        ch.ExtendMotive = motive;
        ch.StopReason = "stayed_up";
        ch.SessionEndTick += total * SimTime.TicksPerMinute;
        _w.Scheduler.Schedule(ch.SessionEndTick, EvSessionEnd, p.Id);
        var causes = new List<EventCause> { new(motive, motive == "enjoying" ? SessionScoreSoFar(ch) : 1000), new("sleep_cut", total) };
        _w.Emit("session_extended", EventImportance.Low, new[] { new EntityRef("person", p.Id) }, causes.ToArray(), $"+{total} min");
        return true;
    }

    /// <summary>
    /// Vontade de jogar agora (v3) para uma decisao do dia: prioridade e disposicao da agenda, humor
    /// (fora do cenario controlado), experiencia e, se for comecar ou voltar depois de uma sessao
    /// interrompida pelo trabalho, a vontade de terminar.
    /// </summary>
    private int PullNow(Person p, DayPlanRecord? plan, int experienceDelta, int tendencyFactor, bool canFinish)
    {
        var rs = _w.Content.Routine;
        int prio = plan?.SessionPriority ?? Math.Clamp(p.Traits.GameImportance, 0, 1000);
        int mood = _w.NeutralMood ? 1000 : PlayFactor(p.Mood.Engagement, _w.Content.Retention);
        int pull = DailySchedule.Pull(prio, plan?.DispositionPermille ?? 0, mood, experienceDelta, tendencyFactor, rs);
        if (canFinish && p.Unfinished) pull = Math.Min(2000, pull + rs.UnfinishedBonusPermille);
        return pull;
    }

    /// <summary>Experiencia da ultima sessao em relacao a satisfacao habitual (0 no cenario controlado).</summary>
    private int LastExperienceDelta(Person p) => _w.NeutralMood ? 0 : p.Mood.LastSessionScore - p.Mood.Satisfaction;

    /// <summary>Atualiza a saciedade ate agora: jogando ela cresce; fora do jogo, cai (v3).</summary>
    private void SatietyToNow(Person p)
    {
        long minutes = (Now - p.SatietyTick) / SimTime.TicksPerMinute;
        // Maratonista (P4c): com a sessao boa, a saciedade cresce mais devagar.
        if (minutes > 0 && p.IsOnline && QuirkSystem.Active(_w.Content, p, QuirkIds.Maratonista)
            && SessionScoreSoFar(_w.GetCharacter(p.CharacterId)) >= _w.Content.QuirkCatalog.Effects.Maratonista.GoodScoreAbove)
        {
            long slow = QuirkSystem.Carry(p, "maratonista_saciedade", (int)Math.Min(int.MaxValue, minutes), _w.Content.QuirkCatalog.Effects.Maratonista.SatietyPermille);
            QuirkSystem.Count(p, "maratonista_minutos_sem_saciar", (int)(minutes - slow));
            minutes = slow;
        }
        if (minutes > 0)
            p.Satiety = p.IsOnline
                ? DailySchedule.SatietyAfterPlay(p.Satiety, minutes, DailySchedule.SessionMinutes(p, _w.Content.Routine), _w.Content.Routine)
                : DailySchedule.SatietyAfterPause(p.Satiety, minutes, _w.Content.Routine);
        p.SatietyTick = Now;
    }

    /// <summary>
    /// Depois de parar (v3): se ainda ha tempo no dia, a pessoa reconsidera voltar depois de uma
    /// pausa, na mesma janela ou na proxima (depois da aula, do compromisso).
    /// </summary>
    private void ScheduleReturn(Person p, long planDay, bool rageFuse = false)
    {
        var rs = _w.Content.Routine;
        var plan = PlanOf(p, planDay);
        if (plan == null || plan.Sittings >= rs.MaxSessionsPerDay || !_w.Server.IsOpen) return;
        var day = LayoutOf(p, planDay);
        long dayStart = LocalDayStart(p, planDay);
        int nowMin = (int)((Now - dayStart + SimTime.TicksPerMinute - 1) / SimTime.TicksPerMinute);   // arredonda para cima: a pausa nunca fica menor
        int pause = Rng("routine.pause", p.Id * 100_000L + planDay * 10 + plan.Sittings)
            .NextIntInclusive(rs.ReturnPauseMinMinutes, Math.Max(rs.ReturnPauseMinMinutes, rs.ReturnPauseMaxMinutes));
        // Pavio curto (P4c): depois de largar com raiva, volta mais cedo.
        if (rageFuse) pause = pause * _w.Content.QuirkCatalog.Effects.PavioCurto.ReturnPausePermille / 1000;
        int at = DailySchedule.NextReturnMinute(day, nowMin, pause, rs);
        if (at < 0) return;
        _w.Scheduler.Schedule(dayStart + at * SimTime.TicksPerMinute, EvConsiderReturn, p.Id, planDay);
    }

    /// <summary>
    /// Voltar (v3): depois da pausa, a saciedade caiu; a vontade de jogar (com a experiencia da ultima
    /// sessao) e comparada com as outras opcoes e a vontade de parar. Se volta, joga ate a sessao normal
    /// (ou o fim da janela) e decide de novo se continua.
    /// </summary>
    private void OnConsiderReturn(ScheduledEvent ev)
    {
        var p = _w.GetPerson((int)ev.TargetId);
        if (!_w.Server.IsOpen || p.CharacterId == 0 || p.IsOnline || p.State is LifecycleState.Quit or LifecycleState.Banned) return;
        if (p.SleepUntilTick > Now) return;   // dormindo (sono deslocado, R6)
        var rs = _w.Content.Routine;
        long planDay = ev.Payload;
        var plan = PlanOf(p, planDay);
        if (plan == null || plan.Sittings >= rs.MaxSessionsPerDay) return;
        SatietyToNow(p);
        int pull = PullNow(p, plan, LastExperienceDelta(p), 1000, true);
        // Depois do turno da noite (R6): o cansaco do turno pesa contra jogar antes de dormir.
        int nowMin = (int)((Now - LocalDayStart(p, planDay)) / SimTime.TicksPerMinute);
        int tired = LayoutOf(p, planDay).OptionalWindows.Any(w => w.Start <= nowMin && nowMin < w.End) ? rs.PostShiftTirednessPermille : 0;
        int chance = DailySchedule.Choice(pull, plan.SessionPriority, DailySchedule.StopPressure(p.Satiety, DailySchedule.Fatigue(p, rs), rs) + tired);
        if (!Rng("routine.return", p.Id * 100_000L + planDay * 10 + plan.Sittings).ChancePermille(chance)) { plan.DeclinedReturns++; return; }
        long windowEnd = -1;
        foreach (var (s, e, _) in p.OpenWindows)
            if (s <= Now && Now < e && e > windowEnd) windowEnd = e;
        if (windowEnd < 0) return;
        int minutes = (int)Math.Min(DailySchedule.SessionMinutes(p, rs), (windowEnd - Now) / SimTime.TicksPerMinute);
        if (minutes < rs.MinSessionMinutes) return;
        // Procrastinador (P4c): a volta tambem atrasa; sem a sessao minima, reconsidera na proxima janela.
        if (QuirkSystem.Active(_w.Content, p, QuirkIds.Procrastinador))
        {
            var e = _w.Content.QuirkCatalog.Effects.Procrastinador;
            int delay = Rng("quirk.procrastinate", p.Id * 100_000L + planDay * 10 + 5 + plan.Sittings).NextIntInclusive(e.MinDelayMinutes, Math.Max(e.MinDelayMinutes, e.MaxDelayMinutes));
            long at = Now + delay * SimTime.TicksPerMinute;
            int left = (int)Math.Min(minutes, (windowEnd - at) / SimTime.TicksPerMinute);
            if (left < rs.MinSessionMinutes)
            {
                QuirkSystem.Count(p, "procrastinador_janelas_perdidas");
                ScheduleReturn(p, planDay);   // reconsidera numa proxima janela valida
                return;
            }
            QuirkSystem.Count(p, "procrastinador_minutos", delay);
            _w.Emit("session_delayed", EventImportance.Low, new[] { new EntityRef("person", p.Id) },
                new[] { new EventCause(QuirkIds.Factor(QuirkIds.Procrastinador), delay) }, $"{delay} min");
            _w.Scheduler.Schedule(at, EvSessionStart, p.Id, SessionPayload(left, SessionNormal, planDay, StartReturn));
            return;
        }
        StartSession(p, minutes, SessionNormal, planDay, StartReturn);
    }

    private bool GoalClose(Character ch)
    {
        int close = _w.Content.Routine.GoalClosePermille;
        foreach (var g in ch.Goals)
        {
            if (g.Type == GoalTypes.PracticeActivity && g.TargetValue > 0 && g.Progress * 1000L / g.TargetValue >= close) return true;
            if ((g.Type == GoalTypes.ReachLevel || g.Type == GoalTypes.UnlockLevel) && ch.Level == g.TargetValue - 1
                && ch.Experience * 1000 / Math.Max(1, ProgressionRules.XpToNextLevel(_w.Content.Progression, ch.Level)) >= close) return true;
        }
        return false;
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
        var personality = _w.Content.PersonalityRules(p.PersonalityId);
        var options = ActivityScoring.Evaluate(ch, p.Traits, p.Preferences, _w.Content, _routes, _w.OccupancyOf, Now, rng, _quests, _goals, p.PlaceImpressions, personality, p);

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

        // Indeciso (P4c): empate apertado entre as duas melhores opcoes, medido SEM o ruido da decisao
        // (o ruido nao fabrica empate): perde alguns minutos decidindo (espera, sem atividade) e decide
        // de novo depois. Uma vez por decisao e uma vez por dupla de opcoes na sessao.
        var fx = _w.Content.QuirkCatalog.Effects;
        if (!ch.Hesitated && options.Count >= 2 && QuirkSystem.Active(_w.Content, p, QuirkIds.Indeciso)
            && Now + fx.Indeciso.DelayMinutes * SimTime.TicksPerMinute < ch.SessionEndTick
            && SessionRules.CloseCall(options, fx.Indeciso.TieGap) is { } tie && tie.Pair != ch.LastHesitationPair)
        {
            ch.Hesitated = true;
            ch.LastHesitationPair = tie.Pair;
            QuirkSystem.Count(p, "indeciso_hesitou");
            QuirkSystem.Count(p, "indeciso_minutos", fx.Indeciso.DelayMinutes);
            _w.Emit("hesitated", EventImportance.Low, new[] { new EntityRef("character", ch.Id) },
                new[] { new EventCause(QuirkIds.Factor(QuirkIds.Indeciso), tie.Gap) }, tie.Pair);
            ch.Status = CharacterStatus.Doing;
            ch.TargetRegionId = ch.RegionId;
            ch.ActivityId = "";
            ch.ActivityStartTick = Now;
            ch.ActivityEndTick = Now + fx.Indeciso.DelayMinutes * SimTime.TicksPerMinute;
            _w.Scheduler.Schedule(ch.ActivityEndTick, EvActivityEnd, ch.Id);
            return;
        }
        ch.Hesitated = false;

        // Plano da sessao (D-037): continua o foco salvo vantagem clara de outra opcao;
        // acontecimentos relevantes liberam a revisao. Viagem ja feita nao conta (sem custo afundado).
        var top = options[0];
        var best = top;
        var focus = ch.Plan == null ? null : options.FirstOrDefault(o => o.Key == ch.Plan.FocusKey);
        bool continued = false;
        int gap = 0;
        var ss = _w.Content.Session;
        // Paciente (P4a): acontecimentos nao derrubam o plano da sessao; ele termina o que planejou.
        bool review = ch.PlanReviewReason.Length > 0 && personality?.Rule != PersonalityRuleIds.FinishPlan;
        string marginQuirk = "";
        if (focus != null && focus != top && !review)
        {
            int baseMargin = Now < ch.Plan!.CommitUntilTick ? ss.CommitMargin : ss.SwitchMargin;
            int margin = baseMargin;
            // Persistente (P4c): depois de um reves, mantem o plano com margem maior.
            bool persistent = ch.LastSetbackTick >= 0 && Now - ch.LastSetbackTick <= fx.Persistente.SetbackWindowMinutes * SimTime.TicksPerMinute
                && QuirkSystem.Active(_w.Content, p, QuirkIds.Persistente);
            if (persistent) margin = margin * fx.Persistente.MarginAfterSetbackPermille / 1000;
            // Impulsivo (P4c): troca de plano com margem menor.
            bool impulsive = QuirkSystem.Active(_w.Content, p, QuirkIds.Impulsivo);
            if (impulsive) margin = margin * fx.Impulsivo.MarginPermille / 1000;
            gap = top.Utility - focus.Utility;
            if (gap < margin) { best = focus; continued = true; }
            // A trait decidiu quando o resultado seria outro com a margem de base.
            if (continued && gap >= baseMargin) { marginQuirk = QuirkIds.Factor(QuirkIds.Persistente); QuirkSystem.Count(p, "persistente_manteve_plano"); }
            else if (!continued && gap < baseMargin) { marginQuirk = QuirkIds.Factor(QuirkIds.Impulsivo); QuirkSystem.Count(p, "impulsivo_trocou_plano"); }
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
        else { ch.RepeatCount = 0; ch.RepeatMinutes = 0; ch.LastActivityKey = best.Key; }
        _w.Today.ActivitiesChosen++;

        var causes = best.Factors.Where(f => f.Key != "noise" && f.Contribution != 0)
            .OrderByDescending(f => Math.Abs(f.Contribution)).ThenBy(f => f.Key, StringComparer.Ordinal).Take(4)
            .Select(f => new EventCause(f.Key, f.Contribution)).ToList();
        if (continued) causes.Insert(0, new EventCause("plan_continuity", gap));
        if (marginQuirk.Length > 0) causes.Insert(0, new EventCause(marginQuirk, gap));
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
        // Traits (P4c): o Estudioso demora mais nas etapas de visita; o Observador explora mais devagar.
        var fxd = _w.Content.QuirkCatalog.Effects;
        var pd = _w.GetPerson(ch.PersonId);
        if (a.Type == ActivityTypes.Quest && QuirkSystem.Active(_w.Content, pd, QuirkIds.Estudioso))
        {
            int longer = Math.Max(minutes, minutes * fxd.Estudioso.VisitStepPermille / 1000);
            QuirkSystem.Count(pd, "estudioso_minutos_a_mais", longer - minutes);
            minutes = longer;
        }
        else if (a.Type == ActivityTypes.Explore && QuirkSystem.Active(_w.Content, pd, QuirkIds.Observador))
        {
            int longer = Math.Max(minutes, minutes * fxd.Observador.ExplorePermille / 1000);
            QuirkSystem.Count(pd, "observador_minutos_a_mais", longer - minutes);
            minutes = longer;
        }
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
            if (region.Id + "/" + a.Id == ch.LastActivityKey) ch.RepeatMinutes += minutes;   // tempo seguido (Inquieto)
            int levelBefore = ch.Level;
            AwardExperience(ch, a, region, minutes);
            _quests.OnActivityDone(ch, region, a, minutes);
            // Atributos vivos e objetivos (D-036): pratica, gosto, risco enfrentado.
            int challenge = ProgressionRules.ChallengeLevel(_w.Content, a);
            int fit = ProgressionRules.MilliXpPerMinute(_w.Content, a, levelBefore) > 0 ? ProgressionRules.Efficiency(_w.Content.Progression, levelBefore, challenge) : 0;
            _traits.OnActivity(p, ch, a, minutes, fit, ActivityScoring.RiskOf(region, a, challenge, levelBefore), firstTimeType);
            _goals.OnActivity(p, ch, a, minutes);
            ch.ZoneMinutes[region.Id] = (ch.ZoneMinutes.TryGetValue(region.Id, out int zm) ? zm : 0) + minutes;
            SessionRules.AccrueBoredom(ch, p.Traits, p.Preferences, a.Type, minutes, _w.Content.Session,
                QuirkSystem.Active(_w.Content, p, QuirkIds.Otimizador) ? _w.Content.QuirkCatalog.Effects.Otimizador.BoredomPermille : 1000);
            int valence = _exp.OnActivity(p, ch, region, a, minutes, fit, fit > 0, ActivityScoring.RiskOf(region, a, challenge, levelBefore), firstTimeType);
            // Frustracao alta e mais uma experiencia ruim: pode largar a sessao na hora (D-039).
            var rt = _w.Content.Retention;
            // Pavio curto (P4c): mais chance de largar a sessao com raiva.
            bool shortFuse = QuirkSystem.Active(_w.Content, p, QuirkIds.PavioCurto);
            int rageChance = shortFuse ? rt.RageQuitChance * _w.Content.QuirkCatalog.Effects.PavioCurto.RageQuitPermille / 1000 : rt.RageQuitChance;
            rageQuit = !_w.NeutralMood && valence < 0 && p.Mood.Frustration >= rt.RageQuitFrustration
                && Rng("behavior.session_flex", ch.Id * 1_000_000L + ch.DecisionCount + 500_000).ChancePermille(rageChance);
            if (rageQuit && shortFuse) QuirkSystem.Count(p, "pavio_curto_largou_com_raiva");
            // Observador (P4c): ao explorar, guarda pistas do conteudo das zonas vizinhas (fonte valida para a descoberta).
            if (a.Type == ActivityTypes.Explore && QuirkSystem.Active(_w.Content, p, QuirkIds.Observador))
            {
                int left = _w.Content.QuirkCatalog.Effects.Observador.HintsPerExplore;
                foreach (var c in region.Connections.OrderBy(x => x.To, StringComparer.Ordinal))
                {
                    if (left <= 0) break;
                    foreach (var act in _w.Content.Regions[c.To].AllActivities().OrderBy(x => x.Id, StringComparer.Ordinal))
                    {
                        if (left <= 0) break;
                        if (ch.Hints.Add(c.To + "/" + act.Id)) { left--; QuirkSystem.Count(p, "observador_pistas"); }
                    }
                }
            }
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
        // Impulsivo (P4c): as vezes revisa o plano sem acontecimento nenhum, por impulso.
        if (QuirkSystem.Active(_w.Content, p, QuirkIds.Impulsivo) && ch.Plan != null && ch.PlanReviewReason.Length == 0
            && Rng("quirk.impulse", ch.Id * 1_000_000L + ch.DecisionCount).ChancePermille(_w.Content.QuirkCatalog.Effects.Impulsivo.ImpulseReviewPermille))
        {
            RequestPlanReview(ch, QuirkIds.Factor(QuirkIds.Impulsivo));
            QuirkSystem.Count(p, "impulsivo_revisou_plano");
        }
        Decide(ch, p);
    }

    /// <summary>Zona revelada: conhecimento, gosto por explorar e objetivos (D-036).</summary>
    private void Discovered(Character ch, string zoneId)
    {
        var p = _w.GetPerson(ch.PersonId);
        RequestPlanReview(ch, "zone_discovered");
        var md = _w.Content.Mood;
        int curiosity = 500 + p.Preferences.Exploration;   // quem prefere explorar valoriza mais a descoberta
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
        // Qualquer XP ganho e progresso das metas de nivel e de desbloqueio (D-055).
        _goals.OnExperience(ch);
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

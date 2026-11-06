using Simulation.Core.Content;
using Simulation.Core.Events;
using Simulation.Core.Population;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Core.Behavior;

/// <summary>
/// Quests estruturadas (D-031). A area de quest (quadro) oferece um catalogo; aceitar e
/// entregar sao acoes pontuais no quadro e so aparecem como opcao quando ha quest elegivel
/// ou entrega pendente. As etapas avancam com o que o personagem realmente faz no mundo:
/// minutos de uma atividade (caca, coleta, exploracao...) ou a acao num ponto de missao.
/// Limitacao explicita: etapas de caca e coleta contam TEMPO de atividade (provisorio);
/// abates e itens so existem no M3/M5, entao nada e registrado como "derrotou N" ou "coletou N".
/// </summary>
public sealed class QuestSystem
{
    private readonly WorldState _w;
    private readonly Action<Character, long, string> _gainXp;
    private readonly RouteFinder? _routes;
    /// <summary>Distancia entre pontos de atividade (conteudo fixo; cache derivado, fora do estado).</summary>
    private readonly Dictionary<(string, string, string, string), int> _dist = new();
    /// <summary>Missoes que tem a missao da chave como pre-requisito (conteudo fixo).</summary>
    private Dictionary<string, List<QuestDefinition>>? _unlockedBy;

    public QuestSystem(WorldState world, Action<Character, long, string> gainXp, RouteFinder? routes = null)
    {
        _w = world;
        _gainXp = gainXp;
        _routes = routes;
    }

    /// <summary>Uma acao no quadro (D-067): aceitar a oferta, largando antes a missao indicada (troca), com o motivo.</summary>
    public sealed record BoardAction(QuestDefinition Offer, ActiveQuest? Drop, string Why, int Interest, string Motive, bool OnTheWay);

    /// <summary>Avisos para objetivos e atributos (D-036): etapa cumprida e quest entregue.</summary>
    internal Action<Character, string>? StepCompleted { get; set; }
    /// <summary>Avanco sem concluir a etapa (minutos parciais) ou missao aceita: progresso de meta (D-065).</summary>
    internal Action<Character, string>? Progressed { get; set; }
    internal Action<Character, string>? Completed { get; set; }

    private ContentDatabase C => _w.Content;
    private long Now => _w.Clock.Tick;
    /// <summary>Dia das missoes: conta a partir do reset diario do servidor (com reset 0, o dia do servidor).</summary>
    private long Today => _w.Clock.Calendar.DayIndex(Math.Max(0, Now - C.QuestSettings.DailyResetMinute * SimTime.TicksPerMinute));
    private QuestJournalDefinition J => C.QuestSettings.Journal;
    private QuestAcceptanceDefinition A => C.QuestSettings.Journal.Acceptance;
    /// <summary>Escolha de ir ao quadro e acao nele pelo mesmo plano (D-067).</summary>
    public bool UsesPlan => J.Enabled && (J.ConsistentVisits || A.Enabled);

    /// <summary>Proximo reset diario do servidor (instante), a partir de agora ou do instante dado.</summary>
    public long NextResetTick(long from = -1)
    {
        long at = from < 0 ? Now : from;
        long r = C.QuestSettings.DailyResetMinute * SimTime.TicksPerMinute;
        if (r > 0 && at < r) return r;
        return r + ((at - r) / SimTime.TicksPerDay + 1) * SimTime.TicksPerDay;
    }

    /// <summary>Etapa de caca ou coleta: conta tempo, nao abates nem itens (ate o M3/M5).</summary>
    public static bool IsProvisional(ContentDatabase content, QuestStepDefinition s) =>
        s.Type == QuestStepTypes.Activity
        && content.Regions.TryGetValue(s.RegionId, out var r)
        && r.FindActivity(s.ActivityId) is { } a
        && a.Type is ActivityTypes.Hunt or ActivityTypes.Gather or ActivityTypes.Patrol or ActivityTypes.Ambush;

    public QuestBoardDefinition? BoardAt(string regionId, string activityId) =>
        C.BoardByActivity.TryGetValue(regionId + "/" + activityId, out var b) ? b : null;

    public static QuestStepDefinition? CurrentStep(ContentDatabase content, ActiveQuest a) =>
        content.Quests.TryGetValue(a.QuestId, out var q) && a.StepIndex < q.Steps.Count ? q.Steps[a.StepIndex] : null;

    public bool CanAccept(Character ch) => ch.ActiveQuestList.Count < C.QuestSettings.MaxActivePerCharacter;

    private int DailyCount(Character ch, string boardId) =>
        ch.DailyDay == Today && ch.DailyByBoard.TryGetValue(boardId, out int n) ? n : 0;

    /// <summary>Requisitos, repeticao e limites (nivel, classe, pre-requisitos, diarias).</summary>
    public bool IsEligible(Character ch, QuestDefinition q)
    {
        if (ch.Level < q.MinLevel || (q.MaxLevel > 0 && ch.Level > q.MaxLevel)) return false;
        if (q.ClassIds.Count > 0 && !q.ClassIds.Contains(ch.ClassId)) return false;
        if (ch.ActiveQuestList.Any(a => a.QuestId == q.Id)) return false;
        // Largada ha pouco (D-066): o conteudo pode exigir um intervalo para aceitar de novo.
        if (C.QuestSettings.ReacceptAfterDropDays > 0 && ch.QuestLog.TryGetValue(q.Id, out var dr) && dr.DroppedDay >= 0
            && Today - dr.DroppedDay < C.QuestSettings.ReacceptAfterDropDays) return false;
        foreach (var pre in q.PrerequisiteQuestIds)
            if (!ch.QuestLog.TryGetValue(pre, out var pr) || pr.Completions == 0) return false;
        ch.QuestLog.TryGetValue(q.Id, out var rec);
        int done = rec?.Completions ?? 0;
        return q.Category switch
        {
            QuestCategories.Main => done == 0,
            QuestCategories.Secondary => done == 0 || (q.RepeatCooldownDays > 0 && Today - rec!.LastCompletedDay >= q.RepeatCooldownDays),
            QuestCategories.Daily => (rec == null || rec.LastCompletedDay != Today)
                                     && C.QuestBoards.TryGetValue(q.BoardId, out var b) && DailyCount(ch, b.Id) < b.DailyLimit,
            _ => false,
        };
    }

    /// <summary>Quests que o quadro oferece ao personagem agora, na ordem em que ele aceitaria.</summary>
    public IEnumerable<QuestDefinition> Offers(Character ch, QuestBoardDefinition board) =>
        (C.QuestsByBoard.TryGetValue(board.Id, out var list) ? list : Array.Empty<QuestDefinition>())
            .Where(q => IsEligible(ch, q))
            .OrderBy(q => q.Category switch { QuestCategories.Main => 0, QuestCategories.Secondary => 1, _ => 2 })
            .ThenBy(q => Math.Abs(q.RecommendedLevel - ch.Level))
            .ThenBy(q => q.Id, StringComparer.Ordinal);

    public bool HasDelivery(Character ch, QuestBoardDefinition board) => ReadyCount(ch, board) > 0;

    /// <summary>Ha diaria pronta para entregar neste quadro (vence no reset).</summary>
    public bool HasReadyDaily(Character ch, QuestBoardDefinition board) =>
        ch.ActiveQuestList.Any(a => C.Quests.TryGetValue(a.QuestId, out var q) && q.BoardId == board.Id && q.Category == QuestCategories.Daily && a.StepIndex >= q.Steps.Count);

    /// <summary>Quests prontas para entregar neste quadro.</summary>
    public int ReadyCount(Character ch, QuestBoardDefinition board) =>
        ch.ActiveQuestList.Count(a => C.Quests.TryGetValue(a.QuestId, out var q) && q.BoardId == board.Id && a.StepIndex >= q.Steps.Count);

    /// <summary>A atividade avanca a etapa atual de alguma quest ativa.</summary>
    public bool AdvancesStep(Character ch, string regionId, string activityId) =>
        ch.ActiveQuestList.Any(a => CurrentStep(C, a) is { } s && s.RegionId == regionId && s.ActivityId == activityId);

    /// <summary>XP da recompensa: fracao do XP do nivel de referencia, com o rendimento pela diferenca de nivel.</summary>
    public static long RewardXp(ContentDatabase content, Character ch, QuestDefinition q) => RewardXp(content, ch.Level, q);

    public static long RewardXp(ContentDatabase content, int level, QuestDefinition q)
    {
        var p = content.Progression;
        if (level >= p.LevelCap) return 0;
        int refLevel = q.ScalesWithLevel ? level : q.RecommendedLevel;
        long baseXp = ProgressionRules.XpToNextLevel(p, refLevel) * q.Rewards.XpPermille / 1000;
        return baseXp * ProgressionRules.Efficiency(p, level, refLevel) / 1000;
    }

    /// <summary>
    /// Fim de um ciclo de atividade: avanca etapas que pedem essa atividade; no quadro,
    /// entrega as quests prontas e aceita as melhores ofertas ate encher as vagas.
    /// </summary>
    public void OnActivityDone(Character ch, RegionDefinition region, ActivityDefinition a, int minutes)
    {
        // Fazer a atividade da etapa de uma diaria que expirou intocada e mudanca real: ela pode voltar (D-067).
        if (A.Enabled && A.IdleExpiryBlock && minutes > 0)
            foreach (var (id, rec) in ch.QuestLog)
                if (rec.IdleExpired && C.Quests.TryGetValue(id, out var iq) && iq.Steps.Count > 0
                    && iq.Steps[0].RegionId == region.Id && iq.Steps[0].ActivityId == a.Id)
                    rec.IdleExpired = false;
        foreach (var aq in ch.ActiveQuestList.ToList())
        {
            var step = CurrentStep(C, aq);
            if (step == null || step.RegionId != region.Id || step.ActivityId != a.Id) continue;
            aq.StepMinutes += minutes;
            if (aq.StepMinutes < step.Minutes)
            {
                if (minutes > 0) Progressed?.Invoke(ch, aq.QuestId);   // avanco parcial da etapa tambem e progresso (D-065)
                continue;
            }
            var q = C.Quests[aq.QuestId];
            aq.StepIndex++;
            aq.StepMinutes = 0;
            _w.Emit("quest_step_completed", EventImportance.Low, new[] { new EntityRef("character", ch.Id) },
                new[] { new EventCause(step.Type, 1000) }, $"{q.Id}#{aq.StepIndex}");
            StepCompleted?.Invoke(ch, q.Id);
        }

        var board = BoardAt(region.Id, a.Id);
        if (board == null) return;
        foreach (var aq in ch.ActiveQuestList.ToList())
            if (C.Quests.TryGetValue(aq.QuestId, out var q) && q.BoardId == board.Id && aq.StepIndex >= q.Steps.Count)
                Deliver(ch, aq, q, board);
        // Na mesma visita, pega quantas couberem (como um jogador faria no quadro). Independente
        // (P4a): so aceita missoes que combinam com o proprio gosto.
        var person = _w.GetPerson(ch.PersonId);
        // Visita coerente (D-067): faz o que o plano diz, o mesmo que tornou o quadro uma opcao.
        if (UsesPlan)
        {
            foreach (var act in PlanBoard(person, ch, board))
            {
                if (act.Drop != null)
                    Drop(ch, act.Drop, "replaced:" + act.Offer.Id, Value(person, ch, C.Quests[act.Drop.QuestId], act.Drop), Value(person, ch, act.Offer, null));
                Accept(ch, act.Offer, act);
            }
            return;
        }
        bool ownPath = C.PersonalityRules(person.PersonalityId)?.Rule == PersonalityRuleIds.OwnPath;
        if (!J.Enabled)
        {
            while (CanAccept(ch) && Offers(ch, board).FirstOrDefault(q => !ownPath || FitsTaste(person.Preferences, q)) is { } offer)
                Accept(ch, offer);
            return;
        }
        // Diario (D-066): primeiro a missao da meta, se esta aqui (com troca, se preciso).
        foreach (var g in ch.Goals.Where(g => g.Type == GoalTypes.CompleteQuest).ToList())
            if (Offers(ch, board).FirstOrDefault(q => q.Id == g.Target) is { } wanted && Take(person, ch, wanted, "goal")) { }
        // Demais ofertas: guarda uma vaga para a missao de uma meta que fica em outro quadro.
        bool reserve = ch.Goals.Any(g => g.Type == GoalTypes.CompleteQuest && ch.ActiveQuestList.All(x => x.QuestId != g.Target)
            && C.Quests.TryGetValue(g.Target, out var gq) && gq.BoardId != board.Id);
        int limit = C.QuestSettings.MaxActivePerCharacter - (reserve ? 1 : 0);
        while (ch.ActiveQuestList.Count < limit && Offers(ch, board).FirstOrDefault(q => !ownPath || FitsTaste(person.Preferences, q)) is { } offer)
            Accept(ch, offer);
        // Vagas cheias: troca de prioridade (uma por visita), se a oferta vale mais que a missao mais fraca.
        if (!reserve && !CanAccept(ch) && Offers(ch, board).FirstOrDefault(q => !ownPath || FitsTaste(person.Preferences, q)) is { } best)
            Take(person, ch, best, "priority");
    }

    /// <summary>
    /// O que a pessoa faria neste quadro agora (D-067), na ordem: a missao de cada meta que esta aqui (com troca,
    /// se preciso); as ofertas que ela quer, ate encher as vagas (guardando uma para a missao de uma meta em outro
    /// quadro); com as vagas cheias, uma troca de prioridade, que nao larga o que acabou de aceitar. A escolha de
    /// ir ao quadro e a acao nele usam este mesmo plano. As prontas para entregar aqui contam como vaga livre.
    /// Com o aceite avaliado, "quer" e ter motivacao (Interest) acima do minimo, caber antes do reset (diaria) e
    /// nao ser a diaria que expirou intocada no mesmo estado; sem ele, todas as que cabem, na ordem do quadro.
    /// </summary>
    public List<BoardAction> PlanBoard(Person p, Character ch, QuestBoardDefinition board, long arrival = -1)
    {
        long at = arrival < 0 ? Now : arrival;   // na decisao de ir ao quadro: o instante da chegada
        var plan = new List<BoardAction>();
        if (!J.Enabled) return plan;
        var active = new List<ActiveQuest>(ch.ActiveQuestList.Count + 1);
        foreach (var aq in ch.ActiveQuestList)
            if (!(C.Quests.TryGetValue(aq.QuestId, out var dq) && dq.BoardId == board.Id && aq.StepIndex >= dq.Steps.Count)) active.Add(aq);
        int max = C.QuestSettings.MaxActivePerCharacter;
        var offers = Offers(ch, board).ToList();
        if (offers.Count == 0) return plan;
        var taken = new HashSet<string>(StringComparer.Ordinal);
        void Plan(QuestDefinition q, ActiveQuest? drop, string why, int interest, string motive, bool same)
        {
            plan.Add(new BoardAction(q, drop, why, interest, motive, same));
            if (drop != null) active.Remove(drop);
            active.Add(new ActiveQuest(q.Id, Now, ch.PlayedTicksAt(Now)));
            taken.Add(q.Id);
        }
        // 1) A missao da meta, se esta aqui (com troca, se preciso).
        foreach (var g in ch.GoalList)
        {
            if (g.Type != GoalTypes.CompleteQuest || taken.Contains(g.Target)) continue;
            var wanted = offers.FirstOrDefault(q => q.Id == g.Target);
            if (wanted == null) continue;
            if (active.Count < max) Plan(wanted, null, "goal", 1000, "goal", false);
            else if (SwapTarget(p, ch, wanted, active, taken) is { } weakest) Plan(wanted, weakest, "goal", 1000, "goal", false);
        }
        // 2) Vaga guardada para a missao de uma meta que fica em outro quadro.
        bool reserve = ch.GoalList.Any(g => g.Type == GoalTypes.CompleteQuest && active.All(x => x.QuestId != g.Target)
            && C.Quests.TryGetValue(g.Target, out var gq) && gq.BoardId != board.Id);
        int limit = max - (reserve ? 1 : 0);
        // 3) As ofertas que quer.
        bool ownPath = C.PersonalityRules(p.PersonalityId)?.Rule == PersonalityRuleIds.OwnPath;
        var wantedOffers = new List<(QuestDefinition Q, int Interest, string Motive, bool Same)>();
        foreach (var q in offers)
        {
            if (taken.Contains(q.Id) || (ownPath && !FitsTaste(p.Preferences, q))) continue;
            if (!A.Enabled) { wantedOffers.Add((q, 0, "board_order", false)); continue; }
            var it = Interest(p, ch, q, board, active);
            if (it.Interest < A.MinInterest || !FitsBeforeReset(p, ch, q, it.Travel, at) || IdleBlocked(ch, q)) continue;
            wantedOffers.Add((q, it.Interest, it.Motive, it.OnTheWay));
        }
        if (A.Enabled) wantedOffers = wantedOffers.OrderByDescending(x => x.Interest).ToList();   // estavel: empate fica na ordem do quadro
        foreach (var w in wantedOffers)
        {
            if (active.Count >= limit) break;
            Plan(w.Q, null, "free", w.Interest, w.Motive, w.Same);
        }
        // 4) Vagas cheias: uma troca de prioridade, pela primeira oferta que quer e que vale a troca.
        if (!reserve && active.Count >= max)
            foreach (var w in wantedOffers)
            {
                if (taken.Contains(w.Q.Id)) continue;
                if (SwapTarget(p, ch, w.Q, active, taken) is { } weakest) { Plan(w.Q, weakest, "priority", w.Interest, w.Motive, w.Same); break; }
            }
        return plan;
    }

    /// <summary>
    /// Motivacao para aceitar a oferta (permille; D-067). O maior motivo: a recompensa no nivel (XP sobre o XP do
    /// nivel, relativo a referencia); a relevancia (principal com a campanha como objetivo; liberar missao no nivel;
    /// meta de praticar o tema de uma etapa); o gosto pelo tema das etapas (Preferencias). Vezes o deslocamento
    /// adicional, como nas metas de missao: do quadro ate a primeira etapa ou, se for menor, o desvio real a partir
    /// da etapa de outra missao ativa (de la ate esta etapa e daqui ao quadro, menos o caminho que ja faria dela ao
    /// quadro). Devolve tambem a ida do quadro ate a etapa (Travel), para o prazo, e o deslocamento usado (Added).
    /// </summary>
    internal (int Interest, string Motive, bool OnTheWay, int Travel, int Added) Interest(Person p, Character ch, QuestDefinition q, QuestBoardDefinition board, IReadOnlyList<ActiveQuest> active)
    {
        int best = RewardShare(ch, q);
        string motive = "reward";
        void Consider(int value, string why) { if (value > best) { best = value; motive = why; } }
        if (q.Category == QuestCategories.Main && CampaignActive(p)) Consider(1000, "campaign");
        if (Unlocks(ch, q)) Consider(A.UnlockRelevance, "unlocks");
        if (PracticeGoalFor(ch, q)) Consider(A.GoalRelevance, "practice_goal");
        Consider(Taste(p.Preferences, q), "taste");
        int speed = Math.Max(1, Combat.CharacterStats.MoveSpeed(C, ch));
        var first = q.Steps.Count > 0 && C.Regions.ContainsKey(q.Steps[0].RegionId) && _routes != null ? q.Steps[0] : null;
        int travel = first == null ? 0 : Minutes(Dist(board.RegionId, board.ActivityId, first.RegionId, first.ActivityId), speed);
        int added = travel;
        bool onTheWay = false;
        if (first != null)
        {
            int back = Dist(first.RegionId, first.ActivityId, board.RegionId, board.ActivityId);
            foreach (var x in active)
            {
                if (CurrentStep(C, x) is not { } os || !C.Regions.ContainsKey(os.RegionId)) continue;
                int there = Dist(os.RegionId, os.ActivityId, first.RegionId, first.ActivityId);
                int direct = Dist(os.RegionId, os.ActivityId, board.RegionId, board.ActivityId);
                if (there < 0 || back < 0 || direct < 0) continue;
                int extra = Minutes(Math.Max(0, there + back - direct), speed);
                if (extra < added) { added = extra; onTheWay = true; }
            }
        }
        var gs = C.GoalSettings;
        int place = 1000 - Math.Min(gs.QuestTravelMaxPenaltyPermille, added * gs.QuestTravelPerMinutePermille);
        return (best * place / 1000, motive, onTheWay, travel, added);
    }

    /// <summary>Recompensa da missao no nivel da pessoa (permille, ate 1000): XP da recompensa sobre o XP do nivel, relativo a referencia.</summary>
    internal int RewardShare(Character ch, QuestDefinition q)
    {
        var pg = C.Progression;
        if (ch.Level >= pg.LevelCap) return 0;
        long reference = ProgressionRules.XpToNextLevel(pg, ch.Level) * A.RewardReferencePermille / 1000;
        return reference <= 0 ? 0 : (int)Math.Min(1000, RewardXp(C, ch, q) * 1000 / reference);
    }

    /// <summary>Gosto pelo tema das etapas: a maior preferencia entre os tipos das atividades (missao so de visita: 0).</summary>
    internal int Taste(PersonPreferences pr, QuestDefinition q)
    {
        int best = 0;
        foreach (var step in q.Steps)
            if (StepType(step) is { } type && PreferenceFor(type) is { } pref) best = Math.Max(best, pr.Get(pref));
        return best;
    }

    private string? StepType(QuestStepDefinition step) =>
        C.Regions.TryGetValue(step.RegionId, out var r) && r.FindActivity(step.ActivityId) is { } a ? a.Type : null;

    /// <summary>Preferencia que corresponde ao tipo de atividade (null: neutro, como visita, treino ou reparo).</summary>
    private static string? PreferenceFor(string activityType) => activityType switch
    {
        ActivityTypes.Hunt or ActivityTypes.Boss => "pve",
        ActivityTypes.Patrol or ActivityTypes.Ambush => "pvp",
        ActivityTypes.Gather => "gathering",
        ActivityTypes.Explore => "exploration",
        ActivityTypes.Trade => "trade",
        ActivityTypes.Socialize => "social",
        _ => null,
    };

    /// <summary>A pessoa tem meta de praticar o tipo de atividade de alguma etapa.</summary>
    private bool PracticeGoalFor(Character ch, QuestDefinition q)
    {
        foreach (var g in ch.GoalList)
            if (g.Type == GoalTypes.PracticeActivity && q.Steps.Any(s => StepType(s) == g.Target)) return true;
        return false;
    }

    private bool CampaignActive(Person p) =>
        p.Objectives.Any(o => o.State == ObjectiveStates.Active && C.Objectives.TryGetValue(o.Id, out var od) && od.Criterion == ObjectiveCriteria.MainCampaign);

    /// <summary>A missao libera outra no nivel da pessoa (e pre-requisito dela).</summary>
    private bool Unlocks(Character ch, QuestDefinition q)
    {
        if (_unlockedBy == null)
        {
            var map = new Dictionary<string, List<QuestDefinition>>(StringComparer.Ordinal);
            foreach (var x in C.Quests.Values.OrderBy(x => x.Id, StringComparer.Ordinal))
                foreach (var pre in x.PrerequisiteQuestIds)
                {
                    if (!map.TryGetValue(pre, out var list)) map[pre] = list = new List<QuestDefinition>();
                    list.Add(x);
                }
            _unlockedBy = map;
        }
        if (!_unlockedBy.TryGetValue(q.Id, out var next)) return false;
        foreach (var x in next)
            if (x.RecommendedLevel + J.UnlockLevelsAhead >= ch.Level && x.MinLevel <= ch.Level + J.UnlockLevelsAhead) return true;
        return false;
    }

    /// <summary>Distancia pelas estradas entre dois pontos de atividade (-1: sem caminho). Conteudo fixo: em cache.</summary>
    private int Dist(string fromZone, string fromActivity, string toZone, string toActivity)
    {
        var key = (fromZone, fromActivity, toZone, toActivity);
        if (!_dist.TryGetValue(key, out int d))
        {
            d = _routes!.Plan(_routes.Layout.Spot(fromZone, fromActivity), fromZone, toZone, _routes.Layout.Spot(toZone, toActivity), 1).Distance;
            _dist[key] = d;
        }
        return d;
    }

    private static int Minutes(int distance, int speed) => distance < 0 ? 10_000 : distance == 0 ? 0 : (distance + speed - 1) / speed;

    /// <summary>
    /// Diaria (D-067): da para ir a etapa, faze-la e voltar ao quadro no tempo de jogo que a pessoa ainda tem
    /// antes do reset: o resto da sessao atual e as janelas livres da rotina ate o reset. Nao exige que caiba
    /// numa sessao so.
    /// </summary>
    internal bool FitsBeforeReset(Person p, Character ch, QuestDefinition q, int travel, long from = -1)
    {
        if (q.Category != QuestCategories.Daily) return true;
        long at = from < 0 ? Now : from;
        long work = 2L * travel;
        foreach (var s in q.Steps) work += s.Minutes;
        return work <= PlayableMinutesUntil(p, ch, NextResetTick(at), at);
    }

    /// <summary>
    /// Minutos em que a pessoa pode estar jogando de agora ate o instante dado: o resto da sessao atual e as
    /// janelas livres planejadas da rotina (as mesmas que limitam as sessoes e as voltas), sem contar duas vezes.
    /// </summary>
    internal int PlayableMinutesUntil(Person p, Character ch, long until, long from = -1)
    {
        long now = from < 0 ? Now : from;
        var spans = new List<(long Start, long End)>();
        if (p.IsOnline && ch.SessionEndTick > now) spans.Add((now, Math.Min(until, ch.SessionEndTick)));
        foreach (var (s, e, _) in p.OpenWindows)
        {
            long a = Math.Max(s, now), b = Math.Min(e, until);
            if (b > a) spans.Add((a, b));
        }
        spans.Sort((x, y) => x.Start != y.Start ? x.Start.CompareTo(y.Start) : x.End.CompareTo(y.End));
        long total = 0, curStart = 0, curEnd = long.MinValue;
        foreach (var (s, e) in spans)
        {
            if (s > curEnd) { if (curEnd > curStart) total += curEnd - curStart; curStart = s; curEnd = e; }
            else if (e > curEnd) curEnd = e;
        }
        if (curEnd > curStart) total += curEnd - curStart;
        return (int)(total / SimTime.TicksPerMinute);
    }

    /// <summary>Diaria que expirou intocada, no mesmo estado: so volta depois de a pessoa fazer a atividade da etapa ou ter meta de praticar o tema.</summary>
    private bool IdleBlocked(Character ch, QuestDefinition q) =>
        A.IdleExpiryBlock && ch.QuestLog.TryGetValue(q.Id, out var rec) && rec.IdleExpired && !PracticeGoalFor(ch, q);

    /// <summary>Aceita a oferta, largando a missao mais fraca se preciso (troca de prioridade). Devolve se aceitou.</summary>
    private bool Take(Person p, Character ch, QuestDefinition offer, string why)
    {
        if (CanAccept(ch)) { Accept(ch, offer); return true; }
        if (SwapTarget(p, ch, offer) is not { } weakest) return false;
        Drop(ch, weakest, "replaced:" + offer.Id, Value(p, ch, C.Quests[weakest.QuestId], weakest), Value(p, ch, offer, null));
        Accept(ch, offer);
        return true;
    }

    /// <summary>
    /// Valor de uma missao para a pessoa agora (permille; D-066). Nao e so idade ou nivel: o maior entre o
    /// rendimento no nivel e a historia (principal), mais o progresso feito, o que ela libera no nivel da
    /// pessoa e o vinculo com um objetivo ativo (a campanha, para as principais).
    /// </summary>
    public int Value(Person p, Character ch, QuestDefinition q, ActiveQuest? aq)
    {
        var j = J;
        // Aceite avaliado (D-067): o rendimento e o da recompensa no nivel (nao so o encaixe), e o gosto pelo tema conta.
        int yieldValue = A.Enabled ? RewardShare(ch, q)
            : ch.Level >= C.Progression.LevelCap ? 0 : ProgressionRules.Efficiency(C.Progression, ch.Level, q.RecommendedLevel);
        int story = Math.Max(q.Category == QuestCategories.Main ? j.MainStoryValue : 0, A.Enabled ? Taste(p.Preferences, q) : 0);
        int progress = 0;
        if (aq != null && q.Steps.Count > 0)
        {
            int need = aq.StepIndex < q.Steps.Count ? Math.Max(1, q.Steps[aq.StepIndex].Minutes) : 1;
            int partial = aq.StepIndex < q.Steps.Count ? Math.Min(1000, aq.StepMinutes * 1000 / need) : 0;
            progress = Math.Min(1000, (aq.StepIndex * 1000 + partial) / q.Steps.Count);
        }
        bool unlocks = Unlocks(ch, q);
        bool objective = q.Category == QuestCategories.Main && CampaignActive(p);
        return Math.Max(yieldValue, story) + progress * j.ProgressWeight / 1000 + (unlocks ? j.UnlockValue : 0) + (objective ? j.ObjectiveValue : 0);
    }

    /// <summary>
    /// Missao que a pessoa largaria para aceitar a oferta (troca de prioridade), ou null: a mais fraca entre
    /// as ativas que nao sao alvo de meta nem estao prontas para entregar, se a oferta valer mais pela margem.
    /// </summary>
    public ActiveQuest? SwapTarget(Person p, Character ch, QuestDefinition offer) => SwapTarget(p, ch, offer, ch.ActiveQuestList, null);

    /// <summary>Troca sobre uma lista de missoes (o plano da visita); as aceitas na mesma visita ficam de fora.</summary>
    private ActiveQuest? SwapTarget(Person p, Character ch, QuestDefinition offer, IReadOnlyList<ActiveQuest> active, HashSet<string>? exclude)
    {
        if (!J.Enabled || active.Count < C.QuestSettings.MaxActivePerCharacter) return null;
        ActiveQuest? weakest = null;
        int weakestValue = int.MaxValue;
        foreach (var aq in active)
        {
            if (!C.Quests.TryGetValue(aq.QuestId, out var q) || aq.StepIndex >= q.Steps.Count) continue;
            if (exclude != null && exclude.Contains(aq.QuestId)) continue;
            if (ch.Goals.Any(g => g.Type == GoalTypes.CompleteQuest && g.Target == aq.QuestId)) continue;
            int v = Value(p, ch, q, aq);
            if (v < weakestValue || (v == weakestValue && weakest != null && string.CompareOrdinal(aq.QuestId, weakest.QuestId) < 0)) { weakest = aq; weakestValue = v; }
        }
        if (weakest == null) return null;
        return Value(p, ch, offer, null) > weakestValue + J.SwapMarginPermille ? weakest : null;
    }

    /// <summary>A pessoa pode pegar a missao agora: ha vaga ou ha troca de prioridade que valha (D-066).</summary>
    public bool CanTake(Person p, Character ch, QuestDefinition q) => CanAccept(ch) || SwapTarget(p, ch, q) != null;

    /// <summary>Larga uma missao: registra, guarda ou perde o progresso conforme o conteudo, sem frustracao (troca de prioridade).</summary>
    internal void Drop(Character ch, ActiveQuest aq, string reason, int oldValue, int newValue)
    {
        ch.ActiveQuestList.Remove(aq);
        if (!ch.QuestLog.TryGetValue(aq.QuestId, out var rec)) ch.QuestLog[aq.QuestId] = rec = new QuestRecord();
        rec.Drops++;
        rec.DroppedDay = Today;
        if (C.QuestSettings.DropKeepsProgress) { rec.SavedStepIndex = aq.StepIndex; rec.SavedStepMinutes = aq.StepMinutes; }
        _w.StatsOf(aq.QuestId).Active--;
        _w.Emit("quest_dropped", EventImportance.Low, new[] { new EntityRef("character", ch.Id) },
            new[] { new EventCause(reason, 1000), new EventCause("value_dropped", oldValue), new EventCause("value_taken", newValue) }, aq.QuestId);
    }

    /// <summary>Reset diario do servidor (D-066): as diarias aceitas antes dele expiram.</summary>
    internal void ExpireDailies()
    {
        if (!J.Enabled) return;
        foreach (var ch in _w.Characters)
            foreach (var aq in ch.ActiveQuestList.ToList())
            {
                if (!C.Quests.TryGetValue(aq.QuestId, out var q) || q.Category != QuestCategories.Daily) continue;
                ch.ActiveQuestList.Remove(aq);
                bool idle = aq.StepIndex == 0 && aq.StepMinutes == 0;
                if (A.Enabled && A.IdleExpiryBlock && idle)
                {
                    if (!ch.QuestLog.TryGetValue(q.Id, out var rec)) ch.QuestLog[q.Id] = rec = new QuestRecord();
                    rec.IdleExpired = true;
                }
                _w.StatsOf(q.Id).Active--;
                _w.Emit("quest_expired", EventImportance.Low, new[] { new EntityRef("character", ch.Id) },
                    idle ? new[] { new EventCause("daily_reset", 1000), new EventCause("no_step_done", 1000) } : new[] { new EventCause("daily_reset", 1000) }, q.Id);
            }
    }

    /// <summary>
    /// Missao que combina com o gosto (regra do Independente): alguma etapa de atividade de um
    /// tema com preferencia de ao menos OwnPathMinPreference. Missao so de visita conta como neutra.
    /// </summary>
    public bool FitsTaste(PersonPreferences pr, QuestDefinition q)
    {
        int min = C.Personality.Rules.OwnPathMinPreference;
        bool any = false;
        foreach (var step in q.Steps)
        {
            if (!C.Regions.TryGetValue(step.RegionId, out var r) || r.FindActivity(step.ActivityId) is not { } a) continue;
            string? pref = a.Type switch
            {
                ActivityTypes.Hunt or ActivityTypes.Boss => "pve",
                ActivityTypes.Patrol or ActivityTypes.Ambush => "pvp",
                ActivityTypes.Gather => "gathering",
                ActivityTypes.Explore => "exploration",
                ActivityTypes.Trade => "trade",
                ActivityTypes.Socialize => "social",
                _ => null,
            };
            if (pref == null) continue;
            any = true;
            if (pr.Get(pref) >= min) return true;
        }
        return !any;
    }

    private void Accept(Character ch, QuestDefinition q, BoardAction? why = null)
    {
        var aq = new ActiveQuest(q.Id, Now, ch.PlayedTicksAt(Now));
        // Progresso guardado ao largar (se o conteudo manda guardar, D-066).
        if (ch.QuestLog.TryGetValue(q.Id, out var saved) && saved.SavedStepIndex >= 0)
        {
            aq.StepIndex = Math.Min(saved.SavedStepIndex, q.Steps.Count);
            aq.StepMinutes = saved.SavedStepMinutes;
            saved.SavedStepIndex = -1;
            saved.SavedStepMinutes = 0;
        }
        ch.ActiveQuestList.Add(aq);
        // Ler a missao informa o que existe nos lugares das etapas (pista de conteudo, D-055).
        foreach (var step in q.Steps)
            if (step.RegionId.Length > 0 && step.ActivityId.Length > 0 && !ch.Known.Contains(step.RegionId))
                ch.Hints.Add(step.RegionId + "/" + step.ActivityId);
        var s = _w.StatsOf(q.Id);
        s.Accepted++;
        s.Active++;
        var causes = new List<EventCause> { new("quest_board", 1000), new(q.Category, 1000) };
        // Motivo do aceite (D-067): meta, recompensa, campanha, liberar missao, meta de praticar ou gosto; e oportunidade.
        if (why != null && A.Enabled) causes.Add(new EventCause(why.Motive, why.Interest));
        if (why != null && why.OnTheWay) causes.Add(new EventCause("on_the_way", 1000));
        if (why != null && why.Why == "priority") causes.Add(new EventCause("priority_swap", 1000));
        _w.Emit("quest_accepted", EventImportance.Low, new[] { new EntityRef("character", ch.Id) }, causes.ToArray(), q.Id);
        Progressed?.Invoke(ch, q.Id);   // aceitar a missao da meta e progresso (D-065)
    }

    private void Deliver(Character ch, ActiveQuest aq, QuestDefinition q, QuestBoardDefinition board)
    {
        ch.ActiveQuestList.Remove(aq);
        if (!ch.QuestLog.TryGetValue(q.Id, out var rec)) ch.QuestLog[q.Id] = rec = new QuestRecord();
        var s = _w.StatsOf(q.Id);
        if (rec.Completions == 0) s.UniqueCompleters++;
        rec.Completions++;
        rec.LastCompletedDay = Today;
        if (q.Category == QuestCategories.Daily)
        {
            if (ch.DailyDay != Today) { ch.DailyByBoard.Clear(); ch.DailyDay = Today; }
            ch.DailyByBoard[board.Id] = DailyCount(ch, board.Id) + 1;
        }
        long xp = RewardXp(C, ch, q);
        s.Completed++;
        s.Active--;
        s.SumPlayedTicksToComplete += Math.Max(0, ch.PlayedTicksAt(Now) - aq.AcceptedPlayedTicks);
        s.SumLevelAtCompletion += ch.Level;
        s.LastCompletedTick = Now;
        s.XpGranted += xp;
        ch.XpFromQuests += xp;
        _w.Emit("quest_completed", EventImportance.Normal, new[] { new EntityRef("character", ch.Id) },
            new[] { new EventCause("quest_reward_xp", (int)Math.Min(int.MaxValue, xp)), new EventCause(q.Category, 1000) }, q.Id);
        if (xp > 0) _gainXp(ch, xp, "quest");
        Completed?.Invoke(ch, q.Id);
    }
}

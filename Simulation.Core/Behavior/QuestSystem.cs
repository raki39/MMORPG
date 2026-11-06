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

    public QuestSystem(WorldState world, Action<Character, long, string> gainXp)
    {
        _w = world;
        _gainXp = gainXp;
    }

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
        int yieldValue = ch.Level >= C.Progression.LevelCap ? 0 : ProgressionRules.Efficiency(C.Progression, ch.Level, q.RecommendedLevel);
        int story = q.Category == QuestCategories.Main ? j.MainStoryValue : 0;
        int progress = 0;
        if (aq != null && q.Steps.Count > 0)
        {
            int need = aq.StepIndex < q.Steps.Count ? Math.Max(1, q.Steps[aq.StepIndex].Minutes) : 1;
            int partial = aq.StepIndex < q.Steps.Count ? Math.Min(1000, aq.StepMinutes * 1000 / need) : 0;
            progress = Math.Min(1000, (aq.StepIndex * 1000 + partial) / q.Steps.Count);
        }
        bool unlocks = C.Quests.Values.Any(x => x.PrerequisiteQuestIds.Contains(q.Id, StringComparer.Ordinal)
            && x.RecommendedLevel + j.UnlockLevelsAhead >= ch.Level && x.MinLevel <= ch.Level + j.UnlockLevelsAhead);
        bool objective = q.Category == QuestCategories.Main
            && p.Objectives.Any(o => o.State == ObjectiveStates.Active && C.Objectives.TryGetValue(o.Id, out var od) && od.Criterion == ObjectiveCriteria.MainCampaign);
        return Math.Max(yieldValue, story) + progress * j.ProgressWeight / 1000 + (unlocks ? j.UnlockValue : 0) + (objective ? j.ObjectiveValue : 0);
    }

    /// <summary>
    /// Missao que a pessoa largaria para aceitar a oferta (troca de prioridade), ou null: a mais fraca entre
    /// as ativas que nao sao alvo de meta nem estao prontas para entregar, se a oferta valer mais pela margem.
    /// </summary>
    public ActiveQuest? SwapTarget(Person p, Character ch, QuestDefinition offer)
    {
        if (!J.Enabled || CanAccept(ch)) return null;
        ActiveQuest? weakest = null;
        int weakestValue = int.MaxValue;
        foreach (var aq in ch.ActiveQuestList)
        {
            if (!C.Quests.TryGetValue(aq.QuestId, out var q) || aq.StepIndex >= q.Steps.Count) continue;
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
                _w.StatsOf(q.Id).Active--;
                _w.Emit("quest_expired", EventImportance.Low, new[] { new EntityRef("character", ch.Id) },
                    new[] { new EventCause("daily_reset", 1000) }, q.Id);
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

    private void Accept(Character ch, QuestDefinition q)
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
        _w.Emit("quest_accepted", EventImportance.Low, new[] { new EntityRef("character", ch.Id) },
            new[] { new EventCause("quest_board", 1000), new EventCause(q.Category, 1000) }, q.Id);
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

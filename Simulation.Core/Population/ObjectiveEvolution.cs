using Simulation.Core.Content;
using Simulation.Core.Random;

namespace Simulation.Core.Population;

/// <summary>Acontecimento da evolucao dos objetivos, para o BehaviorSystem emitir (com causa) e aplicar o humor.</summary>
public sealed record ObjectiveEvent(string Type, string ObjectiveId, string Param, string Tier, string Reason, int Value);

/// <summary>Acontecimentos que pedem revisao dos objetivos no proximo dia jogado.</summary>
public static class ObjectiveTriggers
{
    public const string Completed = "objective_completed";
    public const string Abandoned = "objective_abandoned";
    public const string ConditionLost = "condition_lost";
    public const string MaxLevel = "max_level";
    /// <summary>A pessoa conheceu conteudo com requisito novo (zona com chefe ou PvP).</summary>
    public const string ContentKnown = "content_known";

    /// <summary>
    /// Gatilho de necessidade (concluiu, abandonou ou perdeu um objetivo; chegou ao nivel maximo): a pessoa
    /// procura um objetivo novo e, se nenhum for relevante, registra a falta. Conteudo conhecido e uma
    /// oportunidade: so traz o objetivo ligado ao que existe ali, e nao achar nada nao e falta.
    /// </summary>
    public static bool IsNeed(string trigger) => !trigger.StartsWith(ContentKnown, StringComparison.Ordinal);

    /// <summary>Gatilhos fortes substituem um "conteudo conhecido" pendente; os demais nao se sobrescrevem.</summary>
    public static void Set(Person p, string trigger)
    {
        if (p.ObjectiveTrigger.Length == 0 || (p.ObjectiveTrigger.StartsWith(ContentKnown, StringComparison.Ordinal) && !trigger.StartsWith(ContentKnown, StringComparison.Ordinal)))
            p.ObjectiveTrigger = trigger;
    }
}

/// <summary>
/// Evolucao dos objetivos pessoais (O3, D-064). Uma vez por dia jogado: mede o progresso, confere as
/// condicoes mantidas, promove um secundario quando nao ha geral ativo, baixa a importancia do que
/// podia avancar e parou, e, depois de um acontecimento, renova: adota um objetivo relevante (pela
/// ficha, pelos objetivos anteriores, pelas experiencias e pelo conteudo e requisitos conhecidos), sem
/// sequencia fixa e sem preencher vaga artificialmente; sem nenhum relevante, registra a falta.
/// Nao ha abandono por falhas de metas (D-065): nao conseguir realizar agora nao e motivo de desistir.
/// </summary>
public static class ObjectiveEvolution
{
    public const string RenewStream = "population.objectives.renew";

    /// <summary>Evolucao ligada e objetivos ligados (sem o sorteio, o sistema de objetivos fica desligado).</summary>
    public static bool Enabled(ContentDatabase c) => c.ObjectiveCatalog.Evolution.Enabled && c.ObjectiveCatalog.Generation.Enabled;

    public static IEnumerable<PersonObjective> Active(Person p) => p.ObjectiveList.Where(o => o.State == ObjectiveStates.Active);

    public static bool IsFuture(ContentDatabase c, PersonObjective o) =>
        c.Objectives.TryGetValue(o.Id, out var d) && ObjectiveCriteria.RequiredSystems(d.Criterion).Any(s => !ObjectiveSystems.Exists(s));

    /// <summary>A pessoa pode fazer uma mudanca escolhida hoje (intervalo entre mudancas).</summary>
    public static bool CanChange(ContentDatabase c, Person p) =>
        p.LastObjectiveChangePlayDay < 0 || p.ObjectivePlayDays - p.LastObjectiveChangePlayDay >= c.ObjectiveCatalog.Evolution.ChangeCooldownPlayDays;

    /// <summary>
    /// Termo de experiencia (permille): memorias marcantes boas aproximam e ruins afastam, nos tipos de
    /// atividade do objetivo (conteudo). Patrulha, emboscada e chefe nao contam (sem combate real).
    /// </summary>
    public static int ExperienceTerm(ContentDatabase c, Person p, ObjectiveDefinition d)
    {
        var ev = c.ObjectiveCatalog.Evolution;
        if (d.ExperienceTypes.Count == 0) return 1000;
        int good = 0, bad = 0;
        foreach (var m in p.MemoryList)
        {
            if (m.Kind is not ("great_activity" or "awful_activity")) continue;
            int slash = m.Subject.IndexOf('/');
            if (slash <= 0 || !c.Regions.TryGetValue(m.Subject[..slash], out var z) || z.FindActivity(m.Subject[(slash + 1)..]) is not { } a) continue;
            if (!d.ExperienceTypes.Contains(a.Type, StringComparer.Ordinal)) continue;
            if (m.Kind == "great_activity") good++; else bad++;
        }
        int cap = Math.Max(1, ev.ExperienceCap);
        return 1000 + ev.ExperienceBoostPermille * (Math.Min(good, cap) - Math.Min(bad, cap)) / cap;
    }

    /// <summary>
    /// Avaliacao diaria (meia-noite local, so em dia jogado). Devolve os acontecimentos para emitir.
    /// Desligada (evolution.enabled = false): nada muda.
    /// </summary>
    public static List<ObjectiveEvent> OnDaily(ContentDatabase c, Person p, Character ch, long day, bool playedYesterday, ulong seed)
    {
        var ev = c.ObjectiveCatalog.Evolution;
        var events = new List<ObjectiveEvent>();
        if (!Enabled(c) || !playedYesterday) return events;
        p.ObjectivePlayDays++;
        int today = p.ObjectivePlayDays;

        // 1. Progresso dos que podem avancar (base da estagnacao). Sistema futuro: nunca estagna.
        foreach (var o in Active(p))
        {
            var a = ObjectiveSystem.Assess(c, ch, o);
            if (!a.Measurable || a.MissingSystems.Count > 0) continue;
            if (a.Progress != o.LastProgress) { o.LastProgress = a.Progress; o.LastProgressPlayDay = today; }
        }

        // 2. Condicoes mantidas: perder nao apaga a conquista passada (consequencia, sem intervalo).
        foreach (var o in p.ObjectiveList.Where(o => o.State == ObjectiveStates.Held).ToList())
        {
            if (ObjectiveSystem.Assess(c, ch, o).CriterionMet) continue;
            o.State = ObjectiveStates.Lost;
            o.TimesLost++;
            o.EndedDay = day;
            o.EndedPlayDay = today;
            o.EndReason = "condition_lost";
            p.ObjectiveHistoryList.Add(new ObjectiveChange(day, o.Id, o.Param, "lost", o.Tier, "condition_lost"));
            events.Add(new ObjectiveEvent("objective_lost", o.Id, o.Param, o.Tier, "condition_lost", o.Importance));
            ObjectiveTriggers.Set(p, ObjectiveTriggers.ConditionLost + ":" + o.Id);
        }

        // 2b. Desistencia so por motivo especifico (D-066): interesse em queda sustentada ou experiencia negativa
        // com ligacao concreta. Primeiro deixa de lado; sem melhora, abandona; se o motivo passar, retoma.
        InterestAndExperience(c, p, day, today, events);

        // 3. Sem geral ativo: o secundario mais importante vira geral (consequencia, sem intervalo).
        if (!Active(p).Any(o => o.Tier == ObjectiveTiers.General)
            && Active(p).OrderByDescending(o => o.Importance).ThenBy(o => o.Id, StringComparer.Ordinal).FirstOrDefault() is { } top)
        {
            top.Tier = ObjectiveTiers.General;
            p.ObjectiveHistoryList.Add(new ObjectiveChange(day, top.Id, top.Param, "promoted", top.Tier, "no_general"));
            events.Add(new ObjectiveEvent("objective_priority", top.Id, top.Param, top.Tier, "no_general", top.Importance));
        }

        bool canChange = CanChange(c, p);
        // 4. Estagnacao: podia avancar e nao avancou por StalePlayDays dias jogados -> menos importancia.
        if (canChange)
        {
            var stale = Active(p).Where(o => o.LastProgressPlayDay >= 0 && today - o.LastProgressPlayDay >= ev.StalePlayDays && !IsFuture(c, o))
                .OrderBy(o => o.LastProgressPlayDay).ThenBy(o => o.Id, StringComparer.Ordinal).FirstOrDefault();
            if (stale != null)
            {
                stale.Importance = Math.Max(50, stale.Importance - ev.StaleImportanceDrop);
                stale.LastProgressPlayDay = today;
                p.ObjectiveHistoryList.Add(new ObjectiveChange(day, stale.Id, stale.Param, "importance_down", stale.Tier, "no_progress"));
                events.Add(new ObjectiveEvent("objective_priority", stale.Id, stale.Param, stale.Tier, "no_progress", stale.Importance));
                Retier(p, day, events, "no_progress");
                p.LastObjectiveChangePlayDay = today;
                canChange = false;
            }
        }

        // 5. Renovacao depois de um acontecimento.
        if (canChange && p.ObjectiveTrigger.Length > 0)
        {
            string trigger = p.ObjectiveTrigger;
            p.ObjectiveTrigger = "";
            var rng = new DeterministicRng(RngStreams.DeriveSeed(seed, RenewStream, p.Id * 100_000L + today));
            Renew(c, p, ch, day, trigger, rng, events);
        }
        return events;
    }

    /// <summary>Queda exigida no ajuste para contar como perda de interesse: maior para objetivos mais importantes.</summary>
    public static int RequiredDrop(ObjectiveEvolutionDefinition ev, PersonObjective o) =>
        ev.InterestDropPermille * (500 + Math.Clamp(o.Importance, 0, 1000)) / 1000;

    /// <summary>Interesse baixo agora: ajuste abaixo do piso e caiu, desde a adocao, pelo menos a queda exigida.</summary>
    public static bool LowInterest(ContentDatabase c, Person p, PersonObjective o)
    {
        var ev = c.ObjectiveCatalog.Evolution;
        if (!c.Objectives.TryGetValue(o.Id, out var d)) return false;
        int fit = ObjectiveSystem.Fit(c, p, d);
        return fit < ev.InterestFloorPermille && o.FitAtSet - fit >= RequiredDrop(ev, o);
    }

    /// <summary>Memorias ruins marcantes distintas com ligacao concreta com o objetivo (negativeLink do conteudo).</summary>
    public static int LinkedNegativeMemories(ContentDatabase c, Person p, PersonObjective o)
    {
        if (!c.Objectives.TryGetValue(o.Id, out var d)) return 0;
        var link = d.NegativeLink;
        if (link.ActivityTypes.Count == 0 && !link.MainQuestSteps) return 0;
        var mainSteps = link.MainQuestSteps
            ? c.Quests.Values.Where(q => q.Category == QuestCategories.Main).SelectMany(q => q.Steps).Select(s => s.RegionId + "/" + s.ActivityId).ToHashSet(StringComparer.Ordinal)
            : null;
        var subjects = new HashSet<string>(StringComparer.Ordinal);
        foreach (var m in p.MemoryList)
        {
            if (m.Kind != "awful_activity") continue;
            int slash = m.Subject.IndexOf('/');
            if (slash <= 0 || !c.Regions.TryGetValue(m.Subject[..slash], out var z) || z.FindActivity(m.Subject[(slash + 1)..]) is not { } a) continue;
            if (link.ActivityTypes.Contains(a.Type, StringComparer.Ordinal) || (mainSteps != null && mainSteps.Contains(m.Subject))) subjects.Add(m.Subject);
        }
        return subjects.Count;
    }

    private static void InterestAndExperience(ContentDatabase c, Person p, long day, int today, List<ObjectiveEvent> events)
    {
        var ev = c.ObjectiveCatalog.Evolution;
        if (!ev.InterestEnabled && !ev.NegativeEnabled) return;
        bool changed = false;
        foreach (var o in p.ObjectiveList.OrderBy(o => o.Id, StringComparer.Ordinal).ToList())
        {
            bool low = ev.InterestEnabled && LowInterest(c, p, o);
            bool negative = ev.NegativeEnabled && LinkedNegativeMemories(c, p, o) >= ev.NegativeMinMemories;
            if (o.State == ObjectiveStates.Active)
            {
                o.LowInterestDays = low ? o.LowInterestDays + 1 : 0;
                o.NegativeDays = negative ? o.NegativeDays + 1 : 0;
                if (changed) continue;
                string? why = o.NegativeDays >= ev.NegativeConfirmPlayDays ? "negative_experience"
                    : o.LowInterestDays >= ev.InterestConfirmPlayDays ? "interest_low" : null;
                if (why == null) continue;
                // Deixa de lado (sem frustracao); a vaga abre para outro objetivo.
                o.State = ObjectiveStates.Paused;
                o.EndedDay = day;
                o.EndedPlayDay = today;
                o.EndReason = why;
                o.SetAsideDays = 0;
                o.RecoverDays = 0;
                p.ObjectiveHistoryList.Add(new ObjectiveChange(day, o.Id, o.Param, "paused", o.Tier, why));
                events.Add(new ObjectiveEvent("objective_paused", o.Id, o.Param, o.Tier, why, o.Importance));
                ObjectiveTriggers.Set(p, "objective_set_aside:" + o.Id);
                changed = true;
            }
            else if (o.State == ObjectiveStates.Paused && o.EndReason is "interest_low" or "negative_experience")
            {
                bool still = o.EndReason == "interest_low" ? low : negative;
                bool recovered = o.EndReason == "interest_low"
                    ? c.Objectives.TryGetValue(o.Id, out var d) && ObjectiveSystem.Fit(c, p, d) >= ev.InterestFloorPermille + ev.InterestRecoverMarginPermille
                    : !negative;
                o.SetAsideDays = still ? o.SetAsideDays + 1 : 0;
                o.RecoverDays = recovered ? o.RecoverDays + 1 : 0;
                if (changed) continue;
                if (o.SetAsideDays >= ev.AbandonAfterSetAsidePlayDays)
                {
                    string why = o.EndReason;
                    o.State = ObjectiveStates.Abandoned;
                    o.EndedDay = day;
                    o.EndedPlayDay = today;
                    p.ObjectiveHistoryList.Add(new ObjectiveChange(day, o.Id, o.Param, "abandoned", o.Tier, why));
                    events.Add(new ObjectiveEvent("objective_abandoned", o.Id, o.Param, o.Tier, why, o.Importance));
                    changed = true;
                }
                else if (o.RecoverDays >= (o.EndReason == "interest_low" ? ev.InterestConfirmPlayDays : ev.NegativeConfirmPlayDays)
                         && Active(p).Count(x => x.Tier == o.Tier) < (o.Tier == ObjectiveTiers.General ? ev.MaxGeneral : ev.MaxSecondary))
                {
                    string why = o.EndReason == "interest_low" ? "interest_back" : "experience_faded";
                    o.State = ObjectiveStates.Active;
                    o.EndedDay = -1;
                    o.EndedPlayDay = -1;
                    o.EndReason = "";
                    o.LowInterestDays = o.NegativeDays = o.SetAsideDays = o.RecoverDays = 0;
                    p.ObjectiveHistoryList.Add(new ObjectiveChange(day, o.Id, o.Param, "resumed", o.Tier, why));
                    events.Add(new ObjectiveEvent("objective_set", o.Id, o.Param, o.Tier, why, o.Importance));
                    changed = true;
                }
            }
        }
    }

    /// <summary>Geral menos importante que um secundario troca de lugar com ele (mudanca de prioridade justificada).</summary>
    private static void Retier(Person p, long day, List<ObjectiveEvent> events, string reason)
    {
        var generals = Active(p).Where(o => o.Tier == ObjectiveTiers.General).ToList();
        var secondaries = Active(p).Where(o => o.Tier == ObjectiveTiers.Secondary).ToList();
        if (generals.Count == 0 || secondaries.Count == 0) return;
        var weakest = generals.OrderBy(o => o.Importance).ThenBy(o => o.Id, StringComparer.Ordinal).First();
        var strongest = secondaries.OrderByDescending(o => o.Importance).ThenBy(o => o.Id, StringComparer.Ordinal).First();
        if (strongest.Importance <= weakest.Importance) return;
        weakest.Tier = ObjectiveTiers.Secondary;
        strongest.Tier = ObjectiveTiers.General;
        p.ObjectiveHistoryList.Add(new ObjectiveChange(day, weakest.Id, weakest.Param, "demoted", weakest.Tier, reason));
        p.ObjectiveHistoryList.Add(new ObjectiveChange(day, strongest.Id, strongest.Param, "promoted", strongest.Tier, reason));
        events.Add(new ObjectiveEvent("objective_priority", strongest.Id, strongest.Param, strongest.Tier, reason, strongest.Importance));
    }

    /// <summary>Candidata da renovacao: objetivo, entrada existente (pausado, perdido, abandonado), parametro e pesos.</summary>
    public readonly record struct Candidate(ObjectiveDefinition Def, PersonObjective? Existing, string Param, int Fit, int Weight, string Related, string Viability);

    /// <summary>
    /// Candidatas relevantes agora. Fora: os ativos, os mantidos e as conquistas concluidas (ficam
    /// concluidas); abandonados antes do intervalo; os ja cumpridos; sem parametro possivel; e os sem
    /// interesse real (ajuste abaixo do minimo, mais alto para os de sistema futuro). O peso considera a
    /// viabilidade (pode avancar, preparacao conhecida, sistema futuro), o reforco do maior objetivo
    /// relacionado e a recuperacao de uma condicao perdida.
    /// </summary>
    public static List<Candidate> Candidates(ContentDatabase c, Person p, Character ch, DeterministicRng rng)
    {
        var ev = c.ObjectiveCatalog.Evolution;
        var g = c.ObjectiveCatalog.Generation;
        var list = new List<Candidate>();
        int today = p.ObjectivePlayDays;
        foreach (var def in c.ObjectivesOrdered)
        {
            if (def.Weight <= 0) continue;
            var existing = p.ObjectiveList.FirstOrDefault(o => o.Id == def.Id);
            if (existing != null)
            {
                if (existing.State is ObjectiveStates.Active or ObjectiveStates.Held or ObjectiveStates.Completed) continue;
                if (existing.State == ObjectiveStates.Abandoned && today - existing.EndedPlayDay < ev.AbandonCooldownPlayDays) continue;
                // Deixado de lado por interesse ou experiencia (D-066): so volta pela propria regra (o motivo passar), nao pela renovacao.
                if (existing.State == ObjectiveStates.Paused && existing.EndReason is "interest_low" or "negative_experience") continue;
            }
            string param;
            if (existing != null) param = existing.Param;
            else
            {
                var options = ObjectiveSystem.ParamOptions(c, def, ch.ClassId);
                if (options.Count == 0) continue;
                param = options[rng.NextInt(options.Count)];
            }
            var probe = new PersonObjective(def.Id, param, ObjectiveTiers.Secondary, 500, 500, "", 0, Array.Empty<(string, int)>());
            var a = ObjectiveSystem.Assess(c, ch, probe);
            if (a.CriterionMet) continue;
            bool future = a.MissingSystems.Count > 0;
            bool prepKnown = a.PreparationLevel > 0 && a.PreparationKnown;
            int fit = (int)Math.Clamp((long)ObjectiveSystem.Fit(c, p, def) * ExperienceTerm(c, p, def) / 1000, g.MinMultiplierPermille, g.MaxMultiplierPermille);
            if (fit < (future ? ev.FutureMinFitPermille : ev.MinFitPermille)) continue;
            int factor = !future
                ? (a.Viability == ObjectiveViability.Now ? 1000 : prepKnown ? ev.PreparationFactorPermille : ev.FutureFactorPermille)
                : (prepKnown ? ev.PreparationFactorPermille : ev.FutureFactorPermille);
            // Relacionado ("opens"): de um objetivo alcancado (concluido ou mantido), sempre; de um ainda ativo,
            // so se ele e de sistema futuro e o novo tem preparacao concreta ou pode avancar (D-064). Vale um reforco so.
            string related = "";
            foreach (var src in p.ObjectiveList)
            {
                if (src.Id == def.Id || !c.Objectives.TryGetValue(src.Id, out var sd) || !sd.Opens.Contains(def.Id, StringComparer.Ordinal)) continue;
                bool achieved = src.State is ObjectiveStates.Completed or ObjectiveStates.Held;
                bool fromFuture = src.State == ObjectiveStates.Active && IsFuture(c, src) && (a.Viability == ObjectiveViability.Now || prepKnown);
                if (!achieved && !fromFuture) continue;
                related = src.Id;
                break;
            }
            long w = (long)def.Weight * fit / 1000 * factor / 1000;
            if (related.Length > 0) w = w * (1000 + ev.RelatedBonusPermille) / 1000;
            if (existing?.State == ObjectiveStates.Lost) w = w * (1000 + ev.RecoverBonusPermille) / 1000;
            if (w <= 0) continue;
            list.Add(new Candidate(def, existing, param, fit, (int)Math.Min(1_000_000, w), related, a.Viability));
        }
        return list;
    }

    private static void Renew(ContentDatabase c, Person p, Character ch, long day, string trigger, DeterministicRng rng, List<ObjectiveEvent> events)
    {
        var ev = c.ObjectiveCatalog.Evolution;
        var g = c.ObjectiveCatalog.Generation;
        var cands = Candidates(c, p, ch, rng);
        bool need = ObjectiveTriggers.IsNeed(trigger);
        if (!need)
        {
            // Oportunidade: so o objetivo que o conteudo da zona torna concreto (chefe -> desafios PvE; PvP -> competicao).
            string zone = trigger.Length > ObjectiveTriggers.ContentKnown.Length + 1 ? trigger[(ObjectiveTriggers.ContentKnown.Length + 1)..] : "";
            var types = c.Regions.TryGetValue(zone, out var z) ? z.AllActivities().Select(a => a.Type).ToHashSet(StringComparer.Ordinal) : new HashSet<string>();
            cands = cands.Where(x => (x.Def.Criterion == ObjectiveCriteria.DefeatBosses && types.Contains(ActivityTypes.Boss))
                || (x.Def.Criterion == ObjectiveCriteria.PvpRanking && (types.Contains(ActivityTypes.Patrol) || types.Contains(ActivityTypes.Ambush)))).ToList();
            if (cands.Count == 0) return;   // nada ligado a essa oportunidade: nao e falta de conteudo
        }
        if (cands.Count == 0)
        {
            // Sem objetivo relevante: registrado, nao preenchido. Falta de conteudo e um problema do MMORPG.
            p.ObjectiveGaps++;
            p.LastObjectiveGapDay = day;
            p.ObjectiveHistoryList.Add(new ObjectiveChange(day, "", "", "gap", "", trigger));
            events.Add(new ObjectiveEvent("objective_gap", "", "", "", trigger, Active(p).Count()));
            return;
        }
        var pick = cands[rng.PickWeighted(cands.Select(x => x.Weight).ToList())];
        int importance = Math.Clamp(500 + (pick.Fit - 1000) * g.ImportanceSlopePermille / 1000 + rng.NextCenteredNoise(g.ImportanceNoise), 50, 1000);

        var generals = Active(p).Where(o => o.Tier == ObjectiveTiers.General).ToList();
        var secondaries = Active(p).Where(o => o.Tier == ObjectiveTiers.Secondary).ToList();
        string tier;
        if (generals.Count == 0 || (generals.Count < ev.MaxGeneral && importance > generals.Min(o => o.Importance))) tier = ObjectiveTiers.General;
        else if (secondaries.Count < ev.MaxSecondary) tier = ObjectiveTiers.Secondary;
        else
        {
            // Vagas cheias: substituicao voluntaria do secundario menos importante (sem frustracao), se valer a pena.
            var weakest = secondaries.OrderBy(o => o.Importance).ThenBy(o => o.Id, StringComparer.Ordinal).First();
            if (importance <= weakest.Importance + ev.SubstituteMarginPermille) return;
            weakest.State = ObjectiveStates.Paused;
            weakest.EndedDay = day;
            weakest.EndedPlayDay = p.ObjectivePlayDays;
            weakest.EndReason = "replaced:" + pick.Def.Id;
            p.ObjectiveHistoryList.Add(new ObjectiveChange(day, weakest.Id, weakest.Param, "paused", weakest.Tier, "replaced:" + pick.Def.Id));
            events.Add(new ObjectiveEvent("objective_paused", weakest.Id, weakest.Param, weakest.Tier, "replaced:" + pick.Def.Id, weakest.Importance));
            tier = ObjectiveTiers.Secondary;
        }

        int commitment = ObjectiveSystem.CommitmentOf(g, p.Traits);
        string reason = trigger + (pick.Related.Length > 0 ? "|related:" + pick.Related : "");
        string change;
        if (pick.Existing is { } e)
        {
            change = e.State switch { ObjectiveStates.Lost => "recovered", ObjectiveStates.Paused => "resumed", _ => "readopted" };
            e.State = ObjectiveStates.Active;
            e.Tier = tier;
            e.Importance = importance;
            e.Commitment = commitment;
            e.EndedDay = -1;
            e.EndedPlayDay = -1;
            e.EndReason = "";
            e.FailureStreak = 0;
            e.LastProgress = -1;
            e.LastProgressPlayDay = -1;
            e.FitAtSet = ObjectiveSystem.Fit(c, p, pick.Def);
            e.LowInterestDays = e.NegativeDays = e.SetAsideDays = e.RecoverDays = 0;
        }
        else
        {
            change = "set";
            var motives = ObjectiveSystem.Terms(c, p, pick.Def).Where(t => t.Term != 1000)
                .OrderByDescending(t => Math.Abs(t.Term - 1000)).ThenBy(t => t.Key, StringComparer.Ordinal).Take(3)
                .Select(t => (t.Key, t.Term - 1000)).ToList();
            int exp = ExperienceTerm(c, p, pick.Def);
            if (exp != 1000) motives.Add(("experience", exp - 1000));
            p.ObjectiveList.Add(new PersonObjective(pick.Def.Id, pick.Param, tier, importance, commitment, trigger, day, motives) { FitAtSet = ObjectiveSystem.Fit(c, p, pick.Def) });
        }
        p.ObjectiveHistoryList.Add(new ObjectiveChange(day, pick.Def.Id, pick.Param, change, tier, reason));
        events.Add(new ObjectiveEvent("objective_set", pick.Def.Id, pick.Param, tier, reason, importance));
        p.LastObjectiveChangePlayDay = p.ObjectivePlayDays;
    }

}

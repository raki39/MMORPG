using Simulation.Core.Content;
using Simulation.Core.Events;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Core.Behavior;

/// <summary>
/// Metas de curto prazo (D-026/D-036; as ambicoes de longo prazo sao os objetivos pessoais, D-062: a meta
/// que contribui para um deles ganha peso na adocao e comprometimento, O2). Ao conectar, a pessoa pode adotar um objetivo escolhido
/// pelos tracos (que vem do perfil e mudam com a experiencia): chegar a um nivel, descobrir uma
/// zona, concluir uma missao ou praticar a atividade preferida. O objetivo vira um fator a mais
/// na decisao, com o peso do comprometimento; nao obriga. Sem progresso, ou por impaciencia,
/// e abandonado. Concluido, reforca o traco que o motivou.
/// </summary>
public sealed class GoalSystem
{
    private readonly WorldState _w;
    private readonly QuestSystem _quests;
    private readonly TraitDynamics _traits;

    public GoalSystem(WorldState world, QuestSystem quests, TraitDynamics traits)
    {
        _w = world;
        _quests = quests;
        _traits = traits;
    }

    private ContentDatabase C => _w.Content;
    private GoalSettingsDefinition S => _w.Content.GoalSettings;
    private long Now => _w.Clock.Tick;

    /// <summary>Apego a meta (persistencia): paciencia e lealdade.</summary>
    public static int CommitmentOf(PersonTraits t) => 400 + t.Patience * 3 / 10 + t.Loyalty * 3 / 10;

    // ---------------- Ciclo ao conectar ----------------

    /// <summary>Ao conectar: abandona o que nao anda, e talvez adota um objetivo novo.</summary>
    internal void Review(Person p, Character ch, DeterministicRng rng)
    {
        var t = p.Traits;
        long played = ch.PlayedTicksAt(Now);
        long staleTicks = (S.StaleBaseHours * 1000L + S.StaleExtraHoursAtMaxPatience * (long)t.Patience) * SimTime.TicksPerHour / 1000;
        // Persistente (P4c): demora mais a abandonar metas (por falta de progresso e por impaciencia).
        bool persistent = QuirkSystem.Active(C, p, QuirkIds.Persistente);
        int patience = persistent ? C.QuirkCatalog.Effects.Persistente.GoalPatiencePermille : 1000;
        long baseStale = staleTicks;
        staleTicks = staleTicks * patience / 1000;
        // Alvos largados nesta revisao: nao voltam na mesma decisao (D-065; as de nivel ja tem bloqueio proprio, D-055).
        var droppedNow = new HashSet<string>(StringComparer.Ordinal);
        foreach (var g in ch.GoalList.ToList())
        {
            if (played - g.LastProgressPlayedTicks > staleTicks) { Abandon(ch, g, "no_progress"); droppedNow.Add(g.Type + "|" + g.Target); }
            // Leal (P4a): nao abandona meta por impaciencia.
            else if (C.PersonalityRules(p.PersonalityId)?.Rule != PersonalityRuleIds.SteadfastGoals
                     && t.Patience < S.ImpatientBelow && rng.ChancePermille(S.ImpatientDropChance * 1000 / patience)) { Abandon(ch, g, "impatience"); droppedNow.Add(g.Type + "|" + g.Target); }
            else if (persistent && played - g.LastProgressPlayedTicks > baseStale)
            {
                QuirkSystem.Count(p, "persistente_manteve_meta");
                g.SurvivedByEffect = true;   // P4d: se concluir, foi pelo efeito, nao conta como evidencia
            }
        }
        // Quantidade de metas e chance de adotar nao dependem da paciencia (ela so pesa na persistencia).
        if (ch.GoalList.Count >= S.MaxActive) return;
        if (!rng.ChancePermille(S.AdoptChance)) return;
        var options = Candidates(p, ch, rng);
        if (droppedNow.Count > 0) options = options.Where(o => !droppedNow.Contains(o.Type + "|" + o.Target)).ToList();
        if (options.Count == 0) return;
        // Objetivos pessoais (O2): a meta que contribui de verdade para um objetivo ganha peso na adocao
        // (so o maior reforco entre os objetivos atendidos). Metas sem vinculo continuam como antes.
        var links = options.Select(o => ObjectiveSystem.Links(C, p, ch, o.Type, o.Target)).ToList();
        var weights = options.Select((o, i) => ObjectiveSystem.AdoptionWeight(C, p, o.Weight, links[i])).ToList();
        int k = rng.PickWeighted(weights);
        var pick = options[k];
        var pickLinks = links[k];
        int commitment = ObjectiveSystem.LinkedCommitment(C, p, CommitmentOf(t), pickLinks);
        var g2 = new PersonalGoal(pick.Type, pick.Target, pick.TargetValue, pick.StartValue, commitment,
            pick.MotiveKey, p.MotiveValue(pick.MotiveKey), Now, played);
        g2.LinkList.AddRange(pickLinks);
        ch.GoalList.Add(g2);
        ch.GoalsSet++;
        ch.GoalsSetByTypeMap[pick.Type] = ch.GoalsSetByTypeMap.GetValueOrDefault(pick.Type) + 1;
        var causes = new List<EventCause> { new(pick.MotiveKey, p.MotiveValue(pick.MotiveKey)), new("commitment", g2.Commitment) };
        if (pickLinks.Count > 0)
        {
            ch.GoalsLinkedSet++;
            if (pickLinks.Any(l => l.Kind == ObjectiveLinkKinds.Preparation)) ch.GoalsPreparationSet++;
            foreach (var l in pickLinks)
            {
                if (p.ObjectiveList.FirstOrDefault(o => o.Id == l.ObjectiveId) is not { } o) continue;
                o.LinkedGoalsSet++;
                causes.Add(new EventCause("objective:" + l.ObjectiveId + ":" + l.Kind, o.Importance));
            }
        }
        _w.Emit("goal_set", EventImportance.Low, new[] { new EntityRef("character", ch.Id) }, causes.ToArray(),
            $"{pick.Type}|{pick.Target}|{pick.TargetValue}");
    }

    private readonly record struct Candidate(string Type, string Target, int TargetValue, int StartValue, int Weight, string MotiveKey);

    /// <summary>Candidatas e pesos efetivos (com o reforco dos objetivos), para testes e diagnostico.</summary>
    internal List<(string Type, string Target, int Weight, int Effective, IReadOnlyList<ObjectiveLink> Links)> CandidateWeights(Person p, Character ch, DeterministicRng rng) =>
        Candidates(p, ch, rng).Select(o =>
        {
            var links = ObjectiveSystem.Links(C, p, ch, o.Type, o.Target);
            return (o.Type, o.Target, o.Weight, ObjectiveSystem.AdoptionWeight(C, p, o.Weight, links), (IReadOnlyList<ObjectiveLink>)links);
        }).ToList();

    /// <summary>
    /// Objetivos possiveis agora (um por tipo), com o peso vindo das preferencias (gosto), da
    /// competitividade e da personalidade. Motivos de preferencia usam a chave "pref_x".
    /// </summary>
    private List<Candidate> Candidates(Person p, Character ch, DeterministicRng rng)
    {
        var t = p.Traits;
        var pr = p.Preferences;
        var list = new List<Candidate>();
        bool Has(string type) => ch.GoalList.Any(g => g.Type == type);

        // Chegar a um nivel: quem prefere PvE, os competitivos e os impacientes por progresso.
        if (!Has(GoalTypes.ReachLevel) && ch.Level < C.Progression.LevelCap)
        {
            int k = 1 + (pr.Pve + t.Competitiveness) / 700;
            int target = Math.Min(C.Progression.LevelCap, ch.Level + k);
            string motive = pr.Pve >= t.Competitiveness ? "pref_pve" : "competitiveness";
            if (!IsBlocked(ch, GoalTypes.ReachLevel, "", target))
                list.Add(new(GoalTypes.ReachLevel, "", target, ch.Level, 150 + pr.Pve * 3 / 10 + t.Competitiveness * 2 / 10 + (1000 - t.Patience) / 10, motive));
        }

        // Descobrir uma zona vizinha das conhecidas: quem prefere explorar (os competitivos aceitam zonas mais altas).
        if (!Has(GoalTypes.DiscoverZone))
        {
            int maxLevel = ch.Level + 2 + t.Competitiveness / 250;
            var zones = C.Regions.Values
                .Where(z => !ch.Known.Contains(z.Id) && (z.IsCity || z.LevelMin <= maxLevel))
                .Where(z => ch.Known.Any(k => C.Regions[k].Connections.Any(c => c.To == z.Id) || z.Connections.Any(c => c.To == k)))
                .OrderBy(z => z.Id, StringComparer.Ordinal).ToList();
            if (zones.Count > 0)
            {
                var z = zones[rng.NextInt(zones.Count)];
                list.Add(new(GoalTypes.DiscoverZone, z.Id, 1, 0, 100 + pr.Exploration * 9 / 10, "pref_exploration"));
            }
        }

        // Concluir uma missao principal: pacientes, quem conhece o jogo e gosta de historia (PvE + exploracao).
        if (!Has(GoalTypes.CompleteQuest) && MainQuestTarget(ch) is { } quest
            && (C.PersonalityRules(p.PersonalityId)?.Rule != PersonalityRuleIds.OwnPath || _quests.FitsTaste(pr, quest)))
        {
            int w = 120 + (pr.Pve + pr.Exploration) / 5 + t.Patience / 5 + t.GameKnowledge / 10;
            string motive = t.Patience >= t.GameKnowledge ? "patience" : "gameKnowledge";
            list.Add(new(GoalTypes.CompleteQuest, quest.Id, 1, 0, w, motive));
        }

        // Descoberta orientada pelo conteudo (D-055): zona da fronteira com pista do que a pessoa gosta.
        if (!Has(GoalTypes.DiscoverZone))
        {
            int maxLevel = ch.Level + 2 + t.Competitiveness / 250;
            foreach (var (zone, w, motive) in ContentDiscoveryOptions(p, ch, C, S, maxLevel))
                list.Add(new(GoalTypes.DiscoverZone, zone, 1, 0, w, motive));
        }

        // Desbloqueio (D-055): nivel que libera uma atividade conhecida de um tema que a pessoa prefere.
        if (!Has(GoalTypes.UnlockLevel))
            foreach (var (target, level, w, motive) in UnlockOptions(p, ch, C, S))
                list.Add(new(GoalTypes.UnlockLevel, target, level, ch.Level, w, motive));

        // Praticar a atividade preferida (pela preferencia); meta maior para os pacientes.
        if (!Has(GoalTypes.PracticeActivity))
        {
            var types = ch.Known.SelectMany(k => C.Regions[k].AllActivities())
                .Where(a => a.MinLevel <= ch.Level && a.MinGroupSize <= 1 && AffinityTraitFor(a.Type) != null)
                .Select(a => a.Type).Distinct().OrderBy(x => x, StringComparer.Ordinal).ToList();
            if (types.Count > 0)
            {
                string best = types.OrderByDescending(x => ActivityScoring.Affinity(x, pr, t)).ThenBy(x => x, StringComparer.Ordinal).First();
                string motive = AffinityTraitFor(best)!;
                int minutes = 120 + t.Patience * 3 / 10;
                list.Add(new(GoalTypes.PracticeActivity, best, minutes, 0, 100 + p.MotiveValue(motive) * 5 / 10 + t.Patience / 5, motive));
            }
        }
        return list;
    }

    /// <summary>Chave de uma meta de nivel para o bloqueio de readocao.</summary>
    public static string LevelGoalKey(string type, string target, int value) => $"{type}|{target}|{value}";

    /// <summary>Meta de nivel abandonada com o mesmo alvo, sem subida de nivel desde entao (D-055).</summary>
    public static bool IsBlocked(Character ch, string type, string target, int value) =>
        ch.BlockedLevelGoals.Contains(LevelGoalKey(type, target, value));

    /// <summary>Tipos de atividade que uma preferencia motiva (crafting: nenhum por enquanto).</summary>
    public static IReadOnlyList<string> TypesOf(string preference) => preference switch
    {
        "pve" => new[] { ActivityTypes.Hunt },
        "pvp" => new[] { ActivityTypes.Patrol, ActivityTypes.Ambush },
        "gathering" => new[] { ActivityTypes.Gather },
        "exploration" => new[] { ActivityTypes.Explore },
        "trade" => new[] { ActivityTypes.Trade },
        "social" => new[] { ActivityTypes.Socialize },
        _ => Array.Empty<string>(),
    };

    /// <summary>
    /// Metas de desbloqueio possiveis (D-055): para cada preferencia relevante, a atividade
    /// conhecida (zona conhecida) desse tema com o menor nivel minimo acima do atual, ao alcance
    /// (ate UnlockMaxLevelsAhead) e sem requisito que ainda nao existe (grupo, item, classe).
    /// Varias preferencias podem gerar opcoes; a escolha entre metas e ponderada.
    /// </summary>
    public static List<(string Target, int Level, int Weight, string Motive)> UnlockOptions(Person p, Character ch, ContentDatabase c, GoalSettingsDefinition s)
    {
        var list = new List<(string, int, int, string)>();
        if (!s.UnlockEnabled) return list;
        int limit = Math.Min(c.Progression.LevelCap, ch.Level + s.UnlockMaxLevelsAhead);
        foreach (var key in PersonPreferences.Keys)
        {
            int pref = p.Preferences.Get(key);
            var types = TypesOf(key);
            if (pref < s.UnlockMinPreference || types.Count == 0) continue;
            (string Region, ActivityDefinition? A) best = ("", null);
            foreach (var regionId in ch.Known)
                foreach (var a in c.Regions[regionId].AllActivities())
                {
                    if (!types.Contains(a.Type) || a.MinLevel <= ch.Level || a.MinLevel > limit) continue;
                    if (a.MinGroupSize > 1 || a.RequiredItemIds.Count > 0 || (a.ClassIds.Count > 0 && !a.ClassIds.Contains(ch.ClassId))) continue;
                    if (best.A == null || a.MinLevel < best.A.MinLevel
                        || (a.MinLevel == best.A.MinLevel && string.CompareOrdinal(regionId + "/" + a.Id, best.Region + "/" + best.A.Id) < 0))
                        best = (regionId, a);
                }
            if (best.A != null && !IsBlocked(ch, GoalTypes.UnlockLevel, best.Region + "/" + best.A.Id, best.A.MinLevel))
                list.Add((best.Region + "/" + best.A.Id, best.A.MinLevel, s.UnlockBaseWeight + pref * s.UnlockPerPreference / 1000, Person.PreferenceMotivePrefix + key));
        }
        return list;
    }

    /// <summary>
    /// Descobertas orientadas pelo conteudo (D-055): zonas desconhecidas da fronteira (vizinhas das
    /// conhecidas, dentro do nivel aceito) sobre as quais a pessoa tem pista de uma atividade de um
    /// tema que prefere. Sem pista valida, nada: ela nao sabe o conteudo de zonas desconhecidas.
    /// </summary>
    public static List<(string Zone, int Weight, string Motive)> ContentDiscoveryOptions(Person p, Character ch, ContentDatabase c, GoalSettingsDefinition s, int maxLevel)
    {
        var list = new List<(string, int, string)>();
        if (!s.ContentDiscoveryEnabled || ch.Hints.Count == 0) return list;
        foreach (var key in PersonPreferences.Keys)
        {
            int pref = p.Preferences.Get(key);
            var types = TypesOf(key);
            if (pref < s.ContentDiscoveryMinPreference || types.Count == 0) continue;
            foreach (var hint in ch.Hints)
            {
                int slash = hint.IndexOf('/');
                string zoneId = hint[..slash], activityId = hint[(slash + 1)..];
                if (ch.Known.Contains(zoneId) || !c.Regions.TryGetValue(zoneId, out var z)) continue;
                if (z.FindActivity(activityId) is not { } a || !types.Contains(a.Type)) continue;
                if (!z.IsCity && z.LevelMin > maxLevel) continue;
                if (!ch.Known.Any(k => c.Regions[k].Connections.Any(x => x.To == zoneId) || z.Connections.Any(x => x.To == k))) continue;
                list.Add((zoneId, s.ContentDiscoveryBaseWeight + pref * s.ContentDiscoveryPerPreference / 1000, Person.PreferenceMotivePrefix + key));
                break;   // uma zona por preferencia (a primeira pista em ordem ordinal)
            }
        }
        return list;
    }

    /// <summary>Preferencia que motiva praticar o tipo (chave de motivo "pref_x").</summary>
    private static string? AffinityTraitFor(string type) => type switch
    {
        ActivityTypes.Hunt => "pref_pve",
        ActivityTypes.Gather => "pref_gathering",
        ActivityTypes.Trade => "pref_trade",
        ActivityTypes.Explore => "pref_exploration",
        ActivityTypes.Socialize => "pref_social",
        _ => null,
    };

    /// <summary>
    /// O que cumprir uma meta reforca: o atributo do motivo; para um motivo de preferencia (estavel),
    /// a especialidade do mesmo tema, quando existe (coleta e fabricacao nao tem).
    /// </summary>
    public static string? ReinforcedBy(string motiveKey) => motiveKey switch
    {
        "pref_pve" => "pve",
        "pref_pvp" => "pvp",
        "pref_trade" => "trade",
        "pref_exploration" => "exploration",
        "pref_social" => "socializing",
        _ when motiveKey.StartsWith(Person.PreferenceMotivePrefix, StringComparison.Ordinal) => null,
        _ => motiveKey,
    };

    /// <summary>
    /// Missao principal ativa ou oferecida por um quadro conhecido; senao, uma secundaria. Oferta so
    /// com vaga para aceitar (D-065): meta para missao que nao pode ser aceita agora e requisito
    /// indisponivel, nao estagnacao.
    /// </summary>
    private QuestDefinition? MainQuestTarget(Character ch)
    {
        bool canAccept = _quests.CanAccept(ch);
        foreach (var cat in new[] { QuestCategories.Main, QuestCategories.Secondary })
        {
            foreach (var aq in ch.ActiveQuestList)
                if (C.Quests.TryGetValue(aq.QuestId, out var q) && q.Category == cat) return q;
            if (!canAccept) continue;
            foreach (var b in C.QuestBoards.Values.OrderBy(b => b.Id, StringComparer.Ordinal))
            {
                if (!ch.Known.Contains(b.RegionId)) continue;
                var offer = _quests.Offers(ch, b).FirstOrDefault(q => q.Category == cat);
                if (offer != null) return offer;
            }
        }
        return null;
    }

    // ---------------- Influencia na decisao ----------------

    /// <summary>
    /// Bonus da opcao pelos objetivos ativos: relevancia (0–1000) x comprometimento x peso.
    /// Limitado (nao vira regra): ruido, gosto, risco e distancia ainda podem vencer.
    /// </summary>
    public int Bonus(Character ch, RegionDefinition region, ActivityDefinition a, bool yieldsXp, int levelFit, bool deliversQuests = false)
        => BonusOf(ch, region, a, yieldsXp, levelFit, deliversQuests, onlyProgression: false);

    /// <summary>
    /// Parte das metas que e progressao (nivel, desbloqueio por nivel e missao): o incentivo de
    /// progressao inicial nao soma com ela; na mesma opcao vale o maior dos dois (D-055).
    /// </summary>
    public int ProgressionBonus(Character ch, RegionDefinition region, ActivityDefinition a, bool yieldsXp, int levelFit, bool deliversQuests = false)
        => BonusOf(ch, region, a, yieldsXp, levelFit, deliversQuests, onlyProgression: true);

    private int BonusOf(Character ch, RegionDefinition region, ActivityDefinition a, bool yieldsXp, int levelFit, bool deliversQuests, bool onlyProgression)
    {
        if (ch.GoalList.Count == 0) return 0;
        int total = 0;
        foreach (var g in ch.GoalList)
        {
            if (onlyProgression && g.Type is not (GoalTypes.ReachLevel or GoalTypes.UnlockLevel or GoalTypes.CompleteQuest)) continue;
            int rel = g.Type switch
            {
                // Quem quer subir de nivel tambem e puxado para entregar missoes prontas (recompensa em XP).
                // O desbloqueio por nivel usa a mesma regra: a pessoa escolhe a fonte de XP.
                GoalTypes.ReachLevel or GoalTypes.UnlockLevel => deliversQuests ? 800 : yieldsXp && levelFit >= 600 ? levelFit : 0,
                GoalTypes.DiscoverZone => region.Connections.Any(c => c.To == g.Target) ? 1000
                    : a.Type == ActivityTypes.Explore && region.Connections.Any(c => C.Regions[c.To].Connections.Any(c2 => c2.To == g.Target)) ? 700 : 0,
                GoalTypes.CompleteQuest => QuestRelevance(ch, g.Target, region, a),
                GoalTypes.PracticeActivity => a.Type == g.Target ? 1000 : 0,
                _ => 0,
            };
            total += S.Weight * g.Commitment / 1000 * rel / 1000;
        }
        return Math.Min(total, S.Weight * S.MaxStackPermille / 1000);
    }

    private int QuestRelevance(Character ch, string questId, RegionDefinition region, ActivityDefinition a)
    {
        if (!C.Quests.TryGetValue(questId, out var q)) return 0;
        var active = ch.ActiveQuestList.FirstOrDefault(x => x.QuestId == questId);
        if (active != null)
        {
            if (QuestSystem.CurrentStep(C, active) is { } s) return s.RegionId == region.Id && s.ActivityId == a.Id ? 1000 : 0;
            return _quests.BoardAt(region.Id, a.Id)?.Id == q.BoardId ? 1000 : 0;   // pronta: entregar
        }
        return _quests.BoardAt(region.Id, a.Id)?.Id == q.BoardId ? 1000 : 0;       // ainda nao aceita: ir ao quadro
    }

    // ---------------- Progresso e conclusao ----------------

    internal void OnActivity(Person p, Character ch, ActivityDefinition a, int minutes)
    {
        foreach (var g in ch.GoalList.ToList())
        {
            if (g.Type != GoalTypes.PracticeActivity || g.Target != a.Type || minutes <= 0) continue;
            g.Progress += minutes;
            g.LastProgressPlayedTicks = ch.PlayedTicksAt(Now);
            if (g.Progress >= g.TargetValue) Complete(p, ch, g);
        }
    }

    /// <summary>
    /// XP ganho (atividade ou missao) e progresso das metas de nivel e de desbloqueio: elas so sao
    /// abandonadas por falta de progresso quando a pessoa deixa de ganhar XP (D-055).
    /// </summary>
    internal void OnExperience(Character ch)
    {
        foreach (var g in ch.GoalList)
            if (g.Type is GoalTypes.ReachLevel or GoalTypes.UnlockLevel)
                g.LastProgressPlayedTicks = ch.PlayedTicksAt(Now);
    }

    internal void OnLevelUp(Person p, Character ch)
    {
        // Subir de nivel e mudanca relevante: metas de nivel abandonadas podem voltar.
        ch.BlockedLevelGoals.Clear();
        foreach (var g in ch.GoalList.ToList())
        {
            if (g.Type != GoalTypes.ReachLevel && g.Type != GoalTypes.UnlockLevel) continue;
            g.Progress = ch.Level - g.StartValue;
            g.LastProgressPlayedTicks = ch.PlayedTicksAt(Now);
            if (ch.Level >= g.TargetValue) Complete(p, ch, g);
        }
    }

    internal void OnDiscovered(Person p, Character ch, string zoneId)
    {
        foreach (var g in ch.GoalList.ToList())
        {
            if (g.Type != GoalTypes.DiscoverZone) continue;
            g.LastProgressPlayedTicks = ch.PlayedTicksAt(Now);   // explorar conta como avanco
            if (g.Target == zoneId || ch.Known.Contains(g.Target)) { g.Progress = 1; Complete(p, ch, g); }
        }
    }

    /// <summary>Avanco parcial da etapa ou missao aceita (D-065): progresso, sem contar etapa.</summary>
    internal void OnQuestProgress(Character ch, string questId)
    {
        foreach (var g in ch.GoalList)
            if (g.Type == GoalTypes.CompleteQuest && g.Target == questId)
                g.LastProgressPlayedTicks = ch.PlayedTicksAt(Now);
    }

    internal void OnQuestStep(Character ch, string questId)
    {
        foreach (var g in ch.GoalList)
            if (g.Type == GoalTypes.CompleteQuest && g.Target == questId)
            {
                g.Progress++;
                g.LastProgressPlayedTicks = ch.PlayedTicksAt(Now);
            }
    }

    internal void OnQuestCompleted(Person p, Character ch, string questId)
    {
        foreach (var g in ch.GoalList.ToList())
            if (g.Type == GoalTypes.CompleteQuest && g.Target == questId) Complete(p, ch, g);
    }

    /// <summary>Meta encerrada: (personagem, meta, concluida?, motivo).</summary>
    internal Action<Character, PersonalGoal, bool, string>? Ended;

    private void Complete(Person p, Character ch, PersonalGoal g)
    {
        // Persistente (P4d): concluir meta que passou por reveses e evidencia; se so continuou pelo efeito, nao.
        if (g.SetbacksDuring > 0) QuirkSystem.Observe(C, p, QuirkIds.Persistente, true, false, g.SurvivedByEffect);
        ch.GoalList.Remove(g);
        ch.GoalsCompleted++;
        if (g.Links.Count > 0)
        {
            ch.GoalsLinkedCompleted++;
            foreach (var l in g.Links)
                if (p.ObjectiveList.FirstOrDefault(o => o.Id == l.ObjectiveId) is { } o)
                {
                    o.LinkedGoalsCompleted++;
                    if (l.Kind == ObjectiveLinkKinds.Advance) o.FailureStreak = 0;   // sucesso interrompe a sequencia de falhas (O3)
                }
        }
        Ended?.Invoke(ch, g, true, "goal_completed");
        _w.Emit("goal_completed", EventImportance.Normal, new[] { new EntityRef("character", ch.Id) },
            new[] { new EventCause(g.MotiveKey, g.MotiveValue) }, $"{g.Type}|{g.Target}|{g.TargetValue}");
        // Conseguir o que queria reforca o que motivou o objetivo (a especialidade, se o motivo e uma preferencia).
        if (ReinforcedBy(g.MotiveKey) is { } reinforced) _traits.Adjust(p, reinforced, S.MotiveReinforce, "goal_completed");
    }

    private void Abandon(Character ch, PersonalGoal g, string reason)
    {
        ch.GoalList.Remove(g);
        // Persistente (P4d): desistir (sem progresso ou impaciencia) de meta que passou por reveses e evidencia contraria.
        if (g.SetbacksDuring > 0 && reason is "no_progress" or "impatience")
            QuirkSystem.Observe(C, _w.GetPerson(ch.PersonId), QuirkIds.Persistente, false, true);
        // Meta de nivel abandonada nao volta com o mesmo alvo antes da proxima subida de nivel.
        if (g.Type is GoalTypes.ReachLevel or GoalTypes.UnlockLevel) ch.BlockedLevelGoals.Add(LevelGoalKey(g.Type, g.Target, g.TargetValue));
        ch.GoalsAbandoned++;
        if (g.Links.Count > 0)
        {
            ch.GoalsLinkedAbandoned++;
            var person = _w.GetPerson(ch.PersonId);
            foreach (var l in g.Links)
                if (person.ObjectiveList.FirstOrDefault(o => o.Id == l.ObjectiveId) is { } o)
                {
                    o.LinkedGoalsAbandoned++;
                    // Falha de uma meta que avancaria o objetivo (O3). Preparacao nao conta: sistema futuro nao perde por nao avancar.
                    if (l.Kind == ObjectiveLinkKinds.Advance && o.State == ObjectiveStates.Active) o.FailureStreak++;
                }
        }
        Ended?.Invoke(ch, g, false, reason);
        _w.Emit("goal_abandoned", EventImportance.Low, new[] { new EntityRef("character", ch.Id) },
            new[] { new EventCause(reason, 1000) }, $"{g.Type}|{g.Target}|{g.TargetValue}");
    }
}

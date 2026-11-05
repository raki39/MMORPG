using Simulation.Core.Content;
using Simulation.Core.Events;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Core.Behavior;

/// <summary>
/// Metas de curto prazo (D-026/D-036; ambicoes de longo prazo ficam para uma etapa futura). Ao conectar, a pessoa pode adotar um objetivo escolhido
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
        foreach (var g in ch.GoalList.ToList())
        {
            if (played - g.LastProgressPlayedTicks > staleTicks) Abandon(ch, g, "no_progress");
            else if (t.Patience < S.ImpatientBelow && rng.ChancePermille(S.ImpatientDropChance)) Abandon(ch, g, "impatience");
        }
        // Quantidade de metas e chance de adotar nao dependem da paciencia (ela so pesa na persistencia).
        if (ch.GoalList.Count >= S.MaxActive) return;
        if (!rng.ChancePermille(S.AdoptChance)) return;
        var options = Candidates(p, ch, rng);
        if (options.Count == 0) return;
        var pick = options[rng.PickWeighted(options.Select(o => o.Weight).ToList())];
        var g2 = new PersonalGoal(pick.Type, pick.Target, pick.TargetValue, pick.StartValue, CommitmentOf(t),
            pick.MotiveKey, p.MotiveValue(pick.MotiveKey), Now, played);
        ch.GoalList.Add(g2);
        ch.GoalsSet++;
        _w.Emit("goal_set", EventImportance.Low, new[] { new EntityRef("character", ch.Id) },
            new[] { new EventCause(pick.MotiveKey, p.MotiveValue(pick.MotiveKey)), new EventCause("commitment", g2.Commitment) },
            $"{pick.Type}|{pick.Target}|{pick.TargetValue}");
    }

    private readonly record struct Candidate(string Type, string Target, int TargetValue, int StartValue, int Weight, string MotiveKey);

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
        if (!Has(GoalTypes.CompleteQuest) && MainQuestTarget(ch) is { } quest)
        {
            int w = 120 + (pr.Pve + pr.Exploration) / 5 + t.Patience / 5 + t.GameKnowledge / 10;
            string motive = t.Patience >= t.GameKnowledge ? "patience" : "gameKnowledge";
            list.Add(new(GoalTypes.CompleteQuest, quest.Id, 1, 0, w, motive));
        }

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

    /// <summary>Missao principal ativa ou oferecida por um quadro conhecido; senao, uma secundaria.</summary>
    private QuestDefinition? MainQuestTarget(Character ch)
    {
        foreach (var cat in new[] { QuestCategories.Main, QuestCategories.Secondary })
        {
            foreach (var aq in ch.ActiveQuestList)
                if (C.Quests.TryGetValue(aq.QuestId, out var q) && q.Category == cat) return q;
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
    {
        if (ch.GoalList.Count == 0) return 0;
        int total = 0;
        foreach (var g in ch.GoalList)
        {
            int rel = g.Type switch
            {
                // Quem quer subir de nivel tambem e puxado para entregar missoes prontas (recompensa em XP).
                GoalTypes.ReachLevel => deliversQuests ? 800 : yieldsXp && levelFit >= 600 ? levelFit : 0,
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

    internal void OnLevelUp(Person p, Character ch)
    {
        foreach (var g in ch.GoalList.ToList())
        {
            if (g.Type != GoalTypes.ReachLevel) continue;
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
        ch.GoalList.Remove(g);
        ch.GoalsCompleted++;
        Ended?.Invoke(ch, g, true, "goal_completed");
        _w.Emit("goal_completed", EventImportance.Normal, new[] { new EntityRef("character", ch.Id) },
            new[] { new EventCause(g.MotiveKey, g.MotiveValue) }, $"{g.Type}|{g.Target}|{g.TargetValue}");
        // Conseguir o que queria reforca o que motivou o objetivo (a especialidade, se o motivo e uma preferencia).
        if (ReinforcedBy(g.MotiveKey) is { } reinforced) _traits.Adjust(p, reinforced, S.MotiveReinforce, "goal_completed");
    }

    private void Abandon(Character ch, PersonalGoal g, string reason)
    {
        ch.GoalList.Remove(g);
        ch.GoalsAbandoned++;
        Ended?.Invoke(ch, g, false, reason);
        _w.Emit("goal_abandoned", EventImportance.Low, new[] { new EntityRef("character", ch.Id) },
            new[] { new EventCause(reason, 1000) }, $"{g.Type}|{g.Target}|{g.TargetValue}");
    }
}

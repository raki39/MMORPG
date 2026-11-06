using Simulation.Core.Content;
using Simulation.Core.Random;

namespace Simulation.Core.Population;

/// <summary>Viabilidade de um objetivo agora (calculada, nao guardada).</summary>
public static class ObjectiveViability
{
    /// <summary>Pode avancar no momento.</summary>
    public const string Now = "now";
    /// <summary>Os sistemas existem, mas falta um requisito conhecido (ex.: nivel minimo).</summary>
    public const string Preparation = "preparation";
    /// <summary>Depende de um sistema ainda nao implementado: sem progresso ficticio.</summary>
    public const string Future = "future";
}

/// <summary>
/// Avaliacao de um objetivo para a ficha: viabilidade, sistemas que faltam, progresso (so quando
/// mensuravel com o que existe) e a preparacao possivel por requisito concreto do conteudo, mostrada
/// separada da etapa final (que pode estar indisponivel).
/// </summary>
/// <param name="PreparationLevel">Nivel exigido por um requisito conhecido ainda nao cumprido (0 = nenhum).</param>
/// <param name="PreparationSubject">De onde vem o requisito: missao, "zona/atividade" ou item.</param>
public sealed record ObjectiveAssessment(string Viability, IReadOnlyList<string> MissingSystems, int Progress, int ProgressTotal,
    int PreparationLevel, string PreparationSubject, bool CriterionMet)
{
    public bool Measurable => ProgressTotal > 0;
}

/// <summary>
/// Objetivos pessoais (O1, D-062): sorteio dos objetivos iniciais no registro (fluxo proprio por
/// pessoa) e avaliacao da viabilidade. Sem influencia no comportamento nesta entrega: a ligacao com
/// as metas e a satisfacao chegam na O2, e a evolucao na O3.
/// </summary>
public static class ObjectiveSystem
{
    public const string StreamName = "population.objectives";
    public const string InitialOrigin = "initial";
    /// <summary>Chave do termo de ambicao (dedicacao x dificuldade) nos motivos.</summary>
    public const string AmbitionKey = "ambition";

    /// <summary>Ambicao pela dedicacao (perfil): a importancia do jogo para a pessoa.</summary>
    public static int Ambition(Person p) => p.Traits.GameImportance;

    /// <summary>Termo de ambicao (permille): quem se dedica muito puxa para objetivos dificeis; quem se dedica pouco, para os simples.</summary>
    public static int AmbitionTerm(ObjectiveGenerationDefinition g, int ambition, int difficulty) =>
        1000 + (int)((long)g.DifficultyStrengthPermille * ((Math.Clamp(ambition, 0, 1000) - 500) * 2) * ((Math.Clamp(difficulty, 0, 1000) - 500) * 2) / 1_000_000);

    /// <summary>Termo de uma afinidade: as chaves das traits e "quirk.x" (ter a trait, inicial ou adquirida).</summary>
    public static int AffinityTerm(ContentDatabase c, Person p, QuirkAffinityDefinition a)
    {
        if (a.Key.StartsWith("quirk.", StringComparison.Ordinal))
        {
            string id = a.Key["quirk.".Length..];
            foreach (var q in p.QuirkList) if (q.Id == id) return 1000 + a.StrengthPermille;
            return 1000;
        }
        return QuirkSystem.AffinityTerm(c, p, a);
    }

    /// <summary>Termos da coerencia (afinidades e ambicao), na ordem do conteudo.</summary>
    public static List<(string Key, int Term)> Terms(ContentDatabase c, Person p, ObjectiveDefinition o)
    {
        var list = new List<(string, int)>(o.Affinities.Count + 1);
        foreach (var a in o.Affinities) list.Add((a.Key, AffinityTerm(c, p, a)));
        list.Add((AmbitionKey, AmbitionTerm(c.ObjectiveCatalog.Generation, Ambition(p), o.Difficulty)));
        return list;
    }

    /// <summary>Ajuste da pessoa ao objetivo (permille; 1000 = neutro): produto dos termos, limitado.</summary>
    public static int Fit(ContentDatabase c, Person p, ObjectiveDefinition o)
    {
        var g = c.ObjectiveCatalog.Generation;
        long m = 1000;
        foreach (var (_, term) in Terms(c, p, o)) m = Math.Clamp(m * Math.Max(0, term) / 1000, 0, 1_000_000);
        return (int)Math.Clamp(m, g.MinMultiplierPermille, g.MaxMultiplierPermille);
    }

    /// <summary>Comprometimento com as ambicoes: dedicacao (perfil), paciencia e lealdade.</summary>
    public static int CommitmentOf(ObjectiveGenerationDefinition g, PersonTraits t) =>
        Math.Clamp(g.CommitmentBase + (t.GameImportance * g.CommitmentGameImportancePermille + t.Patience * g.CommitmentPatiencePermille
            + t.Loyalty * g.CommitmentLoyaltyPermille) / 1000, 0, 1000);

    /// <summary>Categorias de item que sao especialidades de fabricacao.</summary>
    public static readonly string[] CraftingSpecialties = { ItemCategories.Weapon, ItemCategories.Armor, ItemCategories.Consumable };

    /// <summary>
    /// Parametros possiveis para a pessoa: um item raro que a classe usa; uma especialidade com itens
    /// no conteudo; "" para objetivos sem parametro. Lista vazia = o objetivo nao cabe (sem inventar alvo).
    /// </summary>
    public static List<string> ParamOptions(ContentDatabase c, ObjectiveDefinition o, string classId)
    {
        var g = c.ObjectiveCatalog.Generation;
        switch (o.Criterion)
        {
            case ObjectiveCriteria.RareItem:
                return c.Items.Values
                    .Where(i => (i.Category == ItemCategories.Weapon || i.Category == ItemCategories.Armor)
                                && g.RareItemRarities.Contains(i.Rarity, StringComparer.Ordinal)
                                && (i.ClassIds.Count == 0 || i.ClassIds.Contains(classId, StringComparer.Ordinal)))
                    .Select(i => i.Id).ToList();
            case ObjectiveCriteria.CraftingRank:
                return CraftingSpecialties.Where(cat => c.Items.Values.Any(i => i.Category == cat)).ToList();
            default:
                return new List<string> { "" };
        }
    }

    /// <summary>
    /// Objetivos iniciais (no registro, com a classe ja escolhida). Quantidades pelos pesos do conteudo;
    /// cada objetivo sorteado pelo peso x ajuste, sem repetir; a importancia vem do ajuste com ruido; os
    /// mais importantes para a pessoa sao os gerais. Fluxo proprio: nao muda os demais sorteios.
    /// </summary>
    public static List<PersonObjective> Draw(ContentDatabase c, Person p, string classId, ulong campaignSeed, long day)
    {
        var g = c.ObjectiveCatalog.Generation;
        var result = new List<PersonObjective>();
        if (!g.Enabled || c.ObjectivesOrdered.Count == 0) return result;
        var rng = new DeterministicRng(RngStreams.DeriveSeed(campaignSeed, StreamName, p.Id));
        int general = rng.PickWeighted(g.GeneralCountWeights);
        int secondary = rng.PickWeighted(g.SecondaryCountWeights);

        var pool = new List<(ObjectiveDefinition O, int Fit, List<string> Params)>();
        foreach (var o in c.ObjectivesOrdered)
        {
            var options = ParamOptions(c, o, classId);
            if (options.Count == 0 || o.Weight <= 0) continue;
            pool.Add((o, Fit(c, p, o), options));
        }
        var chosen = new List<(ObjectiveDefinition O, int Fit, string Param, int Importance)>();
        for (int k = 0; k < general + secondary && pool.Count > 0; k++)
        {
            var weights = pool.Select(x => (int)Math.Min(1_000_000L, (long)x.O.Weight * x.Fit / 1000)).ToList();
            if (weights.All(w => w <= 0)) break;
            int i = rng.PickWeighted(weights);
            var pick = pool[i];
            pool.RemoveAt(i);
            string param = pick.Params[rng.NextInt(pick.Params.Count)];
            int importance = Math.Clamp(500 + (pick.Fit - 1000) * g.ImportanceSlopePermille / 1000 + rng.NextCenteredNoise(g.ImportanceNoise), 50, 1000);
            chosen.Add((pick.O, pick.Fit, param, importance));
        }

        int commitment = CommitmentOf(g, p.Traits);
        var ordered = chosen.OrderByDescending(x => x.Importance).ThenBy(x => x.O.Id, StringComparer.Ordinal).ToList();
        for (int i = 0; i < ordered.Count; i++)
        {
            var x = ordered[i];
            var motives = Terms(c, p, x.O).Where(t => t.Term != 1000)
                .OrderByDescending(t => Math.Abs(t.Term - 1000)).ThenBy(t => t.Key, StringComparer.Ordinal).Take(3)
                .Select(t => (t.Key, t.Term - 1000)).ToList();
            result.Add(new PersonObjective(x.O.Id, x.Param, i < general ? ObjectiveTiers.General : ObjectiveTiers.Secondary,
                x.Importance, commitment, InitialOrigin, day, motives));
        }
        return result;
    }

    /// <summary>
    /// Viabilidade, progresso e preparacao com o que existe hoje. Objetivo de sistema futuro: sem
    /// progresso; a preparacao so aparece quando ha um requisito concreto no conteudo (nivel minimo).
    /// </summary>
    public static ObjectiveAssessment Assess(ContentDatabase c, Character? ch, PersonObjective o)
    {
        string crit = c.Objectives.TryGetValue(o.Id, out var def) ? def.Criterion : "";
        var missing = ObjectiveCriteria.RequiredSystems(crit).Where(s => !ObjectiveSystems.Exists(s)).ToList();
        int level = ch?.Level ?? 1, cap = c.Progression.LevelCap;
        int progress = 0, total = 0, prepLevel = 0;
        string prepSubject = "";
        bool met = false;
        switch (crit)
        {
            case ObjectiveCriteria.MaxLevel:
                progress = level; total = cap; met = level >= cap;
                break;
            case ObjectiveCriteria.MainCampaign:
            {
                var mains = c.Quests.Values.Where(q => q.Category == QuestCategories.Main).ToList();
                bool Done(string id) => ch != null && ch.QuestHistory.TryGetValue(id, out var r) && r.Completions > 0;
                total = mains.Count;
                progress = mains.Count(q => Done(q.Id));
                met = total > 0 && progress >= total;
                // Proxima missao principal liberada (pre-requisitos feitos): nivel minimo e requisito conhecido.
                var next = mains.Where(q => !Done(q.Id) && q.PrerequisiteQuestIds.All(Done))
                    .OrderBy(q => q.MinLevel).ThenBy(q => q.Id, StringComparer.Ordinal).FirstOrDefault();
                if (!met && next != null && next.MinLevel > level) { prepLevel = next.MinLevel; prepSubject = next.Id; }
                break;
            }
            case ObjectiveCriteria.AllZones:
                total = c.Regions.Count; progress = ch?.KnownRegions.Count ?? 0; met = progress >= total;
                break;
            case ObjectiveCriteria.StrengthRanking:
                // Os mais fortes estao no nivel maximo: requisito concreto de preparacao (nao o indice de poder).
                if (level < cap) prepLevel = cap;
                break;
            case ObjectiveCriteria.DefeatBosses:
            {
                var next = BossRequirements(c).Where(b => b.Level > level).OrderBy(b => b.Level).ThenBy(b => b.Key, StringComparer.Ordinal).FirstOrDefault();
                if (next.Key != null) { prepLevel = next.Level; prepSubject = next.Key; }
                break;
            }
            case ObjectiveCriteria.PvpRanking:
            {
                // Acesso ao conteudo PvP (nivel minimo da primeira atividade PvP). Nao e evidencia de vitoria.
                var first = PvpRequirements(c).OrderBy(b => b.Level).ThenBy(b => b.Key, StringComparer.Ordinal).FirstOrDefault();
                if (first.Key != null && first.Level > level) { prepLevel = first.Level; prepSubject = first.Key; }
                break;
            }
            case ObjectiveCriteria.RareItem:
                if (c.Items.TryGetValue(o.Param, out var item) && item.LevelRequirement > level) { prepLevel = item.LevelRequirement; prepSubject = item.Id; }
                break;
        }
        string viability = missing.Count > 0 ? ObjectiveViability.Future : prepLevel > 0 ? ObjectiveViability.Preparation : ObjectiveViability.Now;
        return new ObjectiveAssessment(viability, missing, progress, total, prepLevel, prepSubject, missing.Count == 0 && met);
    }

    /// <summary>Chefes do mundo ("zona/atividade") e o nivel minimo de cada um.</summary>
    public static IEnumerable<(string Key, int Level)> BossRequirements(ContentDatabase c) =>
        c.Regions.Values.SelectMany(r => r.AllActivities().Where(a => a.Type == ActivityTypes.Boss).Select(a => (r.Id + "/" + a.Id, a.MinLevel)));

    /// <summary>Atividades de PvP ("zona/atividade") e o nivel minimo de cada uma.</summary>
    public static IEnumerable<(string Key, int Level)> PvpRequirements(ContentDatabase c) =>
        c.Regions.Values.SelectMany(r => r.AllActivities().Where(a => a.Type is ActivityTypes.Patrol or ActivityTypes.Ambush).Select(a => (r.Id + "/" + a.Id, a.MinLevel)));
}

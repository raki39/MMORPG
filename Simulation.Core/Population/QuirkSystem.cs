using Simulation.Core.Content;
using Simulation.Core.Random;

namespace Simulation.Core.Population;

/// <summary>Ids das traits com efeito implementado no Core (P4c): lista fechada de pontos de avaliacao.</summary>
public static class QuirkIds
{
    public const string Persistente = "persistente", Impulsivo = "impulsivo", Colecionador = "colecionador", Otimizador = "otimizador",
        Resiliente = "resiliente", PavioCurto = "pavio_curto", Saudosista = "saudosista", Rancoroso = "rancoroso", Estudioso = "estudioso",
        Esquecido = "esquecido", MemoriaFirme = "memoria_firme", Observador = "observador", Indeciso = "indeciso",
        Maratonista = "maratonista", Procrastinador = "procrastinador";

    /// <summary>Chave do fator ou da causa de uma trait (decisao, eventos e interface).</summary>
    public static string Factor(string id) => "quirk_" + id;
}

/// <summary>Trait da pessoa (P4b): id, origem (innate ou acquired) e dia em que entrou (0 = criacao).</summary>
public sealed record QuirkState(string Id, string Source, long Day);

/// <summary>
/// Traits (P4b): sorteio na criacao, com fluxo de RNG proprio (nenhum outro atributo de ninguem
/// muda). Quantidade pela tabela do conteudo; cada vaga e positiva ou negativa por sorteio proprio;
/// dentro da classificacao, a escolha pondera o peso base pela coerencia com a pessoa
/// (personalidade, atributos, conduta, preferencias e perfil), com multiplicador limitado.
/// Exclusoes valem sobre o conjunto inteiro e com a personalidade principal; sem candidata
/// valida, a vaga fica vazia (sem forcar). Os efeitos chegam na P4c; aquisicao e perda na P4d.
/// </summary>
public static class QuirkSystem
{
    public const string StreamName = "population.quirks";

    /// <summary>A pessoa tem a trait e os efeitos estao ligados (P4c).</summary>
    public static bool Active(ContentDatabase c, Person p, string id)
    {
        if (!c.QuirkCatalog.Effects.Enabled) return false;
        foreach (var q in p.QuirkList) if (q.Id == id) return true;
        return false;
    }

    /// <summary>Conta uma atuacao da trait (medicao e ficha).</summary>
    public static void Count(Person p, string key, int amount = 1)
    {
        if (amount == 0) return;
        p.QuirkStatsMap[key] = p.QuirkStatsMap.GetValueOrDefault(key) + amount;
    }

    /// <summary>Multiplica com resto guardado (inteiros): devolve as unidades inteiras de value * permille / 1000.</summary>
    public static int Carry(Person p, string key, int value, int permille)
    {
        long milli = (long)value * permille + p.QuirkCarryMap.GetValueOrDefault(key);
        int units = (int)(milli / 1000);
        int rest = (int)(milli - units * 1000L);
        if (rest == 0) p.QuirkCarryMap.Remove(key); else p.QuirkCarryMap[key] = rest;
        return units;
    }

    /// <summary>Atributos que podem entrar nas afinidades: personalidade, conduta e autocontrole.</summary>
    public static readonly string[] AffinityAttributes =
    {
        "sociability", "aggressiveness", "patience", "loyalty", "riskAversion", "variety",
        "competitiveness", "opportunism", "cheatPropensity", "selfControl", "extendTendency",
    };

    public static int AttributeValue(PersonTraits t, string key) => key switch
    {
        "selfControl" => t.SelfControl,
        "extendTendency" => t.ExtendTendency,
        _ => t.Get(key),
    };

    /// <summary>
    /// Posicao (0..1000) de um valor na distribuicao do preset. Triangular: a acumulada exata.
    /// Media e dispersao: o desvio em relacao a media dividido pela dispersao (aproximacao linear).
    /// </summary>
    public static int PositionOf(TraitRange r, int value)
    {
        if (r.IsTriangular) return PersonalitySystem.Position(r, value);
        if (r.Spread <= 0) return 500;
        return Math.Clamp(500 + (int)((long)(value - r.Mean) * 500 / r.Spread), 0, 1000);
    }

    /// <summary>Termo de uma afinidade (permille; 1000 = neutro).</summary>
    public static int AffinityTerm(ContentDatabase c, Person p, QuirkAffinityDefinition a)
    {
        int dot = a.Key.IndexOf('.');
        string kind = dot > 0 ? a.Key[..dot] : "", key = dot > 0 ? a.Key[(dot + 1)..] : a.Key;
        switch (kind)
        {
            case "profile": return p.ProfileId == key ? 1000 + a.StrengthPermille : 1000;
            case "personality": return p.PersonalityId == key ? 1000 + a.StrengthPermille : 1000;
            case "traits":
            case "preferences":
            {
                int pos = kind == "traits"
                    ? PositionOf(PopulationGenerator.TraitRangeOf(c.PopulationTraits, key), AttributeValue(p.Traits, key))
                    : PositionOf(PopulationGenerator.PreferenceRangeOf(c.PreferenceRanges, key), p.Preferences.Get(key));
                int dev = a.Direction == "low" ? (500 - pos) * 2 : (pos - 500) * 2;   // -1000..1000
                return 1000 + a.StrengthPermille * dev / 1000;
            }
            default: return 1000;
        }
    }

    /// <summary>Multiplicador de coerencia (permille): produto dos termos, limitado pelo conteudo.</summary>
    public static int Multiplier(ContentDatabase c, Person p, QuirkDefinition q)
    {
        var g = c.QuirkCatalog.Generation;
        long m = 1000;
        foreach (var a in q.Affinities)
            m = Math.Clamp(m * Math.Max(0, AffinityTerm(c, p, a)) / 1000, 0, 1_000_000);
        return (int)Math.Clamp(m, g.MinMultiplierPermille, g.MaxMultiplierPermille);
    }

    /// <summary>A trait combina com o que a pessoa ja tem e com a personalidade principal?</summary>
    public static bool Compatible(ContentDatabase c, QuirkDefinition q, IEnumerable<string> chosen, string personalityId)
    {
        if (personalityId.Length > 0 && q.ExcludesPersonalities.Contains(personalityId, StringComparer.Ordinal)) return false;
        foreach (var id in chosen)
        {
            if (id == q.Id || q.Excludes.Contains(id, StringComparer.Ordinal)) return false;
            if (c.Quirks.TryGetValue(id, out var other) && other.Excludes.Contains(q.Id, StringComparer.Ordinal)) return false;
        }
        return true;
    }

    /// <summary>Sorteio da criacao (fluxo proprio por pessoa). Vazio se desligado.</summary>
    public static List<QuirkState> Draw(ContentDatabase c, Person p, ulong campaignSeed)
    {
        var result = new List<QuirkState>();
        var g = c.QuirkCatalog.Generation;
        if (!g.Enabled || c.QuirksOrdered.Count == 0) return result;
        var rng = new DeterministicRng(RngStreams.DeriveSeed(campaignSeed, StreamName, p.Id));
        int n = Math.Min(rng.PickWeighted(g.CountWeights), g.MaxTotal);
        var ids = new List<string>();
        for (int slot = 0; slot < n; slot++)
        {
            string polarity = rng.ChancePermille(g.NegativeChancePermille) ? QuirkPolarities.Negative : QuirkPolarities.Positive;
            var candidates = new List<QuirkDefinition>();
            var weights = new List<int>();
            foreach (var q in c.QuirksOrdered)
            {
                if (!q.Drawn || q.Polarity != polarity || !Compatible(c, q, ids, p.PersonalityId)) continue;
                int w = (int)((long)q.Weight * Multiplier(c, p, q) / 1000);
                if (w <= 0) continue;
                candidates.Add(q);
                weights.Add(w);
            }
            if (candidates.Count == 0) continue;   // vaga vazia, sem forcar
            var pick = candidates[rng.PickWeighted(weights)];
            ids.Add(pick.Id);
            result.Add(new QuirkState(pick.Id, QuirkAcquisitions.Innate, 0));
        }
        return result;
    }

    // ---------------- Aquisicao e perda (P4d) ----------------

    /// <summary>Traits com avaliador de aquisicao e perda (acontecimentos ja simulados). As de combate e social esperam os sistemas.</summary>
    public static readonly string[] Evaluators = { QuirkIds.Persistente, QuirkIds.Rancoroso };

    public static bool HasEvaluator(string id) => Array.IndexOf(Evaluators, id) >= 0;

    public static AcquisitionRuleDefinition RuleOf(ContentDatabase c, string id) =>
        id == QuirkIds.Rancoroso ? c.QuirkCatalog.Acquisition.Rancoroso : c.QuirkCatalog.Acquisition.Persistente;

    private static QuirkEvidence EvidenceOf(Person p, string id)
    {
        if (!p.QuirkEvidenceMap.TryGetValue(id, out var e)) p.QuirkEvidenceMap[id] = e = new QuirkEvidence();
        return e;
    }

    /// <summary>
    /// Uma oportunidade de manifestar a trait e o que a pessoa fez nela: a favor (+1000), contra
    /// (+1000 na contraria) ou nada. "effectOnly": o comportamento so aconteceu pelo efeito da propria
    /// trait (sem autorreforco: conta como oportunidade, nao como evidencia).
    /// </summary>
    public static void Observe(ContentDatabase c, Person p, string id, bool supports, bool contradicts, bool effectOnly = false)
    {
        if (!c.QuirkCatalog.Acquisition.Enabled || !HasEvaluator(id)) return;
        var e = EvidenceOf(p, id);
        e.Opportunities++;
        if (effectOnly) { e.EffectOnlyCount++; return; }
        if (supports) { e.For += 1000; e.ForCount++; }
        else if (contradicts) { e.Against += 1000; e.AgainstCount++; }
    }

    /// <summary>
    /// Avaliacao diaria (meia-noite local) da aquisicao e da perda. So anda em dia jogado (jogou no dia
    /// local anterior): a evidencia enfraquece pela meia-vida, a confirmacao conta e os intervalos
    /// passam. Sem jogar, nada muda: falta de oportunidade nao e evidencia contraria. No maximo uma
    /// mudanca por dia. Devolve a mudanca aplicada, ou null.
    /// </summary>
    public static QuirkChange? OnDaily(ContentDatabase c, Person p, long localDay, bool playedYesterday)
    {
        var a = c.QuirkCatalog.Acquisition;
        if (!a.Enabled || !playedYesterday) return null;
        p.PlayDayCount++;
        long factor = Behavior.DailySchedule.HalfLifeFactor(1, Math.Max(1, a.HalfLifePlayDays));
        QuirkChange? applied = null;
        foreach (var id in Evaluators)
        {
            if (!c.Quirks.TryGetValue(id, out var def)) continue;
            var e = EvidenceOf(p, id);
            e.For = (int)(e.For * factor / 1_000_000);
            e.Against = (int)(e.Against * factor / 1_000_000);
            var rule = RuleOf(c, id);
            var state = p.QuirkList.FirstOrDefault(q => q.Id == id);
            bool initial = state != null && state.Source == QuirkAcquisitions.Innate;
            int total = e.For + e.Against;
            int rate = total == 0 ? 0 : (int)((long)e.For * 1000 / total);
            string want = "";
            if (state == null && e.For >= rule.AcquireEvidence && rate >= rule.AcquireRatePermille) want = "acquire";
            else if (state != null && total >= (initial ? rule.LoseInitialMinOpportunities : rule.LoseMinOpportunities)
                     && rate <= (initial ? rule.LoseInitialRatePermille : rule.LoseRatePermille)) want = "lose";
            if (want.Length == 0) { e.Streak = 0; e.Pending = ""; continue; }
            if (e.Pending != want) { e.Pending = want; e.Streak = 0; }
            e.Streak++;
            int need = want == "lose" && initial ? a.InitialConfirmPlayDays : a.ConfirmPlayDays;
            if (e.Streak < need || applied != null) continue;
            if (e.LastChangePlayDay >= 0 && p.PlayDayCount - e.LastChangePlayDay < a.CooldownPlayDays) continue;
            string previous;
            if (want == "acquire")
            {
                if (def.Acquisition == QuirkAcquisitions.Innate) continue;   // o conteudo nao permite adquirir
                if (p.QuirkList.Count >= c.QuirkCatalog.Generation.MaxTotal) { Count(p, id + "_bloqueada_pelo_total"); continue; }
                if (p.LastAcquisitionPlayDay >= 0 && p.PlayDayCount - p.LastAcquisitionPlayDay < a.PlayDaysBetweenAcquisitions) continue;
                if (!Compatible(c, def, p.QuirkList.Select(q => q.Id), p.PersonalityId)) { Count(p, id + "_bloqueada_por_exclusao"); continue; }
                previous = p.QuirkHistoryList.LastOrDefault(h => h.Id == id)?.PreviousSource ?? "";
                p.QuirkList.Add(new QuirkState(id, QuirkAcquisitions.Acquired, localDay));
                p.LastAcquisitionPlayDay = p.PlayDayCount;
            }
            else
            {
                previous = state!.Source;
                p.QuirkList.Remove(state);
            }
            string reason = id == QuirkIds.Persistente
                ? (want == "acquire" ? "goals_after_setbacks" : "gave_up_after_setbacks")
                : (want == "acquire" ? "repeated_bad_experience_same_place" : "second_chance_succeeded");
            applied = new QuirkChange(localDay, p.PlayDayCount, id, want == "acquire" ? "acquired" : "lost", previous, e.For, e.Against, reason);
            p.QuirkHistoryList.Add(applied);
            e.LastChangePlayDay = p.PlayDayCount;
            e.Streak = 0;
            e.Pending = "";
        }
        return applied;
    }
}

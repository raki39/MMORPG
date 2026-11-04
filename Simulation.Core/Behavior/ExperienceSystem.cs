using Simulation.Core.Content;
using Simulation.Core.Events;
using Simulation.Core.Population;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Core.Behavior;

/// <summary>
/// Experiencia, memoria e humor (D-038; plano 4.2 "Estado" e 4.5 "Memoria").
///
/// Cada atividade vira uma experiencia com componentes explicaveis (gosto, encaixe de nivel,
/// lotacao, companhia, tedio, risco). A sessao soma essas experiencias com o progresso
/// (comparado a expectativa da pessoa), a novidade e as conquistas/reveses, e vira uma nota.
/// A satisfacao e a media movel dessas notas; a frustracao reage a reveses e sessoes ruins.
/// A memoria guarda impressoes por lugar (influenciam escolhas) e acontecimentos marcantes.
/// Parametros em population.json ("mood" e "memory"). So inteiros, sem sorteio.
/// </summary>
public sealed class ExperienceSystem
{
    // Componentes da nota da sessao (chaves estaveis; a interface traduz).
    public const string Fun = "fun", Fit = "fit", Crowding = "crowding", Company = "company",
        Boredom = "boredom", Risk = "risk", Progress = "progress", Novelty = "novelty", Events = "events";

    private readonly WorldState _w;

    public ExperienceSystem(WorldState world) => _w = world;

    private MoodSettingsDefinition M => _w.Content.Mood;
    private MemorySettingsDefinition Mem => _w.Content.Memory;
    private long Now => _w.Clock.Tick;

    // ---------------- Sessao ----------------

    internal void OnSessionStart(Character ch) =>
        ch.SessionLedger.Reset(ch.XpFromActivities + ch.XpFromQuests, ch.Level);

    /// <summary>
    /// Fim de uma atividade: componentes da experiencia, impressao do lugar, novidade e
    /// memoria marcante quando a experiencia foi muito boa ou muito ruim.
    /// </summary>
    internal int OnActivity(Person p, Character ch, RegionDefinition region, ActivityDefinition a, int minutes,
        int fit, bool yieldsXp, int risk, bool firstTimeType)
    {
        if (minutes <= 0) return 0;
        var parts = Components(p.Traits, ch, region, a, fit, yieldsXp, risk, _w.OccupancyOf(region.Id, a.Id), M);
        int valence = 0;
        foreach (var (key, value) in parts)
        {
            valence += value;
            ch.SessionLedger.Sums[key] = ch.SessionLedger.Sums.GetValueOrDefault(key) + (long)value * minutes;
        }
        valence = Math.Clamp(valence, -1000, 1000);
        ch.SessionLedger.Minutes += minutes;

        string key2 = region.Id + "/" + a.Id;
        bool firstTimeHere = !p.Impressions.ContainsKey(key2);
        int before = p.Impressions.GetValueOrDefault(key2);
        p.Impressions[key2] = before + (valence - before) * Mem.ImpressionLearn / 1000;
        if (firstTimeType) AddNovelty(p, M.NoveltyNewActivityType);
        else if (firstTimeHere) AddNovelty(p, M.NoveltyNewActivity);

        if (valence >= Mem.NotableValence) Remember(p, "great_activity", key2, valence, valence);
        else if (valence <= -Mem.NotableValence) Remember(p, "awful_activity", key2, valence, -valence);
        return valence;
    }

    /// <summary>Componentes da experiencia de uma atividade (pontos da nota, +-).</summary>
    public static (string Key, int Value)[] Components(PersonTraits t, Character ch, RegionDefinition region,
        ActivityDefinition a, int fit, bool yieldsXp, int risk, int others, MoodSettingsDefinition m)
    {
        int cap = Math.Max(5, region.Capacity / Math.Max(1, region.AllActivities().Count()));
        bool disputed = a.Type is ActivityTypes.Hunt or ActivityTypes.Gather;
        int crowd = disputed ? Math.Min(1000, others * 1000 / cap) : 0;
        int company = a.Type == ActivityTypes.Socialize
            ? t.Sociability * Math.Min(others, 10) / 10
            : t.Socializing * Math.Min(others, 5) / 5 / 3;
        return new[]
        {
            (Fun, (ActivityScoring.Affinity(a.Type, t) - 500) * m.FunWeight / 1000),
            (Fit, yieldsXp ? (fit - 600) * m.FitWeight / 1000 : 0),
            (Crowding, -crowd * m.CrowdingWeight / 1000 * (1150 - t.Coordination * 3 / 10) / 1000),
            (Company, company * m.CompanyWeight / 1000),
            (Boredom, -ch.BoredomByType.GetValueOrDefault(a.Type) * m.BoredomWeight / 1000),
            (Risk, risk * (500 - t.RiskAversion) / 1000 * m.RiskWeight / 1000),
        };
    }

    /// <summary>Conquista: pontos na sessao, novidade e (se marcante) memoria.</summary>
    internal void OnAchievement(Person p, Character ch, string kind, string subject, int points, int novelty, int memoryIntensity)
    {
        ch.SessionLedger.Points += points;
        AddNovelty(p, novelty);
        if (memoryIntensity > 0) Remember(p, kind, subject, memoryIntensity, memoryIntensity);
    }

    /// <summary>Reves: pontos negativos, frustracao imediata e memoria.</summary>
    internal void OnSetback(Person p, Character ch, string kind, string subject, int points, int frustration, int memoryIntensity)
    {
        ch.SessionLedger.Points -= points;
        p.Mood.Frustration = Math.Clamp(p.Mood.Frustration + frustration, 0, 1000);
        if (memoryIntensity > 0) Remember(p, kind, subject, -memoryIntensity, memoryIntensity);
    }

    /// <summary>
    /// Fim da sessao: nota (0–1000) com os componentes medios, o progresso contra a
    /// expectativa, a novidade e os acontecimentos. Atualiza satisfacao, frustracao,
    /// expectativa, motivos acumulados e engajamento. Devolve a nota.
    /// </summary>
    internal int OnSessionEnd(Person p, Character ch, long now)
    {
        var L = ch.SessionLedger;
        var m = M;
        var mood = p.Mood;
        int minutes = (int)((now - ch.SessionStartTick) / SimTime.TicksPerMinute);
        if (minutes <= 0) return mood.LastSessionScore;

        mood.Novelty = Math.Max(0, mood.Novelty - minutes * m.NoveltyDecayPerHour / 60);
        var parts = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var kv in L.Sums) parts[kv.Key] = (int)(kv.Value / Math.Max(1, L.Minutes));
        parts[Progress] = ProgressTerm(p, ch, minutes) * ProgressCare(p.Traits, m) / 1000;
        parts[Novelty] = (mood.Novelty - 500) * m.NoveltyWeight / 1000 * NoveltyCare(p.Traits, m) / 1000;
        parts[Events] = Math.Clamp(L.Points, -m.EventClamp, m.EventClamp);
        int score = 500;
        foreach (var v in parts.Values) score += v;
        score = Math.Clamp(score, 0, 1000);

        int adapt = m.SatisfactionAdapt * Math.Min(minutes, m.FullWeightSessionMinutes) / Math.Max(1, m.FullWeightSessionMinutes);
        mood.Satisfaction = Math.Clamp(mood.Satisfaction + (score - mood.Satisfaction) * adapt / 1000, 0, 1000);
        if (score < m.BadSessionBelow) mood.Frustration += (m.BadSessionBelow - score) * m.FrustrationFromBadSession / 1000;
        else if (score > m.GoodSessionAbove) mood.Frustration -= (score - m.GoodSessionAbove) * m.FrustrationReliefFromGoodSession / 1000;
        mood.Frustration = Math.Clamp(mood.Frustration, 0, 1000);
        foreach (var kv in parts) p.Drivers[kv.Key] = p.Drivers.GetValueOrDefault(kv.Key) + kv.Value * adapt / 1000;
        mood.LastSessionScore = score;
        mood.SessionsRated++;
        UpdateEngagement(p, ch);

        if (score >= Mem.GreatSessionScore) Remember(p, "great_session", "", score - 500, Mem.SessionIntensity);
        else if (score <= Mem.BadSessionScore) Remember(p, "bad_session", "", score - 500, Mem.SessionIntensity);
        if (score >= Mem.GreatSessionScore || score <= Mem.BadSessionScore)
        {
            var causes = parts.Where(kv => kv.Value != 0)
                .OrderByDescending(kv => score >= Mem.GreatSessionScore ? kv.Value : -kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal)
                .Take(3).Select(kv => new EventCause(kv.Key, kv.Value)).ToArray();
            _w.Emit(score >= Mem.GreatSessionScore ? "session_great" : "session_bad", EventImportance.Low,
                new[] { new EntityRef("person", p.Id), new EntityRef("character", ch.Id) }, causes, score.ToString());
        }
        return score;
    }

    /// <summary>
    /// Progresso da sessao contra a expectativa (D-026): XP ganho / XP de referencia do nivel
    /// (permille). A expectativa se adapta aos poucos, com piso: progredir mal por dias nao vira
    /// "satisfatorio". No teto de nivel o termo e neutro (a novidade decide).
    /// </summary>
    private int ProgressTerm(Person p, Character ch, int minutes)
    {
        var L = ch.SessionLedger;
        var m = M;
        var prog = _w.Content.Progression;
        if (L.LevelAtStart >= prog.LevelCap) return 0;
        long refMilli = ProgressionRules.ReferenceMilliXpPerMinute(_w.Content, L.LevelAtStart);
        if (refMilli <= 0) return 0;
        long gained = ch.XpFromActivities + ch.XpFromQuests - L.XpAtStart;
        int ratio = (int)Math.Min(m.ProgressRatioCap, gained * 1_000_000 / (refMilli * minutes));
        var mood = p.Mood;
        mood.LastProgressRatio = ratio;
        int term = Math.Clamp((ratio - mood.ProgressExpectation) * m.ProgressWeight / 1000, -m.ProgressClamp, m.ProgressClamp);
        mood.ProgressExpectation = Math.Clamp(mood.ProgressExpectation + (ratio - mood.ProgressExpectation) * m.ExpectationAdapt / 1000,
            m.ExpectationFloor, m.ExpectationCeiling);
        return term;
    }

    /// <summary>
    /// Quanto a pessoa liga para evoluir o personagem (permille): PvE, competitividade e PvP.
    /// Quem joga para socializar ou negociar sente pouco a falta de XP (plano 4.4: pesos individuais).
    /// </summary>
    public static int ProgressCare(PersonTraits t, MoodSettingsDefinition m) =>
        Math.Clamp((t.Pve * m.ProgressCarePve + t.Aggressiveness * m.ProgressCareAggressiveness + t.Pvp * m.ProgressCarePvp) / 1000, 0, 1000);

    /// <summary>Quanto a pessoa precisa de coisas novas (permille): exploracao e impaciencia.</summary>
    public static int NoveltyCare(PersonTraits t, MoodSettingsDefinition m) =>
        Math.Clamp(m.NoveltyCareBase + (t.Exploration * m.NoveltyCareExploration + (1000 - t.Patience) * m.NoveltyCareImpatience) / 1000, 0, 1000);

    /// <summary>Expectativa inicial de progresso: competitivos esperam mais (permille da referencia).</summary>
    public static int InitialExpectation(PersonTraits t, MoodSettingsDefinition m) =>
        Math.Clamp(m.ExpectationBase + t.Aggressiveness * m.ExpectationPerAggressiveness / 1000, m.ExpectationFloor, m.ExpectationCeiling);

    /// <summary>
    /// Engajamento (vontade de jogar) e intencao de continuar: satisfacao, frustracao e
    /// novidade; a lealdade e o investimento (nivel) seguram quem esta insatisfeito.
    /// </summary>
    internal void UpdateEngagement(Person p, Character? ch)
    {
        var m = M;
        var mood = p.Mood;
        mood.Engagement = Math.Clamp((mood.Satisfaction * m.EngagementSatisfaction + (1000 - mood.Frustration) * m.EngagementCalm
            + mood.Novelty * m.EngagementNovelty) / Math.Max(1, m.EngagementSatisfaction + m.EngagementCalm + m.EngagementNovelty), 0, 1000);
        int investment = ch == null ? 0 : Math.Min(m.InvestmentCap, ch.Level * m.InvestmentPerLevel);
        mood.ReturnIntention = Math.Clamp(mood.Engagement + (p.Traits.Loyalty - 500) * m.LoyaltyWeight / 1000 + investment, 0, 1000);
    }

    /// <summary>Meia-noite: frustracao, motivos, impressoes e memorias perdem forca.</summary>
    internal void OnDaily(Person p, Character? ch)
    {
        var m = M;
        var mood = p.Mood;
        mood.Frustration = Math.Max(0, mood.Frustration - m.FrustrationDecayPerDay);
        foreach (var k in p.Drivers.Keys.ToList())
        {
            int v = p.Drivers[k] * (1000 - m.DriverDecayPerDay) / 1000;
            if (v == 0) p.Drivers.Remove(k); else p.Drivers[k] = v;
        }
        foreach (var k in p.Impressions.Keys.ToList())
        {
            int v = p.Impressions[k];
            p.Impressions[k] = v * (1000 - (v > 0 ? Mem.ImpressionDecayPositivePerDay : Mem.ImpressionDecayNegativePerDay)) / 1000;
        }
        for (int i = p.MemoryList.Count - 1; i >= 0; i--)
        {
            var mem = p.MemoryList[i];
            mem.Intensity -= mem.Valence >= 0 ? Mem.PositiveDecayPerDay : Mem.NegativeDecayPerDay;
            if (mem.Intensity <= 0) p.MemoryList.RemoveAt(i);
        }
        UpdateEngagement(p, ch);
    }

    private void AddNovelty(Person p, int amount)
    {
        if (amount > 0) p.Mood.Novelty = Math.Min(1000, p.Mood.Novelty + amount);
    }

    /// <summary>
    /// Guarda um acontecimento marcante. O mesmo tipo no mesmo lugar se reforca; com a memoria
    /// cheia, sai o de menor intensidade atual (empate: o mais antigo). Ordem deterministica.
    /// </summary>
    internal void Remember(Person p, string kind, string subject, int valence, int intensity)
    {
        intensity = Math.Clamp(intensity, 1, 1000);
        foreach (var mem in p.MemoryList)
            if (mem.Kind == kind && mem.Subject == subject)
            {
                mem.Intensity = Math.Max(mem.Intensity, intensity);
                mem.Valence = valence;
                mem.Tick = Now;
                mem.Count++;
                return;
            }
        p.MemoryList.Add(new Memory(kind, subject, valence, intensity, Now));
        if (p.MemoryList.Count <= Mem.MaxMemories) return;
        var weakest = p.MemoryList.OrderBy(x => x.Intensity).ThenBy(x => x.Tick).ThenBy(x => x.Kind, StringComparer.Ordinal)
            .ThenBy(x => x.Subject, StringComparer.Ordinal).First();
        p.MemoryList.Remove(weakest);
    }
}

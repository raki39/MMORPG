using Simulation.Core.Content;
using Simulation.Core.Population;
using Simulation.Core.Random;

namespace Simulation.Core.Behavior;

/// <summary>Opcao avaliada: atividade oferecida por um local conhecido, com fatores da nota.</summary>
public sealed class ScoredOption
{
    public ScoredOption(RegionDefinition region, ActivityDefinition activity, TravelPlan plan, int utility, List<DecisionFactor> factors)
    {
        Region = region;
        Activity = activity;
        Plan = plan;
        Utility = utility;
        Factors = factors;
    }

    public RegionDefinition Region { get; }
    public ActivityDefinition Activity { get; }
    /// <summary>Caminho ate o ponto da atividade (portoes e trechos dentro das zonas).</summary>
    public TravelPlan Plan { get; }
    public int TravelMinutes => Plan.Minutes;
    public int Utility { get; }
    public List<DecisionFactor> Factors { get; }
    public string Key => Region.Id + "/" + Activity.Id;
}

/// <summary>
/// Decisao por utilidade (plano 4.4 e 4.4.1). So pontua atividades oferecidas por locais
/// que o personagem conhece e cujos requisitos ele cumpre. Termos dependem do estado do
/// mundo: lotacao atual, viagem a partir de onde esta, tempo restante da sessao.
///
/// U = afinidade + recompensa + social - risco - viagem - lotacao - tedio - tempo + ruido
/// (cada termo normalizado em permille e multiplicado pelo peso de population.json, secao "behavior").
/// </summary>
public static class ActivityScoring
{
    /// <summary>Tipos fora das decisoes individuais ate existirem grupos (M6).</summary>
    private static readonly string[] NeedsGroupSystem = { ActivityTypes.Recruit };

    public static List<ScoredOption> Evaluate(Character ch, PersonTraits t, PersonPreferences pr, ContentDatabase content, RouteFinder routes,
        Func<string, string, int> occupancy, long now, DeterministicRng rng, QuestSystem? quests = null, GoalSystem? goals = null,
        IReadOnlyDictionary<string, int>? impressions = null, PersonalityTypeDefinition? personality = null, Person? person = null)
    {
        bool firstDecision = ch.LastDecision == null || ch.LastDecision.Tick < ch.SessionStartTick;
        // Traits da pessoa (P4c): avaliadas uma vez por decisao.
        var fx = content.QuirkCatalog.Effects;
        bool collector = person != null && QuirkSystem.Active(content, person, QuirkIds.Colecionador);
        bool optimizer = person != null && QuirkSystem.Active(content, person, QuirkIds.Otimizador);
        int noisePermille = 1000;
        if (person != null && QuirkSystem.Active(content, person, QuirkIds.Impulsivo)) noisePermille = noisePermille * fx.Impulsivo.NoisePermille / 1000;
        if (person != null && QuirkSystem.Active(content, person, QuirkIds.Indeciso)) noisePermille = noisePermille * fx.Indeciso.NoisePermille / 1000;
        var options = new List<ScoredOption>();
        int remaining = (int)Math.Max(0, (ch.SessionEndTick - now) / Time.SimTime.TicksPerMinute);
        int moveSpeed = Combat.CharacterStats.MoveSpeed(content, ch);

        foreach (var regionId in ch.KnownRegions)
        {
            var region = content.Regions[regionId];
            if (routes.ZonePath(ch.RegionId, regionId) == null) continue;
            int perActivityCapacity = Math.Max(5, region.Capacity / Math.Max(1, region.AllActivities().Count()));

            foreach (var a in region.AllActivities())
            {
                if (a.MinGroupSize > 1 || NeedsGroupSystem.Contains(a.Type)) continue;
                if (a.MinLevel > ch.Level) continue;
                if (a.ClassIds.Count > 0 && !a.ClassIds.Contains(ch.ClassId)) continue;
                if (a.RequiredItemIds.Count > 0) continue; // inventario chega no M5

                // Quests (D-031): o quadro so e opcao com quest para aceitar ou entregar; ponto de
                // missao so com etapa de visita pendente; atividade que avanca etapa ganha peso.
                (string Key, int Value)? questTerm = null;
                if (quests != null)
                {
                    var board = quests.BoardAt(region.Id, a.Id);
                    if (board != null)
                    {
                        int ready = quests.ReadyCount(ch, board);
                        // Independente (P4a): so contam as ofertas que ele aceitaria (sem ida ao quadro a toa).
                        bool ownPath = personality?.Rule == PersonalityRuleIds.OwnPath;
                        bool offer = quests.CanAccept(ch) && quests.Offers(ch, board).Any(q => !ownPath || quests.FitsTaste(pr, q));
                        if (ready == 0 && !offer) continue;
                        questTerm = ready > 0
                            ? ("quest_deliver", content.Behavior.QuestDeliverWeight + (ready - 1) * content.Behavior.QuestDeliverPerExtra)
                            : ("quest_offer", content.Behavior.QuestOfferWeight);
                    }
                    else if (a.Type == ActivityTypes.Quest)
                    {
                        if (!quests.AdvancesStep(ch, region.Id, a.Id)) continue;
                        questTerm = ("quest_step", content.Behavior.QuestStepWeight);
                    }
                    else if (quests.AdvancesStep(ch, region.Id, a.Id))
                        questTerm = ("quest_step", content.Behavior.QuestStepWeight);
                }

                // Caminho real: ate o portao, pelas zonas, e ate o ponto da atividade (fora da estrada).
                var plan = routes.Plan(ch.Position, ch.RegionId, region.Id, routes.Layout.Spot(region.Id, a.Id), moveSpeed);
                if (!plan.Reachable) continue;
                int travel = plan.Minutes;

                var f = new List<DecisionFactor>();
                // Gosto (Preferencias) e o reforco minimo da especialidade, como fatores separados.
                int affinity = Affinity(a.Type, pr, t);
                int affinityTerm = affinity * content.Behavior.WeightAffinity / 1000;
                f.Add(new DecisionFactor("affinity", affinityTerm));
                if (SpecialtyBonus(a.Type, t, content.Behavior) is > 0 and var spec)
                    f.Add(new DecisionFactor("specialty", spec * content.Behavior.WeightAffinity / 1000));
                // Explorar sem nada novo ao alcance perde parte da graca (D-037): o gosto de explorar
                // e descobrir; terreno ja conhecido so rende XP.
                if (a.Type == ActivityTypes.Explore && !ExploreRevealsSomething(ch, region, content))
                    f.Add(new DecisionFactor("nothing_new", -affinity * content.Behavior.WeightAffinity / 1000 * content.Session.ExploreKnownPenalty / 1000));

                // Adequacao ao nivel pelo desafio da propria atividade (D-027), nao pela faixa da zona.
                int challenge = ProgressionRules.ChallengeLevel(content, a);
                bool yieldsXp = ProgressionRules.MilliXpPerMinute(content, a, ch.Level) > 0;
                int fit = yieldsXp ? ProgressionRules.Efficiency(content.Progression, ch.Level, challenge) : 0;
                if (yieldsXp)
                {
                    f.Add(new DecisionFactor("level_fit", (fit - 500) * content.Behavior.WeightReward / 1000));
                    // Otimizador (P4c): o rendimento pesa mais (o gosto, menos, logo abaixo).
                    if (optimizer) f.Add(new DecisionFactor(QuirkIds.Factor(QuirkIds.Otimizador), (fit - 500) * content.Behavior.WeightReward / 1000 * (fx.Otimizador.RewardPermille - 1000) / 1000));
                    // Gosto por desafio acima do nivel (D-036a): competitividade, confianca
                    // (mecanica) e pouca aversao a risco.
                    if (challenge > ch.Level)
                    {
                        f.Add(new DecisionFactor("challenge", ChallengeAppetite(t) * Math.Min(3, challenge - ch.Level) * content.Dynamics.ChallengeWeightPerLevel / 1000));
                    }
                }

                int count = occupancy(region.Id, a.Id);
                int crowd = Math.Min(1000, count * 1000 / perActivityCapacity);
                int crowdingContribution = 0;
                if (a.Type == ActivityTypes.Socialize)
                    f.Add(new DecisionFactor("company", t.Sociability * Math.Min(count, 10) / 10 * content.Behavior.WeightSocial / 1000));
                else if (a.Type == ActivityTypes.Hunt || a.Type == ActivityTypes.Gather)
                {
                    // Coordenacao divide o lugar melhor; oportunismo foge da disputa (D-036).
                    int crowdWeight = content.Behavior.WeightCrowding * (1150 - t.Coordination * 3 / 10) / 1000 * (900 + t.Opportunism * 2 / 10) / 1000;
                    crowdingContribution = -crowd * crowdWeight / 1000;
                    f.Add(new DecisionFactor("crowding", crowdingContribution));
                }
                else if (count > 0)
                    f.Add(new DecisionFactor("company", pr.Social * Math.Min(count, 5) / 5 * content.Behavior.WeightSocial / 3000));

                // Risco percebido: mecanica alta da confianca (D-036).
                int risk = RiskOf(region, a, challenge, ch.Level) * (1150 - t.MechanicalSkill * 3 / 10) / 1000;
                if (risk > 0)
                    f.Add(new DecisionFactor("risk", -risk * t.RiskAversion / 1000 * content.Behavior.WeightRisk / 1000));
                // Destemido (P4a): o risco da primeira faixa acima do nivel nao pesa (devolvido no fator da personalidade).
                int fearlessRelief = 0;
                if (personality?.Rule == PersonalityRuleIds.Fearless && challenge > ch.Level && risk > 0)
                {
                    int band = content.Personality.Rules.FearlessBandLevels;
                    int riskAlt = RiskOf(region, a, Math.Max(ch.Level, challenge - band), ch.Level) * (1150 - t.MechanicalSkill * 3 / 10) / 1000;
                    fearlessRelief = (risk - riskAlt) * t.RiskAversion / 1000 * content.Behavior.WeightRisk / 1000;
                }

                if (travel > 0)
                {
                    // Conhecimento (D-036a): estimativa do tempo de viagem mais precisa; o custo real
                    // do tempo e o mesmo para todos (quem conhece pouco erra para mais ou para menos).
                    int errMax = TravelErrorPermille(t, content.Dynamics);
                    int estimate = travel * (1000 + rng.NextIntInclusive(-errMax, errMax)) / 1000;
                    int travelCost = Math.Min(1000, estimate * 1000 / Math.Max(30, remaining));
                    f.Add(new DecisionFactor("travel", -travelCost * content.Behavior.WeightTravel / 1000));
                }
                if (travel + a.TypicalMinutes > remaining + content.Behavior.LogoutToleranceMinutes)
                    f.Add(new DecisionFactor("time_budget", -content.Behavior.TimeBudgetPenalty));

                // Sessao interrompivel (D-044: pausa ou escapada no trabalho): o custo de largar
                // a atividade no meio pesa; comercio e coleta atraem mais que um chefe demorado.
                if (ch.SessionKind != 0)
                {
                    int cost = content.Routine.InterruptCost.TryGetValue(a.Type, out int ic) ? ic : 500;
                    if (cost > 0) f.Add(new DecisionFactor("interruption", -cost * content.Routine.InterruptWeight / 1000));
                }

                // Tedio contextual (D-037): cansaco do tipo de atividade, aliviado pela paciencia.
                if (BoredomPenalty(ch, t, a.Type, content.Session) is > 0 and var bored)
                    f.Add(new DecisionFactor("boredom", -bored));

                if (questTerm is { } qt) f.Add(new DecisionFactor(qt.Key, qt.Value));

                // Familiaridade (D-036a): zona onde ja passou tempo atrai; quem prefere variedade
                // sente menos essa atracao (D-039a).
                if (FamiliarityPull(ch, region.Id, t, content.Dynamics) is > 0 and var pull)
                    f.Add(new DecisionFactor("familiarity", pull * content.Dynamics.FamiliarityWeight / 1000));

                // Memoria (D-038): a impressao deixada pelo lugar atrai ou afasta.
                if (impressions != null && impressions.TryGetValue(region.Id + "/" + a.Id, out int imp)
                    && Math.Abs(imp) >= content.Memory.ImpressionMinAbs)
                    f.Add(new DecisionFactor("memory", imp * content.Memory.DecisionWeight / 1000));

                // Objetivos pessoais: influenciam com o peso do comprometimento, sem obrigar (D-036).
                bool delivers = questTerm is { Key: "quest_deliver" };
                if (goals != null && goals.Bonus(ch, region, a, yieldsXp, fit, delivers) is > 0 and var gb)
                    f.Add(new DecisionFactor("goal", gb));

                // Personalidade principal (P4a): regra propria, separada dos efeitos graduais dos atributos.
                if (personality != null)
                {
                    bool needed = questTerm != null || (goals != null && goals.Bonus(ch, region, a, yieldsXp, fit, delivers) > 0);
                    var ctx = new PersonalityContext(a.Type, region.Pvp == PvpRules.Open, needed, count, crowd, crowdingContribution,
                        questTerm is { Key: "quest_step" }, region.Id + "/" + a.Id == ch.LastActivityKey, ch.RepeatMinutes, firstDecision, remaining,
                        challenge, ch.Level, fearlessRelief);
                    if (PersonalityRule(personality, content.Personality.Rules, ctx) is not 0 and var pv)
                        f.Add(new DecisionFactor("personality", pv));
                }

                // Progressao inicial (D-055): prioridade ate o nivel-alvo, igual para todos. Conta uma
                // vez por opcao (o maior entre rendimento de XP adequado e missao disponivel), so se a
                // opcao cabe na sessao, e nao soma com metas de progressao (na opcao vale o maior).
                bool fitsSession = travel + a.TypicalMinutes <= remaining + content.Behavior.LogoutToleranceMinutes;
                int early = EarlyProgressionValue(ch.Level, content.EarlyProgression, yieldsXp, fit, questTerm != null, fitsSession);
                if (early > 0)
                {
                    int progressGoals = goals?.ProgressionBonus(ch, region, a, yieldsXp, fit, delivers) ?? 0;
                    if (early > progressGoals) f.Add(new DecisionFactor("early_progression", early - progressGoals));
                }

                // Ruido: quem conhece o jogo decide de forma mais consistente (D-036).
                // Otimizador (P4c): o gosto pesa menos na escolha.
                if (optimizer && affinityTerm != 0) f.Add(new DecisionFactor(QuirkIds.Factor(QuirkIds.Otimizador) + "_gosto", affinityTerm * (fx.Otimizador.AffinityPermille - 1000) / 1000));
                // Colecionador (P4c): atividade nunca feita e zona nunca visitada valem mais.
                if (collector)
                {
                    int bonus = (impressions == null || !impressions.ContainsKey(region.Id + "/" + a.Id) ? fx.Colecionador.NewActivityBonus : 0)
                        + (!ch.ZoneMinutes.ContainsKey(region.Id) ? fx.Colecionador.NewZoneBonus : 0);
                    if (bonus > 0) f.Add(new DecisionFactor(QuirkIds.Factor(QuirkIds.Colecionador), bonus));
                }
                int amp = content.Behavior.Noise * (1400 - t.GameKnowledge * 8 / 10) / 1000 * noisePermille / 1000;   // Impulsivo e Indeciso (P4c)
                int noise = rng.NextIntInclusive(-amp, amp);
                f.Add(new DecisionFactor("noise", noise));

                int utility = 0;
                foreach (var x in f) utility += x.Contribution;
                options.Add(new ScoredOption(region, a, plan, utility, f));
            }
        }

        // Ordem total e estavel: maior utilidade; desempate por chave ordinal.
        options.Sort((x, y) =>
        {
            int c = y.Utility.CompareTo(x.Utility);
            return c != 0 ? c : string.CompareOrdinal(x.Key, y.Key);
        });
        return options;
    }

    /// <summary>Situacao de uma opcao que as regras de personalidade leem.</summary>
    public readonly record struct PersonalityContext(string Type, bool OpenPvpZone, bool Needed, int Count, int CrowdPermille,
        int CrowdingContribution, bool QuestStep, bool SameAsLast, int RepeatMinutes, bool FirstDecision, int RemainingMinutes,
        int Challenge, int Level, int FearlessRelief);

    /// <summary>
    /// Regra da personalidade principal para uma opcao (fator "personality"). Sao regras
    /// categoricas (nunca, sempre, comeca por), separadas do efeito gradual do atributo, e nunca
    /// tiram opcoes: o "nunca" e uma penalidade grande, entao a opcao so vence se nao houver outra
    /// valida. Regras sem efeito na nota (Paciente no plano, Leal nas metas, Independente nas
    /// missoes) agem nos seus proprios sistemas.
    /// </summary>
    public static int PersonalityRule(PersonalityTypeDefinition? p, PersonalityRuleSettingsDefinition r, in PersonalityContext x)
    {
        if (p == null) return 0;
        bool pvpActivity = x.Type is ActivityTypes.Ambush or ActivityTypes.Patrol;
        return p.Rule switch
        {
            // Pacifico: nunca inicia PvP; evita zona de PvP aberto, salvo se uma missao ou meta pedir.
            PersonalityRuleIds.AvoidPvp => pvpActivity ? -r.NeverPenalty : x.OpenPvpZone && !x.Needed ? -r.PvpZonePenalty : 0,
            // Agressivo: vai atras de confronto e disputa lugar cheio na caca e na coleta.
            PersonalityRuleIds.SeekConflict => (pvpActivity ? r.ConflictBonus : 0)
                + (x.Type is ActivityTypes.Hunt or ActivityTypes.Gather && x.CrowdingContribution < 0 ? -x.CrowdingContribution * r.CrowdReliefPermille / 1000 : 0),
            // Sociavel: comeca e termina a sessao onde ha gente.
            PersonalityRuleIds.SocialBookends => (x.FirstDecision || x.RemainingMinutes <= r.SocialEndMinutes) && x.Count >= r.SocialMinPeople ? r.SocialBonus : 0,
            // Reservado: nao entra em atividade lotada.
            PersonalityRuleIds.AvoidCrowds => x.CrowdPermille >= r.CrowdLimitPermille ? -r.NeverPenalty : 0,
            // Paciente: fecha as etapas de missao antes de outras coisas (o plano fica no Decide).
            PersonalityRuleIds.FinishPlan => x.QuestStep ? r.QuestStepBonus : 0,
            // Inquieto: depois de um tempo seguido na mesma atividade (minutos, na sessao), procura outra.
            PersonalityRuleIds.Restless => x.SameAsLast && x.RepeatMinutes >= r.RestlessMinutes ? -r.RestlessPenalty : 0,
            // Cauteloso: nunca entra em conteudo acima do nivel + margem.
            PersonalityRuleIds.NoOverlevel => x.Challenge > 0 && x.Challenge > x.Level + r.MaxLevelsAbove ? -r.NeverPenalty : 0,
            // Destemido: a primeira faixa de risco acima do nivel nao pesa.
            PersonalityRuleIds.Fearless => x.FearlessRelief,
            // Versatil e Metodico: inicio de sessao diferente ou igual ao da sessao anterior.
            PersonalityRuleIds.VaryStart => x.FirstDecision && x.SameAsLast ? -r.SessionStartBonus : 0,
            PersonalityRuleIds.RepeatStart => x.FirstDecision && x.SameAsLast ? r.SessionStartBonus : 0,
            _ => 0,
        };
    }

    /// <summary>
    /// Incentivo de progressao inicial de uma opcao (D-055): peso x reducao pelo nivel x
    /// contribuicao, onde a contribuicao e o rendimento de XP (se ao menos MinFitPermille) ou 1000
    /// para opcao de missao (oferta, etapa ou entrega), o maior dos dois. Zero desligado, no
    /// nivel-alvo ou acima, ou se a opcao nao cabe na sessao.
    /// </summary>
    public static int EarlyProgressionValue(int level, EarlyProgressionDefinition e, bool yieldsXp, int fit, bool quest, bool fitsSession)
    {
        if (!e.Enabled || level >= e.TargetLevel || !fitsSession) return 0;
        int contribution = Math.Max(yieldsXp && fit >= e.MinFitPermille ? fit : 0, quest ? 1000 : 0);
        return e.Weight * EarlyDecay(e, level) / 1000 * contribution / 1000;
    }

    /// <summary>Reducao pelo nivel (permille): pleno ate FullUntilLevel, linear ate zero no nivel-alvo.</summary>
    public static int EarlyDecay(EarlyProgressionDefinition e, int level)
    {
        if (level >= e.TargetLevel) return 0;
        if (level <= e.FullUntilLevel) return 1000;
        return 1000 * (e.TargetLevel - level) / (e.TargetLevel - e.FullUntilLevel);
    }

    /// <summary>
    /// Gosto por desafio acima do nivel (permille, D-036a): competitividade (40%), confianca na
    /// propria mecanica (30%) e pouca aversao a risco (30%). A agressividade nao entra (ela e
    /// confronto: tanque, emboscada, PvP), so a vontade de superar.
    /// </summary>
    public static int ChallengeAppetite(PersonTraits t) =>
        (t.Competitiveness * 4 + t.MechanicalSkill * 3 + (1000 - t.RiskAversion) * 3) / 10;

    /// <summary>
    /// Atracao pela zona onde ja passou tempo (permille, D-036a): cresce com os minutos ali ate
    /// a familiaridade total e e menor para quem prefere variedade (D-039a).
    /// </summary>
    public static int FamiliarityPull(Character ch, string regionId, PersonTraits t, TraitDynamicsDefinition d)
    {
        if (!ch.ZoneMinutesSpent.TryGetValue(regionId, out int minutes) || minutes <= 0) return 0;
        int fam = Math.Min(1000, minutes * 1000 / Math.Max(1, d.FamiliarityFullMinutes));
        return fam * (1000 - t.Variety / 2) / 1000;
    }

    /// <summary>Explorar a zona ainda revelaria alguma zona desconhecida (vizinhas das vizinhas)?</summary>
    public static bool ExploreRevealsSomething(Character ch, RegionDefinition region, ContentDatabase content) =>
        region.Connections.Any(c => content.Regions[c.To].Connections.Any(c2 => !ch.KnownRegions.Contains(c2.To)));

    /// <summary>Peso do tedio do tipo na nota (positivo = penalidade), D-037.</summary>
    public static int BoredomPenalty(Character ch, PersonTraits t, string type, SessionSettingsDefinition s)
    {
        if (!ch.BoredomByType.TryGetValue(type, out int bored) || bored <= 0) return 0;
        return bored * s.BoredomWeight / 1000 * (1000 - t.Patience * s.PatienceRelief / 1000) / 1000;
    }

    /// <summary>Erro maximo (+-, permille) da estimativa do tempo de viagem: some com conhecimento alto.</summary>
    public static int TravelErrorPermille(PersonTraits t, TraitDynamicsDefinition d) =>
        d.TravelEstimateErrorAtZero * (1000 - t.GameKnowledge) / 1000;

    /// <summary>Risco bruto da atividade (permille): base, PvP aberto e desafio acima do nivel.</summary>
    public static int RiskOf(RegionDefinition region, ActivityDefinition a, int challenge, int level) =>
        Math.Min(1000, a.BaseRiskPermille + (region.Pvp == PvpRules.Open ? 250 : 0) + Math.Max(0, challenge - level) * 120);

    /// <summary>
    /// Rendimento de XP pelos tracos (permille, D-036): conhecimento do jogo em tudo
    /// (0,95–1,05) e mecanica nas atividades de combate (0,88–1,12).
    /// </summary>
    public static int TraitXpPermille(PersonTraits t, string type)
    {
        int k = 950 + t.GameKnowledge / 10;
        bool combat = type is ActivityTypes.Hunt or ActivityTypes.Boss or ActivityTypes.Patrol or ActivityTypes.Ambush;
        return combat ? k * (880 + t.MechanicalSkill * 24 / 100) / 1000 : k;
    }

    /// <summary>
    /// Quanto a pessoa gosta do tipo de atividade (permille). O gosto vem das Preferencias; no PvP
    /// a personalidade e a conduta completam (patrulha: lealdade e pouco oportunismo; emboscada:
    /// agressividade e oportunismo); no treino pesa a necessidade de aprender. Coleta tem
    /// preferencia propria, separada do comercio e da fabricacao.
    /// </summary>
    public static int Affinity(string type, PersonPreferences pr, PersonTraits t) => type switch
    {
        ActivityTypes.Hunt => pr.Pve,
        ActivityTypes.Quest => (pr.Pve * 6 + pr.Exploration * 4) / 10,
        ActivityTypes.Explore => pr.Exploration,
        ActivityTypes.Gather => pr.Gathering,
        ActivityTypes.Trade => pr.Trade,
        ActivityTypes.Socialize => pr.Social,
        ActivityTypes.Train => ((1000 - t.GameKnowledge) * 5 + pr.Pve * 2) / 10,
        ActivityTypes.Repair => 150,
        ActivityTypes.Patrol => (pr.Pvp * 5 + t.Loyalty * 3 + (1000 - t.Opportunism) * 2) / 10,
        ActivityTypes.Ambush => (pr.Pvp * 5 + t.Aggressiveness * 3 + t.Opportunism * 2) / 10,
        ActivityTypes.Boss => pr.Pve,
        _ => 200,
    };

    /// <summary>
    /// Especialidade ligada ao tipo de atividade (afinidade desenvolvida); null quando o tipo nao
    /// tem uma (coleta, missao, treino, reparo). A mesma ligacao decide o que a pratica faz crescer.
    /// </summary>
    public static string? SpecialtyOf(string type) => type switch
    {
        ActivityTypes.Hunt or ActivityTypes.Boss => "pve",
        ActivityTypes.Trade => "trade",
        ActivityTypes.Explore => "exploration",
        ActivityTypes.Socialize => "socializing",
        ActivityTypes.Patrol or ActivityTypes.Ambush => "pvp",
        _ => null,
    };

    /// <summary>
    /// Reforco minimo da especialidade no gosto pela atividade (escala 0..1000 da afinidade):
    /// especialidade x percentual, com teto, ambos do conteudo. Aparece como fator "specialty".
    /// </summary>
    public static int SpecialtyBonus(string type, PersonTraits t, BehaviorSettingsDefinition b) =>
        SpecialtyOf(type) is { } key ? Math.Min(b.SpecialtyAffinityCap, t.Get(key) * b.SpecialtyAffinityPermille / 1000) : 0;
}

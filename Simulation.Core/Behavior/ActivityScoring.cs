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
/// (cada termo normalizado em permille e multiplicado pelo peso de BehaviorTuning).
/// </summary>
public static class ActivityScoring
{
    /// <summary>Tipos fora das decisoes individuais ate existirem grupos (M6).</summary>
    private static readonly string[] NeedsGroupSystem = { ActivityTypes.Recruit };

    public static List<ScoredOption> Evaluate(Character ch, PersonTraits t, ContentDatabase content, RouteFinder routes,
        Func<string, string, int> occupancy, long now, DeterministicRng rng, QuestSystem? quests = null, GoalSystem? goals = null,
        IReadOnlyDictionary<string, int>? impressions = null)
    {
        var options = new List<ScoredOption>();
        int remaining = (int)Math.Max(0, (ch.SessionEndTick - now) / Time.SimTime.TicksPerMinute);

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
                        bool offer = quests.CanAccept(ch) && quests.Offers(ch, board).Any();
                        if (ready == 0 && !offer) continue;
                        questTerm = ready > 0
                            ? ("quest_deliver", BehaviorTuning.QuestDeliverWeight + (ready - 1) * BehaviorTuning.QuestDeliverPerExtra)
                            : ("quest_offer", BehaviorTuning.QuestOfferWeight);
                    }
                    else if (a.Type == ActivityTypes.Quest)
                    {
                        if (!quests.AdvancesStep(ch, region.Id, a.Id)) continue;
                        questTerm = ("quest_step", BehaviorTuning.QuestStepWeight);
                    }
                    else if (quests.AdvancesStep(ch, region.Id, a.Id))
                        questTerm = ("quest_step", BehaviorTuning.QuestStepWeight);
                }

                // Caminho real: ate o portao, pelas zonas, e ate o ponto da atividade (fora da estrada).
                var plan = routes.Plan(ch.Position, ch.RegionId, region.Id, routes.Layout.Spot(region.Id, a.Id));
                if (!plan.Reachable) continue;
                int travel = plan.Minutes;

                var f = new List<DecisionFactor>();
                int affinity = Affinity(a.Type, t);
                f.Add(new DecisionFactor("affinity", affinity * BehaviorTuning.WeightAffinity / 1000));
                // Explorar sem nada novo ao alcance perde parte da graca (D-037): o gosto de explorar
                // e descobrir; terreno ja conhecido so rende XP.
                if (a.Type == ActivityTypes.Explore && !ExploreRevealsSomething(ch, region, content))
                    f.Add(new DecisionFactor("nothing_new", -affinity * BehaviorTuning.WeightAffinity / 1000 * content.Session.ExploreKnownPenalty / 1000));

                // Adequacao ao nivel pelo desafio da propria atividade (D-027), nao pela faixa da zona.
                int challenge = ProgressionRules.ChallengeLevel(content, a);
                bool yieldsXp = ProgressionRules.MilliXpPerMinute(content, a, ch.Level) > 0;
                int fit = yieldsXp ? ProgressionRules.Efficiency(content.Progression, ch.Level, challenge) : 0;
                if (yieldsXp)
                {
                    f.Add(new DecisionFactor("level_fit", (fit - 500) * BehaviorTuning.WeightReward / 1000));
                    // Gosto por desafio acima do nivel (D-036a): competitividade (agressividade),
                    // confianca (mecanica) e pouca aversao a risco.
                    if (challenge > ch.Level)
                    {
                        f.Add(new DecisionFactor("challenge", ChallengeAppetite(t) * Math.Min(3, challenge - ch.Level) * content.Dynamics.ChallengeWeightPerLevel / 1000));
                    }
                }

                int count = occupancy(region.Id, a.Id);
                int crowd = Math.Min(1000, count * 1000 / perActivityCapacity);
                if (a.Type == ActivityTypes.Socialize)
                    f.Add(new DecisionFactor("company", t.Sociability * Math.Min(count, 10) / 10 * BehaviorTuning.WeightSocial / 1000));
                else if (a.Type == ActivityTypes.Hunt || a.Type == ActivityTypes.Gather)
                {
                    // Coordenacao divide o lugar melhor; oportunismo foge da disputa (D-036).
                    int crowdWeight = BehaviorTuning.WeightCrowding * (1150 - t.Coordination * 3 / 10) / 1000 * (900 + t.Opportunism * 2 / 10) / 1000;
                    f.Add(new DecisionFactor("crowding", -crowd * crowdWeight / 1000));
                }
                else if (count > 0)
                    f.Add(new DecisionFactor("company", t.Socializing * Math.Min(count, 5) / 5 * BehaviorTuning.WeightSocial / 3000));

                // Risco percebido: mecanica alta da confianca (D-036).
                int risk = RiskOf(region, a, challenge, ch.Level) * (1150 - t.MechanicalSkill * 3 / 10) / 1000;
                if (risk > 0)
                    f.Add(new DecisionFactor("risk", -risk * t.RiskAversion / 1000 * BehaviorTuning.WeightRisk / 1000));

                if (travel > 0)
                {
                    // Conhecimento (D-036a): estimativa do tempo de viagem mais precisa; o custo real
                    // do tempo e o mesmo para todos (quem conhece pouco erra para mais ou para menos).
                    int errMax = TravelErrorPermille(t, content.Dynamics);
                    int estimate = travel * (1000 + rng.NextIntInclusive(-errMax, errMax)) / 1000;
                    int travelCost = Math.Min(1000, estimate * 1000 / Math.Max(30, remaining));
                    f.Add(new DecisionFactor("travel", -travelCost * BehaviorTuning.WeightTravel / 1000));
                }
                if (travel + a.TypicalMinutes > remaining + BehaviorTuning.LogoutToleranceMinutes)
                    f.Add(new DecisionFactor("time_budget", -BehaviorTuning.TimeBudgetPenalty));

                // Tedio contextual (D-037): cansaco do tipo de atividade, aliviado pela paciencia.
                if (BoredomPenalty(ch, t, a.Type, content.Session) is > 0 and var bored)
                    f.Add(new DecisionFactor("boredom", -bored));

                if (questTerm is { } qt) f.Add(new DecisionFactor(qt.Key, qt.Value));

                // Familiaridade (D-036a): zona onde ja passou tempo atrai; quem gosta de variedade
                // (exploracao alta) sente menos essa atracao.
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

                // Ruido: quem conhece o jogo decide de forma mais consistente (D-036).
                int amp = BehaviorTuning.Noise * (1400 - t.GameKnowledge * 8 / 10) / 1000;
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

    /// <summary>
    /// Gosto por desafio acima do nivel (permille, D-036a): competitividade (agressividade 40%),
    /// confianca na propria mecanica (30%) e pouca aversao a risco (30%).
    /// </summary>
    public static int ChallengeAppetite(PersonTraits t) =>
        (t.Aggressiveness * 4 + t.MechanicalSkill * 3 + (1000 - t.RiskAversion) * 3) / 10;

    /// <summary>
    /// Atracao pela zona onde ja passou tempo (permille, D-036a): cresce com os minutos ali ate
    /// a familiaridade total e e menor para quem gosta de variedade (exploracao).
    /// </summary>
    public static int FamiliarityPull(Character ch, string regionId, PersonTraits t, TraitDynamicsDefinition d)
    {
        if (!ch.ZoneMinutesSpent.TryGetValue(regionId, out int minutes) || minutes <= 0) return 0;
        int fam = Math.Min(1000, minutes * 1000 / Math.Max(1, d.FamiliarityFullMinutes));
        return fam * (1000 - t.Exploration / 2) / 1000;
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

    /// <summary>Quanto o perfil gosta do tipo de atividade (permille).</summary>
    public static int Affinity(string type, PersonTraits t) => type switch
    {
        ActivityTypes.Hunt => t.Pve,
        ActivityTypes.Quest => (t.Pve * 6 + t.Exploration * 4) / 10,
        ActivityTypes.Explore => t.Exploration,
        ActivityTypes.Gather => (t.Trade * 5 + t.Patience * 3 + t.Exploration * 2) / 10,
        ActivityTypes.Trade => t.Trade,
        ActivityTypes.Socialize => t.Socializing,
        ActivityTypes.Train => ((1000 - t.GameKnowledge) * 5 + t.Pve * 2) / 10,
        ActivityTypes.Repair => 150,
        ActivityTypes.Patrol => (t.Pvp * 5 + t.Loyalty * 3 + (1000 - t.Opportunism) * 2) / 10,
        ActivityTypes.Ambush => (t.Pvp * 5 + t.Aggressiveness * 3 + t.Opportunism * 2) / 10,
        ActivityTypes.Boss => (t.Pve * 6 + t.Coordination * 4) / 10,
        _ => 200,
    };
}

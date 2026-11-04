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
/// U = afinidade + recompensa + social - risco - viagem - lotacao - repeticao - tempo + ruido
/// (cada termo normalizado em permille e multiplicado pelo peso de BehaviorTuning).
/// </summary>
public static class ActivityScoring
{
    /// <summary>Tipos fora das decisoes individuais ate existirem grupos (M6).</summary>
    private static readonly string[] NeedsGroupSystem = { ActivityTypes.Recruit };

    public static List<ScoredOption> Evaluate(Character ch, PersonTraits t, ContentDatabase content, RouteFinder routes,
        Func<string, string, int> occupancy, long now, DeterministicRng rng)
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

                // Caminho real: ate o portao, pelas zonas, e ate o ponto da atividade (fora da estrada).
                var plan = routes.Plan(ch.Position, ch.RegionId, region.Id, routes.Layout.Spot(region.Id, a.Id));
                if (!plan.Reachable) continue;
                int travel = plan.Minutes;

                var f = new List<DecisionFactor>();
                int affinity = Affinity(a.Type, t);
                f.Add(new DecisionFactor("affinity", affinity * BehaviorTuning.WeightAffinity / 1000));

                int fit = LevelFit(ch.Level, region);
                if (BehaviorTuning.XpPerMinute(a.Type) > 0)
                    f.Add(new DecisionFactor("level_fit", (fit - 500) * BehaviorTuning.WeightReward / 1000));

                int count = occupancy(region.Id, a.Id);
                int crowd = Math.Min(1000, count * 1000 / perActivityCapacity);
                if (a.Type == ActivityTypes.Socialize)
                    f.Add(new DecisionFactor("company", t.Sociability * Math.Min(count, 10) / 10 * BehaviorTuning.WeightSocial / 1000));
                else if (a.Type == ActivityTypes.Hunt || a.Type == ActivityTypes.Gather)
                    f.Add(new DecisionFactor("crowding", -crowd * BehaviorTuning.WeightCrowding / 1000));
                else if (count > 0)
                    f.Add(new DecisionFactor("company", t.Socializing * Math.Min(count, 5) / 5 * BehaviorTuning.WeightSocial / 3000));

                int risk = a.BaseRiskPermille + (region.Pvp == PvpRules.Open ? 250 : 0)
                           + Math.Max(0, region.LevelMin - ch.Level) * 120;
                risk = Math.Min(1000, risk);
                if (risk > 0)
                    f.Add(new DecisionFactor("risk", -risk * t.RiskAversion / 1000 * BehaviorTuning.WeightRisk / 1000));

                if (travel > 0)
                {
                    int travelCost = Math.Min(1000, travel * 1000 / Math.Max(30, remaining));
                    f.Add(new DecisionFactor("travel", -travelCost * BehaviorTuning.WeightTravel / 1000));
                }
                if (travel + a.TypicalMinutes > remaining + BehaviorTuning.LogoutToleranceMinutes)
                    f.Add(new DecisionFactor("time_budget", -BehaviorTuning.TimeBudgetPenalty));

                string key = region.Id + "/" + a.Id;
                if (key == ch.LastActivityKey && ch.RepeatCount > 0)
                    f.Add(new DecisionFactor("repetition", -BehaviorTuning.RepeatPenalty * ch.RepeatCount * (1000 - t.Patience / 2) / 1000));

                int noise = rng.NextIntInclusive(-BehaviorTuning.Noise, BehaviorTuning.Noise);
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

    /// <summary>Adequacao do conteudo da regiao ao nivel (permille): 1000 dentro da faixa.</summary>
    public static int LevelFit(int level, RegionDefinition r)
    {
        if (level < r.LevelMin) return Math.Max(0, 1000 - (r.LevelMin - level) * 250);
        if (level > r.LevelMax) return Math.Max(100, 1000 - (level - r.LevelMax) * 150);
        return 1000;
    }
}

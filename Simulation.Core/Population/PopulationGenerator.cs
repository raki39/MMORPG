using Simulation.Core.Content;
using Simulation.Core.Random;

namespace Simulation.Core.Population;

/// <summary>
/// Gera pessoas a partir dos perfis de populacao do conteudo.
/// Cada pessoa usa seu proprio fluxo de RNG derivado de (seed, "population.person", id),
/// entao gerar a pessoa N nao depende de quantas pessoas vieram antes, e mudar o
/// tamanho da populacao nao altera quem ja existia.
/// Diversidade: perfil define tendencia (media), ruido individual cria variacao;
/// correlacoes surgem da mistura de perfis, sem tornar ninguem identico.
/// </summary>
public static class PopulationGenerator
{
    public const string StreamName = "population.person";
    public const string AppearanceStreamName = "population.appearance";

    public static Person Generate(int personId, ContentDatabase content, ulong campaignSeed)
    {
        if (personId <= 0) throw new ArgumentOutOfRangeException(nameof(personId), "IDs de pessoa comecam em 1.");
        var rng = new DeterministicRng(RngStreams.DeriveSeed(campaignSeed, StreamName, personId));

        var profiles = content.ProfilesOrdered;
        var weights = profiles.Select(p => p.Weight).ToList();
        var profile = profiles[rng.PickWeighted(weights)];

        var names = content.Names;
        string name = names.FirstNames[rng.NextInt(names.FirstNames.Count)] + " " +
                      names.LastNames[rng.NextInt(names.LastNames.Count)];

        // Ordem de sorteio fixa: alterar esta ordem muda populacoes geradas (versionar).
        var t = new PersonTraits
        {
            Pve = Roll(rng, profile.Pve, 0, 1000),
            Pvp = Roll(rng, profile.Pvp, 0, 1000),
            Trade = Roll(rng, profile.Trade, 0, 1000),
            Exploration = Roll(rng, profile.Exploration, 0, 1000),
            Socializing = Roll(rng, profile.Socializing, 0, 1000),
            Sociability = Roll(rng, profile.Sociability, 0, 1000),
            Aggressiveness = Roll(rng, profile.Aggressiveness, 0, 1000),
            Patience = Roll(rng, profile.Patience, 0, 1000),
            Loyalty = Roll(rng, profile.Loyalty, 0, 1000),
            RiskAversion = Roll(rng, profile.RiskAversion, 0, 1000),
            MechanicalSkill = Roll(rng, profile.MechanicalSkill, 0, 1000),
            GameKnowledge = Roll(rng, profile.GameKnowledge, 0, 1000),
            Coordination = Roll(rng, profile.Coordination, 0, 1000),
            CheatPropensity = Roll(rng, profile.CheatPropensity, 0, 1000),
            Opportunism = Roll(rng, profile.Opportunism, 0, 1000),
            LeisureBudgetCentsPerMonth = Roll(rng, profile.LeisureBudgetCentsPerMonth, 0, 1_000_000),
            WillingnessToPay = Roll(rng, profile.WillingnessToPay, 0, 1000),
            PaidAdvantageSensitivity = Roll(rng, profile.PaidAdvantageSensitivity, 0, 1000),
            PreferredStartHour = Roll(rng, profile.PreferredStartHour, 0, 23),
            SessionMinutes = Roll(rng, profile.SessionMinutes, 10, 720),
            DaysPerWeek = Roll(rng, profile.DaysPerWeek, 0, 7),
            WeekendBoost = Roll(rng, profile.WeekendBoost, 0, 1000),
        };
        // Sorteado por ultimo (D-036): nao altera os tracos ja existentes das populacoes.
        t.Talent = Roll(rng, profile.Talent, 0, 1000);
        // Depois do talento (D-039a): mantem os tracos e o talento ja gerados.
        t.Variety = Roll(rng, profile.Variety, 0, 1000);

        ulong appearance = RngStreams.DeriveSeed(campaignSeed, AppearanceStreamName, personId);
        return new Person(personId, name, profile.Id, appearance, t);
    }

    private static int Roll(DeterministicRng rng, TraitRange range, int min, int max)
    {
        int value = range.Mean + rng.NextCenteredNoise(range.Spread);
        return value < min ? min : value > max ? max : value;
    }
}

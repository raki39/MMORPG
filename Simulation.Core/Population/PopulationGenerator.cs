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
    public const string NameStreamName = "population.name";

    /// <summary>
    /// Gera a pessoa. Com "cell" (cenario controlado, R4): perfil e ocupacao fixos, tracos e
    /// rotina na media do conteudo e residencia na localidade do servidor. Pessoas da mesma
    /// celula ficam iguais; o resultado so varia pelos sorteios do dia a dia.
    /// </summary>
    public static Person Generate(int personId, ContentDatabase content, ulong campaignSeed, ControlledCell? cell = null)
    {
        if (personId <= 0) throw new ArgumentOutOfRangeException(nameof(personId), "IDs de pessoa comecam em 1.");
        var rng = new DeterministicRng(RngStreams.DeriveSeed(campaignSeed, StreamName, personId));

        var profiles = content.ProfilesOrdered;
        var weights = profiles.Select(p => p.Weight).ToList();
        var profile = profiles[rng.PickWeighted(weights)];
        bool mean = cell != null;
        if (cell != null) profile = content.Profiles[cell.Profile];

        var names = content.Names;
        string name = names.FirstNames[rng.NextInt(names.FirstNames.Count)] + " " +
                      names.LastNames[rng.NextInt(names.LastNames.Count)];

        // Ordem de sorteio fixa: alterar esta ordem muda populacoes geradas (versionar).
        var t = new PersonTraits
        {
            Pve = Roll(rng, mean, profile.Pve, 0, 1000),
            Pvp = Roll(rng, mean, profile.Pvp, 0, 1000),
            Trade = Roll(rng, mean, profile.Trade, 0, 1000),
            Exploration = Roll(rng, mean, profile.Exploration, 0, 1000),
            Socializing = Roll(rng, mean, profile.Socializing, 0, 1000),
            Sociability = Roll(rng, mean, profile.Sociability, 0, 1000),
            Aggressiveness = Roll(rng, mean, profile.Aggressiveness, 0, 1000),
            Patience = Roll(rng, mean, profile.Patience, 0, 1000),
            Loyalty = Roll(rng, mean, profile.Loyalty, 0, 1000),
            RiskAversion = Roll(rng, mean, profile.RiskAversion, 0, 1000),
            MechanicalSkill = Roll(rng, mean, profile.MechanicalSkill, 0, 1000),
            GameKnowledge = Roll(rng, mean, profile.GameKnowledge, 0, 1000),
            Coordination = Roll(rng, mean, profile.Coordination, 0, 1000),
            CheatPropensity = Roll(rng, mean, profile.CheatPropensity, 0, 1000),
            Opportunism = Roll(rng, mean, profile.Opportunism, 0, 1000),
            LeisureBudgetCentsPerMonth = Roll(rng, mean, profile.LeisureBudgetCentsPerMonth, 0, 1_000_000),
            WillingnessToPay = Roll(rng, mean, profile.WillingnessToPay, 0, 1000),
            PaidAdvantageSensitivity = Roll(rng, mean, profile.PaidAdvantageSensitivity, 0, 1000),
            PreferredStartHour = Roll(rng, mean, profile.PreferredStartHour, 0, 23),
            SessionMinutes = Roll(rng, mean, profile.SessionMinutes, 10, 720),
            // Mesmas posicoes de sorteio dos antigos dias/semana e reforco de fim de semana (P2):
            // talento, variedade e localidade das pessoas nao mudam.
            GameImportance = Roll(rng, mean, profile.GameImportance, 0, 1000),
            Habit = Roll(rng, mean, profile.Habit, 0, 1000),
        };
        // Sorteado por ultimo (D-036): nao altera os tracos ja existentes das populacoes.
        t.Talent = Roll(rng, mean, profile.Talent, 0, 1000);
        // Depois do talento (D-039a): mantem os tracos e o talento ja gerados.
        t.Variety = Roll(rng, mean, profile.Variety, 0, 1000);

        ulong appearance = RngStreams.DeriveSeed(campaignSeed, AppearanceStreamName, personId);
        var person = new Person(personId, name, profile.Id, appearance, t);
        AssignLocality(person, content, rng, mean);   // depois dos tracos: nao altera populacoes ja geradas
        AssignRoutine(person, content, rng, cell?.Occupation, mean);    // depois da localidade (P2)
        return person;
    }

    /// <summary>
    /// Ocupacao pelo peso (independente do perfil: ter tempo nao e ser hardcore), variacao de
    /// horario e deslocamento, folga da escala, sono, renda e apoio (P2). A renda nao tem efeito
    /// ate o M7.
    /// </summary>
    private static void AssignRoutine(Person person, ContentDatabase content, DeterministicRng rng, string? occupationId, bool mean)
    {
        var occs = content.OccupationsOrdered;
        if (occs.Count == 0) return;
        var occ = occupationId != null ? content.Occupations[occupationId] : occs[rng.PickWeighted(occs.Select(o => Math.Max(0, o.Weight)).ToList())];
        var rs = content.Routine;
        person.OccupationId = occ.Id;
        person.ShiftOffsetMinutes = occ.StartVariationMinutes == 0 || mean ? 0 : rng.NextIntInclusive(-occ.StartVariationMinutes, occ.StartVariationMinutes) / 5 * 5;
        person.CommutePermille = mean ? 1000 : 1000 + rng.NextIntInclusive(-rs.CommuteVariationPermille, rs.CommuteVariationPermille);
        // Folga fixa (R1): em qualquer dia dos blocos com folga (a escala 6x1 pode folgar no domingo).
        var dayOffDays = occ.Blocks.Where(b => b.PersonalDayOff).SelectMany(b => b.Days).Select(ContentValidator.WeekDayIndex)
            .Where(d => d >= 0 && d <= 6).Distinct().OrderBy(d => d).ToList();
        person.DayOffIndex = dayOffDays.Count == 0 ? -1 : dayOffDays[rng.NextInt(dayOffDays.Count)];
        // Escala 5x2: a segunda folga e o dia seguinte (sem sorteio novo).
        person.DayOff2Index = person.DayOffIndex >= 0 && occ.DaysOff >= 2 && dayOffDays.Contains((person.DayOffIndex + 1) % 7) ? (person.DayOffIndex + 1) % 7 : -1;
        person.SleepNeedMinutes = Roll(rng, mean, rs.SleepNeedMinutes, 300, 660);
        person.BedtimeMinute = Roll(rng, mean, rs.BedtimeMinute, 1260, 1680) / 5 * 5;
        int factor = content.Localities.TryGetValue(person.ResidenceId, out var loc) ? loc.IncomeFactorPermille : 1000;
        person.IncomeCentsPerMonth = (long)Roll(rng, mean, occ.Income, 0, 100_000_000) * factor / 1000;
        person.SupportCentsPerMonth = rng.ChancePermille(occ.SupportChancePermille) ? (long)Roll(rng, mean, occ.Support, 0, 100_000_000) * factor / 1000 : 0;
        person.SleepDebtTolerance = Roll(rng, mean, rs.SleepDebtTolerance, 0, 1000);
        // Tendencia a prolongar e autocontrole (P3): do perfil, sorteados por ultimo.
        var profile = content.Profiles[person.ProfileId];
        person.Traits.ExtendTendency = Roll(rng, mean, profile.ExtendTendency, 0, 1000);
        person.Traits.SelfControl = Roll(rng, mean, profile.SelfControl, 0, 1000);
        // Coerencia (D-045): o sono se acostuma as obrigacoes e a hora preferida de jogar cai no
        // tempo livre da rotina. Nada e sorteado aqui: so ajusta o que ja foi sorteado.
        person.BedtimeMinute = Behavior.DailySchedule.AdaptedBedtime(person, occ, rs);
        int preferred = Behavior.DailySchedule.CoherentPreferredHour(person, occ, rs);
        person.Traits.PreferredStartHour = preferred;
        person.BaseTraits.PreferredStartHour = preferred;
        person.ExpectedWeeklyMinutes = Behavior.DailySchedule.ExpectedWeeklyMinutes(person, occ, rs);
    }

    /// <summary>
    /// Residencia pelo peso de cada localidade; nacionalidade igual, salvo residentes
    /// estrangeiros; idiomas: o nativo da nacionalidade e os da residencia, pela parcela de
    /// falantes. A nacionalidade nao decide horario, renda nem personalidade (P1).
    /// </summary>
    private static void AssignLocality(Person person, ContentDatabase content, DeterministicRng rng, bool mean)
    {
        var locs = content.LocalitiesOrdered;
        if (locs.Count == 0) return;
        var residence = mean ? content.ServerLocality : locs[rng.PickWeighted(locs.Select(l => Math.Max(0, l.Weight)).ToList())];
        var nationality = residence;
        if (!mean && locs.Count > 1 && rng.ChancePermille(residence.ForeignResidentPermille))
        {
            var others = locs.Where(l => l.Id != residence.Id).ToList();
            nationality = others[rng.PickWeighted(others.Select(l => Math.Max(1, l.Weight)).ToList())];
        }
        var languages = new SortedSet<string>(StringComparer.Ordinal) { nationality.Languages[0].Id };
        foreach (var lang in residence.Languages)
            if (rng.ChancePermille(lang.Permille)) languages.Add(lang.Id);
        person.ResidenceId = residence.Id;
        person.NationalityId = nationality.Id;
        person.Languages = languages.ToArray();
        person.LocalOffsetMinutes = residence.UtcOffsetMinutes - content.ServerLocality.UtcOffsetMinutes;
        // Nome da nacionalidade, quando ela tem os seus; fluxo proprio para nao mexer nos outros sorteios.
        var names = nationality.Names;
        if (names.FirstNames.Count > 0 && names.LastNames.Count > 0)
        {
            var nr = new DeterministicRng(RngStreams.DeriveSeed(person.AppearanceSeed, NameStreamName, person.Id));
            person.Name = names.FirstNames[nr.NextInt(names.FirstNames.Count)] + " " + names.LastNames[nr.NextInt(names.LastNames.Count)];
        }
    }

    private static int Roll(DeterministicRng rng, bool mean, TraitRange range, int min, int max)
    {
        int value = mean ? range.Mean : range.Mean + rng.NextCenteredNoise(range.Spread);
        return value < min ? min : value > max ? max : value;
    }
}

/// <summary>Celula de um cenario controlado (R4): perfil e ocupacao fixos, tracos na media.</summary>
public sealed record ControlledCell(string Profile, string Occupation);

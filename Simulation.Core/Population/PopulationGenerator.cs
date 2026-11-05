using Simulation.Core.Content;
using Simulation.Core.Random;

namespace Simulation.Core.Population;

/// <summary>
/// Gera pessoas a partir do conteudo. Cada pessoa usa seus proprios fluxos de RNG derivados de
/// (seed, fluxo, id): gerar a pessoa N nao depende de quantas vieram antes, e mudar o tamanho da
/// populacao nao altera quem ja existia.
///
/// Estrutura (separacao Perfil x Preferencias x atributos):
/// - Perfil = dedicacao: importancia do jogo, habito e sessao normal, com a dedicacao
///   compartilhada parcial (D-053).
/// - Preferencias = interesses pessoais (fluxo proprio), independentes do perfil.
/// - Demais atributos (especialidade, personalidade, conduta com Competitividade, capacidades,
///   financas, rotina, talento e variedade) = distribuicoes gerais da populacao.
/// - Ligacoes parciais configuraveis ("links") deixam atributos coerentes sem obrigar: o alvo
///   mistura a posicao da ancora com a propria e mantem a sua distribuicao.
/// </summary>
public static class PopulationGenerator
{
    public const string StreamName = "population.person";
    public const string AppearanceStreamName = "population.appearance";
    public const string NameStreamName = "population.name";
    /// <summary>Fluxo proprio da posicao de dedicacao: nao muda o fluxo dos demais atributos.</summary>
    public const string DedicationStreamName = "population.dedication";
    /// <summary>Fluxo proprio das preferencias e da competitividade (atributos novos): nao muda os antigos.</summary>
    public const string InterestsStreamName = "population.interests";

    /// <summary>Atributos de dedicacao, gerados pelo perfil.</summary>
    public static readonly string[] DedicationKeys = { "sessionMinutes", "gameImportance", "habit" };

    /// <summary>
    /// Ordem fixa do fluxo principal (a mesma de antes da separacao, com os mesmos tres sorteios
    /// por atributo): nome, localidade e ocupacao de cada pessoa continuam os mesmos.
    /// </summary>
    private static readonly (string Key, int Lo, int Hi)[] MainOrder =
    {
        ("pve", 0, 1000), ("pvp", 0, 1000), ("trade", 0, 1000), ("exploration", 0, 1000), ("socializing", 0, 1000),
        ("sociability", 0, 1000), ("aggressiveness", 0, 1000), ("patience", 0, 1000), ("loyalty", 0, 1000), ("riskAversion", 0, 1000),
        ("mechanicalSkill", 0, 1000), ("gameKnowledge", 0, 1000), ("coordination", 0, 1000),
        ("cheatPropensity", 0, 1000), ("opportunism", 0, 1000),
        ("leisureBudgetCentsPerMonth", 0, 1_000_000), ("willingnessToPay", 0, 1000), ("paidAdvantageSensitivity", 0, 1000),
        ("preferredStartHour", 0, 23), ("sessionMinutes", 10, 720), ("gameImportance", 0, 1000), ("habit", 0, 1000),
        ("talent", 0, 1000), ("variety", 0, 1000),
    };

    /// <summary>Chaves que podem entrar em ligacoes (sorteadas antes da rotina, fora da dedicacao).</summary>
    public static IEnumerable<string> LinkableKeys() =>
        MainOrder.Select(m => m.Key).Where(k => !DedicationKeys.Contains(k)).Select(k => "traits." + k)
            .Concat(PersonPreferences.Keys.Select(k => "preferences." + k)).Append("traits.competitiveness");

    /// <summary>
    /// Gera a pessoa. Com "cell" (cenario controlado, R4): perfil e ocupacao fixos, atributos e
    /// rotina na referencia do conteudo (mediana no formato triangular, media no antigo) e
    /// residencia na localidade do servidor. Pessoas da mesma celula ficam iguais.
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

        // Fase 1: posicoes proprias (triangular) ou valores (media e dispersao), em ordem fixa.
        var draws = new Dictionary<string, Draw>(StringComparer.Ordinal);
        foreach (var (key, lo, hi) in MainOrder)
            draws["traits." + key] = First(rng, mean, RangeOf(content, profile, key), lo, hi);
        var irng = new DeterministicRng(RngStreams.DeriveSeed(campaignSeed, InterestsStreamName, personId));
        foreach (var key in PersonPreferences.Keys)
            draws["preferences." + key] = First(irng, mean, PreferenceRangeOf(content.PreferenceRanges, key), 0, 1000);
        draws["traits.competitiveness"] = First(irng, mean, content.PopulationTraits.Competitiveness, 0, 1000);

        // Fase 2: dedicacao compartilhada e ligacoes; depois o valor de cada posicao.
        int influence = content.Dedication.InfluencePermille;
        long? dedication = influence > 0 && !mean
            ? TraitDraw.Position(new DeterministicRng(RngStreams.DeriveSeed(campaignSeed, DedicationStreamName, personId)))
            : null;
        var links = content.TraitLinks.Where(l => l.InfluencePermille > 0)
            .GroupBy(l => l.Trait, StringComparer.Ordinal).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        var final = new Dictionary<string, long>(StringComparer.Ordinal);
        var visiting = new HashSet<string>(StringComparer.Ordinal);
        long FinalPosition(string key)
        {
            if (final.TryGetValue(key, out long f)) return f;
            if (!visiting.Add(key)) throw new InvalidOperationException($"Ligação circular em {key}.");
            var d = draws[key];
            long pos = d.Own;
            if (dedication is long ded && key.StartsWith("traits.", StringComparison.Ordinal) && DedicationKeys.Contains(key["traits.".Length..]))
                pos = TraitDraw.Combine(ded, d.Own, influence);
            else if (links.TryGetValue(key, out var link) && draws.TryGetValue(link.Anchor, out var a) && a.Triangular && !mean)
                pos = TraitDraw.Combine(FinalPosition(link.Anchor), d.Own, link.InfluencePermille);
            final[key] = pos;
            return pos;
        }
        int Value(string key)
        {
            var d = draws[key];
            if (!d.Triangular || mean) return d.Value;
            var r = d.Range;
            int v = TraitDraw.Triangular(FinalPosition(key), r.Min!.Value, r.Mode!.Value, r.Max!.Value);
            return Math.Clamp(v, d.Lo, d.Hi);
        }

        var t = new PersonTraits();
        foreach (var (key, _, _) in MainOrder) Assign(t, key, Value("traits." + key));
        t.Competitiveness = Value("traits.competitiveness");

        ulong appearance = RngStreams.DeriveSeed(campaignSeed, AppearanceStreamName, personId);
        var person = new Person(personId, name, profile.Id, appearance, t);
        foreach (var key in PersonPreferences.Keys) person.Preferences.Set(key, Value("preferences." + key));
        // Personalidade principal (P4a): pelos atributos de nascenca, na distribuicao do preset.
        person.PersonalityId = PersonalitySystem.Classify(content, t);
        AssignLocality(person, content, rng, mean);   // depois dos tracos: nao altera populacoes ja geradas
        AssignRoutine(person, content, rng, cell?.Occupation, mean);    // depois da localidade (P2)
        return person;
    }

    /// <summary>Resultado da fase 1: posicao propria (triangular) ou valor ja sorteado.</summary>
    private readonly record struct Draw(TraitRange Range, int Lo, int Hi, bool Triangular, long Own, int Value);

    private static Draw First(DeterministicRng rng, bool mean, TraitRange range, int lo, int hi)
    {
        if (range.IsTriangular)
        {
            int min = range.Min ?? range.Mode ?? 0, mode = range.Mode ?? min, max = range.Max ?? mode;
            var r = new TraitRange { Min = min, Mode = mode, Max = max };
            if (mean) return new Draw(r, lo, hi, true, TraitDraw.Scale / 2, Math.Clamp(TraitDraw.Median(min, mode, max), lo, hi));
            return new Draw(r, lo, hi, true, TraitDraw.Position(rng), 0);
        }
        int value = mean ? range.Mean : range.Mean + rng.NextCenteredNoise(range.Spread);
        return new Draw(range, lo, hi, false, 0, Math.Clamp(value, lo, hi));
    }

    /// <summary>Faixa de um atributo: dedicacao vem do perfil; o resto, da populacao.</summary>
    private static TraitRange RangeOf(ContentDatabase c, PopulationProfileDefinition profile, string key)
    {
        var t = c.PopulationTraits;
        return key switch
        {
            "sessionMinutes" => profile.SessionMinutes,
            "gameImportance" => profile.GameImportance,
            "habit" => profile.Habit,
            _ => TraitRangeOf(t, key),
        };
    }

    /// <summary>Faixa de um atributo da populacao pela chave do conteudo.</summary>
    public static TraitRange TraitRangeOf(PopulationTraitsDefinition t, string key) => key switch
    {
        "pve" => t.Pve, "pvp" => t.Pvp, "trade" => t.Trade, "exploration" => t.Exploration, "socializing" => t.Socializing,
        "sociability" => t.Sociability, "aggressiveness" => t.Aggressiveness, "patience" => t.Patience, "loyalty" => t.Loyalty,
        "riskAversion" => t.RiskAversion, "mechanicalSkill" => t.MechanicalSkill, "gameKnowledge" => t.GameKnowledge,
        "coordination" => t.Coordination, "cheatPropensity" => t.CheatPropensity, "opportunism" => t.Opportunism,
        "competitiveness" => t.Competitiveness,
        "leisureBudgetCentsPerMonth" => t.LeisureBudgetCentsPerMonth, "willingnessToPay" => t.WillingnessToPay,
        "paidAdvantageSensitivity" => t.PaidAdvantageSensitivity, "preferredStartHour" => t.PreferredStartHour,
        "extendTendency" => t.ExtendTendency, "selfControl" => t.SelfControl, "talent" => t.Talent, "variety" => t.Variety,
        _ => throw new ArgumentException($"Atributo desconhecido: {key}"),
    };

    /// <summary>Faixa de uma preferencia pela chave do conteudo.</summary>
    public static TraitRange PreferenceRangeOf(PreferencesDefinition p, string key) => key switch
    {
        "pve" => p.Pve, "pvp" => p.Pvp, "crafting" => p.Crafting, "trade" => p.Trade, "gathering" => p.Gathering,
        "exploration" => p.Exploration, "social" => p.Social,
        _ => throw new ArgumentException($"Preferência desconhecida: {key}"),
    };

    private static void Assign(PersonTraits t, string key, int v)
    {
        switch (key)
        {
            case "pve": t.Pve = v; break;
            case "pvp": t.Pvp = v; break;
            case "trade": t.Trade = v; break;
            case "exploration": t.Exploration = v; break;
            case "socializing": t.Socializing = v; break;
            case "sociability": t.Sociability = v; break;
            case "aggressiveness": t.Aggressiveness = v; break;
            case "patience": t.Patience = v; break;
            case "loyalty": t.Loyalty = v; break;
            case "riskAversion": t.RiskAversion = v; break;
            case "mechanicalSkill": t.MechanicalSkill = v; break;
            case "gameKnowledge": t.GameKnowledge = v; break;
            case "coordination": t.Coordination = v; break;
            case "cheatPropensity": t.CheatPropensity = v; break;
            case "opportunism": t.Opportunism = v; break;
            case "leisureBudgetCentsPerMonth": t.LeisureBudgetCentsPerMonth = v; break;
            case "willingnessToPay": t.WillingnessToPay = v; break;
            case "paidAdvantageSensitivity": t.PaidAdvantageSensitivity = v; break;
            case "preferredStartHour": t.PreferredStartHour = v; break;
            case "sessionMinutes": t.SessionMinutes = v; break;
            case "gameImportance": t.GameImportance = v; break;
            case "habit": t.Habit = v; break;
            case "talent": t.Talent = v; break;
            case "variety": t.Variety = v; break;
            default: throw new ArgumentException($"Atributo desconhecido: {key}");
        }
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
        var own = content.Profiles[person.ProfileId].OccupationWeights;
        OccupationDefinition occ;
        if (occupationId != null) occ = content.Occupations[occupationId];
        else if (own.Count > 0)
        {
            // Perfil com ocupacoes proprias (regra do preset): um sorteio so, em ordem ordinal.
            var keys = own.Keys.Where(content.Occupations.ContainsKey).OrderBy(k => k, StringComparer.Ordinal).ToList();
            occ = content.Occupations[keys[rng.PickWeighted(keys.Select(k => Math.Max(0, own[k])).ToList())]];
        }
        else occ = occs[rng.PickWeighted(occs.Select(o => Math.Max(0, o.Weight)).ToList())];
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
        // Tendencia a prolongar e autocontrole (P3): caracteristicas individuais da populacao toda,
        // independentes do perfil; sorteadas por ultimo, como antes.
        person.Traits.ExtendTendency = Roll(rng, mean, content.PopulationTraits.ExtendTendency, 0, 1000);
        person.Traits.SelfControl = Roll(rng, mean, content.PopulationTraits.SelfControl, 0, 1000);
        // Coerencia (D-045): o sono se acostuma as obrigacoes e o horario habitual de jogar cai no
        // tempo livre da rotina. Nada e sorteado aqui. O gosto de horario sorteado fica guardado a
        // parte (Traits.PreferredStartHour): preferir 20 h e normalmente so conseguir as 21 h (R4).
        person.BedtimeMinute = Behavior.DailySchedule.AdaptedBedtime(person, occ, rs);
        person.HabitualStartHour = Behavior.DailySchedule.ViableStartHour(person, occ, rs);
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

    /// <summary>
    /// Sorteia um parametro avulso (rotina, renda): triangular com posicao propria (mediana na
    /// referencia) ou media e dispersao.
    /// </summary>
    private static int Roll(DeterministicRng rng, bool mean, TraitRange range, int min, int max)
    {
        var d = First(rng, mean, range, min, max);
        if (!d.Triangular || mean) return d.Value;
        return Math.Clamp(TraitDraw.Triangular(d.Own, d.Range.Min!.Value, d.Range.Mode!.Value, d.Range.Max!.Value), min, max);
    }
}

/// <summary>Celula de um cenario controlado (R4): perfil e ocupacao fixos, atributos na referencia.</summary>
public sealed record ControlledCell(string Profile, string Occupation);

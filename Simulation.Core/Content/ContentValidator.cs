using System.Globalization;
using System.Text.RegularExpressions;

namespace Simulation.Core.Content;

public enum IssueSeverity
{
    Warning = 0,
    Error = 1,
}

/// <summary>Problema de conteudo apontando arquivo e campo exato (ex.: abilities[3].effects[0].type).</summary>
public sealed class ValidationIssue
{
    public ValidationIssue(IssueSeverity severity, string file, string path, string message)
    {
        Severity = severity;
        File = file;
        Path = path;
        Message = message;
    }

    public IssueSeverity Severity { get; }
    public string File { get; }
    public string Path { get; }
    public string Message { get; }

    public override string ToString() =>
        $"[{(Severity == IssueSeverity.Error ? "ERRO" : "AVISO")}] {File} :: {Path} — {Message}";
}

/// <summary>
/// Regras de validacao de conteudo (dominio puro). Conteudo com erro nao pode ser
/// publicado nem usado para criar mundo. Mensagens em pt-BR para o editor.
/// </summary>
public static class ContentValidator
{
    private static readonly HashSet<string> PermilleMoodFields = new(StringComparer.Ordinal)
    {
        "badSessionBelow", "badSessionScore", "driverDecayPerDay", "expectationAdapt", "frustrationFromBadSession", "frustrationReliefFromGoodSession", "goodSessionAbove", "greatSessionScore", "impressionDecayNegativePerDay", "impressionDecayPositivePerDay", "impressionLearn", "initialNovelty", "initialSatisfaction", "notableValence", "satisfactionAdapt",
    };

    public const string WorldFile = "world.json";
    public const string RegionsFile = "regions.json";
    public const string ClassesFile = "classes.json";
    public const string AbilitiesFile = "abilities.json";
    public const string ItemsFile = "items.json";
    public const string CreaturesFile = "creatures.json";
    public const string PopulationFile = "population.json";
    public const string ProgressionFile = "progression.json";
    public const string QuestsFile = "quests.json";
    public const string LocalitiesFile = "localities.json";
    public const string OccupationsFile = "occupations.json";

    private static readonly Regex IdPattern = new("^[a-z][a-z0-9_]*$", RegexOptions.CultureInvariant);

    public static List<ValidationIssue> Validate(ContentSet set)
    {
        var v = new Collector();

        ValidateWorld(set, v);
        ValidateLocalities(set, v);
        ValidateOccupations(set, v);
        var regionIds = CheckIds(set.Regions, RegionsFile, "regions", v);
        var classIds = CheckIds(set.Classes, ClassesFile, "classes", v);
        var abilityIds = CheckIds(set.Abilities, AbilitiesFile, "abilities", v);
        var itemIds = CheckIds(set.Items, ItemsFile, "items", v);
        CheckIds(set.Creatures, CreaturesFile, "creatures", v);
        CheckIds(set.Population.Profiles, PopulationFile, "profiles", v);

        if (!string.IsNullOrEmpty(set.World.StartingRegionId))
        {
            var start = set.Regions.FirstOrDefault(r => r.Id == set.World.StartingRegionId);
            if (start == null)
                v.Error(WorldFile, "startingRegionId", $"Região '{set.World.StartingRegionId}' não existe.");
            else if (!start.IsCity)
                v.Error(WorldFile, "startingRegionId", "A região inicial precisa ser uma cidade (zona segura).");
        }

        ValidateRegions(set, regionIds, itemIds, v);
        ValidateClasses(set, abilityIds, v);
        ValidateAbilities(set, classIds, v);
        ValidateItems(set, classIds, v);
        ValidateCreatures(set, regionIds, itemIds, v);
        ValidateActivities(set, classIds, itemIds, v);
        ValidatePopulation(set, v);
        ValidateProgression(set, v);
        ValidateQuests(set, classIds, itemIds, v);

        return v.Issues;
    }

    private static void ValidateWorld(ContentSet set, Collector v)
    {
        var w = set.World;
        if (string.IsNullOrWhiteSpace(w.Id) || !IdPattern.IsMatch(w.Id))
            v.Error(WorldFile, "id", "ID do mundo inválido (use minúsculas, números e _).");
        if (w.Version < 1) v.Error(WorldFile, "version", "Versão deve ser >= 1.");
        if (string.IsNullOrWhiteSpace(w.Name)) v.Error(WorldFile, "name", "Nome do mundo obrigatório.");
        if (!DateTime.TryParseExact(w.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            v.Error(WorldFile, "startDate", $"Data '{w.StartDate}' inválida; use yyyy-MM-dd.");
        if (w.ZoneRadius < 60 || w.ZoneRadius > 300)
            v.Error(WorldFile, "zoneRadius", "Raio padrão das zonas deve estar entre 60 e 300 unidades (600 m a 3 km).");
        if (string.IsNullOrWhiteSpace(w.StartingRegionId))
            v.Error(WorldFile, "startingRegionId", "Região inicial obrigatória.");
        var g = w.Map;
        if (g.Columns < 1 || g.Columns > 64) v.Error(WorldFile, "map.columns", "A grade deve ter entre 1 e 64 colunas.");
        if (g.Rows < 1 || g.Rows > 64) v.Error(WorldFile, "map.rows", "A grade deve ter entre 1 e 64 linhas.");
        if (g.SpacingX <= 0) v.Error(WorldFile, "map.spacingX", "Espaçamento horizontal deve ser > 0.");
        else if (g.SpacingX < w.ZoneRadius * 18 / 10 || g.SpacingX > w.ZoneRadius * 22 / 10)
            v.Error(WorldFile, "map.spacingX", $"Espaçamento deve ficar entre 1,8 e 2,2 vezes o raio das zonas ({w.ZoneRadius}), para lotes vizinhos se encostarem.");
        if (g.Mask.Count > 0)
        {
            if (g.Mask.Count != g.Rows) v.Error(WorldFile, "map.mask", $"A máscara precisa de uma linha por linha da grade ({g.Rows}).");
            for (int i = 0; i < g.Mask.Count; i++)
                if (g.Mask[i].Length != g.Columns || g.Mask[i].Any(ch => ch != '#' && ch != '.'))
                    v.Error(WorldFile, $"map.mask[{i}]", $"Cada linha da máscara tem {g.Columns} caracteres: '#' (terra) ou '.' (mar).");
        }
        if (g.SpacingY <= 0) v.Error(WorldFile, "map.spacingY", "Espaçamento vertical deve ser > 0.");
        else if (g.SpacingX > 0)
        {
            double diag = Math.Sqrt(g.SpacingX * g.SpacingX / 4.0 + (double)g.SpacingY * g.SpacingY);
            if (Math.Abs(diag - g.SpacingX) > g.SpacingX * 0.08)
                v.Warning(WorldFile, "map.spacingY", $"Vizinhos na diagonal ficam a {diag:0} unidades e na linha a {g.SpacingX}: lotes desiguais.");
        }
    }

    private static readonly string[] WeekDays = { "mon", "tue", "wed", "thu", "fri", "sat", "sun" };

    /// <summary>"HH:mm" em minutos do dia; -1 se invalido.</summary>
    public static int ParseClock(string s)
    {
        if (s.Length != 5 || s[2] != ':' || !int.TryParse(s[..2], out int h) || !int.TryParse(s[3..], out int m) || h > 23 || m > 59) return -1;
        return h * 60 + m;
    }

    /// <summary>Indice do dia (mon = 0 .. sun = 6); -1 se invalido.</summary>
    public static int WeekDayIndex(string d) => Array.IndexOf(WeekDays, d);

    /// <summary>Acordar mais cedo que isso so em turno da noite (D-045).</summary>
    public const int EarliestDayShiftWakeMinute = 4 * 60 + 30;

    /// <summary>Ocupacoes (P2): blocos validos, sem sobreposicao no mesmo dia, renda e apoio coerentes.</summary>
    private static void ValidateOccupations(ContentSet set, Collector v)
    {
        CheckIds(set.Occupations, OccupationsFile, "occupations", v);
        if (set.Occupations.Count == 0) v.Error(OccupationsFile, "occupations", "Defina ao menos uma ocupação.");
        else if (set.Occupations.Sum(o => Math.Max(0, o.Weight)) <= 0) v.Error(OccupationsFile, "occupations", "A soma dos pesos precisa ser positiva.");
        var kinds = new[] { "work", "study", "work_study", "care", "none" };
        for (int i = 0; i < set.Occupations.Count; i++)
        {
            var o = set.Occupations[i];
            string p = $"occupations[{i}]";
            if (!kinds.Contains(o.Kind)) v.Error(OccupationsFile, p + ".kind", $"Use {string.Join(", ", kinds)}.");
            if (o.Weight < 0) v.Error(OccupationsFile, p + ".weight", "O peso não pode ser negativo.");
            if (o.StartVariationMinutes is < 0 or > 360) v.Error(OccupationsFile, p + ".startVariationMinutes", "Use de 0 a 360 minutos.");
            if (o.DaysOff is < 1 or > 2) v.Error(OccupationsFile, p + ".daysOff", "Use 1 (escala 6x1) ou 2 (escala 5x2).");
            if (o.DaysOff == 2 && !o.Blocks.Any(b => b.PersonalDayOff)) v.Error(OccupationsFile, p + ".daysOff", "Duas folgas só fazem sentido com blocos de folga individual.");
            if (o.SupportChancePermille is < 0 or > 1000) v.Error(OccupationsFile, p + ".supportChancePermille", "Use 0 a 1000.");
            if (o.PlayDuringWorkFlexibility is < 0 or > 1000) v.Error(OccupationsFile, p + ".playDuringWorkFlexibility", "Use 0 a 1000.");
            if (o.PlayDuringWorkFlexibility > 0 && !o.Blocks.Any(b => b.Remote))
                v.Error(OccupationsFile, p + ".playDuringWorkFlexibility", "Só faz sentido com algum bloco remoto.");
            Range(p, "income", o.Income, 0, 100_000_000, v, OccupationsFile);
            Range(p, "support", o.Support, 0, 100_000_000, v, OccupationsFile);
            // Coerencia (D-045): com a variacao de horario e o deslocamento mais longo, ninguem
            // acorda antes das 04:30 para um turno diurno (turno da noite cruza a meia-noite).
            var rsv = set.Population.Routine;
            for (int j = 0; j < o.Blocks.Count; j++)
            {
                int bs = ParseClock(o.Blocks[j].Start), be = ParseClock(o.Blocks[j].End);
                if (bs < 0 || be < 0 || be <= bs || o.Blocks[j].IsFromPrevious()) continue;   // vindo direto de outro bloco: nao sai de casa
                int earliestWake = bs - o.StartVariationMinutes - o.Blocks[j].CommuteMinutes * (1000 + rsv.CommuteVariationPermille) / 1000 - rsv.PrepMinutes;
                if (earliestWake < EarliestDayShiftWakeMinute)
                    v.Error(OccupationsFile, $"{p}.blocks[{j}].start",
                        $"Com a variação de horário (±{o.StartVariationMinutes} min) e o deslocamento, alguém acordaria às {earliestWake / 60:00}:{earliestWake % 60:00} para um turno diurno. Use turnos separados (ex.: manhã e tarde) ou reduza a variação.");
            }
            var perDay = new List<(int Start, int End)>[7];
            for (int d = 0; d < 7; d++) perDay[d] = new();
            for (int j = 0; j < o.Blocks.Count; j++)
            {
                var b = o.Blocks[j];
                string bp = $"{p}.blocks[{j}]";
                int s = ParseClock(b.Start), e = ParseClock(b.End);
                if (s < 0) v.Error(OccupationsFile, bp + ".start", "Use HH:mm.");
                if (e < 0) v.Error(OccupationsFile, bp + ".end", "Use HH:mm.");
                if (s >= 0 && e == s) v.Error(OccupationsFile, bp + ".end", "Início e fim iguais.");
                if (b.CommuteMinutes is < 0 or > 240) v.Error(OccupationsFile, bp + ".commuteMinutes", "Use de 0 a 240 minutos.");
                if (b.Route is not ("home" or "fromPrevious")) v.Error(OccupationsFile, bp + ".route", "Use \"home\" ou \"fromPrevious\".");
                if (b.MealMinutes is < 0 or > 180) v.Error(OccupationsFile, bp + ".mealMinutes", "Use de 0 a 180 minutos.");
                if (b.TransferMinutes is < 0 or > 240) v.Error(OccupationsFile, bp + ".transferMinutes", "Use de 0 a 240 minutos.");
                if (b.IsFromPrevious() && s >= 0)
                {
                    // Em cada dia do bloco, precisa haver um bloco anterior no mesmo dia que termine a tempo
                    // da refeicao e da transferencia.
                    foreach (var day in b.Days)
                    {
                        var before = o.Blocks.Where(x => x != b && x.Days.Contains(day) && ParseClock(x.End) >= 0 && ParseClock(x.End) > ParseClock(x.Start) && ParseClock(x.End) <= s)
                            .Select(x => ParseClock(x.End)).DefaultIfEmpty(-1).Max();
                        if (before < 0) { v.Error(OccupationsFile, bp + ".route", $"\"fromPrevious\" precisa de um bloco anterior em '{day}'."); break; }
                        if (before + b.MealMinutes + b.TransferMinutes > s) { v.Error(OccupationsFile, bp + ".route", $"Refeição e transferência não cabem entre o bloco anterior e este em '{day}'."); break; }
                    }
                }
                if (b.Days.Count == 0) v.Error(OccupationsFile, bp + ".days", "Defina ao menos um dia.");
                if (b.Breaks.Count > 0 && !b.Remote) v.Error(OccupationsFile, bp + ".breaks", "Pausas para jogar só em blocos remotos.");
                for (int k = 0; k < b.Breaks.Count; k++)
                {
                    int bs = ParseClock(b.Breaks[k].Start), be = ParseClock(b.Breaks[k].End);
                    if (bs < 0 || be < 0 || be <= bs) v.Error(OccupationsFile, $"{bp}.breaks[{k}]", "Use HH:mm com fim depois do início.");
                    else if (s >= 0 && e > s && (bs < s || be > e)) v.Error(OccupationsFile, $"{bp}.breaks[{k}]", "A pausa precisa ficar dentro do bloco.");
                }
                foreach (var day in b.Days)
                {
                    int di = WeekDayIndex(day);
                    if (di < 0) { v.Error(OccupationsFile, bp + ".days", $"Dia '{day}' inválido; use {string.Join(", ", WeekDays)}."); continue; }
                    if (s < 0 || e < 0) continue;
                    int end = e > s ? e : e + 1440;
                    if (perDay[di].Any(x => s < x.End && x.Start < end))
                        v.Error(OccupationsFile, bp, $"Sobrepõe outro bloco em '{day}'.");
                    perDay[di].Add((s, end));
                }
            }
        }
        var r = set.Population.Routine;
        Range("routine", "sleepNeedMinutes", r.SleepNeedMinutes, 240, 720, v, PopulationFile);
        Range("routine", "bedtimeMinute", r.BedtimeMinute, 1200, 1800, v, PopulationFile);
        if (r.BedtimeAdaptMinutes is < 0 or > 240) v.Error(PopulationFile, "routine.bedtimeAdaptMinutes", "Use de 0 a 240 minutos.");
        if (r.PreferredDelayMaxMinutes is < 0 or > 600) v.Error(PopulationFile, "routine.preferredDelayMaxMinutes", "Use de 0 a 600 minutos.");
        if (r.FreeTimeDurationBase is < 0 or > 2000) v.Error(PopulationFile, "routine.freeTimeDurationBase", "Use 0 a 2000.");
        if (r.WorkCallToleranceMinutes is < 0 or > 30) v.Error(PopulationFile, "routine.workCallToleranceMinutes", "Use de 0 a 30 minutos.");
        if (r.CommitmentChancePermille is < 0 or > 1000) v.Error(PopulationFile, "routine.commitmentChancePermille", "Use 0 a 1000.");
        var commitIds = new HashSet<string>(StringComparer.Ordinal);
        for (int ci = 0; ci < r.Commitments.Count; ci++)
        {
            var c = r.Commitments[ci];
            string cp = $"routine.commitments[{ci}]";
            if (string.IsNullOrWhiteSpace(c.Id) || !commitIds.Add(c.Id)) v.Error(PopulationFile, cp + ".id", "Id vazio ou repetido.");
            if (string.IsNullOrWhiteSpace(c.Label)) v.Error(PopulationFile, cp + ".label", "Dê um nome ao compromisso.");
            int cf = ParseClock(c.StartFrom), ct = ParseClock(c.StartTo);
            if (cf < 0) v.Error(PopulationFile, cp + ".startFrom", "Use HH:MM.");
            if (ct < 0) v.Error(PopulationFile, cp + ".startTo", "Use HH:MM.");
            if (cf >= 0 && ct >= 0 && ct < cf) v.Error(PopulationFile, cp + ".startTo", "O início mais tarde não pode ser antes do mais cedo.");
            if (c.MinMinutes < 15 || c.MaxMinutes > 720 || c.MinMinutes > c.MaxMinutes) v.Error(PopulationFile, cp + ".minMinutes", "Use 15 ≤ mínimo ≤ máximo ≤ 720 minutos.");
            if (c.Weight < 0) v.Error(PopulationFile, cp + ".weight", "O peso não pode ser negativo.");
        }
        foreach (var (name, value) in new (string, int)[]
        {
            ("prepMinutes", r.PrepMinutes), ("arrivalMinutes", r.ArrivalMinutes), ("minWindowMinutes", r.MinWindowMinutes),
            ("minSessionMinutes", r.MinSessionMinutes), ("fullFreeMinutes", r.FullFreeMinutes), ("planHistoryDays", r.PlanHistoryDays),
        })
            if (value < 0) v.Error(PopulationFile, "routine." + name, "O valor não pode ser negativo.");
        foreach (var (name, value) in new (string, int)[]
        {
            ("freeDayChanceBonus", r.FreeDayChanceBonus), ("habitStreakPermille", r.HabitStreakPermille), ("secondSessionFromImportance", r.SecondSessionFromImportance),
            ("secondSessionPermilleAtMax", r.SecondSessionPermilleAtMax), ("secondSessionDurationPermille", r.SecondSessionDurationPermille), ("commuteVariationPermille", r.CommuteVariationPermille),
            ("busyDayPenaltyPermille", r.BusyDayPenaltyPermille),
        })
            if (value is < 0 or > 1000) v.Error(PopulationFile, "routine." + name, "Use um valor de 0 a 1000.");
        if (r.MaxSessionsPerDay is < 1 or > 4) v.Error(PopulationFile, "routine.maxSessionsPerDay", "Use de 1 a 4.");
        if (r.FullFreeMinutes <= 0) v.Error(PopulationFile, "routine.fullFreeMinutes", "Use mais que zero.");
        if (r.FullLoadMinutes <= 0) v.Error(PopulationFile, "routine.fullLoadMinutes", "Use mais que zero.");
        // Sono, extensao e sessoes no expediente (P3).
        foreach (var (name, value) in new (string, int)[]
        {
            ("selfControlWeight", r.SelfControlWeight), ("goalClosePermille", r.GoalClosePermille), ("fatigueChancePenalty", r.FatigueChancePenalty),
            ("fatigueDurationPenalty", r.FatigueDurationPenalty), ("tiredReasonFatigue", r.TiredReasonFatigue), ("breakSessionChancePermille", r.BreakSessionChancePermille),
            ("slackChanceAtMax", r.SlackChanceAtMax), ("workInterruptChancePermille", r.WorkInterruptChancePermille), ("extendTendencyBase", r.ExtendTendencyBase),
        })
            if (value is < 0 or > 1000) v.Error(PopulationFile, "routine." + name, "Use um valor de 0 a 1000.");
        foreach (var (name, value) in new (string, int)[]
        {
            ("extendMaxTimesPerSession", r.ExtendMaxTimesPerSession), ("extendMaxMinutes", r.ExtendMaxMinutes), ("maxSleepCutMinutes", r.MaxSleepCutMinutes),
            ("maxDeficitMinutes", r.MaxDeficitMinutes), ("recoveryFreeNightMinutes", r.RecoveryFreeNightMinutes), ("recoveryNormalNightMinutes", r.RecoveryNormalNightMinutes),
            ("fatigueMoodWeight", r.FatigueMoodWeight), ("slackEdgeMinutes", r.SlackEdgeMinutes), ("interruptWeight", r.InterruptWeight),
        })
            if (value < 0) v.Error(PopulationFile, "routine." + name, "O valor não pode ser negativo.");
        if (r.FatigueFullDeficitMinutes <= 0) v.Error(PopulationFile, "routine.fatigueFullDeficitMinutes", "Use mais que zero.");
        if (r.MinSleepMinutes is < 120 or > 600) v.Error(PopulationFile, "routine.minSleepMinutes", "Use de 120 a 600 minutos.");
        Range("routine", "sleepDebtTolerance", r.SleepDebtTolerance, 0, 1000, v, PopulationFile);
        Range("routine", "slackMinutes", r.SlackMinutes, 5, 120, v, PopulationFile);
        foreach (var kv in r.InterruptCost)
        {
            if (!ActivityTypes.All.Contains(kv.Key)) v.Error(PopulationFile, "routine.interruptCost." + kv.Key, "Tipo de atividade desconhecido.");
            if (kv.Value is < 0 or > 1000) v.Error(PopulationFile, "routine.interruptCost." + kv.Key, "Use 0 a 1000.");
        }
    }

    /// <summary>Localidades (P1): fuso, peso, idiomas e feriados; o servidor aponta uma delas.</summary>
    private static void ValidateLocalities(ContentSet set, Collector v)
    {
        var ids = CheckIds(set.Localities, LocalitiesFile, "localities", v);
        if (set.Localities.Count == 0) v.Error(LocalitiesFile, "localities", "Defina ao menos uma localidade.");
        if (!ids.Contains(set.World.ServerLocalityId))
            v.Error(WorldFile, "serverLocalityId", $"Localidade '{set.World.ServerLocalityId}' não existe em {LocalitiesFile}.");
        if (set.Localities.Count > 0 && set.Localities.Sum(l => Math.Max(0, l.Weight)) <= 0)
            v.Error(LocalitiesFile, "localities", "A soma dos pesos precisa ser positiva.");
        for (int i = 0; i < set.Localities.Count; i++)
        {
            var l = set.Localities[i];
            string p = $"localities[{i}]";
            if (l.UtcOffsetMinutes is < -720 or > 840) v.Error(LocalitiesFile, p + ".utcOffsetMinutes", "Use de -720 (UTC-12) a 840 (UTC+14).");
            if (l.Weight < 0) v.Error(LocalitiesFile, p + ".weight", "O peso não pode ser negativo.");
            if (l.ForeignResidentPermille is < 0 or > 1000) v.Error(LocalitiesFile, p + ".foreignResidentPermille", "Use 0 a 1000.");
            if (l.IncomeFactorPermille is < 0 or > 20000) v.Error(LocalitiesFile, p + ".incomeFactorPermille", "Use de 0 a 20000.");
            if (l.Languages.Count == 0) v.Error(LocalitiesFile, p + ".languages", "Defina ao menos o idioma nativo.");
            else if (l.Languages[0].Permille != 1000) v.Error(LocalitiesFile, p + ".languages[0].permille", "O primeiro idioma é o nativo: use 1000.");
            for (int j = 0; j < l.Languages.Count; j++)
            {
                if (string.IsNullOrWhiteSpace(l.Languages[j].Id)) v.Error(LocalitiesFile, $"{p}.languages[{j}].id", "Obrigatório.");
                if (l.Languages[j].Permille is < 0 or > 1000) v.Error(LocalitiesFile, $"{p}.languages[{j}].permille", "Use 0 a 1000.");
            }
            if ((l.Names.FirstNames.Count == 0) != (l.Names.LastNames.Count == 0))
                v.Error(LocalitiesFile, p + ".names", "Defina nomes e sobrenomes juntos (ou nenhum, para usar os gerais).");
            for (int j = 0; j < l.Holidays.Count; j++)
            {
                if (!TryParseHoliday(l.Holidays[j].Date, out _, out _, out _))
                    v.Error(LocalitiesFile, $"{p}.holidays[{j}].date", $"Data '{l.Holidays[j].Date}' inválida; use MM-dd ou yyyy-MM-dd.");
                if (string.IsNullOrWhiteSpace(l.Holidays[j].Name)) v.Error(LocalitiesFile, $"{p}.holidays[{j}].name", "Nome do feriado obrigatório.");
            }
        }
    }

    /// <summary>Interpreta "MM-dd" (recorrente) ou "yyyy-MM-dd".</summary>
    public static bool TryParseHoliday(string text, out int month, out int day, out int? year)
    {
        month = 0; day = 0; year = null;
        if (DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var full))
        {
            month = full.Month; day = full.Day; year = full.Year;
            return true;
        }
        // Ano bissexto como referencia para aceitar 02-29 recorrente.
        if (DateTime.TryParseExact("2000-" + text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var md))
        {
            month = md.Month; day = md.Day;
            return true;
        }
        return false;
    }

    private static HashSet<string> CheckIds<T>(List<T> items, string file, string root, Collector v) where T : IContentDefinition
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (string.IsNullOrWhiteSpace(item.Id) || !IdPattern.IsMatch(item.Id))
                v.Error(file, $"{root}[{i}].id", $"ID '{item.Id}' inválido (use minúsculas, números e _, iniciando por letra).");
            else if (!seen.Add(item.Id))
                v.Error(file, $"{root}[{i}].id", $"ID '{item.Id}' duplicado.");
            if (item.Version < 1)
                v.Error(file, $"{root}[{i}].version", "Versão deve ser >= 1.");
        }
        return seen;
    }

    private static void ValidateRegions(ContentSet set, HashSet<string> regionIds, HashSet<string> itemIds, Collector v)
    {
        for (int i = 0; i < set.Regions.Count; i++)
        {
            var r = set.Regions[i];
            string p = $"regions[{i}]";
            v.Require(RegionsFile, p + ".name", r.Name);
            v.OneOf(RegionsFile, p + ".function", r.Function, ZoneFunctions.All);
            v.OneOf(RegionsFile, p + ".pvp", r.Pvp, PvpRules.All);
            v.OneOf(RegionsFile, p + ".deathRule", r.DeathRule, DeathRules.All);
            v.OneOf(RegionsFile, p + ".control", r.Control, ControlRules.All);
            v.OneOf(RegionsFile, p + ".biome", r.Biome, Biomes.All);
            if (r.IsCity && (r.CityLevel < 1 || r.CityLevel > 5))
                v.Error(RegionsFile, p + ".cityLevel", "Cidades têm nível de 1 a 5 (define o tamanho da área urbana).");
            if (!r.IsCity && r.CityLevel != 0)
                v.Error(RegionsFile, p + ".cityLevel", "Só cidades têm nível de cidade.");
            if (r.CityPosition != null)
            {
                if (!r.IsCity) v.Error(RegionsFile, p + ".cityPosition", "Só cidades têm posição de cidade.");
                else
                {
                    int wallR = World.ZoneLayout.CityWallFor(r, set.World.ZoneRadius);
                    double off = Math.Sqrt((double)r.CityPosition.X * r.CityPosition.X + (double)r.CityPosition.Y * r.CityPosition.Y);
                    if (off + wallR > set.World.ZoneRadius * 0.80)
                        v.Error(RegionsFile, p + ".cityPosition", $"A muralha (raio {wallR}) precisa caber na zona: afaste a cidade no máximo {Math.Max(0, set.World.ZoneRadius * 80 / 100 - wallR)} unidades do centro.");
                }
            }
            var river = r.Terrain.River;
            if (river != null)
            {
                if (river.Direction is < 0 or > 7) v.Error(RegionsFile, p + ".terrain.river.direction", "Direção do rio de 0 a 7 (passos de 22,5°; 0 = leste-oeste, 4 = norte-sul).");
                if (river.Width is < 3 or > 14) v.Error(RegionsFile, p + ".terrain.river.width", "Largura do rio de 3 a 14 unidades.");
                if (Math.Abs(river.Offset) > set.World.ZoneRadius * 8 / 10) v.Error(RegionsFile, p + ".terrain.river.offset", "O rio precisa passar dentro da zona.");
                if (river.MeanderPermille is < 0 or > 1000) v.Error(RegionsFile, p + ".terrain.river.meanderPermille", "Sinuosidade de 0 a 1000 permille.");
            }
            if (r.Terrain.PoolsPermille is < 0 or > 700)
                v.Error(RegionsFile, p + ".terrain.poolsPermille", "Cobertura de poças de 0 a 700 permille.");
            v.OneOf(RegionsFile, p + ".terrain.relief", r.Terrain.Relief, Reliefs.All);
            if (r.Terrain.ElevationPermille is < 0 or > 1000)
                v.Error(RegionsFile, p + ".terrain.elevationPermille", "Altitude deve estar entre 0 e 1000 permille.");
            if (r.Terrain.RoughnessPermille is < 0 or > 1000)
                v.Error(RegionsFile, p + ".terrain.roughnessPermille", "Aspereza deve estar entre 0 e 1000 permille.");
            if (r.IsCity && r.Pvp != PvpRules.None)
                v.Error(RegionsFile, p + ".pvp", "Cidades são sempre zonas sem PvP.");
            if (r.IsCity && r.DeathRule != DeathRules.None)
                v.Error(RegionsFile, p + ".deathRule", "Cidades não podem ter perda por morte.");
            if (r.IsCity && r.Control != ControlRules.Neutral)
                v.Error(RegionsFile, p + ".control", "Cidades não são disputáveis nesta versão.");
            if (r.DeathRule == DeathRules.FullLoot && r.Pvp == PvpRules.None)
                v.Warning(RegionsFile, p + ".deathRule", "Full loot sem PvP só se aplica a mortes por criaturas.");
            if (r.LevelMin < 1) v.Error(RegionsFile, p + ".levelMin", "Nível mínimo deve ser >= 1.");
            if (r.LevelMax < r.LevelMin) v.Error(RegionsFile, p + ".levelMax", "Nível máximo menor que o mínimo.");
            if (r.Capacity <= 0) v.Error(RegionsFile, p + ".capacity", "Capacidade deve ser > 0.");
            if (r.Connections.Count == 0 && set.Regions.Count > 1)
                v.Warning(RegionsFile, p + ".connections", "Região sem conexões fica inacessível.");
            for (int c = 0; c < r.Connections.Count; c++)
            {
                var con = r.Connections[c];
                string cp = $"{p}.connections[{c}]";
                if (!regionIds.Contains(con.To))
                    v.Error(RegionsFile, cp + ".to", $"Região '{con.To}' não existe.");
                else if (con.To == r.Id)
                    v.Error(RegionsFile, cp + ".to", "Região não pode se conectar a si mesma.");
                else
                {
                    var other = set.Regions.First(x => x.Id == con.To);
                    if (!other.Connections.Any(x => x.To == r.Id))
                        v.Error(RegionsFile, cp + ".to", $"Conexão de mão única: '{con.To}' não volta para '{r.Id}' (toda passagem tem portão dos dois lados).");
                    long dx = other.MapPosition.X - r.MapPosition.X, dy = other.MapPosition.Y - r.MapPosition.Y;
                    double gap = Math.Sqrt(dx * dx + dy * dy) - 2.0 * set.World.ZoneRadius;
                    if (gap > 120)
                        v.Warning(RegionsFile, cp + ".to", $"Zonas conectadas estão a {gap:0} unidades de distância; a estrada entre os portões ficará longa.");
                }
            }
            for (int k = 0; k < r.ResourceItemIds.Count; k++)
                if (!itemIds.Contains(r.ResourceItemIds[k]))
                    v.Error(RegionsFile, $"{p}.resourceItemIds[{k}]", $"Item '{r.ResourceItemIds[k]}' não existe.");
        }

        // Grade de lotes (D-028): cada zona no centro de um lote, um lote por zona.
        var grid = new World.MapGrid(set.World, Array.Empty<RegionDefinition>());
        if (!grid.IsValid) return;
        var taken = new Dictionary<int, string>();
        for (int i = 0; i < set.Regions.Count; i++)
        {
            var r = set.Regions[i];
            string p = $"regions[{i}].mapPosition";
            if (!grid.TrySlotAt(r.MapPosition.X, r.MapPosition.Y, out var slot))
            {
                var near = grid.NearestSlot(r.MapPosition.X, r.MapPosition.Y);
                v.Error(RegionsFile, p, near is { } n
                    ? $"A zona precisa ficar no centro de um lote da grade; o mais próximo é ({n.X}, {n.Y})."
                    : "A zona está fora da grade do mapa (world.json map).");
            }
            else if (!grid.IsLand(slot))
                v.Error(RegionsFile, p, $"O lote ({slot.Column + 1}, {slot.Row + 1}) é mar na máscara do mapa (world.json map.mask).");
            else if (taken.TryGetValue(slot.Index(grid.Columns), out var other))
                v.Error(RegionsFile, p, $"O lote já é ocupado por '{other}'.");
            else taken[slot.Index(grid.Columns)] = r.Id;
        }
    }

    private static void ValidateClasses(ContentSet set, HashSet<string> abilityIds, Collector v)
    {
        for (int i = 0; i < set.Classes.Count; i++)
        {
            var c = set.Classes[i];
            string p = $"classes[{i}]";
            v.Require(ClassesFile, p + ".name", c.Name);
            v.Require(ClassesFile, p + ".resourceName", c.ResourceName);
            v.OneOf(ClassesFile, p + ".role", c.Role, ClassRoles.All);
            if (c.BaseStats.Health <= 0) v.Error(ClassesFile, p + ".baseStats.health", "Vida base deve ser > 0.");
            NonNegativeStats(ClassesFile, p + ".baseStats", c.BaseStats, v);
            NonNegativeStats(ClassesFile, p + ".perLevel", c.PerLevel, v);
            if (c.AbilityIds.Count == 0) v.Error(ClassesFile, p + ".abilityIds", "Classe precisa de ao menos uma habilidade utilizável.");
            for (int k = 0; k < c.AbilityIds.Count; k++)
            {
                string id = c.AbilityIds[k];
                if (!abilityIds.Contains(id))
                    v.Error(ClassesFile, $"{p}.abilityIds[{k}]", $"Habilidade '{id}' não existe.");
                else
                {
                    var a = set.Abilities.First(x => x.Id == id);
                    if (a.ClassId != c.Id)
                        v.Error(ClassesFile, $"{p}.abilityIds[{k}]", $"Habilidade '{id}' pertence a classe '{a.ClassId}'.");
                }
            }
        }
    }

    private static void NonNegativeStats(string file, string p, CombatStats s, Collector v)
    {
        if (s.Health < 0) v.Error(file, p + ".health", "Valor negativo.");
        if (s.Resource < 0) v.Error(file, p + ".resource", "Valor negativo.");
        if (s.Armor < 0) v.Error(file, p + ".armor", "Valor negativo.");
        if (s.Power < 0) v.Error(file, p + ".power", "Valor negativo.");
        if (s.Intelligence < 0) v.Error(file, p + ".intelligence", "Valor negativo.");
    }

    private static readonly string[] ScaleAttributes = { "", "power", "intelligence" };
    private static readonly string[] BuffAttributes = { "health", "resource", "armor", "power", "intelligence", "speed", "damageTaken", "healingTaken" };

    private static void ValidateAbilities(ContentSet set, HashSet<string> classIds, Collector v)
    {
        for (int i = 0; i < set.Abilities.Count; i++)
        {
            var a = set.Abilities[i];
            string p = $"abilities[{i}]";
            v.Require(AbilitiesFile, p + ".name", a.Name);
            if (!classIds.Contains(a.ClassId)) v.Error(AbilitiesFile, p + ".classId", $"Classe '{a.ClassId}' não existe.");
            if (a.ResourceCost < 0) v.Error(AbilitiesFile, p + ".resourceCost", "Custo não pode ser negativo.");
            if (a.RangeMeters < 0 || a.RangeMeters > 100) v.Error(AbilitiesFile, p + ".rangeMeters", "Alcance deve estar entre 0 e 100 m.");
            if (a.CastTimeMs < 0 || a.CastTimeMs > 10_000) v.Error(AbilitiesFile, p + ".castTimeMs", "Tempo de lançamento deve estar entre 0 e 10000 ms.");
            if (a.CooldownMs < 0 || a.CooldownMs > 600_000) v.Error(AbilitiesFile, p + ".cooldownMs", "Recarga deve estar entre 0 e 600000 ms.");
            if (a.Effects.Count == 0) v.Error(AbilitiesFile, p + ".effects", "Habilidade precisa de ao menos um efeito.");
            for (int e = 0; e < a.Effects.Count; e++)
                ValidateEffect(a.Effects[e], $"{p}.effects[{e}]", v);
        }
    }

    private static void ValidateEffect(EffectDefinition fx, string p, Collector v)
    {
        const string f = AbilitiesFile;
        if (!EffectTypes.All.Contains(fx.Type))
        {
            v.Error(f, p + ".type", $"Tipo de efeito '{fx.Type}' desconhecido. Válidos: {string.Join(", ", EffectTypes.All)}.");
            return;
        }
        v.OneOf(f, p + ".scaleAttribute", fx.ScaleAttribute, ScaleAttributes);
        if (fx.ScalePermille < 0 || fx.ScalePermille > 5000) v.Error(f, p + ".scalePermille", "Escala deve estar entre 0 e 5000 permille.");
        if (fx.ScalePermille > 0 && fx.ScaleAttribute.Length == 0) v.Error(f, p + ".scaleAttribute", "Informe o atributo de escala.");
        if (fx.RadiusMeters < 0 || fx.RadiusMeters > 50) v.Error(f, p + ".radiusMeters", "Raio deve estar entre 0 e 50 m.");
        if (fx.MaxTargets < 0) v.Error(f, p + ".maxTargets", "Máximo de alvos não pode ser negativo.");
        if (fx.RadiusMeters > 0 && fx.MaxTargets == 0) v.Error(f, p + ".maxTargets", "Efeito em área precisa de máximo de alvos.");

        switch (fx.Type)
        {
            case EffectTypes.Damage:
            case EffectTypes.Heal:
            case EffectTypes.Shield:
            case EffectTypes.Threat:
                if (fx.Value <= 0) v.Error(f, p + ".value", "Valor deve ser > 0.");
                if (fx.Type == EffectTypes.Shield && fx.DurationMs <= 0) v.Error(f, p + ".durationMs", "Escudo precisa de duração > 0.");
                break;
            case EffectTypes.DamageOverTime:
            case EffectTypes.HealOverTime:
                if (fx.ValuePerPulse <= 0) v.Error(f, p + ".valuePerPulse", "Valor por pulso deve ser > 0.");
                if (fx.IntervalMs < 100) v.Error(f, p + ".intervalMs", "Intervalo deve ser >= 100 ms (1 tick).");
                if (fx.DurationMs < fx.IntervalMs) v.Error(f, p + ".durationMs", "Duração deve ser >= intervalo.");
                break;
            case EffectTypes.Stun:
            case EffectTypes.Silence:
            case EffectTypes.Root:
                if (fx.DurationMs <= 0 || fx.DurationMs > 30_000) v.Error(f, p + ".durationMs", "Duração de controle deve estar entre 1 e 30000 ms.");
                break;
            case EffectTypes.Buff:
            case EffectTypes.Debuff:
                v.OneOf(f, p + ".attribute", fx.Attribute, BuffAttributes);
                if (fx.MagnitudePermille <= 0 || fx.MagnitudePermille > 1000) v.Error(f, p + ".magnitudePermille", "Magnitude deve estar entre 1 e 1000 permille.");
                if (fx.DurationMs <= 0) v.Error(f, p + ".durationMs", "Duração deve ser > 0.");
                if (fx.MaxStacks < 1) v.Error(f, p + ".maxStacks", "Acúmulo máximo deve ser >= 1.");
                break;
        }
    }

    private static void ValidateItems(ContentSet set, HashSet<string> classIds, Collector v)
    {
        for (int i = 0; i < set.Items.Count; i++)
        {
            var it = set.Items[i];
            string p = $"items[{i}]";
            v.Require(ItemsFile, p + ".name", it.Name);
            v.OneOf(ItemsFile, p + ".category", it.Category, ItemCategories.All);
            v.OneOf(ItemsFile, p + ".rarity", it.Rarity, Rarities.All);
            if (it.LevelRequirement < 0) v.Error(ItemsFile, p + ".levelRequirement", "Nível requerido não pode ser negativo.");
            if (it.MaxStack < 1) v.Error(ItemsFile, p + ".maxStack", "Pilha máxima deve ser >= 1.");
            if ((it.Category == ItemCategories.Weapon || it.Category == ItemCategories.Armor) && it.MaxStack != 1)
                v.Error(ItemsFile, p + ".maxStack", "Armas e armaduras são itens individuais (maxStack = 1).");
            if (it.BaseValueCopper < 0) v.Error(ItemsFile, p + ".baseValueCopper", "Valor não pode ser negativo.");
            NonNegativeStats(ItemsFile, p + ".stats", it.Stats, v);
            for (int k = 0; k < it.ClassIds.Count; k++)
                if (!classIds.Contains(it.ClassIds[k]))
                    v.Error(ItemsFile, $"{p}.classIds[{k}]", $"Classe '{it.ClassIds[k]}' não existe.");
        }
    }

    private static void ValidateCreatures(ContentSet set, HashSet<string> regionIds, HashSet<string> itemIds, Collector v)
    {
        for (int i = 0; i < set.Creatures.Count; i++)
        {
            var c = set.Creatures[i];
            string p = $"creatures[{i}]";
            v.Require(CreaturesFile, p + ".name", c.Name);
            v.OneOf(CreaturesFile, p + ".kind", c.Kind, CreatureKinds.All);
            if (c.Level < 1) v.Error(CreaturesFile, p + ".level", "Nível deve ser >= 1.");
            if (c.Health <= 0) v.Error(CreaturesFile, p + ".health", "Vida deve ser > 0.");
            if (c.Damage < 0) v.Error(CreaturesFile, p + ".damage", "Dano não pode ser negativo.");
            if (c.Armor < 0) v.Error(CreaturesFile, p + ".armor", "Armadura não pode ser negativa.");
            if (c.AttackIntervalMs < 100) v.Error(CreaturesFile, p + ".attackIntervalMs", "Intervalo de ataque deve ser >= 100 ms.");
            if (c.RespawnSeconds < 1) v.Error(CreaturesFile, p + ".respawnSeconds", "Reaparecimento deve ser >= 1 s.");
            if (c.RecommendedGroupSize < 1 || c.RecommendedGroupSize > 40) v.Error(CreaturesFile, p + ".recommendedGroupSize", "Grupo recomendado deve estar entre 1 e 40.");
            if (c.GoldMinCopper < 0 || c.GoldMaxCopper < c.GoldMinCopper) v.Error(CreaturesFile, p + ".goldMaxCopper", "Faixa de ouro inválida.");
            if (c.RegionIds.Count == 0) v.Warning(CreaturesFile, p + ".regionIds", "Criatura não aparece em nenhuma região.");
            for (int k = 0; k < c.RegionIds.Count; k++)
            {
                string rid = c.RegionIds[k];
                var region = set.Regions.FirstOrDefault(r => r.Id == rid);
                if (region == null)
                    v.Error(CreaturesFile, $"{p}.regionIds[{k}]", $"Região '{rid}' não existe.");
                else if (region.IsCity)
                    v.Error(CreaturesFile, $"{p}.regionIds[{k}]", $"Criaturas hostis não podem aparecer na cidade '{rid}'.");
            }
            for (int k = 0; k < c.Loot.Count; k++)
            {
                var l = c.Loot[k];
                string lp = $"{p}.loot[{k}]";
                if (!itemIds.Contains(l.ItemId)) v.Error(CreaturesFile, lp + ".itemId", $"Item '{l.ItemId}' não existe.");
                if (l.ChancePermille < 1 || l.ChancePermille > 1000) v.Error(CreaturesFile, lp + ".chancePermille", "Chance deve estar entre 1 e 1000 permille.");
                if (l.Min < 1 || l.Max < l.Min) v.Error(CreaturesFile, lp + ".max", "Quantidade inválida (min >= 1 e max >= min).");
            }
        }
    }

    private static void ValidateActivities(ContentSet set, HashSet<string> classIds, HashSet<string> itemIds, Collector v)
    {
        const string f = RegionsFile;
        // Geometria derivada (posicoes automaticas ou do conteudo) para as regras de construcao.
        World.ZoneLayout? layout = null;
        try { layout = new World.ZoneLayout(set.World, set.Regions); }
        catch (Exception) { /* referencias quebradas ja geram erros proprios */ }

        for (int i = 0; i < set.Regions.Count; i++)
        {
            var r = set.Regions[i];
            string rp = $"regions[{i}]";
            if (layout != null) ValidateAreaPlacement(set, r, rp, layout, v);
            if (!r.AllActivities().Any())
                v.Warning(f, rp + ".activities", "Zona não oferece nenhuma atividade; jogadores simulados não terão motivo para visitá-la.");

            var seen = new HashSet<string>(StringComparer.Ordinal);
            for (int a = 0; a < r.Activities.Count; a++)
                ValidateActivity(set, r, r.Activities[a], $"{rp}.activities[{a}]", seen, classIds, itemIds, v);

            var areaIds = new HashSet<string>(StringComparer.Ordinal);
            for (int j = 0; j < r.Areas.Count; j++)
            {
                var area = r.Areas[j];
                string ap = $"{rp}.areas[{j}]";
                if (string.IsNullOrWhiteSpace(area.Id) || !IdPattern.IsMatch(area.Id))
                    v.Error(f, ap + ".id", $"ID '{area.Id}' inválido (use minúsculas, números e _, iniciando por letra).");
                else if (!areaIds.Add(area.Id))
                    v.Error(f, ap + ".id", $"Área '{area.Id}' duplicada nesta zona.");
                v.Require(f, ap + ".name", area.Name);
                v.OneOf(f, ap + ".kind", area.Kind, AreaKinds.All);
                if (AreaKinds.All.Contains(area.Kind) && !AreaKinds.AllowedIn(r.Function).Contains(area.Kind))
                    v.Error(f, ap + ".kind", r.IsCity
                        ? $"Área do tipo '{area.Kind}' não pode existir em cidade."
                        : $"Área do tipo '{area.Kind}' só existe em cidades.");
                var (rmin, rmax) = AreaKinds.RadiusRange(area.Kind);
                if (area.Radius < rmin || area.Radius > rmax)
                    v.Error(f, ap + ".radius", $"Raio deste tipo de área deve estar entre {rmin} e {rmax} unidades ({rmin * 10} a {rmax * 10} m)."); 
                if (area.LevelMin != 0 || area.LevelMax != 0)
                {
                    if (r.IsCity) v.Error(f, ap + ".levelMin", "Áreas da cidade não têm faixa de nível.");
                    else if (area.LevelMin < 1 || area.LevelMax < area.LevelMin || (set.Progression.LevelCap > 0 && area.LevelMax > set.Progression.LevelCap))
                        v.Error(f, ap + ".levelMax", "Faixa da área inválida: 1 <= levelMin <= levelMax <= teto.");
                    else
                        foreach (var act in area.Activities)
                        {
                            int ch = act.ChallengeLevel > 0 ? act.ChallengeLevel : AverageCreatureLevel(set, act);
                            if (ch > 0 && (ch < area.LevelMin - 2 || ch > area.LevelMax + 2))
                                v.Warning(f, ap + ".levelMin", $"Desafio de '{act.Id}' ({ch}) longe da faixa da área ({area.LevelMin}–{area.LevelMax}).");
                        }
                }
                if (area.Kind == AreaKinds.Hideout && r.Pvp != PvpRules.Open)
                    v.Warning(f, ap + ".kind", "Esconderijos de guilda costumam ficar em zonas com PvP aberto.");
                if (area.Activities.Count == 0 && !AreaKinds.IsPortal(area.Kind) && area.Kind != AreaKinds.Landmark)
                    v.Warning(f, ap + ".activities", "Área sem atividades: ninguém terá motivo para ir até lá.");
                for (int a = 0; a < area.Activities.Count; a++)
                    ValidateActivity(set, r, area.Activities[a], $"{ap}.activities[{a}]", seen, classIds, itemIds, v);
            }
        }
    }

    /// <summary>
    /// Regras de construcao: a area cabe na zona, nao bloqueia saidas nem a encruzilhada
    /// e nao ocupa o mesmo espaco de outra area.
    /// </summary>
    private static void ValidateAreaPlacement(ContentSet set, RegionDefinition r, string rp, World.ZoneLayout layout, Collector v)
    {
        const string f = RegionsFile;
        var center = layout.Center(r.Id);
        for (int j = 0; j < r.Areas.Count; j++)
        {
            var a = r.Areas[j];
            string ap = $"{rp}.areas[{j}]";
            World.MapPos c;
            try { c = layout.AreaCenter(r.Id, a.Id); } catch (KeyNotFoundException) { continue; }
            if (c.DistanceTo(center) + a.Radius > set.World.ZoneRadius * 0.92)
                v.Error(f, ap + ".position", "A área não cabe dentro da zona.");
            int wall = layout.CityWallRadius(r.Id);
            if (wall > 0 && c.DistanceTo(layout.Hub(r.Id)) + a.Radius > wall)
                v.Error(f, ap + ".position", $"Na cidade, a área fica dentro da muralha (raio {wall} no nível {r.CityLevel}).");

            if (c.DistanceTo(layout.Hub(r.Id)) <= a.Radius + 4)
                v.Error(f, ap + ".position", "A área cobre a encruzilhada da zona e bloquearia as estradas.");
            foreach (var con in r.Connections)
            {
                World.MapPos gate;
                try { gate = layout.Gate(r.Id, con.To); } catch (KeyNotFoundException) { continue; }
                if (c.DistanceTo(gate) <= a.Radius + 4)
                    v.Error(f, ap + ".position", $"A área bloqueia a saída para '{con.To}'.");
            }
            for (int k = 0; k < j; k++)
            {
                var b = r.Areas[k];
                World.MapPos cb;
                try { cb = layout.AreaCenter(r.Id, b.Id); } catch (KeyNotFoundException) { continue; }
                if (c.DistanceTo(cb) < a.Radius + b.Radius)
                    v.Error(f, ap + ".position", $"A área ocupa o mesmo espaço que '{b.Name}'.");
            }
        }
    }

    private static void ValidateActivity(ContentSet set, RegionDefinition r, ActivityDefinition act, string p,
        HashSet<string> seen, HashSet<string> classIds, HashSet<string> itemIds, Collector v)
    {
        const string f = RegionsFile;
        bool isCity = r.IsCity;
        if (string.IsNullOrWhiteSpace(act.Id) || !IdPattern.IsMatch(act.Id))
            v.Error(f, p + ".id", $"ID '{act.Id}' inválido (use minúsculas, números e _, iniciando por letra).");
        else if (!seen.Add(act.Id))
            v.Error(f, p + ".id", $"ID '{act.Id}' duplicado neste local.");
        v.Require(f, p + ".name", act.Name);
        if (!ActivityTypes.All.Contains(act.Type))
        {
            v.Error(f, p + ".type", $"Tipo de atividade '{act.Type}' desconhecido. Válidos: {string.Join(", ", ActivityTypes.All)}.");
            return;
        }
        if (isCity && ActivityTypes.NotInCity.Contains(act.Type))
            v.Error(f, p + ".type", $"Atividade '{act.Type}' envolve combate e não pode existir em cidade.");
        if (!isCity && ActivityTypes.CityOnly.Contains(act.Type))
            v.Error(f, p + ".type", $"Atividade '{act.Type}' é um serviço de cidade.");
        if (act.Type == ActivityTypes.Ambush && r.Pvp != PvpRules.Open)
            v.Error(f, p + ".type", "Emboscada exige área com PvP aberto.");
        if (act.MinLevel < 1) v.Error(f, p + ".minLevel", "Nível mínimo deve ser >= 1.");
        if (act.MinGroupSize < 1) v.Error(f, p + ".minGroupSize", "Grupo mínimo deve ser >= 1.");
        if (act.MaxGroupSize < act.MinGroupSize || act.MaxGroupSize > 40)
            v.Error(f, p + ".maxGroupSize", "Grupo máximo deve estar entre o mínimo e 40.");
        if (act.TypicalMinutes < 1 || act.TypicalMinutes > 600)
            v.Error(f, p + ".typicalMinutes", "Duração típica deve estar entre 1 e 600 min.");
        if (act.BaseRiskPermille < 0 || act.BaseRiskPermille > 1000)
            v.Error(f, p + ".baseRiskPermille", "Risco base deve estar entre 0 e 1000 permille.");
        ValidateChallenge(set, r, act, p, v);
        for (int k = 0; k < act.ClassIds.Count; k++)
            if (!classIds.Contains(act.ClassIds[k]))
                v.Error(f, $"{p}.classIds[{k}]", $"Classe '{act.ClassIds[k]}' não existe.");
        for (int k = 0; k < act.RequiredItemIds.Count; k++)
            if (!itemIds.Contains(act.RequiredItemIds[k]))
                v.Error(f, $"{p}.requiredItemIds[{k}]", $"Item '{act.RequiredItemIds[k]}' não existe.");
        for (int k = 0; k < act.ItemIds.Count; k++)
            if (!itemIds.Contains(act.ItemIds[k]))
                v.Error(f, $"{p}.itemIds[{k}]", $"Item '{act.ItemIds[k]}' não existe.");

        bool needsCreatures = act.Type == ActivityTypes.Hunt || act.Type == ActivityTypes.Boss;
        if (needsCreatures && act.CreatureIds.Count == 0)
            v.Error(f, p + ".creatureIds", "Caça e boss precisam de ao menos uma criatura.");
        for (int k = 0; k < act.CreatureIds.Count; k++)
        {
            string cid = act.CreatureIds[k];
            var creature = set.Creatures.FirstOrDefault(c => c.Id == cid);
            string cp = $"{p}.creatureIds[{k}]";
            if (creature == null)
                v.Error(f, cp, $"Criatura '{cid}' não existe.");
            else
            {
                if (!creature.RegionIds.Contains(r.Id))
                    v.Error(f, cp, $"Criatura '{cid}' não aparece em '{r.Id}' (ver creatures.json regionIds).");
                if (act.Type == ActivityTypes.Boss && creature.Kind != CreatureKinds.Boss)
                    v.Error(f, cp, $"Criatura '{cid}' não é um boss.");
                if (act.Type == ActivityTypes.Hunt && creature.Kind == CreatureKinds.Boss)
                    v.Error(f, cp, $"Boss '{cid}' deve ser oferecido como atividade 'boss', não 'hunt'.");
                if (act.Type == ActivityTypes.Boss && act.MinGroupSize < creature.RecommendedGroupSize)
                    v.Warning(f, p + ".minGroupSize", $"Grupo mínimo menor que o recomendado para '{cid}' ({creature.RecommendedGroupSize}).");
            }
        }
        if (act.Type == ActivityTypes.Gather)
        {
            if (act.ItemIds.Count == 0)
                v.Error(f, p + ".itemIds", "Coleta precisa de ao menos um item.");
            for (int k = 0; k < act.ItemIds.Count; k++)
                if (!r.ResourceItemIds.Contains(act.ItemIds[k]))
                    v.Error(f, $"{p}.itemIds[{k}]", $"Item '{act.ItemIds[k]}' não está em resourceItemIds deste local.");
        }
    }

    private static void ValidatePopulation(ContentSet set, Collector v)
    {
        var pop = set.Population;
        if (pop.Profiles.Count == 0) v.Error(PopulationFile, "profiles", "Defina ao menos um perfil de população.");
        // Evolucao dos atributos e metas (D-036): valores positivos e faixas coerentes.
        var dy = pop.Dynamics;
        foreach (var (name, value) in new (string, int)[]
        {
            ("mechanicalCombatMinutes", dy.MechanicalCombatMinutes), ("knowledgePlayMinutes", dy.KnowledgePlayMinutes),
            ("knowledgeExploreMinutes", dy.KnowledgeExploreMinutes), ("sociabilitySocialMinutes", dy.SociabilitySocialMinutes),
            ("affinityEnjoyedMinutes", dy.AffinityEnjoyedMinutes), ("riskConfidenceMinutes", dy.RiskConfidenceMinutes),
            ("explorationPerDiscoveries", dy.ExplorationPerDiscoveries), ("familiarityFullMinutes", dy.FamiliarityFullMinutes),
            ("learnCostAtZeroTalent", dy.LearnCostAtZeroTalent), ("learnCostAtMaxTalent", dy.LearnCostAtMaxTalent),
        })
            if (value <= 0) v.Error(PopulationFile, "dynamics." + name, "Use um valor maior que zero.");
        foreach (var (name, value) in new (string, int)[]
        {
            ("rangeSkill", dy.RangeSkill), ("rangePlaystyle", dy.RangePlaystyle), ("rangePersonality", dy.RangePersonality),
            ("potentialBase", dy.PotentialBase), ("potentialPerTalent", dy.PotentialPerTalent), ("potentialMinHeadroom", dy.PotentialMinHeadroom),
            ("enjoyedMinFitPermille", dy.EnjoyedMinFitPermille), ("riskConfidenceMinRisk", dy.RiskConfidenceMinRisk),
            ("travelEstimateErrorAtZero", dy.TravelEstimateErrorAtZero),
        })
            if (value < 0 || value > 1000) v.Error(PopulationFile, "dynamics." + name, "Use um valor de 0 a 1000.");
        var gs = pop.Goals;
        if (gs.MaxActive < 0 || gs.MaxActive > 5) v.Error(PopulationFile, "goals.maxActive", "Use de 0 a 5 metas ao mesmo tempo.");
        if (gs.AdoptChance < 0 || gs.AdoptChance > 1000) v.Error(PopulationFile, "goals.adoptChance", "Use um valor de 0 a 1000.");
        if (gs.Weight < 0) v.Error(PopulationFile, "goals.weight", "O peso não pode ser negativo.");
        var ss = pop.Session;
        foreach (var (name, value) in new (string, int)[]
        {
            ("switchMargin", ss.SwitchMargin), ("commitMargin", ss.CommitMargin), ("commitBaseMinutes", ss.CommitBaseMinutes),
            ("commitExtraMinutesAtMaxPatience", ss.CommitExtraMinutesAtMaxPatience), ("boredomPerHour", ss.BoredomPerHour),
            ("boredomRecoveryPerHour", ss.BoredomRecoveryPerHour), ("boredomWeight", ss.BoredomWeight),
            ("varietyBase", ss.VarietyBase), ("varietyPerPreference", ss.VarietyPerPreference),
        })
            if (value < 0) v.Error(PopulationFile, "session." + name, "O valor não pode ser negativo.");
        if (ss.BoredomKeptBetweenSessions is < 0 or > 1000) v.Error(PopulationFile, "session.boredomKeptBetweenSessions", "Use um valor de 0 a 1000.");
        if (ss.PatienceRelief is < 0 or > 1000) v.Error(PopulationFile, "session.patienceRelief", "Use um valor de 0 a 1000.");
        if (ss.ExploreKnownPenalty is < 0 or > 1000) v.Error(PopulationFile, "session.exploreKnownPenalty", "Use um valor de 0 a 1000.");
        if (ss.EnjoymentRelief is < 0 or > 1000) v.Error(PopulationFile, "session.enjoymentRelief", "Use um valor de 0 a 1000.");
        // Humor e memoria (D-038): nada negativo; fracoes em permille.
        foreach (var (section, list) in new (string, (string, int)[])[]
        {
            ("mood", new (string, int)[]
            {
            ("initialSatisfaction", pop.Mood.InitialSatisfaction),
            ("initialNovelty", pop.Mood.InitialNovelty),
            ("funWeight", pop.Mood.FunWeight),
            ("fitWeight", pop.Mood.FitWeight),
            ("crowdingWeight", pop.Mood.CrowdingWeight),
            ("companyWeight", pop.Mood.CompanyWeight),
            ("boredomWeight", pop.Mood.BoredomWeight),
            ("riskWeight", pop.Mood.RiskWeight),
            ("progressWeight", pop.Mood.ProgressWeight),
            ("progressClamp", pop.Mood.ProgressClamp),
            ("progressRatioCap", pop.Mood.ProgressRatioCap),
            ("expectationBase", pop.Mood.ExpectationBase),
            ("expectationPerAggressiveness", pop.Mood.ExpectationPerAggressiveness),
            ("expectationAdapt", pop.Mood.ExpectationAdapt),
            ("expectationFloor", pop.Mood.ExpectationFloor),
            ("expectationCeiling", pop.Mood.ExpectationCeiling),
            ("noveltyWeight", pop.Mood.NoveltyWeight),
            ("noveltyDecayPerHour", pop.Mood.NoveltyDecayPerHour),
            ("noveltyPerDiscovery", pop.Mood.NoveltyPerDiscovery),
            ("noveltyNewActivityType", pop.Mood.NoveltyNewActivityType),
            ("noveltyNewActivity", pop.Mood.NoveltyNewActivity),
            ("noveltyPerLevel", pop.Mood.NoveltyPerLevel),
            ("noveltyNewQuest", pop.Mood.NoveltyNewQuest),
            ("eventClamp", pop.Mood.EventClamp),
            ("levelUpPoints", pop.Mood.LevelUpPoints),
            ("questPoints", pop.Mood.QuestPoints),
            ("mainQuestPoints", pop.Mood.MainQuestPoints),
            ("goalCompletedPoints", pop.Mood.GoalCompletedPoints),
            ("discoveryPoints", pop.Mood.DiscoveryPoints),
            ("goalAbandonedPoints", pop.Mood.GoalAbandonedPoints),
            ("nothingToDoPoints", pop.Mood.NothingToDoPoints),
            ("goalAbandonedFrustration", pop.Mood.GoalAbandonedFrustration),
            ("nothingToDoFrustration", pop.Mood.NothingToDoFrustration),
            ("satisfactionAdapt", pop.Mood.SatisfactionAdapt),
            ("fullWeightSessionMinutes", pop.Mood.FullWeightSessionMinutes),
            ("badSessionBelow", pop.Mood.BadSessionBelow),
            ("frustrationFromBadSession", pop.Mood.FrustrationFromBadSession),
            ("goodSessionAbove", pop.Mood.GoodSessionAbove),
            ("frustrationReliefFromGoodSession", pop.Mood.FrustrationReliefFromGoodSession),
            ("frustrationDecayPerDay", pop.Mood.FrustrationDecayPerDay),
            ("driverDecayPerDay", pop.Mood.DriverDecayPerDay),
            ("engagementSatisfaction", pop.Mood.EngagementSatisfaction),
            ("engagementCalm", pop.Mood.EngagementCalm),
            ("engagementNovelty", pop.Mood.EngagementNovelty),
            ("loyaltyWeight", pop.Mood.LoyaltyWeight),
            ("investmentPerLevel", pop.Mood.InvestmentPerLevel),
            ("investmentCap", pop.Mood.InvestmentCap)
            }),
            ("memory", new (string, int)[]
            {
            ("impressionLearn", pop.Memory.ImpressionLearn),
            ("impressionDecayPositivePerDay", pop.Memory.ImpressionDecayPositivePerDay),
            ("impressionDecayNegativePerDay", pop.Memory.ImpressionDecayNegativePerDay),
            ("decisionWeight", pop.Memory.DecisionWeight),
            ("impressionMinAbs", pop.Memory.ImpressionMinAbs),
            ("maxMemories", pop.Memory.MaxMemories),
            ("notableValence", pop.Memory.NotableValence),
            ("positiveDecayPerDay", pop.Memory.PositiveDecayPerDay),
            ("negativeDecayPerDay", pop.Memory.NegativeDecayPerDay),
            ("levelMilestoneEvery", pop.Memory.LevelMilestoneEvery),
            ("levelMilestoneIntensity", pop.Memory.LevelMilestoneIntensity),
            ("mainQuestIntensity", pop.Memory.MainQuestIntensity),
            ("goalCompletedIntensity", pop.Memory.GoalCompletedIntensity),
            ("goalAbandonedIntensity", pop.Memory.GoalAbandonedIntensity),
            ("discoveryIntensity", pop.Memory.DiscoveryIntensity),
            ("nothingToDoIntensity", pop.Memory.NothingToDoIntensity),
            ("greatSessionScore", pop.Memory.GreatSessionScore),
            ("badSessionScore", pop.Memory.BadSessionScore),
            ("sessionIntensity", pop.Memory.SessionIntensity)
            }),
        })
            foreach (var (name, value) in list)
            {
                if (value < 0) v.Error(PopulationFile, section + "." + name, "O valor não pode ser negativo.");
                else if (PermilleMoodFields.Contains(name) && value > 1000) v.Error(PopulationFile, section + "." + name, "Use um valor de 0 a 1000.");
            }
        foreach (var (name, value) in new (string, int)[]
        {
            ("progressCarePve", pop.Mood.ProgressCarePve), ("progressCareAggressiveness", pop.Mood.ProgressCareAggressiveness),
            ("progressCarePvp", pop.Mood.ProgressCarePvp), ("noveltyCareBase", pop.Mood.NoveltyCareBase), ("noveltyCareVariety", pop.Mood.NoveltyCareVariety), ("noveltyClamp", pop.Mood.NoveltyClamp),
            ("noveltyCareExploration", pop.Mood.NoveltyCareExploration), ("noveltyCareImpatience", pop.Mood.NoveltyCareImpatience),
        })
            if (value is < 0 or > 1000) v.Error(PopulationFile, "mood." + name, "Use um valor de 0 a 1000.");
        // Pesos da decisao e regras de sessao (P0).
        var bh = pop.Behavior;
        foreach (var (name, value) in new (string, int)[]
        {
            ("sessionStartJitterMinutes", bh.SessionStartJitterMinutes),
            ("sessionDurationJitterPermille", bh.SessionDurationJitterPermille),
            ("logoutToleranceMinutes", bh.LogoutToleranceMinutes),
            ("walkSpeed", bh.WalkSpeed),
            ("weightAffinity", bh.WeightAffinity),
            ("weightReward", bh.WeightReward),
            ("weightSocial", bh.WeightSocial),
            ("weightRisk", bh.WeightRisk),
            ("weightTravel", bh.WeightTravel),
            ("weightCrowding", bh.WeightCrowding),
            ("timeBudgetPenalty", bh.TimeBudgetPenalty),
            ("questOfferWeight", bh.QuestOfferWeight),
            ("questDeliverWeight", bh.QuestDeliverWeight),
            ("questDeliverPerExtra", bh.QuestDeliverPerExtra),
            ("questStepWeight", bh.QuestStepWeight),
            ("noise", bh.Noise),
            ("activityDurationMin", bh.ActivityDurationMin),
            ("activityDurationMax", bh.ActivityDurationMax),
            ("idleRethinkMinutes", bh.IdleRethinkMinutes)
        })
            if (value < 0) v.Error(PopulationFile, "behavior." + name, "O valor não pode ser negativo.");
        if (bh.WalkSpeed <= 0) v.Error(PopulationFile, "behavior.walkSpeed", "A velocidade precisa ser maior que zero.");
        if (bh.ActivityDurationMin <= 0 || bh.ActivityDurationMin > bh.ActivityDurationMax)
            v.Error(PopulationFile, "behavior.activityDurationMin", "Use um mínimo maior que zero e até o máximo.");
        if (bh.IdleRethinkMinutes <= 0) v.Error(PopulationFile, "behavior.idleRethinkMinutes", "Use 1 minuto ou mais.");
        if (bh.RegistrationDayWeights.Count == 0 || bh.RegistrationDayWeights.Any(w => w < 0) || bh.RegistrationDayWeights.Sum() <= 0)
            v.Error(PopulationFile, "behavior.registrationDayWeights", "Defina pesos não negativos com soma positiva.");
        // Retencao (D-039).
        foreach (var (name, value) in new (string, int)[]
        {
            ("engagementNeutral", pop.Retention.EngagementNeutral),
            ("engagementEffect", pop.Retention.EngagementEffect),
            ("minPlayFactor", pop.Retention.MinPlayFactor),
            ("maxPlayFactor", pop.Retention.MaxPlayFactor),
            ("inactiveAfterDays", pop.Retention.InactiveAfterDays),
            ("inactiveReturnPermille", pop.Retention.InactiveReturnPermille),
            ("quitBelow", pop.Retention.QuitBelow),
            ("quitChancePerDay", pop.Retention.QuitChancePerDay),
            ("quitAfterInactiveDays", pop.Retention.QuitAfterInactiveDays),
            ("quitReturnMinDays", pop.Retention.QuitReturnMinDays),
            ("quitReturnChancePerDay", pop.Retention.QuitReturnChancePerDay),
            ("quitGraceDaysAfterReturn", pop.Retention.QuitGraceDaysAfterReturn),
            ("extendScoreAbove", pop.Retention.ExtendScoreAbove),
            ("extendChance", pop.Retention.ExtendChance),
            ("extendPermille", pop.Retention.ExtendPermille),
            ("rageQuitFrustration", pop.Retention.RageQuitFrustration),
            ("rageQuitChance", pop.Retention.RageQuitChance)
        })
            if (value < 0) v.Error(PopulationFile, "retention." + name, "O valor não pode ser negativo.");
        var rt = pop.Retention;
        foreach (var (name, value) in new (string, int)[]
        {
            ("engagementNeutral", rt.EngagementNeutral), ("inactiveReturnPermille", rt.InactiveReturnPermille), ("quitBelow", rt.QuitBelow),
            ("quitChancePerDay", rt.QuitChancePerDay), ("quitReturnChancePerDay", rt.QuitReturnChancePerDay),
            ("extendScoreAbove", rt.ExtendScoreAbove), ("extendChance", rt.ExtendChance), ("rageQuitFrustration", rt.RageQuitFrustration), ("rageQuitChance", rt.RageQuitChance),
        })
            if (value > 1000) v.Error(PopulationFile, "retention." + name, "Use um valor de 0 a 1000.");
        if (rt.MinPlayFactor > rt.MaxPlayFactor) v.Error(PopulationFile, "retention.minPlayFactor", "O piso não pode passar do teto.");
        if (rt.InactiveAfterDays < 1) v.Error(PopulationFile, "retention.inactiveAfterDays", "Use 1 dia ou mais.");
        if (rt.QuitAfterInactiveDays <= rt.InactiveAfterDays) v.Error(PopulationFile, "retention.quitAfterInactiveDays", "Precisa ser maior que inactiveAfterDays.");
        if (pop.Mood.NoveltyMin is < -1000 or > 0) v.Error(PopulationFile, "mood.noveltyMin", "Use um valor de -1000 a 0.");
        if (pop.Mood.FrustrationInIntention is < 0 or > 1000) v.Error(PopulationFile, "mood.frustrationInIntention", "Use um valor de 0 a 1000.");
        if (pop.Mood.ExpectationFloor > pop.Mood.ExpectationCeiling) v.Error(PopulationFile, "mood.expectationFloor", "O piso da expectativa não pode passar do teto.");
        if (pop.Memory.MaxMemories < 1) v.Error(PopulationFile, "memory.maxMemories", "Guarde ao menos uma memória.");
        if (pop.Memory.LevelMilestoneEvery < 1) v.Error(PopulationFile, "memory.levelMilestoneEvery", "Use 1 ou mais.");
        if (ss.CommitMargin < ss.SwitchMargin) v.Warning(PopulationFile, "session.commitMargin", "A margem do compromisso costuma ser maior que a margem de troca.");
        if (pop.Names.FirstNames.Count == 0) v.Error(PopulationFile, "names.firstNames", "Lista de nomes vazia.");
        if (pop.Names.LastNames.Count == 0) v.Error(PopulationFile, "names.lastNames", "Lista de sobrenomes vazia.");
        if (pop.Names.NickPrefixes.Count == 0) v.Error(PopulationFile, "names.nickPrefixes", "Lista de prefixos de apelido vazia.");
        if (pop.Names.NickSuffixes.Count == 0) v.Error(PopulationFile, "names.nickSuffixes", "Lista de sufixos de apelido vazia.");
        for (int i = 0; i < pop.Profiles.Count; i++)
        {
            var pr = pop.Profiles[i];
            string p = $"profiles[{i}]";
            v.Require(PopulationFile, p + ".name", pr.Name);
            if (pr.Weight <= 0) v.Error(PopulationFile, p + ".weight", "Peso deve ser > 0.");
            Permille(p, "pve", pr.Pve, v);
            Permille(p, "pvp", pr.Pvp, v);
            Permille(p, "trade", pr.Trade, v);
            Permille(p, "exploration", pr.Exploration, v);
            Permille(p, "socializing", pr.Socializing, v);
            Permille(p, "sociability", pr.Sociability, v);
            Permille(p, "aggressiveness", pr.Aggressiveness, v);
            Permille(p, "patience", pr.Patience, v);
            Permille(p, "loyalty", pr.Loyalty, v);
            Permille(p, "riskAversion", pr.RiskAversion, v);
            Permille(p, "mechanicalSkill", pr.MechanicalSkill, v);
            Permille(p, "gameKnowledge", pr.GameKnowledge, v);
            Permille(p, "coordination", pr.Coordination, v);
            Permille(p, "cheatPropensity", pr.CheatPropensity, v);
            Permille(p, "opportunism", pr.Opportunism, v);
            Permille(p, "willingnessToPay", pr.WillingnessToPay, v);
            Permille(p, "paidAdvantageSensitivity", pr.PaidAdvantageSensitivity, v);
            Permille(p, "gameImportance", pr.GameImportance, v);
            Permille(p, "habit", pr.Habit, v);
            Permille(p, "talent", pr.Talent, v);
            Permille(p, "variety", pr.Variety, v);
            Permille(p, "extendTendency", pr.ExtendTendency, v);
            Permille(p, "selfControl", pr.SelfControl, v);
            Range(p, "preferredStartHour", pr.PreferredStartHour, 0, 23, v);
            Range(p, "sessionMinutes", pr.SessionMinutes, 10, 720, v);
            Range(p, "leisureBudgetCentsPerMonth", pr.LeisureBudgetCentsPerMonth, 0, 1_000_000, v);
        }
    }

    private static void Permille(string p, string name, TraitRange t, Collector v) => Range(p, name, t, 0, 1000, v);

    private static void Range(string p, string name, TraitRange t, int min, int max, Collector v, string file)
    {
        if (t.Mean < min || t.Mean > max) v.Error(file, $"{p}.{name}.mean", $"Use de {min} a {max}.");
        if (t.Spread < 0) v.Error(file, $"{p}.{name}.spread", "A variação não pode ser negativa.");
    }

    private static void Range(string p, string name, TraitRange t, int min, int max, Collector v)
    {
        if (t.Mean < min || t.Mean > max)
            v.Error(PopulationFile, $"{p}.{name}.mean", $"Média deve estar entre {min} e {max}.");
        if (t.Spread < 0 || t.Spread > (max - min))
            v.Error(PopulationFile, $"{p}.{name}.spread", $"Dispersão deve estar entre 0 e {max - min}.");
    }

    /// <summary>
    /// Nivel do desafio (D-027): cidade nao tem desafio (nao da XP); fora da cidade, tipo que
    /// rende XP precisa de desafio (declarado ou, em caca e chefe, pelas criaturas).
    /// </summary>
    private static void ValidateChallenge(ContentSet set, RegionDefinition r, ActivityDefinition act, string p, Collector v)
    {
        const string f = RegionsFile;
        var prog = set.Progression;
        if (act.ChallengeLevel < 0 || (prog.LevelCap > 0 && act.ChallengeLevel > prog.LevelCap))
            v.Error(f, p + ".challengeLevel", $"Nível do desafio deve estar entre 1 e o teto ({prog.LevelCap}).");
        if (r.IsCity)
        {
            if (act.ChallengeLevel > 0)
                v.Error(f, p + ".challengeLevel", "Atividades da cidade não têm desafio e não dão XP; o XP vem do que se faz no mundo.");
            return;
        }
        bool givesXp = prog.ActivityXpPerMinute.TryGetValue(act.Type, out int xp) && xp > 0;
        bool derived = (act.Type == ActivityTypes.Hunt || act.Type == ActivityTypes.Boss) && act.CreatureIds.Count > 0;
        if (givesXp && act.ChallengeLevel == 0 && !derived)
            v.Error(f, p + ".challengeLevel", $"Atividade do tipo '{act.Type}' dá XP e precisa do nível do desafio.");
    }

    /// <summary>Catalogo de quests (D-031): quadros, quests, etapas e recompensas.</summary>
    private static void ValidateQuests(ContentSet set, HashSet<string> classIds, HashSet<string> itemIds, Collector v)
    {
        const string f = QuestsFile;
        var cat = set.Quests;
        int cap = Math.Max(1, set.Progression.LevelCap);
        if (cat.Settings.MaxActivePerCharacter is < 1 or > 10)
            v.Error(f, "settings.maxActivePerCharacter", "Quests ativas por personagem: de 1 a 10.");
        var boardIds = CheckIds(cat.Boards, f, "boards", v);
        ActivityDefinition? Find(string region, string activity) =>
            set.Regions.FirstOrDefault(r => r.Id == region)?.FindActivity(activity);
        var boardActivities = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < cat.Boards.Count; i++)
        {
            var b = cat.Boards[i];
            string p = $"boards[{i}]";
            v.Require(f, p + ".name", b.Name);
            var region = set.Regions.FirstOrDefault(r => r.Id == b.RegionId);
            if (region == null) { v.Error(f, p + ".regionId", $"Zona '{b.RegionId}' não existe."); continue; }
            var area = region.Areas.FirstOrDefault(a => a.Id == b.AreaId);
            if (area == null) { v.Error(f, p + ".areaId", $"Área '{b.AreaId}' não existe em '{region.Id}'."); continue; }
            var act = area.Activities.FirstOrDefault(a => a.Id == b.ActivityId);
            if (act == null) v.Error(f, p + ".activityId", $"A área '{area.Id}' não tem a atividade '{b.ActivityId}'.");
            else if (act.Type != ActivityTypes.Quest) v.Error(f, p + ".activityId", "A atividade do quadro precisa ser do tipo quest (aceitar e entregar).");
            else if (!boardActivities.Add(region.Id + "/" + act.Id)) v.Error(f, p + ".activityId", "Dois quadros usam a mesma atividade.");
            if (b.DailyLimit is < 0 or > 10) v.Error(f, p + ".dailyLimit", "Diárias por dia de 0 a 10.");
        }

        var questIds = CheckIds(cat.Quests, f, "quests", v);
        for (int i = 0; i < cat.Quests.Count; i++)
        {
            var q = cat.Quests[i];
            string p = $"quests[{i}]";
            v.Require(f, p + ".name", q.Name);
            v.Require(f, p + ".description", q.Description);
            if (!boardIds.Contains(q.BoardId)) v.Error(f, p + ".boardId", $"Quadro '{q.BoardId}' não existe.");
            v.OneOf(f, p + ".category", q.Category, QuestCategories.All);
            if (q.RecommendedLevel < 1 || q.RecommendedLevel > cap) v.Error(f, p + ".recommendedLevel", $"Nível recomendado de 1 a {cap}.");
            if (q.MinLevel < 1 || q.MinLevel > cap) v.Error(f, p + ".minLevel", $"Nível mínimo de 1 a {cap}.");
            if (q.MaxLevel != 0 && q.MaxLevel < q.MinLevel) v.Error(f, p + ".maxLevel", "Nível máximo menor que o mínimo.");
            if (q.RepeatCooldownDays < 0) v.Error(f, p + ".repeatCooldownDays", "Intervalo de repetição não pode ser negativo.");
            if (q.Category == QuestCategories.Main && q.RepeatCooldownDays > 0) v.Error(f, p + ".repeatCooldownDays", "Quest principal é concluída uma vez só.");
            for (int k = 0; k < q.PrerequisiteQuestIds.Count; k++)
                if (!questIds.Contains(q.PrerequisiteQuestIds[k]) || q.PrerequisiteQuestIds[k] == q.Id)
                    v.Error(f, $"{p}.prerequisiteQuestIds[{k}]", $"Pré-requisito '{q.PrerequisiteQuestIds[k]}' inválido.");
            for (int k = 0; k < q.ClassIds.Count; k++)
                if (!classIds.Contains(q.ClassIds[k])) v.Error(f, $"{p}.classIds[{k}]", $"Classe '{q.ClassIds[k]}' não existe.");
            if (q.Steps.Count == 0) v.Error(f, p + ".steps", "A quest precisa de ao menos uma etapa.");
            for (int k = 0; k < q.Steps.Count; k++)
            {
                var s = q.Steps[k];
                string sp = $"{p}.steps[{k}]";
                v.OneOf(f, sp + ".type", s.Type, QuestStepTypes.All);
                v.Require(f, sp + ".text", s.Text);
                var act = Find(s.RegionId, s.ActivityId);
                if (act == null) { v.Error(f, sp + ".activityId", $"Atividade '{s.RegionId}/{s.ActivityId}' não existe."); continue; }
                if (s.Type == QuestStepTypes.Activity)
                {
                    if (act.Type is ActivityTypes.Quest or ActivityTypes.Trade or ActivityTypes.Repair or ActivityTypes.Socialize or ActivityTypes.Train or ActivityTypes.Recruit)
                        v.Error(f, sp + ".activityId", $"Etapa de atividade precisa ser algo feito no mundo (caça, coleta, exploração, patrulha...), não '{act.Type}'.");
                    if (act.MinGroupSize > 1) v.Error(f, sp + ".activityId", "Etapa exige grupo; grupos chegam no M6.");
                    if (s.Minutes is < 5 or > 600) v.Error(f, sp + ".minutes", "Etapa de atividade: de 5 a 600 minutos.");
                }
                else if (s.Type == QuestStepTypes.Visit)
                {
                    if (act.Type != ActivityTypes.Quest) v.Error(f, sp + ".activityId", "Etapa de visita precisa de um ponto de missão (atividade do tipo quest).");
                    else if (boardActivities.Contains(s.RegionId + "/" + s.ActivityId)) v.Error(f, sp + ".activityId", "O quadro não é ponto de visita; a entrega no quadro é automática no fim.");
                    if (s.Minutes is < 1 or > 120) v.Error(f, sp + ".minutes", "Visita: de 1 a 120 minutos.");
                }
                if (act.MinLevel > q.MinLevel)
                    v.Error(f, sp + ".activityId", $"A etapa exige nível {act.MinLevel}, acima do nível mínimo da quest ({q.MinLevel}).");
            }
            if (q.Rewards.XpPermille is < 0 or > 3000) v.Error(f, p + ".rewards.xpPermille", "XP da recompensa de 0 a 3000 permille do nível.");
            if (q.Rewards.GoldCopper < 0) v.Error(f, p + ".rewards.goldCopper", "Ouro não pode ser negativo.");
            for (int k = 0; k < q.Rewards.ItemIds.Count; k++)
                if (!itemIds.Contains(q.Rewards.ItemIds[k])) v.Error(f, $"{p}.rewards.itemIds[{k}]", $"Item '{q.Rewards.ItemIds[k]}' não existe.");
        }

        // Pre-requisitos sem ciclo.
        var byId = cat.Quests.Where(q => q.Id.Length > 0).GroupBy(q => q.Id).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
        foreach (var q in cat.Quests)
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            var stack = new Stack<string>(q.PrerequisiteQuestIds);
            while (stack.Count > 0)
            {
                string id = stack.Pop();
                if (id == q.Id) { v.Error(f, $"quests[{cat.Quests.IndexOf(q)}].prerequisiteQuestIds", "Pré-requisitos formam um ciclo."); break; }
                if (!seen.Add(id) || !byId.TryGetValue(id, out var pre)) continue;
                foreach (var x in pre.PrerequisiteQuestIds) stack.Push(x);
            }
        }
    }

    private static int AverageCreatureLevel(ContentSet set, ActivityDefinition a)
    {
        if (a.Type != ActivityTypes.Hunt && a.Type != ActivityTypes.Boss) return 0;
        var levels = a.CreatureIds.Select(id => set.Creatures.FirstOrDefault(c => c.Id == id)).Where(c => c != null).Select(c => c!.Level).ToList();
        return levels.Count == 0 ? 0 : levels.Sum() / levels.Count;
    }

    private static void ValidateProgression(ContentSet set, Collector v)
    {
        const string f = ProgressionFile;
        var p = set.Progression;
        if (p.LevelCap < 2 || p.LevelCap > 100)
            v.Error(f, "levelCap", "Teto de nível deve estar entre 2 e 100.");
        var c = p.XpCurve;
        if (c.Cubic < 0 || c.Quadratic < 0 || c.Linear < 0 || c.Constant < 0)
            v.Error(f, "xpCurve", "Coeficientes da curva não podem ser negativos (a curva precisa crescer com o nível).");
        else if (c.Cubic + c.Quadratic + c.Linear + c.Constant <= 0)
            v.Error(f, "xpCurve", "A curva precisa exigir XP (algum coeficiente maior que zero).");
        else if (c.Cubic + c.Quadratic + c.Linear == 0)
            v.Warning(f, "xpCurve", "Curva constante: todos os níveis exigem o mesmo XP.");
        if (p.Stages.Count == 0)
            v.Error(f, "stages", "Defina ao menos uma faixa de taxa (fromLevel 1).");
        for (int i = 0; i < p.Stages.Count; i++)
        {
            var s = p.Stages[i];
            string sp = $"stages[{i}]";
            if (i == 0 && s.FromLevel != 1) v.Error(f, sp + ".fromLevel", "A primeira faixa deve começar no nível 1.");
            if (i > 0 && s.FromLevel <= p.Stages[i - 1].FromLevel) v.Error(f, sp + ".fromLevel", "Faixas devem estar em ordem crescente de nível, sem repetir.");
            if (s.FromLevel < 1 || s.FromLevel > Math.Max(1, p.LevelCap)) v.Error(f, sp + ".fromLevel", "Nível inicial da faixa fora de 1..teto.");
            if (s.RatePermille < 1 || s.RatePermille > 20000) v.Error(f, sp + ".ratePermille", "Taxa deve estar entre 1 e 20000 permille (×0,001 a ×20).");
        }
        foreach (var kv in p.ActivityXpPerMinute.OrderBy(k => k.Key, StringComparer.Ordinal))
        {
            string kp = $"activityXpPerMinute.{kv.Key}";
            if (!ActivityTypes.All.Contains(kv.Key)) v.Error(f, kp, $"Tipo de atividade '{kv.Key}' desconhecido. Válidos: {string.Join(", ", ActivityTypes.All)}.");
            else if (ActivityTypes.CityOnly.Contains(kv.Key) || kv.Key == ActivityTypes.Socialize)
                v.Error(f, kp, "Serviços e convívio na cidade não dão XP.");
            if (kv.Value < 0 || kv.Value > 10000) v.Error(f, kp, "XP por minuto deve estar entre 0 e 10000.");
        }
        if (p.ChallengeGrowthPermille < 0 || p.ChallengeGrowthPermille > 5000)
            v.Error(f, "challengeGrowthPermille", "Crescimento por nível do desafio deve estar entre 0 e 5000 permille.");
        var d = p.LevelDifference;
        if (d.ToleranceLevels < 0 || d.ToleranceLevels > 20) v.Error(f, "levelDifference.toleranceLevels", "Tolerância deve estar entre 0 e 20 níveis.");
        if (d.WeakChallengePenaltyPermille < 0 || d.WeakChallengePenaltyPermille > 1000) v.Error(f, "levelDifference.weakChallengePenaltyPermille", "Perda deve estar entre 0 e 1000 permille por nível.");
        if (d.StrongChallengePenaltyPermille < 0 || d.StrongChallengePenaltyPermille > 1000) v.Error(f, "levelDifference.strongChallengePenaltyPermille", "Perda deve estar entre 0 e 1000 permille por nível.");
        if (d.WeakChallengeFloorPermille < 0 || d.WeakChallengeFloorPermille > 1000) v.Error(f, "levelDifference.weakChallengeFloorPermille", "Rendimento mínimo deve estar entre 0 e 1000 permille.");
    }

    private sealed class Collector
    {
        public List<ValidationIssue> Issues { get; } = new();

        public void Error(string file, string path, string msg) => Issues.Add(new ValidationIssue(IssueSeverity.Error, file, path, msg));
        public void Warning(string file, string path, string msg) => Issues.Add(new ValidationIssue(IssueSeverity.Warning, file, path, msg));

        public void Require(string file, string path, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) Error(file, path, "Campo obrigatório.");
        }

        public void OneOf(string file, string path, string value, string[] allowed)
        {
            if (!allowed.Contains(value))
                Error(file, path, $"Valor '{value}' inválido. Válidos: {string.Join(", ", allowed.Where(a => a.Length > 0))}.");
        }
    }
}

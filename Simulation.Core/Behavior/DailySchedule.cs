using Simulation.Core.Content;
using Simulation.Core.Population;

namespace Simulation.Core.Behavior;

/// <summary>
/// Dia local de uma pessoa (P2, D-043): obrigacoes da ocupacao (com deslocamento e variacao
/// individual), sono, preparo e chegada; o resto sao as janelas livres. Logica pura em minutos
/// a partir da meia-noite local do dia (podem passar de 1440 quando o dia vai ate a madrugada).
/// Ter janela livre nao significa jogar: quem decide e a vontade de jogar (BehaviorSystem).
/// </summary>
public static class DailySchedule
{
    public readonly record struct Busy(int Start, int End, string Label, bool Remote = false);

    /// <summary>Bloco de trabalho remoto (D-044): pausas permitidas e flexibilidade para jogar.</summary>
    public readonly record struct RemoteWork(int Start, int End, IReadOnlyList<(int Start, int End)> Breaks);

    public sealed class DayLayout
    {
        public List<Busy> Blocks { get; } = new();
        /// <summary>Blocos remotos de hoje: unico lugar onde jogar durante o trabalho pode ser permitido.</summary>
        public List<RemoteWork> Remote { get; } = new();
        /// <summary>Sono a mais nesta manha para recuperar o deficit (P3).</summary>
        public int RecoveryMinutes { get; internal set; }
        /// <summary>A noite anterior terminou num turno (dorme de manha).</summary>
        public bool DaySleeper { get; internal set; }
        /// <summary>O dia termina saindo para um turno da noite: o fim da ultima janela nao e a hora de dormir.</summary>
        public bool NightShiftAhead { get; internal set; }
        public int Wake { get; internal set; }
        public int Bed { get; internal set; }
        public List<(int Start, int End)> Windows { get; } = new();
        /// <summary>Sem obrigacoes hoje (folga, fim de semana, feriado, sem ocupacao).</summary>
        public bool FreeDay { get; internal set; }
        public int FreeMinutes => Windows.Sum(w => w.End - w.Start);
        /// <summary>Compromisso ocasional do dia livre (D-046): inicio, fim e id; nao conta como obrigacao.</summary>
        public List<(int Start, int End, string Id)> Commitments { get; } = new();
        /// <summary>Minutos de obrigacao hoje, com deslocamento (carga do dia).</summary>
        public int ObligationMinutes => Blocks.Sum(b => b.End - b.Start);
    }

    /// <summary>Obrigacoes do dia da semana (0 = segunda): inicio, fim (pode passar de 1440), deslocamento e rotulo.</summary>
    public static List<(int Start, int End, int Commute, string Label)> Obligations(Person p, OccupationDefinition? occ, int dow, bool holiday) =>
        Blocks(p, occ, dow, holiday).Select(b => (b.Start, b.End, b.Commute, b.Label)).ToList();

    /// <summary>Obrigacoes com o bloco de origem (remoto, pausas).</summary>
    public static List<(int Start, int End, int Commute, string Label, ObligationBlockDefinition Def)> Blocks(Person p, OccupationDefinition? occ, int dow, bool holiday)
    {
        var list = new List<(int Start, int End, int Commute, string Label, ObligationBlockDefinition Def)>();
        if (occ == null || (holiday && !occ.WorksHolidays)) return list;
        foreach (var b in occ.Blocks)
        {
            if (!b.Days.Any(d => ContentValidator.WeekDayIndex(d) == dow)) continue;
            if (b.PersonalDayOff && p.DayOffIndex == dow) continue;
            int s = ContentValidator.ParseClock(b.Start), e = ContentValidator.ParseClock(b.End);
            if (s < 0 || e < 0) continue;
            if (e <= s) e += 1440;
            list.Add((s + p.ShiftOffsetMinutes, e + p.ShiftOffsetMinutes, b.CommuteMinutes * p.CommutePermille / 1000, b.Label, b));
        }
        list.Sort((a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : string.CompareOrdinal(a.Label, b.Label));
        return list;
    }

    /// <summary>
    /// Monta o dia: quem trabalhou a noite dorme de manha; os outros acordam a tempo da primeira
    /// obrigacao (ou na hora natural) e dormem a tempo da obrigacao do dia seguinte (o deficit de
    /// sono fica para a P3). Janelas livres = do acordar + preparo ate dormir, sem obrigacoes nem chegada.
    /// </summary>
    public static DayLayout Build(Person p, OccupationDefinition? occ, RoutineSettingsDefinition rs, int dow, bool holiday, bool prevHoliday, bool nextHoliday,
        (int Start, int End, string Id)? commitment = null)
    {
        var todayBlocks = Blocks(p, occ, dow, holiday);
        var today = todayBlocks.Select(b => (b.Start, b.End, b.Commute, b.Label)).ToList();
        var prev = Obligations(p, occ, (dow + 6) % 7, prevHoliday);
        var next = Obligations(p, occ, (dow + 1) % 7, nextHoliday);
        var day = new DayLayout();

        int spillEnd = -1;   // turno da noite anterior que termina hoje
        foreach (var o in prev)
            if (o.End > 1440) spillEnd = Math.Max(spillEnd, o.End - 1440 + o.Commute);
        if (spillEnd > 0) day.Blocks.Add(new Busy(0, spillEnd, prev.First(o => o.End > 1440).Label));
        foreach (var o in todayBlocks)
        {
            day.Blocks.Add(new Busy(Math.Max(0, o.Start - o.Commute), o.End + o.Commute, o.Label, o.Def.Remote));
            if (o.Def.Remote)
                day.Remote.Add(new RemoteWork(o.Start, o.End, o.Def.Breaks
                    .Select(br => (ContentValidator.ParseClock(br.Start) + p.ShiftOffsetMinutes, ContentValidator.ParseClock(br.End) + p.ShiftOffsetMinutes))
                    .Where(br => br.Item1 >= o.Start && br.Item2 <= o.End).ToList()));
        }
        day.FreeDay = day.Blocks.Count == 0;

        // Recuperacao (P3): sem obrigacao cedo, dorme mais para pagar o deficit.
        int recovery = Math.Min(p.SleepDeficitMinutes, rs.RecoveryFreeNightMinutes);
        int wake;
        if (spillEnd > 0)
        {
            wake = spillEnd + rs.ArrivalMinutes + p.SleepNeedMinutes + recovery;   // dorme depois do turno
            day.DaySleeper = true;
        }
        else
        {
            int natural = Math.Max(0, p.BedtimeMinute + p.SleepNeedMinutes - 1440);
            int limit = today.Count > 0 ? today[0].Start - today[0].Commute - rs.PrepMinutes : int.MaxValue;
            wake = Math.Max(0, Math.Min(natural + recovery, limit));
            if (wake > natural) day.RecoveryMinutes = wake - natural;
        }
        if (spillEnd > 0) day.RecoveryMinutes = recovery;

        int bed;
        var night = today.Where(o => o.End > 1440).ToList();
        if (night.Count > 0) { bed = night[0].Start - night[0].Commute; day.NightShiftAhead = true; }   // sai para o turno da noite
        else
        {
            bed = p.BedtimeMinute;
            if (next.Count > 0) bed = Math.Min(bed, 1440 + next[0].Start - next[0].Commute - rs.PrepMinutes - p.SleepNeedMinutes);
            int lastEnd = day.Blocks.Count == 0 ? 0 : day.Blocks.Max(b => b.End);
            bed = Math.Max(bed, lastEnd + rs.ArrivalMinutes);   // ninguem dorme antes de voltar e comer
        }
        day.Wake = wake;
        day.Bed = bed;

        // Compromisso do dia livre (D-046): dentro do dia acordado, saindo do tempo livre.
        if (commitment is { } c)
        {
            int cs = Math.Max(c.Start, wake + rs.PrepMinutes), ce = Math.Min(c.End, bed - 15);
            if (ce - cs >= 30) day.Commitments.Add((cs, ce, c.Id));
        }
        // Janelas: [acordar + preparo, dormir) menos obrigacoes e compromissos (com chegada depois de cada um).
        var taken = day.Blocks.Select(b => (b.Start, End: b.End + rs.ArrivalMinutes))
            .Concat(day.Commitments.Select(c => (c.Start, End: c.End + rs.ArrivalMinutes))).OrderBy(b => b.Start).ToList();
        int cursor = wake + rs.PrepMinutes;
        foreach (var (s, e) in taken)
        {
            if (e <= cursor) continue;
            if (s > cursor) AddWindow(day, cursor, Math.Min(s, bed), rs);
            cursor = Math.Max(cursor, e);
            if (cursor >= bed) break;
        }
        AddWindow(day, cursor, bed, rs);
        return day;
    }

    private static void AddWindow(DayLayout day, int start, int end, RoutineSettingsDefinition rs)
    {
        if (end - start >= rs.MinWindowMinutes) day.Windows.Add((start, end));
    }

    /// <summary>
    /// Chance de jogar no dia antes do habito e do humor (permille): a importancia do jogo; o
    /// tempo livre aumenta a chance de quem da pouca importancia (mais lazer disponivel) e a
    /// carga de obrigacoes a reduz.
    /// </summary>
    public static int BaseChance(Person p, DayLayout day, RoutineSettingsDefinition rs)
    {
        int imp = p.Traits.GameImportance;
        int freeness = Math.Min(1000, day.FreeMinutes * 1000 / Math.Max(1, rs.FullFreeMinutes));
        int chance = imp + (1000 - imp) * rs.FreeDayChanceBonus / 1000 * freeness / 1000;
        // Cansaco do dia de obrigacoes: depois de trabalhar ou estudar o dia todo, joga menos,
        // a nao ser que o jogo seja muito importante para a pessoa (a dedicacao supera o cansaco).
        int load = Math.Min(1000, day.ObligationMinutes * 1000 / Math.Max(1, rs.FullLoadMinutes));
        chance = chance * (1000 - load * rs.BusyDayPenaltyPermille / 1000 * (1000 - imp) / 1000) / 1000;
        return Math.Clamp(chance, 0, 1000);
    }

    /// <summary>Duracao desejada: a habitual; em dia livre, mais longa conforme a importancia.</summary>
    public static int DesiredMinutes(Person p, DayLayout day, RoutineSettingsDefinition rs, int jitterPermille = 1000)
    {
        int m = p.Traits.SessionMinutes * jitterPermille / 1000;
        // Dia com muito tempo livre (dia livre, meio periodo, estudante a tarde): todo mundo joga um
        // pouco mais (base) e quem prioriza o jogo joga bem mais (importancia) (D-045).
        int freeness = Math.Min(1000, day.FreeMinutes * 1000 / Math.Max(1, rs.FullFreeMinutes));
        int boost = rs.FreeTimeDurationBase + p.Traits.GameImportance * rs.FreeDayDurationPerImportance / 1000;
        m = m * (1000 + boost * freeness / 1000) / 1000;
        return Math.Max(rs.MinSessionMinutes, m);
    }

    /// <summary>
    /// Escolhe a janela e o inicio: a que contem o horario preferido (ou a mais proxima; empate,
    /// a mais longa); comeca mais cedo se a sessao desejada nao couber depois do preferido.
    /// Devolve false se nenhuma janela comporta a sessao minima.
    /// </summary>
    public static bool PlaceMain(DayLayout day, int preferredMinute, int desired, RoutineSettingsDefinition rs, out int window, out int start, out int minutes)
    {
        window = -1; start = 0; minutes = 0;
        if (preferredMinute < day.Wake) preferredMinute += 1440;   // preferido de madrugada: depois da meia-noite
        int best = -1, bestDist = int.MaxValue, bestLen = 0;
        for (int i = 0; i < day.Windows.Count; i++)
        {
            var (s, e) = day.Windows[i];
            if (e - s < rs.MinSessionMinutes) continue;
            int dist = preferredMinute < s ? s - preferredMinute : preferredMinute > e ? preferredMinute - e : 0;
            if (dist < bestDist || (dist == bestDist && e - s > bestLen)) { best = i; bestDist = dist; bestLen = e - s; }
        }
        if (best < 0) return false;
        var (ws, we) = day.Windows[best];
        start = Math.Max(ws, Math.Min(preferredMinute, we - desired));
        minutes = Math.Min(desired, we - start);
        window = best;
        return minutes >= rs.MinSessionMinutes;
    }

    /// <summary>Hora de deitar exigida em cada noite que antecede um dia de obrigacao (sem turno da noite).</summary>
    public static List<int> RequiredBedtimes(Person p, OccupationDefinition? occ, RoutineSettingsDefinition rs)
    {
        var list = new List<int>();
        for (int dow = 0; dow < 7; dow++)
        {
            var next = Obligations(p, occ, (dow + 1) % 7, false);
            if (next.Count == 0) continue;
            list.Add(1440 + next[0].Start - next[0].Commute - rs.PrepMinutes - p.SleepNeedMinutes);
        }
        return list;
    }

    /// <summary>Ocupacao com turno que atravessa a meia-noite (dorme de dia).</summary>
    public static bool HasNightShift(OccupationDefinition? occ) =>
        occ != null && occ.Blocks.Any(b => ContentValidator.ParseClock(b.End) >= 0 && ContentValidator.ParseClock(b.End) <= ContentValidator.ParseClock(b.Start));

    /// <summary>
    /// Hora natural de dormir coerente com a rotina (D-045): quem acorda cedo para as obrigacoes
    /// se acostuma a dormir cedo; no dia livre dorme no maximo bedtimeAdaptMinutes mais tarde que
    /// a hora tipica (mediana) das noites antes de obrigacao. Turno da noite segue o proprio ritmo.
    /// </summary>
    public static int AdaptedBedtime(Person p, OccupationDefinition? occ, RoutineSettingsDefinition rs)
    {
        if (HasNightShift(occ)) return p.BedtimeMinute;
        var required = RequiredBedtimes(p, occ, rs);
        if (required.Count == 0) return p.BedtimeMinute;
        required.Sort();
        int limit = required[required.Count / 2] + rs.BedtimeAdaptMinutes;
        return limit < p.BedtimeMinute ? Math.Max(1200, limit / 5 * 5) : p.BedtimeMinute;
    }

    /// <summary>
    /// Hora preferida coerente com a rotina e com a vontade de jogar (D-045). Em cada dia do
    /// habito, na janela mais perto do gosto de horario sorteado pelo perfil:
    /// - quem da muita importancia ao jogo comeca logo que fica livre (chegou, comeu, ja joga);
    /// - quem da pouca espera mais (ate preferredDelayMaxMinutes) e segue mais o proprio horario
    ///   de lazer sorteado;
    /// - sempre de modo que a sessao tipica caiba inteira antes de dormir.
    /// A hora final e a hora cheia em que isso vale no maior numero de dias (empate: mais perto
    /// do alvo). Sem sorteio: so ajusta o que ja foi sorteado.
    /// </summary>
    public static int CoherentPreferredHour(Person p, OccupationDefinition? occ, RoutineSettingsDefinition rs)
    {
        int drawn = p.Traits.PreferredStartHour;
        var week = new List<DayLayout>();
        for (int d = 0; d < 7; d++) week.Add(Build(p, occ, rs, d, false, false, false));
        var (pool, session) = PreferredPool(week, Math.Max(rs.MinSessionMinutes, p.Traits.SessionMinutes), rs);
        if (pool.Count == 0) return drawn;
        int imp = Math.Clamp(p.Traits.GameImportance, 0, 1000);
        var targets = new List<int>();
        foreach (var d in pool)
        {
            int taste = drawn * 60 < d.Wake ? drawn * 60 + 1440 : drawn * 60;
            var fit = d.Windows.Where(w => w.End - w.Start >= session).ToList();
            if (fit.Count == 0) fit = d.Windows.Where(w => w.End - w.Start >= rs.MinSessionMinutes).ToList();
            if (fit.Count == 0) continue;
            int Near((int Start, int End) w) => taste < w.Start ? w.Start - taste : taste > w.End ? taste - w.End : 0;
            var w0 = fit.OrderBy(Near).ThenByDescending(w => w.End - w.Start).First();
            int latest = Math.Max(w0.Start, w0.End - Math.Min(session, w0.End - w0.Start));
            // Falta de interesse ao quadrado: importancia 15/20 ja comeca quase na hora; so o casual
            // espera de verdade e segue o proprio horario de lazer.
            int laziness = (1000 - imp) * (1000 - imp) / 1000;
            int eager = w0.Start + laziness * rs.PreferredDelayMaxMinutes / 1000;
            int natural = Math.Max(taste, eager);
            int t = eager + (natural - eager) * laziness / 1000;
            targets.Add(Math.Clamp(t, w0.Start, latest));
        }
        if (targets.Count == 0) return drawn;
        targets.Sort();
        int target = targets[targets.Count / 2];
        static int Circ(int a, int b) { int x = ((a - b) % 1440 + 1440) % 1440; return Math.Min(x, 1440 - x); }
        // Mesma conta do planejador do dia: a sessao tipica comeca na hora ou ate 59 min depois
        // (a janela abre um pouco depois) e cabe inteira ate dormir, sem ser puxada para antes.
        int Fits(int hour) => pool.Count(d => StartsAtPreferred(d, hour, session, rs));
        return Enumerable.Range(0, 24).OrderByDescending(Fits).ThenBy(h => Circ(h * 60, target)).ThenBy(h => h).First();
    }

    /// <summary>
    /// Dias que definem o habito de horario e a sessao considerada (D-045): nos dias com
    /// obrigacao, a sessao tipica limitada a maior janela que eles tem (quem joga muito joga o
    /// que der); valem os dias com essa janela. Uma brecha curta antes do trabalho nao vira hora
    /// preferida se outro dia com obrigacao tem mais tempo. Sem tempo nesses dias: os dias livres.
    /// </summary>
    public static (List<DayLayout> Pool, int Session) PreferredPool(IReadOnlyList<DayLayout> week, int session, RoutineSettingsDefinition rs)
    {
        bool Has(DayLayout d, int len) => d.Windows.Any(w => w.End - w.Start >= len);
        var busy = week.Where(d => !d.FreeDay).ToList();
        int largest = busy.SelectMany(d => d.Windows).Select(w => w.End - w.Start).DefaultIfEmpty(0).Max();
        if (largest >= rs.MinSessionMinutes)
        {
            int eff = Math.Min(session, largest);
            return (busy.Where(d => Has(d, eff)).ToList(), eff);
        }
        var free = week.Where(d => d.FreeDay && Has(d, rs.MinSessionMinutes)).ToList();
        if (free.Count > 0)
        {
            int eff = Math.Min(session, free.SelectMany(d => d.Windows).Max(w => w.End - w.Start));
            return (free.Where(d => Has(d, eff)).ToList(), eff);
        }
        return (new List<DayLayout>(), session);
    }

    /// <summary>
    /// A sessao de "session" minutos, preferida as "hour" h, comeca na hora (ou ate 59 min depois,
    /// quando a janela abre um pouco depois) e cabe inteira, sem ser puxada para antes (D-045).
    /// </summary>
    public static bool StartsAtPreferred(DayLayout day, int hour, int session, RoutineSettingsDefinition rs)
    {
        int pref = hour * 60 < day.Wake ? hour * 60 + 1440 : hour * 60;   // como no PlaceMain
        return PlaceMain(day, hour * 60, session, rs, out _, out int start, out _) && start >= pref && start - pref < 60;
    }

    /// <summary>Cansaco (permille) pelo deficit de sono (P3).</summary>
    public static int Fatigue(Person p, RoutineSettingsDefinition rs) =>
        Math.Min(1000, p.SleepDeficitMinutes * 1000 / Math.Max(1, rs.FatigueFullDeficitMinutes));

    /// <summary>Teto do deficit da pessoa: o geral escalado pela tolerancia dela.</summary>
    public static int MaxDeficit(Person p, RoutineSettingsDefinition rs) =>
        rs.MaxDeficitMinutes * (500 + p.SleepDebtTolerance) / 1000;

    /// <summary>Minutos esperados numa semana tipica (sem feriados, habito e humor neutros): base do "ritmo de jogo".</summary>
    /// <summary>Previsto de um dia pela rotina (sem humor): chance com o habito, sessao principal e segunda sessao.</summary>
    public readonly record struct DayExpectation(int ChancePermille, int Start, int Minutes, int ExpectedMinutes);

    /// <summary>
    /// Previsto do dia (D-045), a mesma regra do planejador sem o humor:
    /// - chance = rotina com o habito proporcional (quem jogou ontem tende a jogar hoje), na
    ///   frequencia de longo prazo da cadeia "jogou ontem?": a / (1 - b + a);
    /// - sessao principal na janela da hora preferida, mais longa com tempo livre e importancia;
    /// - segunda sessao em outra janela, para quem prioriza muito o jogo.
    /// </summary>
    public static DayExpectation Expect(Person p, DayLayout day, RoutineSettingsDefinition rs)
    {
        if (!PlaceMain(day, p.Traits.PreferredStartHour * 60, DesiredMinutes(p, day, rs), rs, out int window, out int start, out int minutes))
            return new DayExpectation(0, -1, 0, 0);
        int baseChance = BaseChance(p, day, rs);
        int streak = p.Traits.Habit * rs.HabitStreakPermille / 1000;
        int a = Math.Max(0, baseChance * (1000 - streak) / 1000), b = Math.Min(1000, baseChance * (1000 + streak) / 1000);
        int chance = 1000 - b + a <= 0 ? 1000 : (int)((long)a * 1000 / (1000 - b + a));
        long expected = (long)minutes * chance / 1000;
        int imp = p.Traits.GameImportance;
        if (rs.MaxSessionsPerDay > 1 && imp > rs.SecondSessionFromImportance)
        {
            int c2 = (imp - rs.SecondSessionFromImportance) * rs.SecondSessionPermilleAtMax / Math.Max(1, 1000 - rs.SecondSessionFromImportance);
            int other = -1;
            for (int i = 0; i < day.Windows.Count; i++)
            {
                if (i == window || day.Windows[i].End - day.Windows[i].Start < rs.MinSessionMinutes) continue;
                if (other < 0 || day.Windows[i].End - day.Windows[i].Start > day.Windows[other].End - day.Windows[other].Start) other = i;
            }
            if (other >= 0)
            {
                var (s2, e2) = day.Windows[other];
                int m2 = Math.Min(e2 - s2, Math.Max(rs.MinSessionMinutes, p.Traits.SessionMinutes * rs.SecondSessionDurationPermille / 1000));
                expected += (long)m2 * chance / 1000 * c2 / 1000;
            }
        }
        return new DayExpectation(chance, start, minutes, (int)expected);
    }

    public static int ExpectedWeeklyMinutes(Person p, OccupationDefinition? occ, RoutineSettingsDefinition rs)
    {
        long total = 0;
        for (int dow = 0; dow < 7; dow++) total += Expect(p, Build(p, occ, rs, dow, false, false, false), rs).ExpectedMinutes;
        return (int)total;
    }
}

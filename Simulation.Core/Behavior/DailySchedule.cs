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
    public readonly record struct Busy(int Start, int End, string Label);

    public sealed class DayLayout
    {
        public List<Busy> Blocks { get; } = new();
        public int Wake { get; internal set; }
        public int Bed { get; internal set; }
        public List<(int Start, int End)> Windows { get; } = new();
        /// <summary>Sem obrigacoes hoje (folga, fim de semana, feriado, sem ocupacao).</summary>
        public bool FreeDay { get; internal set; }
        public int FreeMinutes => Windows.Sum(w => w.End - w.Start);
        /// <summary>Minutos de obrigacao hoje, com deslocamento (carga do dia).</summary>
        public int ObligationMinutes => Blocks.Sum(b => b.End - b.Start);
    }

    /// <summary>Obrigacoes do dia da semana (0 = segunda): inicio, fim (pode passar de 1440), deslocamento e rotulo.</summary>
    public static List<(int Start, int End, int Commute, string Label)> Obligations(Person p, OccupationDefinition? occ, int dow, bool holiday)
    {
        var list = new List<(int Start, int End, int Commute, string Label)>();
        if (occ == null || (holiday && !occ.WorksHolidays)) return list;
        foreach (var b in occ.Blocks)
        {
            if (!b.Days.Any(d => ContentValidator.WeekDayIndex(d) == dow)) continue;
            if (b.PersonalDayOff && p.DayOffIndex == dow) continue;
            int s = ContentValidator.ParseClock(b.Start), e = ContentValidator.ParseClock(b.End);
            if (s < 0 || e < 0) continue;
            if (e <= s) e += 1440;
            list.Add((s + p.ShiftOffsetMinutes, e + p.ShiftOffsetMinutes, b.CommuteMinutes * p.CommutePermille / 1000, b.Label));
        }
        list.Sort((a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : string.CompareOrdinal(a.Label, b.Label));
        return list;
    }

    /// <summary>
    /// Monta o dia: quem trabalhou a noite dorme de manha; os outros acordam a tempo da primeira
    /// obrigacao (ou na hora natural) e dormem a tempo da obrigacao do dia seguinte (o deficit de
    /// sono fica para a P3). Janelas livres = do acordar + preparo ate dormir, sem obrigacoes nem chegada.
    /// </summary>
    public static DayLayout Build(Person p, OccupationDefinition? occ, RoutineSettingsDefinition rs, int dow, bool holiday, bool prevHoliday, bool nextHoliday)
    {
        var today = Obligations(p, occ, dow, holiday);
        var prev = Obligations(p, occ, (dow + 6) % 7, prevHoliday);
        var next = Obligations(p, occ, (dow + 1) % 7, nextHoliday);
        var day = new DayLayout();

        int spillEnd = -1;   // turno da noite anterior que termina hoje
        foreach (var o in prev)
            if (o.End > 1440) spillEnd = Math.Max(spillEnd, o.End - 1440 + o.Commute);
        if (spillEnd > 0) day.Blocks.Add(new Busy(0, spillEnd, prev.First(o => o.End > 1440).Label));
        foreach (var o in today) day.Blocks.Add(new Busy(Math.Max(0, o.Start - o.Commute), o.End + o.Commute, o.Label));
        day.FreeDay = day.Blocks.Count == 0;

        int wake;
        if (spillEnd > 0)
            wake = spillEnd + rs.ArrivalMinutes + p.SleepNeedMinutes;   // dorme depois do turno
        else
        {
            int natural = Math.Max(0, p.BedtimeMinute + p.SleepNeedMinutes - 1440);
            wake = today.Count > 0 ? Math.Max(0, Math.Min(natural, today[0].Start - today[0].Commute - rs.PrepMinutes)) : natural;
        }

        int bed;
        var night = today.Where(o => o.End > 1440).ToList();
        if (night.Count > 0) bed = night[0].Start - night[0].Commute;   // sai para o turno da noite
        else
        {
            bed = p.BedtimeMinute;
            if (next.Count > 0) bed = Math.Min(bed, 1440 + next[0].Start - next[0].Commute - rs.PrepMinutes - p.SleepNeedMinutes);
            int lastEnd = day.Blocks.Count == 0 ? 0 : day.Blocks.Max(b => b.End);
            bed = Math.Max(bed, lastEnd + rs.ArrivalMinutes);   // ninguem dorme antes de voltar e comer
        }
        day.Wake = wake;
        day.Bed = bed;

        // Janelas: [acordar + preparo, dormir) menos obrigacoes (com chegada depois de cada uma).
        var taken = day.Blocks.Select(b => (b.Start, End: b.End + rs.ArrivalMinutes)).OrderBy(b => b.Start).ToList();
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
        if (day.FreeDay) m = m * (1000 + p.Traits.GameImportance * rs.FreeDayDurationPerImportance / 1000) / 1000;
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

    /// <summary>Minutos esperados numa semana tipica (sem feriados, habito e humor neutros): base do "ritmo de jogo".</summary>
    public static int ExpectedWeeklyMinutes(Person p, OccupationDefinition? occ, RoutineSettingsDefinition rs)
    {
        long total = 0;
        for (int dow = 0; dow < 7; dow++)
        {
            var day = Build(p, occ, rs, dow, false, false, false);
            if (!PlaceMain(day, p.Traits.PreferredStartHour * 60, DesiredMinutes(p, day, rs), rs, out _, out _, out int minutes)) continue;
            total += (long)minutes * BaseChance(p, day, rs) / 1000;
        }
        return (int)total;
    }
}

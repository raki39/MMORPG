using Simulation.Core.Content;
using Simulation.Core.Random;
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

    /// <summary>Trecho do dia (minutos locais, fim exclusivo) com o que a pessoa faz nele.</summary>
    public readonly record struct Segment(int Start, int End, string Kind);

    /// <summary>
    /// Dia acordado da pessoa (R1): comeca no sono que termina hoje (SleepStart, pode ser negativo:
    /// deitou ontem) e acaba quando ela vai dormir (Bed, pode passar de 1440; depois de um turno da
    /// noite e a manha seguinte). O proximo dia comeca exatamente onde este termina, entao cada
    /// minuto pertence a um unico dia. Minutos locais a partir da meia-noite de hoje.
    /// </summary>
    public sealed class DayLayout
    {
        /// <summary>Periodos fora de casa ou ocupados (obrigacao com deslocamento; blocos em sequencia ficam contiguos).</summary>
        public List<Busy> Blocks { get; } = new();
        /// <summary>Blocos remotos de hoje: unico lugar onde jogar durante o trabalho pode ser permitido.</summary>
        public List<RemoteWork> Remote { get; } = new();
        /// <summary>Partes acordadas do dia, sem sobreposicao: preparo, deslocamento, obrigacao, refeicao, transferencia, espera, chegada, compromisso.</summary>
        public List<Segment> Parts { get; } = new();
        /// <summary>Inicio do sono que termina em Wake (o fim do dia acordado de ontem, nas coordenadas de hoje).</summary>
        public int SleepStart { get; internal set; }
        /// <summary>Sono a mais nesta manha para recuperar o deficit (P3).</summary>
        public int RecoveryMinutes { get; internal set; }
        /// <summary>Dormiu de manha depois de um turno da noite.</summary>
        public bool DaySleeper { get; internal set; }
        /// <summary>Hoje tem turno que atravessa a meia-noite: o dia acordado so termina depois dele.</summary>
        public bool NightShiftAhead { get; internal set; }
        /// <summary>Saida para o turno da noite (inicio do deslocamento); -1 sem turno.</summary>
        public int ShiftDeparture { get; internal set; } = -1;
        public int Wake { get; internal set; }
        /// <summary>Quando o dia acordado termina e o sono comeca.</summary>
        public int Bed { get; internal set; }
        public List<(int Start, int End)> Windows { get; } = new();
        /// <summary>
        /// Janelas que sao so possibilidade (R6): depois do turno da noite, antes de dormir. Ficam em
        /// Windows (a pessoa pode jogar nelas), mas nao contam como tempo livre do dia nem adiam o sono planejado.
        /// </summary>
        public List<(int Start, int End)> OptionalWindows { get; } = new();
        /// <summary>Sem obrigacoes neste dia acordado (folga, fim de semana, feriado, sem ocupacao).</summary>
        public bool FreeDay { get; internal set; }
        public int FreeMinutes => Windows.Where(w => !OptionalWindows.Contains(w)).Sum(w => w.End - w.Start);
        /// <summary>Compromisso ocasional do dia livre (D-046): inicio, fim e id; nao conta como obrigacao.</summary>
        public List<(int Start, int End, string Id)> Commitments { get; } = new();
        /// <summary>Minutos de obrigacao hoje, com deslocamento (carga do dia).</summary>
        public int ObligationMinutes => Blocks.Sum(b => b.End - b.Start);
        /// <summary>Sono planejado da noite que termina hoje.</summary>
        public int SleepMinutes => Wake - SleepStart;

        /// <summary>
        /// O dia inteiro em trechos que se encaixam sem sobra nem sobreposicao, de SleepStart a Bed:
        /// sono, partes acordadas, janelas livres e, no que sobrar, "awake" (acordado, sem janela).
        /// </summary>
        public List<Segment> Timeline()
        {
            int len = Math.Max(0, Bed - SleepStart);
            var owner = new string?[len];
            void Paint(int from, int to, string kind)
            {
                for (int m = Math.Max(from, SleepStart); m < Math.Min(to, Bed); m++)
                    owner[m - SleepStart] ??= kind;
            }
            Paint(SleepStart, Wake, "sleep");
            foreach (var p in Parts) Paint(p.Start, p.End, p.Kind);
            foreach (var (ws, we) in Windows) Paint(ws, we, "free");
            var list = new List<Segment>();
            for (int i = 0; i < len;)
            {
                string kind = owner[i] ?? "awake";
                int j = i + 1;
                while (j < len && (owner[j] ?? "awake") == kind) j++;
                list.Add(new Segment(SleepStart + i, SleepStart + j, kind));
                i = j;
            }
            return list;
        }
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
            if (b.PersonalDayOff && (p.DayOffIndex == dow || p.DayOff2Index == dow)) continue;
            int s = ContentValidator.ParseClock(b.Start), e = ContentValidator.ParseClock(b.End);
            if (s < 0 || e < 0) continue;
            if (e <= s) e += 1440;
            list.Add((s + p.ShiftOffsetMinutes, e + p.ShiftOffsetMinutes, b.CommuteMinutes * p.CommutePermille / 1000, b.Label, b));
        }
        list.Sort((a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : string.CompareOrdinal(a.Label, b.Label));
        return list;
    }

    /// <summary>
    /// Recorte civil (R1/R2): os dias acordados sao a organizacao interna; calendario, feriados e
    /// relatorios usam o dia civil (meia-noite a meia-noite). Este recorte coloca cada trecho dos
    /// dias acordados no seu horario absoluto e corta numa janela fixa de dias civis: cada minuto
    /// da janela aparece uma unica vez (o sono que atravessa a borda entra so com a parte de dentro).
    /// days[i] e o dia acordado do dia civil (firstDay + i); a janela vai de fromDay ate fromDay + count.
    /// </summary>
    public static List<Segment> CivilWindow(IReadOnlyList<DayLayout> days, int firstDay, int fromDay, int count)
    {
        int from = fromDay * 1440, to = (fromDay + count) * 1440;
        var list = new List<Segment>();
        for (int i = 0; i < days.Count; i++)
        {
            int offset = (firstDay + i) * 1440;
            foreach (var t in days[i].Timeline())
            {
                int s = Math.Max(from, offset + t.Start), e = Math.Min(to, offset + t.End);
                if (e > s) list.Add(new Segment(s - from, e - from, t.Kind));
            }
        }
        list.Sort((a, b) => a.Start.CompareTo(b.Start));
        return list;
    }

    /// <summary>Tipo do trecho de obrigacao (linha do tempo e interface).</summary>
    public static string ObligationKind(string label, bool remote) => remote ? "remote" : label switch { "estudo" => "study", "cuidados" => "care", _ => "work" };

    /// <summary>
    /// Fim do dia acordado (R1): depois de um turno que atravessa a meia-noite, a volta para casa e
    /// a chegada; senao a hora natural de dormir, cedo o bastante para a obrigacao de amanha e
    /// nunca antes de voltar e comer. Minutos nas coordenadas do proprio dia (pode passar de 1440).
    /// </summary>
    private static int DayEndOf(Person p, OccupationDefinition? occ, RoutineSettingsDefinition rs,
        List<(int Start, int End, int Commute, string Label, ObligationBlockDefinition Def)> day,
        List<(int Start, int End, int Commute, string Label, ObligationBlockDefinition Def)> next, int nextCommitStart, bool freeEve = true)
    {
        var night = day.Where(o => o.End > 1440).ToList();
        if (night.Count > 0)
        {
            var n = night.OrderBy(o => o.End).Last();
            return n.End + n.Commute + (n.Commute > 0 ? rs.ArrivalMinutes : 0);
        }
        int bed = p.BedtimeMinute;
        if (HasNightShift(occ))
        {
            // Folga do turno da noite (R6): continua perto do ritmo dos dias de turno, sem voltar de uma vez a hora natural.
            if (NightSleepStart(p, occ, rs) is int ws && ws > bed) bed += (ws - bed) * rs.NightShiftOffContinuityPermille / 1000;
        }
        else if (freeEve && next.Count == 0 && p.FreeEveShiftMinutes > 0 && occ != null && occ.Blocks.Count > 0)
            bed += p.FreeEveShiftMinutes;   // vespera de folga (R6): deslocamento individual
        if (next.Count > 0) bed = Math.Min(bed, 1440 + next[0].Start - next[0].Commute - rs.PrepMinutes - p.SleepNeedMinutes);
        // Compromisso de amanha (R6): acorda a tempo, com o sono necessario.
        if (nextCommitStart >= 0) bed = Math.Min(bed, 1440 + nextCommitStart - rs.PrepMinutes - p.SleepNeedMinutes);
        if (day.Count > 0)
        {
            var last = day[^1];
            bed = Math.Max(bed, last.End + last.Commute + (last.Commute > 0 ? rs.ArrivalMinutes : 0));   // ninguem dorme antes de voltar e comer
        }
        return bed;
    }

    /// <summary>
    /// Monta o dia acordado (R1): o sono que termina hoje comeca no fim do dia acordado de ontem
    /// (inclusive depois de um turno da noite); acorda a tempo da primeira obrigacao ou depois do
    /// sono necessario (mais a recuperacao, se sobrar manha); obrigacoes com deslocamento, ou em
    /// sequencia direta (refeicao, transferencia e espera fora de casa); chegada so depois de voltar
    /// para casa; compromisso do dia livre; o resto sao as janelas livres.
    /// recoveryMinutes: a recuperacao decidida para este dia (null: pelo deficit atual).
    /// </summary>
    public static DayLayout Build(Person p, OccupationDefinition? occ, RoutineSettingsDefinition rs, int dow, bool holiday, bool prevHoliday, bool nextHoliday,
        (int Start, int End, string Id)? commitment = null, int? recoveryMinutes = null, int nextCommitStart = -1, bool freeEve = true)
    {
        var today = Blocks(p, occ, dow, holiday);
        var prev = Blocks(p, occ, (dow + 6) % 7, prevHoliday);
        var next = Blocks(p, occ, (dow + 1) % 7, nextHoliday);
        var day = new DayLayout();

        // Sono que termina hoje: comeca no fim do dia acordado de ontem.
        day.SleepStart = DayEndOf(p, occ, rs, prev, today, commitment?.Start ?? -1, freeEve) - 1440;
        day.DaySleeper = prev.Any(o => o.End > 1440);
        int recovery = Math.Max(0, recoveryMinutes ?? Math.Min(p.SleepDeficitMinutes, rs.RecoveryFreeNightMinutes));
        int natural = day.SleepStart + p.SleepNeedMinutes;
        int limit = today.Count > 0 ? today[0].Start - today[0].Commute - rs.PrepMinutes : int.MaxValue;
        int wake = Math.Max(day.SleepStart, Math.Min(natural + recovery, limit));
        day.RecoveryMinutes = Math.Max(0, wake - natural);
        day.Wake = wake;
        day.Bed = DayEndOf(p, occ, rs, today, next, nextCommitStart, freeEve);

        // Obrigacoes: de casa (deslocamento antes e depois) ou em sequencia direta.
        day.Parts.Add(new Segment(wake, Math.Min(wake + rs.PrepMinutes, day.Bed), "prep"));
        for (int i = 0; i < today.Count; i++)
        {
            var o = today[i];
            bool fromPrev = i > 0 && o.Def.IsFromPrevious();
            bool toNext = i + 1 < today.Count && today[i + 1].Def.IsFromPrevious();
            int busyStart = fromPrev ? o.Start : Math.Max(wake, o.Start - o.Commute);
            int busyEnd = toNext ? today[i + 1].Start : o.End + o.Commute;
            day.Blocks.Add(new Busy(busyStart, busyEnd, o.Label, o.Def.Remote));
            if (!fromPrev && o.Commute > 0) day.Parts.Add(new Segment(busyStart, o.Start, "commute"));
            day.Parts.Add(new Segment(o.Start, o.End, ObligationKind(o.Label, o.Def.Remote)));
            if (toNext)
            {
                var n = today[i + 1];
                int meal = Math.Min(n.Def.MealMinutes, n.Start - o.End);
                int transfer = Math.Min(n.Def.TransferMinutes, n.Start - o.End - meal);
                if (meal > 0) day.Parts.Add(new Segment(o.End, o.End + meal, "meal"));
                if (transfer > 0) day.Parts.Add(new Segment(o.End + meal, o.End + meal + transfer, "transfer"));
                if (n.Start > o.End + meal + transfer) day.Parts.Add(new Segment(o.End + meal + transfer, n.Start, "wait"));
            }
            else if (o.Commute > 0)
            {
                day.Parts.Add(new Segment(o.End, o.End + o.Commute, "commute"));
                day.Parts.Add(new Segment(o.End + o.Commute, o.End + o.Commute + rs.ArrivalMinutes, "arrival"));   // chegar e comer
            }
            if (o.End > 1440) { day.NightShiftAhead = true; day.ShiftDeparture = busyStart; }
            if (o.Def.Remote)
                day.Remote.Add(new RemoteWork(o.Start, o.End, o.Def.Breaks
                    .Select(br => (ContentValidator.ParseClock(br.Start) + p.ShiftOffsetMinutes, ContentValidator.ParseClock(br.End) + p.ShiftOffsetMinutes))
                    .Where(br => br.Item1 >= o.Start && br.Item2 <= o.End).ToList()));
        }
        day.FreeDay = today.Count == 0;

        // Compromisso do dia livre (D-046): dentro do dia acordado, saindo do tempo livre.
        if (commitment is { } c)
        {
            int cs = Math.Max(c.Start, wake + rs.PrepMinutes), ce = Math.Min(c.End, day.Bed - 15);
            if (ce - cs >= 30)
            {
                day.Commitments.Add((cs, ce, c.Id));
                day.Parts.Add(new Segment(cs, ce, "commitment"));
                day.Parts.Add(new Segment(ce, Math.Min(ce + rs.ArrivalMinutes, day.Bed), "arrival"));
            }
        }
        // Partes sem sobreposicao: cada uma termina antes da seguinte comecar (a chegada encolhe).
        day.Parts.Sort((x, y) => x.Start != y.Start ? x.Start.CompareTo(y.Start) : string.CompareOrdinal(x.Kind, y.Kind));
        for (int i = 0; i + 1 < day.Parts.Count; i++)
            if (day.Parts[i].End > day.Parts[i + 1].Start)
                day.Parts[i] = day.Parts[i] with { End = Math.Max(day.Parts[i].Start, day.Parts[i + 1].Start) };
        day.Parts.RemoveAll(x => x.End <= x.Start);

        // Janelas livres: o que sobra do dia acordado entre as partes.
        int cursor = wake;
        foreach (var part in day.Parts)
        {
            if (part.Start > cursor) AddWindow(day, cursor, Math.Min(part.Start, day.Bed), rs);
            cursor = Math.Max(cursor, part.End);
            if (cursor >= day.Bed) break;
        }
        AddWindow(day, cursor, day.Bed, rs);

        // Depois do turno da noite (R6): pode jogar um pouco antes de dormir. E so possibilidade: nao
        // conta como tempo livre e o sono planejado comeca na chegada; se jogar, o sono se desloca.
        if (day.NightShiftAhead && rs.PostShiftWindowMinutes >= rs.MinSessionMinutes)
        {
            int ps = day.Bed, pe = day.Bed + rs.PostShiftWindowMinutes;
            if (next.Count > 0) pe = Math.Min(pe, 1440 + next[0].Start - next[0].Commute - rs.PrepMinutes - p.SleepNeedMinutes);
            if (pe - ps >= rs.MinSessionMinutes) { day.Windows.Add((ps, pe)); day.OptionalWindows.Add((ps, pe)); }
        }
        return day;
    }

    /// <summary>
    /// Inicio do sono nos dias de turno da noite (R6): a chegada depois do turno, em minutos desde a
    /// meia-noite do dia em que o turno comeca (passa de 1440). null se a ocupacao nao tem turno da noite.
    /// </summary>
    public static int? NightSleepStart(Person p, OccupationDefinition? occ, RoutineSettingsDefinition rs)
    {
        if (occ == null) return null;
        int? best = null;
        foreach (var b in occ.Blocks)
        {
            int s = ContentValidator.ParseClock(b.Start), e = ContentValidator.ParseClock(b.End);
            if (s < 0 || e < 0 || e > s) continue;
            int commute = b.CommuteMinutes * p.CommutePermille / 1000;
            int arrive = e + 1440 + p.ShiftOffsetMinutes + commute + (commute > 0 ? rs.ArrivalMinutes : 0);
            if (best == null || arrive > best) best = arrive;
        }
        return best;
    }

    /// <summary>
    /// Ultimo minuto em que a pessoa ainda pode acordar hoje sem atrasar a obrigacao (com o
    /// deslocamento) ou o compromisso, contando o preparo (R6). int.MaxValue se nada obriga.
    /// </summary>
    public static int LatestWake(DayLayout day, RoutineSettingsDefinition rs)
    {
        int limit = int.MaxValue;
        if (day.Blocks.Count > 0) limit = day.Blocks[0].Start - rs.PrepMinutes;   // o bloco ja comeca na saida de casa
        foreach (var c in day.Commitments) limit = Math.Min(limit, c.Start - rs.PrepMinutes);
        return limit;
    }

    /// <summary>
    /// Minuto do dia acordado para um horario do relogio (R6), numa faixa continua que atravessa a
    /// meia-noite: horario antes de acordar e a madrugada seguinte quando o dia acordado chega perto
    /// dela (03h vem depois das 23h); senao e cedo demais para hoje (vale o acordar). 25 h = 01h.
    /// </summary>
    public static int AwakeMinute(int minute, DayLayout day)
    {
        int m = ((minute % 1440) + 1440) % 1440;
        if (m >= day.Wake) return m;
        int late = m + 1440;
        return Math.Max(0, late - day.Bed) <= day.Wake - m ? late : m;
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

    /// <summary>
    /// Sessao normal (v3): a duracao que a pessoa gosta de jogar de uma vez (sessionMinutes). E uma
    /// preferencia: ao chega-la, a pessoa decide se continua; nao e limite nem e ampliada pelo
    /// tempo livre. A variacao so vale na primeira sessao do cadastro e da volta (sem agenda).
    /// </summary>
    public static int SessionMinutes(Person p, RoutineSettingsDefinition rs, int jitterPermille = 1000) =>
        Math.Max(rs.MinSessionMinutes, p.Traits.SessionMinutes * jitterPermille / 1000);

    /// <summary>
    /// Prioridade do jogo dentro do dia (v3), 0..1000: a importancia e o tempo livre (quem liga pouco
    /// joga mais quando sobra lazer), sem o peso do dia de obrigacoes, que entra uma vez so, na chance
    /// de jogar no dia (BaseChance). Usada nas decisoes de comecar, continuar e voltar.
    /// </summary>
    public static int SessionPriority(Person p, DayLayout day, RoutineSettingsDefinition rs)
    {
        int imp = Math.Clamp(p.Traits.GameImportance, 0, 1000);
        int freeness = Math.Min(1000, day.FreeMinutes * 1000 / Math.Max(1, rs.FullFreeMinutes));
        return Math.Clamp(imp + (1000 - imp) * rs.FreeDayChanceBonus / 1000 * freeness / 1000, 0, 1000);
    }

    /// <summary>
    /// Vontade de jogar agora (v3, permille; 0..2000): a prioridade do jogo no dia vezes a disposicao
    /// do dia, o humor (engajamento), a experiencia (a sessao em relacao ao que a pessoa costuma
    /// sentir, para nao contar duas vezes o que o humor ja tem) e, para continuar, a tendencia a
    /// prolongar. Combinados e limitados uma vez no fim.
    /// </summary>
    public static int Pull(int priority, int disposition, int moodFactor, int experienceDelta, int tendencyFactor, RoutineSettingsDefinition rs)
    {
        long v = (long)priority * (1000 + disposition) / 1000;
        v = v * moodFactor / 1000;
        v = v * (1000 + experienceDelta * rs.ExperienceWeightPermille / 1000) / 1000;
        v = v * tendencyFactor / 1000;
        return (int)Math.Clamp(v, 0, 2000);
    }

    /// <summary>Fator da tendencia a prolongar na vontade de continuar (v3, permille; 500 = neutro).</summary>
    public static int TendencyFactor(Person p, RoutineSettingsDefinition rs) =>
        1000 + (Math.Clamp(p.Traits.ExtendTendency, 0, 1000) - 500) * rs.ContinueTendencyWeightPermille / 1000;

    /// <summary>Vontade de parar (v3): a saciedade (0..1000) mais o cansaco do sono, com o peso dele na chance.</summary>
    public static int StopPressure(int satiety, int fatigue, RoutineSettingsDefinition rs) =>
        Math.Max(0, satiety) + fatigue * rs.FatigueChancePenalty / 1000;

    /// <summary>
    /// Decisao de jogar agora (v3, permille): a vontade de jogar comparada com as outras opcoes de
    /// lazer (1000 - prioridade) e a vontade de parar. Sem saciedade nem cansaco, com disposicao,
    /// humor e experiencia neutros, a chance e a propria prioridade do jogo.
    /// </summary>
    public static int Choice(int pull, int priority, int push) =>
        (int)((long)pull * 1000 / Math.Max(1, pull + (1000 - Math.Clamp(priority, 0, 1000)) + Math.Max(0, push)));

    /// <summary>2^(-k/64) em milionesimos, k = 0..64: tabela fixa (inteiros) da meia-vida.</summary>
    private static readonly int[] HalfTable = { 1000000, 989228, 978572, 968031, 957603, 947288, 937084, 926990, 917004, 907126, 897355, 887688, 878126, 868667, 859310, 850053, 840896, 831838, 822878, 814014, 805245, 796571, 787990, 779502, 771105, 762799, 754582, 746454, 738413, 730459, 722590, 714807, 707107, 699490, 691955, 684501, 677128, 669834, 662618, 655481, 648420, 641435, 634525, 627690, 620929, 614240, 607624, 601078, 594604, 588198, 581862, 575595, 569394, 563261, 557193, 551191, 545254, 539380, 533570, 527823, 522137, 516512, 510949, 505445, 500000 };

    /// <summary>
    /// Fator de meia-vida 2^(-minutos / meia-vida), em milionesimos (R5): inteiro, deterministico e
    /// composto (aplicar 30 + 30 minutos da o mesmo que 60, a menos de arredondamento), para que o
    /// resultado nao dependa de quantas vezes se atualiza.
    /// </summary>
    public static long HalfLifeFactor(long minutes, long halfLife)
    {
        if (minutes <= 0) return 1_000_000;
        halfLife = Math.Max(1, halfLife);
        long whole = minutes / halfLife;
        if (whole >= 40) return 0;
        long rest = minutes % halfLife;
        long pos = rest * 64 * 1000 / halfLife;                 // posicao na tabela, em milesimos de faixa
        int k = (int)(pos / 1000);
        long frac = pos % 1000;
        long within = HalfTable[k] + (HalfTable[Math.Min(64, k + 1)] - HalfTable[k]) * frac / 1000;
        return within >> (int)whole;
    }

    /// <summary>
    /// Saciedade depois de jogar "minutes" (v3/R5): o que falta ate 1000 cai pela metade a cada
    /// satietyHalfLifeSessionsPermille x sessao normal (padrao 1,5 sessao normal). Todo o tempo
    /// jogado conta, inclusive o interrompido. Meia-vida: nao depende da frequencia das decisoes.
    /// </summary>
    public static int SatietyAfterPlay(int satiety, long minutes, int sessionMinutes, RoutineSettingsDefinition rs)
    {
        if (minutes <= 0) return satiety;
        long half = Math.Max(1, (long)Math.Max(1, sessionMinutes) * rs.SatietyHalfLifeSessionsPermille / 1000);
        // Arredonda para o mais proximo: truncar a cada atualizacao acumularia vies com atualizacoes frequentes.
        long remaining = ((1000 - Math.Clamp(satiety, 0, 1000)) * HalfLifeFactor(minutes, half) + 500_000) / 1_000_000;
        return (int)(1000 - remaining);
    }

    /// <summary>Saciedade depois de uma pausa (v3/R5): cai pela metade a cada satietyRecoveryHalfLifeMinutes.</summary>
    public static int SatietyAfterPause(int satiety, long minutes, RoutineSettingsDefinition rs)
    {
        if (minutes <= 0) return satiety;
        return (int)((Math.Clamp(satiety, 0, 1000) * HalfLifeFactor(minutes, rs.SatietyRecoveryHalfLifeMinutes) + 500_000) / 1_000_000);
    }

    /// <summary>
    /// Inicio do dia (v3), com um sorteio so (u em 0..999999), depois que a pessoa decidiu jogar hoje:
    /// - a vontade de comecar vale por hora e se acumula no tempo livre, em passos de startStepMinutes
    ///   (o passo so arredonda o horario: nao sao tentativas independentes nem dependem de quantas
    ///   vezes se avalia);
    /// - antes do horario de costume ha resistencia (earlyStartResistancePerHour por hora de antecedencia);
    /// - no horario de costume (ou no primeiro tempo livre depois dele) comeca; se ele nao cabe no dia,
    ///   no ultimo momento em que ainda da para jogar a sessao normal;
    /// - no dia livre, a primeira janela comeca depois da manha (freeDayMorningMinutes).
    /// Devolve -1 se nenhuma janela comporta a sessao minima.
    /// </summary>
    public static int StartMinute(DayLayout day, int target, int sessionMinutes, int pull, int priority, int push, int u, RoutineSettingsDefinition rs)
    {
        var usable = day.Windows.Where(w => w.End - w.Start >= rs.MinSessionMinutes).ToList();
        if (usable.Count == 0) return -1;
        target = AwakeMinute(target, day);   // faixa continua: madrugada depois da meia-noite (R6)
        var last = usable[^1];
        int latest = last.End - Math.Min(Math.Max(rs.MinSessionMinutes, sessionMinutes), last.End - last.Start);
        int step = Math.Max(1, rs.StartStepMinutes);
        long survive = 1_000_000;   // chance (milionesimos) de ainda nao ter comecado
        for (int i = 0; i < usable.Count; i++)
        {
            var (ws, we) = usable[i];
            bool isLast = i == usable.Count - 1;
            int t = ws;
            if (i == 0 && day.FreeDay && ws + rs.FreeDayMorningMinutes <= we - rs.MinSessionMinutes) t = ws + rs.FreeDayMorningMinutes;
            for (; t <= we - rs.MinSessionMinutes; t += step)
            {
                if (isLast && t >= latest) return Math.Max(ws, latest);
                if (t >= target) return t;
                int tired = day.OptionalWindows.Contains((ws, we)) ? rs.PostShiftTirednessPermille : 0;   // depois do turno (R6)
                int q = Choice(pull, priority, push + tired + (int)Math.Min(100_000, (long)(target - t) * rs.EarlyStartResistancePerHour / 60));
                survive = survive * (60_000 - (long)q * step) / 60_000;
                if (1_000_000 - survive > u) return t;
            }
        }
        return Math.Max(last.Start, latest);
    }

    /// <summary>
    /// Regularidade recente (R4), 0..1000: nos ultimos "days" dias, quanto a pessoa escolheu jogar
    /// quando podia, pesando mais os mais recentes. Dia sem oportunidade (sem janela, so obrigacao)
    /// e neutro; escolher outro lazer, cansaco, desanimo e inatividade contam como falta; sessao
    /// interrompida conta como jogo (a escolha foi jogar). Sem dias para medir: 500 (neutro).
    /// </summary>
    public static (int Regularity, int Played, int Missed) Regularity(IReadOnlyList<DayPlanRecord> plans, long today, int days)
    {
        long weightSum = 0, weightPlayed = 0;
        int played = 0, missed = 0;
        foreach (var r in plans)
        {
            long age = today - r.LocalDay;   // 1 = ontem
            if (age < 1 || age > days) continue;
            bool chose = r.Sessions > 0 || r.WorkSessions > 0;
            if (!chose && r.Reason == "no_free_time") continue;   // nao pode jogar: neutro
            int w = days - (int)age + 1;
            weightSum += w;
            if (chose) { weightPlayed += w; played++; } else missed++;
        }
        return (weightSum == 0 ? 500 : (int)(weightPlayed * 1000 / weightSum), played, missed);
    }

    /// <summary>
    /// Fator do habito na chance (R4, permille): o traco de habito vezes a regularidade recente.
    /// Joga com regularidade: mantem o embalo; vem faltando por escolha: tende a continuar faltando.
    /// </summary>
    public static int HabitFactor(int habit, int regularity, RoutineSettingsDefinition rs) =>
        1000 + habit * rs.HabitStreakPermille / 1000 * (2 * regularity - 1000) / 1000;

    /// <summary>
    /// Escolhe a janela e o inicio: a que contem o horario preferido (ou a mais proxima; empate,
    /// a mais longa); comeca mais cedo se a sessao desejada nao couber depois do preferido.
    /// Devolve false se nenhuma janela comporta a sessao minima.
    /// </summary>
    public static bool PlaceMain(DayLayout day, int preferredMinute, int desired, RoutineSettingsDefinition rs, out int window, out int start, out int minutes)
    {
        window = -1; start = 0; minutes = 0;
        preferredMinute = AwakeMinute(preferredMinute, day);   // faixa continua (R6)
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
    /// Horario habitual (viavel) coerente com a rotina e com a vontade de jogar (D-045; R4 separa
    /// do gosto). Em cada dia do habito, na janela mais perto do gosto de horario sorteado pelo perfil:
    /// - quem da muita importancia ao jogo comeca logo que fica livre (chegou, comeu, ja joga);
    /// - quem da pouca espera mais (ate preferredDelayMaxMinutes) e segue mais o proprio horario
    ///   de lazer sorteado;
    /// - sempre de modo que a sessao tipica caiba inteira antes de dormir.
    /// A hora final e a hora cheia em que isso vale no maior numero de dias (empate: mais perto
    /// do alvo). Sem sorteio: so ajusta o que ja foi sorteado.
    /// </summary>
    public static int ViableStartHour(Person p, OccupationDefinition? occ, RoutineSettingsDefinition rs)
    {
        int drawn = p.Traits.PreferredStartHour;
        var week = new List<DayLayout>();
        // O habito vem dos dias tipicos: a vespera de folga (R6) e excecao da semana, nao define o costume.
        for (int d = 0; d < 7; d++) week.Add(Build(p, occ, rs, d, false, false, false, freeEve: false));
        var (pool, session) = PreferredPool(week, Math.Max(rs.MinSessionMinutes, p.Traits.SessionMinutes), rs);
        if (pool.Count == 0) return drawn;
        int imp = Math.Clamp(p.Traits.GameImportance, 0, 1000);
        var targets = new List<int>();
        foreach (var d in pool)
        {
            int taste = AwakeMinute(drawn * 60, d);
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
        int pref = AwakeMinute(hour * 60, day);   // como no PlaceMain
        return PlaceMain(day, hour * 60, session, rs, out _, out int start, out _) && start >= pref && start - pref < 60;
    }

    /// <summary>
    /// Variacao do deficit por um periodo de sono (R1), aplicada uma unica vez quando a pessoa acorda:
    /// dormiu menos que precisa, o deficit sobe a diferenca; dormiu o necessario ou mais, cai o que
    /// sobrou mais a recuperacao de uma noite completa (recoveryNormalNightMinutes, explicita).
    /// </summary>
    public static int DeficitChange(int needMinutes, int sleptMinutes, RoutineSettingsDefinition rs) =>
        sleptMinutes < needMinutes ? needMinutes - sleptMinutes : -(sleptMinutes - needMinutes) - rs.RecoveryNormalNightMinutes;

    /// <summary>Cansaco (permille) pelo deficit de sono (P3).</summary>
    public static int Fatigue(Person p, RoutineSettingsDefinition rs) =>
        Math.Min(1000, p.SleepDeficitMinutes * 1000 / Math.Max(1, rs.FatigueFullDeficitMinutes));

    /// <summary>Teto do deficit da pessoa: o geral escalado pela tolerancia dela.</summary>
    public static int MaxDeficit(Person p, RoutineSettingsDefinition rs) =>
        rs.MaxDeficitMinutes * (500 + p.SleepDebtTolerance) / 1000;

    /// <summary>Estimativa de um dia pela rotina (sem humor): chance, inicio e fim tipicos (fim da ultima sessao), minutos num dia com jogo e minutos esperados.</summary>
    public readonly record struct DayExpectation(int ChancePermille, int Start, int Minutes, int ExpectedMinutes, int End = -1);

    /// <summary>Um dia simulado pela regra da v3 (sem humor, cansaco, jogo no trabalho nem corte de sono): inicio e minutos.</summary>
    public static (int Start, int Minutes, int Sittings, int End) SimulateDay(Person p, DayLayout day, int disposition, DeterministicRng rng, RoutineSettingsDefinition rs)
    {
        int session = SessionMinutes(p, rs);
        int prio = SessionPriority(p, day, rs);
        int target = (day.FreeDay ? p.Traits.PreferredStartHour : p.HabitualStartHour) * 60;
        int start = StartMinute(day, target, session, Pull(prio, disposition, 1000, 0, 1000, rs), prio, 0, rng.NextInt(1_000_000), rs);
        if (start < 0) return (-1, 0, 0, -1);
        int pullContinue = Pull(prio, disposition, 1000, 0, TendencyFactor(p, rs), rs);
        int pullReturn = Pull(prio, disposition, 1000, 0, 1000, rs);
        int satiety = 0, total = 0, sittings = 0, t = start, last = start;
        while (true)
        {
            int wi = day.Windows.FindIndex(w => w.Start <= t && t < w.End);
            if (wi < 0) break;
            int we = day.Windows[wi].End;
            int end = Math.Min(we, t + session);
            satiety = SatietyAfterPlay(satiety, end - t, session, rs);
            while (we - end >= rs.ContinueMinMinutes && rng.ChancePermille(Choice(pullContinue, prio, StopPressure(satiety, 0, rs))))
            {
                int b = Math.Min(rs.ContinueBlockMinutes, we - end);
                satiety = SatietyAfterPlay(satiety, b, session, rs);
                end += b;
            }
            total += end - t;
            last = end;
            sittings++;
            if (sittings >= rs.MaxSessionsPerDay) break;
            int next = NextReturnMinute(day, end, rng.NextIntInclusive(rs.ReturnPauseMinMinutes, Math.Max(rs.ReturnPauseMinMinutes, rs.ReturnPauseMaxMinutes)), rs);
            if (next < 0) break;
            int rested = SatietyAfterPause(satiety, next - end, rs);
            if (!rng.ChancePermille(Choice(pullReturn, prio, StopPressure(rested, 0, rs)))) break;
            satiety = rested;
            t = next;
        }
        return (start, total, sittings, last);
    }

    /// <summary>
    /// Quando a pessoa pode voltar (v3): depois da pausa, se ainda cabe a sessao minima na mesma janela;
    /// senao no comeco da proxima janela do dia (depois da aula, do compromisso). -1 = sem tempo.
    /// </summary>
    public static int NextReturnMinute(DayLayout day, int endMinute, int pauseMinutes, RoutineSettingsDefinition rs)
    {
        int at = endMinute + Math.Max(0, pauseMinutes);
        foreach (var (s, e) in day.Windows)
        {
            if (e <= endMinute) continue;
            int t = Math.Max(s, at);
            if (e - t >= rs.MinSessionMinutes) return t;
        }
        return -1;
    }

    /// <summary>
    /// Estimativa da semana (R2/v3), a mesma regra do planejador sem o humor e o cansaco, dia a dia:
    /// - a chance de cada dia usa a regularidade dos dias anteriores com oportunidade (habito) e a
    ///   disposicao amostrada; a semana real e repetida ate estabilizar;
    /// - os minutos de um dia com jogo vem de estimateSamples dias simulados pela regra da v3
    ///   (comecar, continuar, parar e voltar), com sorteio fixo da pessoa: sempre o mesmo resultado;
    /// - as probabilidades sao combinadas e limitadas a 0..1000 uma vez, como no planejador.
    /// O jogo no trabalho, o corte de sono e o humor ficam de fora. E uma estimativa.
    /// </summary>
    public static DayExpectation[] ExpectWeek(Person p, IReadOnlyList<DayLayout> week, RoutineSettingsDefinition rs)
    {
        int n = week.Count, k = Math.Max(1, rs.EstimateSamples);
        var fits = new bool[n];
        var baseChance = new long[n];
        var disp = new int[n, k];
        var minutes = new int[n, k];
        var starts = new List<int>[n];
        var ends = new List<int>[n];
        for (int d = 0; d < n; d++)
        {
            starts[d] = new List<int>();
            ends[d] = new List<int>();
            fits[d] = week[d].Windows.Any(w => w.End - w.Start >= rs.MinSessionMinutes);
            if (!fits[d]) continue;
            baseChance[d] = BaseChance(p, week[d], rs);
            var rng = new DeterministicRng(RngStreams.DeriveSeed(p.AppearanceSeed, "routine.estimate", d));
            for (int j = 0; j < k; j++)
            {
                disp[d, j] = rng.NextCenteredNoise(Math.Max(0, rs.DispositionSpreadPermille));
                var (st, m, _, en) = SimulateDay(p, week[d], disp[d, j], rng, rs);
                minutes[d, j] = m;
                if (st >= 0) { starts[d].Add(st); ends[d].Add(en); }
            }
        }
        int hist = Math.Max(1, rs.HabitHistoryDays);
        int Reg(double[] played, int d)
        {
            double wSum = 0, wPlayed = 0;
            for (int age = 1; age <= hist; age++)
            {
                int prev = ((d - age) % n + n) % n;
                if (!fits[prev]) continue;   // sem oportunidade: neutro
                int w = hist - age + 1;
                wSum += w;
                wPlayed += w * played[prev];
            }
            return wSum == 0 ? 500 : (int)Math.Round(wPlayed * 1000 / wSum);
        }
        // Probabilidade de jogar em cada dia, repetindo a semana ate estabilizar (habito pelos dias anteriores).
        var playedP = new double[n];
        for (int d = 0; d < n; d++) playedP[d] = fits[d] ? 0.5 : 0;
        for (int it = 0; it < 40; it++)
            for (int d = 0; d < n; d++)
            {
                if (!fits[d]) { playedP[d] = 0; continue; }
                long withHabit = baseChance[d] * HabitFactor(p.Traits.Habit, Reg(playedP, d), rs) / 1000;
                double prob = 0;
                for (int j = 0; j < k; j++) prob += Math.Clamp(withHabit * (1000 + disp[d, j]) / 1000, 0, 1000) / 1000.0;
                playedP[d] = prob / k;
            }
        var result = new DayExpectation[n];
        for (int d = 0; d < n; d++)
        {
            if (!fits[d]) { result[d] = new DayExpectation(0, -1, 0, 0); continue; }
            long withHabit = baseChance[d] * HabitFactor(p.Traits.Habit, Reg(playedP, d), rs) / 1000;
            double expected = 0;
            for (int j = 0; j < k; j++) expected += Math.Clamp(withHabit * (1000 + disp[d, j]) / 1000, 0, 1000) / 1000.0 * minutes[d, j];
            var sortedMinutes = Enumerable.Range(0, k).Select(j => minutes[d, j]).OrderBy(x => x).ToList();
            starts[d].Sort();
            ends[d].Sort();
            result[d] = new DayExpectation((int)Math.Round(playedP[d] * 1000), starts[d].Count > 0 ? starts[d][starts[d].Count / 2] : -1,
                sortedMinutes[k / 2], (int)Math.Round(expected / k), ends[d].Count > 0 ? ends[d][ends[d].Count / 2] : -1);
        }
        return result;
    }

    /// <summary>Estimativa semanal pela rotina tipica (sem feriados), em minutos.</summary>
    public static int ExpectedWeeklyMinutes(Person p, OccupationDefinition? occ, RoutineSettingsDefinition rs)
    {
        var week = Enumerable.Range(0, 7).Select(d => Build(p, occ, rs, d, false, false, false)).ToList();
        return ExpectWeek(p, week, rs).Sum(x => x.ExpectedMinutes);
    }
}

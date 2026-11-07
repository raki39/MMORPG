using Godot;
using Simulation.Core.Population;
using GodotClient.Sim;
using SimTime = Simulation.Core.Time.SimTime;

namespace GodotClient.Ui;

/// <summary>
/// Aba Rotina (P1-P3): a semana da pessoa em agenda grafica, os ultimos dias (tempo livre x
/// tempo jogado e o motivo), sono e cansaco, vontade de jogar, ocupacao, onde mora e financas.
/// </summary>
public partial class PlayerModal
{
    public static string LanguageName(string id) => id switch
    {
        "pt" => "português", "es" => "espanhol", "en" => "inglês", "zh" => "chinês", _ => id,
    };

    private string LocalityName(string id) => _ctx.Content.Localities.TryGetValue(id, out var l) ? l.Name : id;
    private static string Clock(int minute) { int m = ((minute % 1440) + 1440) % 1440; return $"{m / 60:00}:{m % 60:00}"; }
    private static readonly string[] DowPt = { "seg", "ter", "qua", "qui", "sex", "sáb", "dom" };
    private static readonly string[] DowLong = { "Segunda", "Terça", "Quarta", "Quinta", "Sexta", "Sábado", "Domingo" };
    private static string Hours(int minutes) => $"{PtBr.Decimal(minutes / 60.0, 1)} h";

    public static string DayReasonPt(string key) => key switch
    {
        "played" => "Jogou",
        "no_free_time" => "Sem tempo livre",
        "other_leisure" => "Tinha tempo, preferiu outro lazer",
        "low_engagement" => "Sem vontade (engajamento baixo)",
        "inactive" => "Inativo",
        "came_back" => "Voltou ao jogo",
        "tired" => "Cansado (dormiu pouco)",
        "work_only" => "Jogou só no horário de trabalho",
        _ => key,
    };

    /// <summary>Jogou no dia: sessao principal ou so no horario do trabalho remoto (pausa ou escapada).</summary>
    private static bool PlayedOn(Simulation.Core.Population.DayPlanRecord d) => d.Sessions > 0 || d.WorkSessions > 0;

    /// <summary>Motivo mostrado: quem nao teve sessao principal mas jogou no trabalho tem motivo proprio.</summary>
    private static string ReasonOf(Simulation.Core.Population.DayPlanRecord d) => d.Sessions == 0 && d.WorkSessions > 0 ? "work_only" : d.Reason;

    private static Color DayReasonColor(string key) => key switch
    {
        "work_only" => UiTokens.Info,
        "played" => UiTokens.Success,
        "came_back" => UiTokens.Info,
        "no_free_time" => UiTokens.Danger,
        "tired" => UiTokens.Warning,
        "low_engagement" => UiTokens.Exceptional,
        "inactive" => UiTokens.TextDisabled,
        _ => UiTokens.TextSecondary,
    };

    private static string KindPt(string kind) => kind switch
    {
        "sleep" => "Sono", "free" => "Tempo livre", "work" => "Trabalho", "study" => "Estudo", "care" => "Cuidados",
        "remote" => "Trabalho remoto", "break" => "Pausa (pode jogar)", "commute" => "Deslocamento", _ => kind,
    };

    private string CommitmentName(string id) =>
        _ctx.Content.Routine.Commitments.FirstOrDefault(k => k.Id == id)?.Label ?? id;

    private string OccupationName => _ctx.Content.Occupations.TryGetValue(_person.OccupationId, out var o) ? o.Name : _person.OccupationId;

    private CharacterDetailView? RoutineDetail => _ctx.Snapshot().Watched is { } d && d.PersonId == _person.Id ? d : null;

    /// <summary>Dia local (indice) e minuto local de um instante do servidor.</summary>
    private (long Day, int Minute) LocalOf(long tick)
    {
        long t = tick + _person.LocalOffsetMinutes * SimTime.TicksPerMinute;
        long day = (long)Math.Floor(t / (double)SimTime.TicksPerDay);
        return (day, (int)((t - day * SimTime.TicksPerDay) / SimTime.TicksPerMinute));
    }

    /// <summary>Sessoes registradas, em trechos por dia local (dia, inicio, fim em minutos).</summary>
    private List<(long Day, int Start, int End)> LocalSessionPieces()
    {
        long now = _ctx.Snapshot().Tick;
        var list = new List<(long, int, int)>();
        foreach (var s in _ctx.PersonSessions(_person.Id))
        {
            long end = s.End < 0 ? now : s.End;
            var (d0, m0) = LocalOf(s.Start);
            var (d1, m1) = LocalOf(end);
            for (long d = d0; d <= d1; d++)
                list.Add((d, d == d0 ? m0 : 0, d == d1 ? m1 : 1440));
        }
        return list;
    }

    private Control BuildRoutine()
    {
        var detail = RoutineDetail;
        var occ = _ctx.Content.Occupations.TryGetValue(_person.OccupationId, out var o) ? o : null;
        // Pagina em telas (D-068). Tela 1, quem e e o padrao: contexto (onde mora, financas, vontade) e a semana
        // tipica ao lado das obrigacoes que a explicam. Tela 2, o que aconteceu: os ultimos 30 dias, sono e
        // motivos, e quando joga (horarios e as sessoes recentes).
        return new ScreenPage()
            .Add(Grid(12,
                (FillCard("Onde mora", PlaceBody(), info: "Localidade, fuso e idiomas (P1)\n~A rotina segue o horário e os feriados de onde a pessoa mora; o relógio do jogo é o do servidor."), 3),
                (FillCard("Finanças", MoneyBody(), info: "Dinheiro real fictício do tycoon (P2)\n~Separado do ouro do MMORPG. Sem efeito até a monetização (M7)."), 3),
                (FillCard("Vontade de jogar", WillBody(), info: "O que faz a pessoa jogar e parar\n~Importância do jogo, hábito, tendência a prolongar e autocontrole são traços separados: dá para priorizar o jogo e parar na hora, ou ligar pouco e se empolgar.\n~O humor (satisfação e engajamento) muda a vontade de verdade, e a disposição de cada dia varia um pouco (querer descansar ou fazer outra coisa). A estimativa é só a rotina e o hábito; o real inclui humor, cansaço e sessões estendidas."), 6)), weight: 46)
            .Add(Grid(12,
                (FillCard("Semana típica", AgendaBody(), info: "O que a rotina permite e o que se espera (hora local)\n~Azul (verde em dia livre): o tempo livre do dia, o que sobra depois de sono, obrigações, deslocamento, preparo e chegada. Dourado: o jogo estimado, a chance de jogar no dia (rotina e hábito, calculada na semana real dia a dia) × o tempo desejado do dia que cabe na janela, com as voltas depois de uma pausa e o continuar jogando, pela mesma regra do planejador (dias simulados); sem o humor. É uma estimativa. Embaixo de cada dia, a chance de jogar.\n~Passe o mouse num dia para ver os horários."), 6),
                (FillCard("Ocupação", OccupationBody(occ), info: "Obrigações da ocupação\n~Horários individuais (cada pessoa varia um pouco), deslocamento e folgas. No trabalho remoto, a pausa permite jogar; com flexibilidade, às vezes há uma escapada no expediente, curta e interrompível por um chamado do trabalho."), 6)), weight: 54)
            .Section()
            .Add(Grid(12,
                (FillCard("Últimos 30 dias", DaysBody(detail), info: "O que a pessoa fez com o tempo livre\n~Área azul: tempo livre de cada dia. Linha dourada: tempo jogado. Quando não jogou, o ponto fica no zero com a cor do motivo (sem tempo, outro lazer, cansaço, desânimo). Faixas verdes: dias livres. Losango azul: jogou no horário do trabalho remoto. Ponto roxo no alto: um compromisso no dia livre (festa, visita à família, passeio), que tira horas do tempo livre.\n~A agenda é estado da simulação (30 dias), sem um evento por dia."), 6),
                (FillCard("Sono e cansaço", SleepBody(detail), info: "Sono, déficit e cansaço (P3)\n~Noite mais curta que a necessidade vira déficit; ficar jogando além da hora de dormir também. Manhã livre recupera até 2 h e noite normal, 20 min. O cansaço reduz a vontade de jogar, encurta as sessões e pesa na nota da sessão."), 3),
                (FillCard("Como terminaram os dias", ReasonsBody(detail), info: "Motivo de cada dia da agenda\n~Ter tempo livre não é jogar: quem tem tempo pode preferir outro lazer. Sem tempo livre, cansaço e desânimo também aparecem aqui."), 3)), weight: 52)
            .Add(Grid(12,
                (FillCard("Horários de jogo", HoursBody(), info: "Quando costuma jogar\n~Minutos conectados por hora do dia nas sessões registradas. Dourado: janela da rotina (início preferido + sessão típica). Útil para planejar eventos e manutenção."), 6),
                (FillCard("Sessões · últimos 14 dias", FormBody(), info: "Minutos conectados por dia\n~Verde: dia no ritmo da rotina (80% da sessão típica ou mais). Vermelho: abaixo. A linha tracejada é a média dos dias jogados."), 6)), weight: 48);
    }

    // ----- Semana tipica (agenda grafica) -----

    private Control AgendaBody()
    {
        long now = _ctx.Snapshot().Tick;
        var (today, _) = LocalOf(now);
        int todayDow = ((int)_ctx.Calendar.StartDate.AddDays(today).DayOfWeek + 6) % 7;
        var groups = new List<BarGroup>();
        foreach (var d in _person.Week)
        {
            var lines = new List<(string, Color)>
            {
                (d.Obligations.Count == 0 ? "Sem obrigações" : string.Join(" · ", d.Obligations.Select(x => $"{x.Label} {Clock(x.Start)}–{Clock(x.End)}")), UiTokens.TextLabel),
            };
            if (d.CommuteMinutes > 0) lines.Add(($"Deslocamento: {d.CommuteMinutes} min no dia", UiTokens.TextSecondary));
            lines.Add((d.NightShift ? $"Acorda {Clock(d.Wake)} · sai para o turno às {Clock(d.ShiftDeparture)} · dorme às {Clock(d.Bed)} (depois do turno)" : $"Acorda {Clock(d.Wake)} · dorme {Clock(d.Bed)}", UiTokens.TextSecondary));
            lines.Add(($"Tempo livre: {Hours(d.FreeMinutes)}" + (d.Windows.Count > 0 ? $" ({string.Join(", ", d.Windows.Select(w => $"{Clock(w.Start)}–{Clock(w.End)}"))})" : ""), d.FreeDay ? UiTokens.Success : UiTokens.Info));
            if (d.PostShiftWindows.Count > 0)
                lines.Add(($"Depois do turno, se quiser jogar: {string.Join(", ", d.PostShiftWindows.Select(w => $"{Clock(w.Start)}–{Clock(w.End)}"))} (não é tempo livre: sai do sono, que se desloca)", UiTokens.TextSecondary));
            var br = d.Segments.Where(s => s.Kind == "break").ToList();
            if (br.Count > 0) lines.Add(($"Pausa do trabalho (pode jogar): {string.Join(", ", br.Select(b => $"{Clock(b.Start)}–{Clock(b.End)}"))}", UiTokens.Info));
            lines.Add(d.PlannedStart >= 0
                ? ($"Chance de jogar {d.ChancePermille / 10}% · sessão de {d.PlannedMinutes} min às {Clock(d.PlannedStart)}", UiTokens.AccentSoft)
                : ("Nenhuma janela comporta uma sessão", UiTokens.Danger));
            if (d.PlannedStart >= 0) lines.Add(($"Estimativa: {d.ExpectedMinutes} min (chance do dia com o hábito × o jogo de um dia típico, com as voltas)", UiTokens.AccentSoft));
            var chanceColor = d.ChancePermille >= 600 ? UiTokens.Success : d.ChancePermille >= 300 ? UiTokens.TextLabel : UiTokens.TextSecondary;
            groups.Add(new BarGroup
            {
                Label = DowPt[d.Dow], Highlight = d.Dow == todayDow, LabelGreen = d.FreeDay,
                SubLabel = d.PlannedStart >= 0 ? $"{d.ChancePermille / 10}%" : "–", SubColor = d.PlannedStart >= 0 ? chanceColor : UiTokens.Danger,
                Bars = new[] { (d.FreeMinutes, d.FreeDay ? UiTokens.Success : UiTokens.Info), (d.ExpectedMinutes, UiTokens.Accent) },
                Title = DowLong[d.Dow] + (d.FreeDay ? " · dia livre" : "") + (d.Dow == todayDow ? " · hoje" : ""), Lines = lines,
            });
        }
        var chart = new GroupedBarChart { Groups = groups, CustomMinimumSize = new Vector2(0, 236) };
        var legend = new HFlowContainer();
        legend.AddThemeConstantOverride("h_separation", 16);
        legend.AddThemeConstantOverride("v_separation", 4);
        legend.AddChild(LegendItem(UiTokens.Info, "Tempo livre"));
        legend.AddChild(LegendItem(UiTokens.Success, "Tempo livre (dia livre)"));
        legend.AddChild(LegendItem(UiTokens.Accent, "Jogo estimado"));
        legend.AddChild(ThemeFactory.Text("% embaixo: chance de jogar no dia", UiTokens.FontCaption, UiTokens.TextDisabled, UiFonts.Medium));
        int free = _person.Week.Sum(d => d.FreeMinutes), obl = _person.Week.Sum(d => d.ObligationMinutes);
        int sleep = _person.Week.Sum(d => d.SleepMinutes) / 7, expected = _person.Week.Sum(d => d.ExpectedMinutes);
        return Column(12, Grow(chart), legend, Divider(), Row(0,
            MiniStat(Hours(free), "Livre na semana"),
            MiniStat(Hours(expected), "Jogo estimado"),
            MiniStat(Hours(obl), "Obrigações"),
            MiniStat(Hours(sleep), "Sono por noite")));
    }

    private static Control LegendItem(Color color, string text, bool dashed = false)
    {
        var r = new HBoxContainer();
        r.AddThemeConstantOverride("separation", 6);
        var sw = dashed
            ? (Control)new ColorRect { Color = color, CustomMinimumSize = new Vector2(2, 12), SizeFlagsVertical = SizeFlags.ShrinkCenter }
            : new ColorRect { Color = color, CustomMinimumSize = new Vector2(10, 10), SizeFlagsVertical = SizeFlags.ShrinkCenter };
        r.AddChild(sw);
        r.AddChild(ThemeFactory.Text(text, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium));
        return r;
    }

    // ----- Vontade de jogar -----

    /// <summary>
    /// Medidor 1-20 com a mesma area de dica dos atributos: "tip" no formato antigo
    /// ("Titulo\n~texto") vira titulo com o valor, subtitulo e "O que afeta" em marcadores.
    /// </summary>
    private Control TraitMeter(string name, int value, string tip, string group = "Rotina")
    {
        int v20 = UiTokens.To20(value);
        var valueColor = v20 >= 14 ? AttrTier(v20) : v20 <= 5 ? UiTokens.Danger : UiTokens.TextPrimary;
        string body = tip.Contains("\n~", StringComparison.Ordinal) ? tip[(tip.IndexOf("\n~", StringComparison.Ordinal) + 2)..] : "";
        var cell = new HoverTip($"{name}|{v20} / 20|#{valueColor.ToHtml(false)}\n^{group}\n#O que afeta\n{AttributeInfo.Bullets(body)}");
        var c = Column(5);
        var r = new HBoxContainer();
        var l = ThemeFactory.Text(name, UiTokens.FontBody, UiTokens.TextLabel, UiFonts.Medium);
        l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        r.AddChild(l);
        r.AddChild(ThemeFactory.Text(v20.ToString(), UiTokens.FontBody, v20 >= 14 ? AttrTier(v20) : v20 <= 5 ? UiTokens.Danger : UiTokens.TextPrimary, UiFonts.Numbers));
        c.AddChild(r);
        c.AddChild(new PotentialBar(v20 / 20f, 0, AttrTier(v20)) { CustomMinimumSize = new Vector2(0, 3) });
        cell.AddChild(c);
        return cell;
    }

    private Control WillBody()
    {
        var t = _person.Traits;
        int free = Math.Max(1, _person.Week.Sum(d => d.FreeMinutes));
        int use = _person.ExpectedWeeklyMinutes * 100 / free;
        var useBar = new TipPanel { TooltipText = $"Uso previsto do tempo livre\nEstimativa|{Hours(_person.ExpectedWeeklyMinutes)} por semana\nTempo livre|{Hours(free)} por semana\n~A rotina e a importância do jogo, sem o humor. Quem tem muito tempo pode jogar pouco.", MouseFilter = MouseFilterEnum.Stop };
        useBar.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        useBar.AddChild(Bar("Estimado", Math.Min(100, use), 100, UiTokens.Accent, $"{use}% do livre", 110));
        // Uso real do tempo livre nos ultimos 30 dias (inclui extensoes, segunda sessao e humor).
        var detail = RoutineDetail;
        Control realBar = new Control();
        if (detail != null && detail.DayPlans.Count > 0)
        {
            int freeSum = detail.DayPlans.Sum(d => d.FreeMinutes);
            var playedByDay = new Dictionary<long, int>();
            long nowTick = _ctx.Snapshot().Tick;
            foreach (var ses in _ctx.PersonSessions(_person.Id))
            {
                long day = LocalOf(ses.Start).Day;
                playedByDay[day] = playedByDay.GetValueOrDefault(day) + (int)Math.Max(0, ((ses.End < 0 ? nowTick : ses.End) - ses.Start) / SimTime.TicksPerMinute);
            }
            int played = detail.DayPlans.Sum(d => playedByDay.TryGetValue(d.LocalDay, out int m) ? m : d.Sessions > 0 ? d.PlannedMinutes : 0);
            int real = freeSum == 0 ? 0 : played * 100 / freeSum;
            var tip = new TipPanel { TooltipText = $"Uso real do tempo livre\nJogou|{Hours(played)} em {detail.DayPlans.Count} dias\nTempo livre|{Hours(freeSum)}\n~Inclui o continuar jogando, as voltas, ficar acordado e o efeito do humor; o previsto é só a rotina.", MouseFilter = MouseFilterEnum.Stop };
            tip.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
            tip.AddChild(Bar("Real (30 dias)", Math.Min(100, real), 100, UiTokens.Success, $"{real}% do livre", 110));
            realBar = tip;
        }
        var meters = new HBoxContainer();
        meters.AddThemeConstantOverride("separation", 24);
        var mLeft = Column(12, TraitMeter("Importância do jogo", t.GameImportance, "Importância do jogo\n~Prioridade do jogo entre os lazeres. Pesa em cada decisão: jogar no dia, começar antes do horário de costume, continuar depois da sessão normal, voltar depois de uma pausa e jogar na pausa do trabalho. Não muda a duração normal da sessão. É o que define o hardcore, não o tempo livre."),
            TraitMeter("Hábito", t.Habit, HabitTip()));
        var mRight = Column(12, TraitMeter("Tendência a prolongar", t.ExtendTendency, "Tendência a prolongar\n~Dificuldade de encerrar: aumenta a vontade de continuar depois da sessão normal (até ±20%). Na hora de dormir, quando a sessão está boa ou a meta está perto, pode ficar acordado jogando (até 2 vezes por sessão e 3 h no total, dentro do corte de sono)."),
            TraitMeter("Autocontrole", t.SelfControl, "Autocontrole\n~Freia ficar acordado jogando depois da hora de dormir e a escapada no expediente. Independe da importância do jogo."));
        mLeft.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        mRight.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        meters.AddChild(mLeft);
        meters.AddChild(mRight);
        // Humor (D-038/D-039): a satisfacao com o jogo e o engajamento mudam a vontade de jogar de verdade.
        Control moodRow = new Control();
        if (View is { } v)
        {
            var rt = _ctx.Content.Retention;
            int factor = Simulation.Core.Behavior.BehaviorSystem.PlayFactor(v.Mood.Engagement, rt);
            int pct = (factor - 1000) / 10;
            string effect = pct == 0 ? "sem efeito" : $"{(pct > 0 ? "+" : "−")}{Math.Abs(pct)}% na chance de jogar";
            var moodColor = pct >= 5 ? UiTokens.Success : pct <= -5 ? UiTokens.Danger : UiTokens.TextLabel;
            var mrow = new HBoxContainer();
            mrow.AddThemeConstantOverride("separation", 24);
            var ml = TraitMeter("Satisfação com o jogo", v.Mood.Satisfaction, "Satisfação com o jogo\n~Média das notas das sessões: gosto pelas atividades, progresso, novidade, companhia, tédio e reveses. Alimenta o engajamento.", "Humor (muda com o que vive no jogo)");
            var mr = TraitMeter("Engajamento", v.Mood.Engagement, $"Engajamento\n~Vontade de jogar vinda do humor. Hoje: {effect}. Engajado joga mais dias que a rotina; desanimado, menos (e pode acabar desistindo).", "Humor (muda com o que vive no jogo)");
            ml.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            mr.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            mrow.AddChild(ml);
            mrow.AddChild(mr);
            string today = TodayDisposition() is { } disp ? $" · disposição de hoje: {(disp >= 0 ? "+" : "−")}{Math.Abs(disp) / 10}%" : "";
            var note = ThemeFactory.Text($"Humor agora: {effect}{today}", UiTokens.FontSmall, moodColor, UiFonts.Medium);
            moodRow = Column(8, mrow, note);
        }
        return Column(12,
            meters,
            moodRow,
            Divider(),
            HabitRow(),
            useBar,
            realBar,
            Divider(),
            Wrap(RoutineVerdict(), UiTokens.FontSmall, UiTokens.TextLabel));
    }

    /// <summary>
    /// O habito de jogo pela mesma conta do planejador (semana tipica): previsto por semana, a
    /// sessao de um dia util (ou livre, se nao joga nos uteis), quando costuma comecar e sair.
    /// </summary>
    private Control HabitRow()
    {
        var t = _person.Traits;
        static int Median(List<int> v) { v.Sort(); return v[v.Count / 2]; }
        var busy = _person.Week.Where(d => !d.FreeDay && d.PlannedStart >= 0).ToList();
        var days = busy.Count > 0 ? busy : _person.Week.Where(d => d.PlannedStart >= 0).ToList();
        string which = busy.Count > 0 ? "dia útil" : "dia livre";
        if (days.Count == 0)
            return Row(0, MiniStat(Hours(_person.ExpectedWeeklyMinutes), "Estimativa por semana"), MiniStat("–", "Sem tempo para uma sessão"));
        int start = Median(days.Select(d => d.PlannedStart).ToList());
        int minutes = Median(days.Select(d => d.PlannedMinutes).ToList());
        int end = Median(days.Select(d => d.PlannedEnd >= 0 ? d.PlannedEnd : d.PlannedStart + d.PlannedMinutes).ToList());
        var row = Row(0,
            MiniStat(Hours(_person.ExpectedWeeklyMinutes), "Estimativa/semana"),
            MiniStat(minutes >= 60 ? Hours(minutes) : $"{minutes} min", $"Jogo num {which}"),
            MiniStat($"{t.PreferredStartHour:00}h", "Horário preferido"),
            MiniStat(Clock(start), "Costuma começar"),
            MiniStat(Clock(end), "Costuma sair"));
        var tip = new TipPanel
        {
            TooltipText = $"Hábito de jogo ({which}, hora local)\nHorário preferido|{t.PreferredStartHour:00}h\nCostuma começar|{Clock(start)}\nSai (última sessão)|{Clock(end)}\nJogo num dia típico|{minutes} min\nSessão normal da pessoa|{t.SessionMinutes} min\n~Gosto e prática são coisas separadas: a pessoa pode preferir uma hora e a rotina só permitir outra. Quem prioriza o jogo começa logo que fica livre; o casual espera mais.\n~A sessão normal é uma preferência: ao chegar nela, a pessoa decide se continua (vontade de jogar contra saciedade, cansaço e outras opções). Depois de parar, pode voltar após uma pausa. Num dia típico pela estimativa, sem o humor.",
            MouseFilter = MouseFilterEnum.Stop,
        };
        tip.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        tip.AddChild(row);
        return tip;
    }

    /// <summary>
    /// Dica do habito (R4): o traco e a regularidade recente pela mesma conta do planejador (dias
    /// com oportunidade; faltar por obrigacao nao conta; sessao interrompida conta como jogo).
    /// </summary>
    private string HabitTip()
    {
        var rs = _ctx.Content.Routine;
        string head = "Hábito\n";
        if (RoutineDetail is { } detail && detail.DayPlans.Count > 0)
        {
            long today = LocalOf(_ctx.Snapshot().Tick).Day;
            var (reg, played, missed) = Simulation.Core.Behavior.DailySchedule.Regularity(detail.DayPlans, today, rs.HabitHistoryDays);
            int factor = Simulation.Core.Behavior.DailySchedule.HabitFactor(_person.Traits.Habit, reg, rs);
            int pct = (factor - 1000) / 10;
            head += played + missed == 0
                ? $"Últimos {rs.HabitHistoryDays} dias|sem dias com oportunidade\n"
                : $"Últimos {rs.HabitHistoryDays} dias|jogou em {played} de {played + missed} com oportunidade\nEfeito na chance hoje|{(pct >= 0 ? "+" : "−")}{Math.Abs(pct)}%\n";
        }
        return head + "~O traço diz quanto a regularidade recente pesa: quem vem jogando sempre que pode mantém o embalo; quem vem escolhendo outra coisa tende a continuar. Os dias mais recentes pesam mais.\n~Faltar por obrigação não conta; sessão interrompida conta como jogo (a escolha foi jogar).";
    }

    /// <summary>Disposicao sorteada para hoje (R4, permille), se o dia ja foi planejado.</summary>
    private int? TodayDisposition()
    {
        if (RoutineDetail is not { } detail || detail.DayPlans.Count == 0) return null;
        long today = LocalOf(_ctx.Snapshot().Tick).Day;
        var plan = detail.DayPlans.LastOrDefault(d => d.LocalDay == today);
        return plan == null || plan.Reason == "came_back" ? null : plan.DispositionPermille;
    }

    /// <summary>Leitura do perfil de rotina em uma frase (como um relatorio de olheiro).</summary>
    private string RoutineVerdict()
    {
        var t = _person.Traits;
        int imp = UiTokens.To20(t.GameImportance), ext = UiTokens.To20(t.ExtendTendency), sc = UiTokens.To20(t.SelfControl), hab = UiTokens.To20(t.Habit);
        string first = imp >= 15 ? "O jogo é prioridade entre os lazeres" : imp <= 7 ? "O jogo é um lazer entre outros" : "Dá uma importância moderada ao jogo";
        string second = ext >= 13 && sc <= 9 ? "se empolga e tem pouco freio: costuma estender a sessão e às vezes dorme menos"
            : ext >= 13 ? "se empolga, mas o autocontrole segura boa parte das extensões"
            : ext <= 8 && sc >= 12 ? "costuma parar na hora combinada"
            : "raramente passa muito do planejado";
        string third = hab >= 14 ? " Joga com regularidade." : hab <= 7 ? " Joga em rajadas, sem muita regularidade." : "";
        return $"{first}; {second}.{third}";
    }

    // ----- Ultimos 30 dias -----

    private Control DaysBody(CharacterDetailView? detail)
    {
        if (detail == null) return Wrap("Carregando…");
        var plans = detail.DayPlans;
        // Cada sessao conta no dia local em que comecou (como a agenda), mesmo passando da meia-noite.
        var playedByDay = new Dictionary<long, int>();
        long nowTick = _ctx.Snapshot().Tick;
        foreach (var ses in _ctx.PersonSessions(_person.Id))
        {
            long day = LocalOf(ses.Start).Day;
            long end = ses.End < 0 ? nowTick : ses.End;
            playedByDay[day] = playedByDay.GetValueOrDefault(day) + (int)Math.Max(0, (end - ses.Start) / SimTime.TicksPerMinute);
        }
        var points = new List<DayPoint>();
        foreach (var d in plans)
        {
            var date = _ctx.Calendar.StartDate.AddDays(d.LocalDay);
            int dow = ((int)date.DayOfWeek + 6) % 7;
            bool logged = playedByDay.TryGetValue(d.LocalDay, out int playedMin);
            if (!logged && d.Sessions > 0) playedMin = d.PlannedMinutes;   // antes do registro de sessoes da tela: o planejado
            var lines = new List<(string, Color)>
            {
                (d.Sessions > 1 ? $"Jogou ({d.Sessions} sessões)" : DayReasonPt(ReasonOf(d)), DayReasonColor(ReasonOf(d))),
                ($"Tempo livre: {Hours(d.FreeMinutes)}", UiTokens.Info),
            };
            if (d.Sessions > 0 || playedMin > 0)
                lines.Add(($"Jogou: {playedMin} min{(logged ? "" : " (planejado)")}" + (d.FirstStartMinute >= 0 ? $" · começou {Clock(d.FirstStartMinute)}" : ""), UiTokens.AccentSoft));
            if (d.Sittings > 0)
                lines.Add(($"Sessões: {d.Sittings}" + (d.Returns > 0 ? $" (voltou {d.Returns} vez{(d.Returns > 1 ? "es" : "")} depois de uma pausa)" : "")
                    + $" · disposição {(d.DispositionPermille >= 0 ? "+" : "−")}{Math.Abs(d.DispositionPermille) / 10}%", UiTokens.TextSecondary));
            if (d.DeclinedReturns > 0) lines.Add(("Pensou em voltar e preferiu outra coisa", UiTokens.TextSecondary));
            if (d.SkippedAfterWork > 0) lines.Add(("Já tinha jogado no trabalho e não quis jogar à noite", UiTokens.TextSecondary));
            if (d.SleepMinutes > 0) lines.Add(($"Sono na noite anterior: {Hours(d.SleepMinutes)}" + (d.DeficitMinutes > 0 ? $" · déficit {Hours(d.DeficitMinutes)}" : ""), UiTokens.TextSecondary));
            if (d.WorkOverlapMinutes > 0) lines.Add(($"Escapada no expediente: {d.WorkOverlapMinutes} min" + (d.WorkSessions > 1 ? " + pausa" : ""), UiTokens.Warning));
            else if (d.WorkSessions > 0) lines.Add(("Jogou na pausa do trabalho", UiTokens.Info));
            if (d.CommitmentId.Length > 0)
                lines.Add(($"Compromisso: {CommitmentName(d.CommitmentId)} ({Clock(d.CommitmentStart)}–{Clock(d.CommitmentEnd)})", UiTokens.Exceptional));
            points.Add(new DayPoint
            {
                Label = $"{date:dd/MM}", FreeDay = d.FreeDay, FreeMinutes = d.FreeMinutes, PlayedMinutes = playedMin,
                ReasonColor = DayReasonColor(ReasonOf(d)), Work = d.WorkSessions > 0, Commitment = d.CommitmentId.Length > 0,
                Title = $"{DowLong[dow]}, {date:dd/MM}" + (d.FreeDay ? " · dia livre" : ""), Lines = lines,
            });
        }
        var chart = new DayTrendChart { Days = points, CustomMinimumSize = new Vector2(0, 220) };
        int days = plans.Count, withPlay = plans.Count(PlayedOn);
        int freeSum = plans.Sum(p => p.FreeMinutes), playedSum = points.Sum(p => p.PlayedMinutes);
        int work = plans.Sum(p => p.WorkSessions);
        var legend = new HFlowContainer();
        legend.AddThemeConstantOverride("h_separation", 16);
        legend.AddThemeConstantOverride("v_separation", 4);
        legend.AddChild(LegendItem(new Color(UiTokens.Info, 0.6f), "Tempo livre"));
        legend.AddChild(LegendItem(UiTokens.Accent, "Jogou"));
        foreach (var reason in plans.Where(p => !PlayedOn(p)).Select(ReasonOf).Distinct().OrderBy(r => r, StringComparer.Ordinal))
            legend.AddChild(LegendDot(DayReasonColor(reason), DayReasonPt(reason)));
        legend.AddChild(LegendItem(new Color(UiTokens.Success, 0.3f), "Dia livre"));
        if (work > 0) legend.AddChild(LegendItem(UiTokens.Info, "Jogou no horário de trabalho"));
        if (plans.Any(p => p.CommitmentId.Length > 0)) legend.AddChild(LegendDot(UiTokens.Exceptional, "Compromisso (festa, visita…)"));
        return Column(12, Grow(chart), legend, Divider(), Row(0,
            MiniStat(days == 0 ? "–" : $"{withPlay} de {days}", "Dias com jogo"),
            MiniStat(freeSum == 0 ? "–" : $"{playedSum * 100 / freeSum}%", "Do tempo livre jogado"),
            MiniStat(withPlay == 0 ? "–" : Hours(points.Where(p => p.PlayedMinutes > 0).Sum(p => p.PlayedMinutes) / Math.Max(1, points.Count(p => p.PlayedMinutes > 0))), "Por dia de jogo"),
            MiniStat(work == 0 ? "–" : work.ToString(), "Sessões no trabalho")));
    }

    private static Control LegendDot(Color color, string text)
    {
        var r = new HBoxContainer();
        r.AddThemeConstantOverride("separation", 6);
        var dot = new Control { CustomMinimumSize = new Vector2(10, 10), SizeFlagsVertical = SizeFlags.ShrinkCenter };
        dot.Draw += () => dot.DrawCircle(new Vector2(5, 5), 4, color);
        r.AddChild(dot);
        r.AddChild(ThemeFactory.Text(text, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium));
        return r;
    }

    // ----- Sono e cansaco -----

    private Control SleepBody(CharacterDetailView? detail)
    {
        if (detail == null) return Wrap("Carregando…");
        var rs = _ctx.Content.Routine;
        int deficit = detail.SleepDeficitMinutes;
        int max = rs.MaxDeficitMinutes * (500 + _person.SleepDebtTolerance) / 1000;
        int fatigue = Math.Min(1000, deficit * 1000 / Math.Max(1, rs.FatigueFullDeficitMinutes));
        var fatigueColor = fatigue >= rs.TiredReasonFatigue ? UiTokens.Danger : fatigue >= 250 ? UiTokens.Warning : UiTokens.Success;
        var ring = new RingGauge { Value = fatigue / 1000f, Center = $"{fatigue / 10}%", Caption = "cansaço", Color = fatigueColor, CustomMinimumSize = new Vector2(92, 92) };
        var ringTip = new TipPanel { TooltipText = $"Cansaço\nDéficit|{Hours(deficit)}\nCansaço total com|{Hours(rs.FatigueFullDeficitMinutes)} de déficit\n~Reduz a chance de jogar (até −{rs.FatigueChancePenalty / 10}%), a sessão desejada (até −{rs.FatigueDurationPenalty / 10}%) e a nota da sessão. Acima de {rs.TiredReasonFatigue / 10}%, o dia sem jogo fica como \"cansado\".", MouseFilter = MouseFilterEnum.Stop, SizeFlagsVertical = SizeFlags.ShrinkCenter };
        ringTip.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        ringTip.AddChild(ring);
        // Ao lado do anel so o deficit; os outros dados ocupam a largura toda (cabem no card estreito de 1366).
        var side = Column(6,
            KeyValue("Déficit", Hours(deficit), deficit > 0 ? UiTokens.Warning : UiTokens.TextPrimary),
            KeyValue("Teto", Hours(max)));
        side.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        var (bedLabel, bedValue) = BedtimeLabel();
        var top = Column(8, Row(14, ringTip, side),
            KeyValue("Precisa", $"{Hours(_person.SleepNeedMinutes)} por noite"),
            KeyValue(bedLabel, bedValue),
            KeyValue("Perdido para o jogo", detail.SleepCutTotalMinutes == 0 ? "nenhum" : Hours(detail.SleepCutTotalMinutes), detail.SleepCutTotalMinutes > 0 ? UiTokens.Warning : UiTokens.TextPrimary));
        var nights = new List<(int, string, string, IReadOnlyList<(string, Color)>)>();
        foreach (var d in detail.DayPlans.Where(p => p.SleepMinutes > 0))
        {
            var date = _ctx.Calendar.StartDate.AddDays(d.LocalDay);
            int dow = ((int)date.DayOfWeek + 6) % 7;
            int diff = d.SleepMinutes - _person.SleepNeedMinutes;
            var lines = new List<(string, Color)>
            {
                ($"Dormiu {Hours(d.SleepMinutes)} ({(diff >= 0 ? "+" : "−")}{Math.Abs(diff)} min da necessidade)", SleepChart.ColorFor(d.SleepMinutes, _person.SleepNeedMinutes)),
                ($"Déficit ao acordar: {Hours(d.DeficitMinutes)}", UiTokens.TextSecondary),
            };
            if (diff > 30) lines.Add(("Dormiu mais para recuperar", UiTokens.Success));
            nights.Add((d.SleepMinutes, $"{date:dd}", $"Noite antes de {DowLong[dow].ToLowerInvariant()}, {date:dd/MM}", lines));
        }
        var chart = new SleepChart { Nights = nights, NeedMinutes = _person.SleepNeedMinutes, CustomMinimumSize = new Vector2(0, 120) };
        return Column(12, top, Divider(), Grow(chart),
            Wrap("Tracejado: a necessidade. Laranja, noite curta; verde, recuperação.", UiTokens.FontCaption, UiTokens.TextDisabled));
    }

    /// <summary>
    /// Hora real de deitar pela semana tipica (a mesma conta da simulacao): nas noites antes de
    /// obrigacao e nas noites antes de dia livre; turno da noite dorme de manha.
    /// </summary>
    private (string Label, string Value) BedtimeLabel()
    {
        var week = _person.Week;
        if (week.Any(d => d.Blocks.Any(b => b.End > 1440))) return ("Dorme", "de manhã, depois do turno");
        static int Median(List<int> v) { v.Sort(); return v[v.Count / 2]; }
        var beforeBusy = Enumerable.Range(0, 7).Where(d => !week[(d + 1) % 7].FreeDay).Select(d => week[d].Bed).ToList();
        var beforeFree = Enumerable.Range(0, 7).Where(d => week[(d + 1) % 7].FreeDay).Select(d => week[d].Bed).ToList();
        if (beforeBusy.Count == 0) return ("Deita", Clock(Median(beforeFree)));
        if (beforeFree.Count == 0) return ("Deita", Clock(Median(beforeBusy)));
        int busy = Median(beforeBusy), free = Median(beforeFree);
        return busy == free ? ("Deita", $"{Clock(busy)}, todo dia") : ("Deita", $"{Clock(busy)} · dia livre {Clock(free)}");
    }

    // ----- Motivos -----

    private Control ReasonsBody(CharacterDetailView? detail)
    {
        if (detail == null) return Wrap("Carregando…");
        var plans = detail.DayPlans;
        if (plans.Count == 0) return Wrap("A agenda aparece depois do primeiro dia.");
        var groups = plans.GroupBy(ReasonOf).OrderByDescending(g => g.Count()).ThenBy(g => g.Key, StringComparer.Ordinal).ToList();
        int played = plans.Count(PlayedOn);
        var donut = new DonutChart
        {
            CustomMinimumSize = new Vector2(112, 112), SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            Slices = groups.Select(g => ((float)g.Count(), DayReasonColor(g.Key))).ToArray(),
            Center = $"{played}/{plans.Count}", Caption = "com jogo",
        };
        var col = Column(12, donut);
        var list = Column(6);
        foreach (var g in groups)
        {
            var r = new HBoxContainer();
            r.AddThemeConstantOverride("separation", 8);
            r.AddChild(new ColorRect { Color = DayReasonColor(g.Key), CustomMinimumSize = new Vector2(8, 8), SizeFlagsVertical = SizeFlags.ShrinkCenter });
            var l = ThemeFactory.Text(DayReasonPt(g.Key), UiTokens.FontSmall, UiTokens.TextLabel);
            l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            l.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            r.AddChild(l);
            r.AddChild(ThemeFactory.Text($"{g.Count()}", UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Numbers));
            list.AddChild(r);
        }
        col.AddChild(list);
        int streak = 0;
        for (int i = plans.Count - 1; i >= 0 && PlayedOn(plans[i]); i--) streak++;
        var withPlay = plans.Where(p => p.Sessions > 0).ToList();   // media da sessao principal planejada
        int noTime = plans.Count(p => p.Reason == "no_free_time");
        col.AddChild(Divider());
        col.AddChild(Row(0,
            MiniStat(streak == 0 ? "–" : $"{streak} {(streak == 1 ? "dia" : "dias")}", "Sequência atual"),
            MiniStat(withPlay.Count == 0 ? "–" : $"{withPlay.Average(p => p.PlannedMinutes):0} min", "Planejado por dia de jogo")));
        if (noTime > 0) col.AddChild(Wrap($"{noTime} {(noTime == 1 ? "dia" : "dias")} sem uma janela que comporte uma sessão.", UiTokens.FontCaption, UiTokens.TextDisabled));
        return col;
    }

    // ----- Ocupacao, localidade e financas -----

    private Control OccupationBody(Simulation.Core.Content.OccupationDefinition? occ)
    {
        if (occ == null) return Wrap(_person.OccupationId);
        var week = _person.Week;
        bool remote = occ.Blocks.Any(b => b.Remote);
        // Topo: nome, o que faz e as condicoes, lado a lado (quebram linha se faltar espaco).
        var head = new HFlowContainer();
        head.AddThemeConstantOverride("h_separation", 8);
        head.AddThemeConstantOverride("v_separation", 6);
        var title = new HBoxContainer();
        title.AddThemeConstantOverride("separation", 10);
        title.AddChild(new Icon(remote ? "server" : occ.Blocks.Count == 0 ? "sun" : "landmark", 18, UiTokens.AccentSoft) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
        var nameLabel = ThemeFactory.Text(occ.Name, 15, UiTokens.TextPrimary, UiFonts.SemiBold);
        nameLabel.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        title.AddChild(nameLabel);
        head.AddChild(title);
        head.AddChild(Chip(KindIcon(occ.Kind, occ.Id), KindPt(occ.Kind, occ.Id), UiTokens.Accent));
        // O essencial da ocupacao em numeros.
        int load = week.Sum(d => d.ObligationMinutes), workDays = week.Count(d => !d.FreeDay);
        long income = _person.IncomeCentsPerMonth, support = _person.SupportCentsPerMonth;
        var facts = Row(0,
            MiniStat(ShiftPt(week), "Turno"),
            MiniStat(load == 0 ? "–" : Hours(load), "Obrigações por semana"),
            MiniStat(workDays == 0 ? "–" : $"{workDays}", "Dias com obrigação"),
            MiniStat(income > 0 ? $"R$ {PtBr.Decimal(income / 100.0, 0)}" : support > 0 ? $"R$ {PtBr.Decimal(support / 100.0, 0)}" : "–",
                income > 0 ? "Renda por mês" : support > 0 ? "Apoio por mês" : "Sem renda"));
        // Condicoes em etiquetas com icone, no topo ao lado do tipo.
        var chips = head;
        int perTrip = week.SelectMany(d => d.Blocks).Select(b => b.Commute).DefaultIfEmpty(0).Max();
        if (occ.Blocks.Count == 0) chips.AddChild(Chip("sun", "Sem obrigações fixas", UiTokens.Success));
        else chips.AddChild(remote ? Chip("server", "Trabalho remoto", UiTokens.Info) : Chip("landmark", "Presencial", UiTokens.TextLabel));
        if (perTrip > 0) chips.AddChild(Chip("route", $"Deslocamento {perTrip} min por trecho", UiTokens.TextLabel));
        if (occ.Blocks.Count > 0) chips.AddChild(Chip("calendar-days", occ.WorksHolidays ? "Trabalha em feriado" : "Folga em feriado", UiTokens.TextLabel));
        foreach (var br in occ.Blocks.Where(b => b.Remote).SelectMany(b => b.Breaks).Select(b => $"{b.Start}–{b.End}").Distinct())
            chips.AddChild(Chip("pause", $"Pausa {br}: pode jogar", UiTokens.Info));
        if (occ.PlayDuringWorkFlexibility > 0)
            chips.AddChild(Chip("timer", $"Escapada no expediente às vezes ({UiTokens.To20(occ.PlayDuringWorkFlexibility)}/20)", UiTokens.Warning));
        // A semana desta pessoa, dia por dia.
        long now = _ctx.Snapshot().Tick;
        var (today, _) = LocalOf(now);
        int todayDow = ((int)_ctx.Calendar.StartDate.AddDays(today).DayOfWeek + 6) % 7;
        var tiles = new HBoxContainer();
        tiles.AddThemeConstantOverride("separation", 6);
        foreach (var d in week) tiles.AddChild(DayTile(d, d.Dow == todayDow));
        return Column(12, head, Wrap(occ.Description, UiTokens.FontSmall, UiTokens.TextSecondary), facts,
            Column(8, Section("Dia a dia (horário desta pessoa)"), tiles));
    }

    /// <summary>O que a pessoa faz, em linguagem direta.</summary>
    private static string KindPt(string kind, string id) => kind switch
    {
        "work" => "Trabalha",
        "study" => "Estuda",
        "work_study" => "Trabalha e estuda",
        "care" => "Cuida da casa e da família",
        _ => id == "aposentado" ? "Aposentado" : "Não trabalha nem estuda",
    };

    private static string KindIcon(string kind, string id) => kind switch
    {
        "study" => "book-open", "work_study" => "layers", "care" => "heart-handshake", "work" => "landmark", _ => id == "aposentado" ? "sun" : "user",
    };

    /// <summary>Turno pelos horarios desta pessoa: manha, tarde, integral, noite ou turno da noite.</summary>
    private static string ShiftPt(IReadOnlyList<WeekDayView> week)
    {
        var day = week.FirstOrDefault(d => d.Blocks.Count > 0);
        if (day == null) return "–";
        var parts = new List<string>();
        foreach (var b in day.Blocks)
        {
            string part = b.End > 1440 ? "Noturno"
                : b.Start < 12 * 60 && b.End > 14 * 60 ? "Integral"
                : b.Start < 12 * 60 ? "Manhã"
                : b.Start < 18 * 60 ? "Tarde" : "Noite";
            if (!parts.Contains(part)) parts.Add(part);
        }
        return string.Join(" + ", parts);
    }

    private static Control Chip(string icon, string text, Color color)
    {
        var p = new PanelContainer();
        var sb = new StyleBoxFlat { BgColor = new Color(color, 0.08f), BorderColor = new Color(color, 0.35f), ContentMarginLeft = 8, ContentMarginRight = 10, ContentMarginTop = 3, ContentMarginBottom = 3 };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(10);
        p.AddThemeStyleboxOverride("panel", sb);
        var r = new HBoxContainer();
        r.AddThemeConstantOverride("separation", 6);
        r.AddChild(new Icon(icon, 14, color) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
        r.AddChild(ThemeFactory.Text(text, UiTokens.FontSmall, color == UiTokens.TextLabel ? UiTokens.TextLabel : color.Lightened(0.15f), UiFonts.Medium));
        p.AddChild(r);
        return p;
    }

    private static string BlockIcon(string label, bool remote) => remote ? "server" : label switch { "estudo" => "book-open", "cuidados" => "heart-handshake", _ => "landmark" };

    /// <summary>Cartao de um dia: obrigacoes com icone e horario, tempo livre e a maior janela.</summary>
    private Control DayTile(WeekDayView d, bool today)
    {
        var tile = new TipPanel { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Stop };
        var sb = new StyleBoxFlat
        {
            BgColor = new Color("#12161C"), BorderColor = today ? UiTokens.Accent : d.FreeDay ? new Color(UiTokens.Success, 0.35f) : UiTokens.Border,
            ContentMarginLeft = 8, ContentMarginRight = 8, ContentMarginTop = 7, ContentMarginBottom = 8,
        };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(3);
        tile.AddThemeStyleboxOverride("panel", sb);
        var col = Column(5);
        // Hoje: rotulo e borda dourados (como na Semana tipica); "hoje" fica no tooltip, para o rotulo caber no dia estreito.
        var dayLabel = ThemeFactory.Text(DowPt[d.Dow], UiTokens.FontSmall, today ? UiTokens.Accent : d.FreeDay ? UiTokens.Success : UiTokens.TextPrimary, UiFonts.SemiBold);
        col.AddChild(dayLabel);
        if (d.Blocks.Count == 0)
        {
            var r = new HBoxContainer();
            r.AddThemeConstantOverride("separation", 5);
            r.AddChild(new Icon("sun", 14, UiTokens.Success) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
            r.AddChild(ThemeFactory.Text("livre", UiTokens.FontCaption, UiTokens.Success, UiFonts.Medium));
            col.AddChild(r);
        }
        foreach (var b in d.Blocks)
        {
            var r = new HBoxContainer();
            r.AddThemeConstantOverride("separation", 5);
            r.AddChild(new Icon(BlockIcon(b.Label, b.Remote), 14, b.Remote ? UiTokens.Info : UiTokens.TextSecondary) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
            // Entrada e saida em duas linhas ao lado do icone: cabe no dia estreito de 1366 sem cortar (o horario
            // inteiro fica no tooltip).
            var times = Column(0,
                ThemeFactory.Text(Clock(b.Start), UiTokens.FontCaption, UiTokens.TextLabel, UiFonts.Numbers),
                ThemeFactory.Text(Clock(b.End), UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers));
            r.AddChild(times);
            col.AddChild(r);
        }
        col.AddChild(new Control { SizeFlagsVertical = SizeFlags.ExpandFill });
        col.AddChild(new ColorRect { Color = UiTokens.Divider, CustomMinimumSize = new Vector2(0, 1) });
        var free = ThemeFactory.Text(Hours(d.FreeMinutes), 15, d.FreeMinutes >= 120 ? (d.FreeDay ? UiTokens.Success : UiTokens.Info) : UiTokens.Warning, UiFonts.SemiBold);
        col.AddChild(free);
        var main = d.Windows.Count == 0 ? ((int Start, int End)?)null : d.Windows.OrderByDescending(w => w.End - w.Start).First();
        var win = ThemeFactory.Text(main is { } m ? $"{Clock(m.Start)}–{Clock(m.End)}" + (d.Windows.Count > 1 ? $" +{d.Windows.Count - 1}" : "") : "sem janela", UiTokens.FontCaption, main == null ? UiTokens.Danger : UiTokens.TextSecondary, UiFonts.Numbers);
        win.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        col.AddChild(win);
        tile.AddChild(col);
        var obligations = d.Blocks.Count == 0 ? "sem obrigações" : string.Join("\n", d.Blocks.Select(b => $"{b.Label}{(b.Remote ? " (remoto)" : "")}|{Clock(b.Start)}–{Clock(b.End)}"));
        tile.TooltipText = $"{DowLong[d.Dow]}{(d.FreeDay ? " · dia livre" : "")}{(today ? " · hoje" : "")}\n{(d.Blocks.Count == 0 ? "Obrigações|nenhuma" : obligations)}"
            + (d.CommuteMinutes > 0 ? $"\nDeslocamento|{d.CommuteMinutes} min no dia" : "")
            + $"\nAcorda|{Clock(d.Wake)}" + (d.NightShift ? $"\nSai para o turno|{Clock(d.ShiftDeparture)}\nDorme|{Clock(d.Bed)} (depois do turno)" : $"\nDorme|{Clock(d.Bed)}") + $"\nTempo livre|{Hours(d.FreeMinutes)}"
            + (d.Windows.Count > 0 ? $"\n~Janelas livres: {string.Join(", ", d.Windows.Select(w => $"{Clock(w.Start)}–{Clock(w.End)}"))}" : "\n~Sem janela livre que comporte uma sessão.");
        return tile;
    }

    private Control PlaceBody()
    {
        int off = _ctx.Content.Localities.TryGetValue(_person.ResidenceId, out var loc) ? loc.UtcOffsetMinutes : 0;
        long now = _ctx.Snapshot().Tick;
        var localNow = _ctx.Calendar.ToDateTime(Math.Max(0, now + _person.LocalOffsetMinutes * SimTime.TicksPerMinute));
        string diff = _person.LocalOffsetMinutes == 0 ? "mesmo horário do servidor"
            : $"{(_person.LocalOffsetMinutes > 0 ? "+" : "−")}{Math.Abs(_person.LocalOffsetMinutes) / 60} h do servidor";
        return Column(10,
            Row(0, MiniStat($"{localNow:HH:mm}", "Hora local agora"), MiniStat($"UTC{(off >= 0 ? "+" : "−")}{Math.Abs(off) / 60}", diff)),
            Divider(),
            KeyValue("Mora em", LocalityName(_person.ResidenceId)),
            KeyValue("Nacionalidade", LocalityName(_person.NationalityId) + (_person.NationalityId != _person.ResidenceId ? " (mora fora)" : "")),
            KeyValue("Idiomas", string.Join(", ", _person.Languages.Select(LanguageName))),
            Wrap("A nacionalidade não decide horário, renda nem personalidade; os idiomas servirão ao social (M6).", UiTokens.FontCaption, UiTokens.TextDisabled));
    }

    private Control MoneyBody()
    {
        var t = _person.Traits;
        long income = _person.IncomeCentsPerMonth, support = _person.SupportCentsPerMonth, total = income + support;
        var donut = new DonutChart
        {
            CustomMinimumSize = new Vector2(96, 96), SizeFlagsVertical = SizeFlags.ShrinkCenter,
            Slices = new[] { ((float)income, UiTokens.Info), ((float)support, UiTokens.AccentSoft) },
            Center = total >= 100_000 ? $"{PtBr.Decimal(total / 100_000.0, 1)} mil" : $"{total / 100}", Caption = "R$/mês",
        };
        var legend = Column(6,
            KeyValue("Trabalho", $"R$ {PtBr.Decimal(income / 100.0, 0)}", UiTokens.Info),
            KeyValue("Apoio e outras", $"R$ {PtBr.Decimal(support / 100.0, 0)}", UiTokens.AccentSoft),
            KeyValue("Lazer", $"R$ {PtBr.Decimal(t.LeisureBudgetCentsPerMonth / 100.0, 0)}"));
        legend.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        int pay = UiTokens.To20(t.WillingnessToPay), adv = UiTokens.To20(t.PaidAdvantageSensitivity);
        return Column(10, Row(14, donut, legend), Divider(),
            TraitMeter("Disposição a pagar", t.WillingnessToPay, "Disposição a pagar\n~Quanto aceita gastar no jogo; sem efeito até a monetização (M7).", "Finanças"),
            TraitMeter("Aversão a vantagem paga", t.PaidAdvantageSensitivity, "Aversão a vantagem paga\n~Quanto se incomoda com vantagens compradas; sem efeito até a monetização (M7).", "Finanças"));
    }
}

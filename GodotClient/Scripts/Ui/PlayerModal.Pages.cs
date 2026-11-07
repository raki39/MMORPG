using Godot;
using Simulation.Core.Content;
using Simulation.Core.Events;
using Simulation.Core.Population;
using GodotClient.Sim;

namespace GodotClient.Ui;

/// <summary>
/// Paginas da ficha (D-068, etapa 3): cada aba com o seu assunto, em telas inteiras (ScreenPage) e cards de
/// altura fixa (rolagem dentro do card). A Visao geral so resume quem e e o que acontece agora; cada resumo
/// tem um atalho no titulo para a aba do assunto.
/// </summary>
public partial class PlayerModal
{
    private readonly Dictionary<string, int> _tabAt = new(StringComparer.Ordinal);

    private void Tab(string name, Func<Control> build, bool live = false)
    {
        _tabAt[name] = TabCount;
        AddTab(name, build, live);
    }

    private void OpenTab(string name)
    {
        if (_tabAt.TryGetValue(name, out int i)) SelectTab(i);
    }

    /// <summary>Atalho no titulo de um card de resumo: abre a aba do assunto.</summary>
    private Control TabLink(string tab)
    {
        var b = new TipButton
        {
            TooltipText = $"Abrir a aba {tab}", FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(22, 20),
            MouseDefaultCursorShape = CursorShape.PointingHand, SizeFlagsVertical = SizeFlags.ShrinkCenter,
        };
        b.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
        var hover = ThemeFactory.Box(UiTokens.SurfaceHover, 0, radius: 3);
        b.AddThemeStyleboxOverride("hover", hover);
        b.AddThemeStyleboxOverride("pressed", hover);
        var ic = new Icon("chevron-right", 14, UiTokens.TextLabel) { MouseFilter = MouseFilterEnum.Ignore };
        ic.SetAnchorsPreset(LayoutPreset.FullRect);
        b.AddChild(ic);
        b.Pressed += () => OpenTab(tab);
        return b;
    }

    /// <summary>Conteudo de uma linha clicavel: nada dentro dela captura o mouse (o clique e o tooltip sao da linha).</summary>
    private static T IgnoreMouse<T>(T c) where T : Control
    {
        c.MouseFilter = MouseFilterEnum.Ignore;
        foreach (var n in c.GetChildren())
            if (n is Control k) IgnoreMouse(k);
        return c;
    }

    private static Label Caption(string text) => ThemeFactory.Text(text, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers);

    private const string NowInfo = "O que está fazendo e por quê\n~A última decisão mostra os fatores que somaram ou tiraram pontos da opção escolhida.";
    private const string MoodInfo = "Como a pessoa está se sentindo no jogo (D-038)\n~Cada sessão vira uma nota: gosto pelas atividades, encaixe de nível, lotação, companhia, tédio, risco, progresso contra a expectativa, novidade e conquistas. A satisfação é a média dessas notas; o engajamento muda quanto ela joga; a intenção de continuar decide se fica.";

    // ---------------- Visao geral ----------------

    private Control BuildProfile()
    {
        var v = View;
        if (v == null)
            return new ScreenPage().Add(Grid(12,
                (FillCard("Situação", Wrap("Ainda não criou conta no MMORPG; pode se registrar nos próximos dias do lançamento.")), 4),
                (FillCard("Atributos em destaque", AttributeSummary(), info: AttrInfo, headerRight: TabLink("Atributos")), 4),
                (FillCard("Personalidade e traços", TraitsBody(), info: TraitsInfo, headerRight: TabLink("Atributos")), 4)));
        var detail = WatchedDetail;
        var st = Server(v);
        // Uma tela, em tercos alinhados nas duas linhas: o que faz agora, onde esta no progresso e as missoes;
        // embaixo, humor, atributos e os ultimos acontecimentos. O detalhe de cada assunto fica na aba dele.
        return new ScreenPage()
            .Add(Grid(12,
                (FillCard("Agora", NowBody(v), info: NowInfo), 4),
                (FillCard("Progresso e metas", ProgressSummary(v, st, detail), headerRight: TabLink("Progressão"),
                    info: "Nível, objetivo principal e metas de curto prazo\n~Metas de curto prazo (chegar a um nível, descobrir uma zona, concluir uma missão, praticar uma atividade) influenciam as escolhas sem obrigar. As ambições de longo prazo ficam na aba Objetivos; a curva de nível, na aba Progressão."), 4),
                (FillCard("Missões em andamento", QuestSummary(v), info: "Missões do personagem\n~Clique numa missão para abrir a ficha dela. Concluídas e o diário de missões ficam na aba Missões.", headerRight: TabLink("Missões")), 4)), weight: 54)
            .Add(Grid(12,
                (FillCard("Humor", MoodSummary(v, detail), info: MoodInfo + "\n~Frustração, novidade, tédio e memórias na aba Humor e memória.", headerRight: TabLink("Humor e memória")), 4),
                (FillCard("Atributos em destaque", AttributeSummary(), info: "Os atributos mais altos e mais baixos do jogador\n~Todos os atributos, as preferências, a personalidade e os traços ficam na aba Atributos.", headerRight: TabLink("Atributos")), 4),
                (FillCard("Últimos acontecimentos", RecentBody(), info: "Últimos acontecimentos\n~Clique numa linha para abrir a área, a zona ou a missão. Histórico completo na aba Histórico.", headerRight: TabLink("Histórico")), 4)), weight: 46);
    }

    /// <summary>Resumo do progresso: nivel, objetivo principal e metas de curto prazo (detalhes no tooltip).</summary>
    private Control ProgressSummary(CharacterView v, ServerStats st, CharacterDetailView? detail)
    {
        var col = Column(12, LevelBlock(v, st, 88));
        if (ObjectiveSummary(detail) is { } objective)
        {
            col.AddChild(Section("Objetivo principal"));
            col.AddChild(objective);
        }
        col.AddChild(Section("Metas", Caption($"{N(v.GoalsCompleted, "concluída", "concluídas")} · {N(v.GoalsAbandoned, "abandonada", "abandonadas")}")));
        if (v.Goals.Count == 0) col.AddChild(Wrap("Sem meta agora. Nem todo jogador tem uma o tempo todo.", UiTokens.FontSmall, UiTokens.TextSecondary));
        foreach (var g in v.Goals) col.AddChild(GoalItem(v, g, full: false));
        return col;
    }

    /// <summary>Resumo do humor: satisfacao, engajamento e intencao de continuar; o que mais agrada e incomoda.</summary>
    private Control MoodSummary(CharacterView v, CharacterDetailView? detail)
    {
        var col = Column(12, Column(9, MoodBars(v.Mood, all: false).ToArray()));
        var drivers = detail?.Drivers ?? new Dictionary<string, int>();
        var good = drivers.Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).Take(2);
        var bad = drivers.Where(kv => kv.Value < 0).OrderBy(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).Take(2);
        var list = good.Concat(bad).ToList();
        col.AddChild(Section("O que mais pesa"));
        if (list.Count == 0) col.AddChild(Wrap("Nada se destaca ainda.", UiTokens.FontSmall, UiTokens.TextSecondary));
        var lines = Column(6);
        foreach (var kv in list) lines.AddChild(DriverLine(kv.Key, kv.Value));
        col.AddChild(lines);
        return col;
    }

    /// <summary>Resumo dos atributos: os quatro mais altos e os tres mais baixos, no mesmo formato da aba Atributos.</summary>
    private Control AttributeSummary()
    {
        var t = _person.Traits;
        var all = AttrGroups.SelectMany(g => g.Keys).Select(k => (Key: k, Value: t.Get(k))).ToList();
        var high = all.OrderByDescending(a => a.Value).ThenBy(a => a.Key, StringComparer.Ordinal).Take(4).ToList();
        var low = all.Where(a => !high.Contains(a)).OrderBy(a => a.Value).ThenBy(a => a.Key, StringComparer.Ordinal).Take(3).ToList();
        var col = Column(10, Section("Mais altos"));
        foreach (var a in high) col.AddChild(AttributeCell(a.Key));
        col.AddChild(Section("Mais baixos"));
        foreach (var a in low) col.AddChild(AttributeCell(a.Key));
        return col;
    }

    // ---------------- Atributos ----------------

    private Control BuildAttributes() => new ScreenPage()
        .Add(Grid(12,
            (FillCard("Atributos do jogador", AttributeGrid(), info: AttrInfo), 8),
            (FillCard("Personalidade e traços", TraitsBody(), info: TraitsInfo), 4)), weight: 52)
        .Add(Grid(12,
            (FillCard("Preferências", PreferencesBody(), info: "Preferências · o que a pessoa gosta de fazer, de 1 a 20\n~Dão o peso principal de gosto nas escolhas, sem obrigar: viagem, risco, tédio, metas, lotação e tempo podem fazer outra opção vencer. Os atributos de especialidade só reforçam um pouco.\n~Embaixo, o que mais pesa nas escolhas (gostos fortes e competitividade).\n~Crafting ainda não tem efeito (a fabricação não existe no jogo)."), 8),
            (FillCard("Mudanças desde o cadastro", ChangesBody(), info: "O que mudou na pessoa desde que entrou no jogo\n~Atributos mudam devagar com o que a pessoa vive (a seta na ficha mostra a direção; aqui, o valor no cadastro e o de agora). Traços são ganhos ou perdidos por evidência repetida (P4d). A personalidade muda se outro atributo passar a ser o mais extremo."), 4)), weight: 48);

    /// <summary>As sete preferencias em duas colunas, da mais forte para a mais fraca, e o gosto marcante.</summary>
    private Control PreferencesBody()
    {
        var pr = _person.Preferences;
        var grid = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        grid.AddThemeConstantOverride("h_separation", 24);
        grid.AddThemeConstantOverride("v_separation", 12);
        foreach (var k in PersonPreferences.Keys.OrderByDescending(pr.Get).ThenBy(k => k, StringComparer.Ordinal))
            grid.AddChild(PreferenceCell(k, pr.Get(k)));
        return Column(6, grid, Highlights());
    }

    private static Control PreferenceCell(string key, int value)
    {
        var e = PreferenceInfo.ByKey[key];
        int v20 = UiTokens.To20(value);
        var bar = v20 >= 14 ? AttrTier(v20) : v20 >= 9 ? new Color("#8B94A1") : new Color("#4E5661");
        var cell = new HoverTip($"{e.Name}|{v20} / 20|#{(v20 >= 14 ? bar : UiTokens.TextPrimary).ToHtml(false)}\n^Preferência · {PreferenceInfo.Level(value)}\n{e.Influences}")
        {
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
        };
        var c = Column(5);
        var r = new HBoxContainer();
        r.AddThemeConstantOverride("separation", 8);
        r.AddChild(new Icon(e.Icon, 14, v20 >= 14 ? UiTokens.AccentSoft : UiTokens.TextSecondary) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
        var name = ThemeFactory.Text(e.Name, UiTokens.FontBody, UiTokens.TextLabel, UiFonts.Medium);
        name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        name.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        r.AddChild(name);
        r.AddChild(ThemeFactory.Text(PreferenceInfo.Level(value), UiTokens.FontCaption, UiTokens.TextSecondary));
        r.AddChild(ThemeFactory.Text(v20.ToString(), UiTokens.FontBody, v20 >= 14 ? bar : UiTokens.TextPrimary, UiFonts.Numbers));
        c.AddChild(r);
        c.AddChild(new PotentialBar(v20 / 20f, 0, bar) { CustomMinimumSize = new Vector2(0, 3) });
        cell.AddChild(c);
        return cell;
    }

    /// <summary>Motivo de um traco ganho ou perdido (P4d), em portugues.</summary>
    private static string QuirkReasonPt(string reason) => reason switch
    {
        "goals_after_setbacks" => "seguiu com as metas depois de reveses",
        "gave_up_after_setbacks" => "desistiu de metas depois de reveses",
        "repeated_bad_experience_same_place" => "experiências ruins repetidas no mesmo lugar",
        "second_chance_succeeded" => "uma segunda chance no lugar deu certo",
        _ => reason,
    };

    /// <summary>Mudancas desde o cadastro: atributos (antes e agora), tracos ganhos e perdidos, personalidade.</summary>
    private Control ChangesBody()
    {
        var c = _ctx.Content;
        var t = _person.Traits;
        var b0 = _person.BaseTraits;
        string Date(long day) => PtBr.ShortDate(_ctx.Calendar.ToDateTime(_ctx.Calendar.StartOfDay(day)));
        var col = Column(10);

        var changed = AttrGroups.SelectMany(g => g.Keys)
            .Select(k => (Key: k, Now: UiTokens.To20(t.Get(k)), Was: UiTokens.To20(b0.Get(k))))
            .Where(x => x.Now != x.Was)
            .OrderByDescending(x => Math.Abs(x.Now - x.Was)).ThenBy(x => x.Key, StringComparer.Ordinal).ToList();
        col.AddChild(Section("Atributos", Caption($"{changed.Count} de {AttrGroups.Sum(g => g.Keys.Length)}")));
        if (changed.Count == 0) col.AddChild(Wrap("Nenhum atributo mudou na escala de 1 a 20 desde o cadastro.", UiTokens.FontSmall, UiTokens.TextSecondary));
        foreach (var (key, now, was) in changed)
        {
            var info = AttributeInfo.ByKey[key];
            bool up = now > was;
            var color = up ? UiTokens.Success : UiTokens.Danger;
            var tip = new HoverTip($"{info.Name}|{(up ? "+" : "−")}{Math.Abs(now - was)}|#{color.ToHtml(false)}\n^{AttributeInfo.GroupOf(key)}\nNo cadastro|{was} / 20\nAgora|{now} / 20\n#Como muda\n{AttributeInfo.Bullets(info.Changes)}");
            var r = Row(10);
            r.AddChild(new Icon(up ? "trending-up" : "trending-down", 14, color) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
            var name = ThemeFactory.Text(info.Name, UiTokens.FontBody, UiTokens.TextLabel, UiFonts.Medium);
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            name.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            r.AddChild(name);
            r.AddChild(ThemeFactory.Text(AttrGroups.First(g => g.Keys.Contains(key)).Title, UiTokens.FontCaption, UiTokens.TextSecondary));
            var values = ThemeFactory.Text($"{was} → {now}", UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Numbers);
            values.CustomMinimumSize = new Vector2(64, 0);
            values.HorizontalAlignment = HorizontalAlignment.Right;
            r.AddChild(values);
            var delta = ThemeFactory.Text($"{(up ? "+" : "−")}{Math.Abs(now - was)}", UiTokens.FontSmall, color, UiFonts.Numbers);
            delta.CustomMinimumSize = new Vector2(28, 0);
            delta.HorizontalAlignment = HorizontalAlignment.Right;
            r.AddChild(delta);
            tip.AddChild(r);
            col.AddChild(tip);
        }

        var quirks = View?.QuirkHistory ?? Array.Empty<QuirkChange>();
        col.AddChild(Section("Traços", Caption(quirks.Count.ToString())));
        if (quirks.Count == 0) col.AddChild(Wrap("Nenhum traço ganho ou perdido desde o cadastro.", UiTokens.FontSmall, UiTokens.TextSecondary));
        foreach (var h in quirks.Reverse())
        {
            var def = c.Quirks.TryGetValue(h.Id, out var d) ? d : null;
            bool gained = h.Change == "acquired";
            var color = def != null ? TraitInfo.PolarityColor(def) : UiTokens.TextSecondary;
            string what = $"{(gained ? "Ganhou" : "Perdeu")} {def?.Name ?? h.Id}";
            string why = QuirkReasonPt(h.Reason);
            var r = Row(10);
            r.AddChild(new TraitBadge(TraitInfo.QuirkIcon(def), color, 24, pending: !gained));
            var body = Column(2);
            body.AddChild(ThemeFactory.Text(what, UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Medium));
            var wl = ThemeFactory.Text(why, UiTokens.FontCaption, UiTokens.TextSecondary);
            wl.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            body.AddChild(wl);
            r.AddChild(body);
            r.AddChild(Caption(Date(h.Day)));
            var tip = new TipPanel
            {
                TooltipText = (def != null ? TraitInfo.Tooltip(c, def) + "\n" : $"{what}\n") + $"Quando|{Date(h.Day)}\nMotivo|{why}\nEvidências|{h.For} a favor · {h.Against} contra",
                MouseFilter = MouseFilterEnum.Stop,
            };
            tip.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
            tip.AddChild(r);
            col.AddChild(tip);
        }

        var personality = View?.PersonalityHistory ?? Array.Empty<PersonalityChange>();
        col.AddChild(Section("Personalidade"));
        if (personality.Count == 0)
            col.AddChild(Wrap($"A mesma desde o cadastro: {MainPersonality().Label}.", UiTokens.FontSmall, UiTokens.TextSecondary));
        foreach (var p in personality.Reverse())
        {
            var from = PersonalityOf(c, p.From);
            var to = PersonalityOf(c, p.To);
            var r = Row(10);
            r.AddChild(new TraitBadge(to.Icon, UiTokens.Accent, 24, round: true));
            var body = Column(2);
            body.AddChild(ThemeFactory.Text($"Passou de {from.Label} para {to.Label}", UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Medium));
            body.AddChild(ThemeFactory.Text($"{AttributeInfo.Name(p.Attribute)} {UiTokens.To20(p.Value)} / 20", UiTokens.FontCaption, UiTokens.TextSecondary));
            r.AddChild(body);
            r.AddChild(Caption(Date(p.Day)));
            col.AddChild(r);
        }
        return col;
    }

    // ---------------- Progressao ----------------

    private Control BuildProgress()
    {
        if (View is not { } v) return Wrap("Ainda sem personagem.");
        var d = WatchedDetail;
        var st = Server(v);
        // Uma tela: o nivel e a curva de horas por nivel; embaixo, para onde vai o tempo, de onde vem o XP e onde joga.
        return new ScreenPage()
            .Add(Grid(12,
                (FillCard("Nível e experiência", LevelBody(v, d, st), info: "Progresso no nível atual\n~Faixas de nível mais altas rendem menos XP (taxa da faixa). A previsão do próximo nível usa o XP por hora deste personagem."), 4),
                (FillCard("Evolução", EvolutionBody(d), info: "Horas jogadas ao atingir cada nível\n~Curva real deste personagem (tempo conectado acumulado)."), 8)), weight: 55)
            .Add(Grid(12,
                (FillCard("Uso do tempo", TimeUseBody(d), info: "Divisão do tempo conectado\n~Por tipo de atividade desde o cadastro; o resto é deslocamento."), 4),
                (FillCard("XP por fonte", XpSourceBody(v), info: "De onde vem a experiência\n~Atividades no mundo contra entregas de missões."), 4),
                (FillCard("Zonas frequentadas", ZonesBody(), info: "Onde escolhe jogar\n~Escolhas de atividade por zona registradas desde que o jogo foi aberto. Clique para abrir a zona."), 4)), weight: 45);
    }

    private Control LevelBody(CharacterView v, CharacterDetailView? d, ServerStats st)
    {
        long played = d?.PlayedTicks ?? v.PlayedTicks;
        int ups = d?.LevelTicks.Count ?? 0;
        long atLevel = ups > 0 ? played - d!.LevelTicks[^1] : played;
        var col = Column(10, LevelBlock(v, st, 104), Divider());
        col.AddChild(KeyValue("XP por hora", st.MyXpH < 0 ? "–" : PtBr.Int((long)st.MyXpH)));
        col.AddChild(KeyValue($"No nível {v.Level}", $"{PtBr.Decimal(Hours(atLevel))} h"));
        col.AddChild(KeyValue("Média por nível", ups == 0 ? "–" : $"{PtBr.Decimal(Hours(d!.LevelTicks[^1]) / ups)} h"));
        col.AddChild(KeyValue("Zonas conhecidas", $"{v.KnownRegions} de {_ctx.Content.Regions.Count}"));
        return col;
    }

    // ---------------- Estatisticas ----------------

    private Control BuildStats()
    {
        if (View is not { } v) return Wrap("Ainda sem personagem.");
        var st = Server(v);
        return new ScreenPage()
            .Add(Grid(12,
                (FillCard("Comparação com o servidor", CompareBody(v, st), info: "Este personagem contra a mediana do servidor\n~A barra mostra a diferença percentual; verde acima, vermelho abaixo."), 8),
                (FillCard("Classificação", RankBody(st), info: "Posição no servidor\n~Ordem por nível e XP. XP por hora considera só quem jogou ao menos 1 hora."), 4)), weight: 50)
            .Add(Grid(12,
                (FillCard("Níveis no servidor", ServerLevelsBody(v), info: "Distribuição de níveis dos personagens\n~Dourado: o nível deste personagem."), 12)), weight: 50);
    }

    // ---------------- Missoes ----------------

    private static bool IsQuestEvent(SimEvent e) =>
        e.Type is "quest_accepted" or "quest_step_completed" or "quest_completed" or "quest_expired" or "quest_dropped" or "board_visit_empty";

    private Control BuildQuests()
    {
        if (View is not { } v) return Wrap("Ainda sem personagem.");
        int max = _ctx.Content.QuestSettings.MaxActivePerCharacter;
        return new ScreenPage()
            .Add(Grid(12,
                (FillCard("Em andamento", ActiveQuestsBody(v), info: "Missões aceitas e a etapa atual\n~Clique para abrir a missão. A barra mostra o tempo feito na etapa; pronta, falta entregar no quadro.", headerRight: Caption($"{v.ActiveQuests.Count} de {max}")), 8),
                (FillCard("Resumo", QuestStatsBody(v, max), info: "Números das missões do personagem\n~Entregas contam cada vez que uma missão repetível (diária ou secundária) foi entregue."), 4)), weight: 52)
            .Add(Grid(12,
                (FillCard("Diário de missões", RecentBody(IsQuestEvent, 40, "Nenhum acontecimento de missão desde que o jogo foi aberto."), info: "Aceites, etapas, entregas, diárias vencidas, missões largadas e idas ao quadro sem resultado\n~Com o motivo de cada aceite e de cada ida sem resultado. Clique para abrir a missão."), 8),
                (FillCard("Concluídas", CompletedQuestsBody(v), info: "Missões já entregues\n~Principais primeiro. ×N: entregue N vezes (repetíveis). Clique para abrir a missão.", headerRight: Caption(N(v.CompletedQuests.Count, "missão", "missões"))), 4)), weight: 48);
    }

    private Control ActiveQuestsBody(CharacterView v)
    {
        var col = Column(6);
        if (v.ActiveQuests.Count == 0) col.AddChild(Wrap("Nenhuma missão em andamento.", UiTokens.FontSmall, UiTokens.TextSecondary));
        foreach (var p in v.ActiveQuests)
        {
            if (!_ctx.Content.Quests.TryGetValue(p.QuestId, out var q)) continue;
            bool ready = p.StepIndex >= q.Steps.Count;
            int steps = q.Steps.Count + 1, step = Math.Min(p.StepIndex + 1, steps);
            var color = QuestModal.CategoryColor(q.Category);
            string qid = q.Id;
            var b = HoverRow(() => _ctx.OpenQuest(qid),
                $"{q.Name}|{QuestModal.CategoryName(q.Category)}\nEtapa|{step} de {steps}\nAceita em|{PtBr.ShortDate(_ctx.Calendar.ToDateTime(p.AcceptedTick))} {_ctx.Clock(p.AcceptedTick)}\n~Clique para abrir a missão.");
            b.CustomMinimumSize = new Vector2(0, 70);
            var c = Column(5);
            c.SetAnchorsPreset(LayoutPreset.FullRect);
            c.OffsetLeft = 8; c.OffsetRight = -8; c.OffsetTop = 7;
            var top = Row(8);
            top.AddChild(new Icon("scroll-text", 16, color) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
            var nm = ThemeFactory.Text(q.Name, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Medium);
            nm.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            nm.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            top.AddChild(nm);
            top.AddChild(ThemeFactory.Text(QuestModal.CategoryName(q.Category), UiTokens.FontCaption, color, UiFonts.Medium));
            top.AddChild(new Icon("chevron-right", 14, UiTokens.TextDisabled) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
            c.AddChild(top);
            c.AddChild(Bar($"Etapa {step}/{steps}", ready ? 1 : p.StepMinutes, ready ? 1 : q.Steps[p.StepIndex].Minutes, ready ? UiTokens.Success : color,
                ready ? "pronta" : $"{p.StepMinutes}/{q.Steps[p.StepIndex].Minutes} min", 90));
            var stepText = ThemeFactory.Text(ready ? "Pronta para entregar no quadro" : q.Steps[p.StepIndex].Text, UiTokens.FontSmall, ready ? UiTokens.Success : UiTokens.TextSecondary);
            stepText.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            c.AddChild(stepText);
            b.AddChild(IgnoreMouse(c));
            col.AddChild(b);
        }
        return col;
    }

    private Control QuestStatsBody(CharacterView v, int max)
    {
        var c = _ctx.Content;
        long xpTotal = v.XpFromQuests + v.XpFromActivities;
        int Delivered(string cat) => v.CompletedQuests.Where(x => c.Quests.TryGetValue(x.QuestId, out var q) && q.Category == cat).Sum(x => x.Times);
        var tiles = new GridContainer { Columns = 2, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        tiles.AddThemeConstantOverride("h_separation", 8);
        tiles.AddThemeConstantOverride("v_separation", 8);
        tiles.AddChild(Stat(v.ActiveQuests.Count.ToString(), $"ativas (máx. {max})", UiTokens.Info, fill: true));
        tiles.AddChild(Stat(PtBr.Int(v.CompletedQuests.Sum(x => x.Times)), "entregas", UiTokens.Success, fill: true));
        tiles.AddChild(Stat(PtBr.Int(v.CompletedQuests.Count(x => c.Quests.TryGetValue(x.QuestId, out var q) && q.Category == QuestCategories.Main)), "principais concluídas", UiTokens.Accent, fill: true));
        tiles.AddChild(Stat(xpTotal == 0 ? "–" : $"{v.XpFromQuests * 100 / xpTotal}%", "do XP veio de missões", UiTokens.AccentSoft, fill: true));
        var col = Column(10, tiles, Section("Entregas por categoria"));
        foreach (var cat in QuestCategories.All) col.AddChild(KeyValue(QuestModal.CategoryName(cat), PtBr.Int(Delivered(cat)), QuestModal.CategoryColor(cat)));
        col.AddChild(KeyValue("XP de missões", PtBr.Int(v.XpFromQuests)));
        return col;
    }

    private Control CompletedQuestsBody(CharacterView v)
    {
        var c = _ctx.Content;
        var col = Column(2);
        var list = v.CompletedQuests.Where(x => c.Quests.ContainsKey(x.QuestId)).Select(x => (x.QuestId, x.Times, Def: c.Quests[x.QuestId]))
            .OrderBy(x => Array.IndexOf(QuestCategories.All, x.Def.Category)).ThenBy(x => x.Def.Name, StringComparer.Ordinal).ToList();
        if (list.Count == 0) col.AddChild(Wrap("Nenhuma missão concluída ainda.", UiTokens.FontSmall, UiTokens.TextSecondary));
        foreach (var (id, times, q) in list)
        {
            var color = QuestModal.CategoryColor(q.Category);
            string qid = id;
            var b = HoverRow(() => _ctx.OpenQuest(qid), $"{q.Name}|{QuestModal.CategoryName(q.Category)}\nEntregas|{times}\n~Clique para abrir a missão.");
            b.CustomMinimumSize = new Vector2(0, 30);
            var r = Row(10);
            r.SetAnchorsPreset(LayoutPreset.FullRect);
            r.OffsetLeft = 8; r.OffsetRight = -8;
            r.AddChild(new Icon("scroll-text", 15, color) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
            var nm = ThemeFactory.Text(q.Name, UiTokens.FontBody, UiTokens.TextLabel);
            nm.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            nm.VerticalAlignment = VerticalAlignment.Center;
            nm.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            r.AddChild(nm);
            var cat = ThemeFactory.Text(QuestModal.CategoryName(q.Category), UiTokens.FontCaption, UiTokens.TextSecondary);
            cat.VerticalAlignment = VerticalAlignment.Center;
            r.AddChild(cat);
            var n = ThemeFactory.Text(times > 1 ? $"×{times}" : "", UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Numbers);
            n.CustomMinimumSize = new Vector2(34, 0);
            n.HorizontalAlignment = HorizontalAlignment.Right;
            n.VerticalAlignment = VerticalAlignment.Center;
            r.AddChild(n);
            b.AddChild(IgnoreMouse(r));
            col.AddChild(b);
        }
        return col;
    }

    // ---------------- Humor e memoria ----------------

    private Control BuildMood()
    {
        if (View is not { } v) return Wrap("Ainda sem personagem.");
        var d = WatchedDetail;
        // Uma tela: como esta, o que pesa e o que cansa; embaixo, o que ficou na memoria e os lugares que marcaram.
        return new ScreenPage()
            .Add(Grid(12,
                (FillCard("Humor", MoodBody(v), info: MoodInfo), 4),
                (FillCard("O que pesa no humor", DriversBody(d), info: "O que tem agradado e incomodado\n~Soma do que cada sessão trouxe, esmaecendo com os dias. A porcentagem é a parcela de cada fator no total que pesa no humor agora."), 4),
                (FillCard("Tédio por atividade", BoredomBody(d), info: "Tédio por tipo de atividade (D-037)\n~Cresce com o tempo fazendo o mesmo tipo, mais rápido para quem gosta de variedade; a paciência alivia. Fazer outra coisa alivia, e só uma parte passa para a próxima sessão. Pesa contra o tipo nas escolhas e na nota da sessão."), 4)), weight: 52)
            .Add(Grid(12,
                (FillCard("Memórias marcantes", MemoriesBody(d), info: "O que ficou na memória (D-038)\n~Conquistas duram mais que frustrações. A saudade dos bons momentos pode trazer de volta quem desistiu."), 8),
                (FillCard("Lugares que marcaram", PlacesBody(d), info: "Impressão de cada lugar (D-038)\n~As experiências em cada atividade deixam uma impressão: boa atrai, ruim afasta nas próximas escolhas. Aqui, as mais fortes."), 4)), weight: 48);
    }

    /// <summary>Todos os fatores do humor, do que mais agrada ao que mais incomoda, com a parcela de cada um.</summary>
    private Control DriversBody(CharacterDetailView? d)
    {
        var col = Column(8);
        if (d == null) { col.AddChild(Wrap("Carregando…")); return col; }
        var list = d.Drivers.Where(kv => kv.Value != 0).OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).ToList();
        if (list.Count == 0) { col.AddChild(Wrap("Nada pesa ainda: aparece depois das primeiras sessões avaliadas.", UiTokens.FontSmall, UiTokens.TextSecondary)); return col; }
        float max = list.Max(kv => Math.Abs(kv.Value)), total = list.Sum(kv => Math.Abs(kv.Value));
        foreach (var (key, value) in list)
        {
            bool good = value > 0;
            var color = good ? UiTokens.Success : UiTokens.Danger;
            int share = (int)Math.Round(Math.Abs(value) * 100 / total);
            var row = new TipPanel { TooltipText = $"{DriverPt(key)}|{(good ? "agrada" : "incomoda")}\nParcela|{share}% do que pesa no humor\n~{DriverHint(key)}", MouseFilter = MouseFilterEnum.Stop };
            row.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
            var r = new HBoxContainer();
            r.AddThemeConstantOverride("separation", 8);
            var l = ThemeFactory.Text(DriverPt(key), UiTokens.FontSmall, UiTokens.TextLabel, UiFonts.Medium);
            l.CustomMinimumSize = new Vector2(150, 0);
            l.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            r.AddChild(l);
            r.AddChild(new DivergingTrack(value / max, color) { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 14) });
            var n = ThemeFactory.Text($"{(good ? "+" : "−")}{share}%", UiTokens.FontSmall, color, UiFonts.Numbers);
            n.CustomMinimumSize = new Vector2(40, 0);
            n.HorizontalAlignment = HorizontalAlignment.Right;
            r.AddChild(n);
            row.AddChild(r);
            col.AddChild(row);
        }
        return col;
    }

    /// <summary>Tedio acumulado por tipo de atividade, do maior para o menor.</summary>
    private Control BoredomBody(CharacterDetailView? d)
    {
        var col = Column(10);
        if (d == null) { col.AddChild(Wrap("Carregando…")); return col; }
        var list = d.Boredom.Where(kv => kv.Value > 0).OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).ToList();
        if (list.Count == 0) col.AddChild(Wrap("Sem tédio acumulado agora: nenhum tipo de atividade cansou.", UiTokens.FontSmall, UiTokens.TextSecondary));
        foreach (var (type, value) in list)
        {
            var color = value >= 600 ? UiTokens.Danger : value >= 300 ? UiTokens.Warning : UiTokens.Info;
            var t = new TipPanel { TooltipText = $"{PtBr.ActivityType(type)}|{value / 10}%\n~Tédio acumulado neste tipo de atividade. Acima de 60%, pesa bastante contra ele nas escolhas.", MouseFilter = MouseFilterEnum.Stop };
            t.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
            var r = Row(10);
            r.AddChild(new Icon(TypeIcon(type), 16, ActivityPalette.Color(type)) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
            var info = Column(4);
            var top = new HBoxContainer();
            var nm = ThemeFactory.Text(PtBr.ActivityType(type), UiTokens.FontSmall, UiTokens.TextLabel, UiFonts.Medium);
            nm.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            top.AddChild(nm);
            top.AddChild(ThemeFactory.Text($"{value / 10}%", UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Numbers));
            info.AddChild(top);
            info.AddChild(new ThinBar(value / 1000f, color) { CustomMinimumSize = new Vector2(0, 4) });
            r.AddChild(info);
            t.AddChild(r);
            col.AddChild(t);
        }
        return col;
    }

    // ---------------- Historico ----------------

    private Control BuildHistory()
    {
        var events = _ctx.PersonHistory(_person.Id);
        var col = Column(0);
        var head = new HBoxContainer();
        head.AddChild(ColHead("Quando", 110));
        head.AddChild(ColHead("Acontecimento", 0));
        col.AddChild(head);
        if (events.Count == 0) col.AddChild(Wrap("Nada registrado desde que o jogo foi aberto."));
        int i = 0;
        foreach (var e in events.Take(150))
        {
            var bg = new PanelContainer();
            bg.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = i++ % 2 == 1 ? UiTokens.RowAlt : new Color(0, 0, 0, 0), ContentMarginLeft = 4 });
            bg.AddChild(EventRow(e, compact: false));
            col.AddChild(bg);
        }
        return new ScreenPage().Add(Grid(12,
            (FillCard("Acontecimentos", col, info: "Tudo o que a pessoa fez e viveu desde que o jogo foi aberto\n~Mais recente primeiro (até 150). Na Visão geral ficam só os últimos; as missões também têm um diário próprio na aba Missões.",
                headerRight: Caption(N(Math.Min(150, events.Count), "registro", "registros"))), 12)));
    }
}

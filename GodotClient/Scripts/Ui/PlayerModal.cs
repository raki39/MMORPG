using System.Globalization;
using Godot;
using Simulation.Core.Behavior;
using Simulation.Core.Content;
using Simulation.Core.Events;
using Simulation.Core.Population;
using GodotClient.Sim;

namespace GodotClient.Ui;

/// <summary>
/// Dossie do jogador simulado (estilo FM): faixa de dados-chave, atributos 1–20, o que faz
/// agora, por que decidiu, progresso, rotina, financas e historico. Dados reais do Core.
/// </summary>
public partial class PlayerModal : ModalFrame
{
    private readonly ClientContext _ctx;
    private readonly PersonInfo _person;
    private Button _favorite = null!;

    public PlayerModal(ClientContext ctx, int personId) : base(UiTokens.ProfileColor(ctx.Persons[personId - 1].ProfileId))
    {
        _ctx = ctx;
        _person = ctx.Persons[personId - 1];
        ctx.WatchPerson(personId);
        Closed += () => ctx.WatchPerson(0);
        BuildHeader();
        Breadcrumb = $"Jogadores › {View?.Name ?? _person.Name}";
        AddTab("Visão geral", BuildProfile, live: true);
        AddTab("Estatísticas", BuildStats, live: true);
        AddTab("Missões", BuildQuests, live: true);
        AddTab("Histórico", BuildHistory, live: true);
        AddTab("Rotina", BuildRoutine);
        AddTab("Combate", () => FutureTab("Combate", "M3", "Abates, mortes, saque, rivais e desempenho em PvE e PvP aparecem aqui quando o combate existir na simulação. Nada é inventado antes disso."));
        AddTab("Inventário", () => FutureTab("Inventário", "M5", "Itens, equipamento e carga do personagem aparecem aqui quando existirem inventário e itens na simulação. As recompensas de missões já registram os itens previstos."));
        AddTab("Banco", () => FutureTab("Banco", "M5", "Ouro guardado, depósitos e movimentações aparecem aqui com a economia. Hoje o ouro de missões está definido, mas ainda não é aplicado."));
    }

    private CharacterView? View => _ctx.Snapshot().ByPerson is { } a && _person.Id < a.Length ? a[_person.Id] : null;
    private string ProfileName => _ctx.Content.Profiles.TryGetValue(_person.ProfileId, out var p) ? p.Name : _person.ProfileId;
    private static double Hours(long ticks) => ticks / (double)Simulation.Core.Time.SimTime.TicksPerHour;

    /// <summary>Icone da classe (Lucide), pelo id e, na falta, pelo papel.</summary>
    public static string ClassIcon(string? classId, string? role) => classId switch
    {
        "guerreiro" => "swords",
        "mago" => "wand-sparkles",
        "sacerdote" => "cross",
        _ => role == ClassRoles.Tank ? "shield" : role == ClassRoles.Support ? "heart-pulse" : "sword",
    };

    // ---------------- Cabecalho ----------------

    private void BuildHeader()
    {
        var v = View;
        var color = UiTokens.ProfileColor(_person.ProfileId);
        ClassDefinition? cls = v != null && _ctx.Content.Classes.TryGetValue(v.ClassId, out var c) ? c : null;
        string icon = ClassIcon(v?.ClassId, cls?.Role);
        Art.AddChild(new PortraitBadge(_person.AppearanceSeed, color, v?.Name ?? _person.Name)
        {
            CustomMinimumSize = new Vector2(104, 104), SizeFlagsVertical = SizeFlags.ShrinkBegin, ClassIcon = v != null ? icon : "",
        });

        var nameRow = new HBoxContainer();
        nameRow.AddThemeConstantOverride("separation", 10);
        nameRow.AddChild(ThemeFactory.Text(v?.Name ?? _person.Name, 28, UiTokens.TextPrimary, UiFonts.SemiBold));
        if (_ctx.Favorites.Contains(_person.Id)) nameRow.AddChild(new Icon("star", 18, UiTokens.Accent));
        var (prev, next) = Neighbors();
        var arrows = new HBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        arrows.AddThemeConstantOverride("separation", 2);
        arrows.AddChild(IconButton("chevron-left", prev > 0 ? $"Jogador anterior\n~{NameOf(prev)} (ordem por nível)" : "Primeiro da lista", () => _ctx.OpenPerson(prev), prev > 0));
        arrows.AddChild(IconButton("chevron-right", next > 0 ? $"Próximo jogador\n~{NameOf(next)} (ordem por nível)" : "Último da lista", () => _ctx.OpenPerson(next), next > 0));
        nameRow.AddChild(arrows);
        Header.AddChild(nameRow);
        var sub = new HBoxContainer();
        sub.AddThemeConstantOverride("separation", 7);
        if (v != null)
        {
            sub.AddChild(new Icon(icon, 16, RoleColor(cls?.Role)));
            sub.AddChild(ThemeFactory.Text($"{cls?.Name} · {RolePt(cls?.Role)}", UiTokens.FontBody, UiTokens.TextLabel, UiFonts.Medium));
            Header.AddChild(sub);
        }
        // Nome da pessoa por tras do personagem, numa linha propria.
        Header.AddChild(ThemeFactory.Text(_person.Name, UiTokens.FontBody, UiTokens.TextSecondary));

        if (v != null)
        {
            bool online = v.Status != CharacterStatus.Offline;
            var prog = _ctx.Content.Progression;
            long need = ProgressionRules.XpToNextLevel(prog, v.Level);
            string stage = (ProgressionRules.StageRate(prog, v.Level) / 1000.0).ToString("0.##", CultureInfo.GetCultureInfo("pt-BR"));
            Fact("Nível", v.Level.ToString(), UiTokens.Accent, v.Level >= prog.LevelCap
                ? $"Nível {v.Level}\n~Nível máximo atual ({prog.LevelCap})."
                : $"Nível {v.Level}\nXP|{PtBr.Int(v.Experience)} / {PtBr.Int(need)}\nPara o próximo|{v.Experience * 100 / Math.Max(1, need)}%\nTaxa da faixa|×{stage}\n~Faixas de nível mais altas rendem menos XP.", "award");
            Fact("Status", online ? "Conectado" : "Desconectado", online ? UiTokens.Success : UiTokens.TextSecondary, "", "activity");
            Fact("Zona", _ctx.ZoneName(v.RegionId), UiTokens.TextSecondary, "", "map-pin");
            Fact("Conhece", $"{v.KnownRegions}/{_ctx.Content.Regions.Count}", UiTokens.TextSecondary, "Zonas conhecidas\n~Só zonas conhecidas entram nas decisões. Viajar e explorar revelam mais.", "compass");
            Fact("Perfil", ProfileName, color, $"Perfil {ProfileName}\n~{ProfileHint(_person.ProfileId)} Tendência de origem; a pessoa não é um clone do perfil.", "user");
            var (picon, plabel, ptext) = MainPersonality();
            Fact("Personalidade", plabel, UiTokens.AccentSoft, $"{plabel}\n~{ptext}", picon);
            int talent = _person.Traits.Talent;
            Fact("Talento", AttributeInfo.TalentTier(talent), UiTokens.Accent,
                $"Talento: {AttributeInfo.TalentTier(talent)}\nValor|{UiTokens.To20(talent)} / 20\n~{AttributeInfo.TalentHint(talent)}\n~Talento é do jogador (a pessoa): define o potencial e a velocidade com que Mecânica, Conhecimento e Coordenação evoluem. Não muda e não é experiência.", "sparkles");
            double hours = v.PlayedTicks / (double)Simulation.Core.Time.SimTime.TicksPerHour;
            string exp = AttributeInfo.ExperienceTier(hours);
            Fact("Experiência", exp, UiTokens.TextSecondary,
                $"Experiência: {exp}\nHoras jogadas|{PtBr.Decimal(hours)} h\nConhecimento|{UiTokens.To20(_person.Traits.GameKnowledge)} / 20\n~Vem da prática neste jogo: Novato (< 10 h), Iniciante (< 50 h), Experiente (< 200 h), Veterano. O Conhecimento cresce com ela; quem já jogava o gênero chega com mais conhecimento.", "clock");

            var e = Engagement();
            var gauge = new SemiGauge
            {
                CustomMinimumSize = new Vector2(168, 84), Value = Mathf.Clamp(e.Pct / 1.2f, 0, 1), Center = $"{e.Pct * 100:0}%",
                Caption = $"Engajamento · {e.Days} dia{(e.Days == 1 ? "" : "s")}", Verdict = EngagementVerdict(e.Pct), Color = RatingColor(EngagementScore(e.Pct)),
                TooltipText = $"Engajamento\nJogou|{e.Played} min\nPrevisto pela rotina|{e.Expected} min\nDias com sessão|{e.DaysPlayed} de {e.Days}\n~Minutos jogados ÷ minutos previstos pela rotina (sessão típica × dias por semana) nos últimos {e.Days} dias, contando hoje até agora.",
            };
            Highlight.AddChild(gauge);
        }
        else Fact("Status", "Interessado (sem conta)", UiTokens.Warning, "", "user");

        bool on = v != null && v.Status != CharacterStatus.Offline;
        Actions.AddChild(Action("Seguir no mapa", "Seguir no mapa\n~Volta ao mapa acompanhando este jogador.", () => { _ctx.FocusPerson(_person.Id, true); Close(); }, primary: true, enabled: on));
        Actions.AddChild(Action("Localizar", "Localizar\n~Volta ao mapa centralizado no jogador.", () => { _ctx.FocusPerson(_person.Id, false); Close(); }, enabled: on));
        _favorite = Action(FavText(), "Favoritar\n~Acompanha este jogador (favoritos desta sessão).", ToggleFavorite);
        Actions.AddChild(_favorite);
        if (v != null && v.Status == CharacterStatus.Doing && v.ActivityId.Length > 0
            && _ctx.Content.Regions[v.TargetRegionId].AreaOfActivity(v.ActivityId) is { } area)
        {
            string z = v.TargetRegionId, a = area.Id;
            Actions.AddChild(Action("Ver área", $"{area.Name}\n~Abre a página da área onde está.", () => _ctx.OpenArea(z, a)));
        }
        else if (v != null) Actions.AddChild(Action("Ver zona", "Ver zona\n~Abre a página da zona atual.", () => _ctx.OpenZone(v.RegionId)));
    }

    /// <summary>Botao so com icone (setas, acoes compactas).</summary>
    private static Button IconButton(string icon, string tip, Action press, bool enabled = true)
    {
        var b = new TipButton { TooltipText = tip, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(28, 28), Disabled = !enabled };
        b.AddThemeStyleboxOverride("normal", ThemeFactory.Box(UiTokens.SurfaceRaised, 4, border: UiTokens.Border, radius: 3));
        b.AddThemeStyleboxOverride("hover", ThemeFactory.Box(UiTokens.SurfaceHover, 4, border: UiTokens.BorderStrong, radius: 3));
        b.AddThemeStyleboxOverride("pressed", ThemeFactory.Box(UiTokens.SurfaceHover, 4, border: UiTokens.Accent, radius: 3));
        b.AddThemeStyleboxOverride("disabled", ThemeFactory.Box(new Color(0, 0, 0, 0), 4, border: new Color(UiTokens.Border, 0.6f), radius: 3));
        var ic = new Icon(icon, 16, enabled ? UiTokens.TextPrimary : UiTokens.TextDisabled);
        ic.SetAnchorsPreset(LayoutPreset.FullRect);
        b.AddChild(ic);
        b.Pressed += press;
        return b;
    }

    private string NameOf(int personId) => _ctx.Snapshot().ByPerson[personId]?.Name ?? _ctx.Persons[personId - 1].Name;

    /// <summary>Vizinhos na lista de jogadores (nivel, XP, id) para as setas do cabecalho.</summary>
    private (int Prev, int Next) Neighbors()
    {
        var snap = _ctx.Snapshot();
        var order = Enumerable.Range(1, snap.ByPerson.Length - 1).Where(i => snap.ByPerson[i] != null)
            .OrderByDescending(i => snap.ByPerson[i]!.Level).ThenByDescending(i => snap.ByPerson[i]!.Experience).ThenBy(i => i).ToList();
        int at = order.IndexOf(_person.Id);
        if (at < 0) return (0, 0);
        return (at > 0 ? order[at - 1] : 0, at < order.Count - 1 ? order[at + 1] : 0);
    }

    private string FavText() => _ctx.Favorites.Contains(_person.Id) ? "★ Favorito" : "☆ Favoritar";

    private void ToggleFavorite()
    {
        if (!_ctx.Favorites.Remove(_person.Id)) _ctx.Favorites.Add(_person.Id);
        _favorite.Text = FavText();
    }

    // ---------------- Metricas derivadas (relatorio do analista) ----------------

    private readonly record struct EngagementInfo(float Pct, int Played, int Expected, int Days, int DaysPlayed);

    /// <summary>Minutos jogados nos ultimos 7 dias contra o previsto pela rotina da pessoa.</summary>
    private EngagementInfo Engagement()
    {
        long now = _ctx.Snapshot().Tick, tpd = Simulation.Core.Time.SimTime.TicksPerDay, tpm = Simulation.Core.Time.SimTime.TicksPerMinute;
        var sessions = _ctx.PersonSessions(_person.Id);
        long reg = _ctx.RegisteredAt(_person.Id);
        long first = reg >= 0 ? reg : sessions.Count > 0 ? sessions[0].Start : now;
        long from = Math.Max(first, now - 7 * tpd);
        long played = 0;
        var days = new HashSet<long>();
        foreach (var s in sessions)
        {
            long end = s.End < 0 ? now : s.End;
            long a = Math.Max(s.Start, from);
            if (end > a) { played += end - a; days.Add(a / tpd); }
        }
        var t = _person.Traits;
        double spanDays = Math.Max(2.0, (now - from) / (double)tpd);   // conta nova: janela minima de 2 dias
        int expected = (int)Math.Max(1, t.SessionMinutes * t.DaysPerWeek / 7.0 * spanDays);
        int playedMin = (int)(played / tpm);
        return new EngagementInfo(playedMin / (float)expected, playedMin, expected, (int)Math.Ceiling(spanDays), days.Count);
    }

    private static int EngagementScore(float pct) => pct >= 0.9f ? 5 : pct >= 0.7f ? 4 : pct >= 0.5f ? 3 : pct >= 0.3f ? 2 : 1;
    private static string EngagementVerdict(float pct) => EngagementScore(pct) switch { 5 => "Muito ativo", 4 => "Ativo", 3 => "Regular", 2 => "Pouco ativo", _ => "Sumido" };

    public static Color RatingColor(int score) => score switch
    {
        >= 5 => UiTokens.Success,
        4 => new Color("#8FD08A"),
        3 => UiTokens.Accent,
        2 => UiTokens.Warning,
        _ => UiTokens.Danger,
    };

    private sealed class ServerStats
    {
        public float MedianXpH, MedianKnown, MedianQuests, MyXpH;
        public int Rank, Total, ClassRank, ClassTotal, ProfileRank, ProfileTotal, XpHRank, XpHTotal;
    }

    private static float Median(List<float> xs)
    {
        if (xs.Count == 0) return 0;
        xs.Sort();
        return xs.Count % 2 == 1 ? xs[xs.Count / 2] : (xs[xs.Count / 2 - 1] + xs[xs.Count / 2]) / 2f;
    }

    private static float XpH(CharacterView c) => c.PlayedTicks < Simulation.Core.Time.SimTime.TicksPerHour ? -1
        : (float)((c.XpFromQuests + c.XpFromActivities) / Hours(c.PlayedTicks));

    private ServerStats Server(CharacterView me)
    {
        var all = _ctx.Snapshot().ByPerson.Where(c => c != null).Select(c => c!).ToList();
        var order = all.OrderByDescending(c => c.Level).ThenByDescending(c => c.Experience).ThenBy(c => c.PersonId).ToList();
        var st = new ServerStats { Total = order.Count, Rank = order.FindIndex(c => c.PersonId == me.PersonId) + 1 };
        var sameClass = order.Where(c => c.ClassId == me.ClassId).ToList();
        st.ClassTotal = sameClass.Count;
        st.ClassRank = sameClass.FindIndex(c => c.PersonId == me.PersonId) + 1;
        var sameProfile = order.Where(c => _ctx.Persons[c.PersonId - 1].ProfileId == _person.ProfileId).ToList();
        st.ProfileTotal = sameProfile.Count;
        st.ProfileRank = sameProfile.FindIndex(c => c.PersonId == me.PersonId) + 1;
        var rates = all.Select(c => (c.PersonId, R: XpH(c))).Where(x => x.R >= 0).OrderByDescending(x => x.R).ToList();
        st.XpHTotal = rates.Count;
        st.XpHRank = rates.FindIndex(x => x.PersonId == me.PersonId) + 1;
        st.MyXpH = XpH(me);
        st.MedianXpH = Median(rates.Select(x => x.R).ToList());
        st.MedianKnown = Median(all.Select(c => (float)c.KnownRegions).ToList());
        st.MedianQuests = Median(all.Select(c => (float)c.CompletedQuests.Sum(q => q.Times)).ToList());
        return st;
    }

    // ---------------- Visao geral (D-035) ----------------

    private Control BuildProfile()
    {
        var v = View;
        if (v == null)
            return Column(12, Grid(12, (Card("Atributos", AttributeGrid(), info: AttrInfo), 8),
                (Card("Situação", Wrap("Ainda não criou conta no MMORPG; pode se registrar nos próximos dias do lançamento.")), 4)));
        var detail = _ctx.Snapshot().Watched is { } d && d.PersonId == _person.Id ? d : null;
        var st = Server(v);
        var row1 = Grid(12,
            (Card("Situação atual", NowBody(v), info: "O que está fazendo e por quê\n~A última decisão mostra os fatores que somaram ou tiraram pontos da opção escolhida."), 3),
            (Card("Atributos do jogador", Column(16, AttributeGrid(), Highlights()), info: AttrInfo), 6),
            (Card("Inventário · M5", InventoryBody(v), info: "Equipamento e bens\n~Inventário, equipamento, carga e ouro passam a valer com a economia (M5). Os espaços seguem as categorias de item do conteúdo."), 3));
        var row2 = Grid(12,
            (Card("Missões", QuestSummary(v), info: "Missões do personagem\n~Detalhes na aba Missões."), 3),
            (Card("Evolução", EvolutionBody(v, detail, st), info: "Horas jogadas ao atingir cada nível\n~Curva real deste personagem (tempo conectado acumulado)."), 3),
            (Card("Horários de jogo", HoursBody(), info: "Quando costuma jogar\n~Minutos conectados por hora do dia nas sessões registradas. Dourado: janela da rotina (início preferido + sessão típica). Útil para planejar eventos e manutenção."), 3),
            (Card("Uso do tempo", TimeUseBody(detail), info: "Divisão do tempo conectado\n~Por tipo de atividade desde o cadastro; o resto é deslocamento."), 3));
        var row3 = Grid(12,
            (Card("Zonas frequentadas", ZonesBody(), info: "Onde escolhe jogar\n~Escolhas de atividade por zona registradas desde que o jogo foi aberto. Clique para abrir a zona."), 3),
            (Card("Progresso e metas", GoalsBody(v, st), info: "Nível, personalidade e metas de curto prazo\n~Metas de curto prazo (chegar a um nível, descobrir uma zona, concluir uma missão, praticar uma atividade) influenciam as escolhas sem obrigar. Ambições de longo prazo (ex.: liderar guilda) virão em etapas."), 3),
            (Card("Atividades recentes", RecentBody(), info: "Últimos acontecimentos\n~Clique numa linha para abrir a área, a zona ou a missão. Histórico completo na aba Histórico."), 6));
        return Column(12, row1, row2, row3);
    }

    // ---------------- Estatisticas (D-035) ----------------

    private Control BuildStats()
    {
        var v = View;
        if (v == null) return Wrap("Ainda sem personagem.");
        var st = Server(v);
        var row1 = Grid(12,
            (Card("Comparação com o servidor", CompareBody(v, st), info: "Este personagem contra a mediana do servidor\n~A barra mostra a diferença percentual; verde acima, vermelho abaixo."), 6),
            (Card("Sessões · últimos 14 dias", FormBody(), info: "Minutos conectados por dia\n~Verde: dia no ritmo da rotina (80% da sessão típica ou mais). Vermelho: abaixo. A linha tracejada é a média dos dias jogados."), 6));
        var row2 = Grid(12,
            (Card("Classificação", RankBody(st), info: "Posição no servidor\n~Ordem por nível e XP. XP por hora considera só quem jogou ao menos 1 hora."), 3),
            (Card("XP por fonte", XpSourceBody(v), info: "De onde vem a experiência\n~Atividades no mundo contra entregas de missões."), 3),
            (Card("Níveis no servidor", ServerLevelsBody(v), info: "Distribuição de níveis dos personagens\n~Dourado: o nível deste personagem."), 6));
        return Column(12, row1, row2);
    }

    // ----- Personalidade principal (cabecalho e progresso) -----

    /// <summary>Traco de personalidade que mais se afasta do meio (alto ou baixo).</summary>
    private (string Icon, string Label, string Text) MainPersonality() => PersonalityOf(_person.Traits);

    /// <summary>Personalidade principal: o traco que mais se afasta do meio (alto ou baixo).</summary>
    public static (string Icon, string Label, string Text) PersonalityOf(Simulation.Core.Population.PersonTraits t)
    {
        var options = new (int V, string HiIcon, string Hi, string HiText, string LoIcon, string Lo, string LoText)[]
        {
            (UiTokens.To20(t.Sociability), "users", "Sociável", "Cria vínculos com facilidade e gosta de companhia.", "user", "Reservado", "Prefere jogar sozinho e em lugares vazios."),
            (UiTokens.To20(t.Aggressiveness), "flame", "Agressivo", "Tende a confrontar e a disputar espaço.", "heart-handshake", "Pacífico", "Evita confronto e disputa."),
            (UiTokens.To20(t.Patience), "hourglass", "Paciente", "Repete a mesma atividade sem cansar tão cedo.", "zap", "Inquieto", "Cansa de repetir e troca de atividade logo."),
            (UiTokens.To20(t.Loyalty), "shield", "Leal", "Mantém as metas assumidas; no M6, fiel a grupos e guildas.", "compass", "Independente", "Larga metas com mais facilidade; no M6, muda de grupo sem apego."),
            (UiTokens.To20(t.RiskAversion), "shield", "Cauteloso", "Evita perigo, PvP e conteúdo acima do nível.", "flame", "Destemido", "Aceita risco e encara zonas perigosas."),
        };
        var best = options.OrderByDescending(o => Math.Abs(o.V - 10.5f)).First();
        if (Math.Abs(best.V - 10.5f) < 3) return ("scale", "Equilibrado", "Nenhum traço de personalidade se destaca.");
        return best.V >= 11 ? (best.HiIcon, best.Hi, best.HiText) : (best.LoIcon, best.Lo, best.LoText);
    }

    // ----- Progresso e metas de curto prazo -----

    private Control GoalsBody(CharacterView v, ServerStats st)
    {
        var prog = _ctx.Content.Progression;
        long need = ProgressionRules.XpToNextLevel(prog, v.Level);
        bool max = v.Level >= prog.LevelCap;
        var ring = new RingGauge
        {
            CustomMinimumSize = new Vector2(88, 88), SizeFlagsVertical = SizeFlags.ShrinkCenter,
            Value = max ? 1 : v.Experience / (float)Math.Max(1, need), Color = UiTokens.Success,
            Center = max ? "máx." : $"{v.Experience * 100 / Math.Max(1, need)}%", Caption = max ? "" : $"do Nv {v.Level + 1}",
        };
        string stage = (ProgressionRules.StageRate(prog, v.Level) / 1000.0).ToString("0.##", CultureInfo.GetCultureInfo("pt-BR"));
        string eta = max || st.MyXpH <= 0 ? "–" : $"~{PtBr.Decimal((need - v.Experience) / st.MyXpH)} h";
        var data = Column(5,
            KeyValue("Nível", $"{v.Level} de {prog.LevelCap}"),
            KeyValue("XP", max ? PtBr.Int(v.Experience) : $"{PtBr.Int(v.Experience)} / {PtBr.Int(need)}"),
            KeyValue("Taxa da faixa", $"×{stage}"),
            KeyValue("Próximo nível", eta));
        data.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        var col = Column(12, Row(14, ring, data));

        var (picon, plabel, ptext) = MainPersonality();
        col.AddChild(Section("Personalidade"));
        var p = new HBoxContainer();
        p.AddThemeConstantOverride("separation", 10);
        p.AddChild(new Icon(picon, 18, UiTokens.AccentSoft));
        p.AddChild(Column(1, ThemeFactory.Text(plabel, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.SemiBold), Wrap(ptext, UiTokens.FontSmall, UiTokens.TextSecondary)));
        col.AddChild(p);
        col.AddChild(Wrap($"Perfil {ProfileName}: {ProfileHint(_person.ProfileId)}", UiTokens.FontSmall, UiTokens.TextSecondary));

        col.AddChild(Section("Metas", ThemeFactory.Text($"{v.GoalsCompleted} concluídas · {v.GoalsAbandoned} abandonadas", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers)));
        if (v.Goals.Count == 0)
            col.AddChild(Wrap("Sem meta agora. Nem todo jogador tem uma o tempo todo; ao conectar, pode adotar uma conforme o perfil e a personalidade.", UiTokens.FontSmall, UiTokens.TextSecondary));
        foreach (var g in v.Goals)
        {
            var (icon, text, progress) = GoalText(v, g);
            var row = GoalRow(icon, text, progress, UiTokens.Accent);
            var motive = AttributeInfo.Name(g.MotiveKey);
            var tip = new TipPanel
            {
                TooltipText = $"{text}\nMotivo|{motive} {UiTokens.To20(g.MotiveValue)}\nComprometimento|{g.Commitment / 10}%\nDesde|{PtBr.ShortDate(_ctx.Calendar.ToDateTime(g.SetTick))}\n~Meta de curto prazo: influencia as escolhas sem obrigar; vira um fator a mais na decisão, com o peso do comprometimento (paciência e lealdade). Sem progresso por muitas horas de jogo, é abandonada (os pacientes insistem mais).",
                MouseFilter = MouseFilterEnum.Stop,
            };
            tip.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
            tip.AddChild(Column(3, row, ThemeFactory.Text($"Motivo: {motive} · comprometimento {g.Commitment / 10}%", UiTokens.FontCaption, UiTokens.TextSecondary)));
            col.AddChild(tip);
        }
        return col;
    }

    /// <summary>Icone, texto e progresso (0–1) de uma meta de curto prazo.</summary>
    private (string Icon, string Text, float Progress) GoalText(CharacterView v, GoalView g)
    {
        switch (g.Type)
        {
            case Simulation.Core.Population.GoalTypes.ReachLevel:
                return ("trending-up", $"Chegar ao nível {g.TargetValue}", (v.Level - g.StartValue) / (float)Math.Max(1, g.TargetValue - g.StartValue));
            case Simulation.Core.Population.GoalTypes.DiscoverZone:
                return ("compass", $"Descobrir {_ctx.ZoneName(g.Target)}", 0);
            case Simulation.Core.Population.GoalTypes.CompleteQuest:
            {
                string name = _ctx.Content.Quests.TryGetValue(g.Target, out var q) ? q.Name : g.Target;
                var aq = v.ActiveQuests.FirstOrDefault(x => x.QuestId == g.Target);
                float t = aq == null || q == null ? 0 : aq.StepIndex / (float)(q.Steps.Count + 1);
                return ("scroll-text", $"Concluir {name}", t);
            }
            default:
                return (TypeIcon(g.Target), $"Praticar {PtBr.ActivityType(g.Target).ToLowerInvariant()} · {g.Progress}/{g.TargetValue} min", g.Progress / (float)Math.Max(1, g.TargetValue));
        }
    }

    private static Control GoalRow(string icon, string text, float t, Color color)
    {
        var r = new HBoxContainer();
        r.AddThemeConstantOverride("separation", 10);
        r.AddChild(new Icon(icon, 16, color));
        var c = Column(4);
        var top = new HBoxContainer();
        var l = ThemeFactory.Text(text, UiTokens.FontSmall, UiTokens.TextLabel, UiFonts.Medium);
        l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        l.ClipText = true;
        top.AddChild(l);
        top.AddChild(ThemeFactory.Text($"{Math.Clamp(t, 0, 1) * 100:0}%", UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Numbers));
        c.AddChild(top);
        c.AddChild(new ThinBar(t, color) { CustomMinimumSize = new Vector2(0, 3) });
        r.AddChild(c);
        return r;
    }

    // ----- Inventario (M5) -----

    private Control InventoryBody(CharacterView v)
    {
        Control Slot(string icon, string name)
        {
            var box = new PanelContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 86) };
            var sb = new StyleBoxFlat { BgColor = new Color("#0C0F13"), BorderColor = UiTokens.BorderStrong, ContentMarginTop = 10, ContentMarginBottom = 10 };
            sb.SetBorderWidthAll(1);
            sb.SetCornerRadiusAll(4);
            box.AddThemeStyleboxOverride("panel", sb);
            var c = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
            c.AddThemeConstantOverride("separation", 4);
            var ic = new Icon(icon, 26, UiTokens.TextDisabled) { SizeFlagsHorizontal = SizeFlags.ShrinkCenter };
            c.AddChild(ic);
            var n = ThemeFactory.Text(name, UiTokens.FontSmall, UiTokens.TextLabel, UiFonts.Medium);
            n.HorizontalAlignment = HorizontalAlignment.Center;
            c.AddChild(n);
            var e = ThemeFactory.Text("vazio", UiTokens.FontCaption, UiTokens.TextDisabled);
            e.HorizontalAlignment = HorizontalAlignment.Center;
            c.AddChild(e);
            box.AddChild(c);
            return box;
        }
        // Espacos de equipamento conforme as categorias de item do conteudo (arma, armadura).
        var slots = Row(8);
        var cats = _ctx.Content.Items.Values.Select(i => i.Category).Distinct().ToList();
        if (cats.Contains("weapon")) slots.AddChild(Slot("sword", "Arma"));
        if (cats.Contains("armor")) slots.AddChild(Slot("shield", "Armadura"));
        var col = Column(12, Section("Equipamento"), slots);

        // O que as missoes ja entregues teriam dado (definido, aplicado no M5).
        long gold = 0;
        var items = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var (qid, times, _) in v.CompletedQuests)
        {
            if (!_ctx.Content.Quests.TryGetValue(qid, out var q)) continue;
            gold += q.Rewards.GoldCopper * times;
            foreach (var it in q.Rewards.ItemIds) items[it] = items.GetValueOrDefault(it) + times;
        }
        col.AddChild(Section("Previsto pelas missões"));
        col.AddChild(KeyValue("Ouro", gold > 0 ? QuestModal.Coins(gold) : "—", gold > 0 ? UiTokens.AccentSoft : null));
        if (items.Count == 0) col.AddChild(KeyValue("Itens", "—"));
        foreach (var (id, n) in items.OrderBy(kv => kv.Key, StringComparer.Ordinal).Take(4))
            col.AddChild(KeyValue(_ctx.Content.Items.TryGetValue(id, out var item) ? item.Name : id, $"×{n}"));
        col.AddChild(Wrap("Inventário, carga e ouro passam a valer com a economia (M5).", UiTokens.FontCaption, UiTokens.TextDisabled));
        return col;
    }

    // ----- Horarios de jogo -----

    private Control HoursBody()
    {
        long now = _ctx.Snapshot().Tick, tph = Simulation.Core.Time.SimTime.TicksPerHour, tpm = Simulation.Core.Time.SimTime.TicksPerMinute;
        var minutes = new float[24];
        foreach (var s in _ctx.PersonSessions(_person.Id))
        {
            long end = s.End < 0 ? now : s.End;
            for (long t = s.Start; t < end;)
            {
                long hourEnd = (t / tph + 1) * tph;
                long stop = Math.Min(end, hourEnd);
                int h = _ctx.Calendar.ToDateTime(t).Hour;
                minutes[h] += (stop - t) / (float)tpm;
                t = stop;
            }
        }
        var tr = _person.Traits;
        int startH = tr.PreferredStartHour, spanH = Math.Max(1, (tr.SessionMinutes + 59) / 60);
        bool InRoutine(int h) => ((h - startH + 24) % 24) < spanH;
        var chart = new ColumnChart
        {
            CustomMinimumSize = new Vector2(0, 150), Values = minutes, Labels = Enumerable.Range(0, 24).Select(h => $"{h:00}h").ToList(),
            Unit = " min", ShowAverage = false, ColorAt = i => InRoutine(i) ? UiTokens.Accent : UiTokens.Info,
            EmptyText = "Sem sessões registradas ainda.",
        };
        int peak = minutes.Max() <= 0 ? -1 : Array.IndexOf(minutes, minutes.Max());
        return Column(12, chart, Row(0,
            MiniStat(peak < 0 ? "–" : $"{peak:00}h", "Pico"),
            MiniStat($"{startH:00}h", "Início preferido"),
            MiniStat($"{tr.DaysPerWeek}/7", "Dias por semana")));
    }

    // ----- Estatisticas: comparacao, XP por fonte, niveis -----

    private Control CompareBody(CharacterView v, ServerStats st)
    {
        var all = _ctx.Snapshot().ByPerson.Where(c => c != null).Select(c => c!).ToList();
        float medLevel = Median(all.Select(c => (float)c.Level).ToList());
        float medHours = Median(all.Select(c => (float)Hours(c.PlayedTicks)).ToList());
        var e = Engagement();
        var rows = new (string Icon, string Name, float Mine, float Med, string MineText, string MedText)[]
        {
            ("award", "Nível", v.Level, medLevel, v.Level.ToString(), PtBr.Decimal(medLevel)),
            ("timer", "Horas jogadas", (float)Hours(v.PlayedTicks), medHours, PtBr.Decimal(Hours(v.PlayedTicks)) + " h", PtBr.Decimal(medHours) + " h"),
            ("trending-up", "XP por hora", Math.Max(0, st.MyXpH), st.MedianXpH, st.MyXpH < 0 ? "–" : PtBr.Int((long)st.MyXpH), PtBr.Int((long)st.MedianXpH)),
            ("compass", "Zonas conhecidas", v.KnownRegions, st.MedianKnown, v.KnownRegions.ToString(), $"{st.MedianKnown:0}"),
            ("scroll-text", "Missões entregues", v.CompletedQuests.Sum(q => q.Times), st.MedianQuests, v.CompletedQuests.Sum(q => q.Times).ToString(), $"{st.MedianQuests:0}"),
        };
        var col = Column(10);
        var head = new HBoxContainer();
        foreach (var (t, w, right) in new[] { ("Indicador", 0f, false), ("Este", 80f, true), ("Mediana", 80f, true), ("Diferença", 200f, true) })
        {
            var l = ThemeFactory.Text(t, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium);
            if (w > 0) l.CustomMinimumSize = new Vector2(w, 0); else l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            if (right) l.HorizontalAlignment = HorizontalAlignment.Right;
            head.AddChild(l);
        }
        col.AddChild(Column(6, head, Divider()));
        foreach (var r in rows)
        {
            float diff = r.Med <= 0 ? (r.Mine > 0 ? 1 : 0) : r.Mine / r.Med - 1;
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 0);
            var name = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            name.AddThemeConstantOverride("separation", 10);
            name.AddChild(new Icon(r.Icon, 16, UiTokens.TextSecondary));
            name.AddChild(ThemeFactory.Text(r.Name, UiTokens.FontBody, UiTokens.TextLabel, UiFonts.Medium));
            row.AddChild(name);
            var mine = ThemeFactory.Text(r.MineText, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Numbers);
            mine.CustomMinimumSize = new Vector2(80, 0);
            mine.HorizontalAlignment = HorizontalAlignment.Right;
            row.AddChild(mine);
            var med = ThemeFactory.Text(r.MedText, UiTokens.FontBody, UiTokens.TextSecondary, UiFonts.Numbers);
            med.CustomMinimumSize = new Vector2(80, 0);
            med.HorizontalAlignment = HorizontalAlignment.Right;
            row.AddChild(med);
            var bar = new HBoxContainer { CustomMinimumSize = new Vector2(200, 0) };
            bar.AddThemeConstantOverride("separation", 8);
            bar.AddChild(new Control { CustomMinimumSize = new Vector2(16, 0) });
            bar.AddChild(new DivergingTrack(Mathf.Clamp(diff, -1, 1), diff >= 0 ? UiTokens.Success : UiTokens.Danger) { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 14) });
            var pct = ThemeFactory.Text($"{(diff >= 0 ? "+" : "")}{diff * 100:0}%", UiTokens.FontSmall, diff >= 0 ? UiTokens.Success : UiTokens.Danger, UiFonts.Numbers);
            pct.CustomMinimumSize = new Vector2(48, 0);
            pct.HorizontalAlignment = HorizontalAlignment.Right;
            bar.AddChild(pct);
            row.AddChild(bar);
            col.AddChild(row);
        }
        col.AddChild(Divider());
        col.AddChild(KeyValue("Engajamento (7 dias)", $"{e.Pct * 100:0}% da rotina · {e.Played} de {e.Expected} min"));
        return col;
    }

    private Control XpSourceBody(CharacterView v)
    {
        long total = v.XpFromQuests + v.XpFromActivities;
        if (total == 0) return Wrap("Ainda sem XP registrado.");
        var donut = new DonutChart
        {
            CustomMinimumSize = new Vector2(124, 124), SizeFlagsHorizontal = SizeFlags.ShrinkCenter,
            Slices = new[] { ((float)v.XpFromActivities, UiTokens.Info), ((float)v.XpFromQuests, UiTokens.Accent) },
            Center = PtBr.Int(total), Caption = "XP total",
        };
        return Column(14, donut,
            KeyValue("Atividades no mundo", $"{PtBr.Int(v.XpFromActivities)} · {v.XpFromActivities * 100 / total}%", UiTokens.Info),
            KeyValue("Missões", $"{PtBr.Int(v.XpFromQuests)} · {v.XpFromQuests * 100 / total}%", UiTokens.AccentSoft));
    }

    private Control ServerLevelsBody(CharacterView v)
    {
        var all = _ctx.Snapshot().ByPerson.Where(c => c != null).Select(c => c!.Level).ToList();
        int hi = Math.Max(v.Level, all.Count == 0 ? 1 : all.Max());
        var values = Enumerable.Range(1, hi).Select(l => (float)all.Count(x => x == l)).ToList();
        return new ColumnChart
        {
            CustomMinimumSize = new Vector2(0, 200), CountScale = true, ShowAverage = false, Unit = " personagens",
            Values = values, Labels = Enumerable.Range(1, hi).Select(l => $"Nv {l}").ToList(),
            ColorAt = i => i + 1 == v.Level ? UiTokens.Accent : UiTokens.Info,
        };
    }

    private const string AttrInfo = "Atributos do jogador (a pessoa, não o personagem), de 1 a 20\n~Mudam devagar com o que a pessoa vive no jogo: a seta mostra a evolução desde o início. Nas capacidades, a parte clara da barra é o potencial, que depende do talento.\n~Passe o mouse em cada um para ver o que influencia e o que o faz mudar.";

    // ----- Situacao atual -----

    private Control NowBody(CharacterView v)
    {
        long now = _ctx.Snapshot().Tick;
        var col = Column(8);
        var hero = new HBoxContainer();
        hero.AddThemeConstantOverride("separation", 10);
        string type = v.Status == CharacterStatus.Traveling ? "travel" : v.ActivityId.Length > 0 ? ActivityTypeOf(v.TargetRegionId, v.ActivityId) : "";
        hero.AddChild(new Icon(v.Status == CharacterStatus.Offline ? "moon" : v.Status == CharacterStatus.Traveling ? "footprints" : TypeIcon(type), 20,
            v.Status == CharacterStatus.Offline ? UiTokens.TextSecondary : type == "travel" ? UiTokens.Info : ActivityPalette.Color(type)));
        var heroText = Wrap(StatusText.Of(v, _ctx.Content), 15, UiTokens.TextPrimary, UiFonts.SemiBold);
        heroText.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        hero.AddChild(heroText);
        col.AddChild(hero);
        if (v.Status == CharacterStatus.Doing && v.ActivityEndTick > v.ActivityStartTick)
        {
            float total = v.ActivityEndTick - v.ActivityStartTick, done = Math.Clamp(now - v.ActivityStartTick, 0, (long)total);
            col.AddChild(Bar("Atividade", done, total, UiTokens.Accent, $"{(int)(done * 100 / total)}%", 90));
        }
        else if (v.Status == CharacterStatus.Traveling && v.MoveEndTick > v.MoveStartTick)
        {
            float total = v.MoveEndTick - v.MoveStartTick, done = Math.Clamp(now - v.MoveStartTick, 0, (long)total);
            col.AddChild(Bar("Trecho", done, total, UiTokens.Info, $"{(int)(done * 100 / total)}%", 90));
        }
        col.AddChild(KeyValue("Zona atual", _ctx.ZoneName(v.RegionId)));
        if (v.Status != CharacterStatus.Offline)
        {
            col.AddChild(KeyValue("Sessão", $"{_ctx.Clock(v.SessionStartTick)} – {_ctx.Clock(v.SessionEndTick)}"));
            col.AddChild(KeyValue("Restante", $"{Math.Max(0, (v.SessionEndTick - now) / Simulation.Core.Time.SimTime.TicksPerMinute)} min", UiTokens.AccentSoft));
        }
        var dcs = v.LastDecision;
        if (dcs != null)
        {
            col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 2) });
            col.AddChild(Section("Por que escolheu", ThemeFactory.Text($"{dcs.OptionsConsidered} opções · nota {dcs.Utility}", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers)));
            var factors = dcs.Factors.Where(f => f.Contribution != 0).OrderByDescending(f => Math.Abs(f.Contribution)).Take(5).ToList();
            int max = Math.Max(1, factors.Count == 0 ? 1 : factors.Max(f => Math.Abs(f.Contribution)));
            foreach (var f in factors)
            {
                var row = new TipPanel { TooltipText = $"{FactorPt(f.Key)}\nPeso|{(f.Contribution > 0 ? "+" : "")}{f.Contribution}\n~{FactorHint(f.Key)}", MouseFilter = MouseFilterEnum.Stop };
                row.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
                var r = new HBoxContainer();
                r.AddThemeConstantOverride("separation", 8);
                var l = ThemeFactory.Text(FactorPt(f.Key), UiTokens.FontSmall, UiTokens.TextLabel, UiFonts.Medium);
                l.CustomMinimumSize = new Vector2(132, 0);
                l.ClipText = true;
                r.AddChild(l);
                r.AddChild(new DivergingTrack(f.Contribution / (float)max, f.Contribution > 0 ? UiTokens.Success : UiTokens.Danger) { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 14) });
                var n = ThemeFactory.Text((f.Contribution > 0 ? "+" : "") + f.Contribution, UiTokens.FontSmall, f.Contribution > 0 ? UiTokens.Success : UiTokens.Danger, UiFonts.Numbers);
                n.CustomMinimumSize = new Vector2(38, 0);
                n.HorizontalAlignment = HorizontalAlignment.Right;
                r.AddChild(n);
                row.AddChild(r);
                col.AddChild(row);
            }
            if (dcs.RunnerUpActivityKey.Length > 0)
            {
                var p = dcs.RunnerUpActivityKey.Split('/');
                col.AddChild(KeyValue("2ª opção", _ctx.ActivityName(p[0], p[1]), UiTokens.TextSecondary));
            }
        }
        return col;
    }

    private static string ProfileHint(string profileId) => profileId switch
    {
        "casual" => "joga pouco por dia e sem pressa.",
        "competitivo" => "busca desafio e ranking.",
        "social" => "joga pela companhia.",
        "comerciante" => "prefere coletar e negociar.",
        "explorador" => "quer ver o mundo inteiro.",
        "hardcore" => "joga muitas horas e quer evoluir rápido.",
        _ => "perfil de origem.",
    };

    // ----- Forma (minutos por dia + ultimas sessoes) -----

    private Control FormBody()
    {
        long now = _ctx.Snapshot().Tick, tpd = Simulation.Core.Time.SimTime.TicksPerDay, tpm = Simulation.Core.Time.SimTime.TicksPerMinute;
        long today = now / tpd;
        var sessions = _ctx.PersonSessions(_person.Id);
        long reg = _ctx.RegisteredAt(_person.Id);
        long firstDay = reg >= 0 ? reg / tpd : sessions.Count > 0 ? sessions[0].Start / tpd : today;
        int days = (int)Math.Clamp(today - firstDay + 1, 1, 14);
        var minutes = new float[days];
        foreach (var s in sessions)
        {
            long end = s.End < 0 ? now : s.End;
            for (long t = s.Start; t < end;)
            {
                long dayEnd = (t / tpd + 1) * tpd;
                long stop = Math.Min(end, dayEnd);
                int idx = (int)(days - 1 - (today - t / tpd));
                if (idx >= 0 && idx < days) minutes[idx] += (stop - t) / (float)tpm;
                t = stop;
            }
        }
        var labels = Enumerable.Range(0, days).Select(i => { var dt = _ctx.Calendar.ToDateTime(Math.Max(0, today - days + 1 + i) * tpd); return $"{dt.Day:00}/{dt.Month:00}"; }).ToList();
        int routine = _person.Traits.SessionMinutes;
        var chart = new ColumnChart
        {
            CustomMinimumSize = new Vector2(0, 132), Values = minutes, Labels = labels, Unit = " min",
            ColorOf = m => m >= routine * 0.8f ? UiTokens.Success : UiTokens.Danger,
            EmptyText = "Sem sessões registradas ainda.",
        };
        var played = minutes.Where(m => m > 0).ToList();
        var stats = Row(0,
            MiniStat(played.Count == 0 ? "–" : $"{played.Average():0}", "Média (min)"),
            MiniStat($"{played.Count}/{days}", "Dias jogados"),
            MiniStat($"{routine}", "Rotina (min)"));
        var col = Column(12, chart, stats);
        // Ultimas sessoes (tabela compacta, mais nova primeiro).
        var grid = new GridContainer { Columns = 4 };
        grid.AddThemeConstantOverride("h_separation", 12);
        grid.AddThemeConstantOverride("v_separation", 6);
        foreach (var h in new[] { "Dia", "Início", "Duração", "Prevista" })
            grid.AddChild(Cell(h, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium, h != "Dia"));
        foreach (var s in sessions.Reverse().Take(3))
        {
            var dt = _ctx.Calendar.ToDateTime(s.Start);
            long end = s.End < 0 ? now : s.End;
            int dur = (int)((end - s.Start) / tpm);
            grid.AddChild(Cell(PtBr.ShortDate(dt), UiTokens.FontSmall, UiTokens.TextLabel, UiFonts.Medium, false));
            grid.AddChild(Cell(_ctx.Clock(s.Start), UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Numbers, true));
            grid.AddChild(Cell(s.End < 0 ? $"{dur} min ▸" : $"{dur} min", UiTokens.FontSmall, dur >= s.PlannedMinutes * 0.8f ? UiTokens.Success : UiTokens.Warning, UiFonts.Numbers, true));
            grid.AddChild(Cell($"{s.PlannedMinutes} min", UiTokens.FontSmall, UiTokens.TextSecondary, UiFonts.Numbers, true));
        }
        if (sessions.Count > 0) col.AddChild(Column(8, Divider(), grid));
        return col;
    }

    private static Label Cell(string text, int size, Color color, Font font, bool right)
    {
        var l = ThemeFactory.Text(text, size, color, font);
        l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        if (right) l.HorizontalAlignment = HorizontalAlignment.Right;
        return l;
    }

    // ----- Evolucao -----

    private Control EvolutionBody(CharacterView v, CharacterDetailView? d, ServerStats st)
    {
        var prog = _ctx.Content.Progression;
        long need = ProgressionRules.XpToNextLevel(prog, v.Level);
        bool max = v.Level >= prog.LevelCap;
        Control chart;
        if (d == null || d.LevelTicks.Count == 0) chart = Wrap(d == null ? "Carregando…" : "Ainda não subiu de nível.");
        else chart = new LineChart
        {
            CustomMinimumSize = new Vector2(0, 170), Unit = " h",
            XLabels = Enumerable.Range(2, d.LevelTicks.Count).Select(l => $"Nv {l}").ToList(),
            Series = new[] { new ChartSeries { Name = "Horas até o nível", Color = UiTokens.Info, Values = d.LevelTicks.Select(t => (float)Hours(t)).ToList() } },
        };
        chart.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        long played = d?.PlayedTicks ?? v.PlayedTicks;
        long atLevel = d != null && d.LevelTicks.Count > 0 ? played - d.LevelTicks[^1] : played;
        var side = Column(10,
            Column(0, ThemeFactory.Text(st.MyXpH < 0 ? "–" : PtBr.Int((long)st.MyXpH), 20, UiTokens.TextPrimary, UiFonts.Numbers), ThemeFactory.Text("XP por hora", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium)),
            Column(0, ThemeFactory.Text($"{PtBr.Decimal(Hours(atLevel))} h", 20, UiTokens.TextPrimary, UiFonts.Numbers), ThemeFactory.Text($"no nível {v.Level}", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium)),
            Column(0, ThemeFactory.Text($"{PtBr.Decimal(Hours(played))} h", 20, UiTokens.TextPrimary, UiFonts.Numbers), ThemeFactory.Text("jogadas no total", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium)));
        side.CustomMinimumSize = new Vector2(96, 0);
        side.SizeFlagsHorizontal = SizeFlags.ShrinkEnd;
        side.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        return Row(16, chart, new ColorRect { Color = UiTokens.Divider, CustomMinimumSize = new Vector2(1, 0) }, side);
    }

    // ----- Classificacao -----

    private Control RankBody(ServerStats st)
    {
        string cls = View != null && _ctx.Content.Classes.TryGetValue(View.ClassId, out var c) ? c.Name : "classe";
        int topPct = st.Total == 0 ? 0 : (int)Math.Ceiling(st.Rank * 100.0 / st.Total);
        Control Tile(int rank, int total, string caption)
        {
            var t = Column(0, ThemeFactory.Text(rank > 0 ? $"{rank}º" : "–", 22, UiTokens.TextPrimary, UiFonts.Numbers),
                ThemeFactory.Text(caption, UiTokens.FontCaption, UiTokens.TextLabel, UiFonts.Medium),
                ThemeFactory.Text($"de {total}", UiTokens.FontCaption, UiTokens.TextSecondary));
            return t;
        }
        var grid = new GridContainer { Columns = 2 };
        grid.AddThemeConstantOverride("h_separation", 16);
        grid.AddThemeConstantOverride("v_separation", 14);
        grid.AddChild(Tile(st.Rank, st.Total, "No servidor"));
        grid.AddChild(Tile(st.ClassRank, st.ClassTotal, $"Entre {cls}s".Replace("ss", "s")));
        grid.AddChild(Tile(st.ProfileRank, st.ProfileTotal, $"Perfil {ProfileName}"));
        grid.AddChild(Tile(st.XpHRank, st.XpHTotal, "Em XP por hora"));
        var badge = new HBoxContainer();
        badge.AddThemeConstantOverride("separation", 8);
        badge.AddChild(new Icon("trophy", 16, UiTokens.Accent));
        badge.AddChild(ThemeFactory.Text($"Top {Math.Max(1, topPct)}% do servidor", UiTokens.FontBody, UiTokens.AccentSoft, UiFonts.SemiBold));
        return Column(14, badge, Divider(), grid);
    }

    // ----- Zonas frequentadas -----

    private Control ZonesBody()
    {
        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var e in _ctx.PersonHistory(_person.Id))
            if (e.Type == "activity_chosen" && (e.Detail ?? "").Split('/') is { Length: 2 } p) counts[p[0]] = counts.GetValueOrDefault(p[0]) + 1;
        if (counts.Count == 0) return Wrap("Nenhuma escolha registrada desde que o jogo foi aberto.");
        int total = counts.Values.Sum(), max = counts.Values.Max();
        var col = Column(6);
        foreach (var (zid, n) in counts.OrderByDescending(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.Ordinal).Take(5))
        {
            if (!_ctx.Content.Regions.TryGetValue(zid, out var z)) continue;
            string id = zid;
            var b = HoverRow(() => _ctx.OpenZone(id), $"{z.Name}\nEscolhas|{n} de {total}\n~Clique para abrir a zona.");
            var r = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            r.SetAnchorsPreset(LayoutPreset.FullRect);
            r.OffsetLeft = 6; r.OffsetRight = -6;
            r.AddThemeConstantOverride("separation", 10);
            r.AddChild(new ZoneEmblem(z) { CustomMinimumSize = new Vector2(26, 26), SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore });
            var info = Column(4);
            info.MouseFilter = MouseFilterEnum.Ignore;
            info.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            var top = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            var nm = ThemeFactory.Text(z.Name, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Medium);
            nm.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            top.AddChild(nm);
            top.AddChild(ThemeFactory.Text($"{n * 100 / total}%", UiTokens.FontSmall, UiTokens.TextLabel, UiFonts.Numbers));
            info.AddChild(top);
            info.AddChild(new ThinBar(n / (float)max, z.IsCity ? UiTokens.Accent : UiTokens.Info) { CustomMinimumSize = new Vector2(0, 3) });
            r.AddChild(info);
            b.CustomMinimumSize = new Vector2(0, 40);
            b.AddChild(r);
            col.AddChild(b);
        }
        return col;
    }

    // ----- Missoes -----

    private Control QuestSummary(CharacterView v)
    {
        long xpTotal = v.XpFromQuests + v.XpFromActivities;
        var nums = Row(0,
            MiniStat(v.ActiveQuests.Count.ToString(), "Ativas"),
            MiniStat(PtBr.Int(v.CompletedQuests.Sum(c => c.Times)), "Entregues"),
            MiniStat(xpTotal == 0 ? "–" : $"{v.XpFromQuests * 100 / xpTotal}%", "Do XP"));
        var col = Column(10, nums, Divider());
        foreach (var p in v.ActiveQuests.Take(4))
        {
            if (!_ctx.Content.Quests.TryGetValue(p.QuestId, out var q)) continue;
            bool ready = p.StepIndex >= q.Steps.Count;
            string qid = q.Id;
            var b = HoverRow(() => _ctx.OpenQuest(qid), $"{q.Name}\nEtapa|{Math.Min(p.StepIndex + 1, q.Steps.Count + 1)} de {q.Steps.Count + 1}\n~Clique para abrir a missão.");
            b.CustomMinimumSize = new Vector2(0, 40);
            var c = Column(5);
            c.MouseFilter = MouseFilterEnum.Ignore;
            c.SetAnchorsPreset(LayoutPreset.FullRect);
            c.OffsetLeft = 6; c.OffsetRight = -6; c.OffsetTop = 4;
            var top = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            top.AddThemeConstantOverride("separation", 8);
            top.AddChild(new Icon("scroll-text", 14, QuestModal.CategoryColor(q.Category)));
            var nm = ThemeFactory.Text(q.Name, UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Medium);
            nm.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            nm.ClipText = true;
            top.AddChild(nm);
            top.AddChild(ThemeFactory.Text(ready ? "entregar" : $"{p.StepMinutes}/{q.Steps[p.StepIndex].Minutes} min", UiTokens.FontCaption, ready ? UiTokens.Success : UiTokens.TextSecondary, UiFonts.Numbers));
            c.AddChild(top);
            c.AddChild(new ThinBar(ready ? 1 : p.StepMinutes / (float)Math.Max(1, q.Steps[p.StepIndex].Minutes), ready ? UiTokens.Success : UiTokens.Info) { CustomMinimumSize = new Vector2(0, 3) });
            b.AddChild(c);
            col.AddChild(b);
        }
        if (v.ActiveQuests.Count == 0) col.AddChild(Wrap("Nenhuma missão em andamento."));
        return col;
    }

    private static Control MiniStat(string value, string caption)
    {
        var c = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        c.AddThemeConstantOverride("separation", 0);
        c.AddChild(ThemeFactory.Text(value, 20, UiTokens.TextPrimary, UiFonts.Numbers));
        c.AddChild(ThemeFactory.Text(caption, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium));
        return c;
    }

    // ----- Atributos e tracos -----

    private static Color AttrTier(int v20) => v20 >= 18 ? UiTokens.Exceptional : v20 >= 14 ? UiTokens.Success : v20 >= 9 ? new Color("#8B94A1") : v20 >= 6 ? new Color("#4E5661") : UiTokens.Danger;

    private Control AttributeGrid()
    {
        var t = _person.Traits;
        var b0 = _person.BaseTraits;
        Control Group(string title, string[] keys)
        {
            var col = Column(10);
            col.AddChild(Section(title));
            foreach (var key in keys)
            {
                var info = AttributeInfo.ByKey[key];
                int value = t.Get(key), baseValue = b0.Get(key);
                int v20 = UiTokens.To20(value), base20 = UiTokens.To20(baseValue);
                int cap = Simulation.Core.Behavior.TraitDynamics.CeilingOf(_ctx.Content.Dynamics, b0, t.Talent, key);
                int cap20 = UiTokens.To20(cap);
                string change = value == baseValue ? "igual à base" : $"{(value > baseValue ? "+" : "")}{value - baseValue} desde o início";
                string tip = $"{info.Name}\nAtual|{v20} / 20\nNo início|{base20} / 20 ({change})"
                    + (info.Skill ? $"\nPotencial|{cap20} / 20 (talento {AttributeInfo.TalentTier(t.Talent)})" : "")
                    + $"\n~Influencia: {info.Influences}\n~Muda com: {info.Changes}";
                var cell = new TipPanel { TooltipText = tip, MouseFilter = MouseFilterEnum.Stop };
                cell.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
                var c = Column(5);
                var r = new HBoxContainer();
                r.AddThemeConstantOverride("separation", 4);
                var l = ThemeFactory.Text(info.Name, UiTokens.FontBody, UiTokens.TextLabel, UiFonts.Medium);
                l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                r.AddChild(l);
                // Seta de evolucao em relacao ao inicio (como nos jogos de gestao esportiva).
                if (v20 != base20)
                    r.AddChild(ThemeFactory.Text(v20 > base20 ? "▲" : "▼", 9, v20 > base20 ? UiTokens.Success : UiTokens.Danger, UiFonts.Numbers));
                r.AddChild(ThemeFactory.Text(v20.ToString(), UiTokens.FontBody, v20 >= 14 ? AttrTier(v20) : v20 <= 5 ? UiTokens.Danger : UiTokens.TextPrimary, UiFonts.Numbers));
                c.AddChild(r);
                c.AddChild(new PotentialBar(v20 / 20f, info.Skill ? cap20 / 20f : 0, AttrTier(v20)) { CustomMinimumSize = new Vector2(0, 3) });
                cell.AddChild(c);
                col.AddChild(cell);
            }
            return col;
        }
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 24);
        foreach (var g in new[]
        {
            Group("Estilo de jogo", new[] { "pve", "pvp", "trade", "exploration", "socializing" }),
            Group("Personalidade", new[] { "sociability", "aggressiveness", "patience", "loyalty", "riskAversion" }),
            Group("Capacidade", new[] { "mechanicalSkill", "gameKnowledge", "coordination" }),
            Group("Conduta", new[] { "cheatPropensity", "opportunism" }),
        })
        {
            g.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(g);
        }
        return row;
    }

    /// <summary>Tracos marcantes derivados dos atributos (o que mais pesa nas escolhas).</summary>
    private Control Highlights()
    {
        var t = _person.Traits;
        var candidates = new List<(int Score, string Icon, string Title, string Text)>();
        void High(int value, string icon, string title, string text) { int v20 = UiTokens.To20(value); if (v20 >= 14) candidates.Add((v20, icon, title, text)); }
        void Low(int value, string icon, string title, string text) { int v20 = UiTokens.To20(value); if (v20 <= 6) candidates.Add((27 - v20, icon, title, text)); }
        High(t.Pve, "sword", "Caçador", "Prefere caçar criaturas; áreas de caça pesam mais nas escolhas.");
        High(t.Pvp, "swords", "Gosta de PvP", "Procura combate contra jogadores (o PvP chega no M3).");
        High(t.Trade, "coins", "Mercador", "Gosta de coletar, comprar e vender.");
        High(t.Exploration, "compass", "Explorador", "Tende a descobrir zonas novas cedo.");
        High(t.Socializing, "users", "Sociável", "Gosta de lugares com gente e de estar em grupo.");
        High(t.Patience, "hourglass", "Paciente", "Repete a mesma atividade sem cansar tão cedo.");
        High(t.RiskAversion, "shield", "Cauteloso", "Evita perigo, PvP e conteúdo acima do nível.");
        High(t.Loyalty, "heart-handshake", "Leal", "Tende a ficar com grupos e guildas (M6).");
        Low(t.RiskAversion, "flame", "Destemido", "Aceita risco; encara zonas perigosas.");
        Low(t.Patience, "zap", "Inquieto", "Cansa de repetir e troca de atividade logo.");
        Low(t.Sociability, "user", "Reservado", "Prefere lugares vazios e jogar sozinho.");
        var top = candidates.OrderByDescending(c => c.Score).Take(3).ToList();
        if (top.Count == 0) top.Add((0, "scale", "Equilibrado", "Nenhum traço se destaca; escolhe pelo conteúdo e pelo momento."));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 20);
        foreach (var (_, icon, title, text) in top)
        {
            var c = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
            c.AddThemeConstantOverride("separation", 4);
            var h = new HBoxContainer();
            h.AddThemeConstantOverride("separation", 8);
            h.AddChild(new Icon(icon, 16, UiTokens.AccentSoft));
            h.AddChild(ThemeFactory.Text(title, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.SemiBold));
            c.AddChild(h);
            var l = Wrap(text, UiTokens.FontSmall, UiTokens.TextSecondary);
            l.CustomMinimumSize = new Vector2(120, 0);
            c.AddChild(l);
            row.AddChild(c);
        }
        return Column(14, Divider(), row);
    }

    private Control FutureNote(string text)
    {
        var l = ThemeFactory.Text(text, UiTokens.FontBody, UiTokens.TextSecondary);
        l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return l;
    }

    private Control FutureTab(string title, string milestone, string text) =>
        Grid(12, (Card($"{title} · chega no {milestone}", FutureNote(text), info: $"{title}\n~Disponível quando o sistema existir na simulação ({milestone})."), 6), (new Control(), 6));

    // ----- Uso do tempo -----

    /// <summary>Divisao do tempo conectado por tipo de atividade (o resto e deslocamento): rosca + legenda.</summary>
    private Control TimeUseBody(CharacterDetailView? d)
    {
        if (d == null || d.PlayedTicks <= 0) return Wrap("Carregando…");
        long sum = d.ActivityTicks.Values.Sum();
        var items = d.ActivityTicks.Select(kv => (Type: kv.Key, Ticks: kv.Value)).ToList();
        items.Add(("travel", Math.Max(0, d.PlayedTicks - sum)));
        var list = items.Where(i => i.Ticks > 0).OrderByDescending(i => i.Ticks).Take(6)
            .Select(i => (Type: i.Type, Name: i.Type == "travel" ? "Deslocamento" : i.Type == "idle" ? "Esperando" : PtBr.ActivityType(i.Type),
                Pct: i.Ticks * 100f / d.PlayedTicks, Hours: Hours(i.Ticks),
                Color: i.Type == "travel" ? new Color("#5B6573") : ActivityPalette.Color(i.Type))).ToList();
        var donut = new DonutChart { CustomMinimumSize = new Vector2(124, 124), SizeFlagsVertical = SizeFlags.ShrinkCenter, Slices = list.Select(x => (x.Pct, x.Color)).ToList(), Center = $"{PtBr.Decimal(Hours(d.PlayedTicks))} h", Caption = "conectado" };
        var legend = Column(8);
        legend.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        foreach (var x in list)
        {
            var row = new TipPanel { TooltipText = $"{x.Name}\nTempo|{PtBr.Decimal(x.Hours)} h\nParcela|{PtBr.Decimal(x.Pct)}%", MouseFilter = MouseFilterEnum.Stop };
            row.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
            var r = new HBoxContainer();
            r.AddThemeConstantOverride("separation", 8);
            r.AddChild(new Icon(x.Type == "travel" ? "footprints" : TypeIcon(x.Type), 14, x.Color));
            var n = ThemeFactory.Text(x.Name, UiTokens.FontSmall, UiTokens.TextLabel, UiFonts.Medium);
            n.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            r.AddChild(n);
            r.AddChild(ThemeFactory.Text($"{x.Pct:0}%", UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Numbers));
            row.AddChild(r);
            legend.AddChild(row);
        }
        return Row(18, donut, legend);
    }

    private string ActivityTypeOf(string zone, string activity) =>
        _ctx.Content.Regions.TryGetValue(zone, out var r) ? r.FindActivity(activity)?.Type ?? "" : "";

    /// <summary>Icone Lucide por tipo de atividade.</summary>
    public static string TypeIcon(string type) => type switch
    {
        ActivityTypes.Hunt => "sword",
        ActivityTypes.Gather => "pickaxe",
        ActivityTypes.Boss => "crown",
        ActivityTypes.Quest => "scroll-text",
        ActivityTypes.Explore => "compass",
        ActivityTypes.Trade => "store",
        ActivityTypes.Repair => "hammer",
        ActivityTypes.Train => "dumbbell",
        ActivityTypes.Socialize => "beer",
        ActivityTypes.Recruit => "users",
        ActivityTypes.Patrol => "eye",
        ActivityTypes.Ambush => "skull",
        "idle" => "hourglass",
        _ => "activity",
    };

    // ----- Atividades recentes -----

    private Control RecentBody()
    {
        var col = Column(2);
        var events = _ctx.PersonHistory(_person.Id);
        if (events.Count == 0) { col.AddChild(Wrap("Nada desde que o jogo foi aberto.")); return col; }
        foreach (var e in events.Take(9)) col.AddChild(RecentRow(e));
        return col;
    }

    /// <summary>Linha de evento: hora, icone, texto; clicavel quando leva a uma pagina.</summary>
    private Control RecentRow(SimEvent e)
    {
        var (text, color) = Describe(e);
        string icon = "activity";
        Action? go = null;
        var parts = (e.Detail ?? "").Split('/');
        switch (e.Type)
        {
            case "activity_chosen":
                icon = parts.Length == 2 ? TypeIcon(ActivityTypeOf(parts[0], parts[1])) : "activity";
                if (parts.Length == 2 && _ctx.Content.Regions.TryGetValue(parts[0], out var z))
                {
                    string zid = parts[0];
                    var area = z.AreaOfActivity(parts[1]);
                    go = area != null ? () => _ctx.OpenArea(zid, area.Id) : () => _ctx.OpenZone(zid);
                }
                color = parts.Length == 2 ? ActivityPalette.Color(ActivityTypeOf(parts[0], parts[1])) : color;
                break;
            case "session_started": icon = "play"; break;
            case "session_ended": icon = "moon"; break;
            case "level_up": icon = "award"; break;
            case "goal_set": icon = "target"; break;
            case "goal_completed": icon = "trophy"; break;
            case "goal_abandoned": icon = "circle-x"; break;
            case "trait_changed": icon = "activity"; break;
            case "person_registered": icon = "user"; break;
            case "region_discovered": icon = "map"; { string zid = e.Detail ?? ""; go = () => _ctx.OpenZone(zid); } break;
            case "quest_accepted": icon = "scroll-text"; { string q = e.Detail ?? ""; go = () => _ctx.OpenQuest(q); } break;
            case "quest_step_completed": icon = "circle-check"; { string q = (e.Detail ?? "").Split('#')[0]; go = () => _ctx.OpenQuest(q); } break;
            case "quest_completed": icon = "medal"; { string q = e.Detail ?? ""; go = () => _ctx.OpenQuest(q); } break;
        }
        var b = HoverRow(go ?? (() => { }), go != null ? "Clique para abrir" : "");
        if (go == null) b.MouseDefaultCursorShape = CursorShape.Arrow;
        b.CustomMinimumSize = new Vector2(0, 30);
        var r = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        r.SetAnchorsPreset(LayoutPreset.FullRect);
        r.OffsetLeft = 8; r.OffsetRight = -8;
        r.AddThemeConstantOverride("separation", 12);
        var when = ThemeFactory.Text(_ctx.Clock(e.Tick), UiTokens.FontSmall, UiTokens.AxisText, UiFonts.Numbers);
        when.CustomMinimumSize = new Vector2(38, 0);
        when.VerticalAlignment = VerticalAlignment.Center;
        r.AddChild(when);
        r.AddChild(new Icon(icon, 16, color));
        var l = ThemeFactory.Text(text, UiTokens.FontBody, UiTokens.TextLabel);
        l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        l.VerticalAlignment = VerticalAlignment.Center;
        l.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        r.AddChild(l);
        if (go != null) r.AddChild(new Icon("chevron-right", 14, UiTokens.TextDisabled));
        b.AddChild(r);
        return b;
    }

    public static string FactorPt(string key) => key switch
    {
        "affinity" => "Gosto pela atividade",
        "level_fit" => "Adequada ao nível",
        "company" => "Companhia",
        "crowding" => "Lotação",
        "risk" => "Risco percebido",
        "travel" => "Distância",
        "time_budget" => "Tempo de sessão",
        "repetition" => "Cansaço de repetir",
        "noise" => "Variação do momento",
        "quest_offer" => "Missão para aceitar",
        "quest_deliver" => "Missão para entregar",
        "quest_step" => "Avança uma missão",
        "goal" => "Meta pessoal",
        "familiarity" => "Zona familiar",
        "challenge" => "Gosto por desafio",
        _ => key,
    };

    private static string FactorHint(string key) => key switch
    {
        "affinity" => "Traços da pessoa combinados com o tipo de atividade.",
        "level_fit" => "Conteúdo dentro da faixa de nível da zona rende mais.",
        "company" => "Outros jogadores no lugar (sociabilidade).",
        "crowding" => "Caça e coleta disputadas rendem menos.",
        "risk" => "Risco da atividade, PvP e nível × aversão a risco.",
        "travel" => "Tempo andando até lá em relação ao tempo de sessão.",
        "time_budget" => "Viagem + atividade não cabem no tempo restante.",
        "repetition" => "Repetir a mesma coisa cansa (menos para os pacientes).",
        "quest_offer" => "O quadro tem missão que a pessoa pode aceitar.",
        "quest_deliver" => "Há missões prontas para entregar no quadro (mais prontas, mais vontade de voltar).",
        "quest_step" => "Fazer isto avança a etapa atual de uma missão em andamento.",
        "goal" => "Ajuda uma meta de curto prazo; o peso cresce com o comprometimento (paciência e lealdade).",
        "familiarity" => "Zona onde já passou bastante tempo; quem gosta de variedade (exploração) sente menos essa atração.",
        "challenge" => "Conteúdo acima do nível: competitividade (agressividade), confiança (mecânica) e pouca aversão a risco.",
        _ => "Pequena aleatoriedade reproduzível.",
    };

    // ---------------- Historico (tabela) ----------------

    // ---------------- Missoes (D-031) ----------------

    private Control BuildQuests()
    {
        var v = View;
        if (v == null) return ThemeFactory.Text("Ainda sem personagem.", UiTokens.FontBody, UiTokens.TextSecondary);
        long xpTotal = v.XpFromQuests + v.XpFromActivities;
        var stats = Row(10,
            Stat(v.ActiveQuests.Count.ToString(), $"ativas (máx. {_ctx.Content.QuestSettings.MaxActivePerCharacter})", UiTokens.Info),
            Stat(PtBr.Int(v.CompletedQuests.Sum(c => c.Times)), "entregas", UiTokens.Success),
            Stat(PtBr.Int(v.CompletedQuests.Count(c => _ctx.Content.Quests.TryGetValue(c.QuestId, out var q) && q.Category == QuestCategories.Main)), "principais concluídas", UiTokens.Accent),
            Stat(xpTotal == 0 ? "–" : $"{v.XpFromQuests * 100 / xpTotal}%", "do XP veio de quests", UiTokens.AccentSoft));

        var active = Column(8);
        if (v.ActiveQuests.Count == 0)
            active.AddChild(ThemeFactory.Text("Nenhuma missão em andamento.", UiTokens.FontBody, UiTokens.TextSecondary));
        foreach (var p in v.ActiveQuests)
        {
            if (!_ctx.Content.Quests.TryGetValue(p.QuestId, out var q)) continue;
            bool ready = p.StepIndex >= q.Steps.Count;
            string stepText = ready ? "Pronta para entregar no quadro" : q.Steps[p.StepIndex].Text;
            float max = ready ? 1 : q.Steps[p.StepIndex].Minutes;
            float val = ready ? 1 : p.StepMinutes;
            string qid = q.Id;
            var line = Column(4,
                Action($"{QuestModal.CategoryName(q.Category)} · {q.Name}", "Abre a ficha da missão", () => { _ctx.OpenQuest(qid); }),
                Bar($"Etapa {Math.Min(p.StepIndex + 1, q.Steps.Count + 1)}/{q.Steps.Count + 1}", val, max, QuestModal.CategoryColor(q.Category),
                    ready ? "pronta" : $"{p.StepMinutes}/{q.Steps[p.StepIndex].Minutes} min", 110),
                ThemeFactory.Text(stepText, UiTokens.FontSmall, UiTokens.TextSecondary));
            active.AddChild(line);
        }

        var done = new HFlowContainer();
        done.AddThemeConstantOverride("h_separation", 6);
        done.AddThemeConstantOverride("v_separation", 6);
        foreach (var (id, times, _) in v.CompletedQuests.OrderBy(c => c.QuestId, StringComparer.Ordinal))
        {
            if (!_ctx.Content.Quests.TryGetValue(id, out var q)) continue;
            string qid = id;
            done.AddChild(Action(times > 1 ? $"{q.Name} ×{times}" : q.Name, QuestModal.CategoryName(q.Category), () => { _ctx.OpenQuest(qid); }));
        }
        if (v.CompletedQuests.Count == 0) done.AddChild(ThemeFactory.Text("Nenhuma missão concluída ainda.", UiTokens.FontBody, UiTokens.TextSecondary));
        return Column(12, stats, Card("Em andamento", active, UiTokens.Info), Card("Concluídas", done, UiTokens.Success));
    }

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
        return col;
    }

    private static Label ColHead(string text, float width)
    {
        var l = ThemeFactory.Text(text, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium);
        if (width > 0) l.CustomMinimumSize = new Vector2(width, 22);
        else l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        return l;
    }

    private Control EventRow(SimEvent e, bool compact)
    {
        var (text, color) = Describe(e);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 8);
        var when = ThemeFactory.Text(compact ? _ctx.Clock(e.Tick) : $"{PtBr.ShortDate(_ctx.Calendar.ToDateTime(e.Tick))} {_ctx.Clock(e.Tick)}", UiTokens.FontCaption, UiTokens.TextDisabled, UiFonts.Numbers);
        when.CustomMinimumSize = new Vector2(compact ? 38 : 106, 20);
        row.AddChild(when);
        row.AddChild(new ColorRect { Color = color, CustomMinimumSize = new Vector2(3, 12), SizeFlagsVertical = SizeFlags.ShrinkCenter });
        var l = ThemeFactory.Text(text, UiTokens.FontSmall, UiTokens.TextPrimary);
        l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        l.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        row.AddChild(l);
        return row;
    }

    /// <summary>Texto de um objetivo a partir do detalhe do evento ("tipo|alvo|valor").</summary>
    private string GoalDetail(string? detail)
    {
        var p = (detail ?? "").Split('|');
        if (p.Length != 3) return detail ?? "";
        return p[0] switch
        {
            Simulation.Core.Population.GoalTypes.ReachLevel => $"chegar ao nível {p[2]}",
            Simulation.Core.Population.GoalTypes.DiscoverZone => $"descobrir {_ctx.ZoneName(p[1])}",
            Simulation.Core.Population.GoalTypes.CompleteQuest => $"concluir {QuestName(p[1])}",
            _ => $"praticar {PtBr.ActivityType(p[1]).ToLowerInvariant()} por {p[2]} min",
        };
    }

    private string QuestName(string? id) => id != null && _ctx.Content.Quests.TryGetValue(id, out var q) ? q.Name : id ?? "";

    private (string, Color) Describe(SimEvent e)
    {
        switch (e.Type)
        {
            case "person_registered": return ($"Criou a conta: {e.Detail}", UiTokens.Accent);
            case "session_started": return ($"Conectou ({e.Detail})", UiTokens.Success);
            case "session_ended": return ("Desconectou", UiTokens.TextSecondary);
            case "level_up": return ($"Subiu para o {e.Detail}", UiTokens.Accent);
            case "region_discovered": return ($"Descobriu {_ctx.ZoneName(e.Detail ?? "")}", UiTokens.Info);
            case "activity_chosen":
                var p = (e.Detail ?? "/").Split('/');
                string why = e.Causes.Count > 0 ? " — " + string.Join(", ", e.Causes.Take(2).Select(c => FactorPt(c.Factor).ToLowerInvariant())) : "";
                return ($"{_ctx.ActivityName(p[0], p[1])} · {_ctx.PlaceName(p[0], p[1])}{why}", UiTokens.Info);
            case "quest_accepted": return ($"Aceitou a missão {QuestName(e.Detail)}", UiTokens.Accent);
            case "quest_step_completed": return ($"Cumpriu uma etapa de {QuestName((e.Detail ?? "").Split('#')[0])}", UiTokens.Info);
            case "quest_completed": return ($"Entregou a missão {QuestName(e.Detail)}", UiTokens.Success);
            case "goal_set": return ($"Nova meta: {GoalDetail(e.Detail)}", UiTokens.Accent);
            case "goal_completed": return ($"Concluiu a meta: {GoalDetail(e.Detail)}", UiTokens.Success);
            case "goal_abandoned": return ($"Desistiu da meta: {GoalDetail(e.Detail)}{(e.Causes.Count > 0 && e.Causes[0].Factor == "impatience" ? " (impaciência)" : " (sem progresso)")}", UiTokens.TextSecondary);
            case "trait_changed":
            {
                var parts = (e.Detail ?? "").Split(':');
                if (parts.Length != 3) return (e.Type, UiTokens.TextSecondary);
                bool up = int.Parse(parts[2]) > int.Parse(parts[1]);
                string cause = e.Causes.Count > 0 ? AttributeInfo.CausePt(e.Causes[0].Factor) : "";
                return ($"{AttributeInfo.Name(parts[0])} {(up ? "subiu" : "caiu")} para {parts[2]} ({cause})", up ? UiTokens.Success : UiTokens.Danger);
            }
            default: return (e.Type, UiTokens.TextSecondary);
        }
    }

    // ---------------- Rotina ----------------

    private Control BuildRoutine()
    {
        var t = _person.Traits;
        var routine = Column(4,
            KeyValue("Horário preferido", $"{t.PreferredStartHour:00}h"),
            KeyValue("Sessão típica", $"{t.SessionMinutes} min"),
            KeyValue("Dias por semana", $"{t.DaysPerWeek}/7"),
            KeyValue("A mais no fim de semana", $"+{t.WeekendBoost / 10}%"));
        int pay = UiTokens.To20(t.WillingnessToPay), adv = UiTokens.To20(t.PaidAdvantageSensitivity);
        var money = Column(4,
            KeyValue("Orçamento de lazer", $"R$ {PtBr.Decimal(t.LeisureBudgetCentsPerMonth / 100.0, 0)}/mês"),
            KeyValue("Disposição a pagar", $"{pay}/20", UiTokens.AttributeColor(pay)),
            KeyValue("Aversão a vantagem paga", $"{adv}/20", UiTokens.AttributeColor(adv)),
            Wrap("Dinheiro real fictício do tycoon, separado do ouro do MMORPG. Usado pela monetização no M7.", UiTokens.FontCaption, UiTokens.TextDisabled));
        return Row(12, Card("Rotina", routine, UiTokens.Info), Card("Finanças", money, UiTokens.Accent));
    }

    private static Label Wrap(string text, int size = UiTokens.FontSmall, Color? color = null, Font? font = null)
    {
        var l = ThemeFactory.Text(text, size, color ?? UiTokens.TextSecondary, font);
        l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        l.CustomMinimumSize = new Vector2(200, 0);
        return l;
    }

    public static string RolePt(string? role) => role switch
    {
        ClassRoles.Tank => "Tanque",
        ClassRoles.Damage => "Dano",
        ClassRoles.Support => "Suporte",
        _ => "",
    };

    public static Color RoleColor(string? role) => role switch
    {
        ClassRoles.Tank => new Color("#7FB8EE"),
        ClassRoles.Damage => new Color("#EE6A63"),
        ClassRoles.Support => new Color("#6FDB93"),
        _ => UiTokens.TextSecondary,
    };
}

/// <summary>Barra divergente: positivo para a direita, negativo para a esquerda.</summary>
public partial class DivergingTrack : Control
{
    private readonly float _value;
    private readonly Color _color;

    public DivergingTrack(float value, Color color)
    {
        _value = Mathf.Clamp(value, -1, 1);
        _color = color;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    public override void _Draw()
    {
        float mid = Size.X / 2f, h = 5, y = (Size.Y - h) / 2f;
        DrawRect(new Rect2(0, y, Size.X, h), new Color(UiTokens.Background, 0.9f));
        DrawLine(new Vector2(mid, y - 2), new Vector2(mid, y + h + 2), UiTokens.BorderStrong, 1);
        float w = mid * Mathf.Abs(_value);
        DrawRect(_value >= 0 ? new Rect2(mid, y, w, h) : new Rect2(mid - w, y, w, h), _color);
    }
}

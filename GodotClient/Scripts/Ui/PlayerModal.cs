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
        AddTab("Perfil", BuildProfile, live: true);
        AddTab("Missões", BuildQuests, live: true);
        AddTab("Histórico", BuildHistory, live: true);
        AddTab("Rotina e finanças", BuildRoutine);
        AddTab("Inventário · M5", () => FutureTab("Inventário", "Itens, equipamento e carga do personagem aparecem aqui quando existirem inventário e itens na simulação (M5). As recompensas de missões já registram os itens previstos."));
        AddTab("Banco · M5", () => FutureTab("Banco", "Ouro guardado, depósitos e movimentações aparecem aqui com a economia (M5). Hoje o ouro de missões está definido, mas ainda não é aplicado."));
    }

    private CharacterView? View => _ctx.Snapshot().ByPerson is { } a && _person.Id < a.Length ? a[_person.Id] : null;
    private string ProfileName => _ctx.Content.Profiles.TryGetValue(_person.ProfileId, out var p) ? p.Name : _person.ProfileId;

    private void BuildHeader()
    {
        var v = View;
        var color = UiTokens.ProfileColor(_person.ProfileId);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 14);
        row.AddChild(new PortraitBadge(_person.AppearanceSeed, color, v?.Name ?? _person.Name) { CustomMinimumSize = new Vector2(60, 60), SizeFlagsVertical = SizeFlags.ShrinkCenter });
        var names = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        names.AddThemeConstantOverride("separation", 0);
        names.AddChild(ThemeFactory.Text(v?.Name ?? _person.Name, 24, UiTokens.TextPrimary, UiFonts.Title));
        var cls = v != null && _ctx.Content.Classes.TryGetValue(v.ClassId, out var c) ? c : null;
        string line = v == null ? $"{_person.Name} · ainda sem conta" : $"{_person.Name} · {cls?.Name} ({RolePt(cls?.Role)}) · Pessoa #{_person.Id}";
        names.AddChild(ThemeFactory.Text(line, UiTokens.FontBody, UiTokens.TextSecondary));
        row.AddChild(names);
        Header.AddChild(row);

        if (v != null)
        {
            bool online = v.Status != CharacterStatus.Offline;
            var prog = _ctx.Content.Progression;
            string stage = (ProgressionRules.StageRate(prog, v.Level) / 1000.0).ToString("0.##", CultureInfo.GetCultureInfo("pt-BR"));
            Fact("Nível", v.Level.ToString(), UiTokens.Info, v.Level >= prog.LevelCap
                ? $"Nível {v.Level}\n~Nível máximo atual ({prog.LevelCap})."
                : $"Nível {v.Level}\nXP|{v.Experience} / {ProgressionRules.XpToNextLevel(prog, v.Level)}\nTaxa da faixa|×{stage}\n~Faixas de nível mais altas rendem menos XP.");
            Fact("Classe", cls?.Name ?? v.ClassId, RoleColor(cls?.Role));
            Fact("Perfil", ProfileName, color, $"Perfil {ProfileName}\n~Tendência de origem; a pessoa não é um clone do perfil.");
            Fact("Status", online ? "Conectado" : "Desconectado", online ? UiTokens.Success : UiTokens.TextSecondary);
            Fact("Zona", _ctx.ZoneName(v.RegionId));
            Fact("Conhece", $"{v.KnownRegions}/{_ctx.Content.Regions.Count} zonas", null, "Zonas conhecidas\n~Só zonas conhecidas entram nas decisões. Viajar e explorar revelam mais.");
            if (online) Fact("Sessão", $"até {_ctx.Clock(v.SessionEndTick)}", UiTokens.AccentSoft);
        }
        else Fact("Status", "Interessado (sem conta)", UiTokens.Warning);

        bool on = v != null && v.Status != CharacterStatus.Offline;
        Actions.AddChild(Action("Seguir no mapa", "Seguir no mapa\n~Centraliza e acompanha este jogador.", () => { _ctx.FocusPerson(_person.Id, true); Close(); }, primary: true, enabled: on));
        Actions.AddChild(Action("Localizar", "Localizar\n~Centraliza o mapa no jogador.", () => { _ctx.FocusPerson(_person.Id, false); Close(); }, enabled: on));
        _favorite = Action(FavText(), "Favoritar\n~Acompanha este jogador (favoritos desta sessão).", ToggleFavorite);
        Actions.AddChild(_favorite);
        if (v != null && v.Status == CharacterStatus.Doing && v.ActivityId.Length > 0
            && _ctx.Content.Regions[v.TargetRegionId].AreaOfActivity(v.ActivityId) is { } area)
        {
            string z = v.TargetRegionId, a = area.Id;
            Actions.AddChild(Action("Ver área", $"{area.Name}\n~Abre a ficha da área onde está.", () => { _ctx.OpenArea(z, a); }));
        }
        else if (v != null) Actions.AddChild(Action("Ver zona", "Ver zona\n~Abre a ficha da zona atual.", () => { _ctx.OpenZone(v.RegionId); }));
    }

    private string FavText() => _ctx.Favorites.Contains(_person.Id) ? "★ Favorito" : "☆ Favoritar";

    private void ToggleFavorite()
    {
        if (!_ctx.Favorites.Remove(_person.Id)) _ctx.Favorites.Add(_person.Id);
        _favorite.Text = FavText();
    }

    // ---------------- Perfil (grade de cards, D-034) ----------------

    private Control BuildProfile()
    {
        var v = View;
        if (v == null)
            return Column(12, Grid(12, (Card("Atributos", AttributeGrid(), info: AttrInfo), 2f), (Card("Situação", Wrap("Ainda não criou conta no MMORPG; pode se registrar nos próximos dias do lançamento.")), 1f)));
        var detail = _ctx.Snapshot().Watched is { } d && d.PersonId == _person.Id ? d : null;
        var colA = Column(12,
            Card("Agora", NowBody(v), info: "O que está fazendo\n~Atualiza ao vivo no mapa; aqui o tempo está pausado."),
            Card("Progresso", ProgressBody(v), info: "Progresso do personagem\n~XP até o próximo nível e a taxa da faixa de nível (faixas altas rendem menos XP)."));
        var row1 = Grid(12,
            (colA, 1.1f),
            (Card("Atributos", Column(14, AttributeGrid(), Highlights()), info: AttrInfo), 2.2f),
            (Card("Por que escolheu isso", DecisionBody(v), info: "Última decisão\n~Cada fator soma ou tira pontos da opção; vence a maior nota. Os pesos vêm dos traços da pessoa e do conteúdo."), 1.3f));
        var row2 = Grid(12,
            (Card("Ritmo de evolução", LevelChart(detail), info: "Horas jogadas ao atingir cada nível\n~Curva real deste personagem (tempo conectado acumulado)."), 1f),
            (Card("Minutos jogados por dia", PlayChart(), info: "Minutos conectados por dia\n~Desde o cadastro, até 14 dias. A linha tracejada é a média dos dias jogados."), 1f),
            (Card("Uso do tempo conectado", TimeUseBody(detail), info: "Divisão do tempo conectado\n~Por tipo de atividade desde o cadastro; o resto é deslocamento."), 1f),
            (Card("Missões", QuestSummary(v), info: "Missões do personagem\n~Detalhes na aba Missões."), 1f));
        var row3 = Grid(12,
            (Card("Atividades recentes", RecentBody(), info: "Últimos acontecimentos\n~Desde que o jogo foi aberto. Histórico completo na aba Histórico."), 1.5f),
            (Card("Combate · últimos abates", KillsBody(), info: "Últimos abates e mortes\n~O combate chega no M3; nada é inventado antes disso."), 1f));
        return Column(12, row1, row2, row3);
    }

    private const string AttrInfo = "Atributos da pessoa (1 a 20)\n~Estilo de jogo e personalidade guiam as escolhas; capacidade pesa no combate (M3); conduta, nos incidentes (M9).";

    /// <summary>Tabela de abates com estado vazio honesto ate o combate existir.</summary>
    private Control KillsBody()
    {
        var head = new HBoxContainer();
        foreach (var (t, w) in new[] { ("Quando", 80f), ("Alvo", 0f), ("Local", 160f), ("Resultado", 90f) })
        {
            var l = ThemeFactory.Text(t, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium);
            if (w > 0) l.CustomMinimumSize = new Vector2(w, 0); else l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            head.AddChild(l);
        }
        var empty = Wrap("Sem combate na simulação ainda. Abates, mortes, saque e rivais aparecem aqui no M3.", UiTokens.FontSmall, UiTokens.TextDisabled);
        empty.HorizontalAlignment = HorizontalAlignment.Center;
        empty.SizeFlagsVertical = SizeFlags.ExpandFill;
        empty.VerticalAlignment = VerticalAlignment.Center;
        return Column(8, head, new ColorRect { Color = UiTokens.Border, CustomMinimumSize = new Vector2(0, 1) }, empty);
    }

    /// <summary>Resumo das missoes: numeros e as ativas com a etapa atual.</summary>
    private Control QuestSummary(CharacterView v)
    {
        long xpTotal = v.XpFromQuests + v.XpFromActivities;
        var nums = Row(0,
            MiniStat(v.ActiveQuests.Count.ToString(), "Ativas"),
            MiniStat(PtBr.Int(v.CompletedQuests.Sum(c => c.Times)), "Entregues"),
            MiniStat(xpTotal == 0 ? "–" : $"{v.XpFromQuests * 100 / xpTotal}%", "Do XP"));
        var col = Column(8, nums);
        foreach (var p in v.ActiveQuests.Take(3))
        {
            if (!_ctx.Content.Quests.TryGetValue(p.QuestId, out var q)) continue;
            bool ready = p.StepIndex >= q.Steps.Count;
            col.AddChild(Bar(q.Name, ready ? 1 : p.StepMinutes, ready ? 1 : q.Steps[p.StepIndex].Minutes, ready ? UiTokens.Success : UiTokens.Info,
                ready ? "entregar" : $"etapa {p.StepIndex + 1}", 130));
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

    /// <summary>Tracos marcantes derivados dos atributos (o que mais pesa nas escolhas).</summary>
    private Control Highlights()
    {
        var t = _person.Traits;
        var candidates = new List<(int Score, string Title, string Text)>();
        void High(int value, string title, string text) { int v20 = UiTokens.To20(value); if (v20 >= 14) candidates.Add((v20, title, text)); }
        void Low(int value, string title, string text) { int v20 = UiTokens.To20(value); if (v20 <= 6) candidates.Add((27 - v20, title, text)); }
        High(t.Pve, "Caçador", "Prefere caçar criaturas; áreas de caça pesam mais nas escolhas.");
        High(t.Pvp, "Gosta de PvP", "Procura combate contra jogadores (o PvP chega no M3).");
        High(t.Trade, "Mercador", "Gosta de coletar, comprar e vender.");
        High(t.Exploration, "Explorador", "Tende a descobrir zonas novas cedo.");
        High(t.Socializing, "Sociável", "Gosta de lugares com gente e de estar em grupo.");
        High(t.Patience, "Paciente", "Repete a mesma atividade sem cansar tão cedo.");
        High(t.RiskAversion, "Cauteloso", "Evita perigo, PvP e conteúdo acima do nível.");
        High(t.Loyalty, "Leal", "Tende a ficar com grupos e guildas (M6).");
        Low(t.RiskAversion, "Destemido", "Aceita risco; encara zonas perigosas.");
        Low(t.Patience, "Inquieto", "Cansa de repetir e troca de atividade logo.");
        Low(t.Sociability, "Reservado", "Prefere lugares vazios e jogar sozinho.");
        var top = candidates.OrderByDescending(c => c.Score).Take(3).ToList();
        if (top.Count == 0) top.Add((0, "Equilibrado", "Nenhum traço se destaca; escolhe pelo conteúdo e pelo momento."));
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 16);
        foreach (var (_, title, text) in top)
        {
            var c = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsStretchRatio = 1 };
            c.AddThemeConstantOverride("separation", 2);
            var h = new HBoxContainer();
            h.AddThemeConstantOverride("separation", 6);
            h.AddChild(new ColorRect { Color = UiTokens.AccentSoft, CustomMinimumSize = new Vector2(6, 6), SizeFlagsVertical = SizeFlags.ShrinkCenter });
            h.AddChild(ThemeFactory.Text(title, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.SemiBold));
            c.AddChild(h);
            var l = Wrap(text, UiTokens.FontCaption, UiTokens.TextSecondary);
            l.CustomMinimumSize = new Vector2(120, 0);
            c.AddChild(l);
            row.AddChild(c);
        }
        return Column(10, new ColorRect { Color = UiTokens.Border, CustomMinimumSize = new Vector2(0, 1) }, row);
    }

    private Control FutureNote(string text)
    {
        var l = ThemeFactory.Text(text, UiTokens.FontSmall, UiTokens.TextSecondary);
        l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return l;
    }

    private Control FutureTab(string title, string text) =>
        Column(12, Card($"{title} · disponível no M5", FutureNote(text), UiTokens.TextSecondary));

    /// <summary>Minutos conectados por dia (desde o cadastro, ate 14 dias), em colunas.</summary>
    private Control PlayChart()
    {
        var events = _ctx.PersonHistory(_person.Id);
        var now = _ctx.Snapshot().Tick;
        long tpd = Simulation.Core.Time.SimTime.TicksPerDay;
        long today = now / tpd;
        long first = today;
        foreach (var e in events) if (e.Type is "person_registered" or "session_started") first = Math.Min(first, e.Tick / tpd);
        int days = (int)Math.Clamp(today - first + 1, 1, 14);
        var minutes = new float[days];
        long? end = null;
        foreach (var e in events)   // mais novos primeiro
        {
            if (e.Type == "session_ended") end = e.Tick;
            else if (e.Type == "session_started")
            {
                long stop = end ?? now;
                int idx = (int)(days - 1 - (today - e.Tick / tpd));
                if (idx >= 0 && idx < days) minutes[idx] += (stop - e.Tick) / (float)Simulation.Core.Time.SimTime.TicksPerMinute;
                end = null;
            }
        }
        var labels = Enumerable.Range(0, days).Select(i =>
        {
            var d = _ctx.Calendar.ToDateTime(Math.Max(0, today - days + 1 + i) * tpd);
            return $"{d.Day:00}/{d.Month:00}";
        }).ToList();
        float avg = minutes.Where(m => m > 0).DefaultIfEmpty(0).Average();
        var chart = new ColumnChart
        {
            CustomMinimumSize = new Vector2(0, 130), Values = minutes, Labels = labels, Unit = " min",
            ColorOf = m => m >= avg ? UiTokens.Success : new Color(UiTokens.Success, 0.55f),
            EmptyText = "Sem sessões registradas desde que o jogo foi aberto.",
        };
        var foot = Row(0, MiniStat($"{avg:0}", "Média (min)"), MiniStat(minutes.Count(m => m > 0).ToString(), "Dias jogados"), MiniStat($"{minutes.Sum() / 60f:0.#}".Replace('.', ','), "Horas no período"));
        return Column(10, chart, foot);
    }

    /// <summary>Divisao do tempo conectado por tipo de atividade (o resto e deslocamento): rosca + legenda.</summary>
    private Control TimeUseBody(CharacterDetailView? d)
    {
        if (d == null || d.PlayedTicks <= 0) return Wrap("Coletando dados…");
        long sum = d.ActivityTicks.Values.Sum();
        var items = d.ActivityTicks.Select(kv => (Type: kv.Key, Ticks: kv.Value)).ToList();
        items.Add(("travel", Math.Max(0, d.PlayedTicks - sum)));
        var list = items.Where(i => i.Ticks > 0).OrderByDescending(i => i.Ticks).Take(6)
            .Select(i => (Name: i.Type == "travel" ? "Deslocamento" : i.Type == "idle" ? "Esperando" : PtBr.ActivityType(i.Type),
                Pct: i.Ticks * 100f / d.PlayedTicks, Hours: i.Ticks / (double)Simulation.Core.Time.SimTime.TicksPerHour,
                Color: i.Type == "travel" ? new Color("#5B6573") : ActivityPalette.Color(i.Type))).ToList();
        double total = d.PlayedTicks / (double)Simulation.Core.Time.SimTime.TicksPerHour;
        var donut = new DonutChart { CustomMinimumSize = new Vector2(112, 112), SizeFlagsVertical = SizeFlags.ShrinkCenter, Slices = list.Select(x => (x.Pct, x.Color)).ToList(), Center = $"{PtBr.Decimal(total)} h", Caption = "conectado" };
        var legend = Column(5);
        legend.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        foreach (var x in list)
        {
            var row = new TipPanel { TooltipText = $"{x.Name}\nTempo|{PtBr.Decimal(x.Hours)} h\nParcela|{PtBr.Decimal(x.Pct)}%", MouseFilter = MouseFilterEnum.Stop };
            row.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
            var r = new HBoxContainer();
            r.AddThemeConstantOverride("separation", 8);
            r.AddChild(new ColorRect { Color = x.Color, CustomMinimumSize = new Vector2(8, 8), SizeFlagsVertical = SizeFlags.ShrinkCenter });
            var n = ThemeFactory.Text(x.Name, UiTokens.FontSmall, UiTokens.TextPrimary);
            n.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            r.AddChild(n);
            r.AddChild(ThemeFactory.Text($"{x.Pct:0}%", UiTokens.FontSmall, UiTokens.TextSecondary, UiFonts.Numbers));
            row.AddChild(r);
            legend.AddChild(row);
        }
        return Row(16, donut, legend);
    }

    /// <summary>Horas jogadas ate cada nivel (curva real do personagem).</summary>
    private Control LevelChart(CharacterDetailView? d)
    {
        if (d == null || d.LevelTicks.Count == 0) return Wrap("Ainda não subiu de nível.");
        var hours = d.LevelTicks.Select(t => (float)(t / (double)Simulation.Core.Time.SimTime.TicksPerHour)).ToList();
        var chart = new LineChart
        {
            CustomMinimumSize = new Vector2(0, 176),
            XLabels = Enumerable.Range(2, hours.Count).Select(l => $"Nv {l}").ToList(),
            Series = new[] { new ChartSeries { Name = "Horas até o nível", Color = UiTokens.Info, Values = hours } },
            TooltipText = "Cada ponto é o tempo conectado acumulado quando o personagem chegou ao nível.",
        };
        return chart;
    }

    /// <summary>Cor da barra de atributo: excepcional, bom, medio, fraco.</summary>
    private static Color AttrTier(int v20) => v20 >= 17 ? UiTokens.Accent : v20 >= 14 ? UiTokens.Success : v20 >= 9 ? new Color("#7D8693") : v20 >= 6 ? new Color("#525A66") : UiTokens.Danger;

    private Control AttributeGrid()
    {
        var t = _person.Traits;
        Control Group(string title, (string Name, int Value, string Hint)[] items)
        {
            var col = Column(7);
            col.AddChild(ThemeFactory.Text(title, UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.SemiBold));
            foreach (var (name, value, hint) in items)
            {
                int v20 = UiTokens.To20(value);
                var cell = new TipPanel { TooltipText = $"{name}\nValor|{v20} / 20\nBruto|{value} / 1000\n~{hint}", MouseFilter = MouseFilterEnum.Stop };
                cell.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
                var c = Column(3);
                var r = new HBoxContainer();
                var l = ThemeFactory.Text(name, UiTokens.FontSmall, UiTokens.TextSecondary);
                l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                r.AddChild(l);
                r.AddChild(ThemeFactory.Text(v20.ToString(), UiTokens.FontBody, v20 >= 14 ? AttrTier(v20) : v20 <= 5 ? UiTokens.Danger : UiTokens.TextPrimary, UiFonts.Numbers));
                c.AddChild(r);
                c.AddChild(new ThinBar(v20 / 20f, AttrTier(v20)) { CustomMinimumSize = new Vector2(0, 3) });
                cell.AddChild(c);
                col.AddChild(cell);
            }
            return col;
        }
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 22);
        foreach (var g in new[]
        {
            Group("Estilo de jogo", new[] { ("PvE", t.Pve, "Gosto por caçar e enfrentar criaturas."), ("PvP", t.Pvp, "Gosto por combate contra jogadores."), ("Comércio", t.Trade, "Gosto por comprar, vender e coletar."), ("Exploração", t.Exploration, "Gosto por descobrir lugares."), ("Socializar", t.Socializing, "Gosto por conversar e estar em grupo.") }),
            Group("Personalidade", new[] { ("Sociabilidade", t.Sociability, "Facilidade de criar vínculos."), ("Agressividade", t.Aggressiveness, "Tendência a confrontar."), ("Paciência", t.Patience, "Tolera repetição e espera."), ("Lealdade", t.Loyalty, "Fica com grupos e guildas."), ("Aversão a risco", t.RiskAversion, "Evita PvP, perdas e perigo.") }),
            Group("Capacidade", new[] { ("Mecânica", t.MechanicalSkill, "Execução e reflexo em combate."), ("Conhecimento", t.GameKnowledge, "Entende sistemas e rotas."), ("Coordenação", t.Coordination, "Joga bem em grupo.") }),
            Group("Conduta", new[] { ("Trapaça", t.CheatPropensity, "Propensão a trapacear (incidentes no M9)."), ("Oportunismo", t.Opportunism, "Aproveita vantagens sobre outros.") }),
        })
        {
            g.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(g);
        }
        return row;
    }

    private Control NowBody(CharacterView v)
    {
        long now = _ctx.Snapshot().Tick;
        var col = Column(6);
        col.AddChild(Wrap(StatusText.Of(v, _ctx.Content), 15, UiTokens.TextPrimary, UiFonts.SemiBold));
        if (v.Status == CharacterStatus.Doing && v.ActivityEndTick > v.ActivityStartTick)
        {
            float total = v.ActivityEndTick - v.ActivityStartTick, done = Math.Clamp(now - v.ActivityStartTick, 0, (long)total);
            col.AddChild(Bar("Atividade", done, total, UiTokens.Accent, $"{(int)(done * 100 / total)}%", 90));
            col.AddChild(KeyValue("Termina às", _ctx.Clock(v.ActivityEndTick)));
        }
        else if (v.Status == CharacterStatus.Traveling && v.MoveEndTick > v.MoveStartTick)
        {
            float total = v.MoveEndTick - v.MoveStartTick, done = Math.Clamp(now - v.MoveStartTick, 0, (long)total);
            col.AddChild(Bar("Trecho", done, total, UiTokens.Info, $"{(int)(done * 100 / total)}%", 90));
        }
        col.AddChild(KeyValue("Zona atual", _ctx.ZoneName(v.RegionId)));
        if (v.Status != CharacterStatus.Offline)
        {
            col.AddChild(KeyValue("Conectou às", _ctx.Clock(v.SessionStartTick)));
            col.AddChild(KeyValue("Sai às (previsto)", _ctx.Clock(v.SessionEndTick)));
            col.AddChild(KeyValue("Restante", $"{Math.Max(0, (v.SessionEndTick - now) / Simulation.Core.Time.SimTime.TicksPerMinute)} min", UiTokens.AccentSoft));
        }
        return col;
    }

    private Control ProgressBody(CharacterView v)
    {
        var prog = _ctx.Content.Progression;
        long need = ProgressionRules.XpToNextLevel(prog, v.Level);
        bool max = v.Level >= prog.LevelCap;
        var ring = new RingGauge
        {
            CustomMinimumSize = new Vector2(86, 86), SizeFlagsVertical = SizeFlags.ShrinkCenter,
            Value = max ? 1 : v.Experience / (float)Math.Max(1, need), Color = UiTokens.Success,
            Center = max ? "máx." : $"{v.Experience * 100 / Math.Max(1, need)}%", Caption = max ? "" : $"do Nv {v.Level + 1}",
        };
        string stage = (ProgressionRules.StageRate(prog, v.Level) / 1000.0).ToString("0.##", CultureInfo.GetCultureInfo("pt-BR"));
        var data = Column(5,
            KeyValue("Nível", $"{v.Level} de {prog.LevelCap}"),
            KeyValue("XP", max ? PtBr.Int(v.Experience) : $"{PtBr.Int(v.Experience)} / {PtBr.Int(need)}"),
            KeyValue("Taxa da faixa", $"×{stage}"),
            KeyValue("Zonas conhecidas", $"{v.KnownRegions} de {_ctx.Content.Regions.Count}"));
        data.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        return Row(16, ring, data);
    }

    private Control DecisionBody(CharacterView v)
    {
        var d = v.LastDecision;
        if (d == null) return Wrap("Nenhuma decisão registrada ainda.");
        var col = Column(5);
        col.AddChild(Wrap(_ctx.ActivityName(d.RegionId, d.ActivityId), 15, UiTokens.TextPrimary, UiFonts.SemiBold));
        string place = _ctx.PlaceName(d.RegionId, d.ActivityId) == _ctx.ZoneName(d.RegionId) ? _ctx.ZoneName(d.RegionId) : $"{_ctx.PlaceName(d.RegionId, d.ActivityId)} · {_ctx.ZoneName(d.RegionId)}";
        col.AddChild(Wrap($"{place} · às {_ctx.Clock(d.Tick)} · {d.OptionsConsidered} opções", UiTokens.FontCaption));
        var factors = d.Factors.Where(f => f.Contribution != 0).OrderByDescending(f => Math.Abs(f.Contribution)).ToList();
        int max = Math.Max(1, factors.Count == 0 ? 1 : factors.Max(f => Math.Abs(f.Contribution)));
        foreach (var f in factors)
        {
            var row = new TipPanel { TooltipText = $"{FactorPt(f.Key)}\nPeso|{(f.Contribution > 0 ? "+" : "")}{f.Contribution}\n~{FactorHint(f.Key)}", MouseFilter = MouseFilterEnum.Stop };
            row.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
            var r = new HBoxContainer();
            r.AddThemeConstantOverride("separation", 8);
            var l = ThemeFactory.Text(FactorPt(f.Key), UiTokens.FontSmall, UiTokens.TextPrimary);
            l.CustomMinimumSize = new Vector2(150, 0);
            r.AddChild(l);
            r.AddChild(new DivergingTrack(f.Contribution / (float)max, f.Contribution > 0 ? UiTokens.Success : UiTokens.Danger) { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 12) });
            var n = ThemeFactory.Text((f.Contribution > 0 ? "+" : "") + f.Contribution, UiTokens.FontSmall, f.Contribution > 0 ? UiTokens.Success : UiTokens.Danger, UiFonts.Numbers);
            n.CustomMinimumSize = new Vector2(36, 0);
            n.HorizontalAlignment = HorizontalAlignment.Right;
            r.AddChild(n);
            row.AddChild(r);
            col.AddChild(row);
        }
        col.AddChild(KeyValue("Nota final", d.Utility.ToString()));
        if (d.RunnerUpActivityKey.Length > 0)
        {
            var p = d.RunnerUpActivityKey.Split('/');
            col.AddChild(KeyValue("2ª opção", _ctx.ActivityName(p[0], p[1]), UiTokens.TextSecondary));
        }
        return col;
    }

    private Control RecentBody()
    {
        var col = Column(3);
        var events = _ctx.PersonHistory(_person.Id);
        if (events.Count == 0) { col.AddChild(Wrap("Nada desde que o jogo foi aberto.")); return col; }
        foreach (var e in events.Take(8)) col.AddChild(EventRow(e, compact: true));
        return col;
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
        foreach (var (id, times) in v.CompletedQuests.OrderBy(c => c.QuestId, StringComparer.Ordinal))
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
        var l = ThemeFactory.Text(text.ToUpperInvariant(), 10, UiTokens.TextSecondary, UiFonts.CondensedBold);
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

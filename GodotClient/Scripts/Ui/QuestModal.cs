using System.Globalization;
using Godot;
using Simulation.Core.Behavior;
using Simulation.Core.Content;
using Simulation.Core.Time;
using GodotClient.Sim;

namespace GodotClient.Ui;

/// <summary>
/// Ficha da quest (estilo FM): descricao, objetivos (etapas), requisitos, recompensas e os
/// numeros da campanha (quantos aceitaram, concluiram, tempo medio, quem esta fazendo agora).
/// Ouro e itens aparecem definidos, mas marcados como ainda nao implementados (M5).
/// </summary>
public partial class QuestModal : ModalFrame
{
    private readonly ClientContext _ctx;
    private readonly QuestDefinition _quest;
    private readonly QuestBoardDefinition? _board;
    private DataTable<(int Id, CharacterView V, QuestProgressView P)>? _doingTable;

    public QuestModal(ClientContext ctx, string questId) : base(CategoryColor(ctx.Content.Quests[questId].Category))
    {
        _ctx = ctx;
        _quest = ctx.Content.Quests[questId];
        ctx.Content.QuestBoards.TryGetValue(_quest.BoardId, out _board);
        BuildHeader();
        Breadcrumb = _board != null
            ? $"Mundo › {ctx.ZoneName(_board.RegionId)} › {_board.Name} › {_quest.Name}"
            : $"Missões › {_quest.Name}";
        // Paginas em telas (D-068). Objetivos, Requisitos e recompensas e Dados repetiam a Visao geral: o que so
        // elas tinham (aviso de PvP na etapa, limite do quadro, missoes que esta libera, raridade dos itens)
        // entrou na Visao geral.
        AddTab("Visão geral", BuildOverview, live: true);
        AddTab($"Concluíram · {Completers().Count}", BuildCompleters, live: true);
    }

    public static Color CategoryColor(string category) => category switch
    {
        QuestCategories.Main => UiTokens.Accent,
        QuestCategories.Daily => UiTokens.Success,
        _ => UiTokens.Info,
    };

    public static string CategoryName(string category) => category switch
    {
        QuestCategories.Main => "Principal",
        QuestCategories.Daily => "Diária",
        _ => "Secundária",
    };

    public static string Repetition(QuestDefinition q) => q.Category switch
    {
        QuestCategories.Main => "Uma vez por personagem",
        QuestCategories.Daily => "Renova todo dia (limite diário do quadro)",
        _ => q.RepeatCooldownDays > 0 ? $"Repetível a cada {q.RepeatCooldownDays} dias" : "Uma vez por personagem",
    };

    /// <summary>Repeticao em poucas palavras (cabecalho); a frase inteira fica no tooltip.</summary>
    public static string RepetitionShort(QuestDefinition q) => q.Category switch
    {
        QuestCategories.Main => "Uma vez por personagem",
        QuestCategories.Daily => "Renova todo dia",
        _ => q.RepeatCooldownDays > 0 ? $"A cada {q.RepeatCooldownDays} dias" : "Uma vez por personagem",
    };

    private QuestStatsView? Stats => _ctx.Snapshot().QuestStats.TryGetValue(_quest.Id, out var s) ? s : null;

    private void BuildHeader()
    {
        // Cabecalho (D-068): emblema da categoria; titulo e o texto que identifica em duas colunas alinhadas;
        // ao lado, os numeros da missao em etiquetas.
        var color = CategoryColor(_quest.Category);
        string icon = GameIcons.Quest(_quest.Category);
        Art.AddChild(new EmblemBox(icon, color) { CustomMinimumSize = new Vector2(92, 92) });
        Header.AddChild(TitleText(_quest.Name));
        Header.CustomMinimumSize = new Vector2(410, 0);   // largura fixa: as etiquetas comecam no mesmo lugar em toda missao
        Header.AddChild(InfoGrid(
            Info(icon, CategoryName(_quest.Category), color, color, "", UiFonts.SemiBold, 172),
            Info("repeat", RepetitionShort(_quest), UiTokens.TextSecondary, UiTokens.TextLabel, $"Repetição\n~{Repetition(_quest)}", null, 172),
            Info("scroll-text", _board?.Name ?? "Sem quadro", UiTokens.TextSecondary, UiTokens.TextLabel, _board?.Name ?? "", null, 172),
            Info("map-pin", _board != null ? _ctx.ZoneName(_board.RegionId) : "—", UiTokens.TextSecondary, UiTokens.TextLabel, "Zona do quadro", null, 172)));

        var s = Stats;
        TagWidth = 150;
        Tag("trending-up", "Nível", _quest.MaxLevel > 0 ? $"{_quest.MinLevel}–{_quest.MaxLevel}" : $"{_quest.MinLevel}+", UiTokens.Info, "Nível para aceitar");
        Tag("list", "Etapas", (_quest.Steps.Count + 1).ToString(), color, "Objetivos no mundo + entrega no quadro");
        Tag("sparkles", "XP", PtBr.Int(QuestSystem.RewardXp(_ctx.Content, Math.Max(_quest.MinLevel, _quest.RecommendedLevel), _quest)), UiTokens.Success,
            _quest.ScalesWithLevel ? "XP de referência\n~Diária: o XP acompanha o nível de quem entrega." : "XP no nível recomendado\n~Abaixo ou acima do nível, rende menos.");
        Tag("circle-check", "Concluíram", s == null ? "0" : PtBr.Int(s.UniqueCompleters), UiTokens.AccentSoft, "Personagens diferentes que já entregaram");
        Tag("hourglass", "Fazendo agora", s == null ? "0" : PtBr.Int(s.Active), UiTokens.Info, "Personagens com a missão aceita agora");

        if (_board != null)
        {
            Actions.AddChild(Action("Ver o quadro", $"Abre {_board.Name}", () => { _ctx.OpenArea(_board.RegionId, _board.AreaId); }, primary: true));
            Actions.AddChild(Action("Aproximar no mapa", "Centraliza a área do quadro", () => { _ctx.FocusArea(_board.RegionId, _board.AreaId); Close(); }));
        }
        Actions.AddChild(Action("Editar quest", "O editor de quests chega em uma tela própria (conteúdo já configurável)", () => { }, enabled: false));
    }

    // ---------------- Visao geral (kit D-034) ----------------

    private Control BuildOverview()
    {
        var doing = Fit(0, "fazendo");
        var table = BuildDoingTable(doing);
        // Uma tela: as etapas, quem pode fazer e o que ganha, e os numeros; embaixo, quem esta fazendo agora.
        return new ScreenPage()
            .Add(Grid(12,
                (FillCard($"Objetivos · {_quest.Steps.Count + 1} etapas", StepsTimeline(), info: "Etapas da missão\n~As etapas acontecem no mundo; aceitar e entregar são ações rápidas no quadro."), 6),
                (FillCard("Requisitos e recompensas", RequirementsBody(), info: "Quem pode fazer e o que ganha\n~Ouro e itens já fazem parte da missão, mas só passam a ter efeito com inventário e economia (M5). Hoje a recompensa aplicada é o XP."), 3),
                (FillCard("Desempenho da missão", FunnelBody(), info: "Números da campanha\n~Aceites, quem está fazendo e entregas desde o lançamento."), 3)), weight: 55)
            .Add(Grid(12,
                (FillCard("Fazendo agora", table, info: "Personagens com a missão em andamento\n~Clique para abrir a ficha. Ordene pelas colunas.", headerRight: Pager(doing, "fazendo", "personagem", "personagens")), 9),
                (FillCard("Em que etapa estão", StageBody(), info: "Distribuição de quem está fazendo\n~Quantos personagens estão em cada etapa agora."), 3)), weight: 45);
    }

    /// <summary>Concluiram: a lista de quem entregou e, ao lado, quem sao (perfil e classe).</summary>
    private Control BuildCompleters()
    {
        var rows = Fit(0, "concluiram");
        var table = CompletersTable(rows);
        return new ScreenPage().Add(Grid(12,
            (FillCard("Quem já entregou esta missão", table, info: "Personagens que entregaram a missão\n~Mais recentes primeiro. Clique para abrir a ficha. Ordene pelas colunas.", headerRight: Pager(rows, "concluiram", "personagem", "personagens")), 9),
            (FillCard("Quem concluiu", CompletersMix(), info: "Quem são os personagens que já entregaram\n~Perfil de dedicação e classe."), 3)));
    }

    private Control CompletersMix()
    {
        var done = Completers();
        var col = Column(12);
        var nums = Row(0,
            Mini(PtBr.Int(done.Count), "Personagens"),
            Mini(PtBr.Int(done.Sum(x => x.Times)), "Entregas"),
            Mini(done.Count == 0 ? "–" : PtBr.Decimal(done.Average(x => x.V.Level), 1), "Nível atual médio"));
        col.AddChild(nums);
        void Bars(string title, IEnumerable<(string Name, Color Color)> items)
        {
            var groups = items.GroupBy(x => x.Name).Select(g => (Name: g.Key, Color: g.First().Color, N: g.Count()))
                .OrderByDescending(g => g.N).ThenBy(g => g.Name, StringComparer.Ordinal).ToList();
            col.AddChild(Section(title));
            if (groups.Count == 0) { col.AddChild(ThemeFactory.Text("—", UiTokens.FontSmall, UiTokens.TextDisabled)); return; }
            int max = groups.Max(g => g.N);
            foreach (var g in groups) col.AddChild(Bar(g.Name, g.N, max, g.Color, g.N.ToString(), 120));
        }
        Bars("Por perfil", done.Select(x => (_ctx.Content.Profiles.TryGetValue(_ctx.Persons[x.Id - 1].ProfileId, out var p) ? p.Name : "", UiTokens.ProfileColor(_ctx.Persons[x.Id - 1].ProfileId))));
        Bars("Por classe", done.Select(x => _ctx.Content.Classes.TryGetValue(x.V.ClassId, out var c) ? (c.Name, PlayerModal.RoleColor(c.Role)) : (x.V.ClassId, UiTokens.TextSecondary)));
        return col;
    }

    private static Control Mini(string value, string caption)
    {
        var c = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        c.AddThemeConstantOverride("separation", 0);
        c.AddChild(ThemeFactory.Text(value, 20, UiTokens.TextPrimary, UiFonts.Numbers));
        var cap = ThemeFactory.Text(caption, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium);
        cap.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        c.AddChild(cap);
        return c;
    }

    private Control StepsTimeline()
    {
        var col = Column(0);
        var color = CategoryColor(_quest.Category);
        for (int i = 0; i <= _quest.Steps.Count; i++)
        {
            bool deliver = i == _quest.Steps.Count;
            var step = deliver ? null : _quest.Steps[i];
            var act = step != null && _ctx.Content.Regions.TryGetValue(step.RegionId, out var r) ? r.FindActivity(step.ActivityId) : null;
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 14);
            row.AddChild(new StepBadge(i + 1, color, last: deliver) { CustomMinimumSize = new Vector2(28, 52) });
            row.AddChild(new Icon(deliver ? "medal" : step!.Type == QuestStepTypes.Visit ? "map-pin" : PlayerModal.TypeIcon(act?.Type ?? ActivityTypes.Quest), 18,
                deliver ? UiTokens.Accent : act != null ? ActivityPalette.Color(act.Type) : UiTokens.Info));
            var info = Column(2);
            info.SizeFlagsVertical = SizeFlags.ShrinkCenter;
            if (deliver)
            {
                info.AddChild(ThemeFactory.Text(_board != null ? $"Entregar em {_board.Name}" : "Entregar no quadro", UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Medium));
                info.AddChild(ThemeFactory.Text(_board != null ? $"{_ctx.ZoneName(_board.RegionId)} · a recompensa vem na entrega" : "A recompensa vem na entrega", UiTokens.FontSmall, UiTokens.TextSecondary));
            }
            else
            {
                info.AddChild(ThemeFactory.Text(step!.Text, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Medium));
                string what = step.Type == QuestStepTypes.Visit ? "ponto de missão" : act != null ? PtBr.ActivityType(act.Type) : "atividade";
                info.AddChild(ThemeFactory.Text($"{_ctx.PlaceName(step.RegionId, step.ActivityId)} · {_ctx.ZoneName(step.RegionId)} · {what} · {step.Minutes} min", UiTokens.FontSmall, UiTokens.TextSecondary));
                if (_ctx.Content.Regions.TryGetValue(step.RegionId, out var sz) && sz.Pvp == PvpRules.Open)
                    info.AddChild(ThemeFactory.Text("Zona com PvP aberto: risco de ataque de outros jogadores.", UiTokens.FontCaption, UiTokens.Danger));
            }
            row.AddChild(info);
            if (step != null && QuestSystem.IsProvisional(_ctx.Content, step))
            {
                var chip = ThemeFactory.Chip("Provisório", UiTokens.Warning, dot: true);
                chip.TooltipText = "Hoje a etapa conta o TEMPO fazendo a atividade.\nAbates e itens só existem a partir do combate (M3) e da economia (M5).";
                chip.MouseFilter = MouseFilterEnum.Stop;
                chip.SizeFlagsVertical = SizeFlags.ShrinkCenter;
                row.AddChild(new Control { SizeFlagsHorizontal = SizeFlags.ExpandFill });
                row.AddChild(chip);
            }
            col.AddChild(row);
        }
        var desc = ThemeFactory.Text(_quest.Description, UiTokens.FontSmall, UiTokens.TextSecondary);
        desc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return Column(12, col, Divider(), desc);
    }

    private Control RequirementsBody()
    {
        Control Line(string icon, string key, string value, Color color)
        {
            var r = new HBoxContainer();
            r.AddThemeConstantOverride("separation", 10);
            r.AddChild(new Icon(icon, 16, color));
            var k = ThemeFactory.Text(key, UiTokens.FontSmall, UiTokens.TextSecondary);
            k.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            r.AddChild(k);
            var v = ThemeFactory.Text(value, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Medium);
            v.HorizontalAlignment = HorizontalAlignment.Right;
            r.AddChild(v);
            return r;
        }
        var col = Column(9, Section("Requisitos"),
            Line("trending-up", "Nível", _quest.MaxLevel > 0 ? $"{_quest.MinLevel} a {_quest.MaxLevel}" : $"{_quest.MinLevel} ou mais", UiTokens.Accent),
            Line("shield", "Classe", _quest.ClassIds.Count == 0 ? "qualquer" : string.Join(", ", _quest.ClassIds.Select(c => _ctx.Content.Classes.TryGetValue(c, out var cl) ? cl.Name : c)), UiTokens.Info),
            Line("clock", "Repetição", Repetition(_quest), UiTokens.TextSecondary));
        if (_board != null && _quest.Category == QuestCategories.Daily)
            col.AddChild(Line("sun", "Limite do quadro", $"{_board.DailyLimit} por dia", UiTokens.Success));
        foreach (var pre in _quest.PrerequisiteQuestIds)
        {
            string id = pre;
            string name = _ctx.Content.Quests.TryGetValue(pre, out var pq) ? pq.Name : pre;
            var b = HoverRow(() => _ctx.OpenQuest(id), "Abre a quest anterior");
            b.CustomMinimumSize = new Vector2(0, 28);
            var r = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            r.SetAnchorsPreset(LayoutPreset.FullRect);
            r.AddThemeConstantOverride("separation", 10);
            r.AddChild(new Icon("scroll-text", 16, UiTokens.AccentSoft));
            var k = ThemeFactory.Text("Exige", UiTokens.FontSmall, UiTokens.TextSecondary);
            k.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            k.VerticalAlignment = VerticalAlignment.Center;
            r.AddChild(k);
            var v = ThemeFactory.Text(name + "  ›", UiTokens.FontBody, UiTokens.AccentSoft, UiFonts.Medium);
            v.VerticalAlignment = VerticalAlignment.Center;
            r.AddChild(v);
            b.AddChild(r);
            col.AddChild(b);
        }
        foreach (var u in _ctx.Content.Quests.Values.Where(x => x.PrerequisiteQuestIds.Contains(_quest.Id)).OrderBy(x => x.Name, StringComparer.Ordinal))
        {
            string id = u.Id;
            var b = HoverRow(() => _ctx.OpenQuest(id), $"{u.Name}\n~Esta missão libera a seguinte. Clique para abrir.");
            b.CustomMinimumSize = new Vector2(0, 28);
            var r = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            r.SetAnchorsPreset(LayoutPreset.FullRect);
            r.AddThemeConstantOverride("separation", 10);
            r.AddChild(new Icon("door-open", 16, UiTokens.Success));
            var k = ThemeFactory.Text("Libera", UiTokens.FontSmall, UiTokens.TextSecondary);
            k.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            k.VerticalAlignment = VerticalAlignment.Center;
            r.AddChild(k);
            var v = ThemeFactory.Text(u.Name + "  ›", UiTokens.FontBody, UiTokens.Success, UiFonts.Medium);
            v.VerticalAlignment = VerticalAlignment.Center;
            r.AddChild(v);
            b.AddChild(r);
            col.AddChild(b);
        }
        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });
        col.AddChild(Section("Recompensas"));
        long xp = QuestSystem.RewardXp(_ctx.Content, Math.Max(_quest.MinLevel, _quest.RecommendedLevel), _quest);
        col.AddChild(Line("award", "Experiência", _quest.ScalesWithLevel ? $"{_quest.Rewards.XpPermille / 10.0:0.#}% do nível" : $"{PtBr.Int(xp)} XP", UiTokens.Success));
        col.AddChild(Line("coins", "Ouro · M5", _quest.Rewards.GoldCopper > 0 ? Coins(_quest.Rewards.GoldCopper) : "—", UiTokens.TextDisabled));
        foreach (var it in _quest.Rewards.ItemIds)
            col.AddChild(Line("backpack", "Item · M5", _ctx.Content.Items.TryGetValue(it, out var item) ? $"{item.Name} ({item.Rarity})" : it, UiTokens.TextDisabled));
        return col;
    }

    private Control FunnelBody()
    {
        var s = Stats;
        int accepted = s?.Accepted ?? 0, completed = s?.Completed ?? 0, active = s?.Active ?? 0;
        float rate = accepted == 0 ? 0 : completed / (float)accepted;
        var ring = new RingGauge { CustomMinimumSize = new Vector2(96, 96), SizeFlagsVertical = SizeFlags.ShrinkCenter, Value = rate, Color = UiTokens.Success, Center = accepted == 0 ? "–" : $"{rate * 100:0}%", Caption = "conclusão" };
        var bars = Column(8,
            Bar("Aceitaram", accepted, Math.Max(1, accepted), UiTokens.Info, PtBr.Int(accepted), 80),
            Bar("Fazendo", active, Math.Max(1, accepted), UiTokens.Accent, PtBr.Int(active), 80),
            Bar("Entregaram", completed, Math.Max(1, accepted), UiTokens.Success, PtBr.Int(completed), 80));
        bars.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        string avgHours = completed == 0 ? "–" : PtBr.Decimal(s!.SumPlayedTicksToComplete / (double)completed / SimTime.TicksPerHour) + " h";
        string avgLevel = completed == 0 ? "–" : PtBr.Decimal(s!.SumLevelAtCompletion / (double)completed);
        string last = s == null || s.LastCompletedTick < 0 ? "nunca" : $"{PtBr.ShortDate(_ctx.Calendar.ToDateTime(s.LastCompletedTick))} {_ctx.Clock(s.LastCompletedTick)}";
        return Column(14, Row(16, ring, bars), Divider(),
            KeyValue("Personagens que concluíram", PtBr.Int(s?.UniqueCompleters ?? 0)),
            KeyValue("Tempo médio até entregar", avgHours),
            KeyValue("Nível médio na entrega", avgLevel),
            KeyValue("XP entregue no total", PtBr.Int(s?.XpGranted ?? 0)),
            KeyValue("Última entrega", last));
    }

    private Control StageBody()
    {
        var snap = _ctx.Snapshot();
        var counts = new float[_quest.Steps.Count + 1];
        for (int id = 1; id < snap.ByPerson.Length; id++)
            if (snap.ByPerson[id] is { } v)
                foreach (var p in v.ActiveQuests)
                    if (p.QuestId == _quest.Id) counts[Math.Min(p.StepIndex, _quest.Steps.Count)]++;
        return Grow(new ColumnChart
        {
            CustomMinimumSize = new Vector2(0, 120), CountScale = true, ShowAverage = false, Unit = " personagens",
            Values = counts, Labels = Enumerable.Range(0, counts.Length).Select(i => i == _quest.Steps.Count ? "Entrega" : $"Etapa {i + 1}").ToList(),
            ColorAt = i => i == _quest.Steps.Count ? UiTokens.Success : UiTokens.Info, EmptyText = "Ninguém está fazendo esta missão agora.",
        });
    }

    public static string Coins(long copper)
    {
        long gold = copper / 10000, silver = copper / 100 % 100, cu = copper % 100;
        var parts = new List<string>();
        if (gold > 0) parts.Add($"{gold} ouro");
        if (silver > 0) parts.Add($"{silver} prata");
        if (cu > 0 || parts.Count == 0) parts.Add($"{cu} cobre");
        return string.Join(" ", parts);
    }

    private DataTable<(int Id, CharacterView V, int Times, long LastDay)>? _doneTable;

    /// <summary>Quem ja entregou a missao (vezes e dia da ultima entrega).</summary>
    private List<(int Id, CharacterView V, int Times, long LastDay)> Completers()
    {
        var snap = _ctx.Snapshot();
        var list = new List<(int Id, CharacterView V, int Times, long LastDay)>();
        for (int id = 1; id < snap.ByPerson.Length; id++)
            if (snap.ByPerson[id] is { } v)
                foreach (var (qid, times, lastDay) in v.CompletedQuests)
                    if (qid == _quest.Id) list.Add((id, v, times, lastDay));
        return list;
    }

    private Control CompletersTable(FitList rows)
    {
        string Day(long day) => day < 0 ? "—" : PtBr.ShortDate(_ctx.Calendar.ToDateTime(day * SimTime.TicksPerDay));
        string Prof(int id) => _ctx.Content.Profiles.TryGetValue(_ctx.Persons[id - 1].ProfileId, out var p) ? p.Name : "";
        string Cls(string id) => _ctx.Content.Classes.TryGetValue(id, out var c) ? c.Name : id;
        _doneTable ??= new DataTable<(int Id, CharacterView V, int Times, long LastDay)>(new[]
        {
            new Column<(int Id, CharacterView V, int Times, long LastDay)> { Title = "Personagem", Text = x => x.V.Name, SortKey = x => x.V.Name },
            new Column<(int Id, CharacterView V, int Times, long LastDay)> { Title = "Perfil", Width = 110, Text = x => Prof(x.Id), SortKey = x => Prof(x.Id), Color = x => UiTokens.ProfileColor(_ctx.Persons[x.Id - 1].ProfileId).Lightened(0.15f) },
            new Column<(int Id, CharacterView V, int Times, long LastDay)> { Title = "Classe", Width = 100, Text = x => Cls(x.V.ClassId), SortKey = x => x.V.ClassId },
            new Column<(int Id, CharacterView V, int Times, long LastDay)> { Title = "Nível atual", Width = 84, Right = true, Numeric = true, Text = x => x.V.Level.ToString(), SortKey = x => x.V.Level },
            new Column<(int Id, CharacterView V, int Times, long LastDay)> { Title = "Entregas", Width = 74, Right = true, Numeric = true, Text = x => x.Times.ToString(), SortKey = x => x.Times },
            new Column<(int Id, CharacterView V, int Times, long LastDay)> { Title = "Última entrega", Width = 120, Text = x => Day(x.LastDay), SortKey = x => x.LastDay, Color = _ => UiTokens.TextSecondary },
            new Column<(int Id, CharacterView V, int Times, long LastDay)> { Title = "Situação agora", Width = 260, Text = x => StatusText.Of(x.V, _ctx.Content), SortKey = x => (int)x.V.Status, Color = x => x.V.Status == Simulation.Core.Population.CharacterStatus.Offline ? UiTokens.TextDisabled : UiTokens.TextLabel },
        }, sortColumn: 5) { OnClick = x => _ctx.OpenPerson(x.Id) };
        _doneTable.OnSortChanged = () => Rebuild(false);
        var done = Completers();
        if (done.Count == 0) return ThemeFactory.Text("Ninguém entregou esta missão ainda.", UiTokens.FontBody, UiTokens.TextSecondary);
        return _doneTable.Build(done.OrderByDescending(x => x.LastDay), rows);
    }

    private Control BuildDoingTable(FitList rows)
    {
        var snap = _ctx.Snapshot();
        var doing = new List<(int Id, CharacterView V, QuestProgressView P)>();
        for (int id = 1; id < snap.ByPerson.Length; id++)
            if (snap.ByPerson[id] is { } v)
                foreach (var p in v.ActiveQuests)
                    if (p.QuestId == _quest.Id) doing.Add((id, v, p));
        _doingTable ??= new DataTable<(int Id, CharacterView V, QuestProgressView P)>(new[]
        {
            new Column<(int Id, CharacterView V, QuestProgressView P)> { Title = "Personagem", Text = x => x.V.Name, SortKey = x => x.V.Name },
            new Column<(int Id, CharacterView V, QuestProgressView P)> { Title = "Nv", Width = 32, Right = true, Numeric = true, Text = x => x.V.Level.ToString(), SortKey = x => x.V.Level, Color = _ => UiTokens.Info },
            new Column<(int Id, CharacterView V, QuestProgressView P)> { Title = "Etapa", Width = 260, Text = StepText, SortKey = x => x.P.StepIndex * 1000 + x.P.StepMinutes },
            new Column<(int Id, CharacterView V, QuestProgressView P)> { Title = "Aceitou", Width = 110, Text = x => PtBr.ShortDate(_ctx.Calendar.ToDateTime(x.P.AcceptedTick)), SortKey = x => x.P.AcceptedTick, Color = _ => UiTokens.TextSecondary },
            new Column<(int Id, CharacterView V, QuestProgressView P)> { Title = "Status", Width = 110, Text = x => StatusText.Of(x.V, _ctx.Content), Color = _ => UiTokens.TextSecondary },
        }, sortColumn: 2) { OnClick = x => { _ctx.OpenPerson(x.Id); } };
        _doingTable.OnSortChanged = () => Rebuild(false);

        return doing.Count == 0
            ? ThemeFactory.Text("Ninguém está fazendo esta missão agora.", UiTokens.FontBody, UiTokens.TextSecondary)
            : _doingTable.Build(doing, rows);
    }

    private string StepText((int Id, CharacterView V, QuestProgressView P) x)
    {
        if (x.P.StepIndex >= _quest.Steps.Count) return "Pronta para entregar";
        var st = _quest.Steps[x.P.StepIndex];
        return $"{x.P.StepIndex + 1}/{_quest.Steps.Count + 1} · {st.Text} ({x.P.StepMinutes}/{st.Minutes} min)";
    }
}

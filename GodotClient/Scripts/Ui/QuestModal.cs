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
        AddTab("Visão geral", BuildOverview, live: true);
        AddTab($"Objetivos · {_quest.Steps.Count + 1}", BuildSteps);
        AddTab("Requisitos e recompensas", BuildRewards);
        AddTab("Dados", BuildData, live: true);
        AddTab($"Concluíram · {Completers().Count}", () => Column(12, Card("Quem já entregou esta missão", CompletersTable(int.MaxValue),
            info: "Personagens que entregaram a missão\n~Clique para abrir a ficha. Ordene pelas colunas.")), live: true);
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

    private QuestStatsView? Stats => _ctx.Snapshot().QuestStats.TryGetValue(_quest.Id, out var s) ? s : null;

    private void BuildHeader()
    {
        var color = CategoryColor(_quest.Category);
        Art.AddChild(new EmblemBox(_quest.Category == QuestCategories.Main ? "crown" : _quest.Category == QuestCategories.Daily ? "sun" : "scroll-text", color) { CustomMinimumSize = new Vector2(84, 84) });
        var col = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        col.AddThemeConstantOverride("separation", 2);
        col.AddChild(ThemeFactory.Text(_quest.Name, 28, UiTokens.TextPrimary, UiFonts.SemiBold));
        string sub = CategoryName(_quest.Category) + (_board != null ? $" · {_board.Name} · {_ctx.ZoneName(_board.RegionId)}" : "") + " · " + Repetition(_quest);
        col.AddChild(ThemeFactory.Text(sub, UiTokens.FontBody, UiTokens.TextSecondary));
        Header.AddChild(col);

        var s = Stats;
        Fact("Nível mínimo", _quest.MaxLevel > 0 ? $"{_quest.MinLevel}–{_quest.MaxLevel}" : $"{_quest.MinLevel}+", UiTokens.Info);
        Fact("Etapas", (_quest.Steps.Count + 1).ToString(), color, "Objetivos no mundo + entrega no quadro");
        Fact("XP", PtBr.Int(QuestSystem.RewardXp(_ctx.Content, Math.Max(_quest.MinLevel, _quest.RecommendedLevel), _quest)), UiTokens.Success,
            _quest.ScalesWithLevel ? "XP de referência\n~Diária: o XP acompanha o nível de quem entrega." : "XP no nível recomendado\n~Abaixo ou acima do nível, rende menos.");
        Fact("Concluíram", s == null ? "0" : PtBr.Int(s.UniqueCompleters), UiTokens.AccentSoft, "Personagens diferentes que já entregaram");
        Fact("Fazendo agora", s == null ? "0" : PtBr.Int(s.Active), UiTokens.Info);

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
        var row1 = Grid(12,
            (Card($"Objetivos · {_quest.Steps.Count + 1} etapas", StepsTimeline(), info: "Etapas da missão\n~As etapas acontecem no mundo; aceitar e entregar são ações rápidas no quadro."), 6),
            (Card("Requisitos e recompensas", RequirementsBody(), info: "Quem pode fazer e o que ganha\n~Ouro e itens já estão definidos, mas só têm efeito com a economia (M5)."), 3),
            (Card("Desempenho da missão", FunnelBody(), info: "Números da campanha\n~Aceites, quem está fazendo e entregas desde o lançamento."), 3));
        var row2 = Grid(12,
            (Card($"Fazendo agora · {Stats?.Active ?? 0}", BuildDoingTable(), info: "Personagens com a missão em andamento\n~Clique para abrir a ficha."), 9),
            (Card("Em que etapa estão", StageBody(), info: "Distribuição de quem está fazendo\n~Quantos personagens estão em cada etapa agora."), 3));
        var done = Completers();
        var row3 = Grid(12,
            (Card($"Quem concluiu · {done.Count}", CompletersTable(10), info: "Personagens que já entregaram\n~Os 10 mais recentes; a lista completa está na aba Concluíram."), 12));
        return Column(12, row1, row2, row3);
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
        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 4) });
        col.AddChild(Section("Recompensas"));
        long xp = QuestSystem.RewardXp(_ctx.Content, Math.Max(_quest.MinLevel, _quest.RecommendedLevel), _quest);
        col.AddChild(Line("award", "Experiência", _quest.ScalesWithLevel ? $"{_quest.Rewards.XpPermille / 10.0:0.#}% do nível" : $"{PtBr.Int(xp)} XP", UiTokens.Success));
        col.AddChild(Line("coins", "Ouro · M5", _quest.Rewards.GoldCopper > 0 ? Coins(_quest.Rewards.GoldCopper) : "—", UiTokens.TextDisabled));
        foreach (var it in _quest.Rewards.ItemIds)
            col.AddChild(Line("backpack", "Item · M5", _ctx.Content.Items.TryGetValue(it, out var item) ? item.Name : it, UiTokens.TextDisabled));
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
        return new ColumnChart
        {
            CustomMinimumSize = new Vector2(0, 190), CountScale = true, ShowAverage = false, Unit = " personagens",
            Values = counts, Labels = Enumerable.Range(0, counts.Length).Select(i => i == _quest.Steps.Count ? "Entrega" : $"Etapa {i + 1}").ToList(),
            ColorAt = i => i == _quest.Steps.Count ? UiTokens.Success : UiTokens.Info, EmptyText = "Ninguém está fazendo esta missão agora.",
        };
    }

    // ---------------- Objetivos ----------------

    private Control BuildSteps()
    {
        var col = Column(10);
        for (int i = 0; i < _quest.Steps.Count; i++) col.AddChild(StepCard(i + 1, _quest.Steps[i]));
        // Entrega: sempre no quadro de origem.
        var deliver = Column(4);
        deliver.AddChild(ThemeFactory.Text(_board != null ? $"Voltar a {_board.Name} ({_ctx.ZoneName(_board.RegionId)}) e entregar" : "Entregar no quadro", UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Medium));
        deliver.AddChild(ThemeFactory.Text("A recompensa é dada na entrega. Aceitar e entregar são ações rápidas no quadro.", UiTokens.FontSmall, UiTokens.TextSecondary));
        col.AddChild(Card($"Etapa {_quest.Steps.Count + 1} · entrega", deliver, CategoryColor(_quest.Category)));
        return col;
    }

    private Control StepCard(int n, QuestStepDefinition step)
    {
        var act = _ctx.Content.Regions.TryGetValue(step.RegionId, out var r) ? r.FindActivity(step.ActivityId) : null;
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        row.AddChild(new GlyphIcon(act?.Type ?? ActivityTypes.Quest) { CustomMinimumSize = new Vector2(36, 36) });
        var info = Column(3);
        info.AddChild(ThemeFactory.Text(step.Text, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Medium));
        string where = $"{_ctx.PlaceName(step.RegionId, step.ActivityId)} · {_ctx.ZoneName(step.RegionId)}";
        string what = step.Type == QuestStepTypes.Visit ? $"ponto de missão · {step.Minutes} min" : $"{(act != null ? PtBr.ActivityType(act.Type) : "atividade")} · {step.Minutes} min";
        info.AddChild(ThemeFactory.Text($"{where} · {what}", UiTokens.FontSmall, UiTokens.TextSecondary, UiFonts.Numbers));
        if (r != null && r.Pvp == PvpRules.Open)
            info.AddChild(ThemeFactory.Text("Zona com PvP aberto: risco de ataque de outros jogadores.", UiTokens.FontCaption, UiTokens.Danger));
        row.AddChild(info);
        if (QuestSystem.IsProvisional(_ctx.Content, step))
        {
            var chip = ThemeFactory.Chip("Provisório", UiTokens.Warning, dot: true);
            chip.TooltipText = "Hoje a etapa conta o TEMPO fazendo a atividade.\nAbates e itens coletados só existem a partir do combate (M3) e da economia (M5);\nentão a quest não registra \"derrotou N\" nem \"coletou N\".";
            chip.MouseFilter = MouseFilterEnum.Stop;
            row.AddChild(chip);
        }
        return Card($"Etapa {n} · {(step.Type == QuestStepTypes.Visit ? "visita" : "atividade")}", row, CategoryColor(_quest.Category));
    }

    // ---------------- Requisitos e recompensas ----------------

    private Control BuildRewards()
    {
        var req = Column(6);
        req.AddChild(KeyValue("Nível", _quest.MaxLevel > 0 ? $"de {_quest.MinLevel} a {_quest.MaxLevel}" : $"{_quest.MinLevel} ou mais", UiTokens.Info));
        req.AddChild(KeyValue("Classe", _quest.ClassIds.Count == 0 ? "qualquer" : string.Join(", ", _quest.ClassIds.Select(c => _ctx.Content.Classes.TryGetValue(c, out var cl) ? cl.Name : c))));
        req.AddChild(KeyValue("Repetição", Repetition(_quest)));
        if (_board != null && _quest.Category == QuestCategories.Daily)
            req.AddChild(KeyValue("Limite do quadro", $"{_board.DailyLimit} diárias por dia por personagem"));
        if (_quest.PrerequisiteQuestIds.Count == 0)
            req.AddChild(KeyValue("Quest anterior", "nenhuma"));
        foreach (var pre in _quest.PrerequisiteQuestIds)
        {
            var name = _ctx.Content.Quests.TryGetValue(pre, out var pq) ? pq.Name : pre;
            string id = pre;
            req.AddChild(Action($"Exige: {name}", "Abre a quest anterior", () => { _ctx.OpenQuest(id); }));
        }
        var unlocks = _ctx.Content.Quests.Values.Where(x => x.PrerequisiteQuestIds.Contains(_quest.Id)).ToList();
        foreach (var u in unlocks)
        {
            string id = u.Id;
            req.AddChild(Action($"Libera: {u.Name}", "Abre a quest seguinte", () => { _ctx.OpenQuest(id); }));
        }

        var rew = Column(6);
        long xp = QuestSystem.RewardXp(_ctx.Content, Math.Max(_quest.MinLevel, _quest.RecommendedLevel), _quest);
        rew.AddChild(KeyValue("Experiência", _quest.ScalesWithLevel
            ? $"{_quest.Rewards.XpPermille / 10.0:0.#}% do XP do nível de quem entrega"
            : $"{PtBr.Int(xp)} XP no nível {Math.Max(_quest.MinLevel, _quest.RecommendedLevel)} ({_quest.Rewards.XpPermille / 10.0:0.#}% do nível)", UiTokens.Success));
        rew.AddChild(KeyValue("Ouro", _quest.Rewards.GoldCopper > 0 ? $"{Coins(_quest.Rewards.GoldCopper)} · não implementado (M5)" : "—", UiTokens.TextSecondary));
        if (_quest.Rewards.ItemIds.Count == 0) rew.AddChild(KeyValue("Itens", "—"));
        foreach (var it in _quest.Rewards.ItemIds)
        {
            string nm = _ctx.Content.Items.TryGetValue(it, out var item) ? item.Name : it;
            string rarity = item != null ? $" ({item.Rarity})" : "";
            rew.AddChild(KeyValue("Item", $"{nm}{rarity} · não implementado (M5)", UiTokens.TextSecondary));
        }
        var note = ThemeFactory.Text("Ouro e itens já fazem parte da quest, mas só passam a ter efeito quando existirem inventário e economia (M5). Hoje a recompensa aplicada é o XP.", UiTokens.FontCaption, UiTokens.TextSecondary);
        note.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        rew.AddChild(note);

        return Row(12, Card("Requisitos", req, UiTokens.Info), Card("Recompensas", rew, UiTokens.Success));
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

    // ---------------- Dados ----------------

    private Control BuildData()
    {
        var s = Stats;
        var snap = _ctx.Snapshot();
        int accepted = s?.Accepted ?? 0, completed = s?.Completed ?? 0;
        string rate = accepted == 0 ? "–" : $"{completed * 100 / Math.Max(1, accepted)}%";
        string avgHours = completed == 0 ? "–" : PtBr.Decimal(s!.SumPlayedTicksToComplete / (double)completed / SimTime.TicksPerHour) + " h";
        string avgLevel = completed == 0 ? "–" : PtBr.Decimal(s!.SumLevelAtCompletion / (double)completed);
        string last = s == null || s.LastCompletedTick < 0 ? "nunca" : $"{PtBr.ShortDate(_ctx.Calendar.ToDateTime(s.LastCompletedTick))} {_ctx.Clock(s.LastCompletedTick)}";

        var stats = Row(10,
            Stat(PtBr.Int(accepted), "aceitaram", UiTokens.Info),
            Stat(PtBr.Int(completed), "entregas", UiTokens.Success),
            Stat(rate, "taxa de conclusão", UiTokens.AccentSoft, "Entregas ÷ aceites (inclui quem ainda está fazendo)"),
            Stat(PtBr.Int(s?.UniqueCompleters ?? 0), "personagens concluíram", UiTokens.Accent));
        var stats2 = Row(10,
            Stat(avgHours, "tempo médio jogado até entregar", UiTokens.Info, "Tempo conectado entre aceitar e entregar"),
            Stat(avgLevel, "nível médio na entrega", UiTokens.Info),
            Stat(PtBr.Int(s?.XpGranted ?? 0), "XP entregue no total", UiTokens.Success),
            Stat(last, "última entrega", UiTokens.TextSecondary));

        return Column(12, stats, stats2, Card($"Fazendo agora · {Stats?.Active ?? 0}", BuildDoingTable(), CategoryColor(_quest.Category)));
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

    private Control CompletersTable(int max)
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
        return _doneTable.Build(done.OrderByDescending(x => x.LastDay).Take(max));
    }

    private Control BuildDoingTable()
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
            ? ThemeFactory.Text("Ninguém está fazendo esta quest agora.", UiTokens.FontBody, UiTokens.TextSecondary)
            : _doingTable.Build(doing.Take(12));
    }

    private string StepText((int Id, CharacterView V, QuestProgressView P) x)
    {
        if (x.P.StepIndex >= _quest.Steps.Count) return "Pronta para entregar";
        var st = _quest.Steps[x.P.StepIndex];
        return $"{x.P.StepIndex + 1}/{_quest.Steps.Count + 1} · {st.Text} ({x.P.StepMinutes}/{st.Minutes} min)";
    }
}

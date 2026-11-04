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
        AddTab($"Objetivos · {_quest.Steps.Count + 1}", BuildSteps);
        AddTab("Requisitos e recompensas", BuildRewards);
        AddTab("Dados", BuildData, live: true);
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
        var col = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        col.AddThemeConstantOverride("separation", 6);
        col.AddChild(ThemeFactory.Text(_quest.Name, 30, UiTokens.TextPrimary, UiFonts.Display));
        var desc = ThemeFactory.Text(_quest.Description, UiTokens.FontBody, UiTokens.TextSecondary);
        desc.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        desc.CustomMinimumSize = new Vector2(560, 0);
        col.AddChild(desc);
        var chips = new HFlowContainer();
        chips.AddThemeConstantOverride("h_separation", 8);
        chips.AddThemeConstantOverride("v_separation", 6);
        chips.AddChild(ThemeFactory.Chip(CategoryName(_quest.Category), color, dot: true));
        if (_board != null)
        {
            chips.AddChild(ThemeFactory.Chip(_board.Name, UiTokens.AccentSoft));
            chips.AddChild(ThemeFactory.Chip(_ctx.ZoneName(_board.RegionId), UiTokens.TextSecondary));
        }
        chips.AddChild(ThemeFactory.Chip(_quest.ScalesWithLevel ? "Recompensa pelo seu nível" : $"Pensada para o nível {_quest.RecommendedLevel}", UiTokens.Info));
        chips.AddChild(ThemeFactory.Chip(Repetition(_quest), UiTokens.TextSecondary));
        col.AddChild(chips);
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
            Actions.AddChild(Action("Ver o quadro", $"Abre {_board.Name}", () => { Close(); _ctx.OpenArea(_board.RegionId, _board.AreaId); }, primary: true));
            Actions.AddChild(Action("Aproximar no mapa", "Centraliza a área do quadro", () => { _ctx.FocusArea(_board.RegionId, _board.AreaId); Close(); }));
        }
        Actions.AddChild(Action("Editar quest", "O editor de quests chega em uma tela própria (conteúdo já configurável)", () => { }, enabled: false));
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
            req.AddChild(Action($"Exige: {name}", "Abre a quest anterior", () => { Close(); _ctx.OpenQuest(id); }));
        }
        var unlocks = _ctx.Content.Quests.Values.Where(x => x.PrerequisiteQuestIds.Contains(_quest.Id)).ToList();
        foreach (var u in unlocks)
        {
            string id = u.Id;
            req.AddChild(Action($"Libera: {u.Name}", "Abre a quest seguinte", () => { Close(); _ctx.OpenQuest(id); }));
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

    private static string Coins(long copper)
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
        }, sortColumn: 2) { OnClick = x => { Close(); _ctx.OpenPerson(x.Id); } };
        _doingTable.OnSortChanged = () => Rebuild(false);

        var body = doing.Count == 0
            ? (Control)ThemeFactory.Text("Ninguém está fazendo esta quest agora.", UiTokens.FontBody, UiTokens.TextSecondary)
            : _doingTable.Build(doing);
        return Column(12, stats, stats2, Card($"Fazendo agora · {doing.Count}", body, CategoryColor(_quest.Category)));
    }

    private string StepText((int Id, CharacterView V, QuestProgressView P) x)
    {
        if (x.P.StepIndex >= _quest.Steps.Count) return "Pronta para entregar";
        var st = _quest.Steps[x.P.StepIndex];
        return $"{x.P.StepIndex + 1}/{_quest.Steps.Count + 1} · {st.Text} ({x.P.StepMinutes}/{st.Minutes} min)";
    }
}

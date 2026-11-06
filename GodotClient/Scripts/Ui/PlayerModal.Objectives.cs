using Godot;
using Simulation.Core.Content;
using Simulation.Core.Population;
using GodotClient.Sim;

namespace GodotClient.Ui;

/// <summary>
/// Aba Objetivos (O1): ambicoes da pessoa (gerais e secundarios pela importancia para ela), com
/// horizonte, viabilidade, progresso quando mensuravel e a preparacao possivel separada da etapa
/// final; metas atuais; historico. Na Visao geral, so uma linha com o objetivo principal.
/// </summary>
public partial class PlayerModal
{
    private int _objectivesTab = -1;

    private CharacterDetailView? WatchedDetail => _ctx.Snapshot().Watched is { } d && d.PersonId == _person.Id ? d : null;

    private const string ObjectivesInfo = "Ambições da pessoa neste MMORPG\n~Objetivos atravessam várias sessões e são diferentes das metas. Geral ou secundário é a importância para esta pessoa, não uma categoria fixa.\n~Escolhidos ao entrar no jogo pelas Preferências, personalidade, conduta, traços e dedicação (quem se dedica mais tende a ambições de fim de jogo).\n~Objetivos que dependem de sistemas ainda inexistentes não recebem progresso.";

    private Control BuildObjectives()
    {
        if (View is not { } v)
            return Column(12, Card("Objetivos", Wrap("Ainda não entrou no jogo. A pessoa define suas ambições ao se registrar.", UiTokens.FontSmall, UiTokens.TextSecondary), info: ObjectivesInfo));
        var d = WatchedDetail;
        if (d == null) return Column(12, Card("Objetivos", Wrap("Carregando…", UiTokens.FontSmall, UiTokens.TextSecondary)));
        var active = d.Objectives.Where(o => o.State == ObjectiveStates.Active).ToList();
        var general = active.Where(o => o.Tier == ObjectiveTiers.General).ToList();
        var secondary = active.Where(o => o.Tier == ObjectiveTiers.Secondary).ToList();

        Control Count(int n) => ThemeFactory.Text(n.ToString(), UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers);
        var row1 = Grid(12,
            (Card("Objetivos gerais", ObjectiveList(general, "Sem ambição geral definida."), info: ObjectivesInfo, headerRight: Count(general.Count)), 6),
            (Card("Objetivos secundários", ObjectiveList(secondary, "Nenhum projeto secundário: só as ambições gerais."),
                info: "Projetos de menor prioridade\n~Podem ser do mesmo tema de um objetivo geral ou independentes. Um secundário também pode ser de longo prazo.", headerRight: Count(secondary.Count)), 6));
        var row2 = Grid(12,
            (Card("Metas atuais", ObjectiveGoalsBody(v), info: "Metas de curto prazo\n~Chegar a um nível, descobrir uma zona, concluir uma missão ou praticar uma atividade. Influenciam as escolhas sem obrigar. Metas também surgem por necessidade, oportunidade ou vontade do momento, sem ligação com as ambições."), 6),
            (Card("Histórico", ObjectiveHistoryBody(d), info: "Mudanças nos objetivos\n~Cada objetivo registra quando passou a valer e por quê. Conclusões, mudanças de prioridade e abandonos aparecem aqui com o motivo."), 6));
        return Column(12, row1, row2);
    }

    private Control ObjectiveList(List<ObjectiveView> list, string empty)
    {
        var col = Column(14);
        if (list.Count == 0) col.AddChild(Wrap(empty, UiTokens.FontSmall, UiTokens.TextSecondary));
        foreach (var o in list) col.AddChild(ObjectiveItem(o));
        return col;
    }

    /// <summary>Um objetivo: emblema, nome, horizonte e viabilidade; progresso, preparacao e etapa final.</summary>
    private Control ObjectiveItem(ObjectiveView o)
    {
        var c = _ctx.Content;
        var a = o.Assessment;
        var vc = ObjectiveInfo.ViabilityColor(a.Viability);
        var row = Row(12);
        row.AddChild(new TraitBadge(ObjectiveInfo.Icon(c, o.Id), o.Tier == ObjectiveTiers.General ? UiTokens.Accent : UiTokens.Info, 30));
        var body = Column(4);
        body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        var top = Row(8);
        var name = ThemeFactory.Text(ObjectiveInfo.Title(c, o.Id, o.Param), UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Medium);
        name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        name.ClipText = true;
        top.AddChild(name);
        top.AddChild(ThemeFactory.Text("importância", UiTokens.FontCaption, UiTokens.TextSecondary));
        top.AddChild(ThemeFactory.Text($"{o.Importance / 10}%", UiTokens.FontSmall, UiTokens.TextLabel, UiFonts.Numbers));
        body.AddChild(top);
        string horizon = c.Objectives.TryGetValue(o.Id, out var def) ? ObjectiveInfo.HorizonPt(def.Horizon) : "";
        var tags = Row(6);
        tags.AddChild(ThemeFactory.Text(horizon, UiTokens.FontCaption, UiTokens.TextSecondary));
        tags.AddChild(ThemeFactory.Text("·", UiTokens.FontCaption, UiTokens.TextDisabled));
        tags.AddChild(ThemeFactory.Text(a.CriterionMet ? "Critério cumprido" : ObjectiveInfo.ViabilityPt(a.Viability), UiTokens.FontCaption, a.CriterionMet ? UiTokens.Success : vc, UiFonts.Medium));
        body.AddChild(tags);

        // Progresso so quando mensuravel com o que existe (sem progresso ficticio).
        if (a.Measurable)
        {
            var pr = Row(8);
            var bar = new ThinBar(a.Progress / (float)Math.Max(1, a.ProgressTotal), vc) { CustomMinimumSize = new Vector2(0, 4), SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter };
            pr.AddChild(bar);
            pr.AddChild(ThemeFactory.Text($"{a.Progress} de {a.ProgressTotal}", UiTokens.FontCaption, UiTokens.TextLabel, UiFonts.Numbers));
            body.AddChild(pr);
        }
        // Preparacao possivel (requisito concreto) separada da etapa final indisponivel.
        if (a.PreparationLevel > 0)
            body.AddChild(Hint("footprints", $"Preparação: nível {a.PreparationLevel} ({ObjectiveInfo.SubjectPt(_ctx, a.PreparationSubject)})", UiTokens.Warning));
        else if (a.Viability == ObjectiveViability.Future && HasPreparationKind(def))
            body.AddChild(Hint("circle-check", "Requisitos de nível conhecidos: cumpridos", UiTokens.Success));
        if (a.MissingSystems.Count > 0)
            body.AddChild(Hint("hourglass", "Etapa final indisponível: depende de " + string.Join(" e ", a.MissingSystems.Select(ObjectiveInfo.SystemPt)), UiTokens.TextSecondary));
        row.AddChild(body);

        var tip = new TipPanel { TooltipText = ObjectiveTip(o, def), MouseFilter = MouseFilterEnum.Stop };
        tip.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        tip.AddChild(row);
        return tip;
    }

    private static bool HasPreparationKind(ObjectiveDefinition? d) =>
        d?.Criterion is ObjectiveCriteria.DefeatBosses or ObjectiveCriteria.PvpRanking or ObjectiveCriteria.RareItem or ObjectiveCriteria.StrengthRanking;

    private static Control Hint(string icon, string text, Color color)
    {
        var r = Row(6);
        var i = new Icon(icon, 12, color) { SizeFlagsVertical = SizeFlags.ShrinkCenter };
        r.AddChild(i);
        var l = ThemeFactory.Text(text, UiTokens.FontCaption, color);
        l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        r.AddChild(l);
        return r;
    }

    private string ObjectiveTip(ObjectiveView o, ObjectiveDefinition? def)
    {
        var c = _ctx.Content;
        string color = (o.Tier == ObjectiveTiers.General ? UiTokens.Accent : UiTokens.Info).ToHtml(false);
        var lines = new List<string>
        {
            $"{ObjectiveInfo.Title(c, o.Id, o.Param)}|{ObjectiveInfo.TierPt(o.Tier)}|#{color}",
            "^" + (o.Origin == ObjectiveSystem.InitialOrigin ? "Objetivo inicial: a pessoa já tinha ao entrar no jogo" : "Surgiu durante o jogo"),
        };
        if (def != null) lines.Add(def.Description);
        lines.Add($"Importância|{o.Importance / 10}%");
        lines.Add($"Comprometimento|{o.Commitment / 10}%");
        if (def != null) lines.Add($"Horizonte|{ObjectiveInfo.HorizonPt(def.Horizon)}");
        lines.Add($"Desde|{PtBr.ShortDate(_ctx.Calendar.ToDateTime(_ctx.Calendar.StartOfDay(o.Day)))}");
        if (o.Motives.Count > 0)
        {
            lines.Add("#O que pesou na escolha");
            foreach (var (key, value) in o.Motives)
                lines.Add($"{ObjectiveInfo.MotivePt(c, key)}|{(value > 0 ? "+" : "−")}{Math.Abs(value) / 10}%");
        }
        lines.Add("~Motivação, não ordem: as preferências, o humor e o tempo continuam decidindo cada sessão.");
        return string.Join("\n", lines);
    }

    private Control ObjectiveGoalsBody(CharacterView v)
    {
        var col = Column(10);
        if (v.Goals.Count == 0)
            col.AddChild(Wrap("Sem meta agora. Nem todo jogador tem uma o tempo todo.", UiTokens.FontSmall, UiTokens.TextSecondary));
        foreach (var g in v.Goals)
        {
            var (icon, text, progress) = GoalText(v, g);
            col.AddChild(GoalRow(icon, text, progress, UiTokens.Accent));
        }
        col.AddChild(ThemeFactory.Text($"{v.GoalsCompleted} concluídas · {v.GoalsAbandoned} abandonadas", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers));
        return col;
    }

    private Control ObjectiveHistoryBody(CharacterDetailView d)
    {
        var col = Column(8);
        if (d.ObjectiveHistory.Count == 0) col.AddChild(Wrap("Nenhuma mudança registrada.", UiTokens.FontSmall, UiTokens.TextSecondary));
        foreach (var h in d.ObjectiveHistory.Reverse())
        {
            string what = h.Change switch
            {
                "set" when h.Reason == ObjectiveSystem.InitialOrigin => "Objetivo inicial",
                "set" => "Novo objetivo",
                _ => h.Change,
            };
            var r = Row(8);
            r.AddChild(new Icon(ObjectiveInfo.Icon(_ctx.Content, h.Id), 14, h.Tier == ObjectiveTiers.General ? UiTokens.Accent : UiTokens.Info) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
            var l = ThemeFactory.Text($"{what}: {ObjectiveInfo.Title(_ctx.Content, h.Id, h.Param)} ({(h.Tier == ObjectiveTiers.General ? "geral" : "secundário")})", UiTokens.FontSmall, UiTokens.TextLabel);
            l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            l.ClipText = true;
            r.AddChild(l);
            r.AddChild(ThemeFactory.Text(PtBr.ShortDate(_ctx.Calendar.ToDateTime(_ctx.Calendar.StartOfDay(h.Day))), UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers));
            col.AddChild(r);
        }
        return col;
    }

    /// <summary>Resumo compacto na Visao geral: o objetivo geral principal e acesso a aba.</summary>
    private Control? ObjectiveSummary(CharacterDetailView? d)
    {
        var main = d?.Objectives.Where(o => o.State == ObjectiveStates.Active).OrderBy(o => o.Tier == ObjectiveTiers.General ? 0 : 1)
            .ThenByDescending(o => o.Importance).FirstOrDefault();
        if (d == null || main == null) return null;
        int others = d.Objectives.Count(o => o.State == ObjectiveStates.Active) - 1;
        var c = _ctx.Content;
        var b = HoverRow(() => { if (_objectivesTab >= 0) SelectTab(_objectivesTab); }, $"{ObjectiveInfo.Title(c, main.Id, main.Param)}|{ObjectiveInfo.TierPt(main.Tier)}\n{ObjectiveInfo.ViabilityPt(main.Assessment.Viability)}\n~Clique para abrir a aba Objetivos.");
        b.CustomMinimumSize = new Vector2(0, 28);
        var r = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        r.SetAnchorsPreset(LayoutPreset.FullRect);
        r.OffsetLeft = 4; r.OffsetRight = -4;
        r.AddThemeConstantOverride("separation", 8);
        r.AddChild(new Icon(ObjectiveInfo.Icon(c, main.Id), 16, UiTokens.Accent) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
        var nm = ThemeFactory.Text(ObjectiveInfo.Title(c, main.Id, main.Param), UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Medium);
        nm.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        nm.VerticalAlignment = VerticalAlignment.Center;
        nm.ClipText = true;
        r.AddChild(nm);
        if (others > 0) r.AddChild(ThemeFactory.Text($"+{others}", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers));
        r.AddChild(new Icon("chevron-right", 14, UiTokens.TextSecondary) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
        b.AddChild(r);
        return b;
    }
}

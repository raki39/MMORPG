using Godot;
using Simulation.Core.Content;
using Simulation.Core.Population;
using GodotClient.Sim;

namespace GodotClient.Ui;

/// <summary>
/// Aba Objetivos (O1 a O3): ambicoes ativas (gerais e secundarios pela importancia para a pessoa), com
/// horizonte, viabilidade, progresso quando mensuravel e a preparacao separada da etapa final; metas
/// atuais e os vinculos; conquistas e encerrados (concluidos, mantidos, perdidos, deixados de lado,
/// abandonados), com data, motivo e resultado; historico. Na Visao geral, so uma linha.
/// </summary>
public partial class PlayerModal
{


    private CharacterDetailView? WatchedDetail => _ctx.Snapshot().Watched is { } d && d.PersonId == _person.Id ? d : null;

    private const string ObjectivesInfo = "Ambições da pessoa neste MMORPG\n~Objetivos atravessam várias sessões e são diferentes das metas. Geral ou secundário é a importância para esta pessoa, não uma categoria fixa.\n~Escolhidos ao entrar no jogo pelas Preferências, personalidade, conduta, traços e dedicação (quem se dedica mais tende a ambições de fim de jogo).\n~Objetivos que dependem de sistemas ainda inexistentes não recebem progresso.";

    private Control BuildObjectives()
    {
        if (View is not { } v)
            return Column(12, Card("Objetivos", Wrap("Ainda não entrou no jogo. A pessoa define suas ambições ao se registrar.", UiTokens.FontSmall, UiTokens.TextSecondary), info: ObjectivesInfo));
        var d = WatchedDetail;
        if (d == null) return Column(12, Card("Objetivos", Wrap("Carregando…", UiTokens.FontSmall, UiTokens.TextSecondary)));
        // So os ativos nas vagas; os encerrados (com data, motivo e resultado) ficam no card proprio.
        var active = d.Objectives.Where(o => o.State == ObjectiveStates.Active).OrderByDescending(o => o.Importance).ToList();
        var general = active.Where(o => o.Tier == ObjectiveTiers.General).ToList();
        var secondary = active.Where(o => o.Tier == ObjectiveTiers.Secondary).ToList();
        var closed = d.Objectives.Where(o => o.State != ObjectiveStates.Active).ToList();
        string noGeneral = d.LastObjectiveGapDay >= 0 && active.Count == 0
            ? $"Sem ambição ativa: em {DayText(d.LastObjectiveGapDay)} buscou um objetivo novo e não encontrou nenhum relevante no conteúdo atual."
            : "Sem ambição geral ativa.";

        Control Count(int n) => ThemeFactory.Text(n.ToString(), UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers);
        // Pagina em telas (D-068), uma tela: em cima as ambicoes ativas e as metas que as servem; embaixo o que ja
        // saiu das vagas e o historico de mudancas (largo, porque cada linha traz o motivo).
        return new ScreenPage()
            .Add(Grid(12,
                (FillCard("Objetivos gerais", ObjectiveList(general, noGeneral), info: ObjectivesInfo, headerRight: Count(general.Count)), 4),
                (FillCard("Objetivos secundários", ObjectiveList(secondary, "Nenhum projeto secundário: só as ambições gerais."),
                    info: "Projetos de menor prioridade\n~Podem ser do mesmo tema de um objetivo geral ou independentes. Um secundário também pode ser de longo prazo.", headerRight: Count(secondary.Count)), 4),
                (FillCard("Metas atuais", ObjectiveGoalsBody(v), info: "Metas de curto prazo e o que elas ajudam\n~Uma meta se liga a um objetivo só quando contribui de verdade: avanço real (nível para o nível máximo, missão principal para a campanha, zona nova para conhecer o mundo) ou preparação por um requisito que a pessoa conhece (o nível do chefe, do PvP, do item).\n~A meta ligada tem mais chance de ser adotada e mais comprometimento, pelo objetivo mais forte que ela atende. Metas também surgem por necessidade, oportunidade ou vontade do momento, sem ligação.",
                    headerRight: ThemeFactory.Text($"{N(v.GoalsCompleted, "concluída", "concluídas")} · {N(v.GoalsAbandoned, "abandonada", "abandonadas")}", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers)), 4)), weight: 55)
            .Add(Grid(12,
                (FillCard("Conquistas e encerrados", ClosedBody(closed), info: "Objetivos que saíram das vagas\n~Conquista definitiva (ex.: concluir a campanha) fica concluída. Condição a manter (ex.: estar entre os mais fortes, ter uma grande fortuna) pode ser alcançada e perdida depois; perder não apaga a conquista passada, e a pessoa pode querer recuperá-la.\n~Deixado de lado: trocado por uma prioridade nova, sem frustração. Não conseguir realizar agora não é motivo para abandonar um objetivo.", headerRight: Count(closed.Count)), 4),
                (FillCard("Histórico", ObjectiveHistoryBody(d), info: "Mudanças nos objetivos\n~Cada mudança tem o motivo: o acontecimento que levou a um objetivo novo, a conclusão, a perda de uma condição, a troca de prioridade, a substituição ou o abandono. Quando a pessoa buscou um objetivo novo e nenhum era relevante, isso também fica registrado: falta de conteúdo do MMORPG.", headerRight: Count(d.ObjectiveHistory.Count)), 8)), weight: 45);
    }

    private string DayText(long day) => PtBr.ShortDate(_ctx.Calendar.ToDateTime(_ctx.Calendar.StartOfDay(day)));

    /// <summary>Encerrados: concluidos, mantidos, perdidos, deixados de lado e abandonados, com data, motivo e resultado.</summary>
    private Control ClosedBody(List<ObjectiveView> closed)
    {
        var col = Column(10);
        if (closed.Count == 0) col.AddChild(Wrap("Nenhum ainda. Concluídos e encerrados ficam aqui, com a data e o motivo.", UiTokens.FontSmall, UiTokens.TextSecondary));
        int Order(ObjectiveView o) => o.State switch { ObjectiveStates.Held => 0, ObjectiveStates.Completed => 1, ObjectiveStates.Lost => 2, ObjectiveStates.Paused => 3, _ => 4 };
        foreach (var o in closed.OrderBy(Order).ThenByDescending(o => Math.Max(o.CompletedDay, o.EndedDay)))
        {
            var (text, color) = o.State switch
            {
                ObjectiveStates.Completed => ($"Concluído em {DayText(o.CompletedDay)}", UiTokens.Success),
                ObjectiveStates.Held => ($"Alcançado em {DayText(o.CompletedDay)} · mantido agora", UiTokens.Success),
                ObjectiveStates.Lost => ($"Alcançado em {DayText(o.CompletedDay)} · perdido em {DayText(o.EndedDay)}", UiTokens.Warning),
                ObjectiveStates.Paused => ($"Deixado de lado em {DayText(o.EndedDay)}: {ReasonPt(o.EndReason)}", UiTokens.TextSecondary),
                _ => ($"Abandonado em {DayText(o.EndedDay)}: {ReasonPt(o.EndReason)}", UiTokens.Danger),
            };
            var r = Row(10);
            r.AddChild(new TraitBadge(ObjectiveInfo.Icon(_ctx.Content, o.Id), color, 24));
            var body = Column(2);
            body.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            var name = ThemeFactory.Text(ObjectiveInfo.Title(_ctx.Content, o.Id, o.Param), UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Medium);
            name.ClipText = true;
            body.AddChild(name);
            body.AddChild(ThemeFactory.Text(text, UiTokens.FontCaption, color));
            r.AddChild(body);
            var tip = new TipPanel
            {
                TooltipText = $"{ObjectiveInfo.Title(_ctx.Content, o.Id, o.Param)}|{ObjectiveInfo.TierPt(o.Tier)}\n{text}"
                    + (o.TimesHeld > 0 ? $"\nVezes alcançado|{o.TimesHeld}" : "") + (o.TimesLost > 0 ? $"\nVezes perdido|{o.TimesLost}" : "")
                    + (o.LinkedSet > 0 ? $"\nMetas ligadas|{o.LinkedSet} ({o.LinkedCompleted} concluídas)" : ""),
                MouseFilter = MouseFilterEnum.Stop,
            };
            tip.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
            tip.AddChild(r);
            col.AddChild(tip);
        }
        return col;
    }

    /// <summary>Motivo de uma mudanca de objetivo, em portugues.</summary>
    private string ReasonPt(string reason)
    {
        if (reason.Length == 0 || reason == ObjectiveSystem.InitialOrigin) return "";
        var parts = reason.Split('|').Select(part =>
        {
            int colon = part.IndexOf(':');
            string key = colon > 0 ? part[..colon] : part, arg = colon > 0 ? part[(colon + 1)..] : "";
            string name = ObjectiveInfo.Title(_ctx.Content, arg, "");
            return key switch
            {
                ObjectiveTriggers.Completed => $"depois de alcançar “{name}”",
                ObjectiveTriggers.Abandoned => $"depois de abandonar “{name}”",
                ObjectiveTriggers.ConditionLost => arg.Length == 0 ? "a condição deixou de valer" : $"depois de perder “{name}”",
                ObjectiveTriggers.MaxLevel => "ao chegar ao nível máximo",
                ObjectiveTriggers.ContentKnown => $"ao conhecer {_ctx.ZoneName(arg)}",
                "related" => $"ligado a “{name}”",
                "replaced" => $"trocado por “{name}”",
                "repeated_failures" => "metas que o avançariam falharam em seguida",
                "no_general" => "sem objetivo geral ativo",
                "no_progress" => "sem progresso por muitos dias de jogo",
                "level_up" => "ao subir de nível",
                "quest_completed" => "ao concluir uma missão",
                "zone_discovered" => "ao descobrir uma zona",
                _ => part,
            };
        });
        return string.Join(", ", parts);
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
        bool done = o.State == ObjectiveStates.Completed;
        string state = done ? $"Concluído em {PtBr.ShortDate(_ctx.Calendar.ToDateTime(_ctx.Calendar.StartOfDay(o.CompletedDay)))}"
            : a.CriterionMet ? "Critério cumprido" : ObjectiveInfo.ViabilityPt(a.Viability);
        tags.AddChild(ThemeFactory.Text(state, UiTokens.FontCaption, done || a.CriterionMet ? UiTokens.Success : vc, UiFonts.Medium));
        body.AddChild(tags);

        // Progresso so quando mensuravel com o que existe (sem progresso ficticio).
        if (a.Measurable)
        {
            var pr = Row(8);
            var bar = new ThinBar(a.Progress / (float)Math.Max(1, a.ProgressTotal), vc) { CustomMinimumSize = new Vector2(0, 4), SizeFlagsHorizontal = SizeFlags.ExpandFill, SizeFlagsVertical = SizeFlags.ShrinkCenter };
            pr.AddChild(bar);
            string unit = def?.Criterion switch { ObjectiveCriteria.MainCampaign => " missões", ObjectiveCriteria.AllZones => " zonas", ObjectiveCriteria.MaxLevel => "", _ => "" };
            pr.AddChild(ThemeFactory.Text($"{a.Progress} de {a.ProgressTotal}{unit}", UiTokens.FontCaption, UiTokens.TextLabel, UiFonts.Numbers));
            body.AddChild(pr);
        }
        // Preparacao possivel (requisito concreto) separada da etapa final indisponivel.
        if (done) { }
        else if (a.PreparationLevel > 0)
            body.AddChild(Hint("footprints", $"Preparação: nível {a.PreparationLevel} ({ObjectiveInfo.SubjectPt(_ctx, a.PreparationSubject)})"
                + (a.PreparationKnown ? "" : " · a pessoa ainda não conhece este lugar"), a.PreparationKnown ? UiTokens.Warning : UiTokens.TextSecondary));
        else if (a.Viability == ObjectiveViability.Future && HasPreparationKind(def))
            body.AddChild(Hint("circle-check", "Requisitos de nível conhecidos: cumpridos", UiTokens.Success));
        if (a.MissingSystems.Count > 0)
            body.AddChild(Hint("hourglass", "Etapa final indisponível: depende de " + string.Join(" e ", a.MissingSystems.Select(ObjectiveInfo.SystemPt)), UiTokens.TextSecondary));
        if (o.LinkedSet > 0)
            body.AddChild(Hint("link", $"Metas ligadas: {N(o.LinkedSet, "adotada", "adotadas")} · {N(o.LinkedCompleted, "concluída", "concluídas")} · {N(o.LinkedAbandoned, "abandonada", "abandonadas")}", UiTokens.TextSecondary));
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
        var col = Column(12);
        if (v.Goals.Count == 0)
            col.AddChild(Wrap("Sem meta agora. Nem todo jogador tem uma o tempo todo; ao conectar, pode adotar uma conforme as preferências, a competitividade e a personalidade.", UiTokens.FontSmall, UiTokens.TextSecondary));
        foreach (var g in v.Goals) col.AddChild(GoalItem(v, g, full: true));
        return col;
    }

    /// <summary>Quantidade com singular ou plural (1 concluída, 2 concluídas).</summary>
    private static string N(int n, string one, string many) => $"{n} {(n == 1 ? one : many)}";

    /// <summary>"Objetivo (avanço)" ou "Objetivo (preparação: nível do chefe)", separados por ponto.</summary>
    private string LinksText(IReadOnlyList<ObjectiveLink> links) =>
        string.Join(" · ", links.Select(l =>
        {
            string name = _ctx.Content.Objectives.TryGetValue(l.ObjectiveId, out var d) ? d.Name : l.ObjectiveId;
            return l.Kind == ObjectiveLinkKinds.Advance ? $"{name} (avanço)" : $"{name} (preparação: {ObjectiveInfo.SubjectPt(_ctx, l.Subject)})";
        }));

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
                "completed" => "Concluiu",
                "held" => "Alcançou",
                "lost" => "Perdeu",
                "paused" => "Deixou de lado",
                "resumed" => "Retomou",
                "recovered" => "Quer recuperar",
                "readopted" => "Voltou a querer",
                "abandoned" => "Abandonou",
                "promoted" => "Passou a geral",
                "demoted" => "Passou a secundário",
                "importance_down" => "Perdeu importância",
                "gap" => "Sem objetivo relevante",
                _ => h.Change,
            };
            string why = ReasonPt(h.Reason);
            var r = Row(8);
            r.AddChild(new Icon(h.Change == "gap" ? "circle-alert" : ObjectiveInfo.Icon(_ctx.Content, h.Id), 14,
                h.Change == "gap" ? UiTokens.Warning : h.Tier == ObjectiveTiers.General ? UiTokens.Accent : UiTokens.Info) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
            string subject = h.Change == "gap" ? "buscou um objetivo novo e nenhum era relevante" : $"{ObjectiveInfo.Title(_ctx.Content, h.Id, h.Param)} ({(h.Tier == ObjectiveTiers.General ? "geral" : "secundário")})";
            var l = ThemeFactory.Text($"{what}: {subject}{(why.Length > 0 ? " — " + why : "")}", UiTokens.FontSmall, UiTokens.TextLabel);
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
        if (d != null && main == null && d.LastObjectiveGapDay >= 0)
        {
            // Sem ambicao ativa por falta de objetivo relevante: aparece, porque e um problema do MMORPG.
            var g = HoverRow(() => OpenTab("Objetivos"), "Sem ambição ativa|Falta de conteúdo\nBuscou um objetivo novo e nenhum era relevante no conteúdo atual.\n~Clique para abrir a aba Objetivos.");
            g.CustomMinimumSize = new Vector2(0, 28);
            var gr = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            gr.SetAnchorsPreset(LayoutPreset.FullRect);
            gr.OffsetLeft = 4; gr.OffsetRight = -4;
            gr.AddThemeConstantOverride("separation", 8);
            gr.AddChild(new Icon("circle-alert", 16, UiTokens.Warning) { SizeFlagsVertical = SizeFlags.ShrinkCenter });
            var gl = ThemeFactory.Text("Sem ambição ativa: nenhum objetivo relevante no conteúdo", UiTokens.FontSmall, UiTokens.Warning, UiFonts.Medium);
            gl.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            gl.VerticalAlignment = VerticalAlignment.Center;
            gl.ClipText = true;
            gr.AddChild(gl);
            g.AddChild(gr);
            return g;
        }
        if (d == null || main == null) return null;
        int others = d.Objectives.Count(o => o.State == ObjectiveStates.Active) - 1;
        var c = _ctx.Content;
        var b = HoverRow(() => OpenTab("Objetivos"), $"{ObjectiveInfo.Title(c, main.Id, main.Param)}|{ObjectiveInfo.TierPt(main.Tier)}\n{ObjectiveInfo.ViabilityPt(main.Assessment.Viability)}\n~Clique para abrir a aba Objetivos.");
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

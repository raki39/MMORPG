using Godot;
using Simulation.Core.Behavior;
using Simulation.Core.Content;
using Simulation.Core.Population;
using GodotClient.Map;
using GodotClient.Sim;

namespace GodotClient.Ui;

/// <summary>
/// Cartao-resumo do mapa (D-034): o primeiro clique num jogador, zona ou area abre este
/// cartao pequeno ao lado do cursor, com o essencial ao vivo. "Ver informações" abre a
/// pagina completa (o tempo pausa). Fecha com Esc, com o X ou clicando em outra coisa.
/// </summary>
public partial class QuickCard : PanelContainer
{
    private readonly Func<Control> _build;
    private readonly MarginContainer _body;
    private Vector2 _anchor;
    private readonly Rect2 _bounds;

    public string CardKey { get; }

    private QuickCard(string key, Color accent, Func<Control> build, Vector2 anchor, Rect2 bounds)
    {
        CardKey = key;
        _build = build;
        _anchor = anchor;
        _bounds = bounds;
        MouseFilter = MouseFilterEnum.Stop;
        CustomMinimumSize = new Vector2(340, 0);
        var sb = new StyleBoxFlat { BgColor = UiTokens.SurfaceRaised, BorderColor = UiTokens.BorderStrong, ShadowColor = new Color(0, 0, 0, 0.55f), ShadowSize = 14, ShadowOffset = new Vector2(0, 4) };
        sb.SetBorderWidthAll(1);
        sb.BorderWidthTop = 2;
        sb.BorderColor = UiTokens.BorderStrong;
        sb.SetCornerRadiusAll(4);
        AddThemeStyleboxOverride("panel", sb);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 0);
        AddChild(col);
        col.AddChild(new ColorRect { Color = accent, CustomMinimumSize = new Vector2(0, 2) });
        _body = new MarginContainer();
        foreach (var (k, v) in new[] { ("margin_left", 14), ("margin_right", 14), ("margin_top", 12), ("margin_bottom", 12) }) _body.AddThemeConstantOverride(k, v);
        col.AddChild(_body);
        Refresh();
    }

    public void Refresh()
    {
        foreach (var c in _body.GetChildren()) { _body.RemoveChild(c); c.QueueFree(); }
        _body.AddChild(_build());
        CallDeferred(nameof(Place));
    }

    private void Place()
    {
        ResetSize();
        var size = Size;
        var pos = _anchor + new Vector2(22, -size.Y * 0.35f);
        if (pos.X + size.X > _bounds.End.X) pos.X = _anchor.X - size.X - 22;
        pos.X = Mathf.Clamp(pos.X, _bounds.Position.X, Mathf.Max(_bounds.Position.X, _bounds.End.X - size.X));
        pos.Y = Mathf.Clamp(pos.Y, _bounds.Position.Y, Mathf.Max(_bounds.Position.Y, _bounds.End.Y - size.Y));
        Position = pos;
    }

    /// <summary>Fechado pelo jogador (X ou Esc), nao por troca de cartao.</summary>
    public event Action? Dismissed;

    public void Dismiss()
    {
        Dismissed?.Invoke();
        QueueFree();
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            Dismiss();
            GetViewport().SetInputAsHandled();
        }
    }

    // ---------- Partes comuns ----------

    private static Control Head(Control icon, string title, string subtitle, Action close)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 12);
        icon.CustomMinimumSize = new Vector2(40, 40);
        icon.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        row.AddChild(icon);
        var names = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center };
        names.AddThemeConstantOverride("separation", 0);
        var t = ThemeFactory.Text(title, 16, UiTokens.TextPrimary, UiFonts.SemiBold);
        t.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        names.AddChild(t);
        var s = ThemeFactory.Text(subtitle, UiTokens.FontSmall, UiTokens.TextSecondary);
        s.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        names.AddChild(s);
        row.AddChild(names);
        var x = new Button { Text = "✕", Flat = true, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(24, 24), TooltipText = "Fechar (Esc)", SizeFlagsVertical = SizeFlags.ShrinkBegin };
        x.AddThemeColorOverride("font_color", UiTokens.TextSecondary);
        x.AddThemeFontSizeOverride("font_size", 12);
        x.Pressed += close;
        row.AddChild(x);
        return row;
    }

    /// <summary>Grade de mini indicadores (valor em cima, rotulo embaixo).</summary>
    private static Control Stats(params (string Value, string Caption, string Tip)[] items)
    {
        var grid = new HBoxContainer();
        grid.AddThemeConstantOverride("separation", 0);
        for (int i = 0; i < items.Length; i++)
        {
            if (i > 0) grid.AddChild(new ColorRect { Color = UiTokens.Border, CustomMinimumSize = new Vector2(1, 26), SizeFlagsVertical = SizeFlags.ShrinkCenter });
            var cell = new TipPanel { TooltipText = items[i].Tip, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = items[i].Tip.Length > 0 ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore };
            cell.AddThemeStyleboxOverride("panel", new StyleBoxEmpty { ContentMarginLeft = i == 0 ? 0 : 10, ContentMarginRight = 6 });
            var c = new VBoxContainer();
            c.AddThemeConstantOverride("separation", 0);
            var v = ThemeFactory.Text(items[i].Value, 14, UiTokens.TextPrimary, UiFonts.Numbers);
            v.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            c.AddChild(v);
            c.AddChild(ThemeFactory.Text(items[i].Caption, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium));
            cell.AddChild(c);
            grid.AddChild(cell);
        }
        return grid;
    }

    private static Control Footer(Action more, params Button[] extra)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        var b = ModalFrame.Action("Ver informações  ›", "Abre a página completa\n~O tempo pausa enquanto você estiver fora do mapa.", more, primary: true);
        b.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(b);
        foreach (var e in extra) row.AddChild(e);
        return row;
    }

    private static Label Line(string text, Color? color = null, int size = UiTokens.FontSmall)
    {
        var l = ThemeFactory.Text(text, size, color ?? UiTokens.TextSecondary);
        l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        l.CustomMinimumSize = new Vector2(300, 0);
        return l;
    }

    private static VBoxContainer Stack(params Control[] items) => ModalFrame.Column(10, items);

    // ---------- Jogador ----------

    public static QuickCard Person(ClientContext ctx, int personId, Vector2 anchor, Rect2 bounds, Action openPage)
    {
        var p = ctx.Persons[personId - 1];
        QuickCard? self = null;
        Control Build()
        {
            var snap = ctx.Snapshot();
            var v = personId < snap.ByPerson.Length ? snap.ByPerson[personId] : null;
            string profile = ctx.Content.Profiles.TryGetValue(p.ProfileId, out var pr) ? pr.Name : p.ProfileId;
            var icon = new PortraitBadge(p.AppearanceSeed, UiTokens.ProfileColor(p.ProfileId), v?.Name ?? p.Name);
            if (v == null)
                return Stack(Head(icon, p.Name, $"{profile} · ainda sem conta", () => self?.Dismiss()), Line("Interessado no MMORPG; pode criar a conta nos próximos dias."), Footer(openPage));
            string cls = ctx.Content.Classes.TryGetValue(v.ClassId, out var c) ? c.Name : v.ClassId;
            bool online = v.Status != CharacterStatus.Offline;
            long now = snap.Tick;
            long need = ProgressionRules.XpToNextLevel(ctx.Content.Progression, v.Level);
            var col = Stack(Head(icon, (ctx.Favorites.Contains(personId) ? "★ " : "") + v.Name, $"Nv {v.Level} · {cls} · {profile}", () => self?.Dismiss()));
            var status = ModalFrame.Column(4, ThemeFactory.Text(StatusText.Of(v, ctx.Content), 13, online ? UiTokens.TextPrimary : UiTokens.TextSecondary, UiFonts.Medium));
            if (v.Status == CharacterStatus.Doing && v.ActivityEndTick > v.ActivityStartTick)
                status.AddChild(new ThinBar((now - v.ActivityStartTick) / (float)(v.ActivityEndTick - v.ActivityStartTick), UiTokens.Accent) { CustomMinimumSize = new Vector2(0, 4) });
            else if (v.Status == CharacterStatus.Traveling && v.MoveEndTick > v.MoveStartTick)
                status.AddChild(new ThinBar((now - v.MoveStartTick) / (float)(v.MoveEndTick - v.MoveStartTick), UiTokens.Info) { CustomMinimumSize = new Vector2(0, 4) });
            col.AddChild(status);
            col.AddChild(Stats(
                (v.Level.ToString(), "Nível", ""),
                (v.Level >= ctx.Content.Progression.LevelCap ? "máx." : $"{v.Experience * 100 / Math.Max(1, need)}%", "Do próximo", $"XP\nAtual|{v.Experience}\nPróximo nível|{need}"),
                (online ? ctx.Clock(v.SessionEndTick) : "—", online ? "Sai às" : "Offline", online ? "Fim previsto da sessão" : ""),
                ($"{v.ActiveQuests.Count}", "Missões", "Missões em andamento")));
            if (v.LastDecision is { } d)
            {
                var why = d.Factors.Where(f => f.Contribution > 0).OrderByDescending(f => f.Contribution).Take(2).Select(f => PlayerModal.FactorPt(f.Key).ToLowerInvariant());
                col.AddChild(Line($"Escolheu {ctx.ActivityName(d.RegionId, d.ActivityId).ToLowerInvariant()} por: {string.Join(", ", why)}."));
            }
            col.AddChild(Footer(openPage,
                ModalFrame.Action("Seguir", "Seguir no mapa\n~Centraliza e acompanha este jogador.", () => ctx.FocusPerson(personId, true), enabled: online)));
            return col;
        }
        self = new QuickCard("person#" + personId, UiTokens.ProfileColor(p.ProfileId), Build, anchor, bounds);
        return self;
    }

    // ---------- Zona ----------

    public static QuickCard Zone(ClientContext ctx, WorldMapView map, string zoneId, Vector2 anchor, Rect2 bounds, Action openPage)
    {
        var z = ctx.Content.Regions[zoneId];
        QuickCard? self = null;
        Control Build()
        {
            int people = map.PeopleInZone(z.Id);
            string sub = z.IsCity ? $"{PtBr.Function(z.Function)} · nível {z.CityLevel} · todos os níveis" : $"{PtBr.Function(z.Function)} · {PtBr.Biome(z.Biome)} · Nv {z.LevelMin}–{z.LevelMax}";
            var col = Stack(Head(new ZoneEmblem(z), z.Name, sub, () => self?.Dismiss()));
            col.AddChild(Stats(
                (people.ToString(), "Jogadores", "Jogadores na zona agora"),
                ($"{people * 100 / Math.Max(1, z.Capacity)}%", "Lotação", $"Lotação\nJogadores|{people}\nCapacidade|{z.Capacity}"),
                (((int)(WorldMapView.DangerOf(z) * 100)).ToString(), "Perigo", "Índice de perigo\n~Risco das atividades, PvP e regra de morte."),
                (z.Areas.Count.ToString(), "Áreas", "")));
            col.AddChild(new ThinBar(people / (float)Math.Max(1, z.Capacity) * 4f, WorldMapView.Ramp(Mathf.Clamp(people / (float)Math.Max(1, z.Capacity) * 4f, 0, 1))) { CustomMinimumSize = new Vector2(0, 4), TooltipText = "Lotação (25% da capacidade enche a barra)" });
            var busiest = z.Areas.Select(a => (a, n: map.PeopleInArea(z.Id, a.Id).Count)).Where(x => x.n > 0).OrderByDescending(x => x.n).Take(3).ToList();
            col.AddChild(Line(busiest.Count == 0 ? "Nenhuma área ocupada agora." : "Mais cheias: " + string.Join(" · ", busiest.Select(x => $"{x.a.Name} ({x.n})"))));
            col.AddChild(Line($"{PtBr.Pvp(z.Pvp)} · {PtBr.Death(z.DeathRule)}", UiTokens.TextDisabled, UiTokens.FontCaption));
            col.AddChild(Footer(openPage, ModalFrame.Action("Enquadrar", "Centraliza e aproxima a zona", () => ctx.FocusZone(z.Id))));
            return col;
        }
        self = new QuickCard("zone#" + zoneId, z.IsCity ? UiTokens.Accent : z.Pvp == PvpRules.Open ? UiTokens.Danger : UiTokens.Success, Build, anchor, bounds);
        return self;
    }

    // ---------- Area ----------

    public static QuickCard Area(ClientContext ctx, WorldMapView map, string zoneId, string areaId, Vector2 anchor, Rect2 bounds, Action openPage)
    {
        var z = ctx.Content.Regions[zoneId];
        var a = z.Areas.First(x => x.Id == areaId);
        QuickCard? self = null;
        Control Build()
        {
            int people = map.PeopleInArea(z.Id, a.Id).Count;
            var board = ctx.Content.QuestBoards.Values.FirstOrDefault(b => b.RegionId == zoneId && b.AreaId == areaId);
            string sub = $"{AreaPalette.Name(a.Kind)} · {z.Name}" + (a.LevelMin > 0 ? $" · Nv {a.LevelMin}–{a.LevelMax}" : "");
            var col = Stack(Head(new KindEmblem(a.Kind), a.Name, sub, () => self?.Dismiss()));
            col.AddChild(Stats(
                (people.ToString(), "Jogadores", "Pessoas na área agora"),
                (a.Activities.Count.ToString(), "Tarefas", ""),
                (board != null ? ctx.Content.QuestsByBoard[board.Id].Count.ToString() : "—", "Missões", board != null ? board.Name : "Sem quadro de missões"),
                ($"{a.Radius * 20} m", "Tamanho", "Diâmetro da área")));
            if (a.Activities.Count > 0)
                col.AddChild(Line(string.Join(" · ", a.Activities.Select(x => $"{x.Name} ({map.PeopleAtSpot(z.Id, x.Id)})"))));
            else if (a.Description.Length > 0) col.AddChild(Line(a.Description));
            col.AddChild(Footer(openPage, ModalFrame.Action("Enquadrar", "Centraliza a área", () => ctx.FocusArea(z.Id, a.Id))));
            return col;
        }
        self = new QuickCard("area#" + zoneId + "#" + areaId, AreaPalette.Color(a.Kind), Build, anchor, bounds);
        return self;
    }
}

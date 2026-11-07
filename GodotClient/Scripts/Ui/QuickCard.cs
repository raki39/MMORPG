using Godot;
using Simulation.Core.Behavior;
using Simulation.Core.Combat;
using Simulation.Core.Content;
using Simulation.Core.Population;
using GodotClient.Map;
using GodotClient.Sim;

namespace GodotClient.Ui;

/// <summary>
/// Cartao-resumo do mapa (D-034; refeito no padrao das paginas, D-068): o clique num jogador, zona ou area abre
/// este cartao ao lado do cursor, com o essencial ao vivo. Cabecalho como o das paginas (emblema, nome, linhas
/// de identificacao), etiquetas com rotulo e valor, secoes com titulo. Arrasta pelo cabecalho. O alfinete fixa:
/// o cartao fica aberto (outros cliques nao o fecham) e entra na aba Fixados do painel lateral. "Ver
/// informacoes" abre a pagina completa (o tempo pausa).
/// </summary>
public partial class QuickCard : PanelContainer
{
    public const int Width = 364;

    /// <summary>O que o cartao mostra: emblema, titulo, duas linhas de identificacao e o corpo.</summary>
    private readonly record struct Parts(Control Emblem, string Title, string Line1, string Line2, Control Body);

    private readonly Func<Parts> _build;
    private readonly HBoxContainer _headSlot;
    private readonly MarginContainer _body;
    private readonly Button _pin;
    private Vector2 _anchor;
    private Rect2 _bounds;
    private bool _dragging, _moved;
    private Vector2 _grab;

    public string CardKey { get; }
    public bool Pinned { get; private set; }
    /// <summary>Fixado e fechado pelo X: continua na lista de fixados, invisivel ate ser reaberto.</summary>
    public bool UserHidden { get; set; }

    /// <summary>Fechado pelo jogador (X ou Esc), nao por troca de cartao (so cartoes nao fixados).</summary>
    public event Action? Dismissed;
    /// <summary>O alfinete mudou (o mapa atualiza a lista de fixados).</summary>
    public event Action<QuickCard>? PinToggled;

    private QuickCard(string key, Color accent, Func<Parts> build, Vector2 anchor, Rect2 bounds)
    {
        CardKey = key;
        _build = build;
        _anchor = anchor;
        _bounds = bounds;
        MouseFilter = MouseFilterEnum.Stop;
        CustomMinimumSize = new Vector2(Width, 0);
        var sb = new StyleBoxFlat { BgColor = UiTokens.CardTop, BorderColor = UiTokens.BorderStrong, ShadowColor = new Color(0, 0, 0, 0.6f), ShadowSize = 16, ShadowOffset = new Vector2(0, 5) };
        sb.SetBorderWidthAll(1);
        sb.SetCornerRadiusAll(4);
        AddThemeStyleboxOverride("panel", sb);
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 0);
        AddChild(col);
        col.AddChild(new ColorRect { Color = accent, CustomMinimumSize = new Vector2(0, 2), MouseFilter = MouseFilterEnum.Ignore });

        // Cabecalho: arrasta o cartao; botoes de fixar e fechar a direita.
        var head = new PanelContainer { MouseFilter = MouseFilterEnum.Stop, MouseDefaultCursorShape = CursorShape.Move, TooltipText = "" };
        head.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = new Color(UiTokens.Surface, 0.9f), BorderColor = UiTokens.Divider, BorderWidthBottom = 1,
            ContentMarginLeft = 12, ContentMarginRight = 8, ContentMarginTop = 10, ContentMarginBottom = 10,
        });
        var headRow = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        headRow.AddThemeConstantOverride("separation", 12);
        _headSlot = new HBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Ignore };
        _headSlot.AddThemeConstantOverride("separation", 12);
        headRow.AddChild(_headSlot);
        var buttons = new VBoxContainer { SizeFlagsVertical = SizeFlags.ShrinkBegin };
        buttons.AddThemeConstantOverride("separation", 2);
        var close = IconButton("x", "Fechar (Esc)\n~Fixado, o cartão continua na aba Fixados do painel lateral.");
        close.Pressed += Dismiss;
        _pin = IconButton("pin", "");
        _pin.Pressed += TogglePin;
        buttons.AddChild(close);
        buttons.AddChild(_pin);
        headRow.AddChild(buttons);
        head.AddChild(headRow);
        head.GuiInput += OnHeadInput;
        col.AddChild(head);

        _body = new MarginContainer();
        foreach (var (k, v) in new[] { ("margin_left", 14), ("margin_right", 14), ("margin_top", 12), ("margin_bottom", 14) }) _body.AddThemeConstantOverride(k, v);
        col.AddChild(_body);
        SetPinned(false);
        Refresh();
    }

    private static Button IconButton(string icon, string tip)
    {
        var b = new TipButton { TooltipText = tip, FocusMode = FocusModeEnum.None, CustomMinimumSize = new Vector2(26, 26), Icon = Icons.Get(icon, 15), IconAlignment = HorizontalAlignment.Center, MouseDefaultCursorShape = CursorShape.PointingHand };
        b.AddThemeStyleboxOverride("normal", new StyleBoxEmpty());
        var hover = ThemeFactory.Box(UiTokens.SurfaceHover, 0, radius: 3);
        b.AddThemeStyleboxOverride("hover", hover);
        b.AddThemeStyleboxOverride("pressed", hover);
        foreach (var st in new[] { "icon_normal_color", "icon_hover_color", "icon_pressed_color", "icon_focus_color" }) b.AddThemeColorOverride(st, UiTokens.TextLabel);
        return b;
    }

    /// <summary>O alfinete: fixa ou solta e avisa o mapa.</summary>
    public void TogglePin()
    {
        SetPinned(!Pinned);
        PinToggled?.Invoke(this);
    }

    /// <summary>Fixa ou solta o cartao (o alfinete fica dourado e com fundo quando fixado).</summary>
    public void SetPinned(bool on)
    {
        Pinned = on;
        var c = on ? UiTokens.Accent : UiTokens.TextLabel;
        foreach (var st in new[] { "icon_normal_color", "icon_hover_color", "icon_pressed_color", "icon_focus_color" }) _pin.AddThemeColorOverride(st, c);
        _pin.AddThemeStyleboxOverride("normal", on ? ThemeFactory.Box(new Color(UiTokens.Accent, 0.16f), 0, radius: 3) : new StyleBoxEmpty());
        _pin.TooltipText = on
            ? "Fixado\n~Fica aberto e na aba Fixados do painel lateral. Clique para soltar."
            : "Fixar\n~O cartão fica aberto (outros cliques no mapa não o fecham) e entra na aba Fixados do painel lateral.";
    }

    public void Refresh()
    {
        var parts = _build();
        foreach (var c in _headSlot.GetChildren()) { _headSlot.RemoveChild(c); c.QueueFree(); }
        parts.Emblem.CustomMinimumSize = new Vector2(52, 52);
        parts.Emblem.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        parts.Emblem.MouseFilter = MouseFilterEnum.Ignore;
        _headSlot.AddChild(parts.Emblem);
        var names = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill, Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
        names.AddThemeConstantOverride("separation", 1);
        var t = ThemeFactory.Text(parts.Title, 17, UiTokens.TextPrimary, UiFonts.SemiBold);
        t.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        names.AddChild(t);
        foreach (var (line, color) in new[] { (parts.Line1, UiTokens.TextLabel), (parts.Line2, UiTokens.TextSecondary) })
        {
            if (line.Length == 0) continue;
            var l = ThemeFactory.Text(line, UiTokens.FontSmall, color, UiFonts.Medium);
            l.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            names.AddChild(l);
        }
        _headSlot.AddChild(names);
        foreach (var c in _body.GetChildren()) { _body.RemoveChild(c); c.QueueFree(); }
        _body.AddChild(parts.Body);
        CallDeferred(nameof(Place));
    }

    /// <summary>Area da tela onde o cartao pode ficar (muda com o painel lateral aberto ou fechado).</summary>
    public void SetBounds(Rect2 bounds)
    {
        _bounds = bounds;
        Position = Clamp(Position);
    }

    private Vector2 Clamp(Vector2 p) => new(
        Mathf.Clamp(p.X, _bounds.Position.X, Mathf.Max(_bounds.Position.X, _bounds.End.X - Size.X)),
        Mathf.Clamp(p.Y, _bounds.Position.Y, Mathf.Max(_bounds.Position.Y, _bounds.End.Y - Size.Y)));

    private void Place()
    {
        ResetSize();
        if (_moved) { Position = Clamp(Position); return; }
        var size = Size;
        var pos = _anchor + new Vector2(22, -size.Y * 0.3f);
        if (pos.X + size.X > _bounds.End.X) pos.X = _anchor.X - size.X - 22;
        Position = Clamp(pos);
    }

    /// <summary>Coloca o cartao num ponto (reabrir um fixado) sem seguir mais o cursor.</summary>
    public void PlaceAt(Vector2 anchor)
    {
        _anchor = anchor;
        _moved = false;
        CallDeferred(nameof(Place));
    }

    private void OnHeadInput(InputEvent e)
    {
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left } mb)
        {
            _dragging = mb.Pressed;
            if (mb.Pressed) { _grab = mb.GlobalPosition - GlobalPosition; MoveToFront(); }
            AcceptEvent();
        }
        else if (e is InputEventMouseMotion mm && _dragging)
        {
            // Posicao do proprio evento: o botao pressionado mantem o foco do mouse no cabecalho ate soltar.
            Position = Clamp(mm.GlobalPosition - _grab - (GetParent() is Control parent ? parent.GlobalPosition : Vector2.Zero));
            _moved = true;
            AcceptEvent();
        }
    }

    public void Dismiss()
    {
        if (Pinned)
        {
            UserHidden = true;
            Visible = false;
            return;
        }
        Dismissed?.Invoke();
        QueueFree();
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (Pinned || !Visible) return;
        if (e is InputEventKey { Pressed: true, Keycode: Key.Escape })
        {
            Dismiss();
            GetViewport().SetInputAsHandled();
        }
    }

    // ---------- Partes comuns ----------

    /// <summary>Etiquetas como as do cabecalho das paginas: rotulo curto em cima, icone e valor embaixo, filetes.</summary>
    private static Control Tags(params (string Icon, string Caption, string Value, Color Color, string Tip)[] items)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 0);
        for (int i = 0; i < items.Length; i++)
        {
            if (i > 0) row.AddChild(new ColorRect { Color = UiTokens.Divider, CustomMinimumSize = new Vector2(1, 30), SizeFlagsVertical = SizeFlags.ShrinkCenter });
            var (icon, caption, value, color, tip) = items[i];
            var cell = new TipPanel { TooltipText = tip, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = tip.Length > 0 ? MouseFilterEnum.Stop : MouseFilterEnum.Ignore };
            cell.AddThemeStyleboxOverride("panel", new StyleBoxEmpty { ContentMarginLeft = i == 0 ? 0 : 10, ContentMarginRight = 4 });
            var c = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            c.AddThemeConstantOverride("separation", 2);
            c.AddChild(ThemeFactory.Text(caption, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium));
            var r = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
            r.AddThemeConstantOverride("separation", 6);
            r.AddChild(new Icon(icon, 14, color));
            var v = ThemeFactory.Text(value, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.SemiBold);
            v.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            v.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            r.AddChild(v);
            c.AddChild(r);
            cell.AddChild(c);
            row.AddChild(cell);
        }
        return row;
    }

    /// <summary>Numero pequeno com rotulo (grade da secao Personagem).</summary>
    private static Control Mini(string value, string caption, string tip, Color? color = null)
    {
        var p = new TipPanel { TooltipText = tip, SizeFlagsHorizontal = SizeFlags.ExpandFill, MouseFilter = MouseFilterEnum.Stop };
        p.AddThemeStyleboxOverride("panel", new StyleBoxEmpty());
        var c = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        c.AddThemeConstantOverride("separation", 0);
        c.AddChild(ThemeFactory.Text(value, 15, color ?? UiTokens.TextPrimary, UiFonts.Numbers));
        c.AddChild(ThemeFactory.Text(caption, UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium));
        p.AddChild(c);
        return p;
    }

    /// <summary>Linha de lista dentro do cartao: icone, nome, detalhe e numero a direita (clicavel).</summary>
    private static Control ListLine(Control icon, string name, string detail, string count, Color countColor, Action? open, string tip)
    {
        var b = ModalFrame.HoverRow(open ?? (() => { }), tip);
        if (open == null) b.MouseDefaultCursorShape = CursorShape.Arrow;
        b.CustomMinimumSize = new Vector2(0, 30);
        var r = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        r.SetAnchorsPreset(LayoutPreset.FullRect);
        r.OffsetLeft = 4; r.OffsetRight = -4;
        r.AddThemeConstantOverride("separation", 9);
        icon.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        icon.MouseFilter = MouseFilterEnum.Ignore;
        r.AddChild(icon);
        var n = ThemeFactory.Text(name, UiTokens.FontSmall, UiTokens.TextPrimary, UiFonts.Medium);
        n.VerticalAlignment = VerticalAlignment.Center;
        n.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
        n.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        r.AddChild(n);
        if (detail.Length > 0)
        {
            var d = ThemeFactory.Text(detail, UiTokens.FontCaption, UiTokens.TextSecondary);
            d.VerticalAlignment = VerticalAlignment.Center;
            r.AddChild(d);
        }
        var c = ThemeFactory.Text(count, UiTokens.FontBody, countColor, UiFonts.Numbers);
        c.VerticalAlignment = VerticalAlignment.Center;
        c.CustomMinimumSize = new Vector2(22, 0);
        c.HorizontalAlignment = HorizontalAlignment.Right;
        r.AddChild(c);
        b.AddChild(r);
        return b;
    }

    private static Control Footer(Action more, params Button[] extra)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 6);
        var b = ModalFrame.Action("Ver informações  ›", "Abre a página completa\n~O tempo pausa enquanto você estiver fora do mapa.", more, primary: true);
        b.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        b.CustomMinimumSize = new Vector2(0, 30);
        row.AddChild(b);
        foreach (var e in extra) { e.CustomMinimumSize = new Vector2(0, 30); row.AddChild(e); }
        return row;
    }

    private static Label Note(string text, Color? color = null)
    {
        var l = ThemeFactory.Text(text, UiTokens.FontSmall, color ?? UiTokens.TextSecondary);
        l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        l.CustomMinimumSize = new Vector2(Width - 40, 0);
        return l;
    }

    private static VBoxContainer Stack(int sep, params Control[] items) => ModalFrame.Column(sep, items);

    // ---------- Jogador ----------

    public static QuickCard Person(ClientContext ctx, int personId, Vector2 anchor, Rect2 bounds, Action openPage)
    {
        var p = ctx.Persons[personId - 1];
        Parts Build()
        {
            var snap = ctx.Snapshot();
            var v = personId < snap.ByPerson.Length ? snap.ByPerson[personId] : null;
            string profile = ctx.Content.Profiles.TryGetValue(p.ProfileId, out var pr) ? pr.Name : p.ProfileId;
            var cls = v != null && ctx.Content.Classes.TryGetValue(v.ClassId, out var c) ? c : null;
            var crest = new PortraitBadge(p.AppearanceSeed, UiTokens.ProfileColor(p.ProfileId), v?.Name ?? p.Name) { ClassIcon = v != null ? PlayerModal.ClassIcon(cls) : "" };
            string personality = PlayerModal.PersonalityOf(ctx.Content, PlayerModal.CurrentPersonality(ctx, personId)).Label;
            if (v == null)
                return new Parts(crest, p.Name, $"{profile} · {personality}", "Ainda sem conta no MMORPG",
                    Stack(12, Note("Interessado no MMORPG; pode criar a conta nos próximos dias."), Footer(openPage)));

            bool online = v.Status != CharacterStatus.Offline;
            long now = snap.Tick;
            var body = Stack(12);

            // Agora: o que faz, com a barra do trecho ou da atividade.
            var status = Stack(5);
            var sr = new HBoxContainer();
            sr.AddThemeConstantOverride("separation", 8);
            string type = v.Status == CharacterStatus.Traveling ? "travel" : v.ActivityId.Length > 0 ? (ctx.Content.Regions.TryGetValue(v.TargetRegionId, out var tz) ? tz.FindActivity(v.ActivityId)?.Type ?? "" : "") : "";
            sr.AddChild(new Icon(!online ? "moon" : v.Status == CharacterStatus.Traveling ? "footprints" : PlayerModal.TypeIcon(type), 16,
                !online ? UiTokens.TextSecondary : type == "travel" ? UiTokens.Info : ActivityPalette.Color(type)));
            var st = ThemeFactory.Text(StatusText.Of(v, ctx.Content), 13, online ? UiTokens.TextPrimary : UiTokens.TextSecondary, UiFonts.Medium);
            st.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            st.TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis;
            sr.AddChild(st);
            status.AddChild(sr);
            if (v.Status == CharacterStatus.Doing && v.ActivityEndTick > v.ActivityStartTick)
                status.AddChild(new ThinBar((now - v.ActivityStartTick) / (float)(v.ActivityEndTick - v.ActivityStartTick), UiTokens.Accent) { CustomMinimumSize = new Vector2(0, 4) });
            else if (v.Status == CharacterStatus.Traveling && v.MoveEndTick > v.MoveStartTick)
                status.AddChild(new ThinBar((now - v.MoveStartTick) / (float)(v.MoveEndTick - v.MoveStartTick), UiTokens.Info) { CustomMinimumSize = new Vector2(0, 4) });
            body.AddChild(status);

            long need = ProgressionRules.XpToNextLevel(ctx.Content.Progression, v.Level);
            bool max = v.Level >= ctx.Content.Progression.LevelCap;
            var m = v.Mood;
            body.AddChild(Tags(
                ("g-nivel", "Nível", v.Level.ToString(), UiTokens.Accent, $"Nível {v.Level}\nXP|{PtBr.Int(v.Experience)} / {PtBr.Int(need)}"),
                ("trending-up", "Próximo", max ? "máx." : $"{v.Experience * 100 / Math.Max(1, need)}%", UiTokens.Success, "XP do nível atual até o próximo"),
                ("clock", online ? "Sessão até" : "Sessão", online ? ctx.Clock(v.SessionEndTick) : "offline", online ? UiTokens.Info : UiTokens.TextSecondary, online ? "Fim previsto da sessão" : "Desconectado"),
                ("activity", "Satisfação", $"{m.Satisfaction / 10}%", m.Satisfaction >= 650 ? UiTokens.Success : m.Satisfaction >= 450 ? UiTokens.Accent : UiTokens.Warning, "Satisfação com o jogo\n~Média das notas das sessões.")));

            // Personagem: as estatisticas da classe e do nivel (E1a); o equipamento entra no M5.
            if (cls != null)
            {
                var s = CharacterStats.Sheet(ctx.Content, cls, v.Level).Stats;
                var power = PowerIndex.Compute(ctx.Content.Combat, s);
                var grid = new GridContainer { Columns = 3, SizeFlagsHorizontal = SizeFlags.ExpandFill };
                grid.AddThemeConstantOverride("h_separation", 10);
                grid.AddThemeConstantOverride("v_separation", 8);
                grid.AddChild(Mini(PtBr.Int(power.Power), "Poder", "Índice de poder\n~Estimativa da força do personagem (ataque com crítico × vida efetiva); a referência vale 100.", UiTokens.AccentSoft));
                grid.AddChild(Mini(PtBr.Int(s.Health), "Vida", "Vida máxima\n~Classe e nível; equipamento entra no M5."));
                grid.AddChild(Mini(PtBr.Int(s.Resource), cls.Resource.Name.Length > 0 ? cls.Resource.Name : "Recurso", $"{cls.Resource.Name}\n~Recurso da classe para as habilidades."));
                grid.AddChild(Mini(PtBr.Int(Math.Max(s.PhysicalAttack, s.MagicAttack)), s.MagicAttack > s.PhysicalAttack ? "Ataque mágico" : "Ataque físico", $"Ataque\nFísico|{s.PhysicalAttack}\nMágico|{s.MagicAttack}"));
                grid.AddChild(Mini(PtBr.Int((s.PhysicalDefense + s.MagicDefense) / 2), "Defesa", $"Defesa\nFísica|{s.PhysicalDefense}\nMágica|{s.MagicDefense}"));
                grid.AddChild(Mini($"{s.CritChancePermille / 10}%", "Crítico", $"Crítico\nChance|{s.CritChancePermille / 10}%\nMultiplicador|×{PtBr.Decimal(s.CritMultiplierPermille / 1000.0, 2)}"));
                var equip = new HBoxContainer();
                equip.AddThemeConstantOverride("separation", 6);
                equip.AddChild(new Icon("backpack", 13, UiTokens.TextDisabled));
                equip.AddChild(ThemeFactory.Text("Equipamento e combate chegam no M3 e no M5", UiTokens.FontCaption, UiTokens.TextDisabled));
                body.AddChild(Stack(8, ModalFrame.Section("Personagem", ThemeFactory.Text($"base do nível {v.Level}", UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Medium)), grid, equip));
            }

            // Agora: plano da sessao e a missao em andamento.
            var now2 = Stack(6, ModalFrame.Section("Sessão"));
            if (v.Plan is { } plan)
            {
                var fp = plan.FocusKey.Split('/');
                string focus = fp.Length == 2 ? ctx.ActivityName(fp[0], fp[1]) : plan.FocusKey;
                now2.AddChild(ModalFrame.KeyValue("Plano", focus, UiTokens.AccentSoft));
            }
            if (v.LastDecision is { } d)
            {
                var why = d.Factors.Where(f => f.Contribution > 0).OrderByDescending(f => f.Contribution).Take(2).Select(f => PlayerModal.FactorPt(f.Key).ToLowerInvariant()).ToList();
                if (why.Count > 0) now2.AddChild(ModalFrame.KeyValue("Por quê", string.Join(", ", why)));
            }
            var aq = v.ActiveQuests.FirstOrDefault();
            if (aq != null && ctx.Content.Quests.TryGetValue(aq.QuestId, out var q))
            {
                bool ready = aq.StepIndex >= q.Steps.Count;
                string qid = q.Id;
                now2.AddChild(ListLine(new Icon(GameIcons.Quest(q.Category), 15, QuestModal.CategoryColor(q.Category)), q.Name,
                    ready ? "entregar" : $"etapa {aq.StepIndex + 1}/{q.Steps.Count + 1}", v.ActiveQuests.Count > 1 ? $"+{v.ActiveQuests.Count - 1}" : "",
                    UiTokens.TextSecondary, () => ctx.OpenQuest(qid), $"{q.Name}\n~Missão em andamento ({v.ActiveQuests.Count} no total). Clique para abrir."));
            }
            if (now2.GetChildCount() > 1) body.AddChild(now2);

            body.AddChild(Footer(openPage, ModalFrame.Action("Seguir", "Seguir no mapa\n~Centraliza e acompanha este jogador.", () => ctx.FocusPerson(personId, true), enabled: online)));
            string fav = ctx.Favorites.Contains(personId) ? "★ " : "";
            return new Parts(crest, fav + v.Name, cls != null ? $"{cls.Name} · {PlayerModal.RolePt(cls.Role)} · Nível {v.Level}" : $"Nível {v.Level}",
                $"{profile} · {personality} · {ctx.ZoneName(v.RegionId)}", body);
        }
        return new QuickCard("person#" + personId, UiTokens.ProfileColor(p.ProfileId), Build, anchor, bounds);
    }

    // ---------- Zona ----------

    public static QuickCard Zone(ClientContext ctx, WorldMapView map, string zoneId, Vector2 anchor, Rect2 bounds, Action openPage)
    {
        var z = ctx.Content.Regions[zoneId];
        Parts Build()
        {
            int people = map.PeopleInZone(z.Id);
            float load = people / (float)Math.Max(1, z.Capacity);
            float danger = WorldMapView.DangerOf(z);
            var body = Stack(12);
            body.AddChild(Tags(
                ("users", "Jogadores", people.ToString(), UiTokens.Info, "Jogadores na zona agora"),
                ("gauge", "Lotação", $"{load * 100:0}%", WorldMapView.Ramp(Mathf.Clamp(load * 4f, 0, 1)), $"Lotação\nJogadores|{people}\nCapacidade|{z.Capacity}"),
                ("skull", "Perigo", ((int)(danger * 100)).ToString(), WorldMapView.Ramp(danger), "Índice de perigo\n~Risco das atividades, PvP e regra de morte."),
                ("trending-up", "Níveis", z.IsCity ? "todos" : $"{z.LevelMin}–{z.LevelMax}", UiTokens.Accent, "Faixa de nível da zona")));
            body.AddChild(new ThinBar(Mathf.Clamp(load * 4f, 0, 1), WorldMapView.Ramp(Mathf.Clamp(load * 4f, 0, 1))) { CustomMinimumSize = new Vector2(0, 4), TooltipText = "Lotação (25% da capacidade enche a barra)" });

            var areas = Stack(2, ModalFrame.Section("Áreas", ThemeFactory.Text(z.Areas.Count.ToString(), UiTokens.FontCaption, UiTokens.TextSecondary, UiFonts.Numbers)));
            foreach (var a in z.Areas.OrderByDescending(a => map.PeopleInArea(z.Id, a.Id).Count).ThenBy(a => a.Name, StringComparer.Ordinal).Take(4))
            {
                int n = map.PeopleInArea(z.Id, a.Id).Count;
                string zid = z.Id, aid = a.Id;
                areas.AddChild(ListLine(new KindEmblem(a.Kind) { CustomMinimumSize = new Vector2(22, 22) }, a.Name, AreaPalette.Name(a.Kind), n.ToString(),
                    n > 0 ? UiTokens.TextPrimary : UiTokens.TextDisabled, () => ctx.OpenArea(zid, aid), $"{a.Name}\n~Clique para ver a área."));
            }
            if (z.Areas.Count > 4) areas.AddChild(ThemeFactory.Text($"+{z.Areas.Count - 4} na ficha da zona", UiTokens.FontCaption, UiTokens.TextSecondary));
            body.AddChild(areas);

            var rules = new HBoxContainer();
            rules.AddThemeConstantOverride("separation", 14);
            foreach (var (icon, text, color) in new[]
            {
                ("swords", PtBr.Pvp(z.Pvp), z.Pvp == PvpRules.Open ? UiTokens.Danger : UiTokens.Success),
                ("skull", PtBr.Death(z.DeathRule), z.DeathRule == DeathRules.FullLoot ? UiTokens.Danger : z.DeathRule == DeathRules.Partial ? UiTokens.Warning : UiTokens.TextSecondary),
                ("flag", PtBr.Control(z.Control), z.Control == ControlRules.Contestable ? UiTokens.Warning : UiTokens.TextSecondary),
            })
            {
                var r = new HBoxContainer();
                r.AddThemeConstantOverride("separation", 5);
                r.AddChild(new Icon(icon, 13, color));
                r.AddChild(ThemeFactory.Text(text, UiTokens.FontSmall, UiTokens.TextLabel, UiFonts.Medium));
                rules.AddChild(r);
            }
            body.AddChild(Stack(8, ModalFrame.Section("Regras"), rules));
            body.AddChild(Footer(openPage, ModalFrame.Action("Enquadrar", "Centraliza e aproxima a zona", () => ctx.FocusZone(z.Id))));
            string line1 = z.IsCity ? $"{PtBr.Function(z.Function)} nível {z.CityLevel} · {PtBr.Biome(z.Biome)}" : $"{PtBr.Function(z.Function)} · {PtBr.Biome(z.Biome)}";
            return new Parts(new ZoneEmblem(z), z.Name, line1, $"{z.Areas.Count} áreas · {z.Connections.Count} saídas · capacidade {z.Capacity}", body);
        }
        return new QuickCard("zone#" + zoneId, z.IsCity ? UiTokens.Accent : z.Pvp == PvpRules.Open ? UiTokens.Danger : UiTokens.Success, Build, anchor, bounds);
    }

    // ---------- Area ----------

    public static QuickCard Area(ClientContext ctx, WorldMapView map, string zoneId, string areaId, Vector2 anchor, Rect2 bounds, Action openPage)
    {
        var z = ctx.Content.Regions[zoneId];
        var a = z.Areas.First(x => x.Id == areaId);
        Parts Build()
        {
            int people = map.PeopleInArea(z.Id, a.Id).Count;
            var board = ctx.Content.QuestBoards.Values.FirstOrDefault(b => b.RegionId == zoneId && b.AreaId == areaId);
            var quests = board != null ? ctx.Content.QuestsByBoard[board.Id] : null;
            var body = Stack(12);
            body.AddChild(Tags(
                ("users", "Jogadores", people.ToString(), UiTokens.Info, "Pessoas na área agora"),
                ("list", "Tarefas", a.Activities.Count.ToString(), AreaPalette.Color(a.Kind), "Tarefas oferecidas aqui"),
                ("g-principal", "Missões", quests != null ? quests.Count.ToString() : "—", UiTokens.Accent, board != null ? board.Name : "Sem quadro de missões"),
                ("ruler", "Tamanho", $"{a.Radius * 20} m", UiTokens.TextSecondary, "Diâmetro da área")));

            if (a.Activities.Count > 0)
            {
                var tasks = Stack(2, ModalFrame.Section("Tarefas"));
                foreach (var act in a.Activities)
                {
                    int n = map.PeopleAtSpot(z.Id, act.Id);
                    tasks.AddChild(ListLine(new Icon(PlayerModal.TypeIcon(act.Type), 15, ActivityPalette.Color(act.Type)), act.Name, $"Nv {act.MinLevel}+ · {act.TypicalMinutes} min", n.ToString(),
                        n > 0 ? UiTokens.TextPrimary : UiTokens.TextDisabled, null, $"{act.Name}\n{PtBr.ActivityType(act.Type)}|nível {act.MinLevel}+\nRisco|{act.BaseRiskPermille / 10}%\nFazendo agora|{n}"));
                }
                body.AddChild(tasks);
            }
            else if (a.Description.Length > 0) body.AddChild(Note(a.Description));

            if (quests != null)
            {
                var stats = ctx.Snapshot().QuestStats;
                int active = quests.Sum(q => stats.TryGetValue(q.Id, out var s) ? s.Active : 0);
                var row = new HBoxContainer();
                row.AddThemeConstantOverride("separation", 0);
                foreach (var cat in QuestCategories.All)
                    row.AddChild(Mini(quests.Count(q => q.Category == cat).ToString(), QuestModal.CategoryName(cat) switch { "Principal" => "principais", "Diária" => "diárias", _ => "secundárias" }, "", QuestModal.CategoryColor(cat)));
                row.AddChild(Mini(active.ToString(), "em andamento", "Missões deste quadro aceitas agora", UiTokens.Info));
                body.AddChild(Stack(8, ModalFrame.Section(board!.Name), row));
            }
            body.AddChild(Footer(openPage, ModalFrame.Action("Enquadrar", "Centraliza a área", () => ctx.FocusArea(z.Id, a.Id))));
            return new Parts(new KindEmblem(a.Kind), a.Name, $"{AreaPalette.Name(a.Kind)} · {z.Name}",
                (a.LevelMin > 0 ? $"Faixa {a.LevelMin}–{a.LevelMax} · " : "Todos os níveis · ") + $"{PtBr.Pvp(z.Pvp)}", body);
        }
        return new QuickCard("area#" + zoneId + "#" + areaId, AreaPalette.Color(a.Kind), Build, anchor, bounds);
    }
}

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
        BuildHeader();
        AddTab("Perfil", BuildProfile, live: true);
        AddTab("Missões", BuildQuests, live: true);
        AddTab("Histórico", BuildHistory, live: true);
        AddTab("Rotina e finanças", BuildRoutine);
    }

    private CharacterView? View => _ctx.Snapshot().ByPerson is { } a && _person.Id < a.Length ? a[_person.Id] : null;
    private string ProfileName => _ctx.Content.Profiles.TryGetValue(_person.ProfileId, out var p) ? p.Name : _person.ProfileId;

    private void BuildHeader()
    {
        var v = View;
        var color = UiTokens.ProfileColor(_person.ProfileId);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 14);
        row.AddChild(new PortraitBadge(_person.AppearanceSeed, color) { CustomMinimumSize = new Vector2(62, 62) });
        var names = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        names.AddThemeConstantOverride("separation", 0);
        names.AddChild(ThemeFactory.Text(v?.Name ?? _person.Name, 24, UiTokens.TextPrimary, UiFonts.Display));
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
            Actions.AddChild(Action("Ver área", $"{area.Name}\n~Abre a ficha da área onde está.", () => { Close(); _ctx.OpenArea(z, a); }));
        }
        else if (v != null) Actions.AddChild(Action("Ver zona", "Ver zona\n~Abre a ficha da zona atual.", () => { Close(); _ctx.OpenZone(v.RegionId); }));
    }

    private string FavText() => _ctx.Favorites.Contains(_person.Id) ? "★ Favorito" : "☆ Favoritar";

    private void ToggleFavorite()
    {
        if (!_ctx.Favorites.Remove(_person.Id)) _ctx.Favorites.Add(_person.Id);
        _favorite.Text = FavText();
    }

    // ---------------- Perfil (3 colunas) ----------------

    private Control BuildProfile()
    {
        var left = Column(10, Card("Atributos", AttributeGrid(), UiTokens.Accent));
        left.CustomMinimumSize = new Vector2(380, 0);
        var v = View;
        if (v == null)
            return Row(12, left, Column(10, Card("Situação", Wrap("Ainda não criou conta no MMORPG; pode se registrar nos próximos dias do lançamento."))));
        var middle = Column(10, Card("Agora", NowBody(v), UiTokens.Info), Card("Progresso", ProgressBody(v)));
        var right = Column(10, Card("Por que escolheu isso", DecisionBody(v), UiTokens.Success), Card("Recente", RecentBody()));
        return Row(12, left, middle, right);
    }

    private Control AttributeGrid()
    {
        var t = _person.Traits;
        Control Group(string title, (string Name, int Value, string Hint)[] items)
        {
            var col = Column(1);
            col.AddChild(ThemeFactory.Text(title.ToUpperInvariant(), 10, UiTokens.AccentSoft, UiFonts.CondensedBold));
            foreach (var (name, value, hint) in items)
            {
                int v20 = UiTokens.To20(value);
                var row = new TipPanel { TooltipText = $"{name}\nValor|{v20} / 20\nBruto|{value} / 1000\n~{hint}", MouseFilter = MouseFilterEnum.Stop };
                row.AddThemeStyleboxOverride("panel", new StyleBoxEmpty { ContentMarginTop = 1, ContentMarginBottom = 1 });
                var r = new HBoxContainer();
                var l = ThemeFactory.Text(name, UiTokens.FontSmall, UiTokens.TextPrimary);
                l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                r.AddChild(l);
                var num = ThemeFactory.Text(v20.ToString(CultureInfo.InvariantCulture), 13, UiTokens.AttributeColor(v20), UiFonts.Numbers);
                num.CustomMinimumSize = new Vector2(22, 0);
                num.HorizontalAlignment = HorizontalAlignment.Right;
                r.AddChild(num);
                row.AddChild(r);
                col.AddChild(row);
            }
            return col;
        }
        var g1 = Column(10,
            Group("Estilo de jogo", new[] { ("PvE", t.Pve, "Gosto por caçar e enfrentar criaturas."), ("PvP", t.Pvp, "Gosto por combate contra jogadores."), ("Comércio", t.Trade, "Gosto por comprar, vender e coletar."), ("Exploração", t.Exploration, "Gosto por descobrir lugares."), ("Socializar", t.Socializing, "Gosto por conversar e estar em grupo.") }),
            Group("Capacidade", new[] { ("Mecânica", t.MechanicalSkill, "Execução e reflexo em combate."), ("Conhecimento", t.GameKnowledge, "Entende sistemas e rotas."), ("Coordenação", t.Coordination, "Joga bem em grupo.") }));
        var g2 = Column(10,
            Group("Personalidade", new[] { ("Sociabilidade", t.Sociability, "Facilidade de criar vínculos."), ("Agressividade", t.Aggressiveness, "Tendência a confrontar."), ("Paciência", t.Patience, "Tolera repetição e espera."), ("Lealdade", t.Loyalty, "Fica com grupos e guildas."), ("Aversão a risco", t.RiskAversion, "Evita PvP, perdas e perigo.") }),
            Group("Conduta", new[] { ("Trapaça", t.CheatPropensity, "Propensão a trapacear (incidentes no M9)."), ("Oportunismo", t.Opportunism, "Aproveita vantagens sobre outros.") }));
        return Row(18, g1, g2);
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
        long need = ProgressionRules.XpToNextLevel(_ctx.Content.Progression, v.Level);
        return Column(6,
            Bar($"Nível {v.Level} → {v.Level + 1}", v.Experience, need, UiTokens.Success, $"{v.Experience}/{need}", 110),
            Bar("Zonas conhecidas", v.KnownRegions, _ctx.Content.Regions.Count, UiTokens.Info, $"{v.KnownRegions}/{_ctx.Content.Regions.Count}", 110));
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
        foreach (var e in events.Take(7)) col.AddChild(EventRow(e, compact: true));
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
                Action($"{QuestModal.CategoryName(q.Category)} · {q.Name}", "Abre a ficha da missão", () => { Close(); _ctx.OpenQuest(qid); }),
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
            done.AddChild(Action(times > 1 ? $"{q.Name} ×{times}" : q.Name, QuestModal.CategoryName(q.Category), () => { Close(); _ctx.OpenQuest(qid); }));
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

using System.Globalization;
using Godot;
using Simulation.Core.Behavior;
using Simulation.Core.Content;
using Simulation.Core.Events;
using Simulation.Core.Population;
using GodotClient.Sim;

namespace GodotClient.Ui;

/// <summary>
/// Ficha completa do jogador simulado: identidade, o que faz agora e por que, progresso,
/// atributos, rotina, financas e historico. Tudo vem do Core (snapshot e eventos).
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
        AddTab("Visão geral", BuildOverview, live: true);
        AddTab("Atributos", BuildTraits);
        AddTab("Rotina e finanças", BuildRoutine);
        AddTab("Histórico", BuildHistory, live: true);
    }

    private CharacterView? View => _ctx.Snapshot().ByPerson is { } a && _person.Id < a.Length ? a[_person.Id] : null;

    private string ProfileName => _ctx.Content.Profiles.TryGetValue(_person.ProfileId, out var p) ? p.Name : _person.ProfileId;

    private void BuildHeader()
    {
        var v = View;
        var color = UiTokens.ProfileColor(_person.ProfileId);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 20);
        row.AddChild(new PortraitBadge(_person.AppearanceSeed, color) { CustomMinimumSize = new Vector2(92, 92) });

        var names = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        names.AddThemeConstantOverride("separation", 4);
        names.AddChild(ThemeFactory.Text(v?.Name ?? _person.Name, 30, UiTokens.TextPrimary, UiFonts.Display));
        names.AddChild(ThemeFactory.Text($"{_person.Name}  ·  Pessoa #{_person.Id}", UiTokens.FontBody, UiTokens.TextSecondary));
        var chips = new HFlowContainer();
        chips.AddThemeConstantOverride("h_separation", 8);
        chips.AddThemeConstantOverride("v_separation", 6);
        if (v != null)
        {
            var cls = _ctx.Content.Classes.TryGetValue(v.ClassId, out var c) ? c : null;
            chips.AddChild(ThemeFactory.Chip($"{cls?.Name ?? v.ClassId} · {RolePt(cls?.Role)}", RoleColor(cls?.Role), dot: true));
            chips.AddChild(ThemeFactory.Chip($"Nível {v.Level}", UiTokens.Info));
            chips.AddChild(ThemeFactory.Chip(v.Status == CharacterStatus.Offline ? "Desconectado" : "Conectado",
                v.Status == CharacterStatus.Offline ? UiTokens.TextSecondary : UiTokens.Success, dot: true));
        }
        else chips.AddChild(ThemeFactory.Chip("Ainda não se registrou", UiTokens.Warning, dot: true));
        chips.AddChild(ThemeFactory.Chip($"Perfil {ProfileName}", color));
        names.AddChild(chips);
        row.AddChild(names);
        Header.AddChild(row);

        bool online = v != null && v.Status != CharacterStatus.Offline;
        Actions.AddChild(Action("Seguir no mapa", "Centraliza e acompanha este jogador", () => { _ctx.FocusPerson(_person.Id, true); Close(); }, primary: true, enabled: online));
        Actions.AddChild(Action("Localizar", "Centraliza o mapa no jogador", () => { _ctx.FocusPerson(_person.Id, false); Close(); }, enabled: online));
        _favorite = Action(FavText(), "Acompanhar este jogador (favoritos desta sessão)", ToggleFavorite);
        Actions.AddChild(_favorite);
        if (v != null && v.Status == CharacterStatus.Doing && v.ActivityId.Length > 0
            && _ctx.Content.Regions[v.TargetRegionId].AreaOfActivity(v.ActivityId) is { } area)
        {
            string z = v.TargetRegionId, a = area.Id;
            Actions.AddChild(Action(area.Name, "Abre a ficha da área onde está", () => { Close(); _ctx.OpenArea(z, a); }));
        }
        else if (v != null) Actions.AddChild(Action("Zona atual", "Abre a ficha da zona", () => { Close(); _ctx.OpenZone(v.RegionId); }));
        Actions.AddChild(Action("Observar em 3D", "Disponível a partir do M4", () => { }, enabled: false));
    }

    private string FavText() => _ctx.Favorites.Contains(_person.Id) ? "★ Favorito" : "☆ Favoritar";

    private void ToggleFavorite()
    {
        if (!_ctx.Favorites.Remove(_person.Id)) _ctx.Favorites.Add(_person.Id);
        _favorite.Text = FavText();
    }

    // ---------------- Visao geral ----------------

    private Control BuildOverview()
    {
        var v = View;
        if (v == null)
            return Column(14, Card("Situação", ThemeFactory.Text(
                "Esta pessoa ainda não criou conta no MMORPG. Ela conhece o jogo e pode se registrar nos próximos dias do lançamento.",
                UiTokens.FontBody, UiTokens.TextSecondary)));

        var snap = _ctx.Snapshot();
        long now = snap.Tick;

        // Agora
        var nowCol = Column(10);
        var status = ThemeFactory.Text(StatusText.Of(v, _ctx.Content), 19, UiTokens.TextPrimary, UiFonts.SemiBold);
        status.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        nowCol.AddChild(status);
        nowCol.AddChild(ThemeFactory.Text($"Zona: {_ctx.ZoneName(v.RegionId)}", UiTokens.FontSmall, UiTokens.TextSecondary));
        if (v.Status == CharacterStatus.Doing && v.ActivityEndTick > v.ActivityStartTick)
        {
            float total = v.ActivityEndTick - v.ActivityStartTick;
            float done = Math.Clamp(now - v.ActivityStartTick, 0, (long)total);
            nowCol.AddChild(Bar("Atividade", done, total, UiTokens.Accent, $"{(int)(done * 100 / total)}%", 110));
            nowCol.AddChild(ThemeFactory.Text($"Termina por volta de {_ctx.Clock(v.ActivityEndTick)}", UiTokens.FontSmall, UiTokens.TextSecondary));
        }
        else if (v.Status == CharacterStatus.Traveling && v.MoveEndTick > v.MoveStartTick)
        {
            float total = v.MoveEndTick - v.MoveStartTick;
            float done = Math.Clamp(now - v.MoveStartTick, 0, (long)total);
            nowCol.AddChild(Bar("Trecho atual", done, total, UiTokens.Info, $"{(int)(done * 100 / total)}%", 110));
            nowCol.AddChild(ThemeFactory.Text($"Zona atual: {_ctx.ZoneName(v.RegionId)} · andando pela estrada", UiTokens.FontSmall, UiTokens.TextSecondary));
        }


        // Sessao
        Control session;
        if (v.Status == CharacterStatus.Offline)
            session = ThemeFactory.Text("Desconectado. A próxima sessão depende da rotina dele (veja Rotina e finanças).", UiTokens.FontBody, UiTokens.TextSecondary);
        else
        {
            long minutesLeft = Math.Max(0, (v.SessionEndTick - now) / Simulation.Core.Time.SimTime.TicksPerMinute);
            session = Row(10, Stat(_ctx.Clock(v.SessionStartTick), "conectou às"), Stat(_ctx.Clock(v.SessionEndTick), "deve sair às"), Stat($"{minutesLeft} min", "restantes"));
        }

        // Progresso
        long need = BehaviorTuning.XpToNextLevel(v.Level);
        var progress = Column(10,
            Bar($"Nível {v.Level} → {v.Level + 1}", v.Experience, need, UiTokens.Success, $"{v.Experience}/{need} XP", 150),
            Bar("Locais conhecidos", v.KnownRegions, _ctx.Content.Regions.Count, UiTokens.Info, $"{v.KnownRegions} de {_ctx.Content.Regions.Count}", 150));

        var left = Column(14, Card("Agora", nowCol, UiTokens.Accent), Card("Sessão", session), Card("Progresso", progress));
        var right = Column(14, Card("Por que escolheu isso", DecisionBody(v), UiTokens.Info));
        return Row(16, left, right);
    }

    private Control DecisionBody(CharacterView v)
    {
        var d = v.LastDecision;
        if (d == null) return ThemeFactory.Text("Nenhuma decisão registrada ainda.", UiTokens.FontBody, UiTokens.TextSecondary);
        var col = Column(10);
        var title = ThemeFactory.Text($"{_ctx.ActivityName(d.RegionId, d.ActivityId)}", 17, UiTokens.TextPrimary, UiFonts.SemiBold);
        title.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        col.AddChild(title);
        string place = _ctx.PlaceName(d.RegionId, d.ActivityId) == _ctx.ZoneName(d.RegionId) ? _ctx.ZoneName(d.RegionId) : $"{_ctx.PlaceName(d.RegionId, d.ActivityId)} · {_ctx.ZoneName(d.RegionId)}";
        col.AddChild(ThemeFactory.Text($"{place} · decidido às {_ctx.Clock(d.Tick)} · {d.OptionsConsidered} opções avaliadas",
            UiTokens.FontSmall, UiTokens.TextSecondary));

        var factors = d.Factors.Where(f => f.Contribution != 0).OrderByDescending(f => Math.Abs(f.Contribution)).ToList();
        int max = Math.Max(1, factors.Count == 0 ? 1 : factors.Max(f => Math.Abs(f.Contribution)));
        foreach (var f in factors)
        {
            bool pos = f.Contribution > 0;
            col.AddChild(DivergingBar(FactorPt(f.Key), f.Contribution, max, pos ? UiTokens.Success : UiTokens.Danger));
        }
        col.AddChild(ThemeFactory.Text($"Nota final: {d.Utility}", UiTokens.FontSmall, UiTokens.TextSecondary, UiFonts.Numbers));
        if (d.RunnerUpActivityKey.Length > 0)
        {
            var parts = d.RunnerUpActivityKey.Split('/');
            col.AddChild(ThemeFactory.Text($"Segunda opção: {_ctx.ActivityName(parts[0], parts[1])} · {_ctx.ZoneName(parts[0])}", UiTokens.FontSmall, UiTokens.TextSecondary));
        }
        var hint = ThemeFactory.Text("Pesos calculados pela simulação a partir dos traços da pessoa, do nível, da distância, da lotação e do risco no momento da decisão.",
            UiTokens.FontCaption, UiTokens.TextDisabled);
        hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        col.AddChild(hint);
        return col;
    }

    private static Control DivergingBar(string label, int value, int max, Color color)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        var l = ThemeFactory.Text(label, UiTokens.FontSmall, UiTokens.TextPrimary);
        l.CustomMinimumSize = new Vector2(190, 0);
        row.AddChild(l);
        var track = new DivergingTrack(value / (float)max, color) { SizeFlagsHorizontal = SizeFlags.ExpandFill, CustomMinimumSize = new Vector2(0, 14) };
        row.AddChild(track);
        var v = ThemeFactory.Text((value > 0 ? "+" : "") + value.ToString(CultureInfo.InvariantCulture), UiTokens.FontCaption, color, UiFonts.Numbers);
        v.CustomMinimumSize = new Vector2(44, 0);
        v.HorizontalAlignment = HorizontalAlignment.Right;
        row.AddChild(v);
        return row;
    }

    public static string FactorPt(string key) => key switch
    {
        "affinity" => "Gosta desse tipo de atividade",
        "level_fit" => "Conteúdo adequado ao nível",
        "company" => "Companhia de outros jogadores",
        "crowding" => "Local disputado (lotação)",
        "risk" => "Risco percebido",
        "travel" => "Distância até lá",
        "time_budget" => "Não cabe no tempo da sessão",
        "repetition" => "Cansaço de repetir",
        "noise" => "Variação pessoal do momento",
        _ => key,
    };

    // ---------------- Atributos ----------------

    private Control BuildTraits()
    {
        var t = _person.Traits;
        Control Group(string title, (string, int)[] items)
        {
            var c = Column(8);
            foreach (var (n, val) in items) c.AddChild(Bar(n, val, 1000, TraitColor(val), $"{val / 10}", 170));
            return Card(title, c);
        }
        var left = Column(14,
            Group("Estilo de jogo", new[] { ("PvE", t.Pve), ("PvP", t.Pvp), ("Comércio", t.Trade), ("Exploração", t.Exploration), ("Socializar", t.Socializing) }),
            Group("Capacidade", new[] { ("Habilidade mecânica", t.MechanicalSkill), ("Conhecimento do jogo", t.GameKnowledge), ("Coordenação", t.Coordination) }));
        var right = Column(14,
            Group("Personalidade", new[] { ("Sociabilidade", t.Sociability), ("Agressividade", t.Aggressiveness), ("Paciência", t.Patience), ("Lealdade", t.Loyalty), ("Aversão a risco", t.RiskAversion) }),
            Group("Conduta", new[] { ("Propensão a trapacear", t.CheatPropensity), ("Oportunismo", t.Opportunism) }));
        return Row(16, left, right);
    }

    private static Color TraitColor(int v) => v >= 700 ? UiTokens.Accent : v >= 400 ? UiTokens.Info : new Color(UiTokens.TextSecondary, 0.8f);

    // ---------------- Rotina ----------------

    private Control BuildRoutine()
    {
        var t = _person.Traits;
        var routine = Row(10, Stat($"{t.PreferredStartHour:00}h", "horário preferido"), Stat($"{t.SessionMinutes} min", "sessão típica"),
            Stat($"{t.DaysPerWeek}/7", "dias por semana"), Stat($"+{t.WeekendBoost / 10}%", "a mais no fim de semana"));
        var money = Row(10, Stat($"R$ {PtBr.Decimal(t.LeisureBudgetCentsPerMonth / 100.0, 0)}", "orçamento de lazer/mês"),
            Stat($"{t.WillingnessToPay / 10}%", "disposição a pagar"), Stat($"{t.PaidAdvantageSensitivity / 10}%", "aversão a vantagem paga"));
        var note = ThemeFactory.Text("Dinheiro real fictício do tycoon (separado do ouro do MMORPG). A monetização usa estes valores a partir do M7.",
            UiTokens.FontCaption, UiTokens.TextDisabled);
        note.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return Column(14, Card("Rotina", routine), Card("Finanças", Column(10, money, note)));
    }

    // ---------------- Historico ----------------

    private Control BuildHistory()
    {
        var events = _ctx.PersonHistory(_person.Id);
        var col = Column(4);
        if (events.Count == 0)
        {
            col.AddChild(ThemeFactory.Text("Nada registrado desde que você abriu o jogo.", UiTokens.FontBody, UiTokens.TextSecondary));
            return col;
        }
        foreach (var e in events.Take(80))
        {
            var (text, color) = Describe(e);
            var row = new HBoxContainer();
            row.AddThemeConstantOverride("separation", 12);
            var when = ThemeFactory.Text($"{PtBr.ShortDate(_ctx.Calendar.ToDateTime(e.Tick))} {_ctx.Clock(e.Tick)}", UiTokens.FontCaption, UiTokens.TextDisabled, UiFonts.Numbers);
            when.CustomMinimumSize = new Vector2(120, 0);
            row.AddChild(when);
            row.AddChild(new DotIcon(color) { CustomMinimumSize = new Vector2(9, 9), SizeFlagsVertical = SizeFlags.ShrinkCenter });
            var l = ThemeFactory.Text(text, UiTokens.FontSmall, UiTokens.TextPrimary);
            l.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            l.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(l);
            col.AddChild(row);
        }
        return col;
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
                var parts = (e.Detail ?? "/").Split('/');
                string why = e.Causes.Count > 0 ? " — " + string.Join(", ", e.Causes.Take(2).Select(c => FactorPt(c.Factor).ToLowerInvariant())) : "";
                return ($"Decidiu: {_ctx.ActivityName(parts[0], parts[1])} em {_ctx.ZoneName(parts[0])}{why}", ActivityColorFor(parts[0], parts[1]));
            default: return (e.Type, UiTokens.TextSecondary);
        }
    }

    private Color ActivityColorFor(string zone, string activity) =>
        _ctx.Content.Regions.TryGetValue(zone, out var r) && r.Activities.FirstOrDefault(a => a.Id == activity) is { } a
            ? ActivityPalette.Color(a.Type) : UiTokens.TextSecondary;

    public static string RolePt(string? role) => role switch
    {
        ClassRoles.Tank => "Tanque",
        ClassRoles.Damage => "Dano",
        ClassRoles.Support => "Suporte",
        _ => "",
    };

    public static Color RoleColor(string? role) => role switch
    {
        ClassRoles.Tank => new Color("#8EC9FF"),
        ClassRoles.Damage => new Color("#F0796F"),
        ClassRoles.Support => new Color("#7FD8A6"),
        _ => UiTokens.TextSecondary,
    };
}

/// <summary>Barra divergente: positivo para a direita (verde), negativo para a esquerda (vermelho).</summary>
public partial class DivergingTrack : Control
{
    private readonly float _value;
    private readonly Color _color;

    public DivergingTrack(float value, Color color)
    {
        _value = Mathf.Clamp(value, -1, 1);
        _color = color;
    }

    public override void _Draw()
    {
        float mid = Size.X / 2f;
        float h = 8, y = (Size.Y - h) / 2f;
        DrawRect(new Rect2(0, y, Size.X, h), new Color(UiTokens.Background, 0.9f));
        DrawLine(new Vector2(mid, 0), new Vector2(mid, Size.Y), UiTokens.BorderStrong, 1);
        float w = mid * Mathf.Abs(_value);
        DrawRect(_value >= 0 ? new Rect2(mid, y, w, h) : new Rect2(mid - w, y, w, h), _color);
    }
}

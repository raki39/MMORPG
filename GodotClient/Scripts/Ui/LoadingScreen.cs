using Godot;
using GodotClient.Sim;

namespace GodotClient.Ui;

/// <summary>
/// Tela de carregamento da abertura: a campanha e preparada numa tarefa separada (gerar as pessoas e simular os
/// primeiros dias) e esta tela mostra a etapa e o progresso, sem a janela congelar.
/// </summary>
public partial class LoadingScreen : Control
{
    private readonly Label _phase;
    private readonly Label _detail;
    private readonly ProgressLine _bar;
    private readonly int _population;
    private readonly int _days;

    public LoadingScreen(int population, int days)
    {
        _population = population;
        _days = days;
        SetAnchorsPreset(LayoutPreset.FullRect);
        MouseFilter = MouseFilterEnum.Stop;
        AddChild(new ColorRect { Color = UiTokens.Background, MouseFilter = MouseFilterEnum.Ignore, AnchorRight = 1, AnchorBottom = 1 });

        var center = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
        center.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(center);
        var col = new VBoxContainer { CustomMinimumSize = new Vector2(460, 0), MouseFilter = MouseFilterEnum.Ignore };
        col.AddThemeConstantOverride("separation", 14);
        center.AddChild(col);

        var brand = new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = MouseFilterEnum.Ignore };
        brand.AddThemeConstantOverride("separation", 12);
        brand.AddChild(ThemeFactory.Text("◆", 22, UiTokens.Accent));
        brand.AddChild(ThemeFactory.Text("MMORPG TYCOON", 26, UiTokens.AccentSoft, UiFonts.Display));
        col.AddChild(brand);
        col.AddChild(new Control { CustomMinimumSize = new Vector2(0, 10) });

        _phase = ThemeFactory.Text("Preparando a campanha…", UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Medium);
        _phase.HorizontalAlignment = HorizontalAlignment.Center;
        col.AddChild(_phase);
        _bar = new ProgressLine { CustomMinimumSize = new Vector2(460, 6) };
        col.AddChild(_bar);
        _detail = ThemeFactory.Text("", UiTokens.FontSmall, UiTokens.TextSecondary);
        _detail.HorizontalAlignment = HorizontalAlignment.Center;
        _detail.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _detail.CustomMinimumSize = new Vector2(460, 0);
        col.AddChild(_detail);
    }

    /// <summary>Atualiza a etapa e a barra (chamado a cada quadro enquanto a tarefa roda).</summary>
    public void Show(LoadProgress p)
    {
        if (p.Phase == LoadProgress.Generating)
        {
            _phase.Text = $"Gerando {PtBr.Int(_population)} pessoas…";
            _detail.Text = "Cada pessoa tem rotina, personalidade, preferências e objetivos próprios.";
            _bar.Value = -1;   // sem medida: a barra corre de um lado ao outro
        }
        else
        {
            int day = Math.Clamp(p.Day + 1, 1, Math.Max(1, _days));
            _phase.Text = $"Simulando os primeiros dias da campanha · dia {day} de {_days}";
            _detail.Text = p.Registered > 0
                ? $"{PtBr.Int(p.Registered)} contas criadas até agora. O jogo abre com a campanha em andamento."
                : "O jogo abre com a campanha em andamento.";
            _bar.Value = (float)p.Fraction;
        }
    }
}

/// <summary>Barra de progresso fina; Value &lt; 0 anima um trecho indo e voltando (etapa sem medida).</summary>
public partial class ProgressLine : Control
{
    private float _value;
    private double _t;
    public float Value { get => _value; set { _value = value; QueueRedraw(); } }

    public override void _Process(double delta)
    {
        if (_value >= 0) return;
        _t += delta;
        QueueRedraw();
    }

    public override void _Draw()
    {
        float h = Mathf.Round(Mathf.Min(Size.Y, 6)), y = Mathf.Round((Size.Y - h) / 2f), w = Mathf.Round(Size.X);
        DrawRect(new Rect2(0, y, w, h), UiTokens.Track);
        if (_value >= 0)
        {
            if (_value > 0) DrawRect(new Rect2(0, y, Mathf.Max(2, Mathf.Round(w * Mathf.Clamp(_value, 0, 1))), h), UiTokens.Accent);
            return;
        }
        float seg = w * 0.28f;
        float x = (float)((Math.Sin(_t * 2.2) + 1) / 2) * (w - seg);
        DrawRect(new Rect2(Mathf.Round(x), y, Mathf.Round(seg), h), UiTokens.Accent);
    }
}

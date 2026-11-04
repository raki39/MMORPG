using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Preferencias do cliente (video, interface, jogo), em user://settings.cfg (D-034).
/// Nao fazem parte do estado da simulacao nem do save da campanha: nada aqui muda resultados.
/// </summary>
public static class ClientSettings
{
    private const string Path = "user://settings.cfg";

    public static readonly (string Id, string Name)[] WindowModes =
    {
        ("window", "Janela"), ("borderless", "Tela cheia (sem bordas)"), ("exclusive", "Tela cheia exclusiva"),
    };
    public static readonly (int W, int H)[] Resolutions = { (1366, 768), (1600, 900), (1920, 1080), (2560, 1440) };
    public static readonly int[] FpsLimits = { 0, 60, 120, 144, 165, 240 };
    public static readonly (string Id, string Name)[] VsyncModes = { ("on", "Ligada"), ("adaptive", "Adaptativa"), ("off", "Desligada") };
    public static readonly (int Id, string Name)[] MsaaModes = { (0, "Desligada"), (1, "2×"), (2, "4×"), (3, "8×") };

    public static string WindowMode { get; set; } = "window";
    public static int ResolutionIndex { get; set; } = 1;
    public static string Vsync { get; set; } = "on";
    public static int MaxFps { get; set; }
    public static int Msaa { get; set; } = 1;
    public static bool ShowFps { get; set; } = true;
    /// <summary>Texto nitido (LCD, como o ClearType) ou suave (tons de cinza).</summary>
    public static bool TextLcd { get; set; } = true;
    public static bool PauseOffMap { get; set; } = true;
    public static int StartSpeed { get; set; } = 1;
    public static bool CampaignNote { get; set; } = true;

    public static void Load()
    {
        var cfg = new ConfigFile();
        if (cfg.Load(Path) != Error.Ok) return;
        WindowMode = (string)cfg.GetValue("video", "window_mode", WindowMode);
        ResolutionIndex = Math.Clamp((int)cfg.GetValue("video", "resolution", ResolutionIndex), 0, Resolutions.Length - 1);
        Vsync = (string)cfg.GetValue("video", "vsync", Vsync);
        MaxFps = (int)cfg.GetValue("video", "max_fps", MaxFps);
        Msaa = Math.Clamp((int)cfg.GetValue("video", "msaa", Msaa), 0, 3);
        ShowFps = (bool)cfg.GetValue("interface", "show_fps", ShowFps);
        TextLcd = (bool)cfg.GetValue("interface", "text_lcd", TextLcd);
        PauseOffMap = (bool)cfg.GetValue("game", "pause_off_map", PauseOffMap);
        StartSpeed = (int)cfg.GetValue("game", "start_speed", StartSpeed);
        CampaignNote = (bool)cfg.GetValue("game", "campaign_note", CampaignNote);
    }

    public static void Save()
    {
        var cfg = new ConfigFile();
        cfg.SetValue("video", "window_mode", WindowMode);
        cfg.SetValue("video", "resolution", ResolutionIndex);
        cfg.SetValue("video", "vsync", Vsync);
        cfg.SetValue("video", "max_fps", MaxFps);
        cfg.SetValue("video", "msaa", Msaa);
        cfg.SetValue("interface", "show_fps", ShowFps);
        cfg.SetValue("interface", "text_lcd", TextLcd);
        cfg.SetValue("game", "pause_off_map", PauseOffMap);
        cfg.SetValue("game", "start_speed", StartSpeed);
        cfg.SetValue("game", "campaign_note", CampaignNote);
        cfg.Save(Path);
    }

    /// <summary>Aplica as opcoes de video. Em capturas e medicoes a janela nao e alterada.</summary>
    public static void ApplyVideo(Viewport viewport, bool automation)
    {
        if (!automation)
        {
            ApplyWindow();
            DisplayServer.WindowSetVsyncMode(Vsync switch { "off" => DisplayServer.VSyncMode.Disabled, "adaptive" => DisplayServer.VSyncMode.Adaptive, _ => DisplayServer.VSyncMode.Enabled });
            Engine.MaxFps = MaxFps;
        }
        viewport.Msaa2D = (Viewport.Msaa)Msaa;
    }

    public static void ApplyWindow()
    {
        switch (WindowMode)
        {
            case "borderless": DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen); break;
            case "exclusive": DisplayServer.WindowSetMode(DisplayServer.WindowMode.ExclusiveFullscreen); break;
            default:
                DisplayServer.WindowSetMode(DisplayServer.WindowMode.Windowed);
                var (w, h) = Resolutions[ResolutionIndex];
                var screen = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen());
                var size = new Vector2I(Math.Min(w, screen.Size.X), Math.Min(h, screen.Size.Y));
                DisplayServer.WindowSetSize(size);
                DisplayServer.WindowSetPosition(screen.Position + (screen.Size - size) / 2);
                break;
        }
    }

    /// <summary>F11 / Alt+Enter: alterna entre janela e tela cheia sem bordas.</summary>
    public static void ToggleFullscreen()
    {
        WindowMode = WindowMode == "window" ? "borderless" : "window";
        ApplyWindow();
        Save();
    }
}

/// <summary>Pagina de configuracoes (D-034): video e graficos, interface e jogo.</summary>
public partial class SettingsPage : ModalFrame
{
    private readonly Viewport _viewport;
    private readonly Action _changed;

    public SettingsPage(Viewport viewport, Action changed) : base(UiTokens.Accent)
    {
        _viewport = viewport;
        _changed = changed;
        var col = new VBoxContainer();
        col.AddThemeConstantOverride("separation", 2);
        Art.AddChild(new EmblemBox("settings", UiTokens.Accent) { CustomMinimumSize = new Vector2(72, 72) });
        col.AddChild(ThemeFactory.Text("Configurações", 28, UiTokens.TextPrimary, UiFonts.SemiBold));
        col.AddChild(ThemeFactory.Text("Preferências deste computador · salvas automaticamente", UiTokens.FontBody, UiTokens.TextSecondary));
        Header.AddChild(col);
        Breadcrumb = "Configurações";
        var screen = DisplayServer.ScreenGetSize(DisplayServer.WindowGetCurrentScreen());
        Fact("Tela", $"{screen.X}×{screen.Y}", null, "Resolução do monitor atual");
        Fact("Janela", $"{DisplayServer.WindowGetSize().X}×{DisplayServer.WindowGetSize().Y}");
        Fact("Atualização", $"{DisplayServer.ScreenGetRefreshRate(DisplayServer.WindowGetCurrentScreen()):0} Hz", null, "Taxa de atualização do monitor");
        Fact("Renderizador", RenderingServer.GetVideoAdapterName());
        AddTab("Vídeo e gráficos", BuildVideo);
        AddTab("Jogo", BuildGame);
        AddTab("Atalhos", BuildKeys);
    }

    private void Changed()
    {
        ClientSettings.Save();
        _changed();
    }

    private static Control Setting(string name, string hint, Control input)
    {
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 24);
        var text = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        text.AddThemeConstantOverride("separation", 1);
        text.AddChild(ThemeFactory.Text(name, UiTokens.FontBody, UiTokens.TextPrimary, UiFonts.Medium));
        var h = ThemeFactory.Text(hint, UiTokens.FontSmall, UiTokens.TextSecondary);
        h.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        text.AddChild(h);
        row.AddChild(text);
        input.CustomMinimumSize = new Vector2(240, 30);
        input.SizeFlagsVertical = SizeFlags.ShrinkCenter;
        row.AddChild(input);
        var box = new PanelContainer();
        box.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = new Color(0, 0, 0, 0), BorderColor = UiTokens.Border, BorderWidthBottom = 1, ContentMarginTop = 8, ContentMarginBottom = 10 });
        box.AddChild(row);
        return box;
    }

    private static OptionButton Options(IEnumerable<string> items, int selected, Action<int> onPick)
    {
        var o = new OptionButton { FocusMode = FocusModeEnum.None };
        foreach (var i in items) o.AddItem(i);
        o.Selected = Math.Clamp(selected, 0, o.ItemCount - 1);
        o.ItemSelected += i => onPick((int)i);
        return o;
    }

    private static CheckButton Toggle(bool on, Action<bool> onToggle)
    {
        var c = new CheckButton { ButtonPressed = on, FocusMode = FocusModeEnum.None, Text = on ? "Ligado" : "Desligado" };
        c.Toggled += v => { c.Text = v ? "Ligado" : "Desligado"; onToggle(v); };
        return c;
    }

    private Control BuildVideo()
    {
        var modes = ClientSettings.WindowModes;
        var display = Column(0,
            Setting("Modo de tela", "Tela cheia sem bordas troca de janela mais rápido; a exclusiva pode reduzir a latência. Atalhos: F11 ou Alt+Enter.",
                Options(modes.Select(m => m.Name), Array.FindIndex(modes, m => m.Id == ClientSettings.WindowMode), i => { ClientSettings.WindowMode = modes[i].Id; ClientSettings.ApplyWindow(); Changed(); })),
            Setting("Tamanho da janela", "Usado no modo janela. A interface foi pensada para 1600×900 ou mais.",
                Options(ClientSettings.Resolutions.Select(r => $"{r.W} × {r.H}"), ClientSettings.ResolutionIndex, i => { ClientSettings.ResolutionIndex = i; if (ClientSettings.WindowMode == "window") ClientSettings.ApplyWindow(); Changed(); })));
        var vs = ClientSettings.VsyncModes;
        var perf = Column(0,
            Setting("Sincronização vertical (VSync)", "Ligada evita cortes na imagem e limita os quadros à taxa do monitor.",
                Options(vs.Select(v => v.Name), Array.FindIndex(vs, v => v.Id == ClientSettings.Vsync), i => { ClientSettings.Vsync = vs[i].Id; ClientSettings.ApplyVideo(_viewport, false); Changed(); })),
            Setting("Limite de quadros", "Teto de FPS (útil com a VSync desligada). O mapa foi otimizado para 144 Hz.",
                Options(ClientSettings.FpsLimits.Select(f => f == 0 ? "Sem limite" : $"{f} FPS"), Array.IndexOf(ClientSettings.FpsLimits, ClientSettings.MaxFps), i => { ClientSettings.MaxFps = ClientSettings.FpsLimits[i]; Engine.MaxFps = ClientSettings.MaxFps; Changed(); })),
            Setting("Suavização de bordas (MSAA 2D)", "Bordas do mapa e dos gráficos mais lisas. 2× é o equilíbrio; 8× custa mais da placa de vídeo.",
                Options(ClientSettings.MsaaModes.Select(m => m.Name), ClientSettings.Msaa, i => { ClientSettings.Msaa = i; _viewport.Msaa2D = (Viewport.Msaa)i; Changed(); })),
            Setting("Texto", "Nítido usa suavização por subpixel, como o ClearType do Windows. Use Suave se aparecerem franjas coloridas nas letras.",
                Options(new[] { "Nítido (recomendado)", "Suave" }, ClientSettings.TextLcd ? 0 : 1, i => { ClientSettings.TextLcd = i == 0; UiFonts.ApplyRendering(); Changed(); })),
            Setting("Mostrar FPS na barra superior", "Contador de quadros por segundo ao lado do relógio.",
                Toggle(ClientSettings.ShowFps, v => { ClientSettings.ShowFps = v; Changed(); })));
        return Grid(12, (Card("Tela", display, info: "Modo e tamanho da janela\n~Salvo neste computador; não faz parte do save da campanha."), 6),
            (Card("Desempenho e qualidade", perf, info: "Opções de gráficos\n~Nenhuma delas muda o resultado da simulação."), 6));
    }

    private Control BuildGame()
    {
        var speeds = new[] { 1, 2, 5, 10 };
        var game = Column(0,
            Setting("Pausar fora do mapa", "O tempo só passa na tela do mapa; páginas de jogadores, zonas e missões pausam e voltam na mesma velocidade.",
                Toggle(ClientSettings.PauseOffMap, v => { ClientSettings.PauseOffMap = v; Changed(); })),
            Setting("Velocidade ao abrir o jogo", "Velocidade inicial da simulação.",
                Options(speeds.Select(s => $"{s}×"), Array.IndexOf(speeds, ClientSettings.StartSpeed), i => { ClientSettings.StartSpeed = speeds[i]; Changed(); })),
            Setting("Aviso de campanha ao iniciar", "Mostra no mapa o aviso de que a campanha começa em andamento.",
                Toggle(ClientSettings.CampaignNote, v => { ClientSettings.CampaignNote = v; Changed(); })));
        var future = ThemeFactory.Text("Idioma, áudio, salvamento automático e acessibilidade entram aqui quando existirem no jogo.", UiTokens.FontSmall, UiTokens.TextDisabled);
        future.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        return Grid(12, (Card("Jogo", game), 6), (Card("Em breve", future), 6));
    }

    private Control BuildKeys()
    {
        var keys = Column(6);
        foreach (var (k, what) in new[]
        {
            ("Espaço", "Pausar / continuar (no mapa)"), ("1 · 2 · 3 · 4", "Velocidade 1× · 2× · 5× · 10×"), ("M · J · Z · Q", "Mapa · Jogadores · Zonas · Missões"),
            ("Alt+← · Alt+→", "Voltar · avançar (também os botões laterais do mouse)"), ("Esc", "Fechar o cartão ou voltar ao mapa"),
            ("F11 · Alt+Enter", "Alternar tela cheia"), ("+ · − · Home", "Aproximar · afastar · enquadrar o mapa"), ("Botão direito", "Arrastar o mapa"),
        })
        {
            var r = new HBoxContainer();
            var key = ThemeFactory.Text(k, UiTokens.FontBody, UiTokens.AccentSoft, UiFonts.Numbers);
            key.CustomMinimumSize = new Vector2(180, 0);
            r.AddChild(key);
            r.AddChild(ThemeFactory.Text(what, UiTokens.FontBody, UiTokens.TextPrimary));
            keys.AddChild(r);
        }
        return Grid(12, (Card("Atalhos de teclado e mouse", keys), 6), (new Control(), 6));
    }
}

using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Tokens visuais do cliente — paleta propria "Atlas" (D-012; substitui a paleta da secao 12.1
/// do plano a pedido do criador). Tinta escura azulada + dourado como acao principal, texto
/// levemente quente. Toda tela usa estes valores; nada de cores soltas.
/// </summary>
public static class UiTokens
{
    // Superficies
    public static readonly Color Background = new("#0B0F14");
    public static readonly Color Surface = new("#121921");
    public static readonly Color SurfaceGlass = new(0.071f, 0.098f, 0.129f, 0.90f);
    public static readonly Color SurfaceRaised = new("#1A2330");
    public static readonly Color SurfaceHover = new("#233042");
    public static readonly Color Border = new("#263243");
    public static readonly Color BorderStrong = new("#3A4A5F");
    public static readonly Color Shadow = new(0, 0, 0, 0.45f);

    // Texto
    public static readonly Color TextPrimary = new("#EDE7DA");
    public static readonly Color TextSecondary = new("#98A3B0");
    public static readonly Color TextDisabled = new("#5B6673");

    // Acao e estado
    public static readonly Color Accent = new("#E8B44E");      // dourado: selecao e acao principal
    public static readonly Color AccentSoft = new("#F3D08A");
    public static readonly Color Info = new("#5CC8E8");
    public static readonly Color Success = new("#5FD08F");
    public static readonly Color Warning = new("#F29D52");
    public static readonly Color Danger = new("#F0625D");

    // Mapa
    public static readonly Color Road = new("#C9A86A");
    public static readonly Color NightTint = new("#2B3D6B");

    // Espacamento (px)
    public const int S4 = 4, S8 = 8, S12 = 12, S16 = 16, S24 = 24, S32 = 32;

    // Tipografia (px)
    public const int FontCaption = 12;
    public const int FontSmall = 13;
    public const int FontBody = 15;
    public const int FontTitle = 18;
    public const int FontHeading = 24;

    // Forma
    public const int ControlHeight = 36;
    public const int CornerRadius = 8;
    public const int PanelRadius = 12;

    public const int TopBarHeight = 56;
    public const int InspectorWidth = 380;

    /// <summary>Cores por perfil de populacao: vivas para destacar sobre o terreno.</summary>
    public static Color ProfileColor(string profileId) => profileId switch
    {
        "casual" => new Color("#8EC9FF"),
        "competitivo" => new Color("#FF6B6B"),
        "social" => new Color("#FFB86B"),
        "comerciante" => new Color("#FFE066"),
        "explorador" => new Color("#7CF29A"),
        "hardcore" => new Color("#C49BFF"),
        _ => TextSecondary,
    };

    /// <summary>Indice de bioma usado pelo shader de terreno.</summary>
    public static int BiomeIndex(string biome) => biome switch
    {
        "planicie" => 0,
        "floresta" => 1,
        "caverna" => 2,
        "ruinas" => 3,
        _ => 4,
    };

    public static Color BiomeColor(string biome) => BiomeIndex(biome) switch
    {
        0 => new Color("#7E9152"),
        1 => new Color("#3E7A4E"),
        2 => new Color("#8A7A68"),
        3 => new Color("#6F6783"),
        _ => new Color("#7A7F78"),
    };
}

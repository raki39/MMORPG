using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Tokens visuais — paleta "Atlas" (D-012) em versao densa de jogo de gestao (D-022):
/// superficies planas, bordas finas, cantos pequenos, texto compacto. Cor so com significado.
/// </summary>
public static class UiTokens
{
    // Superficies (do fundo para cima)
    public static readonly Color Background = new("#08090C");
    public static readonly Color Surface = new("#0D1015");
    public static readonly Color SurfaceGlass = new(0.047f, 0.063f, 0.082f, 0.97f);
    public static readonly Color SurfaceRaised = new("#12161C");
    public static readonly Color SurfaceHeader = new("#171B22");
    public static readonly Color SurfaceHover = new("#1D232C");
    public static readonly Color RowAlt = new(1, 1, 1, 0.025f);
    public static readonly Color Border = new("#222831");
    public static readonly Color BorderStrong = new("#313944");
    public static readonly Color Shadow = new(0, 0, 0, 0.45f);

    // Texto
    public static readonly Color TextPrimary = new("#ECEEF1");
    public static readonly Color TextSecondary = new("#8B94A1");
    public static readonly Color TextDisabled = new("#58606B");

    // Acao e estado
    public static readonly Color Accent = new("#D4A64A");
    public static readonly Color AccentSoft = new("#E8C67F");
    public static readonly Color Info = new("#4DA8E8");
    public static readonly Color Success = new("#4CC38A");
    public static readonly Color Warning = new("#E89A4A");
    public static readonly Color Danger = new("#E5544F");

    // Mapa
    public static readonly Color Road = new("#C9A86A");

    // Espacamento (px)
    public const int S4 = 4, S8 = 8, S12 = 12, S16 = 16, S24 = 24, S32 = 32;

    // Tipografia (px) — densa
    public const int FontCaption = 10;
    public const int FontSmall = 11;
    public const int FontBody = 12;
    public const int FontTitle = 15;
    public const int FontHeading = 20;

    // Forma
    public const int ControlHeight = 28;
    public const int CornerRadius = 2;
    public const int PanelRadius = 2;

    public const int TopBarHeight = 50;
    public const int SidePanelWidth = 430;
    public const int InspectorWidth = 380;

    public static Color ProfileColor(string profileId) => profileId switch
    {
        "casual" => new Color("#7FB8EE"),
        "competitivo" => new Color("#EE6A63"),
        "social" => new Color("#F2A85E"),
        "comerciante" => new Color("#E9CF5A"),
        "explorador" => new Color("#6FDB93"),
        "hardcore" => new Color("#B48CF2"),
        _ => TextSecondary,
    };

    /// <summary>Escala de atributo 1–20 (estilo FM): vermelho -> laranja -> verde -> ciano.</summary>
    public static Color AttributeColor(int v20) => v20 switch
    {
        <= 5 => new Color("#C9584F"),
        <= 9 => new Color("#D9893F"),
        <= 12 => new Color("#D9C25A"),
        <= 15 => new Color("#8CCB5E"),
        <= 17 => new Color("#4FC27E"),
        _ => new Color("#3FC9C0"),
    };

    /// <summary>Converte permille (0..1000) para a escala 1..20.</summary>
    public static int To20(int permille) => Math.Clamp((int)Math.Round(permille / 1000.0 * 19.0) + 1, 1, 20);

    public static int BiomeIndex(string biome) => biome switch
    {
        "planicie" => 0,
        "floresta" => 1,
        "caverna" => 2,
        "ruinas" => 3,
        "pantano" => 4,
        "neve" => 5,
        "vulcanico" => 6,
        "sombrio" => 7,
        _ => 0,
    };

    public static Color BiomeColor(string biome) => BiomeIndex(biome) switch
    {
        0 => new Color("#7E9152"),
        1 => new Color("#3E7A4E"),
        2 => new Color("#8A7A68"),
        3 => new Color("#6F6783"),
        4 => new Color("#5E7A5A"),
        5 => new Color("#C9D3DD"),
        6 => new Color("#7A4A3A"),
        7 => new Color("#5A4A6E"),
        _ => new Color("#7A7F78"),
    };
}

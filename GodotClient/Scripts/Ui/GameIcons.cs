using Simulation.Core.Content;

namespace GodotClient.Ui;

/// <summary>
/// Icones proprios do jogo (D-068, etapa 4): desenhados para o MMORPG no traco de linha do Lucide (24×24, sem
/// preenchimento, pontas redondas), rasterizados pelo mesmo Icons.Get. Cobrem classes, papeis, tipos de zona
/// (cidade e bioma), tipos de area, categorias de missao e os acontecimentos marcantes. Nomes com "g-".
/// O Lucide continua nas acoes e nas linhas de dados.
/// </summary>
public static class GameIcons
{
    public static readonly IReadOnlyDictionary<string, string> Svg = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        // ---- Classes ----
        ["g-guerreiro"] = @"<path d=""M12 22s7-3.5 7-10V5l-7-3-7 3v7c0 6.5 7 10 7 10z"" /> <path d=""M12 6v11"" /> <path d=""M9.5 13.5h5"" />",
        ["g-mago"] = @"<path d=""M3 18.5c0 1.4 4 2.5 9 2.5s9-1.1 9-2.5"" /> <path d=""M5.5 19c1.8-3 3.3-8 5-13 .5-1.5 2-2.5 3.5-2.5-.8 1.2-1 2.5-.6 4 .9 3.6 2.6 8 4.6 11.5"" /> <path d=""M11.5 11.5l.6 1.3 1.4.2-1 1 .3 1.4-1.3-.7-1.3.7.3-1.4-1-1 1.4-.2z"" />",
        ["g-sacerdote"] = @"<path d=""M12 11c-2-1.5-3-3.3-3-5a3 3 0 0 1 6 0c0 1.7-1 3.5-3 5z"" /> <path d=""M12 11v11"" /> <path d=""M7 14h10"" />",

        // ---- Papeis ----
        ["g-tanque"] = @"<path d=""M5 4h14v8c0 4.5-3 7.5-7 10-4-2.5-7-5.5-7-10z"" /> <path d=""M12 4v18"" /> <path d=""M5 10h14"" />",
        ["g-dano"] = @"<path d=""M4 14 14 4"" /> <path d=""M7 18.5 18.5 7"" /> <path d=""M12 21l8-8"" />",
        ["g-suporte"] = @"<path d=""M19 14c1.5-1.5 3-3.2 3-5.5A5.5 5.5 0 0 0 16.5 3c-1.8 0-3 .5-4.5 2-1.5-1.5-2.7-2-4.5-2A5.5 5.5 0 0 0 2 8.5c0 2.3 1.5 4 3 5.5l7 7z"" /> <path d=""M12 8.5v6"" /> <path d=""M9 11.5h6"" />",

        // ---- Zonas: cidade e biomas ----
        ["g-cidade"] = @"<path d=""M2 21h20"" /> <path d=""M4 21V8h1.5V6h2v2H9v13"" /> <path d=""M15 21V8h1.5V6h2v2H20v13"" /> <path d=""M9 12h6"" /> <path d=""M10.5 21v-3a1.5 1.5 0 0 1 3 0v3"" /> <path d=""M6.5 11.5v2"" /> <path d=""M17.5 11.5v2"" />",
        ["g-planicie"] = @"<circle cx=""7"" cy=""7"" r=""3"" /> <path d=""M2 20c2-3 4.5-4.5 7.5-4.5S15 17 17 20"" /> <path d=""M11.5 16.5c1.5-2 3.5-3 5.5-3s4 1.5 5 4"" /> <path d=""M2 20h20"" />",
        ["g-floresta"] = @"<path d=""M8 3 3.5 10H6l-3.5 6h11L10 10h2.5z"" /> <path d=""M8 16v5"" /> <path d=""M17.5 8l-3 5H16l-2.5 4h8L19 13h1.5z"" /> <path d=""M17.5 17v4"" />",
        ["g-caverna"] = @"<path d=""M3 20c0-6.5 3.5-11 9-11s9 4.5 9 11z"" /> <path d=""M9 20v-3a3 3 0 0 1 6 0v3"" /> <path d=""M10 9.5V12"" /> <path d=""M14 9.5V11"" />",
        ["g-neve"] = @"<path d=""M2 20 9.5 6l4 6.5L16 9l6 11z"" /> <path d=""M7 11l1.5 1.5L10 11l1.5 1.5"" />",
        ["g-pantano"] = @"<path d=""M2 19c1.7-1 3.3-1 5 0s3.3 1 5 0 3.3-1 5 0 3.3 1 5 0"" /> <path d=""M7 16V8"" /> <path d=""M12 16V4"" /> <path d=""M17 16V9"" /> <path d=""M12 10c-2-1-3-3-3-5"" /> <path d=""M17 13c1.5-.5 3-2 3-4"" />",
        ["g-ruinas"] = @"<path d=""M3 21h18"" /> <path d=""M4 8.5 12 4l5 2.8"" /> <path d=""M4 8.5h9"" /> <path d=""M6 21V8.5"" /> <path d=""M10 21V8.5"" /> <path d=""M14 21v-7l1.5 1 1-1.5"" /> <path d=""M17.5 21v-2.5h3V21"" />",
        ["g-sombrio"] = @"<path d=""M4 21h16"" /> <path d=""M9 21V10l3-6 3 6v11"" /> <path d=""M12 13v2.5"" /> <path d=""M19.5 3.2a2.5 2.5 0 1 0 1.8 4.3 2 2 0 0 1-1.8-4.3z"" />",
        ["g-vulcanico"] = @"<path d=""M2 21l6-10h8l6 10z"" /> <path d=""M8 11c1.3 1 2.7 1 4 0s2.7-1 4 0"" /> <path d=""M11 13l-1 3.5"" /> <path d=""M13 8c-1-1-.5-2.5 1-3s2-2 1-3"" />",

        // ---- Areas ----
        ["g-loja"] = @"<path d=""M2 9l2-5h16l2 5"" /> <path d=""M2 9a2.5 2.5 0 0 0 5 0 2.5 2.5 0 0 0 5 0 2.5 2.5 0 0 0 5 0 2.5 2.5 0 0 0 5 0"" /> <path d=""M4 11.5V21"" /> <path d=""M20 11.5V21"" /> <path d=""M4 16h16"" /> <path d=""M2 21h20"" />",
        ["g-forja"] = @"<path d=""M4 8h12c0 2.5-2 4-4 4v3h2v3H6v-3h2v-3c-2 0-4-1.5-4-4z"" /> <path d=""M16 8h3l-3 2.5"" /> <path d=""M19 2.5l-1 2"" /> <path d=""M21.5 5.5l-2 .5"" /> <path d=""M5 21h10"" />",
        ["g-taverna"] = @"<path d=""M5 8h10v10a3 3 0 0 1-3 3H8a3 3 0 0 1-3-3z"" /> <path d=""M15 10h1.5a2.5 2.5 0 0 1 2.5 2.5v1a2.5 2.5 0 0 1-2.5 2.5H15"" /> <path d=""M5 8a2 2 0 0 1 1.5-3.4A2.5 2.5 0 0 1 11 4a2.5 2.5 0 0 1 4 2 2 2 0 0 1 0 2"" /> <path d=""M8.5 11v7"" /> <path d=""M11.5 11v7"" />",
        ["g-praca"] = @"<rect x=""3"" y=""3"" width=""18"" height=""12"" rx=""1"" /> <path d=""M6 15v6"" /> <path d=""M18 15v6"" /> <rect x=""6"" y=""6"" width=""4"" height=""5"" /> <path d=""M13 7h5"" /> <path d=""M13 10.5h5"" />",
        ["g-treino"] = @"<circle cx=""12"" cy=""4.5"" r=""2.5"" /> <path d=""M9 7.5h6l-1 7.5h-4z"" /> <path d=""M5.5 10h3.5"" /> <path d=""M15 10h3.5"" /> <path d=""M12 15v6"" /> <path d=""M8.5 21h7"" />",
        ["g-caca"] = @"<path d=""M7.5 16c0-2.5 2-4.5 4.5-4.5s4.5 2 4.5 4.5c0 2-1.5 3-3 3-1 0-1-.5-1.5-.5s-.5.5-1.5.5c-1.5 0-3-1-3-3z"" /> <circle cx=""6"" cy=""10.5"" r=""1.6"" /> <circle cx=""9.5"" cy=""6.5"" r=""1.6"" /> <circle cx=""14.5"" cy=""6.5"" r=""1.6"" /> <circle cx=""18"" cy=""10.5"" r=""1.6"" />",
        ["g-minerio"] = @"<path d=""M3 20l2.5-6L10 11l5 1 3.5 3L21 20z"" /> <path d=""M9 16h.01"" /> <path d=""M13 15h.01"" /> <path d=""M15.5 18h.01"" /> <path d=""M14 3.5c2.5-.5 5 .5 6.5 2.5"" /> <path d=""M17.5 4.5 13.5 10"" />",
        ["g-ervas"] = @"<path d=""M12 21v-9"" /> <path d=""M12 15c-3.5 0-6-2.5-6-6 3.5 0 6 2.5 6 6z"" /> <path d=""M12 12c0-3.5 2.5-6 6-6 0 3.5-2.5 6-6 6z"" /> <path d=""M8 21h8"" />",
        ["g-cristal"] = @"<path d=""M9 21l-2-7 4-10 4 10-2 7z"" /> <path d=""M7 14h8"" /> <path d=""M11 4v17"" /> <path d=""M15.5 21l-.5-4 3-4 2.5 4-1 4z"" />",
        ["g-marco"] = @"<path d=""M10 20l.8-14L12 3l1.2 3 .8 14z"" /> <path d=""M6 21h12"" /> <path d=""M11 10.5h2"" />",
        ["g-masmorra"] = @"<path d=""M4 21V11a8 8 0 0 1 16 0v10"" /> <path d=""M8 21v-9.5"" /> <path d=""M12 21V7"" /> <path d=""M16 21v-9.5"" /> <path d=""M5 15.5h14"" />",
        ["g-covil"] = @"<path d=""M8 18v3h8v-3c1.8-1.2 3-3.3 3-5.5a7 7 0 0 0-14 0c0 2.2 1.2 4.3 3 5.5z"" /> <circle cx=""9.5"" cy=""12.5"" r=""1"" /> <circle cx=""14.5"" cy=""12.5"" r=""1"" /> <path d=""M5.5 9C3.5 8 2.5 6 3 3c1 2 2.5 3 4.5 3.5"" /> <path d=""M18.5 9c2-1 3-3 2.5-6-1 2-2.5 3-4.5 3.5"" />",
        ["g-passagem"] = @"<path d=""M2 21V12l2.5-5 2 3L9 9v12"" /> <path d=""M22 21V10l-2-4-2.5 3.5L15 9v12"" /> <path d=""M12 20v-5.5"" /> <path d=""M10.5 16 12 14.5l1.5 1.5"" />",
        ["g-esconderijo"] = @"<path d=""M2 20 12 6l10 14z"" /> <path d=""M10 20l2-5 2 5"" /> <path d=""M12 6V2.5l3.5 1.5L12 5.5"" />",

        // ---- Categorias de missao ----
        ["g-principal"] = @"<path d=""M6 3v18"" /> <path d=""M6 4h12l-3 4 3 4H6"" /> <path d=""M11.5 6v2.5"" /> <path d=""M11.5 10.5h.01"" />",
        ["g-secundaria"] = @"<path d=""M6 5h10v13a2 2 0 0 0 2 2H8a2 2 0 0 1-2-2z"" /> <path d=""M6 5a2 2 0 0 1 2-2h10a2 2 0 0 1 0 4h-2"" /> <path d=""M18 20a2 2 0 0 0 2-2v-1h-4"" /> <path d=""M9 10h4"" /> <path d=""M9 13.5h4"" />",
        ["g-diaria"] = @"<path d=""M21 12a9 9 0 1 1-2.6-6.4"" /> <path d=""M21 3v5h-5"" /> <circle cx=""12"" cy=""12"" r=""3"" />",

        // ---- Acontecimentos marcantes ----
        ["g-nivel"] = @"<circle cx=""12"" cy=""12"" r=""10"" /> <path d=""M8 12.5l4-4 4 4"" /> <path d=""M8 16.5l4-4 4 4"" />",
        ["g-descoberta"] = @"<path d=""M3 6l6-3 6 3 6-3v15l-6 3-6-3-6 3z"" /> <path d=""M13.5 10.5l3 3"" /> <path d=""M16.5 10.5l-3 3"" />",
        ["g-entrega"] = @"<path d=""M3 10h18v10H3z"" /> <path d=""M3 10a4 4 0 0 1 4-4h10a4 4 0 0 1 4 4"" /> <path d=""M3 14h7"" /> <path d=""M14 14h7"" /> <rect x=""10"" y=""12.5"" width=""4"" height=""4"" />",
    };

    /// <summary>Icone do papel (tanque, dano, suporte).</summary>
    public static string Role(string? role) => role switch
    {
        ClassRoles.Tank => "g-tanque",
        ClassRoles.Support => "g-suporte",
        _ => "g-dano",
    };

    /// <summary>Icone da zona: cidade, ou o bioma da zona de caca.</summary>
    public static string Zone(RegionDefinition z) => z.IsCity ? "g-cidade" : z.Biome switch
    {
        "floresta" => "g-floresta",
        "caverna" => "g-caverna",
        "neve" => "g-neve",
        "pantano" => "g-pantano",
        "ruinas" => "g-ruinas",
        "sombrio" => "g-sombrio",
        "vulcanico" => "g-vulcanico",
        _ => "g-planicie",
    };

    /// <summary>Icone do tipo de area.</summary>
    public static string Area(string kind) => kind switch
    {
        AreaKinds.Shop => "g-loja",
        AreaKinds.Forge => "g-forja",
        AreaKinds.Tavern => "g-taverna",
        AreaKinds.TownSquare => "g-praca",
        AreaKinds.TrainingGround => "g-treino",
        AreaKinds.HuntingGround => "g-caca",
        AreaKinds.OreVein => "g-minerio",
        AreaKinds.HerbPatch => "g-ervas",
        AreaKinds.CrystalNode => "g-cristal",
        AreaKinds.Landmark => "g-marco",
        AreaKinds.Dungeon => "g-masmorra",
        AreaKinds.BossLair => "g-covil",
        AreaKinds.Chokepoint => "g-passagem",
        AreaKinds.Hideout => "g-esconderijo",
        _ => "g-marco",
    };

    /// <summary>Icone da categoria de missao.</summary>
    public static string Quest(string category) => category switch
    {
        QuestCategories.Main => "g-principal",
        QuestCategories.Daily => "g-diaria",
        _ => "g-secundaria",
    };

    /// <summary>Todos os icones do jogo por grupo (galeria de conferencia).</summary>
    public static readonly (string Group, string[] Names)[] Groups =
    {
        ("Classes", new[] { "g-guerreiro", "g-mago", "g-sacerdote" }),
        ("Papéis", new[] { "g-tanque", "g-dano", "g-suporte" }),
        ("Zonas", new[] { "g-cidade", "g-planicie", "g-floresta", "g-caverna", "g-neve", "g-pantano", "g-ruinas", "g-sombrio", "g-vulcanico" }),
        ("Áreas", new[] { "g-loja", "g-forja", "g-taverna", "g-praca", "g-treino", "g-caca", "g-minerio", "g-ervas", "g-cristal", "g-marco", "g-masmorra", "g-covil", "g-passagem", "g-esconderijo" }),
        ("Missões", new[] { "g-principal", "g-secundaria", "g-diaria" }),
        ("Acontecimentos", new[] { "g-nivel", "g-descoberta", "g-entrega" }),
    };
}

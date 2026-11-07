using Godot;
using Simulation.Core.Content;

namespace GodotClient.Ui;

/// <summary>Identidade visual de cada tipo de area: nome, cor e emblema vetorial.</summary>
public static class AreaPalette
{
    public static string Name(string kind) => kind switch
    {
        AreaKinds.Shop => "Loja / mercado",
        AreaKinds.Forge => "Forja",
        AreaKinds.Tavern => "Taverna",
        AreaKinds.TownSquare => "Praça",
        AreaKinds.TrainingGround => "Campo de treino",
        AreaKinds.HuntingGround => "Área de caça",
        AreaKinds.OreVein => "Veio de minério",
        AreaKinds.HerbPatch => "Campo de ervas",
        AreaKinds.CrystalNode => "Veio de cristal",
        AreaKinds.Landmark => "Marco",
        AreaKinds.Dungeon => "Entrada de masmorra",
        AreaKinds.BossLair => "Covil de boss",
        AreaKinds.Chokepoint => "Passagem estreita",
        AreaKinds.Hideout => "Esconderijo de guilda",
        _ => kind,
    };

    public static Color Color(string kind) => kind switch
    {
        AreaKinds.Shop => UiTokens.Accent,
        AreaKinds.Forge => new Color("#F28C4E"),
        AreaKinds.Tavern => new Color("#FFB86B"),
        AreaKinds.TownSquare => new Color("#F3D08A"),
        AreaKinds.TrainingGround => new Color("#9DB4FF"),
        AreaKinds.HuntingGround => new Color("#F0796F"),
        AreaKinds.OreVein => new Color("#C9B79C"),
        AreaKinds.HerbPatch => new Color("#7FD8A6"),
        AreaKinds.CrystalNode => new Color("#B48CF2"),
        AreaKinds.Landmark => new Color("#D7CDBB"),
        AreaKinds.Dungeon => new Color("#A884FF"),
        AreaKinds.BossLair => UiTokens.Accent,
        AreaKinds.Chokepoint => UiTokens.Danger,
        AreaKinds.Hideout => new Color("#E8B44E"),
        _ => UiTokens.TextSecondary,
    };
}

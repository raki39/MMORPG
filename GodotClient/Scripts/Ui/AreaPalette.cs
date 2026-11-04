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

    /// <summary>Emblema do tipo de area centrado em c, meia-largura s.</summary>
    public static void DrawGlyph(CanvasItem g, string kind, Vector2 c, float s, Color col)
    {
        float w = Mathf.Max(1.4f, s * 0.2f);
        switch (kind)
        {
            case AreaKinds.Shop:
                g.DrawCircle(c + new Vector2(-s * 0.3f, s * 0.25f), s * 0.55f, col);
                g.DrawCircle(c + new Vector2(s * 0.35f, -s * 0.2f), s * 0.55f, col.Darkened(0.15f));
                g.DrawArc(c + new Vector2(s * 0.35f, -s * 0.2f), s * 0.32f, 0, Mathf.Tau, 14, new Color(0, 0, 0, 0.45f), 1f, true);
                break;
            case AreaKinds.Forge:
                g.DrawColoredPolygon(new[] { c + new Vector2(-s, -s * 0.35f), c + new Vector2(s, -s * 0.35f), c + new Vector2(s * 0.55f, s * 0.1f), c + new Vector2(-s * 0.45f, s * 0.1f) }, col);
                g.DrawColoredPolygon(new[] { c + new Vector2(-s * 0.25f, s * 0.1f), c + new Vector2(s * 0.3f, s * 0.1f), c + new Vector2(s * 0.55f, s * 0.75f), c + new Vector2(-s * 0.5f, s * 0.75f) }, col.Darkened(0.2f));
                break;
            case AreaKinds.Tavern:
                ActivityPalette.DrawGlyph(g, ActivityTypes.Socialize, c, s, col);
                break;
            case AreaKinds.TownSquare:
                g.DrawRect(new Rect2(c.X - s * 0.85f, c.Y - s * 0.75f, s * 1.7f, s * 1.1f), col);
                for (int i = 0; i < 3; i++)
                    g.DrawLine(c + new Vector2(-s * 0.6f, -s * 0.5f + i * s * 0.3f), c + new Vector2(s * 0.6f, -s * 0.5f + i * s * 0.3f), new Color(0, 0, 0, 0.5f), 1f, true);
                g.DrawLine(c + new Vector2(-s * 0.6f, s * 0.35f), c + new Vector2(-s * 0.6f, s), col, w, true);
                g.DrawLine(c + new Vector2(s * 0.6f, s * 0.35f), c + new Vector2(s * 0.6f, s), col, w, true);
                break;
            case AreaKinds.TrainingGround:
                ActivityPalette.DrawGlyph(g, ActivityTypes.Train, c, s, col);
                break;
            case AreaKinds.HuntingGround:
                ActivityPalette.DrawGlyph(g, ActivityTypes.Hunt, c, s, col);
                break;
            case AreaKinds.OreVein:
                g.DrawLine(c + new Vector2(-s * 0.7f, s * 0.8f), c + new Vector2(s * 0.45f, -s * 0.35f), col, w * 1.1f, true);
                g.DrawArc(c + new Vector2(s * 0.25f, -s * 0.2f), s * 0.75f, -Mathf.Pi * 0.95f, -Mathf.Pi * 0.1f, 14, col, w * 1.3f, true);
                break;
            case AreaKinds.HerbPatch:
                g.DrawColoredPolygon(new[] { c + new Vector2(0, -s), c + new Vector2(s * 0.7f, -s * 0.1f), c + new Vector2(0, s * 0.85f), c + new Vector2(-s * 0.7f, -s * 0.1f) }, col);
                g.DrawLine(c + new Vector2(0, -s * 0.8f), c + new Vector2(0, s), new Color(0, 0, 0, 0.45f), 1.2f, true);
                break;
            case AreaKinds.CrystalNode:
                g.DrawColoredPolygon(new[] { c + new Vector2(-s * 0.15f, -s), c + new Vector2(s * 0.3f, -s * 0.2f), c + new Vector2(0, s * 0.8f), c + new Vector2(-s * 0.45f, -s * 0.15f) }, col);
                g.DrawColoredPolygon(new[] { c + new Vector2(s * 0.45f, -s * 0.45f), c + new Vector2(s * 0.85f, s * 0.15f), c + new Vector2(s * 0.45f, s * 0.8f), c + new Vector2(s * 0.2f, s * 0.1f) }, col.Darkened(0.2f));
                break;
            case AreaKinds.Landmark:
                g.DrawRect(new Rect2(c.X - s * 0.65f, c.Y - s * 0.1f, s * 1.3f, s * 0.9f), col);
                g.DrawColoredPolygon(new[] { c + new Vector2(-s * 0.9f, -s * 0.1f), c + new Vector2(0, -s * 0.9f), c + new Vector2(s * 0.9f, -s * 0.1f) }, col.Darkened(0.2f));
                break;
            case AreaKinds.Dungeon:
                g.DrawArc(c + new Vector2(0, s * 0.2f), s * 0.85f, Mathf.Pi, Mathf.Tau, 18, col, w * 1.2f, true);
                g.DrawLine(c + new Vector2(-s * 0.85f, s * 0.2f), c + new Vector2(-s * 0.85f, s * 0.9f), col, w * 1.2f, true);
                g.DrawLine(c + new Vector2(s * 0.85f, s * 0.2f), c + new Vector2(s * 0.85f, s * 0.9f), col, w * 1.2f, true);
                for (int i = 0; i < 3; i++)
                    g.DrawLine(c + new Vector2(-s * 0.5f + i * s * 0.12f, s * (0.25f + i * 0.22f)), c + new Vector2(s * 0.5f - i * s * 0.12f, s * (0.25f + i * 0.22f)), col.Darkened(0.2f), 1.4f, true);
                break;
            case AreaKinds.BossLair:
                ActivityPalette.DrawGlyph(g, ActivityTypes.Boss, c, s, col);
                break;
            case AreaKinds.Chokepoint:
                g.DrawColoredPolygon(new[] { c + new Vector2(-s, s * 0.8f), c + new Vector2(-s * 0.55f, -s * 0.8f), c + new Vector2(-s * 0.18f, s * 0.8f) }, col);
                g.DrawColoredPolygon(new[] { c + new Vector2(s * 0.18f, s * 0.8f), c + new Vector2(s * 0.55f, -s * 0.8f), c + new Vector2(s, s * 0.8f) }, col.Darkened(0.2f));
                break;
            case AreaKinds.Hideout:
                ActivityPalette.DrawGlyph(g, ActivityTypes.Patrol, c, s, col);
                g.DrawCircle(c + new Vector2(0, -s * 0.15f), s * 0.2f, new Color(0, 0, 0, 0.6f));
                g.DrawRect(new Rect2(c.X - s * 0.08f, c.Y - s * 0.1f, s * 0.16f, s * 0.42f), new Color(0, 0, 0, 0.6f));
                break;
            default:
                g.DrawCircle(c, s * 0.5f, col);
                break;
        }
    }
}

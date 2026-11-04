using Godot;
using Simulation.Core.Content;
using Simulation.Core.Population;
using GodotClient.Sim;

namespace GodotClient.Ui;

/// <summary>Cor e icone vetorial de cada tipo de atividade (mapa, cartoes e listas).</summary>
public static class ActivityPalette
{
    public static Color Color(string type) => type switch
    {
        ActivityTypes.Hunt => new Color("#F0796F"),
        ActivityTypes.Boss => UiTokens.Accent,
        ActivityTypes.Gather => new Color("#7FD8A6"),
        ActivityTypes.Explore => new Color("#6FC6E8"),
        ActivityTypes.Quest => new Color("#F3D08A"),
        ActivityTypes.Trade => UiTokens.Accent,
        ActivityTypes.Repair => new Color("#C9B79C"),
        ActivityTypes.Train => new Color("#9DB4FF"),
        ActivityTypes.Socialize => new Color("#FFB86B"),
        ActivityTypes.Recruit => new Color("#C49BFF"),
        ActivityTypes.Patrol => new Color("#8EC9FF"),
        ActivityTypes.Ambush => UiTokens.Danger,
        _ => UiTokens.TextSecondary,
    };

    /// <summary>Desenha o icone do tipo centrado em c, com meia-largura s.</summary>
    public static void DrawGlyph(CanvasItem g, string type, Vector2 c, float s, Color col)
    {
        float w = Mathf.Max(1.4f, s * 0.22f);
        switch (type)
        {
            case ActivityTypes.Hunt:
                g.DrawLine(c + new Vector2(-s, s), c + new Vector2(s, -s), col, w, true);
                g.DrawLine(c + new Vector2(s, s), c + new Vector2(-s, -s), col, w, true);
                g.DrawLine(c + new Vector2(-s * 0.9f, s * 0.35f), c + new Vector2(-s * 0.35f, s * 0.9f), col, w, true);
                g.DrawLine(c + new Vector2(s * 0.9f, s * 0.35f), c + new Vector2(s * 0.35f, s * 0.9f), col, w, true);
                break;
            case ActivityTypes.Gather:
                g.DrawColoredPolygon(new[] { c + new Vector2(0, -s), c + new Vector2(s * 0.8f, -s * 0.2f), c + new Vector2(0, s), c + new Vector2(-s * 0.8f, -s * 0.2f) }, col);
                g.DrawLine(c + new Vector2(-s * 0.8f, -s * 0.2f), c + new Vector2(s * 0.8f, -s * 0.2f), new Color(0, 0, 0, 0.5f), 1f, true);
                break;
            case ActivityTypes.Boss:
                g.DrawColoredPolygon(new[] { c + new Vector2(-s, s * 0.6f), c + new Vector2(-s, -s * 0.5f), c + new Vector2(-s * 0.45f, 0), c + new Vector2(0, -s), c + new Vector2(s * 0.45f, 0), c + new Vector2(s, -s * 0.5f), c + new Vector2(s, s * 0.6f) }, col);
                break;
            case ActivityTypes.Quest:
                g.DrawRect(new Rect2(c.X - w * 0.7f, c.Y - s, w * 1.4f, s * 1.3f), col);
                g.DrawCircle(c + new Vector2(0, s * 0.75f), w * 0.85f, col);
                break;
            case ActivityTypes.Explore:
                g.DrawArc(c, s, 0, Mathf.Tau, 20, col, w * 0.7f, true);
                g.DrawColoredPolygon(new[] { c + new Vector2(0, -s * 0.8f), c + new Vector2(s * 0.28f, 0), c + new Vector2(0, s * 0.8f), c + new Vector2(-s * 0.28f, 0) }, col);
                break;
            case ActivityTypes.Trade:
                g.DrawCircle(c, s, col);
                g.DrawArc(c, s * 0.62f, 0, Mathf.Tau, 20, new Color(0, 0, 0, 0.45f), w * 0.6f, true);
                break;
            case ActivityTypes.Repair:
                g.DrawLine(c + new Vector2(-s * 0.8f, s * 0.8f), c + new Vector2(s * 0.3f, -s * 0.3f), col, w, true);
                g.DrawColoredPolygon(new[] { c + new Vector2(-s * 0.1f, -s), c + new Vector2(s, -s * 0.1f), c + new Vector2(s * 0.6f, s * 0.3f), c + new Vector2(-s * 0.5f, -s * 0.6f) }, col);
                break;
            case ActivityTypes.Train:
                g.DrawArc(c, s, 0, Mathf.Tau, 20, col, w * 0.7f, true);
                g.DrawArc(c, s * 0.55f, 0, Mathf.Tau, 16, col, w * 0.7f, true);
                g.DrawCircle(c, s * 0.18f, col);
                break;
            case ActivityTypes.Socialize:
                g.DrawRect(new Rect2(c.X - s * 0.75f, c.Y - s * 0.65f, s * 1.1f, s * 1.4f), col);
                g.DrawArc(c + new Vector2(s * 0.42f, 0), s * 0.38f, -Mathf.Pi / 2f, Mathf.Pi / 2f, 10, col, w * 0.7f, true);
                break;
            case ActivityTypes.Recruit:
                g.DrawLine(c + new Vector2(-s * 0.6f, s), c + new Vector2(-s * 0.6f, -s), col, w * 0.8f, true);
                g.DrawColoredPolygon(new[] { c + new Vector2(-s * 0.6f, -s), c + new Vector2(s, -s * 0.5f), c + new Vector2(-s * 0.6f, 0) }, col);
                break;
            case ActivityTypes.Patrol:
                g.DrawColoredPolygon(new[] { c + new Vector2(-s * 0.8f, -s * 0.8f), c + new Vector2(s * 0.8f, -s * 0.8f), c + new Vector2(s * 0.7f, s * 0.2f), c + new Vector2(0, s), c + new Vector2(-s * 0.7f, s * 0.2f) }, col);
                break;
            case ActivityTypes.Ambush:
                g.DrawColoredPolygon(new[] { c + new Vector2(0, -s), c + new Vector2(s * 0.28f, s * 0.35f), c + new Vector2(-s * 0.28f, s * 0.35f) }, col);
                g.DrawLine(c + new Vector2(-s * 0.55f, s * 0.42f), c + new Vector2(s * 0.55f, s * 0.42f), col, w, true);
                g.DrawLine(c + new Vector2(0, s * 0.42f), c + new Vector2(0, s), col, w, true);
                break;
            default:
                g.DrawCircle(c, s * 0.5f, col);
                break;
        }
    }
}

/// <summary>Frase curta do que a pessoa esta fazendo agora (dados reais do Core).</summary>
public static class StatusText
{
    public static string Of(CharacterView v, ContentDatabase content)
    {
        string Zone(string id) => content.Regions.TryGetValue(id, out var r) ? r.Name : id;
        string Act(string zone, string act) =>
            content.Regions.TryGetValue(zone, out var r) ? r.FindActivity(act)?.Name ?? act : act;
        string Place(string zone, string act) =>
            content.Regions.TryGetValue(zone, out var r) && r.AreaOfActivity(act) is { } area ? area.Name : Zone(zone);
        if (v.State == LifecycleState.Quit) return "Desistiu do jogo";
        if (v.State == LifecycleState.Inactive) return "Inativo";
        return v.Status switch
        {
            CharacterStatus.Offline => "Desconectado",
            CharacterStatus.Idle => $"Decidindo o que fazer · {Zone(v.RegionId)}",
            CharacterStatus.Traveling when v.ActivityId.Length > 0 => $"A caminho de {Place(v.TargetRegionId, v.ActivityId)} · {Act(v.TargetRegionId, v.ActivityId)}",
            CharacterStatus.Traveling => $"Andando · {Zone(v.RegionId)}",
            CharacterStatus.Doing when v.ActivityId.Length > 0 => $"{Act(v.TargetRegionId, v.ActivityId)} · {Place(v.TargetRegionId, v.ActivityId)}",
            _ => $"Descansando · {Zone(v.RegionId)}",
        };
    }
}

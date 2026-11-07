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

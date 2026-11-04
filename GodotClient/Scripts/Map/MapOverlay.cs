using Godot;

namespace GodotClient.Map;

/// <summary>Camada desenhada acima da populacao (selecao, tooltip, bussola).</summary>
public partial class MapOverlay : Node2D
{
    public Action<MapOverlay>? Painter { get; set; }

    public override void _Draw() => Painter?.Invoke(this);
}

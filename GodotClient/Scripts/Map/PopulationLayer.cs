using Godot;

namespace GodotClient.Map;

/// <summary>
/// Desenha todas as pessoas do mapa numa unica MultiMesh (plano 13.7: nada de um no por
/// jogador; atualizacao em lote). Por quadro, um unico envio de buffer para a engine.
/// Layout por instancia (Transform2D + cor): 12 floats.
/// </summary>
public partial class PopulationLayer : MultiMeshInstance2D
{
    private const int Stride = 12;
    private float[] _buffer = Array.Empty<float>();

    public void Init(int count)
    {
        var mm = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
            UseColors = true,
            Mesh = new QuadMesh { Size = Vector2.One },
        };
        mm.InstanceCount = count;
        Multimesh = mm;
        Material = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/person_dot.gdshader") };
        _buffer = new float[count * Stride];
    }

    public void Write(int index, Vector2 position, float diameter, Color color)
    {
        int o = index * Stride;
        _buffer[o + 0] = diameter;
        _buffer[o + 1] = 0;
        _buffer[o + 2] = 0;
        _buffer[o + 3] = position.X;
        _buffer[o + 4] = 0;
        _buffer[o + 5] = diameter;
        _buffer[o + 6] = 0;
        _buffer[o + 7] = position.Y;
        _buffer[o + 8] = color.R;
        _buffer[o + 9] = color.G;
        _buffer[o + 10] = color.B;
        _buffer[o + 11] = color.A;
    }

    public void Commit()
    {
        if (Multimesh != null && _buffer.Length > 0) Multimesh.Buffer = _buffer;
    }
}

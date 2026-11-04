using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Retrato estilizado gerado a partir da semente de aparencia da pessoa (plano 7.3:
/// variacao individual deterministica). Mesma pessoa, mesmo rosto, em qualquer sessao.
/// </summary>
public partial class PortraitBadge : Control
{
    private static readonly Color[] Skin = { new("#F1C9A5"), new("#E3B08C"), new("#C98E68"), new("#A06A47"), new("#7C5036"), new("#5C3A27") };
    private static readonly Color[] Hair = { new("#2A1C13"), new("#4B3021"), new("#8B5A2B"), new("#C9A25A"), new("#D9D3C6"), new("#A33B2B"), new("#3A3B46") };

    private readonly ulong _seed;
    private readonly Color _ring;

    public PortraitBadge(ulong seed, Color ring)
    {
        _seed = seed;
        _ring = ring;
    }

    private int Bits(int shift, int mod) => (int)((_seed >> shift) % (ulong)mod);

    public override void _Draw()
    {
        var size = Size;
        var c = size / 2f;
        float r = Mathf.Min(size.X, size.Y) / 2f - 2;
        var skin = Skin[Bits(0, Skin.Length)];
        var hair = Hair[Bits(8, Hair.Length)];
        int style = Bits(16, 4);
        var bg = UiTokens.SurfaceRaised.Lerp(_ring, 0.18f);

        DrawCircle(c, r, bg);
        // Ombros: circulo recortado pelo contorno do retrato.
        var shoulders = new Vector2[40];
        var sc = c + new Vector2(0, r * 1.05f);
        for (int i = 0; i < shoulders.Length; i++)
        {
            float a = i * Mathf.Tau / shoulders.Length;
            var pt = sc + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 0.78f;
            var off = pt - c;
            if (off.Length() > r - 1) pt = c + off.Normalized() * (r - 1);
            shoulders[i] = pt;
        }
        DrawColoredPolygon(shoulders, _ring.Darkened(0.35f));
        // Cabeca
        var head = c + new Vector2(0, -r * 0.05f);
        float hr = r * 0.52f;
        DrawCircle(head + new Vector2(0, 2), hr, new Color(0, 0, 0, 0.25f));
        DrawCircle(head, hr, skin);
        // Cabelo por estilo
        switch (style)
        {
            case 0: DrawArc(head, hr * 0.92f, Mathf.Pi * 1.05f, Mathf.Pi * 1.95f, 24, hair, hr * 0.45f, true); break;
            case 1:
                DrawArc(head, hr * 0.9f, Mathf.Pi * 0.9f, Mathf.Pi * 2.1f, 28, hair, hr * 0.5f, true);
                DrawCircle(head + new Vector2(-hr * 0.95f, hr * 0.2f), hr * 0.32f, hair);
                DrawCircle(head + new Vector2(hr * 0.95f, hr * 0.2f), hr * 0.32f, hair);
                break;
            case 2: DrawCircle(head + new Vector2(0, -hr * 0.85f), hr * 0.42f, hair); DrawArc(head, hr * 0.9f, Mathf.Pi * 1.1f, Mathf.Pi * 1.9f, 20, hair, hr * 0.3f, true); break;
            default: DrawArc(head, hr * 0.95f, Mathf.Pi * 1.15f, Mathf.Pi * 1.85f, 20, hair, hr * 0.22f, true); break;
        }
        // Olhos e boca
        var eye = new Color(0.08f, 0.07f, 0.07f);
        DrawCircle(head + new Vector2(-hr * 0.36f, hr * 0.08f), hr * 0.11f, eye);
        DrawCircle(head + new Vector2(hr * 0.36f, hr * 0.08f), hr * 0.11f, eye);
        DrawArc(head + new Vector2(0, hr * 0.28f), hr * 0.28f, Mathf.Pi * 0.2f, Mathf.Pi * 0.8f, 10, new Color(0.25f, 0.12f, 0.1f, 0.9f), 1.6f, true);
        // Aro do perfil
        DrawArc(c, r, 0, Mathf.Tau, 48, _ring, 2.5f, true);
    }
}

/// <summary>Pequena esfera colorida (legenda), no mesmo estilo dos marcadores do mapa.</summary>
public partial class DotIcon : Control
{
    private readonly Color _color;

    public DotIcon(Color color) => _color = color;

    public override void _Draw()
    {
        var c = Size / 2f;
        float r = Mathf.Min(Size.X, Size.Y) / 2f;
        DrawCircle(c, r, new Color(0.03f, 0.04f, 0.05f));
        DrawCircle(c, r - 1.5f, _color);
        DrawCircle(c - new Vector2(r * 0.3f, r * 0.3f), r * 0.3f, new Color(1, 1, 1, 0.45f));
    }
}

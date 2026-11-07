using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Retrato da pessoa (D-068, refeito com o criador): um brasao unico, gerado da semente de aparencia, dentro da
/// moldura padrao dos emblemas (degrade e faixa no topo na cor do perfil). O escudo tem uma das oito divisoes
/// heraldicas, um metal (ouro ou prata) e uma cor; no centro, o icone da classe. Desenho estatico, deterministico.
/// </summary>
public partial class PortraitBadge : Control
{
    private readonly ulong _seed;
    private readonly Color _ring;
    /// <summary>Icone da classe: a figura no centro do escudo.</summary>
    public string ClassIcon { get; set; } = "";

    public PortraitBadge(ulong seed, Color ring, string name = "")
    {
        _seed = seed;
        _ring = ring;
        MouseFilter = MouseFilterEnum.Ignore;
    }

    /// <summary>Iniciais do nome (listas e rotulos que ainda usam texto).</summary>
    public static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "";
        if (parts.Length == 1) return parts[0].Length >= 2 ? parts[0][..2].ToUpperInvariant() : parts[0].ToUpperInvariant();
        return (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }

    private static readonly Color[] Metals = { new("#C9A45C"), new("#AEB6C1") };
    private static readonly Color[] Tinctures =
    {
        new("#3D6EA6"), new("#A6453D"), new("#3E8A5B"), new("#6C4E9C"), new("#2B3039"), new("#A2652F"), new("#2E7C84"),
    };

    /// <summary>Numeros estaveis da semente (mistura inteira, sem RNG).</summary>
    private static ulong Mix(ulong x, ulong salt)
    {
        x += 0x9E3779B97F4A7C15UL * (salt + 1);
        x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
        x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
        return x ^ (x >> 31);
    }

    /// <summary>Contorno do escudo (heater): topo reto, lados retos ate ~45% e curvas ate a ponta.</summary>
    private static Vector2[] Shield(Rect2 r)
    {
        var pts = new List<Vector2>();
        float x0 = r.Position.X, x1 = r.End.X, y0 = r.Position.Y, y1 = r.End.Y, cx = (x0 + x1) / 2f;
        float ym = y0 + r.Size.Y * 0.42f;
        pts.Add(new Vector2(x0, y0));
        pts.Add(new Vector2(x1, y0));
        pts.Add(new Vector2(x1, ym));
        const int n = 10;
        for (int i = 1; i <= n; i++)
        {
            float t = i / (float)n;
            // Bezier quadratica: (x1, ym) -> controle (x1, y1 - 12%) -> (cx, y1)
            var a = new Vector2(x1, ym);
            var c = new Vector2(x1, y1 - r.Size.Y * 0.12f);
            var b = new Vector2(cx, y1);
            pts.Add((1 - t) * (1 - t) * a + 2 * (1 - t) * t * c + t * t * b);
        }
        for (int i = n - 1; i >= 1; i--)
        {
            float t = i / (float)n;
            var a = new Vector2(x0, ym);
            var c = new Vector2(x0, y1 - r.Size.Y * 0.12f);
            var b = new Vector2(cx, y1);
            pts.Add((1 - t) * (1 - t) * a + 2 * (1 - t) * t * c + t * t * b);
        }
        pts.Add(new Vector2(x0, ym));
        return pts.ToArray();
    }

    private void Fill(Vector2[] shield, Vector2[] region, Color color)
    {
        foreach (var poly in Geometry2D.IntersectPolygons(shield, region))
            if (poly.Length >= 3) DrawColoredPolygon(poly, color);
    }

    public override void _Draw()
    {
        float side = Mathf.Min(Size.X, Size.Y);
        bool small = side < 40;
        // Moldura padrao (como EmblemBox), na cor do perfil.
        var rect = new Rect2(Vector2.One, Size - Vector2.One * 2);
        var top = new Color("#141922").Lerp(_ring, 0.18f);
        var bottom = new Color("#0B0E13");
        DrawPolygon(new[] { rect.Position, new Vector2(rect.End.X, rect.Position.Y), rect.End, new Vector2(rect.Position.X, rect.End.Y) }, new[] { top, top, bottom, bottom });
        DrawRect(rect, new Color(_ring, 0.45f), false, 1f);
        DrawRect(new Rect2(rect.Position, new Vector2(rect.Size.X, small ? 2 : 3)), _ring);

        // Escudo centrado, um pouco abaixo da faixa.
        float sw = Mathf.Round(side * (small ? 0.68f : 0.64f)), sh = Mathf.Round(sw * 1.14f);
        var sr = new Rect2(new Vector2(Mathf.Round((Size.X - sw) / 2f), Mathf.Round((Size.Y - sh) / 2f + side * 0.04f)), new Vector2(sw, sh));
        var shield = Shield(sr);
        var metal = Metals[(int)(Mix(_seed, 1) % (ulong)Metals.Length)];
        var tincture = Tinctures[(int)(Mix(_seed, 2) % (ulong)Tinctures.Length)];
        bool metalField = Mix(_seed, 3) % 2 == 0;
        var field = metalField ? metal : tincture;
        var second = metalField ? tincture : metal;
        int division = (int)(Mix(_seed, 4) % 8);
        float x0 = sr.Position.X - 2, x1 = sr.End.X + 2, y0 = sr.Position.Y - 2, y1 = sr.End.Y + 2, cx = sr.GetCenter().X;
        float h = sr.Size.Y;
        DrawColoredPolygon(shield, field);
        switch (division)
        {
            case 1: Fill(shield, new[] { new Vector2(cx, y0), new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(cx, y1) }, second); break;            // partido
            case 2: Fill(shield, new[] { new Vector2(x0, y0 + h * 0.48f), new Vector2(x1, y0 + h * 0.48f), new Vector2(x1, y1), new Vector2(x0, y1) }, second); break;   // cortado
            case 3: Fill(shield, new[] { new Vector2(x0, y0), new Vector2(x1, y1), new Vector2(x0, y1) }, second); break;                                    // fendido em banda
            case 4: Fill(shield, new[] { new Vector2(x1, y0), new Vector2(x1, y1), new Vector2(x0, y1) }, second); break;                                    // em barra
            case 5: Fill(shield, new[] { new Vector2(x0, y1), new Vector2(x0, y0 + h * 0.78f), new Vector2(cx, y0 + h * 0.36f), new Vector2(x1, y0 + h * 0.78f), new Vector2(x1, y1) }, second); break;   // chaveirão
            case 6:                                                                                                                                         // esquartelado
                Fill(shield, new[] { new Vector2(cx, y0), new Vector2(x1, y0), new Vector2(x1, y0 + h * 0.48f), new Vector2(cx, y0 + h * 0.48f) }, second);
                Fill(shield, new[] { new Vector2(x0, y0 + h * 0.48f), new Vector2(cx, y0 + h * 0.48f), new Vector2(cx, y1), new Vector2(x0, y1) }, second);
                break;
            case 7:                                                                                                                                         // bordadura
                var inner = Shield(sr.Grow(-Mathf.Max(2f, sw * 0.13f)));
                DrawColoredPolygon(shield, second);
                DrawColoredPolygon(inner, field);
                break;
        }
        // Brilho leve no alto e contorno escuro (acabamento de emblema).
        Fill(shield, new[] { new Vector2(x0, y0), new Vector2(x1, y0), new Vector2(x1, y0 + h * 0.18f), new Vector2(x0, y0 + h * 0.18f) }, new Color(1, 1, 1, 0.07f));
        var outline = new Vector2[shield.Length + 1];
        shield.CopyTo(outline, 0);
        outline[^1] = shield[0];
        DrawPolyline(outline, new Color("#07090C"), small ? 1.2f : 1.6f, true);

        // Figura: o icone da classe, claro com contorno escuro (le em qualquer divisao).
        if (ClassIcon.Length > 0 && sw >= 18)
        {
            int isz = (int)Mathf.Max(10, Mathf.Round(sw * 0.56f / 2f) * 2);
            var at = new Vector2(Mathf.Round(sr.GetCenter().X - isz / 2f), Mathf.Round(sr.Position.Y + h * 0.42f - isz / 2f));
            float stroke = isz >= 24 ? 2.2f : 1.7f;
            var tex = Icons.Get(ClassIcon, isz, stroke);
            // Contorno escuro em volta do traco claro: le tanto na prata quanto nas cores escuras.
            var dark = new Color("#11151B", 0.9f);
            float o = isz >= 24 ? 1.5f : 1f;
            foreach (var d in new[] { new Vector2(o, 0), new Vector2(-o, 0), new Vector2(0, o), new Vector2(0, -o), new Vector2(o, o) })
                DrawTextureRect(tex, new Rect2(at + d, new Vector2(isz, isz)), false, dark);
            DrawTextureRect(tex, new Rect2(at, new Vector2(isz, isz)), false, new Color("#F4EEDF"));
        }
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

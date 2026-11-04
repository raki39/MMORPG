using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Identificador visual da pessoa (D-033): cartao plano com as iniciais na cor do perfil e
/// uma faixa estavel derivada da semente de aparencia. Substitui o rosto cartunesco: tom de
/// jogo de gestao, nao de MMORPG mobile.
/// </summary>
public partial class PortraitBadge : Control
{
    private readonly ulong _seed;
    private readonly Color _ring;
    private readonly string _initials;
    /// <summary>Icone da classe (selo no canto e marca d'agua nos tamanhos grandes).</summary>
    public string ClassIcon { get; set; } = "";

    public PortraitBadge(ulong seed, Color ring, string name = "")
    {
        _seed = seed;
        _ring = ring;
        _initials = Initials(name);
    }

    public static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return "";
        if (parts.Length == 1) return parts[0].Length >= 2 ? parts[0][..2].ToUpperInvariant() : parts[0].ToUpperInvariant();
        return (parts[0][..1] + parts[^1][..1]).ToUpperInvariant();
    }

    public override void _Draw()
    {
        var rect = new Rect2(Vector2.One, Size - Vector2.One * 2);
        float h = Mathf.Min(Size.X, Size.Y);
        bool big = h >= 64;
        // Fundo em degrade vertical tingido pelo perfil.
        var top = new Color("#141922").Lerp(_ring, 0.16f);
        var bottom = new Color("#0B0E13");
        DrawPolygon(new[] { rect.Position, new Vector2(rect.End.X, rect.Position.Y), rect.End, new Vector2(rect.Position.X, rect.End.Y) },
            new[] { top, top, bottom, bottom });
        // Faixa diagonal estavel pela semente (diferencia pessoas de mesmo perfil).
        float k = (_seed % 7) / 7f;
        var band = new[] { rect.Position + new Vector2(rect.Size.X * (0.15f + k * 0.5f), rect.Size.Y), rect.Position + new Vector2(rect.Size.X * (0.45f + k * 0.5f), rect.Size.Y), rect.Position + new Vector2(rect.Size.X, rect.Size.Y * (0.25f + k * 0.3f)), rect.Position + new Vector2(rect.Size.X, rect.Size.Y * (0.05f + k * 0.2f)) };
        DrawColoredPolygon(band, new Color(_ring, 0.08f));
        if (big && ClassIcon.Length > 0)
        {
            int ws = (int)(h * 0.62f);
            DrawTextureRect(Icons.Get(ClassIcon, ws, 2f), new Rect2(((Size - new Vector2(ws, ws)) / 2f + new Vector2(h * 0.14f, h * 0.1f)).Round(), new Vector2(ws, ws)), false, new Color(_ring, 0.10f));
        }
        DrawRect(rect, new Color(_ring, 0.45f), false, 1f);
        DrawRect(new Rect2(rect.Position, new Vector2(rect.Size.X, big ? 3 : 2)), _ring);
        if (_initials.Length > 0)
        {
            int fs = (int)Mathf.Max(9, h * 0.36f);
            var font = UiFonts.Bold;
            var ts = font.GetStringSize(_initials, HorizontalAlignment.Left, -1, fs);
            DrawString(font, new Vector2(Mathf.Round((Size.X - ts.X) / 2f), Mathf.Round((Size.Y + font.GetAscent(fs) - font.GetDescent(fs)) / 2f)), _initials, HorizontalAlignment.Left, -1, fs, _ring.Lightened(0.35f));
        }
        if (big && ClassIcon.Length > 0)
        {
            // Selo da classe no canto inferior direito.
            float bs = Mathf.Round(h * 0.28f);
            var br = new Rect2(rect.End - new Vector2(bs, bs), new Vector2(bs, bs));
            DrawRect(br, new Color("#0B0E13"));
            DrawRect(br, new Color(_ring, 0.5f), false, 1f);
            int isz = (int)(bs * 0.62f);
            DrawTextureRect(Icons.Get(ClassIcon, isz), new Rect2((br.Position + (br.Size - new Vector2(isz, isz)) / 2f).Round(), new Vector2(isz, isz)), false, UiTokens.TextPrimary);
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

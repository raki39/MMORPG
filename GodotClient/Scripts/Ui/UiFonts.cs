using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Fontes do cliente (licenca OFL, arquivos em Assets/Fonts):
///  - Inter (variavel): interface, com numeros tabulares para metricas alinhadas;
///  - Cinzel (variavel): nomes no mapa e titulos de destaque.
/// </summary>
public static class UiFonts
{
    private static FontVariation? _regular, _medium, _semibold, _bold, _numbers, _display;

    public static FontVariation Regular => _regular ??= Inter(400);
    public static FontVariation Medium => _medium ??= Inter(500);
    public static FontVariation SemiBold => _semibold ??= Inter(600);
    public static FontVariation Bold => _bold ??= Inter(700);
    /// <summary>Inter com numeros tabulares (tnum): datas, contadores e tabelas.</summary>
    public static FontVariation Numbers => _numbers ??= Inter(500, tabular: true);
    public static FontVariation Display => _display ??= Make("res://Assets/Fonts/Cinzel.ttf", 700, false, 1);

    private static FontVariation Inter(int weight, bool tabular = false) =>
        Make("res://Assets/Fonts/Inter.ttf", weight, tabular, 0);

    private static FontVariation Make(string path, int weight, bool tabular, int spacing)
    {
        var file = GD.Load<FontFile>(path);
        var ts = TextServerManager.GetPrimaryInterface();
        var v = new FontVariation { BaseFont = file };
        v.VariationOpentype = new Godot.Collections.Dictionary { { ts.NameToTag("wght"), weight } };
        if (tabular)
            v.OpentypeFeatures = new Godot.Collections.Dictionary { { ts.NameToTag("tnum"), 1 } };
        if (spacing != 0)
            v.SpacingGlyph = spacing;
        return v;
    }
}

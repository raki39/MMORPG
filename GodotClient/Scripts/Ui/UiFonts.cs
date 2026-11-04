using Godot;

namespace GodotClient.Ui;

/// <summary>
/// Fontes do cliente (OFL, em Assets/Fonts), direcao "gestao esportiva + fantasia" (D-022):
///  - Inter: interface, textos e numeros tabulares (D-033);
///  - Barlow Condensed: rotulos, cabecalhos de secao e colunas (caixa alta);
///  - numeros tabulares para metricas alinhadas;
///  - Cinzel: apenas nomes de zona no mapa (tom de MMORPG); fichas usam Barlow (D-033).
/// </summary>
public static class UiFonts
{
    private static Font? _regular, _medium, _semibold, _bold, _condMedium, _condSemi, _condBold, _numbers, _display;

    // Inter (OFL): fonte de interface de dados, legivel em tamanhos pequenos (D-033).
    public static Font Regular => _regular ??= Load("Inter-Regular.ttf");
    public static Font Medium => _medium ??= Load("Inter-Medium.ttf");
    public static Font SemiBold => _semibold ??= Load("Inter-SemiBold.ttf");
    public static Font Bold => _bold ??= Load("Inter-Bold.ttf");
    public static Font Condensed => _condMedium ??= Load("BarlowCondensed-Medium.ttf");
    public static Font CondensedSemiBold => _condSemi ??= Load("BarlowCondensed-SemiBold.ttf");
    public static Font CondensedBold => _condBold ??= Load("BarlowCondensed-Bold.ttf");
    /// <summary>Numeros tabulares (tnum) para colunas e indicadores.</summary>
    public static Font Numbers => _numbers ??= Load("Inter-SemiBold.ttf", tabular: true);
    public static Font Display => _display ??= Variable("Cinzel.ttf", 700);
    /// <summary>Titulos de fichas e paineis: Barlow SemiBold (tom de jogo de gestao).</summary>
    public static Font Title => SemiBold;

    private static Font Load(string file, bool tabular = false)
    {
        var f = GD.Load<FontFile>("res://Assets/Fonts/" + file);
        if (!tabular) return f;
        var ts = TextServerManager.GetPrimaryInterface();
        return new FontVariation { BaseFont = f, OpentypeFeatures = new Godot.Collections.Dictionary { { ts.NameToTag("tnum"), 1 } } };
    }

    private static Font Variable(string file, int weight)
    {
        var ts = TextServerManager.GetPrimaryInterface();
        return new FontVariation
        {
            BaseFont = GD.Load<FontFile>("res://Assets/Fonts/" + file),
            VariationOpentype = new Godot.Collections.Dictionary { { ts.NameToTag("wght"), weight } },
        };
    }
}

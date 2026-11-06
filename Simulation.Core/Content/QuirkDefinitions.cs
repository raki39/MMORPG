namespace Simulation.Core.Content;

// Traits (P4b): tracos marcantes e nomeados da pessoa, de 0 a 6, separados do Perfil, das
// Preferencias, das Especialidades, dos atributos numericos e da personalidade principal. Ficam em
// traits.json. No codigo o nome e "Quirk" para nao confundir com os atributos numericos
// (PersonTraits, TraitRange, TraitDraw), que vieram antes.

/// <summary>Conteudo de traits.json: regras do sorteio e catalogo.</summary>
public sealed class QuirkCatalogDefinition
{
    public QuirkGenerationDefinition Generation { get; set; } = new();
    public List<QuirkDefinition> Traits { get; set; } = new();
}

/// <summary>Sorteio na criacao da pessoa (P4b). Todos os valores sao candidatos para medicao.</summary>
public sealed class QuirkGenerationDefinition
{
    public bool Enabled { get; set; } = true;
    /// <summary>Pesos de 0, 1, 2... traits (o indice e a quantidade). Preset: 8, 25, 50, 10, 4, 2 e 1%.</summary>
    public List<int> CountWeights { get; set; } = new() { 80, 250, 500, 100, 40, 20, 10 };
    /// <summary>Chance de cada vaga ser negativa (sorteio por vaga, nao proporcao fixa por pessoa).</summary>
    public int NegativeChancePermille { get; set; } = 400;
    /// <summary>Limites do multiplicador de coerencia (permille): nada fica impossivel nem obrigatorio.</summary>
    public int MinMultiplierPermille { get; set; } = 250;
    public int MaxMultiplierPermille { get; set; } = 3000;
    /// <summary>Total de traits por pessoa, contando as adquiridas depois (P4d).</summary>
    public int MaxTotal { get; set; } = 8;
}

public static class QuirkPolarities
{
    public const string Positive = "positive";
    public const string Negative = "negative";
}

public static class QuirkAcquisitions
{
    /// <summary>Nasce com a pessoa (predisposicao).</summary>
    public const string Innate = "innate";
    /// <summary>So adquirida por evidencia, com o sistema correspondente (P4d).</summary>
    public const string Acquired = "acquired";
    public const string Both = "both";
    public static readonly string[] All = { Innate, Acquired, Both };
}

/// <summary>Sistema de que o efeito depende; vazio = efeitos da pessoa (decisao, humor, memoria, rotina).</summary>
public static class QuirkSystems
{
    public const string Person = "";
    public const string Combat = "M3";
    public const string Economy = "M5";
    public const string Groups = "M6";
    public const string Pvp = "etapa3";
    public const string Crafting = "fabricacao";
    public static readonly string[] All = { Person, Combat, Economy, Groups, Pvp, Crafting };
}

public sealed class QuirkDefinition : IContentDefinition
{
    public string Id { get; set; } = "";
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "";
    /// <summary>positive ou negative (explicita).</summary>
    public string Polarity { get; set; } = QuirkPolarities.Positive;
    /// <summary>innate, acquired ou both.</summary>
    public string Acquisition { get; set; } = QuirkAcquisitions.Innate;
    /// <summary>Entra no sorteio da criacao. Falso: so no catalogo (definicao pendente) ou so adquirida.</summary>
    public bool Drawn { get; set; } = true;
    /// <summary>Sistema de que o efeito depende (QuirkSystems).</summary>
    public string System { get; set; } = QuirkSystems.Person;
    /// <summary>Peso base no sorteio dentro da classificacao.</summary>
    public int Weight { get; set; } = 1000;
    /// <summary>Condicao e efeito, para a ficha.</summary>
    public string Effect { get; set; } = "";
    /// <summary>Contrapartida, para a ficha.</summary>
    public string Tradeoff { get; set; } = "";
    /// <summary>Coerencia com a pessoa: aumentam ou reduzem a chance (multiplicador limitado).</summary>
    public List<QuirkAffinityDefinition> Affinities { get; set; } = new();
    /// <summary>Traits contraditorias (valem nos dois sentidos).</summary>
    public List<string> Excludes { get; set; } = new();
    /// <summary>Personalidades principais contraditorias ou redundantes.</summary>
    public List<string> ExcludesPersonalities { get; set; } = new();
}

/// <summary>
/// Afinidade de uma trait com a pessoa. Chaves:
/// - "traits.x": atributo (posicao no preset); direction "high" ou "low"; o termo vai de
///   1000 - forca (extremo oposto) a 1000 + forca (extremo da direcao);
/// - "preferences.x": preferencia (posicao no preset), igual;
/// - "profile.x" e "personality.x": se a pessoa tem esse perfil ou personalidade, termo 1000 + forca
///   (a forca pode ser negativa).
/// O multiplicador e o produto dos termos, limitado por min e max do sorteio.
/// </summary>
public sealed class QuirkAffinityDefinition
{
    public string Key { get; set; } = "";
    public string Direction { get; set; } = "high";
    public int StrengthPermille { get; set; }
}

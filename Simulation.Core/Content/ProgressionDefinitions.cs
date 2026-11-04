namespace Simulation.Core.Content;

/// <summary>
/// Regras de progressao do MMORPG (D-027). Fazem parte do conteudo: o jogador, como
/// desenvolvedor do MMORPG, podera ajusta-las no editor do jogo (como areas, itens e magias).
/// Unidades: XP inteiro, taxas em permille.
/// </summary>
public sealed class ProgressionDefinition
{
    /// <summary>Nivel maximo atual (teto). Expansoes podem aumentar (ate 100).</summary>
    public int LevelCap { get; set; }

    /// <summary>XP para sair do nivel n: cubic*n^3 + quadratic*n^2 + linear*n + constant.</summary>
    public XpCurveDefinition XpCurve { get; set; } = new();

    /// <summary>
    /// Taxa de XP por faixa de nivel ("stages"): a partir de fromLevel, todo XP ganho e
    /// multiplicado por ratePermille/1000. Faixas mais altas tem taxa menor.
    /// </summary>
    public List<XpStageDefinition> Stages { get; set; } = new();

    /// <summary>XP por minuto de cada tipo de atividade num desafio de nivel 1. Tipos ausentes nao dao XP.</summary>
    public Dictionary<string, int> ActivityXpPerMinute { get; set; } = new();

    /// <summary>Quanto o XP por minuto cresce por nivel de desafio acima de 1 (permille por nivel).</summary>
    public int ChallengeGrowthPermille { get; set; }

    /// <summary>Perda de rendimento quando o desafio esta longe do nivel do personagem.</summary>
    public LevelDifferenceDefinition LevelDifference { get; set; } = new();
}

public sealed class XpCurveDefinition
{
    public long Cubic { get; set; }
    public long Quadratic { get; set; }
    public long Linear { get; set; }
    public long Constant { get; set; }
}

public sealed class XpStageDefinition
{
    public int FromLevel { get; set; }
    public int RatePermille { get; set; }
}

public sealed class LevelDifferenceDefinition
{
    /// <summary>Diferenca de niveis sem perda nenhuma, para cima e para baixo.</summary>
    public int ToleranceLevels { get; set; }
    /// <summary>Perda por nivel quando o personagem esta acima do desafio (permille por nivel).</summary>
    public int WeakChallengePenaltyPermille { get; set; }
    /// <summary>Rendimento minimo contra desafio fraco (permille).</summary>
    public int WeakChallengeFloorPermille { get; set; }
    /// <summary>Perda por nivel quando o desafio esta acima do personagem (permille por nivel).</summary>
    public int StrongChallengePenaltyPermille { get; set; }
}

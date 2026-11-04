using Simulation.Core.Content;

namespace Simulation.Core.Behavior;

/// <summary>
/// Regras de XP e nivel lidas do conteudo (progression.json, D-027). Tres parametros
/// separados: faixa da area (orientacao), nivel do desafio (rendimento) e requisitos.
/// O XP de uma atividade sai do nivel do desafio comparado ao do personagem, nunca da
/// faixa da zona; atividades sem desafio (servicos da cidade, pegar missao) nao dao XP.
/// </summary>
public static class ProgressionRules
{
    /// <summary>XP necessario para sair do nivel atual.</summary>
    public static long XpToNextLevel(ProgressionDefinition p, int level)
    {
        long n = level;
        var c = p.XpCurve;
        return Math.Max(1, c.Cubic * n * n * n + c.Quadratic * n * n + c.Linear * n + c.Constant);
    }

    /// <summary>Taxa da faixa de nivel (permille). Antes da primeira faixa vale 1000.</summary>
    public static int StageRate(ProgressionDefinition p, int level)
    {
        int rate = 1000;
        foreach (var s in p.Stages)
            if (level >= s.FromLevel) rate = s.RatePermille;
        return rate;
    }

    /// <summary>XP por minuto do tipo num desafio de nivel 1 (0 = nao da XP).</summary>
    public static int BaseXpPerMinute(ProgressionDefinition p, string type) =>
        p.ActivityXpPerMinute.TryGetValue(type, out int v) ? v : 0;

    /// <summary>
    /// Nivel do desafio da atividade: o declarado; senao, para caca e chefe, a media dos
    /// niveis das criaturas. 0 = sem desafio (nao da XP).
    /// </summary>
    public static int ChallengeLevel(ContentDatabase content, ActivityDefinition a)
    {
        if (a.ChallengeLevel > 0) return a.ChallengeLevel;
        if (a.Type != ActivityTypes.Hunt && a.Type != ActivityTypes.Boss) return 0;
        int sum = 0, count = 0;
        foreach (var id in a.CreatureIds)
            if (content.Creatures.TryGetValue(id, out var cr)) { sum += cr.Level; count++; }
        return count == 0 ? 0 : sum / count;
    }

    /// <summary>Rendimento pela diferenca entre personagem e desafio (permille, 1000 = pleno).</summary>
    public static int Efficiency(ProgressionDefinition p, int level, int challenge)
    {
        var d = p.LevelDifference;
        int diff = level - challenge;
        if (diff > d.ToleranceLevels)
            return Math.Max(d.WeakChallengeFloorPermille, 1000 - (diff - d.ToleranceLevels) * d.WeakChallengePenaltyPermille);
        if (-diff > d.ToleranceLevels)
            return Math.Max(0, 1000 - (-diff - d.ToleranceLevels) * d.StrongChallengePenaltyPermille);
        return 1000;
    }

    /// <summary>
    /// XP por minuto da atividade para o personagem, em milesimos de XP (para nao perder
    /// precisao com inteiros): base x crescimento do desafio x rendimento x faixa.
    /// </summary>
    public static long MilliXpPerMinute(ContentDatabase content, ActivityDefinition a, int level)
    {
        var p = content.Progression;
        int baseRate = BaseXpPerMinute(p, a.Type);
        int challenge = ChallengeLevel(content, a);
        if (baseRate == 0 || challenge == 0 || level >= p.LevelCap) return 0;
        long growth = 1000L + (long)p.ChallengeGrowthPermille * (challenge - 1);
        return baseRate * growth * Efficiency(p, level, challenge) / 1000 * StageRate(p, level) / 1000;
    }

    /// <summary>XP inteiro de uma atividade com a duracao dada.</summary>
    public static long XpFor(ContentDatabase content, ActivityDefinition a, int level, int minutes) =>
        minutes <= 0 ? 0 : MilliXpPerMinute(content, a, level) * minutes / 1000;
}

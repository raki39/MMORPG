using Simulation.Core.Content;

namespace Simulation.Core.Combat;

/// <summary>Resultado do indice de poder, com as parcelas (tooltip e testes).</summary>
/// <param name="Power">Poder nominal (o personagem de referencia vale Scale).</param>
/// <param name="AttackMilli">Ataque considerado, em milesimos de ponto: principal + parte do secundario.</param>
/// <param name="CritFactorPermille">Fator medio do critico: 1000 + chance x (multiplicador - 1000) / 1000.</param>
/// <param name="OffensePermille">Ataque com critico dividido pela referencia (1000 = referencia).</param>
/// <param name="DefenseMixMilli">Defesa media, em milesimos de ponto (fisica e magica pelos pesos).</param>
/// <param name="Survivability">Vida efetiva: vida x (escala + defesa media) / escala.</param>
/// <param name="SurvivabilityPermille">Vida efetiva dividida pela referencia (1000 = referencia).</param>
public readonly record struct PowerBreakdown(int Power, long AttackMilli, int CritFactorPermille, int OffensePermille,
    long DefenseMixMilli, long Survivability, int SurvivabilityPermille);

/// <summary>
/// Indice de poder estimado (Etapa 1, D-061). Comparacao na interface e estimativa rapida de ameaca e
/// dificuldade; nao substitui as formulas do combate. Calculado das estatisticas EFETIVAS (hoje classe e
/// nivel; depois equipamento e outros modificadores, sem mudar a formula).
/// <code>
/// Ataque        = max(AF, AM) + min(AF, AM) x secundario       (o secundario conta pouco: 250 = 25%)
/// Critico       = 1 + chance x (multiplicador - 1)             (dano medio por golpe)
/// Ofensiva      = Ataque x Critico / ataque de referencia
/// Defesa media  = DF x peso fisico + DM x (1 - peso fisico)
/// Sobrevivencia = Vida x (escala + Defesa media) / escala / sobrevivencia de referencia
/// Poder         = Escala x raiz(Ofensiva x Sobrevivencia)
/// </code>
/// A media geometrica segue a logica de um duelo (dano por tempo x tempo ate cair) e cresce perto do
/// linear com o nivel. Nominal: nao olha a vida ou o recurso atuais (a condicao fica a parte).
/// </summary>
public static class PowerIndex
{
    public static PowerBreakdown Compute(CombatRulesDefinition r, CombatStats s)
    {
        var pi = r.PowerIndex;
        long hi = Math.Max(s.PhysicalAttack, s.MagicAttack), lo = Math.Min(s.PhysicalAttack, s.MagicAttack);
        long attackMilli = hi * 1000 + lo * pi.SecondaryAttackPermille;
        long chance = Math.Clamp(s.CritChancePermille, 0, r.CritChanceMaxPermille);
        long mult = Math.Clamp(s.CritMultiplierPermille, r.CritMultiplierMinPermille, r.CritMultiplierMaxPermille);
        long crit = 1000 + chance * (mult - 1000) / 1000;
        long offenseMilli = attackMilli * crit / 1000;
        long share = Math.Clamp(pi.PhysicalDefenseSharePermille, 0, 1000);
        long defMilli = (s.PhysicalDefense * share + s.MagicDefense * (1000 - share));
        long k = Math.Max(1, r.DefenseScale);
        long ehp = s.Health * (k * 1000 + defMilli) / (k * 1000);
        long refA = Math.Max(1, pi.ReferenceAttack), refS = Math.Max(1, pi.ReferenceSurvivability);

        // Poder = Escala x raiz(ofensiva/refA x ehp/refS), arredondado, sem ponto flutuante.
        UInt128 scale2 = (UInt128)(ulong)Math.Max(1, pi.Scale) * (ulong)Math.Max(1, pi.Scale);
        UInt128 num = scale2 * (ulong)offenseMilli * (ulong)ehp;
        UInt128 den = (UInt128)(ulong)(refA * 1000) * (ulong)refS;
        long power = (long)RoundSqrt(num / den);
        return new PowerBreakdown((int)Math.Min(power, int.MaxValue), attackMilli, (int)crit,
            (int)Math.Min(offenseMilli * 1000 / (refA * 1000), int.MaxValue), defMilli, ehp,
            (int)Math.Min(ehp * 1000 / refS, int.MaxValue));
    }

    public static PowerBreakdown Compute(ContentDatabase c, StatSheet sheet) => Compute(c.Combat, sheet.Stats);

    /// <summary>
    /// Estimativa no momento, separada do nominal: a vida atual entra na sobrevivencia, entao o poder cai
    /// com a raiz da fracao de vida (metade da vida = cerca de 71% do poder). O recurso nao entra (furia
    /// comeca vazia por regra, nao por fraqueza). Para estimar ameaca depois do combate; nao muda o nominal.
    /// </summary>
    public static int AtHealth(int nominal, int currentHealth, int maxHealth)
    {
        if (maxHealth <= 0 || nominal <= 0) return 0;
        long hp = Math.Clamp(currentHealth, 0, maxHealth);
        return (int)RoundSqrt((UInt128)(ulong)nominal * (ulong)nominal * (ulong)hp / (ulong)maxHealth);
    }

    /// <summary>Raiz quadrada inteira arredondada ao mais proximo.</summary>
    internal static UInt128 RoundSqrt(UInt128 n)
    {
        if (n == 0) return 0;
        // Newton a partir de uma estimativa acima da raiz.
        UInt128 x = n, y = (x + 1) / 2;
        while (y < x) { x = y; y = (x + n / x) / 2; }
        // x = piso da raiz; arredonda: (x + 0,5)^2 = x^2 + x + 0,25.
        return n - x * x > x ? x + 1 : x;
    }
}

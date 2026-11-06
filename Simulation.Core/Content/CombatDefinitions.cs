namespace Simulation.Core.Content;

/// <summary>
/// Regras gerais de combate (combat.json, Etapa 1): limites das estatisticas e o indice de poder
/// estimado. A resolucao do combate (M3) usara as mesmas constantes (ex.: escala da defesa).
/// </summary>
public sealed class CombatRulesDefinition
{
    /// <summary>Escala da defesa: mitigacao = defesa / (defesa + isto). 100 de defesa = metade do dano.</summary>
    public int DefenseScale { get; set; } = 100;
    /// <summary>Teto da chance de critico efetiva (milesimos).</summary>
    public int CritChanceMaxPermille { get; set; } = 750;
    /// <summary>Limites do multiplicador de critico (milesimos do dano normal).</summary>
    public int CritMultiplierMinPermille { get; set; } = 1000;
    public int CritMultiplierMaxPermille { get; set; } = 5000;
    public PowerIndexDefinition PowerIndex { get; set; } = new();
}

/// <summary>
/// Indice de poder estimado (Etapa 1): ferramenta de comparacao e estimativa, nao substitui o combate.
/// Poder = Escala x raiz(Ataque normalizado x Sobrevivencia normalizada):
/// - Ataque = ataque principal + parte do secundario, vezes o fator medio do critico;
/// - Sobrevivencia = vida x (escala da defesa + defesa media) / escala da defesa (vida efetiva);
/// - normalizados pelos valores de referencia (o personagem de referencia vale Escala).
/// O produto ataque x sobrevivencia e o que decide um duelo (quem derruba o outro antes).
/// </summary>
public sealed class PowerIndexDefinition
{
    /// <summary>Quanto do ataque secundario conta (permille): ninguem usa ataque fisico e magico inteiros ao mesmo tempo.</summary>
    public int SecondaryAttackPermille { get; set; } = 250;
    /// <summary>Peso da defesa fisica na defesa media (permille; o resto e a magica).</summary>
    public int PhysicalDefenseSharePermille { get; set; } = 500;
    /// <summary>Valores de referencia: o personagem com estes valores tem poder igual a Scale.</summary>
    public int ReferenceAttack { get; set; } = 22;
    public int ReferenceSurvivability { get; set; } = 200;
    public int Scale { get; set; } = 100;
}

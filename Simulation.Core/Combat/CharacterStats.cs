using Simulation.Core.Content;
using Simulation.Core.Population;

namespace Simulation.Core.Combat;

/// <summary>Uma estatistica com a origem do valor (ficha e tooltip do calculo).</summary>
/// <param name="Key">Chave estavel (CombatStats.Keys).</param>
/// <param name="Base">Base da classe.</param>
/// <param name="FromLevels">Crescimento por nivel x (nivel - 1).</param>
/// <param name="Total">Valor efetivo, ja limitado.</param>
/// <param name="Limited">O valor bruto passou de um limite (teto do critico, minimo da velocidade) e foi cortado.</param>
public readonly record struct StatLine(string Key, int Base, int FromLevels, int Total, bool Limited);

/// <summary>Estatisticas efetivas de um personagem num nivel, com a decomposicao de cada uma.</summary>
public sealed class StatSheet
{
    internal StatSheet(string classId, int level, CombatStats stats, IReadOnlyList<StatLine> lines)
    {
        ClassId = classId;
        Level = level;
        Stats = stats;
        Lines = lines;
    }

    public string ClassId { get; }
    /// <summary>Nivel usado no calculo (limitado a 1..nivel maximo).</summary>
    public int Level { get; }
    public CombatStats Stats { get; }
    public IReadOnlyList<StatLine> Lines { get; }
}

/// <summary>
/// Estatisticas do personagem (Etapa 1, D-061): base da classe + crescimento por nivel x (nivel - 1),
/// so com inteiros. O nivel e limitado ao maximo da progressao; a chance e o multiplicador de critico,
/// aos limites de combat.json; a velocidade, a pelo menos 1. Equipamento e efeitos temporarios entram
/// depois, como novas parcelas da decomposicao. As capacidades da pessoa nunca entram aqui.
/// </summary>
public static class CharacterStats
{
    public static int ClampLevel(ContentDatabase c, int level) => Math.Clamp(level, 1, Math.Max(1, c.Progression.LevelCap));

    /// <summary>Uma estatistica efetiva (sem montar a ficha inteira; usado no deslocamento).</summary>
    public static int Stat(ContentDatabase c, ClassDefinition cls, int level, string key) => Line(c, cls, ClampLevel(c, level), key).Total;

    public static StatSheet Sheet(ContentDatabase c, ClassDefinition cls, int level)
    {
        int lv = ClampLevel(c, level);
        var stats = new CombatStats();
        var lines = new List<StatLine>(CombatStats.Keys.Length);
        foreach (var key in CombatStats.Keys)
        {
            var line = Line(c, cls, lv, key);
            stats.Set(key, line.Total);
            lines.Add(line);
        }
        return new StatSheet(cls.Id, lv, stats, lines);
    }

    public static StatSheet Sheet(ContentDatabase c, Character ch) => Sheet(c, c.Classes[ch.ClassId], ch.Level);

    /// <summary>Velocidade de movimento do personagem (unidades de mapa por minuto).</summary>
    public static int MoveSpeed(ContentDatabase c, Character ch) =>
        c.Classes.TryGetValue(ch.ClassId, out var cls) ? Stat(c, cls, ch.Level, "moveSpeed") : ReferenceMoveSpeed(c);

    /// <summary>
    /// Velocidade de referencia para estimativas sem personagem (tempo de caminhada entre zonas no mapa):
    /// a menor velocidade base entre as classes, ou seja, o tempo de quem anda mais devagar.
    /// </summary>
    public static int ReferenceMoveSpeed(ContentDatabase c)
    {
        int best = int.MaxValue;
        foreach (var cls in c.Classes.Values) best = Math.Min(best, Math.Max(1, cls.BaseStats.MoveSpeed));
        return best == int.MaxValue ? 15 : best;
    }

    /// <summary>Maximo de vida e de recurso no nivel atual.</summary>
    public static (int Health, int Resource) Maximums(ContentDatabase c, Character ch)
    {
        var cls = c.Classes[ch.ClassId];
        return (Stat(c, cls, ch.Level, "health"), Stat(c, cls, ch.Level, "resource"));
    }

    /// <summary>
    /// Condicao fora de combate: vida cheia; recurso cheio (mana, energia) ou vazio (furia). Na criacao e
    /// a cada nivel. O combate (Etapa 2) passa a gastar e recuperar.
    /// </summary>
    public static void RestoreOutOfCombat(ContentDatabase c, Character ch)
    {
        var cls = c.Classes[ch.ClassId];
        var (hp, res) = Maximums(c, ch);
        ch.CurrentHealth = hp;
        ch.CurrentResource = ResourceTypes.StartsFull(cls.Resource.Type) ? res : 0;
    }

    private static StatLine Line(ContentDatabase c, ClassDefinition cls, int level, string key)
    {
        int b = cls.BaseStats.Get(key);
        long grow = (long)cls.PerLevel.Get(key) * (level - 1);
        long raw = b + grow;
        var r = c.Combat;
        long total = key switch
        {
            "critChancePermille" => Math.Clamp(raw, 0, r.CritChanceMaxPermille),
            "critMultiplierPermille" => Math.Clamp(raw, r.CritMultiplierMinPermille, r.CritMultiplierMaxPermille),
            "moveSpeed" => Math.Max(1, raw),
            _ => Math.Max(0, raw),
        };
        total = Math.Min(total, int.MaxValue);
        return new StatLine(key, b, (int)Math.Min(grow, int.MaxValue), (int)total, total != raw);
    }
}

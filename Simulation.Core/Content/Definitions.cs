namespace Simulation.Core.Content;

// Definicoes de conteudo: dados puros carregados do JSON (fonte autoral canonica).
// Nenhuma logica de jogo aqui; novos comportamentos viram primitivas testadas no Core.
// Unidades: distancias em metros (inteiros), tempos em ms (convertidos para ticks),
// moeda em cobre (inteiro), parametros comportamentais em permille (0..1000).

public interface IContentDefinition
{
    string Id { get; }
    int Version { get; }
}

public sealed class WorldDefinition
{
    public string Id { get; set; } = "";
    public int Version { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Data da campanha no tick 0, formato yyyy-MM-dd.</summary>
    public string StartDate { get; set; } = "";
    public string StartingRegionId { get; set; } = "";
    public List<HolidayDefinition> Holidays { get; set; } = new();
}

public sealed class HolidayDefinition
{
    /// <summary>"MM-dd" (recorrente) ou "yyyy-MM-dd" (data unica).</summary>
    public string Date { get; set; } = "";
    public string Name { get; set; } = "";
}

public static class RegionKinds
{
    public const string City = "city";
    public const string Area = "area";
    public static readonly string[] All = { City, Area };
}

public static class PvpRules
{
    public const string None = "none";
    /// <summary>PvP permitido apenas entre quem marcou participacao.</summary>
    public const string Consensual = "consensual";
    /// <summary>PvP aberto na area.</summary>
    public const string Open = "open";
    public static readonly string[] All = { None, Consensual, Open };
}

public sealed class RegionDefinition : IContentDefinition
{
    public string Id { get; set; } = "";
    public int Version { get; set; }
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Biome { get; set; } = "";
    public int LevelMin { get; set; }
    public int LevelMax { get; set; }
    public string Pvp { get; set; } = PvpRules.None;
    /// <summary>Capacidade nominal de personagens simultaneos (modelo do MMORPG).</summary>
    public int Capacity { get; set; }
    /// <summary>Posicao no mapa estrategico 2D (unidades de mapa).</summary>
    public MapPoint MapPosition { get; set; } = new();
    public List<RegionConnection> Connections { get; set; } = new();
    /// <summary>Itens coletaveis na regiao (ex.: minerio).</summary>
    public List<string> ResourceItemIds { get; set; } = new();
    /// <summary>
    /// Atividades que o local oferece ("objetos inteligentes", plano 4.4.1). A decisao por
    /// utilidade so pontua atividades de locais que a pessoa conhece. Conteudo novo se
    /// integra sozinho: publicar uma area publica suas atividades.
    /// </summary>
    public List<ActivityDefinition> Activities { get; set; } = new();
}

public static class ActivityTypes
{
    public const string Hunt = "hunt";
    public const string Gather = "gather";
    public const string Boss = "boss";
    public const string Quest = "quest";
    public const string Explore = "explore";
    public const string Trade = "trade";
    public const string Repair = "repair";
    public const string Train = "train";
    public const string Socialize = "socialize";
    public const string Recruit = "recruit";
    public const string Patrol = "patrol";
    public const string Ambush = "ambush";

    public static readonly string[] All =
        { Hunt, Gather, Boss, Quest, Explore, Trade, Repair, Train, Socialize, Recruit, Patrol, Ambush };

    /// <summary>Atividades de servico que exigem zona segura (cidade).</summary>
    public static readonly string[] CityOnly = { Trade, Repair, Train, Recruit };

    /// <summary>Atividades de combate contra criaturas ou jogadores: proibidas na cidade.</summary>
    public static readonly string[] NotInCity = { Hunt, Boss, Patrol, Ambush };
}

/// <summary>
/// Atividade oferecida por um local: requisitos e o que rende. Os valores efetivos
/// (recompensa, risco, tempo, beneficio social) sao calculados no M2 com o estado do mundo:
/// lotacao, PKs e mortes recentes, viagem e amigos presentes.
/// </summary>
public sealed class ActivityDefinition
{
    /// <summary>ID unico dentro do local.</summary>
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string Name { get; set; } = "";
    public int MinLevel { get; set; } = 1;
    public int MinGroupSize { get; set; } = 1;
    public int MaxGroupSize { get; set; } = 1;
    /// <summary>Vazio = qualquer classe.</summary>
    public List<string> ClassIds { get; set; } = new();
    public List<string> RequiredItemIds { get; set; } = new();
    /// <summary>Criaturas envolvidas (hunt/boss).</summary>
    public List<string> CreatureIds { get; set; } = new();
    /// <summary>Itens que a atividade rende diretamente (gather) ou consome (servicos).</summary>
    public List<string> ItemIds { get; set; } = new();
    /// <summary>Duracao tipica de um ciclo da atividade, em minutos simulados.</summary>
    public int TypicalMinutes { get; set; }
    /// <summary>Risco base declarado pelo autor (permille); o risco percebido varia com o mundo.</summary>
    public int BaseRiskPermille { get; set; }
}

public sealed class MapPoint
{
    public int X { get; set; }
    public int Y { get; set; }
}

public sealed class RegionConnection
{
    public string To { get; set; } = "";
    public int TravelMinutes { get; set; }
}

public static class ClassRoles
{
    public const string Tank = "tank";
    public const string Damage = "damage";
    public const string Support = "support";
    public static readonly string[] All = { Tank, Damage, Support };
}

public sealed class ClassDefinition : IContentDefinition
{
    public string Id { get; set; } = "";
    public int Version { get; set; }
    public string Name { get; set; } = "";
    public string Role { get; set; } = "";
    /// <summary>Nome do recurso de habilidades (ex.: mana, furia).</summary>
    public string ResourceName { get; set; } = "";
    public CombatStats BaseStats { get; set; } = new();
    public CombatStats PerLevel { get; set; } = new();
    public List<string> AbilityIds { get; set; } = new();
}

public sealed class CombatStats
{
    public int Health { get; set; }
    public int Resource { get; set; }
    public int Armor { get; set; }
    public int Power { get; set; }
    public int Intelligence { get; set; }
}

public sealed class AbilityDefinition : IContentDefinition
{
    public string Id { get; set; } = "";
    public int Version { get; set; }
    public string Name { get; set; } = "";
    public string ClassId { get; set; } = "";
    public int ResourceCost { get; set; }
    public int RangeMeters { get; set; }
    public int CastTimeMs { get; set; }
    public int CooldownMs { get; set; }
    public List<EffectDefinition> Effects { get; set; } = new();
    /// <summary>Referencia a recurso visual do cliente; nao controla a resolucao.</summary>
    public string VisualId { get; set; } = "";
}

public static class EffectTypes
{
    public const string Damage = "damage";
    public const string Heal = "heal";
    public const string DamageOverTime = "damageOverTime";
    public const string HealOverTime = "healOverTime";
    public const string Stun = "stun";
    public const string Silence = "silence";
    public const string Root = "root";
    public const string Buff = "buff";
    public const string Debuff = "debuff";
    public const string Threat = "threat";
    public const string Shield = "shield";

    public static readonly string[] All =
        { Damage, Heal, DamageOverTime, HealOverTime, Stun, Silence, Root, Buff, Debuff, Threat, Shield };
}

/// <summary>
/// Efeito combinavel. Campos nao usados pelo tipo ficam zerados; o validador
/// exige os campos obrigatorios de cada tipo. Area: Radius/MaxTargets opcionais.
/// </summary>
public sealed class EffectDefinition
{
    public string Type { get; set; } = "";
    public int Value { get; set; }
    public string Element { get; set; } = "";
    /// <summary>Escala pelo atributo (Power ou Intelligence) em permille.</summary>
    public string ScaleAttribute { get; set; } = "";
    public int ScalePermille { get; set; }
    public int ValuePerPulse { get; set; }
    public int IntervalMs { get; set; }
    public int DurationMs { get; set; }
    /// <summary>Atributo afetado por buff/debuff.</summary>
    public string Attribute { get; set; } = "";
    /// <summary>Magnitude de buff/debuff em permille (ex.: 150 = +15%).</summary>
    public int MagnitudePermille { get; set; }
    public int MaxStacks { get; set; }
    public int RadiusMeters { get; set; }
    public int MaxTargets { get; set; }
}

public static class ItemCategories
{
    public const string Weapon = "weapon";
    public const string Armor = "armor";
    public const string Material = "material";
    public const string Consumable = "consumable";
    public static readonly string[] All = { Weapon, Armor, Material, Consumable };
}

public static class Rarities
{
    public static readonly string[] All = { "common", "uncommon", "rare", "epic" };
}

public sealed class ItemDefinition : IContentDefinition
{
    public string Id { get; set; } = "";
    public int Version { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Rarity { get; set; } = "common";
    public int LevelRequirement { get; set; }
    /// <summary>1 = item individual (rastreado por instancia); maior = empilhavel em lotes.</summary>
    public int MaxStack { get; set; } = 1;
    /// <summary>Valor de referencia para NPC, em cobre. Nao e preco de mercado.</summary>
    public int BaseValueCopper { get; set; }
    public CombatStats Stats { get; set; } = new();
    /// <summary>Restricao opcional de classe (vazio = qualquer classe).</summary>
    public List<string> ClassIds { get; set; } = new();
}

public static class CreatureKinds
{
    public const string Melee = "melee";
    public const string Ranged = "ranged";
    public const string Support = "support";
    public const string Boss = "boss";
    public static readonly string[] All = { Melee, Ranged, Support, Boss };
}

public sealed class CreatureDefinition : IContentDefinition
{
    public string Id { get; set; } = "";
    public int Version { get; set; }
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public int Level { get; set; }
    public int Health { get; set; }
    public int Damage { get; set; }
    public int Armor { get; set; }
    public int AttackIntervalMs { get; set; }
    public int RangeMeters { get; set; }
    public int RespawnSeconds { get; set; }
    /// <summary>Tamanho de grupo recomendado (1 = solo).</summary>
    public int RecommendedGroupSize { get; set; } = 1;
    public int Experience { get; set; }
    public int GoldMinCopper { get; set; }
    public int GoldMaxCopper { get; set; }
    public List<string> RegionIds { get; set; } = new();
    public List<LootEntry> Loot { get; set; } = new();
}

public sealed class LootEntry
{
    public string ItemId { get; set; } = "";
    public int ChancePermille { get; set; }
    public int Min { get; set; } = 1;
    public int Max { get; set; } = 1;
}

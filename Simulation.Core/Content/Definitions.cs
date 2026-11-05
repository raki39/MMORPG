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
    /// <summary>Raio padrao de todas as zonas (unidades de mapa; 1 unidade = 10 m). Escala unica (D-016).</summary>
    public int ZoneRadius { get; set; }
    /// <summary>Localidade do servidor (P1): fuso e calendario do relogio global e dos relatorios diarios.</summary>
    public string ServerLocalityId { get; set; } = "";
    /// <summary>Grade de lotes do mapa: cada zona ocupa um lote; lotes vazios ficam para expansao.</summary>
    public MapGridDefinition Map { get; set; } = new();
}

/// <summary>
/// Grade hexagonal de lotes (D-028). Linhas impares deslocadas meio passo. Cada lote tem o
/// tamanho de uma zona; o jogador (desenvolvedor do MMORPG) podera ocupar lotes livres no
/// editor de mapa futuro.
/// </summary>
public sealed class MapGridDefinition
{
    public int OriginX { get; set; }
    public int OriginY { get; set; }
    /// <summary>Distancia entre centros na mesma linha (unidades de mapa).</summary>
    public int SpacingX { get; set; }
    /// <summary>Distancia entre linhas (unidades de mapa).</summary>
    public int SpacingY { get; set; }
    public int Columns { get; set; }
    public int Rows { get; set; }
    /// <summary>
    /// Forma da terra: uma linha de texto por linha da grade, um caractere por coluna
    /// ('#' = terra, '.' = mar). Vazio = grade inteira e terra. Zonas so em terra.
    /// </summary>
    public List<string> Mask { get; set; } = new();
}

/// <summary>Biomas conhecidos (cor e textura do terreno).</summary>
public static class Biomes
{
    public static readonly string[] All = { "planicie", "floresta", "caverna", "ruinas", "pantano", "neve", "vulcanico", "sombrio" };
}

/// <summary>Formas de relevo do terreno da zona (apresentacao; nao afeta a simulacao).</summary>
public static class Reliefs
{
    public static readonly string[] All = { "plains", "hills", "mountains", "valley", "plateau", "marsh", "badlands" };
}

/// <summary>
/// Aparencia do terreno da zona: forma do relevo, altitude, aspereza e semente (variacao
/// estavel). So apresentacao; editavel no futuro editor de zonas.
/// </summary>
public sealed class TerrainDefinition
{
    public string Relief { get; set; } = "hills";
    public int ElevationPermille { get; set; } = 500;
    public int RoughnessPermille { get; set; } = 500;
    public int Seed { get; set; }
    /// <summary>
    /// Rio que atravessa a zona (simulacao respeita: estradas so cruzam por ponte). Nulo = vale
    /// ganha um rio automatico pela semente; outros relevos ficam sem rio.
    /// </summary>
    public RiverDefinition? River { get; set; }
    /// <summary>Cobertura de pocas ou lagos (permille da zona, aproximado). Nulo = brejo 380, demais 0.</summary>
    public int? PoolsPermille { get; set; }
}

/// <summary>Rio configuravel da zona (editor de tiles futuro).</summary>
public sealed class RiverDefinition
{
    public bool Enabled { get; set; } = true;
    /// <summary>Direcao do curso: 0..7 em passos de 22,5 graus (0 = leste-oeste, 4 = norte-sul).</summary>
    public int Direction { get; set; }
    /// <summary>Distancia lateral do curso ate o centro da zona (unidades; sinal = lado).</summary>
    public int Offset { get; set; }
    /// <summary>Meia largura da agua (unidades).</summary>
    public int Width { get; set; } = 6;
    /// <summary>Quanto o rio serpenteia (permille do raio da zona).</summary>
    public int MeanderPermille { get; set; } = 220;
}

/// <summary>
/// Localidade de residencia (P1, D-042): fuso, idiomas, feriados e participacao na populacao.
/// Fuso em minutos do UTC, inteiro e fixo (sem horario de verao por enquanto).
/// </summary>
public sealed class LocalityDefinition : IContentDefinition
{
    public string Id { get; set; } = "";
    public int Version { get; set; }
    public string Name { get; set; } = "";
    public int UtcOffsetMinutes { get; set; }
    /// <summary>Peso relativo de residentes na populacao gerada.</summary>
    public int Weight { get; set; }
    /// <summary>Residentes com outra nacionalidade (permille).</summary>
    public int ForeignResidentPermille { get; set; }
    /// <summary>Idiomas falados pelos residentes; o primeiro e o idioma nativo de quem tem esta nacionalidade.</summary>
    public List<LanguageShareDefinition> Languages { get; set; } = new();
    public List<HolidayDefinition> Holidays { get; set; } = new();
    /// <summary>Renda local em relacao a do Brasil, em reais equivalentes (permille), P2.</summary>
    public int IncomeFactorPermille { get; set; } = 1000;
    /// <summary>Nomes de quem tem esta nacionalidade (vazio = nomes gerais de population.json).</summary>
    public LocalNamesDefinition Names { get; set; } = new();
}

/// <summary>Nomes por nacionalidade (P1). Para a China, primeiro o sobrenome (ordem local).</summary>
public sealed class LocalNamesDefinition
{
    public List<string> FirstNames { get; set; } = new();
    public List<string> LastNames { get; set; } = new();
}

public sealed class LanguageShareDefinition
{
    public string Id { get; set; } = "";
    /// <summary>Residentes que falam o idioma (permille).</summary>
    public int Permille { get; set; }
}

/// <summary>
/// Ocupacao (P2, D-043): obrigacoes semanais (trabalho, estudo, cuidados), deslocamento, renda
/// e apoio familiar. A pessoa recebe uma ocupacao pelo peso; o horario varia por pessoa.
/// </summary>
public sealed class OccupationDefinition : IContentDefinition
{
    public string Id { get; set; } = "";
    public int Version { get; set; }
    public string Name { get; set; } = "";
    /// <summary>work, study, work_study, care ou none.</summary>
    public string Kind { get; set; } = "none";
    public int Weight { get; set; }
    public string Description { get; set; } = "";
    public List<ObligationBlockDefinition> Blocks { get; set; } = new();
    /// <summary>Variacao individual fixa do horario (minutos, +-): cada pessoa entra um pouco antes ou depois.</summary>
    public int StartVariationMinutes { get; set; }
    /// <summary>Trabalha mesmo em feriados (escala, turnos, autonomos).</summary>
    public bool WorksHolidays { get; set; }
    /// <summary>
    /// Flexibilidade para jogar no expediente remoto (permille; 0 = nunca). So vale em blocos
    /// remotos; nao transforma o trabalho em tempo livre (D-044).
    /// </summary>
    public int PlayDuringWorkFlexibility { get; set; }
    /// <summary>Renda do trabalho em centavos de real por mes (sem efeito ate o M7).</summary>
    public TraitRange Income { get; set; } = new();
    public int SupportChancePermille { get; set; }
    /// <summary>Apoio familiar ou outras fontes regulares (centavos/mes).</summary>
    public TraitRange Support { get; set; } = new();
}

/// <summary>Bloco de obrigacao: dias (mon..sun), "HH:mm" de inicio e fim (fim menor = vira a noite).</summary>
public sealed class ObligationBlockDefinition
{
    public List<string> Days { get; set; } = new();
    public string Start { get; set; } = "";
    public string End { get; set; } = "";
    /// <summary>Deslocamento antes e depois (minutos; varia por pessoa).</summary>
    public int CommuteMinutes { get; set; }
    /// <summary>trabalho, estudo, cuidados (texto da ficha).</summary>
    public string Label { get; set; } = "";
    /// <summary>A pessoa tem uma folga fixa num dos dias deste bloco (escala).</summary>
    public bool PersonalDayOff { get; set; }
    /// <summary>Trabalho remoto (em casa, com o computador): pausas e, com flexibilidade, alguma escapada.</summary>
    public bool Remote { get; set; }
    /// <summary>Pausas permitidas dentro do bloco ("HH:mm"), so em blocos remotos.</summary>
    public List<BreakDefinition> Breaks { get; set; } = new();
    /// <summary>
    /// Trajeto (R1): "home" (padrao) sai de casa antes e volta para casa depois (commuteMinutes
    /// cada trecho); "fromPrevious" vem direto do bloco anterior do mesmo dia: refeicao
    /// (mealMinutes) e transferencia (transferMinutes) logo depois dele, o resto e espera fora de
    /// casa. A volta para casa depois deste bloco usa commuteMinutes. Outros trajetos depois.
    /// </summary>
    public string Route { get; set; } = "home";
    public int MealMinutes { get; set; }
    public int TransferMinutes { get; set; }
    public bool IsFromPrevious() => Route == "fromPrevious";
}

public sealed class BreakDefinition
{
    public string Start { get; set; } = "";
    public string End { get; set; } = "";
}

public sealed class HolidayDefinition
{
    /// <summary>"MM-dd" (recorrente) ou "yyyy-MM-dd" (data unica).</summary>
    public string Date { get; set; } = "";
    public string Name { get; set; } = "";
}

/// <summary>
/// Funcao principal da zona (D-015). Separada das regras: uma zona de caca pode ter PvP e
/// ser disputada. Cidades sao sempre seguras. Todas as zonas tem a mesma escala e estradas.
/// </summary>
public static class ZoneFunctions
{
    public const string City = "city";
    public const string Hunting = "hunting";
    public static readonly string[] All = { City, Hunting };
}

/// <summary>Regra de combate entre jogadores na zona.</summary>
public static class PvpRules
{
    public const string None = "none";
    /// <summary>PvP permitido apenas entre quem marcou participacao.</summary>
    public const string Consensual = "consensual";
    /// <summary>PvP aberto na zona.</summary>
    public const string Open = "open";
    public static readonly string[] All = { None, Consensual, Open };
}

/// <summary>O que se perde ao morrer na zona (detalhes de full loot no M9).</summary>
public static class DeathRules
{
    public const string None = "none";
    public const string Partial = "partial";
    public const string FullLoot = "full_loot";
    public static readonly string[] All = { None, Partial, FullLoot };
}

/// <summary>Controle territorial da zona (disputas de guilda no M9).</summary>
public static class ControlRules
{
    public const string Neutral = "neutral";
    public const string Contestable = "contestable";
    public static readonly string[] All = { Neutral, Contestable };
}

public sealed class RegionDefinition : IContentDefinition
{
    public string Id { get; set; } = "";
    public int Version { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Funcao principal: city ou hunting.</summary>
    public string Function { get; set; } = "";
    public string Biome { get; set; } = "";
    /// <summary>Aparencia do terreno (relevo, altitude, aspereza, semente).</summary>
    public TerrainDefinition Terrain { get; set; } = new();
    public int LevelMin { get; set; }
    public int LevelMax { get; set; }
    /// <summary>Regra de combate: none, consensual ou open.</summary>
    public string Pvp { get; set; } = PvpRules.None;
    /// <summary>Regra de morte: none, partial ou full_loot.</summary>
    public string DeathRule { get; set; } = DeathRules.None;
    /// <summary>Controle territorial: neutral ou contestable.</summary>
    public string Control { get; set; } = ControlRules.Neutral;

    public bool IsCity => Function == ZoneFunctions.City;
    /// <summary>
    /// Nivel da cidade (1..5; 0 fora de cidades): define o tamanho da area urbana (muralha) e
    /// o detalhe do desenho. Evoluir a cidade aumenta o nivel (futuro).
    /// </summary>
    public int CityLevel { get; set; }
    /// <summary>
    /// Posicao da cidade dentro da zona (deslocamento a partir do centro; nulo = centro). A
    /// muralha, os predios e as estradas se adaptam (editor de tiles futuro).
    /// </summary>
    public MapPoint? CityPosition { get; set; }
    /// <summary>Capacidade nominal de personagens simultaneos (modelo do MMORPG).</summary>
    public int Capacity { get; set; }
    /// <summary>Centro da zona no mapa (unidades de mapa; 1 unidade = 10 m).</summary>
    public MapPoint MapPosition { get; set; } = new();
    public List<RegionConnection> Connections { get; set; } = new();
    /// <summary>Itens coletaveis na regiao (ex.: minerio).</summary>
    public List<string> ResourceItemIds { get; set; } = new();
    /// <summary>
    /// Atividades da zona inteira, sem lugar especifico (ex.: explorar). As demais ficam
    /// nas areas. A decisao por utilidade so pontua atividades de zonas conhecidas.
    /// </summary>
    public List<ActivityDefinition> Activities { get; set; } = new();

    /// <summary>Lugares dentro da zona (clareira, praca, veio, entrada de masmorra, hideout...).</summary>
    public List<AreaDefinition> Areas { get; set; } = new();

    /// <summary>Todas as atividades da zona: as da zona inteira e as de cada area.</summary>
    public IEnumerable<ActivityDefinition> AllActivities()
    {
        foreach (var a in Activities) yield return a;
        foreach (var area in Areas)
            foreach (var a in area.Activities) yield return a;
    }

    public ActivityDefinition? FindActivity(string activityId) => AllActivities().FirstOrDefault(a => a.Id == activityId);

    /// <summary>Area que oferece a atividade (nulo = atividade da zona inteira).</summary>
    public AreaDefinition? AreaOfActivity(string activityId) =>
        Areas.FirstOrDefault(ar => ar.Activities.Any(a => a.Id == activityId));
}

/// <summary>Tipo de area: define o que o lugar e (o icone no mapa vem daqui, nao das tarefas).</summary>
public static class AreaKinds
{
    // Cidade
    public const string Shop = "shop";
    public const string Forge = "forge";
    public const string Tavern = "tavern";
    public const string TownSquare = "town_square";
    public const string TrainingGround = "training_ground";
    // Natureza
    public const string HuntingGround = "hunting_ground";
    public const string OreVein = "ore_vein";
    public const string HerbPatch = "herb_patch";
    public const string CrystalNode = "crystal_node";
    public const string Landmark = "landmark";
    // Especiais
    /// <summary>Entrada de masmorra: leva a uma instancia separada (instancias em marco futuro).</summary>
    public const string Dungeon = "dungeon";
    public const string BossLair = "boss_lair";
    /// <summary>Passagem estreita, ponto natural de emboscada.</summary>
    public const string Chokepoint = "chokepoint";
    /// <summary>Esconderijo de guilda: leva a um hideout seguro (territorios no M9).</summary>
    public const string Hideout = "hideout";

    public static readonly string[] CityOnly = { Shop, Forge, Tavern, TownSquare, TrainingGround };
    public static readonly string[] All =
        { Shop, Forge, Tavern, TownSquare, TrainingGround, HuntingGround, OreVein, HerbPatch, CrystalNode, Landmark, Dungeon, BossLair, Chokepoint, Hideout };

    /// <summary>Areas que sao portais para outro espaco.</summary>
    public static bool IsPortal(string kind) => kind == Dungeon || kind == Hideout;

    /// <summary>Tipos de area permitidos pela funcao da zona (regra de construcao).</summary>
    public static string[] AllowedIn(string zoneFunction) => zoneFunction switch
    {
        ZoneFunctions.City => new[] { Shop, Forge, Tavern, TownSquare, TrainingGround, Landmark, Dungeon },
        _ => new[] { HuntingGround, OreVein, HerbPatch, CrystalNode, Landmark, Dungeon, BossLair, Chokepoint, Hideout },
    };

    /// <summary>Faixa de raio permitida por tipo (unidades de mapa). Tamanho nao cria monstros nem recursos.</summary>
    public static (int Min, int Max) RadiusRange(string kind) => kind switch
    {
        Shop or Tavern or TrainingGround => (10, 20),
        Forge => (8, 16),
        TownSquare => (12, 24),
        HuntingGround => (16, 40),
        OreVein => (10, 24),
        HerbPatch => (10, 26),
        CrystalNode => (8, 20),
        Landmark => (8, 16),
        Dungeon or Hideout => (10, 18),
        BossLair => (16, 32),
        Chokepoint => (12, 24),
        _ => (8, 60),
    };
}

/// <summary>
/// Area: lugar com posicao e tamanho dentro da zona. Oferece atividades; para fazer uma
/// delas o personagem precisa ir ate a area.
/// </summary>
public sealed class AreaDefinition
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Raio da area (unidades de mapa).</summary>
    public int Radius { get; set; } = 18;
    /// <summary>Posicao relativa ao centro da zona (opcional; senao automatica).</summary>
    public MapPoint? Position { get; set; }
    /// <summary>Para masmorras e hideouts: nome do espaco ao qual a area leva (instancias futuras).</summary>
    public string LeadsTo { get; set; } = "";
    /// <summary>Faixa de nivel recomendada da area (orientacao; 0 = sem faixa, ex.: cidade).</summary>
    public int LevelMin { get; set; }
    public int LevelMax { get; set; }
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
    /// <summary>
    /// Nivel do desafio (D-027): define o rendimento de XP. 0 = caca e chefe usam a media das
    /// criaturas; outros tipos sem desafio nao dao XP. Cidades nao declaram desafio.
    /// </summary>
    public int ChallengeLevel { get; set; }
}

public sealed class MapPoint
{
    public int X { get; set; }
    public int Y { get; set; }
}

/// <summary>
/// Passagem declarada entre duas zonas (portao + estrada). Zonas vizinhas sem conexao nao
/// se ligam. O tempo de viagem e calculado pela geometria e pela velocidade (D-016).
/// </summary>
public sealed class RegionConnection
{
    public string To { get; set; } = "";
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

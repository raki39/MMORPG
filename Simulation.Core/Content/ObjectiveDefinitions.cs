namespace Simulation.Core.Content;

/// <summary>
/// Objetivos pessoais (objectives.json, O1): ambicoes que atravessam varias sessoes, diferentes das
/// metas (GoalSystem). Catalogo e regras do sorteio. Geral e secundario sao a importancia para a
/// pessoa, nao categorias do catalogo.
/// </summary>
public sealed class ObjectiveCatalogDefinition
{
    public ObjectiveGenerationDefinition Generation { get; set; } = new();
    /// <summary>Influencia (O2): pelas metas (adocao e comprometimento) e a conquista ao concluir.</summary>
    public ObjectiveInfluenceDefinition Influence { get; set; } = new();
    /// <summary>Evolucao (O3): renovacao, prioridade, substituicao, abandono e condicoes mantidas.</summary>
    public ObjectiveEvolutionDefinition Evolution { get; set; } = new();
    public List<ObjectiveDefinition> Objectives { get; set; } = new();
}

/// <summary>
/// Influencia dos objetivos (O2). So pelas metas: a meta candidata que contribui de verdade para um
/// objetivo (avanco real ou preparacao por requisito conhecido) ganha peso na adocao, e a meta
/// adotada ganha comprometimento. Vale so o maior reforco entre os objetivos atendidos (ter mais
/// objetivos nao da vantagem). Nenhum bonus direto nas atividades. Desligada: os vinculos e as
/// conclusoes continuam registrados, sem efeito (medicao pareada).
/// </summary>
public sealed class ObjectiveInfluenceDefinition
{
    public bool Enabled { get; set; } = true;
    /// <summary>Peso da meta candidata x (1 + reforco x importancia x comprometimento do objetivo).</summary>
    public int AdoptionBoostPermille { get; set; } = 1000;
    /// <summary>Parte da distancia do comprometimento da meta ate 1000 que o objetivo fecha (x importancia x comprometimento).</summary>
    public int CommitmentBoostPermille { get; set; } = 400;
    /// <summary>Conquista ao concluir um objetivo (pontos da sessao) e intensidade da memoria.</summary>
    public int CompletedPoints { get; set; } = 150;
    public int CompletedMemoryIntensity { get; set; } = 800;
}

/// <summary>Sorteio dos objetivos iniciais (no registro). Valores candidatos.</summary>
public sealed class ObjectiveGenerationDefinition
{
    public bool Enabled { get; set; } = true;
    /// <summary>Pesos da quantidade de objetivos gerais (indice = quantidade, 0 a 2).</summary>
    public List<int> GeneralCountWeights { get; set; } = new() { 0, 750, 250 };
    /// <summary>Pesos da quantidade de objetivos secundarios (indice = quantidade, 0 a 3).</summary>
    public List<int> SecondaryCountWeights { get; set; } = new() { 200, 350, 300, 150 };
    /// <summary>Limites do multiplicador de coerencia (produto das afinidades e da ambicao).</summary>
    public int MinMultiplierPermille { get; set; } = 200;
    public int MaxMultiplierPermille { get; set; } = 4000;
    /// <summary>
    /// Ambicao pela dedicacao (importancia do jogo): aproxima quem se dedica muito dos objetivos
    /// dificeis e quem se dedica pouco dos mais simples. Termo = 1 + forca x (ambicao) x (dificuldade),
    /// as duas centradas em 500 e escaladas a -1..1. Tendencia, nao regra.
    /// </summary>
    public int DifficultyStrengthPermille { get; set; } = 600;
    /// <summary>Importancia = 500 + (ajuste - 1000) x inclinacao / 1000 +- ruido, de 50 a 1000.</summary>
    public int ImportanceSlopePermille { get; set; } = 250;
    public int ImportanceNoise { get; set; } = 100;
    /// <summary>Comprometimento: base + importancia do jogo, paciencia e lealdade (permille de cada).</summary>
    public int CommitmentBase { get; set; } = 300;
    public int CommitmentGameImportancePermille { get; set; } = 400;
    public int CommitmentPatiencePermille { get; set; } = 150;
    public int CommitmentLoyaltyPermille { get; set; } = 150;
    /// <summary>Raridades que contam como "equipamento raro".</summary>
    public List<string> RareItemRarities { get; set; } = new() { "rare", "epic" };
}

/// <summary>
/// Evolucao dos objetivos (O3), avaliada uma vez por dia jogado, com intervalos configuraveis. Mudancas
/// so por acontecimentos simulados, com motivo registrado. Valores candidatos.
/// </summary>
public sealed class ObjectiveEvolutionDefinition
{
    public bool Enabled { get; set; } = true;
    /// <summary>Vagas de objetivos ativos (gerais e secundarios).</summary>
    public int MaxGeneral { get; set; } = 2;
    public int MaxSecondary { get; set; } = 3;
    /// <summary>Dias jogados minimos entre mudancas escolhidas (adotar, substituir, abandonar, mudar prioridade).</summary>
    public int ChangeCooldownPlayDays { get; set; } = 7;
    /// <summary>Um objetivo abandonado so volta como candidato depois de tantos dias jogados.</summary>
    public int AbandonCooldownPlayDays { get; set; } = 60;
    /// <summary>Objetivo que pode avancar e nao avancou por tantos dias jogados perde importancia (sistemas futuros nunca).</summary>
    public int StalePlayDays { get; set; } = 30;
    public int StaleImportanceDrop { get; set; } = 150;
    /// <summary>Abandono por experiencia negativa: metas ligadas (avanco real) abandonadas seguidas, base + comprometimento x por mil.</summary>
    public int FailuresBase { get; set; } = 2;
    public int FailuresPerCommitment { get; set; } = 4;
    /// <summary>Renovacao: peso pela viabilidade (pode avancar = 1000; preparacao conhecida; sistema futuro).</summary>
    public int PreparationFactorPermille { get; set; } = 800;
    public int FutureFactorPermille { get; set; } = 300;
    /// <summary>Interesse minimo (ajuste) para um objetivo ser relevante; mais alto para os de sistema futuro.</summary>
    public int MinFitPermille { get; set; } = 1000;
    public int FutureMinFitPermille { get; set; } = 1400;
    /// <summary>Reforco de um objetivo relacionado ("opens") aos que a pessoa tem ou concluiu.</summary>
    public int RelatedBonusPermille { get; set; } = 800;
    /// <summary>Reforco para recuperar uma condicao perdida.</summary>
    public int RecoverBonusPermille { get; set; } = 300;
    /// <summary>Experiencias marcantes (memorias boas e ruins) nos tipos de atividade do objetivo: forca e teto da contagem.</summary>
    public int ExperienceBoostPermille { get; set; } = 400;
    public int ExperienceCap { get; set; } = 4;
    /// <summary>Substituicao voluntaria: o novo precisa ser mais importante que o secundario deixado de lado, por esta margem.</summary>
    public int SubstituteMarginPermille { get; set; } = 100;
    /// <summary>Abandono por experiencia negativa: reves, frustracao e memoria.</summary>
    public int AbandonedPoints { get; set; } = 100;
    public int AbandonedFrustration { get; set; } = 80;
    public int AbandonedMemoryIntensity { get; set; } = 600;
}

/// <summary>Natureza do objetivo: conquista definitiva ou condicao a manter (pode ser perdida e recuperada).</summary>
public static class ObjectiveNatures
{
    public const string Achievement = "achievement";
    public const string Condition = "condition";
    public static readonly string[] All = { Achievement, Condition };
}

/// <summary>Um objetivo do catalogo. O criterio e de um tipo conhecido do Core (ObjectiveCriteria).</summary>
public sealed class ObjectiveDefinition : IContentDefinition
{
    public string Id { get; set; } = "";
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Icone (Lucide) da ficha.</summary>
    public string Icon { get; set; } = "";
    /// <summary>Tipo de criterio verificavel (define os sistemas necessarios e o progresso).</summary>
    public string Criterion { get; set; } = "";
    /// <summary>Dificuldade (0 a 1000): quanto e uma ambicao de fim de jogo.</summary>
    public int Difficulty { get; set; } = 500;
    /// <summary>Horizonte: short, medium ou long (caracteristica, separada da importancia).</summary>
    public string Horizon { get; set; } = ObjectiveHorizons.Medium;
    /// <summary>Peso base no sorteio.</summary>
    public int Weight { get; set; } = 1000;
    /// <summary>Conquista definitiva (fica concluida) ou condicao a manter (pode ser perdida; perder nao apaga a conquista).</summary>
    public string Nature { get; set; } = ObjectiveNatures.Achievement;
    /// <summary>Objetivos que este abre ou aproxima (renovacao, O3): reforco, nao sequencia obrigatoria.</summary>
    public List<string> Opens { get; set; } = new();
    /// <summary>Tipos de atividade cujas experiencias marcantes aproximam (boas) ou afastam (ruins) deste objetivo.</summary>
    public List<string> ExperienceTypes { get; set; } = new();
    /// <summary>
    /// Coerencia com a pessoa (mesmas chaves das traits e mais "quirk.x", ter a trait): aumentam ou
    /// reduzem a chance e a importancia.
    /// </summary>
    public List<QuirkAffinityDefinition> Affinities { get; set; } = new();
}

public static class ObjectiveHorizons
{
    public const string Short = "short";
    public const string Medium = "medium";
    public const string Long = "long";
    public static readonly string[] All = { Short, Medium, Long };
}

/// <summary>Importancia do objetivo para a pessoa.</summary>
public static class ObjectiveTiers
{
    public const string General = "general";
    public const string Secondary = "secondary";
}

/// <summary>
/// Sistemas de que um criterio depende. Implemented lista os que existem hoje: um objetivo que
/// depende de outro sistema nao recebe progresso (sem progresso ficticio).
/// </summary>
public static class ObjectiveSystems
{
    public const string Progression = "progression";
    public const string Quests = "quests";
    public const string Exploration = "exploration";
    public const string Combat = "combat";
    public const string Groups = "groups";
    public const string Pvp = "pvp";
    public const string Ranking = "ranking";
    public const string Items = "items";
    public const string Economy = "economy";
    public const string Guilds = "guilds";
    public const string Crafting = "crafting";
    public static readonly string[] Implemented = { Progression, Quests, Exploration };
    public static bool Exists(string system) => Array.IndexOf(Implemented, system) >= 0;
}

/// <summary>
/// Tipos de criterio verificavel. Cada um declara os sistemas de que depende; o progresso e a
/// preparacao sao calculados pelo Core (ObjectiveSystem.Assess) so com o que existe.
/// </summary>
public static class ObjectiveCriteria
{
    /// <summary>Chegar ao nivel maximo.</summary>
    public const string MaxLevel = "max_level";
    /// <summary>Concluir todas as missoes principais.</summary>
    public const string MainCampaign = "main_campaign";
    /// <summary>Conhecer todas as zonas do mundo.</summary>
    public const string AllZones = "all_zones";
    /// <summary>Estar entre os personagens mais fortes (classificacao real de forca, nao o indice de poder).</summary>
    public const string StrengthRanking = "strength_ranking";
    /// <summary>Vencer os chefes do mundo.</summary>
    public const string DefeatBosses = "defeat_bosses";
    /// <summary>Posicao relevante no PvP real.</summary>
    public const string PvpRanking = "pvp_ranking";
    /// <summary>Riqueza possuida (saldo; patrimonio quando a economia existir), nao o ouro recebido.</summary>
    public const string Wealth = "wealth";
    public const string FoundGuild = "found_guild";
    public const string JoinGuild = "join_guild";
    /// <summary>Estar entre os melhores fabricantes de uma especialidade (parametro: categoria de item).</summary>
    public const string CraftingRank = "crafting_rank";
    /// <summary>Estar entre os maiores comerciantes (prestigio no mercado).</summary>
    public const string TradeRank = "trade_rank";
    /// <summary>Conquistar um equipamento raro (parametro: item).</summary>
    public const string RareItem = "rare_item";

    public static readonly string[] All =
        { MaxLevel, MainCampaign, AllZones, StrengthRanking, DefeatBosses, PvpRanking, Wealth, FoundGuild, JoinGuild, CraftingRank, TradeRank, RareItem };

    public static IReadOnlyList<string> RequiredSystems(string criterion) => criterion switch
    {
        MaxLevel => new[] { ObjectiveSystems.Progression },
        MainCampaign => new[] { ObjectiveSystems.Quests },
        AllZones => new[] { ObjectiveSystems.Exploration },
        StrengthRanking => new[] { ObjectiveSystems.Combat, ObjectiveSystems.Ranking },
        DefeatBosses => new[] { ObjectiveSystems.Groups, ObjectiveSystems.Combat },
        PvpRanking => new[] { ObjectiveSystems.Pvp, ObjectiveSystems.Ranking },
        Wealth => new[] { ObjectiveSystems.Economy },
        FoundGuild or JoinGuild => new[] { ObjectiveSystems.Guilds },
        CraftingRank => new[] { ObjectiveSystems.Crafting, ObjectiveSystems.Ranking },
        TradeRank => new[] { ObjectiveSystems.Economy, ObjectiveSystems.Ranking },
        RareItem => new[] { ObjectiveSystems.Items },
        _ => Array.Empty<string>(),
    };
}

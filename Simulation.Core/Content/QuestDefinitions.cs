namespace Simulation.Core.Content;

/// <summary>
/// Catalogo de quests (D-031): quadros (areas de quest dentro das cidades) e as quests que
/// cada um oferece. Tudo e conteudo: o jogador, como desenvolvedor do MMORPG, podera criar
/// quadros e quests no editor do jogo.
/// </summary>
public sealed class QuestCatalog
{
    public QuestSettings Settings { get; set; } = new();
    public List<QuestBoardDefinition> Boards { get; set; } = new();
    public List<QuestDefinition> Quests { get; set; } = new();
}

public sealed class QuestSettings
{
    /// <summary>Quests ativas ao mesmo tempo por personagem.</summary>
    public int MaxActivePerCharacter { get; set; } = 3;
    /// <summary>Reset diario do MMORPG, em minutos depois da meia-noite do servidor (relogio do servidor, nao o local).</summary>
    public int DailyResetMinute { get; set; }
    /// <summary>Largar uma missao: guarda o progresso para quando aceitar de novo (true) ou perde (false).</summary>
    public bool DropKeepsProgress { get; set; }
    /// <summary>Dias (do servidor) ate poder aceitar de novo uma missao largada (0 = logo).</summary>
    public int ReacceptAfterDropDays { get; set; }
    /// <summary>Diario de missoes (D-066): expiracao das diarias, troca por prioridade e vaga reservada.</summary>
    public QuestJournalDefinition Journal { get; set; } = new();
}

/// <summary>
/// Diario de missoes (D-066). Desligado, nada muda. Ligado: as diarias expiram no reset do
/// servidor; a pessoa larga uma missao para aceitar outra que valoriza mais (troca de prioridade, sem
/// frustracao), pelo valor da missao para ela, que considera o progresso, o rendimento no nivel, a
/// historia, o que a missao libera e os vinculos com objetivos; guarda uma vaga para a missao da meta.
/// </summary>
public sealed class QuestJournalDefinition
{
    public bool Enabled { get; set; }
    /// <summary>A oferta precisa valer mais que a missao largada por esta margem (permille).</summary>
    public int SwapMarginPermille { get; set; } = 200;
    /// <summary>Valor da historia (missao principal), independente do rendimento.</summary>
    public int MainStoryValue { get; set; } = 600;
    /// <summary>Valor de liberar uma missao no nivel da pessoa (pre-requisito dela).</summary>
    public int UnlockValue { get; set; } = 300;
    /// <summary>Niveis acima do atual que ainda contam como "no nivel" para o que a missao libera.</summary>
    public int UnlockLevelsAhead { get; set; } = 4;
    /// <summary>Valor de servir a um objetivo ativo da pessoa (missao principal e a campanha).</summary>
    public int ObjectiveValue { get; set; } = 400;
    /// <summary>Peso do progresso ja feito (permille do valor por missao inteira feita).</summary>
    public int ProgressWeight { get; set; } = 1000;
    /// <summary>
    /// Visita coerente (D-067): a escolha de ir ao quadro e o que a pessoa faz nele saem do mesmo plano
    /// (aceites e trocas). Sem isso, o quadro podia virar opcao por uma troca ou vaga que a acao no quadro
    /// nao usava (vaga reservada, troca so com a primeira oferta), e a pessoa voltava a ele sem resultado.
    /// </summary>
    public bool ConsistentVisits { get; set; }
    /// <summary>Aceite avaliado (D-067): so aceita a oferta com uma motivacao plausivel.</summary>
    public QuestAcceptanceDefinition Acceptance { get; set; } = new();
}

/// <summary>
/// Aceite avaliado (D-067). Desligado, a pessoa aceita tudo o que cabe nas vagas, na ordem do quadro.
/// Ligado: a missao da meta continua aceita sempre; as demais ofertas so se a motivacao (o maior motivo:
/// recompensa no nivel, relevancia ou gosto pelo tema das etapas, vezes o deslocamento adicional ate a
/// primeira etapa) chega ao minimo; a diaria, so se da para fazer e entregar no tempo de jogo que a pessoa
/// ainda tem antes do reset (resto da sessao e janelas livres da rotina); a diaria que expirou sem nenhuma
/// etapa feita nao volta ate a pessoa fazer a atividade da etapa. Aceitar por oportunidade vale: perto da
/// etapa de outra missao ativa, conta so o desvio real.
/// </summary>
public sealed class QuestAcceptanceDefinition
{
    public bool Enabled { get; set; }
    /// <summary>Motivacao minima para aceitar (permille), na escala do gosto (a mesma do Independente, 400).</summary>
    public int MinInterest { get; set; } = 400;
    /// <summary>XP da recompensa que vale 1000 na motivacao (permille do XP do nivel): a de uma missao principal no nivel.</summary>
    public int RewardReferencePermille { get; set; } = 450;
    /// <summary>Relevancia de liberar uma missao no nivel (pre-requisito dela).</summary>
    public int UnlockRelevance { get; set; } = 800;
    /// <summary>Relevancia de servir a uma meta de praticar o tema de uma etapa.</summary>
    public int GoalRelevance { get; set; } = 1000;
    /// <summary>Diaria que expirou sem nenhuma etapa feita nao e aceita de novo ate a pessoa fazer a atividade da etapa.</summary>
    public bool IdleExpiryBlock { get; set; } = true;
    /// <summary>
    /// Prazo da diaria pronta: ir entregar ganha o peso abaixo quando o reset ou o fim previsto da sessao
    /// estao a ate a viagem mais estes minutos (0 = desligado).
    /// </summary>
    public int DeliverDeadlineMarginMinutes { get; set; }
    public int DeliverDeadlineWeight { get; set; } = 220;
}

/// <summary>
/// Area de quest: o lugar (area de uma zona) onde se aceitam e entregam quests. A atividade
/// indicada (tipo quest) e a acao pontual de aceitar ou entregar.
/// </summary>
public sealed class QuestBoardDefinition : IContentDefinition
{
    public string Id { get; set; } = "";
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "";
    public string RegionId { get; set; } = "";
    public string AreaId { get; set; } = "";
    public string ActivityId { get; set; } = "";
    /// <summary>Diarias que um personagem pode concluir por dia neste quadro.</summary>
    public int DailyLimit { get; set; } = 2;
}

public static class QuestCategories
{
    /// <summary>Cadeia principal: pre-requisitos e etapas; uma vez por personagem.</summary>
    public const string Main = "main";
    /// <summary>Historias e desafios locais; repeticao definida pela quest.</summary>
    public const string Secondary = "secondary";
    /// <summary>Renovada pelo calendario simulado; limitada por quadro e por personagem.</summary>
    public const string Daily = "daily";
    public static readonly string[] All = { Main, Secondary, Daily };
}

public static class QuestStepTypes
{
    /// <summary>Fazer uma atividade do mundo por um tempo (caca, coleta, exploracao, patrulha...).</summary>
    public const string Activity = "activity";
    /// <summary>Ir a um ponto de missao (atividade do tipo quest fora do quadro) e cumprir a acao.</summary>
    public const string Visit = "visit";
    public static readonly string[] All = { Activity, Visit };
}

public sealed class QuestDefinition : IContentDefinition
{
    public string Id { get; set; } = "";
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    /// <summary>Quadro (area de quest) que oferece e recebe a quest.</summary>
    public string BoardId { get; set; } = "";
    public string Category { get; set; } = QuestCategories.Secondary;
    /// <summary>Nivel para o qual a quest foi pensada (base da recompensa).</summary>
    public int RecommendedLevel { get; set; } = 1;
    public int MinLevel { get; set; } = 1;
    /// <summary>0 = sem teto de nivel.</summary>
    public int MaxLevel { get; set; }
    public List<string> PrerequisiteQuestIds { get; set; } = new();
    /// <summary>Vazio = qualquer classe.</summary>
    public List<string> ClassIds { get; set; } = new();
    /// <summary>Secundarias: dias ate poder repetir (0 = uma vez so).</summary>
    public int RepeatCooldownDays { get; set; }
    /// <summary>Recompensa calculada pelo nivel do personagem (tipico de diarias).</summary>
    public bool ScalesWithLevel { get; set; }
    public List<QuestStepDefinition> Steps { get; set; } = new();
    public QuestRewardDefinition Rewards { get; set; } = new();
}

public sealed class QuestStepDefinition
{
    public string Type { get; set; } = QuestStepTypes.Activity;
    public string RegionId { get; set; } = "";
    public string ActivityId { get; set; } = "";
    /// <summary>Minutos fazendo a atividade (ou cumprindo a acao no ponto de missao).</summary>
    public int Minutes { get; set; }
    /// <summary>Objetivo como aparece ao jogador.</summary>
    public string Text { get; set; } = "";
}

public sealed class QuestRewardDefinition
{
    /// <summary>XP em milesimos do XP do nivel de referencia (recomendado ou do personagem).</summary>
    public int XpPermille { get; set; }
    /// <summary>Ouro (cobre). Sem efeito nesta versao: a economia chega no M5.</summary>
    public long GoldCopper { get; set; }
    /// <summary>Itens. Sem efeito nesta versao: o inventario chega no M5.</summary>
    public List<string> ItemIds { get; set; } = new();
}

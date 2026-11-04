namespace Simulation.Core.Content;

/// <summary>Faixa de um parametro: media e dispersao (mesma unidade do parametro).</summary>
public sealed class TraitRange
{
    public int Mean { get; set; }
    public int Spread { get; set; }
}

/// <summary>
/// Perfil (predefinicao) de populacao. Gera tendencias, nao clones: cada pessoa recebe
/// ruido individual em cada parametro. Comportamentais em permille (0..1000).
/// </summary>
public sealed class PopulationProfileDefinition : IContentDefinition
{
    public string Id { get; set; } = "";
    public int Version { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Peso relativo na populacao gerada.</summary>
    public int Weight { get; set; }

    // Afinidades
    public TraitRange Pve { get; set; } = new();
    public TraitRange Pvp { get; set; } = new();
    public TraitRange Trade { get; set; } = new();
    public TraitRange Exploration { get; set; } = new();
    public TraitRange Socializing { get; set; } = new();

    // Personalidade
    public TraitRange Sociability { get; set; } = new();
    public TraitRange Aggressiveness { get; set; } = new();
    public TraitRange Patience { get; set; } = new();
    public TraitRange Loyalty { get; set; } = new();
    public TraitRange RiskAversion { get; set; } = new();

    // Capacidade
    public TraitRange MechanicalSkill { get; set; } = new();
    public TraitRange GameKnowledge { get; set; } = new();
    public TraitRange Coordination { get; set; } = new();

    // Conduta
    public TraitRange CheatPropensity { get; set; } = new();
    public TraitRange Opportunism { get; set; } = new();

    // Financas
    /// <summary>Orcamento de lazer em centavos (dinheiro real ficticio do tycoon) por mes.</summary>
    public TraitRange LeisureBudgetCentsPerMonth { get; set; } = new();
    public TraitRange WillingnessToPay { get; set; } = new();
    public TraitRange PaidAdvantageSensitivity { get; set; } = new();

    // Rotina
    /// <summary>Hora preferida de inicio de sessao (0..23).</summary>
    public TraitRange PreferredStartHour { get; set; } = new();
    public TraitRange SessionMinutes { get; set; } = new();
    /// <summary>Dias de jogo por semana em dias uteis+fim de semana (0..7).</summary>
    public TraitRange DaysPerWeek { get; set; } = new();
    /// <summary>Quanto fins de semana/feriados aumentam disponibilidade (permille, 0 = nada).</summary>
    public TraitRange WeekendBoost { get; set; } = new();

    // Jogador (D-036)
    /// <summary>Talento da pessoa como jogador (permille): potencial e velocidade de aprendizado das capacidades.</summary>
    public TraitRange Talent { get; set; } = new();
}

public sealed class NamePoolDefinition
{
    public List<string> FirstNames { get; set; } = new();
    public List<string> LastNames { get; set; } = new();
    /// <summary>Partes para apelidos de personagem (prefixo + sufixo).</summary>
    public List<string> NickPrefixes { get; set; } = new();
    public List<string> NickSuffixes { get; set; } = new();
}

public sealed class PopulationDefinition
{
    public List<PopulationProfileDefinition> Profiles { get; set; } = new();
    public NamePoolDefinition Names { get; set; } = new();
    /// <summary>Como os atributos do jogador evoluem (D-036).</summary>
    public TraitDynamicsDefinition Dynamics { get; set; } = new();
    /// <summary>Metas de curto prazo do jogador (D-036).</summary>
    public GoalSettingsDefinition Goals { get; set; } = new();
    /// <summary>Plano da sessao, continuidade e tedio (D-037).</summary>
    public SessionSettingsDefinition Session { get; set; } = new();
    /// <summary>Experiencia e humor: satisfacao, frustracao, novidade, engajamento (D-038).</summary>
    public MoodSettingsDefinition Mood { get; set; } = new();
    /// <summary>Memoria: impressoes por lugar e acontecimentos marcantes (D-038).</summary>
    public MemorySettingsDefinition Memory { get; set; } = new();
}

/// <summary>
/// Evolucao dos atributos do jogador (D-036). Unidade dos atributos: permille do Core;
/// 1 ponto da escala 1–20 da interface ~ 52,6 unidades. "Minutos por unidade" = minutos de
/// pratica para +1 unidade, antes do talento e da desaceleracao perto do potencial.
/// </summary>
public sealed class TraitDynamicsDefinition
{
    /// <summary>Quanto cada traco pode cair abaixo da base (unidades).</summary>
    public int RangeSkill { get; set; } = 250;
    public int RangePlaystyle { get; set; } = 200;
    public int RangePersonality { get; set; } = 100;
    /// <summary>Potencial absoluto das capacidades: base + talento * porTalento / 1000 (nao depende do inicio).</summary>
    public int PotentialBase { get; set; } = 250;
    public int PotentialPerTalent { get; set; } = 700;
    /// <summary>Folga minima acima do valor inicial, mesmo com talento baixo.</summary>
    public int PotentialMinHeadroom { get; set; } = 50;
    /// <summary>Custo da pratica das capacidades pelo talento (permille): talento 0 e talento 1000.</summary>
    public int LearnCostAtZeroTalent { get; set; } = 1400;
    public int LearnCostAtMaxTalent { get; set; } = 600;
    /// <summary>Custo extra ao chegar no potencial (permille; 2000 = 3x mais caro no teto).</summary>
    public int NearCeilingSlowdown { get; set; } = 2000;

    public int MechanicalCombatMinutes { get; set; } = 150;
    public int KnowledgePlayMinutes { get; set; } = 400;
    public int KnowledgeExploreMinutes { get; set; } = 90;
    public int KnowledgeNewActivityType { get; set; } = 3;
    public int KnowledgePerDiscovery { get; set; } = 2;
    public int KnowledgePerQuest { get; set; } = 1;
    public int SociabilitySocialMinutes { get; set; } = 600;
    /// <summary>Experiencia positiva persistente reforca o gosto (minutos com bom encaixe por unidade).</summary>
    public int AffinityEnjoyedMinutes { get; set; } = 360;
    public int EnjoyedMinFitPermille { get; set; } = 600;
    public int RiskConfidenceMinutes { get; set; } = 600;
    public int RiskConfidenceMinRisk { get; set; } = 300;
    public int ExplorationPerDiscoveries { get; set; } = 4;
    /// <summary>Abater outro jogador (M3): reforco do gosto por PvP e pratica de combate (minutos).</summary>
    public int PvpKillPreference { get; set; } = 3;
    public int PvpKillCombatMinutes { get; set; } = 30;

    public int MechanicalDecayAfterDays { get; set; } = 7;
    public int MechanicalDecayPerDay { get; set; } = 1;
    public int KnowledgeDecayAfterDays { get; set; } = 14;
    public int KnowledgeDecayPerDay { get; set; } = 1;

    /// <summary>Familiaridade: zona onde ja passou tempo (peso maximo e minutos para familiaridade total).</summary>
    public int FamiliarityWeight { get; set; } = 80;
    public int FamiliarityFullMinutes { get; set; } = 600;
    /// <summary>Gosto por desafio acima do nivel: competitividade, confianca e pouca aversao a risco (por nivel, ate 3).</summary>
    public int ChallengeWeightPerLevel { get; set; } = 40;
    /// <summary>Erro da estimativa de viagem com conhecimento zero (permille, +-); preciso com conhecimento alto.</summary>
    public int TravelEstimateErrorAtZero { get; set; } = 300;
}

/// <summary>Metas de curto prazo do jogador (D-036). Ambicoes de longo prazo ficam para depois.</summary>
public sealed class GoalSettingsDefinition
{
    /// <summary>Peso maximo de uma meta na nota de uma opcao (com comprometimento 1000).</summary>
    public int Weight { get; set; } = 200;
    /// <summary>Teto da soma das metas numa opcao (permille do peso).</summary>
    public int MaxStackPermille { get; set; } = 1500;
    public int MaxActive { get; set; } = 2;
    /// <summary>Chance de adotar uma meta ao conectar, quando ha vaga (permille).</summary>
    public int AdoptChance { get; set; } = 450;
    /// <summary>Abandono por falta de progresso: horas de jogo = base + paciencia * extra / 1000.</summary>
    public int StaleBaseHours { get; set; } = 3;
    public int StaleExtraHoursAtMaxPatience { get; set; } = 9;
    public int ImpatientDropChance { get; set; } = 40;
    public int ImpatientBelow { get; set; } = 300;
    public int MotiveReinforce { get; set; } = 1;
}

/// <summary>
/// Plano da sessao e continuidade (D-026/D-037). Utilidades em pontos da nota (mesma escala
/// dos fatores da decisao); tedio em permille; tempos em minutos simulados.
/// </summary>
public sealed class SessionSettingsDefinition
{
    /// <summary>Vantagem minima de outra opcao para trocar de plano.</summary>
    public int SwitchMargin { get; set; } = 60;
    /// <summary>Vantagem minima durante o compromisso inicial do plano.</summary>
    public int CommitMargin { get; set; } = 150;
    /// <summary>Compromisso minimo do plano: base + paciencia * extra / 1000 (minutos).</summary>
    public int CommitBaseMinutes { get; set; } = 30;
    public int CommitExtraMinutesAtMaxPatience { get; set; } = 60;
    /// <summary>Tedio por hora no mesmo tipo de atividade (antes da preferencia por variedade).</summary>
    public int BoredomPerHour { get; set; } = 300;
    /// <summary>Alivio do tedio por hora fazendo outra coisa.</summary>
    public int BoredomRecoveryPerHour { get; set; } = 400;
    /// <summary>Quanto do tedio sobra para a proxima sessao (permille).</summary>
    public int BoredomKeptBetweenSessions { get; set; } = 300;
    /// <summary>Peso do tedio na nota (com tedio 1000 e paciencia 0).</summary>
    public int BoredomWeight { get; set; } = 400;
    /// <summary>Preferencia por variedade: base + exploracao * fator / 1000 (permille do acumulo).</summary>
    public int VarietyBase { get; set; } = 600;
    public int VarietyPerExploration { get; set; } = 800;
    /// <summary>Quanto a paciencia alivia o peso do tedio (permille com paciencia 1000).</summary>
    public int PatienceRelief { get; set; } = 500;
    /// <summary>Gosto pelo tipo reduz o acumulo: gosto 1000 = -metade disto; gosto 0 = +metade (permille).</summary>
    public int EnjoymentRelief { get; set; } = 500;
    /// <summary>Explorar onde nao ha mais nada a descobrir: perde esta parte do gosto (permille).</summary>
    public int ExploreKnownPenalty { get; set; } = 500;
}

/// <summary>Experiencia e humor da pessoa (D-038). Pontos da nota da sessao (0–1000, neutro 500) e permille.</summary>
public sealed class MoodSettingsDefinition
{
    /// <summary>Satisfacao ao entrar no jogo (permille).</summary>
    public int InitialSatisfaction { get; set; } = 600;
    /// <summary>Novidade ao entrar: tudo e novo.</summary>
    public int InitialNovelty { get; set; } = 1000;
    /// <summary>Gosto pelo tipo da atividade (gosto - 500) na experiencia.</summary>
    public int FunWeight { get; set; } = 300;
    /// <summary>Encaixe de nivel (rendimento - 600) nas atividades que dao XP.</summary>
    public int FitWeight { get; set; } = 200;
    /// <summary>Lotacao na caca e na coleta (aliviada pela coordenacao).</summary>
    public int CrowdingWeight { get; set; } = 200;
    /// <summary>Companhia: sociabilidade na taverna; gosto por socializar no resto.</summary>
    public int CompanyWeight { get; set; } = 150;
    /// <summary>Tedio do tipo no fim da atividade.</summary>
    public int BoredomWeight { get; set; } = 250;
    /// <summary>Risco: emociona quem tem pouca aversao, incomoda quem tem muita.</summary>
    public int RiskWeight { get; set; } = 150;
    /// <summary>Progresso da sessao contra a expectativa.</summary>
    public int ProgressWeight { get; set; } = 400;
    /// <summary>Quanto liga para evoluir: parcelas de PvE, competitividade e PvP (permille; somam 1000).</summary>
    public int ProgressCarePve { get; set; } = 500;
    public int ProgressCareAggressiveness { get; set; } = 300;
    public int ProgressCarePvp { get; set; } = 200;
    /// <summary>Quanto precisa de novidade: base + exploracao e impaciencia (permille).</summary>
    public int NoveltyCareBase { get; set; } = 200;
    public int NoveltyCareExploration { get; set; } = 500;
    public int NoveltyCareImpatience { get; set; } = 300;
    /// <summary>Limite (+-) do termo de progresso.</summary>
    public int ProgressClamp { get; set; } = 200;
    /// <summary>Teto da razao de progresso (permille da referencia).</summary>
    public int ProgressRatioCap { get; set; } = 2000;
    /// <summary>Expectativa inicial de progresso (permille da referencia).</summary>
    public int ExpectationBase { get; set; } = 550;
    /// <summary>Competitivos esperam mais: + agressividade * isto / 1000.</summary>
    public int ExpectationPerAggressiveness { get; set; } = 300;
    /// <summary>Quanto a expectativa se aproxima do progresso real por sessao (permille).</summary>
    public int ExpectationAdapt { get; set; } = 100;
    /// <summary>Piso da expectativa: progredir mal por dias nao vira satisfatorio.</summary>
    public int ExpectationFloor { get; set; } = 450;
    /// <summary>Teto da expectativa.</summary>
    public int ExpectationCeiling { get; set; } = 1100;
    /// <summary>Novidade (novidade - 500) na nota da sessao.</summary>
    public int NoveltyWeight { get; set; } = 300;
    /// <summary>Novidade gasta por hora de jogo.</summary>
    public int NoveltyDecayPerHour { get; set; } = 20;
    /// <summary>Novidade por zona descoberta.</summary>
    public int NoveltyPerDiscovery { get; set; } = 60;
    /// <summary>Novidade na primeira vez num tipo de atividade.</summary>
    public int NoveltyNewActivityType { get; set; } = 80;
    /// <summary>Novidade na primeira vez numa atividade (lugar).</summary>
    public int NoveltyNewActivity { get; set; } = 25;
    /// <summary>Novidade por nivel novo (conteudo novo liberado).</summary>
    public int NoveltyPerLevel { get; set; } = 30;
    /// <summary>Novidade na primeira conclusao de uma missao.</summary>
    public int NoveltyNewQuest { get; set; } = 40;
    /// <summary>Limite (+-) dos acontecimentos na nota da sessao.</summary>
    public int EventClamp { get; set; } = 300;
    /// <summary>Pontos de um nivel novo.</summary>
    public int LevelUpPoints { get; set; } = 60;
    /// <summary>Pontos de uma missao concluida.</summary>
    public int QuestPoints { get; set; } = 40;
    /// <summary>Pontos de uma missao principal concluida.</summary>
    public int MainQuestPoints { get; set; } = 100;
    /// <summary>Pontos de uma meta concluida.</summary>
    public int GoalCompletedPoints { get; set; } = 80;
    /// <summary>Pontos de uma zona descoberta (escala com a exploracao).</summary>
    public int DiscoveryPoints { get; set; } = 40;
    /// <summary>Pontos perdidos ao desistir de uma meta.</summary>
    public int GoalAbandonedPoints { get; set; } = 80;
    /// <summary>Pontos perdidos ao ficar sem nada para fazer.</summary>
    public int NothingToDoPoints { get; set; } = 60;
    /// <summary>Frustracao imediata ao desistir de uma meta.</summary>
    public int GoalAbandonedFrustration { get; set; } = 80;
    /// <summary>Frustracao imediata ao ficar sem nada para fazer.</summary>
    public int NothingToDoFrustration { get; set; } = 40;
    /// <summary>Peso de uma sessao longa na satisfacao (permille).</summary>
    public int SatisfactionAdapt { get; set; } = 250;
    /// <summary>Sessoes mais curtas pesam proporcionalmente menos.</summary>
    public int FullWeightSessionMinutes { get; set; } = 120;
    /// <summary>Nota abaixo disto aumenta a frustracao.</summary>
    public int BadSessionBelow { get; set; } = 400;
    /// <summary>Frustracao por ponto abaixo do limite (permille).</summary>
    public int FrustrationFromBadSession { get; set; } = 600;
    /// <summary>Nota acima disto alivia a frustracao.</summary>
    public int GoodSessionAbove { get; set; } = 600;
    /// <summary>Alivio por ponto acima do limite (permille).</summary>
    public int FrustrationReliefFromGoodSession { get; set; } = 500;
    /// <summary>Frustracao que passa por dia.</summary>
    public int FrustrationDecayPerDay { get; set; } = 40;
    /// <summary>Os motivos acumulados perdem esta fracao por dia (permille).</summary>
    public int DriverDecayPerDay { get; set; } = 50;
    /// <summary>Peso da satisfacao no engajamento.</summary>
    public int EngagementSatisfaction { get; set; } = 6;
    /// <summary>Peso da calma (1000 - frustracao) no engajamento.</summary>
    public int EngagementCalm { get; set; } = 3;
    /// <summary>Peso da novidade no engajamento.</summary>
    public int EngagementNovelty { get; set; } = 1;
    /// <summary>Lealdade segura a intencao de continuar: (lealdade - 500) * isto / 1000.</summary>
    public int LoyaltyWeight { get; set; } = 200;
    /// <summary>Investimento: cada nivel segura a intencao de continuar.</summary>
    public int InvestmentPerLevel { get; set; } = 5;
    /// <summary>Teto do investimento.</summary>
    public int InvestmentCap { get; set; } = 150;
}

/// <summary>Memoria da pessoa (D-038): impressoes por lugar e acontecimentos marcantes.</summary>
public sealed class MemorySettingsDefinition
{
    /// <summary>Quanto uma experiencia muda a impressao do lugar (permille).</summary>
    public int ImpressionLearn { get; set; } = 300;
    /// <summary>Boas impressoes esmaecem por dia (permille).</summary>
    public int ImpressionDecayPositivePerDay { get; set; } = 15;
    /// <summary>Mas impressoes esmaecem mais rapido (permille).</summary>
    public int ImpressionDecayNegativePerDay { get; set; } = 40;
    /// <summary>Peso da impressao do lugar na decisao (impressao 1000).</summary>
    public int DecisionWeight { get; set; } = 150;
    /// <summary>Impressoes menores que isto nao pesam na decisao.</summary>
    public int ImpressionMinAbs { get; set; } = 50;
    /// <summary>Acontecimentos marcantes guardados por pessoa.</summary>
    public int MaxMemories { get; set; } = 12;
    /// <summary>Experiencia a partir desta intensidade vira memoria.</summary>
    public int NotableValence { get; set; } = 350;
    /// <summary>Conquistas perdem intensidade por dia.</summary>
    public int PositiveDecayPerDay { get; set; } = 10;
    /// <summary>Frustracoes perdem intensidade por dia.</summary>
    public int NegativeDecayPerDay { get; set; } = 25;
    /// <summary>Niveis multiplos disto viram memoria.</summary>
    public int LevelMilestoneEvery { get; set; } = 5;
    /// <summary>Intensidade da memoria de um nivel marcante.</summary>
    public int LevelMilestoneIntensity { get; set; } = 700;
    /// <summary>Intensidade da memoria de uma missao principal.</summary>
    public int MainQuestIntensity { get; set; } = 650;
    /// <summary>Intensidade da memoria de uma meta concluida.</summary>
    public int GoalCompletedIntensity { get; set; } = 500;
    /// <summary>Intensidade da memoria de uma meta abandonada.</summary>
    public int GoalAbandonedIntensity { get; set; } = 450;
    /// <summary>Intensidade da memoria de uma zona descoberta (escala com a exploracao).</summary>
    public int DiscoveryIntensity { get; set; } = 300;
    /// <summary>Intensidade da memoria de ficar sem nada para fazer.</summary>
    public int NothingToDoIntensity { get; set; } = 300;
    /// <summary>Sessao com nota a partir disto vira memoria boa.</summary>
    public int GreatSessionScore { get; set; } = 780;
    /// <summary>Sessao com nota ate isto vira memoria ruim.</summary>
    public int BadSessionScore { get; set; } = 260;
    /// <summary>Intensidade da memoria de uma sessao marcante.</summary>
    public int SessionIntensity { get; set; } = 500;
}

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

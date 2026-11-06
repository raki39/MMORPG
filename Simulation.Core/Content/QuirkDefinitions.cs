namespace Simulation.Core.Content;

// Traits (P4b): tracos marcantes e nomeados da pessoa, de 0 a 6, separados do Perfil, das
// Preferencias, das Especialidades, dos atributos numericos e da personalidade principal. Ficam em
// traits.json. No codigo o nome e "Quirk" para nao confundir com os atributos numericos
// (PersonTraits, TraitRange, TraitDraw), que vieram antes.

/// <summary>Conteudo de traits.json: regras do sorteio e catalogo.</summary>
public sealed class QuirkCatalogDefinition
{
    public QuirkGenerationDefinition Generation { get; set; } = new();
    /// <summary>Efeitos das traits da pessoa (P4c): magnitudes configuraveis por trait.</summary>
    public QuirkEffectsDefinition Effects { get; set; } = new();
    /// <summary>Aquisicao e perda por evidencia (P4d).</summary>
    public QuirkAcquisitionDefinition Acquisition { get; set; } = new();
    public List<QuirkDefinition> Traits { get; set; } = new();
}

/// <summary>
/// Efeitos das traits da pessoa (P4c). Cada trait age num ponto de avaliacao conhecido do Core;
/// permille = multiplicador (1000 = sem efeito). Traits de sistemas futuros nao tem efeito aqui.
/// Todos os valores sao candidatos para medicao.
/// </summary>
public sealed class QuirkEffectsDefinition
{
    public bool Enabled { get; set; } = true;
    public PersistentEffect Persistente { get; set; } = new();
    public ImpulsiveEffect Impulsivo { get; set; } = new();
    public CollectorEffect Colecionador { get; set; } = new();
    public OptimizerEffect Otimizador { get; set; } = new();
    public ResilientEffect Resiliente { get; set; } = new();
    public ShortFuseEffect PavioCurto { get; set; } = new();
    public NostalgicEffect Saudosista { get; set; } = new();
    public GrudgeEffect Rancoroso { get; set; } = new();
    public StudiousEffect Estudioso { get; set; } = new();
    public ForgetfulEffect Esquecido { get; set; } = new();
    public FirmMemoryEffect MemoriaFirme { get; set; } = new();
    public ObserverEffect Observador { get; set; } = new();
    public IndecisiveEffect Indeciso { get; set; } = new();
    public MarathonEffect Maratonista { get; set; } = new();
    public ProcrastinatorEffect Procrastinador { get; set; } = new();
}

/// <summary>
/// Aquisicao e perda de traits por evidencia (P4d). Relogio: DIAS JOGADOS (dias locais em que a
/// pessoa jogou). Sem jogar, nada anda: a evidencia nao enfraquece e a confirmacao nao avanca.
/// Evidencia e evidencia contraria saem de acontecimentos especificos de cada trait e perdem forca
/// com meia-vida em dias jogados. Avaliadores so para traits cujos acontecimentos sao simulados.
/// </summary>
public sealed class QuirkAcquisitionDefinition
{
    public bool Enabled { get; set; } = true;
    /// <summary>Meia-vida da evidencia, em dias jogados.</summary>
    public int HalfLifePlayDays { get; set; } = 21;
    /// <summary>Dias jogados seguidos com a condicao valendo para adquirir ou perder trait adquirida.</summary>
    public int ConfirmPlayDays { get; set; } = 7;
    /// <summary>Dias jogados seguidos para perder uma trait inicial (evidencia contraria forte e sustentada).</summary>
    public int InitialConfirmPlayDays { get; set; } = 21;
    /// <summary>No maximo uma aquisicao a cada isto de dias jogados.</summary>
    public int PlayDaysBetweenAcquisitions { get; set; } = 30;
    /// <summary>Depois de ganhar ou perder uma trait, ela nao muda de novo antes disto (dias jogados).</summary>
    public int CooldownPlayDays { get; set; } = 30;
    public AcquisitionRuleDefinition Persistente { get; set; } = new();
    public AcquisitionRuleDefinition Rancoroso { get; set; } = new() { SecondChanceValence = 150 };
}

/// <summary>
/// Regra de uma trait (milesimos de ponto; cada acontecimento vale 1000). Taxa = evidencia /
/// (evidencia + contraria), em permille, sobre as oportunidades recentes (as duas somadas).
/// </summary>
public sealed class AcquisitionRuleDefinition
{
    /// <summary>Adquirir: evidencia recente minima e taxa minima.</summary>
    public int AcquireEvidence { get; set; } = 3000;
    public int AcquireRatePermille { get; set; } = 750;
    /// <summary>Perder (trait adquirida): oportunidades recentes minimas e taxa maxima.</summary>
    public int LoseMinOpportunities { get; set; } = 3000;
    public int LoseRatePermille { get; set; } = 350;
    /// <summary>Perder (trait inicial): exige mais oportunidades e taxa menor.</summary>
    public int LoseInitialMinOpportunities { get; set; } = 6000;
    public int LoseInitialRatePermille { get; set; } = 200;
    /// <summary>Rancoroso: experiencia minima (valencia) que conta como segunda chance bem-sucedida.</summary>
    public int SecondChanceValence { get; set; }
}

/// <summary>Persistente: depois de um reves, margem maior para trocar de plano; demora mais a abandonar metas.</summary>
public sealed class PersistentEffect
{
    public int SetbackWindowMinutes { get; set; } = 120;
    public int MarginAfterSetbackPermille { get; set; } = 1500;
    public int GoalPatiencePermille { get; set; } = 1500;
}

/// <summary>Impulsivo: margem de troca menor, mais ruido e revisao do plano por impulso.</summary>
public sealed class ImpulsiveEffect
{
    public int MarginPermille { get; set; } = 600;
    public int NoisePermille { get; set; } = 1300;
    public int ImpulseReviewPermille { get; set; } = 250;
}

/// <summary>Colecionador: valor extra para atividade nunca feita e para zona nunca visitada.</summary>
public sealed class CollectorEffect
{
    public int NewActivityBonus { get; set; } = 150;
    public int NewZoneBonus { get; set; } = 75;
}

/// <summary>Otimizador: rendimento pesa mais e gosto menos; diversao conta menos na nota e o tedio vem mais rapido.</summary>
public sealed class OptimizerEffect
{
    public int RewardPermille { get; set; } = 1300;
    public int AffinityPermille { get; set; } = 800;
    public int FunPermille { get; set; } = 700;
    public int BoredomPermille { get; set; } = 1250;
}

/// <summary>Resiliente: a frustracao passa mais rapido; as conquistas marcam menos.</summary>
public sealed class ResilientEffect
{
    public int FrustrationDecayPermille { get; set; } = 1500;
    public int AchievementMemoryPermille { get; set; } = 700;
}

/// <summary>Pavio curto: a frustracao sobe mais rapido e larga mais a sessao com raiva; depois volta mais cedo.</summary>
public sealed class ShortFuseEffect
{
    public int FrustrationGainPermille { get; set; } = 1400;
    public int RageQuitPermille { get; set; } = 1500;
    public int ReturnPausePermille { get; set; } = 500;
}

/// <summary>Saudosista: boas lembrancas duram mais e volta mais depois de sair; novidade fraca frustra mais.</summary>
public sealed class NostalgicEffect
{
    public int PositiveMemoryDecayPermille { get; set; } = 600;
    public int QuitReturnPermille { get; set; } = 1500;
    public int WeakNoveltyPermille { get; set; } = 1300;
}

/// <summary>Rancoroso: lugares que deram errado ficam marcados por mais tempo.</summary>
public sealed class GrudgeEffect
{
    public int NegativeImpressionDecayPermille { get; set; } = 500;
}

/// <summary>Estudioso: missoes e descobertas rendem mais Conhecimento; demora mais nas etapas de visita.</summary>
public sealed class StudiousEffect
{
    public int KnowledgePermille { get; set; } = 1500;
    public int VisitStepPermille { get; set; } = 1250;
}

/// <summary>Esquecido: as capacidades comecam a cair mais cedo sem jogar; reaprende rapido o que ja sabia.</summary>
public sealed class ForgetfulEffect
{
    public int DecayEarlierDays { get; set; } = 3;
    public int RelearnPermille { get; set; } = 700;
}

/// <summary>Memoria firme: as capacidades caem mais devagar sem jogar; custa a dar segunda chance a um lugar.</summary>
public sealed class FirmMemoryEffect
{
    public int DecayPermille { get; set; } = 500;
    public int SecondChancePermille { get; set; } = 750;
}

/// <summary>Observador: ao explorar, pistas do conteudo das zonas vizinhas; explora mais devagar.</summary>
public sealed class ObserverEffect
{
    public int HintsPerExplore { get; set; } = 1;
    public int ExplorePermille { get; set; } = 1200;
}

/// <summary>Indeciso: em empate apertado perde minutos decidindo; decide com menos ruido.</summary>
public sealed class IndecisiveEffect
{
    public int TieGap { get; set; } = 40;
    public int DelayMinutes { get; set; } = 5;
    public int NoisePermille { get; set; } = 700;
}

/// <summary>Maratonista: com a sessao boa, a saciedade cresce mais devagar; depois de uma maratona, o dia seguinte comeca com menos disposicao.</summary>
public sealed class MarathonEffect
{
    public int GoodScoreAbove { get; set; } = 600;
    public int SatietyPermille { get; set; } = 600;
    public int MarathonSessionsPermille { get; set; } = 2000;
    public int NextDayDispositionPenaltyPermille { get; set; } = 150;
}

/// <summary>Procrastinador: adia o comeco da sessao dentro da janela; sem a sessao minima, reconsidera na proxima janela.</summary>
public sealed class ProcrastinatorEffect
{
    public int MinDelayMinutes { get; set; } = 15;
    public int MaxDelayMinutes { get; set; } = 60;
}

/// <summary>Sorteio na criacao da pessoa (P4b). Todos os valores sao candidatos para medicao.</summary>
public sealed class QuirkGenerationDefinition
{
    public bool Enabled { get; set; } = true;
    /// <summary>Pesos de 0, 1, 2... traits (o indice e a quantidade). Preset: 8, 25, 50, 10, 4, 2 e 1%.</summary>
    public List<int> CountWeights { get; set; } = new() { 80, 250, 500, 100, 40, 20, 10 };
    /// <summary>Chance de cada vaga ser negativa (sorteio por vaga, nao proporcao fixa por pessoa).</summary>
    public int NegativeChancePermille { get; set; } = 400;
    /// <summary>Limites do multiplicador de coerencia (permille): nada fica impossivel nem obrigatorio.</summary>
    public int MinMultiplierPermille { get; set; } = 250;
    public int MaxMultiplierPermille { get; set; } = 3000;
    /// <summary>Total de traits por pessoa, contando as adquiridas depois (P4d).</summary>
    public int MaxTotal { get; set; } = 8;
}

public static class QuirkPolarities
{
    public const string Positive = "positive";
    public const string Negative = "negative";
}

public static class QuirkAcquisitions
{
    /// <summary>Nasce com a pessoa (predisposicao).</summary>
    public const string Innate = "innate";
    /// <summary>So adquirida por evidencia, com o sistema correspondente (P4d).</summary>
    public const string Acquired = "acquired";
    public const string Both = "both";
    public static readonly string[] All = { Innate, Acquired, Both };
}

/// <summary>Sistema de que o efeito depende; vazio = efeitos da pessoa (decisao, humor, memoria, rotina).</summary>
public static class QuirkSystems
{
    public const string Person = "";
    public const string Combat = "M3";
    public const string Economy = "M5";
    public const string Groups = "M6";
    public const string Pvp = "etapa3";
    public const string Crafting = "fabricacao";
    public static readonly string[] All = { Person, Combat, Economy, Groups, Pvp, Crafting };
}

public sealed class QuirkDefinition : IContentDefinition
{
    public string Id { get; set; } = "";
    public int Version { get; set; } = 1;
    public string Name { get; set; } = "";
    /// <summary>positive ou negative (explicita).</summary>
    public string Polarity { get; set; } = QuirkPolarities.Positive;
    /// <summary>innate, acquired ou both.</summary>
    public string Acquisition { get; set; } = QuirkAcquisitions.Innate;
    /// <summary>Entra no sorteio da criacao. Falso: so no catalogo (definicao pendente) ou so adquirida.</summary>
    public bool Drawn { get; set; } = true;
    /// <summary>Sistema de que o efeito depende (QuirkSystems).</summary>
    public string System { get; set; } = QuirkSystems.Person;
    /// <summary>Peso base no sorteio dentro da classificacao.</summary>
    public int Weight { get; set; } = 1000;
    /// <summary>Condicao e efeito, para a ficha.</summary>
    public string Effect { get; set; } = "";
    /// <summary>Contrapartida, para a ficha.</summary>
    public string Tradeoff { get; set; } = "";
    /// <summary>Coerencia com a pessoa: aumentam ou reduzem a chance (multiplicador limitado).</summary>
    public List<QuirkAffinityDefinition> Affinities { get; set; } = new();
    /// <summary>Traits contraditorias (valem nos dois sentidos).</summary>
    public List<string> Excludes { get; set; } = new();
    /// <summary>Personalidades principais contraditorias ou redundantes.</summary>
    public List<string> ExcludesPersonalities { get; set; } = new();
}

/// <summary>
/// Afinidade de uma trait com a pessoa. Chaves:
/// - "traits.x": atributo (posicao no preset); direction "high" ou "low"; o termo vai de
///   1000 - forca (extremo oposto) a 1000 + forca (extremo da direcao);
/// - "preferences.x": preferencia (posicao no preset), igual;
/// - "profile.x" e "personality.x": se a pessoa tem esse perfil ou personalidade, termo 1000 + forca
///   (a forca pode ser negativa).
/// O multiplicador e o produto dos termos, limitado por min e max do sorteio.
/// </summary>
public sealed class QuirkAffinityDefinition
{
    public string Key { get; set; } = "";
    public string Direction { get; set; } = "high";
    public int StrengthPermille { get; set; }
}

namespace Simulation.Core.Population;

/// <summary>Estados de ciclo de vida da pessoa em relacao ao MMORPG (plano 4.3).</summary>
public enum LifecycleState
{
    PotentialInterested = 0,
    Registered = 1,
    Offline = 2,
    Online = 3,
    Inactive = 4,
    Quit = 5,
    Banned = 6,
}

/// <summary>
/// Parametros individuais. Comportamentais em permille (0..1000, D-006);
/// demais com unidade no nome. Nao misturar nivel, personalidade e habilidade.
/// </summary>
public sealed class PersonTraits
{
    // Afinidades de estilo de jogo
    public int Pve;
    public int Pvp;
    public int Trade;
    public int Exploration;
    public int Socializing;

    // Personalidade
    public int Sociability;
    public int Aggressiveness;
    public int Patience;
    public int Loyalty;
    public int RiskAversion;

    // Capacidade
    public int MechanicalSkill;
    public int GameKnowledge;
    public int Coordination;

    // Conduta
    public int CheatPropensity;
    public int Opportunism;

    // Financas (dinheiro ficticio do tycoon, separado do ouro do MMORPG)
    public int LeisureBudgetCentsPerMonth;
    public int WillingnessToPay;
    public int PaidAdvantageSensitivity;

    // Rotina
    public int PreferredStartHour;
    public int SessionMinutes;
    public int DaysPerWeek;
    public int WeekendBoost;

    // Jogador (D-036): talento da pessoa (nao do personagem). Define o potencial e a velocidade
    // de aprendizado das capacidades (mecanica, conhecimento, coordenacao). Nao muda.
    public int Talent;

    /// <summary>Tracos que mudam com a experiencia (D-036), pela chave do conteudo.</summary>
    public static readonly string[] DynamicKeys =
    {
        "pve", "pvp", "trade", "exploration", "socializing",
        "sociability", "aggressiveness", "patience", "loyalty", "riskAversion",
        "mechanicalSkill", "gameKnowledge", "coordination", "cheatPropensity", "opportunism",
    };

    public int Get(string key) => key switch
    {
        "pve" => Pve, "pvp" => Pvp, "trade" => Trade, "exploration" => Exploration, "socializing" => Socializing,
        "sociability" => Sociability, "aggressiveness" => Aggressiveness, "patience" => Patience, "loyalty" => Loyalty,
        "riskAversion" => RiskAversion, "mechanicalSkill" => MechanicalSkill, "gameKnowledge" => GameKnowledge,
        "coordination" => Coordination, "cheatPropensity" => CheatPropensity, "opportunism" => Opportunism,
        _ => throw new ArgumentException($"Traço desconhecido: {key}"),
    };

    internal void Set(string key, int value)
    {
        switch (key)
        {
            case "pve": Pve = value; break;
            case "pvp": Pvp = value; break;
            case "trade": Trade = value; break;
            case "exploration": Exploration = value; break;
            case "socializing": Socializing = value; break;
            case "sociability": Sociability = value; break;
            case "aggressiveness": Aggressiveness = value; break;
            case "patience": Patience = value; break;
            case "loyalty": Loyalty = value; break;
            case "riskAversion": RiskAversion = value; break;
            case "mechanicalSkill": MechanicalSkill = value; break;
            case "gameKnowledge": GameKnowledge = value; break;
            case "coordination": Coordination = value; break;
            case "cheatPropensity": CheatPropensity = value; break;
            case "opportunism": Opportunism = value; break;
            default: throw new ArgumentException($"Traço desconhecido: {key}");
        }
    }

    public PersonTraits Clone() => (PersonTraits)MemberwiseClone();
}

/// <summary>
/// Estado da pessoa em relacao ao jogo (D-038), em permille. Satisfacao: media movel das
/// notas das sessoes. Frustracao: reveses e sessoes ruins, passa com o tempo. Novidade: gasta
/// com o tempo de jogo, renovada por descobertas. Engajamento e intencao de continuar derivam
/// deles (com lealdade e investimento).
/// </summary>
public sealed class PersonMood
{
    public int Satisfaction = 500;
    public int Frustration;
    public int Novelty = 1000;
    public int Engagement = 500;
    public int ReturnIntention = 500;
    /// <summary>Progresso esperado, em permille do XP de referencia do nivel.</summary>
    public int ProgressExpectation = 550;
    /// <summary>Progresso da ultima sessao (permille da referencia).</summary>
    public int LastProgressRatio;
    public int LastSessionScore = 500;
    public int SessionsRated;
}

/// <summary>Acontecimento marcante na memoria da pessoa (D-038; plano 4.5).</summary>
public sealed class Memory
{
    public Memory(string kind, string subject, int valence, int intensity, long tick)
    {
        Kind = kind;
        Subject = subject;
        Valence = valence;
        Intensity = intensity;
        Tick = tick;
        Count = 1;
    }

    /// <summary>Tipo (ex.: level_milestone, main_quest, goal_abandoned, great_session).</summary>
    public string Kind { get; }
    /// <summary>Do que se trata (nivel, missao, "zona/atividade"); "" quando geral.</summary>
    public string Subject { get; }
    /// <summary>Bom (+) ou ruim (-).</summary>
    public int Valence { get; internal set; }
    /// <summary>Forca atual da lembranca (cai por dia; frustracoes caem mais rapido).</summary>
    public int Intensity { get; internal set; }
    public long Tick { get; internal set; }
    /// <summary>Quantas vezes se repetiu.</summary>
    public int Count { get; internal set; }
}

/// <summary>
/// Pessoa simulada: identidade, personalidade, rotina, financas e relacoes.
/// Separada de Conta (adesao ao MMORPG) e Personagem (classe, nivel, inventario).
/// </summary>
public sealed class Person
{
    public Person(int id, string name, string profileId, ulong appearanceSeed, PersonTraits traits)
    {
        Id = id;
        Name = name;
        ProfileId = profileId;
        AppearanceSeed = appearanceSeed;
        Traits = traits;
        BaseTraits = traits.Clone();
    }

    public int Id { get; }
    public string Name { get; }
    /// <summary>Perfil de origem (predefinicao); a pessoa nao e um clone dele.</summary>
    public string ProfileId { get; }
    /// <summary>Semente estavel da aparencia, derivada do ID: mesma pessoa, mesmo visual.</summary>
    public ulong AppearanceSeed { get; }
    /// <summary>Tracos atuais: mudam com a experiencia, dentro de limites em torno da base (D-036).</summary>
    public PersonTraits Traits { get; }
    /// <summary>Tracos de nascenca (imutaveis): referencia dos limites e da comparacao "era X".</summary>
    public PersonTraits BaseTraits { get; }
    /// <summary>Pratica acumulada por direcao e traco ("+mechanicalSkill" = minutos ate o proximo ponto).</summary>
    internal SortedDictionary<string, int> Practice { get; } = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, int> PracticeProgress => Practice;
    public PersonMood Mood { get; } = new();
    /// <summary>Acontecimentos marcantes (limitados; os mais fracos saem), D-038.</summary>
    internal List<Memory> MemoryList { get; } = new();
    public IReadOnlyList<Memory> Memories => MemoryList;
    /// <summary>Impressao de cada lugar ("zona/atividade"), -1000..1000; pesa nas escolhas.</summary>
    internal SortedDictionary<string, int> Impressions { get; } = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, int> PlaceImpressions => Impressions;
    /// <summary>O que tem agradado (+) ou incomodado (-): componentes das sessoes, esmaecendo.</summary>
    internal SortedDictionary<string, int> Drivers { get; } = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, int> MoodDrivers => Drivers;
    public LifecycleState State { get; internal set; } = LifecycleState.PotentialInterested;
    /// <summary>Conta no MMORPG (0 = ainda nao registrada).</summary>
    public int AccountId { get; internal set; }
    /// <summary>Personagem ativo (0 = nenhum). Um por pessoa nesta fase (plano 4.1).</summary>
    public int CharacterId { get; internal set; }
    /// <summary>Ultimo dia (indice) em que jogou; -1 = nunca.</summary>
    public long LastPlayedDay { get; internal set; } = -1;
    public bool IsOnline => State == LifecycleState.Online;
    /// <summary>Dia em que desistiu (-1 = nunca) e quantas vezes desistiu e voltou (D-039).</summary>
    public long QuitDay { get; internal set; } = -1;
    public int TimesQuit { get; internal set; }
    public int TimesReturned { get; internal set; }
    public int TimesInactive { get; internal set; }
}

/// <summary>Conta no MMORPG (plano 4.1). Criada no registro (M2).</summary>
public sealed class Account
{
    public Account(int id, int personId, long createdTick)
    {
        Id = id;
        PersonId = personId;
        CreatedTick = createdTick;
    }

    public int Id { get; }
    public int PersonId { get; }
    public long CreatedTick { get; }
    public int ActiveCharacterId { get; internal set; }
}

/// <summary>O que o personagem esta fazendo no mundo.</summary>
public enum CharacterStatus
{
    Offline = 0,
    Idle = 1,
    Traveling = 2,
    Doing = 3,
}

/// <summary>Contribuicao calculada de um fator na decisao (permille, com sinal).</summary>
public sealed class DecisionFactor
{
    public DecisionFactor(string key, int contribution)
    {
        Key = key;
        Contribution = contribution;
    }

    /// <summary>Chave estavel (ex.: affinity, risk, travel); a interface traduz.</summary>
    public string Key { get; }
    public int Contribution { get; }
}

/// <summary>Ultima decisao de atividade: o que escolheu, onde e por que (fatores reais).</summary>
public sealed class DecisionRecord
{
    public DecisionRecord(long tick, string regionId, string activityId, int utility, int optionsConsidered,
        IReadOnlyList<DecisionFactor> factors, string runnerUpActivityKey)
    {
        Tick = tick;
        RegionId = regionId;
        ActivityId = activityId;
        Utility = utility;
        OptionsConsidered = optionsConsidered;
        Factors = factors;
        RunnerUpActivityKey = runnerUpActivityKey;
    }

    public long Tick { get; }
    public string RegionId { get; }
    public string ActivityId { get; }
    public int Utility { get; }
    /// <summary>Seguiu o plano da sessao embora outra opcao tivesse nota maior (D-037).</summary>
    public bool ContinuedPlan { get; init; }
    /// <summary>Vantagem da melhor opcao sobre a escolhida quando seguiu o plano.</summary>
    public int PlanGap { get; init; }
    public int OptionsConsidered { get; }
    public IReadOnlyList<DecisionFactor> Factors { get; }
    /// <summary>Segunda melhor opcao ("regiao/atividade"), vazio se nao houve.</summary>
    public string RunnerUpActivityKey { get; }
}

/// <summary>Quest em andamento de um personagem (D-031).</summary>
public sealed class ActiveQuest
{
    public ActiveQuest(string questId, long acceptedTick, long acceptedPlayedTicks)
    {
        QuestId = questId;
        AcceptedTick = acceptedTick;
        AcceptedPlayedTicks = acceptedPlayedTicks;
    }

    public string QuestId { get; }
    public long AcceptedTick { get; }
    /// <summary>Tempo conectado do personagem ao aceitar (para medir quanto jogou ate concluir).</summary>
    public long AcceptedPlayedTicks { get; }
    /// <summary>Etapa atual; igual ao numero de etapas = pronta para entregar no quadro.</summary>
    public int StepIndex { get; internal set; }
    /// <summary>Minutos ja cumpridos na etapa atual.</summary>
    public int StepMinutes { get; internal set; }
}

/// <summary>
/// Plano da sessao (D-037): a intencao da sessao entre a meta pessoal e a atividade executada.
/// Formado na primeira decisao; revisto por vantagem clara de outra opcao (com margem e
/// compromisso minimo) ou por acontecimentos relevantes.
/// </summary>
public sealed class SessionPlan
{
    public SessionPlan(string focusKey, string focusType, string reason, long formedTick, long commitUntilTick)
    {
        FocusKey = focusKey;
        FocusType = focusType;
        Reason = reason;
        FormedTick = formedTick;
        CommitUntilTick = commitUntilTick;
    }

    /// <summary>"zona/atividade" em foco.</summary>
    public string FocusKey { get; }
    public string FocusType { get; }
    /// <summary>Fator que mais pesou ao formar ou trocar o plano (ex.: goal, quest_step, affinity).</summary>
    public string Reason { get; }
    public long FormedTick { get; }
    /// <summary>Ate quando a troca exige a margem maior (compromisso minimo).</summary>
    public long CommitUntilTick { get; }
    /// <summary>Ciclos seguidos no foco desde que o plano foi formado.</summary>
    public int Cycles { get; internal set; }
}

/// <summary>Experiencia acumulada da sessao em andamento (D-038).</summary>
public sealed class SessionLedger
{
    public long XpAtStart { get; private set; }
    public int LevelAtStart { get; private set; }
    /// <summary>Minutos em atividade (a viagem fica de fora; ela pesa no progresso).</summary>
    public int Minutes { get; internal set; }
    /// <summary>Conquistas menos reveses da sessao.</summary>
    public int Points { get; internal set; }
    /// <summary>Soma de cada componente da experiencia ponderada pelos minutos.</summary>
    internal SortedDictionary<string, long> Sums { get; } = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, long> ComponentSums => Sums;

    internal void Reset(long xpAtStart, int levelAtStart)
    {
        XpAtStart = xpAtStart;
        LevelAtStart = levelAtStart;
        Minutes = 0;
        Points = 0;
        Sums.Clear();
    }
}

/// <summary>Tipos de objetivo pessoal (D-036).</summary>
public static class GoalTypes
{
    public const string ReachLevel = "reach_level";
    public const string DiscoverZone = "discover_zone";
    public const string CompleteQuest = "complete_quest";
    public const string PracticeActivity = "practice_activity";
}

/// <summary>
/// Objetivo pessoal (D-026/D-036): algo que a pessoa quer conseguir, escolhido pelo perfil e
/// pela personalidade. Influencia as decisoes com o peso do comprometimento; nao obriga.
/// </summary>
public sealed class PersonalGoal
{
    public PersonalGoal(string type, string target, int targetValue, int startValue, int commitment,
        string motiveKey, int motiveValue, long setTick, long setPlayedTicks)
    {
        Type = type;
        Target = target;
        TargetValue = targetValue;
        StartValue = startValue;
        Commitment = commitment;
        MotiveKey = motiveKey;
        MotiveValue = motiveValue;
        SetTick = setTick;
        LastProgressPlayedTicks = setPlayedTicks;
    }

    public string Type { get; }
    /// <summary>Alvo: zona, quest ou tipo de atividade (vazio para nivel).</summary>
    public string Target { get; }
    /// <summary>Valor alvo: nivel desejado ou minutos de pratica.</summary>
    public int TargetValue { get; }
    public int StartValue { get; }
    /// <summary>Quanto a pessoa se apega ao objetivo (permille): paciencia e lealdade.</summary>
    public int Commitment { get; }
    /// <summary>Traco que mais pesou na escolha (ex.: exploration).</summary>
    public string MotiveKey { get; }
    public int MotiveValue { get; }
    public long SetTick { get; }
    /// <summary>Progresso acumulado (minutos de pratica; etapas da quest).</summary>
    public int Progress { get; internal set; }
    /// <summary>Tempo conectado do personagem no ultimo avanco (abandono por falta de progresso).</summary>
    public long LastProgressPlayedTicks { get; internal set; }
}

/// <summary>Historico de uma quest para o personagem.</summary>
public sealed class QuestRecord
{
    public int Completions { get; internal set; }
    /// <summary>Dia (indice do calendario) da ultima conclusao.</summary>
    public long LastCompletedDay { get; internal set; } = -1;
}

/// <summary>Personagem jogavel (plano 4.1). Criado no registro; combate e inventario no M3/M5.</summary>
public sealed class Character
{
    public Character(int id, int accountId, string name, string classId)
    {
        Id = id;
        AccountId = accountId;
        Name = name;
        ClassId = classId;
    }

    public int PersonId { get; internal set; }
    public CharacterStatus Status { get; internal set; } = CharacterStatus.Offline;
    /// <summary>Regiao e atividade do plano atual (destino, se viajando).</summary>
    public string TargetRegionId { get; internal set; } = "";
    public string ActivityId { get; internal set; } = "";
    /// <summary>Posicao atual (parado) ou inicio do trecho em andamento.</summary>
    public World.MapPos Position { get; internal set; }
    /// <summary>Fim do trecho em andamento (valido quando Status = Traveling).</summary>
    public World.MapPos MoveTo { get; internal set; }
    public long MoveStartTick { get; internal set; }
    public long MoveEndTick { get; internal set; }
    /// <summary>Zona em que entra ao concluir o trecho ("" = continua na mesma).</summary>
    public string MoveEntersZone { get; internal set; } = "";
    /// <summary>Pontos seguintes ate o destino (portoes e ponto da atividade).</summary>
    internal List<Behavior.Waypoint> Waypoints { get; } = new();
    public IReadOnlyList<Behavior.Waypoint> RemainingWaypoints => Waypoints;

    /// <summary>Posicao no instante informado (interpolacao inteira do trecho atual).</summary>
    public World.MapPos PositionAt(long tick)
    {
        if (Status != CharacterStatus.Traveling || MoveEndTick <= MoveStartTick) return Position;
        long t = Math.Clamp(tick - MoveStartTick, 0, MoveEndTick - MoveStartTick);
        long span = MoveEndTick - MoveStartTick;
        return new World.MapPos(
            Position.X + (int)((MoveTo.X - Position.X) * t / span),
            Position.Y + (int)((MoveTo.Y - Position.Y) * t / span));
    }
    public long SessionStartTick { get; internal set; }
    public long ActivityStartTick { get; internal set; }
    public long ActivityEndTick { get; internal set; }
    public long SessionEndTick { get; internal set; }
    public bool LogoutPending { get; internal set; }
    /// <summary>A sessao atual ja foi estendida por estar indo bem (D-039).</summary>
    public bool SessionExtended { get; internal set; }
    /// <summary>Locais que o personagem conhece (so estes entram nas decisoes).</summary>
    internal SortedSet<string> Known { get; } = new(StringComparer.Ordinal);
    public IReadOnlyCollection<string> KnownRegions => Known;
    public int DecisionCount { get; internal set; }
    public string LastActivityKey { get; internal set; } = "";
    public int RepeatCount { get; internal set; }
    public DecisionRecord? LastDecision { get; internal set; }

    // ---- Estatisticas de uso (validacao e fichas) ----
    /// <summary>Tempo conectado acumulado (ticks), fechado a cada saida.</summary>
    public long PlayedTicks { get; internal set; }
    /// <summary>Tempo em atividade por tipo (ticks); "idle" = sem opcao viavel. O resto do tempo conectado e deslocamento.</summary>
    internal SortedDictionary<string, long> ActivityTicks { get; } = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, long> ActivityTicksByType => ActivityTicks;
    /// <summary>Tempo conectado (ticks) no instante em que atingiu cada nivel; indice 0 = nivel 2.</summary>
    internal List<long> LevelTicks { get; } = new();
    public IReadOnlyList<long> PlayedTicksAtLevel => LevelTicks;

    // ---- Quests (D-031) ----
    internal List<ActiveQuest> ActiveQuestList { get; } = new();
    public IReadOnlyList<ActiveQuest> ActiveQuests => ActiveQuestList;
    internal SortedDictionary<string, QuestRecord> QuestLog { get; } = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, QuestRecord> QuestHistory => QuestLog;
    /// <summary>Diarias concluidas hoje por quadro (zera ao mudar o dia).</summary>
    internal SortedDictionary<string, int> DailyByBoard { get; } = new(StringComparer.Ordinal);
    public long DailyDay { get; internal set; } = -1;
    /// <summary>XP vindo de quests e de atividades (medida de XP por fonte).</summary>
    public long XpFromQuests { get; internal set; }
    public long XpFromActivities { get; internal set; }

    /// <summary>Minutos em atividade por zona (familiaridade, D-036a).</summary>
    internal SortedDictionary<string, int> ZoneMinutes { get; } = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, int> ZoneMinutesSpent => ZoneMinutes;

    // ---- Experiencia da sessao em andamento (D-038) ----
    public SessionLedger SessionLedger { get; } = new();

    // ---- Plano da sessao e tedio (D-037) ----
    public SessionPlan? Plan { get; internal set; }
    /// <summary>Acontecimento que pede revisao livre do plano na proxima decisao ("" = nenhum).</summary>
    public string PlanReviewReason { get; internal set; } = "";
    /// <summary>Tedio por tipo de atividade (permille).</summary>
    internal SortedDictionary<string, int> Boredom { get; } = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, int> BoredomByType => Boredom;
    public int PlansFormed { get; internal set; }
    public int PlanChanges { get; internal set; }

    // ---- Objetivos pessoais (D-036) ----
    internal List<PersonalGoal> GoalList { get; } = new();
    public IReadOnlyList<PersonalGoal> Goals => GoalList;
    public int GoalsSet { get; internal set; }
    public int GoalsCompleted { get; internal set; }
    public int GoalsAbandoned { get; internal set; }

    /// <summary>Tempo conectado ate o instante informado, incluindo a sessao em andamento.</summary>
    public long PlayedTicksAt(long now) =>
        PlayedTicks + (Status != CharacterStatus.Offline ? Math.Max(0, now - SessionStartTick) : 0);

    public int Id { get; }
    public int AccountId { get; }
    public string Name { get; }
    public string ClassId { get; }
    public int Level { get; internal set; } = 1;
    public long Experience { get; internal set; }
    /// <summary>Ouro em cobre (inteiro). Ouro do MMORPG, nunca dinheiro da empresa.</summary>
    public long GoldCopper { get; internal set; }
    /// <summary>Regiao onde o personagem esta (ou de onde partiu, se viajando).</summary>
    public string RegionId { get; internal set; } = "";
}

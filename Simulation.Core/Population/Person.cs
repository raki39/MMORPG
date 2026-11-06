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
    // Especialidade: afinidade desenvolvida e familiaridade (cresce com a pratica). O gosto em si
    // fica nas Preferencias; aqui so um reforco minimo e limitado na escolha.
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
    /// <summary>Preferencia por variedade (D-039a): cansa de repetir e precisa de novidade; separada da paciencia e da exploracao.</summary>
    public int Variety;

    // Capacidade
    public int MechanicalSkill;
    public int GameKnowledge;
    public int Coordination;

    // Conduta
    public int CheatPropensity;
    public int Opportunism;
    /// <summary>
    /// Competitividade (permille): motivacao para disputar, superar outros e buscar reconhecimento
    /// pelo desempenho. Nao decide agressividade, PK, habilidade nem horas jogadas.
    /// </summary>
    public int Competitiveness;

    // Financas (dinheiro ficticio do tycoon, separado do ouro do MMORPG)
    public int LeisureBudgetCentsPerMonth;
    public int WillingnessToPay;
    public int PaidAdvantageSensitivity;

    // Rotina (P2): horario preferido para jogar (local) e duracao habitual; a frequencia vem da
    // importancia do jogo, do habito e do tempo livre das obrigacoes.
    public int PreferredStartHour;
    /// <summary>Cronotipo (R6): 0 = muito matinal, 1000 = muito notivago.</summary>
    public int Chronotype;
    public int SessionMinutes;
    /// <summary>Importancia do jogo entre os lazeres (permille): o principal diferencial do "hardcore", nao o tempo livre.</summary>
    public int GameImportance;
    /// <summary>Habito (permille): regularidade; quem jogou ontem tende a jogar hoje, quem faltou tende a faltar.</summary>
    public int Habit;
    /// <summary>Tendencia a prolongar a sessao (permille, P3): separada da importancia do jogo.</summary>
    public int ExtendTendency;
    /// <summary>Autocontrole (permille, P3): respeitar o horario planejado e o trabalho.</summary>
    public int SelfControl;

    // Jogador (D-036): talento da pessoa (nao do personagem). Define o potencial e a velocidade
    // de aprendizado das capacidades (mecanica, conhecimento, coordenacao). Nao muda.
    public int Talent;

    /// <summary>Tracos que mudam com a experiencia (D-036), pela chave do conteudo.</summary>
    public static readonly string[] DynamicKeys =
    {
        "pve", "pvp", "trade", "exploration", "socializing",
        "sociability", "aggressiveness", "patience", "loyalty", "riskAversion",
        "mechanicalSkill", "gameKnowledge", "coordination", "cheatPropensity", "opportunism", "variety",
        "competitiveness",
    };

    public int Get(string key) => key switch
    {
        "pve" => Pve, "pvp" => Pvp, "trade" => Trade, "exploration" => Exploration, "socializing" => Socializing,
        "sociability" => Sociability, "aggressiveness" => Aggressiveness, "patience" => Patience, "loyalty" => Loyalty,
        "riskAversion" => RiskAversion, "mechanicalSkill" => MechanicalSkill, "gameKnowledge" => GameKnowledge,
        "coordination" => Coordination, "cheatPropensity" => CheatPropensity, "opportunism" => Opportunism,
        "variety" => Variety, "competitiveness" => Competitiveness, "chronotype" => Chronotype,
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
            case "variety": Variety = value; break;
            case "chronotype": Chronotype = value; break;
            case "competitiveness": Competitiveness = value; break;
            default: throw new ArgumentException($"Traço desconhecido: {key}");
        }
    }

    public PersonTraits Clone() => (PersonTraits)MemberwiseClone();
}

/// <summary>
/// Preferencias: interesses pessoais (permille, 0..1000), separados do perfil (dedicacao) e dos
/// atributos. Dao o peso principal de gosto nas decisoes, nunca uma obrigacao. Estaveis: nao
/// mudam com a pratica (quem muda e a especialidade). Crafting ainda nao tem atividade no jogo.
/// </summary>
public sealed class PersonPreferences
{
    public int Pve;
    public int Pvp;
    /// <summary>Fabricar (sem efeito ate existir a fabricacao). Nao implica gostar de vender nem de coletar.</summary>
    public int Crafting;
    public int Trade;
    /// <summary>Coleta de materiais, separada do comercio e da fabricacao.</summary>
    public int Gathering;
    public int Exploration;
    public int Social;

    /// <summary>Chaves do conteudo, na ordem fixa de sorteio.</summary>
    public static readonly string[] Keys = { "pve", "pvp", "crafting", "trade", "gathering", "exploration", "social" };

    public int Get(string key) => key switch
    {
        "pve" => Pve, "pvp" => Pvp, "crafting" => Crafting, "trade" => Trade, "gathering" => Gathering,
        "exploration" => Exploration, "social" => Social,
        _ => throw new ArgumentException($"Preferência desconhecida: {key}"),
    };

    internal void Set(string key, int value)
    {
        switch (key)
        {
            case "pve": Pve = value; break;
            case "pvp": Pvp = value; break;
            case "crafting": Crafting = value; break;
            case "trade": Trade = value; break;
            case "gathering": Gathering = value; break;
            case "exploration": Exploration = value; break;
            case "social": Social = value; break;
            default: throw new ArgumentException($"Preferência desconhecida: {key}");
        }
    }
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
    /// <summary>Fracao de minutos ainda nao descontada da novidade (sessoes curtas acumulam).</summary>
    public int NoveltyDecayCarry;
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
    public string Name { get; internal set; }
    /// <summary>Perfil de origem (predefinicao); a pessoa nao e um clone dele.</summary>
    public string ProfileId { get; }
    /// <summary>Semente estavel da aparencia, derivada do ID: mesma pessoa, mesmo visual.</summary>
    public ulong AppearanceSeed { get; }
    /// <summary>Tracos atuais: mudam com a experiencia, dentro de limites em torno da base (D-036).</summary>
    public PersonTraits Traits { get; }
    /// <summary>Tracos de nascenca (imutaveis): referencia dos limites e da comparacao "era X".</summary>
    public PersonTraits BaseTraits { get; }
    /// <summary>Preferencias (interesses pessoais), ao lado do perfil; estaveis.</summary>
    public PersonPreferences Preferences { get; internal set; } = new();
    /// <summary>Personalidade principal (P4a): id do tipo, "equilibrado" ou vazio (desligada).</summary>
    public string PersonalityId { get; internal set; } = "";
    /// <summary>Mudanca em confirmacao: alvo e dia em que comecou (vazio = nenhuma).</summary>
    public string PersonalityPending { get; internal set; } = "";
    public long PersonalityPendingSince { get; internal set; }
    internal List<PersonalityChange> PersonalityHistoryList { get; } = new();
    /// <summary>Mudancas de personalidade: anterior, nova e o atributo que motivou.</summary>
    public IReadOnlyList<PersonalityChange> PersonalityHistory => PersonalityHistoryList;

    /// <summary>Prefixo das chaves de motivo que sao preferencias ("pref_pve"); as demais sao atributos.</summary>
    public const string PreferenceMotivePrefix = "pref_";

    /// <summary>Valor de um motivo (meta, causa de evento): preferencia ("pref_x") ou atributo ("x").</summary>
    public int MotiveValue(string key) => key.StartsWith(PreferenceMotivePrefix, StringComparison.Ordinal)
        ? Preferences.Get(key[PreferenceMotivePrefix.Length..])
        : Traits.Get(key);
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
    // ---- Localidade (P1, D-042) ----
    /// <summary>Localidade de residencia: fuso e feriados.</summary>
    public string ResidenceId { get; internal set; } = "";
    /// <summary>Nacionalidade (contexto; nao decide horario, renda nem personalidade).</summary>
    public string NationalityId { get; internal set; } = "";
    /// <summary>Idiomas falados (ordem ordinal); o social do M6 usa.</summary>
    public IReadOnlyList<string> Languages { get; internal set; } = Array.Empty<string>();
    /// <summary>Diferenca do horario local para o do servidor (minutos).</summary>
    public int LocalOffsetMinutes { get; internal set; }

    // ---- Rotina de vida (P2, D-043) ----
    public string OccupationId { get; internal set; } = "";
    /// <summary>Variacao individual do horario das obrigacoes (minutos, +-).</summary>
    public int ShiftOffsetMinutes { get; internal set; }
    /// <summary>Deslocamento em relacao ao da ocupacao (permille).</summary>
    public int CommutePermille { get; internal set; } = 1000;
    /// <summary>Folga fixa da escala (0 = segunda .. 6 = domingo; -1 = sem).</summary>
    public int DayOffIndex { get; internal set; } = -1;
    /// <summary>Segunda folga fixa (escala 5x2: o dia seguinte a primeira); -1 se nao houver.</summary>
    public int DayOff2Index { get; internal set; } = -1;
    public int SleepNeedMinutes { get; internal set; } = 480;
    /// <summary>Hora preferida de dormir (minutos desde a meia-noite anterior; acima de 1440 = madrugada).</summary>
    public int BedtimeMinute { get; internal set; } = 1425;
    /// <summary>Vespera de folga (R6): minutos a mais na hora de dormir quando o dia seguinte e livre (0 = nao desloca).</summary>
    public int FreeEveShiftMinutes { get; internal set; }
    /// <summary>Sono deslocado (R6): acorda neste instante, depois do acordar planejado; 0 = nada deslocado.</summary>
    public long SleepUntilTick { get; internal set; }
    /// <summary>Dia local cujo acordar ja foi deslocado (uma vez por noite).</summary>
    public long SleepShiftedDay { get; internal set; } = -1;

    /// <summary>Traits (P4b): de 0 a 6 na criacao, em ordem de sorteio. Efeitos na P4c; aquisicao e perda na P4d.</summary>
    internal List<QuirkState> QuirkList { get; } = new();
    public IReadOnlyList<QuirkState> Quirks => QuirkList;
    /// <summary>Atuacoes das traits (P4c), por chave: medicao e ficha.</summary>
    internal SortedDictionary<string, int> QuirkStatsMap { get; } = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, int> QuirkStats => QuirkStatsMap;
    /// <summary>Restos das multiplicacoes inteiras das traits (milesimos), por chave.</summary>
    internal SortedDictionary<string, int> QuirkCarryMap { get; } = new(StringComparer.Ordinal);
    /// <summary>Maratonista (P4c): dias locais recentes (ate 14) em que a sessao passou do dobro da normal.</summary>
    internal SortedSet<long> MarathonDaysSet { get; } = new();
    public IReadOnlyCollection<long> MarathonDays => MarathonDaysSet;
    /// <summary>Sessoes encerradas com raiva (frustracao), com ou sem trait.</summary>
    public int RageQuits { get; internal set; }
    /// <summary>Dias locais jogados (relogio da aquisicao e perda de traits, P4d).</summary>
    public int PlayDayCount { get; internal set; }
    /// <summary>Dia jogado (PlayDayCount) da ultima aquisicao; -1 = nenhuma.</summary>
    public int LastAcquisitionPlayDay { get; internal set; } = -1;
    /// <summary>Evidencia de cada trait com avaliador (P4d).</summary>
    internal SortedDictionary<string, QuirkEvidence> QuirkEvidenceMap { get; } = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, QuirkEvidence> QuirkEvidenceByTrait => QuirkEvidenceMap;
    /// <summary>Historico de aquisicoes e perdas (preservado ao perder e readquirir).</summary>
    internal List<QuirkChange> QuirkHistoryList { get; } = new();
    public IReadOnlyList<QuirkChange> QuirkHistory => QuirkHistoryList;
    /// <summary>
    /// Objetivos pessoais (O1): ambicoes da pessoa naquele MMORPG, separadas das metas do personagem.
    /// Ficam na pessoa (sobrevivem a personagens futuros). Gerais primeiro, por importancia.
    /// </summary>
    internal List<PersonObjective> ObjectiveList { get; } = new();
    public IReadOnlyList<PersonObjective> Objectives => ObjectiveList;
    /// <summary>Historico dos objetivos: definicao, conclusao, mudanca de prioridade, abandono (motivo registrado).</summary>
    internal List<ObjectiveChange> ObjectiveHistoryList { get; } = new();
    public IReadOnlyList<ObjectiveChange> ObjectiveHistory => ObjectiveHistoryList;
    /// <summary>Dias jogados (relogio da evolucao dos objetivos, O3).</summary>
    public int ObjectivePlayDays { get; internal set; }
    /// <summary>Dia jogado da ultima mudanca escolhida nos objetivos (-1 = nenhuma): intervalo entre mudancas.</summary>
    public int LastObjectiveChangePlayDay { get; internal set; } = -1;
    /// <summary>Acontecimento que pede revisao dos objetivos no proximo dia jogado ("" = nenhum).</summary>
    public string ObjectiveTrigger { get; internal set; } = "";
    /// <summary>Vezes em que a pessoa buscou um objetivo novo e nao achou nenhum relevante (falta de conteudo), e o ultimo dia.</summary>
    public int ObjectiveGaps { get; internal set; }
    public long LastObjectiveGapDay { get; internal set; } = -1;
    /// <summary>Renda do trabalho e apoio familiar (centavos/mes; sem efeito ate o M7).</summary>
    public long IncomeCentsPerMonth { get; internal set; }
    public long SupportCentsPerMonth { get; internal set; }
    // ---- Sono (P3, D-044) ----
    /// <summary>Tolerancia temporaria ao deficit (permille): escala o teto do deficit.</summary>
    public int SleepDebtTolerance { get; internal set; } = 500;
    /// <summary>Deficit de sono acumulado (minutos): sobe com noites curtas, cai com sono a mais.</summary>
    public int SleepDeficitMinutes { get; internal set; }
    /// <summary>
    /// Recuperacao da manha decidida para cada dia local (R2): fixada na primeira vez que o dia e
    /// planejado (planejamento do dia ou primeira sessao depois do registro ou da volta), para o
    /// dia montado nunca mudar depois. Dia passado que nunca foi planejado: sem recuperacao.
    /// </summary>
    internal SortedDictionary<long, int> RecoveryByDay { get; } = new();
    /// <summary>
    /// Corte de sono ja usado no periodo de sono que comeca em SleepCutBedTick (R3): o teto por noite
    /// e compartilhado por todas as sessoes que afetam o mesmo periodo.
    /// </summary>
    public long SleepCutBedTick { get; internal set; } = -1;
    public int SleepCutUsedMinutes { get; internal set; }
    /// <summary>Ultima saida do jogo (tick): se passou da hora de deitar, o sono real comeca nela (R1).</summary>
    public long LastLogoutTick { get; internal set; } = -1;
    /// <summary>Sono perdido para o jogo: na ultima noite e no total.</summary>
    public int LastSleepCutMinutes { get; internal set; }
    public int SleepCutTotalMinutes { get; internal set; }
    /// <summary>Minutos de jogo esperados numa semana tipica (rotina e importancia, sem humor).</summary>
    public int ExpectedWeeklyMinutes { get; internal set; }
    /// <summary>
    /// Horario habitual de comecar (R4): o viavel pela rotina e pela pressa de comecar (D-045),
    /// calculado na geracao. O gosto pessoal fica separado, em Traits.PreferredStartHour.
    /// </summary>
    public int HabitualStartHour { get; internal set; } = 20;
    /// <summary>
    /// Saciedade (v3, D-051), 0..1000: cresce com o tempo jogado (todo ele, inclusive o interrompido)
    /// e cai nas pausas; zera ao acordar. Pesa na vontade de parar, de continuar e de voltar.
    /// </summary>
    public int Satiety { get; internal set; }
    /// <summary>Ultima atualizacao da saciedade (tick): jogando desde entao cresce; fora do jogo, cai.</summary>
    public long SatietyTick { get; internal set; }
    /// <summary>A ultima sessao foi interrompida pelo trabalho (v3): vontade de terminar na proxima decisao.</summary>
    public bool Unfinished { get; internal set; }
    /// <summary>Ultimo dia local em que comecou uma sessao (informativo; o habito usa a agenda, R4).</summary>
    public long LastPlayedLocalDay { get; internal set; } = -1;
    /// <summary>Agenda dos ultimos dias (mais recente no fim): tempo livre, sessoes e motivo.</summary>
    internal List<DayPlanRecord> PlanList { get; } = new();
    public IReadOnlyList<DayPlanRecord> DayPlans => PlanList;
    /// <summary>Janelas livres planejadas (ticks do servidor) de hoje e de ontem: limite da sessao.</summary>
    internal List<(long Start, long End, bool EndsAtBed)> OpenWindows { get; } = new();

    /// <summary>Ultimo dia (indice) em que jogou; -1 = nunca.</summary>
    public long LastPlayedDay { get; internal set; } = -1;
    public bool IsOnline => State == LifecycleState.Online;
    /// <summary>Dia em que desistiu (-1 = nunca) e quantas vezes desistiu e voltou (D-039).</summary>
    public long QuitDay { get; internal set; } = -1;
    /// <summary>Ultimo dia em que voltou (de inativo ou desistente); conta como presenca para a inatividade (D-039a).</summary>
    public long LastReturnDay { get; internal set; } = -1;
    /// <summary>Memorias ate este instante ja motivaram uma volta por saudade (nao contam de novo).</summary>
    public long NostalgiaSpentTick { get; internal set; } = -1;
    public int NostalgiaReturns { get; internal set; }
    public int TimesQuit { get; internal set; }
    public int TimesReturned { get; internal set; }
    public int TimesInactive { get; internal set; }
}

/// <summary>Agenda de um dia local (P2): tempo livre, janelas, sessoes planejadas e o motivo.</summary>
public sealed class DayPlanRecord
{
    /// <summary>Compromisso do dia livre (D-046): id e horario local; vazio se nao houve.</summary>
    public string CommitmentId { get; }
    /// <summary>Recuperacao decidida para este dia (a montagem do dia fica a mesma o dia todo).</summary>
    public int RecoveryMinutes { get; }
    public int CommitmentStart { get; }
    public int CommitmentEnd { get; }

    public DayPlanRecord(long localDay, int freeMinutes, int windows, int sessions, string reason, int firstStartMinute, int plannedMinutes, bool freeDay,
        int sleepMinutes = 0, int deficitMinutes = 0, int workSessions = 0, int workOverlapMinutes = 0,
        string commitmentId = "", int commitmentStart = 0, int commitmentEnd = 0, int recoveryMinutes = 0,
        int sessionPriority = 0, int dispositionPermille = 0, int regularityPermille = 500)
    {
        SessionPriority = sessionPriority;
        DispositionPermille = dispositionPermille;
        RegularityPermille = regularityPermille;
        RecoveryMinutes = recoveryMinutes;
        CommitmentId = commitmentId;
        CommitmentStart = commitmentStart;
        CommitmentEnd = commitmentEnd;
        SleepMinutes = sleepMinutes;
        DeficitMinutes = deficitMinutes;
        WorkSessions = workSessions;
        WorkOverlapMinutes = workOverlapMinutes;
        LocalDay = localDay;
        FreeMinutes = freeMinutes;
        Windows = windows;
        Sessions = sessions;
        Reason = reason;
        FirstStartMinute = firstStartMinute;
        PlannedMinutes = plannedMinutes;
        FreeDay = freeDay;
    }

    public long LocalDay { get; }
    public int FreeMinutes { get; }
    public int Windows { get; }
    public int Sessions { get; }
    /// <summary>played, no_free_time, other_leisure, low_engagement, inactive, came_back.</summary>
    public string Reason { get; }
    /// <summary>Minuto local de inicio da primeira sessao (-1 = nenhuma).</summary>
    public int FirstStartMinute { get; }
    public int PlannedMinutes { get; }
    /// <summary>Dia sem obrigacoes (folga, fim de semana, feriado ou sem ocupacao).</summary>
    public bool FreeDay { get; }
    /// <summary>Sono da noite anterior (planejado) e deficit depois da conta do dia (P3).</summary>
    public int SleepMinutes { get; internal set; }
    /// <summary>Minutos que acordou depois do planejado por ter ido dormir mais tarde (sono deslocado, R6).</summary>
    public int SleepShiftMinutes { get; internal set; }
    public int DeficitMinutes { get; internal set; }
    /// <summary>Sessoes no expediente remoto (pausa ou escapada) e minutos sobrepostos ao trabalho (D-044).</summary>
    public int WorkSessions { get; }
    public int WorkOverlapMinutes { get; }
    /// <summary>Prioridade do jogo no dia (v3, 0..1000): importancia e tempo livre, para continuar e voltar.</summary>
    public int SessionPriority { get; }
    /// <summary>Disposicao do dia (R4, +-permille): sorteio proprio por pessoa e dia.</summary>
    public int DispositionPermille { get; }
    /// <summary>Regularidade recente no planejamento (R4, 0..1000; 500 = sem dias para medir).</summary>
    public int RegularityPermille { get; }
    /// <summary>Minutos jogados nas sessoes planejadas para este dia (todas, inclusive no trabalho).</summary>
    public int PlayedMinutes { get; internal set; }
    /// <summary>
    /// O que aconteceu no dia (v3): sessoes normais jogadas, voltas depois de uma pausa, voltas
    /// recusadas e a sessao da noite deixada de lado porque ja tinha jogado no trabalho.
    /// </summary>
    public int Sittings { get; internal set; }
    public int Returns { get; internal set; }
    public int DeclinedReturns { get; internal set; }
    public int SkippedAfterWork { get; internal set; }
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
    /// <summary>Chegar ao nivel que libera uma atividade desejada (D-055). Target = "zona/atividade".</summary>
    public const string UnlockLevel = "unlock_level";
}

/// <summary>
/// Evidencia de uma trait (P4d), em milesimos de ponto: comportamentos observados a favor e contra,
/// e as oportunidades (acontecimentos em que a trait podia se manifestar). Enfraquece por dia jogado.
/// </summary>
public sealed class QuirkEvidence
{
    public int For { get; internal set; }
    public int Against { get; internal set; }
    /// <summary>Contagens brutas (medicao): oportunidades, a favor, contra e o que so aconteceu pelo efeito da trait.</summary>
    public int Opportunities { get; internal set; }
    public int ForCount { get; internal set; }
    public int AgainstCount { get; internal set; }
    public int EffectOnlyCount { get; internal set; }
    /// <summary>Dias jogados seguidos com a condicao de mudanca valendo, e qual mudanca.</summary>
    public int Streak { get; internal set; }
    public string Pending { get; internal set; } = "";
    /// <summary>Dia jogado da ultima mudanca desta trait (-1 = nunca).</summary>
    public int LastChangePlayDay { get; internal set; } = -1;
}

/// <summary>
/// Objetivo pessoal de uma pessoa (O1). Geral ou secundario pela importancia para ela; o horizonte e
/// a viabilidade sao caracteristicas separadas (o horizonte vem do catalogo; a viabilidade e
/// calculada por ObjectiveSystem.Assess, sem guardar progresso ficticio).
/// </summary>
public sealed class PersonObjective
{
    public PersonObjective(string id, string param, string tier, int importance, int commitment, string origin, long day, IReadOnlyList<(string Key, int Value)> motives)
    {
        Id = id;
        Param = param;
        Tier = tier;
        Importance = importance;
        Commitment = commitment;
        Origin = origin;
        Day = day;
        Motives = motives;
    }

    public string Id { get; }
    /// <summary>Parametro do objetivo ("" = nenhum): item do equipamento raro, especialidade de fabricacao.</summary>
    public string Param { get; }
    /// <summary>general ou secondary (importancia para a pessoa).</summary>
    public string Tier { get; internal set; }
    /// <summary>Importancia para a pessoa (permille).</summary>
    public int Importance { get; internal set; }
    /// <summary>Comprometimento (permille): dedicacao, paciencia e lealdade. Uso na O2.</summary>
    public int Commitment { get; internal set; }
    public string State { get; internal set; } = ObjectiveStates.Active;
    /// <summary>initial (a pessoa ja tinha ao entrar) ou o acontecimento que o originou (O3).</summary>
    public string Origin { get; }
    /// <summary>Dia (indice do calendario) em que passou a valer.</summary>
    public long Day { get; }
    /// <summary>O que mais pesou na escolha (afinidade ou ambicao) e quanto (termo - 1000).</summary>
    public IReadOnlyList<(string Key, int Value)> Motives { get; }
    /// <summary>Dia em que foi concluido, ou em que a condicao foi alcancada pela primeira vez (-1 = nunca).</summary>
    public long CompletedDay { get; internal set; } = -1;
    /// <summary>Dia em que saiu dos ativos por perda, substituicao ou abandono, e o motivo (O3).</summary>
    public long EndedDay { get; internal set; } = -1;
    public int EndedPlayDay { get; internal set; } = -1;
    public string EndReason { get; internal set; } = "";
    /// <summary>Ultimo progresso medido e o dia jogado em que mudou (estagnacao, O3).</summary>
    public int LastProgress { get; internal set; } = -1;
    public int LastProgressPlayDay { get; internal set; } = -1;
    /// <summary>Metas ligadas por avanco real abandonadas em seguida (experiencia negativa, O3).</summary>
    public int FailureStreak { get; internal set; }
    /// <summary>Vezes em que a condicao foi alcancada e perdida (O3).</summary>
    public int TimesHeld { get; internal set; }
    public int TimesLost { get; internal set; }
    /// <summary>Ajuste da pessoa ao objetivo na adocao (base da mudanca de interesse, D-066).</summary>
    public int FitAtSet { get; internal set; }
    /// <summary>Dias jogados seguidos com interesse baixo, com experiencia negativa ligada, deixado de lado sem melhora e com o motivo passado (D-066).</summary>
    public int LowInterestDays { get; internal set; }
    public int NegativeDays { get; internal set; }
    public int SetAsideDays { get; internal set; }
    public int RecoverDays { get; internal set; }
    /// <summary>Metas ligadas a ele (O2): adotadas, concluidas e abandonadas.</summary>
    public int LinkedGoalsSet { get; internal set; }
    public int LinkedGoalsCompleted { get; internal set; }
    public int LinkedGoalsAbandoned { get; internal set; }
}

public static class ObjectiveStates
{
    public const string Active = "active";
    /// <summary>Deixado de lado (prioridade menor, substituicao voluntaria): sem frustracao; pode voltar.</summary>
    public const string Paused = "paused";
    /// <summary>Conquista definitiva concluida: fica concluida.</summary>
    public const string Completed = "completed";
    /// <summary>Condicao alcancada e mantida agora (sai dos ativos; e acompanhada).</summary>
    public const string Held = "held";
    /// <summary>Condicao perdida depois de alcancada: a conquista passada continua registrada; pode querer recuperar.</summary>
    public const string Lost = "lost";
    /// <summary>Abandonado por experiencia negativa.</summary>
    public const string Abandoned = "abandoned";
}

/// <summary>Mudanca de objetivo: dia, objetivo, parametro, mudanca ("set" na O1), importancia e motivo.</summary>
public sealed record ObjectiveChange(long Day, string Id, string Param, string Change, string Tier, string Reason);

/// <summary>Aquisicao ou perda de trait: dia local, dia jogado, trait, "acquired"/"lost", origem anterior e evidencia.</summary>
public sealed record QuirkChange(long Day, int PlayDay, string Id, string Change, string PreviousSource, int For, int Against, string Reason);

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
    /// <summary>Reveses que a pessoa sofreu com esta meta ativa (P4d: oportunidade de persistir).</summary>
    public int SetbacksDuring { get; internal set; }
    /// <summary>A meta so continuou porque o efeito do Persistente prolongou o prazo (nao conta como evidencia).</summary>
    public bool SurvivedByEffect { get; internal set; }
    /// <summary>
    /// Objetivos para os quais a meta contribui de verdade (O2), com o motivo: avanco real ou
    /// preparacao por requisito conhecido. Vazia = meta independente (necessidade, oportunidade, vontade).
    /// </summary>
    internal List<ObjectiveLink> LinkList { get; } = new();
    public IReadOnlyList<ObjectiveLink> Links => LinkList;
    /// <summary>Tempo conectado em que a meta ficou impedida (D-066; -1 = nao esta impedida).</summary>
    public long BlockedSincePlayedTicks { get; internal set; } = -1;
}

/// <summary>Vinculo de uma meta com um objetivo (O2): tipo (advance ou preparation) e de onde vem (zona, missao, requisito).</summary>
public sealed record ObjectiveLink(string ObjectiveId, string Kind, string Subject);

public static class ObjectiveLinkKinds
{
    /// <summary>A meta avanca o criterio do objetivo (nivel, missao principal, zona).</summary>
    public const string Advance = "advance";
    /// <summary>A meta cumpre parte de um requisito conhecido (nivel minimo do chefe, do PvP, do item...).</summary>
    public const string Preparation = "preparation";
}

/// <summary>Historico de uma quest para o personagem.</summary>
/// <summary>
/// Retrato de uma meta de descoberta bloqueada (D-067a): nivel, quantas zonas que revelam a zona a pessoa
/// conhecia, pistas sobre ela e missoes ativas com etapa num lugar que a revela.
/// </summary>
public sealed class ZoneBlockSnapshot
{
    public ZoneBlockSnapshot(int level, int knownReveal, int hints, string quests)
    {
        Level = level;
        KnownReveal = knownReveal;
        Hints = hints;
        Quests = quests;
    }

    public int Level { get; }
    public int KnownReveal { get; }
    public int Hints { get; }
    /// <summary>Ids das missoes, em ordem ordinal, separados por virgula.</summary>
    public string Quests { get; }
}

public sealed class QuestRecord
{
    public int Completions { get; internal set; }
    /// <summary>Dia (indice do calendario) da ultima conclusao.</summary>
    public long LastCompletedDay { get; internal set; } = -1;
    /// <summary>Vezes que a missao foi largada (D-066) e o dia do servidor da ultima vez (-1 = nunca).</summary>
    public int Drops { get; internal set; }
    public long DroppedDay { get; internal set; } = -1;
    /// <summary>Progresso guardado ao largar, se o conteudo manda guardar (-1 = nenhum).</summary>
    public int SavedStepIndex { get; internal set; } = -1;
    public int SavedStepMinutes { get; internal set; }
    /// <summary>Diaria que expirou sem nenhuma etapa feita (D-067): nao e aceita de novo ate a pessoa fazer a atividade da etapa.</summary>
    public bool IdleExpired { get; internal set; }
}

/// <summary>
/// Personagem jogavel (plano 4.1): classe, nivel, XP, estatisticas (calculadas pela classe e o nivel,
/// Combat.CharacterStats) e vida e recurso atuais. Criado no registro; combate e inventario depois.
/// </summary>
public sealed class Character
{
    /// <summary>Ultimo reves (instante; -1 = nenhum): o Persistente mantem o plano depois dele (P4c).</summary>
    public long LastSetbackTick { get; internal set; } = -1;
    /// <summary>Indeciso (P4c): ja hesitou nesta decisao (a proxima decide sem atraso).</summary>
    public bool Hesitated { get; internal set; }
    /// <summary>Indeciso (P4c): dupla de opcoes da ultima hesitacao nesta sessao (nao hesita de novo por ela).</summary>
    public string LastHesitationPair { get; internal set; } = "";
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
    /// <summary>
    /// Limite absoluto da sessao (R2): nada passa dele, nem terminar a atividade, nem terminar o
    /// trecho de viagem, nem a extensao, nem o chamado do trabalho. Fim da janela livre; na ultima
    /// janela do dia, mais o corte de sono autorizado. Na pausa ou na escapada, o fim planejado.
    /// </summary>
    public long SessionHardEndTick { get; internal set; }
    /// <summary>Dia local da agenda a que a sessao pertence (-1 = nenhum): conta o tempo e as voltas desse dia.</summary>
    public long SessionPlanDay { get; internal set; } = -1;
    /// <summary>Por que a sessao vai terminar (v3): satisfied, window_end, bedtime... vazio = ainda nao decidido.</summary>
    public string StopReason { get; internal set; } = "";
    /// <summary>Vezes que decidiu continuar depois da sessao normal e os minutos acrescentados (v3).</summary>
    public int Continues { get; internal set; }
    public int ContinuedMinutes { get; internal set; }
    public bool LogoutPending { get; internal set; }
    /// <summary>Tipo da sessao atual (D-044): 0 normal, 1 pausa permitida do trabalho remoto, 2 escapada no expediente.</summary>
    public int SessionKind { get; internal set; }
    /// <summary>Extensoes desta sessao (P3) e minutos acrescentados.</summary>
    public int SessionExtensions { get; internal set; }
    public int ExtendedMinutes { get; internal set; }
    /// <summary>Instante em que a extensao comeca a consumir o sono (0 = nao consome) e o motivo.</summary>
    public long SleepCutStartTick { get; internal set; }
    public string ExtendMotive { get; internal set; } = "";
    /// <summary>Locais que o personagem conhece (so estes entram nas decisoes).</summary>
    internal SortedSet<string> Known { get; } = new(StringComparer.Ordinal);
    public IReadOnlyCollection<string> KnownRegions => Known;
    /// <summary>
    /// Pistas de conteudo (D-055): "zona/atividade" de zonas ainda desconhecidas sobre as quais a
    /// pessoa recebeu informacao valida (hoje, as etapas de missoes aceitas). Sem pista, ela nao
    /// sabe o que ha numa zona desconhecida.
    /// </summary>
    internal SortedSet<string> Hints { get; } = new(StringComparer.Ordinal);
    public IReadOnlyCollection<string> ContentHints => Hints;
    /// <summary>
    /// Metas de nivel abandonadas ("tipo|alvo|valor") que nao voltam com o mesmo alvo antes de
    /// uma mudanca relevante: a proxima subida de nivel limpa a lista (D-055).
    /// </summary>
    internal SortedSet<string> BlockedLevelGoals { get; } = new(StringComparer.Ordinal);
    public IReadOnlyCollection<string> BlockedGoals => BlockedLevelGoals;
    /// <summary>Metas largadas ("tipo|alvo") que nao voltam ate avanco ou mudanca relevante (D-066).</summary>
    internal SortedSet<string> BlockedTargetSet { get; } = new(StringComparer.Ordinal);
    public IReadOnlyCollection<string> BlockedTargets => BlockedTargetSet;
    /// <summary>
    /// Zonas com meta de descoberta bloqueada (D-067a): o retrato do momento do bloqueio, para saber se
    /// houve mudanca concreta (nivel que muda um requisito, rota, pista ou missao no caminho).
    /// </summary>
    internal SortedDictionary<string, ZoneBlockSnapshot> ZoneBlockMap { get; } = new(StringComparer.Ordinal);
    /// <summary>
    /// Missoes que a pessoa esperava pegar quando decidiu ir ao quadro (ids separados por virgula; vazio: nenhuma
    /// ou outra atividade). Se na chegada nada acontece, o motivo de cada uma fica registrado (D-067a).
    /// </summary>
    public string BoardExpectation { get; internal set; } = "";
    public IReadOnlyDictionary<string, ZoneBlockSnapshot> ZoneBlocks => ZoneBlockMap;
    public int DecisionCount { get; internal set; }
    public string LastActivityKey { get; internal set; } = "";
    public int RepeatCount { get; internal set; }
    /// <summary>Minutos seguidos fazendo a atividade de LastActivityKey nesta sessao (Inquieto, P4a).</summary>
    public int RepeatMinutes { get; internal set; }
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
    /// <summary>Metas adotadas por tipo (medicao).</summary>
    internal SortedDictionary<string, int> GoalsSetByTypeMap { get; } = new(StringComparer.Ordinal);
    public IReadOnlyDictionary<string, int> GoalsSetByType => GoalsSetByTypeMap;
    /// <summary>Metas ligadas a objetivos (O2): adotadas, concluidas, abandonadas; e as que tinham vinculo de preparacao.</summary>
    public int GoalsLinkedSet { get; internal set; }
    public int GoalsLinkedCompleted { get; internal set; }
    public int GoalsLinkedAbandoned { get; internal set; }
    public int GoalsPreparationSet { get; internal set; }

    /// <summary>Tempo conectado ate o instante informado, incluindo a sessao em andamento.</summary>
    public long PlayedTicksAt(long now) =>
        PlayedTicks + (Status != CharacterStatus.Offline ? Math.Max(0, now - SessionStartTick) : 0);

    public int Id { get; }
    public int AccountId { get; }
    public string Name { get; }
    public string ClassId { get; }
    public int Level { get; internal set; } = 1;
    public long Experience { get; internal set; }
    /// <summary>Vida e recurso atuais (Etapa 1). Fora de combate: vida cheia; recurso cheio ou vazio (furia).</summary>
    public int CurrentHealth { get; internal set; }
    public int CurrentResource { get; internal set; }
    /// <summary>Ouro em cobre (inteiro). Ouro do MMORPG, nunca dinheiro da empresa.</summary>
    public long GoldCopper { get; internal set; }
    /// <summary>Regiao onde o personagem esta (ou de onde partiu, se viajando).</summary>
    public string RegionId { get; internal set; } = "";
}

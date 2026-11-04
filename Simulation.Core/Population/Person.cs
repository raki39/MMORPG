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
}

/// <summary>Estado emocional/engajamento, variavel ao longo da campanha (permille).</summary>
public sealed class PersonMood
{
    public int Satisfaction = 500;
    public int Fatigue;
    public int Frustration;
    public int Engagement = 500;
    public int ReturnIntention = 500;
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
    }

    public int Id { get; }
    public string Name { get; }
    /// <summary>Perfil de origem (predefinicao); a pessoa nao e um clone dele.</summary>
    public string ProfileId { get; }
    /// <summary>Semente estavel da aparencia, derivada do ID: mesma pessoa, mesmo visual.</summary>
    public ulong AppearanceSeed { get; }
    public PersonTraits Traits { get; }
    public PersonMood Mood { get; } = new();
    public LifecycleState State { get; internal set; } = LifecycleState.PotentialInterested;
    /// <summary>Conta no MMORPG (0 = ainda nao registrada).</summary>
    public int AccountId { get; internal set; }
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

/// <summary>Personagem jogavel (plano 4.1). Criado no registro (M2); combate e inventario no M3/M5.</summary>
public sealed class Character
{
    public Character(int id, int accountId, string name, string classId)
    {
        Id = id;
        AccountId = accountId;
        Name = name;
        ClassId = classId;
    }

    public int Id { get; }
    public int AccountId { get; }
    public string Name { get; }
    public string ClassId { get; }
    public int Level { get; internal set; } = 1;
    public long Experience { get; internal set; }
    /// <summary>Ouro em cobre (inteiro). Ouro do MMORPG, nunca dinheiro da empresa.</summary>
    public long GoldCopper { get; internal set; }
    public string RegionId { get; internal set; } = "";
}

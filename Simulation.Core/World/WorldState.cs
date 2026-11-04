using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Events;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Scheduling;
using Simulation.Core.Time;

namespace Simulation.Core.World;

/// <summary>Estado do servidor simulado do MMORPG (aberto/fechado ao publico).</summary>
public sealed class ServerState
{
    public bool IsOpen { get; internal set; }
    /// <summary>Tick da ultima abertura (-1 = nunca aberto).</summary>
    public long LastOpenedTick { get; internal set; } = -1;
}

/// <summary>Contadores do dia corrente, fechados pelo rollup diario.</summary>
public sealed class DayCounters
{
    public int CommandsApplied;
    public int CommandsRejected;
    public int EventsEmitted;
    public int Registrations;
    public int SessionsStarted;
    public int ActivePlayers;
    public int PeakOnline;
    public int ActivitiesChosen;
    public int LevelUps;
    public long PlayMinutes;

    internal void Reset(int onlineNow)
    {
        CommandsApplied = 0;
        CommandsRejected = 0;
        EventsEmitted = 0;
        Registrations = 0;
        SessionsStarted = 0;
        ActivePlayers = 0;
        PeakOnline = onlineNow;
        ActivitiesChosen = 0;
        LevelUps = 0;
        PlayMinutes = 0;
    }
}

/// <summary>Fechamento de um dia simulado (base das series de relatorio).</summary>
public sealed class DailyRecord
{
    public long DayIndex { get; set; }
    public DateTime Date { get; set; }
    public DayKind DayKind { get; set; }
    public long ClosedAtTick { get; set; }
    public int PersonsTotal { get; set; }
    public int PotentialInterested { get; set; }
    public int Registered { get; set; }
    public int Online { get; set; }
    public bool ServerOpen { get; set; }
    public int CommandsApplied { get; set; }
    public int CommandsRejected { get; set; }
    public int EventsEmitted { get; set; }
    public int Registrations { get; set; }
    public int SessionsStarted { get; set; }
    /// <summary>Pessoas distintas que jogaram no dia (DAU).</summary>
    public int ActivePlayers { get; set; }
    public int PeakOnline { get; set; }
    public int ActivitiesChosen { get; set; }
    public int LevelUps { get; set; }
    public long PlayMinutes { get; set; }
}

/// <summary>
/// Estado autoritativo completo da campanha, em memoria. Somente o motor e os
/// comandos alteram este estado; a apresentacao le por consultas/snapshots.
/// </summary>
public sealed class WorldState
{
    private readonly List<Person> _persons = new();
    private readonly List<Account> _accounts = new();
    private readonly List<Character> _characters = new();
    private readonly List<DailyRecord> _history = new();

    internal WorldState(ContentDatabase content, ulong seed, CampaignCalendar calendar)
    {
        Content = content;
        Seed = seed;
        Clock = new SimClock(calendar);
        Rng = new RngStreams(seed);
    }

    public ContentDatabase Content { get; }
    public ulong Seed { get; }
    public SimClock Clock { get; }
    public RngStreams Rng { get; }
    public EventScheduler Scheduler { get; } = new();
    public CommandQueue Commands { get; } = new();
    public EventLog Events { get; } = new();
    public ServerState Server { get; } = new();
    public DayCounters Today { get; } = new();

    /// <summary>Pessoas em ordem de ID (indice = ID - 1).</summary>
    public IReadOnlyList<Person> Persons => _persons;
    public IReadOnlyList<Account> Accounts => _accounts;
    public IReadOnlyList<Character> Characters => _characters;
    public IReadOnlyList<DailyRecord> History => _history;

    public Person GetPerson(int id) => _persons[id - 1];
    public Character GetCharacter(int id) => _characters[id - 1];

    /// <summary>Sistema de comportamento (definido pelo motor na criacao).</summary>
    internal Behavior.BehaviorSystem Behavior { get; set; } = null!;

    /// <summary>Pessoas conectadas agora.</summary>
    public int OnlineCount { get; internal set; }

    /// <summary>
    /// Personagens fazendo ou indo fazer cada atividade ("regiao/atividade"). Base da lotacao
    /// percebida nas decisoes. Ordenado para hash estavel.
    /// </summary>
    internal SortedDictionary<string, int> Occupancy { get; } = new(StringComparer.Ordinal);

    public int OccupancyOf(string regionId, string activityId) =>
        Occupancy.TryGetValue(ActivityKey(regionId, activityId), out var n) ? n : 0;

    public static string ActivityKey(string regionId, string activityId) => regionId + "/" + activityId;

    internal void AddOccupancy(string regionId, string activityId, int delta)
    {
        if (activityId.Length == 0) return;
        string key = ActivityKey(regionId, activityId);
        int n = (Occupancy.TryGetValue(key, out var v) ? v : 0) + delta;
        if (n < 0) throw new InvalidOperationException($"Ocupação negativa em {key}.");
        if (n == 0) Occupancy.Remove(key); else Occupancy[key] = n;
    }

    internal Account AddAccount(int personId, long tick)
    {
        var a = new Account(_accounts.Count + 1, personId, tick);
        _accounts.Add(a);
        return a;
    }

    internal Character AddCharacter(int accountId, int personId, string name, string classId, string regionId)
    {
        var c = new Character(_characters.Count + 1, accountId, name, classId) { PersonId = personId, RegionId = regionId };
        _characters.Add(c);
        return c;
    }

    internal void AddPerson(Person p)
    {
        if (p.Id != _persons.Count + 1)
            throw new InvalidOperationException($"IDs de pessoa devem ser sequenciais (esperado {_persons.Count + 1}, recebido {p.Id}).");
        _persons.Add(p);
    }

    internal void AddDailyRecord(DailyRecord r) => _history.Add(r);

    /// <summary>Emite evento relevante no tick atual e conta no dia.</summary>
    public SimEvent Emit(string type, EventImportance importance,
        IReadOnlyList<EntityRef>? entities = null, IReadOnlyList<EventCause>? causes = null, string? detail = null)
    {
        Today.EventsEmitted++;
        return Events.Emit(Clock.Tick, type, importance, entities, causes, detail);
    }
}

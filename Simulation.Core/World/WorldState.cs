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

    internal void Reset()
    {
        CommandsApplied = 0;
        CommandsRejected = 0;
        EventsEmitted = 0;
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

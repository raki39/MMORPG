namespace Simulation.Core.Events;

/// <summary>
/// Registro de eventos relevantes. Dentro do tick so acumula em memoria; o host
/// (runner ou jogo) drena em lote fora do tick e grava onde quiser (CSV, SQLite).
/// Nunca descarta eventos: se ninguem drenar, o buffer cresce e BackpressureWarning liga.
/// </summary>
public sealed class EventLog
{
    private readonly List<SimEvent> _pending = new();

    public EventLog(int softCapacity = 100_000)
    {
        SoftCapacity = softCapacity;
    }

    public int SoftCapacity { get; }
    public long NextEventId { get; private set; } = 1;
    public long TotalEmitted { get; private set; }
    public int PendingCount => _pending.Count;
    public bool BackpressureWarning => _pending.Count > SoftCapacity;

    public SimEvent Emit(long tick, string type, EventImportance importance,
        IReadOnlyList<EntityRef>? entities = null, IReadOnlyList<EventCause>? causes = null, string? detail = null)
    {
        var ev = new SimEvent(NextEventId++, tick, type, importance,
            entities ?? Array.Empty<EntityRef>(), causes ?? Array.Empty<EventCause>(), detail);
        _pending.Add(ev);
        TotalEmitted++;
        return ev;
    }

    /// <summary>Entrega os eventos pendentes, em ordem de ID, e esvazia o buffer.</summary>
    public IReadOnlyList<SimEvent> Drain()
    {
        if (_pending.Count == 0) return Array.Empty<SimEvent>();
        var batch = _pending.ToArray();
        _pending.Clear();
        return batch;
    }
}

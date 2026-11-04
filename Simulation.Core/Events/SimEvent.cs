namespace Simulation.Core.Events;

public enum EventImportance
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3,
}

/// <summary>Referencia a uma entidade por tipo + ID estavel (nunca por nome).</summary>
public readonly struct EntityRef
{
    public EntityRef(string kind, long id)
    {
        Kind = kind;
        Id = id;
    }

    public string Kind { get; }
    public long Id { get; }

    public override string ToString() => $"{Kind}:{Id}";
}

/// <summary>
/// Causa calculada de um evento. Peso explica a decisao do modelo; nao e prova estatistica.
/// Pode apontar para um evento de origem ou para o comando do gestor que o provocou.
/// </summary>
public sealed class EventCause
{
    public EventCause(string factor, int weightPermille, long? sourceEventId = null, long? sourceCommandSequence = null)
    {
        Factor = factor;
        WeightPermille = weightPermille;
        SourceEventId = sourceEventId;
        SourceCommandSequence = sourceCommandSequence;
    }

    public string Factor { get; }
    public int WeightPermille { get; }
    public long? SourceEventId { get; }
    public long? SourceCommandSequence { get; }
}

/// <summary>Acontecimento relevante e persistivel da simulacao.</summary>
public sealed class SimEvent
{
    public SimEvent(long id, long tick, string type, EventImportance importance,
        IReadOnlyList<EntityRef> entities, IReadOnlyList<EventCause> causes, string? detail)
    {
        Id = id;
        Tick = tick;
        Type = type;
        Importance = importance;
        Entities = entities;
        Causes = causes;
        Detail = detail;
    }

    public long Id { get; }
    public long Tick { get; }
    public string Type { get; }
    public EventImportance Importance { get; }
    public IReadOnlyList<EntityRef> Entities { get; }
    public IReadOnlyList<EventCause> Causes { get; }
    /// <summary>Texto curto de contexto (dados, nao narrativa final).</summary>
    public string? Detail { get; }
}

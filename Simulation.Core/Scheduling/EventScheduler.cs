namespace Simulation.Core.Scheduling;

/// <summary>
/// Evento agendado. So dados (tipo + alvo + payload inteiro) para poder ser salvo;
/// o comportamento fica no handler registrado para o tipo.
/// </summary>
public sealed class ScheduledEvent
{
    public ScheduledEvent(long tick, long sequence, string type, long targetId, long payload)
    {
        Tick = tick;
        Sequence = sequence;
        Type = type;
        TargetId = targetId;
        Payload = payload;
    }

    public long Tick { get; }
    /// <summary>Ordem de insercao: desempate estavel entre eventos do mesmo tick.</summary>
    public long Sequence { get; }
    public string Type { get; }
    public long TargetId { get; }
    public long Payload { get; }

    public override string ToString() => $"{Type}@{Tick}#{Sequence}(alvo={TargetId}, payload={Payload})";
}

/// <summary>
/// Fila de prioridade por (tick, sequencia). Heap binario proprio para manter
/// ordem totalmente definida e independente de implementacoes da plataforma.
/// </summary>
public sealed class EventScheduler
{
    private readonly List<ScheduledEvent> _heap = new();

    public long NextSequence { get; private set; } = 1;
    public int Count => _heap.Count;

    public ScheduledEvent Schedule(long tick, string type, long targetId = 0, long payload = 0)
    {
        if (tick < 0) throw new ArgumentOutOfRangeException(nameof(tick));
        var ev = new ScheduledEvent(tick, NextSequence++, type, targetId, payload);
        _heap.Add(ev);
        SiftUp(_heap.Count - 1);
        return ev;
    }

    /// <summary>Tick do proximo evento, ou long.MaxValue se vazio.</summary>
    public long PeekTick => _heap.Count == 0 ? long.MaxValue : _heap[0].Tick;

    public bool TryDequeueDue(long tick, out ScheduledEvent ev)
    {
        if (_heap.Count == 0 || _heap[0].Tick > tick)
        {
            ev = null!;
            return false;
        }
        ev = _heap[0];
        int last = _heap.Count - 1;
        _heap[0] = _heap[last];
        _heap.RemoveAt(last);
        if (_heap.Count > 0) SiftDown(0);
        return true;
    }

    /// <summary>Primeiro evento pendente de um tipo (sem ordenar a fila; empate pela sequencia), ou null.</summary>
    public ScheduledEvent? EarliestOf(string type)
    {
        ScheduledEvent? best = null;
        foreach (var e in _heap)
            if (e.Type == type && (best == null || Compare(e, best) < 0)) best = e;
        return best;
    }

    /// <summary>Copia ordenada (para hash, save e inspecao).</summary>
    public List<ScheduledEvent> SnapshotOrdered()
    {
        var copy = new List<ScheduledEvent>(_heap);
        copy.Sort(Compare);
        return copy;
    }

    private static int Compare(ScheduledEvent a, ScheduledEvent b)
    {
        int c = a.Tick.CompareTo(b.Tick);
        return c != 0 ? c : a.Sequence.CompareTo(b.Sequence);
    }

    private void SiftUp(int i)
    {
        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (Compare(_heap[i], _heap[parent]) >= 0) break;
            (_heap[i], _heap[parent]) = (_heap[parent], _heap[i]);
            i = parent;
        }
    }

    private void SiftDown(int i)
    {
        int n = _heap.Count;
        while (true)
        {
            int l = 2 * i + 1, r = l + 1, smallest = i;
            if (l < n && Compare(_heap[l], _heap[smallest]) < 0) smallest = l;
            if (r < n && Compare(_heap[r], _heap[smallest]) < 0) smallest = r;
            if (smallest == i) return;
            (_heap[i], _heap[smallest]) = (_heap[smallest], _heap[i]);
            i = smallest;
        }
    }
}

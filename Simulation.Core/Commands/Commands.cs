using Simulation.Core.World;

namespace Simulation.Core.Commands;

/// <summary>Resultado da validacao de um comando no instante da aplicacao.</summary>
public readonly struct CommandCheck
{
    private CommandCheck(bool ok, string? reason)
    {
        IsValid = ok;
        Reason = reason;
    }

    public bool IsValid { get; }
    public string? Reason { get; }

    public static CommandCheck Ok() => new CommandCheck(true, null);
    public static CommandCheck Reject(string reason) => new CommandCheck(false, reason);
}

/// <summary>Contexto entregue ao comando ao ser aplicado.</summary>
public sealed class CommandContext
{
    internal CommandContext(WorldState world, CommandEnvelope envelope)
    {
        World = world;
        Envelope = envelope;
    }

    public WorldState World { get; }
    public CommandEnvelope Envelope { get; }
    public long Tick => World.Clock.Tick;
}

/// <summary>
/// Comando do gestor (jogador) ou da interface. Unica forma de a apresentacao
/// alterar o estado. Deve ser dado puro + regra; aplicado no limite de tick.
/// </summary>
public interface ISimCommand
{
    /// <summary>Identificador estavel do tipo (usado em logs, replays e saves).</summary>
    string Type { get; }
    /// <summary>Payload legivel e estavel (entra no registro de comandos).</summary>
    string Describe();
    CommandCheck Validate(WorldState world);
    void Apply(CommandContext context);
}

public enum CommandStatus
{
    Pending = 0,
    Applied = 1,
    Rejected = 2,
}

/// <summary>Comando registrado: payload, tick previsto, sequencia e resultado.</summary>
public sealed class CommandEnvelope
{
    internal CommandEnvelope(long sequence, long submittedAtTick, long scheduledTick, ISimCommand command)
    {
        Sequence = sequence;
        SubmittedAtTick = submittedAtTick;
        ScheduledTick = scheduledTick;
        Command = command;
    }

    public long Sequence { get; }
    public long SubmittedAtTick { get; }
    public long ScheduledTick { get; }
    public ISimCommand Command { get; }
    public CommandStatus Status { get; internal set; } = CommandStatus.Pending;
    public long AppliedTick { get; internal set; } = -1;
    public string? RejectionReason { get; internal set; }
}

/// <summary>
/// Fila de comandos ordenada por (tick previsto, sequencia) + historico completo.
/// </summary>
public sealed class CommandQueue
{
    private readonly List<CommandEnvelope> _pending = new();
    private readonly List<CommandEnvelope> _history = new();

    public long NextSequence { get; private set; } = 1;
    public IReadOnlyList<CommandEnvelope> History => _history;
    public int PendingCount => _pending.Count;
    public long PeekTick => _pending.Count == 0 ? long.MaxValue : _pending[0].ScheduledTick;

    internal CommandEnvelope Enqueue(ISimCommand command, long submittedAtTick, long scheduledTick)
    {
        var env = new CommandEnvelope(NextSequence++, submittedAtTick, scheduledTick, command);
        // Insercao ordenada estavel: depois de todos com tick <= scheduledTick.
        int idx = _pending.Count;
        while (idx > 0 && _pending[idx - 1].ScheduledTick > scheduledTick) idx--;
        _pending.Insert(idx, env);
        _history.Add(env);
        return env;
    }

    internal bool TryDequeueDue(long tick, out CommandEnvelope env)
    {
        if (_pending.Count == 0 || _pending[0].ScheduledTick > tick)
        {
            env = null!;
            return false;
        }
        env = _pending[0];
        _pending.RemoveAt(0);
        return true;
    }

    public IReadOnlyList<CommandEnvelope> PendingOrdered() => _pending.ToArray();
}

using Simulation.Core.Events;
using Simulation.Core.World;

namespace Simulation.Core.Commands;

/// <summary>Abre ou fecha o servidor simulado do MMORPG ao publico (plano 3.2).</summary>
public sealed class SetServerOpenCommand : ISimCommand
{
    public const string TypeId = "set_server_open";

    public SetServerOpenCommand(bool open)
    {
        Open = open;
    }

    public bool Open { get; }
    public string Type => TypeId;
    public string Describe() => Open ? "open=true" : "open=false";

    public CommandCheck Validate(WorldState world)
    {
        if (world.Server.IsOpen == Open)
            return CommandCheck.Reject(Open ? "O servidor já está aberto." : "O servidor já está fechado.");
        return CommandCheck.Ok();
    }

    public void Apply(CommandContext ctx)
    {
        var w = ctx.World;
        w.Server.IsOpen = Open;
        if (Open) w.Server.LastOpenedTick = ctx.Tick;
        w.Emit(Open ? "server_opened" : "server_closed", EventImportance.High,
            causes: new[] { new EventCause("manager_command", 1000, sourceCommandSequence: ctx.Envelope.Sequence) });
        if (Open) w.Behavior.OnServerOpened();
        else w.Behavior.OnServerClosed();
    }
}

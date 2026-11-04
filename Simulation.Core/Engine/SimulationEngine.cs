using Simulation.Core.Commands;
using Simulation.Core.Population;
using Simulation.Core.Scheduling;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Core.Engine;

/// <summary>
/// Perfilador opcional fornecido pelo host. O Core nunca le o relogio do computador;
/// quem mede tempo real e o host (runner/jogo), apenas para diagnostico.
/// </summary>
public interface ISimulationProfiler
{
    IDisposable Measure(string system);
}

public sealed class NullProfiler : ISimulationProfiler
{
    public static readonly NullProfiler Instance = new();
    private static readonly IDisposable Noop = new NoopScope();
    public IDisposable Measure(string system) => Noop;

    private sealed class NoopScope : IDisposable
    {
        public void Dispose() { }
    }
}

/// <summary>
/// Motor da simulacao: avanca o relogio logico e processa, em cada tick, na ordem:
///   1) comandos previstos para o tick (por sequencia);
///   2) eventos agendados para o tick (por sequencia);
///   3) sistemas continuos (nenhum no M0; combate a partir do M3).
/// Ticks sem trabalho sao saltados quando nao ha sistemas continuos; o resultado e
/// identico a processar tick a tick (testado em DeterminismTests).
/// </summary>
public sealed class SimulationEngine
{
    public const string DailyRollupEvent = "daily_rollup";

    private readonly Dictionary<string, Action<ScheduledEvent>> _handlers = new(StringComparer.Ordinal);

    internal SimulationEngine(WorldState world)
    {
        World = world;
        RegisterHandler(DailyRollupEvent, OnDailyRollup);
        world.Behavior = new Behavior.BehaviorSystem(world);
        foreach (var (type, handler) in world.Behavior.Handlers())
            RegisterHandler(type, handler);
    }

    public WorldState World { get; }
    public ISimulationProfiler Profiler { get; set; } = NullProfiler.Instance;

    /// <summary>Quantos ticks foram efetivamente executados (diagnostico de saltos).</summary>
    public long TicksExecuted { get; private set; }

    /// <summary>Forca processamento tick a tick (modo de verificacao de equivalencia).</summary>
    public bool DisableTickSkipping { get; set; }

    /// <summary>Verdadeiro quando algum sistema exige processamento em todo tick.</summary>
    public bool HasContinuousWork => false;

    public void RegisterHandler(string eventType, Action<ScheduledEvent> handler)
    {
        if (_handlers.ContainsKey(eventType))
            throw new InvalidOperationException($"Handler duplicado para '{eventType}'.");
        _handlers.Add(eventType, handler);
    }

    /// <summary>
    /// Registra um comando. Sem tick explicito, aplica no proximo limite de tick.
    /// Ticks no passado sao recusados (comandos nunca reescrevem historia).
    /// </summary>
    public CommandEnvelope Submit(ISimCommand command, long? atTick = null)
    {
        long now = World.Clock.Tick;
        long tick = atTick ?? now + 1;
        if (tick <= now)
            throw new ArgumentOutOfRangeException(nameof(atTick), $"Comando para o tick {tick}, mas o relógio já está em {now}.");
        return World.Commands.Enqueue(command, now, tick);
    }

    /// <summary>Processa exatamente o proximo tick.</summary>
    public void Step() => ProcessTick(World.Clock.Tick + 1);

    /// <summary>Avanca ate o tick alvo (inclusive), processando tudo que vencer no caminho.</summary>
    public void AdvanceTo(long targetTick)
    {
        if (targetTick < World.Clock.Tick)
            throw new ArgumentOutOfRangeException(nameof(targetTick), "Não é possível voltar no tempo.");
        while (World.Clock.Tick < targetTick)
        {
            long next = World.Clock.Tick + 1;
            if (!DisableTickSkipping && !HasContinuousWork)
            {
                long nextWork = Math.Min(World.Scheduler.PeekTick, World.Commands.PeekTick);
                next = Math.Max(next, Math.Min(nextWork, targetTick));
            }
            ProcessTick(next);
        }
    }

    private void ProcessTick(long tick)
    {
        World.Clock.AdvanceTo(tick);
        TicksExecuted++;

        if (World.Commands.PeekTick <= tick)
        {
            using (Profiler.Measure("commands"))
            {
                while (World.Commands.TryDequeueDue(tick, out var env))
                    ApplyCommand(env);
            }
        }

        while (World.Scheduler.TryDequeueDue(tick, out var ev))
        {
            if (!_handlers.TryGetValue(ev.Type, out var handler))
                throw new InvalidOperationException($"Evento agendado sem handler: {ev}.");
            using (Profiler.Measure("event:" + ev.Type))
                handler(ev);
        }
    }

    private void ApplyCommand(CommandEnvelope env)
    {
        var check = env.Command.Validate(World);
        env.AppliedTick = World.Clock.Tick;
        if (!check.IsValid)
        {
            env.Status = CommandStatus.Rejected;
            env.RejectionReason = check.Reason;
            World.Today.CommandsRejected++;
            return;
        }
        env.Command.Apply(new CommandContext(World, env));
        env.Status = CommandStatus.Applied;
        World.Today.CommandsApplied++;
    }

    /// <summary>Fecha o dia que terminou neste tick (meia-noite) e agenda o proximo.</summary>
    private void OnDailyRollup(ScheduledEvent ev)
    {
        var w = World;
        long closedDay = ev.Payload;
        int potential = 0, registered = 0, online = 0, inactive = 0, quit = 0;
        foreach (var p in w.Persons)
        {
            switch (p.State)
            {
                case LifecycleState.PotentialInterested: potential++; break;
                case LifecycleState.Online: online++; registered++; break;
                case LifecycleState.Registered:
                case LifecycleState.Offline: registered++; break;
                case LifecycleState.Inactive: registered++; inactive++; break;
                case LifecycleState.Quit: quit++; break;
            }
        }
        var date = w.Clock.Calendar.ToDateTime(w.Clock.Calendar.StartOfDay(closedDay)).Date;
        w.AddDailyRecord(new DailyRecord
        {
            DayIndex = closedDay,
            Date = date,
            DayKind = w.Clock.Calendar.KindOf(date),
            ClosedAtTick = w.Clock.Tick,
            PersonsTotal = w.Persons.Count,
            PotentialInterested = potential,
            Registered = registered,
            Online = online,
            ServerOpen = w.Server.IsOpen,
            CommandsApplied = w.Today.CommandsApplied,
            CommandsRejected = w.Today.CommandsRejected,
            EventsEmitted = w.Today.EventsEmitted,
            Registrations = w.Today.Registrations,
            SessionsStarted = w.Today.SessionsStarted,
            ActivePlayers = w.Today.ActivePlayers,
            PeakOnline = w.Today.PeakOnline,
            ActivitiesChosen = w.Today.ActivitiesChosen,
            LevelUps = w.Today.LevelUps,
            PlayMinutes = w.Today.PlayMinutes,
            Inactive = inactive,
            Quit = quit,
            Quits = w.Today.Quits,
            Returns = w.Today.Returns,
            BecameInactive = w.Today.BecameInactive,
        });
        w.Today.Reset(w.OnlineCount);
        // Quem cruza a meia-noite conectado conta como ativo no novo dia.
        w.Today.ActivePlayers = 0;
        foreach (var p in w.Persons)
            if (p.IsOnline) { p.LastPlayedDay = closedDay + 1; w.Today.ActivePlayers++; }
        w.Scheduler.Schedule(w.Clock.Calendar.StartOfDay(closedDay + 2), DailyRollupEvent, payload: closedDay + 1);
    }
}

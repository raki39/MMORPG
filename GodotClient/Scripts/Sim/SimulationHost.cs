using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Events;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace GodotClient.Sim;

/// <summary>Leitura imutavel do estado publicada pela thread da simulacao para a tela.</summary>
public sealed class SimSnapshot
{
    public long Tick { get; init; }
    public DateTime Now { get; init; }
    public DayKind DayKind { get; init; }
    public int RequestedSpeed { get; init; }
    /// <summary>Velocidade efetiva medida (multiplos de 1x); menor que a pedida se a maquina nao acompanha.</summary>
    public double EffectiveSpeed { get; init; }
    public bool ServerOpen { get; init; }
    public int PersonsTotal { get; init; }
    public long EventsTotal { get; init; }
}

/// <summary>Dados estaticos de uma pessoa copiados na criacao (seguros para ler de qualquer thread).</summary>
public sealed class PersonInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string ProfileId { get; init; } = "";
    public ulong AppearanceSeed { get; init; }
    /// <summary>
    /// Tracos gerados na criacao. Hoje nao mudam depois da geracao, entao ler daqui e seguro;
    /// quando passarem a evoluir (M2+), trocar por copia publicada no snapshot.
    /// </summary>
    public Simulation.Core.Population.PersonTraits Traits { get; init; } = null!;
}

/// <summary>
/// Executa o SimulationEngine numa thread propria (plano 13.7). A tela nunca toca no
/// WorldState: le snapshots e envia comandos por fila. Velocidade pedida pode nao ser
/// atingida; nesse caso o relogio anda mais devagar e a velocidade efetiva e informada.
/// Nenhum acontecimento e descartado.
/// </summary>
public sealed class SimulationHost : IDisposable
{
    /// <summary>Hipotese de ritmo (plano 21, em aberto): 1x = 1 minuto simulado por segundo real.</summary>
    public const double SimSecondsPerRealSecondAt1x = 60;
    public static readonly int[] Speeds = { 0, 1, 2, 5, 10 };

    private readonly SimulationEngine _engine;
    private readonly ConcurrentQueue<ISimCommand> _commands = new();
    private readonly ConcurrentQueue<SimEvent> _events = new();
    private readonly Thread _thread;
    private volatile bool _running = true;
    private volatile int _speed = 1;
    private SimSnapshot _snapshot;

    public SimulationHost(ContentDatabase content, ulong seed, int population)
    {
        Content = content;
        Seed = seed;
        _engine = WorldFactory.Create(content, new WorldConfig { Seed = seed, PopulationSize = population });
        Persons = _engine.World.Persons
            .Select(p => new PersonInfo { Id = p.Id, Name = p.Name, ProfileId = p.ProfileId, AppearanceSeed = p.AppearanceSeed, Traits = p.Traits })
            .ToArray();
        Calendar = _engine.World.Clock.Calendar;
        _snapshot = BuildSnapshot(-1);
        _thread = new Thread(Loop) { IsBackground = true, Name = "Simulacao" };
    }

    public ContentDatabase Content { get; }
    public ulong Seed { get; }
    public CampaignCalendar Calendar { get; }
    public IReadOnlyList<PersonInfo> Persons { get; }
    public SimSnapshot Snapshot => Volatile.Read(ref _snapshot);

    public int Speed
    {
        get => _speed;
        set => _speed = Speeds.Contains(value) ? value : _speed;
    }

    public void Start() => _thread.Start();

    /// <summary>Avanca a simulacao antes de iniciar a thread (capturas e testes reproduziveis).</summary>
    public void AdvanceBeforeStart(long ticks)
    {
        if (_thread.IsAlive) throw new InvalidOperationException("A thread da simulação já está rodando.");
        while (_commands.TryDequeue(out var cmd)) _engine.Submit(cmd);
        _engine.AdvanceTo(_engine.World.Clock.Tick + ticks);
        foreach (var e in _engine.World.Events.Drain()) _events.Enqueue(e);
        Volatile.Write(ref _snapshot, BuildSnapshot(-1));
    }

    /// <summary>Envia comando; sera aplicado no proximo limite de tick pela thread da simulacao.</summary>
    public void Submit(ISimCommand command) => _commands.Enqueue(command);

    /// <summary>Eventos novos desde a ultima chamada (para faixa de eventos e efeitos).</summary>
    public List<SimEvent> DrainEvents()
    {
        var list = new List<SimEvent>();
        while (_events.TryDequeue(out var e)) list.Add(e);
        return list;
    }

    private void Loop()
    {
        var clock = Stopwatch.StartNew();
        double last = clock.Elapsed.TotalSeconds;
        double pendingTicks = 0;
        // Janela para medir velocidade efetiva.
        double windowStart = last;
        long windowStartTick = _engine.World.Clock.Tick;
        double effective = -1; // -1 = ainda medindo
        const double frameSeconds = 1.0 / 120.0;

        while (_running)
        {
            double now = clock.Elapsed.TotalSeconds;
            double dt = now - last;
            last = now;

            var world = _engine.World;
            while (_commands.TryDequeue(out var cmd))
                _engine.Submit(cmd);

            int speed = _speed;
            if (speed > 0)
            {
                pendingTicks += dt * SimSecondsPerRealSecondAt1x * speed * SimTime.TicksPerSecond;
                long whole = (long)pendingTicks;
                if (whole > 0)
                {
                    long target = world.Clock.Tick + whole;
                    var budget = Stopwatch.StartNew();
                    // Avanca em blocos de 1 minuto simulado, respeitando um orcamento de tempo real.
                    while (world.Clock.Tick < target && budget.Elapsed.TotalSeconds < frameSeconds)
                        _engine.AdvanceTo(Math.Min(target, world.Clock.Tick + SimTime.TicksPerMinute));
                    pendingTicks -= whole;
                    // Se nao acompanhou, o tempo pedido restante nao vira divida infinita:
                    // o relogio simplesmente anda mais devagar (velocidade efetiva menor).
                }
            }
            else
            {
                pendingTicks = 0;
            }

            foreach (var e in world.Events.Drain()) _events.Enqueue(e);

            if (now - windowStart >= 0.5)
            {
                double simSeconds = (world.Clock.Tick - windowStartTick) / (double)SimTime.TicksPerSecond;
                effective = simSeconds / (now - windowStart) / SimSecondsPerRealSecondAt1x;
                windowStart = now;
                windowStartTick = world.Clock.Tick;
            }
            Volatile.Write(ref _snapshot, BuildSnapshot(effective));
            Thread.Sleep(4);
        }
    }

    private SimSnapshot BuildSnapshot(double effective)
    {
        var w = _engine.World;
        return new SimSnapshot
        {
            Tick = w.Clock.Tick,
            Now = w.Clock.Now,
            DayKind = w.Clock.DayKind,
            RequestedSpeed = _speed,
            EffectiveSpeed = effective,
            ServerOpen = w.Server.IsOpen,
            PersonsTotal = w.Persons.Count,
            EventsTotal = w.Events.TotalEmitted,
        };
    }

    public void Dispose()
    {
        _running = false;
        if (_thread.IsAlive) _thread.Join(1000);
    }
}

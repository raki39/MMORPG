using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Events;
using Simulation.Core.Population;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace GodotClient.Sim;

/// <summary>Estado de um personagem copiado para a tela (imutavel depois de publicado).</summary>
public sealed class CharacterView
{
    public int PersonId { get; init; }
    public int CharacterId { get; init; }
    public string Name { get; init; } = "";
    public string ClassId { get; init; } = "";
    public int Level { get; init; }
    public long Experience { get; init; }
    public long SessionStartTick { get; init; }
    public long ActivityStartTick { get; init; }
    public CharacterStatus Status { get; init; }
    public string RegionId { get; init; } = "";
    public string TargetRegionId { get; init; } = "";
    public string ActivityId { get; init; } = "";
    public MapPos Position { get; init; }
    public MapPos MoveTo { get; init; }
    public long MoveStartTick { get; init; }
    public long MoveEndTick { get; init; }
    public long ActivityEndTick { get; init; }
    public long SessionEndTick { get; init; }
    public int KnownRegions { get; init; }
    /// <summary>Registro imutavel criado pelo Core; seguro para ler fora da thread da simulacao.</summary>
    public DecisionRecord? LastDecision { get; init; }

    /// <summary>Posicao no tick informado (mesma regra do Core: interpolacao do trecho).</summary>
    public (float X, float Y) PositionAt(long tick)
    {
        if (Status != CharacterStatus.Traveling || MoveEndTick <= MoveStartTick) return (Position.X, Position.Y);
        float t = Math.Clamp((tick - MoveStartTick) / (float)(MoveEndTick - MoveStartTick), 0f, 1f);
        return (Position.X + (MoveTo.X - Position.X) * t, Position.Y + (MoveTo.Y - Position.Y) * t);
    }
}

/// <summary>Leitura imutavel do estado publicada pela thread da simulacao para a tela.</summary>
public sealed class SimSnapshot
{
    public long Tick { get; init; }
    public int RequestedSpeed { get; init; }
    /// <summary>Velocidade efetiva medida (multiplos de 1x); -1 = medindo.</summary>
    public double EffectiveSpeed { get; init; }
    public bool ServerOpen { get; init; }
    public int PersonsTotal { get; init; }
    public int Registered { get; init; }
    public int Online { get; init; }
    /// <summary>Personagens por ID de pessoa (null = sem personagem).</summary>
    public CharacterView?[] ByPerson { get; init; } = Array.Empty<CharacterView?>();
}

/// <summary>Dados estaticos de uma pessoa copiados na criacao.</summary>
public sealed class PersonInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string ProfileId { get; init; } = "";
    public ulong AppearanceSeed { get; init; }
    /// <summary>Tracos gerados na criacao (nao mudam depois; leitura segura).</summary>
    public PersonTraits Traits { get; init; } = null!;
}

/// <summary>
/// Executa o SimulationEngine numa thread propria (plano 13.7). A tela nunca toca no
/// WorldState: le snapshots e envia comandos por fila. Se a maquina nao acompanha a
/// velocidade pedida, o relogio anda mais devagar e a velocidade efetiva e informada;
/// nenhum acontecimento e descartado.
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
        Layout = new ZoneLayout(content);
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
    public ZoneLayout Layout { get; }
    public CampaignCalendar Calendar { get; }
    public IReadOnlyList<PersonInfo> Persons { get; }
    public SimSnapshot Snapshot => Volatile.Read(ref _snapshot);

    public int Speed
    {
        get => _speed;
        set => _speed = Speeds.Contains(value) ? value : _speed;
    }

    public void Start() => _thread.Start();

    /// <summary>Avanca a simulacao antes de iniciar a thread (capturas reproduziveis).</summary>
    public void AdvanceBeforeStart(long ticks)
    {
        if (_thread.IsAlive) throw new InvalidOperationException("A thread da simulação já está rodando.");
        while (_commands.TryDequeue(out var cmd)) _engine.Submit(cmd);
        _engine.AdvanceTo(_engine.World.Clock.Tick + ticks);
        foreach (var e in _engine.World.Events.Drain()) _events.Enqueue(e);
        Volatile.Write(ref _snapshot, BuildSnapshot(-1));
    }

    public void Submit(ISimCommand command) => _commands.Enqueue(command);

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
        double windowStart = last;
        long windowStartTick = _engine.World.Clock.Tick;
        double effective = -1;
        double lastPublish = 0;
        const double budgetSeconds = 1.0 / 120.0;

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
                    while (world.Clock.Tick < target && budget.Elapsed.TotalSeconds < budgetSeconds)
                        _engine.AdvanceTo(Math.Min(target, world.Clock.Tick + SimTime.TicksPerMinute));
                    pendingTicks -= whole;
                }
            }
            else pendingTicks = 0;

            foreach (var e in world.Events.Drain()) _events.Enqueue(e);

            if (now - windowStart >= 0.5)
            {
                double simSeconds = (world.Clock.Tick - windowStartTick) / (double)SimTime.TicksPerSecond;
                effective = simSeconds / (now - windowStart) / SimSecondsPerRealSecondAt1x;
                windowStart = now;
                windowStartTick = world.Clock.Tick;
            }
            // Publica no maximo ~30 vezes por segundo (copia dos personagens).
            if (now - lastPublish >= 1.0 / 30.0)
            {
                Volatile.Write(ref _snapshot, BuildSnapshot(effective));
                lastPublish = now;
            }
            Thread.Sleep(3);
        }
    }

    private SimSnapshot BuildSnapshot(double effective)
    {
        var w = _engine.World;
        var byPerson = new CharacterView?[w.Persons.Count + 1];
        foreach (var c in w.Characters)
        {
            byPerson[c.PersonId] = new CharacterView
            {
                PersonId = c.PersonId,
                CharacterId = c.Id,
                Name = c.Name,
                ClassId = c.ClassId,
                Level = c.Level,
                Experience = c.Experience,
                SessionStartTick = c.SessionStartTick,
                ActivityStartTick = c.ActivityStartTick,
                Status = c.Status,
                RegionId = c.RegionId,
                TargetRegionId = c.TargetRegionId,
                ActivityId = c.ActivityId,
                Position = c.Position,
                MoveTo = c.MoveTo,
                MoveStartTick = c.MoveStartTick,
                MoveEndTick = c.MoveEndTick,
                ActivityEndTick = c.ActivityEndTick,
                SessionEndTick = c.SessionEndTick,
                KnownRegions = c.KnownRegions.Count,
                LastDecision = c.LastDecision,
            };
        }
        return new SimSnapshot
        {
            Tick = w.Clock.Tick,
            RequestedSpeed = _speed,
            EffectiveSpeed = effective,
            ServerOpen = w.Server.IsOpen,
            PersonsTotal = w.Persons.Count,
            Registered = w.Characters.Count,
            Online = w.OnlineCount,
            ByPerson = byPerson,
        };
    }

    public void Dispose()
    {
        _running = false;
        if (_thread.IsAlive) _thread.Join(1000);
    }
}

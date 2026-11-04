using Godot;
using Simulation.Core.Content;
using Simulation.Core.Random;
using Simulation.Core.Time;
using GodotClient.Sim;

namespace GodotClient.Fixtures;

/// <summary>
/// FIXTURE DE DEMONSTRACAO (teste de viabilidade). Movimento ficticio das pessoas entre
/// regioes conectadas, so na apresentacao, ate o M2 trazer sessoes e escolhas reais.
/// Nao e estado do dominio, nao afeta o Core e e sempre sinalizado na tela.
/// Dirigido pelo tick do snapshot (pausa e velocidade funcionam) e independente do FPS:
/// as transicoes acontecem em ticks exatos, nao no frame em que sao notadas.
/// </summary>
public sealed class DemoMovementFixture
{
    public const string Label = "DEMONSTRAÇÃO — movimento fictício até o M2";

    private readonly Agent[] _agents;
    private readonly Dictionary<string, Vector2> _regionPos;
    private readonly Dictionary<string, List<RegionConnection>> _connections;

    public DemoMovementFixture(ContentDatabase content, IReadOnlyList<PersonInfo> persons, ulong seed)
    {
        _regionPos = content.Regions.Values.ToDictionary(r => r.Id, r => new Vector2(r.MapPosition.X, r.MapPosition.Y));
        _connections = content.Regions.Values.ToDictionary(r => r.Id, r => r.Connections);
        string start = content.World.StartingRegionId;

        _agents = new Agent[persons.Count];
        for (int i = 0; i < persons.Count; i++)
        {
            var p = persons[i];
            // Fluxo proprio da fixture: nao consome nenhum fluxo do dominio.
            var rng = new DeterministicRng(RngStreams.DeriveSeed(seed, "client.demo_movement", p.Id));
            float angle = rng.NextInt(3600) / 3600f * Mathf.Tau;
            float radius = 8 + rng.NextInt(26);
            _agents[i] = new Agent
            {
                Person = p,
                Rng = rng,
                At = start,
                From = start,
                To = start,
                Offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius,
                // Chegada escalonada: todos comecam na cidade e saem ao longo da primeira hora.
                NextDepartTick = rng.NextInt((int)SimTime.TicksPerHour),
            };
        }
    }

    public int Count => _agents.Length;

    /// <summary>Avanca a fixture ate o tick informado.</summary>
    public void Update(long tick)
    {
        foreach (var a in _agents)
        {
            // Processa todas as transicoes vencidas em ordem, em ticks exatos.
            while (true)
            {
                if (a.Traveling)
                {
                    if (tick < a.ArriveTick) break;
                    a.Traveling = false;
                    a.At = a.To;
                    a.NextDepartTick = a.ArriveTick + SimTime.Minutes(10 + a.Rng.NextInt(80));
                }
                else
                {
                    if (tick < a.NextDepartTick) break;
                    var options = _connections[a.At];
                    var c = options[a.Rng.NextInt(options.Count)];
                    a.Traveling = true;
                    a.From = a.At;
                    a.To = c.To;
                    a.DepartTick = a.NextDepartTick;
                    a.ArriveTick = a.DepartTick + SimTime.Minutes(c.TravelMinutes);
                }
            }
            a.CurrentTick = tick;
        }
    }

    /// <summary>Onde a pessoa esta: parada (At + Offset) ou viajando (From -> To, progresso T em 0..1).</summary>
    public Placement PlacementOf(int index)
    {
        var a = _agents[index];
        float t = a.Traveling
            ? Mathf.Clamp((a.CurrentTick - a.DepartTick) / (float)(a.ArriveTick - a.DepartTick), 0f, 1f)
            : 0f;
        return new Placement(a.Traveling, a.At, a.From, a.To, t, a.Offset);
    }

    public readonly record struct Placement(bool Traveling, string At, string From, string To, float T, Vector2 Offset);

    public PersonInfo PersonAt(int index) => _agents[index].Person;
    public bool IsTraveling(int index) => _agents[index].Traveling;

    /// <summary>Quantas pessoas da fixture estao paradas na regiao.</summary>
    public int CountIn(string regionId) => _agents.Count(a => !a.Traveling && a.At == regionId);
    public int CountTraveling() => _agents.Count(a => a.Traveling);

    private sealed class Agent
    {
        public PersonInfo Person = null!;
        public DeterministicRng Rng = null!;
        public string At = "";
        public string From = "";
        public string To = "";
        public bool Traveling;
        public long DepartTick;
        public long ArriveTick;
        public long NextDepartTick;
        public long CurrentTick;
        public Vector2 Offset;
    }
}

using Simulation.Core.Content;
using Simulation.Core.World;

namespace Simulation.Core.Behavior;

/// <summary>Ponto de passagem de uma viagem. EntersZone != "" quando o ponto fica do outro lado de uma fronteira.</summary>
public sealed class Waypoint
{
    public Waypoint(MapPos pos, string entersZone)
    {
        Pos = pos;
        EntersZone = entersZone;
    }

    public MapPos Pos { get; }
    public string EntersZone { get; }
}

/// <summary>Plano de deslocamento: pontos ate o destino e distancia total (unidades de mapa).</summary>
public sealed class TravelPlan
{
    public static readonly TravelPlan Unreachable = new(new List<Waypoint>(), -1);

    public TravelPlan(List<Waypoint> waypoints, int distance, int walkSpeed = 15)
    {
        Waypoints = waypoints;
        Distance = distance;
        WalkSpeed = Math.Max(1, walkSpeed);
    }

    /// <summary>Velocidade a pe usada no calculo (unidades de mapa por minuto; population.json "behavior").</summary>
    public int WalkSpeed { get; }

    public List<Waypoint> Waypoints { get; }
    public int Distance { get; }
    public bool Reachable => Distance >= 0;

    public int Minutes => Distance <= 0 ? 0 : (Distance + WalkSpeed - 1) / WalkSpeed;
}

/// <summary>
/// Rotas entre zonas pelas conexoes declaradas (zonas vizinhas sem conexao nao se ligam).
/// Dentro da zona o personagem anda ate o portao; entre zonas cruza a estrada de portao a
/// portao. Dijkstra deterministico (desempate ordinal) com custo geometrico.
/// </summary>
public sealed class RouteFinder
{
    private readonly ContentDatabase _content;
    private readonly ZoneLayout _layout;
    private readonly Dictionary<string, List<string>?> _cache = new(StringComparer.Ordinal);

    public RouteFinder(ContentDatabase content, ZoneLayout layout)
    {
        _content = content;
        _layout = layout;
    }

    public ZoneLayout Layout => _layout;

    /// <summary>Custo de atravessar de A para B: centro de A ao portao, estrada, portao ao centro de B.</summary>
    private int EdgeCost(string a, string b) =>
        _layout.LegLength(_layout.Hub(a), _layout.Gate(a, b)) + _layout.Gate(a, b).DistanceTo(_layout.Gate(b, a))
        + _layout.LegLength(_layout.Gate(b, a), _layout.Hub(b));

    /// <summary>Zonas a percorrer depois de "from" ate "to" (inclusive). Nulo se inalcancavel.</summary>
    public IReadOnlyList<string>? ZonePath(string from, string to)
    {
        if (from == to) return Array.Empty<string>();
        string key = from + ">" + to;
        if (_cache.TryGetValue(key, out var hit)) return hit;

        var dist = new Dictionary<string, int>(StringComparer.Ordinal) { [from] = 0 };
        var prev = new Dictionary<string, string>(StringComparer.Ordinal);
        var done = new HashSet<string>(StringComparer.Ordinal);
        while (true)
        {
            string? current = null;
            int best = int.MaxValue;
            foreach (var kv in dist)
            {
                if (done.Contains(kv.Key)) continue;
                if (kv.Value < best || (kv.Value == best && string.CompareOrdinal(kv.Key, current) < 0))
                {
                    best = kv.Value;
                    current = kv.Key;
                }
            }
            if (current == null || current == to) break;
            done.Add(current);
            foreach (var c in _content.Regions[current].Connections)
            {
                if (done.Contains(c.To)) continue;
                int nd = best + EdgeCost(current, c.To);
                if (!dist.TryGetValue(c.To, out var old) || nd < old || (nd == old && string.CompareOrdinal(current, prev[c.To]) < 0))
                {
                    dist[c.To] = nd;
                    prev[c.To] = current;
                }
            }
        }

        List<string>? path = null;
        if (dist.ContainsKey(to))
        {
            path = new List<string>();
            for (string at = to; at != from; at = prev[at]) path.Add(at);
            path.Reverse();
        }
        _cache[key] = path;
        return path;
    }

    /// <summary>
    /// Plano completo pelas estradas: do ponto atual volta a encruzilhada da zona, segue a
    /// estrada ate a saida, cruza para a zona seguinte, passa pela encruzilhada dela e, na
    /// zona de destino, sai da estrada ate o ponto da atividade.
    /// </summary>
    public TravelPlan Plan(MapPos start, string fromZone, string toZone, MapPos target)
    {
        var zones = ZonePath(fromZone, toZone);
        if (zones == null) return TravelPlan.Unreachable;
        var wps = new List<Waypoint>();
        string current = fromZone;
        var hub = _layout.Hub(fromZone);
        if (!start.Equals(hub) && !start.Equals(target)) wps.Add(new Waypoint(hub, ""));   // volta para a estrada
        foreach (var next in zones)
        {
            wps.Add(new Waypoint(_layout.Gate(current, next), ""));      // estrada ate a saida
            wps.Add(new Waypoint(_layout.Gate(next, current), next));    // cruza para a zona seguinte
            wps.Add(new Waypoint(_layout.Hub(next), ""));                // estrada ate a encruzilhada
            current = next;
        }
        if (!target.Equals(wps.Count > 0 ? wps[^1].Pos : start)) wps.Add(new Waypoint(target, ""));  // sai da estrada ate o ponto
        int distance = 0;
        var at = start;
        foreach (var w in wps)
        {
            distance += _layout.LegLength(at, w.Pos);
            at = w.Pos;
        }
        return new TravelPlan(wps, distance, _content.Behavior.WalkSpeed);
    }
}

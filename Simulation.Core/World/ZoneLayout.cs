using Simulation.Core.Content;

namespace Simulation.Core.World;

/// <summary>Ponto no mapa em unidades inteiras (1 unidade = 10 m, hipotese D-016).</summary>
public readonly struct MapPos : IEquatable<MapPos>
{
    public MapPos(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }
    public int Y { get; }

    /// <summary>Distancia euclidiana arredondada. Math.Sqrt e corretamente arredondada (IEEE 754): deterministica.</summary>
    public int DistanceTo(MapPos o)
    {
        long dx = o.X - X, dy = o.Y - Y;
        return (int)Math.Round(Math.Sqrt(dx * dx + dy * dy));
    }

    public bool Equals(MapPos o) => X == o.X && Y == o.Y;
    public override bool Equals(object? obj) => obj is MapPos o && Equals(o);
    public override int GetHashCode() => HashCode.Combine(X, Y);
    public override string ToString() => $"({X},{Y})";
}

/// <summary>
/// Geometria interna das zonas (derivada do conteudo, sem estado): centro (ponto de chegada
/// e espera), portoes na borda voltados para cada zona conectada e pontos das atividades.
/// Viagens e tempos saem desta geometria e da velocidade de deslocamento.
/// </summary>
public sealed class ZoneLayout
{
    private readonly Dictionary<string, MapPos> _centers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MapPos> _gates = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MapPos> _spots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MapPos> _areas = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _areaRadius = new(StringComparer.Ordinal);

    public ZoneLayout(ContentDatabase content) : this(content.World, content.Regions.Values) { }

    public ZoneLayout(WorldDefinition world, IEnumerable<RegionDefinition> regions)
    {
        var byId = regions.ToDictionary(r => r.Id, StringComparer.Ordinal);
        var content = (World: world, Regions: byId);
        ZoneRadius = world.ZoneRadius;
        foreach (var r in byId.Values.OrderBy(x => x.Id, StringComparer.Ordinal))
        {
            var c = new MapPos(r.MapPosition.X, r.MapPosition.Y);
            _centers[r.Id] = c;

            foreach (var con in r.Connections)
            {
                var other = content.Regions[con.To];
                double dx = other.MapPosition.X - c.X, dy = other.MapPosition.Y - c.Y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                double k = len > 0 ? content.World.ZoneRadius * 0.92 / len : 0;
                _gates[GateKey(r.Id, con.To)] = new MapPos(c.X + (int)Math.Round(dx * k), c.Y + (int)Math.Round(dy * k));
            }

            // Areas: posicao do conteudo (relativa ao centro) ou distribuicao automatica num
            // anel de 24 direcoes, evitando o setor do nome da zona (topo) e as estradas ate
            // as saidas; ordem por ID para ser estavel. Atividades de area ficam na area;
            // atividades da zona inteira usam a encruzilhada.
            var auto = r.Areas.Where(a => a.Position == null).OrderBy(a => a.Id, StringComparer.Ordinal).ToList();
            var gateAngles = r.Connections
                .Select(con => Math.Atan2(content.Regions[con.To].MapPosition.Y - c.Y, content.Regions[con.To].MapPosition.X - c.X))
                .ToList();
            var candidates = new List<double>();
            for (int k = 0; k < 24; k++)
            {
                double ang = k * Math.PI / 12 - Math.PI;
                if (Math.Abs(AngleDiff(ang, -Math.PI / 2)) < Math.PI * 50 / 180) continue;
                if (gateAngles.Any(g => Math.Abs(AngleDiff(ang, g)) < Math.PI * 22 / 180)) continue;
                candidates.Add(ang);
            }
            if (candidates.Count == 0) candidates.Add(Math.PI / 2);
            foreach (var area in r.Areas)
            {
                MapPos p;
                if (area.Position != null)
                    p = new MapPos(c.X + area.Position.X, c.Y + area.Position.Y);
                else
                {
                    int idx = auto.IndexOf(area);
                    int pick = (int)((idx + 0.5) * candidates.Count / Math.Max(1, auto.Count));
                    double angle = candidates[Math.Min(pick, candidates.Count - 1)];
                    double radius = Math.Min(content.World.ZoneRadius * 0.58, content.World.ZoneRadius * 0.9 - area.Radius);
                    p = new MapPos(c.X + (int)Math.Round(Math.Cos(angle) * radius), c.Y + (int)Math.Round(Math.Sin(angle) * radius));
                }
                _areas[AreaKey(r.Id, area.Id)] = p;
                _areaRadius[AreaKey(r.Id, area.Id)] = area.Radius;
                foreach (var a in area.Activities) _spots[SpotKey(r.Id, a.Id)] = p;
            }
            foreach (var a in r.Activities) _spots[SpotKey(r.Id, a.Id)] = c;
        }
    }

    private static double AngleDiff(double a, double b)
    {
        double d = a - b;
        while (d > Math.PI) d -= 2 * Math.PI;
        while (d < -Math.PI) d += 2 * Math.PI;
        return d;
    }

    private static string GateKey(string from, string to) => from + ">" + to;
    private static string SpotKey(string region, string activity) => region + "/" + activity;
    private static string AreaKey(string region, string area) => region + "#" + area;

    /// <summary>Centro da area.</summary>
    public MapPos AreaCenter(string regionId, string areaId) => _areas[AreaKey(regionId, areaId)];

    public int AreaRadius(string regionId, string areaId) => _areaRadius[AreaKey(regionId, areaId)];

    public MapPos Center(string regionId) => _centers[regionId];

    /// <summary>Encruzilhada central: as estradas da zona ligam cada saida a este ponto.</summary>
    public MapPos Hub(string regionId) => _centers[regionId];

    public int ZoneRadius { get; private set; }

    /// <summary>Portao da zona "from" que leva a zona "to".</summary>
    public MapPos Gate(string from, string to) => _gates[GateKey(from, to)];

    /// <summary>Onde a atividade acontece: centro da area que a oferece, ou a encruzilhada (zona inteira / vazia).</summary>
    public MapPos Spot(string regionId, string activityId) =>
        activityId.Length == 0 ? _centers[regionId] : _spots[SpotKey(regionId, activityId)];
}

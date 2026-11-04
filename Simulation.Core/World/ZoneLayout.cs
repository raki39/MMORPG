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

/// <summary>Estrada ou trilha calculada dentro de uma zona (D-030).</summary>
public sealed class RoadPath
{
    public RoadPath(string zoneId, List<MapPos> points, List<bool> water, bool isTrail)
    {
        ZoneId = zoneId;
        Points = points;
        Water = water;
        IsTrail = isTrail;
        double len = 0;
        for (int i = 1; i < points.Count; i++)
        {
            double dx = points[i].X - points[i - 1].X, dy = points[i].Y - points[i - 1].Y;
            len += Math.Sqrt(dx * dx + dy * dy);
        }
        Length = (int)Math.Round(len);
    }

    public string ZoneId { get; }
    /// <summary>Pontos do caminho, do inicio ao fim (extremos exatos).</summary>
    public IReadOnlyList<MapPos> Points { get; }
    /// <summary>Trecho i (de Points[i] a Points[i+1]) passa sobre agua: e ponte.</summary>
    public IReadOnlyList<bool> Water { get; }
    /// <summary>Trilha da encruzilhada ate uma area (fora da estrada principal).</summary>
    public bool IsTrail { get; }
    public int Length { get; }

    public RoadPath Reversed() => new(ZoneId, Points.Reverse().ToList(), Water.Reverse().ToList(), IsTrail);
}

/// <summary>
/// Geometria interna das zonas (derivada do conteudo, sem estado): centro (encruzilhada),
/// portoes na borda voltados para cada zona conectada, areas, terreno (agua) e estradas.
/// Estradas e trilhas sao caminhos calculados que contornam agua e areas e so cruzam
/// agua por ponte (D-030); o tempo de viagem segue o comprimento delas.
/// </summary>
public sealed class ZoneLayout
{
    private readonly Dictionary<string, MapPos> _centers = new(StringComparer.Ordinal);
    /// <summary>Encruzilhada: centro da zona ou, nas cidades, o centro da cidade (configuravel).</summary>
    private readonly Dictionary<string, MapPos> _hubs = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MapPos> _gates = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MapPos> _spots = new(StringComparer.Ordinal);
    private readonly Dictionary<string, MapPos> _areas = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _areaRadius = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ZoneTerrain> _terrain = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _cityWall = new(StringComparer.Ordinal);
    private readonly Dictionary<(int, int, int, int), RoadPath> _roads = new();
    private readonly List<RoadPath> _roadList = new();

    public ZoneLayout(ContentDatabase content) : this(content.World, content.Regions.Values) { }

    public ZoneLayout(WorldDefinition world, IEnumerable<RegionDefinition> regions)
    {
        var byId = new Dictionary<string, RegionDefinition>(StringComparer.Ordinal);
        foreach (var r in regions) byId.TryAdd(r.Id, r);
        ZoneRadius = world.ZoneRadius;
        int zr = Math.Max(1, world.ZoneRadius);
        var ordered = byId.Values.OrderBy(x => x.Id, StringComparer.Ordinal).ToList();
        foreach (var r in ordered)
        {
            _centers[r.Id] = new MapPos(r.MapPosition.X, r.MapPosition.Y);
            var off = r.IsCity ? r.CityPosition : null;
            _hubs[r.Id] = off == null ? _centers[r.Id] : new MapPos(r.MapPosition.X + off.X, r.MapPosition.Y + off.Y);
        }

        foreach (var r in ordered)
        {
            var c = _centers[r.Id];
            var hub = _hubs[r.Id];
            foreach (var con in r.Connections)
            {
                if (!byId.TryGetValue(con.To, out var other)) continue;
                double dx = other.MapPosition.X - c.X, dy = other.MapPosition.Y - c.Y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                double k = len > 0 ? zr * 0.92 / len : 0;
                _gates[GateKey(r.Id, con.To)] = new MapPos(c.X + (int)Math.Round(dx * k), c.Y + (int)Math.Round(dy * k));
            }
            int wall = CityWallFor(r, zr);
            _cityWall[r.Id] = wall;
            var dry = new List<MapPos> { hub };
            dry.AddRange(r.Connections.Where(con => byId.ContainsKey(con.To)).Select(con => _gates[GateKey(r.Id, con.To)]));
            var terrain = new ZoneTerrain(r, zr, dry, 12, wall, hub);
            _terrain[r.Id] = terrain;

            PlaceAreas(r, byId, r.IsCity ? hub : c, zr, wall, terrain);
            foreach (var a in r.Activities) _spots[SpotKey(r.Id, a.Id)] = hub;
        }

        foreach (var r in ordered) BuildRoads(r, byId, zr);
    }

    /// <summary>Raio da muralha da cidade pelo nivel (0 fora de cidades).</summary>
    public static int CityWallFor(RegionDefinition r, int zoneRadius) =>
        r.IsCity ? zoneRadius * (30 + 6 * Math.Clamp(r.CityLevel, 1, 5)) / 100 : 0;

    // ---------------- Areas ----------------

    /// <summary>
    /// Direcoes candidatas: 24, evitando as estradas ate as saidas e, fora das cidades, o setor
    /// do nome da zona (topo). Areas com posicao do conteudo vem primeiro; as automaticas
    /// procuram a primeira posicao livre e seca, do anel externo ao interno. Nas cidades os
    /// aneis ficam dentro da muralha.
    /// </summary>
    private void PlaceAreas(RegionDefinition r, Dictionary<string, RegionDefinition> byId, MapPos c, int zr, int wall, ZoneTerrain terrain)
    {
        var auto = r.Areas.Where(a => a.Position == null).OrderBy(a => a.Id, StringComparer.Ordinal).ToList();
        var gateAngles = r.Connections.Where(con => byId.ContainsKey(con.To))
            .Select(con => Math.Atan2(byId[con.To].MapPosition.Y - c.Y, byId[con.To].MapPosition.X - c.X)).ToList();
        var candidates = new List<double>();
        for (int k = 0; k < 24; k++)
        {
            double ang = k * Math.PI / 12 - Math.PI;
            if (!r.IsCity && Math.Abs(AngleDiff(ang, -Math.PI / 2)) < Math.PI * 50 / 180) continue;
            if (gateAngles.Any(g => Math.Abs(AngleDiff(ang, g)) < Math.PI * 22 / 180)) continue;
            candidates.Add(ang);
        }
        if (candidates.Count == 0) candidates.Add(Math.PI / 2);

        var placed = new List<(MapPos P, int R)>();
        foreach (var area in r.Areas.Where(a => a.Position != null))
        {
            var p = new MapPos(c.X + area.Position!.X, c.Y + area.Position.Y);
            Register(r.Id, area, p);
            placed.Add((p, area.Radius));
        }
        foreach (var area in auto)
        {
            int idx = auto.IndexOf(area);
            int pick = Math.Min((int)((idx + 0.5) * candidates.Count / Math.Max(1, auto.Count)), candidates.Count - 1);
            double[] rings = r.IsCity
                ? new[] { wall * 0.64, wall * 0.40 }
                : new[] { Math.Min(zr * 0.60, zr * 0.9 - area.Radius), zr * 0.45, Math.Max(area.Radius + 8, zr * 0.30) };
            MapPos? chosen = null;
            // 1a passada: livre e seca; 2a: so livre (o validador aponta area sobre agua).
            foreach (bool needDry in new[] { true, false })
            {
                foreach (double ring in rings)
                {
                    for (int step = 0; step < candidates.Count && chosen == null; step++)
                    {
                        int k = pick + (step % 2 == 0 ? step / 2 : -(step + 1) / 2);
                        k = ((k % candidates.Count) + candidates.Count) % candidates.Count;
                        double angle = candidates[k];
                        var p = new MapPos(c.X + (int)Math.Round(Math.Cos(angle) * ring), c.Y + (int)Math.Round(Math.Sin(angle) * ring));
                        if (!placed.All(q => p.DistanceTo(q.P) >= area.Radius + q.R + 4)) continue;
                        if (needDry && terrain.AnyWater(p, area.Radius + 2)) continue;
                        chosen = p;
                    }
                    if (chosen != null) break;
                }
                if (chosen != null) break;
            }
            var at = chosen ?? new MapPos(c.X + (int)Math.Round(Math.Cos(candidates[pick]) * rings[0]), c.Y + (int)Math.Round(Math.Sin(candidates[pick]) * rings[0]));
            Register(r.Id, area, at);
            placed.Add((at, area.Radius));
        }
        // Areas ficam em terra firme: a agua sob elas e aterrada.
        foreach (var (p, rad) in placed) terrain.Carve(p, rad + 3);
    }

    private void Register(string regionId, AreaDefinition area, MapPos p)
    {
        _areas[AreaKey(regionId, area.Id)] = p;
        _areaRadius[AreaKey(regionId, area.Id)] = area.Radius;
        foreach (var a in area.Activities) _spots[SpotKey(regionId, a.Id)] = p;
    }

    // ---------------- Estradas ----------------

    /// <summary>
    /// Estradas da encruzilhada ate cada saida e trilhas ate cada area, por A* na grade do
    /// terreno: fica dentro da propria zona, contorna outras areas, prefere terreno firme
    /// (com ondulacao natural pelo ruido) e so cruza agua por ponte, onde ela e estreita.
    /// </summary>
    private void BuildRoads(RegionDefinition r, Dictionary<string, RegionDefinition> byId, int zr)
    {
        var t = _terrain[r.Id];
        var c = _centers[r.Id];
        var hub = _hubs[r.Id];
        var neighbors = _centers.Where(kv => kv.Key != r.Id && kv.Value.DistanceTo(c) < zr * 23 / 10).Select(kv => kv.Value).ToList();
        int n = t.Size;
        var cost = new int[n * n];   // 0 = bloqueado
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
            {
                int x = t.OriginX + i * ZoneTerrain.Cell + ZoneTerrain.Cell / 2, y = t.OriginY + j * ZoneTerrain.Cell + ZoneTerrain.Cell / 2;
                var p = new MapPos(x, y);
                int dc = p.DistanceTo(c);
                if (dc > zr || neighbors.Any(o => p.DistanceTo(o) < dc + 8)) continue;
                cost[j * n + i] = t.IsWaterCell(i, j) ? 90 : 10 + (FixedNoise.Fbm(x, y, 60, r.Terrain.Seed + 31) * 9 >> 16);
            }

        var areas = r.Areas.Select(a => (Key: a.Id, P: _areas[AreaKey(r.Id, a.Id)], R: a.Radius)).ToList();
        foreach (var con in r.Connections.Where(con => byId.ContainsKey(con.To)))
            AddRoad(r.Id, Route(t, cost, areas, hub, _gates[GateKey(r.Id, con.To)], null), false);
        foreach (var a in areas)
            AddRoad(r.Id, Route(t, cost, areas, hub, a.P, a.Key), true);
    }

    private void AddRoad(string zoneId, (List<MapPos> Pts, List<bool> Water) path, bool trail)
    {
        var road = new RoadPath(zoneId, path.Pts, path.Water, trail);
        var a = road.Points[0];
        var b = road.Points[^1];
        if (a.Equals(b)) return;
        _roads[(a.X, a.Y, b.X, b.Y)] = road;
        _roads[(b.X, b.Y, a.X, a.Y)] = road.Reversed();
        _roadList.Add(road);
    }

    private static (List<MapPos>, List<bool>) Route(ZoneTerrain t, int[] baseCost, List<(string Key, MapPos P, int R)> areas, MapPos from, MapPos to, string? targetArea)
    {
        int n = t.Size;
        int CellOf(MapPos p) => Math.Clamp((p.Y - t.OriginY) / ZoneTerrain.Cell, 0, n - 1) * n + Math.Clamp((p.X - t.OriginX) / ZoneTerrain.Cell, 0, n - 1);
        MapPos CenterOf(int idx) => new(t.OriginX + idx % n * ZoneTerrain.Cell + ZoneTerrain.Cell / 2, t.OriginY + idx / n * ZoneTerrain.Cell + ZoneTerrain.Cell / 2);
        int start = CellOf(from), goal = CellOf(to);

        // Outras areas sao obstaculos (a do destino nao).
        var cost = (int[])baseCost.Clone();
        foreach (var a in areas)
        {
            if (a.Key == targetArea) continue;
            for (int idx = 0; idx < cost.Length; idx++)
                if (cost[idx] > 0 && CenterOf(idx).DistanceTo(a.P) <= a.R + 2) cost[idx] = 0;
        }
        if (cost[start] == 0) cost[start] = 10;
        if (cost[goal] == 0) cost[goal] = 10;

        var g = new int[n * n];
        Array.Fill(g, int.MaxValue);
        var prev = new int[n * n];
        Array.Fill(prev, -1);
        var open = new PriorityQueue<int, long>();
        int gx = goal % n, gy = goal / n;
        long H(int idx)
        {
            int dx = Math.Abs(idx % n - gx), dy = Math.Abs(idx / n - gy);
            return 10L * Math.Max(dx, dy) + 4L * Math.Min(dx, dy);
        }
        g[start] = 0;
        open.Enqueue(start, H(start) * 100000 + start);
        bool found = false;
        while (open.TryDequeue(out int cur, out long pri))
        {
            if (cur == goal) { found = true; break; }
            if (pri / 100000 > g[cur] + H(cur)) continue;   // entrada antiga na fila
            int cx = cur % n, cy = cur / n;
            for (int dy = -1; dy <= 1; dy++)
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0) continue;
                    int x = cx + dx, y = cy + dy;
                    if (x < 0 || y < 0 || x >= n || y >= n) continue;
                    int nb = y * n + x;
                    if (cost[nb] == 0) continue;
                    int ng = g[cur] + cost[nb] * (dx != 0 && dy != 0 ? 14 : 10) / 10;
                    if (ng < g[nb])
                    {
                        g[nb] = ng;
                        prev[nb] = cur;
                        open.Enqueue(nb, (ng + H(nb)) * 100000 + nb);
                    }
                }
        }

        var cells = new List<MapPos>();
        if (found)
        {
            for (int at = goal; at != -1; at = prev[at]) cells.Add(CenterOf(at));
            cells.Reverse();
        }
        // Extremos exatos; sem caminho (nao deveria acontecer), linha reta.
        var pts = new List<MapPos> { from };
        if (cells.Count > 2) pts.AddRange(cells.Skip(1).Take(cells.Count - 2));
        pts.Add(to);
        pts = Smooth(Simplify(pts));
        var water = new List<bool>();
        for (int i = 0; i + 1 < pts.Count; i++)
            water.Add(t.IsWater((pts[i].X + pts[i + 1].X) / 2, (pts[i].Y + pts[i + 1].Y) / 2));
        return (pts, water);
    }

    /// <summary>Remove pontos alinhados (mesma direcao entre celulas).</summary>
    private static List<MapPos> Simplify(List<MapPos> pts)
    {
        if (pts.Count < 3) return pts;
        var o = new List<MapPos> { pts[0] };
        for (int i = 1; i + 1 < pts.Count; i++)
        {
            long ax = pts[i].X - o[^1].X, ay = pts[i].Y - o[^1].Y;
            long bx = pts[i + 1].X - pts[i].X, by = pts[i + 1].Y - pts[i].Y;
            if (ax * by - ay * bx != 0) o.Add(pts[i]);
        }
        o.Add(pts[^1]);
        return o;
    }

    /// <summary>Suaviza cantos (Chaikin, 2 passadas) mantendo os extremos.</summary>
    private static List<MapPos> Smooth(List<MapPos> pts)
    {
        for (int pass = 0; pass < 2 && pts.Count >= 3; pass++)
        {
            var o = new List<MapPos> { pts[0] };
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                var a = pts[i];
                var b = pts[i + 1];
                if (i > 0) o.Add(new MapPos((3 * a.X + b.X + 2) / 4, (3 * a.Y + b.Y + 2) / 4));
                if (i + 2 < pts.Count) o.Add(new MapPos((a.X + 3 * b.X + 2) / 4, (a.Y + 3 * b.Y + 2) / 4));
            }
            o.Add(pts[^1]);
            pts = o;
        }
        return pts;
    }

    // ---------------- Consultas ----------------

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
    public MapPos Hub(string regionId) => _hubs[regionId];

    public int ZoneRadius { get; private set; }

    /// <summary>Portao da zona "from" que leva a zona "to".</summary>
    public MapPos Gate(string from, string to) => _gates[GateKey(from, to)];

    /// <summary>Onde a atividade acontece: centro da area que a oferece, ou a encruzilhada (zona inteira / vazia).</summary>
    public MapPos Spot(string regionId, string activityId) =>
        activityId.Length == 0 ? _hubs[regionId] : _spots[SpotKey(regionId, activityId)];

    /// <summary>Raio da muralha da cidade (0 = nao e cidade).</summary>
    public int CityWallRadius(string regionId) => _cityWall.TryGetValue(regionId, out var w) ? w : 0;

    public ZoneTerrain Terrain(string regionId) => _terrain[regionId];

    /// <summary>Agua no ponto (pela zona de centro mais proximo).</summary>
    public bool IsWater(int x, int y)
    {
        var p = new MapPos(x, y);
        string? best = null;
        int bestD = int.MaxValue;
        foreach (var kv in _centers)
        {
            int d = p.DistanceTo(kv.Value);
            if (d < bestD) { bestD = d; best = kv.Key; }
        }
        return best != null && _terrain[best].IsWater(x, y);
    }

    /// <summary>Estradas e trilhas calculadas (um sentido de cada).</summary>
    public IReadOnlyList<RoadPath> Roads => _roadList;

    /// <summary>Caminho de a ate b, se for uma estrada ou trilha conhecida.</summary>
    public RoadPath? RoadBetween(MapPos a, MapPos b) => _roads.TryGetValue((a.X, a.Y, b.X, b.Y), out var r) ? r : null;

    /// <summary>Comprimento de um trecho de viagem: pela estrada quando existe, senao em linha reta.</summary>
    public int LegLength(MapPos a, MapPos b) => RoadBetween(a, b)?.Length ?? a.DistanceTo(b);
}

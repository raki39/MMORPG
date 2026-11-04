using Simulation.Core.Content;

namespace Simulation.Core.World;

/// <summary>
/// Ruido de valor deterministico so com inteiros (ponto fixo 16.16). Usado para gerar o
/// terreno que a simulacao respeita (agua), igual em qualquer maquina.
/// </summary>
public static class FixedNoise
{
    public const int One = 65536;

    private static uint Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)x * 374761393u + (uint)y * 668265263u + (uint)seed * 2246822519u;
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }
    }

    /// <summary>Ruido de valor em (px, py) dados em 1/65536 de celula; resultado 0..65535.</summary>
    public static int Value(long px, long py, int seed)
    {
        long ix = px >> 16, iy = py >> 16;
        long fx = px & 0xFFFF, fy = py & 0xFFFF;
        long sx = fx * fx >> 16; sx = sx * (3 * One - 2 * fx) >> 16;   // suavizacao 3t^2 - 2t^3
        long sy = fy * fy >> 16; sy = sy * (3 * One - 2 * fy) >> 16;
        long a = Hash((int)ix, (int)iy, seed) & 0xFFFF, b = Hash((int)ix + 1, (int)iy, seed) & 0xFFFF;
        long c = Hash((int)ix, (int)iy + 1, seed) & 0xFFFF, d = Hash((int)ix + 1, (int)iy + 1, seed) & 0xFFFF;
        long top = a + ((b - a) * sx >> 16);
        long bottom = c + ((d - c) * sx >> 16);
        return (int)(top + ((bottom - top) * sy >> 16));
    }

    /// <summary>Soma de 3 oitavas, normalizada para 0..65535. Escala = unidades de mapa por celula.</summary>
    public static int Fbm(int x, int y, int scale, int seed)
    {
        long px = (long)x * One / scale, py = (long)y * One / scale;
        long sum = 0, amp = 32768, total = 0;
        for (int o = 0; o < 3; o++)
        {
            sum += Value(px, py, seed + o * 101) * amp >> 16;
            total += amp;
            px *= 2; py *= 2; amp /= 2;
        }
        return (int)(sum * 65535 / Math.Max(1, total));
    }
}

/// <summary>
/// Terreno de uma zona que a simulacao respeita (D-030): agua em celulas de 4 unidades
/// (rio no vale, pocas no brejo), gerada dos parametros de terreno da zona. Estradas
/// contornam a agua e so a cruzam por ponte; areas nao ficam sobre agua. O mapa desenha
/// exatamente esta agua.
/// </summary>
public sealed class ZoneTerrain
{
    public const int Cell = 4;

    private readonly bool[] _water;

    public ZoneTerrain(RegionDefinition zone, int zoneRadius, IEnumerable<MapPos> dryPoints, int dryRadius, int cityRadius)
        : this(zone, zoneRadius, dryPoints, dryRadius, cityRadius, new MapPos(zone.MapPosition.X, zone.MapPosition.Y)) { }

    public ZoneTerrain(RegionDefinition zone, int zoneRadius, IEnumerable<MapPos> dryPoints, int dryRadius, int cityRadius, MapPos cityCenter)
    {
        Center = new MapPos(zone.MapPosition.X, zone.MapPosition.Y);
        Radius = zoneRadius;
        Size = zoneRadius * 2 / Cell + 1;
        OriginX = Center.X - zoneRadius;
        OriginY = Center.Y - zoneRadius;
        _water = new bool[Size * Size];
        var dry = dryPoints.ToList();
        int seed = zone.Terrain.Seed;
        // Rio: configurado no conteudo, ou automatico no vale (pela semente).
        var cfg = zone.Terrain.River ?? (zone.Terrain.Relief == "valley" ? DefaultRiver(seed, cityRadius) : null);
        var river = cfg is { Enabled: true } ? RiverPath(Center, zoneRadius, seed, cfg, cityRadius, cityCenter) : null;
        int pools = zone.Terrain.PoolsPermille ?? (zone.Terrain.Relief == "marsh" ? 380 : 0);
        int poolCut = pools * 65535 / 1000;
        for (int j = 0; j < Size; j++)
            for (int i = 0; i < Size; i++)
            {
                int x = OriginX + i * Cell + Cell / 2, y = OriginY + j * Cell + Cell / 2;
                var p = new MapPos(x, y);
                if (p.DistanceTo(Center) > zoneRadius) continue;
                if (cityRadius > 0 && p.DistanceTo(cityCenter) <= cityRadius + 6) continue;
                if (dry.Any(d => p.DistanceTo(d) <= dryRadius)) continue;
                _water[j * Size + i] = (river != null && IsRiver(p, river, cfg!.Width, seed))
                                       || (pools > 0 && FixedNoise.Fbm(x, y, 46, seed + 7) < poolCut);
            }
    }

    // Direcoes inteiras (x1000) para nao depender de seno/cosseno na geracao.
    private static readonly (int X, int Y)[] Directions =
    {
        (1000, 0), (924, 383), (707, 707), (383, 924), (0, 1000), (-383, 924), (-707, 707), (-924, 383),
    };

    /// <summary>Rio automatico do vale (o que o conteudo pode sobrescrever em terrain.river).</summary>
    public static RiverDefinition DefaultRiver(int seed, int cityRadius)
    {
        int side = (seed / 8) % 2 == 0 ? 1 : -1;
        return new RiverDefinition
        {
            Direction = Math.Abs(seed) % Directions.Length,
            Offset = side * (cityRadius > 0 ? cityRadius + 24 : 30 + Math.Abs(seed * 7) % 25),
            Width = 5 + Math.Abs(seed) % 3,
        };
    }

    /// <summary>
    /// Curso natural do rio: nasce fino dentro da zona, serpenteia em duas escalas (voltas
    /// grandes e pequenas) ao longo da direcao configurada, alarga ao descer e termina num
    /// lago irregular, tambem dentro da zona (sem corte seco na borda). Desvia da muralha.
    /// </summary>
    private static List<MapPos> RiverPath(MapPos c, int r, int seed, RiverDefinition cfg, int cityRadius, MapPos cityCenter)
    {
        var d = Directions[Math.Clamp(cfg.Direction, 0, Directions.Length - 1)];
        var n = (X: -d.Y, Y: d.X);
        int offset = cfg.Offset;
        int side = offset >= 0 ? 1 : -1;
        var pts = new List<MapPos>();
        const int K = 12;
        for (int k = 0; k <= K; k++)
        {
            int along = -r * 78 / 100 + r * 156 / 100 * k / K;
            long big = FixedNoise.Value((long)k * 26000, (long)seed * 3000, seed + 17) - 32768;
            long small = FixedNoise.Value((long)k * 70000, (long)seed * 5000, seed + 29) - 32768;
            int wiggle = (int)((big * 1000 + small * 380) * r * cfg.MeanderPermille / 1000 / 1000 / 32768);
            int lateral = offset + wiggle;
            var pt = new MapPos(c.X + (d.X * along + n.X * lateral) / 1000, c.Y + (d.Y * along + n.Y * lateral) / 1000);
            // A cidade fica sempre em terra: o rio desvia por fora da muralha.
            if (cityRadius > 0)
            {
                int dist = pt.DistanceTo(cityCenter);
                int need = cityRadius + 14 + cfg.Width * 3;
                if (dist < need)
                {
                    int vx = pt.X - cityCenter.X, vy = pt.Y - cityCenter.Y;
                    if (vx == 0 && vy == 0) { vx = n.X * side; vy = n.Y * side; dist = 1000; }
                    pt = new MapPos(cityCenter.X + (int)((long)vx * need / Math.Max(1, dist)), cityCenter.Y + (int)((long)vy * need / Math.Max(1, dist)));
                }
            }
            pts.Add(pt);
        }
        for (int pass = 0; pass < 3; pass++)
        {
            var o = new List<MapPos> { pts[0] };
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                var a = pts[i];
                var b = pts[i + 1];
                o.Add(new MapPos((3 * a.X + b.X) / 4, (3 * a.Y + b.Y) / 4));
                o.Add(new MapPos((a.X + 3 * b.X) / 4, (a.Y + 3 * b.Y) / 4));
            }
            o.Add(pts[^1]);
            pts = o;
        }
        return pts;
    }

    /// <summary>
    /// Agua do rio no ponto: largura cresce da nascente (35%) ate a foz, margem irregular por
    /// ruido, pequena poca na nascente e lago na foz.
    /// </summary>
    private static bool IsRiver(MapPos p, List<MapPos> path, int width, int seed)
    {
        var (dist, t) = DistanceToPath(p, path);
        long bank = FixedNoise.Value((long)p.X * 65536 / 9, (long)p.Y * 65536 / 9, seed + 41);   // 0..65535
        double wobble = 0.80 + 0.40 * bank / 65535.0;
        if (dist <= width * (0.35 + 0.65 * t) * wobble) return true;
        var mouth = path[^1];
        if (p.DistanceTo(mouth) <= width * 2.9 * (0.75 + 0.5 * bank / 65535.0)) return true;
        var spring = path[0];
        return p.DistanceTo(spring) <= width * 1.1 * wobble;
    }

    /// <summary>Distancia ao curso e posicao relativa (0 = nascente, 1 = foz) do ponto mais proximo.</summary>
    private static (double Dist, double T) DistanceToPath(MapPos p, List<MapPos> path)
    {
        double best = double.MaxValue, bestAt = 0, run = 0, total = 0;
        for (int i = 0; i + 1 < path.Count; i++)
        {
            double dx = path[i + 1].X - path[i].X, dy = path[i + 1].Y - path[i].Y;
            total += Math.Sqrt(dx * dx + dy * dy);
        }
        for (int i = 0; i + 1 < path.Count; i++)
        {
            double ax = path[i].X, ay = path[i].Y, bx = path[i + 1].X - ax, by = path[i + 1].Y - ay;
            double len2 = bx * bx + by * by;
            double len = Math.Sqrt(len2);
            double t = len2 > 0 ? Math.Clamp(((p.X - ax) * bx + (p.Y - ay) * by) / len2, 0, 1) : 0;
            double dx = p.X - (ax + bx * t), dy = p.Y - (ay + by * t);
            double dd = Math.Sqrt(dx * dx + dy * dy);
            if (dd < best) { best = dd; bestAt = run + len * t; }
            run += len;
        }
        return (best, total > 0 ? bestAt / total : 0);
    }

    public MapPos Center { get; }
    public int Radius { get; }
    public int Size { get; }
    public int OriginX { get; }
    public int OriginY { get; }

    public bool IsWaterCell(int i, int j) => i >= 0 && j >= 0 && i < Size && j < Size && _water[j * Size + i];

    public bool IsWater(int x, int y) => x >= OriginX && y >= OriginY && IsWaterCell((x - OriginX) / Cell, (y - OriginY) / Cell);

    /// <summary>Alguma agua num circulo (para nao colocar areas sobre agua).</summary>
    public bool AnyWater(MapPos c, int radius)
    {
        for (int y = c.Y - radius; y <= c.Y + radius; y += Cell)
            for (int x = c.X - radius; x <= c.X + radius; x += Cell)
                if (new MapPos(x, y).DistanceTo(c) <= radius && IsWater(x, y)) return true;
        return false;
    }

    public int WaterCells => _water.Count(w => w);

    /// <summary>Torna seca a terra num circulo (as areas ficam em terra firme).</summary>
    public void Carve(MapPos c, int radius)
    {
        for (int j = 0; j < Size; j++)
            for (int i = 0; i < Size; i++)
            {
                var p = new MapPos(OriginX + i * Cell + Cell / 2, OriginY + j * Cell + Cell / 2);
                if (p.DistanceTo(c) <= radius) _water[j * Size + i] = false;
            }
    }
}

using Simulation.Core.Content;

namespace Simulation.Core.World;

/// <summary>Lote da grade: coluna, linha e centro no mapa.</summary>
public readonly record struct MapSlot(int Column, int Row, int X, int Y)
{
    public int Index(int columns) => Row * columns + Column;
}

/// <summary>
/// Grade hexagonal de lotes do mundo (D-028). Linhas impares sao deslocadas meio passo.
/// Cada zona ocupa exatamente um lote; os demais sao lotes livres para expansao.
/// </summary>
public sealed class MapGrid
{
    private readonly MapGridDefinition _g;
    private readonly Dictionary<int, string> _zoneBySlot = new();
    private readonly Dictionary<string, MapSlot> _slotByZone = new(StringComparer.Ordinal);

    public MapGrid(WorldDefinition world, IEnumerable<RegionDefinition> regions)
    {
        _g = world.Map;
        foreach (var r in regions.OrderBy(r => r.Id, StringComparer.Ordinal))
            if (TrySlotAt(r.MapPosition.X, r.MapPosition.Y, out var s) && !_zoneBySlot.ContainsKey(s.Index(Columns)))
            {
                _zoneBySlot[s.Index(Columns)] = r.Id;
                _slotByZone[r.Id] = s;
            }
    }

    public MapGrid(ContentDatabase content) : this(content.World, content.Regions.Values) { }

    public int Columns => _g.Columns;
    public int Rows => _g.Rows;
    public MapGridDefinition Definition => _g;
    public bool IsValid => _g.Columns > 0 && _g.Rows > 0 && _g.SpacingX > 0 && _g.SpacingY > 0;

    public MapSlot Slot(int column, int row) =>
        new(column, row, _g.OriginX + column * _g.SpacingX + (row % 2 == 1 ? _g.SpacingX / 2 : 0), _g.OriginY + row * _g.SpacingY);

    public IEnumerable<MapSlot> AllSlots()
    {
        for (int row = 0; row < Rows; row++)
            for (int col = 0; col < Columns; col++)
                yield return Slot(col, row);
    }

    /// <summary>Lote cujo centro e exatamente o ponto dado.</summary>
    public bool TrySlotAt(int x, int y, out MapSlot slot)
    {
        slot = default;
        if (!IsValid) return false;
        int dy = y - _g.OriginY;
        if (dy < 0 || dy % _g.SpacingY != 0) return false;
        int row = dy / _g.SpacingY;
        int dx = x - _g.OriginX - (row % 2 == 1 ? _g.SpacingX / 2 : 0);
        if (dx < 0 || dx % _g.SpacingX != 0) return false;
        int col = dx / _g.SpacingX;
        if (row >= Rows || col >= Columns) return false;
        slot = Slot(col, row);
        return true;
    }

    /// <summary>Lote mais proximo de um ponto qualquer (nulo fora da grade).</summary>
    public MapSlot? NearestSlot(double x, double y)
    {
        if (!IsValid) return null;
        MapSlot? best = null;
        double bestD = double.MaxValue;
        int baseRow = (int)Math.Round((y - _g.OriginY) / _g.SpacingY);
        for (int row = baseRow - 1; row <= baseRow + 1; row++)
        {
            if (row < 0 || row >= Rows) continue;
            double off = row % 2 == 1 ? _g.SpacingX / 2.0 : 0;
            int baseCol = (int)Math.Round((x - _g.OriginX - off) / _g.SpacingX);
            for (int col = baseCol - 1; col <= baseCol + 1; col++)
            {
                if (col < 0 || col >= Columns) continue;
                var s = Slot(col, row);
                double d = (s.X - x) * (s.X - x) + (s.Y - y) * (s.Y - y);
                if (d < bestD) { bestD = d; best = s; }
            }
        }
        return best;
    }

    /// <summary>Lote de terra (a mascara define a forma da ilha; sem mascara, tudo e terra).</summary>
    public bool IsLand(MapSlot slot)
    {
        if (_g.Mask.Count == 0) return true;
        if (slot.Row >= _g.Mask.Count) return false;
        string line = _g.Mask[slot.Row];
        return slot.Column < line.Length && line[slot.Column] == '#';
    }

    public IEnumerable<MapSlot> LandSlots() => AllSlots().Where(IsLand);

    /// <summary>Zona que ocupa o lote (nulo = lote livre).</summary>
    public string? ZoneAt(MapSlot slot) => _zoneBySlot.TryGetValue(slot.Index(Columns), out var id) ? id : null;

    public bool TryGetSlot(string regionId, out MapSlot slot) => _slotByZone.TryGetValue(regionId, out slot);

    /// <summary>Lotes de terra sem zona (espaco para expansao).</summary>
    public int FreeSlots => LandSlots().Count(s => ZoneAt(s) == null);
}

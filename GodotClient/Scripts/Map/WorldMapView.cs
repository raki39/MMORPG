using Godot;
using Simulation.Core.Content;
using Simulation.Core.Population;
using Simulation.Core.World;
using GodotClient.Sim;
using GodotClient.Ui;

namespace GodotClient.Map;

public enum MapLayer { Normal, Crowding, Danger, Level }

/// <summary>
/// Mapa estrategico em zonas (estilo Albion). Dentro de cada zona, AREAS sao os lugares
/// (clareira, forja, veio, entrada de masmorra, esconderijo) e oferecem as atividades; as
/// pessoas aparecem dentro da area onde estao, nas posicoes do Core.
/// Camadas: terreno (shader) -> estradas e areas (_Draw) -> pessoas (MultiMesh)
/// -> sobreposicao (emblemas, rotulos, selecao, tooltip, bussola).
/// </summary>
public partial class WorldMapView : Node2D
{
    [Signal] public delegate void RegionSelectedEventHandler(string regionId);
    [Signal] public delegate void RegionOpenedEventHandler(string regionId);
    [Signal] public delegate void AreaSelectedEventHandler(string regionId, string areaId);
    [Signal] public delegate void PersonSelectedEventHandler(int personId);
    [Signal] public delegate void ClusterSelectedEventHandler(string spotKey, Vector2 screenPosition);

    private const int CrowdThreshold = 6;

    private ContentDatabase _content = null!;
    private ZoneLayout _layout = null!;
    private IReadOnlyList<PersonInfo> _persons = Array.Empty<PersonInfo>();
    private SimSnapshot? _snap;
    private ShaderMaterial _terrain = null!;
    private PopulationLayer _population = null!;
    private MapOverlay _overlay = null!;
    private RegionDefinition[] _zones = Array.Empty<RegionDefinition>();
    private MapGrid _grid = null!;
    /// <summary>Lote livre sob o mouse (indice linear; -1 = nenhum).</summary>
    public int HoveredFreeSlot { get; private set; } = -1;
    private MapSlot? _hoveredSlot;
    private float _minZoom = 0.1f;
    private const int MaxZones = 64, MaxGates = 128;
    private readonly Dictionary<string, int> _zoneIndex = new(StringComparer.Ordinal);
    private readonly List<(RegionDefinition Zone, AreaDefinition Area, Vector2[] Shape)> _areas = new();

    // Estado por quadro.
    private Vector2[] _personScreen = Array.Empty<Vector2>();
    private bool[] _personVisible = Array.Empty<bool>();
    /// <summary>"zona#area" (ou "zona#" = encruzilhada) -> pessoas paradas ali.</summary>
    private readonly Dictionary<string, List<int>> _byPlace = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _byActivity = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _zonePeople = new(StringComparer.Ordinal);
    private readonly HashSet<string> _crowded = new(StringComparer.Ordinal);
    private readonly List<(string Key, Vector2 Screen, float Radius, List<int> People)> _clusters = new();

    private Vector2 _pan, _targetPan;
    private float _zoom = 1f, _targetZoom = 1f, _fitZoom = 1f;
    private Rect2 _viewArea;
    private bool _pressed, _dragging;
    private Vector2 _pressPos, _mouse;
    private float _time;
    private float _daylight = 1f;
    private StyleBoxFlat _tip = null!;

    public MapLayer Layer { get; set; } = MapLayer.Normal;
    public string? SelectedRegionId { get; private set; }
    public string? SelectedAreaKey { get; private set; }
    public int SelectedPersonId { get; private set; }
    public int FollowPersonId { get; private set; }
    public string? HoveredRegionId { get; private set; }
    public int HoveredPersonId { get; private set; }
    private string? _hoveredArea;
    /// <summary>Cidade cuja area urbana (dentro da muralha) esta sob o mouse.</summary>
    private string? _hoveredUrban;
    private int _hoveredCluster = -1;

    public void Setup(ContentDatabase content, ZoneLayout layout, IReadOnlyList<PersonInfo> persons, ColorRect terrainRect)
    {
        _content = content;
        _layout = layout;
        _persons = persons;
        _zones = content.Regions.Values.Take(MaxZones).ToArray();
        _grid = new MapGrid(content);
        for (int i = 0; i < _zones.Length; i++) _zoneIndex[_zones[i].Id] = i;

        // Contorno organico (estavel) de cada area.
        foreach (var z in _zones)
            foreach (var a in z.Areas)
            {
                var shape = new Vector2[36];
                ulong h = Simulation.Core.Random.StableHash.Fnv1a64(z.Id + "#" + a.Id);
                float p1 = (h % 628) / 100f, p2 = (h / 628 % 628) / 100f;
                for (int k = 0; k < shape.Length; k++)
                {
                    float ang = k * Mathf.Tau / shape.Length;
                    float wob = 1f + 0.10f * Mathf.Sin(ang * 3 + p1) + 0.06f * Mathf.Sin(ang * 5 + p2);
                    shape[k] = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * wob;
                }
                _areas.Add((z, a, shape));
            }

        _terrain = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/terrain.gdshader") };
        terrainRect.Material = _terrain;
        // Terreno por zona (forma do relevo, altitude, aspereza, semente) e regras.
        var terrainData = new Vector4[MaxZones];
        var info = new Vector4[MaxZones];
        for (int i = 0; i < _zones.Length; i++)
        {
            var z = _zones[i];
            var t = z.Terrain;
            terrainData[i] = new Vector4(Array.IndexOf(Reliefs.All, t.Relief), t.ElevationPermille / 1000f, t.RoughnessPermille / 1000f, t.Seed % 997);
            info[i] = new Vector4(UiTokens.BiomeIndex(z.Biome),
                z.Pvp == PvpRules.Open ? 2 : z.Pvp == PvpRules.Consensual ? 1 : 0,
                z.DeathRule == DeathRules.FullLoot ? 2 : z.DeathRule == DeathRules.Partial ? 1 : 0,
                z.IsCity ? 1 : 0);
        }
        _terrain.SetShaderParameter("zone_terrain", terrainData);
        _terrain.SetShaderParameter("zone_info", info);

        // Grade de lotes: textura colunas x linhas (0 = mar, 1 = lote livre, 2+ = zona + 2).
        var g = _grid.Definition;
        var img = Image.CreateEmpty(Math.Max(1, g.Columns), Math.Max(1, g.Rows), false, Image.Format.R8);
        foreach (var slot in _grid.AllSlots())
        {
            string? zid = _grid.ZoneAt(slot);
            int v = zid != null && _zoneIndex.TryGetValue(zid, out int zi) ? zi + 2 : _grid.IsLand(slot) ? 1 : 0;
            img.SetPixel(slot.Column, slot.Row, new Color(v / 255f, 0, 0));
        }
        _terrain.SetShaderParameter("slot_zone", ImageTexture.CreateFromImage(img));
        _terrain.SetShaderParameter("grid", new Vector4(g.OriginX, g.OriginY, g.SpacingX, g.SpacingY));
        _terrain.SetShaderParameter("grid_dims", new Vector2I(g.Columns, g.Rows));
        _terrain.SetShaderParameter("zone_radius", (float)layout.ZoneRadius);

        // Portoes: a deformacao das fronteiras e desligada perto deles.
        var gates = new List<Vector2>();
        foreach (var z in _zones)
            foreach (var c in z.Connections)
            {
                var p = layout.Gate(z.Id, c.To);
                if (gates.Count < MaxGates) gates.Add(new Vector2(p.X, p.Y));
            }
        var gateArr = new Vector2[MaxGates];
        gates.CopyTo(gateArr);
        _terrain.SetShaderParameter("gates", gateArr);
        _terrain.SetShaderParameter("gate_count", gates.Count);

        BuildCities();
        BuildWaterTexture(layout);

        _under = new MapOverlay { Painter = c => { _c = c; DrawCityLights(); } };
        AddChild(_under);
        _static = new MapOverlay { Painter = c => { _c = c; _sw.Restart(); DrawMap(); LastDrawMs += _sw.Elapsed.TotalMilliseconds; } };
        AddChild(_static);
        _over = new MapOverlay { Painter = c => { _c = c; DrawAnimated(); DrawHighlights(); } };
        AddChild(_over);

        _population = new PopulationLayer();
        _population.Init(persons.Count);
        AddChild(_population);
        _staticOverlay = new MapOverlay { Painter = PaintStaticOverlay };
        AddChild(_staticOverlay);
        _overlay = new MapOverlay { Painter = PaintOverlay };
        AddChild(_overlay);
        _personScreen = new Vector2[persons.Count + 1];
        _personVisible = new bool[persons.Count + 1];
        _tip = ThemeFactory.Glass(12, 10);
    }

    /// <summary>
    /// Agua do Core (D-030) numa textura do mundo inteiro (1 texel = 1 celula do terreno):
    /// o mapa desenha exatamente a agua que as estradas contornam. Cidades: calcada e muralha.
    /// </summary>
    private void BuildWaterTexture(ZoneLayout layout)
    {
        int cell = Simulation.Core.World.ZoneTerrain.Cell;
        int r = layout.ZoneRadius;
        int minX = _zones.Min(z => z.MapPosition.X) - r, minY = _zones.Min(z => z.MapPosition.Y) - r;
        int maxX = _zones.Max(z => z.MapPosition.X) + r, maxY = _zones.Max(z => z.MapPosition.Y) + r;
        int w = (maxX - minX) / cell + 1, h = (maxY - minY) / cell + 1;
        var img = Image.CreateEmpty(w, h, false, Image.Format.L8);
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
                if (layout.IsWater(minX + i * cell + cell / 2, minY + j * cell + cell / 2))
                    img.SetPixel(i, j, Colors.White);
        _terrain.SetShaderParameter("water_tex", ImageTexture.CreateFromImage(img));
        _terrain.SetShaderParameter("water_rect", new Vector4(minX, minY, cell, 0));
        _terrain.SetShaderParameter("water_size", new Vector2(w, h));
        var cities = new Vector4[8];
        int n = 0;
        foreach (var c in _cities.Take(8)) cities[n++] = new Vector4(c.Center.X, c.Center.Y, c.Wall, c.Level);
        _terrain.SetShaderParameter("cities", cities);
        _terrain.SetShaderParameter("city_count", n);
    }

    public void SetSnapshot(SimSnapshot snap) => _snap = snap;

    /// <summary>Capturas: posiciona o mouse virtual na tela (hover sem mouse real).</summary>
    public void SimulateMouse(Vector2 screen) { _mouse = screen; UpdateHover(); }

    public int PeopleInZone(string regionId) => _zonePeople.TryGetValue(regionId, out var n) ? n : 0;
    public int PeopleAtSpot(string regionId, string activityId) => _byActivity.TryGetValue(regionId + "/" + activityId, out var n) ? n : 0;
    public IReadOnlyList<int> PeopleInArea(string regionId, string areaId) =>
        _byPlace.TryGetValue(regionId + "#" + areaId, out var l) ? l : (IReadOnlyList<int>)Array.Empty<int>();
    public IReadOnlyList<int> PeopleAt(string placeKey) =>
        _byPlace.TryGetValue(placeKey, out var l) ? l : (IReadOnlyList<int>)Array.Empty<int>();

    // ---------------- Camera ----------------

    public void FitTo(Rect2 viewArea, bool instant = false)
    {
        _viewArea = viewArea;
        if (_zones.Length == 0) return;
        float r = _layout.ZoneRadius;
        var min = new Vector2(_zones.Min(z => z.MapPosition.X) - r, _zones.Min(z => z.MapPosition.Y) - r);
        var max = new Vector2(_zones.Max(z => z.MapPosition.X) + r, _zones.Max(z => z.MapPosition.Y) + r);
        var size = (max - min) * 1.06f;
        _fitZoom = Mathf.Min(viewArea.Size.X / size.X, viewArea.Size.Y / size.Y);
        // Afastar ate ver a grade inteira (lotes livres para expansao) com um pouco de mar.
        if (_grid.IsValid)
        {
            var g = _grid.Definition;
            var land = _grid.LandSlots().ToList();
            if (land.Count == 0) land = _grid.AllSlots().ToList();
            var gmin = new Vector2(land.Min(s => s.X) - r * 1.6f, land.Min(s => s.Y) - r * 1.6f);
            var gmax = new Vector2(land.Max(s => s.X) + r * 1.6f, land.Max(s => s.Y) + r * 1.6f);
            var gsize = gmax - gmin;
            _minZoom = Mathf.Min(_fitZoom * 0.9f, Mathf.Min(viewArea.Size.X / gsize.X, viewArea.Size.Y / gsize.Y) * 0.85f);
        }
        else _minZoom = _fitZoom * 0.6f;
        _targetZoom = _fitZoom;
        _targetPan = viewArea.GetCenter() - (min + max) / 2f * _targetZoom;
        FollowPersonId = 0;
        if (instant) { _zoom = _targetZoom; _pan = _targetPan; }
    }

    public void ZoomBy(float factor) => ZoomAround(_viewArea.GetCenter(), factor);

    /// <summary>A area util mudou (painel lateral recolhido/aberto): desliza o mapa sem mudar o zoom.</summary>
    public void ShiftViewArea(Rect2 oldArea, Rect2 newArea)
    {
        _viewArea = newArea;
        _targetPan += newArea.GetCenter() - oldArea.GetCenter();
    }

    public void FocusRegion(string regionId)
    {
        if (!_zoneIndex.ContainsKey(regionId)) return;
        var c = _layout.Center(regionId);
        FocusMap(new Vector2(c.X, c.Y), 2.2f);
    }

    public void FocusArea(string regionId, string areaId)
    {
        var c = _layout.AreaCenter(regionId, areaId);
        FocusMap(new Vector2(c.X, c.Y), 4.2f);
    }

    private void FocusMap(Vector2 at, float zoomFactor)
    {
        _targetZoom = Mathf.Clamp(_fitZoom * zoomFactor, _minZoom, _fitZoom * 7f);
        _targetPan = _viewArea.GetCenter() - at * _targetZoom;
        FollowPersonId = 0;
    }

    public void FocusPerson(int personId, bool follow)
    {
        FollowPersonId = follow ? personId : 0;
        _targetZoom = Mathf.Max(_targetZoom, _fitZoom * 3.2f);
        var p = MapPositionOf(personId);
        if (p.HasValue) _targetPan = _viewArea.GetCenter() - p.Value * _targetZoom;
    }

    private void ZoomAround(Vector2 screen, float factor)
    {
        var anchor = (screen - _targetPan) / _targetZoom;
        _targetZoom = Mathf.Clamp(_targetZoom * factor, _minZoom, _fitZoom * 7f);
        _targetPan = screen - anchor * _targetZoom;
    }

    private Vector2 ToScreen(Vector2 map) => map * _zoom + _pan;
    private Vector2 ToScreen(MapPos p) => new Vector2(p.X, p.Y) * _zoom + _pan;
    private Vector2 ToMap(Vector2 screen) => (screen - _pan) / _zoom;
    private float Detail => _zoom / Mathf.Max(_fitZoom, 0.0001f);   // 1 = mundo inteiro

    public void SetHour(double hour)
    {
        float h = (float)hour;
        _daylight = Mathf.SmoothStep(4.5f, 7f, h) * (1f - Mathf.SmoothStep(18f, 20.5f, h));
    }

    public void SelectRegion(string? regionId) => SelectedRegionId = regionId;
    public void SelectArea(string? key) => SelectedAreaKey = key;
    public void SelectPerson(int personId) => SelectedPersonId = personId;

    // ---------------- Quadro a quadro ----------------

    public override void _Process(double delta)
    {
        if (_zones.Length == 0) return;
        var t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        ProcessMap(delta);
        LastProcessMs = System.Diagnostics.Stopwatch.GetElapsedTime(t0).TotalMilliseconds;
    }

    private void ProcessMap(double delta)
    {
        float dt = (float)delta;
        _time += dt;
        if (FollowPersonId > 0 && MapPositionOf(FollowPersonId) is { } fp)
            _targetPan = _viewArea.GetCenter() - fp * _targetZoom;
        float k = 1f - Mathf.Exp(-dt * 10f);
        _zoom = Mathf.Lerp(_zoom, _targetZoom, k);
        _pan = _pan.Lerp(_targetPan, k);

        UpdatePopulation();
        UpdateHover();
        UpdateTerrainUniforms();
        LastDrawMs = 0;
        LastOverlayMs = 0;
        _under.QueueRedraw();
        _over.QueueRedraw();
        _overlay.QueueRedraw();
        UpdateStaticCache(dt);
    }

    /// <summary>Forca o redesenho das camadas em cache (conteudo ou camada do mapa mudou).</summary>
    public void InvalidateMapCache() => _cacheKey = "";

    /// <summary>
    /// Desenho em cache: estradas, cidades, areas, emblemas e nomes so sao redesenhados quando
    /// muda o hover, a selecao, a faixa de detalhe, a luz do dia, a camada ou quando o zoom se
    /// afasta muito do zoom do ultimo desenho. Entre redesenhos, a camada so e movida e
    /// escalada pela camera (sem reenviar comandos de desenho). Emblemas e nomes tambem se
    /// atualizam 4 vezes por segundo (contagem de jogadores).
    /// </summary>
    /// <summary>Medicao do pior caso: redesenha o cache a cada quadro.</summary>
    public bool ForceRedrawEveryFrame { get; set; }

    private void UpdateStaticCache(float dt)
    {
        if (ForceRedrawEveryFrame) _cacheKey = "";
        string key = string.Concat(SelectedRegionId, "|", SelectedAreaKey, "|",
            Detail >= 0.75f ? "a" : "b", Detail >= 1.25f ? "a" : "b", Detail >= 1.6f ? "a" : "b", Detail >= 2.4f ? "a" : "b", "|",
            ((int)(_daylight * 20)).ToString(), "|", Layer.ToString());
        bool settled = Mathf.Abs(_zoom - _targetZoom) <= _targetZoom * 0.002f;
        float ratio = _cacheZoom > 0 ? _zoom / _cacheZoom : 0;
        bool zoomFar = ratio < 0.88f || ratio > 1.14f;
        bool settleRedraw = settled && Mathf.Abs(ratio - 1f) > 0.0015f;
        if (key != _cacheKey || zoomFar || settleRedraw)
        {
            _cacheKey = key;
            _cacheZoom = _zoom;
            _cachePan = _pan;
            _static.QueueRedraw();
            _overlayZoom = _zoom;
            _overlayPan = _pan;
            _staticOverlay.QueueRedraw();
            _countsTimer = 0;
        }
        else
        {
            _countsTimer += dt;
            if (_countsTimer >= 0.25f)
            {
                _countsTimer = 0;
                _overlayZoom = _zoom;
                _overlayPan = _pan;
                _staticOverlay.QueueRedraw();
            }
        }
        Place(_static, _cacheZoom, _cachePan);
        Place(_staticOverlay, _overlayZoom, _overlayPan);
    }

    private void Place(Node2D layer, float drawnZoom, Vector2 drawnPan)
    {
        float s = drawnZoom > 0 ? _zoom / drawnZoom : 1f;
        layer.Scale = new Vector2(s, s);
        layer.Position = _pan - drawnPan * s;
    }

    private void UpdateTerrainUniforms()
    {
        _terrain.SetShaderParameter("viewport_size", GetViewportRect().Size);
        _terrain.SetShaderParameter("pan", _pan);
        _terrain.SetShaderParameter("zoom", _zoom);
        _terrain.SetShaderParameter("daylight", _daylight);
        _terrain.SetShaderParameter("anim_time", _time);
        _terrain.SetShaderParameter("selected_zone", SelectedRegionId != null && _zoneIndex.TryGetValue(SelectedRegionId, out var si) ? si : -1);
        _terrain.SetShaderParameter("hovered_zone", HoveredRegionId != null && _zoneIndex.TryGetValue(HoveredRegionId, out var hi) ? hi : -1);
        _terrain.SetShaderParameter("hovered_slot", HoveredFreeSlot);
        var tints = new Vector4[MaxZones];
        for (int i = 0; i < _zones.Length; i++)
        {
            var (c, a) = LayerTint(_zones[i]);
            tints[i] = new Vector4(c.R, c.G, c.B, a);
        }
        _terrain.SetShaderParameter("zone_tint", tints);
    }

    public (Color Color, float Alpha) LayerTint(RegionDefinition z) => Layer switch
    {
        MapLayer.Crowding => (Ramp(Mathf.Clamp(PeopleInZone(z.Id) / (float)Math.Max(1, z.Capacity) * 4f, 0, 1)), 0.62f),
        MapLayer.Danger => (Ramp(DangerOf(z)), 0.62f),
        MapLayer.Level => (new Color("#3E6FB0").Lerp(new Color("#9B4FC6"), Mathf.Clamp((z.LevelMin - 1) / (float)Math.Max(1, _content.Progression.LevelCap - 1), 0, 1)), 0.62f),
        _ => (Colors.Black, 0f),
    };

    public static float DangerOf(RegionDefinition z)
    {
        if (z.IsCity) return 0f;
        var acts = z.AllActivities().ToList();
        float risk = acts.Count == 0 ? 0 : (float)acts.Average(a => a.BaseRiskPermille) / 1000f;
        float pvp = z.Pvp == PvpRules.Open ? 0.40f : z.Pvp == PvpRules.Consensual ? 0.15f : 0f;
        float death = z.DeathRule == DeathRules.FullLoot ? 0.35f : z.DeathRule == DeathRules.Partial ? 0.15f : 0f;
        return Mathf.Clamp(risk * 0.5f + pvp + death, 0, 1);
    }

    public static Color Ramp(float t) => t < 0.5f
        ? UiTokens.Success.Lerp(UiTokens.Warning, t * 2f)
        : UiTokens.Warning.Lerp(UiTokens.Danger, (t - 0.5f) * 2f);

    private Vector2? MapPositionOf(int personId)
    {
        var v = _snap?.ByPerson is { } arr && personId < arr.Length ? arr[personId] : null;
        if (v == null || v.Status == CharacterStatus.Offline) return null;
        if (v.Status == CharacterStatus.Traveling) return TravelPoint(v, _snap!.Tick);
        return new Vector2(v.Position.X, v.Position.Y);
    }

    private float EmblemRadius() => Mathf.Clamp(8.5f * Mathf.Sqrt(Detail) + 1.5f, 9f, 19f);

    /// <summary>Posicao de quem viaja: sobre a estrada ou trilha do trecho (D-030), pela fracao do comprimento.</summary>
    private Vector2 TravelPoint(CharacterView v, long tick)
    {
        var a = new Vector2(v.Position.X, v.Position.Y);
        var b = new Vector2(v.MoveTo.X, v.MoveTo.Y);
        if (v.MoveEndTick <= v.MoveStartTick) return a;
        float t = Math.Clamp((tick - v.MoveStartTick) / (float)(v.MoveEndTick - v.MoveStartTick), 0f, 1f);
        var road = _layout.RoadBetween(v.Position, v.MoveTo);
        if (road == null || road.Points.Count < 2) return a.Lerp(b, t);
        float target = t * road.Length, run = 0;
        for (int i = 0; i + 1 < road.Points.Count; i++)
        {
            var p0 = new Vector2(road.Points[i].X, road.Points[i].Y);
            var p1 = new Vector2(road.Points[i + 1].X, road.Points[i + 1].Y);
            float seg = p0.DistanceTo(p1);
            if (run + seg >= target && seg > 0) return p0.Lerp(p1, (target - run) / seg);
            run += seg;
        }
        return b;
    }

    private Vector2[] Screen(IReadOnlyList<MapPos> pts)
    {
        var o = new Vector2[pts.Count];
        for (int i = 0; i < pts.Count; i++) o[i] = ToScreen(pts[i]);
        return o;
    }

    private void UpdatePopulation()
    {
        _byPlace.Clear();
        _byActivity.Clear();
        _zonePeople.Clear();
        _crowded.Clear();
        _clusters.Clear();
        Array.Clear(_personVisible);
        if (_snap == null) return;
        var snap = _snap;

        for (int id = 1; id < snap.ByPerson.Length; id++)
        {
            var v = snap.ByPerson[id];
            if (v == null || v.Status == CharacterStatus.Offline) continue;
            _zonePeople[v.RegionId] = (_zonePeople.TryGetValue(v.RegionId, out var n) ? n : 0) + 1;
            if (v.Status == CharacterStatus.Traveling) continue;
            string key = v.RegionId + "#";
            if (v.Status == CharacterStatus.Doing && v.ActivityId.Length > 0)
            {
                _byActivity[v.TargetRegionId + "/" + v.ActivityId] = (_byActivity.TryGetValue(v.TargetRegionId + "/" + v.ActivityId, out var c) ? c : 0) + 1;
                var area = _content.Regions[v.TargetRegionId].AreaOfActivity(v.ActivityId);
                key = v.TargetRegionId + "#" + (area?.Id ?? "");
            }
            if (!_byPlace.TryGetValue(key, out var list)) _byPlace[key] = list = new List<int>();
            list.Add(id);
        }

        float diameter = Mathf.Clamp(5.5f * Mathf.Sqrt(Detail) + 1.5f, 6f, 14f);
        int written = 0;
        foreach (var (key, people) in _byPlace)
        {
            var parts = key.Split('#');
            bool isArea = parts[1].Length > 0;
            Vector2 center;
            float maxR;
            if (isArea)
            {
                center = ToScreen(_layout.AreaCenter(parts[0], parts[1]));
                maxR = _layout.AreaRadius(parts[0], parts[1]) * _zoom;
            }
            else
            {
                center = ToScreen(_layout.Hub(parts[0]));
                maxR = 40f;
            }
            float start = isArea ? EmblemRadius() + 4f : 7f;
            float spacing = diameter * 0.8f;
            // Cabem todos dentro da area? Senao, a area vira contador (o emblema mostra o anel por perfil).
            float needed = start + spacing * Mathf.Sqrt(people.Count);
            if (people.Count >= CrowdThreshold && (needed > Mathf.Max(maxR, start + spacing * 2.5f) || Detail < 1.6f))
            {
                if (isArea) _crowded.Add(key);
                else _clusters.Add((key, center, 12f + Mathf.Sqrt(people.Count) * 2.6f, people));
                foreach (var id in people) _personScreen[id] = center;
                continue;
            }
            for (int k = 0; k < people.Count; k++)
            {
                int id = people[k];
                float ang = k * 2.39996f + 0.6f;
                float rad = start + spacing * Mathf.Sqrt(k);
                _personScreen[id] = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;
                _personVisible[id] = true;
            }
        }
        for (int id = 1; id < snap.ByPerson.Length; id++)
        {
            var v = snap.ByPerson[id];
            if (v == null || v.Status != CharacterStatus.Traveling) continue;
            _personScreen[id] = ToScreen(TravelPoint(v, snap.Tick));
            _personVisible[id] = true;
        }
        for (int id = 1; id < _personVisible.Length; id++)
        {
            if (!_personVisible[id]) continue;
            var color = UiTokens.ProfileColor(_persons[id - 1].ProfileId);
            float d = id == SelectedPersonId || id == HoveredPersonId ? diameter * 1.5f : diameter;
            _population.Write(written++, _personScreen[id], d, color);
        }
        for (int i = written; i < _persons.Count; i++) _population.Write(i, new Vector2(-9999, -9999), 0, Colors.Transparent);
        _population.Commit();
    }

    private void UpdateHover()
    {
        HoveredPersonId = 0;
        HoveredRegionId = null;
        _hoveredArea = null;
        _hoveredCluster = -1;
        HoveredFreeSlot = -1;
        _hoveredSlot = null;
        _hoveredUrban = null;
        if (_dragging || !_viewArea.HasPoint(_mouse)) return;

        for (int i = 0; i < _clusters.Count; i++)
            if (_clusters[i].Screen.DistanceTo(_mouse) <= _clusters[i].Radius + 3) { _hoveredCluster = i; return; }

        float best = 8f;
        for (int id = 1; id < _personVisible.Length; id++)
        {
            if (!_personVisible[id]) continue;
            float d = _personScreen[id].DistanceTo(_mouse);
            if (d < best) { best = d; HoveredPersonId = id; }
        }
        if (HoveredPersonId != 0) return;

        foreach (var (z, a, _) in _areas)
        {
            var c = ToScreen(_layout.AreaCenter(z.Id, a.Id));
            float r = Mathf.Max(_layout.AreaRadius(z.Id, a.Id) * _zoom, EmblemRadius() + 2);
            if (c.DistanceTo(_mouse) <= r) { _hoveredArea = z.Id + "#" + a.Id; HoveredRegionId = z.Id; return; }
        }
        // Area urbana: todo o espaco dentro da muralha se comporta como uma area.
        var mm = ToMap(_mouse);
        foreach (var city in _cities)
            if (mm.DistanceTo(city.Center) <= city.Wall + 4) { _hoveredUrban = city.Zone.Id; HoveredRegionId = city.Zone.Id; return; }
        HoveredRegionId = ZoneAt(_mouse);
        if (HoveredRegionId == null && _grid.IsValid)
        {
            // Lote livre: espaco do tamanho de uma zona, reservado para expansao.
            var m = ToMap(_mouse);
            if (_grid.NearestSlot(m.X, m.Y) is { } s && _grid.ZoneAt(s) == null && _grid.IsLand(s)
                && m.DistanceTo(new Vector2(s.X, s.Y)) <= _layout.ZoneRadius * 1.05f)
            {
                _hoveredSlot = s;
                HoveredFreeSlot = s.Index(_grid.Columns);
            }
        }
    }

    private string? ZoneAt(Vector2 screen)
    {
        var m = ToMap(screen);
        string? best = null;
        float bestD = float.MaxValue;
        foreach (var z in _zones)
        {
            float d = m.DistanceTo(new Vector2(z.MapPosition.X, z.MapPosition.Y));
            if (d < bestD) { bestD = d; best = z.Id; }
        }
        return bestD <= _layout.ZoneRadius * 1.05f ? best : null;
    }

    // ---------------- Entrada ----------------

    /// <summary>Capturas automaticas: ignora mouse e teclado reais (resultado reprodutivel).</summary>
    public bool IgnoreRealInput { get; set; }

    public override void _UnhandledInput(InputEvent e)
    {
        if (IgnoreRealInput) return;
        if (e is InputEventMouseMotion mm)
        {
            _mouse = mm.Position;
            if (_pressed && !_dragging && mm.Position.DistanceTo(_pressPos) > 5f) { _dragging = true; FollowPersonId = 0; }
            if (_dragging) { _targetPan += mm.Relative; _pan += mm.Relative; }
            return;
        }
        if (e is not InputEventMouseButton mb) return;
        _mouse = mb.Position;
        switch (mb.ButtonIndex)
        {
            case MouseButton.WheelUp when mb.Pressed: ZoomAround(mb.Position, 1.18f); break;
            case MouseButton.WheelDown when mb.Pressed: ZoomAround(mb.Position, 1f / 1.18f); break;
            case MouseButton.Right:
            case MouseButton.Middle:
                _pressed = mb.Pressed; _dragging = mb.Pressed; _pressPos = mb.Position;
                if (mb.Pressed) FollowPersonId = 0;
                break;
            case MouseButton.Left:
                if (mb.Pressed)
                {
                    _pressed = true; _dragging = false; _pressPos = mb.Position;
                    if (mb.DoubleClick && ZoneAt(mb.Position) is { } z) { FocusRegion(z); EmitSignal(SignalName.RegionOpened, z); }
                }
                else
                {
                    if (!_dragging) Click(mb.Position);
                    _pressed = false; _dragging = false;
                }
                break;
            default: return;
        }
        GetViewport().SetInputAsHandled();
    }

    private void Click(Vector2 at)
    {
        _mouse = at;
        UpdateHover();
        if (_hoveredCluster >= 0) { EmitSignal(SignalName.ClusterSelected, _clusters[_hoveredCluster].Key, _clusters[_hoveredCluster].Screen); return; }
        if (HoveredPersonId != 0) { SelectPerson(HoveredPersonId); EmitSignal(SignalName.PersonSelected, HoveredPersonId); return; }
        if (_hoveredArea != null)
        {
            var parts = _hoveredArea.Split('#');
            SelectArea(_hoveredArea);
            EmitSignal(SignalName.AreaSelected, parts[0], parts[1]);
            return;
        }
        var zone = HoveredRegionId ?? ZoneAt(at);
        SelectRegion(zone);
        EmitSignal(SignalName.RegionSelected, zone ?? "");
    }

    // ---------------- Desenho: estradas, trilhas e areas ----------------

    /// <summary>Medicao: custo do ultimo _Draw, da sobreposicao e do _Process (ms).</summary>
    public double LastDrawMs { get; private set; }
    public double LastOverlayMs { get; private set; }
    public double LastProcessMs { get; private set; }
    private readonly System.Diagnostics.Stopwatch _sw = new();

    /// <summary>Canvas alvo do desenho em andamento (a camada que esta pintando).</summary>
    private CanvasItem _c = null!;

    // Camadas: luzes (anima) -> mapa estatico (cache) -> animacoes -> populacao -> emblemas e
    // nomes (cache) -> sobreposicao dinamica (bolhas, marcador, tooltip, bussola).
    private MapOverlay _under = null!, _static = null!, _over = null!, _staticOverlay = null!;
    private float _cacheZoom = -1f, _overlayZoom = -1f;
    private Vector2 _cachePan, _overlayPan;
    private string _cacheKey = "";
    private float _countsTimer;

    /// <summary>
    /// Lote de triangulos com cor por vertice: todos os preenchimentos de uma etapa vao ao
    /// renderizador numa chamada so (em vez de um poligono ou circulo por objeto).
    /// </summary>
    private sealed class TriBatch
    {
        private readonly List<Vector2> _p = new();
        private readonly List<Color> _c = new();
        private readonly List<int> _i = new();

        /// <summary>Leque a partir de um centro (poligonos estrelados em torno do centro, como as manchas).</summary>
        public void FanCenter(Vector2 center, Vector2[] ring, Color col)
        {
            int b = _p.Count;
            _p.Add(center); _c.Add(col);
            foreach (var q in ring) { _p.Add(q); _c.Add(col); }
            int n = ring.Length;
            for (int k = 0; k < n; k++) { _i.Add(b); _i.Add(b + 1 + k); _i.Add(b + 1 + (k + 1) % n); }
        }

        /// <summary>Leque a partir do primeiro vertice (poligonos convexos).</summary>
        public void Convex(Vector2[] pts, Color col)
        {
            int b = _p.Count;
            foreach (var q in pts) { _p.Add(q); _c.Add(col); }
            for (int k = 1; k + 1 < pts.Length; k++) { _i.Add(b); _i.Add(b + k); _i.Add(b + k + 1); }
        }

        public void Quad(Vector2 a, Vector2 b2, Vector2 c, Vector2 d, Color col)
        {
            int b = _p.Count;
            _p.Add(a); _p.Add(b2); _p.Add(c); _p.Add(d);
            _c.Add(col); _c.Add(col); _c.Add(col); _c.Add(col);
            _i.Add(b); _i.Add(b + 1); _i.Add(b + 2); _i.Add(b); _i.Add(b + 2); _i.Add(b + 3);
        }

        public void Tri(Vector2 a, Vector2 b2, Vector2 c, Color col)
        {
            int b = _p.Count;
            _p.Add(a); _p.Add(b2); _p.Add(c);
            _c.Add(col); _c.Add(col); _c.Add(col);
            _i.Add(b); _i.Add(b + 1); _i.Add(b + 2);
        }

        public void Dot(Vector2 center, float r, Color col)
        {
            int seg = r < 3f ? 6 : r < 8f ? 10 : 16;
            int b = _p.Count;
            _p.Add(center); _c.Add(col);
            for (int k = 0; k < seg; k++)
            {
                float ang = k * Mathf.Tau / seg;
                _p.Add(center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * r); _c.Add(col);
            }
            for (int k = 0; k < seg; k++) { _i.Add(b); _i.Add(b + 1 + k); _i.Add(b + 1 + (k + 1) % seg); }
        }

        public void Flush(CanvasItem target)
        {
            if (_i.Count == 0) return;
            RenderingServer.CanvasItemAddTriangleArray(target.GetCanvasItem(), _i.ToArray(), _p.ToArray(), _c.ToArray());
            _p.Clear(); _c.Clear(); _i.Clear();
        }
    }

    private readonly TriBatch _tb = new();

    private void DrawMap()
    {
        float w = Mathf.Clamp(_zoom * 1.1f, 0.8f, 2.2f);
        // Trilhas ate as areas: tracejado fino numa chamada so; somem com o mapa afastado.
        if (Detail >= 0.75f)
        {
            var trail = new List<Vector2>(2048);
            foreach (var road in _layout.Roads)
            {
                if (!road.IsTrail) continue;
                for (int i = 0; i + 1 < road.Points.Count; i += 2) { trail.Add(ToScreen(road.Points[i])); trail.Add(ToScreen(road.Points[i + 1])); }
            }
            if (trail.Count > 0) _c.DrawMultiline(trail.ToArray(), new Color(RoadLight, 0.45f), Mathf.Max(1f, 0.9f * w), true);
        }

        // Estradas (caminhos do Core) e travessias entre zonas: borda e leito em duas chamadas.
        var segs = new List<Vector2>(4096);
        foreach (var road in _layout.Roads)
        {
            if (road.IsTrail) continue;
            var prev = ToScreen(road.Points[0]);
            for (int i = 1; i < road.Points.Count; i++) { var cur = ToScreen(road.Points[i]); segs.Add(prev); segs.Add(cur); prev = cur; }
        }
        var drawnCross = new HashSet<string>(StringComparer.Ordinal);
        foreach (var z in _zones)
            foreach (var c in z.Connections)
            {
                string key = string.CompareOrdinal(z.Id, c.To) < 0 ? z.Id + "|" + c.To : c.To + "|" + z.Id;
                if (drawnCross.Add(key)) { segs.Add(ToScreen(_layout.Gate(z.Id, c.To))); segs.Add(ToScreen(_layout.Gate(c.To, z.Id))); }
            }
        var roadArr = segs.ToArray();
        _c.DrawMultiline(roadArr, new Color(RoadEdge, 0.85f), 3.0f * w, true);
        _c.DrawMultiline(roadArr, RoadFill, 1.8f * w, true);
        foreach (var road in _layout.Roads)
            for (int i = 0; i < road.Water.Count; i++)
                if (road.Water[i]) Bridge(ToScreen(road.Points[i]), ToScreen(road.Points[i + 1]), w);
        // Areas de caca e coleta: preenchimentos num lote, contornos depois.
        foreach (var (z, a, shape) in _areas)
        {
            if (z.IsCity) continue;
            var c = ToScreen(_layout.AreaCenter(z.Id, a.Id));
            float r = _layout.AreaRadius(z.Id, a.Id) * _zoom;
            var col = AreaPalette.Color(a.Kind);
            var pts = new Vector2[shape.Length];
            var inner = new Vector2[shape.Length];
            for (int i = 0; i < shape.Length; i++) { pts[i] = c + shape[i] * r; inner[i] = c + shape[i] * (r * 0.62f); }
            _tb.FanCenter(c, pts, new Color(col, 0.11f));
            _tb.FanCenter(c, inner, new Color(col, 0.07f));
        }
        _tb.Flush(_c);
        foreach (var (z, a, shape) in _areas)
        {
            if (z.IsCity) continue;
            var c = ToScreen(_layout.AreaCenter(z.Id, a.Id));
            float r = _layout.AreaRadius(z.Id, a.Id) * _zoom;
            var col = AreaPalette.Color(a.Kind);
            if (AreaKinds.IsPortal(a.Kind))
            {
                var outline = new Vector2[shape.Length + 1];
                for (int i = 0; i < shape.Length; i++) outline[i] = c + shape[i] * r;
                outline[^1] = outline[0];
                _c.DrawPolyline(outline, new Color(col, 0.85f), 2f, true);
            }
            else
            {
                var seg = new Vector2[shape.Length / 2 * 2];
                for (int i = 0; i + 1 < seg.Length; i += 2) { seg[i] = c + shape[i] * r; seg[i + 1] = c + shape[(i + 1) % shape.Length] * r; }
                _c.DrawMultiline(seg, new Color(col, 0.35f), 1.1f, true);
            }
        }
        foreach (var city in _cities) DrawCity(city, w);

        // Portoes (marcos), encruzilhadas e castelos: um lote.
        foreach (var z in _zones)
        {
            foreach (var c in z.Connections) DrawGate(ToScreen(_layout.Gate(z.Id, c.To)), ToScreen(_layout.Center(c.To)), w);
            var hub = ToScreen(_layout.Hub(z.Id));
            if (z.IsCity) IconCastle(hub, 7f * w);
            else { _tb.Dot(hub, 2.6f * w, new Color(RoadEdge, 0.8f)); _tb.Dot(hub, 1.6f * w, RoadFill); }
        }
        _tb.Flush(_c);
    }

    /// <summary>Chao da area: mancha organica tingida pelo tipo, contorno tracejado e brilho de portal.</summary>
    private void DrawAreaGround(RegionDefinition z, AreaDefinition a, Vector2[] shape, bool hot = false)
    {
        var c = ToScreen(_layout.AreaCenter(z.Id, a.Id));
        float r = _layout.AreaRadius(z.Id, a.Id) * _zoom;
        var col = AreaPalette.Color(a.Kind);
        var pts = new Vector2[shape.Length];
        var inner = new Vector2[shape.Length];
        for (int i = 0; i < shape.Length; i++) { pts[i] = c + shape[i] * r; inner[i] = c + shape[i] * (r * 0.62f); }
        // Destaque (hover/selecao) e desenhado por cima da versao normal, na camada dinamica.
        _c.DrawColoredPolygon(pts, new Color(col, hot ? 0.11f : 0.11f));
        _c.DrawColoredPolygon(inner, new Color(col, hot ? 0.05f : 0.07f));
        if (AreaKinds.IsPortal(a.Kind))
        {
            var outline = new Vector2[pts.Length + 1];
            Array.Copy(pts, outline, pts.Length);
            outline[^1] = pts[0];
            _c.DrawPolyline(outline, new Color(col, 0.85f), 2f, true);
        }
        else
        {
            // Tracejado numa chamada so (pares de pontos).
            var seg = new Vector2[pts.Length];
            for (int i = 0; i < pts.Length; i += 2) { seg[i] = pts[i]; seg[i + 1 < pts.Length ? i + 1 : i] = pts[(i + 1) % pts.Length]; }
            _c.DrawMultiline(seg, new Color(col, hot ? 0.85f : 0.35f), hot ? 1.8f : 1.1f, true);
        }
    }

    // Estrada: cascalho cinza com borda escura, opaca.
    private static readonly Color RoadEdge = new("#24221F"), RoadFill = new("#8D877C"), RoadLight = new("#B5AE9F");

    private void Road(Vector2[] pts, float w)
    {
        _c.DrawPolyline(pts, new Color(RoadEdge, 0.85f), 3.0f * w, true);
        _c.DrawPolyline(pts, RoadFill, 1.8f * w, true);
    }

    private void Trail(Vector2[] pts, float w)
    {
        int n = pts.Length / 2 * 2;
        if (n < 2) return;
        var seg = new Vector2[n];
        Array.Copy(pts, seg, n);
        _c.DrawMultiline(seg, new Color(RoadLight, 0.45f), Mathf.Max(1f, 0.9f * w), true);
    }

    /// <summary>Ponte de madeira: tabuleiro largo com guarda-corpos.</summary>
    private void Bridge(Vector2 a, Vector2 b, float w)
    {
        var dir = (b - a).Normalized();
        var side = new Vector2(-dir.Y, dir.X);
        var ext = dir * 1.5f * w;
        _c.DrawLine(a - ext, b + ext, new Color(0, 0, 0, 0.55f), 5.2f * w, true);
        _c.DrawLine(a - ext, b + ext, new Color("#6E5A44"), 3.8f * w, true);
        foreach (float k in new[] { -1f, 1f })
            _c.DrawLine(a - ext + side * 2.1f * w * k, b + ext + side * 2.1f * w * k, new Color("#3A2E22"), Mathf.Max(1f, 0.7f * w), true);
    }

    private void DrawGate(Vector2 gate, Vector2 towards, float w)
    {
        // Passagem: dois marcos de pedra de cada lado da estrada (discreto), no lote.
        var dir = (towards - gate).Normalized();
        var side = new Vector2(-dir.Y, dir.X);
        float s = 3.2f * w;
        for (int k = -1; k <= 1; k += 2)
        {
            var post = gate + side * s * k;
            _tb.Dot(post, 1.7f * w + 0.8f, new Color(RoadEdge, 0.85f));
            _tb.Dot(post, 1.7f * w, RoadLight);
        }
    }

    // ---------------- Cidades (muralha, casas e predios pelo nivel) ----------------

    private sealed class CityDeco
    {
        public RegionDefinition Zone = null!;
        public Vector2 Center;
        public float Wall;
        public int Level;
        public Vector2[] Ring = Array.Empty<Vector2>();      // vertices da muralha (mapa)
        public bool[] Open = Array.Empty<bool>();            // segmento i (Ring[i] -> Ring[i+1]) e abertura
        public List<(Vector2 C, Vector2 Half, float Angle, Color Roof)> Houses = new();
    }

    private readonly List<CityDeco> _cities = new();

    /// <summary>
    /// Monta o desenho de cada cidade a partir do nivel: muralha (paliçada no nivel 1-2, pedra
    /// com torres do 3 em diante) com aberturas onde as estradas passam, e casas espalhadas
    /// entre as ruas e os predios. Tudo estavel pelo ID (mesma cidade, mesmo desenho).
    /// </summary>
    private void BuildCities()
    {
        _cities.Clear();
        foreach (var z in _zones.Where(z => z.IsCity))
        {
            var c = _layout.Hub(z.Id);   // centro da cidade (posicao configuravel na zona)
            var deco = new CityDeco { Zone = z, Center = new Vector2(c.X, c.Y), Wall = _layout.CityWallRadius(z.Id), Level = Math.Clamp(z.CityLevel, 1, 5) };
            int sides = 10 + deco.Level * 2;
            ulong h = Simulation.Core.Random.StableHash.Fnv1a64(z.Id + "#muralha");
            var ring = new Vector2[sides + 1];
            for (int i = 0; i <= sides; i++)
            {
                float ang = i * Mathf.Tau / sides + (h % 100) / 100f;
                float jitter = 1f + ((StableNoise(h, i % sides) - 0.5f) * 0.06f);
                ring[i] = deco.Center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * deco.Wall * jitter;
            }
            deco.Ring = ring;
            // Aberturas: onde estradas (nao trilhas) cruzam o raio da muralha.
            var crossings = new List<Vector2>();
            foreach (var road in _layout.Roads.Where(r => r.ZoneId == z.Id && !r.IsTrail))
                foreach (var p in road.Points)
                    if (new Vector2(p.X, p.Y).DistanceTo(deco.Center) >= deco.Wall) { crossings.Add(new Vector2(p.X, p.Y)); break; }
            deco.Open = new bool[sides];
            for (int i = 0; i < sides; i++)
            {
                var mid = (ring[i] + ring[i + 1]) / 2f;
                deco.Open[i] = crossings.Any(x => x.DistanceTo(mid) < deco.Wall * Mathf.Tau / sides * 0.75f);
            }

            // Casas: quantidade pelo nivel; fora das ruas, dos predios e umas das outras.
            var roads = _layout.Roads.Where(r => r.ZoneId == z.Id).Select(r => r.Points.Select(p => new Vector2(p.X, p.Y)).ToArray()).ToList();
            var buildings = z.Areas.Select(a => (P: _layout.AreaCenter(z.Id, a.Id), R: _layout.AreaRadius(z.Id, a.Id))).ToList();
            var roofs = new[] { new Color("#8E4B3A"), new Color("#7A5A44"), new Color("#5D5F66"), new Color("#9A6B47"), new Color("#6B4436") };
            int want = 10 + deco.Level * 10;
            for (int k = 0; k < want * 6 && deco.Houses.Count < want; k++)
            {
                float rr = Mathf.Lerp(12f, deco.Wall - 5f, Mathf.Sqrt(StableNoise(h, 1000 + k * 3)));
                float ang = StableNoise(h, 1001 + k * 3) * Mathf.Tau;
                var pos = deco.Center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rr;
                if (buildings.Any(b => pos.DistanceTo(new Vector2(b.P.X, b.P.Y)) < b.R + 4)) continue;
                if (roads.Any(pts => DistanceToPolyline(pos, pts) < 5f)) continue;
                if (deco.Houses.Any(o => o.C.DistanceTo(pos) < 6.5f)) continue;
                float hw = 2.2f + StableNoise(h, 1002 + k * 3) * 1.4f;
                float hh = 1.6f + StableNoise(h, 2002 + k) * 1.0f;
                deco.Houses.Add((pos, new Vector2(hw, hh), ang + Mathf.Pi / 2, roofs[(int)(StableNoise(h, 3002 + k) * roofs.Length) % roofs.Length]));
            }
            _cities.Add(deco);
        }
    }

    private static float StableNoise(ulong seed, int i)
    {
        ulong x = unchecked(seed ^ ((ulong)(uint)i * 0x9E3779B97F4A7C15UL));
        x ^= x >> 33; x = unchecked(x * 0xff51afd7ed558ccdUL); x ^= x >> 33;
        return (x % 10000) / 10000f;
    }

    private static float DistanceToPolyline(Vector2 p, Vector2[] pts)
    {
        float best = float.MaxValue;
        for (int i = 0; i + 1 < pts.Length; i++)
        {
            var a = pts[i];
            var ab = pts[i + 1] - a;
            float t = ab.LengthSquared() > 0 ? Mathf.Clamp((p - a).Dot(ab) / ab.LengthSquared(), 0, 1) : 0;
            best = Mathf.Min(best, p.DistanceTo(a + ab * t));
        }
        return best;
    }

    private void DrawCity(CityDeco city, float w)
    {
        bool stone = city.Level >= 3;
        float wallW = Mathf.Clamp((stone ? 2.6f : 1.8f) * _zoom, 1.4f, stone ? 7f : 5f);
        var wallDark = stone ? new Color("#2B2824") : new Color("#2A2018");
        var wallFill = stone ? new Color("#9C9585") : new Color("#7A5B3C");
        var cityTint = new Color("#E0B45A");

        // Chao da area urbana (como as areas de caca), casas e predios: um lote de preenchimentos.
        int n = city.Ring.Length - 1;
        var ground = new Vector2[n];
        var core = new Vector2[n];
        float k = (city.Wall + 4f) / city.Wall;
        for (int i = 0; i < n; i++)
        {
            var d = city.Ring[i] - city.Center;
            ground[i] = ToScreen(city.Center + d * k);
            core[i] = ToScreen(city.Center + d * 0.62f);
        }
        var cc = ToScreen(city.Center);
        _tb.FanCenter(cc, ground, new Color(cityTint, 0.14f));
        _tb.FanCenter(cc, core, new Color(cityTint, 0.07f));

        bool houses = Detail >= 0.75f;
        var ridges = new List<Vector2>();
        var ridgeColors = new List<Color>();
        if (houses)
        {
            var shadowOffset = new Vector2(1.2f, 1.6f);
            var shadowColor = new Color(0, 0, 0, 0.45f);
            foreach (var (c, half, ang, roof) in city.Houses)
            {
                var x = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * half.X;
                var y = new Vector2(-Mathf.Sin(ang), Mathf.Cos(ang)) * half.Y;
                var p0 = ToScreen(c - x - y); var p1 = ToScreen(c + x - y); var p2 = ToScreen(c + x + y); var p3 = ToScreen(c - x + y);
                _tb.Quad(p0 + shadowOffset, p1 + shadowOffset, p2 + shadowOffset, p3 + shadowOffset, shadowColor);
                _tb.Quad(p0, p1, p2, p3, roof);
                ridges.Add((p0 + p3) / 2f); ridges.Add((p1 + p2) / 2f); ridgeColors.Add(roof.Darkened(0.35f));
            }
        }
        foreach (var a in city.Zone.Areas) BuildingFill(city.Zone, a, ridges, ridgeColors);
        _tb.Flush(_c);

        // Linhas: contorno tracejado da area urbana, cumeeiras e muralha.
        var dash = new List<Vector2>(n * 6);
        for (int i = 0; i < n; i++)
        {
            var a = ground[i];
            var b = ground[(i + 1) % n];
            for (float t = 0; t < 1f; t += 0.34f) { dash.Add(a.Lerp(b, t)); dash.Add(a.Lerp(b, Mathf.Min(1f, t + 0.2f))); }
        }
        _c.DrawMultiline(dash.ToArray(), new Color(cityTint, 0.45f), 1.2f, true);
        if (ridges.Count > 0) _c.DrawMultilineColors(ridges.ToArray(), ridgeColors.ToArray(), Mathf.Max(1f, 0.6f * _zoom), true);
        foreach (var a in city.Zone.Areas) BuildingLines(city.Zone, a);

        var wall = new List<Vector2>(city.Ring.Length * 2);
        for (int i = 0; i + 1 < city.Ring.Length; i++)
        {
            if (city.Open[i]) continue;
            wall.Add(ToScreen(city.Ring[i])); wall.Add(ToScreen(city.Ring[i + 1]));
        }
        if (wall.Count > 0)
        {
            var wa = wall.ToArray();
            _c.DrawMultiline(wa, wallDark, wallW + 1.6f, true);
            _c.DrawMultiline(wa, wallFill, wallW, true);
        }
        for (int i = 0; i + 1 < city.Ring.Length; i++)
        {
            bool gateSide = city.Open[i] || city.Open[(i + city.Open.Length - 1) % city.Open.Length];
            if (!stone && !gateSide) continue;
            if (!stone && city.Level < 2) continue;
            var t = ToScreen(city.Ring[i]);
            float tr = wallW * (gateSide ? 1.25f : 0.95f);
            _tb.Dot(t + new Vector2(1, 1.5f), tr + 1, new Color(0, 0, 0, 0.45f));
            _tb.Dot(t, tr + 0.8f, wallDark);
            _tb.Dot(t, tr, wallFill.Lightened(0.08f));
        }
        _tb.Flush(_c);
    }

    /// <summary>
    /// Area urbana: ocupa todo o espaco da muralha, com o mesmo desenho das areas de caca
    /// (mancha semiopaca, nucleo e contorno tracejado). O destaque e desenhado por cima.
    /// </summary>
    private void DrawUrbanGround(CityDeco city, bool hot)
    {
        var cityTint = new Color("#E0B45A");
        int n = city.Ring.Length - 1;
        var ground = new Vector2[n];
        var core = new Vector2[n];
        float k = (city.Wall + 4f) / city.Wall;
        for (int i = 0; i < n; i++)
        {
            var d = city.Ring[i] - city.Center;
            ground[i] = ToScreen(city.Center + d * k);
            core[i] = ToScreen(city.Center + d * 0.62f);
        }
        _c.DrawColoredPolygon(ground, new Color(cityTint, hot ? 0.16f : 0.14f));
        _c.DrawColoredPolygon(core, new Color(cityTint, hot ? 0.07f : 0.07f));
        var seg = new List<Vector2>(n * 6);
        for (int i = 0; i < n; i++)
        {
            var a = ground[i];
            var b = ground[(i + 1) % n];
            for (float t = 0; t < 1f; t += 0.34f) { seg.Add(a.Lerp(b, t)); seg.Add(a.Lerp(b, Mathf.Min(1f, t + 0.2f))); }
        }
        _c.DrawMultiline(seg.ToArray(), new Color(cityTint, hot ? 0.95f : 0.45f), hot ? 2.0f : 1.2f, true);
    }

    /// <summary>
    /// Destaques a cada quadro (fora do cache): area, cidade ou predio sob o mouse ou
    /// selecionado. Assim passar o mouse nao redesenha o mapa inteiro.
    /// </summary>
    private void DrawHighlights()
    {
        foreach (var (z, a, shape) in _areas)
        {
            string key = z.Id + "#" + a.Id;
            if (key != _hoveredArea && key != SelectedAreaKey) continue;
            if (z.IsCity)
            {
                var cm = _layout.AreaCenter(z.Id, a.Id);
                _c.DrawArc(ToScreen(cm), _layout.AreaRadius(z.Id, a.Id) * _zoom, 0, Mathf.Tau, 32, new Color(UiTokens.AccentSoft, 0.9f), 2f, true);
            }
            else DrawAreaGround(z, a, shape, hot: true);
        }
        foreach (var city in _cities)
        {
            string id = city.Zone.Id;
            if (_hoveredUrban == id || (_hoveredArea?.StartsWith(id + "#", StringComparison.Ordinal) ?? false) || SelectedRegionId == id)
                DrawUrbanGround(city, true);
        }
    }

    private static Color RoofOf(string kind) => kind switch
    {
        AreaKinds.Forge => new Color("#4B4A4F"),
        AreaKinds.Tavern => new Color("#8A4F2E"),
        AreaKinds.Shop => new Color("#6F5B8E"),
        _ => new Color("#7A6A58"),
    };

    /// <summary>Preenchimentos do predio de uma area da cidade (praca, campo de treino, lojas...).</summary>
    private void BuildingFill(RegionDefinition z, AreaDefinition a, List<Vector2> ridges, List<Color> ridgeColors)
    {
        var cm = _layout.AreaCenter(z.Id, a.Id);
        float r = _layout.AreaRadius(z.Id, a.Id);
        var c = new Vector2(cm.X, cm.Y);
        var cs = ToScreen(c);
        switch (a.Kind)
        {
            case AreaKinds.TownSquare:
                _tb.Dot(cs, r * 0.85f * _zoom, new Color("#B7AE9C"));
                _tb.Dot(cs, r * 0.22f * _zoom, new Color("#4A7C99"));
                break;
            case AreaKinds.TrainingGround:
            {
                var pts = new Vector2[20];
                for (int i = 0; i < 20; i++) pts[i] = ToScreen(c + new Vector2(Mathf.Cos(i * Mathf.Tau / 20) * r * 0.95f, Mathf.Sin(i * Mathf.Tau / 20) * r * 0.7f));
                _tb.Convex(pts, new Color("#C2A774"));
                break;
            }
            default:
            {
                var roof = RoofOf(a.Kind);
                float hw = r * 0.85f, hh = r * 0.62f;
                var p0 = ToScreen(c + new Vector2(-hw, -hh)); var p1 = ToScreen(c + new Vector2(hw, -hh));
                var p2 = ToScreen(c + new Vector2(hw, hh)); var p3 = ToScreen(c + new Vector2(-hw, hh));
                var sh = new Vector2(2f, 2.5f);
                _tb.Quad(p0 + sh, p1 + sh, p2 + sh, p3 + sh, new Color(0, 0, 0, 0.5f));
                _tb.Quad(p0, p1, p2, p3, roof);
                _tb.Quad(p0, p1, (p1 + p2) / 2f, (p0 + p3) / 2f, roof.Lightened(0.12f));
                ridges.Add((p0 + p3) / 2f); ridges.Add((p1 + p2) / 2f); ridgeColors.Add(roof.Darkened(0.4f));
                break;
            }
        }
    }

    /// <summary>Linhas do predio (borda da praca, cerca do campo de treino).</summary>
    private void BuildingLines(RegionDefinition z, AreaDefinition a)
    {
        var cm = _layout.AreaCenter(z.Id, a.Id);
        float r = _layout.AreaRadius(z.Id, a.Id);
        var c = new Vector2(cm.X, cm.Y);
        if (a.Kind == AreaKinds.TownSquare)
            _c.DrawArc(ToScreen(c), r * 0.85f * _zoom, 0, Mathf.Tau, 32, new Color("#5E574C"), Mathf.Max(1f, 0.6f * _zoom), true);
        else if (a.Kind == AreaKinds.TrainingGround)
        {
            var pts = new Vector2[21];
            for (int i = 0; i <= 20; i++) pts[i] = ToScreen(c + new Vector2(Mathf.Cos(i * Mathf.Tau / 20) * r * 0.95f, Mathf.Sin(i * Mathf.Tau / 20) * r * 0.7f));
            _c.DrawPolyline(pts, new Color("#5A4632"), Mathf.Max(1f, 0.7f * _zoom), true);
        }
    }

    private void DrawCityLights()
    {
        float night = 1f - _daylight;
        if (night <= 0.01f) return;
        foreach (var z in _zones.Where(z => z.IsCity))
        {
            var p = ToScreen(_layout.Hub(z.Id));
            float rad = _layout.CityWallRadius(z.Id) * 0.8f * _zoom;
            float flicker = 0.92f + 0.08f * Mathf.Sin(_time * 3.1f);
            for (int i = 6; i >= 1; i--) _c.DrawCircle(p, rad * (1f + i * 0.25f), new Color(1f, 0.72f, 0.38f, 0.03f * night * flicker));
        }
    }

    private void IconCastle(Vector2 c, float s)
    {
        var wall = new Color("#EFE6D2");
        var shadow = new Color(0, 0, 0, 0.45f);
        var off = new Vector2(1.5f, 2f);
        void R(float x, float y, float w2, float h, Color col)
        {
            var a = c + new Vector2(x, y); var b = c + new Vector2(x + w2, y); var d = c + new Vector2(x + w2, y + h); var e = c + new Vector2(x, y + h);
            _tb.Quad(a + off, b + off, d + off, e + off, shadow);
            _tb.Quad(a, b, d, e, col);
        }
        void T(Vector2 a, Vector2 b, Vector2 d) => _tb.Tri(c + a, c + b, c + d, UiTokens.Accent);
        R(-s * 0.85f, -s * 0.05f, s * 1.7f, s * 0.75f, wall);
        R(-s * 0.95f, -s * 0.55f, s * 0.42f, s * 1.25f, wall);
        R(s * 0.53f, -s * 0.55f, s * 0.42f, s * 1.25f, wall);
        R(-s * 0.24f, -s * 0.85f, s * 0.48f, s * 1.55f, wall);
        T(new Vector2(-s, -s * 0.55f), new Vector2(-s * 0.74f, -s * 0.95f), new Vector2(-s * 0.48f, -s * 0.55f));
        T(new Vector2(s * 0.48f, -s * 0.55f), new Vector2(s * 0.74f, -s * 0.95f), new Vector2(s, -s * 0.55f));
        T(new Vector2(-s * 0.3f, -s * 0.85f), new Vector2(0, -s * 1.3f), new Vector2(s * 0.3f, -s * 0.85f));
        var lit = new Color(1f, 0.8f, 0.45f, 0.25f + 0.75f * (1f - _daylight));
        _tb.Quad(c + new Vector2(-s * 0.06f, -s * 0.5f), c + new Vector2(s * 0.06f, -s * 0.5f), c + new Vector2(s * 0.06f, -s * 0.32f), c + new Vector2(-s * 0.06f, -s * 0.32f), lit);
    }

    /// <summary>Animacoes leves desenhadas a cada quadro: pulso dos portais e brilho das forjas.</summary>
    private void DrawAnimated()
    {
        foreach (var (z, a, _) in _areas)
        {
            if (AreaKinds.IsPortal(a.Kind) && !z.IsCity)
            {
                var c = ToScreen(_layout.AreaCenter(z.Id, a.Id));
                float r = _layout.AreaRadius(z.Id, a.Id) * _zoom;
                var col = AreaPalette.Color(a.Kind);
                float pulse = 0.5f + 0.5f * Mathf.Sin(_time * 2.4f + c.X * 0.01f);
                for (int i = 3; i >= 1; i--) _c.DrawCircle(c, r * (0.5f + i * 0.18f), new Color(col, 0.05f + 0.04f * pulse));
            }
            else if (z.IsCity && a.Kind == AreaKinds.Forge)
            {
                var cm = _layout.AreaCenter(z.Id, a.Id);
                float r = _layout.AreaRadius(z.Id, a.Id);
                var pos = ToScreen(new Vector2(cm.X + r * 0.85f * 0.6f, cm.Y - r * 0.62f * 0.4f));
                _c.DrawCircle(pos, Mathf.Max(1.5f, 1.2f * _zoom), new Color(1f, 0.55f, 0.15f, 0.7f + 0.3f * Mathf.Sin(_time * 6f)));
            }
        }
    }

    // ---------------- Sobreposicao ----------------

    private void PaintOverlay(MapOverlay o)
    {
        if (_zones.Length == 0) return;
        var t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        PaintOverlayInner(o);
        LastOverlayMs += System.Diagnostics.Stopwatch.GetElapsedTime(t0).TotalMilliseconds;
    }

    private void PaintStaticOverlay(MapOverlay o)
    {
        if (_zones.Length == 0) return;
        var t0 = System.Diagnostics.Stopwatch.GetTimestamp();
        foreach (var (z, a, _) in _areas) AreaEmblem(o, z, a);
        foreach (var z in _zones) if (z.IsCity) ZoneLabel(o, z);
        LastOverlayMs += System.Diagnostics.Stopwatch.GetElapsedTime(t0).TotalMilliseconds;
    }

    private void PaintOverlayInner(MapOverlay o)
    {
        // Itens em foco (hover/selecao) por cima do cache, a cada quadro.
        foreach (var (z, a, _) in _areas)
        {
            string key = z.Id + "#" + a.Id;
            if (key == _hoveredArea || key == SelectedAreaKey) AreaEmblem(o, z, a, focus: true);
        }
        foreach (var id in new[] { HoveredRegionId, SelectedRegionId }.Distinct())
            if (id != null && _content.Regions.TryGetValue(id, out var fz))
                ZoneLabel(o, fz, focused: true);
        for (int i = 0; i < _clusters.Count; i++) ClusterBubble(o, _clusters[i], i == _hoveredCluster);
        SelectedPersonMarker(o);
        DrawCompass(o);
        if (!_dragging) Tooltip(o);
    }

    private void AreaEmblem(MapOverlay o, RegionDefinition z, AreaDefinition a, bool focus = false)
    {
        string key = z.Id + "#" + a.Id;
        var c = ToScreen(_layout.AreaCenter(z.Id, a.Id));
        // Mesma geometria no cache e no foco: o emblema em foco cobre exatamente o de baixo
        // (sem duplicar); o destaque e um anel externo.
        float r = EmblemRadius();
        var col = AreaPalette.Color(a.Kind);

        // Afastado (mundo todo e lotes livres): o emblema vira um ponto discreto.
        if (Detail < 0.75f && !focus)
        {
            float dot = Mathf.Clamp(_layout.AreaRadius(z.Id, a.Id) * _zoom * 0.5f, 2.5f, 6f);
            o.DrawCircle(c, dot + 1.2f, new Color(0, 0, 0, 0.55f));
            o.DrawCircle(c, dot, col);
            return;
        }
        var people = PeopleInArea(z.Id, a.Id);

        o.DrawCircle(c + new Vector2(1.5f, 3f), r + 2.5f, new Color(0, 0, 0, 0.5f));
        o.DrawCircle(c, r, new Color(UiTokens.Background, 0.94f));
        if (_crowded.Contains(key))
        {
            // Lotado: anel em fatias por perfil (proporcoes reais) no lugar dos pontos.
            float start = -Mathf.Pi / 2f;
            foreach (var g in people.GroupBy(id => _persons[id - 1].ProfileId).OrderBy(g => g.Key, StringComparer.Ordinal))
            {
                float span = Mathf.Tau * g.Count() / people.Count;
                o.DrawArc(c, r + 3.5f, start, start + span - 0.05f, 24, UiTokens.ProfileColor(g.Key), 4f, true);
                start += span;
            }
        }
        o.DrawArc(c, r, 0, Mathf.Tau, 32, col, 2f, true);
        if (focus) o.DrawArc(c, r + 3.5f, 0, Mathf.Tau, 40, new Color(UiTokens.AccentSoft, 0.95f), 2f, true);
        // Icone do tipo de area (etapa 4), em tamanhos pares para o cache de texturas nao crescer com o zoom.
        int isz = Mathf.Clamp(Mathf.RoundToInt(r * 0.62f) * 2, 10, 48);
        o.DrawTextureRect(Icons.Get(GameIcons.Area(a.Kind), isz, isz >= 24 ? 2f : 1.6f), new Rect2((c - new Vector2(isz, isz) / 2f).Round(), new Vector2(isz, isz)), false, col);

        if (people.Count > 0)
        {
            string text = people.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var f = UiFonts.Numbers;
            var ts = f.GetStringSize(text, HorizontalAlignment.Left, -1, 11);
            var bc = c + new Vector2(r * 0.78f, -r * 0.78f);
            float br = Mathf.Max(8.5f, ts.X / 2f + 5f);
            o.DrawCircle(bc, br + 1.5f, new Color(0, 0, 0, 0.6f));
            o.DrawCircle(bc, br, UiTokens.Info.Darkened(0.35f));
            o.DrawString(f, new Vector2(bc.X - ts.X / 2f, bc.Y + f.GetAscent(11) / 2f - 1), text, HorizontalAlignment.Left, -1, 11, UiTokens.TextPrimary);
        }

        // Nome da area ao aproximar (no cache) ou em foco (so se o cache ainda nao o desenhou).
        bool nameInCache = Detail >= 2.4f;
        if (focus ? !nameInCache : nameInCache)
        {
            var f = UiFonts.SemiBold;
            int size = Detail >= 2.6f ? 12 : 11;
            string name = a.Name;
            var ts = f.GetStringSize(name, HorizontalAlignment.Left, -1, size);
            var pos = new Vector2(c.X - ts.X / 2f, c.Y + r + 6 + f.GetAscent(size));
            o.DrawStringOutline(f, pos, name, HorizontalAlignment.Left, -1, size, 5, new Color(0.02f, 0.03f, 0.04f, 0.9f));
            o.DrawString(f, pos, name, HorizontalAlignment.Left, -1, size, UiTokens.TextPrimary);
            if (AreaKinds.IsPortal(a.Kind) && a.LeadsTo.Length > 0)
            {
                string sub = $"→ {a.LeadsTo}";
                var ts2 = f.GetStringSize(sub, HorizontalAlignment.Left, -1, 10);
                var p2 = new Vector2(c.X - ts2.X / 2f, pos.Y + 13);
                o.DrawStringOutline(f, p2, sub, HorizontalAlignment.Left, -1, 10, 4, new Color(0.02f, 0.03f, 0.04f, 0.9f));
                o.DrawString(f, p2, sub, HorizontalAlignment.Left, -1, 10, col);
            }
        }
    }

    private void ZoneLabel(MapOverlay o, RegionDefinition z, bool focused = false)
    {
        var c = _layout.Center(z.Id);
        var outline = new Color(0.02f, 0.03f, 0.04f, 0.85f);
        // So a cidade tem nome sempre (logo abaixo da muralha); as outras zonas mostram nome e
        // regras ao passar o mouse ou quando selecionadas.
        if (!z.IsCity && !focused) return;
        var hubPos = _layout.Hub(z.Id);
        var at = z.IsCity
            ? ToScreen(new Vector2(hubPos.X, hubPos.Y + _layout.CityWallRadius(z.Id) + 6)) + new Vector2(0, 14)
            : ToScreen(new Vector2(c.X, c.Y - _layout.ZoneRadius * 0.80f));
        // Topo da zona fora da tela: o nome desce para dentro da vista (nao some atras da barra).
        at.Y = Mathf.Max(at.Y, _viewArea.Position.Y + 34);

        // Nome: cabe na largura da zona (encolhe ate 10 px); afastado demais, so em foco.
        var font = UiFonts.Display;
        int size = (int)Mathf.Clamp(13 * Mathf.Sqrt(Detail), 13, 20);
        float room = _layout.ZoneRadius * 2f * _zoom * 0.86f;
        while (size > 10 && font.GetStringSize(z.Name, HorizontalAlignment.Left, -1, size).X > room) size--;
        var ts = font.GetStringSize(z.Name, HorizontalAlignment.Left, -1, size);
        if (ts.X > room && !focused) return;

        // Jogadores na zona: numero discreto ao lado do nome (so quando ha alguem).
        int people = PeopleInZone(z.Id);
        var nf = UiFonts.Numbers;
        int csize = Math.Max(10, size - 3);
        string count = people.ToString(System.Globalization.CultureInfo.InvariantCulture);
        float cw = people > 0 ? nf.GetStringSize(count, HorizontalAlignment.Left, -1, csize).X + 12 : 0;
        float x0 = at.X - (ts.X + (cw > 0 ? cw + 6 : 0)) / 2f;
        var pos = new Vector2(x0, at.Y);
        var nameColor = z.Id == SelectedRegionId ? UiTokens.AccentSoft : new Color(UiTokens.TextPrimary, 0.92f);
        o.DrawStringOutline(font, pos, z.Name, HorizontalAlignment.Left, -1, size, 5, outline);
        o.DrawString(font, pos, z.Name, HorizontalAlignment.Left, -1, size, nameColor);
        if (people > 0)
        {
            var bc = new Vector2(x0 + ts.X + 6 + cw / 2f, at.Y - font.GetAscent(size) * 0.35f);
            float br = cw / 2f;
            o.DrawCircle(bc, br + 1.2f, new Color(0, 0, 0, 0.6f));
            o.DrawCircle(bc, br, UiTokens.Info.Darkened(0.35f));
            var cts = nf.GetStringSize(count, HorizontalAlignment.Left, -1, csize);
            o.DrawString(nf, new Vector2(bc.X - cts.X / 2f, bc.Y + nf.GetAscent(csize) / 2f - 1), count, HorizontalAlignment.Left, -1, csize, UiTokens.TextPrimary);
        }

        // Linha de regras em texto simples (sem caixas): so em foco.
        if (!focused) return;
        var parts = new List<(string Text, Color Color)>
        {
            (z.IsCity ? "Cidade" : $"Nív {z.LevelMin}–{z.LevelMax}", z.IsCity ? UiTokens.Accent : UiTokens.Info),
        };
        if (z.Pvp == PvpRules.Open) parts.Add(("PvP", UiTokens.Danger));
        if (z.DeathRule == DeathRules.FullLoot) parts.Add(("☠ saque total", UiTokens.Danger));
        else if (z.DeathRule == DeathRules.Partial) parts.Add(("perda parcial", UiTokens.Warning));
        if (Layer == MapLayer.Crowding) parts.Add(($"{people * 100 / Math.Max(1, z.Capacity)}% lotação", LayerTint(z).Color));
        if (Layer == MapLayer.Danger) parts.Add(($"perigo {(int)(DangerOf(z) * 100)}", Ramp(DangerOf(z))));
        var tf = UiFonts.SemiBold;
        const int tsz = 11;
        const string sep = "  ·  ";
        float sepW = tf.GetStringSize(sep, HorizontalAlignment.Left, -1, tsz).X;
        float total = parts.Sum(t => tf.GetStringSize(t.Text, HorizontalAlignment.Left, -1, tsz).X) + sepW * (parts.Count - 1);
        float x = at.X - total / 2f, y = at.Y + 4 + tf.GetAscent(tsz);
        for (int i = 0; i < parts.Count; i++)
        {
            var (text, color) = parts[i];
            o.DrawStringOutline(tf, new Vector2(x, y), text, HorizontalAlignment.Left, -1, tsz, 4, outline);
            o.DrawString(tf, new Vector2(x, y), text, HorizontalAlignment.Left, -1, tsz, color);
            x += tf.GetStringSize(text, HorizontalAlignment.Left, -1, tsz).X;
            if (i + 1 < parts.Count)
            {
                o.DrawString(tf, new Vector2(x, y), sep, HorizontalAlignment.Left, -1, tsz, new Color(UiTokens.TextSecondary, 0.7f));
                x += sepW;
            }
        }
    }

    private void ClusterBubble(MapOverlay o, (string Key, Vector2 Screen, float Radius, List<int> People) c, bool hover)
    {
        float r = c.Radius * (hover ? 1.08f : 1f);
        o.DrawCircle(c.Screen + new Vector2(2, 3), r + 3, new Color(0, 0, 0, 0.45f));
        o.DrawCircle(c.Screen, r, new Color(UiTokens.Background, 0.95f));
        float start = -Mathf.Pi / 2f;
        foreach (var g in c.People.GroupBy(id => _persons[id - 1].ProfileId).OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            float span = Mathf.Tau * g.Count() / c.People.Count;
            o.DrawArc(c.Screen, r - 2.5f, start, start + span - 0.04f, 24, UiTokens.ProfileColor(g.Key), 4.5f, true);
            start += span;
        }
        if (hover) o.DrawArc(c.Screen, r + 4, 0, Mathf.Tau, 40, UiTokens.AccentSoft, 2f, true);
        var font = UiFonts.Numbers;
        string text = c.People.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var ts = font.GetStringSize(text, HorizontalAlignment.Left, -1, 13);
        o.DrawString(font, new Vector2(c.Screen.X - ts.X / 2f, c.Screen.Y + font.GetAscent(13) / 2f - 1), text, HorizontalAlignment.Left, -1, 13, UiTokens.TextPrimary);
    }

    private void SelectedPersonMarker(MapOverlay o)
    {
        if (SelectedPersonId <= 0 || SelectedPersonId >= _personVisible.Length) return;
        var view = _snap?.ByPerson[SelectedPersonId];
        if (view == null || view.Status == CharacterStatus.Offline) return;
        var p = _personScreen[SelectedPersonId];
        float pulse = 0.5f + 0.5f * Mathf.Sin(_time * 5f);
        o.DrawArc(p, 11 + pulse * 3, 0, Mathf.Tau, 32, new Color(UiTokens.Accent, 0.9f), 2.2f, true);
        var font = UiFonts.SemiBold;
        var ts = font.GetStringSize(view.Name, HorizontalAlignment.Left, -1, 13);
        var rect = new Rect2(p.X - ts.X / 2f - 8, p.Y - 42, ts.X + 16, font.GetHeight(13) + 8);
        o.DrawStyleBox(ThemeFactory.Box(new Color(UiTokens.Background, 0.92f), 8, border: UiTokens.Accent, radius: 9), rect);
        o.DrawLine(new Vector2(p.X, rect.End.Y), p + new Vector2(0, -13), new Color(UiTokens.Accent, 0.8f), 1.5f, true);
        o.DrawString(font, new Vector2(rect.Position.X + 8, rect.Position.Y + 4 + font.GetAscent(13)), view.Name, HorizontalAlignment.Left, -1, 13, UiTokens.AccentSoft);
    }

    private void Tooltip(MapOverlay o)
    {
        var lines = new List<(string, Font, int, Color)>();
        if (_hoveredCluster >= 0)
        {
            var c = _clusters[_hoveredCluster];
            lines.Add(($"{c.People.Count} jogadores na encruzilhada", UiFonts.SemiBold, 14, UiTokens.TextPrimary));
            lines.Add(("Clique para ver a lista", UiFonts.Regular, 12, UiTokens.AccentSoft));
        }
        else if (HoveredPersonId > 0 && _snap?.ByPerson[HoveredPersonId] is { } v)
        {
            var person = _persons[HoveredPersonId - 1];
            var prof = _content.Profiles.TryGetValue(person.ProfileId, out var pr) ? pr.Name : "";
            lines.Add(($"{v.Name} · nível {v.Level}", UiFonts.SemiBold, 14, UiTokens.TextPrimary));
            lines.Add((StatusText.Of(v, _content), UiFonts.Regular, 12, UiTokens.TextSecondary));
            lines.Add(($"{person.Name} · {prof}", UiFonts.Regular, 12, UiTokens.ProfileColor(person.ProfileId)));
            lines.Add(("Clique para abrir a ficha", UiFonts.Regular, 12, UiTokens.AccentSoft));
        }
        else if (_hoveredUrban != null)
        {
            var z = _content.Regions[_hoveredUrban];
            var deco = _cities.First(c => c.Zone.Id == z.Id);
            lines.Add(("Área urbana", UiFonts.SemiBold, 14, UiTokens.TextPrimary));
            lines.Add(($"{z.Name} · cidade nível {deco.Level}", UiFonts.Regular, 12, new Color("#E0B45A")));
            lines.Add((string.Join(" · ", z.Areas.Select(a => a.Name)), UiFonts.Regular, 12, UiTokens.TextSecondary));
            int n = PeopleInZone(z.Id);
            lines.Add((n == 0 ? "Ninguém na cidade agora" : n == 1 ? "1 jogador na cidade" : $"{n} jogadores na cidade", UiFonts.Regular, 12, UiTokens.TextSecondary));
            lines.Add(("Clique para a ficha da cidade", UiFonts.Regular, 12, UiTokens.AccentSoft));
        }
        else if (_hoveredArea != null)
        {
            var parts = _hoveredArea.Split('#');
            var z = _content.Regions[parts[0]];
            var a = z.Areas.First(x => x.Id == parts[1]);
            int n = PeopleInArea(z.Id, a.Id).Count;
            lines.Add((a.Name, UiFonts.SemiBold, 14, UiTokens.TextPrimary));
            lines.Add(($"{AreaPalette.Name(a.Kind)} · {z.Name}", UiFonts.Regular, 12, AreaPalette.Color(a.Kind)));
            if (a.Activities.Count > 0)
                lines.Add((string.Join(" · ", a.Activities.Select(x => x.Name)), UiFonts.Regular, 12, UiTokens.TextSecondary));
            if (AreaKinds.IsPortal(a.Kind)) lines.Add(($"Leva a: {a.LeadsTo} (em breve)", UiFonts.Regular, 12, AreaPalette.Color(a.Kind)));
            lines.Add((n == 0 ? "Ninguém aqui agora" : n == 1 ? "1 jogador aqui" : $"{n} jogadores aqui", UiFonts.Regular, 12, UiTokens.TextSecondary));
            lines.Add(("Clique para abrir a área", UiFonts.Regular, 12, UiTokens.AccentSoft));
        }
        else if (HoveredRegionId != null)
        {
            var z = _content.Regions[HoveredRegionId];
            lines.Add((z.Name, UiFonts.SemiBold, 14, UiTokens.TextPrimary));
            lines.Add(($"{PeopleInZone(z.Id)} jogadores · {z.Areas.Count} áreas · {z.Connections.Count} saídas", UiFonts.Regular, 12, UiTokens.TextSecondary));
            lines.Add(("Clique para a ficha da zona · duplo clique para aproximar", UiFonts.Regular, 12, UiTokens.AccentSoft));
        }
        else if (_hoveredSlot is { } slot)
        {
            lines.Add(("Lote livre", UiFonts.SemiBold, 14, UiTokens.TextPrimary));
            lines.Add(($"Coluna {slot.Column + 1} · linha {slot.Row + 1} · {_grid.FreeSlots} lotes livres no mundo", UiFonts.Regular, 12, UiTokens.TextSecondary));
            lines.Add(("Espaço do tamanho de uma zona, reservado para expandir o mundo", UiFonts.Regular, 12, UiTokens.TextSecondary));
            lines.Add(("Ocupar lotes será feito no editor de mapa (em breve)", UiFonts.Regular, 12, UiTokens.AccentSoft));
        }
        if (lines.Count == 0) return;
        float w = 0, h = 0;
        foreach (var (t, f, s, _) in lines) { w = Mathf.Max(w, f.GetStringSize(t, HorizontalAlignment.Left, -1, s).X); h += f.GetHeight(s) + 2; }
        var size = new Vector2(w + 24, h + 18);
        var pos = _mouse + new Vector2(18, 18);
        var vp = GetViewportRect().Size;
        if (pos.X + size.X > vp.X - 8) pos.X = _mouse.X - size.X - 12;
        if (pos.Y + size.Y > vp.Y - 8) pos.Y = _mouse.Y - size.Y - 12;
        o.DrawStyleBox(_tip, new Rect2(pos, size));
        float y = pos.Y + 9;
        foreach (var (t, f, s, col) in lines) { o.DrawString(f, new Vector2(pos.X + 12, y + f.GetAscent(s)), t, HorizontalAlignment.Left, -1, s, col); y += f.GetHeight(s) + 2; }
    }

    private void DrawCompass(MapOverlay o)
    {
        var c = new Vector2(_viewArea.End.X - 44, _viewArea.Position.Y + 44);
        o.DrawCircle(c, 26, new Color(UiTokens.Background, 0.55f));
        o.DrawArc(c, 26, 0, Mathf.Tau, 48, new Color(UiTokens.Road, 0.6f), 1.4f, true);
        o.DrawColoredPolygon(new[] { c + new Vector2(0, -20), c + new Vector2(5, 0), c + new Vector2(-5, 0) }, UiTokens.Accent);
        o.DrawColoredPolygon(new[] { c + new Vector2(0, 20), c + new Vector2(5, 0), c + new Vector2(-5, 0) }, new Color(UiTokens.TextSecondary, 0.7f));
        var ts = UiFonts.Display.GetStringSize("N", HorizontalAlignment.Left, -1, 11);
        o.DrawString(UiFonts.Display, c + new Vector2(-ts.X / 2f, -29), "N", HorizontalAlignment.Left, -1, 11, UiTokens.AccentSoft);
    }
}

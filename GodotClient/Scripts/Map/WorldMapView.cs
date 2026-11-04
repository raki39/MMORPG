using Godot;
using Simulation.Core.Content;
using Simulation.Core.Population;
using Simulation.Core.World;
using GodotClient.Sim;
using GodotClient.Ui;

namespace GodotClient.Map;

public enum MapLayer { Normal, Crowding, Danger, Level }

/// <summary>
/// Mapa estrategico em zonas (estilo Albion): celulas de escala unica com fronteiras,
/// estradas (saida <-> encruzilhada), trilhas ate os pontos de atividade e as pessoas nas
/// posicoes reais do Core. Aglomeracoes viram bolhas clicaveis. Camadas mudam a leitura.
/// Camadas de desenho: terreno (shader) -> estradas/pontos (_Draw) -> pessoas (MultiMesh)
/// -> sobreposicao (rotulos, bolhas, selecao, tooltip, bussola).
/// </summary>
public partial class WorldMapView : Node2D
{
    [Signal] public delegate void RegionSelectedEventHandler(string regionId);
    [Signal] public delegate void RegionOpenedEventHandler(string regionId);
    [Signal] public delegate void PersonSelectedEventHandler(int personId);
    [Signal] public delegate void ClusterSelectedEventHandler(string spotKey, Vector2 screenPosition);

    private const int ClusterThreshold = 5;

    private ContentDatabase _content = null!;
    private ZoneLayout _layout = null!;
    private IReadOnlyList<PersonInfo> _persons = Array.Empty<PersonInfo>();
    private SimSnapshot? _snap;
    private ShaderMaterial _terrain = null!;
    private PopulationLayer _population = null!;
    private MapOverlay _overlay = null!;
    private RegionDefinition[] _zones = Array.Empty<RegionDefinition>();
    private readonly Dictionary<string, int> _zoneIndex = new(StringComparer.Ordinal);

    // Estado por quadro.
    private Vector2[] _personScreen = Array.Empty<Vector2>();
    private bool[] _personVisible = Array.Empty<bool>();
    private readonly Dictionary<string, List<int>> _bySpot = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _zonePeople = new(StringComparer.Ordinal);
    private readonly List<(string Key, Vector2 Screen, float Radius, List<int> People)> _clusters = new();

    private Vector2 _pan, _targetPan;
    private float _zoom = 1f, _targetZoom = 1f, _fitZoom = 1f;
    private Rect2 _viewArea;
    private bool _pressed, _dragging;
    private Vector2 _pressPos, _mouse;
    private float _time;
    private float _daylight = 1f;
    private StyleBoxFlat _tip = null!, _pill = null!;

    public MapLayer Layer { get; set; } = MapLayer.Normal;
    public string? SelectedRegionId { get; private set; }
    public int SelectedPersonId { get; private set; }
    public int FollowPersonId { get; private set; }
    public string? HoveredRegionId { get; private set; }
    public int HoveredPersonId { get; private set; }
    private string? _hoveredSpot;
    private int _hoveredCluster = -1;

    public void Setup(ContentDatabase content, ZoneLayout layout, IReadOnlyList<PersonInfo> persons, ColorRect terrainRect)
    {
        _content = content;
        _layout = layout;
        _persons = persons;
        _zones = content.Regions.Values.Take(16).ToArray();
        for (int i = 0; i < _zones.Length; i++) _zoneIndex[_zones[i].Id] = i;

        _terrain = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/terrain.gdshader") };
        terrainRect.Material = _terrain;
        var data = new Vector4[16];
        for (int i = 0; i < _zones.Length; i++)
            data[i] = new Vector4(_zones[i].MapPosition.X, _zones[i].MapPosition.Y, layout.ZoneRadius, UiTokens.BiomeIndex(_zones[i].Biome));
        _terrain.SetShaderParameter("zones", data);
        _terrain.SetShaderParameter("zone_count", _zones.Length);

        _population = new PopulationLayer();
        _population.Init(persons.Count);
        AddChild(_population);
        _overlay = new MapOverlay { Painter = PaintOverlay };
        AddChild(_overlay);
        _personScreen = new Vector2[persons.Count + 1];
        _personVisible = new bool[persons.Count + 1];

        _tip = ThemeFactory.Glass(12, 10);
        _pill = ThemeFactory.Box(new Color(UiTokens.Background, 0.80f), 8, border: new Color(UiTokens.Border, 0.9f), radius: 10);
    }

    public void SetSnapshot(SimSnapshot snap) => _snap = snap;

    public int PeopleInZone(string regionId) => _zonePeople.TryGetValue(regionId, out var n) ? n : 0;
    public int PeopleAtSpot(string regionId, string activityId) =>
        _bySpot.TryGetValue(regionId + "/" + activityId, out var l) ? l.Count : 0;
    public IReadOnlyList<int> PeopleAt(string spotKey) =>
        _bySpot.TryGetValue(spotKey, out var l) ? l : (IReadOnlyList<int>)Array.Empty<int>();

    // ---------------- Camera ----------------

    public void FitTo(Rect2 viewArea, bool instant = false)
    {
        _viewArea = viewArea;
        if (_zones.Length == 0) return;
        float r = _layout.ZoneRadius;
        var min = new Vector2(_zones.Min(z => z.MapPosition.X) - r, _zones.Min(z => z.MapPosition.Y) - r);
        var max = new Vector2(_zones.Max(z => z.MapPosition.X) + r, _zones.Max(z => z.MapPosition.Y) + r);
        var size = (max - min) * 1.08f;
        _fitZoom = Mathf.Min(viewArea.Size.X / size.X, viewArea.Size.Y / size.Y);
        _targetZoom = _fitZoom;
        _targetPan = viewArea.GetCenter() - (min + max) / 2f * _targetZoom;
        FollowPersonId = 0;
        if (instant) { _zoom = _targetZoom; _pan = _targetPan; }
    }

    public void ZoomBy(float factor) => ZoomAround(_viewArea.GetCenter(), factor);

    public void FocusRegion(string regionId)
    {
        if (!_zoneIndex.ContainsKey(regionId)) return;
        var c = _layout.Center(regionId);
        _targetZoom = Mathf.Clamp(_fitZoom * 2.2f, _fitZoom * 0.6f, _fitZoom * 6f);
        _targetPan = _viewArea.GetCenter() - new Vector2(c.X, c.Y) * _targetZoom;
        FollowPersonId = 0;
    }

    public void FocusPerson(int personId, bool follow)
    {
        FollowPersonId = follow ? personId : 0;
        _targetZoom = Mathf.Max(_targetZoom, _fitZoom * 2.6f);
        var p = MapPositionOf(personId);
        if (p.HasValue) _targetPan = _viewArea.GetCenter() - p.Value * _targetZoom;
    }

    private void ZoomAround(Vector2 screen, float factor)
    {
        var anchor = (screen - _targetPan) / _targetZoom;
        _targetZoom = Mathf.Clamp(_targetZoom * factor, _fitZoom * 0.6f, _fitZoom * 6f);
        _targetPan = screen - anchor * _targetZoom;
    }

    private Vector2 ToScreen(Vector2 map) => map * _zoom + _pan;
    private Vector2 ToScreen(MapPos p) => new Vector2(p.X, p.Y) * _zoom + _pan;
    private Vector2 ToMap(Vector2 screen) => (screen - _pan) / _zoom;

    public void SetHour(double hour)
    {
        float h = (float)hour;
        _daylight = Mathf.SmoothStep(4.5f, 7f, h) * (1f - Mathf.SmoothStep(18f, 20.5f, h));
    }

    public void SelectRegion(string? regionId)
    {
        SelectedRegionId = regionId;
    }

    public void SelectPerson(int personId) => SelectedPersonId = personId;

    // ---------------- Quadro a quadro ----------------

    public override void _Process(double delta)
    {
        if (_zones.Length == 0) return;
        float dt = (float)delta;
        _time += dt;

        if (FollowPersonId > 0)
        {
            var p = MapPositionOf(FollowPersonId);
            if (p.HasValue) _targetPan = _viewArea.GetCenter() - p.Value * _targetZoom;
        }
        float k = 1f - Mathf.Exp(-dt * 10f);
        _zoom = Mathf.Lerp(_zoom, _targetZoom, k);
        _pan = _pan.Lerp(_targetPan, k);

        UpdatePopulation();
        UpdateHover();
        UpdateTerrainUniforms();
        QueueRedraw();
        _overlay.QueueRedraw();
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
        var tints = new Vector4[16];
        for (int i = 0; i < _zones.Length; i++)
        {
            var (c, a) = LayerTint(_zones[i]);
            tints[i] = new Vector4(c.R, c.G, c.B, a);
        }
        _terrain.SetShaderParameter("zone_tint", tints);
    }

    /// <summary>Cor e intensidade da camada ativa para a zona.</summary>
    public (Color Color, float Alpha) LayerTint(RegionDefinition z) => Layer switch
    {
        MapLayer.Crowding => (Ramp(Mathf.Clamp(PeopleInZone(z.Id) / (float)Math.Max(1, z.Capacity) * 4f, 0, 1)), 0.62f),
        MapLayer.Danger => (Ramp(DangerOf(z)), 0.62f),
        MapLayer.Level => (new Color("#3E6FB0").Lerp(new Color("#9B4FC6"), Mathf.Clamp((z.LevelMin - 1) / 15f, 0, 1)), 0.62f),
        _ => (Colors.Black, 0f),
    };

    public static float DangerOf(RegionDefinition z)
    {
        if (z.IsCity) return 0f;
        float risk = z.Activities.Count == 0 ? 0 : (float)z.Activities.Average(a => a.BaseRiskPermille) / 1000f;
        float pvp = z.Pvp == PvpRules.Open ? 0.45f : z.Pvp == PvpRules.Consensual ? 0.15f : 0f;
        float death = z.DeathRule == DeathRules.FullLoot ? 0.3f : z.DeathRule == DeathRules.Partial ? 0.15f : 0f;
        return Mathf.Clamp(risk * 0.6f + pvp + death, 0, 1);
    }

    public static Color Ramp(float t) => t < 0.5f
        ? UiTokens.Success.Lerp(UiTokens.Warning, t * 2f)
        : UiTokens.Warning.Lerp(UiTokens.Danger, (t - 0.5f) * 2f);

    private Vector2? MapPositionOf(int personId)
    {
        var v = _snap?.ByPerson is { } arr && personId < arr.Length ? arr[personId] : null;
        if (v == null || v.Status == CharacterStatus.Offline) return null;
        if (v.Status == CharacterStatus.Traveling)
        {
            var (x, y) = v.PositionAt(_snap!.Tick);
            return new Vector2(x, y);
        }
        return new Vector2(v.Position.X, v.Position.Y);
    }

    private void UpdatePopulation()
    {
        _bySpot.Clear();
        _zonePeople.Clear();
        _clusters.Clear();
        Array.Clear(_personVisible);
        if (_snap == null) return;
        var snap = _snap;

        // Agrupa quem esta parado no mesmo ponto (atividade ou encruzilhada).
        for (int id = 1; id < snap.ByPerson.Length; id++)
        {
            var v = snap.ByPerson[id];
            if (v == null || v.Status == CharacterStatus.Offline) continue;
            _zonePeople[v.RegionId] = (_zonePeople.TryGetValue(v.RegionId, out var n) ? n : 0) + 1;
            if (v.Status == CharacterStatus.Traveling) continue;
            string key = v.Status == CharacterStatus.Doing && v.ActivityId.Length > 0 ? v.TargetRegionId + "/" + v.ActivityId : v.RegionId + "/";
            if (!_bySpot.TryGetValue(key, out var list)) _bySpot[key] = list = new List<int>();
            list.Add(id);
        }

        float diameter = Mathf.Clamp(6.5f * _zoom * 1.4f, 6f, 15f);
        bool clusterMode = _zoom < _fitZoom * 2.4f;
        int written = 0;

        foreach (var (key, people) in _bySpot)
        {
            var parts = key.Split('/');
            var spot = _layout.Spot(parts[0], parts[1]);
            if (clusterMode && people.Count >= ClusterThreshold)
            {
                var center = ToScreen(spot);
                float r = 12f + Mathf.Sqrt(people.Count) * 2.6f;
                _clusters.Add((key, center, r, people));
                foreach (var id in people) _personScreen[id] = center;
                continue;
            }
            // Em volta do icone do ponto (em pixels), em espiral de angulo dourado:
            // ninguem fica escondido sob o icone e a ordem e estavel por ID.
            var spotScreen = ToScreen(spot);
            float start = (parts[1].Length == 0 ? 7f : SpotRadius() + 6f);
            for (int k = 0; k < people.Count; k++)
            {
                int id = people[k];
                float ang = k * 2.39996f;
                float rad = start + diameter * 0.75f * Mathf.Sqrt(k);
                _personScreen[id] = spotScreen + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;
                _personVisible[id] = true;
            }
        }
        for (int id = 1; id < snap.ByPerson.Length; id++)
        {
            var v = snap.ByPerson[id];
            if (v == null || v.Status != CharacterStatus.Traveling) continue;
            var (x, y) = v.PositionAt(snap.Tick);
            _personScreen[id] = ToScreen(new Vector2(x, y));
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
        _hoveredSpot = null;
        _hoveredCluster = -1;
        if (_dragging || !_viewArea.HasPoint(_mouse)) return;

        for (int i = 0; i < _clusters.Count; i++)
            if (_clusters[i].Screen.DistanceTo(_mouse) <= _clusters[i].Radius + 3) { _hoveredCluster = i; return; }

        float best = 9f;
        for (int id = 1; id < _personVisible.Length; id++)
        {
            if (!_personVisible[id]) continue;
            float d = _personScreen[id].DistanceTo(_mouse);
            if (d < best) { best = d; HoveredPersonId = id; }
        }
        if (HoveredPersonId != 0) return;

        foreach (var z in _zones)
            foreach (var a in z.Activities)
                if (ToScreen(_layout.Spot(z.Id, a.Id)).DistanceTo(_mouse) <= SpotRadius() + 3) { _hoveredSpot = z.Id + "/" + a.Id; HoveredRegionId = z.Id; return; }

        HoveredRegionId = ZoneAt(_mouse);
    }

    private float SpotRadius() => Mathf.Clamp(11f * _zoom * 1.2f, 9f, 18f);

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

    public override void _UnhandledInput(InputEvent e)
    {
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
                    if (mb.DoubleClick)
                    {
                        var z = ZoneAt(mb.Position);
                        if (z != null) { FocusRegion(z); EmitSignal(SignalName.RegionOpened, z); }
                    }
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
        if (_hoveredCluster >= 0)
        {
            var c = _clusters[_hoveredCluster];
            EmitSignal(SignalName.ClusterSelected, c.Key, c.Screen);
            return;
        }
        if (HoveredPersonId != 0)
        {
            SelectPerson(HoveredPersonId);
            EmitSignal(SignalName.PersonSelected, HoveredPersonId);
            return;
        }
        var zone = HoveredRegionId ?? ZoneAt(at);
        SelectRegion(zone);
        EmitSignal(SignalName.RegionSelected, zone ?? "");
    }

    // ---------------- Desenho: estradas e pontos ----------------

    public override void _Draw()
    {
        if (_zones.Length == 0) return;
        float w = Mathf.Clamp(_zoom * 1.3f, 0.8f, 2.2f);
        DrawCityLights();

        // Trilhas (fora da estrada): encruzilhada -> pontos de atividade.
        foreach (var z in _zones)
        {
            var hub = ToScreen(_layout.Hub(z.Id));
            foreach (var a in z.Activities)
                DashedLine(hub, ToScreen(_layout.Spot(z.Id, a.Id)), new Color(UiTokens.Road, 0.38f), 1.4f * w, 5f * w, 5f * w);
        }

        // Estradas: saida <-> encruzilhada em cada zona, e cruzamento de saida a saida.
        var drawnCross = new HashSet<string>(StringComparer.Ordinal);
        foreach (var z in _zones)
        {
            var hub = ToScreen(_layout.Hub(z.Id));
            foreach (var c in z.Connections)
            {
                var gate = ToScreen(_layout.Gate(z.Id, c.To));
                Road(hub, gate, w);
                string key = string.CompareOrdinal(z.Id, c.To) < 0 ? z.Id + "|" + c.To : c.To + "|" + z.Id;
                if (drawnCross.Add(key)) Road(gate, ToScreen(_layout.Gate(c.To, z.Id)), w);
            }
        }

        // Saidas (portais) e encruzilhadas.
        foreach (var z in _zones)
        {
            foreach (var c in z.Connections) DrawGate(ToScreen(_layout.Gate(z.Id, c.To)), ToScreen(_layout.Center(c.To)), w);
            var hub = ToScreen(_layout.Hub(z.Id));
            if (z.IsCity) IconCastle(hub, 16f * w);
            else
            {
                DrawCircle(hub, 4.5f * w, new Color(0, 0, 0, 0.5f));
                DrawCircle(hub, 3.2f * w, UiTokens.Road);
            }
        }

        // Pontos de atividade.
        foreach (var z in _zones)
            foreach (var a in z.Activities)
                DrawSpot(ToScreen(_layout.Spot(z.Id, a.Id)), a, z.Id + "/" + a.Id == _hoveredSpot);
    }

    private void Road(Vector2 a, Vector2 b, float w)
    {
        DrawLine(a, b, new Color(0, 0, 0, 0.45f), 6.5f * w, true);
        DrawLine(a, b, new Color(UiTokens.Road, 0.80f), 3.4f * w, true);
        DrawLine(a, b, new Color(1, 0.92f, 0.75f, 0.25f), 1f * w, true);
    }

    private void DashedLine(Vector2 a, Vector2 b, Color color, float width, float dash, float gap)
    {
        float len = a.DistanceTo(b);
        if (len < 1) return;
        var dir = (b - a) / len;
        float offset = (_time * 6f) % (dash + gap);
        for (float s = -offset; s < len; s += dash + gap)
        {
            float s0 = Mathf.Max(0, s), s1 = Mathf.Min(len, s + dash);
            if (s1 > s0) DrawLine(a + dir * s0, a + dir * s1, color, width, true);
        }
    }

    private void DrawGate(Vector2 gate, Vector2 towards, float w)
    {
        var dir = (towards - gate).Normalized();
        var side = new Vector2(-dir.Y, dir.X);
        float s = 7f * w;
        DrawCircle(gate, s + 3, new Color(0, 0, 0, 0.45f));
        DrawCircle(gate, s, UiTokens.Background);
        DrawArc(gate, s, 0, Mathf.Tau, 24, UiTokens.AccentSoft, 1.6f, true);
        DrawColoredPolygon(new[] { gate + dir * s * 0.7f, gate - dir * s * 0.3f + side * s * 0.45f, gate - dir * s * 0.3f - side * s * 0.45f }, UiTokens.AccentSoft);
    }

    private void DrawSpot(Vector2 p, ActivityDefinition a, bool hover)
    {
        float r = SpotRadius() * (hover ? 1.15f : 1f);
        var color = ActivityPalette.Color(a.Type);
        DrawCircle(p + new Vector2(1.5f, 2.5f), r + 2, new Color(0, 0, 0, 0.45f));
        DrawCircle(p, r, new Color(UiTokens.Background, 0.92f));
        DrawArc(p, r, 0, Mathf.Tau, 32, color, 2f, true);
        ActivityPalette.DrawGlyph(this, a.Type, p, r * 0.55f, color);
    }

    private void DrawCityLights()
    {
        float night = 1f - _daylight;
        if (night <= 0.01f) return;
        foreach (var z in _zones.Where(z => z.IsCity))
        {
            var p = ToScreen(_layout.Hub(z.Id));
            float rad = _layout.ZoneRadius * 0.35f * _zoom;
            float flicker = 0.92f + 0.08f * Mathf.Sin(_time * 3.1f);
            for (int i = 6; i >= 1; i--)
                DrawCircle(p, rad * (1f + i * 0.4f), new Color(1f, 0.72f, 0.38f, 0.035f * night * flicker));
        }
    }

    private void IconCastle(Vector2 c, float s)
    {
        var wall = new Color("#EFE6D2");
        void R(float x, float y, float w2, float h, Color col)
        {
            var pts = new[] { c + new Vector2(x, y), c + new Vector2(x + w2, y), c + new Vector2(x + w2, y + h), c + new Vector2(x, y + h) };
            DrawColoredPolygon(pts.Select(p => p + new Vector2(1.5f, 2f)).ToArray(), new Color(0, 0, 0, 0.45f));
            DrawColoredPolygon(pts, col);
        }
        void T(Vector2 a, Vector2 b, Vector2 d) => DrawColoredPolygon(new[] { c + a, c + b, c + d }, UiTokens.Accent);
        R(-s * 0.85f, -s * 0.05f, s * 1.7f, s * 0.75f, wall);
        R(-s * 0.95f, -s * 0.55f, s * 0.42f, s * 1.25f, wall);
        R(s * 0.53f, -s * 0.55f, s * 0.42f, s * 1.25f, wall);
        R(-s * 0.24f, -s * 0.85f, s * 0.48f, s * 1.55f, wall);
        T(new Vector2(-s, -s * 0.55f), new Vector2(-s * 0.74f, -s * 0.95f), new Vector2(-s * 0.48f, -s * 0.55f));
        T(new Vector2(s * 0.48f, -s * 0.55f), new Vector2(s * 0.74f, -s * 0.95f), new Vector2(s, -s * 0.55f));
        T(new Vector2(-s * 0.3f, -s * 0.85f), new Vector2(0, -s * 1.3f), new Vector2(s * 0.3f, -s * 0.85f));
        var window = new Color(1f, 0.8f, 0.45f, 0.25f + 0.75f * (1f - _daylight));
        R(-s * 0.06f, -s * 0.5f, s * 0.12f, s * 0.18f, window);
    }

    // ---------------- Sobreposicao ----------------

    private void PaintOverlay(MapOverlay o)
    {
        if (_zones.Length == 0) return;
        foreach (var z in _zones) ZoneLabel(o, z);
        for (int i = 0; i < _clusters.Count; i++) ClusterBubble(o, _clusters[i], i == _hoveredCluster);
        SelectedPersonMarker(o);
        DrawCompass(o);
        if (!_dragging) Tooltip(o);
    }

    private void ZoneLabel(MapOverlay o, RegionDefinition z)
    {
        var c = _layout.Center(z.Id);
        var at = ToScreen(new Vector2(c.X, c.Y - _layout.ZoneRadius * 0.70f));
        var font = UiFonts.Display;
        int size = (int)Mathf.Clamp(15 * Mathf.Sqrt(_zoom / Mathf.Max(_fitZoom, 0.01f)), 14, 22);
        var ts = font.GetStringSize(z.Name, HorizontalAlignment.Left, -1, size);
        var nameColor = z.Id == SelectedRegionId ? UiTokens.AccentSoft : UiTokens.TextPrimary;
        var pos = new Vector2(at.X - ts.X / 2f, at.Y);
        o.DrawStringOutline(font, pos, z.Name, HorizontalAlignment.Left, -1, size, 6, new Color(0.02f, 0.03f, 0.04f, 0.85f));
        o.DrawString(font, pos, z.Name, HorizontalAlignment.Left, -1, size, nameColor);

        // Linha de etiquetas: funcao/nivel, regra de combate e pessoas presentes.
        var tags = new List<(string Text, Color Color)>
        {
            (z.IsCity ? "CIDADE" : $"NÍV {z.LevelMin}–{z.LevelMax}", z.IsCity ? UiTokens.Accent : UiTokens.Info),
            (z.Pvp == PvpRules.Open ? "PVP" : "SEGURA", z.Pvp == PvpRules.Open ? UiTokens.Danger : UiTokens.Success),
            ($"● {PeopleInZone(z.Id)}", UiTokens.TextPrimary),
        };
        if (Layer == MapLayer.Crowding) tags.Add(($"{PeopleInZone(z.Id) * 100 / Math.Max(1, z.Capacity)}% LOT.", LayerTint(z).Color));
        if (Layer == MapLayer.Danger) tags.Add(($"PERIGO {(int)(DangerOf(z) * 100)}", Ramp(DangerOf(z))));
        var tf = UiFonts.SemiBold;
        const int tsz = 10;
        float total = tags.Sum(t => tf.GetStringSize(t.Text, HorizontalAlignment.Left, -1, tsz).X + 14) + (tags.Count - 1) * 4;
        float x = at.X - total / 2f;
        float y = at.Y + 8;
        foreach (var (text, color) in tags)
        {
            float tw = tf.GetStringSize(text, HorizontalAlignment.Left, -1, tsz).X + 14;
            var rect = new Rect2(x, y, tw, tf.GetHeight(tsz) + 5);
            var sb = ThemeFactory.Box(new Color(UiTokens.Background, 0.82f), 0, border: new Color(color, 0.55f), radius: 8);
            o.DrawStyleBox(sb, rect);
            o.DrawString(tf, new Vector2(x + 7, y + 2.5f + tf.GetAscent(tsz)), text, HorizontalAlignment.Left, -1, tsz, color);
            x += tw + 4;
        }
    }

    private void ClusterBubble(MapOverlay o, (string Key, Vector2 Screen, float Radius, List<int> People) c, bool hover)
    {
        float r = c.Radius * (hover ? 1.08f : 1f);
        o.DrawCircle(c.Screen + new Vector2(2, 3), r + 3, new Color(0, 0, 0, 0.45f));
        o.DrawCircle(c.Screen, r, new Color(UiTokens.Background, 0.95f));
        // Anel em fatias por perfil (proporcoes reais do grupo).
        var groups = c.People.GroupBy(id => _persons[id - 1].ProfileId).OrderBy(g => g.Key, StringComparer.Ordinal).ToList();
        float start = -Mathf.Pi / 2f;
        foreach (var g in groups)
        {
            float span = Mathf.Tau * g.Count() / c.People.Count;
            o.DrawArc(c.Screen, r - 2.5f, start, start + span - 0.04f, 24, UiTokens.ProfileColor(g.Key), 4.5f, true);
            start += span;
        }
        if (hover) o.DrawArc(c.Screen, r + 4, 0, Mathf.Tau, 40, UiTokens.AccentSoft, 2f, true);
        var font = UiFonts.Numbers;
        string text = c.People.Count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        int size = r > 22 ? 15 : 13;
        var ts = font.GetStringSize(text, HorizontalAlignment.Left, -1, size);
        o.DrawString(font, new Vector2(c.Screen.X - ts.X / 2f, c.Screen.Y + font.GetAscent(size) / 2f - 1), text, HorizontalAlignment.Left, -1, size, UiTokens.TextPrimary);
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
        string name = view.Name;
        var ts = font.GetStringSize(name, HorizontalAlignment.Left, -1, 13);
        var rect = new Rect2(p.X - ts.X / 2f - 8, p.Y - 42, ts.X + 16, font.GetHeight(13) + 8);
        o.DrawStyleBox(ThemeFactory.Box(new Color(UiTokens.Background, 0.92f), 8, border: UiTokens.Accent, radius: 9), rect);
        o.DrawLine(new Vector2(p.X, rect.End.Y), p + new Vector2(0, -13), new Color(UiTokens.Accent, 0.8f), 1.5f, true);
        o.DrawString(font, new Vector2(rect.Position.X + 8, rect.Position.Y + 4 + font.GetAscent(13)), name, HorizontalAlignment.Left, -1, 13, UiTokens.AccentSoft);
    }

    private void Tooltip(MapOverlay o)
    {
        var lines = new List<(string, Font, int, Color)>();
        if (_hoveredCluster >= 0)
        {
            var c = _clusters[_hoveredCluster];
            var parts = c.Key.Split('/');
            var z = _content.Regions[parts[0]];
            string what = parts[1].Length == 0 ? "Na encruzilhada" : z.Activities.First(a => a.Id == parts[1]).Name;
            lines.Add(($"{c.People.Count} jogadores", UiFonts.SemiBold, 14, UiTokens.TextPrimary));
            lines.Add(($"{what} · {z.Name}", UiFonts.Regular, 12, UiTokens.TextSecondary));
            lines.Add(("Clique para ver a lista", UiFonts.Regular, 12, UiTokens.AccentSoft));
        }
        else if (HoveredPersonId > 0 && _snap?.ByPerson[HoveredPersonId] is { } v)
        {
            var prof = _content.Profiles.TryGetValue(_persons[HoveredPersonId - 1].ProfileId, out var pr) ? pr.Name : "";
            lines.Add(($"{v.Name} · nível {v.Level}", UiFonts.SemiBold, 14, UiTokens.TextPrimary));
            lines.Add((StatusText.Of(v, _content), UiFonts.Regular, 12, UiTokens.TextSecondary));
            lines.Add(($"{_persons[HoveredPersonId - 1].Name} · {prof}", UiFonts.Regular, 12, UiTokens.ProfileColor(_persons[HoveredPersonId - 1].ProfileId)));
            lines.Add(("Clique para abrir a ficha", UiFonts.Regular, 12, UiTokens.AccentSoft));
        }
        else if (_hoveredSpot != null)
        {
            var parts = _hoveredSpot.Split('/');
            var z = _content.Regions[parts[0]];
            var a = z.Activities.First(x => x.Id == parts[1]);
            lines.Add((a.Name, UiFonts.SemiBold, 14, UiTokens.TextPrimary));
            lines.Add(($"{PtBr.ActivityType(a.Type)} · nível {a.MinLevel}+ · ~{a.TypicalMinutes} min", UiFonts.Regular, 12, ActivityPalette.Color(a.Type)));
            int n = PeopleAtSpot(z.Id, a.Id);
            lines.Add((n == 0 ? "Ninguém agora" : n == 1 ? "1 jogador agora" : $"{n} jogadores agora", UiFonts.Regular, 12, UiTokens.TextSecondary));
        }
        else if (HoveredRegionId != null)
        {
            var z = _content.Regions[HoveredRegionId];
            lines.Add((z.Name, UiFonts.SemiBold, 14, UiTokens.TextPrimary));
            lines.Add(($"{PeopleInZone(z.Id)} jogadores · {z.Activities.Count} atividades · {z.Connections.Count} saídas", UiFonts.Regular, 12, UiTokens.TextSecondary));
            lines.Add(("Clique para detalhes · duplo clique para aproximar", UiFonts.Regular, 12, UiTokens.AccentSoft));
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
        foreach (var (t, f, s, col) in lines)
        {
            o.DrawString(f, new Vector2(pos.X + 12, y + f.GetAscent(s)), t, HorizontalAlignment.Left, -1, s, col);
            y += f.GetHeight(s) + 2;
        }
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

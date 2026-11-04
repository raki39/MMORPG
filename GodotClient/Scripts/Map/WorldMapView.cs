using Godot;
using Simulation.Core.Content;
using GodotClient.Fixtures;
using GodotClient.Sim;
using GodotClient.Ui;

namespace GodotClient.Map;

/// <summary>
/// Mapa estrategico 2D ("atlas vivo"). Camadas, de baixo para cima:
///   terreno (shader, ColorRect externo) -> rotas, regioes e rotulos (_Draw deste no)
///   -> populacao (MultiMesh) -> sobreposicao (selecao, tooltip, bussola).
/// Camera com zoom/arrasto suaves; hover animado; clique seleciona regiao ou pessoa;
/// duplo clique foca a regiao. Nada aqui decide estado da simulacao.
/// </summary>
public partial class WorldMapView : Node2D
{
    [Signal] public delegate void RegionSelectedEventHandler(string regionId);
    [Signal] public delegate void PersonSelectedEventHandler(int personId);

    private const int NameSize = 17;
    private const int SubSize = 11;

    private ContentDatabase _content = null!;
    private DemoMovementFixture _fixture = null!;
    private IReadOnlyList<PersonInfo> _persons = Array.Empty<PersonInfo>();
    private ShaderMaterial _terrain = null!;
    private PopulationLayer _population = null!;
    private MapOverlay _overlay = null!;
    private RegionDefinition[] _regions = Array.Empty<RegionDefinition>();
    private readonly Dictionary<string, Vector2> _pos = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _hoverAnim = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _countCache = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Vector2> _routeControl = new(StringComparer.Ordinal);
    private readonly List<(string A, string B, int Minutes)> _routes = new();
    private Vector2[] _personScreen = Array.Empty<Vector2>();

    private Vector2 _pan, _targetPan;
    private float _zoom = 1f, _targetZoom = 1f, _fitZoom = 1f;
    private Rect2 _viewArea;
    private bool _pressed, _dragging;
    private Vector2 _pressPos, _mouse;
    private float _time;
    private float _daylight = 1f;
    private StyleBoxFlat _badge = null!, _tip = null!;

    public string? SelectedRegionId { get; private set; }
    public int SelectedPersonId { get; private set; }
    public string? HoveredRegionId { get; private set; }
    public int HoveredPersonId { get; private set; }

    public void Setup(ContentDatabase content, DemoMovementFixture fixture, IReadOnlyList<PersonInfo> persons, ColorRect terrainRect)
    {
        _content = content;
        _fixture = fixture;
        _persons = persons;
        _regions = content.Regions.Values.ToArray();
        foreach (var r in _regions)
        {
            _pos[r.Id] = new Vector2(r.MapPosition.X, r.MapPosition.Y);
            _hoverAnim[r.Id] = 0f;
            foreach (var c in r.Connections)
                if (string.CompareOrdinal(r.Id, c.To) < 0) _routes.Add((r.Id, c.To, c.TravelMinutes));
        }
        BuildRouteCurves();

        _terrain = new ShaderMaterial { Shader = GD.Load<Shader>("res://Shaders/terrain.gdshader") };
        terrainRect.Material = _terrain;
        var data = new Vector4[16];
        for (int i = 0; i < _regions.Length && i < 16; i++)
        {
            var r = _regions[i];
            float sigma = 92f + Mathf.Sqrt(r.Capacity) * 2.2f;
            data[i] = new Vector4(r.MapPosition.X, r.MapPosition.Y, sigma, UiTokens.BiomeIndex(r.Biome));
        }
        _terrain.SetShaderParameter("regions", data);
        _terrain.SetShaderParameter("region_count", Mathf.Min(_regions.Length, 16));

        _population = new PopulationLayer();
        _population.Init(persons.Count);
        AddChild(_population);
        _overlay = new MapOverlay { Painter = PaintOverlay };
        AddChild(_overlay);
        _personScreen = new Vector2[persons.Count];

        _badge = ThemeFactory.Box(new Color(UiTokens.Background, 0.82f), 7, border: new Color(UiTokens.Road, 0.40f), radius: 10);
        _tip = ThemeFactory.Glass(12, 10);
    }

    // ---------------- Camera ----------------

    public void FitTo(Rect2 viewArea, bool instant = false)
    {
        _viewArea = viewArea;
        if (_regions.Length == 0) return;
        var min = new Vector2(_regions.Min(r => r.MapPosition.X), _regions.Min(r => r.MapPosition.Y));
        var max = new Vector2(_regions.Max(r => r.MapPosition.X), _regions.Max(r => r.MapPosition.Y));
        var size = (max - min) + new Vector2(320, 280);
        _fitZoom = Mathf.Min(viewArea.Size.X / size.X, viewArea.Size.Y / size.Y);
        _targetZoom = _fitZoom;
        _targetPan = viewArea.GetCenter() - (min + max) / 2f * _targetZoom;
        if (instant) { _zoom = _targetZoom; _pan = _targetPan; }
    }

    public void ZoomBy(float factor) => ZoomAround(_viewArea.GetCenter(), factor);

    public void FocusRegion(string regionId)
    {
        if (!_pos.TryGetValue(regionId, out var m)) return;
        _targetZoom = Mathf.Clamp(_fitZoom * 1.9f, _fitZoom * 0.6f, _fitZoom * 4f);
        _targetPan = _viewArea.GetCenter() - m * _targetZoom;
    }

    private void ZoomAround(Vector2 screen, float factor)
    {
        var anchor = (screen - _targetPan) / _targetZoom;
        _targetZoom = Mathf.Clamp(_targetZoom * factor, _fitZoom * 0.6f, _fitZoom * 4f);
        _targetPan = screen - anchor * _targetZoom;
    }

    private Vector2 ToScreen(Vector2 map) => map * _zoom + _pan;
    private Vector2 ToMap(Vector2 screen) => (screen - _pan) / _zoom;

    /// <summary>Hora do dia (0..24) da simulacao, para o ciclo de dia e noite.</summary>
    public void SetHour(double hour)
    {
        float h = (float)hour;
        _daylight = Mathf.SmoothStep(4.5f, 7f, h) * (1f - Mathf.SmoothStep(18f, 20.5f, h));
    }

    public void SelectRegion(string? regionId)
    {
        SelectedRegionId = regionId;
        if (regionId != null) SelectedPersonId = 0;
    }

    public void SelectPerson(int personId)
    {
        SelectedPersonId = personId;
        if (personId > 0) SelectedRegionId = null;
    }

    // ---------------- Quadro a quadro ----------------

    public override void _Process(double delta)
    {
        if (_regions.Length == 0) return;
        float dt = (float)delta;
        _time += dt;

        // Suavizacao exponencial da camera (independe do FPS).
        float k = 1f - Mathf.Exp(-dt * 12f);
        _zoom = Mathf.Lerp(_zoom, _targetZoom, k);
        _pan = _pan.Lerp(_targetPan, k);

        foreach (var r in _regions)
        {
            float target = r.Id == HoveredRegionId || r.Id == SelectedRegionId ? 1f : 0f;
            _hoverAnim[r.Id] = Mathf.Lerp(_hoverAnim[r.Id], target, 1f - Mathf.Exp(-dt * 10f));
        }

        _terrain.SetShaderParameter("viewport_size", GetViewportRect().Size);
        _terrain.SetShaderParameter("pan", _pan);
        _terrain.SetShaderParameter("zoom", _zoom);
        _terrain.SetShaderParameter("daylight", _daylight);
        _terrain.SetShaderParameter("anim_time", _time);

        UpdatePopulation();
        UpdateHover();
        QueueRedraw();
        _overlay.QueueRedraw();
    }

    private void UpdatePopulation()
    {
        _countCache.Clear();
        float diameter = Mathf.Clamp(7.5f * _zoom, 6f, 15f);
        for (int i = 0; i < _persons.Count; i++)
        {
            var pl = _fixture.PlacementOf(i);
            Vector2 map;
            if (pl.Traveling)
            {
                map = RoutePoint(pl.From, pl.To, pl.T);
                var tangent = (RoutePoint(pl.From, pl.To, Mathf.Min(pl.T + 0.02f, 1f)) - RoutePoint(pl.From, pl.To, Mathf.Max(pl.T - 0.02f, 0f))).Normalized();
                map += new Vector2(-tangent.Y, tangent.X) * (pl.Offset.X * 0.12f);
            }
            else
            {
                // Acampa em volta do medalhao, sem cobrir o icone.
                var region = _content.Regions[pl.At];
                var dir = pl.Offset.Normalized();
                float dist = RegionRadius(region) + 12f + (pl.Offset.Length() - 8f) * 0.75f;
                map = _pos[pl.At] + dir * dist;
                _countCache[pl.At] = _countCache.TryGetValue(pl.At, out var c) ? c + 1 : 1;
            }
            var screen = ToScreen(map);
            _personScreen[i] = screen;
            var person = _persons[i];
            var color = UiTokens.ProfileColor(person.ProfileId);
            float d = person.Id == SelectedPersonId || person.Id == HoveredPersonId ? diameter * 1.5f : diameter;
            _population.Write(i, screen, d, color);
        }
        _population.Commit();
    }

    public int CountIn(string regionId) => _countCache.TryGetValue(regionId, out var c) ? c : 0;

    private void UpdateHover()
    {
        HoveredPersonId = 0;
        HoveredRegionId = null;
        if (_dragging || !_viewArea.HasPoint(_mouse)) return;

        float best = 9f;
        for (int i = 0; i < _personScreen.Length; i++)
        {
            float d = _personScreen[i].DistanceTo(_mouse);
            if (d < best) { best = d; HoveredPersonId = _persons[i].Id; }
        }
        if (HoveredPersonId != 0) return;
        HoveredRegionId = RegionAt(_mouse);
    }

    private static float RegionRadius(RegionDefinition r) => 24f + Mathf.Sqrt(r.Capacity) * 1.25f;

    private string? RegionAt(Vector2 screen)
    {
        var m = ToMap(screen);
        foreach (var r in _regions)
            if (m.DistanceTo(_pos[r.Id]) <= RegionRadius(r) + 6) return r.Id;
        return null;
    }

    // ---------------- Entrada ----------------

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventMouseMotion mm)
        {
            _mouse = mm.Position;
            if (_pressed && !_dragging && mm.Position.DistanceTo(_pressPos) > 5f) _dragging = true;
            if (_dragging)
            {
                _targetPan += mm.Relative;
                _pan += mm.Relative;
            }
            return;
        }
        if (e is not InputEventMouseButton mb) return;
        _mouse = mb.Position;

        switch (mb.ButtonIndex)
        {
            case MouseButton.WheelUp when mb.Pressed:
                ZoomAround(mb.Position, 1.18f);
                break;
            case MouseButton.WheelDown when mb.Pressed:
                ZoomAround(mb.Position, 1f / 1.18f);
                break;
            case MouseButton.Right:
            case MouseButton.Middle:
                _pressed = mb.Pressed;
                _dragging = mb.Pressed;
                _pressPos = mb.Position;
                break;
            case MouseButton.Left:
                if (mb.Pressed)
                {
                    _pressed = true;
                    _dragging = false;
                    _pressPos = mb.Position;
                    if (mb.DoubleClick)
                    {
                        var id = RegionAt(mb.Position);
                        if (id != null) FocusRegion(id);
                    }
                }
                else
                {
                    if (!_dragging) Click(mb.Position);
                    _pressed = false;
                    _dragging = false;
                }
                break;
            default:
                return;
        }
        GetViewport().SetInputAsHandled();
    }

    private void Click(Vector2 at)
    {
        _mouse = at;
        UpdateHover();
        if (HoveredPersonId != 0)
        {
            SelectPerson(HoveredPersonId);
            EmitSignal(SignalName.PersonSelected, HoveredPersonId);
            return;
        }
        var region = RegionAt(at);
        SelectRegion(region);
        if (region == null) SelectedPersonId = 0;
        EmitSignal(SignalName.RegionSelected, region ?? "");
    }

    // ---------------- Rotas ----------------

    private static string RouteKey(string a, string b) => string.CompareOrdinal(a, b) < 0 ? a + "|" + b : b + "|" + a;

    private static Vector2 Bezier(Vector2 a, Vector2 c, Vector2 b, float t)
    {
        float it = 1f - t;
        return a * (it * it) + c * (2f * it * t) + b * (t * t);
    }

    /// <summary>
    /// Escolhe a curvatura de cada estrada: a que mais se afasta das demais regioes,
    /// preferindo curvas suaves quando ha folga.
    /// </summary>
    private void BuildRouteCurves()
    {
        float[] options = { 0.12f, -0.12f, 0.24f, -0.24f, 0.36f, -0.36f };
        foreach (var (aId, bId, _) in _routes)
        {
            var a = _pos[aId];
            var b = _pos[bId];
            var mid = (a + b) / 2f;
            var dir = b - a;
            var normal = new Vector2(-dir.Y, dir.X).Normalized();
            float len = dir.Length();
            float bestScore = float.NegativeInfinity;
            var best = mid + normal * len * options[0];
            foreach (float k in options)
            {
                var ctrl = mid + normal * len * k;
                float clearance = 200f;
                for (int s = 1; s < 20; s++)
                {
                    var p = Bezier(a, ctrl, b, s / 20f);
                    foreach (var o in _regions)
                    {
                        if (o.Id == aId || o.Id == bId) continue;
                        clearance = Mathf.Min(clearance, p.DistanceTo(_pos[o.Id]) - RegionRadius(o) - 40f);
                    }
                }
                float score = Mathf.Min(clearance, 40f) - Mathf.Abs(k) * 60f;
                if (score > bestScore) { bestScore = score; best = ctrl; }
            }
            _routeControl[RouteKey(aId, bId)] = best;
        }
    }

    /// <summary>Ponto da estrada curva entre duas regioes (t=0 em "from").</summary>
    public Vector2 RoutePoint(string from, string to, float t)
    {
        bool forward = string.CompareOrdinal(from, to) < 0;
        var a = _pos[forward ? from : to];
        var b = _pos[forward ? to : from];
        float u = forward ? t : 1f - t;
        return Bezier(a, _routeControl[RouteKey(from, to)], b, u);
    }

    private Vector2[] RouteScreenPoints(string aId, string bId, int n)
    {
        var ra = _content.Regions[aId];
        var rb = _content.Regions[bId];
        float len = _pos[aId].DistanceTo(_pos[bId]) * 1.08f;
        float t0 = (RegionRadius(ra) + 12) / len;
        float t1 = 1f - (RegionRadius(rb) + 12) / len;
        var pts = new Vector2[n + 1];
        for (int i = 0; i <= n; i++)
            pts[i] = ToScreen(RoutePoint(aId, bId, Mathf.Lerp(t0, t1, i / (float)n)));
        return pts;
    }

    private void DrawRouteLines()
    {
        float w = Mathf.Clamp(_zoom, 0.7f, 1.4f);
        foreach (var (aId, bId, _) in _routes)
        {
            const int n = 48;
            var pts = RouteScreenPoints(aId, bId, n);
            DrawPolyline(pts, new Color(0, 0, 0, 0.42f), 7.5f * w, true);
            DrawPolyline(pts, new Color(UiTokens.Road, 0.50f), 3.6f * w, true);
            // Tracejado luminoso fluindo ao longo da estrada.
            float acc = 0;
            for (int i = 0; i < n; i++)
            {
                float seg = pts[i].DistanceTo(pts[i + 1]);
                float phase = (acc / 16f - _time * 0.9f) % 1f;
                if (phase < 0) phase += 1f;
                if (phase < 0.42f)
                    DrawLine(pts[i], pts[i + 1], new Color(UiTokens.AccentSoft, 0.80f), 1.6f * w, true);
                acc += seg;
            }
        }
    }

    private Rect2 BadgeRect(Vector2 center, string text)
    {
        var font = UiFonts.Numbers;
        var ts = font.GetStringSize(text, HorizontalAlignment.Left, -1, UiTokens.FontCaption);
        float h = font.GetHeight(UiTokens.FontCaption);
        float w = ts.X + 12 + 18;
        return new Rect2(center.X - w / 2f, center.Y - h / 2f - 4, w, h + 8);
    }

    /// <summary>Selos de tempo: escolhem o ponto da estrada que nao colide com rotulos e medalhoes.</summary>
    private void DrawRouteBadges(CanvasItem g, List<Rect2> obstacles)
    {
        float[] candidates = { 0.5f, 0.38f, 0.62f, 0.28f, 0.72f };
        foreach (var (aId, bId, minutes) in _routes)
        {
            string text = $"{minutes} min";
            Rect2 chosen = default;
            bool found = false;
            foreach (float t in candidates)
            {
                var rect = BadgeRect(ToScreen(RoutePoint(aId, bId, t)), text);
                bool hit = obstacles.Any(o => o.Intersects(rect))
                    || _regions.Any(r => rect.Grow(4).HasPoint(ToScreen(_pos[r.Id])) || ToScreen(_pos[r.Id]).DistanceTo(rect.GetCenter()) < RegionRadius(r) * _zoom + rect.Size.X / 2f);
                if (!hit) { chosen = rect; found = true; break; }
            }
            if (!found) chosen = BadgeRect(ToScreen(RoutePoint(aId, bId, 0.5f)), text);
            obstacles.Add(chosen);
            DrawBadge(g, chosen, text);
        }
    }

    private void DrawBadge(CanvasItem g, Rect2 rect, string text)
    {
        var font = UiFonts.Numbers;
        int size = UiTokens.FontCaption;
        g.DrawStyleBox(_badge, rect);
        var c = new Vector2(rect.Position.X + 12, rect.GetCenter().Y);
        g.DrawArc(c, 5f, 0, Mathf.Tau, 20, UiTokens.Road, 1.4f, true);
        g.DrawLine(c, c + new Vector2(0, -3.2f), UiTokens.Road, 1.3f, true);
        g.DrawLine(c, c + new Vector2(2.4f, 0), UiTokens.Road, 1.3f, true);
        g.DrawString(font, new Vector2(rect.Position.X + 21, rect.Position.Y + 4 + font.GetAscent(size)), text,
            HorizontalAlignment.Left, -1, size, UiTokens.TextPrimary);
    }

    // ---------------- Regioes ----------------

    private readonly struct RegionLabel
    {
        public RegionLabel(RegionDefinition region, Vector2 nameAt, Vector2 subAt, string sub, Color subColor, Rect2 bounds)
        {
            Region = region; NameAt = nameAt; SubAt = subAt; Sub = sub; SubColor = subColor; Bounds = bounds;
        }
        public RegionDefinition Region { get; }
        public Vector2 NameAt { get; }
        public Vector2 SubAt { get; }
        public string Sub { get; }
        public Color SubColor { get; }
        public Rect2 Bounds { get; }
    }

    public override void _Draw()
    {
        if (_regions.Length == 0) return;
        DrawCityLights();
        DrawRouteLines();
        foreach (var r in _regions) DrawRegion(r);
        // Rotulos e selos ficam na sobreposicao, acima das pessoas, para manter a leitura.
    }

    private void DrawCityLights()
    {
        float night = 1f - _daylight;
        if (night <= 0.01f) return;
        foreach (var r in _regions.Where(r => r.Kind == RegionKinds.City))
        {
            var p = ToScreen(_pos[r.Id]);
            float rad = RegionRadius(r) * _zoom;
            float flicker = 0.92f + 0.08f * Mathf.Sin(_time * 3.1f);
            for (int i = 6; i >= 1; i--)
                DrawCircle(p, rad * (1f + i * 0.45f), new Color(1f, 0.72f, 0.38f, 0.035f * night * flicker));
        }
    }

    private Color RimColor(RegionDefinition r)
    {
        bool lair = r.Activities.Any(a => a.Type == ActivityTypes.Boss);
        if (r.Kind == RegionKinds.City) return UiTokens.Accent;
        if (r.Pvp == PvpRules.Open) return UiTokens.Danger;
        if (lair) return UiTokens.Warning;
        return UiTokens.BiomeColor(r.Biome).Lightened(0.45f);
    }

    private float ScreenRadius(RegionDefinition r) => RegionRadius(r) * _zoom * (1f + _hoverAnim[r.Id] * 0.06f);

    private void DrawRegion(RegionDefinition r)
    {
        var p = ToScreen(_pos[r.Id]);
        float hover = _hoverAnim[r.Id];
        float rad = ScreenRadius(r);
        bool city = r.Kind == RegionKinds.City;
        bool pvp = r.Pvp == PvpRules.Open;
        bool lair = r.Activities.Any(a => a.Type == ActivityTypes.Boss);
        var biome = UiTokens.BiomeColor(r.Biome);
        Color rim = RimColor(r);

        // Sombra projetada.
        for (int i = 0; i < 4; i++)
            DrawCircle(p + new Vector2(3, 7) * (1 + hover), rad + 9 - i * 3, new Color(0, 0, 0, 0.10f));

        // Corpo com gradiente esferico (luz de cima-esquerda).
        var edge = biome.Darkened(0.62f);
        var center = biome.Lightened(0.10f);
        var lightOffset = new Vector2(-rad * 0.28f, -rad * 0.32f);
        const int steps = 12;
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps;
            DrawCircle(p + lightOffset * t, rad * (1f - t * 0.82f), edge.Lerp(center, t * t));
        }

        // Aro metalico, filete interno e brilho.
        DrawArc(p, rad, 0, Mathf.Tau, 72, rim, 3.2f, true);
        DrawArc(p, rad - 4, 0, Mathf.Tau, 72, new Color(0, 0, 0, 0.35f), 1.5f, true);
        DrawArc(p, rad - 2.5f, Mathf.Pi * 1.08f, Mathf.Pi * 1.42f, 20, new Color(1, 1, 1, 0.30f), 2f, true);

        // Zona PvP: entalhes girando no aro.
        if (pvp)
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.Tau / 12f + _time * 0.15f;
                DrawArc(p, rad + 6, a, a + 0.22f, 6, new Color(UiTokens.Danger, 0.85f), 2.4f, true);
            }

        // Selecao: anel dourado pulsante.
        if (r.Id == SelectedRegionId)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(_time * 4f);
            DrawArc(p, rad + (pvp ? 12 : 8) + pulse * 3, 0, Mathf.Tau, 72, new Color(UiTokens.Accent, 0.55f + 0.35f * pulse), 2.5f, true);
        }

        DrawIcon(r, p, rad * 0.5f, city, lair);
        DrawCountBubble(p + new Vector2(rad * 0.72f, -rad * 0.72f), CountIn(r.Id), rim);
    }

    /// <summary>Contador de pessoas presentes (demonstracao ate o M2), em bolha no medalhao.</summary>
    private void DrawCountBubble(Vector2 c, int count, Color rim)
    {
        string text = count.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var font = UiFonts.Numbers;
        var ts = font.GetStringSize(text, HorizontalAlignment.Left, -1, 11);
        float r = Mathf.Max(10f, ts.X / 2f + 6f);
        DrawCircle(c + new Vector2(1, 2), r, new Color(0, 0, 0, 0.45f));
        DrawCircle(c, r, UiTokens.Background);
        DrawArc(c, r, 0, Mathf.Tau, 32, rim, 1.6f, true);
        DrawString(font, new Vector2(c.X - ts.X / 2f, c.Y + font.GetAscent(11) / 2f - 1), text, HorizontalAlignment.Left, -1, 11, UiTokens.TextPrimary);
    }

    private RegionLabel BuildLabel(RegionDefinition r)
    {
        var p = ToScreen(_pos[r.Id]);
        float rad = ScreenRadius(r) + (r.Pvp == PvpRules.Open ? 12 : 6);
        bool city = r.Kind == RegionKinds.City;
        string sub = city ? "CIDADE · ZONA SEGURA" : $"NÍVEIS {r.LevelMin}–{r.LevelMax}" + (r.Pvp == PvpRules.Open ? " · PVP" : "");
        var (nameAt, subAt) = LabelAnchors(r, p, rad);
        var nameSize = UiFonts.Display.GetStringSize(r.Name, HorizontalAlignment.Left, -1, NameSize);
        var subSize = UiFonts.SemiBold.GetStringSize(sub, HorizontalAlignment.Left, -1, SubSize);
        var nameRect = new Rect2(nameAt - nameSize / 2f, nameSize);
        var subRect = new Rect2(subAt - subSize / 2f, subSize);
        return new RegionLabel(r, nameAt, subAt, sub, new Color(RimColor(r), 0.95f), nameRect.Merge(subRect).Grow(4));
    }

    private void DrawLabel(CanvasItem g, RegionLabel l)
    {
        var nameColor = l.Region.Id == SelectedRegionId ? UiTokens.AccentSoft : UiTokens.TextPrimary;
        OutlinedText(g, UiFonts.Display, l.NameAt, l.Region.Name, NameSize, nameColor, 6);
        OutlinedText(g, UiFonts.SemiBold, l.SubAt, l.Sub, SubSize, l.SubColor, 4);
    }

    private static void OutlinedText(CanvasItem g, Font font, Vector2 center, string text, int size, Color color, int outline)
    {
        var ts = font.GetStringSize(text, HorizontalAlignment.Left, -1, size);
        var pos = new Vector2(center.X - ts.X / 2f, center.Y + font.GetAscent(size) / 2f - 1);
        g.DrawStringOutline(font, pos, text, HorizontalAlignment.Left, -1, size, outline, new Color(0.02f, 0.03f, 0.04f, 0.85f));
        g.DrawString(font, pos, text, HorizontalAlignment.Left, -1, size, color);
    }

    /// <summary>
    /// Posicao dos rotulos: abaixo por padrao; se uma rota sai para baixo (menos de 50 graus),
    /// usa a direcao (acima, direita, esquerda) mais distante angularmente das rotas.
    /// </summary>
    private (Vector2 Name, Vector2 Sub) LabelAnchors(RegionDefinition r, Vector2 p, float rad)
    {
        // Angulo de saida real de cada estrada (curva), nao a linha reta.
        var angles = r.Connections.Select(c => (RoutePoint(r.Id, c.To, 0.15f) - _pos[r.Id]).Angle()).ToList();
        float MinGap(float cand) => angles.Count == 0 ? Mathf.Pi
            : angles.Min(a => Mathf.Abs(Mathf.Wrap(a - cand, -Mathf.Pi, Mathf.Pi)));

        float down = Mathf.Pi / 2f, up = -Mathf.Pi / 2f, right = 0f, left = Mathf.Pi;
        float chosen = down;
        if (MinGap(down) < Mathf.DegToRad(50))
        {
            float bestGap = -1;
            foreach (var cand in new[] { up, right, left, down })
            {
                float g = MinGap(cand);
                if (g > bestGap + 0.001f) { bestGap = g; chosen = cand; }
            }
        }
        if (chosen == down) return (p + new Vector2(0, rad + 16), p + new Vector2(0, rad + 34));
        if (chosen == up) return (p + new Vector2(0, -rad - 32), p + new Vector2(0, -rad - 14));
        float half = Mathf.Max(UiFonts.Display.GetStringSize(r.Name, HorizontalAlignment.Left, -1, NameSize).X, 120) / 2f + 12;
        float dx = (rad + half) * (chosen == right ? 1 : -1);
        return (p + new Vector2(dx, -9), p + new Vector2(dx, 9));
    }

    // ---------------- Icones vetoriais ----------------

    private void DrawIcon(RegionDefinition r, Vector2 c, float s, bool city, bool lair)
    {
        if (city) { IconCastle(c, s); return; }
        if (lair) { IconCrown(c, s); return; }
        switch (r.Biome)
        {
            case "floresta": IconForest(c, s); break;
            case "ruinas": IconRuins(c, s); break;
            default: IconMountain(c, s); break;
        }
    }

    private void Poly(Vector2[] pts, Color color)
    {
        var shadow = pts.Select(p => p + new Vector2(1.5f, 2f)).ToArray();
        DrawColoredPolygon(shadow, new Color(0, 0, 0, 0.45f));
        DrawColoredPolygon(pts, color);
    }

    private void Rect(Vector2 c, float x, float y, float w, float h, Color color) =>
        Poly(new[] { c + new Vector2(x, y), c + new Vector2(x + w, y), c + new Vector2(x + w, y + h), c + new Vector2(x, y + h) }, color);

    private void IconCastle(Vector2 c, float s)
    {
        var wall = new Color("#EFE6D2");
        Rect(c, -s * 0.85f, -s * 0.05f, s * 1.7f, s * 0.75f, wall);
        Rect(c, -s * 0.95f, -s * 0.55f, s * 0.42f, s * 1.25f, wall);
        Rect(c, s * 0.53f, -s * 0.55f, s * 0.42f, s * 1.25f, wall);
        Rect(c, -s * 0.24f, -s * 0.85f, s * 0.48f, s * 1.55f, wall.Lightened(0.05f));
        Poly(new[] { c + new Vector2(-s * 1.0f, -s * 0.55f), c + new Vector2(-s * 0.74f, -s * 0.95f), c + new Vector2(-s * 0.48f, -s * 0.55f) }, UiTokens.Accent);
        Poly(new[] { c + new Vector2(s * 0.48f, -s * 0.55f), c + new Vector2(s * 0.74f, -s * 0.95f), c + new Vector2(s * 1.0f, -s * 0.55f) }, UiTokens.Accent);
        Poly(new[] { c + new Vector2(-s * 0.3f, -s * 0.85f), c + new Vector2(0, -s * 1.3f), c + new Vector2(s * 0.3f, -s * 0.85f) }, UiTokens.Accent);
        Rect(c, -s * 0.13f, s * 0.3f, s * 0.26f, s * 0.4f, new Color(0.12f, 0.09f, 0.07f));
        // Janelas acesas a noite.
        float night = 1f - _daylight;
        var window = new Color(1f, 0.8f, 0.45f, 0.25f + 0.75f * night);
        Rect(c, -s * 0.8f, -s * 0.25f, s * 0.12f, s * 0.16f, window);
        Rect(c, s * 0.68f, -s * 0.25f, s * 0.12f, s * 0.16f, window);
        Rect(c, -s * 0.06f, -s * 0.5f, s * 0.12f, s * 0.18f, window);
    }

    private void IconForest(Vector2 c, float s)
    {
        var green = new Color("#B9DDB0");
        void Tree(float x, float y, float k)
        {
            Poly(new[] { c + new Vector2(x - s * 0.45f * k, y), c + new Vector2(x, y - s * 0.9f * k), c + new Vector2(x + s * 0.45f * k, y) }, green);
            Poly(new[] { c + new Vector2(x - s * 0.35f * k, y - s * 0.4f * k), c + new Vector2(x, y - s * 1.15f * k), c + new Vector2(x + s * 0.35f * k, y - s * 0.4f * k) }, green.Lightened(0.15f));
            Rect(c, x - s * 0.07f * k, y, s * 0.14f * k, s * 0.25f * k, new Color("#8A6A4A"));
        }
        Tree(-s * 0.55f, s * 0.45f, 0.8f);
        Tree(s * 0.55f, s * 0.45f, 0.8f);
        Tree(0, s * 0.55f, 1.05f);
    }

    private void IconMountain(Vector2 c, float s)
    {
        var rock = new Color("#D8CBB8");
        Poly(new[] { c + new Vector2(-s * 1.0f, s * 0.6f), c + new Vector2(-s * 0.35f, -s * 0.55f), c + new Vector2(s * 0.3f, s * 0.6f) }, rock.Darkened(0.15f));
        Poly(new[] { c + new Vector2(-s * 0.45f, s * 0.6f), c + new Vector2(s * 0.25f, -s * 0.9f), c + new Vector2(s * 1.0f, s * 0.6f) }, rock);
        Poly(new[] { c + new Vector2(s * 0.06f, -s * 0.5f), c + new Vector2(s * 0.25f, -s * 0.9f), c + new Vector2(s * 0.44f, -s * 0.5f) }, new Color(1, 1, 1, 0.9f));
        var arch = new List<Vector2>();
        for (int i = 0; i <= 10; i++)
        {
            float a = Mathf.Pi + i * Mathf.Pi / 10f;
            arch.Add(c + new Vector2(s * 0.25f, s * 0.6f) + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * s * 0.28f);
        }
        DrawColoredPolygon(arch.ToArray(), new Color(0.08f, 0.06f, 0.05f));
    }

    private void IconRuins(Vector2 c, float s)
    {
        var stone = new Color("#D6D0DE");
        Rect(c, -s * 0.85f, -s * 0.2f, s * 0.3f, s * 0.85f, stone);
        Rect(c, -s * 0.15f, -s * 0.7f, s * 0.3f, s * 1.35f, stone);
        Rect(c, s * 0.55f, s * 0.05f, s * 0.3f, s * 0.6f, stone.Darkened(0.1f));
        Rect(c, -s * 0.4f, -s * 0.85f, s * 0.95f, s * 0.18f, stone.Lightened(0.1f));
        Rect(c, -s * 1.0f, s * 0.62f, s * 2.0f, s * 0.14f, stone.Darkened(0.2f));
    }

    private void IconCrown(Vector2 c, float s)
    {
        var gold = UiTokens.Accent;
        Poly(new[]
        {
            c + new Vector2(-s * 0.95f, s * 0.45f), c + new Vector2(-s * 0.95f, -s * 0.45f), c + new Vector2(-s * 0.48f, s * 0.0f),
            c + new Vector2(0, -s * 0.75f), c + new Vector2(s * 0.48f, s * 0.0f), c + new Vector2(s * 0.95f, -s * 0.45f),
            c + new Vector2(s * 0.95f, s * 0.45f),
        }, gold);
        Rect(c, -s * 0.95f, s * 0.5f, s * 1.9f, s * 0.22f, gold.Darkened(0.2f));
        DrawCircle(c + new Vector2(0, s * 0.1f), s * 0.14f, UiTokens.Danger);
    }

    // ---------------- Sobreposicao ----------------

    private void PaintOverlay(MapOverlay o)
    {
        if (_regions.Length == 0) return;
        var labels = _regions.Select(BuildLabel).ToList();
        foreach (var l in labels) DrawLabel(o, l);
        DrawRouteBadges(o, labels.Select(l => l.Bounds).ToList());

        if (SelectedPersonId > 0)
        {
            int idx = SelectedPersonId - 1;
            if (idx >= 0 && idx < _personScreen.Length)
            {
                var p = _personScreen[idx];
                float pulse = 0.5f + 0.5f * Mathf.Sin(_time * 5f);
                o.DrawArc(p, 11 + pulse * 3, 0, Mathf.Tau, 32, new Color(UiTokens.Accent, 0.9f), 2.2f, true);
                var name = _persons[idx].Name;
                var font = UiFonts.SemiBold;
                var ts = font.GetStringSize(name, HorizontalAlignment.Left, -1, 13);
                var rect = new Rect2(p.X - ts.X / 2f - 8, p.Y - 40, ts.X + 16, font.GetHeight(13) + 8);
                var pill = ThemeFactory.Box(new Color(UiTokens.Background, 0.92f), 8, border: UiTokens.Accent, radius: 9);
                o.DrawStyleBox(pill, rect);
                o.DrawLine(new Vector2(p.X, rect.End.Y), p + new Vector2(0, -12), new Color(UiTokens.Accent, 0.8f), 1.5f, true);
                o.DrawString(font, new Vector2(rect.Position.X + 8, rect.Position.Y + 4 + font.GetAscent(13)), name,
                    HorizontalAlignment.Left, -1, 13, UiTokens.AccentSoft);
            }
        }

        DrawCompass(o);

        if (_dragging) return;
        if (HoveredPersonId > 0)
        {
            var person = _persons[HoveredPersonId - 1];
            var profile = _content.Profiles.TryGetValue(person.ProfileId, out var pr) ? pr.Name : person.ProfileId;
            Tooltip(o, new (string, Font, int, Color)[]
            {
                (person.Name, UiFonts.SemiBold, 14, UiTokens.TextPrimary),
                ($"Perfil {profile}", UiFonts.Regular, 12, UiTokens.ProfileColor(person.ProfileId)),
                ("Clique para abrir a ficha", UiFonts.Regular, 12, UiTokens.TextSecondary),
            });
        }
        else if (HoveredRegionId != null)
        {
            var r = _content.Regions[HoveredRegionId];
            Tooltip(o, new (string, Font, int, Color)[]
            {
                (r.Name, UiFonts.SemiBold, 14, UiTokens.TextPrimary),
                ($"{CountIn(r.Id)} pessoas agora (demonstração) · {r.Activities.Count} atividades", UiFonts.Regular, 12, UiTokens.TextSecondary),
                ("Clique para inspecionar · duplo clique para focar", UiFonts.Regular, 12, UiTokens.TextSecondary),
            });
        }
    }

    private void Tooltip(MapOverlay o, (string Text, Font Font, int Size, Color Color)[] lines)
    {
        float w = 0, h = 0;
        foreach (var l in lines)
        {
            w = Mathf.Max(w, l.Font.GetStringSize(l.Text, HorizontalAlignment.Left, -1, l.Size).X);
            h += l.Font.GetHeight(l.Size) + 2;
        }
        var size = new Vector2(w + 24, h + 18);
        var pos = _mouse + new Vector2(18, 18);
        var vp = GetViewportRect().Size;
        if (pos.X + size.X > vp.X - 8) pos.X = _mouse.X - size.X - 12;
        if (pos.Y + size.Y > vp.Y - 8) pos.Y = _mouse.Y - size.Y - 12;
        o.DrawStyleBox(_tip, new Rect2(pos, size));
        float y = pos.Y + 9;
        foreach (var l in lines)
        {
            o.DrawString(l.Font, new Vector2(pos.X + 12, y + l.Font.GetAscent(l.Size)), l.Text, HorizontalAlignment.Left, -1, l.Size, l.Color);
            y += l.Font.GetHeight(l.Size) + 2;
        }
    }

    private void DrawCompass(MapOverlay o)
    {
        var c = _viewArea.Position + new Vector2(44, 44);
        o.DrawCircle(c, 26, new Color(UiTokens.Background, 0.55f));
        o.DrawArc(c, 26, 0, Mathf.Tau, 48, new Color(UiTokens.Road, 0.6f), 1.4f, true);
        o.DrawArc(c, 21, 0, Mathf.Tau, 48, new Color(UiTokens.Road, 0.25f), 1f, true);
        o.DrawColoredPolygon(new[] { c + new Vector2(0, -20), c + new Vector2(5, 0), c + new Vector2(-5, 0) }, UiTokens.Accent);
        o.DrawColoredPolygon(new[] { c + new Vector2(0, 20), c + new Vector2(5, 0), c + new Vector2(-5, 0) }, new Color(UiTokens.TextSecondary, 0.7f));
        var font = UiFonts.Display;
        var ts = font.GetStringSize("N", HorizontalAlignment.Left, -1, 11);
        o.DrawString(font, c + new Vector2(-ts.X / 2f, -29), "N", HorizontalAlignment.Left, -1, 11, UiTokens.AccentSoft);
    }
}

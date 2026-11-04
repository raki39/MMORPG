using Simulation.Core.Behavior;
using Simulation.Core.Content;
using Simulation.Core.World;

namespace Simulation.Tests;

/// <summary>Mapa ampliado (D-028): grade de lotes, cobertura ate o teto, cidades e acesso.</summary>
public class MapTests
{
    private static ContentDatabase C => TestSupport.Preset;

    [Fact]
    public void Grade_CadaZonaOcupaUmLote_ESobramLotesLivres()
    {
        var grid = new MapGrid(C);
        Assert.True(grid.IsValid);
        foreach (var z in C.Regions.Values)
        {
            Assert.True(grid.TryGetSlot(z.Id, out var s), z.Id);
            Assert.Equal(z.Id, grid.ZoneAt(s));
        }
        Assert.Equal(grid.LandSlots().Count() - C.Regions.Count, grid.FreeSlots);
        Assert.True(grid.FreeSlots >= C.Regions.Count, "precisa de espaço para expansão");
        // A mascara da forma a ilha: ha mar dentro da grade e toda zona fica em terra.
        Assert.True(grid.LandSlots().Count() < grid.Columns * grid.Rows);
        Assert.All(C.Regions.Values, z => Assert.True(grid.TryGetSlot(z.Id, out var s) && grid.IsLand(s), z.Id));

        // O lote mais proximo de um ponto dentro de uma zona e o da propria zona.
        var vila = C.Regions["vila_aurora"].MapPosition;
        var near = grid.NearestSlot(vila.X + 40, vila.Y - 30)!.Value;
        Assert.Equal("vila_aurora", grid.ZoneAt(near));
        // Lotes vizinhos ficam a um espacamento de distancia (na linha e na diagonal).
        var a = grid.Slot(3, 3);
        var b = grid.Slot(3, 4);
        double diag = Math.Sqrt(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        Assert.InRange(diag, grid.Definition.SpacingX * 0.92, grid.Definition.SpacingX * 1.08);
    }

    [Fact]
    public void Validador_ExigeZonaNoLote_ETerrenoValido()
    {
        var set = TestSupport.FreshPresetSet();
        var floresta = set.Regions.First(r => r.Id == "floresta_sussurrante");
        floresta.MapPosition = new MapPoint { X = floresta.MapPosition.X + 17, Y = floresta.MapPosition.Y };
        var colinas = set.Regions.First(r => r.Id == "colinas_do_vale");
        colinas.MapPosition = new MapPoint { X = set.Regions.First(r => r.Id == "mina_velha").MapPosition.X, Y = set.Regions.First(r => r.Id == "mina_velha").MapPosition.Y };
        var mina = set.Regions.First(r => r.Id == "mina_velha");
        mina.Biome = "lua";
        mina.Terrain.Relief = "cubos";
        mina.Terrain.ElevationPermille = 1200;
        // Zona num lote de mar: a primeira linha da mascara comeca com mar.
        var map = set.World.Map;
        var ravina = set.Regions.First(r => r.Id == "ravina_escarlate");
        ravina.MapPosition = new MapPoint { X = map.OriginX, Y = map.OriginY };
        map.Mask[2] = map.Mask[2][..^1] + "x";
        var errors = ContentValidator.Validate(set).Where(i => i.Severity == IssueSeverity.Error).Select(i => i.Path + " " + i.Message).ToList();
        Assert.Contains(errors, e => e.StartsWith("regions[1].mapPosition") && e.Contains("centro de um lote"));
        Assert.Contains(errors, e => e.Contains("mapPosition") && e.Contains("já é ocupado"));
        Assert.Contains(errors, e => e.StartsWith("regions[3].biome"));
        Assert.Contains(errors, e => e.StartsWith("regions[3].terrain.relief"));
        Assert.Contains(errors, e => e.StartsWith("regions[3].terrain.elevationPermille"));
        Assert.Contains(errors, e => e.StartsWith("regions[5].mapPosition") && e.Contains("é mar"));
        var worldErrors = ContentValidator.Validate(set).Where(i => i.File == ContentValidator.WorldFile).Select(i => i.Path).ToList();
        Assert.Contains("map.mask[2]", worldErrors);
    }

    [Fact]
    public void TodoNivel_TemAoMenosDuasCacas_EUmaSemPvp()
    {
        var p = C.Progression;
        var hunts = C.Regions.Values
            .SelectMany(z => z.AllActivities().Where(a => a.Type == ActivityTypes.Hunt && a.MinGroupSize <= 1).Select(a => (Zone: z, Act: a)))
            .ToList();
        for (int level = 1; level < p.LevelCap; level++)
        {
            var full = hunts.Where(h => h.Act.MinLevel <= level
                && ProgressionRules.Efficiency(p, level, ProgressionRules.ChallengeLevel(C, h.Act)) == 1000).ToList();
            Assert.True(full.Count >= 2, $"nível {level}: {full.Count} caças com rendimento pleno");
            Assert.Contains(full, h => h.Zone.Pvp == PvpRules.None);
        }
    }

    [Fact]
    public void Cidades_TemServicos_EAsCacasFicamPerto()
    {
        var routes = new RouteFinder(C, new ZoneLayout(C));
        var cities = C.Regions.Values.Where(z => z.IsCity).ToList();
        foreach (var city in cities)
        {
            var types = city.AllActivities().Select(a => a.Type).ToHashSet();
            foreach (var t in new[] { ActivityTypes.Trade, ActivityTypes.Repair, ActivityTypes.Socialize, ActivityTypes.Train, ActivityTypes.Quest })
                Assert.Contains(t, types);
        }
        foreach (var z in C.Regions.Values.Where(z => !z.IsCity))
        {
            // Todas alcancaveis a partir do inicio, e a no maximo duas passagens de alguma cidade.
            Assert.NotNull(routes.ZonePath(C.World.StartingRegionId, z.Id));
            int nearest = cities.Min(city => routes.ZonePath(city.Id, z.Id)!.Count);
            Assert.True(nearest <= 2, $"{z.Name}: {nearest} passagens até a cidade mais próxima");
        }
    }

    [Fact]
    public void Areas_TemFaixaCoerenteComODesafio()
    {
        foreach (var z in C.Regions.Values.Where(z => !z.IsCity))
            foreach (var area in z.Areas.Where(a => a.Activities.Any(x => ProgressionRules.ChallengeLevel(C, x) > 0)))
            {
                Assert.True(area.LevelMin > 0, $"{z.Id}/{area.Id} sem faixa");
                foreach (var act in area.Activities)
                {
                    int ch = ProgressionRules.ChallengeLevel(C, act);
                    if (ch > 0) Assert.InRange(ch, area.LevelMin - 2, area.LevelMax + 2);
                }
            }
    }

    [Fact]
    public void Estradas_ContornamAgua_SoCruzamPorPonteCurta_EFicamNaZona()
    {
        var layout = new ZoneLayout(C);
        int bridges = 0;
        foreach (var z in C.Regions.Values)
        {
            var center = layout.Center(z.Id);
            var roads = layout.Roads.Where(r => r.ZoneId == z.Id).ToList();
            // Uma estrada por saida e uma trilha por area.
            Assert.Equal(z.Connections.Count, roads.Count(r => !r.IsTrail));
            Assert.Equal(z.Areas.Count, roads.Count(r => r.IsTrail));
            foreach (var road in roads)
            {
                Assert.Equal(layout.Hub(z.Id), road.Points[0]);
                Assert.All(road.Points, p => Assert.True(p.DistanceTo(center) <= layout.ZoneRadius + 2, $"{z.Id}: ponto fora da zona"));
                // Agua so em pontes, e curtas (onde o rio e estreito).
                double bridge = 0;
                for (int i = 0; i < road.Water.Count; i++)
                    if (road.Water[i]) bridge += road.Points[i].DistanceTo(road.Points[i + 1]);
                if (bridge > 0) bridges++;
                Assert.True(bridge <= 40, $"{z.Id}: ponte de {bridge:0} unidades");
                Assert.True(road.Length >= road.Points[0].DistanceTo(road.Points[^1]));
            }
            // Areas em terra firme.
            foreach (var a in z.Areas)
                Assert.False(layout.Terrain(z.Id).AnyWater(layout.AreaCenter(z.Id, a.Id), a.Radius), $"{z.Id}/{a.Id} sobre água");
        }
        Assert.True(layout.Terrain("brejo_das_brumas").WaterCells > 0, "o brejo deve ter água");
        Assert.True(layout.Terrain("forte_do_vau").WaterCells > 0, "o vale do Forte deve ter rio");
        Assert.True(bridges > 0, "o rio do vale precisa de ponte");

        // Deterministico: outra montagem gera as mesmas estradas.
        var again = new ZoneLayout(C);
        Assert.Equal(layout.Roads.Count, again.Roads.Count);
        for (int i = 0; i < layout.Roads.Count; i++)
            Assert.Equal(layout.Roads[i].Points, again.Roads[i].Points);
    }

    [Fact]
    public void Tile_SeAdapta_ACidadeMovida_RioELagosConfigurados()
    {
        var set = TestSupport.FreshPresetSet();
        var vila = set.Regions.First(r => r.Id == "vila_aurora");
        vila.CityPosition = new MapPoint { X = 30, Y = 12 };
        var forte = set.Regions.First(r => r.Id == "forte_do_vau");
        forte.Terrain.River!.Enabled = false;
        var colinas = set.Regions.First(r => r.Id == "colinas_do_vale");
        colinas.Terrain.PoolsPermille = 250;
        Assert.DoesNotContain(ContentValidator.Validate(set), i => i.Severity == IssueSeverity.Error);

        var db = ContentDatabase.Create(set);
        var layout = new ZoneLayout(db);
        // Cidade movida: encruzilhada, muralha e predios acompanham.
        var hub = layout.Hub("vila_aurora");
        Assert.Equal(new MapPos(vila.MapPosition.X + 30, vila.MapPosition.Y + 12), hub);
        foreach (var a in vila.Areas)
            Assert.True(layout.AreaCenter(vila.Id, a.Id).DistanceTo(hub) + a.Radius <= layout.CityWallRadius(vila.Id));
        Assert.All(layout.Roads.Where(r => r.ZoneId == vila.Id), r => Assert.Equal(hub, r.Points[0]));
        // Rio desligado: sem agua. Lagos nas colinas: ha agua, as estradas contornam (ou ponte curta).
        Assert.Equal(0, layout.Terrain("forte_do_vau").WaterCells);
        Assert.True(layout.Terrain("colinas_do_vale").WaterCells > 0);
        foreach (var road in layout.Roads.Where(r => r.ZoneId == colinas.Id))
        {
            double bridge = 0;
            for (int i = 0; i < road.Water.Count; i++) if (road.Water[i]) bridge += road.Points[i].DistanceTo(road.Points[i + 1]);
            Assert.True(bridge <= 40, $"ponte de {bridge:0}");
        }
        // A simulacao roda com o tile alterado.
        var e = Simulation.Core.Engine.WorldFactory.Create(db, new Simulation.Core.Engine.WorldConfig { Seed = 3, PopulationSize = 60 });
        e.Submit(new Simulation.Core.Commands.SetServerOpenCommand(true), Simulation.Core.Time.SimTime.Hours(8));
        e.AdvanceTo(Simulation.Core.Time.SimTime.Days(3));
        Assert.Contains(e.World.Characters, c => c.Level > 1);

        // Cidade longe demais do centro: a muralha sairia da zona.
        vila.CityPosition = new MapPoint { X = 90, Y = 0 };
        Assert.Contains(ContentValidator.Validate(set), i => i.Path.EndsWith(".cityPosition"));
    }

    [Fact]
    public void Cidade_TemMuralhaPeloNivel_EAsLojasFicamDentro()
    {
        var layout = new ZoneLayout(C);
        foreach (var city in C.Regions.Values.Where(z => z.IsCity))
        {
            Assert.InRange(city.CityLevel, 1, 5);
            int wall = layout.CityWallRadius(city.Id);
            Assert.True(wall > 0);
            foreach (var a in city.Areas)
                Assert.True(layout.AreaCenter(city.Id, a.Id).DistanceTo(layout.Center(city.Id)) + a.Radius <= wall, $"{city.Id}/{a.Id} fora da muralha");
        }
        // Nivel maior, cidade maior.
        var set = TestSupport.FreshPresetSet();
        var vila = set.Regions.First(r => r.Id == "vila_aurora");
        int before = ZoneLayout.CityWallFor(vila, set.World.ZoneRadius);
        vila.CityLevel++;
        Assert.True(ZoneLayout.CityWallFor(vila, set.World.ZoneRadius) > before);
        vila.CityLevel = 9;
        Assert.Contains(ContentValidator.Validate(set), i => i.Path.EndsWith(".cityLevel"));
    }
}

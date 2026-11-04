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
}

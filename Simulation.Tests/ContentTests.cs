using Simulation.Core.Content;
using Simulation.Infrastructure.Content;

namespace Simulation.Tests;

public class ContentTests
{
    private static List<ValidationIssue> Errors(ContentSet set) =>
        ContentValidator.Validate(set).Where(i => i.Severity == IssueSeverity.Error).ToList();

    [Fact]
    public void PresetInicial_CarregaSemErrosNemAvisos()
    {
        var r = ContentLoader.LoadFolder(TestSupport.PresetFolder);
        Assert.True(r.Success, string.Join("\n", r.Issues));
        Assert.Empty(r.Issues);
        var db = r.Database!;
        Assert.Equal(3, db.Classes.Count);
        Assert.Equal(12, db.Abilities.Count);
        Assert.Equal(7, db.Creatures.Count);
        Assert.Single(db.Creatures.Values, c => c.Kind == CreatureKinds.Boss);
        Assert.All(db.Regions.Values, r => Assert.NotEmpty(r.Activities));
        Assert.Contains(db.Regions.Values, r => r.Pvp == PvpRules.Open && r.DeathRule == DeathRules.Partial);
        Assert.Equal(120, db.World.ZoneRadius);
        Assert.Equal(64, r.ContentHash.Length);
    }

    [Fact]
    public void HashDoConteudo_IndependeDeQuebraDeLinha()
    {
        var lf = new Dictionary<string, string> { ["a.json"] = "{\n\"x\": 1\n}" };
        var crlf = new Dictionary<string, string> { ["a.json"] = "{\r\n\"x\": 1\r\n}" };
        Assert.Equal(ContentLoader.ComputeHash(lf), ContentLoader.ComputeHash(crlf));
        var changed = new Dictionary<string, string> { ["a.json"] = "{\n\"x\": 2\n}" };
        Assert.NotEqual(ContentLoader.ComputeHash(lf), ContentLoader.ComputeHash(changed));
    }

    [Fact]
    public void ReferenciaInexistente_ApontaCampoExato()
    {
        var set = TestSupport.FreshPresetSet();
        int idx = set.Abilities.FindIndex(a => a.Id == "bola_de_fogo");
        set.Abilities[idx].ClassId = "necromante";
        var errors = Errors(set);
        Assert.Contains(errors, e => e.File == "abilities.json" && e.Path == $"abilities[{idx}].classId");
    }

    [Fact]
    public void EfeitoDesconhecido_EhErro()
    {
        var set = TestSupport.FreshPresetSet();
        set.Abilities[0].Effects[0].Type = "teleport";
        Assert.Contains(Errors(set), e => e.Path == "abilities[0].effects[0].type");
    }

    [Fact]
    public void IdDuplicado_EhErro()
    {
        var set = TestSupport.FreshPresetSet();
        set.Items[1].Id = set.Items[0].Id;
        Assert.Contains(Errors(set), e => e.Path == "items[1].id" && e.Message.Contains("duplicado"));
    }

    [Fact]
    public void CidadeComPvp_EhErro()
    {
        var set = TestSupport.FreshPresetSet();
        int idx = set.Regions.FindIndex(r => r.IsCity);
        set.Regions[idx].Pvp = PvpRules.Open;
        Assert.Contains(Errors(set), e => e.Path == $"regions[{idx}].pvp");
    }

    [Fact]
    public void CriaturaNaCidade_EhErro()
    {
        var set = TestSupport.FreshPresetSet();
        set.Creatures[0].RegionIds.Add("vila_aurora");
        Assert.Contains(Errors(set), e => e.File == "creatures.json" && e.Path.StartsWith("creatures[0].regionIds"));
    }

    [Fact]
    public void Atividades_RegrasDeLocal()
    {
        var set = TestSupport.FreshPresetSet();
        int city = set.Regions.FindIndex(r => r.Id == "vila_aurora");
        int forest = set.Regions.FindIndex(r => r.Id == "floresta_sussurrante");
        int mine = set.Regions.FindIndex(r => r.Id == "mina_velha");

        // Caca na cidade: proibido.
        set.Regions[city].Activities.Add(new ActivityDefinition
        {
            Id = "cacar_ratos", Type = ActivityTypes.Hunt, Name = "Caçar ratos", TypicalMinutes = 10, CreatureIds = { "lobo_cinzento" },
        });
        // Coleta de item que o local nao tem.
        set.Regions[forest].Activities.Add(new ActivityDefinition
        {
            Id = "minerar", Type = ActivityTypes.Gather, Name = "Minerar", TypicalMinutes = 10, ItemIds = { "minerio_ferro" },
        });
        // Emboscada fora de PvP aberto.
        set.Regions[mine].Activities.Add(new ActivityDefinition
        {
            Id = "emboscar", Type = ActivityTypes.Ambush, Name = "Emboscar", TypicalMinutes = 10,
        });

        var errors = Errors(set);
        string cityPath = $"regions[{city}].activities[{set.Regions[city].Activities.Count - 1}]";
        string forestPath = $"regions[{forest}].activities[{set.Regions[forest].Activities.Count - 1}]";
        string minePath = $"regions[{mine}].activities[{set.Regions[mine].Activities.Count - 1}]";
        Assert.Contains(errors, e => e.Path == cityPath + ".type");
        Assert.Contains(errors, e => e.Path == cityPath + ".creatureIds[0]"); // lobo nao aparece na cidade
        Assert.Contains(errors, e => e.Path == forestPath + ".itemIds[0]");
        Assert.Contains(errors, e => e.Path == minePath + ".type");
    }

    [Fact]
    public void LocalSemAtividades_GeraAviso()
    {
        var set = TestSupport.FreshPresetSet();
        set.Regions[1].Activities.Clear();
        var issues = ContentValidator.Validate(set);
        Assert.Contains(issues, i => i.Severity == IssueSeverity.Warning && i.Path == "regions[1].activities");
        Assert.DoesNotContain(issues, i => i.Severity == IssueSeverity.Error);
    }

    [Fact]
    public void BossComoCaca_EhErro()
    {
        var set = TestSupport.FreshPresetSet();
        int lair = set.Regions.FindIndex(r => r.Id == "covil_do_rei_goblin");
        set.Regions[lair].Activities[0].Type = ActivityTypes.Hunt;
        Assert.Contains(Errors(set), e => e.Path == $"regions[{lair}].activities[0].creatureIds[0]");
    }

    [Fact]
    public void CampoDesconhecidoNoJson_EhErroComLinha()
    {
        var dir = TestSupport.NewTempDir();
        TestSupport.CopyDirectory(TestSupport.PresetFolder, dir);
        var path = Path.Combine(dir, "classes.json");
        File.WriteAllText(path, File.ReadAllText(path).Replace("\"resourceName\": \"Fúria\"", "\"resourceName\": \"Fúria\", \"vidaa\": 10"));

        var r = ContentLoader.LoadFolder(dir);
        Assert.False(r.Success);
        var err = Assert.Single(r.Errors);
        Assert.Equal("classes.json", err.File);
        Assert.Contains("linha", err.Message);
    }

    [Fact]
    public void ArquivoAusente_EhErro()
    {
        var dir = TestSupport.NewTempDir();
        TestSupport.CopyDirectory(TestSupport.PresetFolder, dir);
        File.Delete(Path.Combine(dir, "items.json"));
        var r = ContentLoader.LoadFolder(dir);
        Assert.False(r.Success);
        Assert.Contains(r.Errors, e => e.File == "items.json");
    }

    [Fact]
    public void CriarBaseComConteudoInvalido_Lanca()
    {
        var set = TestSupport.FreshPresetSet();
        set.World.StartingRegionId = "floresta_sussurrante";
        var ex = Assert.Throws<ContentValidationException>(() => ContentDatabase.Create(set));
        Assert.Contains(ex.Issues, i => i.Path == "startingRegionId");
    }
}

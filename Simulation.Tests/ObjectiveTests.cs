using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

/// <summary>
/// Objetivos pessoais (O1, D-062): sorteio inicial (quantidades, importancia, coerencia com
/// Preferencias, conduta, traits e dedicacao), parametros sem inventar alvo, viabilidade separada
/// da preparacao, nenhum progresso ficticio e nenhum efeito no comportamento nesta entrega.
/// </summary>
public class ObjectiveTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private const ulong Seed = 77;

    private static List<ValidationIssue> Errors(ContentSet set) =>
        ContentValidator.Validate(set).Where(i => i.Severity == IssueSeverity.Error).ToList();

    private static readonly string[] Classes = { "guerreiro", "mago", "sacerdote" };

    /// <summary>Populacao gerada (sem simular) com os objetivos de cada pessoa.</summary>
    private static List<(Person P, List<PersonObjective> O)> Population(int n, ContentDatabase? c = null)
    {
        c ??= C;
        var list = new List<(Person, List<PersonObjective>)>(n);
        for (int id = 1; id <= n; id++)
        {
            var p = PopulationGenerator.Generate(id, c, Seed);
            list.Add((p, ObjectiveSystem.Draw(c, p, Classes[id % 3], Seed, 0)));
        }
        return list;
    }

    private static readonly Lazy<List<(Person P, List<PersonObjective> O)>> Pop = new(() => Population(6000));

    /// <summary>Erro padrao da proporcao (5 erros padrao de tolerancia, como nos demais testes estatisticos).</summary>
    private static double Tol(double q, int n) => 5 * Math.Sqrt(q * (1 - q) / n);

    // ---------------- Sorteio ----------------

    [Fact]
    public void Sorteio_QuantidadesImportanciaEDeterminismo()
    {
        var g = C.ObjectiveCatalog.Generation;
        int two = 0;
        foreach (var (p, objs) in Pop.Value)
        {
            int general = objs.Count(o => o.Tier == ObjectiveTiers.General), secondary = objs.Count - general;
            Assert.InRange(general, 1, 2);
            Assert.InRange(secondary, 0, 3);
            Assert.Equal(objs.Count, objs.Select(o => o.Id).Distinct().Count());
            // Geral = o mais importante para a pessoa.
            if (secondary > 0 && general > 0)
                Assert.True(objs.Where(o => o.Tier == ObjectiveTiers.General).Min(o => o.Importance) >= objs.Where(o => o.Tier == ObjectiveTiers.Secondary).Max(o => o.Importance));
            Assert.All(objs, o => Assert.InRange(o.Importance, 50, 1000));
            Assert.All(objs, o => Assert.Equal(ObjectiveSystem.CommitmentOf(g, p.Traits), o.Commitment));
            if (general == 2) two++;
        }
        // A maioria com um geral (pesos 750/250).
        double share = two / (double)Pop.Value.Count;
        Assert.InRange(share, 0.25 - Tol(0.25, Pop.Value.Count), 0.25 + Tol(0.25, Pop.Value.Count));
        // Mesmo resultado com a mesma pessoa e a mesma seed.
        var (p0, o0) = Pop.Value[10];
        var again = ObjectiveSystem.Draw(C, p0, Classes[11 % 3], Seed, 0);
        Assert.Equal(o0.Select(o => (o.Id, o.Param, o.Tier, o.Importance)), again.Select(o => (o.Id, o.Param, o.Tier, o.Importance)));
    }

    [Fact]
    public void Sorteio_GeralESecundarioNaoSaoCategoriasDoCatalogo()
    {
        // O mesmo objetivo aparece como geral para uns e secundario para outros.
        var all = Pop.Value.SelectMany(x => x.O).ToList();
        foreach (var id in new[] { "entrar_guilda", "nivel_maximo", "equipamento_raro" })
        {
            Assert.Contains(all, o => o.Id == id && o.Tier == ObjectiveTiers.General);
            Assert.Contains(all, o => o.Id == id && o.Tier == ObjectiveTiers.Secondary);
        }
    }

    private static double Rate(IEnumerable<(Person P, List<PersonObjective> O)> group, string id)
    {
        var g = group.ToList();
        return g.Count == 0 ? 0 : g.Count(x => x.O.Any(o => o.Id == id)) / (double)g.Count;
    }

    private static (List<(Person P, List<PersonObjective> O)> Low, List<(Person P, List<PersonObjective> O)> High) Thirds(Func<Person, int> key)
    {
        var sorted = Pop.Value.OrderBy(x => key(x.P)).ThenBy(x => x.P.Id).ToList();
        int third = sorted.Count / 3;
        return (sorted.Take(third).ToList(), sorted.Skip(sorted.Count - third).ToList());
    }

    [Fact]
    public void Coerencia_ConhecerOMundoPelaExploracao_SemSerExclusivo()
    {
        var (low, high) = Thirds(p => p.Preferences.Exploration);
        double hi = Rate(high, "conhecer_o_mundo"), lo = Rate(low, "conhecer_o_mundo");
        Assert.True(hi > lo * 1.5, $"exploração alta {hi:P1} x baixa {lo:P1}");
        Assert.True(lo > 0, "quem gosta pouco de explorar ainda pode querer conhecer o mundo");
    }

    [Fact]
    public void Coerencia_PvpPelaPreferenciaECompetitividade()
    {
        var (low, high) = Thirds(p => p.Preferences.Pvp);
        Assert.True(Rate(high, "pvp_competitivo") > Rate(low, "pvp_competitivo") * 1.5);
        var (lc, hc) = Thirds(p => p.Traits.Competitiveness);
        Assert.True(Rate(hc, "mais_forte") > Rate(lc, "mais_forte") * 1.3);
        Assert.True(Rate(hc, "grande_comerciante") > Rate(lc, "grande_comerciante"), "prestígio no comércio pesa pela competitividade");
    }

    [Fact]
    public void Coerencia_TraitsPesam()
    {
        bool Has(Person p, string q) => p.Quirks.Any(x => x.Id == q);
        var lider = Pop.Value.Where(x => Has(x.P, "lider_nato")).ToList();
        var lobo = Pop.Value.Where(x => Has(x.P, "lobo_solitario")).ToList();
        var resto = Pop.Value.Where(x => !Has(x.P, "lider_nato") && !Has(x.P, "lobo_solitario")).ToList();
        if (lider.Count >= 30) Assert.True(Rate(lider, "fundar_guilda") > Rate(resto, "fundar_guilda"));
        if (lobo.Count >= 30) Assert.True(Rate(lobo, "entrar_guilda") < Rate(resto, "entrar_guilda"));
        Assert.True(lider.Count >= 30 || lobo.Count >= 30, "amostra pequena demais para conferir traits");
    }

    [Fact]
    public void Ambicao_DedicacaoPuxaParaObjetivosDificeis_SemImpedirOsCasuais()
    {
        double Difficulty(IEnumerable<(Person P, List<PersonObjective> O)> g) =>
            g.SelectMany(x => x.O.Where(o => o.Tier == ObjectiveTiers.General)).Average(o => C.Objectives[o.Id].Difficulty);
        var casual = Pop.Value.Where(x => x.P.ProfileId == "casual").ToList();
        var dedicated = Pop.Value.Where(x => x.P.ProfileId is "hardcore" or "devoto").ToList();
        Assert.True(casual.Count > 200 && dedicated.Count > 200);
        Assert.True(Difficulty(dedicated) > Difficulty(casual) + 30, $"dedicados {Difficulty(dedicated):0} x casuais {Difficulty(casual):0}");
        // Casuais tambem tem grandes ambicoes; e ambicoes simples aparecem mais entre eles.
        Assert.Contains(casual, x => x.O.Any(o => o.Tier == ObjectiveTiers.General && C.Objectives[o.Id].Difficulty >= 850));
        Assert.True(Rate(casual, "entrar_guilda") > Rate(dedicated, "entrar_guilda"));
        Assert.True(Rate(dedicated, "grandes_desafios_pve") > Rate(casual, "grandes_desafios_pve"));
    }

    [Fact]
    public void Guildas_EntrarECriarPodemCoexistir()
    {
        Assert.Contains(Pop.Value, x => x.O.Any(o => o.Id == "entrar_guilda") && x.O.Any(o => o.Id == "fundar_guilda"));
    }

    [Fact]
    public void Parametros_SoAlvosQueExistemParaAClasse()
    {
        var rare = C.Objectives["equipamento_raro"];
        var g = C.ObjectiveCatalog.Generation;
        foreach (var cls in Classes)
            foreach (var id in ObjectiveSystem.ParamOptions(C, rare, cls))
            {
                var it = C.Items[id];
                Assert.Contains(it.Rarity, g.RareItemRarities);
                Assert.Contains(it.Category, new[] { ItemCategories.Weapon, ItemCategories.Armor });
                Assert.True(it.ClassIds.Count == 0 || it.ClassIds.Contains(cls));
            }
        // O guerreiro nao tem arma rara no conteudo: so a coroa (epica, qualquer classe).
        Assert.Equal(new[] { "coroa_do_rei_goblin" }, ObjectiveSystem.ParamOptions(C, rare, "guerreiro"));
        foreach (var (p, objs) in Pop.Value.Take(2000))
            foreach (var o in objs.Where(o => o.Id == "melhor_fabricante"))
                Assert.Contains(o.Param, ObjectiveSystem.CraftingSpecialties);
    }

    // ---------------- Viabilidade ----------------

    private static ObjectiveAssessment Assess(string id, int level, string param = "", Action<Character>? setup = null)
    {
        var ch = new Character(1, 1, "Teste", "mago") { Level = level };
        setup?.Invoke(ch);
        return ObjectiveSystem.Assess(C, ch, new PersonObjective(id, param, ObjectiveTiers.General, 500, 500, ObjectiveSystem.InitialOrigin, 0, Array.Empty<(string, int)>()));
    }

    [Fact]
    public void Viabilidade_PodeAvancarAgora_ComProgressoReal()
    {
        int cap = C.Progression.LevelCap;
        var a = Assess("nivel_maximo", 9);
        Assert.Equal(ObjectiveViability.Now, a.Viability);
        Assert.Equal((9, cap), (a.Progress, a.ProgressTotal));
        Assert.False(a.CriterionMet);
        Assert.True(Assess("nivel_maximo", cap).CriterionMet);

        var w = Assess("conhecer_o_mundo", 5, setup: ch => { ch.Known.Add("vila_aurora"); ch.Known.Add("floresta_sussurrante"); });
        Assert.Equal(ObjectiveViability.Now, w.Viability);
        Assert.Equal((2, C.Regions.Count), (w.Progress, w.ProgressTotal));

        var m = Assess("campanha_principal", 1);
        Assert.Equal(C.Quests.Values.Count(q => q.Category == QuestCategories.Main), m.ProgressTotal);
        Assert.Equal(0, m.Progress);
    }

    [Fact]
    public void Viabilidade_SistemaFuturo_SemProgresso_ComPreparacaoSeparada()
    {
        // Chefes: depende de grupos e combate; a preparacao e o nivel do proximo chefe (requisito do conteudo).
        var boss = Assess("grandes_desafios_pve", 5);
        Assert.Equal(ObjectiveViability.Future, boss.Viability);
        Assert.Equal(new[] { ObjectiveSystems.Groups, ObjectiveSystems.Combat }, boss.MissingSystems);
        Assert.Equal(15, boss.PreparationLevel);
        Assert.Equal("covil_do_rei_goblin/enfrentar_rei", boss.PreparationSubject);
        Assert.False(boss.Measurable);
        Assert.Equal(28, Assess("grandes_desafios_pve", 20).PreparationLevel);
        Assert.Equal(0, Assess("grandes_desafios_pve", 30).PreparationLevel);

        // PvP: acesso ao conteudo PvP pelo nivel; nao e evidencia de vitoria.
        Assert.Equal(10, Assess("pvp_competitivo", 5).PreparationLevel);
        Assert.Equal(0, Assess("pvp_competitivo", 12).PreparationLevel);
        // Mais forte: o nivel maximo como preparacao (nao o indice de poder).
        Assert.Equal(C.Progression.LevelCap, Assess("mais_forte", 5).PreparationLevel);
        // Equipamento raro: o nivel exigido pelo item.
        var rare = Assess("equipamento_raro", 5, "cajado_runico");
        Assert.Equal((12, "cajado_runico"), (rare.PreparationLevel, rare.PreparationSubject));

        // Sem requisito concreto: nenhuma preparacao inventada, nenhum progresso.
        foreach (var id in new[] { "grande_fortuna", "fundar_guilda", "entrar_guilda", "melhor_fabricante", "grande_comerciante" })
        {
            var a = Assess(id, 3, id == "melhor_fabricante" ? ItemCategories.Weapon : "");
            Assert.Equal(ObjectiveViability.Future, a.Viability);
            Assert.Equal(0, a.PreparationLevel);
            Assert.False(a.Measurable);
            Assert.False(a.CriterionMet);
        }
    }

    // ---------------- Simulacao ----------------

    private static SimulationEngine Engine(ContentDatabase c, int pop, IReadOnlyList<ControlledCell>? cells = null)
    {
        var e = WorldFactory.Create(c, new WorldConfig { Seed = 21, PopulationSize = pop, ControlledCells = cells });
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        return e;
    }

    [Fact]
    public void Simulacao_ObjetivosSemInfluencia_NaoMudamOComportamento()
    {
        // Objetivos sorteados com a influencia (O2) desligada: o comportamento e o mesmo de sem objetivos.
        var noInfluence = TestSupport.FreshPresetSet();
        noInfluence.Objectives.Influence.Enabled = false;
        var off = TestSupport.FreshPresetSet();
        off.Objectives.Generation.Enabled = false;
        var a = Engine(ContentDatabase.Create(noInfluence), 150);
        var b = Engine(ContentDatabase.Create(off), 150);
        a.AdvanceTo(SimTime.Days(4));
        b.AdvanceTo(SimTime.Days(4));
        Assert.Equal(a.World.Characters.Count, b.World.Characters.Count);
        for (int i = 0; i < a.World.Characters.Count; i++)
        {
            var x = a.World.Characters[i];
            var y = b.World.Characters[i];
            Assert.Equal((x.ClassId, x.Level, x.Experience, x.PlayedTicks, x.RegionId, x.Position, x.GoalsSet), (y.ClassId, y.Level, y.Experience, y.PlayedTicks, y.RegionId, y.Position, y.GoalsSet));
        }
        foreach (var p in a.World.Persons)
        {
            var q = b.World.GetPerson(p.Id);
            Assert.Equal((p.Mood.Satisfaction, p.Mood.Frustration, p.Mood.Engagement), (q.Mood.Satisfaction, q.Mood.Frustration, q.Mood.Engagement));
            Assert.Empty(q.Objectives);
            if (p.CharacterId > 0) Assert.NotEmpty(p.Objectives); else Assert.Empty(p.Objectives);
        }
        // Objetivos entram no estado: com e sem eles o hash difere.
        Assert.NotEqual(StateHasher.ComputeWithoutContentId(a.World), StateHasher.ComputeWithoutContentId(b.World));
    }

    [Fact]
    public void Registro_HistoricoEEventosComCausa()
    {
        var e = Engine(C, 120);
        var events = new List<Simulation.Core.Events.SimEvent>();
        for (int d = 1; d <= 3; d++) { e.AdvanceTo(SimTime.Days(d)); events.AddRange(e.World.Events.Drain()); }
        var registered = e.World.Persons.Where(p => p.CharacterId > 0).ToList();
        Assert.NotEmpty(registered);
        foreach (var p in registered)
        {
            Assert.Equal(p.Objectives.Count, p.ObjectiveHistory.Count);
            Assert.All(p.ObjectiveHistory, h => Assert.Equal(("set", ObjectiveSystem.InitialOrigin), (h.Change, h.Reason)));
            var ch = e.World.GetCharacter(p.CharacterId);
            // O equipamento raro e da classe do personagem.
            foreach (var o in p.Objectives.Where(o => o.Id == "equipamento_raro"))
                Assert.Contains(o.Param, ObjectiveSystem.ParamOptions(C, C.Objectives[o.Id], ch.ClassId));
        }
        var set = events.Where(x => x.Type == "objective_set").ToList();
        Assert.Equal(registered.Sum(p => p.Objectives.Count), set.Count);
        Assert.All(set, x => Assert.NotEmpty(x.Causes));
    }

    [Fact]
    public void CenarioControlado_SemObjetivos()
    {
        var e = Engine(C, 40, new[] { new ControlledCell("casual", "sem_ocupacao") });
        e.AdvanceTo(SimTime.Days(3));
        Assert.Contains(e.World.Persons, p => p.CharacterId > 0);
        Assert.All(e.World.Persons, p => Assert.Empty(p.Objectives));
    }

    // ---------------- Validador ----------------

    [Fact]
    public void Validador_Objetivos()
    {
        ContentSet Fresh(Action<ContentSet> change)
        {
            var s = TestSupport.FreshPresetSet();
            change(s);
            return s;
        }
        Assert.Empty(Errors(TestSupport.FreshPresetSet()));
        Assert.Contains(Errors(Fresh(s => s.Objectives.Objectives[0].Criterion = "ser_famoso")), x => x.Path == "objectives[0].criterion");
        Assert.Contains(Errors(Fresh(s => s.Objectives.Objectives[0].Horizon = "eterno")), x => x.Path == "objectives[0].horizon");
        Assert.Contains(Errors(Fresh(s => s.Objectives.Objectives[0].Difficulty = 1200)), x => x.Path == "objectives[0].difficulty");
        Assert.Contains(Errors(Fresh(s => s.Objectives.Objectives[0].Affinities.Add(new QuirkAffinityDefinition { Key = "quirk.nao_existe", StrengthPermille = 100 }))),
            x => x.Path.StartsWith("objectives[0].affinities"));
        Assert.Contains(Errors(Fresh(s => s.Objectives.Generation.GeneralCountWeights = new List<int> { 0, 0, 0 })), x => x.Path == "generation.generalCountWeights");
        Assert.Contains(Errors(Fresh(s => s.Objectives.Generation.RareItemRarities = new List<string> { "lendario" })), x => x.Path == "generation.rareItemRarities");
    }
}

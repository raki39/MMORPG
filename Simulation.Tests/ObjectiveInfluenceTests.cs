using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Time;

namespace Simulation.Tests;

/// <summary>
/// Objetivos pessoais na O2 (D-063): vinculo das metas so por avanco real ou preparacao por requisito
/// conhecido; so o maior reforco; influencia so pela adocao e pelo comprometimento das metas; conclusao
/// com a maior recompensa quando o mesmo acontecimento conclui meta e objetivo; metas independentes.
/// </summary>
public class ObjectiveInfluenceTests
{
    private static ContentDatabase C => TestSupport.Preset;

    private static PersonObjective Obj(string id, string param = "", int importance = 600, int commitment = 600, string tier = ObjectiveTiers.General) =>
        new(id, param, tier, importance, commitment, ObjectiveSystem.InitialOrigin, 0, Array.Empty<(string, int)>());

    private static (Person P, Character Ch) Fixture(int level, params PersonObjective[] objectives)
    {
        var p = PopulationGenerator.Generate(1, C, 5);
        p.ObjectiveList.AddRange(objectives);
        var ch = new Character(1, 1, "Teste", "mago") { Level = level };
        ch.Known.Add("vila_aurora");
        return (p, ch);
    }

    private static List<ObjectiveLink> L(Person p, Character ch, string type, string target = "") => ObjectiveSystem.Links(C, p, ch, type, target);

    // ---------------- Vinculos ----------------

    [Fact]
    public void Vinculo_AvancoReal()
    {
        var (p, ch) = Fixture(5, Obj("nivel_maximo"), Obj("campanha_principal"), Obj("conhecer_o_mundo"));
        Assert.Contains(L(p, ch, GoalTypes.ReachLevel), l => l.ObjectiveId == "nivel_maximo" && l.Kind == ObjectiveLinkKinds.Advance);
        Assert.Contains(L(p, ch, GoalTypes.UnlockLevel, "floresta_sussurrante/x"), l => l.ObjectiveId == "nivel_maximo");
        var quest = L(p, ch, GoalTypes.CompleteQuest, "vila_principal_2");
        Assert.Equal(new[] { ("campanha_principal", ObjectiveLinkKinds.Advance) }, quest.Select(l => (l.ObjectiveId, l.Kind)));
        Assert.Empty(L(p, ch, GoalTypes.CompleteQuest, "vila_secundaria_ervas"));       // secundaria nao avanca a campanha
        Assert.Contains(L(p, ch, GoalTypes.DiscoverZone, "floresta_sussurrante"), l => l.ObjectiveId == "conhecer_o_mundo");
        Assert.Empty(L(p, ch, GoalTypes.DiscoverZone, "vila_aurora"));                  // ja conhecida
        Assert.Empty(L(p, ch, GoalTypes.PracticeActivity, ActivityTypes.Explore));      // praticar nao e avanco concreto
        Assert.Empty(L(p, ch, GoalTypes.PracticeActivity, ActivityTypes.Hunt));
    }

    [Fact]
    public void Vinculo_PreparacaoSoPorRequisitoConcretoEConhecido()
    {
        var (p, ch) = Fixture(5, Obj("grandes_desafios_pve"), Obj("pvp_competitivo"), Obj("equipamento_raro", "cajado_runico"),
            Obj("mais_forte"), Obj("grande_fortuna"), Obj("fundar_guilda"), Obj("entrar_guilda"), Obj("melhor_fabricante", ItemCategories.Weapon), Obj("grande_comerciante"));
        var links = L(p, ch, GoalTypes.ReachLevel);
        Assert.All(links, l => Assert.Equal(ObjectiveLinkKinds.Preparation, l.Kind));
        // Chefe e PvP: a pessoa ainda nao conhece os lugares; item escolhido e nivel maximo ela conhece.
        Assert.Equal(new[] { "equipamento_raro", "mais_forte" }, links.Select(l => l.ObjectiveId).OrderBy(x => x, StringComparer.Ordinal));
        ch.Known.Add("covil_do_rei_goblin");
        ch.Hints.Add("ravina_escarlate/emboscar_viajantes");
        links = L(p, ch, GoalTypes.ReachLevel);
        Assert.Contains(links, l => l.ObjectiveId == "grandes_desafios_pve" && l.Subject == "covil_do_rei_goblin/enfrentar_rei");
        Assert.Contains(links, l => l.ObjectiveId == "pvp_competitivo" && l.Subject == "ravina_escarlate/emboscar_viajantes");
        // Sem requisito existente: nunca ha meta ligada (nada de XP generico).
        foreach (var id in new[] { "grande_fortuna", "fundar_guilda", "entrar_guilda", "melhor_fabricante", "grande_comerciante" })
            foreach (var type in new[] { GoalTypes.ReachLevel, GoalTypes.UnlockLevel, GoalTypes.DiscoverZone, GoalTypes.CompleteQuest, GoalTypes.PracticeActivity })
                Assert.DoesNotContain(L(p, ch, type, "floresta_sussurrante"), l => l.ObjectiveId == id);
        // Requisito cumprido: sem preparacao.
        ch.Level = 16;
        Assert.DoesNotContain(L(p, ch, GoalTypes.ReachLevel), l => l.ObjectiveId is "equipamento_raro" or "pvp_competitivo");
        Assert.DoesNotContain(L(p, ch, GoalTypes.ReachLevel), l => l.ObjectiveId == "grandes_desafios_pve");   // o proximo chefe (28) fica numa zona desconhecida
    }

    [Fact]
    public void Vinculo_ObjetivoConcluidoOuSistemaFuturoSemRequisito_NaoLiga()
    {
        var done = Obj("nivel_maximo");
        done.State = ObjectiveStates.Completed;
        var (p, ch) = Fixture(10, done);
        Assert.Empty(L(p, ch, GoalTypes.ReachLevel));
    }

    // ---------------- Reforco e comprometimento ----------------

    [Fact]
    public void Reforco_SoOMaiorEntreOsObjetivos()
    {
        var strong = Obj("nivel_maximo", importance: 900, commitment: 800);
        var weak = Obj("mais_forte", importance: 300, commitment: 500);
        var (p, ch) = Fixture(5, strong, weak, Obj("equipamento_raro", "cajado_runico", 700, 700));
        var links = L(p, ch, GoalTypes.ReachLevel);
        Assert.Equal(3, links.Count);
        int boost = C.ObjectiveCatalog.Influence.AdoptionBoostPermille;
        int expected = 1000 * (1000 + ObjectiveSystem.Strength(boost, strong)) / 1000;
        Assert.Equal(expected, ObjectiveSystem.AdoptionWeight(C, p, 1000, links));
        // O mesmo que so com o objetivo mais forte: ter mais objetivos nao soma.
        Assert.Equal(expected, ObjectiveSystem.AdoptionWeight(C, p, 1000, links.Where(l => l.ObjectiveId == "nivel_maximo").ToList()));
        Assert.Equal(1000, ObjectiveSystem.AdoptionWeight(C, p, 1000, Array.Empty<ObjectiveLink>()));   // meta independente: igual
        int c = ObjectiveSystem.LinkedCommitment(C, p, 600, links);
        Assert.InRange(c, 601, 1000);
        Assert.Equal(600, ObjectiveSystem.LinkedCommitment(C, p, 600, Array.Empty<ObjectiveLink>()));
    }

    [Fact]
    public void InfluenciaDesligada_PesoEComprometimentoIguais()
    {
        var set = TestSupport.FreshPresetSet();
        set.Objectives.Influence.Enabled = false;
        var db = ContentDatabase.Create(set);
        var p = PopulationGenerator.Generate(1, db, 5);
        p.ObjectiveList.Add(Obj("nivel_maximo", importance: 1000, commitment: 1000));
        var links = new List<ObjectiveLink> { new("nivel_maximo", ObjectiveLinkKinds.Advance, "") };
        Assert.Equal(1000, ObjectiveSystem.AdoptionWeight(db, p, 1000, links));
        Assert.Equal(500, ObjectiveSystem.LinkedCommitment(db, p, 500, links));
    }

    // ---------------- Na simulacao ----------------

    private static SimulationEngine Engine(ContentDatabase c, ulong seed, int pop)
    {
        var e = WorldFactory.Create(c, new WorldConfig { Seed = seed, PopulationSize = pop });
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        return e;
    }

    [Fact]
    public void Simulacao_MetasLigadasEMetasIndependentes()
    {
        var e = Engine(C, 31, 250);
        e.AdvanceTo(SimTime.Days(8));
        var chars = e.World.Characters;
        var goals = chars.SelectMany(ch => ch.Goals.Select(g => (ch, g))).ToList();
        Assert.Contains(goals, x => x.g.Links.Count > 0);
        Assert.Contains(goals, x => x.g.Links.Count == 0);                                   // metas independentes continuam
        Assert.All(goals.Where(x => x.g.Type == GoalTypes.PracticeActivity), x => Assert.Empty(x.g.Links));
        foreach (var (ch, g) in goals)
        {
            var p = e.World.GetPerson(ch.PersonId);
            foreach (var l in g.Links)
            {
                Assert.Contains(p.Objectives, o => o.Id == l.ObjectiveId);
                var crit = C.Objectives[l.ObjectiveId].Criterion;
                Assert.DoesNotContain(crit, new[] { ObjectiveCriteria.Wealth, ObjectiveCriteria.FoundGuild, ObjectiveCriteria.JoinGuild, ObjectiveCriteria.CraftingRank, ObjectiveCriteria.TradeRank });
                if (l.Kind == ObjectiveLinkKinds.Preparation) Assert.Contains(g.Type, new[] { GoalTypes.ReachLevel, GoalTypes.UnlockLevel });
            }
            // Comprometimento no limite (a conta do reforco e conferida em Reforco_SoOMaiorEntreOsObjetivos;
            // a base muda com os atributos depois da adocao, entao nao se compara com a base de agora).
            Assert.InRange(g.Commitment, 1, 1000);
        }
        // Contadores: cada meta ligada conta em pelo menos um objetivo; ligadas nunca passam do total.
        Assert.True(e.World.Persons.Sum(p => p.Objectives.Sum(o => o.LinkedGoalsSet)) >= chars.Sum(c => c.GoalsLinkedSet));
        Assert.True(chars.Sum(c => c.GoalsLinkedSet) <= chars.Sum(c => c.GoalsSet));
        Assert.True(chars.Sum(c => c.GoalsLinkedSet) > 0);
    }

    [Fact]
    public void Adocao_PesoMaiorParaAMetaLigada()
    {
        var e = Engine(C, 31, 120);
        e.AdvanceTo(SimTime.Days(3));
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0);
        var ch = w.GetCharacter(p.CharacterId);
        ch.GoalList.Clear();
        p.ObjectiveList.Clear();
        p.ObjectiveList.Add(Obj("nivel_maximo", importance: 800, commitment: 800));
        var list = w.Behavior.Goals.CandidateWeights(p, ch, new DeterministicRng(1));
        Assert.NotEmpty(list);
        foreach (var x in list)
        {
            if (x.Links.Count == 0) Assert.Equal(x.Weight, x.Effective);
            else Assert.True(x.Effective > x.Weight);
        }
        Assert.Contains(list, x => x.Type == GoalTypes.ReachLevel && x.Links.Count > 0);
    }

    // ---------------- Conclusao e recompensa ----------------

    /// <summary>Pessoa registrada com um so objetivo ("conhecer todo o mundo") a uma zona do fim.</summary>
    private static (Simulation.Core.World.WorldState W, Person P, Character Ch, string Last) AlmostWorld(ContentDatabase c)
    {
        var e = Engine(c, 41, 80);
        e.AdvanceTo(SimTime.Days(2));
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0);
        var ch = w.GetCharacter(p.CharacterId);
        p.ObjectiveList.Clear();
        p.ObjectiveList.Add(Obj("conhecer_o_mundo"));
        ch.GoalList.Clear();
        var eligible = ObjectiveSystem.EligibleZones(c).OrderBy(z => z, StringComparer.Ordinal).ToList();
        string last = eligible.First(z => z != c.World.StartingRegionId);
        foreach (var z in eligible) if (z != last) ch.Known.Add(z);
        ch.Known.Remove(last);
        return (w, p, ch, last);
    }

    [Fact]
    public void ConhecerOMundo_CriterioSaoTodasAsZonasElegiveis()
    {
        Assert.Equal(C.Regions.Count, ObjectiveSystem.EligibleZones(C).Count);   // no preset todas sao alcancaveis
        var (w, p, ch, last) = AlmostWorld(C);
        var a = ObjectiveSystem.Assess(C, ch, p.Objectives[0]);
        Assert.Equal((ObjectiveSystem.EligibleZones(C).Count - 1, ObjectiveSystem.EligibleZones(C).Count), (a.Progress, a.ProgressTotal));
        Assert.False(a.CriterionMet);
        ch.Known.Add(last);
        Assert.True(ObjectiveSystem.Assess(C, ch, p.Objectives[0]).CriterionMet);
    }

    [Fact]
    public void Conclusao_MesmoAcontecimento_ValeAMaiorRecompensa()
    {
        var (w, p, ch, last) = AlmostWorld(C);
        // Meta de descobrir a ultima zona, ligada ao objetivo.
        var g = new PersonalGoal(GoalTypes.DiscoverZone, last, 1, 0, 600, "pref_exploration", 500, w.Clock.Tick, ch.PlayedTicks);
        g.LinkList.Add(new ObjectiveLink("conhecer_o_mundo", ObjectiveLinkKinds.Advance, last));
        ch.GoalList.Add(g);
        int before = ch.SessionLedger.Points;
        ch.Known.Add(last);
        w.Behavior.Goals.OnDiscovered(p, ch, last);       // conclui a meta (recompensa da meta)
        w.Behavior.CheckObjectives(p, ch, "zone_discovered");   // conclui o objetivo no mesmo acontecimento
        int goalPts = C.Mood.GoalCompletedPoints, objPts = C.ObjectiveCatalog.Influence.CompletedPoints;
        Assert.Equal(Math.Max(goalPts, objPts), ch.SessionLedger.Points - before);
        var o = p.Objectives[0];
        Assert.Equal(ObjectiveStates.Completed, o.State);
        Assert.True(o.CompletedDay >= 0);
        Assert.Equal(1, o.LinkedGoalsCompleted);
        Assert.Contains(p.ObjectiveHistory, h => h.Id == "conhecer_o_mundo" && h.Change == "completed" && h.Reason == "zone_discovered");
        Assert.Contains(p.Memories, m => m.Kind == "objective_completed");
        // Nao conclui de novo.
        int after = ch.SessionLedger.Points;
        w.Behavior.CheckObjectives(p, ch, "zone_discovered");
        Assert.Equal(after, ch.SessionLedger.Points);
    }

    [Fact]
    public void Conclusao_SemMetaLigada_RecompensaDoObjetivo()
    {
        var (w, p, ch, last) = AlmostWorld(C);
        int before = ch.SessionLedger.Points;
        ch.Known.Add(last);
        w.Behavior.CheckObjectives(p, ch, "zone_discovered");
        Assert.Equal(C.ObjectiveCatalog.Influence.CompletedPoints, ch.SessionLedger.Points - before);
    }

    [Fact]
    public void Conclusao_InfluenciaDesligada_RegistraSemRecompensa()
    {
        var set = TestSupport.FreshPresetSet();
        set.Objectives.Influence.Enabled = false;
        var db = ContentDatabase.Create(set);
        var (w, p, ch, last) = AlmostWorld(db);
        int before = ch.SessionLedger.Points;
        ch.Known.Add(last);
        w.Behavior.CheckObjectives(p, ch, "zone_discovered");
        Assert.Equal(ObjectiveStates.Completed, p.Objectives[0].State);
        Assert.Equal(before, ch.SessionLedger.Points);
    }

    // ---------------- Medicao e validador ----------------

    [Fact]
    public void Grupo_DeMedicaoPelosObjetivos()
    {
        Person With(params string[] ids) { var p = PopulationGenerator.Generate(1, C, 5); foreach (var id in ids) p.ObjectiveList.Add(Obj(id)); return p; }
        Assert.Equal("actionable", ObjectiveSystem.Group(C, With("grande_fortuna", "conhecer_o_mundo")));
        Assert.Equal("preparation", ObjectiveSystem.Group(C, With("grande_fortuna", "pvp_competitivo")));
        Assert.Equal("unavailable", ObjectiveSystem.Group(C, With("grande_fortuna", "entrar_guilda")));
        Assert.Equal("", ObjectiveSystem.Group(C, With()));
    }

    [Fact]
    public void Validador_Influencia()
    {
        var set = TestSupport.FreshPresetSet();
        set.Objectives.Influence.AdoptionBoostPermille = 5000;
        set.Objectives.Influence.CompletedPoints = -1;
        var errs = ContentValidator.Validate(set).Where(i => i.Severity == IssueSeverity.Error).ToList();
        Assert.Contains(errs, x => x.Path == "influence.adoptionBoostPermille");
        Assert.Contains(errs, x => x.Path == "influence.completedPoints");
    }
}

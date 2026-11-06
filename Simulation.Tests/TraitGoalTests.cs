using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

/// <summary>Atributos vivos e metas de curto prazo (D-036, revisao D-036a).</summary>
public class TraitGoalTests
{
    private static SimulationEngine Opened(ulong seed = 42, int population = 100)
    {
        var e = TestSupport.NewEngine(seed, population);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        return e;
    }

    private static (WorldState W, Person P, Character Ch) Someone(int days = 8)
    {
        var e = Opened();
        e.AdvanceTo(SimTime.Days(days));
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0 && !x.IsOnline && x.LastPlayedDay >= 0);
        return (w, p, w.GetCharacter(p.CharacterId));
    }

    private static ActivityDefinition FirstOfType(ContentDatabase c, string type) =>
        c.Regions.Values.OrderBy(r => r.Id, StringComparer.Ordinal).SelectMany(r => r.AllActivities()).First(a => a.Type == type);

    [Fact]
    public void Inatividade_MaisDeUmaSemana_ReduzMecanica_EDuasSemanas_Conhecimento()
    {
        var (w, p, _) = Someone();
        p.QuirkList.Clear();   // a regra de base (Esquecido e Memoria firme mudam a queda, P4c)
        var traits = w.Behavior.Traits;
        int mech = p.Traits.MechanicalSkill, know = p.Traits.GameKnowledge;
        var d = w.Content.Dynamics;
        int mechLo = traits.Floor(p, "mechanicalSkill");
        int knowLo = traits.Floor(p, "gameKnowledge");

        traits.OnDaily(p, p.LastPlayedDay + 7);   // uma semana exata: nada muda
        Assert.Equal(mech, p.Traits.MechanicalSkill);
        traits.OnDaily(p, p.LastPlayedDay + 8);
        Assert.Equal(Math.Max(mechLo, mech - d.MechanicalDecayPerDay), p.Traits.MechanicalSkill);
        Assert.Equal(know, p.Traits.GameKnowledge);  // conhecimento so cai depois de duas semanas
        traits.OnDaily(p, p.LastPlayedDay + 15);
        Assert.Equal(Math.Max(knowLo, know - d.KnowledgeDecayPerDay), p.Traits.GameKnowledge);
    }

    [Fact]
    public void Pratica_EmCombate_AumentaMecanica_DentroDoLimite()
    {
        var (w, p, ch) = Someone();
        var hunt = FirstOfType(w.Content, ActivityTypes.Hunt);
        p.Practice.Clear();
        p.Traits.MechanicalSkill = p.BaseTraits.MechanicalSkill;
        int before = p.Traits.MechanicalSkill;
        int hi = w.Behavior.Traits.Ceiling(p, "mechanicalSkill");
        w.Behavior.Traits.OnActivity(p, ch, hunt, 10 * w.Content.Dynamics.MechanicalCombatMinutes, 0, 0, false);
        Assert.InRange(p.Traits.MechanicalSkill - before, 1, 17);   // ~10 pontos, conforme o talento
        w.Behavior.Traits.OnActivity(p, ch, hunt, 10_000_000, 0, 0, false);
        Assert.Equal(hi, p.Traits.MechanicalSkill);   // nunca passa do potencial
    }

    [Fact]
    public void Explorar_E_Descobrir_AumentamConhecimento()
    {
        var (w, p, ch) = Someone();
        p.Practice.Clear();
        p.Traits.GameKnowledge = p.BaseTraits.GameKnowledge;
        int know = p.Traits.GameKnowledge;
        w.Behavior.Traits.OnRegionDiscovered(p);
        Assert.Equal(know + w.Content.Dynamics.KnowledgePerDiscovery, p.Traits.GameKnowledge);
        int k2 = p.Traits.GameKnowledge;
        w.Behavior.Traits.OnActivity(p, ch, FirstOfType(w.Content, ActivityTypes.Explore), 6 * w.Content.Dynamics.KnowledgeExploreMinutes, 0, 0, false);
        Assert.True(p.Traits.GameKnowledge > k2);
    }

    [Fact]
    public void AbaterJogador_ReforcaGostoPorPvp_EPraticaMecanica_SemMudarAgressividade()
    {
        var (w, p, _) = Someone();
        var d = w.Content.Dynamics;
        int pvp = p.Traits.Pvp, aggr = p.Traits.Aggressiveness;
        p.Practice.Clear();
        w.Behavior.Traits.OnPvpKill(p);
        Assert.Equal(Math.Min(w.Behavior.Traits.Ceiling(p, "pvp"), pvp + d.PvpKillPreference), p.Traits.Pvp);
        Assert.Equal(aggr, p.Traits.Aggressiveness);
        // A execucao vem da pratica de combate (mecanica), nao do gosto.
        Assert.Contains(p.PracticeProgress, kv => kv.Key.StartsWith("+mechanicalSkill/", StringComparison.Ordinal) && kv.Value > 0);
    }

    [Fact]
    public void Talento_DefineOPotencial_EAVelocidadeDeAprender()
    {
        var (w, p, ch) = Someone();
        var hunt = FirstOfType(w.Content, ActivityTypes.Hunt);
        int Gain(int talent)
        {
            p.Traits.Talent = talent;
            p.Practice.Clear();
            p.Traits.MechanicalSkill = p.BaseTraits.MechanicalSkill;
            w.Behavior.Traits.OnActivity(p, ch, hunt, 40 * w.Content.Dynamics.MechanicalCombatMinutes, 0, 0, false);
            return p.Traits.MechanicalSkill - p.BaseTraits.MechanicalSkill;
        }
        int genius = Gain(950), noob = Gain(80);
        Assert.True(genius > noob, $"genio {genius} vs noob {noob}");
        p.Traits.Talent = 950;
        int capGenius = w.Behavior.Traits.Ceiling(p, "mechanicalSkill");
        p.Traits.Talent = 80;
        int capNoob = w.Behavior.Traits.Ceiling(p, "mechanicalSkill");
        Assert.True(capGenius > capNoob);
        // Gostos e personalidade nao dependem do talento.
        int pveNoob = w.Behavior.Traits.Ceiling(p, "pve");
        p.Traits.Talent = 950;
        Assert.Equal(pveNoob, w.Behavior.Traits.Ceiling(p, "pve"));
    }

    [Fact]
    public void Potencial_EAbsolutoPeloTalento_ComFolgaMinimaAcimaDoInicio()
    {
        var d = TestSupport.Preset.Dynamics;
        var low = new PersonTraits { MechanicalSkill = 150 };
        var mid = new PersonTraits { MechanicalSkill = 400 };
        // Mesmo talento, inicios diferentes: o mesmo potencial (escolha consciente, D-036a).
        Assert.Equal(TraitDynamics.CeilingOf(d, low, 800, "mechanicalSkill"), TraitDynamics.CeilingOf(d, mid, 800, "mechanicalSkill"));
        Assert.Equal(d.PotentialBase + 800 * d.PotentialPerTalent / 1000, TraitDynamics.CeilingOf(d, low, 800, "mechanicalSkill"));
        // Quem ja comeca acima do potencial ainda tem uma folga minima.
        var high = new PersonTraits { MechanicalSkill = 900 };
        Assert.Equal(900 + d.PotentialMinHeadroom, TraitDynamics.CeilingOf(d, high, 100, "mechanicalSkill"));
    }

    [Fact]
    public void MudancaVisivel_NaEscala_GeraEventoComCausa()
    {
        var (w, p, _) = Someone();
        w.Events.Drain();
        int delta = w.Behavior.Traits.Adjust(p, "pve", p.Traits.Pve > 500 ? -120 : 120, "teste");
        Assert.NotEqual(0, delta);
        var ev = w.Events.Drain().Single(x => x.Type == "trait_changed");
        Assert.StartsWith("pve:", ev.Detail);
        Assert.Equal("teste", ev.Causes[0].Factor);
    }

    [Fact]
    public void Mecanica_RendeMaisXp_EmCombate_ENaoNaColeta()
    {
        var hi = new PersonTraits { MechanicalSkill = 900, GameKnowledge = 500 };
        var lo = new PersonTraits { MechanicalSkill = 100, GameKnowledge = 500 };
        Assert.True(ActivityScoring.TraitXpPermille(hi, ActivityTypes.Hunt) > ActivityScoring.TraitXpPermille(lo, ActivityTypes.Hunt));
        Assert.Equal(ActivityScoring.TraitXpPermille(hi, ActivityTypes.Gather), ActivityScoring.TraitXpPermille(lo, ActivityTypes.Gather));
        // Conhecimento rende um pouco em tudo.
        var know = new PersonTraits { MechanicalSkill = 500, GameKnowledge = 1000 };
        Assert.True(ActivityScoring.TraitXpPermille(know, ActivityTypes.Gather) > 1000);
    }

    [Fact]
    public void Objetivos_SaoAdotados_ESeguemOsTracos()
    {
        var e = Opened(7, 300);
        var discover = new Dictionary<bool, (int Goals, int Discover)> { [true] = (0, 0), [false] = (0, 0) };
        for (int d = 0; d < 21; d++)
        {
            e.AdvanceTo(SimTime.Days(d + 1));
            foreach (var ev in e.World.Events.Drain().Where(x => x.Type == "goal_set"))
            {
                var ch = e.World.GetCharacter((int)ev.Entities[0].Id);
                var pr = e.World.GetPerson(ch.PersonId).Preferences;
                if (pr.Exploration is > 350 and < 650) continue;
                bool explorer = pr.Exploration >= 650;
                var (g, disc) = discover[explorer];
                // O motivo do objetivo (1a causa) e o que mais pesou na escolha (aqui, a preferencia).
                discover[explorer] = (g + 1, disc + (ev.Causes[0].Factor == "pref_exploration" ? 1 : 0));
            }
        }
        Assert.True(discover[true].Goals > 20 && discover[false].Goals > 20, $"amostra pequena: {discover[true].Goals}/{discover[false].Goals}");
        // Exploradores escolhem objetivos motivados pela exploracao com mais frequencia: tendencia, nao regra.
        Assert.True(discover[true].Discover * 1000 / discover[true].Goals > discover[false].Discover * 1000 / discover[false].Goals,
            $"exploradores {discover[true].Discover}/{discover[true].Goals} vs outros {discover[false].Discover}/{discover[false].Goals}");
    }

    [Fact]
    public void Objetivo_DaBonusSoAsOpcoesQueAjudam_ELimitado()
    {
        var (w, p, ch) = Someone();
        ch.GoalList.Clear();
        ch.GoalList.Add(new PersonalGoal(GoalTypes.PracticeActivity, ActivityTypes.Gather, 300, 0, 1000, "trade", 600, w.Clock.Tick, 0));
        var region = w.Content.Regions.Values.First(r => r.AllActivities().Any(a => a.Type == ActivityTypes.Gather));
        var gather = region.AllActivities().First(a => a.Type == ActivityTypes.Gather);
        var hunt = FirstOfType(w.Content, ActivityTypes.Hunt);
        var goals = w.Behavior.Goals;
        int bonus = goals.Bonus(ch, region, gather, true, 800);
        Assert.Equal(w.Content.GoalSettings.Weight, bonus);
        Assert.Equal(0, goals.Bonus(ch, region, hunt, true, 800));
        // Comprometimento menor, influencia menor.
        ch.GoalList.Clear();
        ch.GoalList.Add(new PersonalGoal(GoalTypes.PracticeActivity, ActivityTypes.Gather, 300, 0, 500, "trade", 600, w.Clock.Tick, 0));
        Assert.Equal(w.Content.GoalSettings.Weight / 2, goals.Bonus(ch, region, gather, true, 800));
    }

    [Fact]
    public void Objetivo_DeNivel_ConcluiEReforcaOMotivo()
    {
        var (w, p, ch) = Someone();
        ch.GoalList.Clear();
        var g = new PersonalGoal(GoalTypes.ReachLevel, "", ch.Level + 1, ch.Level, 800, "pve", p.Traits.Pve, w.Clock.Tick, 0);
        ch.GoalList.Add(g);
        int completed = ch.GoalsCompleted, pve = p.Traits.Pve;
        ch.Level++;
        w.Events.Drain();
        w.Behavior.Goals.OnLevelUp(p, ch);
        Assert.DoesNotContain(g, ch.Goals);
        Assert.Equal(completed + 1, ch.GoalsCompleted);
        Assert.Contains(w.Events.Drain(), x => x.Type == "goal_completed");
        Assert.Equal(Math.Min(w.Behavior.Traits.Ceiling(p, "pve"), pve + w.Content.GoalSettings.MotiveReinforce), p.Traits.Pve);
    }

    [Fact]
    public void Objetivo_SemProgresso_EAbandonado()
    {
        var (w, p, ch) = Someone();
        ch.GoalList.Clear();
        var g = new PersonalGoal(GoalTypes.PracticeActivity, ActivityTypes.Gather, 300, 0, 800, "trade", 600, w.Clock.Tick, ch.PlayedTicksAt(w.Clock.Tick));
        ch.GoalList.Add(g);
        ch.PlayedTicks += 20 * SimTime.TicksPerHour;   // 20 h de jogo sem avancar (mais que o maximo de paciencia)
        int abandoned = ch.GoalsAbandoned;
        w.Behavior.Goals.Review(p, ch, new DeterministicRng(123));
        Assert.DoesNotContain(g, ch.Goals);
        Assert.Equal(abandoned + 1, ch.GoalsAbandoned);
    }

    [Fact]
    public void AtributosEObjetivos_SaoDeterministicos_EEntramNoHash()
    {
        string Run()
        {
            var e = Opened(11, 80);
            e.AdvanceTo(SimTime.Days(14));
            Assert.True(e.World.Characters.Sum(c => c.GoalsSet) > 0);
            Assert.Contains(e.World.Persons, p => PersonTraits.DynamicKeys.Any(k => p.Traits.Get(k) != p.BaseTraits.Get(k)));
            return StateHasher.Compute(e.World);
        }
        Assert.Equal(Run(), Run());
    }

    [Fact]
    public void Repetir_NaoReduzOGosto_SoExperienciaBoaReforca()
    {
        var (w, p, ch) = Someone();
        var hunt = FirstOfType(w.Content, ActivityTypes.Hunt);
        p.Practice.Clear();
        p.Traits.Patience = 0;
        int pve = p.Traits.Pve;
        // Muita repeticao com experiencia ruim (encaixe baixo): o gosto nao cai.
        for (int i = 0; i < 50; i++) w.Behavior.Traits.OnActivity(p, ch, hunt, 120, 200, 0, false);
        Assert.Equal(pve, p.Traits.Pve);
        // Experiencias boas e persistentes reforcam o gosto.
        w.Behavior.Traits.OnActivity(p, ch, hunt, 20 * w.Content.Dynamics.AffinityEnjoyedMinutes, 900, 0, false);
        Assert.True(p.Traits.Pve > pve || pve == w.Behavior.Traits.Ceiling(p, "pve"));
    }

    [Fact]
    public void Coordenacao_NaoEvoluiNaTaverna()
    {
        var (w, p, ch) = Someone();
        p.Practice.Clear();
        int coord = p.Traits.Coordination;
        w.Behavior.Traits.OnActivity(p, ch, FirstOfType(w.Content, ActivityTypes.Socialize), 100_000, 900, 0, false);
        Assert.Equal(coord, p.Traits.Coordination);
    }

    [Fact]
    public void Familiaridade_VemDoTempoNaZona_EEMenorParaQuemGostaDeVariedade()
    {
        var (w, _, ch) = Someone();
        var d = w.Content.Dynamics;
        var homebody = new PersonTraits { Variety = 100, Loyalty = 500 };
        var wanderer = new PersonTraits { Variety = 900, Loyalty = 500 };
        ch.ZoneMinutes.Clear();
        Assert.Equal(0, ActivityScoring.FamiliarityPull(ch, "zona_x", homebody, d));
        ch.ZoneMinutes["zona_x"] = d.FamiliarityFullMinutes / 2;
        int half = ActivityScoring.FamiliarityPull(ch, "zona_x", homebody, d);
        ch.ZoneMinutes["zona_x"] = d.FamiliarityFullMinutes * 5;
        int full = ActivityScoring.FamiliarityPull(ch, "zona_x", homebody, d);
        Assert.True(full > half && half > 0);
        Assert.True(ActivityScoring.FamiliarityPull(ch, "zona_x", wanderer, d) < full);
        // A lealdade nao entra na familiaridade.
        Assert.Equal(full, ActivityScoring.FamiliarityPull(ch, "zona_x", new PersonTraits { Variety = 100, Loyalty = 1000 }, d));
    }

    [Fact]
    public void GostoPorDesafio_CombinaCompetitividadeConfiancaERisco()
    {
        var bold = new PersonTraits { Competitiveness = 500, MechanicalSkill = 800, RiskAversion = 200 };
        var timid = new PersonTraits { Competitiveness = 500, MechanicalSkill = 200, RiskAversion = 800 };
        Assert.True(ActivityScoring.ChallengeAppetite(bold) > ActivityScoring.ChallengeAppetite(timid));
        var competitive = new PersonTraits { Competitiveness = 900, MechanicalSkill = 500, RiskAversion = 500 };
        var calm = new PersonTraits { Competitiveness = 100, MechanicalSkill = 500, RiskAversion = 500 };
        Assert.True(ActivityScoring.ChallengeAppetite(competitive) > ActivityScoring.ChallengeAppetite(calm));
        // A agressividade continua existindo, mas e confronto: nao entra no gosto por desafio.
        Assert.Equal(ActivityScoring.ChallengeAppetite(new PersonTraits { Aggressiveness = 0, Competitiveness = 500 }),
            ActivityScoring.ChallengeAppetite(new PersonTraits { Aggressiveness = 1000, Competitiveness = 500 }));
    }

    [Fact]
    public void Conhecimento_MelhoraAEstimativaDaViagem_SemReduzirOCusto()
    {
        var d = TestSupport.Preset.Dynamics;
        Assert.Equal(d.TravelEstimateErrorAtZero, ActivityScoring.TravelErrorPermille(new PersonTraits { GameKnowledge = 0 }, d));
        Assert.Equal(0, ActivityScoring.TravelErrorPermille(new PersonTraits { GameKnowledge = 1000 }, d));
    }

    [Fact]
    public void QuantidadeDeMetas_NaoDependeDaPaciencia()
    {
        var (w, p, ch) = Someone();
        p.Traits.Patience = 50;   // muito impaciente
        ch.GoalList.Clear();
        int max = 0;
        for (int i = 0; i < 40; i++)
        {
            w.Behavior.Goals.Review(p, ch, new DeterministicRng((ulong)(1000 + i)));
            max = Math.Max(max, ch.Goals.Count);
        }
        Assert.Equal(w.Content.GoalSettings.MaxActive, max);
    }

    [Fact]
    public void ParametrosDosAtributos_VemDoConteudo_EInvalidosSaoErro()
    {
        var set = TestSupport.FreshPresetSet();
        Assert.Equal(150, set.Population.Dynamics.MechanicalCombatMinutes);
        Assert.Equal(2, set.Population.Goals.MaxActive);
        set.Population.Dynamics.MechanicalCombatMinutes = 0;
        set.Population.Goals.AdoptChance = 1500;
        var errors = ContentValidator.Validate(set).Where(i => i.Severity == IssueSeverity.Error).ToList();
        Assert.Contains(errors, e => e.Path == "dynamics.mechanicalCombatMinutes");
        Assert.Contains(errors, e => e.Path == "goals.adoptChance");
    }
}

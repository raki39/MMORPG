using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

/// <summary>Experiencia, memoria e humor (D-038).</summary>
public class MoodMemoryTests
{
    private static SimulationEngine Run(ulong seed, int population, int days)
    {
        var e = TestSupport.NewEngine(seed, population);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(days));
        return e;
    }

    private static (WorldState W, Person P, Character Ch) Someone(int days = 6)
    {
        var e = Run(42, 100, days);
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0 && !x.IsOnline);
        return (w, p, w.GetCharacter(p.CharacterId));
    }

    private static MoodSettingsDefinition M => TestSupport.Preset.Mood;

    private static (RegionDefinition R, ActivityDefinition A) Hunt()
    {
        var r = TestSupport.Preset.Regions.Values.OrderBy(x => x.Id, StringComparer.Ordinal)
            .First(x => x.AllActivities().Any(a => a.Type == ActivityTypes.Hunt));
        return (r, r.AllActivities().First(a => a.Type == ActivityTypes.Hunt));
    }

    private static int Part((string Key, int Value)[] parts, string key) => parts.Single(p => p.Key == key).Value;

    [Fact]
    public void Experiencia_TemComponentesExplicaveis()
    {
        var (r, a) = Hunt();
        var ch = new Character(1, 1, "x", "mago");
        // O gosto (diversao) vem da preferencia, nao do atributo de especialidade.
        var lover = new PersonTraits { Coordination = 500, RiskAversion = 500 };
        var loves = new PersonPreferences { Pve = 900 };
        var hates = new PersonPreferences { Pve = 100 };
        var none = new PersonPreferences();
        Assert.True(Part(ExperienceSystem.Components(lover, loves, ch, r, a, 1000, true, 0, 0, M), ExperienceSystem.Fun) > 0);
        Assert.True(Part(ExperienceSystem.Components(lover, hates, ch, r, a, 1000, true, 0, 0, M), ExperienceSystem.Fun) < 0);
        Assert.Equal(Part(ExperienceSystem.Components(new PersonTraits { Pve = 100 }, loves, ch, r, a, 1000, true, 0, 0, M), ExperienceSystem.Fun),
            Part(ExperienceSystem.Components(new PersonTraits { Pve = 900 }, loves, ch, r, a, 1000, true, 0, 0, M), ExperienceSystem.Fun));
        // Lotacao incomoda; coordenacao alivia.
        int crowdLo = Part(ExperienceSystem.Components(new PersonTraits { Coordination = 100 }, none, ch, r, a, 1000, true, 0, 200, M), ExperienceSystem.Crowding);
        int crowdHi = Part(ExperienceSystem.Components(new PersonTraits { Coordination = 900 }, none, ch, r, a, 1000, true, 0, 200, M), ExperienceSystem.Crowding);
        Assert.True(crowdLo < crowdHi && crowdHi < 0);
        // Risco: emociona quem tem pouca aversao, incomoda quem tem muita.
        Assert.True(Part(ExperienceSystem.Components(new PersonTraits { RiskAversion = 100 }, none, ch, r, a, 1000, true, 600, 0, M), ExperienceSystem.Risk) > 0);
        Assert.True(Part(ExperienceSystem.Components(new PersonTraits { RiskAversion = 900 }, none, ch, r, a, 1000, true, 600, 0, M), ExperienceSystem.Risk) < 0);
        // Encaixe ruim de nivel pesa contra.
        Assert.True(Part(ExperienceSystem.Components(lover, loves, ch, r, a, 200, true, 0, 0, M), ExperienceSystem.Fit) < 0);
    }

    [Fact]
    public void QuemSocializaOuNegocia_LigaMenosParaXp_EExploradoresPrecisamDeNovidade()
    {
        // Cuidado com o progresso: preferencias PvE e PvP e competitividade (nao a agressividade).
        var grinder = new PersonTraits { Competitiveness = 700 };
        var grinderPref = new PersonPreferences { Pve = 900, Pvp = 600 };
        var social = new PersonTraits { Competitiveness = 200 };
        var socialPref = new PersonPreferences { Pve = 300, Pvp = 100, Social = 900 };
        Assert.True(ExperienceSystem.ProgressCare(grinder, grinderPref, M) > ExperienceSystem.ProgressCare(social, socialPref, M) * 2);
        Assert.Equal(ExperienceSystem.ProgressCare(new PersonTraits { Aggressiveness = 0 }, grinderPref, M),
            ExperienceSystem.ProgressCare(new PersonTraits { Aggressiveness = 1000 }, grinderPref, M));
        var seeker = new PersonTraits { Variety = 900, Patience = 300 };
        var routine = new PersonTraits { Variety = 100, Patience = 900 };
        Assert.True(ExperienceSystem.NoveltyCare(seeker, new PersonPreferences { Exploration = 700 }, M)
            > ExperienceSystem.NoveltyCare(routine, new PersonPreferences { Exploration = 300 }, M) * 2);
        // A preferencia por variedade pesa mais que a preferencia por explorar (D-039a).
        int byVariety = ExperienceSystem.NoveltyCare(new PersonTraits { Variety = 900, Patience = 500 }, new PersonPreferences(), M);
        int byExploration = ExperienceSystem.NoveltyCare(new PersonTraits { Patience = 500 }, new PersonPreferences { Exploration = 900 }, M);
        Assert.True(byVariety > byExploration);
    }

    [Fact]
    public void Saturacao_TemEfeitoLimitado_EQuemGostaDeRepetirSentePouco()
    {
        var routine = new PersonTraits { Variety = 100, Patience = 900 };
        var seeker = new PersonTraits { Variety = 900, Patience = 300 };
        int worst = ExperienceSystem.NoveltyTerm(M.NoveltyMin, seeker, new PersonPreferences { Exploration = 700 }, M);
        Assert.True(worst >= -M.NoveltyClamp);
        Assert.True(ExperienceSystem.NoveltyTerm(M.NoveltyMin, routine, new PersonPreferences { Exploration = 300 }, M) > worst / 2);   // metodico sente bem menos
    }

    [Fact]
    public void NovidadeGastaDevagar_EAsSessoesCurtasAcumulam()
    {
        var (w, p, ch) = Someone();
        var exp = w.Behavior.Experience;
        p.Mood.Novelty = 500;
        p.Mood.NoveltyDecayCarry = 0;
        for (int i = 0; i < 30; i++)   // 30 sessoes de 20 min = 10 h
        {
            ch.SessionStartTick = w.Clock.Tick - SimTime.Minutes(20);
            ch.SessionLedger.Reset(ch.XpFromActivities + ch.XpFromQuests, ch.Level);
            exp.OnSessionEnd(p, ch, w.Clock.Tick);
        }
        Assert.Equal(500 - 10 * w.Content.Mood.NoveltyDecayPerHour, p.Mood.Novelty);
        // Horizonte de meses: um hardcore (25 h/semana) leva mais de 2 meses para ir de 1000 a 500.
        double weeks = 500.0 / w.Content.Mood.NoveltyDecayPerHour / 25;
        Assert.True(weeks > 8, $"{weeks:0.0} semanas");
    }

    [Fact]
    public void Memoria_TemLimite_SaiAMaisFraca_EOMesmoFatoSeReforca()
    {
        var (w, p, _) = Someone();
        var exp = w.Behavior.Experience;
        p.MemoryList.Clear();
        int max = w.Content.Memory.MaxMemories;
        for (int i = 0; i < max; i++) exp.Remember(p, "quest", "q" + i, 300, 300 + i);
        exp.Remember(p, "main_quest", "forte", 900, 900);
        Assert.Equal(max, p.Memories.Count);
        Assert.DoesNotContain(p.Memories, m => m.Subject == "q0");   // a mais fraca saiu
        exp.Remember(p, "main_quest", "forte", 900, 500);
        var forte = p.Memories.Single(m => m.Subject == "forte");
        Assert.Equal(2, forte.Count);
        Assert.Equal(900, forte.Intensity);   // reforco nao enfraquece
    }

    [Fact]
    public void FrustracoesSomemMaisRapidoQueConquistas()
    {
        var (w, p, ch) = Someone();
        var exp = w.Behavior.Experience;
        p.MemoryList.Clear();
        exp.Remember(p, "goal_completed", "bom", 500, 500);
        exp.Remember(p, "goal_abandoned", "ruim", -500, 500);
        for (int d = 0; d < 10; d++) exp.OnDaily(p, ch);
        var good = p.Memories.Single(m => m.Subject == "bom");
        var bad = p.Memories.Single(m => m.Subject == "ruim");
        Assert.True(good.Intensity > bad.Intensity);
        for (int d = 0; d < 400; d++) exp.OnDaily(p, ch);
        Assert.Empty(p.Memories);
    }

    [Fact]
    public void ImpressaoDoLugar_PesaNaDecisao()
    {
        var (w, p, ch) = Someone();
        var opts = ActivityScoring.Evaluate(ch, p.Traits, p.Preferences, w.Content, w.Behavior.Routes, w.OccupancyOf, w.Clock.Tick,
            new DeterministicRng(1), w.Behavior.Quests, w.Behavior.Goals, new Dictionary<string, int>());
        Assert.NotEmpty(opts);
        var key = opts[0].Key;
        var bad = new Dictionary<string, int> { [key] = -900 };
        var again = ActivityScoring.Evaluate(ch, p.Traits, p.Preferences, w.Content, w.Behavior.Routes, w.OccupancyOf, w.Clock.Tick,
            new DeterministicRng(1), w.Behavior.Quests, w.Behavior.Goals, bad);
        var f = again.Single(o => o.Key == key).Factors.Single(x => x.Key == "memory");
        Assert.Equal(-900 * w.Content.Memory.DecisionWeight / 1000, f.Contribution);
    }

    [Fact]
    public void SessaoBoaSobeASatisfacao_RuimSobeAFrustracao()
    {
        var (w, p, ch) = Someone();
        var exp = w.Behavior.Experience;
        ch.SessionStartTick = w.Clock.Tick - SimTime.Hours(2);
        ch.SessionLedger.Reset(ch.XpFromActivities + ch.XpFromQuests, w.Content.Progression.LevelCap);   // no teto: sem termo de progresso
        p.Mood.Satisfaction = 500;
        p.Mood.Frustration = 0;
        p.Mood.Novelty = 500;
        ch.SessionLedger.Points = 300;
        int good = exp.OnSessionEnd(p, ch, w.Clock.Tick);
        Assert.True(good > 700 && p.Mood.Satisfaction > 500);

        ch.SessionLedger.Reset(ch.XpFromActivities + ch.XpFromQuests, w.Content.Progression.LevelCap);
        ch.SessionLedger.Points = -300;
        ch.SessionLedger.Minutes = 60;
        ch.SessionLedger.Sums[ExperienceSystem.Boredom] = -200L * 60;
        int sat = p.Mood.Satisfaction;
        int bad = exp.OnSessionEnd(p, ch, w.Clock.Tick);
        Assert.True(bad < w.Content.Mood.BadSessionBelow);
        Assert.True(p.Mood.Satisfaction < sat);
        Assert.True(p.Mood.Frustration > 0);
    }

    [Fact]
    public void ExpectativaDeProgresso_TemPiso()
    {
        var (w, p, ch) = Someone();
        var exp = w.Behavior.Experience;
        p.Mood.ProgressExpectation = w.Content.Mood.ExpectationFloor;
        for (int i = 0; i < 30; i++)
        {
            ch.SessionStartTick = w.Clock.Tick - SimTime.Hours(2);
            ch.SessionLedger.Reset(ch.XpFromActivities + ch.XpFromQuests, 1);   // nenhuma XP ganha
            exp.OnSessionEnd(p, ch, w.Clock.Tick);
        }
        Assert.Equal(w.Content.Mood.ExpectationFloor, p.Mood.ProgressExpectation);
        Assert.Equal(0, p.Mood.LastProgressRatio);
    }

    [Fact]
    public void HumorEMemoria_EvoluemNaCampanha_SaoDeterministicos_EEntramNoHash()
    {
        string Hash()
        {
            var e = Run(13, 120, 10);
            var regs = e.World.Persons.Where(x => x.CharacterId > 0).ToList();
            Assert.Contains(regs, x => x.Memories.Count > 0);
            Assert.Contains(regs, x => x.PlaceImpressions.Count > 0);
            Assert.Contains(regs, x => x.Mood.SessionsRated > 0 && x.Mood.Satisfaction != e.World.Content.Mood.InitialSatisfaction);
            Assert.Contains(regs, x => x.Mood.Novelty < e.World.Content.Mood.InitialNovelty);
            return StateHasher.Compute(e.World);
        }
        Assert.Equal(Hash(), Hash());
    }

    [Fact]
    public void ParametrosDeHumorEMemoria_InvalidosSaoErro()
    {
        var set = TestSupport.FreshPresetSet();
        set.Population.Mood.SatisfactionAdapt = 1500;
        set.Population.Memory.MaxMemories = 0;
        set.Population.Mood.FunWeight = -1;
        var errors = ContentValidator.Validate(set).Where(i => i.Severity == IssueSeverity.Error).ToList();
        Assert.Contains(errors, x => x.Path == "mood.satisfactionAdapt");
        Assert.Contains(errors, x => x.Path == "memory.maxMemories");
        Assert.Contains(errors, x => x.Path == "mood.funWeight");
    }
}

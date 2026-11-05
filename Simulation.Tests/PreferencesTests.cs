using Simulation.Core.Behavior;
using Simulation.Core.Content;
using Simulation.Core.Population;

namespace Simulation.Tests;

/// <summary>
/// Separacao Perfil x Preferencias x atributos: o perfil e so dedicacao; o gosto vem das
/// preferencias (campo ao lado do perfil); a especialidade da so um reforco minimo e limitado; a
/// competitividade (conduta) assume o papel competitivo e a agressividade fica com o confronto.
/// Estatisticos com amostra fixa e tolerancia de 5 erros padrao (ver TraitDrawTests).
/// </summary>
public class PreferencesTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private static readonly string[] Types =
    {
        ActivityTypes.Hunt, ActivityTypes.Boss, ActivityTypes.Quest, ActivityTypes.Explore, ActivityTypes.Gather, ActivityTypes.Trade,
        ActivityTypes.Socialize, ActivityTypes.Train, ActivityTypes.Repair, ActivityTypes.Patrol, ActivityTypes.Ambush,
    };

    /// <summary>
    /// Preferencias, competitividade e os atributos da populacao nao dependem do perfil: com outros
    /// pesos de perfil (outras pessoas viram casual ou hardcore), cada id mantem tudo, menos a dedicacao.
    /// </summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void PreferenciasEAtributos_NaoDependemDoPerfil()
    {
        var set = TestSupport.FreshPresetSet();
        foreach (var p in set.Population.Profiles) p.Weight = p.Id == "hardcore" ? 60 : p.Weight;
        var other = ContentDatabase.Create(set);
        int profileChanged = 0;
        for (int id = 1; id <= 3000; id++)
        {
            var a = PopulationGenerator.Generate(id, C, 13);
            var b = PopulationGenerator.Generate(id, other, 13);
            if (a.ProfileId != b.ProfileId) profileChanged++;
            foreach (var k in PersonPreferences.Keys) Assert.Equal(a.Preferences.Get(k), b.Preferences.Get(k));
            foreach (var k in PersonTraits.DynamicKeys) Assert.Equal(a.Traits.Get(k), b.Traits.Get(k));
            Assert.Equal(a.Traits.Talent, b.Traits.Talent);
            Assert.Equal(a.Traits.PreferredStartHour, b.Traits.PreferredStartHour);
        }
        Assert.True(profileChanged > 300, $"so {profileChanged} mudaram de perfil");
    }

    [Fact]
    [Trait("Classe", "Regra do preset")]
    public void Preferencias_VariadasComUmaOuDuasPredominantes()
    {
        var people = Enumerable.Range(1, 6000).Select(id => PopulationGenerator.Generate(id, C, 3)).ToList();
        Assert.All(people, p => Assert.All(PersonPreferences.Keys, k => Assert.InRange(p.Preferences.Get(k), 0, 1000)));
        // Cada preferencia e a predominante de alguem; muita gente gosta de mais de uma coisa.
        foreach (var k in PersonPreferences.Keys)
            Assert.Contains(people, p => PersonPreferences.Keys.All(o => o == k || p.Preferences.Get(k) > p.Preferences.Get(o)));
        int Strong(Person p) => PersonPreferences.Keys.Count(k => p.Preferences.Get(k) >= 700);
        Assert.InRange(people.Count(p => Strong(p) >= 2) * 100.0 / people.Count, 15, 45);
        Assert.InRange(people.Count(p => Strong(p) == 0) * 100.0 / people.Count, 15, 50);
        // Independentes do perfil: medianas de PvE parecidas entre casual e hardcore.
        double Med(string profile) => people.Where(p => p.ProfileId == profile).Select(p => (double)p.Preferences.Pve).OrderBy(x => x).ElementAt(people.Count(p => p.ProfileId == profile) / 2);
        Assert.InRange(Med("casual") - Med("hardcore"), -60, 60);
        // Variacao individual: casual com preferencia PvP forte e hardcore que prefere crafting existem.
        Assert.Contains(people, p => p.ProfileId == "casual" && p.Preferences.Pvp >= 700);
        Assert.Contains(people, p => p.ProfileId == "hardcore" && PersonPreferences.Keys.All(o => o == "crafting" || p.Preferences.Crafting > p.Preferences.Get(o)));
    }

    /// <summary>O gosto pela atividade vem da preferencia; a especialidade nao entra na afinidade.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Afinidade_VemDaPreferencia_NaoDaEspecialidade()
    {
        var pr = new PersonPreferences { Pve = 700, Pvp = 300, Crafting = 900, Trade = 200, Gathering = 600, Exploration = 400, Social = 500 };
        var low = new PersonTraits { Pve = 0, Pvp = 0, Trade = 0, Exploration = 0, Socializing = 0, Loyalty = 500, Opportunism = 500, Aggressiveness = 500, GameKnowledge = 500 };
        var high = low.Clone();
        high.Pve = high.Pvp = high.Trade = high.Exploration = high.Socializing = 1000;
        foreach (var type in Types) Assert.Equal(ActivityScoring.Affinity(type, pr, low), ActivityScoring.Affinity(type, pr, high));
        Assert.Equal(700, ActivityScoring.Affinity(ActivityTypes.Hunt, pr, low));
        Assert.Equal(700, ActivityScoring.Affinity(ActivityTypes.Boss, pr, low));
        // Coleta tem preferencia propria: nao depende de comercio, crafting nem paciencia.
        Assert.Equal(600, ActivityScoring.Affinity(ActivityTypes.Gather, pr, low));
        var other = new PersonPreferences { Gathering = 600, Trade = 1000, Crafting = 0 };
        Assert.Equal(600, ActivityScoring.Affinity(ActivityTypes.Gather, other, new PersonTraits { Patience = 1000 }));
        Assert.Equal(200, ActivityScoring.Affinity(ActivityTypes.Trade, pr, low));
    }

    /// <summary>Crafting ainda nao tem atividade: nao muda nenhuma afinidade.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Crafting_SemEfeitoAteExistirFabricacao()
    {
        var t = new PersonTraits { Loyalty = 500, Opportunism = 500, Aggressiveness = 500, GameKnowledge = 500 };
        var a = new PersonPreferences { Pve = 500, Pvp = 500, Trade = 500, Gathering = 500, Exploration = 500, Social = 500, Crafting = 0 };
        var b = new PersonPreferences { Pve = 500, Pvp = 500, Trade = 500, Gathering = 500, Exploration = 500, Social = 500, Crafting = 1000 };
        foreach (var type in Types) Assert.Equal(ActivityScoring.Affinity(type, a, t), ActivityScoring.Affinity(type, b, t));
    }

    /// <summary>Reforco da especialidade: proporcional, com teto, so nos tipos com especialidade.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Especialidade_ReforcoMinimoELimitado()
    {
        var b = C.Behavior;
        Assert.Equal(0, ActivityScoring.SpecialtyBonus(ActivityTypes.Hunt, new PersonTraits { Pve = 0 }, b));
        Assert.Equal(400 * b.SpecialtyAffinityPermille / 1000, ActivityScoring.SpecialtyBonus(ActivityTypes.Hunt, new PersonTraits { Pve = 400 }, b));
        Assert.Equal(Math.Min(b.SpecialtyAffinityCap, 1000 * b.SpecialtyAffinityPermille / 1000), ActivityScoring.SpecialtyBonus(ActivityTypes.Hunt, new PersonTraits { Pve = 1000 }, b));
        Assert.True(ActivityScoring.SpecialtyBonus(ActivityTypes.Hunt, new PersonTraits { Pve = 1000 }, b) <= b.SpecialtyAffinityCap);
        // Coleta, missao, treino e reparo nao tem especialidade.
        var all = new PersonTraits { Pve = 1000, Pvp = 1000, Trade = 1000, Exploration = 1000, Socializing = 1000 };
        foreach (var type in new[] { ActivityTypes.Gather, ActivityTypes.Quest, ActivityTypes.Train, ActivityTypes.Repair })
            Assert.Equal(0, ActivityScoring.SpecialtyBonus(type, all, b));
        // O teto e pequeno perto do gosto: no maximo 20% da escala da afinidade (validador).
        Assert.InRange(b.SpecialtyAffinityCap, 0, 200);
    }

    /// <summary>Na decisao, a especialidade aparece como fator proprio, separado do gosto.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Especialidade_AparecesComoFatorSeparadoNaDecisao()
    {
        var e = TestSupport.NewEngine(21, 60);
        e.Submit(new Simulation.Core.Commands.SetServerOpenCommand(true), Simulation.Core.Time.SimTime.Hours(8));
        e.AdvanceTo(Simulation.Core.Time.SimTime.Days(2));
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0);
        var ch = w.GetCharacter(p.CharacterId);
        var opts = ActivityScoring.Evaluate(ch, p.Traits, p.Preferences, w.Content, w.Behavior.Routes, w.OccupancyOf, w.Clock.Tick,
            new Simulation.Core.Random.DeterministicRng(1), w.Behavior.Quests, w.Behavior.Goals, p.PlaceImpressions);
        foreach (var o in opts)
        {
            int expected = ActivityScoring.SpecialtyBonus(o.Activity.Type, p.Traits, w.Content.Behavior) * w.Content.Behavior.WeightAffinity / 1000;
            var spec = o.Factors.Where(f => f.Key == "specialty").Select(f => f.Contribution).DefaultIfEmpty(0).Single();
            Assert.Equal(expected, spec);
            var aff = o.Factors.Single(f => f.Key == "affinity").Contribution;
            Assert.Equal(ActivityScoring.Affinity(o.Activity.Type, p.Preferences, p.Traits) * w.Content.Behavior.WeightAffinity / 1000, aff);
        }
    }

    /// <summary>Competitividade faz o papel competitivo; a agressividade nao entra nele.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Competitividade_NoPapelCompetitivo_AgressividadeNao()
    {
        var m = C.Mood;
        Assert.True(ExperienceSystem.InitialExpectation(new PersonTraits { Competitiveness = 900 }, m) > ExperienceSystem.InitialExpectation(new PersonTraits { Competitiveness = 100 }, m));
        Assert.Equal(ExperienceSystem.InitialExpectation(new PersonTraits { Aggressiveness = 0 }, m), ExperienceSystem.InitialExpectation(new PersonTraits { Aggressiveness = 1000 }, m));
        // A agressividade continua com o confronto: pesa na emboscada.
        var pr = new PersonPreferences { Pvp = 500 };
        Assert.True(ActivityScoring.Affinity(ActivityTypes.Ambush, pr, new PersonTraits { Aggressiveness = 900 })
            > ActivityScoring.Affinity(ActivityTypes.Ambush, pr, new PersonTraits { Aggressiveness = 100 }));
        // Competitividade nao muda a afinidade de nenhuma atividade.
        foreach (var type in Types)
            Assert.Equal(ActivityScoring.Affinity(type, pr, new PersonTraits { Competitiveness = 0 }), ActivityScoring.Affinity(type, pr, new PersonTraits { Competitiveness = 1000 }));
    }

    /// <summary>Meta cumprida com motivo de preferencia reforca a especialidade do tema (a preferencia e estavel).</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void MetaCumprida_ReforcaAEspecialidade_NaoAPreferencia()
    {
        Assert.Equal("pve", GoalSystem.ReinforcedBy("pref_pve"));
        Assert.Equal("exploration", GoalSystem.ReinforcedBy("pref_exploration"));
        Assert.Equal("socializing", GoalSystem.ReinforcedBy("pref_social"));
        Assert.Null(GoalSystem.ReinforcedBy("pref_gathering"));
        Assert.Null(GoalSystem.ReinforcedBy("pref_crafting"));
        Assert.Equal("competitiveness", GoalSystem.ReinforcedBy("competitiveness"));
        Assert.Equal("patience", GoalSystem.ReinforcedBy("patience"));
        var p = PopulationGenerator.Generate(5, C, 1);
        Assert.Equal(p.Preferences.Exploration, p.MotiveValue("pref_exploration"));
        Assert.Equal(p.Traits.Exploration, p.MotiveValue("exploration"));
    }

    /// <summary>Ligacoes parciais: a correlacao aparece, longe de identidade, e cada distribuicao se mantem.</summary>
    [Fact]
    [Trait("Classe", "Tendência")]
    public void Ligacoes_CriamCoerenciaSemObrigar()
    {
        var people = Enumerable.Range(1, 6000).Select(id => PopulationGenerator.Generate(id, C, 8)).ToList();
        double Rho(Func<Person, int> x, Func<Person, int> y)
        {
            double[] Rank(Func<Person, int> f)
            {
                var v = people.Select(f).ToArray();
                var order = Enumerable.Range(0, v.Length).OrderBy(i => v[i]).ToArray();
                var r = new double[v.Length];
                for (int i = 0; i < order.Length; i++) r[order[i]] = i;
                return r;
            }
            var a = Rank(x); var b = Rank(y);
            double ma = a.Average(), mb = b.Average(), sab = 0, saa = 0, sbb = 0;
            for (int i = 0; i < a.Length; i++) { sab += (a[i] - ma) * (b[i] - mb); saa += (a[i] - ma) * (a[i] - ma); sbb += (b[i] - mb) * (b[i] - mb); }
            return sab / Math.Sqrt(saa * sbb);
        }
        // Social <- Sociabilidade (0,35): correlacao positiva e bem abaixo de 1.
        Assert.InRange(Rho(p => p.Preferences.Social, p => p.Traits.Sociability), 0.15, 0.6);
        // Especialidade <- preferencia (0,5).
        Assert.InRange(Rho(p => p.Preferences.Pve, p => p.Traits.Pve), 0.3, 0.75);
        // Sem ligacao: competitividade e importancia do jogo independentes das preferencias (5/raiz(6000) ~ 0,065).
        Assert.InRange(Rho(p => p.Traits.Competitiveness, p => p.Preferences.Pvp), -0.065, 0.065);
        Assert.InRange(Rho(p => p.Traits.GameImportance, p => p.Preferences.Pve), -0.065, 0.065);
        // Combinacoes "contra a ligacao" continuam existindo: terco de cima da sociabilidade com o
        // terco de baixo da preferencia social (independente seria 11,1%; ligada demais, quase 0%).
        var soc = people.Select(p => p.Traits.Sociability).OrderBy(x => x).ToArray();
        var pref = people.Select(p => p.Preferences.Social).OrderBy(x => x).ToArray();
        int socHigh = soc[soc.Length * 2 / 3], prefLow = pref[pref.Length / 3];
        double against = people.Count(p => p.Traits.Sociability > socHigh && p.Preferences.Social < prefLow) * 100.0 / people.Count;
        Assert.InRange(against, 3, 10.5);
    }
}

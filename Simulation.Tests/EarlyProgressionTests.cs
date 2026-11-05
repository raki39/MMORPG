using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

/// <summary>
/// Progressao inicial, metas de desbloqueio e descoberta orientada pelo conteudo (D-055).
/// - Progressao inicial: prioridade igual para todos ate o nivel-alvo do preset; nao muda gosto,
///   nao e obrigacao, conta uma vez por opcao e nao soma com metas de progressao.
/// - Desbloqueio: nivel que libera conteudo conhecido de um tema preferido; a preferencia nao muda.
/// - Descoberta: so com informacao valida (pista de missao aceita) sobre a zona desconhecida.
/// </summary>
public class EarlyProgressionTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private static EarlyProgressionDefinition E => C.EarlyProgression;

    private static (WorldState W, Person P, Character Ch) Someone(ulong seed = 31, int days = 2)
    {
        var e = WorldFactory.Create(C, new WorldConfig { Seed = seed, PopulationSize = 80 });
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(days));
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0 && !x.IsOnline);
        var ch = w.GetCharacter(p.CharacterId);
        ch.SessionEndTick = w.Clock.Tick + SimTime.Hours(4);
        return (w, p, ch);
    }

    private static List<ScoredOption> Evaluate(WorldState w, Person p, Character ch, PersonPreferences? pr = null) =>
        ActivityScoring.Evaluate(ch, p.Traits, pr ?? p.Preferences, w.Content, w.Behavior.Routes, w.OccupancyOf, w.Clock.Tick,
            new DeterministicRng(5), w.Behavior.Quests, w.Behavior.Goals, p.PlaceImpressions);

    private static int Factor(ScoredOption o, string key) => o.Factors.Where(f => f.Key == key).Select(f => f.Contribution).DefaultIfEmpty(0).Sum();

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Reducao_PlenaAteOInicio_LinearAteZeroNoAlvo()
    {
        var e = new EarlyProgressionDefinition { Enabled = true, TargetLevel = 10, FullUntilLevel = 5, Weight = 150, MinFitPermille = 600 };
        Assert.Equal(1000, ActivityScoring.EarlyDecay(e, 1));
        Assert.Equal(1000, ActivityScoring.EarlyDecay(e, 5));
        Assert.Equal(800, ActivityScoring.EarlyDecay(e, 6));
        Assert.Equal(200, ActivityScoring.EarlyDecay(e, 9));
        Assert.Equal(0, ActivityScoring.EarlyDecay(e, 10));
        Assert.Equal(0, ActivityScoring.EarlyDecay(e, 25));
        // O nivel-alvo e configuracao: com alvo 6 e pleno ate 2, a curva acompanha.
        var short6 = new EarlyProgressionDefinition { Enabled = true, TargetLevel = 6, FullUntilLevel = 2, Weight = 150 };
        Assert.Equal(500, ActivityScoring.EarlyDecay(short6, 4));
        Assert.Equal(0, ActivityScoring.EarlyDecay(short6, 6));
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Valor_SoParaProgressaoValida_QueCabeNaSessao()
    {
        var e = new EarlyProgressionDefinition { Enabled = true, TargetLevel = 10, FullUntilLevel = 5, Weight = 150, MinFitPermille = 600 };
        Assert.Equal(150, ActivityScoring.EarlyProgressionValue(3, e, yieldsXp: false, fit: 0, quest: true, fitsSession: true));
        Assert.Equal(150 * 800 / 1000, ActivityScoring.EarlyProgressionValue(3, e, true, 800, false, true));
        Assert.Equal(0, ActivityScoring.EarlyProgressionValue(3, e, true, 500, false, true));     // rendimento ruim nao e progressao adequada
        Assert.Equal(0, ActivityScoring.EarlyProgressionValue(3, e, false, 0, false, true));      // comercio, taverna: nada
        Assert.Equal(0, ActivityScoring.EarlyProgressionValue(3, e, true, 1000, true, false));    // nao cabe na sessao
        Assert.Equal(0, ActivityScoring.EarlyProgressionValue(10, e, true, 1000, true, true));    // alvo atingido
        Assert.Equal(150 * 600 / 1000, ActivityScoring.EarlyProgressionValue(7, e, false, 0, true, true));
        e.Enabled = false;
        Assert.Equal(0, ActivityScoring.EarlyProgressionValue(3, e, true, 1000, true, true));
        // Missao e XP na mesma opcao contam uma vez (o maior).
        e.Enabled = true;
        Assert.Equal(150, ActivityScoring.EarlyProgressionValue(3, e, true, 900, true, true));
    }

    /// <summary>O incentivo e o mesmo para qualquer perfil e preferencia (nao e gosto).</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Incentivo_IndependeDasPreferencias()
    {
        var (w, p, ch) = Someone();
        Assert.True(ch.Level < E.TargetLevel);
        var a = Evaluate(w, p, ch, new PersonPreferences { Pve = 1000, Social = 0 });
        var b = Evaluate(w, p, ch, new PersonPreferences { Pve = 0, Social = 1000, Trade = 1000 });
        foreach (var o in a)
            Assert.Equal(Factor(o, "early_progression"), Factor(b.Single(x => x.Key == o.Key), "early_progression"));
        Assert.Contains(a, o => Factor(o, "early_progression") > 0);
        // Comercio e taverna nao rendem progressao: sem incentivo.
        Assert.All(a.Where(o => o.Activity.Type is ActivityTypes.Trade or ActivityTypes.Socialize), o => Assert.Equal(0, Factor(o, "early_progression")));
    }

    /// <summary>Com meta de progressao, o mesmo avanco nao conta duas vezes: vale o maior.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Incentivo_NaoSomaComMetaDeProgressao()
    {
        var (w, p, ch) = Someone();
        ch.GoalList.Clear();
        var before = Evaluate(w, p, ch);
        ch.GoalList.Add(new PersonalGoal(GoalTypes.ReachLevel, "", ch.Level + 2, ch.Level, 500, "pref_pve", 500, w.Clock.Tick, 0));
        var after = Evaluate(w, p, ch);
        foreach (var o in after)
        {
            int early0 = Factor(before.Single(x => x.Key == o.Key), "early_progression");
            int goal = Factor(o, "goal"), early = Factor(o, "early_progression");
            Assert.Equal(Math.Max(early0, goal), goal + early);
        }
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Incentivo_AcabaNoNivelAlvo()
    {
        var (w, p, ch) = Someone();
        ch.Level = E.TargetLevel;
        Assert.All(Evaluate(w, p, ch), o => Assert.Equal(0, Factor(o, "early_progression")));
    }

    /// <summary>Desbloqueio: varias preferencias relevantes, so conteudo conhecido, crafting sem atividade.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Desbloqueio_PorPreferencia_ConteudoConhecido()
    {
        var (w, p, ch) = Someone();
        var s = w.Content.GoalSettings;
        ch.Level = 2;
        ch.Known.Clear();
        foreach (var z in new[] { "vila_aurora", "floresta_sussurrante", "colinas_do_vale", "mina_velha" }) ch.Known.Add(z);
        p.Preferences = new PersonPreferences { Gathering = 800, Pve = 500, Crafting = 1000, Social = 1000 };
        var opts = GoalSystem.UnlockOptions(p, ch, w.Content, s);
        // Coleta: a coleta de minerio da Mina Velha (nivel 6) e o proximo degrau conhecido.
        Assert.Contains(opts, o => o.Motive == "pref_gathering" && o.Target == "mina_velha/coletar_minerio" && o.Level == 6);
        // PvE tambem e relevante (500): ganha a sua propria opcao.
        Assert.Contains(opts, o => o.Motive == "pref_pve");
        // Crafting nao tem atividade; taverna nao tem degrau de nivel.
        Assert.DoesNotContain(opts, o => o.Motive is "pref_crafting" or "pref_social");
        // Peso cresce com a preferencia.
        Assert.True(opts.Single(o => o.Motive == "pref_gathering").Weight > opts.Single(o => o.Motive == "pref_pve").Weight);
        // Zona desconhecida nao entra.
        ch.Known.Remove("mina_velha");
        Assert.DoesNotContain(GoalSystem.UnlockOptions(p, ch, w.Content, s), o => o.Target.StartsWith("mina_velha/", StringComparison.Ordinal));
        // Preferencia baixa (abaixo do minimo) nao gera meta.
        p.Preferences = new PersonPreferences { Gathering = s.UnlockMinPreference - 1 };
        Assert.Empty(GoalSystem.UnlockOptions(p, ch, w.Content, s));
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Desbloqueio_TerminaAoCumprir_ESemMudarPreferencia()
    {
        var (w, p, ch) = Someone();
        int gather = p.Preferences.Gathering;
        ch.GoalList.Clear();
        var g = new PersonalGoal(GoalTypes.UnlockLevel, "mina_velha/coletar_minerio", ch.Level + 1, ch.Level, 800, "pref_gathering", gather, w.Clock.Tick, 0);
        ch.GoalList.Add(g);
        ch.Level++;
        w.Behavior.Goals.OnLevelUp(p, ch);
        Assert.DoesNotContain(g, ch.GoalList);
        Assert.Equal(gather, p.Preferences.Gathering);
    }

    /// <summary>
    /// Ciclo das metas de nivel: requisito cumprido nao gera meta; XP ganho conta como progresso;
    /// meta abandonada nao volta com o mesmo alvo antes de subir de nivel.
    /// </summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void MetaDeNivel_SemRecriacaoNemCicloDeAbandono()
    {
        var (w, p, ch) = Someone();
        var s = w.Content.GoalSettings;
        var goals = w.Behavior.Goals;
        ch.Known.Clear();
        foreach (var z in new[] { "vila_aurora", "floresta_sussurrante", "colinas_do_vale", "mina_velha" }) ch.Known.Add(z);
        p.Preferences = new PersonPreferences { Gathering = 800 };
        p.Traits.Patience = 1000;   // sem desistencia por impaciencia (sorteio)
        const string target = "mina_velha/coletar_minerio";

        // Requisito ja cumprido (nivel 6 ou mais): a meta nao e criada.
        ch.Level = 6;
        Assert.DoesNotContain(GoalSystem.UnlockOptions(p, ch, w.Content, s), o => o.Target == target);
        ch.Level = 3;
        Assert.Contains(GoalSystem.UnlockOptions(p, ch, w.Content, s), o => o.Target == target);

        // XP ganho conta como progresso.
        ch.GoalList.Clear();
        var g = new PersonalGoal(GoalTypes.UnlockLevel, target, 6, 3, 800, "pref_gathering", 800, w.Clock.Tick, 0);
        ch.GoalList.Add(g);
        g.LastProgressPlayedTicks = -1;
        goals.OnExperience(ch);
        Assert.Equal(ch.PlayedTicksAt(w.Clock.Tick), g.LastProgressPlayedTicks);

        // Sem progresso por muito tempo: abandonada e bloqueada com o mesmo alvo.
        g.LastProgressPlayedTicks = long.MinValue / 4;
        goals.Review(p, ch, new DeterministicRng(3));
        Assert.DoesNotContain(g, ch.GoalList);
        Assert.True(GoalSystem.IsBlocked(ch, GoalTypes.UnlockLevel, target, 6));
        Assert.DoesNotContain(GoalSystem.UnlockOptions(p, ch, w.Content, s), o => o.Target == target);
        Assert.DoesNotContain(ch.GoalList, x => x.Type == GoalTypes.UnlockLevel && x.Target == target);

        // Subir de nivel e mudanca relevante: o alvo pode voltar.
        ch.Level = 4;
        goals.OnLevelUp(p, ch);
        Assert.False(GoalSystem.IsBlocked(ch, GoalTypes.UnlockLevel, target, 6));
        Assert.Contains(GoalSystem.UnlockOptions(p, ch, w.Content, s), o => o.Target == target);
    }

    /// <summary>Descoberta: so com pista valida, zona desconhecida da fronteira e tema preferido.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Descoberta_SoComInformacaoValida()
    {
        var (w, p, ch) = Someone();
        var s = w.Content.GoalSettings;
        ch.Known.Clear();
        foreach (var z in new[] { "vila_aurora", "floresta_sussurrante", "colinas_do_vale" }) ch.Known.Add(z);
        ch.Hints.Clear();
        p.Preferences = new PersonPreferences { Gathering = 800 };
        // Sem pista, nada: a pessoa nao sabe o que ha na Mina Velha.
        Assert.Empty(GoalSystem.ContentDiscoveryOptions(p, ch, w.Content, s, 30));
        ch.Hints.Add("mina_velha/coletar_minerio");
        var opts = GoalSystem.ContentDiscoveryOptions(p, ch, w.Content, s, 30);
        Assert.Contains(opts, o => o.Zone == "mina_velha" && o.Motive == "pref_gathering");
        // Pista de um tema que ela nao prefere nao gera meta.
        p.Preferences = new PersonPreferences { Pve = 800 };
        Assert.Empty(GoalSystem.ContentDiscoveryOptions(p, ch, w.Content, s, 30));
        // Zona fora da fronteira (nao vizinha das conhecidas) nao entra.
        p.Preferences = new PersonPreferences { Gathering = 800 };
        ch.Hints.Clear();
        ch.Hints.Add("ruinas_sombrias/coletar_cristais");
        Assert.Empty(GoalSystem.ContentDiscoveryOptions(p, ch, w.Content, s, 30));
        // Zona ja conhecida tambem nao (nao ha o que descobrir).
        ch.Hints.Clear();
        ch.Hints.Add("colinas_do_vale/coletar_ervas");
        Assert.Empty(GoalSystem.ContentDiscoveryOptions(p, ch, w.Content, s, 30));
    }

    /// <summary>As pistas vem so das etapas de missoes aceitas (informacao valida).</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Pistas_VemDasMissoesAceitas()
    {
        var e = WorldFactory.Create(C, new WorldConfig { Seed = 12, PopulationSize = 120 });
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(10));
        int withHints = 0;
        foreach (var ch in e.World.Characters)
        {
            var accepted = ch.ActiveQuests.Select(q => q.QuestId).Concat(ch.QuestHistory.Keys).ToHashSet(StringComparer.Ordinal);
            var valid = accepted.SelectMany(id => C.Quests[id].Steps).Select(s => s.RegionId + "/" + s.ActivityId).ToHashSet(StringComparer.Ordinal);
            Assert.All(ch.ContentHints, h => Assert.Contains(h, valid));
            if (ch.ContentHints.Count > 0) withHints++;
        }
        Assert.True(withHints > 0, "nenhuma pista registrada em 10 dias");
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Validador_ProgressaoInicialEMetas()
    {
        static bool HasError(Action<ContentSet> change, string path)
        {
            var set = TestSupport.FreshPresetSet();
            change(set);
            return ContentValidator.Validate(set).Any(i => i.Severity == IssueSeverity.Error && i.Path.Contains(path));
        }
        Assert.False(HasError(_ => { }, "earlyProgression"));
        Assert.True(HasError(s => s.Population.EarlyProgression.TargetLevel = 1, "earlyProgression.targetLevel"));
        Assert.True(HasError(s => s.Population.EarlyProgression.TargetLevel = 500, "earlyProgression.targetLevel"));
        Assert.True(HasError(s => s.Population.EarlyProgression.Weight = 400, "earlyProgression.weight"));
        Assert.True(HasError(s => s.Population.EarlyProgression.FullUntilLevel = 10, "earlyProgression.fullUntilLevel"));
        Assert.True(HasError(s => s.Population.Goals.UnlockMinPreference = 1200, "goals.unlockMinPreference"));
        Assert.True(HasError(s => s.Population.Goals.UnlockMaxLevelsAhead = 0, "goals.unlockMaxLevelsAhead"));
    }
}

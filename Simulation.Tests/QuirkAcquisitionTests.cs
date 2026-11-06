using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Time;

namespace Simulation.Tests;

/// <summary>
/// Aquisicao e perda de traits (P4d): relogio em dias jogados, evidencia especifica, oportunidades
/// reais, sem autorreforco, limites, exclusoes e historico preservado.
/// </summary>
public class QuirkAcquisitionTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private static QuirkAcquisitionDefinition A => C.QuirkCatalog.Acquisition;

    private static Person Fresh(string personality = "equilibrado", params string[] quirks)
    {
        var p = PopulationGenerator.Generate(5, C, 3);
        p.QuirkList.Clear();
        foreach (var q in quirks) p.QuirkList.Add(new QuirkState(q, QuirkAcquisitions.Innate, 0));
        p.QuirkEvidenceMap.Clear();
        p.QuirkHistoryList.Clear();
        p.PersonalityId = personality;
        return p;
    }

    private static void Supports(Person p, string id, int n) { for (int i = 0; i < n; i++) QuirkSystem.Observe(C, p, id, true, false); }
    private static void Contradicts(Person p, string id, int n) { for (int i = 0; i < n; i++) QuirkSystem.Observe(C, p, id, false, true); }

    /// <summary>Sem jogar, nada anda: a evidencia nao enfraquece, a confirmacao nao avanca, nenhum intervalo passa.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void SemJogar_NadaMuda()
    {
        var p = Fresh();
        Supports(p, QuirkIds.Persistente, 4);
        for (long d = 1; d <= 60; d++) Assert.Null(QuirkSystem.OnDaily(C, p, d, false));
        var e = p.QuirkEvidenceByTrait[QuirkIds.Persistente];
        Assert.Equal(4000, e.For);
        Assert.Equal(0, e.Streak);
        Assert.Equal(0, p.PlayDayCount);
        Assert.DoesNotContain(p.Quirks, q => q.Id == QuirkIds.Persistente);
    }

    /// <summary>Adquire depois de 7 dias jogados com a condicao valendo; dias sem jogar so pausam a contagem.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Aquisicao_ConfirmadaEmDiasJogados_ComHistorico()
    {
        var p = Fresh();
        Supports(p, QuirkIds.Persistente, 5);
        QuirkChange? change = null;
        long day = 0;
        for (int played = 0; played < A.ConfirmPlayDays; played++)
        {
            Assert.Null(change);
            Assert.Null(QuirkSystem.OnDaily(C, p, ++day, false));   // dia sem jogar no meio: so pausa
            change = QuirkSystem.OnDaily(C, p, ++day, true);
        }
        Assert.NotNull(change);
        Assert.Equal("acquired", change!.Change);
        Assert.Equal("goals_after_setbacks", change.Reason);
        Assert.Equal(A.ConfirmPlayDays, change.PlayDay);
        var state = Assert.Single(p.Quirks, q => q.Id == QuirkIds.Persistente);
        Assert.Equal(QuirkAcquisitions.Acquired, state.Source);
        Assert.Single(p.QuirkHistory);
    }

    /// <summary>Evidencia fraca ou misturada nao adquire (taxa minima); a evidencia enfraquece por dia jogado.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Aquisicao_ExigeEvidenciaETaxa_EEnfraquece()
    {
        var mixed = Fresh();
        Supports(mixed, QuirkIds.Persistente, 4);
        Contradicts(mixed, QuirkIds.Persistente, 4);
        for (long d = 1; d <= 20; d++) Assert.Null(QuirkSystem.OnDaily(C, mixed, d, true));
        var p = Fresh();
        Supports(p, QuirkIds.Persistente, 4);
        for (long d = 1; d <= A.HalfLifePlayDays; d++) QuirkSystem.OnDaily(C, p, d, true);
        Assert.InRange(p.QuirkEvidenceByTrait[QuirkIds.Persistente].For, 1990, 2010);   // meia-vida em dias jogados
    }

    /// <summary>Exclusoes valem: Persistente nao entra em quem e Leal (personalidade) nem em quem tem Impulsivo.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Aquisicao_RespeitaExclusoesELimites()
    {
        foreach (var p in new[] { Fresh("leal"), Fresh("equilibrado", QuirkIds.Impulsivo) })
        {
            Supports(p, QuirkIds.Persistente, 6);
            for (long d = 1; d <= 30; d++) Assert.Null(QuirkSystem.OnDaily(C, p, d, true));
            Assert.True(p.QuirkStats.GetValueOrDefault("persistente_bloqueada_por_exclusao") > 0);
        }
        // Total maximo.
        var full = Fresh("equilibrado", "observador", "estudioso", "saudosista", "resiliente", "indeciso", "esquecido", "procrastinador", "otimizador");
        Supports(full, QuirkIds.Persistente, 6);
        for (long d = 1; d <= 30; d++) Assert.Null(QuirkSystem.OnDaily(C, full, d, true));
        Assert.True(full.QuirkStats.GetValueOrDefault("persistente_bloqueada_pelo_total") > 0);
        // Uma aquisicao a cada 30 dias jogados: Rancoroso espera depois do Persistente.
        var p2 = Fresh();
        Supports(p2, QuirkIds.Persistente, 6);
        Supports(p2, QuirkIds.Rancoroso, 6);
        var changes = new List<QuirkChange>();
        for (long d = 1; d <= 60; d++)
        {
            if (d % 10 == 0) { Supports(p2, QuirkIds.Persistente, 1); Supports(p2, QuirkIds.Rancoroso, 1); }
            if (QuirkSystem.OnDaily(C, p2, d, true) is { } c) changes.Add(c);
        }
        Assert.Equal(2, changes.Count);
        Assert.True(changes[1].PlayDay - changes[0].PlayDay >= A.PlayDaysBetweenAcquisitions);
    }

    /// <summary>Sem oportunidades reais nada se perde; a perda exige evidencia contraria; a inicial, mais forte e mais longa.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Perda_SoComOportunidadesReais_EInicialMaisDificil()
    {
        var idle = Fresh("equilibrado", QuirkIds.Persistente);
        for (long d = 1; d <= 200; d++) Assert.Null(QuirkSystem.OnDaily(C, idle, d, true));   // joga, mas sem oportunidade
        Assert.Contains(idle.Quirks, q => q.Id == QuirkIds.Persistente);

        // Trait inicial: 4 desistencias (abaixo do minimo de oportunidades da inicial) nao bastam.
        var initial = Fresh("equilibrado", QuirkIds.Persistente);
        Contradicts(initial, QuirkIds.Persistente, 4);
        for (long d = 1; d <= 10; d++) Assert.Null(QuirkSystem.OnDaily(C, initial, d, true));
        // Evidencia contraria forte e sustentada: perde depois de 21 dias jogados.
        var strong = Fresh("equilibrado", QuirkIds.Persistente);
        QuirkChange? lost = null;
        int day = 0;
        while (lost == null && day < 60)
        {
            if (day % 3 == 0) Contradicts(strong, QuirkIds.Persistente, 2);
            lost = QuirkSystem.OnDaily(C, strong, ++day, true);
        }
        Assert.NotNull(lost);
        Assert.Equal("lost", lost!.Change);
        Assert.Equal(QuirkAcquisitions.Innate, lost.PreviousSource);
        Assert.True(lost.PlayDay >= A.InitialConfirmPlayDays);
        Assert.DoesNotContain(strong.Quirks, q => q.Id == QuirkIds.Persistente);
    }

    /// <summary>Perder e readquirir preserva o historico; o intervalo impede voltar logo depois.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void PerderEReadquirir_PreservaHistorico()
    {
        var p = Fresh("equilibrado", QuirkIds.Rancoroso);
        int day = 0;
        while (p.Quirks.Any(q => q.Id == QuirkIds.Rancoroso) && day < 80) { if (day % 3 == 0) Contradicts(p, QuirkIds.Rancoroso, 2); QuirkSystem.OnDaily(C, p, ++day, true); }
        int lostAt = p.PlayDayCount;
        Assert.DoesNotContain(p.Quirks, q => q.Id == QuirkIds.Rancoroso);
        // Evidencia nova a favor: so volta depois do intervalo.
        p.QuirkEvidenceMap[QuirkIds.Rancoroso] = new QuirkEvidence { LastChangePlayDay = lostAt };
        while (!p.Quirks.Any(q => q.Id == QuirkIds.Rancoroso) && day < 300) { if (day % 3 == 0) Supports(p, QuirkIds.Rancoroso, 2); QuirkSystem.OnDaily(C, p, ++day, true); }
        var state = Assert.Single(p.Quirks, q => q.Id == QuirkIds.Rancoroso);
        Assert.Equal(QuirkAcquisitions.Acquired, state.Source);
        Assert.Equal(new[] { "lost", "acquired" }, p.QuirkHistory.Select(h => h.Change));
        Assert.Equal(QuirkAcquisitions.Innate, p.QuirkHistory[0].PreviousSource);   // a origem inicial fica registrada
        Assert.True(p.QuirkHistory[1].PlayDay - p.QuirkHistory[0].PlayDay >= A.CooldownPlayDays);
    }

    /// <summary>
    /// Evidencia especifica e sem autorreforco: concluir meta sem reves nao conta; com reves conta; se a
    /// meta so continuou pelo efeito do Persistente, e oportunidade sem evidencia. Desistir depois de reves conta contra.
    /// </summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Persistente_EvidenciaEspecifica_SemAutorreforco()
    {
        var e = TestSupport.NewEngine(9, 60);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(2));
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0);
        var ch = w.GetCharacter(p.CharacterId);
        p.QuirkEvidenceMap.Clear();
        var hunt = w.Content.Regions.Values.SelectMany(r => r.AllActivities()).First(a => a.Type == ActivityTypes.Hunt);
        PersonalGoal Goal(int setbacks, bool byEffect)
        {
            ch.GoalList.Clear();
            var g = new PersonalGoal(GoalTypes.PracticeActivity, ActivityTypes.Hunt, 10, 0, 800, "pref_pve", 500, w.Clock.Tick, ch.PlayedTicksAt(w.Clock.Tick));
            g.SetbacksDuring = setbacks; g.SurvivedByEffect = byEffect;
            ch.GoalList.Add(g);
            return g;
        }
        Goal(0, false); w.Behavior.Goals.OnActivity(p, ch, hunt, 30);                     // sem reves
        Assert.False(p.QuirkEvidenceByTrait.ContainsKey(QuirkIds.Persistente));
        Goal(1, false); w.Behavior.Goals.OnActivity(p, ch, hunt, 30);                     // concluiu depois de reves
        var ev = p.QuirkEvidenceByTrait[QuirkIds.Persistente];
        Assert.Equal((1, 1, 0), (ev.Opportunities, ev.ForCount, ev.EffectOnlyCount));
        Goal(1, true); w.Behavior.Goals.OnActivity(p, ch, hunt, 30);                      // so pelo efeito
        Assert.Equal((2, 1, 1), (ev.Opportunities, ev.ForCount, ev.EffectOnlyCount));
        // O reves marca as metas ativas.
        var g4 = Goal(0, false);
        w.Behavior.Experience.OnSetback(p, ch, "teste", "", 1, 1, 0);
        Assert.Equal(1, g4.SetbacksDuring);
    }

    /// <summary>Rancoroso: so conta a volta a um lugar com experiencia ruim marcante ainda lembrada.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Rancoroso_SoNoLugarMarcado()
    {
        var e = TestSupport.NewEngine(9, 60);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(2));
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0);
        var ch = w.GetCharacter(p.CharacterId);
        p.QuirkEvidenceMap.Clear();
        p.MemoryList.Clear();
        var region = w.Content.Regions.Values.First(r => r.AllActivities().Any());
        var a = region.AllActivities().First();
        w.Behavior.Experience.OnActivity(p, ch, region, a, 30, 600, true, 0, false);
        Assert.False(p.QuirkEvidenceByTrait.ContainsKey(QuirkIds.Rancoroso));   // lugar sem marca: nao e oportunidade
        w.Behavior.Experience.Remember(p, "awful_activity", region.Id + "/" + a.Id, -800, 800);
        w.Behavior.Experience.OnActivity(p, ch, region, a, 30, 600, true, 0, false);
        Assert.Equal(1, p.QuirkEvidenceByTrait[QuirkIds.Rancoroso].Opportunities);
    }

    /// <summary>Avaliadores de sistemas futuros ficam desligados: numa campanha, so Persistente e Rancoroso mudam.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void SoAvaliadoresAtuais_ECampanhaDeterministica()
    {
        Assert.Equal(new[] { QuirkIds.Persistente, QuirkIds.Rancoroso }, QuirkSystem.Evaluators);
        var e = TestSupport.NewEngine(17, 120);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(20));
        foreach (var p in e.World.Persons)
        {
            Assert.All(p.QuirkHistory, h => Assert.Contains(h.Id, QuirkSystem.Evaluators));
            Assert.All(p.QuirkEvidenceByTrait.Keys, k => Assert.Contains(k, QuirkSystem.Evaluators));
            Assert.True(p.PlayDayCount <= 20);
        }
        Assert.Contains(e.World.Persons, p => p.PlayDayCount > 5);
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Desligada_NadaRegistra_EValidador()
    {
        var set = TestSupport.FreshPresetSet();
        set.Quirks.Acquisition.Enabled = false;
        var off = ContentDatabase.Create(set);
        var p = PopulationGenerator.Generate(5, off, 3);
        QuirkSystem.Observe(off, p, QuirkIds.Persistente, true, false);
        Assert.Empty(p.QuirkEvidenceByTrait);
        Assert.Null(QuirkSystem.OnDaily(off, p, 1, true));
        var bad = TestSupport.FreshPresetSet();
        bad.Quirks.Acquisition.Persistente.LoseRatePermille = 800;
        bad.Quirks.Acquisition.InitialConfirmPlayDays = 2;
        var errors = ContentValidator.Validate(bad).Where(i => i.Severity == IssueSeverity.Error).Select(i => i.Path).ToList();
        Assert.Contains("acquisition.persistente.loseRatePermille", errors);
        Assert.Contains("acquisition.initialConfirmPlayDays", errors);
    }
}

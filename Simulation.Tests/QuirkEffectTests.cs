using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Events;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Time;

namespace Simulation.Tests;

/// <summary>
/// Efeitos das traits da pessoa (P4c): cada efeito no seu ponto de avaliacao, com causa
/// identificavel; desligados, nada muda; limites de sono, obrigacoes e janelas preservados.
/// </summary>
public class QuirkEffectTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private static QuirkEffectsDefinition FX => C.QuirkCatalog.Effects;

    private static void Give(Person p, params string[] ids)
    {
        p.QuirkList.Clear();
        foreach (var id in ids) p.QuirkList.Add(new QuirkState(id, QuirkAcquisitions.Innate, 0));
    }

    private static SimulationEngine Engine(ContentDatabase c, ulong seed, int pop, string? quirkForAll = null)
    {
        var e = WorldFactory.Create(c, new WorldConfig { Seed = seed, PopulationSize = pop });
        if (quirkForAll != null) foreach (var p in e.World.Persons) Give(p, quirkForAll);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        return e;
    }

    private static List<SimEvent> Run(SimulationEngine e, int days)
    {
        var all = new List<SimEvent>();
        for (int d = 0; d < days; d++) { e.AdvanceTo(SimTime.Days(d + 1)); all.AddRange(e.World.Events.Drain()); }
        return all;
    }

    private static ContentDatabase EffectsOff()
    {
        var set = TestSupport.FreshPresetSet();
        set.Quirks.Effects.Enabled = false;
        return ContentDatabase.Create(set);
    }

    private static (Person P, Character Ch, Simulation.Core.World.WorldState W) Online()
    {
        var e = TestSupport.NewEngine(9, 60);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(2));
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0);
        var ch = w.GetCharacter(p.CharacterId);
        ch.SessionEndTick = w.Clock.Tick + SimTime.Hours(4);
        foreach (var r in w.Content.Regions.Keys) ch.Known.Add(r);
        return (p, ch, w);
    }

    private static List<ScoredOption> Eval(Person p, Character ch, Simulation.Core.World.WorldState w, ulong seed = 4) =>
        ActivityScoring.Evaluate(ch, p.Traits, p.Preferences, w.Content, w.Behavior.Routes, w.OccupancyOf, w.Clock.Tick, new DeterministicRng(seed),
            w.Behavior.Quests, w.Behavior.Goals, p.PlaceImpressions, null, p);

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Colecionador_ValorExtraParaONuncaFeito()
    {
        var (p, ch, w) = Online();
        Give(p, QuirkIds.Colecionador);
        var options = Eval(p, ch, w);
        var withBonus = options.Where(o => o.Factors.Any(f => f.Key == QuirkIds.Factor(QuirkIds.Colecionador))).ToList();
        Assert.NotEmpty(withBonus);
        Assert.All(withBonus, o => Assert.True(!p.PlaceImpressions.ContainsKey(o.Key) || !ch.ZoneMinutes.ContainsKey(o.Region.Id)));
        Give(p);
        Assert.DoesNotContain(Eval(p, ch, w), o => o.Factors.Any(f => f.Key.StartsWith("quirk_", StringComparison.Ordinal)));
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Otimizador_RendimentoPesaMaisEGostoMenos()
    {
        var (p, ch, w) = Online();
        Give(p, QuirkIds.Otimizador);
        var options = Eval(p, ch, w);
        foreach (var o in options)
        {
            var fit = o.Factors.FirstOrDefault(f => f.Key == "level_fit");
            var aff = o.Factors.First(f => f.Key == "affinity");
            if (fit != null && fit.Contribution != 0)
                Assert.InRange(o.Factors.First(f => f.Key == "quirk_otimizador").Contribution - fit.Contribution * (FX.Otimizador.RewardPermille - 1000) / 1000, -1, 1);
            if (aff.Contribution != 0)
                Assert.Equal(aff.Contribution * (FX.Otimizador.AffinityPermille - 1000) / 1000, o.Factors.First(f => f.Key == "quirk_otimizador_gosto").Contribution);
        }
        // Tedio mais rapido (contrapartida).
        var a = new Character(1, 1, "a", "x");
        var b = new Character(2, 1, "b", "x");
        SessionRules.AccrueBoredom(a, p.Traits, p.Preferences, ActivityTypes.Hunt, 60, C.Session);
        SessionRules.AccrueBoredom(b, p.Traits, p.Preferences, ActivityTypes.Hunt, 60, C.Session, FX.Otimizador.BoredomPermille);
        Assert.True(b.BoredomByType.GetValueOrDefault(ActivityTypes.Hunt) > a.BoredomByType.GetValueOrDefault(ActivityTypes.Hunt));
    }

    /// <summary>Impulsivo decide com mais ruido; Indeciso, com menos (amplitude do fator "noise").</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void ImpulsivoEIndeciso_RuidoDaDecisao()
    {
        var (p, ch, w) = Online();
        int MaxNoise(params string[] ids)
        {
            Give(p, ids);
            int max = 0;
            for (ulong s = 1; s <= 40; s++)
                foreach (var o in Eval(p, ch, w, s)) max = Math.Max(max, Math.Abs(o.Factors.First(f => f.Key == "noise").Contribution));
            return max;
        }
        int baseAmp = C.Behavior.Noise * (1400 - p.Traits.GameKnowledge * 8 / 10) / 1000;
        int none = MaxNoise(), impulsive = MaxNoise(QuirkIds.Impulsivo), indecisive = MaxNoise(QuirkIds.Indeciso);
        Assert.True(none <= baseAmp && impulsive <= baseAmp * FX.Impulsivo.NoisePermille / 1000 && indecisive <= baseAmp * FX.Indeciso.NoisePermille / 1000);
        Assert.True(impulsive > none && indecisive < none, $"ruido max: {none} / impulsivo {impulsive} / indeciso {indecisive}");
    }

    /// <summary>
    /// Indeciso: a proximidade e medida sem o ruido. Opcoes iguais na base com ruidos diferentes
    /// empatam; opcoes distantes na base nao empatam mesmo que o ruido as aproxime.
    /// </summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Indeciso_EmpateSemORuido()
    {
        var (p, ch, w) = Online();
        var opts = Eval(p, ch, w).Take(2).ToList();
        ScoredOption With(ScoredOption o, int baseUtility, int noise) =>
            new(o.Region, o.Activity, o.Plan, baseUtility + noise, new List<DecisionFactor> { new("affinity", baseUtility), new("noise", noise) });
        var close = new List<ScoredOption> { With(opts[0], 500, 90), With(opts[1], 490, -90) };   // nota final 590 x 400; base 500 x 490
        Assert.Equal(10, SessionRules.CloseCall(close, FX.Indeciso.TieGap)!.Value.Gap);
        var far = new List<ScoredOption> { With(opts[0], 500, -80), With(opts[1], 380, 80) };     // nota final 420 x 460; base 500 x 380
        Assert.Null(SessionRules.CloseCall(far, FX.Indeciso.TieGap));
    }

    /// <summary>Persistente demora mais a abandonar meta sem progresso.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Persistente_DemoraAAbandonarMetas()
    {
        var (p, ch, w) = Online();
        var S = C.GoalSettings;
        p.Traits.Patience = 900;   // sem desistencia por impaciencia (e o prazo calculado com ela)
        long played = ch.PlayedTicksAt(w.Clock.Tick);
        long stale = (S.StaleBaseHours * 1000L + S.StaleExtraHoursAtMaxPatience * (long)p.Traits.Patience) * SimTime.TicksPerHour / 1000;
        bool Abandoned(params string[] ids)
        {
            Give(p, ids);
            ch.GoalList.Clear();
            var g = new PersonalGoal(GoalTypes.PracticeActivity, ActivityTypes.Hunt, 300, 0, 800, "pref_pve", 500, w.Clock.Tick, played);
            g.LastProgressPlayedTicks = played - stale - SimTime.Minutes(30);   // passou do prazo normal, nao do prolongado
            ch.GoalList.Add(g);
            w.Behavior.Goals.Review(p, ch, new DeterministicRng(7));
            return !ch.GoalList.Contains(g);
        }
        Assert.True(Abandoned());
        Assert.False(Abandoned(QuirkIds.Persistente));
        Assert.True(p.QuirkStats.GetValueOrDefault("persistente_manteve_meta") > 0);
    }

    /// <summary>Pavio curto: o reves frustra mais. Resiliente: a frustracao passa mais rapido. Persistente marca o reves.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void PavioCurtoEResiliente_Frustracao()
    {
        var (p, ch, w) = Online();
        int Gain(params string[] ids)
        {
            Give(p, ids);
            p.Mood.Frustration = 100;
            w.Behavior.Experience.OnSetback(p, ch, "teste", "", 10, 100, 0);
            return p.Mood.Frustration - 100;
        }
        Assert.Equal(100, Gain());
        Assert.Equal(100 * FX.PavioCurto.FrustrationGainPermille / 1000, Gain(QuirkIds.PavioCurto));
        Assert.Equal(w.Clock.Tick, ch.LastSetbackTick);
        int Decay(params string[] ids)
        {
            Give(p, ids);
            int total = 0;
            for (int d = 0; d < 10; d++) { p.Mood.Frustration = 500; w.Behavior.Experience.OnDaily(p, ch); total += 500 - p.Mood.Frustration; }
            return total;
        }
        int normal = Decay(), resilient = Decay(QuirkIds.Resiliente);
        Assert.InRange(resilient - normal * FX.Resiliente.FrustrationDecayPermille / 1000, -1, 1);
    }

    /// <summary>Saudosista: boas lembrancas duram mais. Rancoroso: o lugar ruim fica marcado por mais tempo.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void SaudosistaERancoroso_Memoria()
    {
        var (p, ch, w) = Online();
        int GoodLeft(params string[] ids)
        {
            Give(p, ids);
            p.MemoryList.Clear();
            w.Behavior.Experience.Remember(p, "teste", "x", 500, 500);
            for (int d = 0; d < 10; d++) w.Behavior.Experience.OnDaily(p, ch);
            return p.MemoryList.FirstOrDefault(m => m.Kind == "teste")?.Intensity ?? 0;
        }
        Assert.True(GoodLeft(QuirkIds.Saudosista) > GoodLeft());
        int BadLeft(params string[] ids)
        {
            Give(p, ids);
            p.Impressions["z/a"] = -600;
            for (int d = 0; d < 10; d++) w.Behavior.Experience.OnDaily(p, ch);
            return -p.Impressions["z/a"];
        }
        int normal = BadLeft(), grudge = BadLeft(QuirkIds.Rancoroso), firm = BadLeft(QuirkIds.MemoriaFirme);
        Assert.True(grudge > normal && firm > normal, $"{normal} / rancoroso {grudge} / memoria firme {firm}");
    }

    /// <summary>Estudioso ganha mais Conhecimento em missoes; Esquecido e Memoria firme na queda por inatividade.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void EstudiosoEsquecidoMemoriaFirme_Capacidades()
    {
        var (p, ch, w) = Online();
        var D = C.Dynamics;
        int Study(params string[] ids)
        {
            Give(p, ids);
            p.Traits.GameKnowledge = 300;
            for (int i = 0; i < 10; i++) w.Behavior.Traits.OnQuestCompleted(p);
            return p.Traits.GameKnowledge - 300;
        }
        int normal = Study(), studious = Study(QuirkIds.Estudioso);
        Assert.Equal(10 * D.KnowledgePerQuest, normal);
        Assert.Equal(normal * FX.Estudioso.KnowledgePermille / 1000, studious);

        int Rust(int daysOff, params string[] ids)
        {
            Give(p, ids);
            p.Traits.MechanicalSkill = p.BaseTraits.MechanicalSkill;
            bool wasOnline = p.IsOnline;
            p.State = LifecycleState.Offline;
            p.LastPlayedDay = 100;
            int before = p.Traits.MechanicalSkill;
            for (long day = 101; day <= 100 + daysOff; day++) w.Behavior.Traits.OnDaily(p, day);
            return before - p.Traits.MechanicalSkill;
        }
        int early = D.MechanicalDecayAfterDays - FX.Esquecido.DecayEarlierDays + 1;
        Assert.Equal(0, Rust(early));                                  // ainda dentro da carencia
        Assert.True(Rust(early, QuirkIds.Esquecido) > 0);              // Esquecido ja comeca a cair
        int days = D.MechanicalDecayAfterDays + 10;
        int rust = Rust(days), firmRust = Rust(days, QuirkIds.MemoriaFirme);
        Assert.InRange(firmRust - rust * FX.MemoriaFirme.DecayPermille / 1000, -1, 1);
        Assert.True(rust > 0);
    }

    /// <summary>Maratonista: depois de uma maratona, o dia seguinte comeca com menos disposicao.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Maratonista_DisposicaoDoDiaSeguinte()
    {
        var (p, _, w) = Online();
        Give(p, QuirkIds.Maratonista);
        p.MarathonDaysSet.Clear();
        int normal = w.Behavior.Disposition(p, 20), next = w.Behavior.Disposition(p, 21);
        p.MarathonDaysSet.Add(19);
        Assert.Equal(normal - FX.Maratonista.NextDayDispositionPenaltyPermille, w.Behavior.Disposition(p, 20));
        Assert.Equal(next, w.Behavior.Disposition(p, 21));   // so o dia seguinte
        p.MarathonDaysSet.Add(25);
        Assert.Equal(normal - FX.Maratonista.NextDayDispositionPenaltyPermille, w.Behavior.Disposition(p, 20));   // reproduzivel depois de outra maratona
        Give(p);
        Assert.Equal(normal, w.Behavior.Disposition(p, 20));
    }

    /// <summary>
    /// Mundo em que todos tem a trait: ela atua (contagens e causas) e os limites continuam valendo:
    /// toda sessao comeca numa janela livre do dia acordado (nada de sono, obrigacao ou deslocamento).
    /// </summary>
    [Theory]
    [Trait("Classe", "Invariante")]
    [InlineData("procrastinador", "procrastinador_minutos", "session_delayed")]
    [InlineData("indeciso", "indeciso_hesitou", "hesitated")]
    [InlineData("observador", "observador_pistas", "")]
    [InlineData("maratonista", "maratonista_minutos_sem_saciar", "")]
    [InlineData("impulsivo", "impulsivo_revisou_plano", "")]
    public void TraitAtua_ELimitesValem(string quirk, string stat, string eventType)
    {
        var e = Engine(C, 21, 150, quirk);
        var events = Run(e, 6);
        var w = e.World;
        Assert.True(w.Persons.Sum(p => p.QuirkStats.GetValueOrDefault(stat)) > 0, $"{quirk}: {stat} nao atuou");
        if (eventType.Length > 0)
            Assert.Contains(events, x => x.Type == eventType && x.Causes.Any(c => c.Factor == QuirkIds.Factor(quirk)));
        foreach (var s in events.Where(x => x.Type == "session_started" && x.Causes.Count > 0 && x.Causes[0].Factor == "normal"))
        {
            var p = w.GetPerson((int)s.Entities[0].Id);
            long cal = BehaviorSystem.LocalDay(p, s.Tick);
            int minute = (int)((s.Tick - BehaviorSystem.LocalDayStart(p, cal)) / SimTime.TicksPerMinute);
            var day = w.Behavior.LayoutOf(p, cal);
            if (minute < day.Wake) { cal--; minute += 1440; day = w.Behavior.LayoutOf(p, cal); }
            if (day.Wake > minute) continue;   // fora da agenda guardada
            Assert.Contains(day.Windows, x => x.Start <= minute && minute < x.End);
        }
    }

    /// <summary>Efeitos desligados: as traits existem, mas nada atua e nenhum fator aparece.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void EfeitosDesligados_NadaAtua()
    {
        var off = EffectsOff();
        foreach (var q in new[] { QuirkIds.Procrastinador, QuirkIds.Indeciso, QuirkIds.Colecionador, QuirkIds.Maratonista })
        {
            var e = Engine(off, 21, 80, q);
            var events = Run(e, 4);
            Assert.All(e.World.Persons, p => Assert.Empty(p.QuirkStats));
            Assert.DoesNotContain(events, x => x.Causes.Any(c => c.Factor.StartsWith("quirk_", StringComparison.Ordinal)));
        }
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Validador_Efeitos()
    {
        var set = TestSupport.FreshPresetSet();
        set.Quirks.Effects.Procrastinador.MaxDelayMinutes = 5;
        set.Quirks.Effects.Impulsivo.ImpulseReviewPermille = 1500;
        var errors = ContentValidator.Validate(set).Where(i => i.Severity == IssueSeverity.Error).Select(i => i.Path).ToList();
        Assert.Contains("effects.procrastinador.maxDelayMinutes", errors);
        Assert.Contains("effects.impulsivo.impulseReviewPermille", errors);
    }
}

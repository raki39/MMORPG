using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

/// <summary>Quests estruturadas por area de quest (D-031).</summary>
public class QuestTests
{
    private static ContentDatabase C => TestSupport.Preset;

    [Fact]
    public void Catalogo_TemQuadrosNasPracas_ComPrincipaisSecundariasEDiarias()
    {
        Assert.Equal(3, C.QuestBoards.Count);
        foreach (var b in C.QuestBoards.Values)
        {
            var zone = C.Regions[b.RegionId];
            Assert.True(zone.IsCity, b.Id);
            Assert.Equal(ActivityTypes.Quest, zone.FindActivity(b.ActivityId)!.Type);
            var qs = C.QuestsByBoard[b.Id];
            Assert.Contains(qs, q => q.Category == QuestCategories.Main);
            Assert.Contains(qs, q => q.Category == QuestCategories.Secondary);
            Assert.Contains(qs, q => q.Category == QuestCategories.Daily);
            // A cadeia principal tem pre-requisitos encadeados.
            Assert.Equal(qs.Count(q => q.Category == QuestCategories.Main) - 1,
                qs.Count(q => q.Category == QuestCategories.Main && q.PrerequisiteQuestIds.Count > 0));
        }
    }

    private static (Character Ch, QuestSystem Q, WorldStateHandle W) NewCharacter()
    {
        var e = TestSupport.NewEngine(5, 30);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(7));
        var ch = e.World.Characters[0];
        return (ch, e.World.Behavior.Quests, new WorldStateHandle(e.World));
    }

    private sealed record WorldStateHandle(WorldState World);

    private static void Reset(Character ch, int level)
    {
        ch.Level = level;
        ch.Experience = 0;
        ch.ActiveQuestList.Clear();
        ch.QuestLog.Clear();
        ch.DailyByBoard.Clear();
    }

    [Fact]
    public void Ofertas_RespeitamNivelPreRequisitoEOrdem()
    {
        var (ch, q, _) = NewCharacter();
        Reset(ch, 1);
        var vila = C.QuestBoards["quadro_vila_aurora"];
        var offers = q.Offers(ch, vila).Select(x => x.Id).ToList();
        Assert.Equal("vila_principal_1", offers[0]);                  // principal primeiro
        Assert.DoesNotContain("vila_principal_2", offers);            // exige a anterior
        Assert.DoesNotContain("vila_secundaria_ursos", offers);       // exige nivel 4
        Assert.Empty(q.Offers(ch, C.QuestBoards["quadro_cidadela"])); // nada para nivel 1 la
        ch.QuestLog["vila_principal_1"] = new QuestRecord { Completions = 1, LastCompletedDay = 0 };
        Reset(ch, 5);
        ch.QuestLog["vila_principal_1"] = new QuestRecord { Completions = 1, LastCompletedDay = 0 };
        Assert.Contains("vila_principal_2", q.Offers(ch, vila).Select(x => x.Id));
        Assert.DoesNotContain("vila_principal_1", q.Offers(ch, vila).Select(x => x.Id));   // principal: uma vez
    }

    [Fact]
    public void Ciclo_AceitarEtapasEntregar_DaXpERegistraEstatisticas()
    {
        var (ch, q, h) = NewCharacter();
        Reset(ch, 2);
        var w = h.World;
        var vilaZone = C.Regions["vila_aurora"];
        var board = vilaZone.FindActivity("quadro_de_missoes")!;
        var floresta = C.Regions["floresta_sussurrante"];
        var stats0 = w.QuestStats.TryGetValue("vila_principal_1", out var s0) ? (s0.Accepted, s0.Completed) : (0, 0);

        q.OnActivityDone(ch, vilaZone, board, 5);   // aceita no quadro
        Assert.Contains(ch.ActiveQuests, a => a.QuestId == "vila_principal_1");
        q.OnActivityDone(ch, floresta, floresta.FindActivity("cacar_lobos")!, 18);
        var active = ch.ActiveQuests.First(a => a.QuestId == "vila_principal_1");
        Assert.Equal(0, active.StepIndex);
        Assert.Equal(18, active.StepMinutes);
        q.OnActivityDone(ch, floresta, floresta.FindActivity("cacar_lobos")!, 15);   // 33 >= 30
        Assert.Equal(1, active.StepIndex);
        q.OnActivityDone(ch, floresta, floresta.FindActivity("entrega_lenhador")!, 10);
        Assert.Equal(2, active.StepIndex);   // pronta para entregar

        long xpBefore = ch.XpFromQuests;
        q.OnActivityDone(ch, vilaZone, board, 5);   // entrega (e aceita a proxima oferta)
        Assert.DoesNotContain(ch.ActiveQuests, a => a.QuestId == "vila_principal_1");
        Assert.Equal(1, ch.QuestHistory["vila_principal_1"].Completions);
        Assert.True(ch.XpFromQuests > xpBefore);
        var s = w.QuestStats["vila_principal_1"];
        Assert.Equal(stats0.Item1 + 1, s.Accepted);
        Assert.Equal(stats0.Item2 + 1, s.Completed);
        Assert.True(s.XpGranted > 0);
    }

    [Fact]
    public void Diarias_TemLimitePorQuadroEPorDia()
    {
        var (ch, q, _) = NewCharacter();
        Reset(ch, 6);
        var vila = C.QuestBoards["quadro_vila_aurora"];
        var dailies = C.QuestsByBoard[vila.Id].Where(x => x.Category == QuestCategories.Daily && q.IsEligible(ch, x)).ToList();
        Assert.True(dailies.Count >= 2);
        // Concluidas hoje no limite: nenhuma diaria a mais neste quadro.
        ch.DailyDay = TestDay(ch, q);
        ch.DailyByBoard[vila.Id] = vila.DailyLimit;
        Assert.DoesNotContain(q.Offers(ch, vila), x => x.Category == QuestCategories.Daily);
        // Outro dia: volta a oferecer.
        ch.DailyDay = -1;
        Assert.Contains(q.Offers(ch, vila), x => x.Category == QuestCategories.Daily);
    }

    private static long TestDay(Character ch, QuestSystem q)
    {
        // O dia atual do mundo e o usado pelo QuestSystem; aqui basta forcar a igualdade.
        var e = TestSupport.NewEngine(5, 30);
        e.AdvanceTo(SimTime.Days(7));
        return e.World.Clock.Calendar.DayIndex(e.World.Clock.Tick);
    }

    [Fact]
    public void Quadro_SoEhOpcaoComOfertaOuEntrega_EPontoSoComVisitaPendente()
    {
        var (ch, q, _) = NewCharacter();
        var routes = new RouteFinder(C, new ZoneLayout(C));
        Reset(ch, 3);
        ch.RegionId = "vila_aurora";
        ch.Position = routes.Layout.Hub("vila_aurora");
        ch.SessionEndTick = long.MaxValue / 4;
        foreach (var r in C.Regions.Keys) ch.Known.Add(r);
        var t = new PersonTraits { Patience = 500 };
        var pr = new PersonPreferences { Pve = 600, Exploration = 500 };
        List<string> Keys() => ActivityScoring.Evaluate(ch, t, pr, C, routes, (_, _) => 0, 0, new DeterministicRng(3), q).Select(o => o.Key).ToList();

        Assert.Contains("vila_aurora/quadro_de_missoes", Keys());          // ha oferta
        Assert.DoesNotContain("floresta_sussurrante/entrega_lenhador", Keys());   // sem visita pendente
        // Quests ativas no maximo e nada para entregar: o quadro some.
        for (int i = 0; i < C.QuestSettings.MaxActivePerCharacter; i++)
            ch.ActiveQuestList.Add(new ActiveQuest("forte_secundaria_sapos_" + i, 0, 0));
        Assert.DoesNotContain("vila_aurora/quadro_de_missoes", Keys());
        // Etapa de visita pendente: o ponto de missao aparece, com o fator de quest.
        ch.ActiveQuestList.Clear();
        ch.ActiveQuestList.Add(new ActiveQuest("vila_principal_1", 0, 0) { StepIndex = 1 });
        var opts = ActivityScoring.Evaluate(ch, t, pr, C, routes, (_, _) => 0, 0, new DeterministicRng(3), q);
        var visit = opts.First(o => o.Key == "floresta_sussurrante/entrega_lenhador");
        Assert.Contains(visit.Factors, f => f.Key == "quest_step" && f.Contribution > 0);
    }

    [Fact]
    public void Simulacao_ConcluiQuests_ComEstatisticasCoerentes()
    {
        var e = TestSupport.NewEngine(9, 200);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(21));
        var w = e.World;
        int completed = w.QuestStats.Values.Sum(s => s.Completed);
        Assert.True(completed > 20, $"poucas quests concluídas: {completed}");
        foreach (var (id, s) in w.QuestStats)
        {
            Assert.True(s.Completed <= s.Accepted, id);
            Assert.Equal(w.Characters.Sum(c => c.ActiveQuests.Count(a => a.QuestId == id)), s.Active);
            Assert.Equal(w.Characters.Count(c => c.QuestHistory.TryGetValue(id, out var r) && r.Completions > 0), s.UniqueCompleters);
        }
        Assert.All(w.Characters, c => Assert.True(c.ActiveQuests.Count <= C.QuestSettings.MaxActivePerCharacter));
        // Principais concluidas no maximo uma vez por personagem.
        // (D-066: largar uma missao cria registro sem conclusao.)
        Assert.All(w.Characters, c => Assert.All(c.QuestHistory.Where(kv => C.Quests[kv.Key].Category == QuestCategories.Main),
            kv => Assert.True(kv.Value.Completions == 1 || (kv.Value.Completions == 0 && kv.Value.Drops > 0))));
        Assert.True(w.Characters.Sum(c => c.XpFromQuests) > 0);
    }

    [Fact]
    public void Validador_ApontaErrosDeQuest()
    {
        var set = TestSupport.FreshPresetSet();
        var quests = set.Quests.Quests;
        quests[0].Steps[1].ActivityId = "cacar_lobos";                 // visita fora de ponto de missao
        quests[1].PrerequisiteQuestIds = new List<string> { "vila_principal_3" };   // ciclo 2 -> 3 -> 2
        quests[2].MinLevel = 1;                                          // etapa exige nivel maior
        set.Quests.Boards[0].ActivityId = "taverna";
        var errors = ContentValidator.Validate(set).Where(i => i.Severity == IssueSeverity.Error && i.File == ContentValidator.QuestsFile).Select(i => i.Path + " " + i.Message).ToList();
        Assert.Contains(errors, x => x.StartsWith("quests[0].steps[1].activityId") && x.Contains("ponto de missão"));
        Assert.Contains(errors, x => x.Contains("ciclo"));
        Assert.Contains(errors, x => x.StartsWith("quests[2].steps[0].activityId") && x.Contains("nível"));
        Assert.Contains(errors, x => x.StartsWith("boards[0].activityId"));
    }
}

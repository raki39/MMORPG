using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

/// <summary>
/// D-066: diario de missoes (diarias expiram no reset do servidor; troca de prioridade pelo valor da
/// missao; regras do conteudo ao largar; meta impedida nao e fracasso), escolha e readocao das metas
/// (atratividade da etapa real; bloqueio ate avanco ou mudanca relevante) e desistencia de objetivos
/// (interesse em queda sustentada e experiencia negativa com ligacao concreta; primeiro deixa de lado).
/// </summary>
public class QuestJournalTests
{
    private static ContentDatabase C => TestSupport.Preset;

    private static ContentDatabase With(Action<ContentSet> change)
    {
        var s = TestSupport.FreshPresetSet();
        change(s);
        return ContentDatabase.Create(s);
    }

    private static (SimulationEngine E, WorldState W, Person P, Character Ch) World(ContentDatabase c, ulong seed = 71)
    {
        var e = WorldFactory.Create(c, new WorldConfig { Seed = seed, PopulationSize = 80 });
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(2));
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0);
        var ch = w.GetCharacter(p.CharacterId);
        ch.GoalList.Clear();
        ch.ActiveQuestList.Clear();
        p.ObjectiveList.Clear();
        return (e, w, p, ch);
    }

    private static PersonObjective Obj(string id, int importance = 600) =>
        new(id, "", ObjectiveTiers.General, importance, 600, ObjectiveSystem.InitialOrigin, 0, Array.Empty<(string, int)>());

    private static ActiveQuest Active(WorldState w, Character ch, string id)
    {
        var aq = new ActiveQuest(id, w.Clock.Tick, ch.PlayedTicks);
        ch.ActiveQuestList.Add(aq);
        return aq;
    }

    // ---------------- 1. Diario ----------------

    [Fact]
    public void Diarias_ExpiramNoResetDoServidor()
    {
        var c = With(s => s.Quests.Settings.DailyResetMinute = 360);   // 06:00 no relogio do servidor
        var e = WorldFactory.Create(c, new WorldConfig { Seed = 72, PopulationSize = 150 });
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(3) + SimTime.Hours(5));
        var w = e.World;
        long reset = SimTime.Days(3) + SimTime.Hours(6);
        e.AdvanceTo(reset + 1);
        // Logo depois do reset, nenhuma diaria aceita antes dele continua ativa.
        Assert.DoesNotContain(w.Characters.SelectMany(ch => ch.ActiveQuests), a => c.Quests[a.QuestId].Category == QuestCategories.Daily && a.AcceptedTick < reset);
        foreach (var (id, s) in w.QuestStats) Assert.Equal(w.Characters.Sum(ch => ch.ActiveQuests.Count(a => a.QuestId == id)), s.Active);
    }

    [Fact]
    public void Valor_ConsideraProgressoHistoriaLiberacaoEObjetivo()
    {
        var (_, w, p, ch) = World(C);
        ch.Level = 20;
        var q = C.Quests["vila_secundaria_ervas"];
        var fresh = new ActiveQuest(q.Id, 0, 0);
        var halfway = new ActiveQuest(q.Id, 0, 0) { StepMinutes = q.Steps[0].Minutes / 2 };
        var quests = w.Behavior.Quests;
        Assert.True(quests.Value(p, ch, q, halfway) > quests.Value(p, ch, q, fresh));            // progresso protege
        var main = C.Quests["vila_principal_2"];
        Assert.True(quests.Value(p, ch, main, null) > quests.Value(p, ch, q, null));             // historia (principal)
        int before = quests.Value(p, ch, main, null);
        p.ObjectiveList.Add(Obj("campanha_principal"));
        Assert.True(quests.Value(p, ch, main, null) > before);                                    // vinculo com objetivo
    }

    [Fact]
    public void Troca_DePrioridade_LargaAMaisFraca_ComMotivo_SemFrustracao()
    {
        var (_, w, p, ch) = World(C);
        ch.Level = 20;
        foreach (var z in C.Regions.Keys) ch.Known.Add(z);
        // Vagas cheias com secundarias antigas, sem progresso.
        foreach (var id in new[] { "vila_secundaria_ervas", "vila_secundaria_javalis", "vila_secundaria_minerio" }) Active(w, ch, id);
        var board = C.QuestBoards.Values.First(b => b.Id == "quadro_vila_aurora");
        var offer = w.Behavior.Quests.Offers(ch, board).FirstOrDefault(q => q.Category == QuestCategories.Main);
        Assert.NotNull(offer);
        Assert.NotNull(w.Behavior.Quests.SwapTarget(p, ch, offer!));
        int frustration = p.Mood.Frustration;
        w.Events.Drain();
        var region = C.Regions[board.RegionId];
        w.Behavior.Quests.OnActivityDone(ch, region, region.FindActivity(board.ActivityId)!, 5);
        Assert.Contains(ch.ActiveQuests, a => a.QuestId == offer!.Id);
        var dropped = w.Events.Drain().Where(x => x.Type == "quest_dropped").ToList();
        Assert.Single(dropped);
        Assert.StartsWith("replaced:", dropped[0].Causes[0].Factor);
        Assert.Equal(frustration, p.Mood.Frustration);
        Assert.Equal(1, ch.QuestHistory[dropped[0].Detail!].Drops);
    }

    [Fact]
    public void Troca_NaoLargaMissaoComProgressoAlvoDeMetaOuPronta()
    {
        var (_, w, p, ch) = World(C);
        ch.Level = 20;
        foreach (var z in C.Regions.Keys) ch.Known.Add(z);
        var a1 = Active(w, ch, "vila_secundaria_ervas");
        a1.StepIndex = C.Quests[a1.QuestId].Steps.Count;                                    // pronta para entregar
        Active(w, ch, "vila_secundaria_javalis");
        ch.GoalList.Add(new PersonalGoal(GoalTypes.CompleteQuest, "vila_secundaria_javalis", 1, 0, 500, "patience", 500, w.Clock.Tick, ch.PlayedTicks));
        var a3 = Active(w, ch, "vila_secundaria_minerio");
        a3.StepMinutes = C.Quests[a3.QuestId].Steps[0].Minutes - 1;                           // quase feita
        var offer = C.Quests["vila_principal_2"];
        // So a quase feita poderia sair, e o progresso a protege acima da margem.
        var target = w.Behavior.Quests.SwapTarget(p, ch, offer);
        Assert.True(target == null || target.QuestId == "vila_secundaria_minerio");
        if (target != null) Assert.True(w.Behavior.Quests.Value(p, ch, offer, null) > w.Behavior.Quests.Value(p, ch, C.Quests[a3.QuestId], a3) + C.QuestSettings.Journal.SwapMarginPermille);
    }

    [Fact]
    public void RegrasDoConteudo_GuardarProgresso_EIntervaloParaReaceitar()
    {
        var keep = With(s => { s.Quests.Settings.DropKeepsProgress = true; s.Quests.Settings.ReacceptAfterDropDays = 2; });
        var (_, w, p, ch) = World(keep);
        var aq = Active(w, ch, "vila_secundaria_ervas");
        aq.StepMinutes = 7;
        w.Behavior.Quests.Drop(ch, aq, "test", 0, 0);
        var q = keep.Quests["vila_secundaria_ervas"];
        Assert.False(w.Behavior.Quests.IsEligible(ch, q));                                      // intervalo para aceitar de novo
        Assert.Equal(7, ch.QuestHistory[q.Id].SavedStepMinutes);
        var lose = With(s => s.Quests.Settings.DropKeepsProgress = false);
        var (_, w2, p2, ch2) = World(lose);
        var aq2 = Active(w2, ch2, "vila_secundaria_ervas");
        aq2.StepMinutes = 7;
        w2.Behavior.Quests.Drop(ch2, aq2, "test", 0, 0);
        Assert.Equal(-1, ch2.QuestHistory["vila_secundaria_ervas"].SavedStepIndex);              // perde o progresso
        Assert.True(w2.Behavior.Quests.IsEligible(ch2, lose.Quests["vila_secundaria_ervas"]));   // sem intervalo
    }

    [Fact]
    public void MetaImpedida_NaoEFracasso()
    {
        var (_, w, p, ch) = World(C);
        // Vagas cheias com missoes protegidas (alvos de meta): a principal desejada nao cabe nem por troca.
        foreach (var id in new[] { "vila_secundaria_ervas", "vila_secundaria_javalis", "vila_secundaria_minerio" })
        {
            Active(w, ch, id);
            ch.GoalList.Add(new PersonalGoal(GoalTypes.CompleteQuest, id, 1, 0, 500, "patience", 500, w.Clock.Tick, ch.PlayedTicks));
        }
        var g = new PersonalGoal(GoalTypes.CompleteQuest, "vila_principal_2", 1, 0, 500, "patience", 500, w.Clock.Tick, -10_000_000);
        ch.GoalList.Clear();
        ch.GoalList.Add(g);
        foreach (var id in new[] { "vila_secundaria_ervas", "vila_secundaria_javalis" })
            ch.GoalList.Add(new PersonalGoal(GoalTypes.CompleteQuest, id, 1, 0, 500, "patience", 500, w.Clock.Tick, ch.PlayedTicks));
        if (w.Behavior.Quests.CanTake(p, ch, C.Quests["vila_principal_2"])) return;   // a terceira vaga pode ser trocada: caso nao se aplica
        int frustration = p.Mood.Frustration;
        w.Behavior.Goals.Review(p, ch, new DeterministicRng(1));
        Assert.Contains(ch.Goals, x => x == g);                    // nao abandonou por "sem progresso"
        Assert.True(g.BlockedSincePlayedTicks >= 0);
        Assert.Equal(frustration, p.Mood.Frustration);
    }

    // ---------------- 2. Metas ----------------

    [Fact]
    public void Atratividade_MissaoAntigaPerdeRendimento_MasACampanhaContinuaRelevante()
    {
        var (_, w, p, ch) = World(C);
        ch.Level = 28;
        foreach (var z in C.Regions.Keys) ch.Known.Add(z);
        Active(w, ch, "vila_principal_2");                         // recomendada no nivel 5
        var none = w.Behavior.Goals.AttractiveQuest(p, ch);
        Assert.True(none == null || none.Value.Item1.Id != "vila_principal_2" || none.Value.Item2 < 1000);
        p.ObjectiveList.Add(Obj("campanha_principal"));
        var withCampaign = w.Behavior.Goals.AttractiveQuest(p, ch);
        Assert.NotNull(withCampaign);
        Assert.Equal(QuestCategories.Main, C.Quests[withCampaign!.Value.Item1.Id].Category);
        Assert.True(withCampaign.Value.Item2 >= C.GoalSettings.QuestMinAttractiveness);
        Assert.True(withCampaign.Value.Item2 <= 1000);              // o deslocamento so reduz
    }

    [Fact]
    public void Readocao_BloqueadaAteAvancoOuMudancaRelevante()
    {
        var (_, w, p, ch) = World(C);
        var goals = w.Behavior.Goals;
        // Missao: largada sem progresso fica bloqueada; avancar a missao libera.
        Active(w, ch, "vila_principal_1");
        ch.GoalList.Add(new PersonalGoal(GoalTypes.CompleteQuest, "vila_principal_1", 1, 0, 500, "patience", 500, w.Clock.Tick, -10_000_000));
        goals.Review(p, ch, new DeterministicRng(2));
        Assert.Contains("complete_quest|vila_principal_1", ch.BlockedTargets);
        for (ulong k = 3; k < 30; k++)
            Assert.DoesNotContain(goals.CandidateWeights(p, ch, new DeterministicRng(k)), x => x.Type == GoalTypes.CompleteQuest && x.Target == "vila_principal_1");
        goals.OnQuestProgress(ch, "vila_principal_1");
        Assert.DoesNotContain("complete_quest|vila_principal_1", ch.BlockedTargets);
        // Pratica: voltar a praticar libera.
        ch.GoalList.Clear();
        ch.GoalList.Add(new PersonalGoal(GoalTypes.PracticeActivity, ActivityTypes.Hunt, 120, 0, 500, "pref_pve", 500, w.Clock.Tick, -10_000_000));
        goals.Review(p, ch, new DeterministicRng(4));
        Assert.Contains("practice_activity|hunt", ch.BlockedTargets);
        var hunt = C.Regions.Values.SelectMany(r => r.AllActivities()).First(a => a.Type == ActivityTypes.Hunt);
        goals.OnActivity(p, ch, hunt, 5);
        Assert.DoesNotContain("practice_activity|hunt", ch.BlockedTargets);
        // Um objetivo de campanha adotado libera as principais bloqueadas.
        ch.BlockedTargetSet.Add("complete_quest|vila_principal_2");
        goals.OnObjectiveAdopted(ch, "campanha_principal");
        Assert.DoesNotContain("complete_quest|vila_principal_2", ch.BlockedTargets);
    }

    // ---------------- 3. Objetivos ----------------

    private static List<ObjectiveEvent> Days(ContentDatabase c, Person p, Character ch, int n)
    {
        var all = new List<ObjectiveEvent>();
        for (int d = 0; d < n; d++) all.AddRange(ObjectiveEvolution.OnDaily(c, p, ch, 10 + d, true, 9));
        return all;
    }

    [Fact]
    public void Interesse_PequenaMudancaNaoContaMasQuedaSustentadaDeixaDeLadoDepoisAbandona()
    {
        var c = With(s => { s.Objectives.Evolution.InterestFloorPermille = 10_000; s.Objectives.Evolution.ChangeCooldownPlayDays = 365; });
        var (_, w, p, ch) = World(c);
        var o = Obj("nivel_maximo");
        int fit = ObjectiveSystem.Fit(c, p, c.Objectives["nivel_maximo"]);
        o.FitAtSet = fit + ObjectiveEvolution.RequiredDrop(c.ObjectiveCatalog.Evolution, o) - 10;   // queda pequena demais
        p.ObjectiveList.Add(o);
        Days(c, p, ch, 30);
        Assert.Equal(ObjectiveStates.Active, o.State);
        o.FitAtSet = fit + 1000;                                                                      // queda grande e sustentada
        var ev = c.ObjectiveCatalog.Evolution;
        Days(c, p, ch, ev.InterestConfirmPlayDays - 1);
        Assert.Equal(ObjectiveStates.Active, o.State);
        var evs = Days(c, p, ch, 1);
        Assert.Equal(ObjectiveStates.Paused, o.State);                                                // primeiro deixa de lado
        Assert.Equal("interest_low", o.EndReason);
        Assert.Contains(evs, x => x.Type == "objective_paused");
        evs = Days(c, p, ch, ev.AbandonAfterSetAsidePlayDays);
        Assert.Equal(ObjectiveStates.Abandoned, o.State);
        Assert.Contains(evs, x => x.Type == "objective_abandoned" && x.Reason == "interest_low");
    }

    [Fact]
    public void Interesse_QueVolta_RetomaODeixadoDeLado()
    {
        var c = With(s => { s.Objectives.Evolution.InterestFloorPermille = 10_000; s.Objectives.Evolution.ChangeCooldownPlayDays = 365; });
        var (_, w, p, ch) = World(c);
        var o = Obj("nivel_maximo");
        o.FitAtSet = ObjectiveSystem.Fit(c, p, c.Objectives["nivel_maximo"]) + 1000;
        p.ObjectiveList.Add(o);
        Days(c, p, ch, c.ObjectiveCatalog.Evolution.InterestConfirmPlayDays);
        Assert.Equal(ObjectiveStates.Paused, o.State);
        // O piso volta ao normal (a pessoa voltou a se interessar): retoma depois de dias seguidos.
        c.ObjectiveCatalog.Evolution.InterestFloorPermille = 0;
        c.ObjectiveCatalog.Evolution.InterestRecoverMarginPermille = 0;
        Days(c, p, ch, c.ObjectiveCatalog.Evolution.InterestConfirmPlayDays);
        Assert.Equal(ObjectiveStates.Active, o.State);
        Assert.Contains(p.ObjectiveHistory, h => h.Change == "resumed" && h.Reason == "interest_back");
    }

    [Fact]
    public void ExperienciaNegativa_SoComLigacaoConcreta()
    {
        var (_, w, p, ch) = World(C);
        var camp = Obj("campanha_principal");
        p.ObjectiveList.Add(camp);
        // Experiencias ruins de caca fora das etapas da campanha: sem ligacao.
        var mainSteps = C.Quests.Values.Where(q => q.Category == QuestCategories.Main).SelectMany(q => q.Steps).Select(s => s.RegionId + "/" + s.ActivityId).ToHashSet();
        var unrelated = C.Regions.Values.SelectMany(r => r.AllActivities().Where(a => a.Type == ActivityTypes.Hunt).Select(a => r.Id + "/" + a.Id)).Where(k => !mainSteps.Contains(k)).Take(3).ToList();
        foreach (var k in unrelated) p.MemoryList.Add(new Memory("awful_activity", k, -600, 600, w.Clock.Tick));
        Assert.Equal(0, ObjectiveEvolution.LinkedNegativeMemories(C, p, camp));
        // Nos lugares das etapas das missoes principais: ligacao concreta.
        foreach (var k in mainSteps.OrderBy(x => x, StringComparer.Ordinal).Take(3)) p.MemoryList.Add(new Memory("awful_activity", k, -600, 600, w.Clock.Tick));
        Assert.Equal(3, ObjectiveEvolution.LinkedNegativeMemories(C, p, camp));
        Days(C, p, ch, C.ObjectiveCatalog.Evolution.NegativeConfirmPlayDays);
        Assert.Equal(ObjectiveStates.Paused, camp.State);
        Assert.Equal("negative_experience", camp.EndReason);
    }

    [Fact]
    public void Validador_D066()
    {
        var s = TestSupport.FreshPresetSet();
        s.Quests.Settings.DailyResetMinute = 1500;
        s.Population.Goals.QuestMinAttractiveness = 2000;
        s.Objectives.Evolution.InterestConfirmPlayDays = 0;
        s.Objectives.Objectives[0].NegativeLink.ActivityTypes.Add(ActivityTypes.Ambush);
        var errs = ContentValidator.Validate(s).Where(i => i.Severity == IssueSeverity.Error).ToList();
        Assert.Contains(errs, x => x.Path == "settings.dailyResetMinute");
        Assert.Contains(errs, x => x.Path == "goals.questMinAttractiveness");
        Assert.Contains(errs, x => x.Path == "evolution.interestConfirmPlayDays");
        Assert.Contains(errs, x => x.Path == "objectives[0].negativeLink.activityTypes");
    }
}

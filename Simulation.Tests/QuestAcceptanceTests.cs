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
/// D-067: visita coerente (a escolha de ir ao quadro e a acao nele saem do mesmo plano), aceite avaliado
/// (motivacao: recompensa no nivel, relevancia ou gosto, vezes o deslocamento; prazo do reset; a diaria que
/// expirou intocada nao volta no mesmo estado), valor da missao pela recompensa, prazo da diaria pronta e
/// bloqueio da meta de nivel para qualquer alvo ate subir de nivel.
/// </summary>
public class QuestAcceptanceTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private static QuestAcceptanceDefinition A => C.QuestSettings.Journal.Acceptance;
    private static readonly string[] Motives = { "goal", "reward", "campaign", "unlocks", "practice_goal", "taste" };

    private static ContentDatabase With(Action<ContentSet> change)
    {
        var s = TestSupport.FreshPresetSet();
        change(s);
        return ContentDatabase.Create(s);
    }

    private static (SimulationEngine E, WorldState W, Person P, Character Ch) World(ContentDatabase c, ulong seed = 71, long until = -1)
    {
        var e = WorldFactory.Create(c, new WorldConfig { Seed = seed, PopulationSize = 80 });
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(until >= 0 ? until : SimTime.Days(2));
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0);
        var ch = w.GetCharacter(p.CharacterId);
        ch.GoalList.Clear();
        ch.ActiveQuestList.Clear();
        p.ObjectiveList.Clear();
        ch.Level = 6;
        foreach (var z in c.Regions.Keys) ch.Known.Add(z);
        return (e, w, p, ch);
    }

    private static ActiveQuest Active(WorldState w, Character ch, string id)
    {
        var aq = new ActiveQuest(id, w.Clock.Tick, ch.PlayedTicks);
        ch.ActiveQuestList.Add(aq);
        return aq;
    }

    private static QuestBoardDefinition Aurora => C.QuestBoards["quadro_vila_aurora"];

    private static void Taste(Person p, int value)
    {
        foreach (var k in PersonPreferences.Keys) p.Preferences.Set(k, value);
    }

    private static List<ScoredOption> Evaluate(WorldState w, Person p, Character ch) =>
        ActivityScoring.Evaluate(ch, p.Traits, p.Preferences, w.Content, w.Behavior.Routes, w.OccupancyOf, w.Clock.Tick,
            new DeterministicRng(5), w.Behavior.Quests, w.Behavior.Goals, p.PlaceImpressions, null, p);

    private static bool IsBoard(ScoredOption o, QuestBoardDefinition b) => o.Region.Id == b.RegionId && o.Activity.Id == b.ActivityId;

    // ---------------- Visita coerente ----------------

    [Fact]
    public void VisitaCoerente_OQuadroFazExatamenteOQueOPlanoDiz_ERegistraOMotivo()
    {
        var e = WorldFactory.Create(C, new WorldConfig { Seed = 81, PopulationSize = 150 });
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(4) + SimTime.Hours(20));
        var w = e.World;
        var quests = w.Behavior.Quests;
        // Aceites da simulacao: todos com o motivo registrado.
        var accepted = w.Events.Drain().Where(x => x.Type == "quest_accepted").ToList();
        Assert.NotEmpty(accepted);
        Assert.All(accepted, x => Assert.Contains(x.Causes, c => Motives.Contains(c.Factor)));
        int planned = 0;
        foreach (var ch in w.Characters.OrderBy(c => c.Id))
        {
            var p = w.GetPerson(ch.PersonId);
            foreach (var b in C.QuestBoards.Values.OrderBy(b => b.Id, StringComparer.Ordinal))
            {
                if (quests.ReadyCount(ch, b) > 0) continue;
                var plan = quests.PlanBoard(p, ch, b);
                var before = ch.ActiveQuests.Select(a => a.QuestId).ToHashSet(StringComparer.Ordinal);
                var region = C.Regions[b.RegionId];
                quests.OnActivityDone(ch, region, region.FindActivity(b.ActivityId)!, 5);
                var added = ch.ActiveQuests.Select(a => a.QuestId).Where(id => !before.Contains(id)).OrderBy(x => x, StringComparer.Ordinal);
                Assert.Equal(plan.Select(x => x.Offer.Id).OrderBy(x => x, StringComparer.Ordinal), added);
                Assert.True(ch.ActiveQuests.Count <= C.QuestSettings.MaxActivePerCharacter);
                planned += plan.Count;
            }
        }
        Assert.True(planned > 0);
    }

    [Fact]
    public void VagaGuardada_OQuadroNaoViraOpcaoSemNadaAFazer()
    {
        var (_, w, p, ch) = World(C);
        var quests = w.Behavior.Quests;
        Active(w, ch, "vila_principal_2");
        Active(w, ch, "vila_secundaria_ursos");
        // Meta de uma missao de outro quadro, ainda nao aceita: a terceira vaga fica guardada para ela.
        var other = C.Quests.Values.First(q => q.BoardId == "quadro_forte_do_vau" && q.Category == QuestCategories.Main);
        ch.GoalList.Add(new PersonalGoal(GoalTypes.CompleteQuest, other.Id, 1, 0, 500, "patience", 500, w.Clock.Tick, ch.PlayedTicks));
        Assert.NotEmpty(quests.Offers(ch, Aurora));
        Assert.True(quests.CanAccept(ch));
        Assert.Empty(quests.PlanBoard(p, ch, Aurora));
        ch.SessionEndTick = w.Clock.Tick + SimTime.Hours(4);
        Assert.DoesNotContain(Evaluate(w, p, ch), o => IsBoard(o, Aurora));   // sem ida ao quadro a toa
    }

    // ---------------- Aceite avaliado ----------------

    [Fact]
    public void Aceite_PrecisaDeMotivacao_ADiariaNaoEAceitaSoPorEstarNoQuadro()
    {
        var (_, w, p, ch) = World(C);
        var quests = w.Behavior.Quests;
        var daily = C.Quests["vila_diaria_colheita"];                               // coleta; 5% do XP do nivel
        Assert.True(quests.RewardShare(ch, daily) < A.MinInterest);                 // a recompensa sozinha nao motiva
        Taste(p, 100);
        int low = quests.Interest(p, ch, daily, Aurora, ch.ActiveQuestList).Interest;
        Assert.True(low < A.MinInterest);
        Assert.DoesNotContain(quests.PlanBoard(p, ch, Aurora), x => x.Offer.Id == daily.Id);
        // Quem gosta de coletar aceita pelo gosto (ou so perde a vaga para ofertas mais motivadas).
        Taste(p, 950);
        var (high, motive, _, _, _) = quests.Interest(p, ch, daily, Aurora, ch.ActiveQuestList);
        Assert.Equal("taste", motive);
        Assert.True(high >= A.MinInterest);
        var plan = quests.PlanBoard(p, ch, Aurora);
        Assert.True(plan.Any(x => x.Offer.Id == daily.Id)
            || (plan.Count >= C.QuestSettings.MaxActivePerCharacter && plan.All(x => x.Interest >= high)));
    }

    [Fact]
    public void Aceite_PorOportunidade_ContaODesvioReal_NaoZeraPorSerAMesmaZona()
    {
        var (_, w, p, ch) = World(C);
        var quests = w.Behavior.Quests;
        Taste(p, 600);
        var daily = C.Quests["vila_diaria_colheita"];
        var alone = quests.Interest(p, ch, daily, Aurora, ch.ActiveQuestList);
        Assert.False(alone.OnTheWay);
        Assert.Equal(alone.Travel, alone.Added);
        // Outra missao com etapa em outro ponto da mesma zona: conta o desvio de la ate a etapa e dela ao quadro,
        // menos o que ja faria; menor que sair do quadro, mas nao zero.
        var other = C.Quests.Values.First(q => q.Category == QuestCategories.Secondary && q.Steps.Count > 0
            && q.Steps[0].RegionId == daily.Steps[0].RegionId && q.Steps[0].ActivityId != daily.Steps[0].ActivityId);
        Active(w, ch, other.Id);
        var together = quests.Interest(p, ch, daily, Aurora, ch.ActiveQuestList);
        Assert.True(together.OnTheWay);
        Assert.InRange(together.Added, 1, alone.Added - 1);
        Assert.True(together.Interest >= alone.Interest);
        Assert.Equal(alone.Travel, together.Travel);                                // o prazo usa a ida e volta do quadro
        // No mesmo ponto da etapa (mesma atividade), o desvio real e zero.
        ch.ActiveQuestList.Clear();
        var samePoint = C.Quests.Values.FirstOrDefault(q => q.Id != daily.Id && q.Steps.Count > 0
            && q.Steps[0].RegionId == daily.Steps[0].RegionId && q.Steps[0].ActivityId == daily.Steps[0].ActivityId);
        if (samePoint != null)
        {
            Active(w, ch, samePoint.Id);
            Assert.Equal(0, quests.Interest(p, ch, daily, Aurora, ch.ActiveQuestList).Added);
        }
    }

    [Fact]
    public void Diaria_SoSeDaParaFazerEEntregarAntesDoReset()
    {
        var (_, w, p, ch) = World(C, until: SimTime.Days(3) - SimTime.Minutes(10));
        var quests = w.Behavior.Quests;
        Taste(p, 950);
        var daily = C.Quests["vila_diaria_colheita"];
        int travel = quests.Interest(p, ch, daily, Aurora, ch.ActiveQuestList).Travel;
        Assert.False(quests.FitsBeforeReset(p, ch, daily, travel));                  // 10 min ate o reset
        Assert.DoesNotContain(quests.PlanBoard(p, ch, Aurora), x => C.Quests[x.Offer.Id].Category == QuestCategories.Daily);
        Assert.True(quests.FitsBeforeReset(p, ch, C.Quests["vila_secundaria_ervas"], travel));   // secundaria nao tem prazo
    }

    [Fact]
    public void Diaria_ViabilidadePelasJanelasDeJogoAteOReset_SemExigirUmaSessaoSo()
    {
        var (_, w, p, ch) = World(C);                                                // 00:00; reset em 24 h
        var quests = w.Behavior.Quests;
        var daily = C.Quests["vila_diaria_colheita"];
        int travel = quests.Interest(p, ch, daily, Aurora, ch.ActiveQuestList).Travel;
        int need = 2 * travel + daily.Steps.Sum(s => s.Minutes);
        long now = w.Clock.Tick;
        ch.SessionEndTick = now;                                                     // sem sessao em andamento
        p.OpenWindows.Clear();
        Assert.False(quests.FitsBeforeReset(p, ch, daily, travel));                  // 24 h ate o reset, mas nenhuma janela de jogo
        p.OpenWindows.Add((now + SimTime.Hours(3), now + SimTime.Hours(3) + SimTime.Minutes(need - 1), false));
        Assert.False(quests.FitsBeforeReset(p, ch, daily, travel));                  // janela curta demais
        p.OpenWindows.Add((now + SimTime.Hours(20), now + SimTime.Hours(20) + SimTime.Minutes(2), true));
        Assert.True(quests.FitsBeforeReset(p, ch, daily, travel));                   // somando as janelas do dia, cabe
        p.OpenWindows.Clear();
        p.OpenWindows.Add((now + SimTime.Hours(24) - SimTime.Minutes(need - 1), now + SimTime.Hours(27), true));
        Assert.False(quests.FitsBeforeReset(p, ch, daily, travel));                  // a janela atravessa o reset: so conta o que vem antes
        Assert.Equal(need - 1, quests.PlayableMinutesUntil(p, ch, quests.NextResetTick()));
        // Na decisao de ir ao quadro, vale o instante da chegada: o tempo da viagem tambem corre.
        p.OpenWindows.Clear();
        p.OpenWindows.Add((now, now + SimTime.Minutes(need + 5), false));
        Taste(p, 950);
        Assert.True(quests.FitsBeforeReset(p, ch, daily, travel));
        long arrival = now + SimTime.Minutes(10);
        Assert.False(quests.FitsBeforeReset(p, ch, daily, travel, arrival));
        Assert.DoesNotContain(quests.PlanBoard(p, ch, Aurora, arrival), x => x.Offer.Id == daily.Id);
    }

    [Fact]
    public void DiariaQueExpirouIntocada_SoVoltaDepoisDeAPessoaFazerAAtividade()
    {
        var (_, w, p, ch) = World(C);
        var quests = w.Behavior.Quests;
        Taste(p, 950);
        var daily = C.Quests["vila_diaria_colheita"];
        Active(w, ch, daily.Id);
        quests.ExpireDailies();
        Assert.True(ch.QuestHistory[daily.Id].IdleExpired);
        for (int k = 0; k < 3; k++)                                                  // o mesmo estado nao repete o ciclo
            Assert.DoesNotContain(quests.PlanBoard(p, ch, Aurora), x => x.Offer.Id == daily.Id);
        var step = daily.Steps[0];
        var region = C.Regions[step.RegionId];
        quests.OnActivityDone(ch, region, region.FindActivity(step.ActivityId)!, 10);    // fez a atividade da etapa: mudanca real
        Assert.False(ch.QuestHistory[daily.Id].IdleExpired);
        // Expirar com progresso nao bloqueia.
        var aq = Active(w, ch, daily.Id);
        aq.StepMinutes = 5;
        quests.ExpireDailies();
        Assert.False(ch.QuestHistory[daily.Id].IdleExpired);
    }

    [Fact]
    public void Valor_PesaARecompensa_DiariaNaoTomaAVagaDeSecundariaNoNivel()
    {
        var (_, w, p, ch) = World(C);
        var quests = w.Behavior.Quests;
        Taste(p, 100);
        var daily = C.Quests["vila_diaria_colheita"];
        var sec = C.Quests["vila_secundaria_harpias"];
        Assert.True(quests.Value(p, ch, sec, null) > quests.Value(p, ch, daily, null));
        foreach (var id in new[] { "vila_secundaria_harpias", "vila_secundaria_ursos", "vila_secundaria_minerio" }) Active(w, ch, id);
        Assert.Null(quests.SwapTarget(p, ch, daily));
        // Desligado, o rendimento era so o encaixe de nivel: a diaria no nivel valia bem mais.
        var off = With(s => s.Quests.Settings.Journal.Acceptance.Enabled = false);
        var (_, w2, p2, ch2) = World(off);
        Taste(p2, 100);
        Assert.True(w2.Behavior.Quests.Value(p2, ch2, off.Quests["vila_diaria_colheita"], null) > quests.Value(p, ch, daily, null) + C.QuestSettings.Journal.SwapMarginPermille);
    }

    // ---------------- Prazo da diaria pronta ----------------

    [Fact]
    public void DiariaPronta_EntregarPesaMaisPertoDoFimDaSessao()
    {
        var (_, w, p, ch) = World(C);
        var ready = Active(w, ch, "vila_diaria_colheita");
        ready.StepIndex = 1;
        ch.SessionEndTick = w.Clock.Tick + SimTime.Hours(6);
        var far = Evaluate(w, p, ch).Single(o => IsBoard(o, Aurora));
        Assert.DoesNotContain(far.Factors, f => f.Key == "daily_deadline");
        ch.SessionEndTick = w.Clock.Tick + SimTime.Minutes(20);
        var near = Evaluate(w, p, ch).Single(o => IsBoard(o, Aurora));
        Assert.Contains(near.Factors, f => f.Key == "daily_deadline" && f.Contribution == A.DeliverDeadlineWeight);
        // Secundaria pronta nao vence no reset: sem prazo.
        ready.StepIndex = 0;
        Active(w, ch, "vila_secundaria_ervas").StepIndex = 1;
        Assert.DoesNotContain(Evaluate(w, p, ch).Single(o => IsBoard(o, Aurora)).Factors, f => f.Key == "daily_deadline");
    }

    // ---------------- Meta de nivel ----------------

    [Fact]
    public void BloqueioDaMetaDeNivel_SoAfetaAReadocao_NaoAEscolhaNemAProgressao()
    {
        var (_, w, p, ch) = World(C);
        ch.SessionEndTick = w.Clock.Tick + SimTime.Hours(4);
        var before = Evaluate(w, p, ch).Select(o => (o.Key, o.Utility)).ToList();
        ch.BlockedLevelGoals.Add(GoalSystem.LevelGoalKey(GoalTypes.ReachLevel, "", 9));
        var after = Evaluate(w, p, ch).Select(o => (o.Key, o.Utility)).ToList();
        Assert.Equal(before, after);                                                 // mesmas opcoes e notas: XP e atividades seguem normais
        Assert.DoesNotContain(w.Behavior.Goals.CandidateWeights(p, ch, new DeterministicRng(2)), x => x.Type == GoalTypes.ReachLevel);
    }

    [Fact]
    public void MetaDeNivelLargada_BloqueiaQualquerAlvoAteSubirDeNivel()
    {
        var (_, w, p, ch) = World(C);
        var goals = w.Behavior.Goals;
        ch.Level = 8;
        // Criada no nivel 6 (alvo 9); a pessoa subiu ate o 8 e parou de ganhar XP: largada sem progresso.
        ch.GoalList.Add(new PersonalGoal(GoalTypes.ReachLevel, "", 9, 6, 500, "pref_pve", 500, w.Clock.Tick, -10_000_000));
        goals.Review(p, ch, new DeterministicRng(3));
        Assert.DoesNotContain(ch.Goals, x => x.Type == GoalTypes.ReachLevel);
        for (ulong k = 4; k < 30; k++)                                              // nem o alvo novo (8 + k), no mesmo estado
            Assert.DoesNotContain(goals.CandidateWeights(p, ch, new DeterministicRng(k)), x => x.Type == GoalTypes.ReachLevel);
        ch.Level = 9;
        goals.OnLevelUp(p, ch);                                                     // subir de nivel e mudanca relevante
        Assert.Contains(goals.CandidateWeights(p, ch, new DeterministicRng(31)), x => x.Type == GoalTypes.ReachLevel);
    }

    [Fact]
    public void Validador_D067()
    {
        var s = TestSupport.FreshPresetSet();
        s.Quests.Settings.Journal.Acceptance.MinInterest = 2000;
        s.Quests.Settings.Journal.Acceptance.DeliverDeadlineMarginMinutes = 500;
        var errs = ContentValidator.Validate(s).Where(i => i.Severity == IssueSeverity.Error).ToList();
        Assert.Contains(errs, x => x.Path == "settings.journal.acceptance.minInterest");
        Assert.Contains(errs, x => x.Path == "settings.journal.acceptance.deliverDeadlineMarginMinutes");
        var s2 = TestSupport.FreshPresetSet();
        s2.Quests.Settings.Journal.Enabled = false;
        Assert.Contains(ContentValidator.Validate(s2), x => x.Severity == IssueSeverity.Error && x.Path == "settings.journal");
    }
}

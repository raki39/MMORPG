using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

/// <summary>
/// Objetivos pessoais na O3 (D-064): concluidos ficam no historico; conquista definitiva x condicao a
/// manter (perder nao apaga a conquista; pode recuperar); renovacao pela ficha, pelos objetivos
/// anteriores, pelas experiencias e pelo conteudo conhecido, sem preencher vaga artificialmente;
/// prioridade, substituicao voluntaria e abandono por experiencia negativa, com intervalos.
/// </summary>
public class ObjectiveEvolutionTests
{
    private static ContentDatabase C => TestSupport.Preset;

    private static PersonObjective Obj(string id, string tier = ObjectiveTiers.General, int importance = 600, int commitment = 600, string param = "") =>
        new(id, param, tier, importance, commitment, ObjectiveSystem.InitialOrigin, 0, Array.Empty<(string, int)>());

    /// <summary>Mundo com uma pessoa registrada, sem objetivos (o teste define os seus).</summary>
    private static (WorldState W, Person P, Character Ch) World(ContentDatabase c, ulong seed = 51)
    {
        var e = WorldFactory.Create(c, new WorldConfig { Seed = seed, PopulationSize = 60 });
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(2));
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0);
        var ch = w.GetCharacter(p.CharacterId);
        p.ObjectiveList.Clear();
        p.ObjectiveHistoryList.Clear();
        p.ObjectiveTrigger = "";
        ch.GoalList.Clear();
        return (w, p, ch);
    }

    private static ContentDatabase With(Action<ContentSet> change)
    {
        var s = TestSupport.FreshPresetSet();
        change(s);
        return ContentDatabase.Create(s);
    }

    private static List<ObjectiveEvent> Daily(ContentDatabase c, Person p, Character ch, long day = 10) =>
        ObjectiveEvolution.OnDaily(c, p, ch, day, playedYesterday: true, seed: 9);

    // ---------------- Concluidos e condicoes ----------------

    [Fact]
    public void Conquista_FicaConcluidaNoHistorico_ENaoVolta()
    {
        var (w, p, ch) = World(C);
        p.ObjectiveList.Add(Obj("conhecer_o_mundo"));
        foreach (var z in ObjectiveSystem.EligibleZones(C)) ch.Known.Add(z);
        w.Behavior.CheckObjectives(p, ch, "zone_discovered");
        var o = p.Objectives.Single();
        Assert.Equal(ObjectiveStates.Completed, o.State);
        Assert.True(o.CompletedDay >= 0);
        Assert.Contains(p.ObjectiveHistory, h => h.Change == "completed" && h.Id == "conhecer_o_mundo");
        Assert.StartsWith(ObjectiveTriggers.Completed, p.ObjectiveTrigger);
        // Fora dos ativos: nao recebe vinculo; e nunca volta como candidata.
        Assert.Empty(ObjectiveSystem.Links(C, p, ch, GoalTypes.DiscoverZone, "floresta_sussurrante"));
        Assert.DoesNotContain(ObjectiveEvolution.Candidates(C, p, ch, new DeterministicRng(1)), x => x.Def.Id == "conhecer_o_mundo");
    }

    [Fact]
    public void Condicao_MantidaPerdidaERecuperavel_SemApagarAConquista()
    {
        // Conteudo de teste: "conhecer o mundo" como condicao a manter, para exercitar a perda com um criterio que existe hoje.
        var c = With(s => s.Objectives.Objectives.First(o => o.Id == "conhecer_o_mundo").Nature = ObjectiveNatures.Condition);
        var (w, p, ch) = World(c);
        p.ObjectiveList.Add(Obj("conhecer_o_mundo"));
        foreach (var z in ObjectiveSystem.EligibleZones(c)) ch.Known.Add(z);
        w.Behavior.CheckObjectives(p, ch, "zone_discovered");
        var o = p.Objectives.Single();
        Assert.Equal(ObjectiveStates.Held, o.State);
        long achieved = o.CompletedDay;
        Assert.Equal(1, o.TimesHeld);
        // Deixa de cumprir: perdida, com historico; a data da conquista continua.
        ch.Known.Remove("floresta_sussurrante");
        p.ObjectiveTrigger = "";
        var evs = Daily(c, p, ch);
        Assert.Equal(ObjectiveStates.Lost, o.State);
        Assert.Equal(achieved, o.CompletedDay);
        Assert.Equal(1, o.TimesLost);
        Assert.Contains(evs, e => e.Type == "objective_lost");
        Assert.Contains(p.ObjectiveHistory, h => h.Change == "lost" && h.Reason == "condition_lost");
        // Pode querer recuperar: candidata com reforco de recuperacao.
        var cand = ObjectiveEvolution.Candidates(c, p, ch, new DeterministicRng(2)).FirstOrDefault(x => x.Def.Id == "conhecer_o_mundo");
        if (ObjectiveSystem.Fit(c, p, c.Objectives["conhecer_o_mundo"]) * ObjectiveEvolution.ExperienceTerm(c, p, c.Objectives["conhecer_o_mundo"]) / 1000 >= c.ObjectiveCatalog.Evolution.MinFitPermille)
            Assert.Equal(ObjectiveStates.Lost, cand.Existing?.State);
    }

    // ---------------- Renovacao ----------------

    [Fact]
    public void Renovacao_NivelMaximoAbreAmbicoes_PelaFicha_SemSequenciaFixa()
    {
        var (w, p, ch) = World(C);
        var done = Obj("nivel_maximo");
        done.State = ObjectiveStates.Completed;
        p.ObjectiveList.Add(done);
        ch.Level = C.Progression.LevelCap;
        var cands = ObjectiveEvolution.Candidates(C, p, ch, new DeterministicRng(3));
        Assert.DoesNotContain(cands, x => x.Def.Id == "nivel_maximo");
        // Relacionados ao nivel maximo ganham o reforco; nenhum recebe progresso ficticio.
        foreach (var x in cands)
        {
            bool opened = C.Objectives["nivel_maximo"].Opens.Contains(x.Def.Id);
            Assert.Equal(opened, x.Related == "nivel_maximo");
            int min = x.Viability == ObjectiveViability.Future ? C.ObjectiveCatalog.Evolution.FutureMinFitPermille : C.ObjectiveCatalog.Evolution.MinFitPermille;
            Assert.True(x.Fit >= min);
        }
        // Ainda ativo (e que pode avancar), o nivel maximo nao abre nada: so depois de alcancado.
        done.State = ObjectiveStates.Active;
        Assert.DoesNotContain(ObjectiveEvolution.Candidates(C, p, ch, new DeterministicRng(3)), x => x.Related == "nivel_maximo");
        // De um objetivo ativo de sistema futuro, so com preparacao concreta ou se o novo pode avancar.
        p.ObjectiveList.Clear();
        p.ObjectiveList.Add(Obj("grandes_desafios_pve"));
        ch.Level = 5;
        foreach (var x in ObjectiveEvolution.Candidates(C, p, ch, new DeterministicRng(3)).Where(x => x.Related == "grandes_desafios_pve"))
            Assert.True(x.Viability == ObjectiveViability.Now || x.Viability == ObjectiveViability.Preparation || x.Def.Id == "equipamento_raro");
        p.ObjectiveList.Clear();
        done.State = ObjectiveStates.Completed;
        p.ObjectiveList.Add(done);
        ch.Level = C.Progression.LevelCap;
        // Pessoas diferentes escolhem coisas diferentes: os pesos dependem da ficha.
        var weights = new List<string>();
        for (int id = 1; id <= 40; id++)
        {
            var q = PopulationGenerator.Generate(id, C, 13);
            q.ObjectiveList.Add(done);
            var cs = ObjectiveEvolution.Candidates(C, q, ch, new DeterministicRng(4));
            if (cs.Count > 0) weights.Add(cs.OrderByDescending(x => x.Weight).First().Def.Id);
        }
        Assert.True(weights.Distinct().Count() >= 3, string.Join(",", weights.Distinct()));
    }

    [Fact]
    public void Renovacao_AdotaComMotivo_EPromoveQuandoFaltaGeral()
    {
        var (w, p, ch) = World(C);
        var done = Obj("campanha_principal");
        done.State = ObjectiveStates.Completed;
        p.ObjectiveList.Add(done);
        p.ObjectiveList.Add(Obj("equipamento_raro", ObjectiveTiers.Secondary, 400, param: "cajado_runico"));
        p.ObjectiveTrigger = ObjectiveTriggers.Completed + ":campanha_principal";
        var evs = Daily(C, p, ch);
        // Sem geral ativo: o secundario sobe (consequencia).
        Assert.Contains(p.ObjectiveHistory, h => h.Change == "promoted" && h.Id == "equipamento_raro" && h.Reason == "no_general");
        // Renovacao: um novo objetivo com o motivo do acontecimento, ou a falta registrada.
        var set = p.ObjectiveHistory.Where(h => h.Change is "set" or "resumed" or "recovered" or "readopted").ToList();
        var gap = p.ObjectiveHistory.Where(h => h.Change == "gap").ToList();
        Assert.True(set.Count + gap.Count == 1);
        if (set.Count == 1) Assert.StartsWith(ObjectiveTriggers.Completed + ":campanha_principal", set[0].Reason);
        Assert.Equal("", p.ObjectiveTrigger);
        Assert.Equal(p.ObjectivePlayDays, p.LastObjectiveChangePlayDay);
        Assert.Contains(evs, e => e.Type is "objective_set" or "objective_gap");
    }

    [Fact]
    public void Renovacao_SemObjetivoRelevante_RegistraAFalta_SemPreencher()
    {
        var c = With(s => { s.Objectives.Evolution.MinFitPermille = 9000; s.Objectives.Evolution.FutureMinFitPermille = 9000; });
        var (w, p, ch) = World(c);
        p.ObjectiveTrigger = ObjectiveTriggers.MaxLevel;
        var evs = Daily(c, p, ch);
        Assert.Empty(p.Objectives);
        Assert.Equal(1, p.ObjectiveGaps);
        Assert.True(p.LastObjectiveGapDay >= 0);
        Assert.Contains(p.ObjectiveHistory, h => h.Change == "gap" && h.Reason == ObjectiveTriggers.MaxLevel);
        Assert.Contains(evs, e => e.Type == "objective_gap");
    }

    [Fact]
    public void Oportunidade_SoOObjetivoDoConteudoDaZona_ENaoEFalta()
    {
        // Conhecer o covil (chefe): so "grandes desafios PvE" pode vir; interesse minimo zerado para garantir candidatas.
        var c = With(s => { s.Objectives.Evolution.MinFitPermille = 0; s.Objectives.Evolution.FutureMinFitPermille = 0; });
        var (w, p, ch) = World(c);
        ch.Known.Add("covil_do_rei_goblin");
        p.ObjectiveTrigger = ObjectiveTriggers.ContentKnown + ":covil_do_rei_goblin";
        Daily(c, p, ch);
        Assert.All(p.Objectives, o => Assert.Equal("grandes_desafios_pve", o.Id));
        Assert.Single(p.Objectives);
        // Zona sem chefe nem PvP: nada, e nao e falta de conteudo.
        var (w2, p2, ch2) = World(c, 52);
        p2.ObjectiveTrigger = ObjectiveTriggers.ContentKnown + ":vila_aurora";
        Daily(c, p2, ch2);
        Assert.Empty(p2.Objectives);
        Assert.Equal(0, p2.ObjectiveGaps);
        Assert.DoesNotContain(p2.ObjectiveHistory, h => h.Change == "gap");
    }

    [Fact]
    public void Renovacao_RespeitaOIntervaloEntreMudancas()
    {
        var (w, p, ch) = World(C);
        p.ObjectivePlayDays = 20;
        p.LastObjectiveChangePlayDay = 18;   // mudou ha 2 dias jogados (intervalo 7)
        p.ObjectiveTrigger = ObjectiveTriggers.MaxLevel;
        Daily(C, p, ch);
        Assert.Equal(ObjectiveTriggers.MaxLevel, p.ObjectiveTrigger);   // continua pendente
        Assert.Empty(p.ObjectiveHistory);
    }

    [Fact]
    public void Substituicao_Voluntaria_PausaSemFrustracao()
    {
        var c = With(s => s.Objectives.Evolution.MaxSecondary = 1);
        var (w, p, ch) = World(c);
        p.ObjectiveList.Add(Obj("nivel_maximo", ObjectiveTiers.General, 900));
        p.ObjectiveList.Add(Obj("campanha_principal", ObjectiveTiers.General, 900));
        p.ObjectiveList.Add(Obj("equipamento_raro", ObjectiveTiers.Secondary, 50, param: "cajado_runico"));
        int frustration = p.Mood.Frustration;
        // Varios dias com gatilho ate uma candidata valer a substituicao (sorteio por dia).
        for (int d = 0; d < 30 && !p.Objectives.Any(o => o.State == ObjectiveStates.Paused); d++)
        {
            p.LastObjectiveChangePlayDay = -1;
            p.ObjectiveTrigger = ObjectiveTriggers.MaxLevel;
            Daily(c, p, ch, 10 + d);
        }
        var paused = p.Objectives.Single(o => o.Id == "equipamento_raro");
        Assert.Equal(ObjectiveStates.Paused, paused.State);
        Assert.StartsWith("replaced:", paused.EndReason);
        Assert.Equal(frustration, p.Mood.Frustration);
        Assert.Equal(1, p.Objectives.Count(o => o.State == ObjectiveStates.Active && o.Tier == ObjectiveTiers.Secondary));
    }

    // ---------------- Prioridade e abandono ----------------

    [Fact]
    public void Estagnacao_BaixaImportancia_SoDoQuePodiaAvancar()
    {
        var (w, p, ch) = World(C);
        var level = Obj("nivel_maximo", ObjectiveTiers.General, 700);
        var fortune = Obj("grande_fortuna", ObjectiveTiers.Secondary, 600);
        p.ObjectiveList.Add(level);
        p.ObjectiveList.Add(fortune);
        Daily(C, p, ch);                         // mede o progresso
        p.ObjectivePlayDays += C.ObjectiveCatalog.Evolution.StalePlayDays;   // muitos dias sem avancar
        Daily(C, p, ch);
        Assert.Equal(700 - C.ObjectiveCatalog.Evolution.StaleImportanceDrop, level.Importance);
        Assert.Equal(600, fortune.Importance);   // sistema futuro nao perde prioridade por nao avancar
        Assert.Contains(p.ObjectiveHistory, h => h.Change == "importance_down" && h.Reason == "no_progress");
        // Ficou abaixo do secundario: trocam de lugar.
        Assert.Equal(ObjectiveTiers.Secondary, level.Tier);
        Assert.Equal(ObjectiveTiers.General, fortune.Tier);
    }

    [Fact]
    public void Objetivo_NaoEAbandonadoPorFalhasDeMetas()
    {
        // D-065: nao conseguir realizar agora nao e motivo para desistir do objetivo.
        var (w, p, ch) = World(C);
        var camp = Obj("campanha_principal", commitment: 0);
        p.ObjectiveList.Add(camp);
        int frustration = p.Mood.Frustration;
        for (int k = 0; k < 8; k++)
        {
            var g = new PersonalGoal(GoalTypes.CompleteQuest, "vila_principal_2", 1, 0, 500, "patience", 500, w.Clock.Tick, -10_000_000);
            g.LinkList.Add(new ObjectiveLink("campanha_principal", ObjectiveLinkKinds.Advance, "vila_principal_2"));
            ch.GoalList.Clear();
            ch.GoalList.Add(g);
            w.Behavior.Goals.Review(p, ch, new DeterministicRng((ulong)k + 1));   // sem progresso ha muito tempo: meta abandonada
        }
        Assert.Equal(ObjectiveStates.Active, camp.State);
        Assert.True(camp.FailureStreak >= 8);   // a sequencia fica registrada (medida), sem efeito
        Assert.DoesNotContain(p.ObjectiveHistory, h => h.Change == "abandoned");
    }

    [Fact]
    public void Abandono_NaSimulacao_SoComMetaLigadaPorAvanco()
    {
        var (w, p, ch) = World(C);
        var camp = Obj("campanha_principal", commitment: 0);
        p.ObjectiveList.Add(camp);
        // Metas de preparacao abandonadas nao contam como falha.
        var g = new PersonalGoal(GoalTypes.ReachLevel, "", ch.Level + 1, ch.Level, 500, "pref_pve", 500, w.Clock.Tick, -10_000_000);
        g.LinkList.Add(new ObjectiveLink("campanha_principal", ObjectiveLinkKinds.Preparation, "vila_principal_2"));
        ch.GoalList.Add(g);
        w.Behavior.Goals.Review(p, ch, new DeterministicRng(6));   // sem progresso ha muito tempo: abandonada
        Assert.Equal(0, camp.FailureStreak);
    }

    // ---------------- Experiencias e conteudo ----------------

    [Fact]
    public void Experiencias_MarcantesAproximamDoObjetivo()
    {
        var (w, p, ch) = World(C);
        var boss = C.Objectives["grandes_desafios_pve"];
        Assert.Equal(1000, ObjectiveEvolution.ExperienceTerm(C, p, C.Objectives["mais_forte"]));   // sem tipos de experiencia
        int before = ObjectiveEvolution.ExperienceTerm(C, p, boss);
        p.MemoryList.Add(new Memory("great_activity", "floresta_sussurrante/cacar_lobos", 500, 500, w.Clock.Tick));
        Assert.True(ObjectiveEvolution.ExperienceTerm(C, p, boss) > before);
    }

    [Fact]
    public void EvolucaoDesligada_NadaMuda()
    {
        var c = With(s => s.Objectives.Evolution.Enabled = false);
        var (w, p, ch) = World(c);
        p.ObjectiveList.Add(Obj("nivel_maximo"));
        p.ObjectiveTrigger = ObjectiveTriggers.MaxLevel;
        Assert.Empty(Daily(c, p, ch));
        Assert.Equal(0, p.ObjectivePlayDays);
        Assert.Empty(p.ObjectiveHistory);
        Assert.Equal(ObjectiveTriggers.MaxLevel, p.ObjectiveTrigger);
    }

    [Fact]
    public void Validador_Evolucao()
    {
        var s = TestSupport.FreshPresetSet();
        s.Objectives.Objectives[0].Nature = "eterna";
        s.Objectives.Objectives[1].Opens.Add("nao_existe");
        s.Objectives.Objectives[2].ExperienceTypes.Add(ActivityTypes.Patrol);
        s.Objectives.Evolution.MaxGeneral = 5;
        var errs = ContentValidator.Validate(s).Where(i => i.Severity == IssueSeverity.Error).ToList();
        Assert.Contains(errs, x => x.Path == "objectives[0].nature");
        Assert.Contains(errs, x => x.Path == "objectives[1].opens");
        Assert.Contains(errs, x => x.Path == "objectives[2].experienceTypes");
        Assert.Contains(errs, x => x.Path == "evolution.maxGeneral");
    }

    [Fact]
    public void Preset_NaturezasEHorizonte()
    {
        Assert.Equal(ObjectiveHorizons.Medium, C.Objectives["conhecer_o_mundo"].Horizon);
        foreach (var id in new[] { "nivel_maximo", "campanha_principal", "conhecer_o_mundo", "grandes_desafios_pve", "equipamento_raro" })
            Assert.Equal(ObjectiveNatures.Achievement, C.Objectives[id].Nature);
        foreach (var id in new[] { "mais_forte", "pvp_competitivo", "grande_fortuna", "fundar_guilda", "entrar_guilda", "melhor_fabricante", "grande_comerciante" })
            Assert.Equal(ObjectiveNatures.Condition, C.Objectives[id].Nature);
        Assert.All(C.Objectives.Values, o => Assert.DoesNotContain(o.ExperienceTypes, t => t is ActivityTypes.Patrol or ActivityTypes.Ambush or ActivityTypes.Boss));
    }
}

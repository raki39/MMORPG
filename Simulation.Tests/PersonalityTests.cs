using Simulation.Core.Behavior;
using Simulation.Core.Content;
using Simulation.Core.Population;
using Simulation.Core.Random;

namespace Simulation.Tests;

/// <summary>
/// Personalidade principal (P4a): uma por jogador, pela posicao na distribuicao do preset (o
/// atributo mais extremo vence), Equilibrado sem destaque, mudanca rara com margem e confirmacao,
/// e regras proprias separadas dos efeitos graduais dos atributos.
/// </summary>
public class PersonalityTests
{
    private static ContentDatabase C => TestSupport.Preset;
    private static PersonalitySettingsDefinition S => C.Personality;

    private static TraitRange R(string attr) => PopulationGenerator.TraitRangeOf(C.PopulationTraits, attr);

    /// <summary>Valor do atributo na posicao dada (permille) da distribuicao do preset.</summary>
    private static int At(string attr, int positionPermille)
    {
        var r = R(attr);
        return TraitDraw.Triangular(positionPermille * 1_000_000L, r.Min!.Value, r.Mode!.Value, r.Max!.Value);
    }

    /// <summary>Atributos de personalidade todos na mediana (nenhum destaque).</summary>
    private static PersonTraits Neutral()
    {
        var t = new PersonTraits();
        foreach (var a in PersonalitySystem.Attributes) Set(t, a, At(a, 500));
        return t;
    }

    private static void Set(PersonTraits t, string a, int v)
    {
        switch (a)
        {
            case "sociability": t.Sociability = v; break;
            case "aggressiveness": t.Aggressiveness = v; break;
            case "patience": t.Patience = v; break;
            case "loyalty": t.Loyalty = v; break;
            case "riskAversion": t.RiskAversion = v; break;
            case "variety": t.Variety = v; break;
        }
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Posicao_NaDistribuicaoDoPreset()
    {
        var r = R("aggressiveness");
        Assert.Equal(0, PersonalitySystem.Position(r, r.Min!.Value));
        Assert.Equal(1000, PersonalitySystem.Position(r, r.Max!.Value));
        Assert.InRange(PersonalitySystem.Position(r, TraitDraw.Median(r.Min!.Value, r.Mode!.Value, r.Max!.Value)), 495, 505);
        int prev = -1;
        for (int v = r.Min!.Value; v <= r.Max!.Value; v += 7) { int p = PersonalitySystem.Position(r, v); Assert.True(p >= prev); prev = p; }
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Classificacao_MaisExtremoVence_EquilibradoSemDestaque()
    {
        Assert.Equal(PersonalitySystem.Balanced, PersonalitySystem.Classify(C, Neutral()));
        var t = Neutral();
        Set(t, "aggressiveness", At("aggressiveness", 30));   // 3% mais baixos: destaque 940
        Assert.Equal("pacifico", PersonalitySystem.Classify(C, t));
        Set(t, "riskAversion", At("riskAversion", 990));       // 1% mais altos: destaque 980, vence
        Assert.Equal("cauteloso", PersonalitySystem.Classify(C, t));
        // Dois extremos iguais: decide a ordem fixa (sociabilidade vem antes da agressividade).
        var tie = Neutral();
        Set(tie, "sociability", At("sociability", 20));
        Set(tie, "aggressiveness", At("aggressiveness", 980));
        int ps = PersonalitySystem.Position(R("sociability"), tie.Sociability), pa = PersonalitySystem.Position(R("aggressiveness"), tie.Aggressiveness);
        string expected = (500 - ps) * 2 >= (pa - 500) * 2 ? "reservado" : "agressivo";
        Assert.Equal(expected, PersonalitySystem.Classify(C, tie));
        // Logo abaixo do limite (nos 16% de baixo): ainda Equilibrado.
        var near = Neutral();
        Set(near, "patience", At("patience", 160));
        Assert.Equal(PersonalitySystem.Balanced, PersonalitySystem.Classify(C, near));
    }

    /// <summary>Distribuicao: sem forcar nenhuma, cada personalidade perto de 1/12 dos que se destacam.</summary>
    [Fact]
    [Trait("Classe", "Regra do preset")]
    public void Distribuicao_EquilibradaPeloPreset()
    {
        var people = Enumerable.Range(1, 12_000).Select(id => PopulationGenerator.Generate(id, C, 7)).ToList();
        var counts = people.GroupBy(p => p.PersonalityId).ToDictionary(g => g.Key, g => g.Count() * 100.0 / people.Count);
        Assert.Equal(13, counts.Count);
        Assert.InRange(counts[PersonalitySystem.Balanced], 8, 15);
        foreach (var t in S.Types) Assert.InRange(counts[t.Id], 5.5, 9.5);
    }

    /// <summary>Histerese: entrar e sair de Equilibrado e trocar de tipo exigem margem.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Mudanca_ComMargemNosDoisSentidos()
    {
        int thr = S.ThresholdPermille, m = S.ChangeMarginPermille;   // 700 e 100: posicoes 150/100/200
        // Equilibrado -> tipo: destaque 750 (posicao 125) nao basta; 850 (posicao 75) basta.
        var t = Neutral();
        Set(t, "aggressiveness", At("aggressiveness", 500 - (thr + m / 2) / 2));
        Assert.Equal(PersonalitySystem.Balanced, PersonalitySystem.Target(C, PersonalitySystem.Balanced, t));
        Set(t, "aggressiveness", At("aggressiveness", 500 - (thr + m + 50) / 2));
        Assert.Equal("pacifico", PersonalitySystem.Target(C, PersonalitySystem.Balanced, t));
        // Tipo -> Equilibrado: destaque 650 (posicao 175) ainda fica; 550 (posicao 225) sai.
        var u = Neutral();
        Set(u, "aggressiveness", At("aggressiveness", 500 - (thr - m / 2) / 2));
        Assert.Equal("pacifico", PersonalitySystem.Target(C, "pacifico", u));
        Set(u, "aggressiveness", At("aggressiveness", 500 - (thr - m - 50) / 2));
        Assert.Equal(PersonalitySystem.Balanced, PersonalitySystem.Target(C, "pacifico", u));
        // Tipo -> outro tipo: o outro precisa passar o atual + margem.
        var w = Neutral();
        Set(w, "aggressiveness", At("aggressiveness", 500 - 800 / 2));   // pacifico 800
        Set(w, "riskAversion", At("riskAversion", 500 + 860 / 2));      // cauteloso 860 (< 800 + 100)
        Assert.Equal("pacifico", PersonalitySystem.Target(C, "pacifico", w));
        Set(w, "riskAversion", At("riskAversion", 500 + 960 / 2));      // cauteloso 960
        Assert.Equal("cauteloso", PersonalitySystem.Target(C, "pacifico", w));
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Mudanca_SoComConfirmacao_ERegistrada()
    {
        var p = PopulationGenerator.Generate(1, C, 3);
        p.PersonalityId = "pacifico";
        foreach (var a in PersonalitySystem.Attributes) Set(p.Traits, a, At(a, 500));
        Set(p.Traits, "riskAversion", At("riskAversion", 990));   // alvo: cauteloso
        for (int d = 0; d < S.ConfirmDays; d++) Assert.Null(PersonalitySystem.OnDaily(C, p, d));
        Assert.Equal("pacifico", p.PersonalityId);
        var change = PersonalitySystem.OnDaily(C, p, S.ConfirmDays);
        Assert.NotNull(change);
        Assert.Equal(("pacifico", "cauteloso", "riskAversion"), (change!.From, change.To, change.Attribute));
        Assert.Equal("cauteloso", p.PersonalityId);
        Assert.Single(p.PersonalityHistory);
        // Alvo que se desfaz antes de confirmar: a contagem recomeca.
        var q = PopulationGenerator.Generate(2, C, 3);
        q.PersonalityId = PersonalitySystem.Balanced;
        foreach (var a in PersonalitySystem.Attributes) Set(q.Traits, a, At(a, 500));
        Set(q.Traits, "variety", At("variety", 980));
        for (int d = 0; d < S.ConfirmDays - 2; d++) PersonalitySystem.OnDaily(C, q, d);
        Set(q.Traits, "variety", At("variety", 500));
        PersonalitySystem.OnDaily(C, q, S.ConfirmDays - 2);
        Set(q.Traits, "variety", At("variety", 980));
        for (long d = S.ConfirmDays - 1; d < 2 * S.ConfirmDays - 1; d++) Assert.Null(PersonalitySystem.OnDaily(C, q, d));
        Assert.Equal(PersonalitySystem.Balanced, q.PersonalityId);
    }

    /// <summary>Capacidades, especialidades, preferencias e humor nao mudam a personalidade.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void SoAtributosDePersonalidadeContam()
    {
        var t = Neutral();
        Set(t, "aggressiveness", At("aggressiveness", 30));
        string before = PersonalitySystem.Classify(C, t);
        t.MechanicalSkill = 1000; t.GameKnowledge = 0; t.Pve = 1000; t.Competitiveness = 1000; t.Opportunism = 1000;
        Assert.Equal(before, PersonalitySystem.Classify(C, t));
        Assert.Equal(before, PersonalitySystem.Target(C, before, t));
    }

    private static ActivityScoring.PersonalityContext Ctx(string type = ActivityTypes.Hunt, bool pvpZone = false, bool needed = false, int count = 0,
        int crowd = 0, int crowding = 0, bool step = false, bool same = false, int repeatMinutes = 0, bool first = false, int remaining = 200,
        int challenge = 5, int level = 5, int fearless = 0) =>
        new(type, pvpZone, needed, count, crowd, crowding, step, same, repeatMinutes, first, remaining, challenge, level, fearless);

    private static PersonalityTypeDefinition T(string id) => S.Types.Single(x => x.Id == id);

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Regras_FatoresDaPersonalidade()
    {
        var r = S.Rules;
        int Rule(string id, ActivityScoring.PersonalityContext x) => ActivityScoring.PersonalityRule(T(id), r, x);
        Assert.Equal(-r.NeverPenalty, Rule("pacifico", Ctx(ActivityTypes.Ambush)));
        Assert.Equal(-r.PvpZonePenalty, Rule("pacifico", Ctx(pvpZone: true)));
        Assert.Equal(0, Rule("pacifico", Ctx(pvpZone: true, needed: true)));                // missao ou meta pede: alternativa valida
        Assert.Equal(r.ConflictBonus, Rule("agressivo", Ctx(ActivityTypes.Patrol)));
        Assert.Equal(100 * r.CrowdReliefPermille / 1000, Rule("agressivo", Ctx(crowding: -100)));
        Assert.Equal(r.SocialBonus, Rule("sociavel", Ctx(count: r.SocialMinPeople, first: true)));
        Assert.Equal(0, Rule("sociavel", Ctx(count: r.SocialMinPeople)));                    // meio da sessao
        Assert.Equal(-r.NeverPenalty, Rule("reservado", Ctx(crowd: r.CrowdLimitPermille)));
        Assert.Equal(r.QuestStepBonus, Rule("paciente", Ctx(step: true)));
        // Inquieto: minutos seguidos na mesma atividade (nao ciclos); outra atividade nao e penalizada.
        Assert.Equal(-r.RestlessPenalty, Rule("inquieto", Ctx(same: true, repeatMinutes: r.RestlessMinutes)));
        Assert.Equal(0, Rule("inquieto", Ctx(same: true, repeatMinutes: r.RestlessMinutes - 1)));
        Assert.Equal(0, Rule("inquieto", Ctx(same: false, repeatMinutes: r.RestlessMinutes * 3)));
        Assert.Equal(-r.NeverPenalty, Rule("cauteloso", Ctx(challenge: 5 + r.MaxLevelsAbove + 1)));
        Assert.Equal(0, Rule("cauteloso", Ctx(challenge: 5 + r.MaxLevelsAbove)));
        Assert.Equal(37, Rule("destemido", Ctx(fearless: 37)));
        Assert.Equal(-r.SessionStartBonus, Rule("versatil", Ctx(first: true, same: true)));
        Assert.Equal(r.SessionStartBonus, Rule("metodico", Ctx(first: true, same: true)));
        Assert.Equal(0, Rule("metodico", Ctx(first: false, same: true)));
        // Equilibrado e regras que agem em outros sistemas: nada na nota.
        Assert.Equal(0, ActivityScoring.PersonalityRule(null, r, Ctx(ActivityTypes.Ambush)));
        Assert.Equal(0, Rule("leal", Ctx(ActivityTypes.Ambush)));
        Assert.Equal(0, Rule("independente", Ctx(ActivityTypes.Ambush)));
    }

    /// <summary>Uma opcao com "nunca" continua na lista (alternativa valida se for a unica).</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Regra_NaoTiraOpcoes()
    {
        var e = TestSupport.NewEngine(9, 60);
        e.Submit(new Simulation.Core.Commands.SetServerOpenCommand(true), Simulation.Core.Time.SimTime.Hours(8));
        e.AdvanceTo(Simulation.Core.Time.SimTime.Days(2));
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0 && !x.IsOnline);
        var ch = w.GetCharacter(p.CharacterId);
        ch.SessionEndTick = w.Clock.Tick + Simulation.Core.Time.SimTime.Hours(4);
        foreach (var r in w.Content.Regions.Keys) ch.Known.Add(r);
        ch.Level = 1;
        List<ScoredOption> Eval(PersonalityTypeDefinition? pt) => ActivityScoring.Evaluate(ch, p.Traits, p.Preferences, w.Content, w.Behavior.Routes,
            w.OccupancyOf, w.Clock.Tick, new DeterministicRng(4), w.Behavior.Quests, w.Behavior.Goals, p.PlaceImpressions, pt);
        var none = Eval(null);
        var cautious = Eval(T("cauteloso"));
        Assert.Equal(none.Select(o => o.Key).OrderBy(k => k, StringComparer.Ordinal), cautious.Select(o => o.Key).OrderBy(k => k, StringComparer.Ordinal));
        Assert.Contains(cautious, o => o.Factors.Any(f => f.Key == "personality" && f.Contribution == -S.Rules.NeverPenalty));
    }

    /// <summary>
    /// Inquieto: a repeticao e tempo simulado seguido na mesma atividade, dentro da sessao. Nunca
    /// passa do tempo da sessao e zera ao trocar de atividade e ao comecar outra sessao.
    /// </summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Inquieto_RepeticaoEmMinutosDentroDaSessao()
    {
        var e = TestSupport.NewEngine(11, 60);
        e.Submit(new Simulation.Core.Commands.SetServerOpenCommand(true), Simulation.Core.Time.SimTime.Hours(8));
        var w = e.World;
        int checkedOnline = 0, positive = 0;
        for (int h = 9; h <= 72; h++)
        {
            e.AdvanceTo(Simulation.Core.Time.SimTime.Hours(h));
            foreach (var ch in w.Characters)
            {
                if (ch.Status == CharacterStatus.Offline) continue;
                checkedOnline++;
                long sessionMinutes = (w.Clock.Tick - ch.SessionStartTick) / Simulation.Core.Time.SimTime.TicksPerMinute;
                Assert.InRange(ch.RepeatMinutes, 0, sessionMinutes);
                if (ch.RepeatMinutes > 0) positive++;
            }
        }
        Assert.True(checkedOnline > 50 && positive > 0, $"online {checkedOnline}, com repeticao {positive}");
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Independente_SoMissoesDoProprioGosto()
    {
        var w = TestSupport.NewEngine(3, 20).World;
        var quests = w.Behavior.Quests;
        var hunt = C.Quests.Values.First(q => q.Steps.Count > 0 && q.Steps.All(s => C.Regions[s.RegionId].FindActivity(s.ActivityId)?.Type == ActivityTypes.Hunt));
        var p = PopulationGenerator.Generate(1, C, 1);
        Assert.False(quests.FitsTaste(new PersonPreferences { Pve = S.Rules.OwnPathMinPreference - 1 }, hunt));
        Assert.True(quests.FitsTaste(new PersonPreferences { Pve = S.Rules.OwnPathMinPreference }, hunt));
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Leal_NaoDesisteDeMetaPorImpaciencia()
    {
        var e = TestSupport.NewEngine(5, 40);
        e.Submit(new Simulation.Core.Commands.SetServerOpenCommand(true), Simulation.Core.Time.SimTime.Hours(8));
        e.AdvanceTo(Simulation.Core.Time.SimTime.Days(2));
        var w = e.World;
        var p = w.Persons.First(x => x.CharacterId > 0);
        var ch = w.GetCharacter(p.CharacterId);
        p.Traits.Patience = 0;   // impaciente: sem a regra, desistiria por sorteio
        int Abandons(string personality)
        {
            p.PersonalityId = personality;
            int n = 0;
            for (int i = 0; i < 200; i++)
            {
                ch.GoalList.Clear();
                ch.GoalList.Add(new PersonalGoal(GoalTypes.PracticeActivity, ActivityTypes.Hunt, 300, 0, 800, "pref_pve", 500, w.Clock.Tick, ch.PlayedTicksAt(w.Clock.Tick)));
                w.Behavior.Goals.Review(p, ch, new DeterministicRng((ulong)(i + 1)));
                if (!ch.GoalList.Any(g => g.Type == GoalTypes.PracticeActivity && g.Target == ActivityTypes.Hunt && g.TargetValue == 300)) n++;
            }
            return n;
        }
        Assert.Equal(0, Abandons("leal"));
        Assert.True(Abandons("independente") > 0);
    }

    /// <summary>Ligar a personalidade nao muda nenhum atributo, preferencia ou ocupacao (calculo sem sorteio).</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Personalidade_NaoMudaOsDemaisAtributos()
    {
        var set = TestSupport.FreshPresetSet();
        set.Population.Personality.Enabled = false;
        var off = ContentDatabase.Create(set);
        for (int id = 1; id <= 1500; id++)
        {
            var a = PopulationGenerator.Generate(id, C, 21);
            var b = PopulationGenerator.Generate(id, off, 21);
            Assert.Equal("", b.PersonalityId);
            Assert.NotEqual("", a.PersonalityId);
            foreach (var k in PersonTraits.DynamicKeys) Assert.Equal(a.Traits.Get(k), b.Traits.Get(k));
            foreach (var k in PersonPreferences.Keys) Assert.Equal(a.Preferences.Get(k), b.Preferences.Get(k));
            Assert.Equal(a.OccupationId, b.OccupationId);
        }
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Validador_Personalidade()
    {
        static bool Err(Action<ContentSet> change, string path)
        {
            var set = TestSupport.FreshPresetSet();
            change(set);
            return ContentValidator.Validate(set).Any(i => i.Severity == IssueSeverity.Error && i.Path.Contains(path));
        }
        Assert.False(Err(_ => { }, "personality"));
        Assert.True(Err(s => s.Population.Personality.ChangeMarginPermille = 800, "personality.changeMarginPermille"));
        Assert.True(Err(s => s.Population.Personality.ConfirmDays = 0, "personality.confirmDays"));
        Assert.True(Err(s => s.Population.Personality.Types[0].Rule = "voar", "personality.types[0].rule"));
        Assert.True(Err(s => s.Population.Personality.Types[1].Direction = s.Population.Personality.Types[0].Direction, "personality.types[1]"));
        Assert.True(Err(s => s.Population.Traits.Patience = new TraitRange { Mean = 500, Spread = 300 }, "traits.patience"));
    }
}

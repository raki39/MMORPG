using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Time;
using Simulation.Infrastructure.Validation;

namespace Simulation.Tests;

/// <summary>Progressao no conteudo (D-027): curva, faixas de taxa, desafio e teto.</summary>
public class ProgressionTests
{
    private static ProgressionDefinition P => TestSupport.Preset.Progression;

    [Fact]
    public void Curva_CresceComONivel_EFaixasReduzemATaxa()
    {
        for (int n = 1; n < P.LevelCap - 1; n++)
            Assert.True(ProgressionRules.XpToNextLevel(P, n + 1) > ProgressionRules.XpToNextLevel(P, n), $"nível {n}");
        Assert.Equal(30L * 10 * 10 + 190L * 10, ProgressionRules.XpToNextLevel(P, 10));

        // Cada faixa rende menos que a anterior; o valor vale a partir de fromLevel.
        for (int i = 1; i < P.Stages.Count; i++)
        {
            var s = P.Stages[i];
            Assert.True(s.RatePermille < P.Stages[i - 1].RatePermille);
            Assert.Equal(P.Stages[i - 1].RatePermille, ProgressionRules.StageRate(P, s.FromLevel - 1));
            Assert.Equal(s.RatePermille, ProgressionRules.StageRate(P, s.FromLevel));
        }
    }

    [Fact]
    public void Desafio_DefineOXp_ECidadeNaoDaXp()
    {
        var c = TestSupport.Preset;
        var vila = c.Regions["vila_aurora"];
        foreach (var a in vila.AllActivities())
            Assert.Equal(0, ProgressionRules.MilliXpPerMinute(c, a, 5));

        // Caca sem desafio declarado usa a media das criaturas (lobo cinzento = 3).
        var lobos = c.Regions["floresta_sussurrante"].FindActivity("cacar_lobos")!;
        Assert.Equal(0, lobos.ChallengeLevel);
        Assert.Equal(3, ProgressionRules.ChallengeLevel(c, lobos));

        // Desafio mais alto rende mais por minuto para quem esta no nivel dele.
        var goblins = c.Regions["mina_velha"].FindActivity("cacar_goblins")!;
        int g = ProgressionRules.ChallengeLevel(c, goblins);
        Assert.True(ProgressionRules.XpFor(c, goblins, g, 60) * ProgressionRules.StageRate(P, 3)
                    > ProgressionRules.XpFor(c, lobos, 3, 60) * ProgressionRules.StageRate(P, g));
    }

    [Fact]
    public void Rendimento_CaiLongeDoDesafio_ComTolerancia()
    {
        var d = P.LevelDifference;
        Assert.Equal(1000, ProgressionRules.Efficiency(P, 10, 10));
        Assert.Equal(1000, ProgressionRules.Efficiency(P, 10 + d.ToleranceLevels, 10));
        Assert.Equal(1000, ProgressionRules.Efficiency(P, 10 - d.ToleranceLevels, 10));
        Assert.Equal(1000 - d.WeakChallengePenaltyPermille, ProgressionRules.Efficiency(P, 11 + d.ToleranceLevels, 10));
        Assert.Equal(1000 - d.StrongChallengePenaltyPermille, ProgressionRules.Efficiency(P, 9 - d.ToleranceLevels, 10));
        Assert.Equal(d.WeakChallengeFloorPermille, ProgressionRules.Efficiency(P, 29, 1));
        Assert.Equal(0, ProgressionRules.Efficiency(P, 1, 20));
    }

    [Fact]
    public void NoTeto_NaoGanhaXp()
    {
        var c = TestSupport.Preset;
        var lobos = c.Regions["floresta_sussurrante"].FindActivity("cacar_lobos")!;
        Assert.Equal(0, ProgressionRules.XpFor(c, lobos, P.LevelCap, 60));
        Assert.True(ProgressionRules.XpFor(c, lobos, P.LevelCap - 1, 600) >= 0);
    }

    [Fact]
    public void Previa_ComecoRapido_EDesaceleraNasFaixas()
    {
        var rows = ProgressionPreview.Build(TestSupport.Preset);
        Assert.Equal(P.LevelCap - 1, rows.Count);
        Assert.True(rows[0].Hours < 0.5, "o primeiro nível deve levar menos de 30 min");
        for (int i = 1; i < rows.Count; i++)
            Assert.True(rows[i].CumulativeHours > rows[i - 1].CumulativeHours);
        // Na troca de faixa o tempo por nivel salta (a taxa cai); dentro da faixa cresce ~4% por nivel.
        foreach (var s in P.Stages.Skip(1))
            Assert.True(rows[s.FromLevel - 1].Hours > rows[s.FromLevel - 2].Hours * 1.1, $"faixa {s.FromLevel}");
        string md = ProgressionPreview.ToMarkdown(TestSupport.Preset);
        Assert.Contains("Taxa da faixa", md);
    }

    [Fact]
    public void Validador_ApontaErrosDeProgressaoEDesafio()
    {
        var set = TestSupport.FreshPresetSet();
        set.Progression.LevelCap = 1;
        set.Progression.XpCurve.Quadratic = -5;
        set.Progression.Stages = new List<XpStageDefinition> { new() { FromLevel = 2, RatePermille = 1000 }, new() { FromLevel = 2, RatePermille = 0 } };
        set.Progression.ActivityXpPerMinute["voar"] = 3;
        set.Progression.ActivityXpPerMinute[ActivityTypes.Trade] = 3;
        var vila = set.Regions.First(r => r.Id == "vila_aurora");
        vila.Areas.SelectMany(a => a.Activities).First(a => a.Type == ActivityTypes.Quest).ChallengeLevel = 3;
        var floresta = set.Regions.First(r => r.Id == "floresta_sussurrante");
        floresta.Activities.First(a => a.Type == ActivityTypes.Explore).ChallengeLevel = 0;

        var errors = ContentValidator.Validate(set).Where(i => i.Severity == IssueSeverity.Error).Select(i => i.File + ":" + i.Path + " " + i.Message).ToList();
        Assert.Contains(errors, e => e.Contains("progression.json:levelCap"));
        Assert.Contains(errors, e => e.Contains("progression.json:xpCurve"));
        Assert.Contains(errors, e => e.Contains("stages[0].fromLevel"));
        Assert.Contains(errors, e => e.Contains("stages[1].fromLevel"));
        Assert.Contains(errors, e => e.Contains("stages[1].ratePermille"));
        Assert.Contains(errors, e => e.Contains("activityXpPerMinute.voar"));
        Assert.Contains(errors, e => e.Contains("activityXpPerMinute.trade"));
        Assert.Contains(errors, e => e.Contains("regions.json") && e.Contains("challengeLevel") && e.Contains("cidade"));
        Assert.Contains(errors, e => e.Contains("regions.json") && e.Contains("challengeLevel") && e.Contains("explore"));
    }

    [Fact]
    public void Simulacao_RespeitaOTeto_EPrimeirosNiveisChegamCedo()
    {
        var e = TestSupport.NewEngine(11, 150);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(14));
        var chars = e.World.Characters;
        Assert.All(chars, c => Assert.InRange(c.Level, 1, P.LevelCap));
        // O comeco apresenta o jogo: a mediana de quem jogou ao menos 3 horas ja passou do nivel 3.
        var levels = chars.Where(c => c.PlayedTicksAt(e.World.Clock.Tick) >= SimTime.Hours(3)).Select(c => c.Level).OrderBy(l => l).ToList();
        Assert.True(levels.Count > 20, $"amostra pequena: {levels.Count}");
        Assert.True(levels[levels.Count / 2] >= 4, $"mediana {levels[levels.Count / 2]}");
    }
}

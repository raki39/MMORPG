using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Population;
using Simulation.Core.Time;

namespace Simulation.Tests;

/// <summary>Compromissos ocasionais nos dias livres (D-046): festa, visita a familia, passeio...</summary>
public class CommitmentTests
{
    private static ContentDatabase C => TestSupport.Preset;

    /// <summary>
    /// O compromisso de um dia nao muda depois (D-067a): ele e da vida real e usa a sociabilidade sorteada,
    /// nao a do jogo. Antes, a sociabilidade que muda com o tempo mudava o compromisso de um dia ja vivido.
    /// </summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Compromisso_DoDia_NaoMudaQuandoASociabilidadeDoJogoMuda()
    {
        var e = TestSupport.NewEngine(21, 300);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(1));
        var w = e.World;
        int found = 0;
        foreach (var p in w.Persons.OrderBy(x => x.Id))
        {
            for (long d = 1; d < 30 && found < 20; d++)
            {
                var before = w.Behavior.LayoutOf(p, d).Commitments.ToList();
                if (before.Count == 0) continue;
                int soc = p.Traits.Sociability;
                foreach (int v in new[] { 0, 1000 })
                {
                    p.Traits.Sociability = v;
                    Assert.Equal(before, w.Behavior.LayoutOf(p, d).Commitments.ToList());
                }
                p.Traits.Sociability = soc;
                found++;
            }
            if (found >= 20) break;
        }
        Assert.True(found >= 5, $"dias com compromisso conferidos: {found}");
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Compromisso_SoEmDiaLivre_Raro_ESemSessaoDurante()
    {
        var e = TestSupport.NewEngine(21, 600);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        var w = e.World;
        int checkedStarts = 0;
        for (int d = 0; d < 42; d++)
        {
            e.AdvanceTo(SimTime.Days(d + 1));
            foreach (var ev in w.Events.Drain().Where(x => x.Type == "session_started"))
            {
                var p = w.GetPerson((int)ev.Entities[0].Id);
                long localDay = BehaviorSystem.LocalDay(p, ev.Tick);
                int minute = (int)((ev.Tick - BehaviorSystem.LocalDayStart(p, localDay)) / SimTime.TicksPerMinute);
                var day = w.Behavior.LayoutOf(p, localDay);
                Assert.DoesNotContain(day.Commitments, c => minute >= c.Start && minute < c.End);
                checkedStarts++;
            }
        }
        Assert.True(checkedStarts > 1000, $"amostra {checkedStarts}");
        var plans = w.Persons.Where(p => p.CharacterId > 0).SelectMany(p => p.DayPlans).ToList();
        var withCommitment = plans.Where(x => x.CommitmentId.Length > 0).ToList();
        Assert.All(withCommitment, x => Assert.True(x.FreeDay));
        Assert.All(withCommitment, x => Assert.Contains(C.Routine.Commitments, k => k.Id == x.CommitmentId));
        double rate = withCommitment.Count / (double)Math.Max(1, plans.Count(x => x.FreeDay));
        Assert.InRange(rate, 0.02, 0.15);   // possibilidade pequena, mas existe
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Compromisso_EDeterministico_EMaisComumParaQuemESociavel()
    {
        var e = TestSupport.NewEngine(5, 2000);
        var w = e.World;
        int Count(IEnumerable<Person> group) => group.Sum(p => Enumerable.Range(0, 120).Count(d => w.Behavior.LayoutOf(p, d).Commitments.Count > 0));
        var social = w.Persons.Where(p => p.Traits.Sociability >= 650).ToList();
        var reserved = w.Persons.Where(p => p.Traits.Sociability <= 350).ToList();
        Assert.True(social.Count > 100 && reserved.Count > 100, $"amostra {social.Count}/{reserved.Count}");
        Assert.True(Count(social) / (double)social.Count > Count(reserved) / (double)reserved.Count);
        // O mesmo dia sempre da o mesmo compromisso (nao depende de quando a agenda e consultada).
        var p0 = w.Persons.First(p => Enumerable.Range(0, 120).Any(d => w.Behavior.LayoutOf(p, d).Commitments.Count > 0));
        int day0 = Enumerable.Range(0, 120).First(d => w.Behavior.LayoutOf(p0, d).Commitments.Count > 0);
        Assert.Equal(w.Behavior.LayoutOf(p0, day0).Commitments, w.Behavior.LayoutOf(p0, day0).Commitments);
        Assert.Equal(w.Behavior.LayoutOf(p0, day0).FreeMinutes, w.Behavior.LayoutOf(p0, day0).FreeMinutes);
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Compromisso_TiraTempoLivre_ENaoInvadeOSono()
    {
        var p = PopulationGenerator.Generate(1, C, 1);
        p.OccupationId = "comercial";
        var rs = C.Routine;
        var free = DailySchedule.Build(p, C.Occupations[p.OccupationId], rs, 5, false, false, false);
        var party = DailySchedule.Build(p, C.Occupations[p.OccupationId], rs, 5, false, false, false, (20 * 60, 24 * 60 + 30, "festa"));
        Assert.Single(party.Commitments);
        Assert.True(party.FreeMinutes < free.FreeMinutes);
        Assert.True(party.Commitments[0].End <= party.Bed);   // a festa termina antes de dormir
        Assert.Equal(free.Bed, party.Bed);
        Assert.DoesNotContain(party.Windows, x => x.Start < party.Commitments[0].End && party.Commitments[0].Start < x.End);
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void CompromissosInvalidos_SaoErro()
    {
        var set = TestSupport.FreshPresetSet();
        set.Population.Routine.Commitments[0].StartFrom = "25:00";
        set.Population.Routine.Commitments[1].MinMinutes = 500;
        set.Population.Routine.Commitments[1].MaxMinutes = 100;
        var errors = ContentValidator.Validate(set).Where(x => x.Severity == IssueSeverity.Error).ToList();
        Assert.Contains(errors, x => x.Path == "routine.commitments[0].startFrom");
        Assert.Contains(errors, x => x.Path == "routine.commitments[1].minMinutes");
    }
}

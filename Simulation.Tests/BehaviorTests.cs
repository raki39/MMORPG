using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Random;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

public class BehaviorTests
{
    private static SimulationEngine OpenedWorld(ulong seed = 42, int population = 100)
    {
        var e = TestSupport.NewEngine(seed, population);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        return e;
    }

    [Fact]
    public void Lancamento_TodosSeRegistramNaPrimeiraSemana()
    {
        var e = OpenedWorld();
        e.AdvanceTo(SimTime.Days(8));
        var w = e.World;
        Assert.Equal(100, w.Accounts.Count);
        Assert.Equal(100, w.Characters.Count);
        Assert.All(w.Persons, p => Assert.NotEqual(LifecycleState.PotentialInterested, p.State));
        Assert.All(w.Characters, c => Assert.True(w.Content.Classes.ContainsKey(c.ClassId)));
        // Aquisicao progressiva: nao entram todos no primeiro dia.
        Assert.InRange(w.History[0].Registrations, 1, 60);
    }

    [Fact]
    public void Servidor_Fechado_NinguemSeRegistra()
    {
        var e = TestSupport.NewEngine();
        e.AdvanceTo(SimTime.Days(3));
        Assert.Empty(e.World.Accounts);
    }

    [Fact]
    public void Sessoes_ComecamPertoDoHorarioPreferido()
    {
        var e = OpenedWorld();
        var starts = new List<(int PersonId, DateTime At)>();
        for (int d = 0; d < 14; d++)
        {
            e.AdvanceTo(SimTime.Days(d + 1));
            foreach (var ev in e.World.Events.Drain().Where(x => x.Type == "session_started"))
            {
                var p = e.World.GetPerson((int)ev.Entities[0].Id);   // horario local (P1)
                starts.Add((p.Id, e.World.Clock.Calendar.ToDateTime(ev.Tick + p.LocalOffsetMinutes * SimTime.TicksPerMinute)));
            }
        }
        // Quem nao tem obrigacoes (P2): o horario preferido cabe no tempo livre.
        var weekday = starts.Where(s => e.World.CalendarOf(e.World.GetPerson(s.PersonId)).KindOf(s.At.Date) == DayKind.Weekday
            && e.World.GetPerson(s.PersonId).OccupationId is "sem_ocupacao" or "aposentado").ToList();
        Assert.True(weekday.Count > 20, $"amostra pequena: {weekday.Count}");
        int near = weekday.Count(s =>
        {
            int preferred = e.World.GetPerson(s.PersonId).HabitualStartHour * 60;
            int minute = s.At.Hour * 60 + s.At.Minute;
            return Math.Abs(minute - preferred) <= TestSupport.Preset.Behavior.SessionStartJitterMinutes + 5;
        });
        // Tolerancia: o primeiro acesso (registro) pode cair fora da janela por chegar atrasado.
        Assert.True(near * 100 / weekday.Count >= 85, $"{near}/{weekday.Count} perto do horário preferido");
    }

    [Fact]
    public void FimDeSemana_TemMaisJogadoresQueDiaUtil()
    {
        var e = OpenedWorld();
        e.AdvanceTo(SimTime.Days(14));
        var h = e.World.History.Skip(7).ToList(); // apos o lancamento
        double weekend = h.Where(d => d.DayKind != DayKind.Weekday).Average(d => d.ActivePlayers);
        double weekday = h.Where(d => d.DayKind == DayKind.Weekday).Average(d => d.ActivePlayers);
        Assert.True(weekend > weekday, $"fim de semana {weekend:F1} x dia útil {weekday:F1}");
    }

    [Fact]
    public void Conhecimento_Comeca_NaCidadeEVizinhas_EAvancaExplorando()
    {
        var e = OpenedWorld();
        e.AdvanceTo(SimTime.Hours(8) + SimTime.Minutes(1));
        while (e.World.Characters.Count == 0) e.AdvanceTo(e.World.Clock.Tick + SimTime.Minutes(30));
        var first = e.World.Characters[0];
        // Logo apos o registro (antes de sair da cidade): cidade + zonas ligadas a ela.
        if (first.DecisionCount <= 1 && first.RegionId == "vila_aurora")
            Assert.Equal(new[] { "colinas_do_vale", "floresta_sussurrante", "ravina_escarlate", "vila_aurora" }, first.KnownRegions.ToArray());

        e.AdvanceTo(SimTime.Days(10));
        // Ninguem conhece o Covil sem ter chegado as Colinas (exploracao) ou a Mina (saidas visiveis).
        foreach (var ch in e.World.Characters.Where(x => x.KnownRegions.Contains("covil_do_rei_goblin")))
            Assert.True(ch.KnownRegions.Contains("mina_velha"), $"{ch.Name} conhece o covil sem conhecer a mina");
    }

    [Fact]
    public void Decisoes_RespeitamConhecimentoNivelEGrupo()
    {
        var e = OpenedWorld();
        var w = e.World;
        for (int step = 1; step <= 14 * 24; step++)
        {
            e.AdvanceTo(SimTime.Hours(step));
            foreach (var c in w.Characters.Where(x => x.LastDecision != null && x.LastDecision.Tick > w.Clock.Tick - SimTime.TicksPerHour))
            {
                var d = c.LastDecision!;
                var a = w.Content.Regions[d.RegionId].FindActivity(d.ActivityId)!;
                Assert.Contains(d.RegionId, c.KnownRegions);
                Assert.True(a.MinLevel <= c.Level);
                Assert.Equal(1, a.MinGroupSize);
                Assert.Contains(d.Factors, f => f.Key == "affinity");
            }
        }
    }

    [Fact]
    public void Ocupacao_BateComPersonagensEmAtividade()
    {
        var e = OpenedWorld();
        var w = e.World;
        for (int day = 1; day <= 10; day++)
        {
            e.AdvanceTo(SimTime.Days(day) + SimTime.Hours(21)); // horario de pico
            int expected = w.Characters.Count(c => (c.Status == CharacterStatus.Doing || c.Status == CharacterStatus.Traveling) && c.ActivityId.Length > 0);
            int counted = 0;
            foreach (var r in w.Content.Regions.Values)
                foreach (var a in r.AllActivities())
                    counted += w.OccupancyOf(r.Id, a.Id);
            Assert.Equal(expected, counted);
            Assert.Equal(w.Persons.Count(p => p.IsOnline), w.OnlineCount);
            Assert.All(w.Characters.Where(c => c.Status != CharacterStatus.Offline),
                c => Assert.True(w.Clock.Tick <= c.SessionEndTick + SimTime.Hours(2), $"{c.Name} passou do fim da sessão"));
        }
    }

    [Fact]
    public void Rotas_SoUsamConexoesDeclaradas()
    {
        var layout = new ZoneLayout(TestSupport.Preset);
        var routes = new RouteFinder(TestSupport.Preset, layout);
        Assert.Equal(new[] { "colinas_do_vale", "mina_velha", "covil_do_rei_goblin" }, routes.ZonePath("vila_aurora", "covil_do_rei_goblin"));
        Assert.Equal(new[] { "vila_aurora", "ravina_escarlate", "ruinas_sombrias" }, routes.ZonePath("floresta_sussurrante", "ruinas_sombrias"));
        // Zonas encostadas sem passagem: o caminho contorna por uma cidade.
        var c = TestSupport.Preset.Regions;
        bool Touching(string a, string b)
        {
            int dx = c[a].MapPosition.X - c[b].MapPosition.X, dy = c[a].MapPosition.Y - c[b].MapPosition.Y;
            return Math.Sqrt(dx * dx + dy * dy) < 2.1 * TestSupport.Preset.World.ZoneRadius;
        }
        Assert.True(Touching("colinas_do_vale", "ravina_escarlate"), "deveriam ser vizinhas");
        var viaCity = routes.ZonePath("colinas_do_vale", "ravina_escarlate")!;
        Assert.Equal(2, viaCity.Count);
        Assert.True(c[viaCity[0]].IsCity, viaCity[0]);
        // O brejo encosta na Cidadela sem passagem: chega-se pela Mata ou pelo Planalto.
        Assert.True(Touching("brejo_das_brumas", "cidadela_de_cinzaforte"));
        var toCitadel = routes.ZonePath("brejo_das_brumas", "cidadela_de_cinzaforte")!;
        Assert.Equal(2, toCitadel.Count);
        Assert.Contains(toCitadel[0], new[] { "mata_dos_espinhos", "planalto_dos_ventos" });
    }

    [Fact]
    public void Viagem_TempoVemDaGeometria()
    {
        var layout = new ZoneLayout(TestSupport.Preset);
        var routes = new RouteFinder(TestSupport.Preset, layout);
        var start = layout.Center("vila_aurora");
        var target = layout.Spot("floresta_sussurrante", "cacar_lobos");
        var plan = routes.Plan(start, "vila_aurora", "floresta_sussurrante", target);
        Assert.True(plan.Reachable);
        // Encruzilhada da vila -> saida, cruza para a floresta, estrada ate a encruzilhada, trilha ate o ponto (comprimento pelas estradas, D-030).
        Assert.Equal(4, plan.Waypoints.Count);
        Assert.Equal(layout.Gate("vila_aurora", "floresta_sussurrante"), plan.Waypoints[0].Pos);
        Assert.Equal("floresta_sussurrante", plan.Waypoints[1].EntersZone);
        Assert.Equal(layout.Hub("floresta_sussurrante"), plan.Waypoints[2].Pos);
        Assert.Equal(target, plan.Waypoints[^1].Pos);
        int sum = 0;
        var at = start;
        foreach (var wp in plan.Waypoints) { sum += layout.LegLength(at, wp.Pos); Assert.True(layout.LegLength(at, wp.Pos) >= at.DistanceTo(wp.Pos) - 1); at = wp.Pos; }
        Assert.Equal(sum, plan.Distance);
        Assert.Equal((sum + TestSupport.Preset.Behavior.WalkSpeed - 1) / TestSupport.Preset.Behavior.WalkSpeed, plan.Minutes);
        // Pontos de atividade ficam dentro da zona.
        foreach (var r in TestSupport.Preset.Regions.Values)
            foreach (var a in r.AllActivities())
                Assert.True(layout.Spot(r.Id, a.Id).DistanceTo(layout.Center(r.Id)) < layout.ZoneRadius, $"{r.Id}/{a.Id} fora da zona");
    }

    [Fact]
    public void Posicao_DuranteViagem_FicaNoTrecho()
    {
        var e = OpenedWorld();
        var w = e.World;
        bool checkedAny = false;
        for (int m = 9 * 60; m < 9 * 60 + 600 && !checkedAny; m += 3)
        {
            e.AdvanceTo(SimTime.Minutes(m) + SimTime.Days(1));
            foreach (var c in w.Characters.Where(x => x.Status == CharacterStatus.Traveling))
            {
                var p = c.PositionAt(w.Clock.Tick);
                Assert.InRange(p.X, Math.Min(c.Position.X, c.MoveTo.X), Math.Max(c.Position.X, c.MoveTo.X));
                Assert.InRange(p.Y, Math.Min(c.Position.Y, c.MoveTo.Y), Math.Max(c.Position.Y, c.MoveTo.Y));
                checkedAny = true;
            }
        }
        Assert.True(checkedAny);
    }

    [Fact]
    public void Risco_PesaMaisParaQuemTemAversao()
    {
        // Mesmo personagem (nivel 12, na cidade, sessao longa), dois perfis que so diferem na aversao a risco.
        var content = TestSupport.Preset;
        var routes = new RouteFinder(content, new ZoneLayout(content));
        int Gap(int riskAversion)
        {
            var ch = new Character(1, 1, "Teste", "guerreiro");
            SetupCharacter(ch, routes.Layout.Center("vila_aurora"));
            var t = new PersonTraits { Pve = 800, RiskAversion = riskAversion, Patience = 500, Sociability = 500, Socializing = 500 };
            var opts = ActivityScoring.Evaluate(ch, t, content, routes, (_, _) => 0, 0, new DeterministicRng(7));
            int ruins = opts.First(o => o.Key == "ruinas_sombrias/cacar_mortos_vivos").Utility;
            int mine = opts.First(o => o.Key == "mina_velha/cacar_goblins").Utility;
            return mine - ruins; // quanto a mina (segura) fica a frente das ruinas (PvP)
        }
        Assert.True(Gap(900) > Gap(100) + 100, $"avesso {Gap(900)} x ousado {Gap(100)}");
    }

    private static void SetupCharacter(Character ch, MapPos at)
    {
        ch.Level = 12;
        ch.RegionId = "vila_aurora";
        ch.Position = at;
        ch.SessionEndTick = SimTime.Hours(4);
        foreach (var r in TestSupport.Preset.Regions.Keys) ch.Known.Add(r);
    }

    [Fact]
    public void Determinismo_ComComportamento_SaltoIgualTickATick()
    {
        var skip = OpenedWorld();
        var full = OpenedWorld();
        full.DisableTickSkipping = true;
        long t = SimTime.Days(1) + SimTime.Hours(2);
        skip.AdvanceTo(t);
        full.AdvanceTo(t);
        Assert.True(skip.World.Characters.Count > 0);
        Assert.Equal(StateHasher.Compute(full.World), StateHasher.Compute(skip.World));
    }
}

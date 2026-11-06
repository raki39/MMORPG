using Simulation.Core.Behavior;
using Simulation.Core.Combat;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Tests;

/// <summary>
/// Etapa 1a (D-061): estatisticas do personagem pela classe e o nivel, limites do critico e do nivel,
/// recurso pelo tipo, velocidade do personagem, indice de poder e classes personalizadas sem regra por nome.
/// </summary>
public class CharacterStatsTests
{
    private static ContentDatabase C => TestSupport.Preset;

    private static List<ValidationIssue> Errors(ContentSet set) =>
        ContentValidator.Validate(set).Where(i => i.Severity == IssueSeverity.Error).ToList();

    private static CombatStats Stats(int health, int pa, int ma, int pd, int md, int chance = 0, int mult = 1000, int speed = 15) => new()
    {
        Health = health, PhysicalAttack = pa, MagicAttack = ma, PhysicalDefense = pd, MagicDefense = md,
        CritChancePermille = chance, CritMultiplierPermille = mult, MoveSpeed = speed,
    };

    /// <summary>Classe nova feita so de conteudo: papel de suporte, recurso energia e habilidades de outra classe.</summary>
    private static ClassDefinition Bard() => new()
    {
        Id = "bardo", Version = 1, Name = "Bardo", Description = "Classe de teste.", Icon = "music", Role = ClassRoles.Support,
        Resource = new ClassResourceDefinition { Type = ResourceTypes.Energy, Name = "Inspiração" },
        BaseStats = new CombatStats { Health = 140, Resource = 100, PhysicalAttack = 8, MagicAttack = 16, PhysicalDefense = 12, MagicDefense = 18, CritChancePermille = 60, CritMultiplierPermille = 1600, MoveSpeed = 18 },
        PerLevel = new CombatStats { Health = 13, PhysicalAttack = 1, MagicAttack = 3, PhysicalDefense = 1, MagicDefense = 2 },
        AbilityIds = new List<string> { "punicao", "renovar" },
    };

    // ---------------- Estatisticas ----------------

    [Fact]
    public void Estatisticas_SaoBaseMaisCrescimentoPorNivel()
    {
        foreach (var cls in C.Classes.Values)
            foreach (int level in new[] { 1, 2, 10, 30 })
            {
                var sheet = CharacterStats.Sheet(C, cls, level);
                Assert.Equal(level, sheet.Level);
                foreach (var key in CombatStats.Keys)
                {
                    var line = sheet.Lines.Single(l => l.Key == key);
                    Assert.Equal(cls.BaseStats.Get(key), line.Base);
                    Assert.Equal(cls.PerLevel.Get(key) * (level - 1), line.FromLevels);
                    Assert.Equal(line.Base + line.FromLevels, line.Total);
                    Assert.False(line.Limited);
                    Assert.Equal(line.Total, sheet.Stats.Get(key));
                }
            }
        // Valores apresentados ao criador (nivel 30).
        var g30 = CharacterStats.Sheet(C, C.Classes["guerreiro"], 30).Stats;
        Assert.Equal(916, g30.Health);
        Assert.Equal(105, g30.PhysicalAttack);
        Assert.Equal(156, g30.PhysicalDefense);
        Assert.Equal(39, g30.MagicDefense);
    }

    [Fact]
    public void Nivel_LimitadoAoMaximoDaProgressao()
    {
        int cap = C.Progression.LevelCap;
        var cls = C.Classes["mago"];
        var atCap = CharacterStats.Sheet(C, cls, cap);
        var above = CharacterStats.Sheet(C, cls, cap + 25);
        Assert.Equal(cap, above.Level);
        foreach (var key in CombatStats.Keys) Assert.Equal(atCap.Stats.Get(key), above.Stats.Get(key));
        Assert.Equal(1, CharacterStats.Sheet(C, cls, 0).Level);
    }

    [Fact]
    public void Critico_LimitadoAosTetosDoCombate()
    {
        var set = TestSupport.FreshPresetSet();
        var cls = set.Classes.First(c => c.Id == "guerreiro");
        cls.BaseStats.CritChancePermille = 700;
        cls.PerLevel.CritChancePermille = 10;
        cls.BaseStats.CritMultiplierPermille = 4900;
        cls.PerLevel.CritMultiplierPermille = 50;
        Assert.Empty(Errors(set));   // a base esta dentro dos limites; o crescimento e cortado no calculo
        var db = ContentDatabase.Create(set);
        var sheet = CharacterStats.Sheet(db, db.Classes["guerreiro"], 30);
        Assert.Equal(db.Combat.CritChanceMaxPermille, sheet.Stats.CritChancePermille);
        Assert.Equal(db.Combat.CritMultiplierMaxPermille, sheet.Stats.CritMultiplierPermille);
        Assert.True(sheet.Lines.Single(l => l.Key == "critChancePermille").Limited);
        Assert.True(sheet.Lines.Single(l => l.Key == "critMultiplierPermille").Limited);
        // Nivel 1 ainda abaixo dos tetos.
        Assert.Equal(700, CharacterStats.Sheet(db, db.Classes["guerreiro"], 1).Stats.CritChancePermille);
    }

    [Fact]
    public void Recurso_PeloTipo_EVidaCheiaForaDeCombate()
    {
        var e = TestSupport.NewEngine(seed: 7, population: 120);
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(6));
        var chars = e.World.Characters;
        Assert.Contains(chars, ch => ch.Level > 1);
        foreach (var ch in chars)
        {
            var (hp, res) = CharacterStats.Maximums(C, ch);
            Assert.Equal(hp, ch.CurrentHealth);
            bool full = ResourceTypes.StartsFull(C.Classes[ch.ClassId].Resource.Type);
            Assert.Equal(full ? res : 0, ch.CurrentResource);
        }
        Assert.Contains(chars, ch => ch.ClassId == "guerreiro" && ch.CurrentResource == 0);   // furia comeca vazia
        Assert.Contains(chars, ch => ch.ClassId == "mago" && ch.CurrentResource > 0);
    }

    // ---------------- Velocidade ----------------

    [Fact]
    public void Velocidade_DoPersonagemDefineOTempoDeViagem()
    {
        // Presets: velocidade igual (15) para todas as classes, como antes (o tempo de viagem nao muda).
        Assert.All(C.Classes.Values, c => Assert.Equal(15, c.BaseStats.MoveSpeed));
        Assert.Equal(15, CharacterStats.ReferenceMoveSpeed(C));

        var set = TestSupport.FreshPresetSet();
        foreach (var cls in set.Classes) cls.BaseStats.MoveSpeed = cls.Id == "mago" ? 30 : 15;
        var db = ContentDatabase.Create(set);
        var e = WorldFactory.Create(db, new WorldConfig { Seed = 11, PopulationSize = 150 });
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var layout = new ZoneLayout(db);
        for (long t = SimTime.Hours(8) + SimTime.TicksPerMinute; t <= SimTime.Days(3) && seen.Count < 2; t += SimTime.TicksPerMinute * 7)
        {
            e.AdvanceTo(t);
            foreach (var ch in e.World.Characters)
            {
                if (ch.Status != CharacterStatus.Traveling) continue;
                long dist = layout.LegLength(ch.Position, ch.MoveTo);
                if (dist <= 0) continue;
                int speed = CharacterStats.MoveSpeed(db, ch);
                Assert.Equal(ch.ClassId == "mago" ? 30 : 15, speed);
                Assert.Equal(Math.Max(1, (dist * SimTime.TicksPerMinute + speed - 1) / speed), ch.MoveEndTick - ch.MoveStartTick);
                seen.Add(ch.ClassId == "mago" ? "rapido" : "normal");
            }
        }
        Assert.Equal(2, seen.Count);
    }

    // ---------------- Indice de poder ----------------

    [Fact]
    public void Poder_PersonagemDeReferenciaValeAEscala()
    {
        var r = C.Combat;
        var pi = r.PowerIndex;
        // Ataque igual a referencia, sem critico e sem defesa, vida igual a sobrevivencia de referencia.
        var p = PowerIndex.Compute(r, Stats(pi.ReferenceSurvivability, pi.ReferenceAttack, 0, 0, 0));
        Assert.Equal(pi.Scale, p.Power);
        Assert.Equal(1000, p.OffensePermille);
        Assert.Equal(1000, p.SurvivabilityPermille);
        // Dobrar so o ataque multiplica o poder por raiz de 2; dobrar ataque e vida dobra.
        Assert.Equal(141, PowerIndex.Compute(r, Stats(200, 44, 0, 0, 0)).Power);
        Assert.Equal(200, PowerIndex.Compute(r, Stats(400, 44, 0, 0, 0)).Power);
    }

    [Fact]
    public void Poder_NaoSomaAtaqueFisicoEMagicoInteiros()
    {
        var r = C.Combat;
        var both = PowerIndex.Compute(r, Stats(200, 20, 20, 0, 0));
        var single = PowerIndex.Compute(r, Stats(200, 20, 0, 0, 0));
        var sum = PowerIndex.Compute(r, Stats(200, 40, 0, 0, 0));
        Assert.True(both.Power > single.Power);
        Assert.True(both.Power < sum.Power);
        Assert.Equal(25_000, both.AttackMilli);                 // 20 + 20 x 25%
        // O secundario conta igual seja fisico ou magico.
        Assert.Equal(both.Power, PowerIndex.Compute(r, Stats(200, 20, 20, 0, 0)).Power);
        Assert.Equal(PowerIndex.Compute(r, Stats(200, 30, 6, 0, 0)).Power, PowerIndex.Compute(r, Stats(200, 6, 30, 0, 0)).Power);
    }

    [Fact]
    public void Poder_CriticoEDefesaEntramComUnidades()
    {
        var r = C.Combat;
        // 10% de chance com 150%: dano medio 1,05.
        var crit = PowerIndex.Compute(r, Stats(200, 22, 0, 0, 0, chance: 100, mult: 1500));
        Assert.Equal(1050, crit.CritFactorPermille);
        // Defesa media 100 (escala 100): vida efetiva dobra.
        var def = PowerIndex.Compute(r, Stats(200, 22, 0, 100, 100));
        Assert.Equal(400, def.Survivability);
        Assert.Equal(141, def.Power);
        // So defesa fisica conta pela metade (peso 50%).
        Assert.Equal(300, PowerIndex.Compute(r, Stats(200, 22, 0, 100, 0)).Survivability);
    }

    [Fact]
    public void Poder_CresceComONivel_EEDeterministico()
    {
        foreach (var cls in C.Classes.Values)
        {
            int prev = 0;
            foreach (int level in new[] { 1, 5, 10, 20, 30 })
            {
                var sheet = CharacterStats.Sheet(C, cls, level);
                int power = PowerIndex.Compute(C, sheet).Power;
                Assert.True(power > prev, $"{cls.Id} nível {level}: {power} <= {prev}");
                Assert.Equal(power, PowerIndex.Compute(C, CharacterStats.Sheet(C, cls, level)).Power);
                prev = power;
            }
            // Acima do teto nao cresce.
            Assert.Equal(prev, PowerIndex.Compute(C, CharacterStats.Sheet(C, cls, 99)).Power);
        }
    }

    [Fact]
    public void Poder_NominalSeparadoDaCondicao()
    {
        Assert.Equal(100, PowerIndex.AtHealth(100, 500, 500));
        Assert.Equal(71, PowerIndex.AtHealth(100, 250, 500));   // metade da vida: cerca de 71%
        Assert.Equal(0, PowerIndex.AtHealth(100, 0, 500));
        Assert.Equal(100, PowerIndex.AtHealth(100, 900, 500));  // nunca acima do nominal
    }

    [Fact]
    public void RaizInteira_Arredonda()
    {
        Assert.Equal((UInt128)0, PowerIndex.RoundSqrt(0));
        Assert.Equal((UInt128)10, PowerIndex.RoundSqrt(100));
        Assert.Equal((UInt128)10, PowerIndex.RoundSqrt(110));   // 10,49
        Assert.Equal((UInt128)11, PowerIndex.RoundSqrt(111));   // 10,54
        Assert.Equal((UInt128)1_000_000_000, PowerIndex.RoundSqrt((UInt128)1_000_000_000 * 1_000_000_000));
    }

    // ---------------- Classes personalizadas ----------------

    [Fact]
    public void ClassePersonalizada_CarregaEEhEscolhidaPeloPapel()
    {
        var set = TestSupport.FreshPresetSet();
        set.Classes.Add(Bard());
        Assert.Empty(Errors(set));   // habilidades de outra classe valem pela lista da classe nova
        var db = ContentDatabase.Create(set);
        var e = WorldFactory.Create(db, new WorldConfig { Seed = 3, PopulationSize = 300 });
        e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
        e.AdvanceTo(SimTime.Days(3));
        var bards = e.World.Characters.Where(ch => ch.ClassId == "bardo").ToList();
        Assert.NotEmpty(bards);   // entra no mesmo sorteio por papel que as classes de teste
        foreach (var ch in bards)
        {
            var sheet = CharacterStats.Sheet(db, ch);
            Assert.Equal(sheet.Stats.Health, ch.CurrentHealth);
            Assert.Equal(sheet.Stats.Resource, ch.CurrentResource);   // energia comeca cheia
            Assert.Equal(18, CharacterStats.MoveSpeed(db, ch));
        }
        Assert.Contains(bards, ch => ch.PlayedTicks > 0);   // joga normalmente
    }

    [Fact]
    public void Validador_Classes()
    {
        ContentSet Fresh(Action<ClassDefinition> change)
        {
            var s = TestSupport.FreshPresetSet();
            change(s.Classes[0]);
            return s;
        }
        Assert.Contains(Errors(Fresh(c => c.Resource.Type = "alma")), x => x.Path == "classes[0].resource.type");
        Assert.Contains(Errors(Fresh(c => c.Resource.Name = "")), x => x.Path == "classes[0].resource.name");
        Assert.Contains(Errors(Fresh(c => c.BaseStats.MoveSpeed = 0)), x => x.Path == "classes[0].baseStats.moveSpeed");
        Assert.Contains(Errors(Fresh(c => c.BaseStats.CritChancePermille = 800)), x => x.Path == "classes[0].baseStats.critChancePermille");
        Assert.Contains(Errors(Fresh(c => c.BaseStats.CritMultiplierPermille = 900)), x => x.Path == "classes[0].baseStats.critMultiplierPermille");
        Assert.Contains(Errors(Fresh(c => c.PerLevel.MagicDefense = -1)), x => x.Path == "classes[0].perLevel.magicDefense");
        Assert.Contains(Errors(Fresh(c => c.AbilityIds.Add("nao_existe"))), x => x.Path.StartsWith("classes[0].abilityIds"));
        Assert.Contains(Errors(Fresh(c => c.AbilityIds.Add(c.AbilityIds[0]))), x => x.Path == "classes[0].abilityIds");
        // Habilidade de outra classe: permitido (sem regra por nome ou por classe de origem).
        Assert.Empty(Errors(Fresh(c => c.AbilityIds.Add("cura"))));
    }

    [Fact]
    public void Validador_CriticoSoEmDanoECuraDiretos()
    {
        var set = TestSupport.FreshPresetSet();
        int i = set.Abilities.FindIndex(a => a.Id == "barreira_arcana");
        set.Abilities[i].Effects[0].CanCrit = true;   // escudo
        Assert.Contains(Errors(set), x => x.Path == $"abilities[{i}].effects[0].canCrit");
        // Preset: critico declarado so em dano e cura diretos.
        foreach (var a in C.Abilities.Values)
            foreach (var fx in a.Effects.Where(f => f.CanCrit))
                Assert.Contains(fx.Type, new[] { EffectTypes.Damage, EffectTypes.Heal });
        Assert.Equal(6, C.Abilities.Values.Sum(a => a.Effects.Count(f => f.CanCrit)));
    }

    [Fact]
    public void Validador_CombatJson()
    {
        var set = TestSupport.FreshPresetSet();
        set.Combat.CritChanceMaxPermille = 1200;
        set.Combat.PowerIndex.ReferenceAttack = 0;
        var errs = Errors(set);
        Assert.Contains(errs, x => x.Path == "combat.critChanceMaxPermille");
        Assert.Contains(errs, x => x.Path == "combat.powerIndex.referenceAttack");
    }
}

using System.Reflection;
using Simulation.Core.Content;
using Simulation.Core.Population;
using Simulation.Core.Random;

namespace Simulation.Tests;

/// <summary>
/// Formato triangular (min, moda, max) e dedicacao compartilhada (experimento de dedicacao).
/// Testes estatisticos: amostra fixa (seed fixa, resultado deterministico) e tolerancia de 5 erros
/// padrao da proporcao, sqrt(q(1-q)/N). Uma implementacao correta fica dentro dela com
/// probabilidade de cerca de 1 - 6e-7 por conferencia; um desvio maior que isso e erro real.
/// </summary>
public class TraitDrawTests
{
    private const long S = TraitDraw.Scale;
    private static readonly int[] Influences = { 0, 1, 100, 250, 450, 500, 501, 750, 900, 999, 1000 };
    private const int N = 200_000;
    private static readonly (long A, long B)[] Pairs = MakePairs();

    private static (long, long)[] MakePairs()
    {
        var rng = new DeterministicRng(2026);
        var r = new (long, long)[N];
        for (int i = 0; i < N; i++) r[i] = (TraitDraw.Position(rng), TraitDraw.Position(rng));
        return r;
    }

    private static double Tol(double q) => 5 * Math.Sqrt(q * (1 - q) / N);

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Combinacao_ExtremosContinuaMonotonaESimetrica()
    {
        foreach (int w in Influences)
        {
            // F(s) e Combine(s, s): com as duas posicoes iguais, a soma e o proprio s.
            long F(long s) => TraitDraw.Combine(s, s, w);
            Assert.Equal(0, F(0));
            Assert.InRange(F(S - 1), S - 3, S - 1);
            Assert.InRange(F(S / 2), S / 2 - 2, S / 2 + 2);   // a mediana fica no meio
            long prev = -1;
            for (long k = 0; k <= 4000; k++)
            {
                long f = F((S - 1) * k / 4000);
                Assert.True(f >= prev, $"influencia {w}: nao monotona");
                prev = f;
            }
            // Monotona tambem na posicao propria, com a de dedicacao fixa.
            prev = -1;
            for (long k = 0; k <= 4000; k++)
            {
                long f = TraitDraw.Combine(S / 3, (S - 1) * k / 4000, w);
                Assert.True(f >= prev, $"influencia {w}: nao monotona na posicao propria");
                prev = f;
            }
            // Simetria: F(1 - s) = 1 - F(s), a menos do arredondamento.
            for (long k = 0; k <= 400; k++)
            {
                long s = (S - 1) * k / 400;
                Assert.InRange(F(s) + F(S - 1 - s), S - 6, S + 2);
            }
            // Continuidade nas juncoes do trapezio (a e b em unidades de Scale).
            if (w is > 0 and < 1000)
            {
                long a = Math.Min(w, 1000 - w) * 1_000_000L, b = Math.Max(w, 1000 - w) * 1_000_000L;
                Assert.InRange(F(a) - F(a - 1), 0, 3);
                Assert.InRange(F(b + 1) - F(b), 0, 3);
            }
        }
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Combinacao_InfluenciaZeroEhPropriaEUmEhCompartilhada()
    {
        foreach (var (a, b) in Pairs.Take(5000))
        {
            Assert.Equal(b, TraitDraw.Combine(a, b, 0));
            Assert.Equal(a, TraitDraw.Combine(a, b, 1000));
        }
    }

    /// <summary>A posicao combinada continua uniforme para qualquer influencia, inclusive nas caudas.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Combinacao_MantemPosicaoUniforme()
    {
        double[] qs = { 0.001, 0.01, 0.05, 0.1, 0.25, 0.5, 0.75, 0.9, 0.95, 0.99, 0.999 };
        foreach (int w in Influences)
        {
            var pos = Pairs.Select(p => TraitDraw.Combine(p.A, p.B, w)).ToArray();
            Assert.All(pos, x => Assert.InRange(x, 0, S - 1));
            foreach (double q in qs)
            {
                double frac = pos.Count(x => x < (long)(q * S)) / (double)N;
                Assert.True(Math.Abs(frac - q) <= Tol(q), $"influencia {w}, q {q}: {frac:0.00000} (tolerancia {Tol(q):0.00000})");
            }
        }
    }

    /// <summary>
    /// Mede a combinacao (nao exige que os atributos andem juntos): sem influencia, posicoes
    /// independentes; com influencia intermediaria, combinacoes como "alto em um e baixo no outro"
    /// continuam existindo; com influencia total, a mesma posicao.
    /// </summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Combinacao_MedeCorrelacaoECombinacoesVariadas()
    {
        var rng = new DeterministicRng(77);
        var shared = new long[N];
        var x = new long[N];
        var y = new long[N];
        for (int i = 0; i < N; i++) { shared[i] = TraitDraw.Position(rng); x[i] = TraitDraw.Position(rng); y[i] = TraitDraw.Position(rng); }
        double Mixed(int w, out double rho)
        {
            var px = new double[N]; var py = new double[N];
            int highLow = 0;
            for (int i = 0; i < N; i++)
            {
                px[i] = TraitDraw.Combine(shared[i], x[i], w) / (double)S;
                py[i] = TraitDraw.Combine(shared[i], y[i], w) / (double)S;
                if (px[i] >= 2.0 / 3 && py[i] < 1.0 / 3) highLow++;
            }
            rho = Pearson(px, py);   // posicoes ja sao uniformes: Pearson nelas = correlacao de postos
            return highLow / (double)N;
        }
        double free = Mixed(0, out double rho0);
        Assert.InRange(rho0, -5 / Math.Sqrt(N), 5 / Math.Sqrt(N));
        Assert.True(Math.Abs(free - 1.0 / 9) <= Tol(1.0 / 9));
        double mid = Mixed(450, out double rho450);
        Assert.InRange(rho450, 0.2, 0.6);
        Assert.InRange(mid, 0.03, 1.0 / 9 - Tol(1.0 / 9));
        double full = Mixed(1000, out double rho1000);
        Assert.Equal(0, full);
        Assert.Equal(1.0, rho1000, 9);
    }

    private static double Pearson(double[] a, double[] b)
    {
        double ma = a.Average(), mb = b.Average(), sab = 0, saa = 0, sbb = 0;
        for (int i = 0; i < a.Length; i++) { sab += (a[i] - ma) * (b[i] - mb); saa += (a[i] - ma) * (a[i] - ma); sbb += (b[i] - mb) * (b[i] - mb); }
        return sab / Math.Sqrt(saa * sbb);
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Triangular_ExtremosModaMedianaEMonotonia()
    {
        var cases = new[] { (150, 300, 800), (0, 0, 1000), (0, 1000, 1000), (500, 900, 1000), (30, 70, 240), (10, 360, 720), (0, 500_000, 1_000_000), (5, 5, 5) };
        foreach (var (lo, mo, hi) in cases)
        {
            Assert.Equal(lo, TraitDraw.Triangular(0, lo, mo, hi));
            // Na ultima posicao (1 - 1e-9) o valor exato fica a raiz((max-min)(max-moda)/1e9) do maximo.
            int gap = (int)Math.Ceiling(Math.Sqrt((hi - lo) * (double)(hi - mo) / S));
            Assert.InRange(TraitDraw.Triangular(S - 1, lo, mo, hi), hi - gap - 1, hi);
            if (hi > lo) Assert.InRange(TraitDraw.Triangular(S * (mo - lo) / (hi - lo), lo, mo, hi), mo - 1, mo + 1);
            double expected = hi == lo ? lo : mo >= (lo + hi) / 2.0
                ? lo + Math.Sqrt((hi - lo) * (double)(mo - lo) / 2)
                : hi - Math.Sqrt((hi - lo) * (double)(hi - mo) / 2);
            Assert.InRange(TraitDraw.Median(lo, mo, hi), expected - 1, expected + 1);
            int prev = int.MinValue;
            for (long k = 0; k <= 4000; k++)
            {
                int v = TraitDraw.Triangular((S - 1) * k / 4000, lo, mo, hi);
                Assert.InRange(v, lo, hi);
                Assert.True(v >= prev);
                prev = v;
            }
        }
        // Moda e mediana sao coisas diferentes numa triangular assimetrica.
        Assert.Equal(397, TraitDraw.Median(150, 300, 800));
    }

    /// <summary>Os valores sorteados seguem a acumulada da triangular (com o arredondamento ao inteiro).</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void Triangular_SegueADistribuicaoDeclarada()
    {
        foreach (var (lo, mo, hi) in new[] { (150, 300, 800), (500, 900, 1000), (30, 70, 240) })
        {
            var values = Pairs.Select(p => TraitDraw.Triangular(p.A, lo, mo, hi)).ToArray();
            double Cdf(double v) => v <= lo ? 0 : v >= hi ? 1 : v <= mo
                ? (v - lo) * (v - lo) / ((hi - lo) * (double)(mo - lo))
                : 1 - (hi - v) * (hi - v) / ((hi - lo) * (double)(hi - mo));
            for (int k = 1; k < 20; k++)
            {
                int x = lo + (hi - lo) * k / 20;
                double q = Cdf(x + 0.5);   // arredondado ao mais proximo: valor <= x quando o real < x + 0,5
                double frac = values.Count(v => v <= x) / (double)N;
                Assert.True(Math.Abs(frac - q) <= Tol(q) + 1e-6, $"({lo},{mo},{hi}) em {x}: {frac:0.00000} contra {q:0.00000}");
            }
        }
    }

    private static readonly string[] DedicationTraits = { nameof(PersonTraits.SessionMinutes), nameof(PersonTraits.GameImportance), nameof(PersonTraits.Habit) };

    /// <summary>Preset com os atributos de dedicacao em triangulares equivalentes (mesmo centro e dispersao).</summary>
    private static ContentDatabase TriangularPreset(int influence)
    {
        var set = TestSupport.FreshPresetSet();
        foreach (var p in set.Population.Profiles)
        {
            p.SessionMinutes = Equivalent(p.SessionMinutes, 10, 720);
            p.GameImportance = Equivalent(p.GameImportance, 0, 1000);
            p.Habit = Equivalent(p.Habit, 0, 1000);
        }
        set.Population.Dedication.InfluencePermille = influence;
        return ContentDatabase.Create(set);
    }

    private static TraitRange Equivalent(TraitRange t, int lo, int hi)
    {
        int h = t.Spread * 816 / 1000;
        return new TraitRange { Min = Math.Max(lo, t.Mean - h), Mode = t.Mean, Max = Math.Min(hi, t.Mean + h) };
    }

    /// <summary>
    /// Comparacao pareada: trocar o formato dos atributos de dedicacao (e ligar a dedicacao
    /// compartilhada) nao muda nenhum outro atributo de nenhuma pessoa.
    /// </summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void FormatoTriangular_NaoMudaOsDemaisAtributos()
    {
        var baseDb = TestSupport.Preset;
        foreach (int influence in new[] { 0, 450 })
        {
            var tri = TriangularPreset(influence);
            int changedDedication = 0;
            for (int id = 1; id <= 3000; id++)
            {
                var a = PopulationGenerator.Generate(id, baseDb, 19);
                var b = PopulationGenerator.Generate(id, tri, 19);
                Assert.Equal(a.ProfileId, b.ProfileId);
                Assert.Equal(a.Name, b.Name);
                Assert.Equal(a.AppearanceSeed, b.AppearanceSeed);
                Assert.Equal(a.ResidenceId, b.ResidenceId);
                Assert.Equal(a.NationalityId, b.NationalityId);
                Assert.Equal(a.Languages, b.Languages);
                Assert.Equal(a.OccupationId, b.OccupationId);
                Assert.Equal(a.ShiftOffsetMinutes, b.ShiftOffsetMinutes);
                Assert.Equal(a.CommutePermille, b.CommutePermille);
                Assert.Equal(a.DayOffIndex, b.DayOffIndex);
                Assert.Equal(a.SleepNeedMinutes, b.SleepNeedMinutes);
                Assert.Equal(a.BedtimeMinute, b.BedtimeMinute);
                Assert.Equal(a.IncomeCentsPerMonth, b.IncomeCentsPerMonth);
                Assert.Equal(a.SupportCentsPerMonth, b.SupportCentsPerMonth);
                Assert.Equal(a.SleepDebtTolerance, b.SleepDebtTolerance);
                foreach (var f in typeof(PersonTraits).GetFields(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (DedicationTraits.Contains(f.Name)) { if (!Equals(f.GetValue(a.Traits), f.GetValue(b.Traits))) changedDedication++; continue; }
                    Assert.True(Equals(f.GetValue(a.Traits), f.GetValue(b.Traits)), $"pessoa {id}: {f.Name} mudou");
                }
            }
            Assert.True(changedDedication > 3000, "os atributos de dedicacao deveriam ser outro sorteio");
        }
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void FormatoTriangular_DentroDaFaixaDeterministicoEMedianaNaReferencia()
    {
        var db = TriangularPreset(450);
        for (int id = 1; id <= 2000; id++)
        {
            var p = PopulationGenerator.Generate(id, db, 5);
            var again = PopulationGenerator.Generate(id, db, 5);
            var prof = db.Profiles[p.ProfileId];
            foreach (var (value, range) in new[] { (p.Traits.SessionMinutes, prof.SessionMinutes), (p.Traits.GameImportance, prof.GameImportance), (p.Traits.Habit, prof.Habit) })
                Assert.InRange(value, range.Min!.Value, range.Max!.Value);
            Assert.Equal(p.Traits.SessionMinutes, again.Traits.SessionMinutes);
            Assert.Equal(p.Traits.GameImportance, again.Traits.GameImportance);
            Assert.Equal(p.Traits.Habit, again.Traits.Habit);
        }
        // Pessoa de referencia do cenario controlado: a mediana (nao a moda).
        var occ = db.OccupationsOrdered[0].Id;
        foreach (var prof in db.ProfilesOrdered)
        {
            var r = PopulationGenerator.Generate(1, db, 5, new ControlledCell(prof.Id, occ));
            Assert.Equal(TraitDraw.Median(prof.GameImportance.Min!.Value, prof.GameImportance.Mode!.Value, prof.GameImportance.Max!.Value), r.Traits.GameImportance);
            Assert.Equal(TraitDraw.Median(prof.SessionMinutes.Min!.Value, prof.SessionMinutes.Mode!.Value, prof.SessionMinutes.Max!.Value), r.Traits.SessionMinutes);
            Assert.Equal(TraitDraw.Median(prof.Habit.Min!.Value, prof.Habit.Mode!.Value, prof.Habit.Max!.Value), r.Traits.Habit);
        }
    }

    /// <summary>O formato novo carrega dos arquivos JSON (caminho real do runner e do jogo), nao so montado no codigo.</summary>
    [Fact]
    [Trait("Classe", "Invariante")]
    public void FormatoTriangular_CarregaDoJson()
    {
        var dir = TestSupport.NewTempDir();
        TestSupport.CopyDirectory(TestSupport.PresetFolder, dir);
        var path = Path.Combine(dir, "population.json");
        var root = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(path))!;
        foreach (var p in root["profiles"]!.AsArray())
        {
            p!["gameImportance"] = System.Text.Json.Nodes.JsonNode.Parse("{ \"min\": 150, \"mode\": 300, \"max\": 800 }");
            p["sessionMinutes"] = System.Text.Json.Nodes.JsonNode.Parse("{ \"min\": 30, \"mode\": 70, \"max\": 240 }");
            p["habit"] = System.Text.Json.Nodes.JsonNode.Parse("{ \"min\": 100, \"mode\": 350, \"max\": 850 }");
        }
        root["dedication"] = System.Text.Json.Nodes.JsonNode.Parse("{ \"influencePermille\": 450 }");
        File.WriteAllText(path, root.ToJsonString(), new System.Text.UTF8Encoding(false));
        var r = Simulation.Infrastructure.Content.ContentLoader.LoadFolder(dir);
        Assert.True(r.Database != null, string.Join("\n", r.Issues.Select(i => i.Path + ": " + i.Message)));
        Assert.Equal(450, r.Database!.Dedication.InfluencePermille);
        var casual = r.Database.Profiles["casual"].GameImportance;
        Assert.Equal((150, 300, 800), (casual.Min, casual.Mode, casual.Max));
    }

    [Fact]
    [Trait("Classe", "Invariante")]
    public void Validador_FormatoTriangularEInfluencia()
    {
        static List<ValidationIssue> Errors(Action<ContentSet> change)
        {
            var set = TestSupport.FreshPresetSet();
            change(set);
            return ContentValidator.Validate(set).Where(e => e.Severity == IssueSeverity.Error).ToList();
        }
        PopulationProfileDefinition Casual(ContentSet s) => s.Population.Profiles.First(p => p.Id == "casual");

        Assert.Empty(Errors(s => Casual(s).GameImportance = new TraitRange { Min = 150, Mode = 300, Max = 800 }));
        Assert.NotEmpty(Errors(s => Casual(s).GameImportance = new TraitRange { Min = 400, Mode = 300, Max = 800 }));
        Assert.NotEmpty(Errors(s => Casual(s).GameImportance = new TraitRange { Min = 150, Mode = 900, Max = 800 }));
        Assert.NotEmpty(Errors(s => Casual(s).GameImportance = new TraitRange { Min = 150, Mode = 300, Max = 1200 }));
        Assert.NotEmpty(Errors(s => Casual(s).GameImportance = new TraitRange { Min = 150 }));
        Assert.NotEmpty(Errors(s => Casual(s).GameImportance = new TraitRange { Mean = 350, Spread = 100, Min = 150, Mode = 300, Max = 800 }));
        Assert.NotEmpty(Errors(s => s.Population.Dedication.InfluencePermille = 1001));
        // Influencia acima de 0 com atributos de dedicacao ainda no formato de media e dispersao.
        Assert.Contains(Errors(s => s.Population.Dedication.InfluencePermille = 450), e => e.Path.Contains("gameImportance"));
        Assert.NotEmpty(Errors(s => s.Population.Routine.SlackMinutes = new TraitRange { Min = 10, Mode = 20, Max = 40 }));
    }
}

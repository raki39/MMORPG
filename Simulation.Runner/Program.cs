using System.Globalization;
using System.Text;
using Simulation.Runner;

Console.OutputEncoding = Encoding.UTF8;

const string Usage = """
Simulation.Runner — executa a simulação sem interface (headless).

Uso:
  dotnet run --project Simulation.Runner -- [opções]

Opções:
  --world <pasta>       Pasta de conteúdo (padrão: Content/presets/inicial)
  --days <n>            Dias simulados (padrão: 30)
  --seed <n>            Seed da campanha, inteiro sem sinal (padrão: 42)
  --out <pasta>         Pasta dos relatórios (CSV, summary.md, metadata.json)
  --population <n>      Sobrescreve o tamanho da população do cenário
  --scenario <arquivo>  Cenário a usar (padrão: scenario.json da pasta do mundo)
  --no-scenario         Ignora o cenário (sem comandos agendados)
  --no-skip             Processa tick a tick, sem saltar ticks ociosos (verificação)
  --seeds <lista>       Validação com várias seeds, em paralelo: "1-5" ou "3,7,42".
                        Escreve só validation.md e validation.csv (exige --out)
  --targets <arquivo>   Metas da validação (padrão: targets.json da pasta do mundo)
  --no-targets          Não calcula a validação na execução simples
  --progression         Mostra a prévia da curva de progressão do mundo e sai
                        (com --out, grava progression.md)
  --help                Mostra esta ajuda

Códigos de saída: 0 ok · 1 uso incorreto · 2 conteúdo inválido · 3 cenário inválido
""";

var opt = new RunOptions();
bool progressionOnly = false;
for (int i = 0; i < args.Length; i++)
{
    string a = args[i];
    string Next()
    {
        if (i + 1 >= args.Length) throw new ArgumentException($"Faltou o valor de {a}.");
        return args[++i];
    }

    try
    {
        switch (a)
        {
            case "--help":
            case "-h":
                Console.WriteLine(Usage);
                return ScenarioRunner.ExitOk;
            case "--world": opt.WorldFolder = Next(); break;
            case "--days": opt.Days = int.Parse(Next(), CultureInfo.InvariantCulture); break;
            case "--seed": opt.Seed = ulong.Parse(Next(), CultureInfo.InvariantCulture); break;
            case "--out": opt.OutDir = Next(); break;
            case "--population": opt.PopulationOverride = int.Parse(Next(), CultureInfo.InvariantCulture); break;
            case "--scenario": opt.ScenarioPath = Next(); break;
            case "--no-scenario": opt.NoScenario = true; break;
            case "--no-skip": opt.DisableTickSkipping = true; break;
            case "--seeds": opt.Seeds = ParseSeeds(Next()); break;
            case "--targets": opt.TargetsPath = Next(); break;
            case "--no-targets": opt.NoTargets = true; break;
            case "--progression": progressionOnly = true; break;
            default: throw new ArgumentException($"Opção desconhecida: {a}");
        }
    }
    catch (Exception ex) when (ex is ArgumentException or FormatException or OverflowException)
    {
        Console.Error.WriteLine("ERRO: " + ex.Message);
        Console.Error.WriteLine(Usage);
        return ScenarioRunner.ExitUsage;
    }
}

if (progressionOnly) return ScenarioRunner.PrintProgression(opt);

var result = ScenarioRunner.Run(opt);
foreach (var m in result.Messages)
{
    if (result.ExitCode == ScenarioRunner.ExitOk) Console.WriteLine(m);
    else Console.Error.WriteLine(m);
}
return result.ExitCode;

static List<ulong> ParseSeeds(string text)
{
    var seeds = new List<ulong>();
    foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        int dash = part.IndexOf('-');
        if (dash > 0)
        {
            ulong a = ulong.Parse(part[..dash], CultureInfo.InvariantCulture);
            ulong b = ulong.Parse(part[(dash + 1)..], CultureInfo.InvariantCulture);
            if (b < a || b - a > 999) throw new ArgumentException($"Intervalo de seeds inválido: {part}");
            for (ulong s = a; s <= b; s++) seeds.Add(s);
        }
        else seeds.Add(ulong.Parse(part, CultureInfo.InvariantCulture));
    }
    if (seeds.Count == 0) throw new ArgumentException("--seeds vazio.");
    return seeds.Distinct().ToList();
}

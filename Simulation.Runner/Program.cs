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
  --help                Mostra esta ajuda

Códigos de saída: 0 ok · 1 uso incorreto · 2 conteúdo inválido · 3 cenário inválido
""";

var opt = new RunOptions();
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

var result = ScenarioRunner.Run(opt);
foreach (var m in result.Messages)
{
    if (result.ExitCode == ScenarioRunner.ExitOk) Console.WriteLine(m);
    else Console.Error.WriteLine(m);
}
return result.ExitCode;

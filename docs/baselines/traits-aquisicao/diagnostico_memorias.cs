using Simulation.Core.Commands;
using Simulation.Core.Engine;
using Simulation.Core.Time;
using Simulation.Infrastructure.Content;
// Diagnostico (P4d): tipos de memoria, valencias das atividades e voltas a lugares marcados.
var content = ContentLoader.LoadFolder(args[0]).Database!;
var e = WorldFactory.Create(content, new WorldConfig { Seed = 1, PopulationSize = 1000 });
e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
var kinds = new SortedDictionary<string, int>(StringComparer.Ordinal);
int minImp = 0; var impHist = new int[11];
for (int d = 0; d < 45; d++)
{
    e.AdvanceTo(SimTime.Days(d + 1));
    e.World.Events.Drain();
    foreach (var p in e.World.Persons)
        foreach (var m in p.Memories) { string k = m.Kind + (m.Valence < 0 ? "(-)" : "(+)"); kinds[k] = kinds.GetValueOrDefault(k) + 1; }
}
foreach (var p in e.World.Persons)
    foreach (var kv in p.PlaceImpressions) { minImp = Math.Min(minImp, kv.Value); impHist[Math.Clamp((kv.Value + 1000) / 200, 0, 10)]++; }
Console.WriteLine("memorias (soma diaria de presencas, 45 dias): " + string.Join(", ", kinds.Select(k => $"{k.Key} {k.Value}")));
Console.WriteLine($"notableValence {content.Memory.NotableValence}; menor impressao {minImp}; impressoes por faixa de 200 (de -1000 a 1000): {string.Join(" ", impHist)}");

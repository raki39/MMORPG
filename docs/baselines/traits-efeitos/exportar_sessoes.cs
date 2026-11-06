using System.Globalization;
using System.Text;
using Simulation.Core.Commands;
using Simulation.Core.Engine;
using Simulation.Core.Time;
using Simulation.Infrastructure.Content;
// Diagnostico (P4c, fora do jogo): sessoes e pessoas com traits. args: mundo seed populacao dias pasta
var content = ContentLoader.LoadFolder(args[0]).Database!;
var e = WorldFactory.Create(content, new WorldConfig { Seed = ulong.Parse(args[1]), PopulationSize = int.Parse(args[2]) });
e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
var open = new Dictionary<int, (long Start, string Kind)>();
var rows = new List<string>();
for (int d = 0; d < int.Parse(args[3]); d++)
{
    e.AdvanceTo(SimTime.Days(d + 1));
    foreach (var ev in e.World.Events.Drain())
    {
        if (ev.Entities.Count == 0) continue;
        int pid = (int)ev.Entities[0].Id;
        if (ev.Type == "session_started") open[pid] = (ev.Tick, ev.Causes.Count > 0 ? ev.Causes[0].Factor : "");
        else if (ev.Type == "session_ended" && open.Remove(pid, out var s)) rows.Add(FormattableString.Invariant($"{pid},{s.Start},{ev.Tick},{s.Kind},{(ev.Causes.Count > 0 ? ev.Causes[0].Factor : "")}"));
    }
}
Directory.CreateDirectory(args[4]);
var inv = CultureInfo.InvariantCulture;
var sb = new StringBuilder("id,profile,offset_min,session_min,registered,traits,marathons\n");
foreach (var p in e.World.Persons)
    sb.Append(string.Join(",", new object[] { p.Id, p.ProfileId, p.LocalOffsetMinutes, p.Traits.SessionMinutes, p.CharacterId > 0 ? 1 : 0,
        string.Join("|", p.Quirks.Select(q => q.Id)), p.QuirkStats.TryGetValue("maratonista_maratonas", out int m) ? m : 0 }.Select(o => Convert.ToString(o, inv)))).Append('\n');
File.WriteAllText(Path.Combine(args[4], "persons.csv"), sb.ToString(), new UTF8Encoding(false));
File.WriteAllText(Path.Combine(args[4], "sessions.csv"), "person,start,end,kind,reason\n" + string.Join("\n", rows) + "\n", new UTF8Encoding(false));
Console.WriteLine($"ok {rows.Count} sessoes");

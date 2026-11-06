using System.Globalization;
using System.Text;
using Simulation.Core.Commands;
using Simulation.Core.Engine;
using Simulation.Core.Time;
using Simulation.Infrastructure.Content;

// Diagnostico de horarios (R6, nao faz parte do jogo): pessoas, sessoes e dias (sono planejado e real).
// args: mundo seed populacao dias pastaSaida
var content = ContentLoader.LoadFolder(args[0]).Database!;
ulong seed = ulong.Parse(args[1]);
int pop = int.Parse(args[2]);
int days = int.Parse(args[3]);
string outDir = args[4];
Directory.CreateDirectory(outDir);
var e = WorldFactory.Create(content, new WorldConfig { Seed = seed, PopulationSize = pop });
e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
var open = new Dictionary<int, long>();
var sessions = new List<(int P, long S, long E)>();
var plans = new Dictionary<(int, long), string>();
var inv = CultureInfo.InvariantCulture;
for (int d = 0; d < days; d++)
{
    e.AdvanceTo(SimTime.Days(d + 1));
    foreach (var ev in e.World.Events.Drain())
    {
        if (ev.Entities.Count == 0) continue;
        int pid = (int)ev.Entities[0].Id;
        if (ev.Type == "session_started") open[pid] = ev.Tick;
        else if (ev.Type == "session_ended" && open.Remove(pid, out var s)) sessions.Add((pid, s, ev.Tick));
    }
    foreach (var p in e.World.Persons)
    {
        if (p.CharacterId == 0) continue;
        foreach (var r in p.DayPlans)
        {
            var lay = e.World.Behavior.LayoutOf(p, r.LocalDay);
            int firstBlock = lay.Blocks.Count > 0 ? lay.Blocks[0].Start : -1;
            plans[(p.Id, r.LocalDay)] = string.Join(",", new object[] { p.Id, r.LocalDay, lay.FreeDay ? 1 : 0, lay.SleepStart, lay.Wake, lay.Bed, lay.NightShiftAhead ? 1 : 0, lay.DaySleeper ? 1 : 0,
                firstBlock, r.SleepMinutes, r.DeficitMinutes, r.Reason, r.FirstStartMinute, lay.FreeMinutes, r.SleepShiftMinutes, lay.OptionalWindows.Count }.Select(o => Convert.ToString(o, inv)));
        }
    }
}
long end = e.World.Clock.Tick;
foreach (var (pid, s) in open) sessions.Add((pid, s, end));
var sb = new StringBuilder("id,profile,occupation,residence,offset_min,preferred_hour,habitual_hour,bedtime_min,sleep_need_min,registered\n");
foreach (var p in e.World.Persons)
    sb.Append(string.Join(",", new object[] { p.Id, p.ProfileId, p.OccupationId, p.ResidenceId, p.LocalOffsetMinutes, p.Traits.PreferredStartHour, p.HabitualStartHour,
        p.BedtimeMinute, p.SleepNeedMinutes, p.CharacterId > 0 ? 1 : 0, p.Traits.Chronotype, p.FreeEveShiftMinutes }.Select(o => Convert.ToString(o, inv)))).Append('\n');
File.WriteAllText(Path.Combine(outDir, "persons.csv"), sb.ToString(), new UTF8Encoding(false));
var ss = new StringBuilder("person,start_tick,end_tick\n");
foreach (var s in sessions) ss.Append(FormattableString.Invariant($"{s.P},{s.S},{s.E}\n"));
File.WriteAllText(Path.Combine(outDir, "sessions.csv"), ss.ToString(), new UTF8Encoding(false));
var ds = new StringBuilder("person,local_day,free_day,sleep_start,wake,bed,night_shift,day_sleeper,first_block,slept,deficit,reason,first_start,free_minutes\n");
foreach (var kv in plans.OrderBy(k => k.Key.Item1).ThenBy(k => k.Key.Item2)) ds.Append(kv.Value).Append('\n');
File.WriteAllText(Path.Combine(outDir, "days.csv"), ds.ToString(), new UTF8Encoding(false));
Console.WriteLine($"ok {sessions.Count} sessoes, {plans.Count} dias");

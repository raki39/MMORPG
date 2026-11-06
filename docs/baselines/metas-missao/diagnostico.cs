using System.Text;
using Simulation.Core.Behavior;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Population;
using Simulation.Core.Time;
using Simulation.Infrastructure.Content;

// Diagnostico das metas de missao (complete_quest) e do objetivo da campanha.
// Uso: metas <pasta do conteudo> <saida> [seeds] [dias] [populacao]
var content = ContentLoader.LoadFolder(args[0]).Database!;
string outDir = args[1];
int seeds = args.Length > 2 ? int.Parse(args[2]) : 5;
int days = args.Length > 3 ? int.Parse(args[3]) : 90;
int pop = args.Length > 4 ? int.Parse(args[4]) : 1000;
Directory.CreateDirectory(outDir);
var ends = new StringBuilder();
ends.AppendLine("seed,char,person,type,target,cat,completed,reason,set_tick,end_tick,played_end,since_progress,stale,progress,steps,active,step_index,step_min,step_need,step_region,step_act,step_minlevel,step_group,step_zone_known,step_hint,level,quest_minlevel,active_quests,max_active,patience,personality,quirks,profile,links,obj_campaign_streak,obj_campaign_stat,slots");
var evs = new StringBuilder();
evs.AppendLine("seed,char,tick,type,detail,causes");
var gs = content.GoalSettings;

for (int seed = 1; seed <= seeds; seed++)
{
    var e = WorldFactory.Create(content, new WorldConfig { Seed = (ulong)seed, PopulationSize = pop });
    e.Submit(new SetServerOpenCommand(true), SimTime.Hours(8));
    var w = e.World;
    var goals = w.Behavior.Goals;
    var orig = goals.Ended;
    int s = seed;
    goals.Ended = (ch, g, completed, reason) =>
    {
        var p = w.GetPerson(ch.PersonId);
        string cat = "", stepRegion = "", stepAct = "";
        int steps = 0, stepIndex = -1, stepMin = 0, stepNeed = 0, stepMinLevel = 0, stepGroup = 0, questMin = 0;
        bool active = false, zoneKnown = false, hint = false;
        if (g.Type == GoalTypes.CompleteQuest && content.Quests.TryGetValue(g.Target, out var q))
        {
            cat = q.Category; steps = q.Steps.Count; questMin = q.MinLevel;
            var aq = ch.ActiveQuests.FirstOrDefault(x => x.QuestId == q.Id);
            if (aq != null)
            {
                active = true; stepIndex = aq.StepIndex; stepMin = aq.StepMinutes;
                if (QuestSystem.CurrentStep(content, aq) is { } st)
                {
                    stepNeed = st.Minutes; stepRegion = st.RegionId; stepAct = st.ActivityId;
                    if (content.Regions.TryGetValue(st.RegionId, out var z) && z.FindActivity(st.ActivityId) is { } a) { stepMinLevel = a.MinLevel; stepGroup = a.MinGroupSize; }
                    zoneKnown = ch.KnownRegions.Contains(st.RegionId);
                    hint = ch.ContentHints.Contains(st.RegionId + "/" + st.ActivityId);
                }
            }
        }
        long played = ch.PlayedTicksAt(w.Clock.Tick);
        long stale = (gs.StaleBaseHours * 1000L + gs.StaleExtraHoursAtMaxPatience * (long)p.Traits.Patience) * SimTime.TicksPerHour / 1000;
        var camp = p.Objectives.FirstOrDefault(o => o.Id == "campanha_principal");
        ends.AppendLine(string.Join(",", s, ch.Id, p.Id, g.Type, g.Target, cat, completed ? 1 : 0, reason, g.SetTick, w.Clock.Tick, played,
            played - g.LastProgressPlayedTicks, stale, g.Progress, steps, active ? 1 : 0, stepIndex, stepMin, stepNeed, stepRegion, stepAct, stepMinLevel, stepGroup,
            zoneKnown ? 1 : 0, hint ? 1 : 0, ch.Level, questMin, ch.ActiveQuests.Count, content.QuestSettings.MaxActivePerCharacter, p.Traits.Patience,
            p.PersonalityId, string.Join("|", p.Quirks.Select(x => x.Id)), p.ProfileId, string.Join("|", g.Links.Select(l => l.ObjectiveId + ":" + l.Kind)),
            camp?.FailureStreak ?? -1, camp?.State ?? "",
            // Missoes nas vagas: id:categoria:nivel recomendado:etapa/etapas:minutos da etapa:dias desde o aceite.
            string.Join("|", ch.ActiveQuests.Select(x => { var qq = content.Quests[x.QuestId]; return $"{x.QuestId}:{qq.Category}:{qq.RecommendedLevel}:{x.StepIndex}/{qq.Steps.Count}:{x.StepMinutes}:{(w.Clock.Tick - x.AcceptedTick) / SimTime.TicksPerDay}"; }))));
        orig!(ch, g, completed, reason);
    };
    var keep = new HashSet<string>(StringComparer.Ordinal) { "goal_set", "goal_completed", "goal_abandoned", "quest_accepted", "quest_step_completed", "quest_completed", "level_up", "session_started", "session_ended", "objective_abandoned", "objective_completed", "objective_set", "region_discovered", "activity_chosen", "plan_changed", "session_plan" };
    for (int d = 0; d < days; d++)
    {
        e.AdvanceTo(SimTime.Days(d + 1));
        foreach (var ev in e.World.Events.Drain())
        {
            if (!keep.Contains(ev.Type)) continue;
            long id = ev.Entities.Where(x => x.Kind == "character").Select(x => (long)x.Id).FirstOrDefault();
            if (id == 0) id = -ev.Entities.Where(x => x.Kind == "person").Select(x => (long)x.Id).FirstOrDefault();   // pessoa (negativo)
            evs.AppendLine(string.Join(",", s, id, ev.Tick, ev.Type, (ev.Detail ?? "").Replace(',', ';'), string.Join("|", ev.Causes.Select(c => c.Factor + "=" + c.WeightPermille))));
        }
    }
    Console.WriteLine($"seed {seed} ok");
}
File.WriteAllText(Path.Combine(outDir, "ends.csv"), ends.ToString());
File.WriteAllText(Path.Combine(outDir, "events.csv"), evs.ToString());
Console.WriteLine("pronto");

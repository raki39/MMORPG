using System.Globalization;
using Simulation.Core.Content;
using Simulation.Core.Population;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace Simulation.Core.Engine;

public sealed class WorldConfig
{
    public ulong Seed { get; set; } = 42;
    /// <summary>Pessoas geradas no inicio (no M0 todas como potenciais interessadas).</summary>
    public int PopulationSize { get; set; } = 100;
}

/// <summary>Cria uma campanha nova a partir de conteudo validado e configuracao.</summary>
public static class WorldFactory
{
    public static SimulationEngine Create(ContentDatabase content, WorldConfig config)
    {
        if (config.PopulationSize < 0) throw new ArgumentOutOfRangeException(nameof(config), "População negativa.");

        var calendar = BuildCalendar(content.World);
        var world = new WorldState(content, config.Seed, calendar);
        var engine = new SimulationEngine(world);

        for (int id = 1; id <= config.PopulationSize; id++)
            world.AddPerson(PopulationGenerator.Generate(id, content, config.Seed));

        // O dia 0 fecha na meia-noite seguinte (tick = 1 dia).
        world.Scheduler.Schedule(SimTime.TicksPerDay, SimulationEngine.DailyRollupEvent, payload: 0);
        // Planejamento de sessoes a partir do dia 1 (no dia 0 as sessoes vem do registro).
        world.Scheduler.Schedule(SimTime.TicksPerDay, Behavior.BehaviorSystem.EvDailySessions, payload: 1);
        return engine;
    }

    public static CampaignCalendar BuildCalendar(WorldDefinition def)
    {
        var start = DateTime.ParseExact(def.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var holidays = new List<Holiday>();
        foreach (var h in def.Holidays)
        {
            if (!ContentValidator.TryParseHoliday(h.Date, out int m, out int d, out int? y))
                throw new FormatException($"Feriado inválido: {h.Date}");
            holidays.Add(new Holiday(m, d, y, h.Name));
        }
        return new CampaignCalendar(start, holidays);
    }
}

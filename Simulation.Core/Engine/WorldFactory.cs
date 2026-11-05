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
    /// <summary>
    /// Cenario controlado (D-039a): o humor nao muda a rotina nem a permanencia (sem fator de
    /// engajamento, sessoes flexiveis, inatividade, desistencia). Mede a rotina pura.
    /// </summary>
    public bool NeutralMood { get; set; }
}

/// <summary>Cria uma campanha nova a partir de conteudo validado e configuracao.</summary>
public static class WorldFactory
{
    public static SimulationEngine Create(ContentDatabase content, WorldConfig config)
    {
        if (config.PopulationSize < 0) throw new ArgumentOutOfRangeException(nameof(config), "População negativa.");

        var calendar = BuildCalendar(content.World, content.ServerLocality);
        var locals = new SortedDictionary<string, CampaignCalendar>(StringComparer.Ordinal);
        foreach (var l in content.LocalitiesOrdered) locals[l.Id] = BuildCalendar(content.World, l);
        var world = new WorldState(content, config.Seed, calendar) { NeutralMood = config.NeutralMood, LocalityCalendars = locals };
        var engine = new SimulationEngine(world);

        for (int id = 1; id <= config.PopulationSize; id++)
            world.AddPerson(PopulationGenerator.Generate(id, content, config.Seed));

        // O dia 0 fecha na meia-noite seguinte (tick = 1 dia).
        world.Scheduler.Schedule(SimTime.TicksPerDay, SimulationEngine.DailyRollupEvent, payload: 0);
        // Planejamento de sessoes a partir do dia local 1 (no dia 0 as sessoes vem do registro), na
        // meia-noite local de cada localidade (P1): alvo = indice da localidade, carga = dia local.
        var server = content.ServerLocality;
        var ordered = content.LocalitiesOrdered;
        for (int i = 0; i < ordered.Count; i++)
        {
            long offset = (ordered[i].UtcOffsetMinutes - server.UtcOffsetMinutes) * SimTime.TicksPerMinute;
            world.Scheduler.Schedule(SimTime.TicksPerDay - offset, Behavior.BehaviorSystem.EvDailySessions, i, 1);
        }
        return engine;
    }

    /// <summary>Calendario de uma localidade: data inicial do mundo e feriados da localidade.</summary>
    public static CampaignCalendar BuildCalendar(WorldDefinition def, LocalityDefinition locality)
    {
        var start = DateTime.ParseExact(def.StartDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
        var holidays = new List<Holiday>();
        foreach (var h in locality.Holidays)
        {
            if (!ContentValidator.TryParseHoliday(h.Date, out int m, out int d, out int? y))
                throw new FormatException($"Feriado inválido: {h.Date}");
            holidays.Add(new Holiday(m, d, y, h.Name));
        }
        return new CampaignCalendar(start, holidays);
    }
}

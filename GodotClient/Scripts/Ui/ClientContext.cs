using Simulation.Core.Content;
using Simulation.Core.Events;
using Simulation.Core.Time;
using Simulation.Core.World;
using GodotClient.Sim;

namespace GodotClient.Ui;

/// <summary>O que as telas precisam ler do cliente e as acoes que podem pedir.</summary>
public sealed class ClientContext
{
    public ContentDatabase Content { get; init; } = null!;
    public CampaignCalendar Calendar { get; init; } = null!;
    public ZoneLayout Layout { get; init; } = null!;
    public IReadOnlyList<PersonInfo> Persons { get; init; } = Array.Empty<PersonInfo>();
    public Func<SimSnapshot> Snapshot { get; init; } = null!;
    /// <summary>Series amostradas (ultimas 24 h) para graficos.</summary>
    public MetricsHistory Metrics { get; init; } = new();
    /// <summary>Eventos recentes ligados a pessoa (mais novos primeiro).</summary>
    public Func<int, IReadOnlyList<SimEvent>> PersonHistory { get; init; } = _ => Array.Empty<SimEvent>();
    /// <summary>Sessoes registradas da pessoa (mais antigas primeiro; End = -1 se aberta).</summary>
    public Func<int, IReadOnlyList<SessionLog>> PersonSessions { get; init; } = _ => Array.Empty<SessionLog>();
    /// <summary>Tick do cadastro da conta (-1 se antes do registro de eventos).</summary>
    public Func<int, long> RegisteredAt { get; init; } = _ => -1;
    public Action<int, bool> FocusPerson { get; init; } = (_, _) => { };
    public Action<string> FocusZone { get; init; } = _ => { };
    public Action<int> OpenPerson { get; init; } = _ => { };
    public Action<string> OpenZone { get; init; } = _ => { };
    /// <summary>Favoritos desta sessao (persistencia chega com o save).</summary>
    public HashSet<int> Favorites { get; } = new();
    /// <summary>
    /// Cartoes fixados no mapa (nesta sessao), na ordem em que foram fixados: "person#id", "zone#id",
    /// "area#zona#area". Aparecem na aba Fixados do painel lateral.
    /// </summary>
    public List<string> Pinned { get; } = new();
    /// <summary>Mostra o cartao fixado (reabre se estava fechado) e enquadra no mapa.</summary>
    public Action<string> ShowPinned { get; init; } = _ => { };
    public Action<string> Unpin { get; init; } = _ => { };
    /// <summary>Rascunhos de configuracao de zona (publicacao via atualizacao no M7).</summary>
    public Dictionary<string, ZoneDraft> Drafts { get; } = new(StringComparer.Ordinal);

    public string ZoneName(string id) => Content.Regions.TryGetValue(id, out var r) ? r.Name : id;

    public Action<string, string> OpenArea { get; init; } = (_, _) => { };
    public Action<string> OpenQuest { get; init; } = _ => { };
    /// <summary>Pede ao simulador o detalhe da pessoa (ficha aberta).</summary>
    public Action<int> WatchPerson { get; init; } = _ => { };
    public Action<string, string> FocusArea { get; init; } = (_, _) => { };

    public string ActivityName(string zone, string activity) =>
        Content.Regions.TryGetValue(zone, out var r) ? r.FindActivity(activity)?.Name ?? activity : activity;

    /// <summary>Nome da area onde a atividade acontece (ou da zona, se for da zona inteira).</summary>
    public string PlaceName(string zone, string activity) =>
        Content.Regions.TryGetValue(zone, out var r) && r.AreaOfActivity(activity) is { } a ? a.Name : ZoneName(zone);

    public string Clock(long tick)
    {
        var d = Calendar.ToDateTime(tick);
        return $"{d.Hour:00}:{d.Minute:00}";
    }
}

/// <summary>Rascunho de configuracao de uma zona. Nao altera o mundo ativo.</summary>
public sealed class ZoneDraft
{
    public string Name { get; set; } = "";
    public string Biome { get; set; } = "";
    public string Function { get; set; } = "";
    public string Pvp { get; set; } = "";
    public string DeathRule { get; set; } = "";
    public string Control { get; set; } = "";
    public int LevelMin { get; set; }
    public int LevelMax { get; set; }
    public int Capacity { get; set; }
    public HashSet<string> Offered { get; } = new(StringComparer.Ordinal);
    public bool Saved { get; set; }

    public static ZoneDraft From(RegionDefinition r)
    {
        var d = new ZoneDraft
        {
            Name = r.Name, Biome = r.Biome, Function = r.Function, Pvp = r.Pvp, DeathRule = r.DeathRule, Control = r.Control,
            LevelMin = r.LevelMin, LevelMax = r.LevelMax, Capacity = r.Capacity,
        };
        foreach (var a in r.AllActivities()) d.Offered.Add(a.Id);
        return d;
    }

    public bool DiffersFrom(RegionDefinition r) =>
        Name != r.Name || Biome != r.Biome || Function != r.Function || Pvp != r.Pvp || DeathRule != r.DeathRule ||
        Control != r.Control || LevelMin != r.LevelMin || LevelMax != r.LevelMax || Capacity != r.Capacity ||
        Offered.Count != r.AllActivities().Count() || r.AllActivities().Any(a => !Offered.Contains(a.Id));
}

/// <summary>Sessao registrada: inicio, fim (-1 = aberta) e duracao planejada (min).</summary>
public readonly record struct SessionLog(long Start, long End, int PlannedMinutes);

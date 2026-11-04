using Simulation.Core.Population;
using Simulation.Core.Time;

namespace GodotClient.Sim;

/// <summary>
/// Series para graficos e indicadores, amostradas do snapshot real a cada 10 minutos
/// simulados (ultimas 24 h). Apresentacao: nao altera nada da simulacao.
/// </summary>
public sealed class MetricsHistory
{
    public const int SampleMinutes = 10;
    public const int Capacity = 24 * 60 / SampleMinutes;

    private long _lastSampleTick = long.MinValue;
    public List<long> Ticks { get; } = new();
    public List<float> Online { get; } = new();
    public List<float> Registered { get; } = new();
    public List<float> AvgLevel { get; } = new();
    public Dictionary<string, List<float>> OnlineByZone { get; } = new(StringComparer.Ordinal);

    public void Sample(SimSnapshot snap, IEnumerable<string> zoneIds)
    {
        long bucket = snap.Tick / (SampleMinutes * SimTime.TicksPerMinute);
        if (bucket == _lastSampleTick) return;
        _lastSampleTick = bucket;

        var perZone = zoneIds.ToDictionary(z => z, _ => 0f, StringComparer.Ordinal);
        int levels = 0, chars = 0;
        foreach (var v in snap.ByPerson)
        {
            if (v == null) continue;
            chars++;
            levels += v.Level;
            if (v.Status != CharacterStatus.Offline && perZone.ContainsKey(v.RegionId)) perZone[v.RegionId]++;
        }
        Push(Ticks, snap.Tick);
        Push(Online, snap.Online);
        Push(Registered, snap.Registered);
        Push(AvgLevel, chars == 0 ? 0 : levels / (float)chars);
        foreach (var (z, n) in perZone)
        {
            if (!OnlineByZone.TryGetValue(z, out var list)) OnlineByZone[z] = list = new List<float>();
            Push(list, n);
        }
    }

    /// <summary>Copia profunda (a tela le uma copia; a thread da simulacao escreve no original).</summary>
    public void CopyTo(MetricsHistory target)
    {
        target._lastSampleTick = _lastSampleTick;
        Replace(target.Ticks, Ticks);
        Replace(target.Online, Online);
        Replace(target.Registered, Registered);
        Replace(target.AvgLevel, AvgLevel);
        foreach (var (z, list) in OnlineByZone)
        {
            if (!target.OnlineByZone.TryGetValue(z, out var t)) target.OnlineByZone[z] = t = new List<float>();
            Replace(t, list);
        }
    }

    private static void Replace<T>(List<T> target, List<T> source)
    {
        target.Clear();
        target.AddRange(source);
    }

    private static void Push<T>(List<T> list, T value)
    {
        list.Add(value);
        if (list.Count > Capacity) list.RemoveAt(0);
    }

    public float DeltaOf(List<float> series, int samplesBack = 6) =>
        series.Count <= samplesBack ? 0 : series[^1] - series[^(samplesBack + 1)];
}

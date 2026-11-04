namespace Simulation.Core.Random;

/// <summary>
/// Fluxos de RNG derivados da seed da campanha, um por subsistema (e por entidade quando
/// necessario). Fluxos separados evitam que um sistema novo altere sorteios de outro.
/// Os estados sao persistidos e entram no hash de estado.
/// </summary>
public sealed class RngStreams
{
    private readonly SortedDictionary<string, DeterministicRng> _streams = new(StringComparer.Ordinal);

    public RngStreams(ulong campaignSeed)
    {
        CampaignSeed = campaignSeed;
    }

    public ulong CampaignSeed { get; }

    /// <summary>Seed derivada estavel para (subsistema[, entidade]).</summary>
    public static ulong DeriveSeed(ulong campaignSeed, string streamName, long entityId = 0)
    {
        ulong h = StableHash.Mix64(campaignSeed ^ StableHash.Fnv1a64(streamName));
        return StableHash.Mix64(h + unchecked((ulong)entityId) * 0x9E3779B97F4A7C15UL);
    }

    /// <summary>Fluxo persistente do subsistema. Criado sob demanda com seed derivada.</summary>
    public DeterministicRng Get(string streamName)
    {
        if (!_streams.TryGetValue(streamName, out var rng))
        {
            rng = new DeterministicRng(DeriveSeed(CampaignSeed, streamName));
            _streams.Add(streamName, rng);
        }
        return rng;
    }

    /// <summary>
    /// Gerador efemero para (subsistema, entidade): sempre o mesmo para a mesma seed.
    /// Util para geracao inicial (ex.: atributos da pessoa N), nao e persistido.
    /// </summary>
    public DeterministicRng ForEntity(string streamName, long entityId) =>
        new DeterministicRng(DeriveSeed(CampaignSeed, streamName, entityId));

    /// <summary>Estados em ordem ordinal do nome (estavel para hash e save).</summary>
    public IEnumerable<KeyValuePair<string, RngState>> States()
    {
        foreach (var kv in _streams)
            yield return new KeyValuePair<string, RngState>(kv.Key, kv.Value.State);
    }

    public void Restore(string streamName, RngState state) => _streams[streamName] = new DeterministicRng(state);
}

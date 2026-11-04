using Simulation.Core.Content;
using Simulation.Core.Population;

namespace Simulation.Core.Behavior;

/// <summary>
/// Regras do plano da sessao e do tedio contextual (D-026/D-037). Parametros em
/// population.json ("session"). Tudo em inteiros e sem sorteio.
/// </summary>
public static class SessionRules
{
    /// <summary>
    /// Fim de uma atividade: o tedio do tipo cresce com os minutos (mais rapido para quem prefere
    /// variedade, mais devagar para quem gosta muito daquele tipo) e o dos outros se alivia.
    /// </summary>
    public static void AccrueBoredom(Character ch, PersonTraits t, string type, int minutes, SessionSettingsDefinition s)
    {
        if (minutes <= 0) return;
        int variety = s.VarietyBase + t.Variety * s.VarietyPerPreference / 1000;
        int relief = minutes * s.BoredomRecoveryPerHour / 60;
        foreach (var key in ch.Boredom.Keys.ToList())
        {
            if (key == type) continue;
            int left = ch.Boredom[key] - relief;
            if (left > 0) ch.Boredom[key] = left; else ch.Boredom.Remove(key);
        }
        // Quem gosta muito do que faz cansa mais devagar daquilo (gosto 1000 = -EnjoymentRelief/2).
        int enjoyment = 1000 - (ActivityScoring.Affinity(type, t) - 500) * s.EnjoymentRelief / 1000;
        int gain = minutes * s.BoredomPerHour / 60 * variety / 1000 * enjoyment / 1000;
        int now = (ch.Boredom.TryGetValue(type, out int b) ? b : 0) + gain;
        if (now > 0) ch.Boredom[type] = Math.Min(1000, now);
    }

    /// <summary>Entre sessoes, o tedio se dissipa: sobra s.BoredomKeptBetweenSessions (permille).</summary>
    public static void FadeBoredomBetweenSessions(Character ch, SessionSettingsDefinition s)
    {
        foreach (var key in ch.Boredom.Keys.ToList())
        {
            int left = ch.Boredom[key] * s.BoredomKeptBetweenSessions / 1000;
            if (left > 0) ch.Boredom[key] = left; else ch.Boredom.Remove(key);
        }
    }

    /// <summary>Fator positivo que mais pesou na opcao (motivo do plano); "noise" fica de fora.</summary>
    public static string MainReason(ScoredOption o)
    {
        string key = "affinity";
        int best = int.MinValue;
        foreach (var f in o.Factors)
        {
            if (f.Key == "noise") continue;
            if (f.Contribution > best || (f.Contribution == best && string.CompareOrdinal(f.Key, key) < 0)) { best = f.Contribution; key = f.Key; }
        }
        return key;
    }

    /// <summary>
    /// Por que a nova opcao venceu o foco: o fator com a maior diferenca a favor dela
    /// (ex.: tedio do foco, meta pessoal, missao). Ruido so quando nada mais explica.
    /// </summary>
    public static string MainDifference(ScoredOption chosen, ScoredOption focus)
    {
        var diff = new SortedDictionary<string, int>(StringComparer.Ordinal);
        foreach (var f in chosen.Factors) diff[f.Key] = diff.GetValueOrDefault(f.Key) + f.Contribution;
        foreach (var f in focus.Factors) diff[f.Key] = diff.GetValueOrDefault(f.Key) - f.Contribution;
        string key = "noise";
        int best = 0;
        foreach (var kv in diff)
            if (kv.Key != "noise" && kv.Value > best) { best = kv.Value; key = kv.Key; }
        return key;
    }
}

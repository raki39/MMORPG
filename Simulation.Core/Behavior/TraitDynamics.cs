using Simulation.Core.Content;
using Simulation.Core.Events;
using Simulation.Core.Population;
using Simulation.Core.World;

namespace Simulation.Core.Behavior;

/// <summary>
/// Atributos vivos (D-036): os tracos da pessoa sobem com a pratica e com o que ela vive no
/// jogo e caem com a inatividade, sempre dentro de uma faixa em torno da base de nascenca.
/// Mudanca visivel na escala 1–20 gera evento com a causa.
/// </summary>
public sealed class TraitDynamics
{
    private readonly WorldState _w;

    public TraitDynamics(WorldState world) => _w = world;

    private TraitDynamicsDefinition D => _w.Content.Dynamics;

    public static bool IsSkill(string key) => key is "mechanicalSkill" or "gameKnowledge" or "coordination";

    public static int Range(TraitDynamicsDefinition d, string key) => key switch
    {
        "mechanicalSkill" or "gameKnowledge" or "coordination" => d.RangeSkill,
        "pve" or "pvp" or "trade" or "exploration" or "socializing" => d.RangePlaystyle,
        _ => d.RangePersonality,
    };

    /// <summary>Piso do traco: a base menos a faixa de queda.</summary>
    public static int FloorOf(TraitDynamicsDefinition d, PersonTraits baseTraits, string key) => Math.Max(0, baseTraits.Get(key) - Range(d, key));

    /// <summary>
    /// Teto do traco. Capacidades: potencial ABSOLUTO pelo talento (escolha consciente, D-036a):
    /// quem comeca baixo mas tem talento pode chegar alto; o inicio so garante uma folga minima.
    /// Gostos e personalidade: base + faixa (mudam pela experiencia, nao por talento).
    /// </summary>
    public static int CeilingOf(TraitDynamicsDefinition d, PersonTraits baseTraits, int talent, string key) => IsSkill(key)
        ? Math.Min(1000, Math.Max(baseTraits.Get(key) + d.PotentialMinHeadroom, d.PotentialBase + talent * d.PotentialPerTalent / 1000))
        : Math.Min(1000, baseTraits.Get(key) + Range(d, key));

    public int Floor(Person p, string key) => FloorOf(D, p.BaseTraits, key);
    public int Ceiling(Person p, string key) => CeilingOf(D, p.BaseTraits, p.Traits.Talent, key);

    /// <summary>Velocidade de aprendizado das capacidades pelo talento (permille do custo).</summary>
    public int LearnCostPermille(Person p, string key) => IsSkill(key)
        ? D.LearnCostAtZeroTalent + (D.LearnCostAtMaxTalent - D.LearnCostAtZeroTalent) * p.Traits.Talent / 1000
        : 1000;

    /// <summary>Escala 1–20 da interface (mesma conta do cliente).</summary>
    public static int Scale20(int permille) => Math.Clamp((permille * 19 + 500) / 1000 + 1, 1, 20);

    /// <summary>
    /// Especialidade que a pratica do tipo faz crescer (afinidade desenvolvida). A coleta nao
    /// alimenta mais o comercio (sao coisas separadas) e nao tem especialidade propria.
    /// </summary>
    public static string? AffinityTrait(string activityType) => ActivityScoring.SpecialtyOf(activityType);

    /// <summary>Muda o traco dentro da faixa; devolve a mudanca efetiva.</summary>
    public int Adjust(Person p, string trait, int delta, string cause)
    {
        int cur = p.Traits.Get(trait);
        int next = Math.Clamp(cur + delta, Floor(p, trait), Math.Max(Floor(p, trait), Ceiling(p, trait)));
        if (next == cur) return 0;
        p.Traits.Set(trait, next);
        int s0 = Scale20(cur), s1 = Scale20(next);
        if (s0 != s1)
            _w.Emit("trait_changed", EventImportance.Low, new[] { new EntityRef("person", p.Id) },
                new[] { new EventCause(cause, next - cur) }, $"{trait}:{s0}:{s1}");
        return next - cur;
    }

    /// <summary>Acumula pratica; a cada custoPorPonto pontos, o traco anda 1 na direcao.</summary>
    internal void Practice(Person p, string trait, int points, int costPerPoint, int sign, string cause)
    {
        if (points <= 0) return;
        if (sign > 0)
        {
            // Subir: talento acelera (capacidades) e fica mais lento perto do potencial (ate 3x).
            int cur = p.Traits.Get(trait), b = p.BaseTraits.Get(trait), cap = Ceiling(p, trait);
            int closeness = cur <= b || cap <= b ? 0 : Math.Min(1000, (cur - b) * 1000 / (cap - b));
            costPerPoint = Math.Max(1, costPerPoint * LearnCostPermille(p, trait) / 1000 * (1000 + closeness * D.NearCeilingSlowdown / 1000) / 1000);
        }
        // Acumulador por direcao, traco e fonte (cada fonte tem o seu custo por ponto).
        string key = (sign > 0 ? "+" : "-") + trait + "/" + cause;
        p.Practice.TryGetValue(key, out int acc);
        acc += points;
        int units = acc / costPerPoint;
        acc -= units * costPerPoint;
        if (acc == 0) p.Practice.Remove(key); else p.Practice[key] = acc;
        if (units > 0) Adjust(p, trait, sign * units, cause);
    }

    /// <summary>
    /// Fim de uma atividade: pratica das capacidades, experiencia positiva persistente (reforca o
    /// gosto) e confianca diante do risco. Repetir nao reduz o gosto: gera tedio na decisao
    /// (fator de repeticao), nao uma mudanca de preferencia. Coordenacao so evolui em atividades
    /// cooperativas (grupos, M6).
    /// </summary>
    internal void OnActivity(Person p, Character ch, ActivityDefinition a, int minutes, int levelFit, int risk, bool firstTimeType)
    {
        if (minutes <= 0) return;
        var d = D;
        bool combat = a.Type is ActivityTypes.Hunt or ActivityTypes.Boss or ActivityTypes.Patrol or ActivityTypes.Ambush;
        if (combat) Practice(p, "mechanicalSkill", minutes, d.MechanicalCombatMinutes, +1, "practice_combat");
        if (a.Type == ActivityTypes.Explore) Practice(p, "gameKnowledge", minutes, d.KnowledgeExploreMinutes, +1, "exploration");
        else Practice(p, "gameKnowledge", minutes, d.KnowledgePlayMinutes, +1, "practice_play");
        if (firstTimeType) Adjust(p, "gameKnowledge", d.KnowledgeNewActivityType, "new_activity_type");
        if (a.Type == ActivityTypes.Socialize) Practice(p, "sociability", minutes, d.SociabilitySocialMinutes, +1, "practice_social");
        if (AffinityTrait(a.Type) is { } aff && levelFit >= d.EnjoyedMinFitPermille)
            Practice(p, aff, minutes, d.AffinityEnjoyedMinutes, +1, "enjoyed_activity");
        if (risk >= d.RiskConfidenceMinRisk)
            Practice(p, "riskAversion", minutes, d.RiskConfidenceMinutes, -1, "faced_risk");
    }

    internal void OnRegionDiscovered(Person p)
    {
        Adjust(p, "gameKnowledge", D.KnowledgePerDiscovery, "discovery");
        Practice(p, "exploration", 1, D.ExplorationPerDiscoveries, +1, "discovery");
    }

    internal void OnQuestCompleted(Person p) => Adjust(p, "gameKnowledge", D.KnowledgePerQuest, "quest_completed");

    /// <summary>
    /// Gancho do combate (M3): abater outro jogador e uma experiencia positiva de PvP (reforca o
    /// gosto) e pratica de combate (mecanica). Gostar de PvP nao melhora a execucao por si.
    /// </summary>
    public void OnPvpKill(Person killer)
    {
        Adjust(killer, "pvp", D.PvpKillPreference, "pvp_kill");
        Practice(killer, "mechanicalSkill", D.PvpKillCombatMinutes, D.MechanicalCombatMinutes, +1, "practice_combat");
    }

    /// <summary>Meia-noite: inatividade enferruja a mecanica e envelhece o conhecimento.</summary>
    internal void OnDaily(Person p, long day)
    {
        if (p.CharacterId == 0 || p.LastPlayedDay < 0 || p.IsOnline) return;
        long off = day - p.LastPlayedDay;
        if (off > D.MechanicalDecayAfterDays) Adjust(p, "mechanicalSkill", -D.MechanicalDecayPerDay, "inactivity_week");
        if (off > D.KnowledgeDecayAfterDays) Adjust(p, "gameKnowledge", -D.KnowledgeDecayPerDay, "inactivity_long");
    }
}

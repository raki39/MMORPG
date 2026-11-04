using Simulation.Core.Content;
using Simulation.Core.Events;
using Simulation.Core.Population;
using Simulation.Core.World;

namespace Simulation.Core.Behavior;

/// <summary>
/// Hipoteses dos atributos vivos (D-036). Pontos de partida para calibrar, nao regras do mundo.
/// Unidades: permille de traco, minutos simulados de pratica, dias.
/// </summary>
public static class TraitTuning
{
    /// <summary>Quanto cada traco pode cair abaixo da base (permille).</summary>
    public const int RangeSkill = 250, RangePlaystyle = 200, RangePersonality = 100;
    /// <summary>Potencial das capacidades acima da base: 50 + talento * 350 / 1000 (noob ~ +85, genio ~ +380).</summary>
    public const int SkillHeadroomBase = 50, SkillHeadroomPerTalent = 350;

    // ---- Pratica (minutos por ponto, antes do talento e da desaceleracao) ----
    // Ritmo lento, como os atributos de um jogo de gestao esportiva: um jogador ativo ganha
    // cerca de 1 ponto da escala 1–20 por temporada de 2 a 3 meses.
    public const int MechanicalPerCombatMinutes = 150;
    public const int KnowledgePerPlayMinutes = 400;
    public const int KnowledgePerExploreMinutes = 90;
    public const int KnowledgeNewActivityType = 3;
    public const int KnowledgePerDiscovery = 2;
    public const int KnowledgePerQuest = 1;
    public const int CoordinationPerGroupMinutes = 240;
    public const int SociabilityPerSocialMinutes = 600;
    /// <summary>Atividade com bom encaixe de nivel reforca o gosto pelo tipo.</summary>
    public const int AffinityPerEnjoyedMinutes = 360;
    public const int EnjoyedMinFitPermille = 600;
    /// <summary>Repetir sem paciencia cansa o gosto pelo tipo.</summary>
    public const int AffinityFatiguePerMinutes = 300;
    public const int FatigueRepeatCount = 4, FatiguePatienceBelow = 400;
    /// <summary>Encarar risco sem perder (sem mortes ate o M3) reduz a aversao a risco.</summary>
    public const int RiskConfidencePerMinutes = 600;
    public const int RiskConfidenceMinRisk = 300;
    public const int ExplorationPerDiscoveries = 4;

    // ---- Inatividade (queda lenta: cerca de 1 ponto da escala a cada 50 dias parado) ----
    /// <summary>Mais de 7 dias sem jogar: a mecanica enferruja (pontos por dia).</summary>
    public const int MechanicalDecayAfterDays = 7, MechanicalDecayPerDay = 1;
    /// <summary>Mais de 14 dias sem jogar: o conhecimento do jogo envelhece (atualizacoes no M7).</summary>
    public const int KnowledgeDecayAfterDays = 14, KnowledgeDecayPerDay = 1;

    // ---- Objetivo concluido reforca o motivo ----
    public const int GoalMotiveReinforce = 1;
}

/// <summary>
/// Atributos vivos (D-036): os tracos da pessoa sobem com a pratica e com o que ela vive no
/// jogo e caem com a inatividade, sempre dentro de uma faixa em torno da base de nascenca.
/// Mudanca visivel na escala 1–20 gera evento com a causa.
/// </summary>
public sealed class TraitDynamics
{
    private readonly WorldState _w;

    public TraitDynamics(WorldState world) => _w = world;

    public static bool IsSkill(string key) => key is "mechanicalSkill" or "gameKnowledge" or "coordination";

    public static int Range(string key) => key switch
    {
        "mechanicalSkill" or "gameKnowledge" or "coordination" => TraitTuning.RangeSkill,
        "pve" or "pvp" or "trade" or "exploration" or "socializing" => TraitTuning.RangePlaystyle,
        _ => TraitTuning.RangePersonality,
    };

    /// <summary>Piso do traco: a base menos a faixa de queda.</summary>
    public static int Floor(Person p, string key) => FloorOf(p.BaseTraits, key);
    public static int FloorOf(PersonTraits baseTraits, string key) => Math.Max(0, baseTraits.Get(key) - Range(key));

    /// <summary>
    /// Teto (potencial) do traco. Capacidades: base + folga pelo talento da pessoa como jogador
    /// (noob quase nao passa de onde comecou; genio pode subir muito). Gostos e personalidade:
    /// base + faixa (mudam pela experiencia, nao por talento).
    /// </summary>
    public static int Ceiling(Person p, string key) => CeilingOf(p.BaseTraits, p.Traits.Talent, key);
    public static int CeilingOf(PersonTraits baseTraits, int talent, string key) => IsSkill(key)
        ? Math.Min(1000, baseTraits.Get(key) + TraitTuning.SkillHeadroomBase + talent * TraitTuning.SkillHeadroomPerTalent / 1000)
        : Math.Min(1000, baseTraits.Get(key) + Range(key));

    /// <summary>Velocidade de aprendizado das capacidades pelo talento (permille do custo: 600 genio – 1400 noob).</summary>
    public static int LearnCostPermille(Person p, string key) => IsSkill(key) ? 1400 - p.Traits.Talent * 8 / 10 : 1000;

    /// <summary>Escala 1–20 da interface (mesma conta do cliente).</summary>
    public static int Scale20(int permille) => Math.Clamp((permille * 19 + 500) / 1000 + 1, 1, 20);

    /// <summary>Traco de estilo de jogo reforcado (ou cansado) por um tipo de atividade.</summary>
    public static string? AffinityTrait(string activityType) => activityType switch
    {
        ActivityTypes.Hunt or ActivityTypes.Boss => "pve",
        ActivityTypes.Gather or ActivityTypes.Trade => "trade",
        ActivityTypes.Explore => "exploration",
        ActivityTypes.Socialize => "socializing",
        ActivityTypes.Patrol or ActivityTypes.Ambush => "pvp",
        _ => null,
    };

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
            costPerPoint = Math.Max(1, costPerPoint * LearnCostPermille(p, trait) / 1000 * (1000 + closeness * 2) / 1000);
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

    /// <summary>Fim de uma atividade: pratica, gosto (ou cansaco) e confianca diante do risco.</summary>
    internal void OnActivity(Person p, Character ch, ActivityDefinition a, int minutes, int levelFit, int risk, bool firstTimeType)
    {
        if (minutes <= 0) return;
        bool combat = a.Type is ActivityTypes.Hunt or ActivityTypes.Boss or ActivityTypes.Patrol or ActivityTypes.Ambush;
        if (combat) Practice(p, "mechanicalSkill", minutes, TraitTuning.MechanicalPerCombatMinutes, +1, "practice_combat");
        if (a.Type == ActivityTypes.Explore) Practice(p, "gameKnowledge", minutes, TraitTuning.KnowledgePerExploreMinutes, +1, "exploration");
        else Practice(p, "gameKnowledge", minutes, TraitTuning.KnowledgePerPlayMinutes, +1, "practice_play");
        if (firstTimeType) Adjust(p, "gameKnowledge", TraitTuning.KnowledgeNewActivityType, "new_activity_type");
        if (a.Type is ActivityTypes.Socialize or ActivityTypes.Recruit)
        {
            Practice(p, "coordination", minutes, TraitTuning.CoordinationPerGroupMinutes, +1, "practice_group");
            Practice(p, "sociability", minutes, TraitTuning.SociabilityPerSocialMinutes, +1, "practice_social");
        }
        if (AffinityTrait(a.Type) is { } aff)
        {
            if (levelFit >= TraitTuning.EnjoyedMinFitPermille)
                Practice(p, aff, minutes, TraitTuning.AffinityPerEnjoyedMinutes, +1, "enjoyed_activity");
            if (ch.RepeatCount >= TraitTuning.FatigueRepeatCount && p.Traits.Patience < TraitTuning.FatiguePatienceBelow)
                Practice(p, aff, minutes, TraitTuning.AffinityFatiguePerMinutes, -1, "repetition_fatigue");
        }
        if (risk >= TraitTuning.RiskConfidenceMinRisk)
            Practice(p, "riskAversion", minutes, TraitTuning.RiskConfidencePerMinutes, -1, "faced_risk");
    }

    internal void OnRegionDiscovered(Person p)
    {
        Adjust(p, "gameKnowledge", TraitTuning.KnowledgePerDiscovery, "discovery");
        Practice(p, "exploration", 1, TraitTuning.ExplorationPerDiscoveries, +1, "discovery");
    }

    internal void OnQuestCompleted(Person p) => Adjust(p, "gameKnowledge", TraitTuning.KnowledgePerQuest, "quest_completed");

    /// <summary>Gancho do combate (M3): abater outro jogador reforca o gosto por PvP e a agressividade.</summary>
    public void OnPvpKill(Person killer)
    {
        Adjust(killer, "pvp", 3, "pvp_kill");
        Practice(killer, "aggressiveness", 1, 3, +1, "pvp_kill");
    }

    /// <summary>Meia-noite: inatividade enferruja a mecanica e envelhece o conhecimento.</summary>
    internal void OnDaily(Person p, long day)
    {
        if (p.CharacterId == 0 || p.LastPlayedDay < 0 || p.IsOnline) return;
        long off = day - p.LastPlayedDay;
        if (off > TraitTuning.MechanicalDecayAfterDays) Adjust(p, "mechanicalSkill", -TraitTuning.MechanicalDecayPerDay, "inactivity_week");
        if (off > TraitTuning.KnowledgeDecayAfterDays) Adjust(p, "gameKnowledge", -TraitTuning.KnowledgeDecayPerDay, "inactivity_long");
    }
}

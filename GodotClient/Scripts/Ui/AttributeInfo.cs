using Simulation.Core.Population;

namespace GodotClient.Ui;

/// <summary>
/// O que cada atributo do jogador (a pessoa, nao o personagem) faz na simulacao e como muda
/// (D-036). Texto ao jogador; deve acompanhar ActivityScoring, TraitDynamics e GoalSystem.
/// Referencia completa em docs/atributos.md.
/// </summary>
public static class AttributeInfo
{
    public sealed record Entry(string Name, string Influences, string Changes, bool Skill);

    public static readonly IReadOnlyDictionary<string, Entry> ByKey = new Dictionary<string, Entry>(StringComparer.Ordinal)
    {
        ["pve"] = new("PvE", "Vontade de caçar e enfrentar chefes; entra também no gosto por missões e em objetivos de nível.",
            "Sobe devagar ao caçar com bom encaixe de nível; cansa se repetir muito sem paciência.", false),
        ["pvp"] = new("PvP", "Vontade de patrulhar e emboscar outros jogadores. No combate (M3) também fará o jogador se sair melhor em PvP.",
            "Sobe ao patrulhar ou emboscar com gosto e ao abater jogadores (M3).", false),
        ["trade"] = new("Comércio", "Vontade de coletar, comprar e vender; escolhe objetivos de praticar coleta ou comércio.",
            "Sobe ao coletar ou negociar com bom encaixe; cansa com repetição sem paciência.", false),
        ["exploration"] = new("Exploração", "Vontade de explorar e de fazer missões; puxa objetivos de descobrir zonas.",
            "Sobe ao explorar e a cada 4 zonas descobertas.", false),
        ["socializing"] = new("Socializar", "Vontade de ir à taverna e de jogar onde há gente.",
            "Sobe ao socializar com gosto.", false),
        ["sociability"] = new("Sociabilidade", "Quanto valoriza companhia na taverna; pesa na escolha de classe de suporte.",
            "Sobe bem devagar com tempo em grupo ou na taverna.", false),
        ["aggressiveness"] = new("Agressividade", "Gosto por desafio acima do próprio nível e por emboscadas; puxa objetivos de nível; pesa na classe tanque.",
            "Sobe com abates de jogadores (M3).", false),
        ["patience"] = new("Paciência", "Tolera repetir a mesma atividade; aumenta o apego aos objetivos e permite manter dois ao mesmo tempo; pesa na coleta.",
            "Traço de personalidade: estável por enquanto.", false),
        ["loyalty"] = new("Lealdade", "Prefere continuar na zona onde estava jogando; aumenta o apego aos objetivos; pesa na patrulha e na classe tanque. Guildas no M6.",
            "Traço de personalidade: estável por enquanto.", false),
        ["riskAversion"] = new("Aversão a risco", "Pesa contra atividades arriscadas, PvP aberto e conteúdo acima do nível.",
            "Cai devagar ao enfrentar risco sem perder; mortes (M3) a farão subir.", false),
        ["mechanicalSkill"] = new("Mecânica", "Rendimento em combate (XP de caça de ×0,88 a ×1,12) e menos medo de risco. No combate (M3) será a habilidade de execução do jogador.",
            "Sobe com tempo em combate: mais rápido com talento, mais devagar perto do potencial. Cai depois de 7 dias sem jogar.", true),
        ["gameKnowledge"] = new("Conhecimento", "Rendimento geral (×0,95 a ×1,05), deslocamentos mais curtos e decisões mais consistentes.",
            "Sobe jogando, explorando, descobrindo zonas e concluindo missões. Cai depois de 14 dias sem jogar (atualizações no M7).", true),
        ["coordination"] = new("Coordenação", "Convive melhor com lugares cheios na caça e na coleta; pesa em chefes e na classe tanque. Grupos no M6.",
            "Sobe com tempo em grupo ou na taverna: mais rápido com talento.", true),
        ["cheatPropensity"] = new("Trapaça", "Sem efeito até os incidentes (M9).", "Estável.", false),
        ["opportunism"] = new("Oportunismo", "Foge mais de lugares disputados; pesa na emboscada. Incidentes no M9.", "Estável por enquanto.", false),
    };

    /// <summary>Faixa de talento do jogador (a pessoa): potencial e velocidade de aprender.</summary>
    public static string TalentTier(int talent) => talent switch
    {
        < 150 => "Noob",
        < 350 => "Inexperiente",
        < 600 => "Padrão",
        < 800 => "Experiente",
        < 930 => "Prodígio",
        _ => "Gênio",
    };

    public static string TalentHint(int talent) => talent switch
    {
        < 150 => "Aprende devagar e quase não passa de onde começou.",
        < 350 => "Evolui pouco além do nível de entrada.",
        < 600 => "Evolução normal com a prática.",
        < 800 => "Aprende rápido e tem margem para crescer.",
        < 930 => "Aprende muito rápido; alto potencial.",
        _ => "Potencial excepcional: pode chegar ao topo das capacidades.",
    };

    /// <summary>Causa de mudanca de atributo, em portugues.</summary>
    public static string CausePt(string cause) => cause switch
    {
        "practice_combat" => "prática em combate",
        "practice_play" => "tempo de jogo",
        "exploration" => "exploração",
        "new_activity_type" => "primeira vez numa atividade",
        "discovery" => "zona descoberta",
        "quest_completed" => "missão concluída",
        "practice_group" => "tempo em grupo",
        "practice_social" => "tempo na taverna",
        "enjoyed_activity" => "gostou da atividade",
        "repetition_fatigue" => "cansou de repetir",
        "faced_risk" => "enfrentou risco",
        "pvp_kill" => "abateu um jogador",
        "inactivity_week" => "mais de 7 dias sem jogar",
        "inactivity_long" => "mais de 14 dias sem jogar",
        "goal_completed" => "objetivo concluído",
        _ => cause,
    };

    public static string Name(string key) => ByKey.TryGetValue(key, out var e) ? e.Name : key;

    public static int Get(PersonTraits t, string key) => t.Get(key);
}

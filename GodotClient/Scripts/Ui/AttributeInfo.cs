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
        // Gostos (estilo de jogo): decidem a vontade de fazer, nunca a qualidade da execucao.
        ["pve"] = new("PvE", "Vontade de caçar e enfrentar chefes; entra no gosto por missões e em metas de nível. Não melhora a execução.",
            "Sobe devagar com experiências boas e persistentes de caça. Repetir demais gera tédio na hora de decidir, mas não muda o gosto.", false),
        ["pvp"] = new("PvP", "Vontade de patrulhar, emboscar e entrar em PvP. Não melhora a execução: no combate (M3), quem decide é Mecânica, Conhecimento e Coordenação.",
            "Sobe com experiências boas de PvP e ao abater jogadores (M3).", false),
        ["trade"] = new("Comércio", "Vontade de coletar, comprar e vender; puxa metas de praticar coleta ou comércio.",
            "Sobe com experiências boas de coleta ou comércio.", false),
        ["exploration"] = new("Exploração", "Vontade de explorar e de fazer missões; puxa metas de descobrir zonas e pesa um pouco na necessidade de novidade. A vontade de variar é outro traço (Variedade).",
            "Sobe com experiências boas de exploração e a cada 4 zonas descobertas.", false),
        ["socializing"] = new("Socializar", "Vontade de ir à taverna e de jogar onde há gente.",
            "Sobe com experiências boas na taverna.", false),
        // Personalidade.
        ["sociability"] = new("Sociabilidade", "Quanto valoriza companhia na taverna; pesa na escolha de classe de suporte.",
            "Sobe bem devagar com tempo na taverna.", false),
        ["aggressiveness"] = new("Agressividade", "Competitividade. Junto com a confiança (Mecânica) e a pouca aversão a risco, forma o gosto por desafio acima do nível; pesa em emboscadas, metas de nível e na classe tanque.",
            "Traço de personalidade: estável por enquanto.", false),
        ["patience"] = new("Paciência", "Tolera repetir a mesma atividade e persiste nas metas por mais tempo antes de desistir; pesa na coleta. Não muda quantas metas a pessoa tem.",
            "Traço de personalidade: estável por enquanto.", false),
        ["loyalty"] = new("Lealdade", "Apego às metas assumidas; pesa na patrulha e na classe tanque. Fidelidade a grupos e guildas no M6.",
            "Traço de personalidade: estável por enquanto.", false),
        ["variety"] = new("Variedade", "Preferência por variedade: cansa mais rápido de repetir o mesmo tipo de atividade, sente menos atração pelas zonas familiares e precisa mais de novidade. Quem tem pouca repete o que funciona sem se cansar.",
            "Traço de personalidade: estável por enquanto.", false),
        ["riskAversion"] = new("Aversão a risco", "Pesa contra atividades arriscadas e PvP aberto e reduz o gosto por desafio acima do nível.",
            "Cai devagar ao enfrentar risco sem perder; mortes (M3) a farão subir.", false),
        // Capacidades: a execucao. Evoluem com pratica, talento e potencial.
        ["mechanicalSkill"] = new("Mecânica", "Execução: rendimento em combate (XP de caça de ×0,88 a ×1,12), menos medo de risco e confiança para desafios acima do nível. No combate (M3), a execução em PvE e PvP.",
            "Sobe com tempo em combate: cerca de 130 h por ponto com talento moderado, mais rápido com talento alto e mais devagar perto do potencial. Cai depois de 7 dias sem jogar.", true),
        ["gameKnowledge"] = new("Conhecimento", "Rendimento geral (×0,95 a ×1,05), estimativa mais precisa do tempo de viagem (o tempo real é o mesmo para todos) e decisões mais consistentes.",
            "Sobe jogando, explorando, descobrindo zonas e concluindo missões. Cai depois de 14 dias sem jogar (atualizações no M7).", true),
        ["coordination"] = new("Coordenação", "Convive melhor com lugares cheios na caça e na coleta; pesa em chefes e na classe tanque. Execução em grupo no M6.",
            "Evolui só em atividades cooperativas (grupos e raides, M6): por enquanto estável.", true),
        ["cheatPropensity"] = new("Trapaça", "Sem efeito até os incidentes (M9).", "Estável.", false),
        ["opportunism"] = new("Oportunismo", "Foge mais de lugares disputados; pesa na emboscada. Incidentes no M9.", "Estável por enquanto.", false),
    };

    /// <summary>
    /// Faixa de talento do jogador (a pessoa): potencial e velocidade de aprender as capacidades.
    /// Talento nao e experiencia: a experiencia vem da pratica (ExperienceTier).
    /// </summary>
    /// <summary>Texto corrido em marcadores do RichTip ("- item" por frase ou oracao), sem ponto final.</summary>
    public static string Bullets(string text)
    {
        var items = System.Text.RegularExpressions.Regex.Split(text.Trim(), @"(?<=\.)\s+|;\s+")
            .Select(s => s.Trim().TrimEnd('.').Trim())
            .Where(s => s.Length > 0)
            .Select(s => char.ToUpper(s[0], System.Globalization.CultureInfo.GetCultureInfo("pt-BR")) + s[1..]);
        return string.Join("\n", items.Select(s => "- " + s));
    }

    /// <summary>Grupo do atributo na ficha (subtitulo da dica).</summary>
    public static string GroupOf(string key) => key switch
    {
        "pve" or "pvp" or "trade" or "exploration" or "socializing" => "Estilo de jogo · gosto, não execução",
        "mechanicalSkill" or "gameKnowledge" or "coordination" => "Capacidade · evolui com prática e talento",
        "cheatPropensity" or "opportunism" => "Conduta",
        _ => "Personalidade",
    };

    public static string TalentTier(int talent) => talent switch
    {
        < 300 => "Baixo",
        < 600 => "Moderado",
        < 800 => "Alto",
        _ => "Excepcional",
    };

    public static string TalentHint(int talent) => talent switch
    {
        < 300 => "Aprende devagar e tem potencial limitado nas capacidades.",
        < 600 => "Aprende no ritmo normal; potencial mediano.",
        < 800 => "Aprende rápido e pode ir longe nas capacidades.",
        _ => "Aprende muito rápido; pode chegar ao topo das capacidades.",
    };

    /// <summary>Experiencia: pratica neste jogo (horas jogadas), separada do talento.</summary>
    public static string ExperienceTier(double hours) => hours switch
    {
        < 10 => "Novato",
        < 50 => "Iniciante",
        < 200 => "Experiente",
        _ => "Veterano",
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
        "faced_risk" => "enfrentou risco",
        "pvp_kill" => "abateu um jogador",
        "inactivity_week" => "mais de 7 dias sem jogar",
        "inactivity_long" => "mais de 14 dias sem jogar",
        "goal_completed" => "meta concluída",
        _ => cause,
    };

    public static string Name(string key) => ByKey.TryGetValue(key, out var e) ? e.Name : key;

    public static int Get(PersonTraits t, string key) => t.Get(key);
}

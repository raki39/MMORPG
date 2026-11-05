using Simulation.Core.Population;

namespace GodotClient.Ui;

/// <summary>
/// O que cada atributo do jogador (a pessoa, nao o personagem) faz na simulacao e como muda
/// (D-036). Texto ao jogador; deve acompanhar ActivityScoring, TraitDynamics e GoalSystem.
/// Referencia completa em docs/atributos.md. Preferencias (interesses) ficam em PreferenceInfo:
/// sao um campo ao lado do Perfil, nao um grupo de atributos.
/// </summary>
public static class AttributeInfo
{
    public sealed record Entry(string Name, string Influences, string Changes, bool Skill);

    public static readonly IReadOnlyDictionary<string, Entry> ByKey = new Dictionary<string, Entry>(StringComparer.Ordinal)
    {
        // Especialidade: afinidade desenvolvida e familiaridade. O gosto e da Preferencia.
        ["pve"] = new("PvE", "Familiaridade com caça e chefes. Dá um reforço mínimo no gosto por essas atividades (5% do valor, no máximo +50 na escala de 0 a 1000 da afinidade); o gosto principal vem da preferência PvE. Benefícios de execução ainda não existem (virão com o combate).",
            "Sobe com experiências boas de caça e chefes e ao concluir metas motivadas pela preferência PvE.", false),
        ["pvp"] = new("PvP", "Familiaridade com patrulha e emboscada. Reforço mínimo no gosto por elas; o gosto principal vem da preferência PvP. Benefícios de execução ainda não existem (virão com o PvP, Etapa 3).",
            "Sobe com experiências boas de PvP e ao abater jogadores (Etapa 3).", false),
        ["trade"] = new("Comércio", "Familiaridade com comprar e vender. Reforço mínimo no gosto pelo comércio; o gosto principal vem da preferência Comércio. A coleta é outra coisa e não usa este atributo. Benefícios de execução ainda não existem (virão com a economia, M5).",
            "Sobe com experiências boas de comércio.", false),
        ["exploration"] = new("Exploração", "Familiaridade com explorar. Reforço mínimo no gosto por explorar; o gosto principal vem da preferência Exploração. Benefícios de execução ainda não existem.",
            "Sobe com experiências boas de exploração e a cada 4 zonas descobertas.", false),
        ["socializing"] = new("Socializar", "Familiaridade com a vida social do jogo. Reforço mínimo no gosto pela taverna; o gosto principal vem da preferência Social. Benefícios de execução ainda não existem (virão com os grupos, M6).",
            "Sobe com experiências boas na taverna.", false),
        // Personalidade.
        ["sociability"] = new("Sociabilidade", "Como a pessoa se relaciona: valoriza companhia na taverna, aceita mais compromissos sociais fora do jogo e pesa na classe de suporte. Puxa um pouco a preferência Social, sem decidir por ela.",
            "Sobe bem devagar com tempo na taverna.", false),
        ["aggressiveness"] = new("Agressividade", "Confronto: pesa na emboscada e na classe tanque e puxa um pouco a preferência PvP. Não é competitividade (outro atributo, em Conduta). PK no futuro.",
            "Traço de personalidade: estável por enquanto.", false),
        ["patience"] = new("Paciência", "Tolera repetir a mesma atividade (alivia o tédio) e persiste nas metas por mais tempo antes de desistir. Não muda quantas metas a pessoa tem.",
            "Traço de personalidade: estável por enquanto.", false),
        ["loyalty"] = new("Lealdade", "Apego às metas assumidas; pesa na patrulha e na classe tanque. Fidelidade a grupos e guildas no M6.",
            "Traço de personalidade: estável por enquanto.", false),
        ["variety"] = new("Variedade", "Preferência por variedade: cansa mais rápido de repetir o mesmo tipo de atividade, sente menos atração pelas zonas familiares e precisa mais de novidade. Puxa um pouco a preferência Exploração.",
            "Traço de personalidade: estável por enquanto.", false),
        ["riskAversion"] = new("Aversão a risco", "Pesa contra atividades arriscadas e PvP aberto e reduz o gosto por desafio acima do nível.",
            "Cai devagar ao enfrentar risco sem perder; mortes (M3) a farão subir.", false),
        // Capacidades: a execucao. Evoluem com pratica, talento e potencial.
        ["mechanicalSkill"] = new("Mecânica", "Execução: rendimento em combate (XP de caça de ×0,88 a ×1,12), menos medo de risco e confiança para desafios acima do nível. No combate (M3), a execução em PvE e PvP.",
            "Sobe com tempo em combate: cerca de 130 h por ponto com talento moderado, mais rápido com talento alto e mais devagar perto do potencial. Cai depois de 7 dias sem jogar.", true),
        ["gameKnowledge"] = new("Conhecimento", "Rendimento geral (×0,95 a ×1,05), estimativa mais precisa do tempo de viagem (o tempo real é o mesmo para todos) e decisões mais consistentes.",
            "Sobe jogando, explorando, descobrindo zonas e concluindo missões. Cai depois de 14 dias sem jogar (atualizações no M7).", true),
        ["coordination"] = new("Coordenação", "Convive melhor com lugares cheios na caça e na coleta; pesa na classe tanque. Execução em grupo no M6.",
            "Evolui só em atividades cooperativas (grupos e raides, M6): por enquanto estável.", true),
        // Conduta.
        ["cheatPropensity"] = new("Trapaça", "Sem efeito até os incidentes (M9).", "Estável.", false),
        ["opportunism"] = new("Oportunismo", "Foge mais de lugares disputados; pesa na emboscada. Incidentes no M9.", "Estável por enquanto.", false),
        ["competitiveness"] = new("Competitividade", "Motivação para disputar, superar outros e ser reconhecido pelo desempenho. Deve acompanhar os interesses da pessoa: um comerciante competitivo quer ser o melhor no comércio, não necessariamente chegar a nível alto. Hoje os efeitos são parciais e só na progressão: gosto por desafio acima do nível, metas de nível (e zonas mais altas aceitas), expectativa de progresso e quanto liga para evoluir. Rankings, mercado, fabricação e PvP entram com os sistemas. Não decide agressividade, PK, habilidade nem horas jogadas.",
            "Estável por enquanto.", false),
    };

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
        "pve" or "pvp" or "trade" or "exploration" or "socializing" => "Especialidade · afinidade desenvolvida",
        "mechanicalSkill" or "gameKnowledge" or "coordination" => "Capacidade · evolui com prática e talento",
        "cheatPropensity" or "opportunism" or "competitiveness" => "Conduta",
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

    /// <summary>Nome de um atributo ou de um motivo de meta ("pref_x" = preferencia).</summary>
    public static string Name(string key) =>
        key.StartsWith(Person.PreferenceMotivePrefix, StringComparison.Ordinal) && PreferenceInfo.ByKey.TryGetValue(key[Person.PreferenceMotivePrefix.Length..], out var p)
            ? $"{p.Name} (preferência)"
            : ByKey.TryGetValue(key, out var e) ? e.Name : key;

    public static int Get(PersonTraits t, string key) => t.Get(key);
}

/// <summary>
/// Preferencias: interesses pessoais (campo ao lado do Perfil). Dao o peso principal de gosto
/// nas decisoes, sem obrigar: viagem, risco, tedio, metas, lotacao e tempo competem com elas.
/// </summary>
public static class PreferenceInfo
{
    public sealed record Entry(string Name, string Icon, string Influences);

    public static readonly IReadOnlyDictionary<string, Entry> ByKey = new Dictionary<string, Entry>(StringComparer.Ordinal)
    {
        ["pve"] = new("PvE", "sword", "Gosto por caçar, enfrentar chefes e fazer missões; pesa em metas de nível e no cuidado com o progresso."),
        ["pvp"] = new("PvP", "swords", "Gosto por patrulhar, emboscar e lutar contra jogadores (o PvP chega na Etapa 3); pesa na classe de dano."),
        ["crafting"] = new("Crafting", "hammer", "Gosto por fabricar. Ainda sem efeito: a fabricação não existe no jogo. Não implica gostar de vender nem de coletar."),
        ["trade"] = new("Comércio", "coins", "Gosto por comprar e vender."),
        ["gathering"] = new("Coleta", "pickaxe", "Gosto por coletar materiais, separado do comércio e da fabricação."),
        ["exploration"] = new("Exploração", "compass", "Gosto por explorar e descobrir zonas; pesa nas missões, nas metas de descoberta e na necessidade de novidade."),
        ["social"] = new("Social", "users", "Gosto por atividades sociais e por jogar onde há gente; pesa na classe de suporte."),
    };

    /// <summary>Nivel de uma preferencia (permille) em palavras.</summary>
    public static string Level(int v) => v switch
    {
        >= 750 => "muito forte",
        >= 550 => "forte",
        >= 350 => "moderada",
        >= 150 => "fraca",
        _ => "quase nenhuma",
    };

    /// <summary>
    /// Uma ou duas preferencias predominantes (para destacar na ficha e filtrar). A segunda so
    /// aparece se estiver perto da primeira; as decisoes usam todos os valores, nao este resumo.
    /// </summary>
    public static IReadOnlyList<string> Predominant(PersonPreferences pr)
    {
        var order = PersonPreferences.Keys.OrderByDescending(pr.Get).ThenBy(k => k, StringComparer.Ordinal).ToList();
        int top = pr.Get(order[0]);
        var list = new List<string> { order[0] };
        if (order.Count > 1 && pr.Get(order[1]) >= 450 && top - pr.Get(order[1]) <= 120) list.Add(order[1]);
        return list;
    }

    public static string Summary(PersonPreferences pr) => string.Join(" e ", Predominant(pr).Select(k => ByKey[k].Name));
}

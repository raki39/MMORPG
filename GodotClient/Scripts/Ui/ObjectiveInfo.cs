using Godot;
using Simulation.Core.Content;
using Simulation.Core.Population;

namespace GodotClient.Ui;

/// <summary>Textos e cores dos objetivos pessoais (O1) para a ficha.</summary>
public static class ObjectiveInfo
{
    public static string HorizonPt(string h) => h switch
    {
        ObjectiveHorizons.Short => "Curto prazo",
        ObjectiveHorizons.Medium => "Médio prazo",
        ObjectiveHorizons.Long => "Longo prazo",
        _ => h,
    };

    public static string TierPt(string tier) => tier == ObjectiveTiers.General ? "Objetivo geral" : "Objetivo secundário";

    public static string ViabilityPt(string v) => v switch
    {
        ObjectiveViability.Now => "Pode avançar agora",
        ObjectiveViability.Preparation => "Exige preparação",
        _ => "Depende de sistema futuro",
    };

    public static Color ViabilityColor(string v) => v switch
    {
        ObjectiveViability.Now => UiTokens.Success,
        ObjectiveViability.Preparation => UiTokens.Warning,
        _ => UiTokens.TextSecondary,
    };

    /// <summary>Sistema que ainda nao existe (ou existe) e quando chega.</summary>
    public static string SystemPt(string s) => s switch
    {
        ObjectiveSystems.Progression => "Níveis",
        ObjectiveSystems.Quests => "Missões",
        ObjectiveSystems.Exploration => "Exploração",
        ObjectiveSystems.Combat => "Combate (Etapa 2)",
        ObjectiveSystems.Groups => "Grupos (Etapa 3)",
        ObjectiveSystems.Pvp => "PvP real (Etapa 3)",
        ObjectiveSystems.Ranking => "Classificações",
        ObjectiveSystems.Items => "Itens e inventário",
        ObjectiveSystems.Economy => "Economia (M5)",
        ObjectiveSystems.Guilds => "Guildas (M6)",
        ObjectiveSystems.Crafting => "Fabricação",
        _ => s,
    };

    public static string CategoryPt(string cat) => cat switch
    {
        ItemCategories.Weapon => "armas",
        ItemCategories.Armor => "armaduras",
        ItemCategories.Consumable => "consumíveis",
        _ => cat,
    };

    /// <summary>Nome do objetivo com o parametro (item ou especialidade).</summary>
    public static string Title(ContentDatabase c, string id, string param)
    {
        if (!c.Objectives.TryGetValue(id, out var d)) return id;
        if (param.Length == 0) return d.Name;
        return d.Criterion switch
        {
            ObjectiveCriteria.RareItem => $"{d.Name}: {(c.Items.TryGetValue(param, out var it) ? it.Name : param)}",
            ObjectiveCriteria.CraftingRank => $"{d.Name} de {CategoryPt(param)}",
            _ => d.Name,
        };
    }

    public static string Icon(ContentDatabase c, string id) =>
        c.Objectives.TryGetValue(id, out var d) && d.Icon.Length > 0 && Icons.Has(d.Icon) ? d.Icon : "target";

    /// <summary>Nome legivel de um motivo da escolha (afinidade ou ambicao).</summary>
    public static string MotivePt(ContentDatabase c, string key)
    {
        if (key == ObjectiveSystem.AmbitionKey) return "Dedicação ao jogo";
        int dot = key.IndexOf('.');
        string kind = dot > 0 ? key[..dot] : "", k = dot > 0 ? key[(dot + 1)..] : key;
        return kind switch
        {
            "preferences" => AttributeInfo.Name(Person.PreferenceMotivePrefix + k),
            "traits" => AttributeInfo.Name(k),
            "quirk" => $"Traço {(c.Quirks.TryGetValue(k, out var q) ? q.Name : k)}",
            "personality" => $"Personalidade {PlayerModal.PersonalityOf(c, k).Label}",
            "profile" => $"Perfil {(c.Profiles.TryGetValue(k, out var p) ? p.Name : k)}",
            _ => key,
        };
    }

    /// <summary>De onde vem um requisito de preparacao: missao, "zona/atividade", item ou o nivel maximo.</summary>
    public static string SubjectPt(ClientContext ctx, string subject)
    {
        var c = ctx.Content;
        if (subject.Length == 0) return "nível máximo";
        if (c.Quests.TryGetValue(subject, out var q)) return $"missão {q.Name}";
        if (c.Items.TryGetValue(subject, out var it)) return it.Name;
        int slash = subject.IndexOf('/');
        if (slash > 0) return $"{ctx.ActivityName(subject[..slash], subject[(slash + 1)..])} · {ctx.ZoneName(subject[..slash])}";
        return subject;
    }
}

namespace Simulation.Core.Content;

/// <summary>Faixa de um parametro: media e dispersao (mesma unidade do parametro).</summary>
public sealed class TraitRange
{
    public int Mean { get; set; }
    public int Spread { get; set; }
}

/// <summary>
/// Perfil (predefinicao) de populacao. Gera tendencias, nao clones: cada pessoa recebe
/// ruido individual em cada parametro. Comportamentais em permille (0..1000).
/// </summary>
public sealed class PopulationProfileDefinition : IContentDefinition
{
    public string Id { get; set; } = "";
    public int Version { get; set; }
    public string Name { get; set; } = "";
    /// <summary>Peso relativo na populacao gerada.</summary>
    public int Weight { get; set; }

    // Afinidades
    public TraitRange Pve { get; set; } = new();
    public TraitRange Pvp { get; set; } = new();
    public TraitRange Trade { get; set; } = new();
    public TraitRange Exploration { get; set; } = new();
    public TraitRange Socializing { get; set; } = new();

    // Personalidade
    public TraitRange Sociability { get; set; } = new();
    public TraitRange Aggressiveness { get; set; } = new();
    public TraitRange Patience { get; set; } = new();
    public TraitRange Loyalty { get; set; } = new();
    public TraitRange RiskAversion { get; set; } = new();

    // Capacidade
    public TraitRange MechanicalSkill { get; set; } = new();
    public TraitRange GameKnowledge { get; set; } = new();
    public TraitRange Coordination { get; set; } = new();

    // Conduta
    public TraitRange CheatPropensity { get; set; } = new();
    public TraitRange Opportunism { get; set; } = new();

    // Financas
    /// <summary>Orcamento de lazer em centavos (dinheiro real ficticio do tycoon) por mes.</summary>
    public TraitRange LeisureBudgetCentsPerMonth { get; set; } = new();
    public TraitRange WillingnessToPay { get; set; } = new();
    public TraitRange PaidAdvantageSensitivity { get; set; } = new();

    // Rotina
    /// <summary>Hora preferida de inicio de sessao (0..23).</summary>
    public TraitRange PreferredStartHour { get; set; } = new();
    public TraitRange SessionMinutes { get; set; } = new();
    /// <summary>Dias de jogo por semana em dias uteis+fim de semana (0..7).</summary>
    public TraitRange DaysPerWeek { get; set; } = new();
    /// <summary>Quanto fins de semana/feriados aumentam disponibilidade (permille, 0 = nada).</summary>
    public TraitRange WeekendBoost { get; set; } = new();
}

public sealed class NamePoolDefinition
{
    public List<string> FirstNames { get; set; } = new();
    public List<string> LastNames { get; set; } = new();
}

public sealed class PopulationDefinition
{
    public List<PopulationProfileDefinition> Profiles { get; set; } = new();
    public NamePoolDefinition Names { get; set; } = new();
}

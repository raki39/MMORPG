namespace Simulation.Core.Behavior;

/// <summary>
/// Hipoteses numericas do comportamento (M2). Todas sao pontos de partida para calibrar,
/// nao regras do mundo; mudar qualquer valor altera trajetorias (e o hash) das campanhas.
/// Unidades: permille, minutos simulados.
/// </summary>
public static class BehaviorTuning
{
    // ---- Aquisicao inicial (lancamento) ----
    /// <summary>Peso relativo do dia de registro apos a abertura (dia 0..6).</summary>
    public static readonly int[] RegistrationDayWeights = { 30, 22, 16, 12, 9, 6, 5 };

    // ---- Sessoes ----
    /// <summary>Variacao do horario de inicio em torno do preferido (minutos, +-).</summary>
    public const int SessionStartJitterMinutes = 75;
    /// <summary>Variacao da duracao (permille, +-).</summary>
    public const int SessionDurationJitterPermille = 200;
    /// <summary>Tolerancia para terminar a atividade antes de sair (plano 4.3).</summary>
    public const int LogoutToleranceMinutes = 15;

    // ---- Deslocamento (D-016) ----
    /// <summary>Velocidade a pe, em unidades de mapa por minuto (1 unidade = 10 m; 15 = 9 km/h).</summary>
    public const int WalkSpeed = 15;

    // ---- Pesos da utilidade (plano 4.4); termos normalizados em permille ----
    public const int WeightAffinity = 450;
    public const int WeightReward = 300;
    public const int WeightSocial = 150;
    public const int WeightRisk = 350;
    public const int WeightTravel = 250;
    public const int WeightCrowding = 200;
    /// <summary>Penalidade por repetir a mesma atividade em sequencia (escala com impaciencia).</summary>
    public const int RepeatPenalty = 140;
    /// <summary>Penalidade quando viagem + atividade nao cabem no tempo restante da sessao.</summary>
    public const int TimeBudgetPenalty = 400;
    /// <summary>Aleatoriedade pequena e reproduzivel (+-).</summary>
    public const int Noise = 50;

    /// <summary>Duracao real de uma atividade em relacao a tipica (permille).</summary>
    public const int ActivityDurationMin = 800, ActivityDurationMax = 1250;
    /// <summary>Espera quando nao ha nenhuma opcao viavel (minutos).</summary>
    public const int IdleRethinkMinutes = 20;

    // ---- Progressao (hipotese simples ate o combate do M3) ----
    /// <summary>XP por minuto de atividade, por tipo (antes do ajuste de adequacao ao nivel).</summary>
    public static int XpPerMinute(string activityType) => activityType switch
    {
        "hunt" => 12,
        "quest" => 10,
        "ambush" => 8,
        "patrol" => 8,
        "explore" => 6,
        "gather" => 5,
        "train" => 4,
        _ => 0,
    };

    /// <summary>XP necessario para sair do nivel atual.</summary>
    public static long XpToNextLevel(int level) => 60L * level * level + 140L * level;

    public const int MaxLevel = 60;
}

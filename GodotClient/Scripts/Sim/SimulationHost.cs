using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using Simulation.Core.Commands;
using Simulation.Core.Content;
using Simulation.Core.Engine;
using Simulation.Core.Events;
using Simulation.Core.Population;
using Simulation.Core.Time;
using Simulation.Core.World;

namespace GodotClient.Sim;

/// <summary>Estado de um personagem copiado para a tela (imutavel depois de publicado).</summary>
public sealed class CharacterView
{
    public int PersonId { get; init; }
    public int CharacterId { get; init; }
    public string Name { get; init; } = "";
    public string ClassId { get; init; } = "";
    public int Level { get; init; }
    public long Experience { get; init; }
    public long SessionStartTick { get; init; }
    public long ActivityStartTick { get; init; }
    public CharacterStatus Status { get; init; }
    public string RegionId { get; init; } = "";
    public string TargetRegionId { get; init; } = "";
    public string ActivityId { get; init; } = "";
    public MapPos Position { get; init; }
    public MapPos MoveTo { get; init; }
    public long MoveStartTick { get; init; }
    public long MoveEndTick { get; init; }
    public long ActivityEndTick { get; init; }
    public long SessionEndTick { get; init; }
    public int KnownRegions { get; init; }
    /// <summary>Registro imutavel criado pelo Core; seguro para ler fora da thread da simulacao.</summary>
    public DecisionRecord? LastDecision { get; init; }
    /// <summary>Quests em andamento (copia).</summary>
    public IReadOnlyList<QuestProgressView> ActiveQuests { get; init; } = Array.Empty<QuestProgressView>();
    /// <summary>Quests ja concluidas (ID e vezes).</summary>
    public IReadOnlyList<(string QuestId, int Times, long LastDay)> CompletedQuests { get; init; } = Array.Empty<(string, int, long)>();
    public long XpFromQuests { get; init; }
    public long XpFromActivities { get; init; }
    /// <summary>Tempo conectado acumulado (ticks) no momento da publicacao.</summary>
    public long PlayedTicks { get; init; }
    /// <summary>Objetivos pessoais em andamento (copia, D-036).</summary>
    public IReadOnlyList<GoalView> Goals { get; init; } = Array.Empty<GoalView>();
    public int GoalsSet { get; init; }
    public int GoalsCompleted { get; init; }
    public int GoalsAbandoned { get; init; }
    /// <summary>Situacao da pessoa no jogo (ativo, inativo, desistiu), D-039.</summary>
    public LifecycleState State { get; init; }
    /// <summary>Humor da pessoa (copia, D-038).</summary>
    public MoodView Mood { get; init; } = MoodView.Neutral;
    /// <summary>Plano da sessao em andamento (D-037); nulo fora de sessao.</summary>
    public PlanView? Plan { get; init; }
    public long LastPlayedDay { get; init; }

    /// <summary>Posicao no tick informado (mesma regra do Core: interpolacao do trecho).</summary>
    public (float X, float Y) PositionAt(long tick)
    {
        if (Status != CharacterStatus.Traveling || MoveEndTick <= MoveStartTick) return (Position.X, Position.Y);
        float t = Math.Clamp((tick - MoveStartTick) / (float)(MoveEndTick - MoveStartTick), 0f, 1f);
        return (Position.X + (MoveTo.X - Position.X) * t, Position.Y + (MoveTo.Y - Position.Y) * t);
    }
}

/// <summary>Detalhe da pessoa com a ficha aberta (copiado so para ela, a cada publicacao).</summary>
public sealed record CharacterDetailView(int PersonId, long PlayedTicks, IReadOnlyDictionary<string, long> ActivityTicks, IReadOnlyList<long> LevelTicks)
{
    /// <summary>Acontecimentos marcantes (D-038), mais fortes primeiro.</summary>
    public IReadOnlyList<MemoryView> Memories { get; init; } = Array.Empty<MemoryView>();
    /// <summary>O que tem agradado (+) ou incomodado (-), acumulado e esmaecendo.</summary>
    public IReadOnlyDictionary<string, int> Drivers { get; init; } = new Dictionary<string, int>();
    /// <summary>Lugares que mais marcaram: melhores e piores impressoes ("zona/atividade").</summary>
    public IReadOnlyList<(string Key, int Value)> Impressions { get; init; } = Array.Empty<(string, int)>();
    /// <summary>Tedio por tipo de atividade (D-037).</summary>
    public IReadOnlyDictionary<string, int> Boredom { get; init; } = new Dictionary<string, int>();
    /// <summary>Agenda dos ultimos dias (P2), mais recente no fim.</summary>
    public IReadOnlyList<DayPlanRecord> DayPlans { get; init; } = Array.Empty<DayPlanRecord>();
    /// <summary>Sono (P3): deficit acumulado e sono perdido para o jogo (ultima noite e total).</summary>
    public int SleepDeficitMinutes { get; init; }
    public int LastSleepCutMinutes { get; init; }
    public int SleepCutTotalMinutes { get; init; }
}

/// <summary>Um dia da semana tipica (P2): obrigacoes, acordar, dormir e janelas livres (minutos locais).</summary>
public sealed record WeekDayView(int Dow, IReadOnlyList<(int Start, int End, string Label)> Obligations, int Wake, int Bed,
    IReadOnlyList<(int Start, int End)> Windows, int FreeMinutes, bool FreeDay)
{
    /// <summary>
    /// O dia de 0 a 24 h em trechos (para a agenda grafica): sleep, free, work, study, care,
    /// remote (trabalho remoto), break (pausa do remoto) e commute. Inclui o que vem da vespera
    /// (sono que comecou antes da meia-noite, turno que a atravessa, janela que passa dela).
    /// </summary>
    public IReadOnlyList<(int Start, int End, string Kind)> Segments { get; init; } = Array.Empty<(int, int, string)>();
    public int ObligationMinutes { get; init; }
    public int CommuteMinutes { get; init; }
    public int SleepMinutes { get; init; }
    /// <summary>Preparo ao acordar, chegada e refeicao (cuidados pessoais); espera fora de casa e brechas curtas; compromissos.</summary>
    public int CareMinutes { get; init; }
    public int WaitMinutes { get; init; }
    public int CommitmentMinutes { get; init; }
    /// <summary>Turno que atravessa a meia-noite: sai de casa em ShiftDeparture e dorme depois do turno.</summary>
    public bool NightShift { get; init; }
    public int ShiftDeparture { get; init; } = -1;
    /// <summary>Blocos de obrigacao do dia (horario desta pessoa), se e remoto e o deslocamento por trecho.</summary>
    public IReadOnlyList<(int Start, int End, string Label, bool Remote, int Commute)> Blocks { get; init; } = Array.Empty<(int, int, string, bool, int)>();
    /// <summary>Previsto pela rotina (sem humor nem habito): chance de jogar, sessao que cabe e minutos esperados.</summary>
    public int ChancePermille { get; init; }
    public int PlannedStart { get; init; } = -1;
    public int PlannedMinutes { get; init; }
    public int ExpectedMinutes { get; init; }
}

/// <summary>Humor da pessoa (copia imutavel, D-038).</summary>
public sealed record MoodView(int Satisfaction, int Frustration, int Novelty, int Engagement, int ReturnIntention,
    int Expectation, int LastRatio, int LastScore, int SessionsRated)
{
    public static readonly MoodView Neutral = new(500, 0, 1000, 500, 500, 550, 0, 500, 0);
}

/// <summary>Plano da sessao (copia imutavel, D-037).</summary>
public sealed record PlanView(string FocusKey, string FocusType, string Reason, long FormedTick, int Cycles);

/// <summary>Acontecimento marcante (copia imutavel, D-038).</summary>
public sealed record MemoryView(string Kind, string Subject, int Valence, int Intensity, long Tick, int Count);

/// <summary>Objetivo pessoal (copia imutavel, D-036).</summary>
public sealed record GoalView(string Type, string Target, int TargetValue, int StartValue, int Progress, int Commitment, string MotiveKey, int MotiveValue, long SetTick);

/// <summary>Progresso de uma quest ativa (copia imutavel).</summary>
public sealed record QuestProgressView(string QuestId, int StepIndex, int StepMinutes, long AcceptedTick);

/// <summary>Numeros de uma quest na campanha (copia imutavel para a ficha).</summary>
public sealed record QuestStatsView(int Accepted, int Completed, int UniqueCompleters, int Active,
    long SumPlayedTicksToComplete, long SumLevelAtCompletion, long LastCompletedTick, long XpGranted);

/// <summary>Leitura imutavel do estado publicada pela thread da simulacao para a tela.</summary>
public sealed class SimSnapshot
{
    public long Tick { get; init; }
    public int RequestedSpeed { get; init; }
    /// <summary>Velocidade efetiva medida (multiplos de 1x); -1 = medindo.</summary>
    public double EffectiveSpeed { get; init; }
    public bool ServerOpen { get; init; }
    public int PersonsTotal { get; init; }
    public int Registered { get; init; }
    public int Online { get; init; }
    /// <summary>Retencao (D-039): inativos e desistentes agora.</summary>
    public int Inactive { get; init; }
    public int Quit { get; init; }
    /// <summary>Personagens por ID de pessoa (null = sem personagem).</summary>
    public CharacterView?[] ByPerson { get; init; } = Array.Empty<CharacterView?>();
    /// <summary>Detalhe da pessoa observada pela ficha aberta (nulo se nenhuma).</summary>
    public CharacterDetailView? Watched { get; init; }
    /// <summary>Estatisticas por quest (ID -> numeros).</summary>
    public IReadOnlyDictionary<string, QuestStatsView> QuestStats { get; init; } = new Dictionary<string, QuestStatsView>();
}

/// <summary>Dados estaticos de uma pessoa copiados na criacao.</summary>
public sealed class PersonInfo
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string ProfileId { get; init; } = "";
    public ulong AppearanceSeed { get; init; }
    /// <summary>Tracos gerados na criacao (nao mudam depois; leitura segura).</summary>
    public PersonTraits Traits { get; init; } = null!;
    /// <summary>Tracos de nascenca (imutaveis): comparacao "era X" e limites (D-036).</summary>
    public PersonTraits BaseTraits { get; init; } = null!;
    /// <summary>Localidade (P1): residencia, nacionalidade, idiomas e diferenca para o horario do servidor.</summary>
    public string ResidenceId { get; init; } = "";
    public string NationalityId { get; init; } = "";
    public IReadOnlyList<string> Languages { get; init; } = Array.Empty<string>();
    public int LocalOffsetMinutes { get; init; }
    /// <summary>Rotina de vida (P2): ocupacao, sono, renda e a semana tipica montada (fixa por pessoa).</summary>
    public string OccupationId { get; init; } = "";
    public int SleepNeedMinutes { get; init; }
    public int BedtimeMinute { get; init; }
    public long IncomeCentsPerMonth { get; init; }
    public long SupportCentsPerMonth { get; init; }
    public int ExpectedWeeklyMinutes { get; init; }
    /// <summary>Tolerancia temporaria ao deficit de sono (permille, P3).</summary>
    public int SleepDebtTolerance { get; init; }
    public IReadOnlyList<WeekDayView> Week { get; init; } = Array.Empty<WeekDayView>();
}

/// <summary>
/// Executa o SimulationEngine numa thread propria (plano 13.7). A tela nunca toca no
/// WorldState: le snapshots e envia comandos por fila. Se a maquina nao acompanha a
/// velocidade pedida, o relogio anda mais devagar e a velocidade efetiva e informada;
/// nenhum acontecimento e descartado.
/// </summary>
public sealed class SimulationHost : IDisposable
{
    /// <summary>Hipotese de ritmo (plano 21, em aberto): 1x = 1 minuto simulado por segundo real.</summary>
    public const double SimSecondsPerRealSecondAt1x = 60;
    public static readonly int[] Speeds = { 0, 1, 2, 5, 10 };

    private readonly SimulationEngine _engine;
    private readonly ConcurrentQueue<ISimCommand> _commands = new();
    private readonly ConcurrentQueue<SimEvent> _events = new();
    private readonly Thread _thread;
    private volatile bool _running = true;
    private volatile int _speed = 1;
    private volatile bool _skipToLogin;

    /// <summary>Pede para avancar o tempo ate alguem conectar (no maximo 1 dia simulado).</summary>
    public void SkipToNextLogin() => _skipToLogin = true;
    public bool Skipping => _skipToLogin;
    private SimSnapshot _snapshot;
    private volatile int _watchedPerson;

    /// <summary>A ficha aberta pede o detalhe desta pessoa (0 = nenhuma).</summary>
    public void WatchPerson(int personId) => _watchedPerson = personId;
    private readonly MetricsHistory _metrics = new();
    private readonly object _metricsLock = new();

    /// <summary>Amostra metricas na thread da simulacao (nenhuma amostra se perde em velocidade alta).</summary>
    private void SampleMetrics()
    {
        lock (_metricsLock) _metrics.Sample(BuildSnapshot(-1), Content.Regions.Keys);
    }

    /// <summary>Copia as series para a tela.</summary>
    public void CopyMetricsInto(MetricsHistory target)
    {
        lock (_metricsLock) _metrics.CopyTo(target);
    }

    /// <summary>Avanca em blocos de 1 minuto, amostrando metricas ao cruzar cada janela.</summary>
    private void AdvanceSampled(long target, Stopwatch? budget, double budgetSeconds)
    {
        var w = _engine.World;
        while (w.Clock.Tick < target && (budget == null || budget.Elapsed.TotalSeconds < budgetSeconds))
        {
            _engine.AdvanceTo(Math.Min(target, w.Clock.Tick + SimTime.TicksPerMinute));
            if (w.Clock.Tick % (MetricsHistory.SampleMinutes * SimTime.TicksPerMinute) == 0) SampleMetrics();
        }
    }

    public SimulationHost(ContentDatabase content, ulong seed, int population)
    {
        Content = content;
        Seed = seed;
        Layout = new ZoneLayout(content);
        _engine = WorldFactory.Create(content, new WorldConfig { Seed = seed, PopulationSize = population });
        Persons = _engine.World.Persons
            .Select(p => new PersonInfo { Id = p.Id, Name = p.Name, ProfileId = p.ProfileId, AppearanceSeed = p.AppearanceSeed, Traits = p.Traits, BaseTraits = p.BaseTraits,
                ResidenceId = p.ResidenceId, NationalityId = p.NationalityId, Languages = p.Languages, LocalOffsetMinutes = p.LocalOffsetMinutes,
                OccupationId = p.OccupationId, SleepNeedMinutes = p.SleepNeedMinutes, BedtimeMinute = p.BedtimeMinute,
                IncomeCentsPerMonth = p.IncomeCentsPerMonth, SupportCentsPerMonth = p.SupportCentsPerMonth, ExpectedWeeklyMinutes = p.ExpectedWeeklyMinutes,
                SleepDebtTolerance = p.SleepDebtTolerance, Week = WeekOf(p) })
            .ToArray();
        Calendar = _engine.World.Clock.Calendar;
        _snapshot = BuildSnapshot(-1);
        _thread = new Thread(Loop) { IsBackground = true, Name = "Simulacao" };
    }

    public ContentDatabase Content { get; }
    public ulong Seed { get; }
    public ZoneLayout Layout { get; }
    public CampaignCalendar Calendar { get; }
    public IReadOnlyList<PersonInfo> Persons { get; }
    public SimSnapshot Snapshot => Volatile.Read(ref _snapshot);

    public int Speed
    {
        get => _speed;
        set => _speed = Speeds.Contains(value) ? value : _speed;
    }

    public void Start() => _thread.Start();

    /// <summary>Avanca a simulacao antes de iniciar a thread (capturas reproduziveis).</summary>
    public void AdvanceBeforeStart(long ticks)
    {
        if (_thread.IsAlive) throw new InvalidOperationException("A thread da simulação já está rodando.");
        while (_commands.TryDequeue(out var cmd)) _engine.Submit(cmd);
        SampleMetrics();
        AdvanceSampled(_engine.World.Clock.Tick + ticks, null, 0);
        foreach (var e in _engine.World.Events.Drain()) _events.Enqueue(e);
        Volatile.Write(ref _snapshot, BuildSnapshot(-1));
    }

    public void Submit(ISimCommand command) => _commands.Enqueue(command);

    public List<SimEvent> DrainEvents()
    {
        var list = new List<SimEvent>();
        while (_events.TryDequeue(out var e)) list.Add(e);
        return list;
    }

    private void Loop()
    {
        var clock = Stopwatch.StartNew();
        double last = clock.Elapsed.TotalSeconds;
        double pendingTicks = 0;
        double windowStart = last;
        long windowStartTick = _engine.World.Clock.Tick;
        double effective = -1;
        double lastPublish = 0;
        const double budgetSeconds = 1.0 / 120.0;

        while (_running)
        {
            double now = clock.Elapsed.TotalSeconds;
            double dt = now - last;
            last = now;

            var world = _engine.World;
            while (_commands.TryDequeue(out var cmd))
                _engine.Submit(cmd);

            if (_skipToLogin)
            {
                // Avanca minuto a minuto (com amostras) ate alguem entrar; nada e descartado.
                long limit = world.Clock.Tick + SimTime.TicksPerDay;
                var budget = Stopwatch.StartNew();
                while (world.OnlineCount == 0 && world.Clock.Tick < limit && budget.Elapsed.TotalSeconds < 0.5)
                    AdvanceSampled(world.Clock.Tick + SimTime.TicksPerMinute, null, 0);
                if (world.OnlineCount > 0 || world.Clock.Tick >= limit) _skipToLogin = false;
                pendingTicks = 0;
            }

            int speed = _speed;
            if (speed > 0)
            {
                pendingTicks += dt * SimSecondsPerRealSecondAt1x * speed * SimTime.TicksPerSecond;
                long whole = (long)pendingTicks;
                if (whole > 0)
                {
                    long target = world.Clock.Tick + whole;
                    var budget = Stopwatch.StartNew();
                    AdvanceSampled(target, budget, budgetSeconds);
                    pendingTicks -= whole;
                }
            }
            else pendingTicks = 0;

            foreach (var e in world.Events.Drain()) _events.Enqueue(e);

            if (now - windowStart >= 0.5)
            {
                double simSeconds = (world.Clock.Tick - windowStartTick) / (double)SimTime.TicksPerSecond;
                effective = simSeconds / (now - windowStart) / SimSecondsPerRealSecondAt1x;
                windowStart = now;
                windowStartTick = world.Clock.Tick;
            }
            // Publica no maximo ~30 vezes por segundo (copia dos personagens).
            if (now - lastPublish >= 1.0 / 30.0)
            {
                Volatile.Write(ref _snapshot, BuildSnapshot(effective));
                lastPublish = now;
            }
            Thread.Sleep(3);
        }
    }

    private SimSnapshot BuildSnapshot(double effective)
    {
        var w = _engine.World;
        var byPerson = new CharacterView?[w.Persons.Count + 1];
        foreach (var c in w.Characters)
        {
            byPerson[c.PersonId] = new CharacterView
            {
                PersonId = c.PersonId,
                CharacterId = c.Id,
                Name = c.Name,
                ClassId = c.ClassId,
                Level = c.Level,
                Experience = c.Experience,
                SessionStartTick = c.SessionStartTick,
                ActivityStartTick = c.ActivityStartTick,
                Status = c.Status,
                RegionId = c.RegionId,
                TargetRegionId = c.TargetRegionId,
                ActivityId = c.ActivityId,
                Position = c.Position,
                MoveTo = c.MoveTo,
                MoveStartTick = c.MoveStartTick,
                MoveEndTick = c.MoveEndTick,
                ActivityEndTick = c.ActivityEndTick,
                SessionEndTick = c.SessionEndTick,
                KnownRegions = c.KnownRegions.Count,
                LastDecision = c.LastDecision,
                ActiveQuests = c.ActiveQuests.Count == 0 ? Array.Empty<QuestProgressView>()
                    : c.ActiveQuests.Select(a => new QuestProgressView(a.QuestId, a.StepIndex, a.StepMinutes, a.AcceptedTick)).ToArray(),
                CompletedQuests = c.QuestHistory.Count == 0 ? Array.Empty<(string, int, long)>()
                    : c.QuestHistory.Where(kv => kv.Value.Completions > 0).Select(kv => (kv.Key, kv.Value.Completions, kv.Value.LastCompletedDay)).ToArray(),
                XpFromQuests = c.XpFromQuests,
                PlayedTicks = c.PlayedTicksAt(w.Clock.Tick),
                Goals = c.Goals.Count == 0 ? Array.Empty<GoalView>()
                    : c.Goals.Select(g => new GoalView(g.Type, g.Target, g.TargetValue, g.StartValue, g.Progress, g.Commitment, g.MotiveKey, g.MotiveValue, g.SetTick)).ToArray(),
                GoalsSet = c.GoalsSet,
                GoalsCompleted = c.GoalsCompleted,
                GoalsAbandoned = c.GoalsAbandoned,
                XpFromActivities = c.XpFromActivities,
                State = w.GetPerson(c.PersonId).State,
                Mood = MoodOf(w.GetPerson(c.PersonId).Mood),
                Plan = c.Plan == null ? null : new PlanView(c.Plan.FocusKey, c.Plan.FocusType, c.Plan.Reason, c.Plan.FormedTick, c.Plan.Cycles),
                LastPlayedDay = w.GetPerson(c.PersonId).LastPlayedDay,
            };
        }
        int inactive = 0, quit = 0;
        foreach (var p in w.Persons)
            if (p.State == LifecycleState.Inactive) inactive++;
            else if (p.State == LifecycleState.Quit) quit++;
        CharacterDetailView? watched = null;
        int wp = _watchedPerson;
        if (wp > 0 && wp <= w.Persons.Count && w.Persons[wp - 1].CharacterId > 0)
        {
            var c = w.GetCharacter(w.Persons[wp - 1].CharacterId);
            var pw = w.Persons[wp - 1];
            var imps = pw.PlaceImpressions.Where(kv => Math.Abs(kv.Value) >= 30).ToList();
            watched = new CharacterDetailView(wp, c.PlayedTicksAt(w.Clock.Tick),
                c.ActivityTicksByType.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal), c.PlayedTicksAtLevel.ToArray())
            {
                Memories = pw.Memories.OrderByDescending(m => m.Intensity).ThenByDescending(m => m.Tick)
                    .Select(m => new MemoryView(m.Kind, m.Subject, m.Valence, m.Intensity, m.Tick, m.Count)).ToArray(),
                Drivers = pw.MoodDrivers.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal),
                Impressions = imps.OrderByDescending(kv => kv.Value).Take(3)
                    .Concat(imps.Where(kv => kv.Value < 0).OrderBy(kv => kv.Value).Take(3))
                    .Select(kv => (kv.Key, kv.Value)).Distinct().ToArray(),
                Boredom = c.BoredomByType.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.Ordinal),
                DayPlans = pw.DayPlans.ToArray(),
                SleepDeficitMinutes = pw.SleepDeficitMinutes,
                LastSleepCutMinutes = pw.LastSleepCutMinutes,
                SleepCutTotalMinutes = pw.SleepCutTotalMinutes,
            };
        }
        return new SimSnapshot
        {
            Tick = w.Clock.Tick,
            Watched = watched,
            RequestedSpeed = _speed,
            EffectiveSpeed = effective,
            ServerOpen = w.Server.IsOpen,
            PersonsTotal = w.Persons.Count,
            Registered = w.Characters.Count,
            Online = w.OnlineCount,
            Inactive = inactive,
            Quit = quit,
            ByPerson = byPerson,
            QuestStats = w.QuestStats.ToDictionary(kv => kv.Key, kv => new QuestStatsView(kv.Value.Accepted, kv.Value.Completed,
                kv.Value.UniqueCompleters, kv.Value.Active, kv.Value.SumPlayedTicksToComplete, kv.Value.SumLevelAtCompletion,
                kv.Value.LastCompletedTick, kv.Value.XpGranted), StringComparer.Ordinal),
        };
    }

    /// <summary>Semana tipica da pessoa (sem feriados), montada pelo mesmo codigo do planejador.</summary>
    private IReadOnlyList<WeekDayView> WeekOf(Person p)
    {
        var content = _engine.World.Content;
        var occ = content.Occupations.TryGetValue(p.OccupationId, out var o) ? o : null;
        var rs = content.Routine;
        var list = new List<WeekDayView>();
        for (int dow = 0; dow < 7; dow++)
        {
            var day = Simulation.Core.Behavior.DailySchedule.Build(p, occ, rs, dow, false, false, false);
            var obligations = Simulation.Core.Behavior.DailySchedule.Obligations(p, occ, dow, false).Select(x => (x.Start, x.End, x.Label)).ToArray();
            // A mesma linha do tempo da simulacao (R1): cada minuto do dia acordado em um so trecho.
            var timeline = day.Timeline();
            int Sum(params string[] kinds) => timeline.Where(t => kinds.Contains(t.Kind)).Sum(t => t.End - t.Start);
            var segs = timeline.Select(t => (t.Start, t.End, t.Kind)).ToList();
            foreach (var r in day.Remote)
                foreach (var (bs, be) in r.Breaks) segs.Add((bs, be, "break"));   // pausa: sobreposicao autorizada dentro do trabalho
            var ex = Simulation.Core.Behavior.DailySchedule.Expect(p, day, rs);   // mesma conta do "previsto por semana"
            list.Add(new WeekDayView(dow, obligations, day.Wake, day.Bed, day.Windows.ToArray(), day.FreeMinutes, day.FreeDay)
            {
                Segments = segs,
                ObligationMinutes = Sum("work", "study", "care", "remote"),
                CommuteMinutes = Sum("commute", "transfer"),
                SleepMinutes = Sum("sleep"),
                CareMinutes = Sum("prep", "arrival", "meal"),
                WaitMinutes = Sum("wait", "awake"),
                CommitmentMinutes = Sum("commitment"),
                NightShift = day.NightShiftAhead, ShiftDeparture = day.ShiftDeparture,
                Blocks = Simulation.Core.Behavior.DailySchedule.Blocks(p, occ, dow, false).Select(b => (b.Start, b.End, b.Label, b.Def.Remote, b.Commute)).ToArray(),
                ChancePermille = ex.ChancePermille, PlannedStart = ex.Start, PlannedMinutes = ex.Minutes,
                ExpectedMinutes = ex.ExpectedMinutes,
            });
        }
        return list;
    }

    private static MoodView MoodOf(PersonMood m) =>
        new(m.Satisfaction, m.Frustration, m.Novelty, m.Engagement, m.ReturnIntention, m.ProgressExpectation, m.LastProgressRatio, m.LastSessionScore, m.SessionsRated);

    public void Dispose()
    {
        _running = false;
        if (_thread.IsAlive) _thread.Join(1000);
    }
}

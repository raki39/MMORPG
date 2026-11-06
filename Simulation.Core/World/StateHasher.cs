using System.Security.Cryptography;
using System.Text;
using Simulation.Core.Population;

namespace Simulation.Core.World;

/// <summary>
/// Hash canonico do estado de dominio (SHA-256, campos em ordem fixa, little-endian).
/// Usado para detectar divergencias em replays, saves e na comparacao com/sem 3D.
/// Ao adicionar estado novo ao dominio, inclua-o aqui (e incremente FormatVersion).
/// </summary>
public static class StateHasher
{
    public const int FormatVersion = 34;

    public static string Compute(WorldState w) => Compute(w, includeContentId: true);

    /// <summary>
    /// Estado completo normalizado: tudo o que entra no hash, menos a identificacao do conteudo
    /// (hash dos arquivos). Serve para verificar mudancas que nao deveriam alterar a simulacao,
    /// como mover parametros do codigo para o conteudo. O hash original continua o mesmo.
    /// </summary>
    public static string ComputeWithoutContentId(WorldState w) => Compute(w, includeContentId: false);

    private static string Compute(WorldState w, bool includeContentId)
    {
        using var h = new Writer();
        h.Int(FormatVersion);
        if (includeContentId) h.Str(w.Content.SourceHash);
        h.ULong(w.Seed);
        h.Long(w.Clock.Tick);

        foreach (var kv in w.Rng.States())
        {
            h.Str(kv.Key);
            h.ULong(kv.Value.S0); h.ULong(kv.Value.S1); h.ULong(kv.Value.S2); h.ULong(kv.Value.S3);
        }

        h.Bool(w.NeutralMood);
        h.Bool(w.Server.IsOpen);
        h.Long(w.Server.LastOpenedTick);

        h.Int(w.Persons.Count);
        foreach (var p in w.Persons) HashPerson(h, p);

        h.Int(w.Accounts.Count);
        foreach (var a in w.Accounts) { h.Int(a.Id); h.Int(a.PersonId); h.Long(a.CreatedTick); h.Int(a.ActiveCharacterId); }

        h.Int(w.Characters.Count);
        foreach (var c in w.Characters)
        {
            h.Int(c.Id); h.Int(c.AccountId); h.Int(c.PersonId); h.Str(c.Name); h.Str(c.ClassId);
            h.Int(c.Level); h.Long(c.Experience); h.Long(c.GoldCopper); h.Str(c.RegionId);
            h.Int(c.CurrentHealth); h.Int(c.CurrentResource);
            h.Int((int)c.Status); h.Str(c.TargetRegionId); h.Str(c.ActivityId);
            h.Int(c.Position.X); h.Int(c.Position.Y); h.Int(c.MoveTo.X); h.Int(c.MoveTo.Y);
            h.Long(c.MoveStartTick); h.Long(c.MoveEndTick); h.Str(c.MoveEntersZone);
            h.Int(c.RemainingWaypoints.Count);
            foreach (var wp in c.RemainingWaypoints) { h.Int(wp.Pos.X); h.Int(wp.Pos.Y); h.Str(wp.EntersZone); }
            h.Long(c.SessionStartTick); h.Long(c.ActivityStartTick); h.Long(c.ActivityEndTick); h.Long(c.SessionEndTick); h.Long(c.SessionHardEndTick); h.Long(c.SessionPlanDay); h.Str(c.StopReason); h.Int(c.Continues); h.Int(c.ContinuedMinutes);
            h.Bool(c.LogoutPending); h.Int(c.SessionKind); h.Int(c.SessionExtensions); h.Int(c.ExtendedMinutes);
            h.Long(c.SleepCutStartTick); h.Str(c.ExtendMotive);
            h.Int(c.KnownRegions.Count);
            foreach (var r in c.KnownRegions) h.Str(r);
            h.Int(c.ContentHints.Count);
            foreach (var r in c.ContentHints) h.Str(r);
            h.Int(c.BlockedGoals.Count);
            foreach (var r in c.BlockedGoals) h.Str(r);
            h.Int(c.BlockedTargets.Count);
            foreach (var r in c.BlockedTargets) h.Str(r);
            h.Int(c.ZoneBlocks.Count);
            foreach (var kv in c.ZoneBlocks) { h.Str(kv.Key); h.Int(kv.Value.Level); h.Int(kv.Value.KnownReveal); h.Int(kv.Value.Hints); h.Str(kv.Value.Quests); }
            h.Str(c.BoardExpectation);
            h.Int(c.DecisionCount); h.Str(c.LastActivityKey); h.Int(c.RepeatCount); h.Int(c.RepeatMinutes); h.Long(c.LastSetbackTick); h.Bool(c.Hesitated); h.Str(c.LastHesitationPair);
            if (c.LastDecision != null)
            {
                h.Long(c.LastDecision.Tick); h.Str(c.LastDecision.RegionId); h.Str(c.LastDecision.ActivityId);
                h.Int(c.LastDecision.Utility); h.Int(c.LastDecision.OptionsConsidered);
                h.Bool(c.LastDecision.ContinuedPlan); h.Int(c.LastDecision.PlanGap); h.Str(c.LastDecision.RunnerUpActivityKey);
                foreach (var f in c.LastDecision.Factors) { h.Str(f.Key); h.Int(f.Contribution); }
            }
            h.Long(c.PlayedTicks);
            h.Int(c.ActivityTicksByType.Count);
            foreach (var kv in c.ActivityTicksByType) { h.Str(kv.Key); h.Long(kv.Value); }
            h.Int(c.PlayedTicksAtLevel.Count);
            foreach (var t in c.PlayedTicksAtLevel) h.Long(t);
            h.Int(c.ActiveQuests.Count);
            foreach (var q in c.ActiveQuests) { h.Str(q.QuestId); h.Long(q.AcceptedTick); h.Long(q.AcceptedPlayedTicks); h.Int(q.StepIndex); h.Int(q.StepMinutes); }
            h.Int(c.QuestHistory.Count);
            foreach (var kv in c.QuestHistory)
            {
                h.Str(kv.Key); h.Int(kv.Value.Completions); h.Long(kv.Value.LastCompletedDay);
                h.Int(kv.Value.Drops); h.Long(kv.Value.DroppedDay); h.Int(kv.Value.SavedStepIndex); h.Int(kv.Value.SavedStepMinutes); h.Bool(kv.Value.IdleExpired);
            }
            h.Long(c.DailyDay);
            foreach (var kv in c.DailyByBoard) { h.Str(kv.Key); h.Int(kv.Value); }
            h.Long(c.XpFromQuests); h.Long(c.XpFromActivities);
            // Objetivos pessoais (D-036).
            h.Int(c.Goals.Count);
            foreach (var g in c.Goals)
            {
                h.Str(g.Type); h.Str(g.Target); h.Int(g.TargetValue); h.Int(g.StartValue); h.Int(g.Commitment);
                h.Str(g.MotiveKey); h.Int(g.MotiveValue); h.Long(g.SetTick); h.Int(g.Progress); h.Long(g.LastProgressPlayedTicks); h.Int(g.SetbacksDuring); h.Bool(g.SurvivedByEffect);
                h.Int(g.Links.Count);
                foreach (var l in g.Links) { h.Str(l.ObjectiveId); h.Str(l.Kind); h.Str(l.Subject); }
                h.Long(g.BlockedSincePlayedTicks);
            }
            h.Int(c.GoalsSet); h.Int(c.GoalsCompleted); h.Int(c.GoalsAbandoned);
            h.Int(c.GoalsSetByType.Count);
            foreach (var kv in c.GoalsSetByType) { h.Str(kv.Key); h.Int(kv.Value); }
            h.Int(c.GoalsLinkedSet); h.Int(c.GoalsLinkedCompleted); h.Int(c.GoalsLinkedAbandoned); h.Int(c.GoalsPreparationSet);
            h.Int(c.ZoneMinutesSpent.Count);
            foreach (var kv in c.ZoneMinutesSpent) { h.Str(kv.Key); h.Int(kv.Value); }
            // Plano da sessao e tedio (D-037).
            h.Bool(c.Plan != null);
            if (c.Plan != null) { h.Str(c.Plan.FocusKey); h.Str(c.Plan.FocusType); h.Str(c.Plan.Reason); h.Long(c.Plan.FormedTick); h.Long(c.Plan.CommitUntilTick); h.Int(c.Plan.Cycles); }
            h.Str(c.PlanReviewReason); h.Int(c.PlansFormed); h.Int(c.PlanChanges);
            h.Int(c.BoredomByType.Count);
            foreach (var kv in c.BoredomByType) { h.Str(kv.Key); h.Int(kv.Value); }
            var led = c.SessionLedger;
            h.Long(led.XpAtStart); h.Int(led.LevelAtStart); h.Int(led.Minutes); h.Int(led.Points);
            foreach (var kv in led.ComponentSums) { h.Str(kv.Key); h.Long(kv.Value); }
        }
        h.Int(w.QuestStats.Count);
        foreach (var kv in w.QuestStats)
        {
            var s = kv.Value;
            h.Str(kv.Key); h.Int(s.Accepted); h.Int(s.Completed); h.Int(s.UniqueCompleters); h.Int(s.Active);
            h.Long(s.SumPlayedTicksToComplete); h.Long(s.SumLevelAtCompletion); h.Long(s.LastCompletedTick); h.Long(s.XpGranted);
        }

        h.Int(w.OnlineCount);
        foreach (var kv in w.Occupancy) { h.Str(kv.Key); h.Int(kv.Value); }

        h.Long(w.Scheduler.NextSequence);
        foreach (var ev in w.Scheduler.SnapshotOrdered())
        {
            h.Long(ev.Tick); h.Long(ev.Sequence); h.Str(ev.Type); h.Long(ev.TargetId); h.Long(ev.Payload);
        }

        h.Long(w.Commands.NextSequence);
        foreach (var env in w.Commands.History)
        {
            h.Long(env.Sequence); h.Long(env.ScheduledTick); h.Str(env.Command.Type); h.Str(env.Command.Describe());
            h.Int((int)env.Status); h.Long(env.AppliedTick);
        }

        h.Long(w.Events.NextEventId);
        var d = w.Today;
        h.Int(d.CommandsApplied); h.Int(d.CommandsRejected); h.Int(d.EventsEmitted);
        h.Int(d.Registrations); h.Int(d.SessionsStarted); h.Int(d.ActivePlayers); h.Int(d.PeakOnline);
        h.Int(d.ActivitiesChosen); h.Int(d.LevelUps); h.Long(d.PlayMinutes);
        h.Int(d.Quits); h.Int(d.Returns); h.Int(d.BecameInactive);
        h.Int(w.History.Count);

        return h.Finish();
    }

    private static void HashPerson(Writer h, Person p)
    {
        h.Int(p.Id); h.Str(p.Name); h.Str(p.ProfileId); h.ULong(p.AppearanceSeed);
        h.Int((int)p.State); h.Int(p.AccountId); h.Int(p.CharacterId); h.Long(p.LastPlayedDay);
        h.Long(p.QuitDay); h.Int(p.TimesQuit); h.Int(p.TimesReturned); h.Int(p.TimesInactive);
        h.Str(p.ResidenceId); h.Str(p.NationalityId); h.Int(p.LocalOffsetMinutes);
        // Rotina de vida (P2).
        h.Str(p.OccupationId); h.Int(p.ShiftOffsetMinutes); h.Int(p.CommutePermille); h.Int(p.DayOffIndex); h.Int(p.DayOff2Index);
        h.Int(p.SleepNeedMinutes); h.Int(p.BedtimeMinute); h.Long(p.IncomeCentsPerMonth); h.Long(p.SupportCentsPerMonth);
        h.Int(p.ExpectedWeeklyMinutes); h.Long(p.LastPlayedLocalDay); h.Int(p.HabitualStartHour); h.Int(p.Satiety); h.Long(p.SatietyTick); h.Bool(p.Unfinished);
        h.Int(p.SleepDebtTolerance); h.Int(p.SleepDeficitMinutes); h.Int(p.LastSleepCutMinutes); h.Int(p.SleepCutTotalMinutes); h.Long(p.LastLogoutTick); h.Long(p.SleepCutBedTick); h.Int(p.SleepCutUsedMinutes);
        h.Int(p.RecoveryByDay.Count);
        foreach (var (rd, rm) in p.RecoveryByDay) { h.Long(rd); h.Int(rm); }
        h.Int(p.DayPlans.Count);
        foreach (var d in p.DayPlans)
        {
            h.Long(d.LocalDay); h.Int(d.FreeMinutes); h.Int(d.Windows); h.Int(d.Sessions); h.Str(d.Reason); h.Int(d.FirstStartMinute); h.Int(d.PlannedMinutes); h.Bool(d.FreeDay);
            h.Int(d.SleepMinutes); h.Int(d.DeficitMinutes); h.Int(d.WorkSessions); h.Int(d.WorkOverlapMinutes);
            h.Str(d.CommitmentId); h.Int(d.CommitmentStart); h.Int(d.CommitmentEnd); h.Int(d.RecoveryMinutes);
            h.Int(d.SessionPriority); h.Int(d.DispositionPermille); h.Int(d.RegularityPermille); h.Int(d.PlayedMinutes);
            h.Int(d.Sittings); h.Int(d.Returns); h.Int(d.DeclinedReturns); h.Int(d.SkippedAfterWork); h.Int(d.SleepShiftMinutes);
        }
        h.Int(p.OpenWindows.Count);
        foreach (var w in p.OpenWindows) { h.Long(w.Start); h.Long(w.End); h.Bool(w.EndsAtBed); }
        h.Int(p.Languages.Count);
        foreach (var lang in p.Languages) h.Str(lang);
        h.Long(p.LastReturnDay); h.Long(p.NostalgiaSpentTick); h.Int(p.NostalgiaReturns); h.Int(p.Mood.NoveltyDecayCarry);
        var t = p.Traits;
        h.Int(t.Pve); h.Int(t.Pvp); h.Int(t.Trade); h.Int(t.Exploration); h.Int(t.Socializing);
        h.Int(t.Sociability); h.Int(t.Aggressiveness); h.Int(t.Patience); h.Int(t.Loyalty); h.Int(t.RiskAversion);
        h.Int(t.MechanicalSkill); h.Int(t.GameKnowledge); h.Int(t.Coordination);
        h.Int(t.CheatPropensity); h.Int(t.Opportunism); h.Int(t.Competitiveness);
        // Preferencias (separacao Perfil x Preferencias).
        foreach (var k in PersonPreferences.Keys) h.Int(p.Preferences.Get(k));
        // Personalidade principal (P4a).
        h.Str(p.PersonalityId); h.Str(p.PersonalityPending); h.Long(p.PersonalityPendingSince);
        h.Int(p.PersonalityHistory.Count);
        foreach (var pc in p.PersonalityHistory) { h.Long(pc.Day); h.Str(pc.From); h.Str(pc.To); h.Str(pc.Attribute); h.Int(pc.Value); h.Int(pc.PositionPermille); }
        // Traits (P4b).
        h.Int(p.Quirks.Count);
        foreach (var q in p.Quirks) { h.Str(q.Id); h.Str(q.Source); h.Long(q.Day); }
        h.Int(p.QuirkStats.Count);
        foreach (var kv in p.QuirkStats) { h.Str(kv.Key); h.Int(kv.Value); }
        h.Int(p.QuirkCarryMap.Count);
        foreach (var kv in p.QuirkCarryMap) { h.Str(kv.Key); h.Int(kv.Value); }
        h.Int(p.MarathonDays.Count);
        foreach (var d in p.MarathonDays) h.Long(d);
        h.Int(p.RageQuits);
        h.Int(p.PlayDayCount); h.Int(p.LastAcquisitionPlayDay);
        h.Int(p.QuirkEvidenceByTrait.Count);
        foreach (var kv in p.QuirkEvidenceByTrait)
        {
            var e = kv.Value;
            h.Str(kv.Key); h.Int(e.For); h.Int(e.Against); h.Int(e.Opportunities); h.Int(e.ForCount); h.Int(e.AgainstCount); h.Int(e.EffectOnlyCount);
            h.Int(e.Streak); h.Str(e.Pending); h.Int(e.LastChangePlayDay);
        }
        h.Int(p.QuirkHistory.Count);
        foreach (var qc in p.QuirkHistory) { h.Long(qc.Day); h.Int(qc.PlayDay); h.Str(qc.Id); h.Str(qc.Change); h.Str(qc.PreviousSource); h.Int(qc.For); h.Int(qc.Against); h.Str(qc.Reason); }
        // Objetivos pessoais (O1).
        h.Int(p.Objectives.Count);
        foreach (var o in p.Objectives)
        {
            h.Str(o.Id); h.Str(o.Param); h.Str(o.Tier); h.Int(o.Importance); h.Int(o.Commitment); h.Str(o.State); h.Str(o.Origin); h.Long(o.Day);
            h.Long(o.CompletedDay); h.Int(o.LinkedGoalsSet); h.Int(o.LinkedGoalsCompleted); h.Int(o.LinkedGoalsAbandoned);
            h.Long(o.EndedDay); h.Int(o.EndedPlayDay); h.Str(o.EndReason); h.Int(o.LastProgress); h.Int(o.LastProgressPlayDay); h.Int(o.FailureStreak); h.Int(o.TimesHeld); h.Int(o.TimesLost);
            h.Int(o.FitAtSet); h.Int(o.LowInterestDays); h.Int(o.NegativeDays); h.Int(o.SetAsideDays); h.Int(o.RecoverDays);
            h.Int(o.Motives.Count);
            foreach (var (k, v) in o.Motives) { h.Str(k); h.Int(v); }
        }
        h.Int(p.ObjectiveHistory.Count);
        foreach (var oc in p.ObjectiveHistory) { h.Long(oc.Day); h.Str(oc.Id); h.Str(oc.Param); h.Str(oc.Change); h.Str(oc.Tier); h.Str(oc.Reason); }
        h.Int(p.ObjectivePlayDays); h.Int(p.LastObjectiveChangePlayDay); h.Str(p.ObjectiveTrigger); h.Int(p.ObjectiveGaps); h.Long(p.LastObjectiveGapDay);
        h.Int(t.LeisureBudgetCentsPerMonth); h.Int(t.WillingnessToPay); h.Int(t.PaidAdvantageSensitivity);
        h.Int(t.PreferredStartHour); h.Int(t.SessionMinutes); h.Int(t.GameImportance); h.Int(t.Habit); h.Int(t.ExtendTendency); h.Int(t.SelfControl); h.Int(t.Talent); h.Int(t.Variety); h.Int(t.Chronotype);
        h.Int(p.FreeEveShiftMinutes); h.Long(p.SleepUntilTick); h.Long(p.SleepShiftedDay);
        // Atributos vivos (D-036): base de nascenca e pratica acumulada.
        foreach (var k in PersonTraits.DynamicKeys) h.Int(p.BaseTraits.Get(k));
        h.Int(p.PracticeProgress.Count);
        foreach (var kv in p.PracticeProgress) { h.Str(kv.Key); h.Int(kv.Value); }
        var m = p.Mood;
        h.Int(m.Satisfaction); h.Int(m.Frustration); h.Int(m.Novelty); h.Int(m.Engagement); h.Int(m.ReturnIntention);
        h.Int(m.ProgressExpectation); h.Int(m.LastProgressRatio); h.Int(m.LastSessionScore); h.Int(m.SessionsRated);
        // Memoria (D-038).
        h.Int(p.Memories.Count);
        foreach (var mem in p.Memories) { h.Str(mem.Kind); h.Str(mem.Subject); h.Int(mem.Valence); h.Int(mem.Intensity); h.Long(mem.Tick); h.Int(mem.Count); }
        h.Int(p.PlaceImpressions.Count);
        foreach (var kv in p.PlaceImpressions) { h.Str(kv.Key); h.Int(kv.Value); }
        h.Int(p.MoodDrivers.Count);
        foreach (var kv in p.MoodDrivers) { h.Str(kv.Key); h.Int(kv.Value); }
    }

    private sealed class Writer : IDisposable
    {
        private readonly IncrementalHash _hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        private readonly byte[] _buf = new byte[8];

        public void Long(long v) { BitConverterLE(unchecked((ulong)v)); _hash.AppendData(_buf, 0, 8); }
        public void ULong(ulong v) { BitConverterLE(v); _hash.AppendData(_buf, 0, 8); }
        public void Int(int v) => Long(v);
        public void Bool(bool v) => Long(v ? 1 : 0);

        public void Str(string s)
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            Long(bytes.Length);
            _hash.AppendData(bytes);
        }

        private void BitConverterLE(ulong v)
        {
            for (int i = 0; i < 8; i++) _buf[i] = (byte)(v >> (8 * i));
        }

        public string Finish() => Convert.ToHexString(_hash.GetHashAndReset()).ToLowerInvariant();

        public void Dispose() => _hash.Dispose();
    }
}

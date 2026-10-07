using GymBook.Models;

namespace GymBook.Services;

/// <summary>Starts, persists and finishes the in-progress workout.</summary>
public class WorkoutService(DataStore store, ProgressionEngine engine)
{
    public WorkoutSession? Active => store.Data.ActiveSession;

    /// <summary>Starts <paramref name="workout"/> for plan week <paramref name="week"/>, by default the first unlocked week it isn't done in.</summary>
    public WorkoutSession StartFromPlan(WorkoutPlan plan, PlanWorkout workout, int? week = null)
    {
        var session = new WorkoutSession
        {
            Name = workout.Name,
            PlanId = plan.Id,
            PlanWorkoutId = workout.Id,
            PlanWeek = week ?? new PlanProgress(plan, store.History).FirstOpenWeek(workout),
        };
        var worked = new HashSet<MuscleGroup>();
        foreach (var planned in workout.Exercises)
        {
            // This week's version of it: a deload, or the block's build-up (see PlanCycle).
            var pe = PlanCycle.ForWeek(plan, planned, session.PlanWeek ?? 1);
            var ex = store.GetExercise(pe.ExerciseId);
            if (ex == null)
                continue;
            // A muscle an earlier exercise already worked needs only a lighter warm-up.
            session.Exercises.Add(CreateExercise(ex, pe.Sets, pe.RepMin, pe.RepMax, pe.TargetRir, pe.RestSeconds,
                Warmups.Of(planned, ex, worked.Contains(ex.PrimaryMuscle)), planned));
            worked.Add(ex.PrimaryMuscle);
        }
        return Begin(session);
    }

    public WorkoutSession StartEmpty() => Begin(new WorkoutSession { Name = "Quick workout" });

    WorkoutSession Begin(WorkoutSession session)
    {
        store.Data.ActiveSession = session;
        store.Save();
        return session;
    }

    /// <summary>An exercise of a workout: its suggested sets, after its <paramref name="warmups"/>.</summary>
    public SessionExercise CreateExercise(Exercise ex, int sets, int repMin, int repMax, int rir, int rest, IReadOnlyList<WarmupStep> warmups,
        PlanExercise? planned = null)
    {
        var suggestion = engine.Suggest(ex, sets, repMin, repMax, rir);
        var se = new SessionExercise
        {
            ExerciseId = ex.Id,
            RepMin = repMin,
            RepMax = repMax,
            TargetRir = rir,
            RestSeconds = rest,
            Recommendation = suggestion.Note,
            // The plan's note on it, pinned: unpinning it offers to take it off the plan.
            Note = planned?.Note,
            NotePinned = planned?.Note != null,
        };
        if (warmups.Count > 0 && suggestion.Sets.Count > 0)
            se.Sets.AddRange(engine.Warmups(ex, suggestion.Sets[0].WeightKg, warmups));
        se.Sets.AddRange(suggestion.Sets);
        return se;
    }

    /// <summary>
    /// Defaults for an exercise added on the fly to <paramref name="session"/>, based on the user's goal, with the
    /// recommended warm-ups and the rest of the session's plan (if it's from one).
    /// </summary>
    public SessionExercise CreateAdHoc(Exercise ex, WorkoutSession? session)
    {
        var plan = store.GetPlan(session?.PlanId);
        var p = PlanGenerator.Prescription(store.Profile, ex);
        var rest = plan == null ? p.RestSeconds : PlanRest.DefaultFor(plan, store.Profile, ex);
        var warm = session?.Exercises.Any(e => store.GetExercise(e.ExerciseId)?.PrimaryMuscle == ex.PrimaryMuscle) == true;
        return CreateExercise(ex, p.Sets, p.RepMin, p.RepMax, p.TargetRir, rest, Warmups.Recommended(ex, warm));
    }

    /// <summary>
    /// Moves <paramref name="session"/> (in progress or finished) to start at <paramref name="start"/>, with its end and
    /// every set and rest time moving by the same amount. Null when done, else why not (nothing that happened may end up
    /// in the future). Saving is up to the caller.
    /// </summary>
    public static string? ChangeStart(WorkoutSession session, DateTime start)
    {
        var delta = start - session.StartedAt;
        if (delta == TimeSpan.Zero)
            return null;
        if (SetTimes.LastLogged(session) + delta > DateTime.Now)
            return session.EndedAt == null
                ? "That's in the future: sets already done would come after now. Pick an earlier time."
                : "That's too late: the workout would end in the future. Pick an earlier time.";
        SetTimes.Shift(session, delta);
        return null;
    }

    /// <summary>The rest after a warm-up set in <paramref name="session"/>: its plan's, or the profile's.</summary>
    public int WarmupRest(WorkoutSession? session) => PlanRest.Warmup(store.GetPlan(session?.PlanId), store.Profile);

    public void Save() => store.Save();

    /// <summary>Saves a change to <paramref name="session"/> alone (sets, times, a note): see <see cref="DataStore.SaveSession"/>.</summary>
    public void SaveSession(WorkoutSession session) => store.SaveSession(session);

    public WorkoutSession? Finish()
    {
        var session = Active;
        if (session == null)
            return null;

        session.EndedAt = DateTime.Now;
        // A rest still running ends with the workout.
        SetTimes.EndRest(session, session.EndedAt.Value);
        Settle(session);

        store.Data.ActiveSession = null;
        // Kept when anything was done; exercises not got to stay in it as skipped.
        var done = session.Exercises.Any(e => e.Sets.Count > 0);
        if (done)
        {
            store.Data.Sessions.Add(session);
            AdvancePlan(session);
        }
        store.Save();
        return done ? session : null;
    }

    /// <summary>
    /// A workout as it's kept once finished: sets not done count as skipped (for the finished workout and the exercise's
    /// history) and are left out. An exercise none of whose sets were done stays, skipped (all its sets counted as such),
    /// so the finished workout shows it wasn't got to; only one that never had any sets goes. Undone by <see cref="Unsettle"/>.
    /// </summary>
    public static void Settle(WorkoutSession session)
    {
        foreach (var e in session.Exercises)
        {
            e.SkippedWarmups += e.Sets.Count(s => s.IsWarmup && !s.IsCompleted);
            e.SkippedSets += e.Sets.Count(s => !s.IsWarmup && !s.IsCompleted);
            e.Sets.RemoveAll(s => !s.IsCompleted);
        }
        session.Exercises.RemoveAll(e => e.Sets.Count == 0 && e.SkippedSets + e.SkippedWarmups == 0);
    }

    /// <summary>
    /// A finished workout opened as a workout again (to change it, or to carry on with it): each set it counted as
    /// skipped is back as a skipped set, like the one before it of its kind, so it can be done after all.
    /// </summary>
    public static void Unsettle(WorkoutSession session)
    {
        foreach (var e in session.Exercises)
        {
            AddSkipped(e, warmup: true, e.SkippedWarmups);
            AddSkipped(e, warmup: false, e.SkippedSets);
            (e.SkippedWarmups, e.SkippedSets) = (0, 0);
        }

        static void AddSkipped(SessionExercise e, bool warmup, int count)
        {
            var like = e.Sets.LastOrDefault(s => s.IsWarmup == warmup) ?? e.Sets.LastOrDefault();
            // Warm-ups first, then the working sets.
            var at = warmup ? e.Sets.FindLastIndex(s => s.IsWarmup) + 1 : e.Sets.Count;
            for (var i = 0; i < count; i++)
                e.Sets.Insert(at + i, new SetEntry
                {
                    WeightKg = like?.WeightKg ?? 0,
                    Reps = like?.Reps ?? e.RepMin,
                    Rir = warmup ? null : like?.Rir,
                    IsWarmup = warmup,
                    IsSkipped = true,
                });
        }
    }

    void AdvancePlan(WorkoutSession session)
    {
        var plan = store.GetPlan(session.PlanId);
        if (plan == null || plan.Workouts.Count == 0)
            return;
        var index = plan.Workouts.FindIndex(w => w.Id == session.PlanWorkoutId);
        if (index >= 0)
            plan.NextWorkoutIndex = (index + 1) % plan.Workouts.Count;
    }

    public void Discard()
    {
        store.Data.ActiveSession = null;
        store.Save();
    }

    /// <summary>
    /// Puts the workout in progress aside, unfinished: everything logged stays, it stops being the workout in progress,
    /// and it doesn't count as done until it's resumed and finished.
    /// </summary>
    public WorkoutSession? Pause()
    {
        var session = Active;
        if (session == null)
            return null;
        session.PausedAt = DateTime.Now;
        // A rest still running stops with it.
        SetTimes.EndRest(session, session.PausedAt.Value);
        store.Data.ActiveSession = null;
        store.Data.PausedSessions.RemoveAll(s => s.Id == session.Id);
        store.Data.PausedSessions.Insert(0, session);
        store.Save();
        return session;
    }

    /// <summary>
    /// Makes <paramref name="session"/> (paused or finished) the workout in progress again. Its times move up to now, so
    /// the clock carries on from where it stopped rather than counting the time it was put aside. A workout already in
    /// progress is paused first.
    /// </summary>
    public WorkoutSession Resume(WorkoutSession session)
    {
        if (Active is { } current && current.Id != session.Id)
            Pause();
        var now = DateTime.Now;
        var stoppedAt = session.EndedAt ?? session.PausedAt ?? now;
        if (stoppedAt < now)
            SetTimes.Shift(session, now - stoppedAt);
        if (session.EndedAt != null)
        {
            Unsettle(session);
            session.EndedAt = null;
            // Tells the server this is on purpose, not a stale copy from before the finish.
            session.ReopenedAt = now;
        }
        session.PausedAt = null;
        store.Data.Sessions.RemoveAll(s => s.Id == session.Id);
        store.Data.PausedSessions.RemoveAll(s => s.Id == session.Id);
        return Begin(session);
    }

    /// <summary>
    /// How a workout started from a plan was changed while doing it (exercises added, removed or moved, sets added or
    /// removed, rest times changed), as a new version of the plan workout; null when nothing about its layout changed.
    /// Call it before <see cref="Finish"/>, which drops the sets that weren't done: not getting to a set isn't a change
    /// to the plan. Set counts are compared with what this week called for (a deload or block week), and the difference
    /// is applied to the plan as written.
    /// </summary>
    public PlanUpdate? ProposePlanUpdate(WorkoutSession session)
    {
        var plan = store.GetPlan(session.PlanId);
        var workout = plan?.Workouts.FirstOrDefault(w => w.Id == session.PlanWorkoutId);
        if (plan == null || workout == null)
            return null;

        var week = session.PlanWeek ?? 1;
        var unused = workout.Exercises.ToList();
        var proposed = new List<PlanExercise>();
        var changes = new List<string>();
        string Name(string id) => store.GetExercise(id)?.Name ?? "an exercise";
        var worked = new HashSet<MuscleGroup>();

        foreach (var se in session.Exercises)
        {
            var ex = store.GetExercise(se.ExerciseId);
            var warm = ex != null && !worked.Add(ex.PrimaryMuscle);
            var sets = se.Sets.Count(s => !s.IsWarmup);
            if (sets == 0)
                continue;
            var planned = unused.FirstOrDefault(pe => pe.ExerciseId == se.ExerciseId);
            if (planned == null)
            {
                var note = se.NotePinned ? se.Note : null;
                proposed.Add(new PlanExercise
                {
                    ExerciseId = se.ExerciseId, Sets = sets, RepMin = se.RepMin, RepMax = se.RepMax, TargetRir = se.TargetRir, RestSeconds = se.RestSeconds, Note = note,
                    // The warm-ups it had (none if they were all taken out); null when there's no weight to take them from.
                    Warmups = se.Sets.Any(s => s.IsWarmup) ? Warmups.FromSets(se.Sets) is { } done ? Warmups.FormatSteps(done) : null : "",
                });
                changes.Add($"Add {Name(se.ExerciseId)} ({sets} sets)");
                if (note != null)
                    changes.Add($"{Name(se.ExerciseId)}: pin the note “{note}”");
                continue;
            }
            unused.Remove(planned);
            var expected = PlanCycle.ForWeek(plan, planned, week).Sets;
            // Everything else about it (deloads, its own RIR, ...) stays as the plan has it.
            var updated = LocalJson.Clone(planned);
            // Warm-ups taken out (or added, or sets marked as warm-ups) during the workout: these from now on. The plan's
            // are what it would have had at this weight, so ones left out for a light weight aren't a change.
            if (ex != null && Warmups.Of(planned, ex, warm) is var planSteps
                && se.Sets.Count(s => s.IsWarmup) is var warmupCount
                && warmupCount != engine.Warmups(ex, se.Sets.FirstOrDefault(s => !s.IsWarmup)?.WeightKg ?? 0, planSteps).Count
                && (warmupCount == 0 ? new List<WarmupStep>() : Warmups.FromSets(se.Sets)) is { } doneSteps)
            {
                updated.Warmups = Warmups.FormatSteps(doneSteps);
                changes.Add($"{Name(planned.ExerciseId)}: warm-ups {WarmupCount(planSteps.Count)} → {WarmupCount(doneSteps.Count)}");
            }
            updated.Sets = Math.Clamp(planned.Sets + sets - expected, 1, 10);
            updated.RestSeconds = se.RestSeconds;
            // Rest changed during the workout: this exercise's own from now on.
            updated.CustomRest = planned.CustomRest || se.RestSeconds != planned.RestSeconds;
            // A pinned note becomes the plan's; unpinning the plan's takes it off.
            updated.Note = se.NotePinned ? se.Note : null;
            if (updated.Sets != planned.Sets)
                changes.Add($"{Name(planned.ExerciseId)}: {planned.Sets} → {updated.Sets} sets");
            if (updated.RestSeconds != planned.RestSeconds)
                changes.Add($"{Name(planned.ExerciseId)}: rest {Units.Rest(planned.RestSeconds)} → {Units.Rest(updated.RestSeconds)}");
            if (updated.Note != planned.Note)
                changes.Add(updated.Note == null ? $"{Name(planned.ExerciseId)}: remove the pinned note “{planned.Note}”"
                    : planned.Note == null ? $"{Name(planned.ExerciseId)}: pin the note “{updated.Note}”"
                    : $"{Name(planned.ExerciseId)}: change the pinned note to “{updated.Note}”");
            proposed.Add(updated);
        }
        // Planned exercises the workout ended up without: removed from it, or all their sets were.
        foreach (var pe in unused)
        {
            if (store.GetExercise(pe.ExerciseId) == null)
                // Couldn't be started (e.g. a deleted custom exercise): left in the plan as it was.
                proposed.Add(pe);
            else
                changes.Add($"Remove {Name(pe.ExerciseId)}");
        }
        // The exercises both have, in the plan's order and in the workout's.
        var both = proposed.Select(p => p.ExerciseId).ToHashSet();
        var planOrder = workout.Exercises.Select(pe => pe.ExerciseId).Where(both.Contains);
        var doneOrder = proposed.Select(p => p.ExerciseId).Where(id => workout.Exercises.Any(pe => pe.ExerciseId == id));
        if (!planOrder.SequenceEqual(doneOrder))
            changes.Add("New exercise order");

        return changes.Count == 0 ? null : new PlanUpdate(plan, workout, proposed, changes);

        static string WarmupCount(int n) => n switch { 0 => "none", 1 => "1 set", _ => $"{n} sets" };
    }

    /// <summary>Makes <paramref name="update"/> the plan workout, for next time. The finished workout keeps what was done.</summary>
    public void ApplyPlanUpdate(PlanUpdate update)
    {
        update.Workout.Exercises = update.Exercises;
        store.Save();
    }
}

/// <summary>A plan workout as it would be after taking over the changes made while doing it, and those changes in words.</summary>
public record PlanUpdate(WorkoutPlan Plan, PlanWorkout Workout, List<PlanExercise> Exercises, List<string> Changes);

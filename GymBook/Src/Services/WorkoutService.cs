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
        var warmups = WarmupSettings.For(plan, store.Profile);
        var worked = new HashSet<MuscleGroup>();
        foreach (var planned in workout.Exercises)
        {
            // This week's version of it: a deload, or the block's build-up (see PlanCycle).
            var pe = PlanCycle.ForWeek(plan, planned, session.PlanWeek ?? 1);
            var ex = store.GetExercise(pe.ExerciseId);
            if (ex == null)
                continue;
            // A muscle an earlier exercise already worked needs only a lighter warm-up.
            session.Exercises.Add(CreateExercise(ex, pe.Sets, pe.RepMin, pe.RepMax, pe.TargetRir, pe.RestSeconds, warmups, worked.Contains(ex.PrimaryMuscle), planned));
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

    /// <summary>An exercise of a workout: its suggested sets, after its warm-ups (its own, from <paramref name="planned"/>, or the settings').</summary>
    public SessionExercise CreateExercise(Exercise ex, int sets, int repMin, int repMax, int rir, int rest, WarmupSettings warmups, bool alreadyWarm,
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
        };
        var steps = warmups.StepsFor(planned, ex, alreadyWarm);
        if (steps.Count > 0 && suggestion.Sets.Count > 0)
            se.Sets.AddRange(engine.Warmups(ex, suggestion.Sets[0].WeightKg, warmups, steps));
        se.Sets.AddRange(suggestion.Sets);
        return se;
    }

    /// <summary>
    /// Defaults for an exercise added on the fly to <paramref name="session"/>, based on the user's goal, with the
    /// warm-ups and rest of the session's plan (if it's from one).
    /// </summary>
    public SessionExercise CreateAdHoc(Exercise ex, WorkoutSession? session)
    {
        var plan = store.GetPlan(session?.PlanId);
        var p = PlanGenerator.Prescription(store.Profile, ex);
        var rest = plan == null ? p.RestSeconds : PlanRest.DefaultFor(plan, store.Profile, ex);
        var warm = session?.Exercises.Any(e => store.GetExercise(e.ExerciseId)?.PrimaryMuscle == ex.PrimaryMuscle) == true;
        return CreateExercise(ex, p.Sets, p.RepMin, p.RepMax, p.TargetRir, rest, WarmupSettings.For(plan, store.Profile), warm);
    }

    /// <summary>The warm-up settings for <paramref name="session"/>: its plan's, or the profile's.</summary>
    public WarmupSettings WarmupsFor(WorkoutSession? session) => WarmupSettings.For(store.GetPlan(session?.PlanId), store.Profile);

    public void Save() => store.Save();

    public WorkoutSession? Finish()
    {
        var session = Active;
        if (session == null)
            return null;

        session.EndedAt = DateTime.Now;
        foreach (var e in session.Exercises)
            e.Sets.RemoveAll(s => !s.IsCompleted);
        session.Exercises.RemoveAll(e => e.Sets.Count == 0);

        store.Data.ActiveSession = null;
        if (session.Exercises.Count > 0)
        {
            store.Data.Sessions.Add(session);
            AdvancePlan(session);
        }
        store.Save();
        return session.Exercises.Count > 0 ? session : null;
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

        foreach (var se in session.Exercises)
        {
            var sets = se.Sets.Count(s => !s.IsWarmup);
            if (sets == 0)
                continue;
            var planned = unused.FirstOrDefault(pe => pe.ExerciseId == se.ExerciseId);
            if (planned == null)
            {
                proposed.Add(new PlanExercise { ExerciseId = se.ExerciseId, Sets = sets, RepMin = se.RepMin, RepMax = se.RepMax, TargetRir = se.TargetRir, RestSeconds = se.RestSeconds });
                changes.Add($"Add {Name(se.ExerciseId)} ({sets} sets)");
                continue;
            }
            unused.Remove(planned);
            var expected = PlanCycle.ForWeek(plan, planned, week).Sets;
            var updated = new PlanExercise
            {
                ExerciseId = planned.ExerciseId,
                Sets = Math.Clamp(planned.Sets + sets - expected, 1, 10),
                RepMin = planned.RepMin,
                RepMax = planned.RepMax,
                TargetRir = planned.TargetRir,
                RestSeconds = se.RestSeconds,
                // Rest changed during the workout: this exercise's own from now on.
                CustomRest = planned.CustomRest || se.RestSeconds != planned.RestSeconds,
            };
            if (updated.Sets != planned.Sets)
                changes.Add($"{Name(planned.ExerciseId)}: {planned.Sets} → {updated.Sets} sets");
            if (updated.RestSeconds != planned.RestSeconds)
                changes.Add($"{Name(planned.ExerciseId)}: rest {Units.Rest(planned.RestSeconds)} → {Units.Rest(updated.RestSeconds)}");
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

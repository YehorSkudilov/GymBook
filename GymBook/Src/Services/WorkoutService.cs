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
        foreach (var pe in workout.Exercises)
        {
            var ex = store.GetExercise(pe.ExerciseId);
            if (ex != null)
                session.Exercises.Add(CreateExercise(ex, pe.Sets, pe.RepMin, pe.RepMax, pe.TargetRir, pe.RestSeconds));
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

    public SessionExercise CreateExercise(Exercise ex, int sets, int repMin, int repMax, int rir, int rest)
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
        if (store.Profile.WarmupSuggestions && suggestion.Sets.Count > 0)
            se.Sets.AddRange(engine.Warmups(ex, suggestion.Sets[0].WeightKg));
        se.Sets.AddRange(suggestion.Sets);
        return se;
    }

    /// <summary>Defaults for an exercise added on the fly, based on the user's goal.</summary>
    public SessionExercise CreateAdHoc(Exercise ex)
    {
        var p = PlanGenerator.Prescription(store.Profile, ex);
        return CreateExercise(ex, p.Sets, p.RepMin, p.RepMax, p.TargetRir, p.RestSeconds);
    }

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
}

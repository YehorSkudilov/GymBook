using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// Rest between sets, set at three levels: the profile's rest for compound and isolation lifts (or, without one, what
/// the goal suggests) and after a warm-up set; a plan's own over those (<see cref="WorkoutPlan.OwnRest"/>), kept as the
/// profile's change; and an exercise's own rest over both (<see cref="PlanExercise.CustomRest"/>). An exercise without its
/// own keeps the plan's, even as that changes.
/// </summary>
public static class PlanRest
{
    /// <summary>The plan's own rest for exercises like <paramref name="ex"/>, or null to follow the profile.</summary>
    public static int? PlanOwn(WorkoutPlan plan, Exercise ex) =>
        !plan.OwnRest ? null : ex.Mechanic == Mechanic.Compound ? plan.CompoundRestSeconds : plan.IsolationRestSeconds;

    /// <summary>The rest the profile gives exercises like <paramref name="ex"/> in a plan with <paramref name="goal"/>.</summary>
    public static int FromProfile(Goal goal, UserProfile profile, Exercise ex) =>
        TrainingGoals.Prescription(goal, profile.Experience, ex, profile).RestSeconds;

    /// <summary>The rest <paramref name="plan"/> gives <paramref name="ex"/> when the exercise has none of its own.</summary>
    public static int DefaultFor(WorkoutPlan plan, UserProfile profile, Exercise ex) =>
        PlanOwn(plan, ex) ?? FromProfile(plan.Goal, profile, ex);

    /// <summary>The rest after a warm-up set in <paramref name="plan"/>: its own, or the profile's.</summary>
    public static int Warmup(WorkoutPlan? plan, UserProfile profile) =>
        plan is { OwnRest: true, WarmupRestSeconds: { } own } ? own : profile.WarmupRestSeconds;

    /// <summary>
    /// The profile's rest for a typical compound or isolation lift in a plan with <paramref name="goal"/>: its own
    /// setting, or what the goal suggests. What a plan's own rest starts from.
    /// </summary>
    public static int Typical(Goal goal, UserProfile profile, Mechanic mechanic) =>
        FromProfile(goal, profile, new Exercise { Mechanic = mechanic, PrimaryMuscle = MuscleGroup.Chest, Equipment = Equipment.Dumbbell });

    /// <summary>
    /// Gives <paramref name="plan"/> its own rest times, starting from what it follows now; they stay as they are when
    /// the profile's change.
    /// </summary>
    public static void MakeOwn(WorkoutPlan plan, UserProfile profile)
    {
        if (plan.OwnRest)
            return;
        plan.CompoundRestSeconds = Typical(plan.Goal, profile, Mechanic.Compound);
        plan.IsolationRestSeconds = Typical(plan.Goal, profile, Mechanic.Isolation);
        plan.WarmupRestSeconds = profile.WarmupRestSeconds;
        plan.OwnRest = true;
    }

    /// <summary>Back to the profile's rest times, following them as they change.</summary>
    public static void UseDefaults(WorkoutPlan plan)
    {
        plan.OwnRest = false;
        (plan.CompoundRestSeconds, plan.IsolationRestSeconds, plan.WarmupRestSeconds) = (null, null, null);
    }

    /// <summary>Brings every exercise of <paramref name="plan"/> without its own rest to the plan's default.</summary>
    public static void Apply(WorkoutPlan plan, UserProfile profile, Func<string, Exercise?> exercise)
    {
        foreach (var pe in plan.Workouts.SelectMany(w => w.Exercises))
            if (!pe.CustomRest && exercise(pe.ExerciseId) is { } ex)
                pe.RestSeconds = DefaultFor(plan, profile, ex);
    }

    /// <summary>Every exercise back to the plan's rest, including those with their own.</summary>
    public static void ApplyToAll(WorkoutPlan plan, UserProfile profile, Func<string, Exercise?> exercise)
    {
        foreach (var pe in plan.Workouts.SelectMany(w => w.Exercises))
            pe.CustomRest = false;
        Apply(plan, profile, exercise);
    }
}

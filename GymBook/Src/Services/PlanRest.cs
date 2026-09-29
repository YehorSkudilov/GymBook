using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// Rest between working sets, set at three levels: the profile's rest for compound and isolation lifts (or, without
/// one, what the goal suggests), a plan's own defaults over those, and an exercise's own rest over both
/// (<see cref="PlanExercise.CustomRest"/>). An exercise without its own keeps the plan's default, even as that changes.
/// </summary>
public static class PlanRest
{
    /// <summary>The plan's own default for exercises like <paramref name="ex"/>, or null to follow the profile.</summary>
    public static int? PlanOwn(WorkoutPlan plan, Exercise ex) =>
        ex.Mechanic == Mechanic.Compound ? plan.CompoundRestSeconds : plan.IsolationRestSeconds;

    /// <summary>The rest the profile gives exercises like <paramref name="ex"/> in a plan with <paramref name="goal"/>.</summary>
    public static int FromProfile(Goal goal, UserProfile profile, Exercise ex) =>
        TrainingGoals.Prescription(goal, profile.Experience, ex, profile).RestSeconds;

    /// <summary>The rest <paramref name="plan"/> gives <paramref name="ex"/> when the exercise has none of its own.</summary>
    public static int DefaultFor(WorkoutPlan plan, UserProfile profile, Exercise ex) =>
        PlanOwn(plan, ex) ?? FromProfile(plan.Goal, profile, ex);

    /// <summary>Brings every exercise of <paramref name="plan"/> without its own rest to the plan's default.</summary>
    public static void Apply(WorkoutPlan plan, UserProfile profile, Func<string, Exercise?> exercise)
    {
        foreach (var pe in plan.Workouts.SelectMany(w => w.Exercises))
            if (!pe.CustomRest && exercise(pe.ExerciseId) is { } ex)
                pe.RestSeconds = DefaultFor(plan, profile, ex);
    }
}

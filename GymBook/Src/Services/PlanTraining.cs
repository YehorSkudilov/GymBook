using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// How a plan trains: reps in reserve on or off, deload weeks, periodization, and the target RIR its exercises aim for.
/// A plan follows the profile's defaults (Track RIR, deload weeks, periodization; the target RIR the goal suggests) and
/// is kept at them as they change, unless it has its own (<see cref="WorkoutPlan.OwnTraining"/>). An exercise can have
/// its own target RIR (<see cref="PlanExercise.CustomRir"/>) and its own deloads and periodization.
/// </summary>
public static class PlanTraining
{
    /// <summary>A plan following the defaults takes the profile's current ones; one with its own keeps them.</summary>
    public static void Apply(WorkoutPlan plan, UserProfile profile)
    {
        if (plan.OwnTraining)
            return;
        plan.UseRir = profile.TrackRir;
        plan.Deloads = profile.DefaultDeloads;
        plan.Periodization = profile.DefaultPeriodization;
        plan.TargetRir = null;
    }

    /// <summary>After the profile's defaults change: every plan following them takes the new ones.</summary>
    public static void ApplyToPlans(DataStore store)
    {
        foreach (var plan in store.Data.Plans)
            Apply(plan, store.Profile);
    }

    /// <summary>
    /// Gives <paramref name="plan"/> its own training settings, starting with RIR, deloads and periodization all off
    /// and the target RIR by goal: the user switches on what they want.
    /// </summary>
    public static void MakeOwn(WorkoutPlan plan)
    {
        plan.OwnTraining = true;
        (plan.UseRir, plan.Deloads, plan.Periodization, plan.TargetRir) = (false, false, false, null);
    }

    /// <summary>Back to the profile's defaults, following them as they change.</summary>
    public static void UseDefaults(WorkoutPlan plan, UserProfile profile)
    {
        plan.OwnTraining = false;
        Apply(plan, profile);
    }

    /// <summary>The target RIR <paramref name="plan"/> gives <paramref name="ex"/> when the exercise has none of its own.</summary>
    public static int RirFor(WorkoutPlan plan, UserProfile profile, Exercise ex) =>
        plan is { OwnTraining: true, TargetRir: { } rir } ? rir : TrainingGoals.Prescription(plan.Goal, profile.Experience, ex, profile).TargetRir;

    /// <summary>Brings every exercise of <paramref name="plan"/> without its own target RIR to the plan's.</summary>
    public static void ApplyRir(WorkoutPlan plan, UserProfile profile, Func<string, Exercise?> exercise)
    {
        foreach (var pe in plan.Workouts.SelectMany(w => w.Exercises))
            if (!pe.CustomRir && exercise(pe.ExerciseId) is { } ex)
                pe.TargetRir = RirFor(plan, profile, ex);
    }

    /// <summary>Every exercise back to the plan's target RIR, including those with their own.</summary>
    public static void ApplyRirToAll(WorkoutPlan plan, UserProfile profile, Func<string, Exercise?> exercise)
    {
        foreach (var pe in plan.Workouts.SelectMany(w => w.Exercises))
            pe.CustomRir = false;
        ApplyRir(plan, profile, exercise);
    }

    /// <summary>Every exercise back to the plan's deloads and periodization.</summary>
    public static void WeeksToAll(WorkoutPlan plan)
    {
        foreach (var pe in plan.Workouts.SelectMany(w => w.Exercises))
            (pe.Deloads, pe.Periodization) = (null, null);
    }

    /// <summary>Whether <paramref name="pe"/> lightens in deload weeks and builds over each block, in <paramref name="plan"/>.</summary>
    public static (bool Deloads, bool Periodization) Weeks(WorkoutPlan plan, PlanExercise pe) =>
        (pe.Deloads ?? plan.Deloads, pe.Periodization ?? plan.Periodization);
}

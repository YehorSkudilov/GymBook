using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// Estimates how long a workout takes: every set's reps at the goal's tempo plus setting up, the rest between sets,
/// ramp-up sets on heavier compound lifts, and moving between exercises.
/// </summary>
public class WorkoutEstimator(DataStore store)
{
    const double SetupSeconds = 20, TransitionSeconds = 60, WarmupRestSeconds = 60, WarmupSets = 3, WarmupReps = 5;

    public int Minutes(PlanWorkout workout, Goal goal) => Minutes(workout.Exercises.Select(pe => (store.GetExercise(pe.ExerciseId), pe.Sets, pe.RepMin, pe.RepMax, pe.RestSeconds)), goal);

    public int Minutes(IEnumerable<(Exercise? Exercise, int Sets, int RepMin, int RepMax, int RestSeconds)> exercises, Goal goal)
    {
        var warmups = store.Profile.WarmupSuggestions;
        var seconds = 0.0;
        var count = 0;
        foreach (var (ex, sets, repMin, repMax, rest) in exercises)
        {
            if (sets <= 0)
                continue;
            var perRep = TrainingGoals.SecondsPerRep(goal, ex);
            seconds += sets * (SetupSeconds + (repMin + repMax) / 2.0 * perRep) + (sets - 1) * rest;
            if (warmups && ex is { Mechanic: Mechanic.Compound, IsBodyweight: false } && !TrainingGoals.IsExplosive(ex))
                seconds += WarmupSets * (SetupSeconds + WarmupReps * perRep + WarmupRestSeconds);
            count++;
        }
        if (count == 0)
            return 0;
        seconds += (count - 1) * TransitionSeconds;
        // Round to 5 minutes: it's an estimate, not a timer.
        return Math.Max(5, (int)Math.Round(seconds / 60 / 5) * 5);
    }

    public static string Format(int minutes) => minutes >= 60 ? $"~{minutes / 60} h {minutes % 60:00} min" : $"~{minutes} min";
}

using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// A plan's days in order: its workouts with rest days (<see cref="WorkoutPlan.RestDays"/>) between them.
/// Edits go through <see cref="Days"/> and <see cref="SetDays"/>, so the workout order and rest positions always agree.
/// </summary>
public static class PlanSchedule
{
    /// <summary>Workouts in plan order, with null for each rest day.</summary>
    public static List<PlanWorkout?> Days(WorkoutPlan plan)
    {
        var rest = plan.RestDays?.Where(i => i >= 0).ToHashSet() ?? DefaultRestDays(plan.Workouts.Count);
        var total = plan.Workouts.Count + rest.Count;

        var days = new List<PlanWorkout?>();
        var next = 0;
        for (var i = 0; i < total; i++)
        {
            if (rest.Contains(i))
                days.Add(null);
            else if (next < plan.Workouts.Count)
                days.Add(plan.Workouts[next++]);
        }
        // Positions past the end (which an edit from an older app version could leave) drop out; keep every workout.
        days.AddRange(plan.Workouts.Skip(next));
        return days;
    }

    /// <summary>Stores <paramref name="days"/> as the plan's workout order and rest positions, keeping "next workout" on the same one.</summary>
    public static void SetDays(WorkoutPlan plan, IReadOnlyList<PlanWorkout?> days)
    {
        var oldIndex = plan.Workouts.Count > 0 ? plan.NextWorkoutIndex % plan.Workouts.Count : 0;
        var next = plan.Workouts.Count > 0 ? plan.Workouts[oldIndex] : null;
        plan.Workouts = days.OfType<PlanWorkout>().ToList();
        plan.RestDays = days.Select((d, i) => (d, i)).Where(x => x.d == null).Select(x => x.i).ToList();
        // If the next workout itself was removed, the one after it moves up into its place.
        var index = next == null ? -1 : plan.Workouts.IndexOf(next);
        plan.NextWorkoutIndex = index >= 0 ? index : plan.Workouts.Count == 0 ? 0 : oldIndex % plan.Workouts.Count;
    }

    /// <summary>
    /// Where rest days go in a Monday-first week for a plan with this many workouts. They break up back-to-back
    /// sessions where the split allows it: full body alternates, upper/lower rests after each pair, and six-day
    /// PPL rests between its two rounds.
    /// </summary>
    public static HashSet<int> DefaultRestDays(int workouts) => workouts switch
    {
        <= 1 => [1, 2, 3, 4, 5, 6],
        2 => [1, 2, 4, 5, 6],
        3 => [1, 3, 5, 6],
        4 => [2, 5, 6],
        5 => [2, 6],
        6 => [3],
        _ => [],
    };
}

using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// How a plan changes from week to week. With periodization, each 4-week block builds: week 1 eases in (one more rep
/// in reserve), week 2 is the plan as written, weeks 3 and 4 add a set to exercises with 2 to 5 sets, and week 4 pushes
/// one rep closer to failure. With deloads, a lighter week follows every 4 weeks: about half the sets, 3 more reps in
/// reserve (so the suggested weights drop too). The plan itself stays as written; this is applied as a workout starts.
/// </summary>
public static class PlanCycle
{
    public const int BlockWeeks = 4;

    /// <summary>Weeks before the pattern repeats: the block, plus the deload week when there is one.</summary>
    public static int Length(WorkoutPlan plan) => plan.Deloads ? BlockWeeks + 1 : BlockWeeks;

    /// <summary>1-based position of plan week <paramref name="week"/> in its cycle.</summary>
    public static int WeekInCycle(WorkoutPlan plan, int week) => (Math.Max(1, week) - 1) % Length(plan) + 1;

    public static bool IsDeload(WorkoutPlan plan, int week) => plan.Deloads && WeekInCycle(plan, week) == BlockWeeks + 1;

    /// <summary>Whether the week differs from the plan as written at all.</summary>
    public static bool Changes(WorkoutPlan plan, int week) => IsDeload(plan, week) || (plan.Periodization && WeekInCycle(plan, week) != 2);

    /// <summary>
    /// What to do on <paramref name="pe"/> in plan week <paramref name="week"/>: a copy of it with the week's sets and
    /// reps in reserve. Rep ranges and rest stay as planned. An exercise can sit out the deload week, or not build over
    /// the block (or build in a plan that doesn't), by its own setting (see <see cref="PlanTraining.Weeks"/>).
    /// </summary>
    public static PlanExercise ForWeek(WorkoutPlan plan, PlanExercise pe, int week)
    {
        var sets = pe.Sets;
        var rir = pe.TargetRir;
        var (deloads, periodization) = PlanTraining.Weeks(plan, pe);
        if (IsDeload(plan, week))
        {
            if (deloads)
            {
                sets = Math.Max(1, (sets + 1) / 2);
                rir = Math.Min(5, rir + 3);
            }
        }
        else if (periodization)
        {
            var w = WeekInCycle(plan, week);
            if (w == 1)
                rir = Math.Min(5, rir + 1);
            if (w >= 3 && sets is >= 2 and <= 5)
                sets++;
            if (w == 4)
                rir = Math.Max(0, rir - 1);
        }
        return new PlanExercise
        {
            ExerciseId = pe.ExerciseId,
            Sets = sets,
            RepMin = pe.RepMin,
            RepMax = pe.RepMax,
            TargetRir = rir,
            RestSeconds = pe.RestSeconds,
            CustomRest = pe.CustomRest,
            CustomRir = pe.CustomRir,
            Warmups = pe.Warmups,
            Deloads = pe.Deloads,
            Periodization = pe.Periodization,
        };
    }

    /// <summary>What kind of week it is, for the plan's pages: "Deload week", "Block week 3 of 4", or null for a plain plan.</summary>
    public static string? Describe(WorkoutPlan plan, int week)
    {
        if (IsDeload(plan, week))
            return "Deload week";
        if (!plan.Periodization)
            return null;
        return WeekInCycle(plan, week) switch
        {
            1 => "Block week 1 of 4 · easing in",
            2 => "Block week 2 of 4",
            3 => "Block week 3 of 4 · more volume",
            _ => "Block week 4 of 4 · hardest week",
        };
    }
}

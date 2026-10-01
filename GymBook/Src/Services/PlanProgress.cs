using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// Progress through a plan, week by week. A week is the plan's day order (<see cref="PlanSchedule.Days"/>) done once,
/// in any order: a workout is done when a finished session from it counts toward that week, a rest day when it is
/// marked finished. A workout can also be skipped for a week, which moves Up next past it without counting as done.
/// Finishing or skipping any workout of a week unlocks the next one.
/// Rest days finished and workouts skipped are both stored in <see cref="WorkoutPlan.RestDaysDone"/> (days marked
/// without a session, by week and day): a rest day's key means finished, a workout day's means skipped.
/// </summary>
public class PlanProgress
{
    readonly WorkoutPlan _plan;
    readonly List<WorkoutSession> _sessions;

    public PlanProgress(WorkoutPlan plan, IEnumerable<WorkoutSession> history)
    {
        _plan = plan;
        _sessions = history.Where(s => s.PlanId == plan.Id).ToList();
        Days = PlanSchedule.Days(plan);
        var skipped = (plan.RestDaysDone ?? []).Where(k => Days.ElementAtOrDefault(k % 1000) != null).Select(k => k / 1000);
        LastUnlockedWeek = _sessions.Select(WeekOf).Concat(skipped).DefaultIfEmpty(0).Max() + 1;
    }

    public List<PlanWorkout?> Days { get; }

    /// <summary>The furthest week that can be opened: one past the last week with a workout finished or skipped.</summary>
    public int LastUnlockedWeek { get; }

    /// <summary>
    /// The week to show by default: the first unlocked week with a workout left, which is where Up next comes from.
    /// Rest days don't count, so a week whose workouts are all done isn't held open by rest days never marked finished.
    /// </summary>
    public int CurrentWeek => Enumerable.Range(1, LastUnlockedWeek).FirstOrDefault(w => NextWorkout(w) != null, LastUnlockedWeek);

    /// <summary>The latest session of <paramref name="workout"/> that counts toward <paramref name="week"/>.</summary>
    public WorkoutSession? SessionFor(PlanWorkout workout, int week) =>
        _sessions.Where(s => s.PlanWorkoutId == workout.Id && WeekOf(s) == week).MaxBy(s => s.StartedAt);

    public bool IsRestDone(int day, int week) => _plan.RestDaysDone?.Contains(RestKey(day, week)) == true;

    /// <summary>A workout day skipped in <paramref name="week"/>: marked without a session.</summary>
    public bool IsSkipped(int day, int week) => Days.ElementAtOrDefault(day) != null && _plan.RestDaysDone?.Contains(RestKey(day, week)) == true;

    /// <summary>Done or skipped in <paramref name="week"/>: either way, nothing left to do for it that week.</summary>
    public bool IsHandled(PlanWorkout workout, int week) => SessionFor(workout, week) != null || IsSkipped(Days.IndexOf(workout), week);

    public bool IsDayDone(int day, int week) => Days[day] is { } w ? IsHandled(w, week) : IsRestDone(day, week);

    /// <summary>Workouts finished in <paramref name="week"/>. Rest days don't count.</summary>
    public int WorkoutsDone(int week) => _plan.Workouts.Count(w => SessionFor(w, week) != null);

    public bool IsComplete(int week) => Enumerable.Range(0, Days.Count).All(d => IsDayDone(d, week));

    /// <summary>The first unlocked week <paramref name="workout"/> isn't done or skipped in; the last unlocked week never has it.</summary>
    public int FirstOpenWeek(PlanWorkout workout) =>
        Enumerable.Range(1, LastUnlockedWeek).First(w => !IsHandled(workout, w));

    /// <summary>The first workout of <paramref name="week"/> that isn't done or skipped yet, in day order.</summary>
    public PlanWorkout? NextWorkout(int week) => Days.OfType<PlanWorkout>().FirstOrDefault(w => !IsHandled(w, week));

    /// <summary>
    /// What Up next starts: the first workout left in <paramref name="week"/>, or once that week is all done, in the
    /// first week after it with one left (skipping weeks that are finished too). The last unlocked week has nothing
    /// done, so the search stops there. Null only for a plan without workouts.
    /// </summary>
    public (int Week, PlanWorkout Workout)? NextWorkoutFrom(int week)
    {
        for (var w = week; w <= Math.Max(week, LastUnlockedWeek); w++)
            if (NextWorkout(w) is { } next)
                return (w, next);
        return null;
    }

    /// <summary>Sessions from before plan weeks were stored count toward the calendar week they were done in.</summary>
    int WeekOf(WorkoutSession s) =>
        s.PlanWeek ?? Math.Max(1, (StatsService.WeekStart(s.StartedAt) - StatsService.WeekStart(_plan.CreatedAt)).Days / 7 + 1);

    /// <summary>Every finished session that counts toward <paramref name="week"/>.</summary>
    public List<WorkoutSession> SessionsIn(int week) => _sessions.Where(s => WeekOf(s) == week).ToList();

    /// <summary>Whether anything in <paramref name="week"/> is done: a workout or a rest day.</summary>
    public bool HasAnythingDone(int week) => Enumerable.Range(0, Days.Count).Any(d => IsDayDone(d, week));

    /// <summary>Unmarks every rest day of <paramref name="week"/>.</summary>
    public static void ClearRestDays(WorkoutPlan plan, int week) => plan.RestDaysDone?.RemoveAll(k => k / 1000 == week);

    /// <summary>
    /// Closes the gaps left in a plan's weeks when one is reset or its workouts are deleted: the weeks with anything in
    /// them (a workout, the one in progress, or a rest day done) are numbered 1, 2, 3… again in order, so an empty week
    /// never sits before a week with progress. E.g. weeks 0/6, 1/6 become week 1 with 1/6.
    /// </summary>
    /// <returns>Whether anything was renumbered.</returns>
    public static bool CompactWeeks(WorkoutPlan plan, IEnumerable<WorkoutSession> sessions)
    {
        var progress = new PlanProgress(plan, sessions);
        var used = progress._sessions.Select(progress.WeekOf).Concat((plan.RestDaysDone ?? []).Select(k => k / 1000)).Distinct().Order().ToList();
        var map = used.Select((week, i) => (week, number: i + 1)).ToDictionary(x => x.week, x => x.number);
        if (map.All(x => x.Key == x.Value))
            return false;
        foreach (var s in progress._sessions)
            s.PlanWeek = map[progress.WeekOf(s)];
        plan.RestDaysDone = plan.RestDaysDone?.Select(k => map[k / 1000] * 1000 + k % 1000).ToList();
        return true;
    }

    static int RestKey(int day, int week) => week * 1000 + day;

    /// <summary>Skips (or un-skips) the workout on <paramref name="day"/> for <paramref name="week"/>.</summary>
    public static void SetSkipped(WorkoutPlan plan, int day, int week, bool skipped) => SetRestDone(plan, day, week, skipped);

    public static void SetRestDone(WorkoutPlan plan, int day, int week, bool done)
    {
        var key = RestKey(day, week);
        plan.RestDaysDone ??= [];
        plan.RestDaysDone.Remove(key);
        if (done)
            plan.RestDaysDone.Add(key);
    }
}

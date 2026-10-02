namespace GymBook.Models;

/// <summary>
/// Stamps a workout's sets with when things happened (see <see cref="SetEntry.StartedAt"/> and the rest), so how long each
/// set, warm-up and rest took can be worked out later. Used by the phone, the watch and finishing a workout alike.
/// A rest starts when a set is ticked and lasts until the next set is started (or ticked): it can run past when it was
/// due, and the next set's time starts when the rest ends.
/// </summary>
public static class SetTimes
{
    /// <summary>Ticks <paramref name="set"/> done at <paramref name="now"/>, ending any rest still going.</summary>
    public static void Complete(WorkoutSession session, SetEntry set, DateTime now)
    {
        EndRest(session, now);
        set.IsSkipped = false;
        set.IsCompleted = true;
        set.CompletedAt = now;
        set.StartedAt = StartOf(session, set, now);
        ClearRest(set);
    }

    /// <summary>Unticks <paramref name="set"/>: it hasn't happened, so it has no times.</summary>
    public static void Uncomplete(SetEntry set)
    {
        set.IsCompleted = false;
        set.CompletedAt = null;
        set.StartedAt = null;
        ClearRest(set);
    }

    static void ClearRest(SetEntry set)
    {
        set.RestStartedAt = null;
        set.RestDueAt = null;
        set.RestEndedAt = null;
    }

    /// <summary>
    /// When a set done at <paramref name="now"/> began: the latest end of anything before it, which is the workout's start,
    /// another set ticked, or the rest after one.
    /// </summary>
    public static DateTime StartOf(WorkoutSession session, SetEntry set, DateTime now)
    {
        var start = session.StartedAt;
        foreach (var other in session.Exercises.SelectMany(e => e.Sets))
        {
            if (other == set || !other.IsCompleted || other.CompletedAt is not { } done)
                continue;
            var end = other.RestEndedAt is { } rest && rest > done ? rest : done;
            if (end > start && end <= now)
                start = end;
        }
        return start > now ? now : start;
    }

    /// <summary>A rest begins after <paramref name="after"/>, due at <paramref name="dueAt"/>; any other one still going ends.</summary>
    public static void StartRest(WorkoutSession session, SetEntry after, DateTime now, DateTime dueAt)
    {
        EndRest(session, now);
        after.RestStartedAt = now;
        after.RestDueAt = dueAt < now ? now : dueAt;
        after.RestEndedAt = null;
    }

    /// <summary>The rest after <paramref name="after"/> is now due at <paramref name="dueAt"/> (−/+ on the timer).</summary>
    public static void MoveRestDue(SetEntry after, DateTime dueAt)
    {
        if (after.RestStartedAt is { } started)
            after.RestDueAt = dueAt < started ? started : dueAt;
    }

    /// <summary>Ends the rest still going at <paramref name="now"/>: the next set started, or the workout finished.</summary>
    public static void EndRest(WorkoutSession session, DateTime now)
    {
        foreach (var set in session.Exercises.SelectMany(e => e.Sets))
            if (set.RestStartedAt is { } started && set.RestEndedAt == null)
                set.RestEndedAt = now < started ? started : now;
    }

    /// <summary>Back to resting after the next set was started: the rest after <paramref name="after"/> carries on.</summary>
    public static void ResumeRest(SetEntry after)
    {
        if (after.RestStartedAt != null)
            after.RestEndedAt = null;
    }

    /// <summary>The set whose rest is still going (none, or one), e.g. to pick the timer back up after a restart.</summary>
    public static SetEntry? RunningRest(WorkoutSession session) =>
        session.Exercises.SelectMany(e => e.Sets).FirstOrDefault(s => s.IsCompleted && s.RestStartedAt != null && s.RestEndedAt == null);

    /// <summary>The last set done (by when it was ticked), or null.</summary>
    public static SetEntry? LastDone(WorkoutSession session) =>
        session.Exercises.SelectMany(e => e.Sets).Where(s => s.IsCompleted && s.CompletedAt != null).MaxBy(s => s.CompletedAt);

    /// <summary>
    /// Moves the whole workout by <paramref name="delta"/>: its start and end, and every time logged in it, so how long
    /// each set and rest took stays the same.
    /// </summary>
    public static void Shift(WorkoutSession session, TimeSpan delta)
    {
        session.StartedAt += delta;
        session.EndedAt += delta;
        foreach (var set in session.Exercises.SelectMany(e => e.Sets))
        {
            set.StartedAt += delta;
            set.CompletedAt += delta;
            set.RestStartedAt += delta;
            set.RestDueAt += delta;
            set.RestEndedAt += delta;
        }
    }

    /// <summary>The latest moment that already happened in the workout (not when a rest is due): it can't move past now.</summary>
    public static DateTime LastLogged(WorkoutSession session) =>
        session.Exercises.SelectMany(e => e.Sets)
            .SelectMany(s => new[] { s.StartedAt, s.CompletedAt, s.RestStartedAt, s.RestEndedAt })
            .Append(session.EndedAt)
            .OfType<DateTime>()
            .Append(session.StartedAt)
            .Max();
}

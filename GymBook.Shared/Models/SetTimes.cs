namespace GymBook.Models;

/// <summary>
/// Stamps a workout's sets with when things happened (see <see cref="SetEntry.StartedAt"/> and the rest), so how long each
/// set, warm-up and rest took can be worked out later. Used by the phone, the watch and finishing a workout alike.
/// </summary>
public static class SetTimes
{
    /// <summary>Ticks <paramref name="set"/> done at <paramref name="now"/>, ending any rest still running.</summary>
    public static void Complete(WorkoutSession session, SetEntry set, DateTime now)
    {
        EndRest(session, now);
        set.IsCompleted = true;
        set.CompletedAt = now;
        set.StartedAt = StartOf(session, set, now);
        set.RestStartedAt = null;
        set.RestEndedAt = null;
    }

    /// <summary>Unticks <paramref name="set"/>: it hasn't happened, so it has no times.</summary>
    public static void Uncomplete(SetEntry set)
    {
        set.IsCompleted = false;
        set.CompletedAt = null;
        set.StartedAt = null;
        set.RestStartedAt = null;
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

    /// <summary>A rest begins after <paramref name="after"/>, due to end at <paramref name="endsAt"/>; any other one still running ends.</summary>
    public static void StartRest(WorkoutSession session, SetEntry after, DateTime now, DateTime endsAt)
    {
        EndRest(session, now);
        after.RestStartedAt = now;
        after.RestEndedAt = endsAt;
    }

    /// <summary>The running rest (after <paramref name="after"/>) is now due at <paramref name="endsAt"/>.</summary>
    public static void MoveRestEnd(SetEntry after, DateTime endsAt)
    {
        if (after.RestStartedAt is { } started)
            after.RestEndedAt = endsAt < started ? started : endsAt;
    }

    /// <summary>Ends a rest still running at <paramref name="now"/> (skipped, the next set ticked, the workout finished).</summary>
    public static void EndRest(WorkoutSession session, DateTime now)
    {
        foreach (var set in session.Exercises.SelectMany(e => e.Sets))
            if (set.RestStartedAt is { } started && set.RestEndedAt is { } ends && ends > now)
                set.RestEndedAt = now < started ? started : now;
    }

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
            set.RestEndedAt += delta;
        }
    }

    /// <summary>The latest moment that already happened in the workout (a rest still running doesn't count): it can't move past now.</summary>
    public static DateTime LastLogged(WorkoutSession session) =>
        session.Exercises.SelectMany(e => e.Sets)
            .SelectMany(s => new[] { s.StartedAt, s.CompletedAt, s.RestStartedAt })
            .Append(session.EndedAt)
            .OfType<DateTime>()
            .Append(session.StartedAt)
            .Max();

    /// <summary>The set whose rest is still running at <paramref name="now"/>, to pick the timer back up after a restart.</summary>
    public static SetEntry? RunningRest(WorkoutSession session, DateTime now) =>
        session.Exercises.SelectMany(e => e.Sets).FirstOrDefault(s => s.RestStartedAt != null && s.RestEndedAt > now);
}

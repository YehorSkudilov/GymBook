using GymBook.LocalData;
using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// Owns all user data: <see cref="Data"/> in memory, persisted to the on-device EF Core database by
/// <see cref="LocalStore"/>, which also tracks what still has to sync.
/// </summary>
public class DataStore
{
    readonly LocalStore _local = new(
        Path.Combine(FileSystem.AppDataDirectory, "gymbook-local.db"),
        legacyJsonPath: Path.Combine(FileSystem.AppDataDirectory, "gymbook.json"));

    public AppData Data => _local.Data;

    public LocalStore Local => _local;

    /// <summary>Raised after any data change, local or pulled from the server.</summary>
    public event EventHandler? Changed;

    /// <summary>Raised after the user changed something locally; the sync service listens to push it.</summary>
    public event EventHandler? Saved;

    public void Save()
    {
        _local.Save();
        Saved?.Invoke(this, EventArgs.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>Deletes all data. When signed in, the deletion syncs to the account as well.</summary>
    public void Reset()
    {
        _local.Reset();
        Saved?.Invoke(this, EventArgs.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Deletes what was read from the health apps: each day's calories burned and steps, food logged in them, and
    /// measurements from scales and watches. Food and weights logged in Gym Book stay. Syncs like any deletion.
    /// </summary>
    public void ResetHealthData()
    {
        Data.HealthDays.Clear();
        Data.FoodEntries.RemoveAll(f => f.Source != null);
        Data.BodyWeights.RemoveAll(b => b.Source != null);
        Save();
    }

    /// <summary>
    /// Deletes every finished workout (not the one in progress). Plans stay, starting again from their first workout
    /// and week.
    /// </summary>
    public void ResetHistory()
    {
        Data.Sessions.RemoveAll(s => s.EndedAt != null);
        foreach (var plan in Data.Plans)
            plan.NextWorkoutIndex = 0;
        CompactPlanWeeks();
        Save();
    }

    /// <summary>
    /// Starts <paramref name="plan"/> over: its finished workouts are deleted (from the calendar, History and stats too;
    /// a workout in progress stays), its rest days and skips unmarked, and it begins again from its first workout and
    /// week. The plan itself stays as it is. Syncs like any deletion.
    /// </summary>
    public void ResetPlanHistory(WorkoutPlan plan)
    {
        Data.Sessions.RemoveAll(s => s.EndedAt != null && s.PlanId == plan.Id);
        plan.RestDaysDone = null;
        plan.NextWorkoutIndex = 0;
        CompactPlanWeeks();
        Save();
    }

    /// <summary>
    /// Deletes these finished workouts (from the calendar, History and stats; syncs like any deletion). The plans they
    /// were done with close the gaps in their weeks and pick up after their newest workout left.
    /// </summary>
    public void DeleteSessions(IReadOnlyCollection<WorkoutSession> sessions)
    {
        var plans = sessions.Select(s => GetPlan(s.PlanId)).OfType<WorkoutPlan>().Distinct().ToList();
        var gone = sessions.ToHashSet();
        Data.Sessions.RemoveAll(gone.Contains);
        PlansChanged(plans);
    }

    /// <summary>
    /// Links a finished workout to <paramref name="workout"/> of <paramref name="plan"/> in <paramref name="week"/>, or
    /// unlinks it (all null). Both the plan it leaves and the one it joins close the gaps in their weeks and pick up
    /// after their newest workout.
    /// </summary>
    public void LinkSession(WorkoutSession session, WorkoutPlan? plan, PlanWorkout? workout, int? week)
    {
        var plans = new[] { GetPlan(session.PlanId), plan }.OfType<WorkoutPlan>().Distinct().ToList();
        session.PlanId = workout == null ? null : plan?.Id;
        session.PlanWorkoutId = plan == null ? null : workout?.Id;
        session.PlanWeek = plan == null || workout == null ? null : week;
        PlansChanged(plans);
    }

    /// <summary>The workouts done with these plans changed: their weeks renumbered, and each picks up after its newest.</summary>
    void PlansChanged(IEnumerable<WorkoutPlan> plans)
    {
        foreach (var plan in plans.Where(p => p.Workouts.Count > 0))
            plan.NextWorkoutIndex = History.FirstOrDefault(s => s.PlanId == plan.Id) is { } last
                && plan.Workouts.FindIndex(w => w.Id == last.PlanWorkoutId) is >= 0 and var index
                    ? (index + 1) % plan.Workouts.Count
                    : 0;
        CompactPlanWeeks();
        Save();
    }

    /// <summary>
    /// Workouts deleted from the history and workouts discarded while in progress, most recently deleted first, that can
    /// be brought back (see <see cref="RestoreSessions"/>): those with at least one set done.
    /// </summary>
    public List<WorkoutSession> DeletedWorkouts() =>
        [.. _local.DeletedSessions().Where(s => s.Id != Data.ActiveSession?.Id && Data.Sessions.All(x => x.Id != s.Id)
            && s.Exercises.Any(e => e.Sets.Any(x => x.IsCompleted)))];

    /// <summary>
    /// Brings deleted workouts back into the history, as they were (syncs like any change). One discarded while in
    /// progress comes back finished, like finishing it would have: the sets done, the rest counted as skipped, ending
    /// when the last thing was logged.
    /// </summary>
    public void RestoreSessions(IEnumerable<WorkoutSession> sessions)
    {
        foreach (var session in sessions)
        {
            session.IsDeleted = false;
            if (session.EndedAt == null)
            {
                session.EndedAt = SetTimes.LastLogged(session);
                foreach (var e in session.Exercises)
                {
                    e.SkippedWarmups += e.Sets.Count(s => s.IsWarmup && !s.IsCompleted);
                    e.SkippedSets += e.Sets.Count(s => !s.IsWarmup && !s.IsCompleted);
                    e.Sets.RemoveAll(s => !s.IsCompleted);
                }
                session.Exercises.RemoveAll(e => e.Sets.Count == 0);
            }
            Data.Sessions.Add(session);
        }
        PlansChanged([.. sessions.Select(s => GetPlan(s.PlanId)).OfType<WorkoutPlan>().Distinct()]);
    }

    /// <summary>Deletes every plan. Workouts done with them stay in the history.</summary>
    public void ResetPlans()
    {
        Data.Plans.Clear();
        Data.ActivePlanId = null;
        Save();
    }

    /// <summary>Removes everything from this device only; used on sign-out.</summary>
    public void WipeDevice()
    {
        _local.WipeDevice();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    internal void RaiseChanged() => Changed?.Invoke(this, EventArgs.Empty);

    /// <summary>Writes all data as a single JSON file for sharing and returns its path.</summary>
    public string ExportJson()
    {
        var path = Path.Combine(FileSystem.CacheDirectory, "gymbook-export.json");
        File.WriteAllText(path, LocalJson.Serialize(Data));
        return path;
    }

    public UserProfile Profile => Data.Profile;

    public IEnumerable<Exercise> AllExercises => ExerciseLibrary.All.Concat(Data.CustomExercises);

    public Exercise? GetExercise(string id) => ExerciseLibrary.Find(id) ?? Data.CustomExercises.FirstOrDefault(e => e.Id == id);

    public WorkoutPlan? ActivePlan => Data.Plans.FirstOrDefault(p => p.Id == Data.ActivePlanId);

    public WorkoutPlan? GetPlan(string? id) => Data.Plans.FirstOrDefault(p => p.Id == id);

    public WorkoutSession? GetSession(string? id) => Data.Sessions.FirstOrDefault(s => s.Id == id);

    /// <summary>Finished sessions, newest first.</summary>
    public IEnumerable<WorkoutSession> History => Data.Sessions.Where(s => s.EndedAt != null).OrderByDescending(s => s.StartedAt);

    /// <summary>
    /// Plans and workouts that still use an exercise id from an earlier library (also synced from another device or an
    /// older app) are moved to today's id, so history, progress and plans all agree. Saving is up to the caller.
    /// </summary>
    /// <returns>Whether anything was changed.</returns>
    public bool MigrateExerciseIds()
    {
        var changed = false;
        foreach (var e in Data.Plans.SelectMany(p => p.Workouts).SelectMany(w => w.Exercises).Where(e => ExerciseLibrary.IsAlias(e.ExerciseId)))
        {
            e.ExerciseId = ExerciseLibrary.Canonical(e.ExerciseId);
            changed = true;
        }
        var sessions = Data.ActiveSession is { } active ? Data.Sessions.Append(active) : Data.Sessions;
        foreach (var e in sessions.SelectMany(s => s.Exercises).Where(e => ExerciseLibrary.IsAlias(e.ExerciseId)))
        {
            e.ExerciseId = ExerciseLibrary.Canonical(e.ExerciseId);
            changed = true;
        }
        return changed;
    }

    /// <summary>
    /// After workouts are deleted or a week is reset: every plan's weeks renumbered so none is left empty before one
    /// with progress (see <see cref="PlanProgress.CompactWeeks"/>). Also on showing the plan, for gaps from before this
    /// existed (or synced from another device). Saving is up to the caller.
    /// </summary>
    /// <returns>Whether any plan was renumbered.</returns>
    public bool CompactPlanWeeks()
    {
        var sessions = History.ToList();
        if (Data.ActiveSession is { } active && sessions.All(s => s.Id != active.Id))
            sessions.Add(active);
        var changed = false;
        foreach (var plan in Data.Plans)
            changed |= PlanProgress.CompactWeeks(plan, sessions);
        return changed;
    }
}

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
    /// After workouts are deleted or a week is reset: every plan's weeks renumbered so none is left empty before one
    /// with progress (see <see cref="PlanProgress.CompactWeeks"/>). Saving is up to the caller.
    /// </summary>
    public void CompactPlanWeeks()
    {
        var sessions = History.ToList();
        if (Data.ActiveSession is { } active && sessions.All(s => s.Id != active.Id))
            sessions.Add(active);
        foreach (var plan in Data.Plans)
            PlanProgress.CompactWeeks(plan, sessions);
    }
}

using System.Text.Json;
using GymBook.Models;

namespace GymBook.Services;

/// <summary>Owns the single JSON document that holds all user data.</summary>
public class DataStore
{
    readonly string _path = Path.Combine(FileSystem.AppDataDirectory, "gymbook.json");
    readonly Lock _gate = new();

    public AppData Data { get; private set; } = new();

    public event EventHandler? Changed;

    public DataStore()
    {
        Load();
    }

    public string FilePath => _path;

    void Load()
    {
        try
        {
            if (File.Exists(_path))
                Data = JsonSerializer.Deserialize(File.ReadAllText(_path), AppJsonContext.Default.AppData) ?? new AppData();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load data: {ex}");
            Data = new AppData();
        }
    }

    public void Save()
    {
        lock (_gate)
        {
            var json = JsonSerializer.Serialize(Data, AppJsonContext.Default.AppData);
            var tmp = _path + ".tmp";
            File.WriteAllText(tmp, json);
            File.Move(tmp, _path, overwrite: true);
        }
        Changed?.Invoke(this, EventArgs.Empty);
    }

    public void Reset()
    {
        Data = new AppData();
        Save();
    }

    public UserProfile Profile => Data.Profile;

    public IEnumerable<Exercise> AllExercises => ExerciseLibrary.All.Concat(Data.CustomExercises);

    public Exercise? GetExercise(string id) => ExerciseLibrary.Find(id) ?? Data.CustomExercises.FirstOrDefault(e => e.Id == id);

    public WorkoutPlan? ActivePlan => Data.Plans.FirstOrDefault(p => p.Id == Data.ActivePlanId);

    public WorkoutPlan? GetPlan(string? id) => Data.Plans.FirstOrDefault(p => p.Id == id);

    public WorkoutSession? GetSession(string? id) => Data.Sessions.FirstOrDefault(s => s.Id == id);

    /// <summary>Finished sessions, newest first.</summary>
    public IEnumerable<WorkoutSession> History => Data.Sessions.Where(s => s.EndedAt != null).OrderByDescending(s => s.StartedAt);
}

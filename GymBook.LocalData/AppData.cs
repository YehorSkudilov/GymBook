using System.Text.Json;
using System.Text.Json.Serialization;
using GymBook.Serialization;

namespace GymBook.Models;

/// <summary>Everything the app works with, held in memory. Persisted row by row by <see cref="LocalData.LocalStore"/>.</summary>
public class AppData
{
    public int Version { get; set; } = 1;
    public UserProfile Profile { get; set; } = new();
    public List<WorkoutPlan> Plans { get; set; } = [];

    /// <summary>Stored on the profile so it syncs; kept here for existing callers and the old file format.</summary>
    public string? ActivePlanId
    {
        get => Profile.ActivePlanId;
        set => Profile.ActivePlanId = value;
    }

    public List<WorkoutSession> Sessions { get; set; } = [];

    /// <summary>The workout in progress. Stored and synced as a session without EndedAt; finishing moves it to <see cref="Sessions"/>.</summary>
    public WorkoutSession? ActiveSession { get; set; }

    public List<BodyWeightEntry> BodyWeights { get; set; } = [];
    public List<Exercise> CustomExercises { get; set; } = [];
    public List<FoodEntry> FoodEntries { get; set; } = [];
    public List<HealthDay> HealthDays { get; set; } = [];
    public List<SupplementDose> Supplements { get; set; } = [];
}

/// <summary>JSON for device-side use (export, cloning, old-format import): the wire settings plus <see cref="AppData"/>.</summary>
public static class LocalJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, typeof(T), Options);

    public static T Deserialize<T>(string json) => (T)JsonSerializer.Deserialize(json, typeof(T), Options)!;

    /// <summary>A deep copy that shares no objects with the original.</summary>
    public static T Clone<T>(T value) => Deserialize<T>(Serialize(value));

    static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        GymBookJson.Configure(options);
        options.WriteIndented = false;
        options.TypeInfoResolverChain.Add(LocalJsonContext.Default);
        options.MakeReadOnly();
        return options;
    }
}

[JsonSerializable(typeof(AppData))]
internal partial class LocalJsonContext : JsonSerializerContext;

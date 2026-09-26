using System.Text.Json.Serialization;

namespace GymBook.Models;

public class Exercise
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public MuscleGroup PrimaryMuscle { get; set; }
    public List<MuscleGroup> SecondaryMuscles { get; set; } = [];
    public Equipment Equipment { get; set; }
    public Mechanic Mechanic { get; set; }
    public string Instructions { get; set; } = "";
    public bool IsCustom { get; set; }

    [JsonIgnore]
    public bool IsBodyweight => Equipment is Equipment.Bodyweight or Equipment.Band;

    [JsonIgnore]
    public string Subtitle => $"{PrimaryMuscle.Display()} · {Equipment.Display()}";
}

public class WorkoutPlan
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Goal Goal { get; set; }
    public int DaysPerWeek { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int NextWorkoutIndex { get; set; }
    public List<PlanWorkout> Workouts { get; set; } = [];
}

public class PlanWorkout
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public List<PlanExercise> Exercises { get; set; } = [];
}

public class PlanExercise
{
    public string ExerciseId { get; set; } = "";
    public int Sets { get; set; } = 3;
    public int RepMin { get; set; } = 8;
    public int RepMax { get; set; } = 12;
    public int TargetRir { get; set; } = 2;
    public int RestSeconds { get; set; } = 120;
}

public class WorkoutSession
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "";
    public string? PlanId { get; set; }
    public string? PlanWorkoutId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime? EndedAt { get; set; }
    public List<SessionExercise> Exercises { get; set; } = [];

    [JsonIgnore]
    public TimeSpan Duration => (EndedAt ?? DateTime.Now) - StartedAt;

    [JsonIgnore]
    public IEnumerable<SetEntry> WorkingSets => Exercises.SelectMany(e => e.Sets).Where(s => s.IsCompleted && !s.IsWarmup);
}

public class SessionExercise
{
    public string ExerciseId { get; set; } = "";
    public int RepMin { get; set; } = 8;
    public int RepMax { get; set; } = 12;
    public int TargetRir { get; set; } = 2;
    public int RestSeconds { get; set; } = 120;
    public string? Recommendation { get; set; }
    public List<SetEntry> Sets { get; set; } = [];
}

public class SetEntry
{
    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public int? Rir { get; set; }
    public bool IsWarmup { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class BodyWeightEntry
{
    public DateTime Date { get; set; }
    public double WeightKg { get; set; }
}

public class UserProfile
{
    public string Name { get; set; } = "";
    public Goal Goal { get; set; } = Goal.BuildMuscle;
    public Experience Experience { get; set; } = Experience.Beginner;
    public int DaysPerWeek { get; set; } = 3;
    public int SessionMinutes { get; set; } = 60;
    public EquipmentAccess EquipmentAccess { get; set; } = EquipmentAccess.FullGym;
    public WeightUnit Unit { get; set; } = WeightUnit.Kg;
    public double BodyWeightKg { get; set; } = 75;
    public int DefaultRestSeconds { get; set; } = 120;
    public bool AutoRestTimer { get; set; } = true;
    public bool WarmupSuggestions { get; set; } = true;
    public bool TrackRir { get; set; } = true;
    public bool OnboardingDone { get; set; }
}

public class AppData
{
    public int Version { get; set; } = 1;
    public UserProfile Profile { get; set; } = new();
    public List<WorkoutPlan> Plans { get; set; } = [];
    public string? ActivePlanId { get; set; }
    public List<WorkoutSession> Sessions { get; set; } = [];
    public WorkoutSession? ActiveSession { get; set; }
    public List<BodyWeightEntry> BodyWeights { get; set; } = [];
    public List<Exercise> CustomExercises { get; set; } = [];
}

[JsonSourceGenerationOptions(UseStringEnumConverter = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(AppData))]
public partial class AppJsonContext : JsonSerializerContext;

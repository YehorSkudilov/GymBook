using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace GymBook.Models;

// These classes are shared by the app, the API and both databases, and double as the sync wire format.
// They must never carry anything the server decides on its own (owner, server version, etc.): those live
// in EF shadow properties on the server so a client can't set them. The MaxLength attributes bound what a
// client may upload and are enforced by the API's model validation.

public class Exercise : ISyncEntity
{
    [MaxLength(SyncLimits.IdLength)]
    public string Id { get; set; } = "";
    [MaxLength(SyncLimits.NameLength)]
    public string Name { get; set; } = "";
    public MuscleGroup PrimaryMuscle { get; set; }
    [MaxItems(20)]
    public List<MuscleGroup> SecondaryMuscles { get; set; } = [];
    public Equipment Equipment { get; set; }
    public Mechanic Mechanic { get; set; }
    [MaxLength(SyncLimits.TextLength)]
    public string Instructions { get; set; } = "";
    public bool IsCustom { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    [JsonIgnore]
    public bool IsBodyweight => Equipment is Equipment.Bodyweight or Equipment.Band;

    [JsonIgnore]
    public string Subtitle => $"{PrimaryMuscle.Display()} · {Equipment.Display()}";
}

public class WorkoutPlan : ISyncEntity
{
    [MaxLength(SyncLimits.IdLength)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    [MaxLength(SyncLimits.NameLength)]
    public string Name { get; set; } = "";
    [MaxLength(SyncLimits.TextLength)]
    public string Description { get; set; } = "";
    public Goal Goal { get; set; }
    public int DaysPerWeek { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int NextWorkoutIndex { get; set; }
    [MaxItems(50)]
    public List<PlanWorkout> Workouts { get; set; } = [];
    /// <summary>
    /// Positions of rest days in the plan's day order, counting workouts and rest days together (0-based):
    /// [3] for Push, Pull, Legs, Rest, Push, Pull, Legs. Null for plans from before rest days were stored,
    /// which get a default layout.
    /// </summary>
    [MaxItems(50)]
    public List<int>? RestDays { get; set; }
    /// <summary>
    /// Rest days marked finished, one entry per plan week and day: week × 1000 + day position (as in
    /// <see cref="RestDays"/>). Null until the first one is marked.
    /// </summary>
    [MaxItems(1000)]
    public List<int>? RestDaysDone { get; set; }
    // How the plan is run week to week (see the app's PlanCycle). All on by default; switched in the plan's ··· menu.
    /// <summary>Reps in reserve: a target per exercise, and logged with each set. Off: weight and reps only.</summary>
    public bool UseRir { get; set; } = true;
    /// <summary>A lighter week (about half the sets, easier effort) after every 4 weeks of training.</summary>
    public bool Deloads { get; set; } = true;
    /// <summary>Effort and volume build over each 4-week block, instead of the same prescription every week.</summary>
    public bool Periodization { get; set; } = true;
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class PlanWorkout
{
    [MaxLength(SyncLimits.IdLength)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    [MaxLength(SyncLimits.NameLength)]
    public string Name { get; set; } = "";
    [MaxItems(100)]
    public List<PlanExercise> Exercises { get; set; } = [];
}

public class PlanExercise
{
    [MaxLength(SyncLimits.IdLength)]
    public string ExerciseId { get; set; } = "";
    public int Sets { get; set; } = 3;
    public int RepMin { get; set; } = 8;
    public int RepMax { get; set; } = 12;
    public int TargetRir { get; set; } = 2;
    public int RestSeconds { get; set; } = 120;
}

public class WorkoutSession : ISyncEntity
{
    [MaxLength(SyncLimits.IdLength)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    [MaxLength(SyncLimits.NameLength)]
    public string Name { get; set; } = "";
    [MaxLength(SyncLimits.IdLength)]
    public string? PlanId { get; set; }
    [MaxLength(SyncLimits.IdLength)]
    public string? PlanWorkoutId { get; set; }
    /// <summary>The plan week (1-based) this session counts toward. Null for sessions from before plan weeks were stored.</summary>
    public int? PlanWeek { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.Now;
    public DateTime? EndedAt { get; set; }
    [MaxItems(100)]
    public List<SessionExercise> Exercises { get; set; } = [];
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    [JsonIgnore]
    public TimeSpan Duration => (EndedAt ?? DateTime.Now) - StartedAt;

    [JsonIgnore]
    public IEnumerable<SetEntry> WorkingSets => Exercises.SelectMany(e => e.Sets).Where(s => s.IsCompleted && !s.IsWarmup);
}

public class SessionExercise
{
    [MaxLength(SyncLimits.IdLength)]
    public string ExerciseId { get; set; } = "";
    public int RepMin { get; set; } = 8;
    public int RepMax { get; set; } = 12;
    public int TargetRir { get; set; } = 2;
    public int RestSeconds { get; set; } = 120;
    [MaxLength(SyncLimits.TextLength)]
    public string? Recommendation { get; set; }
    [MaxItems(100)]
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

public class BodyWeightEntry : ISyncEntity
{
    /// <summary>One entry per calendar day, so the day is the identity unless set explicitly.</summary>
    [MaxLength(SyncLimits.IdLength)]
    public string Id { get => field ?? Date.ToString("yyyy-MM-dd"); set; }
    public DateTime Date { get; set; }
    public double WeightKg { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

public class UserProfile
{
    [MaxLength(SyncLimits.NameLength)]
    public string Name { get; set; } = "";
    public Goal Goal { get; set; } = Goal.BuildMuscle;
    public Experience Experience { get; set; } = Experience.Beginner;
    public int DaysPerWeek { get; set; } = 3;
    public int SessionMinutes { get; set; } = 60;
    public EquipmentAccess EquipmentAccess { get; set; } = EquipmentAccess.FullGym;
    public WeightUnit Unit { get; set; } = WeightUnit.Kg;
    public double BodyWeightKg { get; set; } = 75;
    // Optional, used to estimate strength for exercises the user hasn't done yet.
    [Range(1900, 2100)]
    public int? BirthYear { get; set; }
    [Range(3, 60)]
    public double? BodyFatPercent { get; set; }
    /// <summary>When the user started training consistently.</summary>
    public DateTime? TrainingSince { get; set; }
    /// <summary>Whether generated plans include neck work.</summary>
    public bool TrainNeck { get; set; } = true;
    /// <summary>Rest after a working set of a compound or an isolation exercise; null rests as the goal suggests.</summary>
    [Range(15, 600)]
    public int? CompoundRestSeconds { get; set; }
    [Range(15, 600)]
    public int? IsolationRestSeconds { get; set; }
    /// <summary>Rest after a warm-up set.</summary>
    [Range(15, 600)]
    public int WarmupRestSeconds { get; set; } = 60;
    public bool AutoRestTimer { get; set; } = true;
    public bool WarmupSuggestions { get; set; } = true;
    public bool TrackRir { get; set; } = true;
    public bool OnboardingDone { get; set; }
    [MaxLength(SyncLimits.IdLength)]
    public string? ActivePlanId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

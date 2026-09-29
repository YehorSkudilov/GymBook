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
    // How the plan is run week to week (see the app's PlanCycle). Its own values when <see cref="OwnTraining"/>; otherwise
    // kept at the profile's defaults (Track RIR, DefaultDeloads, DefaultPeriodization) as they change. Set in its Plan settings.
    /// <summary>Reps in reserve: a target per exercise, and logged with each set. Off: weight and reps only.</summary>
    public bool UseRir { get; set; } = true;
    /// <summary>A lighter week (about half the sets, easier effort) after every 4 weeks of training.</summary>
    public bool Deloads { get; set; } = true;
    /// <summary>Effort and volume build over each 4-week block, instead of the same prescription every week.</summary>
    public bool Periodization { get; set; } = true;
    /// <summary>The plan has its own RIR, deloads, periodization and target RIR, instead of following the profile's defaults.</summary>
    public bool OwnTraining { get; set; }
    /// <summary>
    /// The reps in reserve every exercise of the plan aims for, unless it has its own (<see cref="PlanExercise.CustomRir"/>).
    /// Null: what the goal suggests for each exercise. Only with <see cref="OwnTraining"/>.
    /// </summary>
    [Range(0, 10)]
    public int? TargetRir { get; set; }
    /// <summary>
    /// This plan's own warm-up settings, as JSON (the app's WarmupSettings), kept as they are when the profile's change.
    /// Null: the profile's, which are the defaults for every plan. The rest after a warm-up set is with the other rest
    /// times (<see cref="WarmupRestSeconds"/>).
    /// </summary>
    [MaxLength(SyncLimits.TextLength)]
    public string? Warmups { get; set; }
    /// <summary>
    /// The plan has its own rest times (<see cref="CompoundRestSeconds"/>, <see cref="IsolationRestSeconds"/>,
    /// <see cref="WarmupRestSeconds"/>), kept as they are when the profile's change. Otherwise it follows the profile's.
    /// </summary>
    public bool OwnRest { get; set; }
    /// <summary>
    /// With <see cref="OwnRest"/>: this plan's rest after a working set of a compound or an isolation exercise (null: the
    /// profile's, for plans from before). Each exercise can still have its own (<see cref="PlanExercise.CustomRest"/>).
    /// </summary>
    [Range(15, 600)]
    public int? CompoundRestSeconds { get; set; }
    [Range(15, 600)]
    public int? IsolationRestSeconds { get; set; }
    /// <summary>With <see cref="OwnRest"/>: this plan's rest after a warm-up set.</summary>
    [Range(15, 600)]
    public int? WarmupRestSeconds { get; set; }
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
    /// <summary>The rest used for this exercise: its own if <see cref="CustomRest"/>, otherwise kept at the plan's default.</summary>
    public int RestSeconds { get; set; } = 120;
    /// <summary>The rest was set for this exercise itself, so the plan's default rest doesn't change it.</summary>
    public bool CustomRest { get; set; }
    /// <summary>The target RIR was set for this exercise itself; otherwise it's kept at the plan's (see WorkoutPlan.TargetRir).</summary>
    public bool CustomRir { get; set; }
    /// <summary>
    /// This exercise's own warm-up sets, as percent × reps ("50x8, 75x4"; empty for none). Null: the plan's, for its kind
    /// of exercise.
    /// </summary>
    [MaxLength(SyncLimits.NameLength)]
    public string? Warmups { get; set; }
    /// <summary>Whether this exercise lightens in the plan's deload weeks; null: as the plan does.</summary>
    public bool? Deloads { get; set; }
    /// <summary>Whether this exercise builds over each 4-week block; null: as the plan does.</summary>
    public bool? Periodization { get; set; }
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
    /// <summary>
    /// The warm-up sets for each kind of exercise, as JSON (the app's WarmupSettings); null for the defaults. On/off and
    /// the rest after them are <see cref="WarmupSuggestions"/> and <see cref="WarmupRestSeconds"/>. Every plan uses these
    /// unless it has its own.
    /// </summary>
    [MaxLength(SyncLimits.TextLength)]
    public string? Warmups { get; set; }
    public bool AutoRestTimer { get; set; } = true;
    public bool WarmupSuggestions { get; set; } = true;
    public bool TrackRir { get; set; } = true;
    /// <summary>What a new plan starts with for its <see cref="WorkoutPlan.Deloads"/>; each plan then has its own.</summary>
    public bool DefaultDeloads { get; set; } = true;
    /// <summary>What a new plan starts with for its <see cref="WorkoutPlan.Periodization"/>; each plan then has its own.</summary>
    public bool DefaultPeriodization { get; set; } = true;
    public bool OnboardingDone { get; set; }
    [MaxLength(SyncLimits.IdLength)]
    public string? ActivePlanId { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

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
    /// <summary>A note pinned to the exercise in this plan: each workout of it starts with it.</summary>
    [MaxLength(SyncLimits.TextLength)]
    public string? Note { get; set; }
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
    /// <summary>The user's own note on the exercise in this workout, typed in while doing it.</summary>
    [MaxLength(SyncLimits.TextLength)]
    public string? Note { get; set; }
    /// <summary>
    /// The note is pinned: on finishing, it's offered as the exercise's note in the plan (and unpinning one that came from
    /// the plan offers to take it off). Only the plan update applies it.
    /// </summary>
    public bool NotePinned { get; set; }
    /// <summary>Warm-up sets it had that were skipped (or not done by the end); the done ones are in <see cref="Sets"/>.</summary>
    public int SkippedWarmups { get; set; }
    /// <summary>Working sets it had that were skipped (or not done by the end); the done ones are in <see cref="Sets"/>.</summary>
    public int SkippedSets { get; set; }
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
    /// <summary>
    /// Skipped in the workout in progress: won't be done this time. Kept so it stays skipped when the app reopens; on
    /// finishing, skipped sets are left out and counted (SessionExercise.SkippedSets, SkippedWarmups).
    /// </summary>
    public bool IsSkipped { get; set; }
    // Times are stored as moments (wall-clock, like the workout's), never as counted seconds: how long a set, a warm-up
    // or a rest took is the difference between two of them. Set by SetTimes.
    /// <summary>When work on the set began: the end of whatever came before it in the workout (a rest, a set, the start).</summary>
    public DateTime? StartedAt { get; set; }
    /// <summary>When it was ticked done.</summary>
    public DateTime? CompletedAt { get; set; }
    /// <summary>When the rest after it began. Every set done starts one.</summary>
    public DateTime? RestStartedAt { get; set; }
    /// <summary>When that rest's countdown reaches zero (moved by −/+); past it, the rest runs over.</summary>
    public DateTime? RestDueAt { get; set; }
    /// <summary>
    /// When that rest really ended: the next set was started (or ticked), or the workout finished. Null while it's still
    /// going, even past <see cref="RestDueAt"/>.
    /// </summary>
    public DateTime? RestEndedAt { get; set; }
}

public class BodyWeightEntry : ISyncEntity
{
    /// <summary>One entry per calendar day, so the day is the identity unless set explicitly.</summary>
    [MaxLength(SyncLimits.IdLength)]
    public string Id { get => field ?? Date.ToString("yyyy-MM-dd"); set; }
    public DateTime Date { get; set; }
    public double WeightKg { get; set; }
    // Body composition, when a scale or watch measured it (read from Samsung Health or Health Connect). Null: not measured.
    [Range(1, 80)]
    public double? BodyFatPercent { get; set; }
    /// <summary>Fat-free mass: everything but fat.</summary>
    [Range(0, 500)]
    public double? LeanMassKg { get; set; }
    [Range(0, 50)]
    public double? BoneMassKg { get; set; }
    [Range(0, 300)]
    public double? BodyWaterKg { get; set; }
    /// <summary>Basal metabolic rate the measurement came with: the calories burned a day at complete rest.</summary>
    [Range(0, 10000)]
    public double? BmrKcal { get; set; }
    /// <summary>The app it was read from ("Samsung Health", "Health Connect"); null when logged in Gym Book.</summary>
    [MaxLength(SyncLimits.NameLength)]
    public string? Source { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

/// <summary>Something eaten, logged in the Nutrition tab.</summary>
public class FoodEntry : ISyncEntity
{
    [MaxLength(SyncLimits.IdLength)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>The day it counts toward (midnight, wall-clock).</summary>
    public DateTime Date { get; set; } = DateTime.Today;
    public MealType Meal { get; set; }
    [MaxLength(SyncLimits.NameLength)]
    public string Name { get; set; } = "";
    // The amounts are for everything eaten, all servings together.
    [Range(0, 20000)]
    public double Calories { get; set; }
    [Range(0, 2000)]
    public double ProteinG { get; set; }
    [Range(0, 2000)]
    public double CarbsG { get; set; }
    [Range(0, 2000)]
    public double FatG { get; set; }
    /// <summary>When it was logged, to keep a meal's foods in order.</summary>
    public DateTime LoggedAt { get; set; } = DateTime.Now;
    /// <summary>
    /// The health app it was logged in (Samsung Health, say), read through Health Connect, its Id "hc-" and the record's;
    /// null when logged in Gym Book. Read ones follow the health app: changed or deleted there, they change here.
    /// </summary>
    [MaxLength(SyncLimits.NameLength)]
    public string? Source { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

/// <summary>A food saved to "My foods" (or made by the user): its name, what one serving has and what a serving is. Kept in <see cref="UserProfile.MyFoods"/>.</summary>
public class SavedFood
{
    public const int Max = 500;

    [MaxLength(SyncLimits.NameLength)]
    public string Name { get; set; } = "";
    [Range(0, 20000)]
    public double Calories { get; set; }
    [Range(0, 2000)]
    public double ProteinG { get; set; }
    [Range(0, 2000)]
    public double CarbsG { get; set; }
    [Range(0, 2000)]
    public double FatG { get; set; }
    /// <summary>The brand, if any (a food made by the user has none).</summary>
    [MaxLength(SyncLimits.NameLength)]
    public string? Brand { get; set; }
    /// <summary>What a serving is ("1 bar", "1 cup"); null for "1 serving".</summary>
    [MaxLength(SyncLimits.NameLength)]
    public string? ServingText { get; set; }
    /// <summary>A serving's weight in grams, when known: then it can be logged by weight too.</summary>
    [Range(0, 5000)]
    public double? ServingG { get; set; }
}

/// <summary>
/// A supplement taken (creatine): how much, on which day and when. Logged with one tap on the Nutrition tab.
/// </summary>
public class SupplementDose : ISyncEntity
{
    public const string Creatine = "Creatine";

    [MaxLength(SyncLimits.IdLength)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    /// <summary>The day it counts toward (midnight, wall-clock).</summary>
    public DateTime Date { get; set; } = DateTime.Today;
    [MaxLength(SyncLimits.NameLength)]
    public string Name { get; set; } = Creatine;
    [Range(0, 100)]
    public double Grams { get; set; }
    /// <summary>When it was taken (wall-clock).</summary>
    public DateTime TakenAt { get; set; } = DateTime.Now;
    public DateTimeOffset UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}

/// <summary>
/// A day's totals read from the phone's health data (Samsung Health or Health Connect): energy burned, steps, and food
/// logged in other apps. One per calendar day, so the day is the identity, like <see cref="BodyWeightEntry"/>.
/// </summary>
public class HealthDay : ISyncEntity
{
    [MaxLength(SyncLimits.IdLength)]
    public string Id { get => field ?? Date.ToString("yyyy-MM-dd"); set; }
    public DateTime Date { get; set; }
    /// <summary>Everything burned that day: resting (basal) plus active.</summary>
    [Range(0, 50000)]
    public double? TotalBurnedKcal { get; set; }
    [Range(0, 50000)]
    public double? ActiveBurnedKcal { get; set; }
    [Range(0, 50000)]
    public double? BasalBurnedKcal { get; set; }
    [Range(0, 1_000_000)]
    public int? Steps { get; set; }
    // Food logged in other apps (e.g. Samsung Health's food diary), counted with what's logged here.
    [Range(0, 50000)]
    public double? FoodKcal { get; set; }
    [Range(0, 5000)]
    public double? FoodProteinG { get; set; }
    [Range(0, 5000)]
    public double? FoodCarbsG { get; set; }
    [Range(0, 5000)]
    public double? FoodFatG { get; set; }
    [MaxLength(SyncLimits.NameLength)]
    public string? Source { get; set; }
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
    [Range(50, 272)]
    public double? HeightCm { get; set; }
    /// <summary>For estimating calories burned when there's no health data; null until asked.</summary>
    public Sex? Sex { get; set; }
    /// <summary>How active a day is outside workouts, for the same estimate.</summary>
    public ActivityLevel ActivityLevel { get; set; } = ActivityLevel.Light;
    // Nutrition goals, per day; null: no goal set.
    [Range(500, 10000)]
    public int? CalorieGoal { get; set; }
    [Range(0, 1000)]
    public int? ProteinGoalG { get; set; }
    [Range(0, 1500)]
    public int? CarbsGoalG { get; set; }
    [Range(0, 600)]
    public int? FatGoalG { get; set; }
    /// <summary>Calories eaten minus burned the user aims for each day: negative for a deficit, positive for a surplus.</summary>
    [Range(-2000, 2000)]
    public int? EnergyBalanceGoal { get; set; }
    /// <summary>Where calories burned, steps and body measurements are read from.</summary>
    public HealthSource HealthSource { get; set; }
    /// <summary>
    /// The apps whose health data is read through Health Connect (their Android package names), picked by the user;
    /// null or empty: every app's. Not used with <see cref="HealthSource.SamsungHealth"/>, which reads Samsung Health itself.
    /// </summary>
    [MaxItems(30)]
    public List<string>? HealthApps { get; set; }
    /// <summary>No longer used: food logged in the health app always counts with what's logged in Gym Book. Kept for older data.</summary>
    public bool ImportHealthFood { get; set; } = true;
    /// <summary>Foods the user saved to find again in the food search ("My foods"), each with its amounts for one serving.</summary>
    [MaxItems(SavedFood.Max)]
    public List<SavedFood> MyFoods { get; set; } = [];
    /// <summary>The usual creatine dose, logged with one tap.</summary>
    [Range(0.5, 50)]
    public double CreatineDoseG { get; set; } = 5;
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

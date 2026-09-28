using System.ComponentModel.DataAnnotations;
using GymBook.Models;

namespace GymBook.Contracts;

public static class PlanLimits
{
    /// <summary>The most exercises the app offers the AI to choose from.</summary>
    public const int MaxCandidates = 800;
    public const int MaxDays = 7;
    public const int MaxExercisesPerDay = 12;
}

/// <summary>What the plan wizard asked, plus the exercises the plan may use (the app owns the exercise library).</summary>
public class GeneratePlanRequest
{
    public Goal Goal { get; set; }
    public Experience Experience { get; set; }
    [Range(1, PlanLimits.MaxDays)]
    public int DaysPerWeek { get; set; } = 3;
    [Range(15, 180)]
    public int SessionMinutes { get; set; } = 60;
    public EquipmentAccess EquipmentAccess { get; set; }
    public bool TrainNeck { get; set; } = true;
    [Range(10, 100)]
    public int? Age { get; set; }
    [Range(20, 400)]
    public double? BodyWeightKg { get; set; }
    /// <summary>How long the user has trained consistently, in years.</summary>
    [Range(0, 80)]
    public double? TrainingYears { get; set; }
    [Required, MinLength(1), MaxItems(PlanLimits.MaxCandidates)]
    public List<PlanCandidate> Exercises { get; set; } = [];
}

/// <summary>An exercise the AI may put in the plan.</summary>
public class PlanCandidate
{
    [Required, MaxLength(SyncLimits.IdLength)]
    public string Id { get; set; } = "";
    [Required, MaxLength(SyncLimits.NameLength)]
    public string Name { get; set; } = "";
    public MuscleGroup PrimaryMuscle { get; set; }
    public Mechanic Mechanic { get; set; }
    public Equipment Equipment { get; set; }
}

/// <summary>A generated plan. Every exercise id is one of the request's candidates.</summary>
public class GeneratePlanResponse
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<GeneratedWorkout> Workouts { get; set; } = [];
}

public class GeneratedWorkout
{
    public string Name { get; set; } = "";
    public List<GeneratedExercise> Exercises { get; set; } = [];
}

public class GeneratedExercise
{
    public string ExerciseId { get; set; } = "";
    public int Sets { get; set; }
    public int RepMin { get; set; }
    public int RepMax { get; set; }
    public int TargetRir { get; set; }
    public int RestSeconds { get; set; }
}

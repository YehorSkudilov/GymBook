using System.ComponentModel.DataAnnotations;
using GymBook.Models;

namespace GymBook.Contracts;

public static class PlanLimits
{
    /// <summary>The most exercises the app offers the AI to choose from.</summary>
    public const int MaxCandidates = 800;
    public const int MaxDays = 7;
    public const int MaxExercisesPerDay = 12;
    /// <summary>An imported plan keeps its workouts as long as the source has them, up to this.</summary>
    public const int MaxExercisesPerImportedDay = 20;
    public const int MaxQuestions = 6;
    public const int MaxOptions = 6;
    public const int QuestionLength = 300;
    public const int AnswerLength = 500;
    public const int MaxChatMessages = 40;
    public const int ChatMessageLength = 2000;
    public const int ImportTextLength = 20000;
    /// <summary>About 7.5 MB of file once decoded.</summary>
    public const int ImportFileBase64Length = 10_000_000;
}

/// <summary>The plan wizard's answers about the user. On its own, the request for follow-up questions.</summary>
public class PlanAnswers
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
}

/// <summary>The wizard's answers, the answers to the AI's follow-up questions, and the exercises the plan may use (the app owns the exercise library).</summary>
public class GeneratePlanRequest : PlanAnswers
{
    [Required, MinLength(1), MaxItems(PlanLimits.MaxCandidates)]
    public List<PlanCandidate> Exercises { get; set; } = [];
    [MaxItems(PlanLimits.MaxQuestions + 1)]
    public List<PlanAnswer> ExtraAnswers { get; set; } = [];
}

/// <summary>Follow-up questions the AI wants answered before it builds the plan.</summary>
public class PlanQuestionsResponse
{
    public List<PlanQuestion> Questions { get; set; } = [];
}

public class PlanQuestion
{
    public string Text { get; set; } = "";
    /// <summary>Whether several options can be picked.</summary>
    public bool Multiple { get; set; }
    public List<string> Options { get; set; } = [];
}

/// <summary>A follow-up question and what the user answered.</summary>
public class PlanAnswer
{
    [Required, MaxLength(PlanLimits.QuestionLength)]
    public string Question { get; set; } = "";
    [Required, MaxLength(PlanLimits.AnswerLength)]
    public string Answer { get; set; } = "";
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
    /// <summary>
    /// An imported plan: where its source puts rest days (0-based over workouts and rest days together). Empty when
    /// the source doesn't say, and for plans the AI designs.
    /// </summary>
    public List<int> RestDays { get; set; } = [];
    /// <summary>What's left after this one.</summary>
    public PlanQuotaResponse? Quota { get; set; }
}

/// <summary>How many AI plans the user may still generate.</summary>
public class PlanQuotaResponse
{
    public int Limit { get; set; }
    public int Remaining { get; set; }
    /// <summary>The window the limit applies to, e.g. "day" or "30 days".</summary>
    public string Period { get; set; } = "";
    /// <summary>When the oldest generation in the window expires, freeing up another; null when none are used.</summary>
    public DateTimeOffset? NextAvailableAt { get; set; }
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

/// <summary>A message to the plan's AI coach, with the plan as it is now and the conversation so far.</summary>
public class PlanChatRequest : PlanAnswers
{
    [Required]
    public GeneratePlanResponse Plan { get; set; } = new();
    /// <summary>The conversation, oldest first; the last one is the user's new message.</summary>
    [Required, MinLength(1), MaxItems(PlanLimits.MaxChatMessages)]
    public List<PlanChatMessage> Messages { get; set; } = [];
    [Required, MinLength(1), MaxItems(PlanLimits.MaxCandidates)]
    public List<PlanCandidate> Exercises { get; set; } = [];
}

public class PlanChatMessage
{
    /// <summary>True for the user, false for the AI.</summary>
    public bool FromUser { get; set; }
    [Required, MaxLength(PlanLimits.ChatMessageLength)]
    public string Text { get; set; } = "";
}

/// <summary>The AI's reply, and the changed plan when it changed anything.</summary>
public class PlanChatResponse
{
    public string Reply { get; set; } = "";
    public GeneratePlanResponse? Plan { get; set; }
    public PlanQuotaResponse? Quota { get; set; }
}

/// <summary>
/// A plan from somewhere else for the AI to turn into a GymBook plan: pasted text, a link (to a web page, image or document), or a file
/// (a photo or screenshot, a PDF, or a text file). At least one of them is set.
/// </summary>
public class ImportPlanRequest : PlanAnswers
{
    [MaxLength(PlanLimits.ImportTextLength)]
    public string? Text { get; set; }
    [MaxLength(2000)]
    public string? Link { get; set; }
    public ImportFile? File { get; set; }
    [Required, MinLength(1), MaxItems(PlanLimits.MaxCandidates)]
    public List<PlanCandidate> Exercises { get; set; } = [];
}

public class ImportFile
{
    [Required, MaxLength(260)]
    public string Name { get; set; } = "";
    /// <summary>e.g. image/jpeg, image/png, application/pdf, text/plain.</summary>
    [Required, MaxLength(100)]
    public string ContentType { get; set; } = "";
    [Required, MaxLength(PlanLimits.ImportFileBase64Length)]
    public string Base64 { get; set; } = "";
}

public static class PlanReviewLimits
{
    public const int MaxExercises = 100;
    public const int MaxSessionsPerExercise = 8;
    public const int MaxSetsPerSession = 20;
    public const int MaxSuggestions = 6;
    public const int MaxChangesPerSuggestion = 12;
}

/// <summary>A plan and how the user has actually been doing on it, for the AI to suggest improvements.</summary>
public class PlanReviewRequest : PlanAnswers
{
    [Required]
    public GeneratePlanResponse Plan { get; set; } = new();
    [Required]
    public PlanPerformance Performance { get; set; } = new();
    /// <summary>What the AI may swap in or add.</summary>
    [Required, MinLength(1), MaxItems(PlanLimits.MaxCandidates)]
    public List<PlanCandidate> Exercises { get; set; } = [];
}

/// <summary>A summary of the logged workouts, worked out on the device so only what matters is sent.</summary>
public class PlanPerformance
{
    /// <summary>Weeks since the plan was started (its first logged workout).</summary>
    [Range(0, 1000)]
    public int WeeksOnPlan { get; set; }
    /// <summary>Workouts of this plan finished in that time, and how many the plan called for.</summary>
    [Range(0, 10000)]
    public int WorkoutsDone { get; set; }
    [Range(0, 10000)]
    public int WorkoutsPlanned { get; set; }
    [Range(0, 600)]
    public int? AverageSessionMinutes { get; set; }
    /// <summary>Hard sets per muscle per week, over the last 4 weeks.</summary>
    [MaxItems(40)]
    public List<MuscleVolume> WeeklySets { get; set; } = [];
    /// <summary>The recent sessions of each exercise in the plan.</summary>
    [MaxItems(PlanReviewLimits.MaxExercises)]
    public List<ExerciseHistory> History { get; set; } = [];
}

public class MuscleVolume
{
    public MuscleGroup Muscle { get; set; }
    [Range(0, 200)]
    public double Sets { get; set; }
}

public class ExerciseHistory
{
    [Required, MaxLength(SyncLimits.IdLength)]
    public string ExerciseId { get; set; } = "";
    /// <summary>Newest first.</summary>
    [MaxItems(PlanReviewLimits.MaxSessionsPerExercise)]
    public List<ExerciseSessionSummary> Sessions { get; set; } = [];
}

public class ExerciseSessionSummary
{
    [Range(0, 10000)]
    public int DaysAgo { get; set; }
    /// <summary>The target that day.</summary>
    public int RepMin { get; set; }
    public int RepMax { get; set; }
    public int TargetRir { get; set; }
    /// <summary>Working sets that were planned but not done.</summary>
    [Range(0, 100)]
    public int SkippedSets { get; set; }
    /// <summary>Completed working sets, warm-ups left out.</summary>
    [MaxItems(PlanReviewLimits.MaxSetsPerSession)]
    public List<LoggedSet> Sets { get; set; } = [];
}

public class LoggedSet
{
    [Range(0, 2000)]
    public double WeightKg { get; set; }
    [Range(0, 1000)]
    public int Reps { get; set; }
    [Range(0, 10)]
    public int? Rir { get; set; }
}

/// <summary>What the AI makes of the user's training on the plan, and what it would change.</summary>
public class PlanReviewResponse
{
    /// <summary>A few sentences on how it's going.</summary>
    public string Summary { get; set; } = "";
    public List<PlanSuggestion> Suggestions { get; set; } = [];
    public PlanQuotaResponse? Quota { get; set; }
}

public class PlanSuggestion
{
    public string Title { get; set; } = "";
    /// <summary>Why, pointing at the data.</summary>
    public string Reason { get; set; } = "";
    /// <summary>The edits that carry it out; empty for advice with nothing to change in the plan.</summary>
    public List<PlanChange> Changes { get; set; } = [];
}

/// <summary>One edit to one exercise of the plan. Numbers of 0 keep the current value (for TargetRir, -1 does, since 0 is a real target).</summary>
public class PlanChange
{
    /// <summary>Index of the workout in the plan.</summary>
    public int WorkoutIndex { get; set; }
    /// <summary>"update", "replace", "add" or "remove".</summary>
    public string Action { get; set; } = "";
    /// <summary>The exercise in that workout it applies to; empty for "add".</summary>
    public string ExerciseId { get; set; } = "";
    /// <summary>For "replace" and "add": the exercise to put in.</summary>
    public string NewExerciseId { get; set; } = "";
    public int Sets { get; set; }
    public int RepMin { get; set; }
    public int RepMax { get; set; }
    public int TargetRir { get; set; }
    public int RestSeconds { get; set; }
}

public static class PlanChangeActions
{
    public const string Update = "update";
    public const string Replace = "replace";
    public const string Add = "add";
    public const string Remove = "remove";
}

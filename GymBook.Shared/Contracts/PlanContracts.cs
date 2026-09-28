using System.ComponentModel.DataAnnotations;
using GymBook.Models;

namespace GymBook.Contracts;

public static class PlanLimits
{
    /// <summary>The most exercises the app offers the AI to choose from.</summary>
    public const int MaxCandidates = 800;
    public const int MaxDays = 7;
    public const int MaxExercisesPerDay = 12;
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

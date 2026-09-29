using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Models;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

/// <summary>
/// The AI's suggestions for a plan, from the workouts logged on it: a short verdict, then suggestions that each say
/// why and what they'd change, and can be applied with one tap. Opened from the plan's page.
/// </summary>
public partial class PlanReviewViewModel(DataStore store, AiPlanService ai) : BaseViewModel, IQueryAttributable
{
    string? _id;
    bool _loaded;

    public ObservableCollection<PlanSuggestionItem> Suggestions { get; } = [];

    [ObservableProperty] string title = "";
    [ObservableProperty] string summary = "";
    [ObservableProperty] string updatedText = "";
    [ObservableProperty] string quotaText = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    string error = "";
    [ObservableProperty] bool isLoading;
    [ObservableProperty] bool hasResult;
    [ObservableProperty] bool hasNoSuggestions;
    /// <summary>Nothing logged on the plan yet, so there's nothing to go on.</summary>
    [ObservableProperty] bool needsWorkouts;
    /// <summary>Signed in and something logged: asking (again) is possible.</summary>
    [ObservableProperty] bool canAsk;

    public bool HasError => Error.Length > 0;

    public static Task OpenAsync(WorkoutPlan plan) => Shell.Current.GoToAsync($"{Routes.PlanReview}?id={plan.Id}");

    public void ApplyQueryAttributes(IDictionary<string, object> query) => _id = query["id"]?.ToString();

    WorkoutPlan? Plan => store.GetPlan(_id);

    public override async Task OnAppearingAsync()
    {
        if (_loaded || Plan is not { } plan)
            return;
        _loaded = true;
        Title = plan.Name;
        if (!ai.IsAvailable)
        {
            Error = "AI suggestions need an account. Sign in from the Profile tab.";
            return;
        }
        if (ai.SessionsOf(plan).Count == 0)
        {
            NeedsWorkouts = true;
            return;
        }
        CanAsk = true;
        // Already looked at this run: show that instead of spending another request. Refresh asks again.
        if (ai.LastReview(plan) is { } last)
            Show(last.Review, last.At);
        else
            await Load();
    }

    [RelayCommand]
    async Task Load()
    {
        if (Plan is not { } plan || IsLoading)
            return;
        Error = "";
        IsLoading = true;
        try
        {
            Show(await ai.ReviewAsync(plan), DateTime.Now);
        }
        catch (Exception e) when (e is ApiException or SessionExpiredException)
        {
            Error = e.Message;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            Error = "Couldn't reach the AI. Check your connection and try again.";
        }
        finally
        {
            IsLoading = false;
            QuotaText = ai.ChatQuota is { } q ? AiPlanService.Describe(q, "AI coach messages") : "";
        }
    }

    void Show(PlanReviewResponse review, DateTime at)
    {
        Summary = review.Summary;
        UpdatedText = at > DateTime.Now.AddMinutes(-1) ? "Just now" : $"From {at:t}";
        Suggestions.Clear();
        foreach (var s in review.Suggestions)
            Suggestions.Add(new PlanSuggestionItem(s, Describe(s), Apply));
        HasNoSuggestions = Suggestions.Count == 0;
        HasResult = true;
    }

    void Apply(PlanSuggestionItem item)
    {
        if (Plan is not { } plan)
            return;
        if (ai.Apply(plan, item.Suggestion) == 0)
        {
            item.Note = "The plan has changed since, so this no longer fits. Refresh for new suggestions.";
            return;
        }
        store.Save();
        item.IsApplied = true;
        item.Note = "Applied to your plan";
    }

    /// <summary>Each edit in words, e.g. "Upper A: swap Bench Press for Incline Dumbbell Press · 3 sets · 8–10 reps".</summary>
    List<string> Describe(PlanSuggestion s)
    {
        var plan = Plan;
        string Name(string id) => store.GetExercise(id)?.Name ?? "an exercise";
        return [.. s.Changes.Select(c =>
        {
            var workout = plan != null && c.WorkoutIndex < plan.Workouts.Count ? plan.Workouts[c.WorkoutIndex].Name : $"Workout {c.WorkoutIndex + 1}";
            var what = c.Action switch
            {
                PlanChangeActions.Replace => $"swap {Name(c.ExerciseId)} for {Name(c.NewExerciseId)}",
                PlanChangeActions.Add => $"add {Name(c.NewExerciseId)}",
                PlanChangeActions.Remove => $"remove {Name(c.ExerciseId)}",
                _ => Name(c.ExerciseId),
            };
            var numbers = new List<string> { $"{workout}: {what}" };
            if (c.Action != PlanChangeActions.Remove)
            {
                if (c.Sets > 0)
                    numbers.Add($"{c.Sets} sets");
                if (c.RepMin > 0 || c.RepMax > 0)
                    numbers.Add(c.RepMin > 0 && c.RepMax > 0 && c.RepMin != c.RepMax ? $"{c.RepMin}–{c.RepMax} reps" : $"{Math.Max(c.RepMin, c.RepMax)} reps");
                if (c.TargetRir >= 0)
                    numbers.Add($"{c.TargetRir} RIR");
                if (c.RestSeconds > 0)
                    numbers.Add($"{Units.Rest(c.RestSeconds)} rest");
            }
            return string.Join(" · ", numbers);
        })];
    }

    [RelayCommand]
    Task Close() => GoBack();
}

/// <summary>One suggestion on the review sheet.</summary>
public partial class PlanSuggestionItem(PlanSuggestion suggestion, List<string> changes, Action<PlanSuggestionItem> apply) : ObservableObject
{
    public PlanSuggestion Suggestion { get; } = suggestion;
    public string Title => Suggestion.Title;
    public string Reason => Suggestion.Reason;
    public List<string> Changes { get; } = changes;
    public bool HasChanges => Changes.Count > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanApply))]
    bool isApplied;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasNote))]
    string note = "";

    public bool CanApply => HasChanges && !IsApplied;
    public bool HasNote => Note.Length > 0;

    [RelayCommand]
    void Apply() => apply(this);
}

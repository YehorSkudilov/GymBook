using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Models;
using GymBook.Services;
using GymBook.Views;

namespace GymBook.ViewModels;

/// <summary>
/// Step-by-step questionnaire used for first-run onboarding, for generating new plans, and for regenerating a saved
/// plan (opened with ?regenerate=id, it starts from that plan's answers and replaces the plan when saved). Signed-in
/// users also get the AI's follow-up questions before the plan is built, and can chat with the AI to change it.
/// </summary>
public partial class PlanWizardViewModel(DataStore store, Units units, DialogService dialogs, AiPlanService ai, IServiceProvider services)
    : BaseViewModel, IQueryAttributable
{
    enum Step { Welcome, About, Goal, Experience, Days, Duration, Equipment, Questions, Result }

    List<Step> _steps = [];
    int _index;
    WorkoutPlan? _plan;
    // Bumped on every step change, so a plan or questions arriving after the user went back are ignored.
    int _buildRun;
    // The saved plan being regenerated, if any.
    WorkoutPlan? _regenerating;
    string? _regenerateId;

    // Answers
    Goal _goal;
    Experience _experience;
    int _days;
    int _minutes;
    EquipmentAccess _equipment;

    // The AI's follow-up questions, for the answers they were asked about (asked again when those change).
    string? _questionsFor;
    List<PlanAnswer> _extraAnswers = [];

    [ObservableProperty] bool isOnboarding;
    [ObservableProperty] string title = "";
    [ObservableProperty] string subtitle = "";
    [ObservableProperty] double progress;
    [ObservableProperty] bool isWelcome;
    [ObservableProperty] bool isAbout;
    [ObservableProperty] bool isOptions;
    [ObservableProperty] bool isQuestions;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPlan), nameof(CanRegenerate), nameof(CanChat))]
    bool isResult;
    [ObservableProperty] bool canGoBack;
    [ObservableProperty] string nextText = "Continue";
    [ObservableProperty] string userName = "";
    [ObservableProperty] string bodyWeight = "";
    [ObservableProperty] string planName = "";
    [ObservableProperty] string planDescription = "";
    [ObservableProperty] List<WorkoutPreviewItem> planWorkouts = [];

    /// <summary>Waiting for the AI: the follow-up questions, or the plan.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPlan), nameof(CanRegenerate), nameof(CanChat), nameof(ShowQuestions))]
    bool isGenerating;
    [ObservableProperty] string generatingText = "";
    public bool ShowPlan => IsResult && !IsGenerating;
    public bool ShowQuestions => IsQuestions && !IsGenerating;
    public bool CanRegenerate => IsResult && !IsGenerating && ai.IsAvailable && !ai.IsQuotaUsedUp;
    // Opened through Shell, which first-run onboarding runs before.
    public bool CanChat => IsResult && !IsGenerating && ai.IsAvailable && _plan != null && Shell.Current != null;
    /// <summary>Where the plan came from: the AI, or the built-in generator and why.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPlanNote))]
    string planNote = "";
    public bool HasPlanNote => PlanNote.Length > 0;
    /// <summary>Why the follow-up questions didn't come, shown on their step with Try again.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasQuestionsError))]
    string questionsError = "";
    public bool HasQuestionsError => QuestionsError.Length > 0;
    /// <summary>Anything else the user wants the AI to know, under the follow-up questions.</summary>
    [ObservableProperty] string extraNote = "";

    public ObservableCollection<OptionItem> Options { get; } = [];
    public ObservableCollection<ChipItem> UnitChips { get; } = [];
    public ObservableCollection<AiQuestionItem> Questions { get; } = [];

    public void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _regenerateId = query.TryGetValue("regenerate", out var id) ? id?.ToString() : null;

    public void Start(bool onboarding)
    {
        IsOnboarding = onboarding;
        // Ready by the last steps, so a used-up quota skips straight to the standard plan.
        _ = ai.RefreshQuotaAsync();
        var p = store.Profile;
        (_goal, _experience, _days, _minutes, _equipment) = (p.Goal, p.Experience, p.DaysPerWeek, p.SessionMinutes, p.EquipmentAccess);
        _regenerating = onboarding ? null : store.GetPlan(_regenerateId);
        if (_regenerating != null)
        {
            // The plan's own goal and days; the rest was saved to the profile when a plan was last made.
            _goal = _regenerating.Goal;
            _days = Math.Clamp(_regenerating.Workouts.Count > 0 ? _regenerating.Workouts.Count : _regenerating.DaysPerWeek, 2, 6);
        }
        UserName = p.Name;
        _steps = onboarding
            ? [Step.Welcome, Step.About, Step.Goal, Step.Experience, Step.Days, Step.Duration, Step.Equipment, Step.Questions, Step.Result]
            : [Step.Goal, Step.Experience, Step.Days, Step.Duration, Step.Equipment, Step.Questions, Step.Result];

        UnitChips.Clear();
        foreach (var u in Enum.GetValues<WeightUnit>())
            UnitChips.Add(new ChipItem(u == WeightUnit.Kg ? "Kilograms (kg)" : "Pounds (lbs)", u, SelectUnit) { IsSelected = p.Unit == u });
        BodyWeight = units.Format(p.BodyWeightKg);
        _index = 0;
        Show();
    }

    public override Task OnAppearingAsync()
    {
        if (_steps.Count == 0)
            Start(onboarding: false);
        return Task.CompletedTask;
    }

    void SelectUnit(ChipItem chip)
    {
        // Keep the typed body weight meaning the same thing when switching units.
        var had = units.TryParse(BodyWeight, out var kg);
        foreach (var c in UnitChips)
            c.IsSelected = c == chip;
        store.Profile.Unit = (WeightUnit)chip.Value!;
        if (had)
            BodyWeight = units.Format(kg);
    }

    /// <summary>The follow-up questions only exist for the AI, and only when it had some for these answers.</summary>
    bool Skipped(Step step) => step == Step.Questions
        && (!ai.IsAvailable || ai.IsQuotaUsedUp || _questionsFor == AnswersKey() && Questions.Count == 0);

    void Show()
    {
        _buildRun++;
        IsGenerating = false;
        var step = _steps[_index];
        QuestionsError = "";
        Progress = (_index + 1.0) / _steps.Count;
        CanGoBack = _index > 0;
        IsWelcome = step == Step.Welcome;
        IsAbout = step == Step.About;
        IsQuestions = step == Step.Questions;
        IsResult = step == Step.Result;
        IsOptions = !IsWelcome && !IsAbout && !IsQuestions && !IsResult;
        NextText = step switch
        {
            Step.Welcome => "Get started",
            Step.Questions => "Build my plan",
            Step.Result => IsOnboarding ? "Start training" : _regenerating != null ? "Replace plan" : "Save plan",
            _ => "Continue",
        };
        Options.Clear();

        switch (step)
        {
            case Step.Welcome:
                Title = "Welcome to GymBook";
                Subtitle = "Your training log that tells you what to lift next. Answer a few questions and we'll build a plan around you.";
                break;
            case Step.About:
                Title = "About you";
                Subtitle = "Used to personalise starting weights and your statistics.";
                break;
            case Step.Goal:
                Title = _regenerating != null ? $"Regenerate {_regenerating.Name}" : "What's your main goal?";
                Subtitle = _regenerating != null
                    ? "Change any answers, and a new plan replaces this one. Your logged workouts stay. What's your main goal?"
                    : "This sets your rep ranges, rest times and exercise choice.";
                AddOptions(TrainingGoals.All, g => g.Display(), g => g.Description(), _goal);
                break;
            case Step.Experience:
                Title = "How experienced are you?";
                Subtitle = "This decides volume and how close to failure you train.";
                AddOptions(Enum.GetValues<Experience>(), e => e.Display(), e => e.Description(), _experience);
                break;
            case Step.Days:
                Title = "How many days a week?";
                Subtitle = "Pick what you can stick to. Consistency beats volume.";
                AddOptions([2, 3, 4, 5, 6], d => $"{d} days", d => d switch
                {
                    2 => "Full body twice a week",
                    3 => "Full body or push / pull / legs",
                    4 => "Upper / lower split",
                    5 => "Upper / lower + push / pull / legs",
                    _ => "Push / pull / legs twice",
                }, _days);
                break;
            case Step.Duration:
                Title = "How long is a session?";
                Subtitle = "Controls how many exercises each workout includes.";
                AddOptions([30, 45, 60, 90], m => $"{m} minutes", m => m switch
                {
                    30 => "4 exercises. Quick and focused",
                    45 => "5 exercises",
                    60 => "6 exercises. Recommended",
                    _ => "8 exercises. Maximum volume",
                }, _minutes);
                break;
            case Step.Equipment:
                Title = "Where do you train?";
                Subtitle = "We'll only pick exercises you can actually do.";
                AddOptions(Enum.GetValues<EquipmentAccess>(), e => e.Display(), e => e.Description(), _equipment);
                break;
            case Step.Questions:
                Title = "A few more questions";
                Subtitle = "Your answers help the AI fit the plan to you. Skip any you like.";
                _ = LoadQuestionsAsync();
                break;
            case Step.Result:
                _ = BuildPlanAsync();
                break;
        }
    }

    void AddOptions<T>(IEnumerable<T> values, Func<T, string> title, Func<T, string> subtitle, T selected) where T : notnull
    {
        foreach (var v in values)
            Options.Add(new OptionItem(title(v), subtitle(v), v, OnOptionSelected) { IsSelected = v.Equals(selected) });
    }

    void OnOptionSelected(OptionItem item)
    {
        foreach (var o in Options)
            o.IsSelected = o == item;
        switch (item.Value)
        {
            case Goal g: _goal = g; break;
            case Experience e: _experience = e; break;
            case EquipmentAccess a: _equipment = a; break;
            case int i when _steps[_index] == Step.Days: _days = i; break;
            case int m: _minutes = m; break;
        }
    }

    UserProfile Answers()
    {
        var p = store.Profile;
        return new UserProfile
        {
            Goal = _goal,
            Experience = _experience,
            DaysPerWeek = _days,
            SessionMinutes = _minutes,
            EquipmentAccess = _equipment,
            TrainNeck = p.TrainNeck,
            BodyWeightKg = units.TryParse(BodyWeight, out var kg) && kg > 0 ? kg : p.BodyWeightKg,
            BirthYear = p.BirthYear,
            TrainingSince = p.TrainingSince,
            CompoundRestSeconds = p.CompoundRestSeconds,
            IsolationRestSeconds = p.IsolationRestSeconds,
        };
    }

    string AnswersKey() => $"{_goal}|{_experience}|{_days}|{_minutes}|{_equipment}";

    /// <summary>
    /// Asks the AI what else it wants to know. The questions are kept while the answers stay the same. If the AI can't
    /// be reached the step says why, with Try again, and Continue builds the plan from the answers alone.
    /// </summary>
    async Task LoadQuestionsAsync()
    {
        if (_questionsFor == AnswersKey())
            return;
        var run = _buildRun;
        QuestionsError = "";
        IsGenerating = true;
        GeneratingText = "Thinking of a few questions for you…";
        List<PlanQuestion> questions;
        try
        {
            questions = await ai.QuestionsAsync(Answers());
        }
        catch (Exception e)
        {
            if (run != _buildRun)
                return;
            IsGenerating = false;
            Questions.Clear();
            QuestionsError = e is Services.Sync.ApiException or Services.Sync.SessionExpiredException
                ? $"The AI couldn't come up with questions: {e.Message}"
                : "Couldn't reach the AI for follow-up questions. Check your connection.";
            NextText = "Continue without";
            return;
        }
        if (run != _buildRun)
            return;
        IsGenerating = false;
        NextText = "Build my plan";

        Questions.Clear();
        foreach (var q in questions)
            Questions.Add(new AiQuestionItem(q));
        _questionsFor = AnswersKey();
        if (Questions.Count == 0)
        {
            // Nothing to ask: on to the plan.
            _index++;
            Show();
        }
    }

    [RelayCommand]
    Task RetryQuestions() => LoadQuestionsAsync();

    /// <summary>
    /// The plan for the answers: made by AI when signed in, otherwise (or if that fails) by the built-in generator.
    /// </summary>
    async Task BuildPlanAsync()
    {
        var run = _buildRun;
        var answers = Answers();

        WorkoutPlan plan;
        if (ai.IsAvailable && !ai.IsQuotaUsedUp)
        {
            IsGenerating = true;
            GeneratingText = "Picking exercises, sets and rest for you…";
            Title = "Building your plan…";
            Subtitle = "AI is putting together a plan for your answers. This can take up to a minute.";
            try
            {
                plan = await ai.GenerateAsync(answers, _extraAnswers);
                PlanNote = ai.Quota is { } q ? $"Made by AI for your answers. {AiPlanService.Describe(q)}." : "Made by AI for your answers.";
            }
            catch (Exception e)
            {
                plan = PlanGenerator.Generate(answers);
                var reason = e is Services.Sync.ApiException or Services.Sync.SessionExpiredException ? e.Message : "Couldn't reach the plan generator.";
                PlanNote = $"{reason} Here's a standard plan instead.";
            }
            if (run != _buildRun)
                return;
            IsGenerating = false;
        }
        else
        {
            plan = PlanGenerator.Generate(answers);
            PlanNote = ai.IsAvailable && ai.Quota is { } q ? $"{AiPlanService.Describe(q)}. Here's a standard plan instead." : "Sign in to have AI build a plan around your answers.";
        }

        Title = "Your plan is ready";
        Subtitle = "You can edit every workout later, or ask the AI to change it. Weights and reps adapt automatically as you log.";
        ShowPlanPreview(plan);
    }

    /// <summary>Asks the AI for a different plan for the same answers.</summary>
    [RelayCommand]
    Task Regenerate()
    {
        _buildRun++;
        return BuildPlanAsync();
    }

    /// <summary>Chat with the AI about the plan before saving it; changes show up here.</summary>
    [RelayCommand]
    Task Chat() => _plan is not { } plan ? Task.CompletedTask : PlanChatViewModel.OpenAsync(plan, Answers(), save: false, onChanged: () => ShowPlanPreview(plan));

    void ShowPlanPreview(WorkoutPlan plan)
    {
        _plan = plan;
        PlanName = _plan.Name;
        PlanDescription = _plan.Description;
        PlanWorkouts = _plan.Workouts.Select(w => new WorkoutPreviewItem
        {
            Name = w.Name,
            Meta = $"{w.Exercises.Count} exercises · {w.Exercises.Sum(e => e.Sets)} sets",
            Exercises = string.Join("\n", w.Exercises.Select(e => $"{e.Sets} × {e.RepMin}–{e.RepMax}   {store.GetExercise(e.ExerciseId)?.Name}")),
        }).ToList();
        OnPropertyChanged(nameof(CanChat));
    }

    [RelayCommand]
    Task SignIn() => Views.AccountPage.ShowAsync(services);

    [RelayCommand]
    async Task Next()
    {
        if (IsGenerating)
            return;
        var step = _steps[_index];
        if (step == Step.About && !units.TryParse(BodyWeight, out _))
        {
            await dialogs.Alert("Body weight", "Please enter your body weight as a number.");
            return;
        }
        if (step == Step.Result)
        {
            await Finish();
            return;
        }
        if (step == Step.Questions)
            _extraAnswers = CollectAnswers();
        do
            _index++;
        while (_index < _steps.Count - 1 && Skipped(_steps[_index]));
        Show();
    }

    [RelayCommand]
    async Task Back()
    {
        var back = _index - 1;
        while (back > 0 && Skipped(_steps[back]))
            back--;
        if (back < 0)
        {
            if (!IsOnboarding)
                await GoBack();
            return;
        }
        _index = back;
        Show();
    }

    List<PlanAnswer> CollectAnswers()
    {
        var answers = Questions
            .Where(q => q.Answer.Length > 0)
            .Select(q => new PlanAnswer { Question = Clip(q.Text, PlanLimits.QuestionLength), Answer = Clip(q.Answer, PlanLimits.AnswerLength) })
            .ToList();
        if (!string.IsNullOrWhiteSpace(ExtraNote))
            answers.Add(new PlanAnswer { Question = "Anything else?", Answer = Clip(ExtraNote.Trim(), PlanLimits.AnswerLength) });
        return answers;
    }

    static string Clip(string text, int max) => text.Length <= max ? text : text[..max];

    async Task Finish()
    {
        if (_plan != null && _regenerating != null
            && !await dialogs.Confirm("Replace plan?", $"The new plan replaces the workouts of {_regenerating.Name}. Workouts you logged stay in your history.", "Replace"))
            return;

        var p = store.Profile;
        p.Goal = _goal;
        p.Experience = _experience;
        p.DaysPerWeek = _days;
        p.SessionMinutes = _minutes;
        p.EquipmentAccess = _equipment;
        if (IsOnboarding)
        {
            p.Name = UserName.Trim();
            if (units.TryParse(BodyWeight, out var kg))
            {
                p.BodyWeightKg = kg;
                store.Data.BodyWeights.Add(new BodyWeightEntry { Date = DateTime.Today, WeightKg = kg });
            }
        }

        if (_plan != null && _regenerating != null)
        {
            // Same plan, new contents. Workouts keep their ids by position, so logged sessions still count toward its weeks.
            var old = _regenerating.Workouts;
            for (var i = 0; i < _plan.Workouts.Count && i < old.Count; i++)
                _plan.Workouts[i].Id = old[i].Id;
            _regenerating.Name = _plan.Name;
            _regenerating.Description = _plan.Description;
            _regenerating.Goal = _plan.Goal;
            _regenerating.DaysPerWeek = _plan.DaysPerWeek;
            _regenerating.Workouts = _plan.Workouts;
            _regenerating.RestDays = _plan.RestDays;
        }
        else if (_plan != null)
        {
            store.Data.Plans.Add(_plan);
            store.Data.ActivePlanId = _plan.Id;
        }
        var wasOnboarding = IsOnboarding;
        p.OnboardingDone = true;
        store.Save();

        if (wasOnboarding)
            App.ShowMainShell();
        else if (_regenerating != null)
            await GoBack();
        else
            await MainPage.ShowTab(AppTab.Plans);
    }
}

/// <summary>One of the AI's follow-up questions, with its options as chips.</summary>
public partial class AiQuestionItem : ObservableObject
{
    public AiQuestionItem(PlanQuestion question)
    {
        Text = question.Text;
        Multiple = question.Multiple;
        foreach (var option in question.Options)
            Options.Add(new ChipItem(option, option, Toggle));
    }

    public string Text { get; }
    public bool Multiple { get; }
    public string Hint => Multiple ? "Pick any that apply" : "";
    public bool HasHint => Multiple;
    public ObservableCollection<ChipItem> Options { get; } = [];

    /// <summary>The picked options, comma-separated; empty when skipped.</summary>
    public string Answer => string.Join(", ", Options.Where(o => o.IsSelected).Select(o => o.Title));

    void Toggle(ChipItem chip)
    {
        if (Multiple)
        {
            chip.IsSelected = !chip.IsSelected;
            return;
        }
        var select = !chip.IsSelected;
        foreach (var o in Options)
            o.IsSelected = false;
        chip.IsSelected = select;
    }
}

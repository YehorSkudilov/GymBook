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
public partial class PlanWizardViewModel(DataStore store, Units units, DialogService dialogs, AiPlanService ai)
    : BaseViewModel, IQueryAttributable
{
    enum Step { Welcome, About, Goal, Experience, Days, Duration, Equipment, Neck, BuildWith, Programs, Questions, Result }

    List<Step> _steps = [];
    int _index;
    WorkoutPlan? _plan;
    // Bumped on every step change, so a plan or questions arriving after the user went back are ignored.
    int _buildRun;
    // The AI request in flight (questions or plan); cancelled when the step changes or the wizard is left.
    CancellationTokenSource? _aiCts;
    // The saved plan being regenerated, if any.
    WorkoutPlan? _regenerating;
    string? _regenerateId;

    // Answers
    Goal _goal;
    Experience _experience;
    int _days;
    int _minutes;
    EquipmentAccess _equipment;
    bool _neck;
    // The last choice: build the plan with AI (follow-up questions, then generation) or from a signature program.
    bool _useAi = true;
    // The signature program picked, and the answers it was recommended for (the top pick is chosen again when they change).
    string? _programId;
    string? _programsFor;
    bool _allPrograms;

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
    [ObservableProperty] bool isPrograms;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowQuestions))]
    bool isQuestions;
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
    public bool CanRegenerate => IsResult && !IsGenerating && _useAi && ai.IsAvailable && !ai.IsQuotaUsedUp;
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
    public ObservableCollection<ProgramItem> Programs { get; } = [];
    public ObservableCollection<ChipItem> ProgramTabs { get; } = [];

    /// <summary>On a plan built from a signature program: who it's from.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasProgramAuthor))]
    string programAuthor = "";
    [ObservableProperty] string programBio = "";
    public bool HasProgramAuthor => ProgramAuthor.Length > 0;

    public void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _regenerateId = query.TryGetValue("regenerate", out var id) ? id?.ToString() : null;

    public void Start(bool onboarding)
    {
        IsOnboarding = onboarding;
        // Signing in from the welcome screen without a finished profile stays here: the link goes.
        if (onboarding)
            IPlatformApplication.Current!.Services.GetRequiredService<GymBook.Services.Sync.AccountService>().Changed +=
                (_, _) => MainThread.BeginInvokeOnMainThread(UpdateShowSignIn);
        // Ready by the last steps, so a used-up quota skips straight to the signature programs.
        _ = ai.RefreshQuotaAsync();
        var p = store.Profile;
        (_goal, _experience, _days, _minutes, _equipment, _neck) = (p.Goal, p.Experience, p.DaysPerWeek, p.SessionMinutes, p.EquipmentAccess, p.TrainNeck);
        _regenerating = onboarding ? null : store.GetPlan(_regenerateId);
        if (_regenerating != null)
        {
            // The plan's own goal and days; the rest was saved to the profile when a plan was last made.
            _goal = _regenerating.Goal;
            _days = Math.Clamp(_regenerating.Workouts.Count > 0 ? _regenerating.Workouts.Count : _regenerating.DaysPerWeek, 2, 6);
        }
        UserName = p.Name;
        _steps = onboarding
            ? [Step.Welcome, Step.About, Step.Goal, Step.Experience, Step.Days, Step.Duration, Step.Equipment, Step.Neck, Step.BuildWith, Step.Programs, Step.Questions, Step.Result]
            : [Step.Goal, Step.Experience, Step.Days, Step.Duration, Step.Equipment, Step.Neck, Step.BuildWith, Step.Programs, Step.Questions, Step.Result];

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

    /// <summary>
    /// The AI steps only exist when AI can be used; the questions only when it was chosen, and had some for these answers.
    /// The signature programs are offered whenever the AI isn't building the plan.
    /// </summary>
    bool Skipped(Step step) => step switch
    {
        Step.BuildWith => !AiUsable,
        Step.Programs => AiUsable && _useAi,
        Step.Questions => !AiUsable || !_useAi || _questionsFor == AnswersKey() && Questions.Count == 0,
        _ => false,
    };

    bool AiUsable => ai.IsAvailable && !ai.IsQuotaUsedUp;

    void Show()
    {
        _buildRun++;
        CancelAi();
        IsGenerating = false;
        var step = _steps[_index];
        QuestionsError = "";
        Progress = (_index + 1.0) / _steps.Count;
        CanGoBack = _index > 0;
        IsWelcome = step == Step.Welcome;
        UpdateShowSignIn();
        IsAbout = step == Step.About;
        IsQuestions = step == Step.Questions;
        IsResult = step == Step.Result;
        IsPrograms = step == Step.Programs;
        IsOptions = !IsWelcome && !IsAbout && !IsQuestions && !IsResult && !IsPrograms;
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
                Title = "Welcome to Gym Book";
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
            case Step.Neck:
                Title = "Train your neck too?";
                Subtitle = "Neck work is optional. It helps in contact sports and for a thicker-looking neck.";
                AddOptions([true, false], neck => neck ? "Yes, add neck exercises" : "No neck exercises", neck => neck
                    ? "A neck exercise on some of your workouts"
                    : "Leave the neck out of the plan", _neck);
                break;
            case Step.BuildWith:
                Title = "How should we build your plan?";
                Subtitle = "AI tailors the plan to you after a few more questions. Signature Programs are proven routines, ready instantly.";
                AddOptions([true, false], useAi => useAi ? "Build with AI" : "Signature Programs", useAi => useAi
                    ? (ai.Quota is { } q ? $"A few more questions, then a plan made for you. {AiPlanService.Describe(q)}" : "A few more questions, then a plan made for you")
                    : "Programs from legendary lifters and coaches, ranked for your answers", _useAi);
                break;
            case Step.Programs:
                Title = "Signature Programs";
                Subtitle = "Proven programs from famous lifters, coaches and communities. Recommended ranks them for your answers; tap one for the details.";
                ShowPrograms();
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
            case bool neck when _steps[_index] == Step.Neck: _neck = neck; break;
            case bool useAi: _useAi = useAi; break;
        }
    }

    void ShowPrograms()
    {
        var ranked = SignaturePrograms.Rank(Answers());
        if (_programsFor != AnswersKey() || SignaturePrograms.Find(_programId) == null)
        {
            // New answers: start on the best fit, in the recommended list.
            _programId = ranked[0].Program.Id;
            _programsFor = AnswersKey();
            _allPrograms = false;
        }
        ProgramTabs.Clear();
        foreach (var all in new[] { false, true })
            ProgramTabs.Add(new ChipItem(all ? "All" : "Recommended", all, SelectProgramTab) { IsSelected = all == _allPrograms });

        Programs.Clear();
        var shown = _allPrograms
            ? ranked.OrderBy(m => m.Program.Name).Select(m => (Match: m, Rank: 0))
            : ranked.Take(RecommendedCount).Select((m, i) => (Match: m, Rank: i + 1));
        foreach (var (match, rank) in shown)
            Programs.Add(new ProgramItem(match, rank, SelectProgram) { IsSelected = match.Program.Id == _programId });
    }

    const int RecommendedCount = 5;

    void SelectProgramTab(ChipItem chip)
    {
        _allPrograms = (bool)chip.Value!;
        ShowPrograms();
    }

    void SelectProgram(ProgramItem item)
    {
        foreach (var p in Programs)
            p.IsSelected = p == item;
        _programId = item.Program.Id;
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
            TrainNeck = _neck,
            BodyWeightKg = units.TryParse(BodyWeight, out var kg) && kg > 0 ? kg : p.BodyWeightKg,
            BirthYear = p.BirthYear,
            TrainingSince = p.TrainingSince,
            CompoundRestSeconds = p.CompoundRestSeconds,
            IsolationRestSeconds = p.IsolationRestSeconds,
        };
    }

    string AnswersKey() => $"{_goal}|{_experience}|{_days}|{_minutes}|{_equipment}|{_neck}";

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
            questions = await ai.QuestionsAsync(Answers(), NewAiToken());
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

    CancellationToken NewAiToken()
    {
        CancelAi();
        _aiCts = new CancellationTokenSource();
        return _aiCts.Token;
    }

    void CancelAi()
    {
        _aiCts?.Cancel();
        _aiCts?.Dispose();
        _aiCts = null;
    }

    /// <summary>Leaves the wizard from any step, after asking, stopping anything the AI is working on.</summary>
    [RelayCommand]
    async Task Cancel()
    {
        var what = _regenerating != null ? "regenerating the plan" : "creating the plan";
        var message = IsGenerating ? "The AI stops, and your answers aren't kept."
            : IsResult && _plan != null ? "This plan hasn't been saved, and your answers aren't kept."
            : "Your answers aren't kept.";
        if (!await dialogs.Confirm($"Stop {what}?", message, "Stop", "Keep going"))
            return;
        _buildRun++;
        CancelAi();
        await GoBack();
    }

    /// <summary>
    /// The plan for the answers: made by AI when chosen (or by the built-in generator if that fails), otherwise from the
    /// signature program picked.
    /// </summary>
    async Task BuildPlanAsync()
    {
        var run = _buildRun;
        var answers = Answers();

        WorkoutPlan plan;
        SignatureProgram? program = null;
        if (AiUsable && _useAi)
        {
            IsGenerating = true;
            GeneratingText = "Picking exercises, sets and rest for you…";
            Title = "Building your plan…";
            Subtitle = "AI is putting together a plan for your answers. This can take up to a minute.";
            try
            {
                plan = await ai.GenerateAsync(answers, _extraAnswers, NewAiToken());
                PlanNote = ai.Quota is { } q ? $"Made by AI for your answers. {AiPlanService.Describe(q)}." : "Made by AI for your answers.";
            }
            catch (Exception e)
            {
                plan = PlanGenerator.Generate(answers);
                var reason = e is Services.Sync.ApiException or Services.Sync.SessionExpiredException ? e.Message : "Couldn't reach the plan generator.";
                PlanNote = $"{reason} Here's a plan built without AI instead.";
            }
            if (run != _buildRun)
                return;
            IsGenerating = false;
        }
        else
        {
            program = SignaturePrograms.Find(_programId) ?? SignaturePrograms.Rank(answers)[0].Program;
            plan = SignaturePrograms.Build(program, answers);
            PlanNote = !ai.IsAvailable ? "Sign in to have AI build a plan around your answers instead."
                : AiUsable ? ""
                : ai.Quota is { } q ? $"{AiPlanService.Describe(q)}." : "";
        }
        ProgramAuthor = program?.Author ?? "";
        ProgramBio = program?.Bio ?? "";

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
        // Past the welcome screen, which holds the account back: a new user makes one.
        if (step == Step.Welcome)
            _ = Views.SignInGate.AskAsync(IPlatformApplication.Current!.Services, register: true);
    }

    /// <summary>On the welcome screen: someone coming back signs in, which can skip the wizard (see <see cref="App.ShowMainShellIfOnboarded"/>).</summary>
    [RelayCommand]
    Task SignIn() => Views.SignInGate.AskAsync(IPlatformApplication.Current!.Services, register: false);

    /// <summary>The welcome screen's "I already have an account", while there's no account on the device.</summary>
    [ObservableProperty] bool showSignIn;

    void UpdateShowSignIn() =>
        ShowSignIn = IsWelcome && !IPlatformApplication.Current!.Services.GetRequiredService<GymBook.Services.Sync.AccountService>().IsSignedIn;

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
        p.TrainNeck = _neck;
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
            // The new exercises take the plan's own rest and target RIR, if it has them; otherwise they're as built.
            if (_regenerating.OwnRest)
                PlanRest.Apply(_regenerating, p, store.GetExercise);
            if (_regenerating is { OwnTraining: true, TargetRir: not null })
                PlanTraining.ApplyRir(_regenerating, p, store.GetExercise);
        }
        else if (_plan != null)
        {
            // A new plan follows the profile's defaults (rest, warm-ups, training) until given its own; a regenerated one
            // keeps its own settings.
            PlanTraining.Apply(_plan, p);
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

/// <summary>A signature program in the wizard's list: who it's from, why it fits, and (when picked) the details.</summary>
public partial class ProgramItem(ProgramMatch match, int rank, Action<ProgramItem> onSelect) : ObservableObject
{
    public SignatureProgram Program => match.Program;
    public string Name => Program.Name;
    public string Author => $"by {Program.Author}";
    public string Tags => Program.Tags;
    public string Why => string.Join(" · ", match.Reasons);
    public string Summary => Program.Summary;
    public string AboutAuthor => $"About {Program.Author}: {Program.Bio}";
    /// <summary>"#1" and on in the recommended list; empty in All.</summary>
    public string Rank => rank > 0 ? $"#{rank}" : "";
    public bool HasRank => rank > 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Background), nameof(StrokeColor))]
    bool isSelected;

    public Color Background => IsSelected ? Color.FromArgb("#1A2A4F") : Color.FromArgb("#151821");
    public Color StrokeColor => IsSelected ? Color.FromArgb("#3F7DFF") : Color.FromArgb("#151821");

    [RelayCommand]
    void Select() => onSelect(this);
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

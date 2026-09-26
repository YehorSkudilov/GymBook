using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>Step-by-step questionnaire used both for first-run onboarding and for generating new plans.</summary>
public partial class PlanWizardViewModel(DataStore store, Units units, DialogService dialogs, IServiceProvider services) : BaseViewModel
{
    enum Step { Welcome, About, Goal, Experience, Days, Duration, Equipment, Result }

    List<Step> _steps = [];
    int _index;
    WorkoutPlan? _plan;

    // Answers
    Goal _goal;
    Experience _experience;
    int _days;
    int _minutes;
    EquipmentAccess _equipment;

    [ObservableProperty] bool isOnboarding;
    [ObservableProperty] string title = "";
    [ObservableProperty] string subtitle = "";
    [ObservableProperty] double progress;
    [ObservableProperty] bool isWelcome;
    [ObservableProperty] bool isAbout;
    [ObservableProperty] bool isOptions;
    [ObservableProperty] bool isResult;
    [ObservableProperty] bool canGoBack;
    [ObservableProperty] string nextText = "Continue";
    [ObservableProperty] string userName = "";
    [ObservableProperty] string bodyWeight = "";
    [ObservableProperty] string planName = "";
    [ObservableProperty] string planDescription = "";
    [ObservableProperty] List<WorkoutPreviewItem> planWorkouts = [];

    public ObservableCollection<OptionItem> Options { get; } = [];
    public ObservableCollection<ChipItem> UnitChips { get; } = [];

    public void Start(bool onboarding)
    {
        IsOnboarding = onboarding;
        var p = store.Profile;
        (_goal, _experience, _days, _minutes, _equipment) = (p.Goal, p.Experience, p.DaysPerWeek, p.SessionMinutes, p.EquipmentAccess);
        UserName = p.Name;
        _steps = onboarding
            ? [Step.Welcome, Step.About, Step.Goal, Step.Experience, Step.Days, Step.Duration, Step.Equipment, Step.Result]
            : [Step.Goal, Step.Experience, Step.Days, Step.Duration, Step.Equipment, Step.Result];

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

    void Show()
    {
        var step = _steps[_index];
        Progress = (_index + 1.0) / _steps.Count;
        CanGoBack = _index > 0;
        IsWelcome = step == Step.Welcome;
        IsAbout = step == Step.About;
        IsResult = step == Step.Result;
        IsOptions = !IsWelcome && !IsAbout && !IsResult;
        NextText = step switch
        {
            Step.Welcome => "Get started",
            Step.Result => IsOnboarding ? "Start training" : "Save plan",
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
                Title = "What's your main goal?";
                Subtitle = "This sets your rep ranges, rest times and exercise choice.";
                AddOptions(Enum.GetValues<Goal>(), g => g.Display(), g => g.Description(), _goal);
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
            case Step.Result:
                BuildPlan();
                Title = "Your plan is ready";
                Subtitle = "You can edit every workout later. Weights and reps adapt automatically as you log.";
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

    void BuildPlan()
    {
        var profile = new UserProfile
        {
            Goal = _goal,
            Experience = _experience,
            DaysPerWeek = _days,
            SessionMinutes = _minutes,
            EquipmentAccess = _equipment,
        };
        _plan = PlanGenerator.Generate(profile);
        PlanName = _plan.Name;
        PlanDescription = _plan.Description;
        PlanWorkouts = _plan.Workouts.Select(w => new WorkoutPreviewItem
        {
            Name = w.Name,
            Meta = $"{w.Exercises.Count} exercises · {w.Exercises.Sum(e => e.Sets)} sets",
            Exercises = string.Join("\n", w.Exercises.Select(e => $"{e.Sets} × {e.RepMin}–{e.RepMax}   {store.GetExercise(e.ExerciseId)?.Name}")),
        }).ToList();
    }

    [RelayCommand]
    Task SignIn() => Views.AccountPage.ShowAsync(services);

    [RelayCommand]
    async Task Next()
    {
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
        _index++;
        Show();
    }

    [RelayCommand]
    async Task Back()
    {
        if (_index == 0)
        {
            if (!IsOnboarding)
                await GoBack();
            return;
        }
        _index--;
        Show();
    }

    async Task Finish()
    {
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
            p.DefaultRestSeconds = _goal == Goal.Strength ? 180 : _goal == Goal.LoseFat ? 60 : 120;
        }

        if (_plan != null)
        {
            store.Data.Plans.Add(_plan);
            store.Data.ActivePlanId = _plan.Id;
        }
        var wasOnboarding = IsOnboarding;
        p.OnboardingDone = true;
        store.Save();

        if (wasOnboarding)
            App.ShowMainShell();
        else
            await Shell.Current.GoToAsync("//plans");
    }
}

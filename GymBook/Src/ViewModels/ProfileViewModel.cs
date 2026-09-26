using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

public partial class ProfileViewModel(DataStore store, Units units, DialogService dialogs) : BaseViewModel
{
    bool _loading;

    [ObservableProperty] string name = "";
    [ObservableProperty] string initial = "";
    [ObservableProperty] string summary = "";
    [ObservableProperty] string unitText = "";
    [ObservableProperty] string bodyWeightText = "";
    [ObservableProperty] string restText = "";
    [ObservableProperty] string goalText = "";
    [ObservableProperty] string experienceText = "";
    [ObservableProperty] string daysText = "";
    [ObservableProperty] string durationText = "";
    [ObservableProperty] string equipmentText = "";
    [ObservableProperty] bool autoRest;
    [ObservableProperty] bool warmups;
    [ObservableProperty] bool trackRir;
    [ObservableProperty] string version = "";

    UserProfile P => store.Profile;

    public override Task OnAppearingAsync()
    {
        Refresh();
        return Task.CompletedTask;
    }

    void Refresh()
    {
        _loading = true;
        Name = string.IsNullOrWhiteSpace(P.Name) ? "Athlete" : P.Name;
        Initial = Name[..1].ToUpperInvariant();
        var count = store.History.Count();
        var since = store.History.LastOrDefault()?.StartedAt;
        Summary = count == 0 ? "No workouts logged yet" : $"{count} workouts since {since:MMM yyyy}";
        UnitText = P.Unit == WeightUnit.Kg ? "Kilograms" : "Pounds";
        BodyWeightText = units.FormatWithUnit(P.BodyWeightKg);
        RestText = Units.Rest(P.DefaultRestSeconds);
        GoalText = P.Goal.Display();
        ExperienceText = P.Experience.Display();
        DaysText = $"{P.DaysPerWeek} days";
        DurationText = $"{P.SessionMinutes} min";
        EquipmentText = P.EquipmentAccess.Display();
        AutoRest = P.AutoRestTimer;
        Warmups = P.WarmupSuggestions;
        TrackRir = P.TrackRir;
        Version = $"GymBook {AppInfo.Current.VersionString}";
        _loading = false;
    }

    partial void OnAutoRestChanged(bool value) => Update(() => P.AutoRestTimer = value);
    partial void OnWarmupsChanged(bool value) => Update(() => P.WarmupSuggestions = value);
    partial void OnTrackRirChanged(bool value) => Update(() => P.TrackRir = value);

    void Update(Action change)
    {
        if (_loading)
            return;
        change();
        store.Save();
    }

    async Task Pick<T>(string title, IEnumerable<T> values, Func<T, string> label, Action<T> apply)
    {
        var list = values.ToList();
        var choice = await dialogs.ActionSheet(title, null, [.. list.Select(label)]);
        var match = list.FirstOrDefault(v => label(v) == choice);
        if (choice == null || match == null)
            return;
        apply(match);
        store.Save();
        Refresh();
    }

    [RelayCommand]
    async Task EditName()
    {
        var value = await dialogs.Prompt("Your name", "What should we call you?", P.Name);
        if (value == null)
            return;
        P.Name = value.Trim();
        store.Save();
        Refresh();
    }

    [RelayCommand]
    Task EditUnit() => Pick("Weight unit", Enum.GetValues<WeightUnit>(), u => u == WeightUnit.Kg ? "Kilograms" : "Pounds", u => P.Unit = u);

    [RelayCommand]
    async Task EditBodyWeight()
    {
        var value = await dialogs.Prompt("Body weight", $"Your weight in {units.Label}", units.Format(P.BodyWeightKg), Keyboard.Numeric);
        if (value == null || !units.TryParse(value, out var kg) || kg <= 0)
            return;
        P.BodyWeightKg = kg;
        store.Data.BodyWeights.RemoveAll(b => b.Date == DateTime.Today);
        store.Data.BodyWeights.Add(new BodyWeightEntry { Date = DateTime.Today, WeightKg = kg });
        store.Save();
        Refresh();
    }

    [RelayCommand]
    Task EditRest() => Pick("Default rest time", [45, 60, 90, 120, 150, 180, 240, 300], s => Units.Rest(s), s => P.DefaultRestSeconds = s);

    [RelayCommand]
    Task EditGoal() => Pick("Goal", Enum.GetValues<Goal>(), g => g.Display(), g => P.Goal = g);

    [RelayCommand]
    Task EditExperience() => Pick("Experience", Enum.GetValues<Experience>(), e => e.Display(), e => P.Experience = e);

    [RelayCommand]
    Task EditDays() => Pick("Training days per week", [2, 3, 4, 5, 6], d => $"{d} days", d => P.DaysPerWeek = d);

    [RelayCommand]
    Task EditDuration() => Pick("Session length", [30, 45, 60, 90], m => $"{m} min", m => P.SessionMinutes = m);

    [RelayCommand]
    Task EditEquipment() => Pick("Equipment", Enum.GetValues<EquipmentAccess>(), e => e.Display(), e => P.EquipmentAccess = e);

    [RelayCommand]
    Task NewPlan() => GoTo(Routes.Wizard);

    [RelayCommand]
    Task OpenHistory() => GoTo(Routes.History);

    [RelayCommand]
    async Task Export()
    {
        store.Save();
        await Share.Default.RequestAsync(new ShareFileRequest("GymBook data", new ShareFile(store.FilePath, "application/json")));
    }

    [RelayCommand]
    async Task Reset()
    {
        if (!await dialogs.Confirm("Reset all data?", "This permanently deletes your plans, workouts and settings.", "Delete everything"))
            return;
        store.Reset();
        Application.Current!.Windows[0].Page = Application.Current.Handler!.MauiContext!.Services
            .GetRequiredService<Views.PlanWizardPage>().ForOnboarding();
    }
}

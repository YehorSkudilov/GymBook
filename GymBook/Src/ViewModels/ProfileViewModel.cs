using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;
using GymBook.Services.Sync;
using GymBook.Views;

namespace GymBook.ViewModels;

public partial class ProfileViewModel(
    DataStore store,
    Units units,
    DialogService dialogs,
    AccountService account,
    SyncService sync,
    IServiceProvider services) : BaseViewModel
{
    bool _loading;

    [ObservableProperty] string name = "";
    [ObservableProperty] string initial = "";
    [ObservableProperty] string summary = "";
    [ObservableProperty] string unitText = "";
    [ObservableProperty] string bodyWeightText = "";
    [ObservableProperty] string compoundRestText = "";
    [ObservableProperty] string isolationRestText = "";
    [ObservableProperty] string warmupRestText = "";
    [ObservableProperty] string goalText = "";
    [ObservableProperty] string experienceText = "";
    [ObservableProperty] string daysText = "";
    [ObservableProperty] string durationText = "";
    [ObservableProperty] string equipmentText = "";
    [ObservableProperty] bool autoRest;
    [ObservableProperty] bool warmups;
    [ObservableProperty] bool trackRir;
    [ObservableProperty] bool trainNeck;
    [ObservableProperty] string ageText = "";
    [ObservableProperty] string bodyFatText = "";
    [ObservableProperty] string trainingSinceText = "";
    [ObservableProperty] string version = "";
    [ObservableProperty] bool isSignedIn;
    [ObservableProperty] string accountEmail = "";
    [ObservableProperty] string syncStatus = "";
    [ObservableProperty] bool hasSyncDetails;
    [ObservableProperty] string syncDetailsSummary = "";

    UserProfile P => store.Profile;

    public override Task OnAppearingAsync()
    {
        sync.StatusChanged += OnSyncChanged;
        store.Changed += OnSyncChanged;
        Refresh();
        return Task.CompletedTask;
    }

    public override void OnDisappearing()
    {
        sync.StatusChanged -= OnSyncChanged;
        store.Changed -= OnSyncChanged;
    }

    void OnSyncChanged(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(Refresh);

    void Refresh()
    {
        IsSignedIn = account.IsSignedIn;
        AccountEmail = account.Email ?? "";
        SyncStatus = sync.LastSyncedAt is { } at && sync.State == SyncState.UpToDate ? $"Synced {at:t}" : sync.Status;
        // While a sync runs, pending changes are about to go up; only a failure or leftovers are worth a look.
        var failed = sync.State is SyncState.Failed or SyncState.Offline or SyncState.SignInRequired;
        var pending = sync.State != SyncState.Syncing && store.Local.HasPendingChanges ? sync.PendingItems().Count : 0;
        HasSyncDetails = account.IsSignedIn && (failed || pending > 0);
        SyncDetailsSummary = failed ? "Sync failed" + (pending > 0 ? $" · {pending} not synced" : "") : $"{pending} not synced yet";

        _loading = true;
        Name = string.IsNullOrWhiteSpace(P.Name) ? "Athlete" : P.Name;
        Initial = Name[..1].ToUpperInvariant();
        var count = store.History.Count();
        var since = store.History.LastOrDefault()?.StartedAt;
        Summary = count == 0 ? "No workouts logged yet" : $"{count} workouts since {since:MMM yyyy}";
        UnitText = P.Unit == WeightUnit.Kg ? "Kilograms" : "Pounds";
        BodyWeightText = units.FormatWithUnit(P.BodyWeightKg);
        AgeText = P.BirthYear is { } year ? $"{DateTime.Now.Year - year}" : "Not set";
        BodyFatText = P.BodyFatPercent is { } bf ? $"{bf:0.#}%" : "Not set";
        TrainingSinceText = P.TrainingSince is { } started ? TrainingAge(started) : "Not set";
        TrainNeck = P.TrainNeck;
        CompoundRestText = RestLabel(P.CompoundRestSeconds);
        IsolationRestText = RestLabel(P.IsolationRestSeconds);
        WarmupRestText = Units.Rest(P.WarmupRestSeconds);
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
    partial void OnTrainNeckChanged(bool value) => Update(() => P.TrainNeck = value);

    // How long the user has trained, bucketed: they pick a range rather than remember a date.
    static readonly (string Label, double Years)[] TrainingAges =
        [("Just starting", 0), ("Under 6 months", 0.25), ("6–12 months", 0.75), ("1–2 years", 1.5), ("2–4 years", 3), ("4+ years", 5)];

    static string TrainingAge(DateTime since)
    {
        var years = (DateTime.Now - since).TotalDays / 365;
        return TrainingAges.LastOrDefault(t => years >= t.Years * 0.9).Label ?? TrainingAges[0].Label;
    }

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

    static readonly int[] RestTimes = [30, 45, 60, 75, 90, 120, 150, 180, 240, 300];

    static string RestLabel(int? seconds) => seconds is { } s ? Units.Rest(s) : "By goal";

    [RelayCommand]
    Task EditCompoundRest() => EditRest("Compound exercises", Mechanic.Compound, s => P.CompoundRestSeconds = s);

    [RelayCommand]
    Task EditIsolationRest() => EditRest("Isolation exercises", Mechanic.Isolation, s => P.IsolationRestSeconds = s);

    [RelayCommand]
    Task EditWarmupRest() => Pick("Rest after warm-up sets", RestTimes, s => Units.Rest(s), s => P.WarmupRestSeconds = s);

    /// <summary>Picks the rest for one kind of exercise, then offers to use it in the plans and the workout in progress.</summary>
    async Task EditRest(string title, Mechanic mechanic, Action<int?> apply)
    {
        List<int?> values = [null, .. RestTimes.Select(s => (int?)s)];
        var labels = values.Select(RestLabel).ToList();
        var index = labels.IndexOf(await dialogs.ActionSheet($"Rest for {title.ToLowerInvariant()}", null, [.. labels]) ?? "");
        if (index < 0)
            return;
        apply(values[index]);

        // Everything already set up with a rest time: plan exercises, and the workout in progress.
        var affected = new List<(Goal Goal, Exercise Exercise, Action<int> Update)>();
        void Add(Goal goal, string exerciseId, Action<int> update)
        {
            if (store.GetExercise(exerciseId) is { } ex && ex.Mechanic == mechanic)
                affected.Add((goal, ex, update));
        }
        foreach (var plan in store.Data.Plans)
            foreach (var pe in plan.Workouts.SelectMany(w => w.Exercises))
                Add(plan.Goal, pe.ExerciseId, s => pe.RestSeconds = s);
        if (store.Data.ActiveSession is { } session)
            foreach (var se in session.Exercises)
                Add(store.GetPlan(session.PlanId)?.Goal ?? P.Goal, se.ExerciseId, s => se.RestSeconds = s);

        if (affected.Count > 0 && await dialogs.Confirm("Update your plans?",
                $"Use this rest for the {title.ToLowerInvariant()} already in your plans and the workout in progress. Otherwise it applies to exercises you add from now on.",
                "Update", "Only new exercises"))
        {
            foreach (var (goal, ex, update) in affected)
                update(TrainingGoals.Prescription(goal, P.Experience, ex, P).RestSeconds);
        }
        store.Save();
        Refresh();
    }

    [RelayCommand]
    async Task EditAge()
    {
        var current = P.BirthYear is { } year ? $"{DateTime.Now.Year - year}" : "";
        var value = await dialogs.Prompt("Age", "Used to estimate starting weights for new exercises", current, Keyboard.Numeric);
        if (value == null)
            return;
        P.BirthYear = int.TryParse(value, out var age) && age is >= 10 and <= 100 ? DateTime.Now.Year - age : null;
        store.Save();
        Refresh();
    }

    [RelayCommand]
    async Task EditBodyFat()
    {
        var value = await dialogs.Prompt("Body fat", "Your body fat percentage, if you know it. Leave empty to skip.",
            P.BodyFatPercent?.ToString("0.#") ?? "", Keyboard.Numeric);
        if (value == null)
            return;
        P.BodyFatPercent = double.TryParse(value.Replace(',', '.'), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var bf)
            && bf is >= 3 and <= 60 ? bf : null;
        store.Save();
        Refresh();
    }

    [RelayCommand]
    Task EditTrainingSince() => Pick("How long have you been training?", TrainingAges, t => t.Label,
        t => P.TrainingSince = DateTime.Today.AddDays(-t.Years * 365));

    [RelayCommand]
    Task EditGoal() => Pick("Goal", TrainingGoals.All, g => g.Display(), g => P.Goal = g);

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
    Task OpenSyncDetails() => GoTo(Routes.SyncDetails);

    /// <summary>The same public page the store listing links to, served by the API.</summary>
    [RelayCommand]
    Task OpenPrivacy() => Browser.Default.OpenAsync(new Uri(ApiConfig.BaseAddress, "privacy"), BrowserLaunchMode.SystemPreferred);

    [RelayCommand]
    Task SignIn() => AccountPage.ShowAsync(services);

    [RelayCommand]
    async Task SyncNow()
    {
        await sync.SyncNowAsync();
        Refresh();
    }

    [RelayCommand]
    async Task SignOut()
    {
        var unsynced = await account.HasUnsyncedChangesAsync();
        var message = unsynced
            ? "Some changes haven't reached your account yet (you seem to be offline). Signing out now deletes them from this device."
            : "Your data stays in your account and will come back when you sign in again. It is removed from this device.";
        if (!await dialogs.Confirm("Sign out?", message, unsynced ? "Sign out anyway" : "Sign out"))
            return;
        await account.SignOutAsync();
        ShowOnboarding();
    }

    [RelayCommand]
    async Task DeleteAccount()
    {
        var password = await dialogs.Prompt("Delete account?",
            "This permanently deletes your account and all synced data. Enter your password to confirm.", accept: "Delete");
        if (string.IsNullOrEmpty(password))
            return;
        try
        {
            await account.DeleteAccountAsync(password);
        }
        catch (Exception e) when (e is ApiException or SessionExpiredException)
        {
            await dialogs.Alert("Couldn't delete account", e.Message);
            return;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            await dialogs.Alert("You're offline", "Connect to the internet to delete your account.");
            return;
        }
        ShowOnboarding();
    }

    static void ShowOnboarding() =>
        Application.Current!.Windows[0].Page = Application.Current.Handler!.MauiContext!.Services
            .GetRequiredService<PlanWizardPage>().ForOnboarding();

    [RelayCommand]
    async Task Export()
    {
        await Share.Default.RequestAsync(new ShareFileRequest("GymBook data", new ShareFile(store.ExportJson(), "application/json")));
    }

    [RelayCommand]
    async Task Reset()
    {
        var message = account.IsSignedIn
            ? "This permanently deletes your plans, workouts and settings from this device and from your account."
            : "This permanently deletes your plans, workouts and settings.";
        if (!await dialogs.Confirm("Reset all data?", message, "Delete everything"))
            return;
        store.Reset();
        ShowOnboarding();
    }
}

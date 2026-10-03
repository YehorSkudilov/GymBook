using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Models;
using GymBook.Services;
using GymBook.Services.Health;
using GymBook.Services.Sync;
using GymBook.Views;

namespace GymBook.ViewModels;

public partial class ProfileViewModel(
    DataStore store,
    Units units,
    DialogService dialogs,
    AccountService account,
    SyncService sync,
    IServiceProvider services,
    HealthSettingsViewModel health,
    HealthSyncService healthSync,
    DataExport export) : BaseViewModel
{
    /// <summary>The Health data section: connecting health apps, which to read from, and so on.</summary>
    public HealthSettingsViewModel Health => health;

    bool _loading;

    [ObservableProperty] string name = "";
    [ObservableProperty] string initial = "";
    [ObservableProperty] string summary = "";
    [ObservableProperty] string unitText = "";
    [ObservableProperty] string bodyWeightText = "";
    [ObservableProperty] string compoundRestText = "";
    [ObservableProperty] string isolationRestText = "";
    [ObservableProperty] string experienceText = "";
    [ObservableProperty] bool trackRir;
    [ObservableProperty] bool defaultDeloads;
    [ObservableProperty] bool defaultPeriodization;
    [ObservableProperty] string ageText = "";
    [ObservableProperty] string bodyFatText = "";
    [ObservableProperty] string trainingSinceText = "";
    [ObservableProperty] string version = "";
    [ObservableProperty] bool isSignedIn;
    [ObservableProperty] string accountEmail = "";
    [ObservableProperty] string passwordRowText = "";
    [ObservableProperty] string syncStatus = "";
    [ObservableProperty] bool hasSyncDetails;
    [ObservableProperty] string syncDetailsSummary = "";

    UserProfile P => store.Profile;

    WarmupSettingsViewModel? _warmupSettings;

    /// <summary>The Warm-ups section: the defaults every plan uses unless it has its own.</summary>
    public WarmupSettingsViewModel WarmupSettings => _warmupSettings ??= new(
        () => Services.WarmupSettings.Global(P),
        Services.WarmupSettings.Defaults,
        s =>
        {
            Services.WarmupSettings.SaveGlobal(P, s);
            store.Save();
        },
        () => !Services.WarmupSettings.Global(P).SameAs(Services.WarmupSettings.Defaults()),
        "Reset to recommended defaults", "the recommended defaults", dialogs, units);

    public override Task OnAppearingAsync()
    {
        sync.StatusChanged += OnSyncChanged;
        store.Changed += OnSyncChanged;
        account.Changed += OnSyncChanged;
        Refresh();
        return Task.CompletedTask;
    }

    public override void OnDisappearing()
    {
        sync.StatusChanged -= OnSyncChanged;
        store.Changed -= OnSyncChanged;
        account.Changed -= OnSyncChanged;
    }

    void OnSyncChanged(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(Refresh);

    void Refresh()
    {
        Health.Refresh();
        IsSignedIn = account.IsSignedIn;
        AccountEmail = account.Email ?? "";
        PasswordRowText = account.HasPassword ? "Change password" : "Set a password";
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
        CompoundRestText = RestLabel(P.CompoundRestSeconds);
        IsolationRestText = RestLabel(P.IsolationRestSeconds);
        ExperienceText = P.Experience.Display();
        WarmupSettings.Refresh();
        TrackRir = P.TrackRir;
        DefaultDeloads = P.DefaultDeloads;
        DefaultPeriodization = P.DefaultPeriodization;
        Version = $"Gym Book {AppInfo.Current.VersionString}";
        _loading = false;
    }

    // The training defaults: plans following them (not those with their own, in their Plan settings) follow along.
    partial void OnTrackRirChanged(bool value) => UpdateTraining(() => P.TrackRir = value);
    partial void OnDefaultDeloadsChanged(bool value) => UpdateTraining(() => P.DefaultDeloads = value);
    partial void OnDefaultPeriodizationChanged(bool value) => UpdateTraining(() => P.DefaultPeriodization = value);

    void UpdateTraining(Action change) => Update(() =>
    {
        change();
        PlanTraining.ApplyToPlans(store);
    });

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

    /// <summary>
    /// Picks the rest for one kind of exercise. Plans without their own rest for it follow along, except exercises
    /// with a rest of their own (see <see cref="PlanRest"/>); the workout in progress is offered it too.
    /// </summary>
    async Task EditRest(string title, Mechanic mechanic, Action<int?> apply)
    {
        List<int?> values = [null, .. RestTimes.Select(s => (int?)s)];
        var labels = values.Select(RestLabel).ToList();
        var index = labels.IndexOf(await dialogs.ActionSheet($"Rest for {title.ToLowerInvariant()}", null, [.. labels]) ?? "");
        if (index < 0)
            return;
        apply(values[index]);

        // The plans inherit it.
        foreach (var plan in store.Data.Plans)
            PlanRest.Apply(plan, P, store.GetExercise);

        // The workout in progress was set up with the old rest: ask.
        var affected = new List<(SessionExercise Exercise, int Seconds)>();
        if (store.Data.ActiveSession is { } session)
        {
            var plan = store.GetPlan(session.PlanId);
            foreach (var se in session.Exercises)
                if (store.GetExercise(se.ExerciseId) is { } ex && ex.Mechanic == mechanic)
                    affected.Add((se, plan == null ? PlanRest.FromProfile(P.Goal, P, ex) : PlanRest.DefaultFor(plan, P, ex)));
        }
        if (affected.Any(a => a.Exercise.RestSeconds != a.Seconds) && await dialogs.Confirm("Use it now too?",
                $"Change the rest for the {title.ToLowerInvariant()} in the workout in progress as well.", "Change", "Not this workout"))
        {
            foreach (var (se, rest) in affected)
                se.RestSeconds = rest;
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
    Task EditExperience() => Pick("Experience", Enum.GetValues<Experience>(), e => e.Display(), e => P.Experience = e);

    [RelayCommand]
    Task OpenSyncDetails() => GoTo(Routes.SyncDetails);

    /// <summary>The exercise library: browse, search and filter every exercise, and make custom ones.</summary>
    [RelayCommand]
    Task OpenExercises() => GoTo(Routes.ExerciseLibrary);

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
    Task ChangeEmail() => ManageAccountPage.ShowAsync(services, AccountChange.Email);

    [RelayCommand]
    Task ChangePassword() => ManageAccountPage.ShowAsync(services, AccountChange.Password);

    [RelayCommand]
    async Task DeleteAccount()
    {
        try
        {
            if (account.HasPassword)
            {
                var password = await dialogs.PasswordPrompt("Delete account?",
                    "This permanently deletes your account and all synced data. Enter your password to confirm.", "Delete");
                if (string.IsNullOrEmpty(password))
                    return;
                await account.DeleteAccountAsync(password);
            }
            else
            {
                // A Google account without a password confirms by picking the Google account again.
                if (!await dialogs.Confirm("Delete account?",
                        "This permanently deletes your account and all synced data. Choose your Google account to confirm.", "Delete"))
                    return;
                if (!await account.DeleteAccountWithGoogleAsync())
                    return;
            }
        }
        catch (Exception e) when (e is ApiException or SessionExpiredException or GoogleSignInException)
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

    static void ShowOnboarding() => App.ShowOnboarding();

    /// <summary>Exports one kind of data as a file to share: workouts and plans as importable CSV, the rest as CSV or JSON.</summary>
    [RelayCommand]
    async Task Export()
    {
        var kinds = Enum.GetValues<ExportKind>();
        var label = await dialogs.ActionSheet("Export", null, [.. kinds.Select(DataExport.Label)]);
        if (label == null)
            return;
        var kind = kinds.First(k => DataExport.Label(k) == label);
        if (export.Write(kind) is not { } file)
        {
            await dialogs.Alert("Nothing to export", "There's nothing of that kind yet.");
            return;
        }
        await Share.Default.RequestAsync(new ShareFileRequest(DataExport.Label(kind), new ShareFile(file.Path, file.ContentType)));
    }

    const string DeleteHealth = "Health data read from other apps", DeleteHistory = "Workout history", DeletePlans = "Plans",
        DeleteEverything = "Everything";

    /// <summary>Deletes one kind of data (health data read in, workout history, plans), or everything.</summary>
    [RelayCommand]
    async Task Reset()
    {
        var where = account.IsSignedIn ? " from this device and your account" : "";
        switch (await dialogs.ActionSheet("Delete data", DeleteEverything, DeleteHealth, DeleteHistory, DeletePlans))
        {
            case DeleteHealth:
                if (!await dialogs.Confirm("Delete health data?",
                        $"Calories burned, steps, and food and body measurements read from Samsung Health or Health Connect are deleted{where}. Food and weights you logged in Gym Book stay."
                        + (healthSync.IsConnected ? " While connected, it's all read again." : ""),
                        "Delete health data"))
                    return;
                store.ResetHealthData();
                healthSync.ReadHistoryAgain();
                await healthSync.SyncAsync(force: true);
                health.Refresh();
                break;
            case DeleteHistory:
                if (!await dialogs.Confirm("Delete workout history?",
                        $"Every finished workout is deleted{where}. Plans stay and start again from their first workout.", "Delete history"))
                    return;
                store.ResetHistory();
                break;
            case DeletePlans:
                if (!await dialogs.Confirm("Delete plans?",
                        $"Every plan is deleted{where}. Workouts already done stay in the history.", "Delete plans"))
                    return;
                store.ResetPlans();
                break;
            case DeleteEverything:
                if (!await dialogs.Confirm("Delete everything?",
                        $"Your plans, workouts, food, health data and settings are permanently deleted{where}.", "Delete everything"))
                    return;
                store.Reset();
                ShowOnboarding();
                break;
        }
    }
}

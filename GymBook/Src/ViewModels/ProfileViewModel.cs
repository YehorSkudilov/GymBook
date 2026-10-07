using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
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
    DataExport export,
    PublicProfileViewModel publicProfile,
    GymBook.Services.Billing.SubscriptionService subscriptions,
    BedtimeService bedtime) : BaseViewModel
{
    /// <summary>Tonight's bedtime and whether its reminders are on, on the Bedtime row.</summary>
    [ObservableProperty] string bedtimeText = "";

    /// <summary>Bedtime's settings, the same sheet as the Workout tab's bedtime card opens.</summary>
    [RelayCommand]
    Task OpenBedtime() => BedtimeSettingsViewModel.Open(bedtime, dialogs);

    /// <summary>The public profile, first on the tab: the username the app calls the user by, picture, bio and friend code.</summary>
    public PublicProfileViewModel Public => publicProfile;

    /// <summary>The Health data section: connecting health apps, which to read from, and so on.</summary>
    public HealthSettingsViewModel Health => health;

    bool _loading;

    [ObservableProperty] string unitText = "";
    [ObservableProperty] string bodyWeightText = "";
    [ObservableProperty] string compoundRestText = "";
    [ObservableProperty] string isolationRestText = "";
    [ObservableProperty] string warmupRestText = "";
    [ObservableProperty] string experienceText = "";
    [ObservableProperty] string sexText = "";
    [ObservableProperty] bool trackRir;
    [ObservableProperty] bool defaultDeloads;
    [ObservableProperty] bool defaultPeriodization;
    [ObservableProperty] string ageText = "";
    [ObservableProperty] string bodyFatText = "";
    [ObservableProperty] string trainingSinceText = "";
    [ObservableProperty] string version = "";
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowProPromo))]
    bool isSignedIn;
    /// <summary>A Gym Book Pro member: the Account section shows their plan rather than the invitation to subscribe.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowProPromo))]
    bool isPro;
    /// <summary>Signed in without Pro: the invitation to subscribe, first in the Account section.</summary>
    public bool ShowProPromo => IsSignedIn && !IsPro;
    [ObservableProperty] string proText = "";
    [ObservableProperty] string passwordRowText = "";
    [ObservableProperty] bool hasSyncDetails;
    [ObservableProperty] string syncDetailsSummary = "";

    UserProfile P => store.Profile;

    public override Task OnAppearingAsync()
    {
        sync.StatusChanged += OnSyncChanged;
        store.Changed += OnSyncChanged;
        account.Changed += OnSyncChanged;
        account.Changed += OnAccountChanged;
        healthSync.HistoryChanged += OnSyncChanged;
        subscriptions.Changed += OnSyncChanged;
        bedtime.Changed += OnSyncChanged;
        Refresh();
        _ = publicProfile.OnAppearingAsync();
        return Task.CompletedTask;
    }

    public override void OnDisappearing()
    {
        sync.StatusChanged -= OnSyncChanged;
        store.Changed -= OnSyncChanged;
        account.Changed -= OnSyncChanged;
        account.Changed -= OnAccountChanged;
        healthSync.HistoryChanged -= OnSyncChanged;
        subscriptions.Changed -= OnSyncChanged;
        bedtime.Changed -= OnSyncChanged;
    }

    // Signed in or out: another account, another username.
    void OnAccountChanged(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(() => _ = publicProfile.OnAppearingAsync());

    void OnSyncChanged(object? sender, EventArgs e) => MainThread.BeginInvokeOnMainThread(Refresh);

    void Refresh()
    {
        Health.Refresh();
        IsSignedIn = account.IsSignedIn;
        IsPro = subscriptions.IsMember;
        publicProfile.RefreshAccount();
        ProText = ProStatusText(subscriptions.Status);
        PasswordRowText = account.HasPassword ? "Change password" : "Set a password";
        // While a sync runs, pending changes are about to go up; only a failure or leftovers are worth a look.
        var failed = sync.State is SyncState.Failed or SyncState.Offline or SyncState.SignInRequired;
        var pending = sync.State != SyncState.Syncing && store.Local.HasPendingChanges ? sync.PendingItems().Count : 0;
        HasSyncDetails = account.IsSignedIn && (failed || pending > 0);
        SyncDetailsSummary = failed ? "Sync failed" + (pending > 0 ? $" · {pending} not synced" : "") : $"{pending} not synced yet";

        _loading = true;
        UnitText = P.Unit == WeightUnit.Kg ? "Kilograms" : "Pounds";
        BedtimeText = $"{BedtimeService.Time(bedtime.Plan(DateTime.Now).Bedtime)}{(bedtime.RemindersOn ? " · reminders on" : "")}";
        BodyWeightText = units.FormatWithUnit(P.BodyWeightKg);
        AgeText = P.BirthYear is { } year ? $"{DateTime.Now.Year - year}" : "Not set";
        BodyFatText = P.BodyFatPercent is { } bf ? $"{bf:0.#}%" : "Not set";
        TrainingSinceText = P.TrainingSince is { } started ? TrainingAge(started) : "Not set";
        CompoundRestText = RestLabel(P.CompoundRestSeconds);
        IsolationRestText = RestLabel(P.IsolationRestSeconds);
        ExperienceText = P.Experience.Display();
        SexText = P.Sex switch { Sex.Male => "Male", Sex.Female => "Female", _ => "Not set" };
        WarmupRestText = Units.Rest(P.WarmupRestSeconds);
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

    /// <summary>The rest after a warm-up set, in plans without rest times of their own.</summary>
    [RelayCommand]
    async Task EditWarmupRest()
    {
        int[] times = [30, 45, 60, 75, 90, 120, 150];
        var labels = times.Select(Units.Rest).ToList();
        var index = labels.IndexOf(await dialogs.ActionSheet("Rest after a warm-up set", null, [.. labels]) ?? "");
        if (index < 0)
            return;
        P.WarmupRestSeconds = times[index];
        store.Save();
        Refresh();
    }

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

    /// <summary>
    /// Starting weights, the AI's plans, calorie goals and the strength ranks go by it. Changed while ranked: the ranks
    /// follow, against the other standards.
    /// </summary>
    [RelayCommand]
    Task EditSex() => Pick("Gender", Enum.GetValues<Sex>(), s => s == Sex.Male ? "Male" : "Female", s =>
    {
        P.Sex = s;
        _ = UpdateRankSexAsync(s);
    });

    async Task UpdateRankSexAsync(Sex sex)
    {
        if (!account.IsSignedIn)
            return;
        var api = services.GetRequiredService<ApiClient>();
        try
        {
            if (await api.GetMyPublicProfileAsync() is { InRanks: true } me && me.Sex != sex)
                await api.JoinRanksAsync(sex);
        }
        catch (Exception e) when (Online.Message(e) != null)
        {
            // Offline: the Ranks tab puts it right when it next loads.
        }
    }

    [RelayCommand]
    Task OpenSyncDetails() => GoTo(Routes.SyncDetails);

    /// <summary>Gym Book Pro: its plans and free trial, or, for a member, managing it.</summary>
    [RelayCommand]
    Task OpenPro() => ProViewModel.ShowAsync();

    /// <summary>A member's plan in a line, under "Gym Book Pro" in the Account section.</summary>
    static string ProStatusText(SubscriptionStatusResponse? s) => s switch
    {
        { Store: null, Granted: true, ExpiresAt: { } until } =>
            "Given to you by Gym Book" + (until >= ProGrant.Forever ? "" : $" · until {until.LocalDateTime:d MMM yyyy}"),
        { ExpiresAt: { } until } =>
            (s.InTrial ? "Free trial" : "Active") + (s.AutoRenewing ? $" · renews {until.LocalDateTime:d MMM}" : $" · ends {until.LocalDateTime:d MMM}"),
        _ => "Active",
    };

    /// <summary>The exercise library: browse, search and filter every exercise, and make custom ones.</summary>
    [RelayCommand]
    Task OpenExercises() => GoTo(Routes.ExerciseLibrary);
    /// <summary>Feature requests (everyone's, to vote for) and the user's bug reports, each a tab of the Feedback page.</summary>
    [RelayCommand]
    Task OpenFeedback() => GoTo(Routes.Feedback);

    /// <summary>Workouts deleted, or discarded while in progress, to bring back.</summary>
    [RelayCommand]
    Task OpenDeleted() => GoTo($"{Routes.PlanWorkouts}?plan=deleted");

    /// <summary>The same public page the store listing links to, served by the API.</summary>
    [RelayCommand]
    Task OpenPrivacy() => Browser.Default.OpenAsync(new Uri(ApiConfig.BaseAddress, "privacy"), BrowserLaunchMode.SystemPreferred);

    [RelayCommand]
    Task SignIn() => AccountPage.ShowAsync(services);

    /// <summary>Syncing before signing out: the Sign out row says so.</summary>
    [ObservableProperty] bool isSyncingToSignOut;

    [RelayCommand]
    async Task SignOut()
    {
        // Last chance to get changes to the account: everything is removed from this device on signing out.
        IsSyncingToSignOut = true;
        bool unsynced;
        try
        {
            unsynced = await account.HasUnsyncedChangesAsync();
        }
        finally
        {
            IsSyncingToSignOut = false;
        }
        if (unsynced)
        {
            var count = sync.PendingItems().Count;
            var reason = sync.State == SyncState.Offline ? "you seem to be offline" : sync.Status is { Length: > 0 } s ? s.TrimEnd('.') : "the sync failed";
            if (!await dialogs.Confirm("Changes will be lost",
                    $"{count} change{(count == 1 ? "" : "s")} couldn't be synced to your account ({reason}). If you sign out now, "
                    + "all unsynced data is permanently deleted from this device. Try again when you're back online to keep it.",
                    "Sign out and lose them", "Keep me signed in"))
                return;
        }
        else if (!await dialogs.Confirm("Sign out?",
                     "Everything is synced. Your data stays in your account and comes back when you sign in again; it is removed from this device.",
                     "Sign out"))
        {
            return;
        }
        await account.SignOutAsync();
        ShowOnboarding();
    }

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
                // Everything, or one plan's workouts (or those linked to none): a list of them, to pick which go.
                var plans = store.Data.Plans.Where(p => store.History.Any(h => h.PlanId == p.Id)).ToList();
                var unlinked = store.History.Any(h => store.GetPlan(h.PlanId) == null);
                if (plans.Count > 0)
                {
                    const string all = "Every workout", none = "Workouts not linked to a plan";
                    // Numbered so plans with the same name stay distinguishable.
                    var labels = plans.Select((p, i) => $"{i + 1}. {p.Name}").ToList();
                    string[] options = unlinked ? [all, .. labels, none] : [all, .. labels];
                    var pick = await dialogs.ActionSheet("Delete workouts of", null, options);
                    if (pick == null)
                        return;
                    if (pick != all)
                    {
                        await GoTo($"{Routes.PlanWorkouts}?plan={(pick == none ? "none" : plans[labels.IndexOf(pick)].Id)}");
                        return;
                    }
                }
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

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Admin.Services;
using GymBook.Contracts;

namespace GymBook.Admin.ViewModels;

/// <summary>
/// One account: who it is, how it trains, its AI use, and what an admin can do about it. The API has the final say on
/// every action (nobody acts on their own account; only a SuperAdmin acts on another admin, changes roles or deletes);
/// the buttons just follow the same rules so they don't offer what would be refused.
/// </summary>
public partial class UserDetailViewModel(AdminApi api, AdminSession session) : BaseViewModel, IQueryAttributable
{
    string _id = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasData), nameof(Title), nameof(Email), nameof(Badges), nameof(AccountRows), nameof(ProfileRows),
        nameof(HasProfile), nameof(StatTiles), nameof(WorkoutsByWeek), nameof(WeeksRange), nameof(Plans), nameof(HasPlans),
        nameof(Workouts), nameof(HasWorkouts), nameof(AiRows), nameof(IsSelf), nameof(CanAct), nameof(ActionsNote), nameof(HasActionsNote),
        nameof(Actions), nameof(Initial), nameof(Summary), nameof(HasBadges))]
    UserDetail? detail;

    public bool HasData => Detail != null;
    UserRow? Row => Detail?.User;

    public string Title => Row == null ? "" : string.IsNullOrWhiteSpace(Row.Name) ? Row.Email : Row.Name;
    public string Email => Row?.Email ?? "";
    public string Initial => Row == null ? "" : Initials.Of(Row.Name, Row.Email);
    public string Summary => Row == null ? "" : $"Joined {Format.Date(Row.CreatedAt)} · active {Format.Ago(Row.LastActiveAt)}";
    public IReadOnlyList<Badge> Badges => Row == null ? [] : Badge.For(Row);
    public bool HasBadges => Badges.Count > 0;

    /// <summary>What this admin may do to this account, as Profile-style rows. Empty when nothing is allowed (see <see cref="ActionsNote"/>).</summary>
    public IReadOnlyList<ActionItem> Actions
    {
        get
        {
            if (Row is not { } user || !CanAct)
                return [];
            var disabled = user.Status == "Disabled";
            var actions = new List<ActionItem>
            {
                disabled
                    ? new("lock_open", Palette.Success, Palette.SuccessSoft, "Enable account", "Lets them sign in again", ToggleDisabledCommand)
                    : new("block", Palette.Danger, Palette.DangerSoft, "Disable account", "Signs them out and keeps them out", ToggleDisabledCommand),
                new("devices", Palette.Accent, Palette.AccentSoft, "Sign out everywhere", $"Ends all {Format.Number(Detail!.ActiveSessions)} sessions", SignOutEverywhereCommand),
                user.EmailVerified
                    ? new("unpublished", Palette.Warning, Palette.WarningSoft, "Mark email unverified", "They'll have to enter a code again", ToggleVerifiedCommand)
                    : new("verified", Palette.Accent, Palette.AccentSoft, "Mark email verified", "Skips the emailed code", ToggleVerifiedCommand),
                new("auto_awesome", Palette.Accent2, Palette.Accent2Soft, "Reset AI quota", "Gives back the AI plans and chats used", ResetAiQuotaCommand),
            };
            if (Detail.HasPassword)
                actions.Add(new("key", Palette.Accent, Palette.AccentSoft, "Send password reset", "Emails a code to choose a new password", SendPasswordResetCommand));
            if (session.IsSuperAdmin)
            {
                actions.Add(new("admin_panel_settings", Palette.Warning, Palette.WarningSoft, "Change role", user.Role ?? "Regular user", ChangeRoleCommand));
                actions.Add(new("delete", Palette.Danger, Palette.DangerSoft, "Delete account", "Removes it and all its data for good", DeleteCommand));
            }
            return actions;
        }
    }

    public IReadOnlyList<InfoRow> AccountRows => Detail is not { } d ? [] :
    [
        new("Joined", Format.DateAndTime(d.User.CreatedAt)),
        new("Last active", Format.Ago(d.User.LastActiveAt)),
        new("Status", d.User.Status),
        new("Email", d.User.EmailVerified ? "Verified" : "Not verified"),
        new("Signs in with", string.Join(" and ", new[] { d.HasPassword ? "password" : null, d.User.HasGoogle ? "Google" : null }.OfType<string>()) is { Length: > 0 } ways ? ways : "nothing (no password or Google)"),
        new("Signed-in devices", Format.Number(d.ActiveSessions)),
        new("Role", d.User.Role ?? "Regular user"),
        new("User ID", d.User.Id),
    ];

    public bool HasProfile => Detail?.Profile != null;

    public IReadOnlyList<InfoRow> ProfileRows => Detail?.Profile is not { } p ? [] :
    [
        new("Name", p.Name),
        new("Goal", Format.Words(p.Goal)),
        new("Experience", Format.Words(p.Experience)),
        new("Schedule", $"{p.DaysPerWeek} days a week, {p.SessionMinutes} min"),
        new("Equipment", Format.Words(p.EquipmentAccess)),
        new("Body weight", Format.Weight(p.BodyWeightKg) + (p.BodyFatPercent is { } fat ? $", {fat:0.#}% fat" : "")),
        new("Born", p.BirthYear?.ToString() ?? "—"),
        new("Training since", p.TrainingSince is { } since ? Format.Date(since) : "—"),
        new("Units", p.Unit),
        new("Onboarding", p.OnboardingDone ? "Done" : "Not finished"),
        new("Updated", Format.DateAndTime(p.UpdatedAt)),
    ];

    public IReadOnlyList<StatTile> StatTiles => Detail?.Stats is not { } s ? [] :
    [
        new("Workouts", Format.Number(s.Workouts), $"{s.Workouts30d} in 30 days"),
        new("Working sets", Format.Number(s.WorkingSets)),
        new("Volume", Format.Volume(s.VolumeKg)),
        new("Time trained", Format.Hours(s.TotalHours)),
        new("First workout", s.FirstWorkoutAt is { } first ? Format.Date(first) : "—"),
        new("Last workout", Format.Ago(s.LastWorkoutAt)),
        new("Custom exercises", Format.Number(s.CustomExercises)),
        new("Body weights", Format.Number(s.BodyWeightEntries), s.LatestBodyWeightKg is { } kg ? $"latest {Format.Weight(kg)}" : ""),
    ];

    public IReadOnlyList<int> WorkoutsByWeek => Detail?.Stats.WorkoutsByWeek.Select(w => w.Count).ToList() ?? [];

    public string WeeksRange => Detail?.Stats.WorkoutsByWeek is { Count: > 0 } weeks ? $"Weeks of {Format.Date(weeks[0].WeekStart)} – {Format.Date(weeks[^1].WeekStart)}" : "";

    public IReadOnlyList<InfoRow> Plans => Detail?.Plans
        .Select(p => new InfoRow(p.IsActive ? $"{p.Name} (active)" : p.Name,
            $"{Format.Words(p.Goal)} · {p.DaysPerWeek}×/week · {p.Days} days, {p.Exercises} exercises · made {Format.Date(p.CreatedAt)}"))
        .ToList() ?? [];

    public bool HasPlans => Plans.Count > 0;

    public IReadOnlyList<InfoRow> Workouts => Detail?.RecentWorkouts
        .Select(w => new InfoRow(Format.Date(w.StartedAt),
            $"{w.Name}{(w.PlanWeek is { } week ? $" (week {week})" : "")} · {Duration(w)}{w.Exercises} exercises, {w.WorkingSets} sets, {Format.Volume(w.VolumeKg)}"))
        .ToList() ?? [];

    public bool HasWorkouts => Workouts.Count > 0;

    public IReadOnlyList<InfoRow> AiRows => Detail?.Ai is not { } ai ? [] :
    [
        new("AI plans", $"{Format.Quota(ai.Plan.Used, ai.Plan.Limit, ai.Plan.Period)} · {ai.Plans30d} in 30 days"),
        new("Plan chat", $"{Format.Quota(ai.Chat.Used, ai.Chat.Limit, ai.Chat.Period)} · {ai.Chats30d} in 30 days"),
    ];

    public bool IsSelf => Row?.Id == session.UserId;

    /// <summary>The everyday actions: not on your own account, and on another admin only as a SuperAdmin.</summary>
    public bool CanAct => Row != null && !IsSelf && (Row.Role == null || session.IsSuperAdmin);

    public string ActionsNote =>
        Row == null ? "" :
        IsSelf ? "This is your account. Admins can't act on their own account." :
        !CanAct ? "Only a SuperAdmin can change another admin's account." :
        "";

    public bool HasActionsNote => ActionsNote.Length > 0;

    [RelayCommand]
    async Task CopyEmail()
    {
        await Clipboard.Default.SetTextAsync(Email);
        await Toast("Email copied");
    }

    [RelayCommand]
    async Task CopyId()
    {
        if (Row == null)
            return;
        await Clipboard.Default.SetTextAsync(Row.Id);
        await Toast("User ID copied");
    }

    /// <summary>Opens the mail app on a new message to the user.</summary>
    [RelayCommand]
    async Task WriteEmail()
    {
        if (Email.Length == 0)
            return;
        try
        {
            await Launcher.Default.OpenAsync($"mailto:{Email}");
        }
        catch (Exception)
        {
            await AlertAsync("No mail app", $"Couldn't open a mail app. The address is {Email}.");
        }
    }

    /// <summary>A short confirmation that disappears by itself.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasToast))]
    string toastText = "";

    public bool HasToast => ToastText.Length > 0;

    async Task Toast(string text)
    {
        ToastText = text;
        await Task.Delay(1600);
        if (ToastText == text)
            ToastText = "";
    }

    static string Duration(WorkoutRow w) =>
        w.EndedAt is { } end && end > w.StartedAt ? $"{(int)(end - w.StartedAt).TotalMinutes} min · " : "";

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("id", out var id))
            _id = Uri.UnescapeDataString(id?.ToString() ?? "");
    }

    public override Task OnAppearingAsync() => Refresh();

    [RelayCommand]
    Task Refresh() => _id.Length == 0 ? Task.CompletedTask : RunAsync(async () => Detail = await api.GetUserAsync(_id));

    [RelayCommand]
    async Task ToggleDisabled()
    {
        if (Row is not { } user)
            return;
        var disable = user.Status != "Disabled";
        if (disable && !await ConfirmAsync("Disable this account?", $"{user.Email} is signed out everywhere and can't sign in until it's enabled again.", "Disable"))
            return;
        await ActAsync(() => api.SetDisabledAsync(user.Id, disable));
    }

    [RelayCommand]
    async Task ToggleVerified()
    {
        if (Row is not { } user)
            return;
        var verify = !user.EmailVerified;
        var message = verify
            ? $"{user.Email} can sync and use AI plans without entering the emailed code."
            : $"{user.Email} has to verify the email again before syncing or using AI plans.";
        if (await ConfirmAsync(verify ? "Mark email verified?" : "Mark email unverified?", message, verify ? "Mark verified" : "Mark unverified"))
            await ActAsync(() => api.SetEmailVerifiedAsync(user.Id, verify));
    }

    [RelayCommand]
    async Task SignOutEverywhere()
    {
        if (Row is { } user && await ConfirmAsync("Sign out everywhere?", $"Every device signed in as {user.Email} is signed out within a few minutes.", "Sign out"))
            await ActAsync(() => api.SignOutEverywhereAsync(user.Id), "Signed out", "All of this account's sessions were ended.");
    }

    [RelayCommand]
    async Task ResetAiQuota()
    {
        if (Row is { } user && await ConfirmAsync("Reset AI quota?", $"{user.Email} gets back the AI plans and chat messages used in the current windows.", "Reset"))
            await ActAsync(() => api.ResetAiQuotaAsync(user.Id));
    }

    [RelayCommand]
    async Task SendPasswordReset()
    {
        if (Row is { } user && await ConfirmAsync("Send a password reset code?", $"{user.Email} gets an email with a code to choose a new password in the app.", "Send"))
            await ActAsync(() => api.SendPasswordResetAsync(user.Id), "Email sent", $"A password reset code went to {user.Email}.");
    }

    [RelayCommand]
    async Task ChangeRole()
    {
        if (Row is not { } user)
            return;
        const string regular = "Regular user";
        var choice = await CurrentPage.DisplayActionSheetAsync($"Role for {user.Email}", "Cancel", null,
            regular, AdminSession.AdminRole, AdminSession.SuperAdminRole);
        if (choice is null or "Cancel")
            return;
        var role = choice == regular ? null : choice;
        if (role == user.Role)
            return;
        var what = role == null ? "a regular user, with no admin access" : role == AdminSession.SuperAdminRole
            ? "a SuperAdmin, who can also change roles and delete accounts"
            : "an Admin, who can see every user and disable accounts";
        if (await ConfirmAsync("Change role?", $"{user.Email} becomes {what}. It takes effect within a few minutes.", "Change"))
            await ActAsync(() => api.SetRoleAsync(user.Id, role));
    }

    [RelayCommand]
    async Task Delete()
    {
        if (Row is not { } user)
            return;
        var typed = await CurrentPage.DisplayPromptAsync("Delete this account?",
            $"This permanently deletes {user.Email} and all of its workouts, plans and settings. It can't be undone.\n\nType the email to confirm.",
            "Delete", "Cancel", user.Email, keyboard: Keyboard.Email);
        if (typed == null)
            return;
        if (!string.Equals(typed.Trim(), user.Email, StringComparison.OrdinalIgnoreCase))
        {
            await AlertAsync("Not deleted", "The email didn't match.");
            return;
        }
        if (await RunAsync(() => api.DeleteUserAsync(user.Id)))
            await Shell.Current.GoToAsync("..");
    }

    /// <summary>Runs an action, then reloads the account so the page shows its effect. Failures show as an alert.</summary>
    async Task ActAsync(Func<Task> action, string? doneTitle = null, string? doneMessage = null)
    {
        if (!await RunAsync(action))
        {
            if (HasError)
                await AlertAsync("Couldn't do that", Error);
            return;
        }
        if (doneTitle != null)
            await AlertAsync(doneTitle, doneMessage ?? "");
        await Refresh();
    }
}

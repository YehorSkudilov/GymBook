using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

public class SharedWorkoutItem
{
    public required string Title { get; init; }
    public required string Exercises { get; init; }
}

/// <summary>
/// Someone else's shared plan before it's in the user's plans (?id=share id): from an invite (&amp;invite=1), to accept
/// or decline, or from a link, to save a copy of.
/// </summary>
public partial class SharedPlanViewModel(ApiClient api, SyncService sync, DialogService dialogs) : BaseViewModel, IQueryAttributable
{
    string? _id;

    [ObservableProperty] bool isLoading = true;
    [ObservableProperty] string? error;
    [ObservableProperty] bool isLoaded;
    [ObservableProperty] string name = "";
    [ObservableProperty] string byline = "";
    [ObservableProperty] string description = "";
    [ObservableProperty] List<SharedWorkoutItem> workouts = [];
    [ObservableProperty] bool isInvite;
    [ObservableProperty] string inviteText = "";
    [ObservableProperty] bool canCopy;
    [ObservableProperty] bool isBusy;

    public bool HasError => Error != null;
    public bool HasDescription => Description.Length > 0;

    partial void OnErrorChanged(string? value) => OnPropertyChanged(nameof(HasError));
    partial void OnDescriptionChanged(string value) => OnPropertyChanged(nameof(HasDescription));

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _id = query.TryGetValue("id", out var id) ? id?.ToString() : null;
        IsInvite = query.TryGetValue("invite", out var invite) && invite?.ToString() == "1";
    }

    public override async Task OnAppearingAsync()
    {
        if (_id == null || IsLoaded)
            return;
        IsLoading = true;
        try
        {
            var plan = await api.ViewSharedPlanAsync(_id);
            Name = plan.Name;
            Byline = plan.Owner is { } owner ? $"By @{owner.Username}" + (owner.Bio.Length > 0 ? $" · {owner.Bio}" : "") : "";
            Description = plan.Description;
            Workouts = [.. plan.Workouts.Select((w, i) => new SharedWorkoutItem
            {
                Title = $"Day {i + 1} · {w.Name}",
                Exercises = string.Join("\n", w.Exercises.Select(e =>
                    $"{e.Name}: {e.Sets} × {(e.RepMin == e.RepMax ? $"{e.RepMin}" : $"{e.RepMin}–{e.RepMax}")}")),
            })];
            InviteText = plan.MyRole == Models.PlanShareRole.Editor
                ? "You're invited to edit this plan: your changes reach everyone in it."
                : "You're invited to follow this plan: its owner keeps it up to date for you.";
            // A member's own copy comes with the share; for anyone else, only if the owner allows copies.
            CanCopy = plan.AllowCopy;
            IsLoaded = true;
            Error = null;
        }
        catch (Exception e) when (Online.Message(e) is { } message)
        {
            Error = message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    async Task Retry()
    {
        IsLoaded = false;
        await OnAppearingAsync();
    }

    [RelayCommand]
    async Task Accept()
    {
        if (_id == null || IsBusy)
            return;
        IsBusy = true;
        try
        {
            if (await Online.Try(dialogs, "Couldn't add the plan", () => api.AcceptPlanInviteAsync(_id!)) != null)
            {
                await sync.SyncNowAsync();
                await dialogs.Alert("Plan added", $"\"{Name}\" is in your plans, and stays in step with everyone sharing it.");
                await GoBack();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    async Task Decline()
    {
        if (_id == null || !await dialogs.Confirm("Decline this plan?", "It won't be added to your plans.", "Decline"))
            return;
        if (await Online.Try(dialogs, "Couldn't decline", () => api.LeavePlanShareAsync(_id!)))
            await GoBack();
    }

    [RelayCommand]
    async Task SaveCopy()
    {
        if (_id == null || IsBusy)
            return;
        IsBusy = true;
        try
        {
            if (await Online.Try(dialogs, "Couldn't save a copy", () => api.CopySharedPlanAsync(_id!)) is { } copy)
            {
                await sync.SyncNowAsync();
                await dialogs.Alert("Copy saved", $"\"{copy.Name}\" is in your plans: yours to change as you like.");
                await GoBack();
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>The share id in a link to a shared plan (…/p/{id}), or the id itself.</summary>
    public static string? ShareIdFrom(string text)
    {
        text = text.Trim();
        if (text.Length == 0)
            return null;
        if (Uri.TryCreate(text, UriKind.Absolute, out var uri))
        {
            var segments = uri.AbsolutePath.Trim('/').Split('/');
            return segments.Length >= 2 && segments[^2] == "p" && segments[^1].Length > 0 ? segments[^1] : null;
        }
        return text.All(char.IsLetterOrDigit) ? text : null;
    }
}

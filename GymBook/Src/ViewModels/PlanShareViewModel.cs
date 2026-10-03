using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Models;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

public class ShareMemberItem
{
    public required string Username { get; init; }
    public required string Initial { get; init; }
    public string? AvatarUrl { get; init; }
    public bool HasAvatar => AvatarUrl != null;
    public required string Detail { get; init; }
    public required IAsyncRelayCommand OpenCommand { get; init; }
}

/// <summary>
/// A plan's sharing (opened from the plan's ··· menu, with ?plan=id). For the owner: a public web link anyone can open
/// without the app, whether people may save copies, and the people it's shared with in the app, each an editor (their
/// changes reach everyone) or a viewer (follows it). For a member: who shares it, saving their own copy, and leaving.
/// </summary>
public partial class PlanShareViewModel(ApiClient api, DataStore store, SyncService sync, DialogService dialogs) : BaseViewModel, IQueryAttributable
{
    string? _planId;
    PlanShareResponse? _share;
    // Set while showing a response, so the switches' changes aren't sent back.
    bool _showing;

    [ObservableProperty] bool isLoading = true;
    [ObservableProperty] bool loadFailed;
    [ObservableProperty] string planName = "";
    /// <summary>The plan isn't shared yet: an invitation to start.</summary>
    [ObservableProperty] bool notShared;
    [ObservableProperty] bool isOwner;
    [ObservableProperty] bool isMember;
    [ObservableProperty] string memberNote = "";
    [ObservableProperty] bool isPublic;
    [ObservableProperty] bool allowCopy = true;
    [ObservableProperty] string? publicUrl;
    [ObservableProperty] List<ShareMemberItem> members = [];
    [ObservableProperty] bool hasMembers;
    [ObservableProperty] bool canCopy;
    [ObservableProperty] bool isBusy;

    public bool HasPublicUrl => PublicUrl != null;

    partial void OnPublicUrlChanged(string? value) => OnPropertyChanged(nameof(HasPublicUrl));

    public void ApplyQueryAttributes(IDictionary<string, object> query) => _planId = query.TryGetValue("plan", out var id) ? id?.ToString() : null;

    public override async Task OnAppearingAsync()
    {
        if (store.GetPlan(_planId) is not { } plan)
        {
            await GoBack();
            return;
        }
        PlanName = plan.Name;
        IsLoading = true;
        LoadFailed = false;
        try
        {
            if (plan.ShareId == null)
            {
                NotShared = true;
                IsOwner = IsMember = false;
            }
            else
                Show(await api.GetPlanShareAsync(plan.ShareId));
        }
        catch (ApiException e) when (e.Status == System.Net.HttpStatusCode.NotFound)
        {
            // The share ended; the next sync takes it off the plan.
            NotShared = true;
            _ = sync.SyncNowAsync();
        }
        catch (Exception e) when (Online.Message(e) != null)
        {
            LoadFailed = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    void Show(PlanShareResponse share)
    {
        _share = share;
        _showing = true;
        NotShared = false;
        IsOwner = share.MyRole == PlanShareRole.Owner;
        IsMember = !IsOwner;
        PlanName = share.PlanName.Length > 0 ? share.PlanName : PlanName;
        IsPublic = share.IsPublic;
        AllowCopy = share.AllowCopy;
        PublicUrl = share.PublicUrl;
        CanCopy = IsOwner || share.AllowCopy;
        MemberNote = share.MyRole switch
        {
            PlanShareRole.Editor => $"@{share.Owner} shares this plan with you to edit. Changes you save reach everyone in it.",
            PlanShareRole.Viewer => $"@{share.Owner} shares this plan with you to follow. They keep it up to date for you.",
            _ => "",
        };
        Members = [.. share.Members.Select(m => new ShareMemberItem
        {
            Username = m.Username,
            Initial = m.Username[..1].ToUpperInvariant(),
            AvatarUrl = ApiClient.AvatarUrl(m.AvatarPath),
            Detail = (m.Role == PlanShareRole.Editor ? "Can edit" : "Can view") + (m.Joined ? "" : " · invited"),
            OpenCommand = new AsyncRelayCommand(() => MemberMenuAsync(m)),
        })];
        HasMembers = Members.Count > 0;
        _showing = false;
    }

    [RelayCommand]
    Task Retry() => OnAppearingAsync();

    [RelayCommand]
    async Task StartSharing()
    {
        if (_planId == null)
            return;
        await RunAsync("Couldn't share the plan", async () =>
        {
            // The server shares its own copy of the plan, so the latest edits go up first.
            await sync.SyncNowAsync();
            var share = await api.SharePlanAsync(_planId);
            // Brings the plan back marked as shared.
            _ = sync.SyncNowAsync();
            return share;
        });
    }

    partial void OnIsPublicChanged(bool value) => _ = SaveSettingsAsync();

    partial void OnAllowCopyChanged(bool value) => _ = SaveSettingsAsync();

    async Task SaveSettingsAsync()
    {
        if (_showing || _share is not { MyRole: PlanShareRole.Owner } share || (share.IsPublic == IsPublic && share.AllowCopy == AllowCopy))
            return;
        await RunAsync("Couldn't change the sharing",
            () => api.UpdatePlanShareAsync(share.Id, new UpdatePlanShareRequest { IsPublic = IsPublic, AllowCopy = AllowCopy }));
        // On failure, the switches go back to what's saved.
        if (_share != null)
            Show(_share);
    }

    [RelayCommand]
    async Task ShareLink()
    {
        if (PublicUrl == null)
            return;
        await Share.Default.RequestAsync(new ShareTextRequest { Title = PlanName, Text = $"{PlanName}, my training plan on Gym Book: {PublicUrl}", Uri = PublicUrl });
    }

    [RelayCommand]
    async Task CopyLink()
    {
        if (PublicUrl == null)
            return;
        await Clipboard.Default.SetTextAsync(PublicUrl);
        await dialogs.Alert("Link copied", "Anyone with the link can see the plan, without the app.");
    }

    [RelayCommand]
    async Task Invite()
    {
        if (_share is not { } share)
            return;
        var username = await dialogs.Prompt("Share with someone", "Their Gym Book username.", null, Keyboard.Plain, "Next");
        if (string.IsNullOrWhiteSpace(username))
            return;
        if (await PickRoleAsync($"What can @{username.Trim().TrimStart('@')} do?") is not { } role)
            return;
        await RunAsync("Couldn't share with them", () => api.InviteToPlanAsync(share.Id, username.Trim(), role));
    }

    async Task<PlanShareRole?> PickRoleAsync(string title)
    {
        const string edit = "Edit: their changes reach everyone", view = "View: follow it as you keep it updated";
        return await dialogs.ActionSheet(title, null, view, edit) switch
        {
            edit => PlanShareRole.Editor,
            view => PlanShareRole.Viewer,
            _ => null,
        };
    }

    async Task MemberMenuAsync(PlanShareMember member)
    {
        if (_share is not { MyRole: PlanShareRole.Owner } share)
            return;
        const string change = "Change what they can do";
        var choice = await dialogs.ActionSheet($"@{member.Username}", "Remove from plan", change);
        if (choice == change && await PickRoleAsync($"What can @{member.Username} do?") is { } role)
            await RunAsync("Couldn't change that", () => api.SetPlanMemberRoleAsync(share.Id, member.Username, role));
        else if (choice == "Remove from plan"
                 && await dialogs.Confirm($"Remove @{member.Username}?", "They keep their copy of the plan as their own, but stop getting your changes.", "Remove"))
            await RunAsync("Couldn't remove them", () => api.RemovePlanMemberAsync(share.Id, member.Username));
    }

    [RelayCommand]
    async Task StopSharing()
    {
        if (_share is not { } share
            || !await dialogs.Confirm("Stop sharing?", "The link stops working, and everyone keeps their copy as their own plan without your changes.", "Stop sharing"))
            return;
        if (await Online.Try(dialogs, "Couldn't stop sharing", () => api.StopSharingPlanAsync(share.Id)))
        {
            await sync.SyncNowAsync();
            await GoBack();
        }
    }

    /// <summary>A copy of the plan in the user's own plans, outside the share.</summary>
    [RelayCommand]
    async Task SaveCopy()
    {
        if (_share is not { } share)
            return;
        if (await Online.Try(dialogs, "Couldn't save a copy", () => api.CopySharedPlanAsync(share.Id)) is { } copy)
        {
            await sync.SyncNowAsync();
            await dialogs.Alert("Copy saved", $"\"{copy.Name}\" is in your plans: yours to change as you like.");
        }
    }

    [RelayCommand]
    async Task Leave()
    {
        if (_share is not { } share
            || !await dialogs.Confirm("Leave this plan?", "You keep it as your own plan, but stop getting changes to it.", "Leave"))
            return;
        if (await Online.Try(dialogs, "Couldn't leave the plan", () => api.LeavePlanShareAsync(share.Id)))
        {
            await sync.SyncNowAsync();
            await GoBack();
        }
    }

    async Task RunAsync(string failTitle, Func<Task<PlanShareResponse>> call)
    {
        if (IsBusy)
            return;
        IsBusy = true;
        try
        {
            if (await Online.Try(dialogs, failTitle, call) is { } share)
                Show(share);
        }
        finally
        {
            IsBusy = false;
        }
    }
}

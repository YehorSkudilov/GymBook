using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Models;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

/// <summary>One of the user's four lifts on the ranks page.</summary>
public class RankLiftItem
{
    public required string Name { get; init; }
    public required bool Ranked { get; init; }
    public required string Tier { get; init; }
    public required Color TierColor { get; init; }
    public required string Detail { get; init; }
    /// <summary>0–1 through the current tier.</summary>
    public required double Progress { get; init; }
}

/// <summary>A row of a leaderboard.</summary>
public class LeaderboardRow
{
    public required string Position { get; init; }
    public required string Username { get; init; }
    public required string Initial { get; init; }
    public string? AvatarUrl { get; init; }
    public bool HasAvatar => AvatarUrl != null;
    public required string Tier { get; init; }
    public required Color TierColor { get; init; }
    public required string Value { get; init; }
    public required bool IsMe { get; init; }
    public Color Background => IsMe ? Color.FromArgb("#1A3F7DFF") : Colors.Transparent;
    public required IAsyncRelayCommand OpenCommand { get; init; }
}

/// <summary>
/// Strength ranks: the user's tier on each of the big four lifts and overall (see <see cref="Ranks"/>), the global and
/// friends leaderboards, and adding friends. Ranks are worked out on the server from synced workouts, so a workout
/// counts once it has synced. Joining needs a public profile and is a choice of its own.
/// </summary>
public partial class RanksViewModel(ApiClient api, DialogService dialogs, Units units, SyncService sync) : BaseViewModel
{
    [ObservableProperty] bool isLoading = true;
    [ObservableProperty] bool loadFailed;
    /// <summary>No public profile yet: ranks need a username.</summary>
    [ObservableProperty] bool needsProfile;
    /// <summary>A profile, but not in the ranks.</summary>
    [ObservableProperty] bool canJoin;
    [ObservableProperty] bool inRanks;
    [ObservableProperty] bool isHidden;

    [ObservableProperty] string overallTier = "";
    [ObservableProperty] Color overallColor = Colors.Gray;
    [ObservableProperty] string overallScore = "";
    [ObservableProperty] string positionText = "";
    [ObservableProperty] double overallProgress;
    [ObservableProperty] List<RankLiftItem> lifts = [];

    [ObservableProperty] List<ChipItem> scopeChips = [];
    [ObservableProperty] List<ChipItem> liftChips = [];
    [ObservableProperty] List<LeaderboardRow> board = [];
    /// <summary>The user's own row when it's below the top of the board: none or one.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMyRow))]
    List<LeaderboardRow> myRows = [];

    public bool HasMyRow => MyRows.Count > 0;
    [ObservableProperty] bool isBoardEmpty;
    [ObservableProperty] bool isBoardLoading;
    [ObservableProperty] string boardEmptyText = "";

    bool _friendsOnly;
    RankLift? _lift;

    public override async Task OnAppearingAsync()
    {
        if (ScopeChips.Count == 0)
            BuildChips();
        // Workouts rank once they're on the server.
        _ = sync.SyncNowAsync();
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        IsLoading = Lifts.Count == 0;
        try
        {
            var profile = await api.GetMyPublicProfileAsync();
            NeedsProfile = !profile.Exists;
            CanJoin = profile.Exists && !profile.InRanks;
            InRanks = profile.InRanks;
            IsHidden = profile.RankHidden;
            LoadFailed = false;
            if (InRanks)
            {
                Show(await api.GetRanksAsync());
                await LoadBoardAsync();
            }
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

    void Show(RankStatusResponse status)
    {
        var tier = Ranks.Tier(status.Score);
        OverallTier = TierName(tier);
        OverallColor = TierColor(tier);
        OverallScore = $"{status.Score:0} points";
        OverallProgress = Progress(status.Score);
        PositionText = IsHidden ? "Hidden from the leaderboards after reports. Contact support if you think that's a mistake."
            : status.Position is { } p ? $"#{p} of {status.Competitors} lifters"
            : "Not on the board yet: log a ranked lift in two workouts.";
        Lifts = [.. status.Lifts.Select(l => new RankLiftItem
        {
            Name = Ranks.Name(l.Lift),
            Ranked = l.Ranked,
            Tier = l.Ranked ? TierName(l.Tier) : "Unranked",
            TierColor = l.Ranked ? TierColor(l.Tier) : Color.FromArgb("#626B7E"),
            Progress = l.Ranked ? Progress(l.Score) : 0,
            Detail = l.Ranked
                ? $"{units.FormatWithUnit(l.E1RmKg)} est. 1RM · {l.Ratio:0.00}× body weight"
                : l.Sessions == 0 ? $"Log it in {Ranks.MinSessions} workouts to rank"
                : $"Log it in {Ranks.MinSessions - l.Sessions} more workout to rank",
        })];
    }

    void BuildChips()
    {
        ScopeChips = [new ChipItem("Global", false, SelectScope) { IsSelected = true }, new ChipItem("Friends", true, SelectScope)];
        LiftChips = [new ChipItem("Overall", null, SelectLift) { IsSelected = true },
            .. Ranks.Lifts.Select(l => new ChipItem(Ranks.Name(l), l, SelectLift))];
    }

    void SelectScope(ChipItem chip)
    {
        foreach (var c in ScopeChips)
            c.IsSelected = c == chip;
        _friendsOnly = (bool)chip.Value!;
        _ = LoadBoardAsync();
    }

    void SelectLift(ChipItem chip)
    {
        foreach (var c in LiftChips)
            c.IsSelected = c == chip;
        _lift = (RankLift?)chip.Value;
        _ = LoadBoardAsync();
    }

    async Task LoadBoardAsync()
    {
        IsBoardLoading = true;
        try
        {
            var response = await api.GetLeaderboardAsync(_friendsOnly, _lift);
            Board = [.. response.Entries.Select(Row)];
            MyRows = response.Me == null ? [] : [Row(response.Me)];
            IsBoardEmpty = Board.Count == 0;
            BoardEmptyText = _friendsOnly ? "No friends on the board yet. Add friends with their friend code or username." : "Nobody's on this board yet.";
        }
        catch (Exception e) when (Online.Message(e) is { } message)
        {
            Board = [];
            MyRows = [];
            IsBoardEmpty = true;
            BoardEmptyText = message;
        }
        finally
        {
            IsBoardLoading = false;
        }
    }

    LeaderboardRow Row(LeaderboardEntry e) => new()
    {
        Position = $"{e.Position}",
        Username = e.Username,
        Initial = e.Username[..1].ToUpperInvariant(),
        AvatarUrl = ApiClient.AvatarUrl(e.AvatarPath),
        Tier = TierName(e.Tier),
        TierColor = TierColor(e.Tier),
        Value = e.E1RmKg is { } kg ? $"{units.FormatWithUnit(kg)} · {e.Ratio:0.00}×" : $"{e.Score:0}",
        IsMe = e.IsMe,
        OpenCommand = new AsyncRelayCommand(() => OpenUserAsync(e.Username, e.IsMe, e.IsFriend)),
    };

    [RelayCommand]
    Task Retry() => LoadAsync();

    [RelayCommand]
    Task SetUpProfile() => GoTo(Routes.PublicProfile);

    [RelayCommand]
    async Task Join()
    {
        const string male = "Men's standards", female = "Women's standards";
        var choice = await dialogs.ActionSheet("Rank against", null, male, female);
        if (choice == null)
            return;
        if (!await dialogs.Confirm("Join the leaderboards?",
                "Your username, picture, tiers and estimated maxes on bench, squat, deadlift and overhead press will be visible to everyone using Gym Book. " +
                "They're worked out from your synced workouts of the last year and your body weight. You can leave at any time.", "Join"))
            return;
        var status = await Online.Try(dialogs, "Couldn't join", () => api.JoinRanksAsync(choice == male ? Sex.Male : Sex.Female));
        if (status != null)
            await LoadAsync();
    }

    [RelayCommand]
    async Task Leave()
    {
        if (!await dialogs.Confirm("Leave the leaderboards?", "You'll disappear from every board. Your workouts stay as they are, and you can join again later.", "Leave"))
            return;
        if (await Online.Try(dialogs, "Couldn't leave", () => api.LeaveRanksAsync()))
        {
            Lifts = [];
            await LoadAsync();
        }
    }

    [RelayCommand]
    async Task HowItWorks() => await dialogs.Alert("How ranks work",
        "Each lift's tier comes from your estimated one-rep max divided by your body weight, against strength standards for your sex: " +
        "Bronze, Silver, Gold, Platinum, Diamond and Elite. Your overall rank averages all four lifts, so an untrained lift counts as zero.\n\n" +
        $"To keep the boards fair, only sets of up to {Ranks.MaxReps} reps count, a lift needs {Ranks.MinSessions} workouts to rank, " +
        "a jump of more than 15% waits for another workout to back it up, and impossible numbers are ignored. " +
        "Variants count too: paused bench, low-bar and pause squats, and sumo deadlifts.\n\n" +
        "Your body weight is your latest weigh-in, or the one in your profile.");

    [RelayCommand]
    async Task AddFriend()
    {
        var code = await dialogs.Prompt("Add a friend", "Their friend code (in their Public profile) or their username.", null, Keyboard.Plain, "Add");
        if (string.IsNullOrWhiteSpace(code))
            return;
        if (await Online.Try(dialogs, "Couldn't add them", () => api.AddFriendAsync(code.Trim())) is { } friend)
        {
            await dialogs.Alert("Friend added", $"@{friend.Username} is your friend now, and you're theirs.");
            await LoadBoardAsync();
        }
    }

    [RelayCommand]
    async Task ShowFriends()
    {
        if (await Online.Try(dialogs, "Couldn't load your friends", () => api.GetFriendsAsync()) is not { } friends)
            return;
        if (friends.Count == 0)
        {
            await dialogs.Alert("No friends yet", "Add friends with their friend code or username.");
            return;
        }
        var pick = await dialogs.ActionSheet("Friends", null, [.. friends.Select(f => "@" + f.Username)]);
        if (pick != null)
            await OpenUserAsync(pick.TrimStart('@'), isMe: false, isFriend: true);
    }

    /// <summary>Someone's profile and lifts, with what can be done about them.</summary>
    async Task OpenUserAsync(string username, bool isMe, bool isFriend)
    {
        if (isMe)
            return;
        if (await Online.Try(dialogs, "Couldn't load their profile", () => api.GetUserAsync(username)) is not { } user)
            return;
        var lines = new List<string>();
        if (user.Bio.Length > 0)
            lines.Add(user.Bio);
        if (user.HomeGym.Length > 0)
            lines.Add($"Trains at {user.HomeGym}");
        if (user.Tier is { } tier)
            lines.Add($"Overall: {TierName(tier)} ({user.Score:0} points)");
        foreach (var l in user.Lifts.Where(l => l.Ranked))
            lines.Add($"{Ranks.Name(l.Lift)}: {TierName(l.Tier)}, {units.FormatWithUnit(l.E1RmKg)} ({units.FormatWithUnit(l.WeightKg)} × {l.Reps})");

        const string add = "Add as friend", remove = "Remove friend", report = "Report";
        var choice = await dialogs.ActionSheet($"@{user.Username}\n\n{string.Join("\n", lines)}", null, user.IsFriend ? remove : add, report);
        switch (choice)
        {
            case add:
                if (await Online.Try(dialogs, "Couldn't add them", () => api.AddFriendAsync(user.Username)) != null)
                    await LoadBoardAsync();
                break;
            case remove:
                if (await dialogs.Confirm($"Remove @{user.Username}?", "You'll leave each other's friends boards.", "Remove")
                    && await Online.Try(dialogs, "Couldn't remove them", () => api.RemoveFriendAsync(user.Username)))
                    await LoadBoardAsync();
                break;
            case report:
                await ReportAsync(user);
                break;
        }
    }

    async Task ReportAsync(ProfileCard user)
    {
        const string profile = "Their username, picture or bio";
        var options = new List<string> { profile };
        options.AddRange(user.Lifts.Where(l => l.Ranked).Select(l => $"Their {Ranks.Name(l.Lift).ToLowerInvariant()} looks fake"));
        var what = await dialogs.ActionSheet($"Report @{user.Username}", null, [.. options]);
        if (what == null)
            return;
        var lift = user.Lifts.Where(l => l.Ranked).Select(l => (RankLift?)l.Lift)
            .FirstOrDefault(l => what.Contains(Ranks.Name(l!.Value).ToLowerInvariant()));
        var reason = await dialogs.Prompt("What's wrong?", "Optional: anything that helps us check.", null, Keyboard.Text, "Report");
        if (reason == null)
            return;
        if (await Online.Try(dialogs, "Couldn't send the report", () => api.ReportAsync(new ReportRequest
            {
                Username = user.Username,
                Lift = what == profile ? null : lift,
                Reason = reason.Length > SocialLimits.ReportReasonLength ? reason[..SocialLimits.ReportReasonLength] : reason,
            })))
            await dialogs.Alert("Thanks", "We'll take a look.");
    }

    public static string TierName(RankTier tier) => tier.ToString();

    public static Color TierColor(RankTier tier) => Color.FromArgb(tier switch
    {
        RankTier.Bronze => "#C0844F",
        RankTier.Silver => "#B8C2D1",
        RankTier.Gold => "#F2C14E",
        RankTier.Platinum => "#4FD1C5",
        RankTier.Diamond => "#7AA8FF",
        _ => "#C77DFF",
    });

    static double Progress(double score) => score >= 600 ? 1 : score % 100 / 100;
}

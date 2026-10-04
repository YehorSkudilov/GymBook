using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Models;
using GymBook.Services;
using GymBook.Services.Sync;
using GymBook.Views;

namespace GymBook.ViewModels;

/// <summary>One of the user's categories on the ranks page (see <see cref="Ranks.Categories"/>).</summary>
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
    /// <summary>A Gym Book Pro member: a ring in Pro's gradient round the picture.</summary>
    public bool IsPro { get; init; }
    public Color Background => IsMe ? Color.FromArgb("#1A3F7DFF") : Colors.Transparent;
    public required IAsyncRelayCommand OpenCommand { get; init; }
}

/// <summary>
/// Ranks: the user's tier in each category (strength by muscle group, and consistency) and overall (see <see cref="Ranks"/>), the global and
/// friends leaderboards, and adding friends. Ranks are worked out on the server from synced workouts, so a workout
/// counts once it has synced. Joining needs the account and a username, and is a choice of its own. The Ranks tab of Progress.
/// </summary>
public partial class RanksViewModel(ApiClient api, DialogService dialogs, Units units, SyncService sync, AccountService account, IServiceProvider services)
    : BaseViewModel
{
    [ObservableProperty] bool isLoading = true;
    [ObservableProperty] bool loadFailed;
    /// <summary>Signed out: ranks need the account.</summary>
    [ObservableProperty] bool needsSignIn;
    /// <summary>Not in the ranks yet.</summary>
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
    RankCategory? _lift;

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
        NeedsSignIn = !account.IsSignedIn;
        if (NeedsSignIn)
        {
            (IsLoading, LoadFailed, CanJoin, InRanks) = (false, false, false, false);
            return;
        }
        IsLoading = Lifts.Count == 0;
        try
        {
            var profile = await api.GetMyPublicProfileAsync();
            CanJoin = !profile.InRanks;
            InRanks = profile.InRanks;
            IsHidden = profile.RankHidden;
            LoadFailed = false;
            if (InRanks)
            {
                // Gender changed in About you while this couldn't reach the server: ranked against the right standards now.
                if (services.GetRequiredService<DataStore>().Profile.Sex is { } sex && profile.Sex != sex)
                    await api.JoinRanksAsync(sex);
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
            : $"Not on the board yet: log a weighted exercise in {Ranks.MinSessions} workouts.";
        Lifts = [.. status.Lifts.Select(l => new RankLiftItem
        {
            Name = Ranks.Name(l.Lift),
            Ranked = l.Ranked,
            Tier = l.Ranked ? TierName(l.Tier) : "Unranked",
            TierColor = l.Ranked ? TierColor(l.Tier) : Color.FromArgb("#626B7E"),
            Progress = l.Ranked ? Progress(l.Score) : 0,
            Detail = Detail(l),
        })];
    }

    /// <summary>A category's line: its best exercise and lift, or for Consistency workouts a week; unranked, how to rank.</summary>
    string Detail(LiftRank l) => (l.Lift, l.Ranked) switch
    {
        (RankCategory.Consistency, true) => $"{l.Ratio:0.#} workouts a week over the last {Ranks.ConsistencyWeeks} weeks",
        (RankCategory.Consistency, false) => $"Train to rank: workouts a week over the last {Ranks.ConsistencyWeeks} weeks",
        (_, true) => $"{l.Exercise}: {units.FormatWithUnit(l.E1RmKg)} est. 1RM · {l.Ratio:0.00}× body weight",
        _ => $"Log a weighted {Ranks.Name(l.Lift).ToLowerInvariant()} exercise in {Ranks.MinSessions} workouts to rank",
    };

    void BuildChips()
    {
        ScopeChips = [new ChipItem("Global", false, SelectScope) { IsSelected = true }, new ChipItem("Friends", true, SelectScope)];
        LiftChips = [new ChipItem("Overall", null, SelectLift) { IsSelected = true },
            .. Ranks.Categories.Select(c => new ChipItem(Ranks.Name(c), c, SelectLift))];
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
        _lift = (RankCategory?)chip.Value;
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
        // On a strength board, the exercise it's from too.
        Tier = e.Exercise is { } exercise ? $"{TierName(e.Tier)} · {exercise}" : TierName(e.Tier),
        TierColor = TierColor(e.Tier),
        // A strength board: the best exercise's lift; Consistency's: workouts a week; overall: points.
        Value = e.E1RmKg is { } kg ? $"{units.FormatWithUnit(kg)} · {e.Ratio:0.00}×"
            : _lift == RankCategory.Consistency && e.Ratio is { } perWeek ? $"{perWeek:0.#} a week"
            : $"{e.Score:0}",
        IsMe = e.IsMe,
        IsPro = e.IsPro,
        OpenCommand = new AsyncRelayCommand(() => OpenUserAsync(e.Username, e.IsMe, e.IsFriend)),
    };

    [RelayCommand]
    Task Retry() => LoadAsync();

    [RelayCommand]
    Task SignIn() => AccountPage.ShowAsync(services);

    [RelayCommand]
    async Task Join()
    {
        // Ranked against the standards for the gender in About you; asked here (and kept there) if it isn't set yet.
        var store = services.GetRequiredService<DataStore>();
        if (store.Profile.Sex == null)
        {
            const string male = "Male", female = "Female";
            var choice = await dialogs.ActionSheet("Your gender", null, male, female);
            if (choice == null)
                return;
            store.Profile.Sex = choice == male ? Sex.Male : Sex.Female;
            store.Save();
        }
        var sex = store.Profile.Sex.Value;
        if (!await dialogs.Confirm("Join the leaderboards?",
                "Your username, picture, strength tiers with your best lift in each, how often you train, and your food logging streak and days on target will be visible to everyone using Gym Book. " +
                "They're worked out from your synced workouts of the last year and your body weight, against " +
                $"{(sex == Sex.Male ? "men's" : "women's")} standards (your gender in Profile). You can leave at any time.", "Join"))
            return;
        var status = await Online.Try(dialogs, "Couldn't join", () => api.JoinRanksAsync(sex));
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
        "You're ranked in seven categories, each Bronze, Silver, Gold, Platinum, Diamond or Elite:\n\n" +
        "Strength: Chest, Back, Shoulders, Legs, Posterior chain (hamstrings, glutes, lower back) and Arms. Each takes your best " +
        "weighted exercise for it, any of them, yours included: its estimated one-rep max divided by your body weight, against a " +
        "standard for that exercise and your gender (set in Profile, under About you). Bench, squat, deadlift and overhead press " +
        "have their own, well-known standards.\n\n" +
        $"Consistency: how many workouts a week you've done over the last {Ranks.ConsistencyWeeks} weeks, from 1 for Silver to 5 for Elite.\n\n" +
        "Your overall rank averages all seven, so a category you don't train counts as zero.\n\n" +
        $"To keep the boards fair, only sets of up to {Ranks.MaxReps} reps count, an exercise needs {Ranks.MinSessions} workouts to rank, " +
        "a jump of more than 15% waits for another workout to back it up, and impossible numbers are ignored. Abs, calves, neck " +
        "and bodyweight-only exercises don't count.\n\n" +
        "Your body weight is your latest weigh-in, or the one in your profile.");

    [RelayCommand]
    async Task AddFriend()
    {
        var code = await dialogs.Prompt("Add a friend", "Their friend code (at the top of their Profile tab) or their username.", null, Keyboard.Plain, "Add");
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
    public async Task OpenUserAsync(string username, bool isMe, bool isFriend)
    {
        if (isMe)
            return;
        if (await Online.Try(dialogs, "Couldn't load their profile", () => api.GetUserAsync(username)) is not { } user)
            return;
        var lines = new List<string>();
        if (user.Bio.Length > 0)
            lines.Add(user.Bio);
        if (user.Tier is { } tier)
            lines.Add($"Overall: {TierName(tier)} ({user.Score:0} points)");
        foreach (var l in user.Lifts.Where(l => l.Ranked))
            lines.Add(l.Lift == RankCategory.Consistency
                ? $"Consistency: {TierName(l.Tier)}, {l.Ratio:0.#} workouts a week"
                : $"{Ranks.Name(l.Lift)}: {TierName(l.Tier)}, {l.Exercise} {units.FormatWithUnit(l.E1RmKg)} ({units.FormatWithUnit(l.WeightKg)} × {l.Reps})");

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
        options.AddRange(user.Lifts.Where(l => l.Ranked && l.Lift != RankCategory.Consistency).Select(l => $"Their {Ranks.Name(l.Lift).ToLowerInvariant()} looks fake"));
        var what = await dialogs.ActionSheet($"Report @{user.Username}", null, [.. options]);
        if (what == null)
            return;
        var lift = user.Lifts.Where(l => l.Ranked && l.Lift != RankCategory.Consistency).Select(l => (RankCategory?)l.Lift)
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

    /// <summary>0–1 through a score's tier.</summary>
    public static double Progress(double score) => score >= 600 ? 1 : score % 100 / 100;
}

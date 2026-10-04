using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Models;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

/// <summary>
/// Nutrition's Ranks tab: two boards, days in a row with food logged and days on target in the last 30, global or among
/// friends. The same opt-in as the strength ranks (joining and opening someone go through <see cref="RanksViewModel"/>);
/// the app sends the user's own numbers whenever the Nutrition tab shows (see NutritionViewModel).
/// </summary>
public partial class NutritionRanksViewModel(ApiClient api, AccountService account, RanksViewModel ranks, IServiceProvider services) : BaseViewModel
{
    [ObservableProperty] bool isLoading = true;
    [ObservableProperty] bool needsSignIn;
    [ObservableProperty] bool canJoin;
    [ObservableProperty] bool inRanks;
    [ObservableProperty] bool loadFailed;

    [ObservableProperty] List<ChipItem> scopeChips = [];
    [ObservableProperty] List<ChipItem> boardChips = [];
    [ObservableProperty] List<LeaderboardRow> board = [];
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMyRow))]
    List<LeaderboardRow> myRows = [];
    public bool HasMyRow => MyRows.Count > 0;
    [ObservableProperty] bool isBoardEmpty;
    [ObservableProperty] bool isBoardLoading;
    [ObservableProperty] string boardEmptyText = "";
    [ObservableProperty] string boardCaption = "";

    bool _friendsOnly;
    NutritionBoard _board = NutritionBoard.Streak;

    // ---- Your numbers: what the boards rank the user on, and why it's that (like the strength categories in Progress) ----

    [ObservableProperty] bool hasMine;
    /// <summary>The nutrition rank: the streak's and days on target's scores averaged (see <see cref="Ranks.NutritionScore"/>).</summary>
    [ObservableProperty] string overallTier = "";
    [ObservableProperty] Color overallColor = Colors.Gray;
    [ObservableProperty] string overallScore = "";
    [ObservableProperty] double overallProgress;
    [ObservableProperty] string streakTier = "";
    [ObservableProperty] Color streakColor = Colors.Gray;
    [ObservableProperty] string onTargetTier = "";
    [ObservableProperty] Color onTargetColor = Colors.Gray;
    [ObservableProperty] string streakText = "";
    [ObservableProperty] string streakDetail = "";
    [ObservableProperty] double streakProgress;
    [ObservableProperty] string onTargetText = "";
    [ObservableProperty] string onTargetDetail = "";
    [ObservableProperty] double onTargetProgress;
    /// <summary>Where the user is on the board showing, or why they aren't on it.</summary>
    [ObservableProperty] string placeText = "";

    NutritionStreaks? _mine;

    /// <summary>
    /// The user's own numbers, as the app works them out (and sends to the boards), each with what it counts; from the
    /// Nutrition tab, which has them.
    /// </summary>
    public void ShowMine(NutritionStreaks s, int? calorieGoal, int? proteinGoal)
    {
        _mine = s;
        HasMine = true;
        var overall = Ranks.NutritionScore(s.Logging, s.OnTargetLast30);
        var tier = Ranks.Tier(overall);
        OverallTier = RanksViewModel.TierName(tier);
        OverallColor = RanksViewModel.TierColor(tier);
        OverallScore = $"{overall:0} points";
        OverallProgress = RanksViewModel.Progress(overall);

        var streak = Ranks.NutritionStreakScore(s.Logging);
        StreakTier = RanksViewModel.TierName(Ranks.Tier(streak));
        StreakColor = RanksViewModel.TierColor(Ranks.Tier(streak));
        StreakText = $"{Days(s.Logging)} in a row";
        // How far through the streak's tier
        StreakProgress = RanksViewModel.Progress(streak);
        StreakDetail = (s.Logging == 0 ? $"No streak right now: log any food today to start one. Your best is {Days(s.BestLogging)}."
            : !s.TodayLogged ? $"Counted to yesterday: log something today to keep it going. Your best is {Days(s.BestLogging)}."
            : s.Logging >= s.BestLogging ? "Your longest streak yet. Every day you log any food adds one."
            : $"Your best is {Days(s.BestLogging)}. Every day you log any food adds one.")
            + Next(Ranks.NutritionStreakDays, s.Logging, "in a row");

        var onTarget = Ranks.OnTargetScore(s.OnTargetLast30);
        OnTargetTier = RanksViewModel.TierName(Ranks.Tier(onTarget));
        OnTargetColor = RanksViewModel.TierColor(Ranks.Tier(onTarget));
        OnTargetText = $"{s.OnTargetLast30} of the last 30 days";
        OnTargetProgress = RanksViewModel.Progress(onTarget);
        OnTargetDetail = calorieGoal is not > 0
            ? "Set a calorie goal (Goals, at the top of Nutrition) to count days on target."
            : $"A day counts when its calories land within {NutritionService.TargetRange:P0} of your {calorieGoal} kcal goal"
              + (proteinGoal is > 0 ? $" and protein reaches {NutritionService.ProteinReached:P0} of your {proteinGoal} g goal." : ".")
              + Next(Ranks.OnTargetDays, s.OnTargetLast30, "in the last 30");
        ShowPlace();
    }

    /// <summary>" 3 days in a row for Gold.", or nothing at Elite.</summary>
    static string Next(int[] thresholds, int value, string what) =>
        Ranks.NextTier(thresholds, value) is { } next ? $" {Days(next.From)} {what} for {RanksViewModel.TierName(next.Tier)}." : "";

    /// <summary>The user's place on the board showing: their row on it, or why they're not there.</summary>
    void ShowPlace()
    {
        var mine = Board.FirstOrDefault(r => r.IsMe) ?? MyRows.FirstOrDefault();
        var board = _board == NutritionBoard.Streak ? "the streak board" : "the on-target board";
        PlaceText = mine != null ? $"#{mine.Position} on {board}{(_friendsOnly ? " among friends" : "")}."
            : _mine == null || IsBoardLoading ? ""
            : _board == NutritionBoard.Streak && _mine.Logging == 0 ? $"Not on {board}: it shows streaks still going."
            : _board == NutritionBoard.OnTarget && _mine.OnTargetLast30 == 0 ? $"Not on {board}: no days on target in the last 30."
            : $"Not on {board} yet: it updates when you open Nutrition.";
    }

    public override async Task OnAppearingAsync()
    {
        if (ScopeChips.Count == 0)
        {
            ScopeChips = [new ChipItem("Global", false, SelectScope) { IsSelected = true }, new ChipItem("Friends", true, SelectScope)];
            BoardChips = [new ChipItem("Streak", NutritionBoard.Streak, SelectBoard) { IsSelected = true },
                new ChipItem("On target", NutritionBoard.OnTarget, SelectBoard)];
        }
        await LoadAsync();
    }

    async Task LoadAsync()
    {
        NeedsSignIn = !account.IsSignedIn;
        if (NeedsSignIn)
        {
            (IsLoading, CanJoin, InRanks, LoadFailed) = (false, false, false, false);
            return;
        }
        IsLoading = Board.Count == 0;
        try
        {
            var profile = await api.GetMyPublicProfileAsync();
            InRanks = profile.InRanks;
            CanJoin = !profile.InRanks;
            LoadFailed = false;
            if (InRanks)
                await LoadBoardAsync();
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

    void SelectScope(ChipItem chip)
    {
        foreach (var c in ScopeChips)
            c.IsSelected = c == chip;
        _friendsOnly = (bool)chip.Value!;
        _ = LoadBoardAsync();
    }

    void SelectBoard(ChipItem chip)
    {
        foreach (var c in BoardChips)
            c.IsSelected = c == chip;
        _board = (NutritionBoard)chip.Value!;
        _ = LoadBoardAsync();
    }

    async Task LoadBoardAsync()
    {
        IsBoardLoading = true;
        BoardCaption = _board == NutritionBoard.Streak
            ? "Days in a row with food logged."
            : "Days in the last 30 with calories in your target range and protein near its goal.";
        try
        {
            var response = await api.GetNutritionLeaderboardAsync(_friendsOnly, _board);
            Board = [.. response.Entries.Select(Row)];
            MyRows = response.Me == null ? [] : [Row(response.Me)];
            IsBoardEmpty = Board.Count == 0;
            BoardEmptyText = _friendsOnly ? "No friends on this board yet. Add friends from Progress, under Ranks."
                : "Nobody's on this board yet. Log your food to be the first.";
        }
        catch (Exception e)
        {
            // Offline and the like say so; anything else still shows that something went wrong, not an empty board.
            Board = [];
            MyRows = [];
            IsBoardEmpty = true;
            BoardEmptyText = Online.Message(e) ?? "Couldn't load this board. Try again later.";
        }
        finally
        {
            IsBoardLoading = false;
            ShowPlace();
        }
    }

    LeaderboardRow Row(NutritionLeaderboardEntry e) => new()
    {
        Position = $"{e.Position}",
        Username = e.Username,
        Initial = e.Username[..1].ToUpperInvariant(),
        AvatarUrl = ApiClient.AvatarUrl(e.AvatarPath),
        // Under the name: the tier on this board, and on the streak board the best streak.
        Tier = RanksViewModel.TierName(BoardTier(e.Days))
               + (_board == NutritionBoard.Streak && e.BestStreak > e.Days ? $" · Best {Days(e.BestStreak)}" : ""),
        TierColor = RanksViewModel.TierColor(BoardTier(e.Days)),
        Value = Days(e.Days),
        IsMe = e.IsMe,
        IsPro = e.IsPro,
        OpenCommand = new AsyncRelayCommand(() => ranks.OpenUserAsync(e.Username, e.IsMe, e.IsFriend)),
    };

    RankTier BoardTier(int days) =>
        Ranks.Tier(_board == NutritionBoard.Streak ? Ranks.NutritionStreakScore(days) : Ranks.OnTargetScore(days));

    static string Days(int n) => n == 1 ? "1 day" : $"{n} days";

    [RelayCommand]
    Task Retry() => LoadAsync();

    [RelayCommand]
    Task SignIn() => Views.AccountPage.ShowAsync(services);

    /// <summary>The same opt-in as the strength ranks: joining puts the user on both.</summary>
    [RelayCommand]
    async Task Join()
    {
        await ranks.JoinCommand.ExecuteAsync(null);
        await LoadAsync();
    }
}

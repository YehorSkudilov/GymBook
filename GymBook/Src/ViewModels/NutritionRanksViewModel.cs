using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
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
        }
    }

    LeaderboardRow Row(NutritionLeaderboardEntry e) => new()
    {
        Position = $"{e.Position}",
        Username = e.Username,
        Initial = e.Username[..1].ToUpperInvariant(),
        AvatarUrl = ApiClient.AvatarUrl(e.AvatarPath),
        // Under the name: on the streak board, the best streak.
        Tier = _board == NutritionBoard.Streak && e.BestStreak > e.Days ? $"Best {Days(e.BestStreak)}" : "",
        TierColor = Color.FromArgb("#9AA3B5"),
        Value = Days(e.Days),
        IsMe = e.IsMe,
        IsPro = e.IsPro,
        OpenCommand = new AsyncRelayCommand(() => ranks.OpenUserAsync(e.Username, e.IsMe, e.IsFriend)),
    };

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

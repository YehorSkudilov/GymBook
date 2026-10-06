using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

/// <summary>A ticket in the list: its votes (feature requests), title, and where it is.</summary>
public partial class FeedbackItem(FeedbackTicket ticket, Func<FeedbackItem, Task> vote, Func<FeedbackItem, Task> open) : ObservableObject
{
    public FeedbackTicket Ticket { get; } = ticket;
    public string Title => Ticket.Title;
    public bool IsFeature => Ticket.Kind == FeedbackKind.Feature;
    public bool IsMine => Ticket.IsMine;
    public string Status => Ticket.Status;
    public Color StatusColor => Ticket.IsClosed ? Color.FromArgb("#9AA3B5") : Color.FromArgb("#7FA6FF");
    public Color StatusBackground => Ticket.IsClosed ? Color.FromArgb("#1D212C") : Color.FromArgb("#2E3F7FFF");

    public string Meta
    {
        get
        {
            var parts = new List<string>();
            if (IsFeature)
                parts.Add(Ticket.IsMine ? "by you" : Ticket.IsTeam || Ticket.Author.Contains(' ') ? $"by {Ticket.Author}" : $"by @{Ticket.Author}");
            parts.Add(Ticket.CreatedAt.ToLocalTime().ToString("d MMM yyyy"));
            if (Ticket.CommentCount > 0)
                parts.Add(Ticket.CommentCount == 1 ? "1 comment" : $"{Ticket.CommentCount} comments");
            return string.Join(" · ", parts);
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VoteBackground), nameof(VoteColor))]
    bool voted = ticket.Voted;
    [ObservableProperty] int votes = ticket.Votes;
    public Color VoteBackground => Voted ? Color.FromArgb("#3F7DFF") : Color.FromArgb("#1D212C");
    public Color VoteColor => Voted ? Colors.White : Color.FromArgb("#9AA3B5");

    [RelayCommand] Task Vote() => vote(this);
    [RelayCommand] Task Open() => open(this);
}

/// <summary>
/// Feature requests (everyone's: vote for them, or suggest one) and the user's own bug reports, from Profile. Both are
/// kept on GitHub by the API (see the API's FeedbackService), so this needs a connection.
/// </summary>
public partial class FeedbackViewModel(ApiClient api, DialogService dialogs) : BaseViewModel, IQueryAttributable
{
    FeedbackKind _kind = FeedbackKind.Feature;
    List<FeedbackTicket> _tickets = [];
    bool _loaded;

    public ObservableCollection<ChipItem> Tabs { get; } = [];
    public ObservableCollection<FeedbackItem> Items { get; } = [];
    [ObservableProperty] string searchPlaceholder = "Search requests";

    [ObservableProperty] bool isLoading;
    [ObservableProperty] bool isRefreshing;
    [ObservableProperty] string? error;
    [ObservableProperty] string search = "";
    [ObservableProperty] bool isFeatures = true;
    [ObservableProperty] string emptyText = "";
    [ObservableProperty] string newText = "";
    [ObservableProperty] string intro = "";

    public bool HasError => Error != null;
    partial void OnErrorChanged(string? value) => OnPropertyChanged(nameof(HasError));
    partial void OnSearchChanged(string value) => Show();

    /// <summary>?kind=Bug opens on the bug reports.</summary>
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("kind", out var kind) && Enum.TryParse<FeedbackKind>(kind?.ToString(), out var k))
            _kind = k;
    }

    public override async Task OnAppearingAsync()
    {
        if (Tabs.Count == 0)
        {
            Tabs.Add(new ChipItem("Feature requests", FeedbackKind.Feature, SelectTab) { Icon = "lightbulb", IsSelected = _kind == FeedbackKind.Feature });
            Tabs.Add(new ChipItem("My bug reports", FeedbackKind.Bug, SelectTab) { Icon = "bug_report", IsSelected = _kind == FeedbackKind.Bug });
            Describe();
        }
        // Back from a ticket (voted, commented, changed or deleted it): as it is now.
        await LoadAsync(quiet: _loaded);
    }

    void SelectTab(ChipItem chip)
    {
        foreach (var c in Tabs)
            c.IsSelected = c == chip;
        _kind = (FeedbackKind)chip.Value!;
        // Each tab starts unfiltered.
        _filter = FeedbackFilter.Default(chip.Value is FeedbackKind.Feature);
        _tickets = [];
        _statuses = [];
        Search = "";
        HasFilters = false;
        Describe();
        Items.Clear();
        _ = LoadAsync(quiet: false);
    }

    void Describe()
    {
        IsFeatures = _kind == FeedbackKind.Feature;
        Intro = IsFeatures
            ? "Ideas for Gym Book, from everyone using it. Vote for the ones you want most: the most wanted come first."
            : "Bugs you've reported. Only you and the Gym Book team can see them.";
        NewText = IsFeatures ? "Suggest a feature" : "Report a bug";
        SearchPlaceholder = IsFeatures ? "Search requests" : "Search your reports";
        EmptyText = IsFeatures ? "No feature requests yet. Be the first to suggest one." : "You haven't reported any bugs.";
    }

    async Task LoadAsync(bool quiet)
    {
        IsLoading = !quiet && !IsRefreshing;
        try
        {
            var kind = _kind;
            var list = await api.GetFeedbackAsync(kind);
            if (kind != _kind)
                return;
            (_tickets, _statuses) = (list.Tickets, list.Statuses);
            _loaded = true;
            Error = null;
            BuildFilters();
            Show();
        }
        catch (Exception e) when (Online.Message(e) is { } message)
        {
            if (!quiet || Items.Count == 0)
                Error = message;
        }
        finally
        {
            IsLoading = false;
            IsRefreshing = false;
        }
    }

    [RelayCommand]
    Task Refresh() => LoadAsync(quiet: true);

    [RelayCommand]
    async Task Retry()
    {
        Error = null;
        await LoadAsync(quiet: false);
    }

    // ---- Filters and order ----

    FeedbackFilter _filter = FeedbackFilter.Default(features: true);
    // The project board's Status columns, in its order.
    List<string> _statuses = [];

    /// <summary>The filter button, lit while the list is filtered or in another order.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FilterIconColor), nameof(FilterBackground))]
    bool hasFilters;
    public Color FilterIconColor => HasFilters ? Colors.White : Color.FromArgb("#9AA3B5");
    public Color FilterBackground => HasFilters ? Color.FromArgb("#3F7DFF") : Color.FromArgb("#1D212C");

    /// <summary>
    /// The statuses to filter by: the board's Status columns in its order, then Open and Closed when there are tickets
    /// off the board.
    /// </summary>
    List<string> FilterStatuses() =>
        [.. _statuses.Concat(new[] { "Open", "Closed" }.Where(s => !_statuses.Contains(s) && _tickets.Any(t => t.Status == s)))];

    /// <summary>After loading: statuses that are gone are dropped from the filter.</summary>
    void BuildFilters()
    {
        var known = FilterStatuses();
        if (_filter.Statuses.Any(s => !known.Contains(s)))
            _filter = _filter with { Statuses = _filter.Statuses.Where(known.Contains).ToHashSet() };
        HasFilters = !_filter.IsDefault(IsFeatures);
    }

    /// <summary>The filter button: the filter sheet, each change applied as it's made.</summary>
    [RelayCommand]
    Task OpenFilters() =>
        Shell.Current.Navigation.PushModalAsync(new Views.FeedbackFilterPage(new FeedbackFilterViewModel(FilterStatuses(), _filter, IsFeatures, filter =>
        {
            _filter = filter;
            HasFilters = !_filter.IsDefault(IsFeatures);
            Show();
        })), false);

    bool IsFiltered => _filter.Statuses.Count > 0 || _filter.Mine || Search.Trim().Length > 0;

    /// <summary>Filtered by status, Mine and the search, in the order picked (newest first among equals).</summary>
    void Show()
    {
        var words = Search.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var shown = _tickets.Where(t => words.All(w => t.Title.Contains(w, StringComparison.OrdinalIgnoreCase))
            && (_filter.Statuses.Count == 0 || _filter.Statuses.Contains(t.Status))
            && (!_filter.Mine || t.IsMine));
        Func<FeedbackTicket, IComparable> key = _filter.Sort switch
        {
            FeedbackSort.Votes => t => t.Votes,
            FeedbackSort.Updated => t => t.UpdatedAt,
            FeedbackSort.Comments => t => t.CommentCount,
            _ => t => t.CreatedAt,
        };
        shown = (_filter.Descending ? shown.OrderByDescending(key) : shown.OrderBy(key)).ThenByDescending(t => t.CreatedAt);
        Items.Clear();
        foreach (var t in shown)
            Items.Add(new FeedbackItem(t, VoteAsync, OpenAsync));
        EmptyText = IsFiltered && _tickets.Count > 0 ? "Nothing matches these filters."
            : IsFeatures ? "No feature requests yet. Be the first to suggest one." : "You haven't reported any bugs.";
    }

    async Task VoteAsync(FeedbackItem item)
    {
        if (item.Ticket.IsClosed && !item.Voted)
        {
            await dialogs.Alert("This request is closed", "Closed requests can't be voted for.");
            return;
        }
        // Straight away; put back if it didn't go through.
        var (voted, votes) = (item.Voted, item.Votes);
        (item.Voted, item.Votes) = (!voted, votes + (voted ? -1 : 1));
        if (await Online.Try(dialogs, "Couldn't vote", () => api.VoteFeedbackAsync(item.Ticket.Number)) is { } result)
        {
            (item.Voted, item.Votes) = (result.Voted, result.Votes);
            (item.Ticket.Voted, item.Ticket.Votes) = (result.Voted, result.Votes);
        }
        else
        {
            (item.Voted, item.Votes) = (voted, votes);
        }
    }

    Task OpenAsync(FeedbackItem item) => GoTo($"{Routes.FeedbackTicket}?number={item.Ticket.Number}");

    [RelayCommand]
    Task New() => GoTo($"{Routes.FeedbackEdit}?kind={_kind}");
}

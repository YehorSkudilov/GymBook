using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

/// <summary>
/// A feature request or bug report (?number=), with its comments, from GitHub through the API. A feature request can be
/// voted for; the user's own ticket and comments can be changed and deleted; comments can be added where allowed.
/// </summary>
public partial class FeedbackTicketViewModel(ApiClient api, DialogService dialogs) : BaseViewModel, IQueryAttributable
{
    int _number;
    FeedbackThread? _thread;

    [ObservableProperty] bool isLoading = true;
    [ObservableProperty] string? error;
    [ObservableProperty] bool isLoaded;
    [ObservableProperty] HtmlWebViewSource? page;
    [ObservableProperty] string pageTitle = "";
    [ObservableProperty] bool isFeature;
    [ObservableProperty] bool isMine;
    [ObservableProperty] bool canComment;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VoteText), nameof(VoteBackground), nameof(VoteColor))]
    bool voted;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VoteText))]
    int votes;
    [ObservableProperty] bool isBusy;

    public bool HasError => Error != null;
    partial void OnErrorChanged(string? value) => OnPropertyChanged(nameof(HasError));

    public string VoteText => $"▲  {Votes}  ·  {(Voted ? "Voted" : "Vote")}";
    public Color VoteBackground => Voted ? Color.FromArgb("#3F7DFF") : Color.FromArgb("#1D212C");
    public Color VoteColor => Voted ? Colors.White : Color.FromArgb("#F4F6FB");

    public void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _number = query.TryGetValue("number", out var n) && int.TryParse(n?.ToString(), out var number) ? number : 0;

    /// <summary>Every time: back from writing a comment or changing the ticket, as it is now.</summary>
    public override Task OnAppearingAsync() => LoadAsync();

    async Task LoadAsync()
    {
        if (_number <= 0)
            return;
        IsLoading = !IsLoaded;
        try
        {
            Show(await api.GetFeedbackTicketAsync(_number));
            Error = null;
        }
        catch (Exception e) when (Online.Message(e) is { } message)
        {
            if (!IsLoaded)
                Error = message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    void Show(FeedbackThread thread)
    {
        _thread = thread;
        var t = thread.Ticket;
        PageTitle = t.Kind == FeedbackKind.Feature ? "Feature request" : "Bug report";
        IsFeature = t.Kind == FeedbackKind.Feature;
        IsMine = t.IsMine;
        CanComment = thread.CanComment;
        (Voted, Votes) = (t.Voted, t.Votes);
        Page = new HtmlWebViewSource { Html = FeedbackHtml.Thread(thread) };
        IsLoaded = true;
    }

    [RelayCommand]
    async Task Retry()
    {
        Error = null;
        IsLoading = true;
        await LoadAsync();
    }

    [RelayCommand]
    async Task Vote()
    {
        if (_thread is not { } thread || IsBusy)
            return;
        if (thread.Ticket.IsClosed && !Voted)
        {
            await dialogs.Alert("This request is closed", "Closed requests can't be voted for.");
            return;
        }
        IsBusy = true;
        try
        {
            if (await Online.Try(dialogs, "Couldn't vote", () => api.VoteFeedbackAsync(_number)) is { } result)
            {
                (Voted, Votes) = (result.Voted, result.Votes);
                (thread.Ticket.Voted, thread.Ticket.Votes) = (result.Voted, result.Votes);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    Task Comment() => GoTo($"{Routes.FeedbackEdit}?number={_number}&comment=new");

    /// <summary>The user's own ticket: change it, or delete it.</summary>
    [RelayCommand]
    async Task Menu()
    {
        if (_thread is not { Ticket.IsMine: true })
            return;
        const string edit = "Edit";
        var choice = await dialogs.ActionSheet(_thread.Ticket.Title, "Delete", edit);
        if (choice == edit)
            await GoTo($"{Routes.FeedbackEdit}?number={_number}");
        else if (choice == "Delete" && await dialogs.Confirm($"Delete this {(IsFeature ? "feature request" : "bug report")}?",
                     "It's deleted for good, with its comments" + (IsFeature ? " and votes." : "."), "Delete")
                 && await Online.Try(dialogs, "Couldn't delete it", () => api.DeleteFeedbackAsync(_number)))
            await GoBack();
    }

    /// <summary>A link on the page: Edit or Delete on one of the user's comments, or anything else, opened in the browser.</summary>
    public async Task OpenLinkAsync(Uri uri)
    {
        if (uri.Scheme == FeedbackHtml.Scheme)
        {
            // gymbook-feedback://comment/{id}/{edit|delete}
            var parts = uri.AbsolutePath.Trim('/').Split('/');
            if (uri.Host != "comment" || parts.Length != 2 || !long.TryParse(parts[0], out var id))
                return;
            if (parts[1] == "edit")
                await GoTo($"{Routes.FeedbackEdit}?number={_number}&comment={id}");
            else if (parts[1] == "delete" && await dialogs.Confirm("Delete your comment?", "It's deleted for good.", "Delete")
                     && await Online.Try(dialogs, "Couldn't delete it", () => api.DeleteFeedbackCommentAsync(id)) is { } thread)
                Show(thread);
            return;
        }
        if (uri.Scheme is "http" or "https" or "mailto")
            await Launcher.Default.OpenAsync(uri);
    }
}

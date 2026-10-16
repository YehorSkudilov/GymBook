using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Contracts;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

/// <summary>
/// Writing a feature request or bug report, or a comment on one, in Markdown: bold, italics, headings, lists, checklists,
/// quotes, code and links from the toolbar (see FeedbackEditPage), shown as they're typed, and attachments. Opened as
/// ?kind= (a new ticket), ?number= (changing the user's own), or ?number=&amp;comment= ("new", or the id of the user's
/// own comment to change).
/// </summary>
public partial class FeedbackEditViewModel(ApiClient api, DialogService dialogs) : BaseViewModel, IQueryAttributable
{
    const string BugTemplate = "**What happened?**\n\n\n**What did you expect to happen?**\n\n\n**Steps to reproduce**\n1. \n";

    FeedbackKind _kind = FeedbackKind.Feature;
    int _number;
    long? _commentId;
    bool _isComment;
    bool _loaded;

    [ObservableProperty] string pageTitle = "";
    [ObservableProperty] string title = "";
    [ObservableProperty] string body = "";
    [ObservableProperty] string bodyPlaceholder = "";
    [ObservableProperty] string hint = "";
    [ObservableProperty] bool isTicket;
    [ObservableProperty] string sendText = "Send";
    [ObservableProperty] bool isBusy;
    [ObservableProperty] string busyText = "";
    [ObservableProperty] bool isLoading;
    [ObservableProperty] string? error;
    public bool HasError => Error != null;
    partial void OnErrorChanged(string? value) => OnPropertyChanged(nameof(HasError));

    /// <summary>Text to put in at the cursor (an attachment): the page does it, as only it knows where the cursor is.</summary>
    public event Action<string>? InsertRequested;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("kind", out var kind) && Enum.TryParse<FeedbackKind>(kind?.ToString(), out var k))
            _kind = k;
        _number = query.TryGetValue("number", out var n) && int.TryParse(n?.ToString(), out var number) ? number : 0;
        _isComment = query.TryGetValue("comment", out var c);
        _commentId = _isComment && long.TryParse(c?.ToString(), out var id) ? id : null;
    }

    public override async Task OnAppearingAsync()
    {
        if (_loaded)
            return;
        _loaded = true;
        IsTicket = !_isComment;
        if (_isComment)
        {
            PageTitle = _commentId == null ? "New comment" : "Edit comment";
            SendText = _commentId == null ? "Comment" : "Save";
            BodyPlaceholder = "Write a comment";
            Hint = "";
        }
        else if (_number == 0)
        {
            var bug = _kind == FeedbackKind.Bug;
            PageTitle = bug ? "Report a bug" : "Suggest a feature";
            SendText = "Send";
            BodyPlaceholder = bug ? "What went wrong?" : "What would you like Gym Book to do, and why?";
            Hint = bug
                ? "Only you and the Gym Book team will see this. Your device and app version are added to help us fix it. Screenshots help a lot."
                : "Everyone using Gym Book will see this with your username, and can vote for it and comment.";
            if (bug)
                Body = BugTemplate;
        }
        else
        {
            PageTitle = "Edit";
            SendText = "Save";
        }

        // Changing something already sent: what it says now.
        if (_number > 0 && (!_isComment || _commentId != null))
        {
            IsLoading = true;
            try
            {
                var thread = await api.GetFeedbackTicketAsync(_number);
                if (_commentId is { } commentId)
                {
                    Body = thread.Comments.FirstOrDefault(x => x.Id == commentId)?.Body ?? "";
                }
                else
                {
                    _kind = thread.Ticket.Kind;
                    PageTitle = _kind == FeedbackKind.Bug ? "Edit bug report" : "Edit feature request";
                    BodyPlaceholder = _kind == FeedbackKind.Bug ? "What went wrong?" : "What would you like Gym Book to do, and why?";
                    (Title, Body) = (thread.Ticket.Title, thread.Ticket.Body);
                }
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
    }

    /// <summary>The address for the Link button; null if none was given (or it isn't one).</summary>
    public async Task<string?> AskLinkAsync()
    {
        var url = (await dialogs.Prompt("Link", "The web address to link to", "https://", Keyboard.Url, "Add link"))?.Trim();
        if (string.IsNullOrEmpty(url) || url == "https://")
            return null;
        if (!url.Contains("://") && !url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase))
            url = "https://" + url;
        return Uri.TryCreate(url, UriKind.Absolute, out _) ? url : null;
    }

    /// <summary>A picture, video or file from the device, put in the text where the cursor is.</summary>
    [RelayCommand]
    async Task Attach()
    {
        if (IsBusy)
            return;
        FileResult? file;
        try
        {
            file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Attach a file" });
        }
        catch (Exception)
        {
            // Closed, or no picker on this device.
            return;
        }
        if (file == null)
            return;

        byte[] data;
        try
        {
            await using var stream = await file.OpenReadAsync();
            if (stream.CanSeek && stream.Length > FeedbackLimits.AttachmentBytes)
            {
                await TooBig();
                return;
            }
            using var copy = new MemoryStream();
            await stream.CopyToAsync(copy);
            data = copy.ToArray();
        }
        catch (Exception e)
        {
            await dialogs.Alert("Couldn't read the file", e.Message);
            return;
        }
        await AttachDataAsync(file.FileName, data);
    }

    /// <summary>
    /// A file's bytes uploaded and put in the text where the cursor is: one picked with <see cref="Attach"/>, or a picture
    /// pasted into the text (the editor hands it over).
    /// </summary>
    public async Task AttachDataAsync(string fileName, byte[] data)
    {
        if (IsBusy || data.Length == 0)
            return;
        if (data.Length > FeedbackLimits.AttachmentBytes)
        {
            await TooBig();
            return;
        }
        IsBusy = true;
        BusyText = "Uploading…";
        try
        {
            if (await Online.Try(dialogs, "Couldn't attach the file", () => api.AttachFeedbackFileAsync(fileName, data)) is { } attached)
                InsertRequested?.Invoke(attached.Markdown);
        }
        finally
        {
            IsBusy = false;
        }
    }

    Task TooBig() => dialogs.Alert("That file is too big", $"Attach files up to {FeedbackLimits.AttachmentBytes / 1024 / 1024} MB.");

    [RelayCommand]
    async Task Send()
    {
        if (IsBusy || IsLoading || HasError)
            return;
        var body = Body.Trim();
        if (IsTicket && Title.Trim().Length == 0)
        {
            await dialogs.Alert("Add a title", "Sum it up in a few words, so it's easy to find.");
            return;
        }
        if (_isComment && body.Length == 0)
            return;

        IsBusy = true;
        BusyText = "Sending…";
        try
        {
            if (_isComment)
            {
                var thread = await Online.Try(dialogs, "Couldn't send your comment", () => _commentId is { } id
                    ? api.UpdateFeedbackCommentAsync(id, body)
                    : api.AddFeedbackCommentAsync(_number, body));
                if (thread != null)
                    await GoBack();
                return;
            }
            var request = new SaveFeedbackRequest
            {
                Kind = _kind,
                Title = Title.Trim(),
                Body = body,
                Device = _number == 0 && _kind == FeedbackKind.Bug ? Device() : null,
            };
            if (_number > 0)
            {
                if (await Online.Try(dialogs, "Couldn't save", () => api.UpdateFeedbackAsync(_number, request)) != null)
                    await GoBack();
            }
            else if (await Online.Try(dialogs, "Couldn't send it", () => api.SendFeedbackAsync(request)) is { } sent)
            {
                // On to what was sent, in place of this page.
                await GoTo($"../{Routes.FeedbackTicket}?number={sent.Ticket.Number}");
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>Back, asking first when something's been written and not sent.</summary>
    [RelayCommand]
    async Task Back()
    {
        var untouched = Title.Trim().Length == 0 && (Body.Trim().Length == 0 || Body == BugTemplate);
        if (_number > 0 || untouched || await dialogs.Confirm("Discard what you wrote?", "It hasn't been sent.", "Discard", "Keep writing"))
            await GoBack();
    }

    static FeedbackDevice Device()
    {
        static string Clip(string s, int max) => s.Length <= max ? s : s[..max];
        var info = DeviceInfo.Current;
        return new FeedbackDevice
        {
            AppVersion = Clip(AppInfo.Current.VersionString, 40),
            Platform = Clip(info.Platform.ToString(), 40),
            OsVersion = Clip(info.VersionString, 40),
            Model = Clip($"{info.Manufacturer} {info.Model}".Trim(), 80),
        };
    }
}

using System.Net;
using System.Text.RegularExpressions;
using GymBook.Api.Data;
using GymBook.Api.Social;
using GymBook.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace GymBook.Api.Feedback;

/// <summary>
/// Feature requests and bug reports, kept on GitHub (see <see cref="GitHubClient"/>): each ticket an issue of a private
/// repository on the project board, its comments the issue's comments, its attachments files in the repository. The team
/// answers on GitHub; the app shows it all, with where the ticket is on the board.
/// <para>
/// Only what GitHub can't know is kept in the database (see Data/FeedbackEntities.cs), since everything goes there with
/// one token: who sent each ticket and comment (theirs alone to edit and delete) and who voted. Feature requests are public
/// in the app, with their author's username, to vote on and comment on; a bug report is seen only by whoever sent it.
/// What came from the app ends with a footer saying so (who sent it, and for a bug, the device), after
/// <see cref="Marker"/>, which the app never shows; without one it's the team's.
/// </para>
/// </summary>
public partial class FeedbackService(
    ApiDbContext db, GitHubClient github, Usernames usernames, IMemoryCache cache, TimeProvider time, ILogger<FeedbackService> log)
{
    const string Marker = "<!-- gymbook -->";
    const string TeamName = "Gym Book";
    const string FormerMember = "Former member";
    const int TicketsPerDay = 10, CommentsPerDay = 30, AttachmentsPerDay = 30;

    // ---- Reading ----

    /// <summary>
    /// Every feature request, or the user's own bug reports, newest first, as they are on GitHub now (nothing kept here,
    /// so what's changed or deleted there shows at once); with the board's statuses, to filter by.
    /// </summary>
    public async Task<FeedbackListResponse> ListAsync(FeedbackKind kind, string me, string baseUrl, CancellationToken ct)
    {
        List<GitHubIssue> issues;
        if (kind == FeedbackKind.Feature)
        {
            issues = await github.ListIssuesAsync(GitHubClient.FeatureLabel, ct);
        }
        else
        {
            var mine = await db.FeedbackTickets.AsNoTracking().Where(t => t.UserId == me && t.Kind == FeedbackKind.Bug).Select(t => t.Number).ToListAsync(ct);
            issues = mine.Count == 0 ? [] : [.. (await github.GetIssuesAsync(mine, full: false, ct)).OrderByDescending(i => i.CreatedAt)];
            await ForgetDeletedAsync(mine.Except(issues.Select(i => i.Number)), ct);
        }
        // With a board, it's what's shown: one taken off it (or archived) is gone from the app too.
        if (await github.HasBoardAsync(ct))
            issues = [.. issues.Where(i => i.ProjectItemId != null)];
        return new FeedbackListResponse
        {
            Tickets = await TicketsAsync(issues, me, baseUrl, ct),
            Statuses = await github.GetStatusesAsync(ct),
        };
    }

    /// <summary>Tickets deleted on GitHub: who sent them, their votes and comments go from here too.</summary>
    async Task ForgetDeletedAsync(IEnumerable<int> numbers, CancellationToken ct)
    {
        var gone = numbers.ToList();
        if (gone.Count == 0)
            return;
        await db.FeedbackTickets.Where(t => gone.Contains(t.Number)).ExecuteDeleteAsync(ct);
        await db.FeedbackVotes.Where(v => gone.Contains(v.Number)).ExecuteDeleteAsync(ct);
        await db.FeedbackComments.Where(c => gone.Contains(c.Number)).ExecuteDeleteAsync(ct);
    }

    /// <summary>A ticket with its comments, when the user may see it: any feature request, or their own bug report.</summary>
    public async Task<FeedbackThread> GetAsync(int number, string me, string baseUrl, CancellationToken ct)
    {
        var (issue, owner) = await VisibleAsync(number, me, ct);
        var ticket = (await TicketsAsync([issue], me, baseUrl, ct, full: true))[0];

        var comments = await github.GetCommentsAsync(number, ct);
        var commentOwners = await db.FeedbackComments.AsNoTracking().Where(c => c.Number == number).ToDictionaryAsync(c => c.CommentId, c => c.UserId, ct);
        var names = await UsernamesAsync(commentOwners.Values, ct);
        return new FeedbackThread
        {
            Ticket = ticket,
            CanComment = IsFeature(issue) || owner?.UserId == me,
            Comments = [.. comments.Select(c =>
            {
                var by = commentOwners.GetValueOrDefault(c.Id);
                var mine = by == me;
                return new FeedbackComment
                {
                    Id = c.Id,
                    Body = mine ? ToApp(StripFooter(c.Body), baseUrl) : "",
                    BodyHtml = ToAppHtml(StripCommentFooterHtml(c.BodyHtml), baseUrl),
                    Author = by != null ? names.GetValueOrDefault(by, FormerMember) : c.Body.Contains(Marker) ? FormerMember : TeamName,
                    IsTeam = by == null && !c.Body.Contains(Marker),
                    IsMine = mine,
                    CreatedAt = c.CreatedAt,
                    Edited = c.Edited,
                };
            })],
        };
    }

    /// <summary>The issue and who sent it, if the user may see it; otherwise as if it didn't exist.</summary>
    async Task<(GitHubIssue Issue, FeedbackTicketOwner? Owner)> VisibleAsync(int number, string me, CancellationToken ct)
    {
        if ((await github.GetIssuesAsync([number], full: true, ct)).FirstOrDefault() is not { } issue)
        {
            // Deleted on GitHub.
            await ForgetDeletedAsync([number], ct);
            throw Missing();
        }
        // Taken off the board (or archived): not shown, though it's still on GitHub.
        if (issue.ProjectItemId == null && await github.HasBoardAsync(ct))
            throw Missing();
        var owner = await db.FeedbackTickets.AsNoTracking().FirstOrDefaultAsync(t => t.Number == number, ct);
        if (!IsFeature(issue) && owner?.UserId != me)
            throw Missing();
        return (issue, owner);
    }

    static bool IsFeature(GitHubIssue issue) => issue.Labels.Contains(GitHubClient.FeatureLabel);

    static FeedbackException Missing() => new(404, "That ticket doesn't exist, or it isn't yours to see.");

    async Task<List<FeedbackTicket>> TicketsAsync(List<GitHubIssue> issues, string me, string baseUrl, CancellationToken ct, bool full = false)
    {
        var numbers = issues.Select(i => i.Number).ToList();
        var owners = await db.FeedbackTickets.AsNoTracking().Where(t => numbers.Contains(t.Number)).ToDictionaryAsync(t => t.Number, t => t.UserId, ct);
        var votes = await db.FeedbackVotes.AsNoTracking().Where(v => numbers.Contains(v.Number))
            .GroupBy(v => v.Number).Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(g => g.Key, g => g.Count, ct);
        var myVotes = (await db.FeedbackVotes.AsNoTracking().Where(v => v.UserId == me && numbers.Contains(v.Number)).Select(v => v.Number).ToListAsync(ct)).ToHashSet();
        var names = await UsernamesAsync(owners.Values, ct);
        return [.. issues.Select(i =>
        {
            var by = owners.GetValueOrDefault(i.Number);
            var fromApp = i.Body.Contains(Marker);
            return new FeedbackTicket
            {
                Number = i.Number,
                Kind = IsFeature(i) ? FeedbackKind.Feature : FeedbackKind.Bug,
                Title = i.Title,
                Body = full && by == me ? ToApp(StripFooter(i.Body), baseUrl) : "",
                BodyHtml = full ? ToAppHtml(StripTicketFooterHtml(i.BodyHtml), baseUrl) : "",
                Status = i.Status ?? (i.IsClosed ? "Closed" : "Open"),
                IsClosed = i.IsClosed,
                CreatedAt = i.CreatedAt,
                UpdatedAt = i.UpdatedAt,
                Author = by != null ? names.GetValueOrDefault(by, FormerMember) : fromApp ? FormerMember : TeamName,
                IsTeam = by == null && !fromApp,
                IsMine = by == me,
                Votes = votes.GetValueOrDefault(i.Number),
                Voted = myVotes.Contains(i.Number),
                CommentCount = i.CommentCount,
            };
        })];
    }

    async Task<Dictionary<string, string>> UsernamesAsync(IEnumerable<string> userIds, CancellationToken ct)
    {
        var ids = userIds.Distinct().ToList();
        return await db.SocialProfiles.AsNoTracking().Where(p => ids.Contains(p.UserId)).ToDictionaryAsync(p => p.UserId, p => p.Username, ct);
    }

    // ---- Tickets ----

    public async Task<FeedbackThread> CreateAsync(string me, SaveFeedbackRequest request, string baseUrl, CancellationToken ct)
    {
        var title = Title(request.Title);
        var since = time.GetUtcNow().AddDays(-1);
        if (await db.FeedbackTickets.CountAsync(t => t.UserId == me && t.CreatedAt > since, ct) >= TicketsPerDay)
            throw new FeedbackException(429, "You've sent a lot today. Please try again tomorrow.");
        // The team sees who sent it: one who never picked a username gets a made-up one.
        var profile = await usernames.EnsureAsync(me, null, ct);
        var body = ToGitHub(request.Body) + Footer(profile.Username, request.Kind == FeedbackKind.Bug ? request.Device : null);
        var label = request.Kind == FeedbackKind.Feature ? GitHubClient.FeatureLabel : GitHubClient.BugLabel;
        var number = await github.CreateIssueAsync(title, body, label, ct);
        db.FeedbackTickets.Add(new FeedbackTicketOwner { Number = number, UserId = me, Kind = request.Kind, CreatedAt = time.GetUtcNow() });
        await db.SaveChangesAsync(ct);
        return await GetAsync(number, me, baseUrl, ct);
    }

    /// <summary>A new title and text for the user's own ticket. What it is (feature or bug) stays.</summary>
    public async Task<FeedbackThread> UpdateAsync(string me, int number, SaveFeedbackRequest request, string baseUrl, CancellationToken ct)
    {
        await OwnedAsync(number, me, ct);
        var title = Title(request.Title);
        var issue = (await github.GetIssuesAsync([number], full: true, ct)).FirstOrDefault() ?? throw Missing();
        // The footer stays as it was sent (the device a bug was found on).
        var at = issue.Body.IndexOf(Marker, StringComparison.Ordinal);
        var footer = at >= 0 ? "\n\n" + issue.Body[at..] : Footer((await usernames.EnsureAsync(me, null, ct)).Username, null);
        await github.UpdateIssueAsync(number, title, ToGitHub(request.Body) + footer, ct);
        return await GetAsync(number, me, baseUrl, ct);
    }

    /// <summary>Deletes the user's own ticket from GitHub, with its comments and votes.</summary>
    public async Task DeleteAsync(string me, int number, CancellationToken ct)
    {
        var owner = await OwnedAsync(number, me, ct);
        if ((await github.GetIssuesAsync([number], full: false, ct)).FirstOrDefault() is { } issue)
            await github.DeleteIssueAsync(issue.NodeId, ct);
        db.FeedbackTickets.Remove(owner);
        await db.FeedbackVotes.Where(v => v.Number == number).ExecuteDeleteAsync(ct);
        await db.FeedbackComments.Where(c => c.Number == number).ExecuteDeleteAsync(ct);
        await db.SaveChangesAsync(ct);
    }

    async Task<FeedbackTicketOwner> OwnedAsync(int number, string me, CancellationToken ct)
    {
        var owner = await db.FeedbackTickets.FirstOrDefaultAsync(t => t.Number == number, ct) ?? throw Missing();
        return owner.UserId == me ? owner : throw new FeedbackException(403, "Only whoever sent it can change it.");
    }

    static string Title(string title) =>
        title.Trim() is { Length: > 0 } t ? t : throw new FeedbackException(400, "Give it a title.");

    /// <summary>Votes for a feature request, or takes the vote back.</summary>
    public async Task<FeedbackVoteResponse> VoteAsync(string me, int number, CancellationToken ct)
    {
        var issue = (await github.GetIssuesAsync([number], full: false, ct)).FirstOrDefault();
        if (issue == null || !IsFeature(issue))
            throw Missing();
        var vote = await db.FeedbackVotes.FirstOrDefaultAsync(v => v.Number == number && v.UserId == me, ct);
        if (vote != null)
            db.FeedbackVotes.Remove(vote);
        else if (issue.IsClosed)
            throw new FeedbackException(400, "This request is closed, so it can't be voted for.");
        else
            db.FeedbackVotes.Add(new FeedbackVote { Number = number, UserId = me, CreatedAt = time.GetUtcNow() });
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Voted twice at once (two taps, two devices): the first counts.
        }
        var votes = await db.FeedbackVotes.CountAsync(v => v.Number == number, ct);
        try
        {
            await github.SetVotesAsync(issue, votes, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            // The board catches up with the next vote.
            log.LogWarning(e, "Feedback: couldn't set the votes of #{Number} on the board", number);
        }
        return new FeedbackVoteResponse { Votes = votes, Voted = vote == null };
    }

    // ---- Comments ----

    public async Task<FeedbackThread> AddCommentAsync(string me, int number, string text, string baseUrl, CancellationToken ct)
    {
        await VisibleAsync(number, me, ct);
        var since = time.GetUtcNow().AddDays(-1);
        if (await db.FeedbackComments.CountAsync(c => c.UserId == me && c.CreatedAt > since, ct) >= CommentsPerDay)
            throw new FeedbackException(429, "You've commented a lot today. Please try again tomorrow.");
        var body = Comment(text, (await usernames.EnsureAsync(me, null, ct)).Username);
        var id = await github.AddCommentAsync(number, body, ct);
        db.FeedbackComments.Add(new FeedbackCommentOwner { CommentId = id, Number = number, UserId = me, CreatedAt = time.GetUtcNow() });
        await db.SaveChangesAsync(ct);
        return await GetAsync(number, me, baseUrl, ct);
    }

    public async Task<FeedbackThread> UpdateCommentAsync(string me, long id, string text, string baseUrl, CancellationToken ct)
    {
        var comment = await OwnedCommentAsync(id, me, ct);
        await github.UpdateCommentAsync(id, Comment(text, (await usernames.EnsureAsync(me, null, ct)).Username), ct);
        return await GetAsync(comment.Number, me, baseUrl, ct);
    }

    public async Task<FeedbackThread> DeleteCommentAsync(string me, long id, string baseUrl, CancellationToken ct)
    {
        var comment = await OwnedCommentAsync(id, me, ct);
        await github.DeleteCommentAsync(id, ct);
        db.FeedbackComments.Remove(comment);
        await db.SaveChangesAsync(ct);
        return await GetAsync(comment.Number, me, baseUrl, ct);
    }

    async Task<FeedbackCommentOwner> OwnedCommentAsync(long id, string me, CancellationToken ct)
    {
        var comment = await db.FeedbackComments.FirstOrDefaultAsync(c => c.CommentId == id, ct)
            ?? throw new FeedbackException(404, "That comment doesn't exist any more.");
        return comment.UserId == me ? comment : throw new FeedbackException(403, "Only whoever wrote it can change it.");
    }

    string Comment(string text, string username) =>
        text.Trim() is { Length: > 0 } t
            ? ToGitHub(t) + $"\n\n{Marker}\n<sub>@{username} in Gym Book</sub>"
            : throw new FeedbackException(400, "Write something first.");

    // ---- Attachments ----

    static readonly Dictionary<string, string> FileTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png", [".jpg"] = "image/jpeg", [".jpeg"] = "image/jpeg", [".gif"] = "image/gif", [".webp"] = "image/webp",
        [".mp4"] = "video/mp4", [".mov"] = "video/quicktime", [".webm"] = "video/webm",
        [".pdf"] = "application/pdf", [".txt"] = "text/plain", [".log"] = "text/plain", [".csv"] = "text/csv",
        [".json"] = "application/json", [".zip"] = "application/zip",
    };

    /// <summary>Puts a file in the repository; returns its address and the Markdown to put it in the text with.</summary>
    public async Task<FeedbackAttachmentResponse> AttachAsync(string me, FeedbackAttachmentRequest request, string baseUrl, CancellationToken ct)
    {
        var name = FileName(request.FileName);
        if (!FileTypes.ContainsKey(Path.GetExtension(name)))
            throw new FeedbackException(400, "Attach a picture, a video, a PDF, a text file or a zip.");
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(request.DataBase64);
        }
        catch (FormatException)
        {
            throw new FeedbackException(400, "That file couldn't be read.");
        }
        if (bytes.Length == 0 || bytes.Length > FeedbackLimits.AttachmentBytes)
            throw new FeedbackException(400, $"A file can be up to {FeedbackLimits.AttachmentBytes / 1024 / 1024} MB.");
        var key = $"feedback:attachments:{me}:{time.GetUtcNow():yyyyMMdd}";
        var count = cache.GetOrCreate(key, e => { e.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1); return 0; });
        if (count >= AttachmentsPerDay)
            throw new FeedbackException(429, "You've attached a lot today. Please try again tomorrow.");
        cache.Set(key, count + 1, TimeSpan.FromDays(1));

        var id = Guid.NewGuid().ToString("N");
        await github.PutFileAsync($"{GitHubClient.FilesFolder}/{id}/{name}", bytes, ct);
        var url = AppFileUrl(baseUrl, id, name);
        var isImage = FileTypes[Path.GetExtension(name)].StartsWith("image/", StringComparison.Ordinal);
        return new FeedbackAttachmentResponse { Url = url, Markdown = isImage ? $"![{name}]({url})" : $"[{name}]({url})" };
    }

    /// <summary>
    /// An attachment, for the app (and anyone with its address: the 128-bit id in it is the key, as with GitHub's own
    /// attachment links). Small ones are kept in memory a while.
    /// </summary>
    public async Task<(byte[] Bytes, string ContentType)?> FileAsync(string id, string name, CancellationToken ct)
    {
        if (!FileIdPattern().IsMatch(id) || !FileNamePattern().IsMatch(name) || !FileTypes.TryGetValue(Path.GetExtension(name), out var type))
            return null;
        var key = $"feedback:file:{id}";
        if (cache.TryGetValue(key, out byte[]? kept) && kept != null)
            return (kept, type);
        var bytes = await github.GetFileAsync($"{GitHubClient.FilesFolder}/{id}/{name}", ct);
        if (bytes == null)
            return null;
        if (bytes.Length <= 2 * 1024 * 1024)
            cache.Set(key, bytes, TimeSpan.FromHours(1));
        return (bytes, type);
    }

    /// <summary>Letters, digits, dots, dashes and underscores, the extension kept.</summary>
    static string FileName(string name)
    {
        name = Path.GetFileName(name.Trim());
        var ext = Path.GetExtension(name).ToLowerInvariant();
        var stem = UnsafeFileChars().Replace(Path.GetFileNameWithoutExtension(name), "-").Trim('-', '.');
        if (stem.Length == 0)
            stem = "file";
        return stem[..Math.Min(stem.Length, 60)] + ext;
    }

    // ---- Account deletion ----

    /// <summary>
    /// Before an account is deleted: its bug reports and its comments are deleted from GitHub, and its feature requests
    /// stay (others voted for them) without its username. Best effort: a failure is logged, and the account still goes.
    /// </summary>
    public async Task ForgetUserAsync(string userId, CancellationToken ct)
    {
        if (!github.Options.IsConfigured)
            return;
        try
        {
            var tickets = await db.FeedbackTickets.AsNoTracking().Where(t => t.UserId == userId).Select(t => t.Number).ToListAsync(ct);
            List<GitHubIssue> issues = tickets.Count == 0 ? [] : await github.GetIssuesAsync(tickets, full: false, ct);
            foreach (var issue in issues)
            {
                if (!IsFeature(issue))
                {
                    await github.DeleteIssueAsync(issue.NodeId, ct);
                    continue;
                }
                var at = issue.Body.IndexOf(Marker, StringComparison.Ordinal);
                await github.UpdateIssueAsync(issue.Number, issue.Title, (at >= 0 ? issue.Body[..at].TrimEnd() : issue.Body) + Footer(null, null), ct);
            }
            var deleted = issues.Where(i => !IsFeature(i)).Select(i => i.Number).ToHashSet();
            var comments = await db.FeedbackComments.AsNoTracking().Where(c => c.UserId == userId).ToListAsync(ct);
            foreach (var comment in comments.Where(c => !deleted.Contains(c.Number)))
                await github.DeleteCommentAsync(comment.CommentId, ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogError(e, "Feedback: couldn't remove the tickets of deleted account {User} from GitHub", userId);
        }
    }

    // ---- Text as it's kept on GitHub and as the app shows it ----

    /// <summary>The footer that marks a ticket as from the app: who sent it, and for a bug the device.</summary>
    static string Footer(string? username, FeedbackDevice? device)
    {
        var parts = new List<string> { username == null ? "Sent from Gym Book" : $"Sent from Gym Book by @{username}" };
        if (device != null)
        {
            parts.Add($"{device.Platform} {device.OsVersion}".Trim());
            parts.Add(device.Model.Trim());
            if (device.AppVersion.Trim().Length > 0)
                parts.Add("app " + device.AppVersion.Trim());
        }
        return $"\n\n{Marker}\n\n---\n<sub>{WebUtility.HtmlEncode(string.Join(" · ", parts.Where(p => p.Length > 0)))}</sub>";
    }

    static string StripFooter(string body) =>
        body.IndexOf(Marker, StringComparison.Ordinal) is >= 0 and var at ? body[..at].TrimEnd() : body;

    // The footers as GitHub shows them (the marker, an HTML comment, isn't shown): taken off the end.
    static string StripTicketFooterHtml(string html) => TicketFooterHtml().Replace(html, "");
    static string StripCommentFooterHtml(string html) => CommentFooterHtml().Replace(html, "");

    /// <summary>The user's text as it goes to GitHub: attachments linked where the team can open them, no stray marker.</summary>
    string ToGitHub(string markdown) =>
        AppFileLink().Replace(markdown.Replace(Marker, "").Trim(), m => GitHubFileUrl(m.Groups["id"].Value, m.Groups["name"].Value));

    string GitHubFileUrl(string id, string name) =>
        $"https://github.com/{github.Options.Owner}/{github.Options.Repo}/blob/{github.Options.AttachmentsBranch}/{GitHubClient.FilesFolder}/{id}/{name}?raw=true";

    /// <summary>Text from GitHub as the app shows it: attachments through the API, which can read the private repository.</summary>
    static string ToApp(string text, string baseUrl) =>
        RawFileLink().Replace(GitHubFileLink().Replace(text, m => AppFileUrl(baseUrl, m.Groups["id"].Value, m.Groups["name"].Value)),
            m => AppFileUrl(baseUrl, m.Groups["id"].Value, m.Groups["name"].Value));

    static string ToAppHtml(string html, string baseUrl) => ToApp(html, baseUrl);

    static string AppFileUrl(string baseUrl, string id, string name) => $"{baseUrl}/api/feedback/files/{id}/{name}";

    [GeneratedRegex("""https?://[^\s()\[\]"'<>]+?/api/feedback/files/(?<id>[0-9a-f]{32})/(?<name>[A-Za-z0-9._-]{1,100})""")]
    private static partial Regex AppFileLink();

    [GeneratedRegex("""https://github\.com/[^/\s"'<>()]+/[^/\s"'<>()]+/(?:blob|raw)/[^\s"'<>()]*?/feedback-files/(?<id>[0-9a-f]{32})/(?<name>[A-Za-z0-9._-]{1,100})(?:\?raw=true)?""")]
    private static partial Regex GitHubFileLink();

    [GeneratedRegex("""https://raw\.githubusercontent\.com/[^\s"'<>()]*?/feedback-files/(?<id>[0-9a-f]{32})/(?<name>[A-Za-z0-9._-]{1,100})(?:\?[^\s"'<>()]*)?""")]
    private static partial Regex RawFileLink();

    [GeneratedRegex("""<hr\s*/?>\s*(?:<p[^>]*>)?\s*<sub>Sent from Gym Book[\s\S]*$""")]
    private static partial Regex TicketFooterHtml();

    [GeneratedRegex("""(?:<p[^>]*>)?\s*<sub>@[^<]*? in Gym Book</sub>[\s\S]*$""")]
    private static partial Regex CommentFooterHtml();

    [GeneratedRegex("^[0-9a-f]{32}$")]
    private static partial Regex FileIdPattern();

    [GeneratedRegex("^[A-Za-z0-9._-]{1,100}$")]
    private static partial Regex FileNamePattern();

    [GeneratedRegex("[^A-Za-z0-9._-]+")]
    private static partial Regex UnsafeFileChars();
}

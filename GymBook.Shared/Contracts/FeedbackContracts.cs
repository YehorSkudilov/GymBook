using System.ComponentModel.DataAnnotations;

namespace GymBook.Contracts;

// Feature requests and bug reports. They live on GitHub, as issues of a private repository on its project board; the
// API only keeps who owns which ticket and comment, and who voted. Feature requests are public in the app, with their
// author's username, and anyone can vote on them and comment; a bug report is seen only by the one who sent it.

public enum FeedbackKind { Feature, Bug }

public static class FeedbackLimits
{
    public const int TitleLength = 120;
    /// <summary>Markdown. GitHub takes up to 65536 characters with what the API adds.</summary>
    public const int BodyLength = 20000;
    public const int CommentLength = 10000;
    public const int AttachmentBytes = 10 * 1024 * 1024;
    public const int AttachmentBase64Length = (AttachmentBytes + 2) / 3 * 4;
    public const int FileNameLength = 100;
}

/// <summary>A ticket: in lists without <see cref="BodyHtml"/> and <see cref="Body"/>, which only a single one has.</summary>
public class FeedbackTicket
{
    /// <summary>The GitHub issue's number.</summary>
    public int Number { get; set; }
    public FeedbackKind Kind { get; set; }
    public string Title { get; set; } = "";
    /// <summary>As written, Markdown, for editing (attachments as links to the API).</summary>
    public string Body { get; set; } = "";
    /// <summary>As GitHub shows it, HTML.</summary>
    public string BodyHtml { get; set; } = "";
    /// <summary>Where it is on the project board ("Todo", "In progress", ...), or "Open" / "Closed" off it.</summary>
    public string Status { get; set; } = "";
    public bool IsClosed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    /// <summary>The username of whoever sent it; "Gym Book" for one the team wrote.</summary>
    public string Author { get; set; } = "";
    public bool IsTeam { get; set; }
    public bool IsMine { get; set; }
    /// <summary>Feature requests only.</summary>
    public int Votes { get; set; }
    public bool Voted { get; set; }
    public int CommentCount { get; set; }
}

public class FeedbackComment
{
    public long Id { get; set; }
    /// <summary>As written, Markdown, for editing; empty for the team's.</summary>
    public string Body { get; set; } = "";
    public string BodyHtml { get; set; } = "";
    /// <summary>The username of whoever wrote it; "Gym Book" for the team.</summary>
    public string Author { get; set; } = "";
    public bool IsTeam { get; set; }
    public bool IsMine { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public bool Edited { get; set; }
}

public class FeedbackThread
{
    public FeedbackTicket Ticket { get; set; } = new();
    public List<FeedbackComment> Comments { get; set; } = [];
    /// <summary>Comments can be added: to any feature request, and to the user's own bug reports.</summary>
    public bool CanComment { get; set; }
}

public class FeedbackListResponse
{
    public List<FeedbackTicket> Tickets { get; set; } = [];
    /// <summary>The project board's Status columns, in its order, to filter by (empty without a board).</summary>
    public List<string> Statuses { get; set; } = [];
}

/// <summary>About the device a bug was found on, added to the report for the team.</summary>
public class FeedbackDevice
{
    [MaxLength(40)]
    public string AppVersion { get; set; } = "";
    [MaxLength(40)]
    public string Platform { get; set; } = "";
    [MaxLength(40)]
    public string OsVersion { get; set; } = "";
    [MaxLength(80)]
    public string Model { get; set; } = "";
}

public class SaveFeedbackRequest
{
    public FeedbackKind Kind { get; set; }
    [Required, MaxLength(FeedbackLimits.TitleLength)]
    public string Title { get; set; } = "";
    [MaxLength(FeedbackLimits.BodyLength)]
    public string Body { get; set; } = "";
    /// <summary>With a new bug report.</summary>
    public FeedbackDevice? Device { get; set; }
}

public class SaveFeedbackCommentRequest
{
    [Required, MaxLength(FeedbackLimits.CommentLength)]
    public string Body { get; set; } = "";
}

public class FeedbackAttachmentRequest
{
    [Required, MaxLength(FeedbackLimits.FileNameLength)]
    public string FileName { get; set; } = "";
    [Required, MaxLength(FeedbackLimits.AttachmentBase64Length)]
    public string DataBase64 { get; set; } = "";
}

public class FeedbackAttachmentResponse
{
    public string Url { get; set; } = "";
    /// <summary>To put in the text: a picture for an image, a link for anything else.</summary>
    public string Markdown { get; set; } = "";
}

public class FeedbackVoteResponse
{
    public int Votes { get; set; }
    public bool Voted { get; set; }
}

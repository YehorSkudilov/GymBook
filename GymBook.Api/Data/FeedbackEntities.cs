using GymBook.Contracts;

namespace GymBook.Api.Data;

// Feature requests and bug reports live on GitHub (see Feedback/FeedbackService.cs). What's kept here is only what
// GitHub can't know, since every ticket is sent there with the API's one token: who sent which, who wrote which comment,
// and who voted. Deleting an account deletes these with it.

/// <summary>A ticket a user sent: the GitHub issue's number, theirs to edit and delete.</summary>
public class FeedbackTicketOwner
{
    public int Number { get; set; }
    public string UserId { get; set; } = "";
    public FeedbackKind Kind { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>A comment a user wrote on a ticket: the GitHub comment's id.</summary>
public class FeedbackCommentOwner
{
    public long CommentId { get; set; }
    public int Number { get; set; }
    public string UserId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>A user's vote for a feature request.</summary>
public class FeedbackVote
{
    public int Number { get; set; }
    public string UserId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

using GymBook.Api.Data;
using GymBook.Api.Feedback;
using GymBook.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GymBook.Api.Controllers;

/// <summary>
/// Feature requests and bug reports (see <see cref="FeedbackService"/>): kept on GitHub, read and written here for the
/// app. Feature requests are everyone's to read, vote on and comment on; a bug report is its sender's alone. Only whoever
/// sent a ticket or comment can change or delete it.
/// </summary>
[ApiController]
[Route("api/feedback")]
public class FeedbackController(FeedbackService feedback, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>Attachments come as base64 in JSON, a third bigger than the file.</summary>
    const long MaxAttachmentRequestBytes = FeedbackLimits.AttachmentBase64Length + 64 * 1024;

    string Me => currentUser.UserId!;

    /// <summary>The API's own address, for links to attachments.</summary>
    string BaseUrl => $"{Request.Scheme}://{Request.Host}{Request.PathBase}";

    /// <summary>Every feature request (?kind=Feature), or the user's own bug reports (?kind=Bug).</summary>
    [HttpGet]
    public Task<ActionResult<FeedbackListResponse>> List([FromQuery] FeedbackKind kind, CancellationToken ct) =>
        RunAsync(() => feedback.ListAsync(kind, Me, BaseUrl, ct));

    [HttpGet("{number:int}")]
    public Task<ActionResult<FeedbackThread>> Get(int number, CancellationToken ct) =>
        RunAsync(() => feedback.GetAsync(number, Me, BaseUrl, ct));

    [HttpPost]
    [EnableRateLimiting(RateLimits.Sync)]
    public Task<ActionResult<FeedbackThread>> Create(SaveFeedbackRequest request, CancellationToken ct) =>
        RunAsync(() => feedback.CreateAsync(Me, request, BaseUrl, ct));

    [HttpPost("{number:int}")]
    [EnableRateLimiting(RateLimits.Sync)]
    public Task<ActionResult<FeedbackThread>> Update(int number, SaveFeedbackRequest request, CancellationToken ct) =>
        RunAsync(() => feedback.UpdateAsync(Me, number, request, BaseUrl, ct));

    [HttpPost("{number:int}/delete")]
    [EnableRateLimiting(RateLimits.Sync)]
    public async Task<IActionResult> Delete(int number, CancellationToken ct)
    {
        var result = await RunAsync(async () =>
        {
            await feedback.DeleteAsync(Me, number, ct);
            return true;
        });
        return result.Result ?? NoContent();
    }

    /// <summary>Votes for a feature request, or takes the vote back.</summary>
    [HttpPost("{number:int}/vote")]
    [EnableRateLimiting(RateLimits.Sync)]
    public Task<ActionResult<FeedbackVoteResponse>> Vote(int number, CancellationToken ct) =>
        RunAsync(() => feedback.VoteAsync(Me, number, ct));

    [HttpPost("{number:int}/comments")]
    [EnableRateLimiting(RateLimits.Sync)]
    public Task<ActionResult<FeedbackThread>> AddComment(int number, SaveFeedbackCommentRequest request, CancellationToken ct) =>
        RunAsync(() => feedback.AddCommentAsync(Me, number, request.Body, BaseUrl, ct));

    [HttpPost("comments/{id:long}")]
    [EnableRateLimiting(RateLimits.Sync)]
    public Task<ActionResult<FeedbackThread>> UpdateComment(long id, SaveFeedbackCommentRequest request, CancellationToken ct) =>
        RunAsync(() => feedback.UpdateCommentAsync(Me, id, request.Body, BaseUrl, ct));

    [HttpPost("comments/{id:long}/delete")]
    [EnableRateLimiting(RateLimits.Sync)]
    public Task<ActionResult<FeedbackThread>> DeleteComment(long id, CancellationToken ct) =>
        RunAsync(() => feedback.DeleteCommentAsync(Me, id, BaseUrl, ct));

    /// <summary>A file to put in a ticket or comment: returns the Markdown that shows it.</summary>
    [HttpPost("attachments")]
    [RequestSizeLimit(MaxAttachmentRequestBytes)]
    [EnableRateLimiting(RateLimits.Sync)]
    public Task<ActionResult<FeedbackAttachmentResponse>> Attach(FeedbackAttachmentRequest request, CancellationToken ct) =>
        RunAsync(() => feedback.AttachAsync(Me, request, BaseUrl, ct));

    /// <summary>
    /// An attachment. Without signing in, so the app's page can show pictures as it would any other: the random id in
    /// the address is what keeps it private.
    /// </summary>
    [HttpGet("files/{id}/{name}")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimits.Foods)]
    public async Task<IActionResult> Attachment(string id, string name, CancellationToken ct)
    {
        try
        {
            if (await feedback.FileAsync(id, name, ct) is not { } file)
                return NotFound();
            Response.Headers.CacheControl = "private, max-age=86400";
            return File(file.Bytes, file.ContentType);
        }
        catch (FeedbackException e)
        {
            return Problem(statusCode: e.Status, title: e.Message);
        }
    }

    async Task<ActionResult<T>> RunAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (FeedbackException e)
        {
            return Problem(statusCode: e.Status, title: e.Message);
        }
    }
}

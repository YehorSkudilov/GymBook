using GymBook.Api.Data;
using GymBook.Api.Plans;
using GymBook.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GymBook.Api.Controllers;

/// <summary>
/// AI plans (ChatGPT): follow-up questions, generating, importing, and chatting about a plan. Signed-in users only.
/// Every paid call is counted against the user's quota: plans and imports share one, chat messages have their own.
/// </summary>
[ApiController]
[Route("api/plans")]
[EnableRateLimiting(RateLimits.Sync)]
public class PlansController(OpenAiPlanGenerator generator, PlanQuota quota, LinkFetcher links, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>A photo or PDF of a plan, base64-encoded, plus the exercise list.</summary>
    const long MaxImportBytes = 12 * 1024 * 1024;

    string UserId => currentUser.UserId!;

    /// <summary>How many AI plans the user has left, for the Plans tab.</summary>
    [HttpGet("quota")]
    public Task<PlanQuotaResponse> Quota(CancellationToken ct) => quota.GetAsync(UserId, QuotaKind.Plan, ct);

    /// <summary>
    /// Follow-up questions for the wizard's answers, asked before the plan is generated. They don't count against the
    /// quota (they're cheap next to a plan), but are only offered while there's a plan left to generate with them.
    /// </summary>
    [HttpPost("questions")]
    public async Task<ActionResult<PlanQuestionsResponse>> Questions(PlanAnswers answers, CancellationToken ct)
    {
        if (!generator.IsConfigured)
            return Unavailable();
        var left = await quota.GetAsync(UserId, QuotaKind.Plan, ct);
        if (left.Remaining == 0)
            return UsedUp(left, "AI plans");
        try
        {
            return await generator.QuestionsAsync(answers, ct);
        }
        catch (PlanGenerationException e)
        {
            return Problem(statusCode: StatusCodes.Status502BadGateway, title: e.Message);
        }
    }

    /// <summary>Generates a plan with AI from the wizard's answers and the answers to the follow-up questions.</summary>
    [HttpPost("generate")]
    public Task<ActionResult<GeneratePlanResponse>> Generate(GeneratePlanRequest request, CancellationToken ct) =>
        Counted(QuotaKind.Plan, "AI plans", () => generator.GenerateAsync(request, ct), (plan, left) => plan.Quota = left, ct);

    /// <summary>
    /// Turns a plan in any format into a GymBook plan with AI: pasted text, a link (a web page, image or PDF, downloaded
    /// here) or a file. Counts as an AI plan.
    /// </summary>
    [HttpPost("import")]
    [RequestSizeLimit(MaxImportBytes)]
    public Task<ActionResult<GeneratePlanResponse>> Import(ImportPlanRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Text) && string.IsNullOrWhiteSpace(request.Link) && request.File == null)
            return Task.FromResult<ActionResult<GeneratePlanResponse>>(Problem(statusCode: StatusCodes.Status400BadRequest, title: "Add the plan as text, a link or a file."));
        if (request.Link is { Length: > 0 } url && !(Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http"))
            return Task.FromResult<ActionResult<GeneratePlanResponse>>(Problem(statusCode: StatusCodes.Status400BadRequest, title: "The link must be a web address (https://…)."));
        // The link is downloaded inside the counted call, so one that doesn't open doesn't use up a plan.
        return Counted(QuotaKind.Plan, "AI plans", async () =>
        {
            var link = string.IsNullOrWhiteSpace(request.Link) ? null : await links.FetchAsync(request.Link, ct);
            return await generator.ImportAsync(request, link, ct);
        }, (plan, left) => plan.Quota = left, ct);
    }

    /// <summary>A message to the plan's AI coach, which may change the plan. Counts against the chat quota.</summary>
    [HttpPost("chat")]
    public Task<ActionResult<PlanChatResponse>> Chat(PlanChatRequest request, CancellationToken ct) =>
        Counted(QuotaKind.Chat, "AI chat messages", () => generator.ChatAsync(request, ct), (reply, left) => reply.Quota = left, ct);

    /// <summary>
    /// Suggestions to improve the plan from how the user has been doing on it, each as edits the app can apply.
    /// Counts against the chat quota: it's one message to the same AI coach.
    /// </summary>
    [HttpPost("review")]
    public Task<ActionResult<PlanReviewResponse>> Review(PlanReviewRequest request, CancellationToken ct) =>
        Counted(QuotaKind.Chat, "AI coach messages", () => generator.ReviewAsync(request, ct), (review, left) => review.Quota = left, ct);

    /// <summary>Runs a paid AI call against the <paramref name="kind"/> quota: refused when it's used up, given back when the call fails.</summary>
    async Task<ActionResult<T>> Counted<T>(string kind, string what, Func<Task<T>> call, Action<T, PlanQuotaResponse> withQuota, CancellationToken ct)
    {
        if (!generator.IsConfigured)
            return Unavailable();
        if (await quota.TryReserveAsync(UserId, kind, ct) is not { } reservation)
            return UsedUp(await quota.GetAsync(UserId, kind, ct), what);

        T result;
        try
        {
            result = await call();
        }
        catch (Exception e)
        {
            // A call that produced nothing doesn't count against the quota.
            await quota.ReleaseAsync(reservation);
            if (e is PlanGenerationException)
                return Problem(statusCode: StatusCodes.Status502BadGateway, title: e.Message);
            throw;
        }
        withQuota(result, await quota.GetAsync(UserId, kind, ct));
        return result;
    }

    ObjectResult Unavailable() => Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "AI plans aren't available right now.");

    ObjectResult UsedUp(PlanQuotaResponse left, string what) =>
        Problem(statusCode: StatusCodes.Status429TooManyRequests, title: $"You've used all {left.Limit} {what} you get per {left.Period}.");
}

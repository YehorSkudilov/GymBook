using GymBook.Api.Data;
using GymBook.Api.Plans;
using GymBook.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GymBook.Api.Controllers;

[ApiController]
[Route("api/plans")]
[EnableRateLimiting(RateLimits.Sync)]
public class PlansController(OpenAiPlanGenerator generator, PlanQuota quota, ICurrentUser currentUser) : ControllerBase
{
    /// <summary>How many AI plans the user has left, for the Plans tab.</summary>
    [HttpGet("quota")]
    public Task<PlanQuotaResponse> Quota(CancellationToken ct) => quota.GetAsync(currentUser.UserId!, ct);

    /// <summary>Generates a plan with AI from the wizard's answers. Signed-in users only; each call costs money, so it's counted against a quota.</summary>
    [HttpPost("generate")]
    public async Task<ActionResult<GeneratePlanResponse>> Generate(GeneratePlanRequest request, CancellationToken ct)
    {
        if (!generator.IsConfigured)
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "AI plans aren't available right now.");

        var userId = currentUser.UserId!;
        if (await quota.TryReserveAsync(userId, ct) is not { } reservation)
        {
            var left = await quota.GetAsync(userId, ct);
            return Problem(statusCode: StatusCodes.Status429TooManyRequests,
                title: $"You've used all {left.Limit} AI plans you get per {left.Period}.");
        }

        GeneratePlanResponse plan;
        try
        {
            plan = await generator.GenerateAsync(request, ct);
        }
        catch (Exception e)
        {
            // A plan that never arrived doesn't count against the quota.
            await quota.ReleaseAsync(reservation);
            if (e is PlanGenerationException)
                return Problem(statusCode: StatusCodes.Status502BadGateway, title: e.Message);
            throw;
        }
        plan.Quota = await quota.GetAsync(userId, ct);
        return plan;
    }
}

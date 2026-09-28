using GymBook.Api.Plans;
using GymBook.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GymBook.Api.Controllers;

[ApiController]
[Route("api/plans")]
public class PlansController(OpenAiPlanGenerator generator) : ControllerBase
{
    /// <summary>Generates a plan with AI from the wizard's answers. Signed-in users only; each call costs money, so it's rate limited.</summary>
    [HttpPost("generate")]
    [EnableRateLimiting(RateLimits.PlanGeneration)]
    public async Task<ActionResult<GeneratePlanResponse>> Generate(GeneratePlanRequest request, CancellationToken ct)
    {
        if (!generator.IsConfigured)
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "AI plans aren't available right now.");
        try
        {
            return await generator.GenerateAsync(request, ct);
        }
        catch (PlanGenerationException e)
        {
            return Problem(statusCode: StatusCodes.Status502BadGateway, title: e.Message);
        }
    }
}

using GymBook.Api.Billing;
using GymBook.Api.Data;
using GymBook.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GymBook.Api.Controllers;

/// <summary>Gym Book Pro (see <see cref="Subscriptions"/>): the user's subscription, and recording a store purchase.</summary>
[ApiController]
[Route("api/subscriptions")]
[EnableRateLimiting(RateLimits.Sync)]
public class SubscriptionsController(Subscriptions subscriptions, MicrosoftStoreVerifier microsoft, ICurrentUser currentUser) : ControllerBase
{
    string UserId => currentUser.UserId!;

    [HttpGet]
    public Task<SubscriptionStatusResponse> Status(CancellationToken ct) => subscriptions.StatusAsync(UserId, ct);

    /// <summary>A purchase made (or restored) in the app, checked with its store and recorded for the account.</summary>
    [HttpPost("verify")]
    public async Task<ActionResult<SubscriptionStatusResponse>> Verify(VerifyPurchaseRequest request, CancellationToken ct)
    {
        try
        {
            return await subscriptions.VerifyAsync(UserId, request, ct);
        }
        catch (BillingException e)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: e.Message);
        }
        catch (HttpRequestException)
        {
            return Problem(statusCode: StatusCodes.Status502BadGateway, title: "Couldn't reach the store to check the purchase. Try again in a moment.");
        }
    }

    /// <summary>For the Windows app: what it makes the purchase ID key it sends to <see cref="Verify"/> with.</summary>
    [HttpGet("microsoft-ticket")]
    public async Task<ActionResult<MicrosoftStoreTicketResponse>> MicrosoftTicket(CancellationToken ct)
    {
        if (!microsoft.IsConfigured)
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Microsoft Store purchases aren't available right now.");
        try
        {
            return new MicrosoftStoreTicketResponse { ServiceTicket = await microsoft.ServiceTicketAsync(ct), PublisherUserId = BillingIds.AccountToken(UserId) };
        }
        catch (HttpRequestException)
        {
            return Problem(statusCode: StatusCodes.Status502BadGateway, title: "Couldn't reach the Microsoft Store. Try again in a moment.");
        }
    }
}

using GymBook.Api.Data;
using GymBook.Api.Sync;
using GymBook.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Controllers;

[ApiController]
[Route("api/sync")]
public class SyncController(ApiDbContext db, SyncProcessor sync, ICurrentUser currentUser, SyncNotifier notifier,
    GymBook.Api.Social.PlanShares shares, GymBook.Api.Social.RankCalculator ranks) : ControllerBase
{
    /// <summary>A full batch of large sessions fits comfortably; anything bigger is rejected before it's buffered.</summary>
    const long MaxRequestBytes = 10 * 1024 * 1024;

    [HttpPost]
    [RequestSizeLimit(MaxRequestBytes)]
    [EnableRateLimiting(RateLimits.Sync)]
    public async Task<ActionResult<SyncResponse>> Sync(SyncRequest request, CancellationToken ct)
    {
        for (var attempt = 0; attempt < 3; attempt++)
        {
            // The token may outlive the account by a few minutes after deletion.
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == currentUser.UserId, ct);
            if (user == null)
                return Unauthorized();
            try
            {
                var response = await sync.ApplyAsync(user, request, ct);
                // Something was pushed: the user's other devices that are connected live sync now (see SyncHub).
                if (request.Changes.Count > 0)
                {
                    await notifier.ChangedAsync(user.Id, response.Cursor, Request.Headers[SyncHub.ConnectionHeader].FirstOrDefault());
                    // A shared plan's change written into the copies of everyone else in the share (see PlanShares).
                    foreach (var (other, cursor) in shares.Changed.Where(c => c.Key != user.Id))
                        await notifier.ChangedAsync(other, cursor, null);
                    if (request.Changes.Sessions.Count > 0 || request.Changes.BodyWeights.Count > 0 || request.Changes.Profile != null)
                        await ranks.RefreshQuietlyAsync(user.Id, ct);
                }
                return response;
            }
            catch (SyncLimitException e)
            {
                return Problem(statusCode: StatusCodes.Status422UnprocessableEntity, title: e.Message);
            }
            catch (DbUpdateException)
            {
                // Another sync for this user won the race (version or key clash); start over on fresh data.
                db.ChangeTracker.Clear();
            }
        }
        return Problem(statusCode: StatusCodes.Status409Conflict, title: "Another sync is in progress. Try again.");
    }
}

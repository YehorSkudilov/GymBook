using GymBook.Api.Auth;
using GymBook.Api.Data;
using GymBook.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Admin;

/// <summary>
/// Reports from users about someone's ranked lifts or public profile, and taking a user off the leaderboards. Hiding
/// keeps them ranked privately (they still see their own tiers) but nobody else sees them on a board.
/// </summary>
[ApiController]
[Route("api/admin/ranks")]
[Authorize(Policy = AuthPolicies.AdminAccess)]
public class AdminRanksController(ApiDbContext db, TimeProvider time, ILogger<AdminRanksController> log) : ControllerBase
{
    /// <summary>Open reports, newest first; with <paramref name="all"/>, resolved ones too.</summary>
    [HttpGet("reports")]
    public async Task<List<RankReportRow>> Reports([FromQuery] bool all, CancellationToken ct)
    {
        var query = db.RankReports.AsNoTracking().Where(r => all || r.ResolvedAt == null);
        var rows = await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(300)
            .Select(r => new
            {
                r.Id,
                Reporter = db.SocialProfiles.Where(p => p.UserId == r.ReporterId).Select(p => p.Username).FirstOrDefault() ?? "",
                r.TargetId,
                Target = db.SocialProfiles.Where(p => p.UserId == r.TargetId).Select(p => new { p.Username, p.RankHidden }).FirstOrDefault(),
                TargetEmail = db.Users.Where(u => u.Id == r.TargetId).Select(u => u.Email).FirstOrDefault() ?? "",
                r.Lift,
                r.Reason,
                r.CreatedAt,
                Open = db.RankReports.Count(o => o.TargetId == r.TargetId && o.ResolvedAt == null),
            })
            .ToListAsync(ct);
        return [.. rows.Select(r => new RankReportRow(r.Id, r.Reporter, r.TargetId, r.Target?.Username ?? "", r.TargetEmail,
            r.Lift?.ToString(), r.Reason, r.CreatedAt, r.Target?.RankHidden ?? false, r.Open))];
    }

    /// <summary>Closes every open report about the user of report <paramref name="id"/>.</summary>
    [HttpPost("reports/{id:guid}/resolve")]
    public async Task<IActionResult> Resolve(Guid id, CancellationToken ct)
    {
        var target = await db.RankReports.Where(r => r.Id == id).Select(r => r.TargetId).FirstOrDefaultAsync(ct);
        if (target == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "No such report.");
        var now = time.GetUtcNow();
        await db.RankReports.Where(r => r.TargetId == target && r.ResolvedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(r => r.ResolvedAt, now), ct);
        return NoContent();
    }

    [HttpPost("users/{userId}/hidden")]
    public async Task<IActionResult> SetHidden(string userId, SetRankHiddenRequest request, CancellationToken ct)
    {
        var profile = await db.SocialProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "That user has no public profile.");
        profile.RankHidden = request.Hidden;
        await db.SaveChangesAsync(ct);
        log.LogInformation("{Admin} {Action} {User} on the leaderboards", User.FindFirst("sub")?.Value, request.Hidden ? "hid" : "unhid", userId);
        return NoContent();
    }

    /// <summary>Clears a user's bio, home gym and picture, e.g. after a report about something offensive in them.</summary>
    [HttpPost("users/{userId}/clear-profile")]
    public async Task<IActionResult> ClearProfile(string userId, CancellationToken ct)
    {
        var profile = await db.SocialProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (profile == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "That user has no public profile.");
        profile.Bio = "";
        profile.HomeGym = "";
        profile.AvatarVersion = 0;
        await db.Avatars.Where(a => a.UserId == userId).ExecuteDeleteAsync(ct);
        await db.SaveChangesAsync(ct);
        log.LogInformation("{Admin} cleared the public profile of {User}", User.FindFirst("sub")?.Value, userId);
        return NoContent();
    }
}

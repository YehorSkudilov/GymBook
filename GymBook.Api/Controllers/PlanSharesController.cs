using GymBook.Api.Data;
using GymBook.Api.Social;
using GymBook.Api.Sync;
using GymBook.Contracts;
using GymBook.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Controllers;

/// <summary>
/// Shared plans (see <see cref="PlanShares"/>): the owner shares a plan publicly on the web and/or with members by
/// username, each an editor or a viewer; members accept or leave; anyone who can see a plan may save their own copy
/// when the owner allows it.
/// </summary>
[ApiController]
[Route("api/plan-shares")]
public class PlanSharesController(ApiDbContext db, ICurrentUser currentUser, PlanShares shares, SyncNotifier notifier) : ControllerBase
{
    string Me => currentUser.UserId!;

    [HttpPost]
    [EnableRateLimiting(RateLimits.Sync)]
    public Task<ActionResult<PlanShareResponse>> Create(CreatePlanShareRequest request, CancellationToken ct) => RunAsync(async () =>
    {
        var share = await shares.ShareAsync(Me, request.PlanId, ct);
        await SaveAsync(ct);
        return await ResponseAsync(share, ct);
    });

    [HttpGet("{id}")]
    public Task<ActionResult<PlanShareResponse>> Get(string id, CancellationToken ct) => RunAsync(async () =>
    {
        var share = await FindAsync(id, ct);
        if (await shares.RoleAsync(share, Me, ct) == null)
            throw Missing();
        return await ResponseAsync(share, ct);
    });

    [HttpPost("{id}/settings")]
    public Task<ActionResult<PlanShareResponse>> Settings(string id, UpdatePlanShareRequest request, CancellationToken ct) => RunAsync(async () =>
    {
        var share = await OwnedAsync(id, ct);
        share.IsPublic = request.IsPublic;
        share.AllowCopy = request.AllowCopy;
        await SaveAsync(ct);
        return await ResponseAsync(share, ct);
    });

    [HttpPost("{id}/invite")]
    [EnableRateLimiting(RateLimits.Sync)]
    public Task<ActionResult<PlanShareResponse>> Invite(string id, ShareInviteRequest request, CancellationToken ct) => RunAsync(async () =>
    {
        var share = await OwnedAsync(id, ct);
        if (!await db.SocialProfiles.AnyAsync(p => p.UserId == Me, ct))
            throw new SocialException(409, "Pick a username first, so people know who's sharing with them.");
        var userId = await UserIdAsync(request.Username, ct);
        await shares.InviteAsync(share, userId, request.Role, ct);
        await SaveAsync(ct);
        return await ResponseAsync(share, ct);
    });

    [HttpPost("{id}/members/role")]
    public Task<ActionResult<PlanShareResponse>> SetRole(string id, ShareInviteRequest request, CancellationToken ct) => RunAsync(async () =>
    {
        var share = await OwnedAsync(id, ct);
        await shares.SetRoleAsync(await MembershipAsync(share.Id, await UserIdAsync(request.Username, ct), ct), request.Role, ct);
        await SaveAsync(ct);
        return await ResponseAsync(share, ct);
    });

    [HttpPost("{id}/members/remove")]
    public Task<ActionResult<PlanShareResponse>> RemoveMember(string id, UsernameRequest request, CancellationToken ct) => RunAsync(async () =>
    {
        var share = await OwnedAsync(id, ct);
        await shares.RemoveMemberAsync(await MembershipAsync(share.Id, await UserIdAsync(request.Username, ct), ct), ct);
        await SaveAsync(ct);
        return await ResponseAsync(share, ct);
    });

    /// <summary>Stops sharing: the link stops working and every member keeps their copy as their own plan.</summary>
    [HttpPost("{id}/stop")]
    public async Task<IActionResult> Stop(string id, CancellationToken ct)
    {
        var result = await RunAsync(async () =>
        {
            await shares.StopAsync(await OwnedAsync(id, ct), ct);
            await SaveAsync(ct);
            return true;
        });
        return result.Result ?? NoContent();
    }

    /// <summary>Plans shared with the user that they haven't accepted yet.</summary>
    [HttpGet("incoming")]
    public async Task<List<PlanShareInvite>> Incoming(CancellationToken ct)
    {
        var pending = await db.PlanShareMembers.AsNoTracking()
            .Where(m => m.UserId == Me && m.PlanId == null)
            .Join(db.PlanShares, m => m.ShareId, s => s.Id, (m, s) => new { m.Role, Share = s })
            .ToListAsync(ct);
        var list = new List<PlanShareInvite>();
        foreach (var p in pending)
        {
            if (await shares.OwnerPlanAsync(p.Share, ct) is not { } plan)
                continue;
            var owner = await db.SocialProfiles.AsNoTracking().FirstOrDefaultAsync(o => o.UserId == p.Share.OwnerId, ct);
            list.Add(new PlanShareInvite
            {
                ShareId = p.Share.Id,
                PlanName = plan.Name,
                Owner = owner?.Username ?? "",
                OwnerAvatarPath = owner == null ? null : SocialController.AvatarPath(owner.Username, owner.AvatarVersion),
                Role = p.Role,
                Workouts = plan.Workouts.Count,
            });
        }
        return list;
    }

    /// <summary>Accepts an invite: a linked copy of the plan is added to the user's plans and syncs to their devices.</summary>
    [HttpPost("{id}/accept")]
    public Task<ActionResult<PlanShareResponse>> Accept(string id, CancellationToken ct) => RunAsync(async () =>
    {
        var share = await FindAsync(id, ct);
        await shares.AcceptAsync(await MembershipAsync(share.Id, Me, ct), ct);
        await SaveAsync(ct);
        return await ResponseAsync(share, ct);
    });

    /// <summary>Declines an invite, or leaves a plan; a copy already made stays as the user's own plan.</summary>
    [HttpPost("{id}/leave")]
    public async Task<IActionResult> Leave(string id, CancellationToken ct)
    {
        var result = await RunAsync(async () =>
        {
            var share = await FindAsync(id, ct);
            await shares.RemoveMemberAsync(await MembershipAsync(share.Id, Me, ct), ct);
            await SaveAsync(ct);
            return true;
        });
        return result.Result ?? NoContent();
    }

    /// <summary>
    /// The plan as it reads. Without signing in for a public share (the web page uses it); otherwise for its owner and
    /// members too.
    /// </summary>
    [HttpGet("{id}/view")]
    [AllowAnonymous]
    public Task<ActionResult<SharedPlanView>> View(string id, CancellationToken ct) => RunAsync(async () =>
    {
        var share = await FindAsync(id, ct);
        var role = await shares.RoleAsync(share, User.FindFirst("sub")?.Value, ct);
        if (!share.IsPublic && role == null)
            throw Missing();
        var view = await shares.ViewAsync(share, ct) ?? throw Missing();
        view.MyRole = role;
        view.AllowCopy = share.AllowCopy || role == PlanShareRole.Owner;
        if (await db.SocialProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == share.OwnerId, ct) is { } owner)
            view.Owner = new ProfileCard
            {
                Username = owner.Username,
                Bio = owner.Bio,
                HomeGym = owner.HomeGym,
                AvatarPath = SocialController.AvatarPath(owner.Username, owner.AvatarVersion),
            };
        return view;
    });

    /// <summary>Saves the user's own copy of the plan, not linked to the share: theirs to change as they like.</summary>
    [HttpPost("{id}/copy")]
    [EnableRateLimiting(RateLimits.Sync)]
    public Task<ActionResult<CopyPlanResponse>> Copy(string id, CancellationToken ct) => RunAsync(async () =>
    {
        var share = await FindAsync(id, ct);
        var role = await shares.RoleAsync(share, Me, ct);
        if (!share.IsPublic && role == null)
            throw Missing();
        if (!share.AllowCopy && role != PlanShareRole.Owner)
            throw new SocialException(403, "The owner of this plan doesn't allow copies.");
        var plan = await shares.CopyAsync(share, Me, ct);
        await SaveAsync(ct);
        return new CopyPlanResponse { PlanId = plan.Id, Name = plan.Name };
    });

    // ---- Helpers ----

    static SocialException Missing() => new(404, "That shared plan doesn't exist, or it's no longer shared.");

    async Task<PlanShare> FindAsync(string id, CancellationToken ct) =>
        await db.PlanShares.FirstOrDefaultAsync(s => s.Id == id, ct) ?? throw Missing();

    async Task<PlanShare> OwnedAsync(string id, CancellationToken ct)
    {
        var share = await FindAsync(id, ct);
        return share.OwnerId == Me ? share : throw new SocialException(403, "Only the plan's owner can do that.");
    }

    async Task<string> UserIdAsync(string username, CancellationToken ct)
    {
        var key = username.Trim().TrimStart('@').ToLowerInvariant();
        return await db.SocialProfiles.Where(p => p.UsernameKey == key).Select(p => p.UserId).FirstOrDefaultAsync(ct)
            ?? throw new SocialException(404, "Nobody has that username.");
    }

    async Task<PlanShareMembership> MembershipAsync(string shareId, string userId, CancellationToken ct) =>
        await db.PlanShareMembers.FirstOrDefaultAsync(m => m.ShareId == shareId && m.UserId == userId, ct)
            ?? throw new SocialException(404, "They aren't in this plan.");

    /// <summary>Saves, then tells the devices of everyone whose plans changed to sync.</summary>
    async Task SaveAsync(CancellationToken ct)
    {
        await db.SaveChangesAsync(ct);
        foreach (var (userId, cursor) in shares.Changed)
            await notifier.ChangedAsync(userId, cursor, null);
    }

    async Task<PlanShareResponse> ResponseAsync(PlanShare share, CancellationToken ct)
    {
        var plan = await shares.OwnerPlanAsync(share, ct);
        var owner = await db.SocialProfiles.AsNoTracking().Where(p => p.UserId == share.OwnerId).Select(p => p.Username).FirstOrDefaultAsync(ct);
        var members = await db.PlanShareMembers.AsNoTracking()
            .Where(m => m.ShareId == share.Id)
            .Join(db.SocialProfiles, m => m.UserId, p => p.UserId, (m, p) => new { m.Role, m.PlanId, p.Username, p.AvatarVersion })
            .OrderBy(m => m.Username)
            .ToListAsync(ct);
        return new PlanShareResponse
        {
            Id = share.Id,
            PlanName = plan?.Name ?? "",
            IsPublic = share.IsPublic,
            AllowCopy = share.AllowCopy,
            PublicUrl = share.IsPublic ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}/p/{share.Id}" : null,
            Owner = owner ?? "",
            MyRole = await shares.RoleAsync(share, Me, ct) ?? PlanShareRole.Viewer,
            Members = [.. members.Select(m => new PlanShareMember
            {
                Username = m.Username,
                AvatarPath = SocialController.AvatarPath(m.Username, m.AvatarVersion),
                Role = m.Role,
                Joined = m.PlanId != null,
            })],
        };
    }

    /// <summary>Runs an operation, turning refusals into problem responses and a race with a sync into "try again".</summary>
    async Task<ActionResult<T>> RunAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            return await operation();
        }
        catch (SocialException e)
        {
            return Problem(statusCode: e.Status, title: e.Message);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "Someone in this plan was syncing at the same moment. Try again.");
        }
    }
}

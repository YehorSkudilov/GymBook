using GymBook.Api.Auth;
using GymBook.Api.Data;
using GymBook.Api.Plans;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Admin;

/// <summary>
/// Every account, what it trains, and what an admin can do about it. Admins can't act on themselves (so nobody locks
/// themselves out or removes the last SuperAdmin), and only a SuperAdmin can act on another admin's account.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Policy = AuthPolicies.AdminAccess)]
public class AdminUsersController(
    ApiDbContext db,
    UserManager<AppUser> users,
    TokenService tokens,
    PlanQuota quota,
    PlanQuotaSettings quotas,
    EmailSender email,
    TimeProvider time,
    ILogger<AdminUsersController> log) : ControllerBase
{
    const int ListLimit = 300;

    /// <summary>Newest first. <paramref name="q"/> matches the email or profile name; <paramref name="filter"/> is admins, disabled or unverified.</summary>
    [HttpGet]
    public async Task<List<UserRow>> List([FromQuery] string? q, [FromQuery] string? filter, CancellationToken ct)
    {
        IQueryable<AppUser> query = db.Users;
        if (!string.IsNullOrWhiteSpace(q))
        {
            var pattern = $"%{q.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(u => EF.Functions.ILike(u.Email!, pattern)
                || db.Profiles.IgnoreQueryFilters().Any(p => EF.Property<string>(p, ApiDbContext.UserIdColumn) == u.Id && EF.Functions.ILike(p.Name, pattern)));
        }
        query = filter switch
        {
            "admins" => query.Where(u => u.AdminRole != null),
            "disabled" => query.Where(u => u.LockoutEnd >= AdminQueries.DisabledThreshold),
            "unverified" => query.Where(u => !u.EmailConfirmed),
            _ => query,
        };
        return await AdminQueries.RowsAsync(db, query, time.GetUtcNow(), ListLimit, ct);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDetail>> Get(string id, CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (user == null)
            return NotFoundProblem();

        var now = time.GetUtcNow();
        var row = (await AdminQueries.RowsAsync(db, db.Users.Where(u => u.Id == id), now, 1, ct))[0];
        var profile = await db.Profiles.OwnedBy(id).FirstOrDefaultAsync(ct);
        var plans = await db.Plans.OwnedBy(id).Where(p => !p.IsDeleted).OrderByDescending(p => p.CreatedAt).ToListAsync(ct);
        var workouts = await db.Sessions.OwnedBy(id).Where(s => !s.IsDeleted && s.EndedAt != null).OrderByDescending(s => s.StartedAt).ToListAsync(ct);
        var bodyWeights = await db.BodyWeights.OwnedBy(id).Where(b => !b.IsDeleted).OrderByDescending(b => b.Date).Select(b => b.WeightKg).ToListAsync(ct);
        var customExercises = await db.CustomExercises.OwnedBy(id).CountAsync(e => !e.IsDeleted, ct);
        var activeSessions = await db.RefreshTokens
            .Where(t => t.UserId == id && t.RevokedAt == null && t.ExpiresAt > now)
            .Select(t => t.FamilyId)
            .Distinct()
            .CountAsync(ct);

        var d30 = now.AddDays(-30);
        var planQuota = await quota.GetAsync(id, QuotaKind.Plan, ct);
        var chatQuota = await quota.GetAsync(id, QuotaKind.Chat, ct);
        var ai = new AiUsage(
            new QuotaUse(planQuota.Limit - planQuota.Remaining, planQuota.Limit, planQuota.Period),
            new QuotaUse(chatQuota.Limit - chatQuota.Remaining, chatQuota.Limit, chatQuota.Period),
            await db.PlanGenerations.CountAsync(g => g.UserId == id && g.Kind == QuotaKind.Plan && g.CreatedAt > d30, ct),
            await db.PlanGenerations.CountAsync(g => g.UserId == id && g.Kind == QuotaKind.Chat && g.CreatedAt > d30, ct));

        var wall30 = AdminQueries.WallClockDaysAgo(30);
        var thisWeek = DateOnly.FromDateTime(DateTime.Now.Date.AddDays(-(((int)DateTime.Now.DayOfWeek + 6) % 7)));
        var weekCounts = workouts
            .GroupBy(w => DateOnly.FromDateTime(w.StartedAt.Date.AddDays(-(((int)w.StartedAt.DayOfWeek + 6) % 7))))
            .ToDictionary(g => g.Key, g => g.Count());
        var stats = new TrainingStats(
            Workouts: workouts.Count,
            Workouts30d: workouts.Count(w => w.StartedAt >= wall30),
            WorkingSets: workouts.Sum(w => w.WorkingSets.Count()),
            VolumeKg: Math.Round(workouts.Sum(Volume)),
            TotalHours: Math.Round(workouts.Sum(w => w.Duration.TotalHours), 1),
            FirstWorkoutAt: workouts.LastOrDefault()?.StartedAt,
            LastWorkoutAt: workouts.FirstOrDefault()?.StartedAt,
            CustomExercises: customExercises,
            BodyWeightEntries: bodyWeights.Count,
            LatestBodyWeightKg: bodyWeights.Count > 0 ? bodyWeights[0] : null,
            // The last 12 weeks, Monday to Sunday, oldest first.
            WorkoutsByWeek: Enumerable.Range(0, 12)
                .Select(i => thisWeek.AddDays((i - 11) * 7))
                .Select(w => new WeekCount(w, weekCounts.GetValueOrDefault(w)))
                .ToList());

        return new UserDetail(
            row,
            user.PasswordHash != null,
            activeSessions,
            profile == null ? null : new ProfileInfo(
                profile.Name,
                profile.Goal.ToString(),
                profile.Experience.ToString(),
                profile.DaysPerWeek,
                profile.SessionMinutes,
                profile.EquipmentAccess.ToString(),
                profile.Unit.ToString(),
                profile.BodyWeightKg,
                profile.BirthYear,
                profile.BodyFatPercent,
                profile.TrainingSince,
                profile.OnboardingDone,
                profile.UpdatedAt),
            stats,
            plans.Select(p => new PlanRow(
                p.Id,
                p.Name,
                p.Goal.ToString(),
                p.DaysPerWeek,
                p.Workouts.Count,
                p.Workouts.Sum(w => w.Exercises.Count),
                p.Id == profile?.ActivePlanId,
                p.CreatedAt)).ToList(),
            workouts.Take(20).Select(w => new WorkoutRow(
                w.Id,
                w.Name,
                w.StartedAt,
                w.EndedAt,
                w.Exercises.Count,
                w.WorkingSets.Count(),
                Math.Round(Volume(w)),
                w.PlanWeek)).ToList(),
            ai);
    }

    /// <summary>Disables (or re-enables) the account. Disabling also signs it out everywhere.</summary>
    [HttpPost("{id}/disabled")]
    public async Task<IActionResult> SetDisabled(string id, SetDisabledRequest request, CancellationToken ct)
    {
        var (user, error) = await FindTargetAsync(id);
        if (user == null)
            return error!;

        if (request.Disabled)
        {
            await users.SetLockoutEnabledAsync(user, true);
            await users.SetLockoutEndDateAsync(user, AccountStatus.DisabledUntil);
            await tokens.RevokeAllAsync(user.Id, ct);
        }
        else
        {
            await users.SetLockoutEndDateAsync(user, null);
            await users.ResetAccessFailedCountAsync(user);
        }
        log.LogInformation("Admin {Admin} {Action} account {User}", CallerId, request.Disabled ? "disabled" : "enabled", user.Id);
        return NoContent();
    }

    /// <summary>Marks the email verified or not; the user's app picks it up at its next token refresh.</summary>
    [HttpPost("{id}/email-verified")]
    public async Task<IActionResult> SetEmailVerified(string id, SetEmailVerifiedRequest request)
    {
        var (user, error) = await FindTargetAsync(id);
        if (user == null)
            return error!;

        user.EmailConfirmed = request.Verified;
        await users.UpdateAsync(user);
        log.LogInformation("Admin {Admin} set email verified={Verified} on {User}", CallerId, request.Verified, user.Id);
        return NoContent();
    }

    /// <summary>Signs the account out on every device (each gets signed out at its next token refresh).</summary>
    [HttpPost("{id}/sign-out")]
    public async Task<IActionResult> SignOutEverywhere(string id, CancellationToken ct)
    {
        var (user, error) = await FindTargetAsync(id);
        if (user == null)
            return error!;

        await tokens.RevokeAllAsync(user.Id, ct);
        log.LogInformation("Admin {Admin} signed out {User} everywhere", CallerId, user.Id);
        return NoContent();
    }

    /// <summary>Gives back the AI plans and chat messages used in the current quota windows.</summary>
    [HttpPost("{id}/ai-quota/reset")]
    public async Task<IActionResult> ResetAiQuota(string id, CancellationToken ct)
    {
        var (user, error) = await FindTargetAsync(id);
        if (user == null)
            return error!;

        var now = time.GetUtcNow();
        var planSince = now - quotas.Plan.Window;
        var chatSince = now - quotas.Chat.Window;
        await db.PlanGenerations
            .Where(g => g.UserId == user.Id
                && ((g.Kind == QuotaKind.Plan && g.CreatedAt > planSince) || (g.Kind == QuotaKind.Chat && g.CreatedAt > chatSince)))
            .ExecuteDeleteAsync(ct);
        log.LogInformation("Admin {Admin} reset the AI quota of {User}", CallerId, user.Id);
        return NoContent();
    }

    /// <summary>Emails the user a password reset code, the same one "Forgot password?" in the app sends.</summary>
    [HttpPost("{id}/password-reset-email")]
    public async Task<IActionResult> SendPasswordReset(string id, CancellationToken ct)
    {
        var (user, error) = await FindTargetAsync(id);
        if (user == null)
            return error!;
        if (!email.IsAvailable || user.Email == null)
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Email isn't set up on the server.");

        var code = await users.GeneratePasswordResetTokenAsync(user);
        bool sent;
        try
        {
            sent = await email.SendCodeAsync(user.Email, "reset-password", new CodeEmail(
                "Your Gym Book password reset code",
                "Reset your password",
                "Open Gym Book, tap \"Forgot password?\" on the sign-in screen, enter your email and this code, then choose a new password.",
                code,
                "Gym Book support sent this at your request. If you didn't ask for it, you can ignore this email. Your password stays as it is."), ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogError(e, "Couldn't send a password reset email");
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Couldn't send the email. Please try again later.");
        }
        if (!sent)
            return Problem(statusCode: StatusCodes.Status429TooManyRequests, title: "A code was sent to this address very recently. Try again in a minute.");
        log.LogInformation("Admin {Admin} sent a password reset email to {User}", CallerId, user.Id);
        return NoContent();
    }

    /// <summary>Makes the account an Admin, a SuperAdmin, or (null) a regular user.</summary>
    [HttpPost("{id}/role")]
    [Authorize(Policy = AuthPolicies.SuperAdminOnly)]
    public async Task<IActionResult> SetRole(string id, SetRoleRequest request, CancellationToken ct)
    {
        if (request.Role != null && !AdminRoles.IsValid(request.Role))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: $"Role must be {AdminRoles.Admin}, {AdminRoles.SuperAdmin} or none.");
        var (user, error) = await FindTargetAsync(id);
        if (user == null)
            return error!;

        user.AdminRole = request.Role;
        await users.UpdateAsync(user);
        log.LogInformation("Admin {Admin} set the role of {User} to {Role}", CallerId, user.Id, request.Role ?? "none");
        return NoContent();
    }

    /// <summary>Deletes the account and, by cascade, all of its data - the same as "Delete account" in the app.</summary>
    [HttpDelete("{id}")]
    [Authorize(Policy = AuthPolicies.SuperAdminOnly)]
    public async Task<IActionResult> Delete(string id)
    {
        var (user, error) = await FindTargetAsync(id);
        if (user == null)
            return error!;

        var result = await users.DeleteAsync(user);
        if (!result.Succeeded)
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Couldn't delete the account.");
        log.LogInformation("Admin {Admin} deleted account {User}", CallerId, user.Id);
        return NoContent();
    }

    string CallerId => User.FindFirst("sub")?.Value ?? "";

    /// <summary>The account to act on, or why it can't be.</summary>
    async Task<(AppUser? User, ActionResult? Error)> FindTargetAsync(string id)
    {
        if (id == CallerId)
            return (null, Problem(statusCode: StatusCodes.Status400BadRequest, title: "You can't do that to your own account."));
        var user = await users.FindByIdAsync(id);
        if (user == null)
            return (null, NotFoundProblem());
        if (user.AdminRole != null && !User.IsInRole(AdminRoles.SuperAdmin))
            return (null, Problem(statusCode: StatusCodes.Status403Forbidden, title: "Only a SuperAdmin can change another admin's account."));
        return (user, null);
    }

    ActionResult NotFoundProblem() => Problem(statusCode: StatusCodes.Status404NotFound, title: "User not found.");

    static double Volume(GymBook.Models.WorkoutSession w) => w.WorkingSets.Sum(s => s.WeightKg * s.Reps);
}

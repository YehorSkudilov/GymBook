using GymBook.Api.Auth;
using GymBook.Api.Data;
using GymBook.Api.Plans;
using GymBook.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Admin;

/// <summary>The admin app's dashboard: how many people use Gym Book and how much they train.</summary>
[ApiController]
[Route("api/admin/dashboard")]
[Authorize(Policy = AuthPolicies.AdminAccess)]
public class AdminDashboardController(ApiDbContext db, TimeProvider time) : ControllerBase
{
    const int ChartDays = 30;

    [HttpGet]
    public async Task<DashboardResponse> Get(CancellationToken ct)
    {
        var now = time.GetUtcNow();
        var d7 = now.AddDays(-7);
        var d30 = now.AddDays(-30);
        var wall7 = AdminQueries.WallClockDaysAgo(6);
        var wallChart = AdminQueries.WallClockDaysAgo(ChartDays - 1);

        var workouts = db.Sessions.AllUsers().Where(s => !s.IsDeleted && s.EndedAt != null);

        var signups = await db.Users.Where(u => u.CreatedAt > d30).Select(u => u.CreatedAt).ToListAsync(ct);
        var workoutDays = await workouts.Where(s => s.StartedAt >= wallChart).Select(s => s.StartedAt).ToListAsync(ct);

        var mostActive = await workouts
            .Where(s => s.StartedAt >= wall7)
            .GroupBy(s => EF.Property<string>(s, ApiDbContext.UserIdColumn))
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .Take(5)
            .ToListAsync(ct);
        var activeIds = mostActive.Select(m => m.UserId).ToList();
        var emails = await db.Users.Where(u => activeIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Email ?? "", ct);

        return new DashboardResponse(
            TotalUsers: await db.Users.CountAsync(ct),
            VerifiedUsers: await db.Users.CountAsync(u => u.EmailConfirmed, ct),
            NewUsers7d: await db.Users.CountAsync(u => u.CreatedAt > d7, ct),
            NewUsers30d: signups.Count,
            ActiveUsers7d: await db.RefreshTokens.Where(t => t.CreatedAt > d7).Select(t => t.UserId).Distinct().CountAsync(ct),
            DisabledUsers: await db.Users.CountAsync(u => u.LockoutEnd >= AdminQueries.DisabledThreshold, ct),
            Admins: await db.Users.CountAsync(u => u.AdminRole != null, ct),
            WorkoutsTotal: await workouts.CountAsync(ct),
            Workouts7d: await workouts.CountAsync(s => s.StartedAt >= wall7, ct),
            PlansTotal: await db.Plans.AllUsers().CountAsync(p => !p.IsDeleted, ct),
            AiPlans7d: await db.PlanGenerations.CountAsync(g => g.Kind == QuotaKind.Plan && g.CreatedAt > d7, ct),
            AiChats7d: await db.PlanGenerations.CountAsync(g => g.Kind == QuotaKind.Chat && g.CreatedAt > d7, ct),
            SignupsByDay: AdminQueries.ByDay(signups.Select(c => DateOnly.FromDateTime(c.ToLocalTime().DateTime)), ChartDays),
            WorkoutsByDay: AdminQueries.ByDay(workoutDays.Select(DateOnly.FromDateTime), ChartDays),
            RecentUsers: await AdminQueries.RowsAsync(db, db.Users, now, 8, ct),
            MostActive7d: mostActive.Select(m => new TopUser(m.UserId, emails.GetValueOrDefault(m.UserId, ""), m.Count)).ToList());
    }
}

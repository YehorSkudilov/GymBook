using GymBook.Api.Auth;
using GymBook.Api.Data;
using GymBook.Api.Plans;
using GymBook.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Admin;

/// <summary>AI plans and plan chat messages - each one a paid OpenAI call - over the last days, and who uses them most.</summary>
[ApiController]
[Route("api/admin/ai-usage")]
[Authorize(Policy = AuthPolicies.AdminAccess)]
public class AdminAiController(ApiDbContext db, PlanQuotaSettings quotas, TimeProvider time) : ControllerBase
{
    [HttpGet]
    public async Task<AiUsageResponse> Get([FromQuery] int days = 30, CancellationToken ct = default)
    {
        days = Math.Clamp(days, 7, 90);
        var since = time.GetUtcNow().AddDays(-days);
        var generations = await db.PlanGenerations
            .Where(g => g.CreatedAt > since)
            .Select(g => new { g.UserId, g.Kind, g.CreatedAt })
            .ToListAsync(ct);

        var byDay = generations
            .GroupBy(g => DateOnly.FromDateTime(g.CreatedAt.ToLocalTime().DateTime))
            .ToDictionary(g => g.Key, g => (Plans: g.Count(x => x.Kind == QuotaKind.Plan), Chats: g.Count(x => x.Kind == QuotaKind.Chat)));
        var today = DateOnly.FromDateTime(DateTime.Now);

        var top = generations
            .GroupBy(g => g.UserId)
            .Select(g => new { UserId = g.Key, Plans = g.Count(x => x.Kind == QuotaKind.Plan), Chats = g.Count(x => x.Kind == QuotaKind.Chat) })
            .OrderByDescending(g => g.Plans + g.Chats)
            .Take(10)
            .ToList();
        var topIds = top.Select(t => t.UserId).ToList();
        var emails = await db.Users.Where(u => topIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Email ?? "", ct);

        return new AiUsageResponse(
            Days: days,
            Plans: generations.Count(g => g.Kind == QuotaKind.Plan),
            Chats: generations.Count(g => g.Kind == QuotaKind.Chat),
            Users: generations.Select(g => g.UserId).Distinct().Count(),
            PlanQuota: new QuotaSetting(quotas.Plan.Limit, quotas.Plan.Period),
            ChatQuota: new QuotaSetting(quotas.Chat.Limit, quotas.Chat.Period),
            ByDay: Enumerable.Range(0, days)
                .Select(i => today.AddDays(i - days + 1))
                .Select(d => byDay.TryGetValue(d, out var c) ? new AiDay(d, c.Plans, c.Chats) : new AiDay(d, 0, 0))
                .ToList(),
            TopUsers: top.Select(t => new AiTopUser(t.UserId, emails.GetValueOrDefault(t.UserId, ""), t.Plans, t.Chats)).ToList());
    }
}

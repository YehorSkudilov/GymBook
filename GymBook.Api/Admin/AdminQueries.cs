using GymBook.Api.Auth;
using GymBook.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Admin;

/// <summary>
/// Reads across every account. The user-owned tables have a query filter that scopes them to the signed-in user, so
/// everything here goes through <see cref="EntityFrameworkQueryableExtensions.IgnoreQueryFilters{TEntity}(IQueryable{TEntity})"/>
/// and matches the owner column itself. Only the admin controllers may use it.
/// </summary>
public static class AdminQueries
{
    public static IQueryable<T> AllUsers<T>(this DbSet<T> set) where T : class => set.IgnoreQueryFilters();

    public static IQueryable<T> OwnedBy<T>(this DbSet<T> set, string userId) where T : class =>
        set.IgnoreQueryFilters().Where(e => EF.Property<string>(e, ApiDbContext.UserIdColumn) == userId);

    /// <summary>The users list's rows, newest first.</summary>
    public static async Task<List<UserRow>> RowsAsync(ApiDbContext db, IQueryable<AppUser> users, DateTimeOffset now, int take, CancellationToken ct)
    {
        var rows = await users
            .IgnoreQueryFilters()
            .OrderByDescending(u => u.CreatedAt)
            .Take(take)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.CreatedAt,
                u.EmailConfirmed,
                u.AdminRole,
                u.LockoutEnd,
                Name = db.Profiles.IgnoreQueryFilters()
                    .Where(p => EF.Property<string>(p, ApiDbContext.UserIdColumn) == u.Id)
                    .Select(p => p.Name)
                    .FirstOrDefault(),
                HasGoogle = db.UserLogins.Any(l => l.UserId == u.Id && l.LoginProvider == GoogleTokenVerifier.Provider),
                Workouts = db.Sessions.IgnoreQueryFilters()
                    .Count(s => EF.Property<string>(s, ApiDbContext.UserIdColumn) == u.Id && !s.IsDeleted && s.EndedAt != null),
                Plans = db.Plans.IgnoreQueryFilters()
                    .Count(p => EF.Property<string>(p, ApiDbContext.UserIdColumn) == u.Id && !p.IsDeleted),
                // A sign-in or token refresh happens at least every access token lifetime while the app is in use.
                LastActiveAt = db.RefreshTokens.Where(t => t.UserId == u.Id).Max(t => (DateTimeOffset?)t.CreatedAt),
            })
            .ToListAsync(ct);

        return rows.Select(r => new UserRow(
            r.Id,
            r.Email ?? "",
            string.IsNullOrWhiteSpace(r.Name) ? null : r.Name,
            r.CreatedAt,
            r.EmailConfirmed,
            r.HasGoogle,
            r.AdminRole,
            AccountStatus.Of(new AppUser { LockoutEnd = r.LockoutEnd }, now),
            r.Workouts,
            r.Plans,
            r.LastActiveAt)).ToList();
    }

    /// <summary>Accounts locked until the end of time (see <see cref="AccountStatus"/>).</summary>
    public static readonly DateTimeOffset DisabledThreshold = new(9999, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>Workout times are wall-clock local times stored without a zone, so compare them with the same kind.</summary>
    public static DateTime WallClockDaysAgo(int days) => DateTime.SpecifyKind(DateTime.Now.Date.AddDays(-days), DateTimeKind.Unspecified);

    /// <summary>One entry per day, oldest first, with zeros for the days without any.</summary>
    public static List<DayCount> ByDay(IEnumerable<DateOnly> dates, int days)
    {
        var counts = dates.GroupBy(d => d).ToDictionary(g => g.Key, g => g.Count());
        var today = DateOnly.FromDateTime(DateTime.Now);
        return Enumerable.Range(0, days)
            .Select(i => today.AddDays(i - days + 1))
            .Select(d => new DayCount(d, counts.GetValueOrDefault(d)))
            .ToList();
    }
}

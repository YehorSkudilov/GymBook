using System.Security.Cryptography;
using GymBook.Api.Admin;
using GymBook.Api.Data;
using GymBook.Contracts;
using GymBook.Models;
using GymBook.Services;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Social;

/// <summary>Refused for a reason the user can be told (<see cref="Exception.Message"/>), with the HTTP status to send.</summary>
public class SocialException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}

/// <summary>
/// Shared plans. The owner's plan and each member's linked copy are ordinary plans in each user's own data, synced as
/// usual and carrying <see cref="WorkoutPlan.ShareId"/>. When the owner or an editor changes theirs, the change is
/// written into every other copy (each user's sync version bumped, so their devices pull it); a viewer's changes are
/// put back. Only the workouts and how the plan is run are shared (<see cref="CopyStructure"/>); where each person is
/// in the plan stays their own. Leaving or being removed keeps the copy as the member's own plan.
/// </summary>
public class PlanShares(ApiDbContext db, TimeProvider time)
{
    /// <summary>Users whose data was changed by the last operation, for the caller to tell their devices after saving.</summary>
    public Dictionary<string, long> Changed { get; } = [];

    /// <summary>Forgets <see cref="Changed"/>, e.g. before a sync is retried.</summary>
    public void Reset() => Changed.Clear();

    /// <summary>The parts of a plan its members share: everything but where each of them is in it.</summary>
    public static void CopyStructure(WorkoutPlan from, WorkoutPlan to)
    {
        to.Name = from.Name;
        to.Description = from.Description;
        to.Goal = from.Goal;
        to.DaysPerWeek = from.DaysPerWeek;
        to.Workouts = [.. from.Workouts.Select(w => new PlanWorkout
        {
            Id = w.Id,
            Name = w.Name,
            Exercises = [.. w.Exercises.Select(Clone)],
        })];
        to.RestDays = from.RestDays is null ? null : [.. from.RestDays];
        to.UseRir = from.UseRir;
        to.Deloads = from.Deloads;
        to.Periodization = from.Periodization;
        to.OwnTraining = from.OwnTraining;
        to.TargetRir = from.TargetRir;
        to.Warmups = from.Warmups;
        to.OwnRest = from.OwnRest;
        to.CompoundRestSeconds = from.CompoundRestSeconds;
        to.IsolationRestSeconds = from.IsolationRestSeconds;
        to.WarmupRestSeconds = from.WarmupRestSeconds;
    }

    static PlanExercise Clone(PlanExercise e)
    {
        var copy = new PlanExercise();
        foreach (var p in typeof(PlanExercise).GetProperties().Where(p => p.CanRead && p.CanWrite))
            p.SetValue(copy, p.GetValue(e));
        return copy;
    }

    static bool SameStructure(WorkoutPlan a, WorkoutPlan b)
    {
        var x = new WorkoutPlan();
        var y = new WorkoutPlan();
        CopyStructure(a, x);
        CopyStructure(b, y);
        return System.Text.Json.JsonSerializer.Serialize(x, GymBook.Serialization.GymBookJson.Options)
            == System.Text.Json.JsonSerializer.Serialize(y, GymBook.Serialization.GymBookJson.Options);
    }

    // ---- During a sync ----

    /// <summary>
    /// After <paramref name="user"/>'s pushed plans were applied (tracked, stamped <paramref name="version"/>, not saved):
    /// spreads the owner's and editors' changes to everyone in the share, puts back a viewer's, and handles a shared plan
    /// being deleted. Plans whose stored copy now differs from what the device sent are dropped from
    /// <paramref name="accepted"/>, so the device gets them back.
    /// </summary>
    public async Task AfterSyncAsync(AppUser user, long version, HashSet<(Type, string)> accepted, CancellationToken ct)
    {
        var pushed = db.ChangeTracker.Entries<WorkoutPlan>()
            .Where(e => e.State is EntityState.Added or EntityState.Modified
                && (long)e.Property(ApiDbContext.VersionColumn).CurrentValue! == version
                && e.Entity.ShareId != null)
            .Select(e => e.Entity)
            .ToList();
        foreach (var plan in pushed)
        {
            var share = await db.PlanShares.FirstOrDefaultAsync(s => s.Id == plan.ShareId, ct);
            var membership = share == null || share.OwnerId == user.Id ? null
                : await db.PlanShareMembers.FirstOrDefaultAsync(m => m.ShareId == share.Id && m.UserId == user.Id, ct);
            var isOwner = share != null && share.OwnerId == user.Id && share.PlanId == plan.Id;
            if (!isOwner && membership?.PlanId != plan.Id)
            {
                // The share is gone, or this user isn't in it any more: it's their own plan now.
                Unlink(plan);
                accepted.Remove((typeof(WorkoutPlan), plan.Id));
                continue;
            }

            if (plan.IsDeleted)
            {
                if (isOwner)
                    await StopAsync(share!, ct);
                else
                    db.PlanShareMembers.Remove(membership!);
                Unlink(plan);
                continue;
            }

            if (membership is { Role: PlanShareRole.Viewer })
            {
                var source = await OwnerPlanAsync(share!, ct);
                if (source != null && !SameStructure(source, plan))
                {
                    CopyStructure(source, plan);
                    accepted.Remove((typeof(WorkoutPlan), plan.Id));
                }
                continue;
            }

            await SpreadAsync(share!, plan, user.Id, ct);
        }
    }

    /// <summary>Writes <paramref name="from"/>'s workouts into every other plan of the share.</summary>
    async Task SpreadAsync(PlanShare share, WorkoutPlan from, string fromUserId, CancellationToken ct)
    {
        var targets = new List<(string UserId, string PlanId)> { (share.OwnerId, share.PlanId) };
        var members = await db.PlanShareMembers
            .Where(m => m.ShareId == share.Id && m.PlanId != null)
            .Select(m => new { m.UserId, m.PlanId })
            .ToListAsync(ct);
        targets.AddRange(members.Select(m => (m.UserId, m.PlanId!)));
        foreach (var (userId, planId) in targets.Where(t => t.UserId != fromUserId))
        {
            var plan = await db.Plans.OwnedBy(userId).FirstOrDefaultAsync(p => p.Id == planId, ct);
            if (plan == null || plan.IsDeleted || SameStructure(from, plan))
                continue;
            CopyStructure(from, plan);
            await TouchAsync(userId, plan, ct);
            await CopyCustomExercisesAsync(fromUserId, userId, from, ct);
        }
    }

    // ---- Sharing ----

    public async Task<PlanShare> ShareAsync(string ownerId, string planId, CancellationToken ct)
    {
        var plan = await db.Plans.OwnedBy(ownerId).FirstOrDefaultAsync(p => p.Id == planId && !p.IsDeleted, ct)
            ?? throw new SocialException(404, "That plan isn't on your account yet. Wait for it to sync, then try again.");
        if (plan.ShareId != null)
        {
            var existing = await db.PlanShares.FirstOrDefaultAsync(s => s.Id == plan.ShareId, ct);
            if (existing?.OwnerId == ownerId)
                return existing;
            throw new SocialException(403, "Someone else shared this plan with you, so only they can share it. Save your own copy to share that.");
        }
        var share = new PlanShare { Id = NewShareId(), OwnerId = ownerId, PlanId = planId, CreatedAt = time.GetUtcNow() };
        db.PlanShares.Add(share);
        plan.ShareId = share.Id;
        plan.ShareRole = PlanShareRole.Owner;
        await TouchAsync(ownerId, plan, ct);
        await db.SaveChangesAsync(ct);
        return share;
    }

    /// <summary>Ends the share: every copy becomes its user's own plan.</summary>
    public async Task StopAsync(PlanShare share, CancellationToken ct)
    {
        var members = await db.PlanShareMembers.Where(m => m.ShareId == share.Id).ToListAsync(ct);
        foreach (var m in members)
            await UnlinkAsync(m.UserId, m.PlanId, ct);
        await UnlinkAsync(share.OwnerId, share.PlanId, ct);
        db.PlanShareMembers.RemoveRange(members);
        db.PlanShares.Remove(share);
    }

    public async Task InviteAsync(PlanShare share, string userId, PlanShareRole role, CancellationToken ct)
    {
        if (role == PlanShareRole.Owner)
            throw new SocialException(400, "A plan has one owner.");
        if (userId == share.OwnerId)
            throw new SocialException(400, "That's you.");
        var membership = await db.PlanShareMembers.FirstOrDefaultAsync(m => m.ShareId == share.Id && m.UserId == userId, ct);
        if (membership != null)
        {
            await SetRoleAsync(membership, role, ct);
            return;
        }
        if (await db.PlanShareMembers.CountAsync(m => m.ShareId == share.Id, ct) >= SocialLimits.MaxShareMembers)
            throw new SocialException(400, $"A plan can be shared with up to {SocialLimits.MaxShareMembers} people.");
        db.PlanShareMembers.Add(new PlanShareMembership { ShareId = share.Id, UserId = userId, Role = role, CreatedAt = time.GetUtcNow() });
    }

    public async Task SetRoleAsync(PlanShareMembership membership, PlanShareRole role, CancellationToken ct)
    {
        if (role == PlanShareRole.Owner)
            throw new SocialException(400, "A plan has one owner.");
        membership.Role = role;
        if (membership.PlanId != null && await db.Plans.OwnedBy(membership.UserId).FirstOrDefaultAsync(p => p.Id == membership.PlanId, ct) is { } plan)
        {
            plan.ShareRole = role;
            await TouchAsync(membership.UserId, plan, ct);
        }
    }

    /// <summary>The invited member's linked copy, made in their plans.</summary>
    public async Task AcceptAsync(PlanShareMembership membership, CancellationToken ct)
    {
        if (membership.PlanId != null)
            return;
        var share = await db.PlanShares.FirstAsync(s => s.Id == membership.ShareId, ct);
        var source = await OwnerPlanAsync(share, ct) ?? throw new SocialException(404, "That plan was deleted.");
        var plan = await AddPlanAsync(membership.UserId, source, share.OwnerId, ct);
        plan.ShareId = share.Id;
        plan.ShareRole = membership.Role;
        membership.PlanId = plan.Id;
    }

    /// <summary>Takes the member out of the share; their copy stays, as their own plan.</summary>
    public async Task RemoveMemberAsync(PlanShareMembership membership, CancellationToken ct)
    {
        await UnlinkAsync(membership.UserId, membership.PlanId, ct);
        db.PlanShareMembers.Remove(membership);
    }

    /// <summary>A copy of the share's plan in <paramref name="userId"/>'s own plans, not linked to the share.</summary>
    public async Task<WorkoutPlan> CopyAsync(PlanShare share, string userId, CancellationToken ct)
    {
        var source = await OwnerPlanAsync(share, ct) ?? throw new SocialException(404, "That plan was deleted.");
        var plan = await AddPlanAsync(userId, source, share.OwnerId, ct);
        if (userId == share.OwnerId)
            plan.Name = Trim($"{source.Name} (copy)", SyncLimits.NameLength);
        return plan;
    }

    /// <summary>The role <paramref name="userId"/> has in the share; null when they aren't in it (or haven't accepted).</summary>
    public async Task<PlanShareRole?> RoleAsync(PlanShare share, string? userId, CancellationToken ct)
    {
        if (userId == null)
            return null;
        if (userId == share.OwnerId)
            return PlanShareRole.Owner;
        return await db.PlanShareMembers.Where(m => m.ShareId == share.Id && m.UserId == userId).Select(m => (PlanShareRole?)m.Role).FirstOrDefaultAsync(ct);
    }

    public Task<WorkoutPlan?> OwnerPlanAsync(PlanShare share, CancellationToken ct) =>
        db.Plans.OwnedBy(share.OwnerId).FirstOrDefaultAsync(p => p.Id == share.PlanId && !p.IsDeleted, ct);

    /// <summary>The plan as it reads, with exercise names: the library's, or the owner's own exercises'.</summary>
    public async Task<SharedPlanView?> ViewAsync(PlanShare share, CancellationToken ct)
    {
        if (await OwnerPlanAsync(share, ct) is not { } plan)
            return null;
        var ids = plan.Workouts.SelectMany(w => w.Exercises).Select(e => e.ExerciseId).Distinct().ToList();
        var custom = await db.CustomExercises.OwnedBy(share.OwnerId).Where(e => ids.Contains(e.Id)).ToDictionaryAsync(e => e.Id, e => e.Name, ct);
        return new SharedPlanView
        {
            ShareId = share.Id,
            Name = plan.Name,
            Description = plan.Description,
            Goal = plan.Goal,
            DaysPerWeek = plan.DaysPerWeek,
            RestDays = plan.RestDays ?? [],
            AllowCopy = share.AllowCopy,
            UpdatedAt = plan.UpdatedAt,
            Workouts = [.. plan.Workouts.Select(w => new SharedWorkoutView
            {
                Name = w.Name,
                Exercises = [.. w.Exercises.Select(e => new SharedExerciseView
                {
                    ExerciseId = e.ExerciseId,
                    Name = custom.GetValueOrDefault(e.ExerciseId) ?? ExerciseLibrary.Find(e.ExerciseId)?.Name ?? Readable(e.ExerciseId),
                    Sets = e.Sets,
                    RepMin = e.RepMin,
                    RepMax = e.RepMax,
                    TargetRir = plan.UseRir ? e.TargetRir : null,
                    RestSeconds = e.RestSeconds,
                })],
            })],
        };
    }

    // ---- Helpers ----

    async Task<WorkoutPlan> AddPlanAsync(string userId, WorkoutPlan source, string sourceUserId, CancellationToken ct)
    {
        if (await db.Plans.OwnedBy(userId).CountAsync(ct) >= Sync.SyncProcessor.MaxRecordsPerKind)
            throw new SocialException(400, "You have too many plans.");
        var plan = new WorkoutPlan { CreatedAt = DateTime.Now, UpdatedAt = time.GetUtcNow() };
        CopyStructure(source, plan);
        await AddOwnedAsync(plan, userId, ct);
        await CopyCustomExercisesAsync(sourceUserId, userId, source, ct);
        return plan;
    }

    /// <summary>
    /// Adds a record to <paramref name="userId"/>'s data. Ownership is part of the key, so it's set before EF starts
    /// tracking the record (as SyncProcessor does).
    /// </summary>
    async Task AddOwnedAsync<T>(T record, string userId, CancellationToken ct) where T : class
    {
        var entry = db.Entry(record);
        entry.Property(ApiDbContext.UserIdColumn).CurrentValue = userId;
        entry.Property(ApiDbContext.VersionColumn).CurrentValue = await VersionForAsync(userId, ct);
        entry.State = EntityState.Added;
    }

    /// <summary>
    /// The plan's own exercises (not the library's) that <paramref name="toUserId"/> doesn't have, copied over with the
    /// same ids so the plan finds them.
    /// </summary>
    async Task CopyCustomExercisesAsync(string fromUserId, string toUserId, WorkoutPlan plan, CancellationToken ct)
    {
        if (fromUserId == toUserId)
            return;
        var ids = plan.Workouts.SelectMany(w => w.Exercises).Select(e => e.ExerciseId)
            .Where(id => ExerciseLibrary.Find(id) == null).Distinct().ToList();
        if (ids.Count == 0)
            return;
        var have = await db.CustomExercises.OwnedBy(toUserId).Where(e => ids.Contains(e.Id)).Select(e => e.Id).ToListAsync(ct);
        var tracked = db.ChangeTracker.Entries<Exercise>().Where(e => e.State == EntityState.Added
            && (string)e.Property(ApiDbContext.UserIdColumn).CurrentValue! == toUserId).Select(e => e.Entity.Id);
        var missing = ids.Except(have).Except(tracked).ToList();
        if (missing.Count == 0)
            return;
        foreach (var source in await db.CustomExercises.OwnedBy(fromUserId).AsNoTracking().Where(e => missing.Contains(e.Id) && !e.IsDeleted).ToListAsync(ct))
        {
            source.UpdatedAt = time.GetUtcNow();
            await AddOwnedAsync(source, toUserId, ct);
        }
    }

    async Task UnlinkAsync(string userId, string? planId, CancellationToken ct)
    {
        if (planId == null || await db.Plans.OwnedBy(userId).FirstOrDefaultAsync(p => p.Id == planId, ct) is not { } plan)
            return;
        Unlink(plan);
        await TouchAsync(userId, plan, ct);
    }

    static void Unlink(WorkoutPlan plan)
    {
        plan.ShareId = null;
        plan.ShareRole = null;
    }

    /// <summary>Marks a plan of <paramref name="userId"/>'s changed now, so it wins over older edits and their devices pull it.</summary>
    async Task TouchAsync(string userId, WorkoutPlan plan, CancellationToken ct)
    {
        plan.UpdatedAt = time.GetUtcNow();
        db.Entry(plan).Property(ApiDbContext.VersionColumn).CurrentValue = await VersionForAsync(userId, ct);
    }

    /// <summary>
    /// The sync version for what's written to <paramref name="userId"/>'s data now: their version bumped once per
    /// operation. It's their concurrency token, so a sync of theirs racing with this makes one of the two retry.
    /// </summary>
    async Task<long> VersionForAsync(string userId, CancellationToken ct)
    {
        if (Changed.TryGetValue(userId, out var version))
            return version;
        var user = db.ChangeTracker.Entries<AppUser>().Select(e => e.Entity).FirstOrDefault(u => u.Id == userId)
            ?? await db.Users.FirstAsync(u => u.Id == userId, ct);
        user.SyncVersion++;
        Changed[userId] = user.SyncVersion;
        return user.SyncVersion;
    }

    /// <summary>
    /// For the syncing user: their writes already carry this sync's version, which their SyncVersion becomes on saving.
    /// </summary>
    public void UseVersion(string userId, long version) => Changed[userId] = version;

    static string NewShareId()
    {
        const string alphabet = "abcdefghijkmnpqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        return RandomNumberGenerator.GetString(alphabet, 12);
    }

    static string Trim(string s, int max) => s.Length <= max ? s : s[..max];

    /// <summary>"cable_fly" as "Cable fly", for an exercise nobody could name.</summary>
    static string Readable(string id)
    {
        var words = id.Replace('_', ' ').Trim();
        return words.Length == 0 ? "Exercise" : char.ToUpperInvariant(words[0]) + words[1..];
    }
}

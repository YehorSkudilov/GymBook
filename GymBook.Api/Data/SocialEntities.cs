using GymBook.Models;

namespace GymBook.Api.Data;

// The social side: public profiles, ranks, friends and shared plans. Unlike the synced records these aren't scoped by
// ApiDbContext's per-user filter (they're about how users see each other), so every query says whose rows it means.

/// <summary>A user's public face, made when they pick a username. Everything social hangs off it.</summary>
public class SocialProfile
{
    public string UserId { get; set; } = "";
    public string Username { get; set; } = "";
    /// <summary><see cref="Username"/> in lower case: usernames are unique ignoring case.</summary>
    public string UsernameKey { get; set; } = "";
    public string Bio { get; set; } = "";
    public string HomeGym { get; set; } = "";
    /// <summary>Bumped with each new picture, so its address changes and caches let go of the old one; 0 for none.</summary>
    public int AvatarVersion { get; set; }
    /// <summary>What a friend enters to add this user.</summary>
    public string FriendCode { get; set; } = "";

    public bool InRanks { get; set; }
    public Sex? RankSex { get; set; }
    /// <summary>Taken off every leaderboard by an admin (see Admin/AdminRanksController).</summary>
    public bool RankHidden { get; set; }
    /// <summary>Overall score (see <see cref="Ranks.Overall"/>), kept up to date as workouts sync.</summary>
    public double Score { get; set; }
    public DateTimeOffset? RanksUpdatedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

public class Avatar
{
    public string UserId { get; set; } = "";
    public byte[] Bytes { get; set; } = [];
    public string ContentType { get; set; } = "image/jpeg";
}

/// <summary>A ranked user's best on one lift, worked out from their synced workouts (see Social/RankCalculator).</summary>
public class RankedLift
{
    public string UserId { get; set; } = "";
    public RankLift Lift { get; set; }
    public bool Ranked { get; set; }
    public double E1RmKg { get; set; }
    public double BodyWeightKg { get; set; }
    public double Ratio { get; set; }
    public double Score { get; set; }
    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public DateTime? Date { get; set; }
    public int Sessions { get; set; }
}

/// <summary>One side of a friendship; adding a friend writes both, so friendships are always mutual.</summary>
public class Friendship
{
    public string UserId { get; set; } = "";
    public string FriendId { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

public class RankReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string ReporterId { get; set; } = "";
    public string TargetId { get; set; } = "";
    public RankLift? Lift { get; set; }
    public string Reason { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ResolvedAt { get; set; }
}

/// <summary>
/// A plan its owner shares: on the web for anyone (<see cref="IsPublic"/>), and in the app with members who each get a
/// linked copy in their own plans. The copies' workouts are kept the same as the owner's (see Social/PlanShares); each
/// keeps its own progress through the plan.
/// </summary>
public class PlanShare
{
    /// <summary>Random and unguessable: it's the public link.</summary>
    public string Id { get; set; } = "";
    public string OwnerId { get; set; } = "";
    public string PlanId { get; set; } = "";
    public bool IsPublic { get; set; }
    public bool AllowCopy { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}

public class PlanShareMembership
{
    public string ShareId { get; set; } = "";
    public string UserId { get; set; } = "";
    public PlanShareRole Role { get; set; }
    /// <summary>The member's linked copy in their own plans; null while the invite hasn't been accepted.</summary>
    public string? PlanId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

using GymBook.Api.Data;
using GymBook.Api.Social;
using GymBook.Contracts;
using GymBook.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Controllers;

/// <summary>
/// The public profile (username, picture, bio), strength ranks, friends and reports. Every account has a public profile
/// (see <see cref="Usernames"/>): one that never got a username gets a made-up one here the first time it's needed.
/// Ranking is a further opt-in.
/// </summary>
[ApiController]
[Route("api/social")]
public class SocialController(ApiDbContext db, ICurrentUser currentUser, RankCalculator ranks, Usernames usernames, TimeProvider time,
    GymBook.Api.Billing.Subscriptions subscriptions) : ControllerBase
{
    string Me => currentUser.UserId!;

    // ---- Public profile ----

    [HttpGet("me")]
    public async Task<MyPublicProfile> GetMine(CancellationToken ct) => ToMine(await MineAsync(ct));

    /// <summary>Changes the public profile. The username is unique ignoring case.</summary>
    [HttpPost("me")]
    [EnableRateLimiting(RateLimits.Sync)]
    public async Task<ActionResult<MyPublicProfile>> SaveMine(SavePublicProfileRequest request, CancellationToken ct)
    {
        var username = request.Username.Trim();
        if (await usernames.ProblemAsync(username, Me, ct) is { } problem)
            return Problem(statusCode: StatusCodes.Status409Conflict, title: problem);

        var profile = await MineAsync(ct);
        profile.Username = username;
        profile.UsernameKey = username.ToLowerInvariant();
        profile.Bio = request.Bio.Trim();
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException)
        {
            // Someone took the name in the same moment.
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "That username is taken.");
        }
        return ToMine(profile);
    }

    /// <summary>Sets the picture: a small JPEG or PNG (the app crops and shrinks it first).</summary>
    [HttpPost("me/avatar")]
    [EnableRateLimiting(RateLimits.Sync)]
    public async Task<ActionResult<MyPublicProfile>> SetAvatar(SetAvatarRequest request, CancellationToken ct)
    {
        var profile = await MineAsync(ct);
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(request.ImageBase64);
        }
        catch (FormatException)
        {
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "That picture couldn't be read.");
        }
        var type = ImageType(bytes);
        if (type == null || bytes.Length > SocialLimits.AvatarBytes)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Pick a JPEG or PNG picture.");

        var avatar = await db.Avatars.FirstOrDefaultAsync(a => a.UserId == Me, ct);
        if (avatar == null)
            db.Avatars.Add(avatar = new Avatar { UserId = Me });
        avatar.Bytes = bytes;
        avatar.ContentType = type;
        profile.AvatarVersion++;
        await db.SaveChangesAsync(ct);
        return ToMine(profile);
    }

    [HttpPost("me/avatar/remove")]
    public async Task<ActionResult<MyPublicProfile>> RemoveAvatar(CancellationToken ct)
    {
        var profile = await MineAsync(ct);
        await db.Avatars.Where(a => a.UserId == Me).ExecuteDeleteAsync(ct);
        profile.AvatarVersion = 0;
        await db.SaveChangesAsync(ct);
        return ToMine(profile);
    }

    /// <summary>Anyone's picture, by username: public, since shared plans on the web show their owner.</summary>
    [HttpGet("avatars/{username}")]
    [AllowAnonymous]
    [ResponseCache(Duration = 7 * 24 * 3600)]
    public async Task<IActionResult> GetAvatar(string username, CancellationToken ct)
    {
        var key = username.ToLowerInvariant();
        var avatar = await db.Avatars.AsNoTracking()
            .Where(a => db.SocialProfiles.Any(p => p.UserId == a.UserId && p.UsernameKey == key))
            .FirstOrDefaultAsync(ct);
        return avatar == null ? NotFound() : File(avatar.Bytes, avatar.ContentType);
    }

    /// <summary>Someone's public profile, with their ranks when they're ranked.</summary>
    [HttpGet("users/{username}")]
    public async Task<ActionResult<ProfileCard>> GetUser(string username, CancellationToken ct)
    {
        var key = username.ToLowerInvariant();
        var profile = await db.SocialProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UsernameKey == key, ct);
        if (profile == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nobody has that username.");
        var card = await CardAsync(profile, ct);
        if (IsRanked(profile))
            card.Lifts = await LiftsAsync(profile.UserId, ct);
        return card;
    }

    // ---- Ranks ----

    /// <summary>Puts the user on the leaderboards, ranked against <see cref="JoinRanksRequest.Sex"/>'s standards.</summary>
    [HttpPost("ranks/join")]
    [EnableRateLimiting(RateLimits.Sync)]
    public async Task<ActionResult<RankStatusResponse>> JoinRanks(JoinRanksRequest request, CancellationToken ct)
    {
        var profile = await MineAsync(ct);
        profile.InRanks = true;
        profile.RankSex = request.Sex;
        await db.SaveChangesAsync(ct);
        await ranks.RefreshAsync(Me, ct);
        return await GetRanks(ct);
    }

    [HttpPost("ranks/leave")]
    public async Task<IActionResult> LeaveRanks(CancellationToken ct)
    {
        var profile = await db.SocialProfiles.FirstOrDefaultAsync(p => p.UserId == Me, ct);
        if (profile == null)
            return NoContent();
        profile.InRanks = false;
        profile.Score = 0;
        await db.RankedLifts.Where(l => l.UserId == Me).ExecuteDeleteAsync(ct);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpGet("ranks")]
    public async Task<RankStatusResponse> GetRanks(CancellationToken ct)
    {
        var profile = await db.SocialProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == Me, ct);
        if (profile is not { InRanks: true })
            return new RankStatusResponse();
        // Worked out after each sync that brings workouts, weigh-ins or the profile; but never, if joining failed partway
        // or nothing has synced since, and the last-year window moves on regardless. Not today: worked out now.
        if (profile.RanksUpdatedAt is not { } updated || time.GetUtcNow() - updated > TimeSpan.FromDays(1))
        {
            await ranks.RefreshQuietlyAsync(Me, ct);
            profile = await db.SocialProfiles.AsNoTracking().FirstAsync(p => p.UserId == Me, ct);
        }
        var lifts = await LiftsAsync(Me, ct);
        var board = Board();
        var onBoard = !profile.RankHidden && profile.Score > 0;
        return new RankStatusResponse
        {
            InRanks = true,
            Score = profile.Score,
            Tier = Ranks.Tier(profile.Score),
            Position = onBoard ? await board.CountAsync(p => p.Score > profile.Score, ct) + 1 : null,
            Competitors = await board.CountAsync(ct),
            Lifts = lifts,
        };
    }

    /// <summary>
    /// The top of a board: <paramref name="scope"/> is "global" or "friends" (the user and their friends);
    /// <paramref name="lift"/> is a <see cref="RankCategory"/>, or empty for overall.
    /// </summary>
    [HttpGet("leaderboard")]
    public async Task<ActionResult<LeaderboardResponse>> Leaderboard([FromQuery] string? scope, [FromQuery] RankCategory? lift, CancellationToken ct)
    {
        var friendIds = await db.Friendships.Where(f => f.UserId == Me).Select(f => f.FriendId).ToListAsync(ct);
        var friends = scope == "friends";
        var people = Board();
        if (friends)
            people = people.Where(p => p.UserId == Me || friendIds.Contains(p.UserId));

        // (user, score, e1RM, ratio) rows, best first.
        IQueryable<Row> rows = lift is { } l
            ? people.Join(db.RankedLifts.Where(x => x.Lift == l && x.Ranked), p => p.UserId, x => x.UserId,
                (p, x) => new Row { UserId = p.UserId, Username = p.Username, AvatarVersion = p.AvatarVersion, Score = x.Score, ExerciseId = x.ExerciseId, E1RmKg = l == RankCategory.Consistency ? null : (double?)x.E1RmKg, Ratio = x.Ratio })
            : people.Select(p => new Row { UserId = p.UserId, Username = p.Username, AvatarVersion = p.AvatarVersion, Score = p.Score });

        var top = await rows.OrderByDescending(r => r.Score).ThenBy(r => r.Username).Take(SocialLimits.LeaderboardSize).ToListAsync(ct);
        var mine = top.All(r => r.UserId != Me) ? await rows.FirstOrDefaultAsync(r => r.UserId == Me, ct) : null;
        var shown = top.Select(r => r.UserId).ToList();
        if (mine != null)
            shown.Add(mine.UserId);
        var members = await subscriptions.MembersAsync(shown, ct);
        var response = new LeaderboardResponse { Entries = [.. top.Select((r, i) => Entry(r, i + 1))] };
        if (mine != null)
            response.Me = Entry(mine, await rows.CountAsync(r => r.Score > mine.Score, ct) + 1);
        return response;

        LeaderboardEntry Entry(Row r, int position) => new()
        {
            Position = position,
            Username = r.Username,
            AvatarPath = AvatarPath(r.Username, r.AvatarVersion),
            Score = r.Score,
            Tier = Ranks.Tier(r.Score),
            Exercise = ExerciseName(r.ExerciseId),
            E1RmKg = r.E1RmKg,
            Ratio = r.Ratio,
            IsMe = r.UserId == Me,
            IsFriend = friendIds.Contains(r.UserId),
            IsPro = members.Contains(r.UserId),
        };
    }

    class Row
    {
        public string UserId { get; set; } = "";
        public string Username { get; set; } = "";
        public int AvatarVersion { get; set; }
        public double Score { get; set; }
        public string? ExerciseId { get; set; }
        public double? E1RmKg { get; set; }
        public double? Ratio { get; set; }
    }

    /// <summary>Who's on the boards: ranked, not hidden by an admin, with at least one ranked lift.</summary>
    IQueryable<SocialProfile> Board() => db.SocialProfiles.AsNoTracking().Where(p => p.InRanks && !p.RankHidden && p.Score > 0);

    // ---- Nutrition boards ----

    /// <summary>The user's food logging, as the app works it out; it sends it whenever the Nutrition tab shows.</summary>
    [HttpPost("nutrition")]
    [EnableRateLimiting(RateLimits.Sync)]
    public async Task<IActionResult> SaveNutrition(NutritionStatsRequest request, CancellationToken ct)
    {
        var profile = await MineAsync(ct);
        profile.FoodStreak = request.Streak;
        profile.FoodBestStreak = Math.Max(request.BestStreak, request.Streak);
        profile.FoodOnTarget = request.OnTargetLast30;
        profile.FoodUpdatedAt = time.GetUtcNow();
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    /// <summary>
    /// The top of a nutrition board, for the ranked (the same opt-in as strength): <paramref name="board"/> is days in a
    /// row with food logged, or days on target in the last 30. Numbers the app hasn't sent lately are out of date (a
    /// streak can't be kept up without opening the app), so those users are left off: 2 days for a streak, a week for
    /// days on target.
    /// </summary>
    [HttpGet("nutrition/leaderboard")]
    public async Task<NutritionLeaderboardResponse> NutritionLeaderboard([FromQuery] string? scope, [FromQuery] NutritionBoard board, CancellationToken ct)
    {
        var friendIds = await db.Friendships.Where(f => f.UserId == Me).Select(f => f.FriendId).ToListAsync(ct);
        var fresh = time.GetUtcNow() - (board == NutritionBoard.Streak ? TimeSpan.FromDays(2) : TimeSpan.FromDays(7));
        var people = db.SocialProfiles.AsNoTracking().Where(p => p.InRanks && !p.RankHidden && p.FoodUpdatedAt >= fresh
            && (board == NutritionBoard.Streak ? p.FoodStreak : p.FoodOnTarget) > 0);
        if (scope == "friends")
            people = people.Where(p => p.UserId == Me || friendIds.Contains(p.UserId));
        var rows = people.Select(p => new NutritionRow
        {
            UserId = p.UserId,
            Username = p.Username,
            AvatarVersion = p.AvatarVersion,
            Days = board == NutritionBoard.Streak ? p.FoodStreak : p.FoodOnTarget,
            BestStreak = p.FoodBestStreak,
        });

        var top = await rows.OrderByDescending(r => r.Days).ThenByDescending(r => r.BestStreak).ThenBy(r => r.Username)
            .Take(SocialLimits.LeaderboardSize).ToListAsync(ct);
        var mine = top.All(r => r.UserId != Me) ? await rows.FirstOrDefaultAsync(r => r.UserId == Me, ct) : null;
        var shown = top.Select(r => r.UserId).ToList();
        if (mine != null)
            shown.Add(mine.UserId);
        var members = await subscriptions.MembersAsync(shown, ct);
        var response = new NutritionLeaderboardResponse { Entries = [.. top.Select((r, i) => Entry(r, i + 1))] };
        if (mine != null)
            response.Me = Entry(mine, await rows.CountAsync(r => r.Days > mine.Days, ct) + 1);
        return response;

        NutritionLeaderboardEntry Entry(NutritionRow r, int position) => new()
        {
            Position = position,
            Username = r.Username,
            AvatarPath = AvatarPath(r.Username, r.AvatarVersion),
            Days = r.Days,
            BestStreak = r.BestStreak,
            IsMe = r.UserId == Me,
            IsFriend = friendIds.Contains(r.UserId),
            IsPro = members.Contains(r.UserId),
        };
    }

    class NutritionRow
    {
        public string UserId { get; set; } = "";
        public string Username { get; set; } = "";
        public int AvatarVersion { get; set; }
        public int Days { get; set; }
        public int BestStreak { get; set; }
    }

    // ---- Friends ----

    [HttpGet("friends")]
    public async Task<List<ProfileCard>> Friends(CancellationToken ct)
    {
        var profiles = await db.SocialProfiles.AsNoTracking()
            .Where(p => db.Friendships.Any(f => f.UserId == Me && f.FriendId == p.UserId))
            .OrderBy(p => p.Username)
            .ToListAsync(ct);
        return [.. profiles.Select(p => ToCard(p, isFriend: true))];
    }

    /// <summary>Adds the user with this friend code (or username) as a friend, both ways.</summary>
    [HttpPost("friends")]
    [EnableRateLimiting(RateLimits.Auth)]
    public async Task<ActionResult<ProfileCard>> AddFriend(AddFriendRequest request, CancellationToken ct)
    {
        await MineAsync(ct);
        var code = request.Code.Trim().TrimStart('@');
        var key = code.ToLowerInvariant();
        var friend = await db.SocialProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.FriendCode == code.ToUpperInvariant() || p.UsernameKey == key, ct);
        if (friend == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nobody has that friend code or username.");
        if (friend.UserId == Me)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "That's you.");
        if (await db.Friendships.CountAsync(f => f.UserId == Me, ct) >= SocialLimits.MaxFriends)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: $"You can have up to {SocialLimits.MaxFriends} friends.");
        var now = time.GetUtcNow();
        if (!await db.Friendships.AnyAsync(f => f.UserId == Me && f.FriendId == friend.UserId, ct))
            db.Friendships.Add(new Friendship { UserId = Me, FriendId = friend.UserId, CreatedAt = now });
        if (!await db.Friendships.AnyAsync(f => f.UserId == friend.UserId && f.FriendId == Me, ct))
            db.Friendships.Add(new Friendship { UserId = friend.UserId, FriendId = Me, CreatedAt = now });
        await db.SaveChangesAsync(ct);
        return ToCard(friend, isFriend: true);
    }

    [HttpPost("friends/remove")]
    public async Task<IActionResult> RemoveFriend(UsernameRequest request, CancellationToken ct)
    {
        var key = request.Username.Trim().ToLowerInvariant();
        var id = await db.SocialProfiles.Where(p => p.UsernameKey == key).Select(p => p.UserId).FirstOrDefaultAsync(ct);
        if (id != null)
            await db.Friendships.Where(f => (f.UserId == Me && f.FriendId == id) || (f.UserId == id && f.FriendId == Me)).ExecuteDeleteAsync(ct);
        return NoContent();
    }

    // ---- Reports ----

    /// <summary>Flags someone's lift or profile for an admin to look at. One open report per person reported.</summary>
    [HttpPost("report")]
    [EnableRateLimiting(RateLimits.Auth)]
    public async Task<IActionResult> Report(ReportRequest request, CancellationToken ct)
    {
        var key = request.Username.Trim().ToLowerInvariant();
        var target = await db.SocialProfiles.Where(p => p.UsernameKey == key).Select(p => p.UserId).FirstOrDefaultAsync(ct);
        if (target == null)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Nobody has that username.");
        if (target == Me)
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "That's you.");
        if (!await db.RankReports.AnyAsync(r => r.ReporterId == Me && r.TargetId == target && r.ResolvedAt == null, ct))
        {
            db.RankReports.Add(new RankReport { ReporterId = Me, TargetId = target, Lift = request.Lift, Reason = request.Reason.Trim(), CreatedAt = time.GetUtcNow() });
            await db.SaveChangesAsync(ct);
        }
        return NoContent();
    }

    // ---- Helpers ----

    /// <summary>The user's public profile, made with a made-up username if they have none yet.</summary>
    Task<SocialProfile> MineAsync(CancellationToken ct) => usernames.EnsureAsync(Me, null, ct);

    static bool IsRanked(SocialProfile p) => p is { InRanks: true, RankHidden: false };

    async Task<List<LiftRank>> LiftsAsync(string userId, CancellationToken ct)
    {
        var rows = await db.RankedLifts.AsNoTracking().Where(l => l.UserId == userId).ToListAsync(ct);
        return [.. Ranks.Categories.Select(category => rows.FirstOrDefault(r => r.Lift == category) is { } r
            ? new LiftRank
            {
                Lift = category, Exercise = ExerciseName(r.ExerciseId), Ranked = r.Ranked, E1RmKg = r.E1RmKg, BodyWeightKg = r.BodyWeightKg,
                Ratio = r.Ratio, Score = r.Score, Tier = Ranks.Tier(r.Score), WeightKg = r.WeightKg, Reps = r.Reps, Date = r.Date,
                Sessions = r.Sessions,
            }
            : new LiftRank { Lift = category })];
    }

    /// <summary>
    /// A category's best exercise as others see it: a built-in one by its name, one the user made as "Custom exercise"
    /// (what people call their own exercises stays theirs); null without one.
    /// </summary>
    static string? ExerciseName(string? exerciseId) =>
        exerciseId == null ? null : GymBook.Services.ExerciseLibrary.Find(exerciseId)?.Name ?? "Custom exercise";

    async Task<ProfileCard> CardAsync(SocialProfile p, CancellationToken ct) =>
        ToCard(p, await db.Friendships.AnyAsync(f => f.UserId == Me && f.FriendId == p.UserId, ct));

    ProfileCard ToCard(SocialProfile p, bool isFriend) => new()
    {
        Username = p.Username,
        Bio = p.Bio,
        AvatarPath = AvatarPath(p.Username, p.AvatarVersion),
        Tier = IsRanked(p) ? Ranks.Tier(p.Score) : null,
        Score = IsRanked(p) ? p.Score : null,
        IsFriend = isFriend,
        IsMe = p.UserId == currentUser.UserId,
    };

    public static string? AvatarPath(string username, int version) =>
        version == 0 ? null : $"api/social/avatars/{Uri.EscapeDataString(username.ToLowerInvariant())}?v={version}";

    static MyPublicProfile ToMine(SocialProfile? p) => p == null ? new MyPublicProfile() : new MyPublicProfile
    {
        Exists = true,
        Username = p.Username,
        Bio = p.Bio,
        AvatarPath = AvatarPath(p.Username, p.AvatarVersion),
        FriendCode = p.FriendCode,
        InRanks = p.InRanks,
        Sex = p.RankSex,
        RankHidden = p.RankHidden,
    };

    /// <summary>"image/jpeg" or "image/png" from the file's first bytes; null for anything else.</summary>
    static string? ImageType(byte[] b) =>
        b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF ? "image/jpeg"
        : b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 ? "image/png"
        : null;
}

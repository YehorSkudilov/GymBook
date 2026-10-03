using System.Security.Cryptography;
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
/// The public profile (username, picture, bio), strength ranks, friends and reports. Everything social needs a public
/// profile first; until then the user is invisible to everyone. Ranking is a further opt-in.
/// </summary>
[ApiController]
[Route("api/social")]
public class SocialController(ApiDbContext db, ICurrentUser currentUser, RankCalculator ranks, TimeProvider time) : ControllerBase
{
    string Me => currentUser.UserId!;

    // ---- Public profile ----

    [HttpGet("me")]
    public async Task<MyPublicProfile> GetMine(CancellationToken ct) =>
        ToMine(await db.SocialProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == Me, ct));

    /// <summary>Makes the public profile, or changes it. The username is unique ignoring case.</summary>
    [HttpPost("me")]
    [EnableRateLimiting(RateLimits.Sync)]
    public async Task<ActionResult<MyPublicProfile>> SaveMine(SavePublicProfileRequest request, CancellationToken ct)
    {
        var username = request.Username.Trim();
        var key = username.ToLowerInvariant();
        if (Reserved.Contains(key))
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "That username is taken.");
        if (await db.SocialProfiles.AnyAsync(p => p.UsernameKey == key && p.UserId != Me, ct))
            return Problem(statusCode: StatusCodes.Status409Conflict, title: "That username is taken.");

        var profile = await db.SocialProfiles.FirstOrDefaultAsync(p => p.UserId == Me, ct);
        if (profile == null)
        {
            profile = new SocialProfile { UserId = Me, FriendCode = await NewFriendCodeAsync(ct), CreatedAt = time.GetUtcNow() };
            db.SocialProfiles.Add(profile);
        }
        profile.Username = username;
        profile.UsernameKey = key;
        profile.Bio = request.Bio.Trim();
        profile.HomeGym = request.HomeGym.Trim();
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
        var profile = await db.SocialProfiles.FirstOrDefaultAsync(p => p.UserId == Me, ct);
        if (profile == null)
            return NeedsProfile();
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
        var profile = await db.SocialProfiles.FirstOrDefaultAsync(p => p.UserId == Me, ct);
        if (profile == null)
            return NeedsProfile();
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
        var profile = await db.SocialProfiles.FirstOrDefaultAsync(p => p.UserId == Me, ct);
        if (profile == null)
            return NeedsProfile();
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
    /// <paramref name="lift"/> is a <see cref="RankLift"/>, or empty for overall.
    /// </summary>
    [HttpGet("leaderboard")]
    public async Task<ActionResult<LeaderboardResponse>> Leaderboard([FromQuery] string? scope, [FromQuery] RankLift? lift, CancellationToken ct)
    {
        var friendIds = await db.Friendships.Where(f => f.UserId == Me).Select(f => f.FriendId).ToListAsync(ct);
        var friends = scope == "friends";
        var people = Board();
        if (friends)
            people = people.Where(p => p.UserId == Me || friendIds.Contains(p.UserId));

        // (user, score, e1RM, ratio) rows, best first.
        IQueryable<Row> rows = lift is { } l
            ? people.Join(db.RankedLifts.Where(x => x.Lift == l && x.Ranked), p => p.UserId, x => x.UserId,
                (p, x) => new Row { UserId = p.UserId, Username = p.Username, AvatarVersion = p.AvatarVersion, Score = x.Score, E1RmKg = x.E1RmKg, Ratio = x.Ratio })
            : people.Select(p => new Row { UserId = p.UserId, Username = p.Username, AvatarVersion = p.AvatarVersion, Score = p.Score });

        var top = await rows.OrderByDescending(r => r.Score).ThenBy(r => r.Username).Take(SocialLimits.LeaderboardSize).ToListAsync(ct);
        var response = new LeaderboardResponse { Entries = [.. top.Select((r, i) => Entry(r, i + 1))] };
        if (top.All(r => r.UserId != Me) && await rows.FirstOrDefaultAsync(r => r.UserId == Me, ct) is { } mine)
            response.Me = Entry(mine, await rows.CountAsync(r => r.Score > mine.Score, ct) + 1);
        return response;

        LeaderboardEntry Entry(Row r, int position) => new()
        {
            Position = position,
            Username = r.Username,
            AvatarPath = AvatarPath(r.Username, r.AvatarVersion),
            Score = r.Score,
            Tier = Ranks.Tier(r.Score),
            E1RmKg = r.E1RmKg,
            Ratio = r.Ratio,
            IsMe = r.UserId == Me,
            IsFriend = friendIds.Contains(r.UserId),
        };
    }

    class Row
    {
        public string UserId { get; set; } = "";
        public string Username { get; set; } = "";
        public int AvatarVersion { get; set; }
        public double Score { get; set; }
        public double? E1RmKg { get; set; }
        public double? Ratio { get; set; }
    }

    /// <summary>Who's on the boards: ranked, not hidden by an admin, with at least one ranked lift.</summary>
    IQueryable<SocialProfile> Board() => db.SocialProfiles.AsNoTracking().Where(p => p.InRanks && !p.RankHidden && p.Score > 0);

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
        if (!await db.SocialProfiles.AnyAsync(p => p.UserId == Me, ct))
            return NeedsProfile();
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

    static readonly HashSet<string> Reserved = ["admin", "administrator", "gymbook", "gym_book", "support", "moderator", "staff", "official"];

    ActionResult NeedsProfile() => Problem(statusCode: StatusCodes.Status409Conflict, title: "Pick a username first.");

    static bool IsRanked(SocialProfile p) => p is { InRanks: true, RankHidden: false };

    async Task<List<LiftRank>> LiftsAsync(string userId, CancellationToken ct)
    {
        var rows = await db.RankedLifts.AsNoTracking().Where(l => l.UserId == userId).ToListAsync(ct);
        return [.. Ranks.Lifts.Select(lift => rows.FirstOrDefault(r => r.Lift == lift) is { } r
            ? new LiftRank
            {
                Lift = lift, Ranked = r.Ranked, E1RmKg = r.E1RmKg, BodyWeightKg = r.BodyWeightKg, Ratio = r.Ratio, Score = r.Score,
                Tier = Ranks.Tier(r.Score), WeightKg = r.WeightKg, Reps = r.Reps, Date = r.Date, Sessions = r.Sessions,
            }
            : new LiftRank { Lift = lift })];
    }

    async Task<ProfileCard> CardAsync(SocialProfile p, CancellationToken ct) =>
        ToCard(p, await db.Friendships.AnyAsync(f => f.UserId == Me && f.FriendId == p.UserId, ct));

    ProfileCard ToCard(SocialProfile p, bool isFriend) => new()
    {
        Username = p.Username,
        Bio = p.Bio,
        HomeGym = p.HomeGym,
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
        HomeGym = p.HomeGym,
        AvatarPath = AvatarPath(p.Username, p.AvatarVersion),
        FriendCode = p.FriendCode,
        InRanks = p.InRanks,
        Sex = p.RankSex,
        RankHidden = p.RankHidden,
    };

    async Task<string> NewFriendCodeAsync(CancellationToken ct)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        while (true)
        {
            var code = RandomNumberGenerator.GetString(alphabet, SocialLimits.FriendCodeLength);
            if (!await db.SocialProfiles.AnyAsync(p => p.FriendCode == code, ct))
                return code;
        }
    }

    /// <summary>"image/jpeg" or "image/png" from the file's first bytes; null for anything else.</summary>
    static string? ImageType(byte[] b) =>
        b.Length > 3 && b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF ? "image/jpeg"
        : b.Length > 8 && b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47 ? "image/png"
        : null;
}

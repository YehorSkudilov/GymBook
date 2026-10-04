using System.Security.Cryptography;
using System.Text.RegularExpressions;
using GymBook.Api.Data;
using GymBook.Contracts;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Social;

/// <summary>
/// Usernames and the public profile they make. Every account has one: picked when signing up with an email, or made up
/// (e.g. lifter_k3m9q2) for a Google sign-up and for older accounts that never picked one, to be changed in the app.
/// </summary>
public partial class Usernames(ApiDbContext db, TimeProvider time)
{
    static readonly HashSet<string> Reserved = ["admin", "administrator", "gymbook", "gym_book", "support", "moderator", "staff", "official"];

    /// <summary>Why <paramref name="username"/> can't be <paramref name="userId"/>'s, or null when it can.</summary>
    public async Task<string?> ProblemAsync(string username, string? userId, CancellationToken ct)
    {
        if (username.Length is < SocialLimits.UsernameMinLength or > SocialLimits.UsernameMaxLength)
            return $"A username is {SocialLimits.UsernameMinLength} to {SocialLimits.UsernameMaxLength} characters.";
        if (!Pattern().IsMatch(username))
            return "A username can only have letters, numbers, dots and underscores.";
        var key = username.ToLowerInvariant();
        if (Reserved.Contains(key) || await db.SocialProfiles.AnyAsync(p => p.UsernameKey == key && p.UserId != userId, ct))
            return "That username is taken.";
        return null;
    }

    /// <summary>
    /// <paramref name="userId"/>'s public profile, made now if they have none yet: with <paramref name="username"/> when
    /// it's given and still free, otherwise a made-up one. Saves.
    /// </summary>
    public async Task<SocialProfile> EnsureAsync(string userId, string? username, CancellationToken ct)
    {
        if (await db.SocialProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct) is { } existing)
            return existing;

        var profile = new SocialProfile { UserId = userId, FriendCode = await NewFriendCodeAsync(ct), CreatedAt = time.GetUtcNow() };
        db.SocialProfiles.Add(profile);
        var name = username != null && await ProblemAsync(username, userId, ct) == null ? username : await RandomAsync(ct);
        for (var attempt = 0; ; attempt++)
        {
            profile.Username = name;
            profile.UsernameKey = name.ToLowerInvariant();
            try
            {
                await db.SaveChangesAsync(ct);
                return profile;
            }
            catch (DbUpdateException) when (attempt < 3)
            {
                // Taken by someone else in the same moment (or, for a second request of this user's, the profile
                // itself was): use theirs if it's this user's, otherwise go again with a made-up name.
                db.Entry(profile).State = EntityState.Detached;
                if (await db.SocialProfiles.FirstOrDefaultAsync(p => p.UserId == userId, ct) is { } made)
                    return made;
                profile.FriendCode = await NewFriendCodeAsync(ct);
                db.SocialProfiles.Add(profile);
                name = await RandomAsync(ct);
            }
        }
    }

    /// <summary>A free made-up username: "lifter_" and six random letters and digits.</summary>
    async Task<string> RandomAsync(CancellationToken ct)
    {
        const string alphabet = "abcdefghjkmnpqrstuvwxyz23456789";
        while (true)
        {
            var name = "lifter_" + RandomNumberGenerator.GetString(alphabet, 6);
            if (!await db.SocialProfiles.AnyAsync(p => p.UsernameKey == name, ct))
                return name;
        }
    }

    public async Task<string> NewFriendCodeAsync(CancellationToken ct)
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        while (true)
        {
            var code = RandomNumberGenerator.GetString(alphabet, SocialLimits.FriendCodeLength);
            if (!await db.SocialProfiles.AnyAsync(p => p.FriendCode == code, ct))
                return code;
        }
    }

    [GeneratedRegex(SocialLimits.UsernamePattern)]
    private static partial Regex Pattern();
}

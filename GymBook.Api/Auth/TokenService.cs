using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;
using GymBook.Api.Data;
using GymBook.Contracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace GymBook.Api.Auth;

public class JwtOptions
{
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";

    /// <summary>HMAC-SHA256 key, from Jwt__SigningKey in .env.</summary>
    public string SigningKey { get; set; } = "";

    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;

    public SymmetricSecurityKey Key => new(Encoding.UTF8.GetBytes(SigningKey));

    /// <summary>Fails startup rather than running with a missing or guessable key.</summary>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
            throw new InvalidOperationException("Jwt:Issuer and Jwt:Audience must be configured.");
        if (Encoding.UTF8.GetByteCount(SigningKey) < 32)
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes. Set it with `dotnet user-secrets set Jwt:SigningKey <value>` or the Jwt__SigningKey environment variable.");
        if (AccessTokenMinutes is < 1 or > 60 || RefreshTokenDays is < 1 or > 90)
            throw new InvalidOperationException("Jwt token lifetimes are out of range.");
    }
}

/// <summary>
/// Issues short-lived JWT access tokens and opaque, rotating refresh tokens. Refresh tokens are stored only
/// as hashes; presenting one that was already rotated is treated as theft and revokes the whole family.
/// </summary>
public class TokenService(JwtOptions options, ApiDbContext db, TimeProvider clock, EmailSender email)
{
    public async Task<AuthResponse> IssueAsync(AppUser user, Guid? familyId = null, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        var accessExpires = now.AddMinutes(options.AccessTokenMinutes);
        var accessToken = new JsonWebTokenHandler().CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = accessExpires.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = user.Id,
                [JwtRegisteredClaimNames.Jti] = Guid.NewGuid().ToString("N"),
                [AuthPolicies.EmailVerifiedClaim] = IsVerified(user) ? "true" : "false",
            },
            SigningCredentials = new SigningCredentials(options.Key, SecurityAlgorithms.HmacSha256),
        });

        var refreshToken = Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(32));
        var refreshExpires = now.AddDays(options.RefreshTokenDays);
        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = Hash(refreshToken),
            FamilyId = familyId ?? Guid.NewGuid(),
            CreatedAt = now,
            ExpiresAt = refreshExpires,
        });
        await db.SaveChangesAsync(ct);

        return new AuthResponse
        {
            UserId = user.Id,
            Email = user.Email ?? "",
            AccessToken = accessToken,
            AccessTokenExpiresAt = accessExpires,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAt = refreshExpires,
            HasPassword = user.PasswordHash != null,
            EmailVerified = IsVerified(user),
        };
    }

    /// <summary>Exchanges a refresh token for a new pair. Returns null for anything invalid, expired, revoked or reused.</summary>
    public async Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        var hash = Hash(refreshToken);
        var stored = await db.RefreshTokens.AsNoTracking().FirstOrDefaultAsync(t => t.TokenHash == hash, ct);
        if (stored == null)
            return null;

        if (stored.RevokedAt != null)
        {
            // A token that was already exchanged is being replayed: someone else may hold this session.
            await RevokeFamilyAsync(stored.FamilyId, ct);
            return null;
        }
        if (stored.ExpiresAt <= now)
            return null;

        // Claim the token atomically so two concurrent refreshes with the same token can't both succeed.
        var claimed = await db.RefreshTokens
            .Where(t => t.Id == stored.Id && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
        if (claimed == 0)
        {
            await RevokeFamilyAsync(stored.FamilyId, ct);
            return null;
        }

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == stored.UserId, ct);
        if (user == null || (user.LockoutEnd != null && user.LockoutEnd > now))
            return null;
        return await IssueAsync(user, stored.FamilyId, ct);
    }

    /// <summary>Signs out the session the token belongs to. Unknown tokens are ignored.</summary>
    public async Task RevokeAsync(string refreshToken, CancellationToken ct = default)
    {
        var hash = Hash(refreshToken);
        var familyId = await db.RefreshTokens.Where(t => t.TokenHash == hash).Select(t => (Guid?)t.FamilyId).FirstOrDefaultAsync(ct);
        if (familyId != null)
            await RevokeFamilyAsync(familyId.Value, ct);
    }

    /// <summary>Signs the user out everywhere, e.g. after the password changed.</summary>
    public Task RevokeAllAsync(string userId, CancellationToken ct = default)
    {
        var now = clock.GetUtcNow();
        return db.RefreshTokens
            .Where(t => t.UserId == userId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    Task RevokeFamilyAsync(Guid familyId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        return db.RefreshTokens
            .Where(t => t.FamilyId == familyId && t.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.RevokedAt, now), ct);
    }

    // Without a way to send the code nobody could verify, so the requirement only applies once email is set up.
    bool IsVerified(AppUser user) => user.EmailConfirmed || !email.IsAvailable;

    static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

public static class AuthPolicies
{
    /// <summary>In the access token: "true" once the account's email is verified. Everything but the account endpoints requires it.</summary>
    public const string EmailVerifiedClaim = "email_verified";

    /// <summary>Signed in, verified or not: for the account endpoints, where the email gets verified or corrected.</summary>
    public const string AnyAccount = "any-account";
}

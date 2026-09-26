using System.ComponentModel.DataAnnotations;

namespace GymBook.Contracts;

public class RegisterRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string Email { get; set; } = "";

    // The upper bound keeps password hashing cost bounded.
    [Required, MinLength(AuthLimits.MinPasswordLength), MaxLength(AuthLimits.MaxPasswordLength)]
    public string Password { get; set; } = "";
}

public class LoginRequest
{
    [Required, MaxLength(256)]
    public string Email { get; set; } = "";

    [Required, MaxLength(AuthLimits.MaxPasswordLength)]
    public string Password { get; set; } = "";
}

public class RefreshRequest
{
    [Required, MaxLength(AuthLimits.MaxTokenLength)]
    public string RefreshToken { get; set; } = "";
}

public class DeleteAccountRequest
{
    [Required, MaxLength(AuthLimits.MaxPasswordLength)]
    public string Password { get; set; } = "";
}

public class AuthResponse
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = "";
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }
}

public class AccountResponse
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>The subset of RFC 7807 problem details the app reads from error responses.</summary>
public class ApiProblem
{
    public string? Title { get; set; }
    public string? Detail { get; set; }
    public int? Status { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}

public static class AuthLimits
{
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;
    public const int MaxTokenLength = 256;
}

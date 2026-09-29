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

public class GoogleSignInRequest
{
    /// <summary>The ID token Google's sign-in returned on the device.</summary>
    [Required, MaxLength(AuthLimits.MaxIdTokenLength)]
    public string IdToken { get; set; } = "";
}

public class ForgotPasswordRequest
{
    [Required, MaxLength(256)]
    public string Email { get; set; } = "";
}

public class ResetPasswordRequest
{
    [Required, MaxLength(256)]
    public string Email { get; set; } = "";

    /// <summary>The code forgot-password emailed.</summary>
    [Required, MaxLength(AuthLimits.MaxCodeLength)]
    public string Code { get; set; } = "";

    [Required, MinLength(AuthLimits.MinPasswordLength), MaxLength(AuthLimits.MaxPasswordLength)]
    public string NewPassword { get; set; } = "";
}

public class ChangePasswordRequest
{
    /// <summary>Required when the account has a password; empty when adding one to a Google-only account.</summary>
    [MaxLength(AuthLimits.MaxPasswordLength)]
    public string CurrentPassword { get; set; } = "";

    [Required, MinLength(AuthLimits.MinPasswordLength), MaxLength(AuthLimits.MaxPasswordLength)]
    public string NewPassword { get; set; } = "";
}

public class ChangeEmailRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string NewEmail { get; set; } = "";

    /// <summary>Required when the account has a password.</summary>
    [MaxLength(AuthLimits.MaxPasswordLength)]
    public string Password { get; set; } = "";
}

public class ConfirmEmailChangeRequest
{
    [Required, EmailAddress, MaxLength(256)]
    public string NewEmail { get; set; } = "";

    /// <summary>The code emailed to the new address.</summary>
    [Required, MaxLength(AuthLimits.MaxCodeLength)]
    public string Code { get; set; } = "";
}

/// <summary>Proof it's really the owner: the password, or for an account without one a fresh Google ID token.</summary>
public class DeleteAccountRequest
{
    [MaxLength(AuthLimits.MaxPasswordLength)]
    public string Password { get; set; } = "";

    [MaxLength(AuthLimits.MaxIdTokenLength)]
    public string? GoogleIdToken { get; set; }
}

public class AuthResponse
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public string AccessToken { get; set; } = "";
    public DateTimeOffset AccessTokenExpiresAt { get; set; }
    public string RefreshToken { get; set; } = "";
    public DateTimeOffset RefreshTokenExpiresAt { get; set; }

    /// <summary>False for an account made with Google that has no password (yet).</summary>
    public bool HasPassword { get; set; } = true;
}

public class AccountResponse
{
    public string UserId { get; set; } = "";
    public string Email { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public bool HasPassword { get; set; }
    public bool HasGoogle { get; set; }
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
    public const int MaxIdTokenLength = 4096;
    public const int MaxCodeLength = 16;
}

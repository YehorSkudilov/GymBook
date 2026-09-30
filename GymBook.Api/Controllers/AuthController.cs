using GymBook.Api.Auth;
using GymBook.Api.Data;
using GymBook.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GymBook.Api.Controllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
[EnableRateLimiting(RateLimits.Auth)]
public class AuthController(
    UserManager<AppUser> users,
    SignInManager<AppUser> signIn,
    TokenService tokens,
    GoogleTokenVerifier google,
    EmailSender email,
    ILogger<AuthController> log) : ControllerBase
{
    // Used to spend the same hashing time on unknown emails as on real ones, so response timing doesn't
    // reveal which emails have accounts.
    static readonly string DummyHash = new PasswordHasher<AppUser>().HashPassword(new AppUser(), Guid.NewGuid().ToString());

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var address = request.Email.Trim();
        var user = new AppUser { UserName = address, Email = address };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(error.Code.Contains("Password") ? nameof(request.Password) : nameof(request.Email), error.Description);
            return ValidationProblem(ModelState);
        }
        // No verification email yet: the app asks for one when it shows the verification screen, so there's never a
        // second one on top.
        return await tokens.IssueAsync(user, ct: ct);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user == null)
        {
            new PasswordHasher<AppUser>().VerifyHashedPassword(new AppUser(), DummyHash, request.Password);
            return InvalidCredentials();
        }

        // Counts failures and locks the account for a while after too many, to stop password guessing.
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!result.Succeeded)
            return InvalidCredentials();
        return await tokens.IssueAsync(user, ct: ct);
    }

    /// <summary>Signs in with Google, creating the account on first use or linking it to the account with the same email.</summary>
    [HttpPost("google")]
    public async Task<ActionResult<AuthResponse>> Google(GoogleSignInRequest request, CancellationToken ct)
    {
        if (!google.IsConfigured)
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "Google sign-in isn't available.");
        var payload = await google.VerifyAsync(request.IdToken);
        if (payload == null)
            return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Google sign-in failed. Please try again.");

        var user = await users.FindByLoginAsync(GoogleTokenVerifier.Provider, payload.Subject);
        if (user == null)
        {
            if (!payload.EmailVerified || string.IsNullOrWhiteSpace(payload.Email))
                return Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Your Google account's email address isn't verified.");

            user = await users.FindByEmailAsync(payload.Email);
            if (user == null)
            {
                user = new AppUser { UserName = payload.Email, Email = payload.Email, EmailConfirmed = true };
                var created = await users.CreateAsync(user);
                if (!created.Succeeded)
                    return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Couldn't create the account.");
            }
            else if (!user.EmailConfirmed)
            {
                // Nobody had proved owning this address, so its password may be someone else's: signing up with another
                // person's email and waiting for them to sign in with Google would otherwise hand over their account.
                // From here on only the Google account gets in; the owner can add a password again in the app.
                await users.RemovePasswordAsync(user);
                await tokens.RevokeAllAsync(user.Id, ct);
                user.EmailConfirmed = true;
                await users.UpdateAsync(user);
            }

            var linked = await users.AddLoginAsync(user, new UserLoginInfo(GoogleTokenVerifier.Provider, payload.Subject, "Google"));
            if (!linked.Succeeded)
                return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Couldn't link your Google account.");
        }
        return await tokens.IssueAsync(user, ct: ct);
    }

    /// <summary>Emails a code for <see cref="ResetPassword"/>. The same answer whether or not the email has an account.</summary>
    [HttpPost("password/forgot")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken ct)
    {
        if (!email.IsAvailable)
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Password reset by email isn't available right now.");

        var user = await users.FindByEmailAsync(request.Email.Trim());
        if (user?.Email == null)
            return NoContent();

        var code = await users.GeneratePasswordResetTokenAsync(user);
        try
        {
            await email.SendCodeAsync(user.Email, "reset-password", new CodeEmail(
                "Your Gym Book password reset code",
                "Reset your password",
                "Enter this code in Gym Book, then choose a new password.",
                code,
                "If you didn't ask to reset your password, you can ignore this email. Your password stays as it is."), ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogError(e, "Couldn't send a password reset email");
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Couldn't send the email. Please try again later.");
        }
        return NoContent();
    }

    /// <summary>Sets a new password with the emailed code, signs out every other session and signs this one in.</summary>
    [HttpPost("password/reset")]
    public async Task<ActionResult<AuthResponse>> ResetPassword(ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email.Trim());
        // Wrong codes count towards the lockout, so the six digits can't be guessed.
        if (user == null || await users.IsLockedOutAsync(user))
            return InvalidCode();

        var result = await users.ResetPasswordAsync(user, request.Code.Trim(), request.NewPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.InvalidToken)))
            {
                await users.AccessFailedAsync(user);
                return InvalidCode();
            }
            foreach (var error in result.Errors)
                ModelState.AddModelError(nameof(request.NewPassword), error.Description);
            return ValidationProblem(ModelState);
        }

        // The code proved the inbox is theirs.
        user.EmailConfirmed = true;
        await users.UpdateAsync(user);
        await users.ResetAccessFailedCountAsync(user);
        await tokens.RevokeAllAsync(user.Id, ct);
        return await tokens.IssueAsync(user, ct: ct);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh(RefreshRequest request, CancellationToken ct)
    {
        var response = await tokens.RefreshAsync(request.RefreshToken, ct);
        return response == null ? Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Session expired. Please sign in again.") : response;
    }

    /// <summary>Anonymous because the access token may already be expired; holding the refresh token is the proof.</summary>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshRequest request, CancellationToken ct)
    {
        await tokens.RevokeAsync(request.RefreshToken, ct);
        return NoContent();
    }

    // Same answer for unknown email, wrong password and locked account, so none of them can be told apart.
    ActionResult InvalidCredentials() => Problem(statusCode: StatusCodes.Status401Unauthorized, title: "Invalid email or password.");

    ActionResult InvalidCode() => Problem(statusCode: StatusCodes.Status400BadRequest, title: "That code is wrong or has expired.");
}

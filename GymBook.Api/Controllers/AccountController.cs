using GymBook.Api.Auth;
using GymBook.Api.Data;
using GymBook.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GymBook.Api.Controllers;

[ApiController]
[Route("api/account")]
// Open before the email is verified: this is where it gets verified, or corrected if it was mistyped.
[Authorize(Policy = AuthPolicies.AnyAccount)]
public class AccountController(
    UserManager<AppUser> users,
    SignInManager<AppUser> signIn,
    ICurrentUser currentUser,
    TokenService tokens,
    GoogleTokenVerifier google,
    EmailSender email,
    ILogger<AccountController> log) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AccountResponse>> Get()
    {
        var user = await users.FindByIdAsync(currentUser.UserId!);
        if (user == null)
            return Unauthorized();
        return await ToResponseAsync(user);
    }

    /// <summary>Emails a code for <see cref="VerifyEmail"/>. Nothing to do when the email is already verified.</summary>
    [HttpPost("verify-email/send")]
    [EnableRateLimiting(RateLimits.Auth)]
    public async Task<IActionResult> SendVerificationCode(CancellationToken ct)
    {
        var user = await users.FindByIdAsync(currentUser.UserId!);
        if (user == null)
            return Unauthorized();
        if (user.EmailConfirmed)
            return NoContent();

        switch (await SendVerificationCodeAsync(user, email, users, log, ct))
        {
            case false:
                return Problem(statusCode: StatusCodes.Status429TooManyRequests, title: "A code was just sent, or today's codes are used up. Check your inbox and spam, or try again later.");
            case null:
                return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Couldn't send the email. Please try again later.");
        }
        return NoContent();
    }

    /// <summary>
    /// Marks the email verified with the emailed code. The access token still says unverified until it's renewed, so
    /// the app refreshes its session afterwards.
    /// </summary>
    [HttpPost("verify-email")]
    [EnableRateLimiting(RateLimits.Auth)]
    public async Task<ActionResult<AccountResponse>> VerifyEmail(VerifyEmailRequest request)
    {
        var user = await users.FindByIdAsync(currentUser.UserId!);
        if (user == null)
            return Unauthorized();
        if (user.EmailConfirmed)
            return await ToResponseAsync(user);
        // Wrong codes count towards the lockout, so the six digits can't be guessed.
        if (await users.IsLockedOutAsync(user))
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Too many wrong codes. Try again in 15 minutes.");

        var result = await users.ConfirmEmailAsync(user, request.Code.Trim());
        if (!result.Succeeded)
        {
            await users.AccessFailedAsync(user);
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "That code is wrong or has expired.");
        }
        await users.ResetAccessFailedCountAsync(user);
        return await ToResponseAsync(user);
    }

    /// <summary>
    /// Emails an email verification code. True when sent, false when one was sent within the last minute, null when
    /// sending failed. Only when the app asks: it does once, when the verification sheet first shows for an address.
    /// </summary>
    internal static async Task<bool?> SendVerificationCodeAsync(AppUser user, EmailSender email, UserManager<AppUser> users, ILogger log, CancellationToken ct)
    {
        if (user.Email == null || !email.IsAvailable)
            return null;
        var code = await users.GenerateEmailConfirmationTokenAsync(user);
        try
        {
            return await email.SendCodeAsync(user.Email, "verify-email", new CodeEmail(
                "Verify your Gym Book email",
                "Verify your email",
                "Enter this code in Gym Book to finish setting up your account.",
                code,
                "If you didn't create a Gym Book account, you can ignore this email."), ct);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogError(e, "Couldn't send an email verification code");
            return null;
        }
    }

    /// <summary>
    /// Changes the password, or adds one to an account made with Google. Every other session is signed out; the
    /// response is a fresh session for this device.
    /// </summary>
    [HttpPost("password")]
    [EnableRateLimiting(RateLimits.Auth)]
    public async Task<ActionResult<AuthResponse>> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await users.FindByIdAsync(currentUser.UserId!);
        if (user == null)
            return Unauthorized();

        IdentityResult result;
        if (await users.HasPasswordAsync(user))
        {
            var check = await signIn.CheckPasswordSignInAsync(user, request.CurrentPassword, lockoutOnFailure: true);
            if (!check.Succeeded)
                return IncorrectPassword();
            result = await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        }
        else
            result = await users.AddPasswordAsync(user, request.NewPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(nameof(request.NewPassword), error.Description);
            return ValidationProblem(ModelState);
        }
        await tokens.RevokeAllAsync(user.Id, ct);
        return await tokens.IssueAsync(user, ct: ct);
    }

    /// <summary>Emails a code to the new address; <see cref="ConfirmEmailChange"/> switches to it once the code comes back.</summary>
    [HttpPost("email")]
    [EnableRateLimiting(RateLimits.Auth)]
    public async Task<IActionResult> ChangeEmail(ChangeEmailRequest request, CancellationToken ct)
    {
        if (!email.IsAvailable)
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Changing your email isn't available right now.");
        var user = await users.FindByIdAsync(currentUser.UserId!);
        if (user == null)
            return Unauthorized();

        // An access token alone isn't enough to move the account to another inbox.
        if (await users.HasPasswordAsync(user) && !(await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true)).Succeeded)
            return IncorrectPassword();

        var newEmail = request.NewEmail.Trim();
        if (string.Equals(newEmail, user.Email, StringComparison.OrdinalIgnoreCase))
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "That's already your email.");
        if (await users.FindByEmailAsync(newEmail) != null)
            return EmailTaken();

        var code = await users.GenerateChangeEmailTokenAsync(user, newEmail);
        try
        {
            if (!await email.SendCodeAsync(newEmail, "change-email", new CodeEmail(
                    "Confirm your new Gym Book email",
                    "Confirm your new email",
                    "Enter this code in Gym Book to sign in with this address from now on.",
                    code,
                    "If you didn't ask for this, you can ignore this email. Your account stays as it is."), ct))
                return Problem(statusCode: StatusCodes.Status429TooManyRequests, title: "A code was just sent, or today's codes are used up. Check your inbox and spam, or try again later.");
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            log.LogError(e, "Couldn't send an email change code");
            return Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Couldn't send the email. Please try again later.");
        }
        return NoContent();
    }

    [HttpPost("email/confirm")]
    [EnableRateLimiting(RateLimits.Auth)]
    public async Task<ActionResult<AccountResponse>> ConfirmEmailChange(ConfirmEmailChangeRequest request)
    {
        var user = await users.FindByIdAsync(currentUser.UserId!);
        if (user == null)
            return Unauthorized();
        // Wrong codes count towards the lockout, so the six digits can't be guessed.
        if (await users.IsLockedOutAsync(user))
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Too many wrong codes. Try again in 15 minutes.");

        var newEmail = request.NewEmail.Trim();
        var result = await users.ChangeEmailAsync(user, newEmail, request.Code.Trim());
        if (!result.Succeeded)
        {
            if (result.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.DuplicateEmail)))
                return EmailTaken();
            await users.AccessFailedAsync(user);
            return Problem(statusCode: StatusCodes.Status400BadRequest, title: "That code is wrong or has expired.");
        }
        // The user name is the email.
        await users.SetUserNameAsync(user, newEmail);
        await users.ResetAccessFailedCountAsync(user);
        return await ToResponseAsync(user);
    }

    /// <summary>Permanently deletes the account and, by cascade, all synced data and sessions.</summary>
    [HttpPost("delete")]
    [EnableRateLimiting(RateLimits.Auth)]
    public async Task<IActionResult> Delete(DeleteAccountRequest request)
    {
        var user = await users.FindByIdAsync(currentUser.UserId!);
        if (user == null)
            return Unauthorized();

        // A stolen access token alone isn't enough to destroy the account.
        if (await users.HasPasswordAsync(user))
        {
            var check = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (!check.Succeeded)
                return IncorrectPassword();
        }
        else
        {
            // No password to ask for: signing in with the linked Google account again is the proof.
            var payload = request.GoogleIdToken is { } idToken ? await google.VerifyAsync(idToken) : null;
            var logins = await users.GetLoginsAsync(user);
            if (payload == null || !logins.Any(l => l.LoginProvider == GoogleTokenVerifier.Provider && l.ProviderKey == payload.Subject))
                return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Confirm with the Google account you signed in with.");
        }

        var result = await users.DeleteAsync(user);
        if (!result.Succeeded)
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Couldn't delete the account.");
        return NoContent();
    }

    async Task<AccountResponse> ToResponseAsync(AppUser user) => new()
    {
        UserId = user.Id,
        Email = user.Email ?? "",
        CreatedAt = user.CreatedAt,
        HasPassword = await users.HasPasswordAsync(user),
        HasGoogle = (await users.GetLoginsAsync(user)).Any(l => l.LoginProvider == GoogleTokenVerifier.Provider),
        EmailVerified = user.EmailConfirmed || !email.IsAvailable,
    };

    ActionResult IncorrectPassword() => Problem(statusCode: StatusCodes.Status403Forbidden, title: "Incorrect password.");

    ActionResult EmailTaken() => Problem(statusCode: StatusCodes.Status409Conflict, title: "An account with that email already exists.");
}

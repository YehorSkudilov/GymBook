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
public class AuthController(UserManager<AppUser> users, SignInManager<AppUser> signIn, TokenService tokens) : ControllerBase
{
    // Used to spend the same hashing time on unknown emails as on real ones, so response timing doesn't
    // reveal which emails have accounts.
    static readonly string DummyHash = new PasswordHasher<AppUser>().HashPassword(new AppUser(), Guid.NewGuid().ToString());

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request, CancellationToken ct)
    {
        var email = request.Email.Trim();
        var user = new AppUser { UserName = email, Email = email };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError(error.Code.Contains("Password") ? nameof(request.Password) : nameof(request.Email), error.Description);
            return ValidationProblem(ModelState);
        }
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
}

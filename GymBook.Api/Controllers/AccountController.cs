using GymBook.Api.Data;
using GymBook.Contracts;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GymBook.Api.Controllers;

[ApiController]
[Route("api/account")]
public class AccountController(UserManager<AppUser> users, SignInManager<AppUser> signIn, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AccountResponse>> Get()
    {
        var user = await users.FindByIdAsync(currentUser.UserId!);
        if (user == null)
            return Unauthorized();
        return new AccountResponse { UserId = user.Id, Email = user.Email ?? "", CreatedAt = user.CreatedAt };
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
        var check = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (!check.Succeeded)
            return Problem(statusCode: StatusCodes.Status403Forbidden, title: "Incorrect password.");

        var result = await users.DeleteAsync(user);
        if (!result.Succeeded)
            return Problem(statusCode: StatusCodes.Status500InternalServerError, title: "Couldn't delete the account.");
        return NoContent();
    }
}

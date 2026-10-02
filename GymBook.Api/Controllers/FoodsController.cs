using GymBook.Api.Foods;
using GymBook.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace GymBook.Api.Controllers;

/// <summary>
/// Food search for logging food: databases that need a key (USDA branded foods, FatSecret), searched here so the keys
/// never ship in the app. Open to everyone, signed in or not (the app works without an account), limited per address.
/// </summary>
[ApiController]
[Route("api/foods")]
[AllowAnonymous]
[EnableRateLimiting(RateLimits.Foods)]
public class FoodsController(FoodDatabases foods) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<FoodSearchResponse>> Search([FromQuery] string q, CancellationToken ct)
    {
        q = (q ?? "").Trim();
        if (q.Length is < 2 or > 100)
            return new FoodSearchResponse();
        return await foods.SearchAsync(q, ct);
    }

    /// <summary>The packaged food with this barcode; 404 when none is known.</summary>
    [HttpGet("barcode/{code}")]
    public async Task<ActionResult<FoodInfo>> Barcode(string code, CancellationToken ct)
    {
        if (code.Length is < 6 or > 14 || !code.All(char.IsAsciiDigit))
            return BadRequest();
        return await foods.BarcodeAsync(code, ct) is { } food ? food : NotFound();
    }
}

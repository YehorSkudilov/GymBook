using GymBook.Api.Auth;
using GymBook.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GymBook.Api.Admin;

/// <summary>Settings that apply to everyone (see <see cref="ServerSettings"/>): every admin sees them, a SuperAdmin changes them.</summary>
[ApiController]
[Route("api/admin/settings")]
[Authorize(Policy = AuthPolicies.AdminAccess)]
public class AdminSettingsController(ServerSettings settings, ILogger<AdminSettingsController> log) : ControllerBase
{
    [HttpGet]
    public async Task<AdminSettings> Get(CancellationToken ct) => new(await settings.ProRequiredAsync(ct));

    [HttpPut]
    [Authorize(Policy = AuthPolicies.SuperAdminOnly)]
    public async Task<AdminSettings> Set(AdminSettings request, CancellationToken ct)
    {
        await settings.SetProRequiredAsync(request.ProRequired, ct);
        log.LogInformation("Admin {Admin} set Pro required to {Required}", User.FindFirst("sub")?.Value, request.ProRequired);
        return await Get(ct);
    }
}

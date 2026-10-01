using GymBook.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Admin;

/// <summary>
/// Makes the accounts in Admin:BootstrapEmails (Admin__BootstrapEmails, comma-separated) SuperAdmins at startup, so
/// there's someone to hand out the other roles. Sign up in the app first; an email without an account is retried on the
/// next start. Accounts that already have a role keep it, so a SuperAdmin can still change it from the admin app.
/// </summary>
public static class AdminBootstrapper
{
    public static async Task RunAsync(IServiceProvider services, IConfiguration config)
    {
        var emails = (config["Admin:BootstrapEmails"] ?? "")
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (emails.Length == 0)
            return;

        var db = services.GetRequiredService<ApiDbContext>();
        var log = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(AdminBootstrapper));
        foreach (var email in emails)
        {
            var normalized = email.ToUpperInvariant();
            var user = await db.Users.FirstOrDefaultAsync(u => u.NormalizedEmail == normalized);
            if (user == null)
            {
                log.LogWarning("Admin bootstrap: no account for {Email} yet; it's retried on the next start.", email);
                continue;
            }
            if (user.AdminRole != null)
                continue;

            user.AdminRole = AdminRoles.SuperAdmin;
            log.LogInformation("Admin bootstrap: {Email} is now a SuperAdmin.", email);
        }
        await db.SaveChangesAsync();
    }
}

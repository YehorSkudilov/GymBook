using GymBook.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace GymBook.Api.Admin;

/// <summary>
/// Settings admins change from the admin apps while the server runs, kept in the database (server_settings). A
/// setting nobody has changed has its default.
/// </summary>
public class ServerSettings(ApiDbContext db, TimeProvider time)
{
    const string ProRequiredKey = "pro.required";

    /// <summary>
    /// The AI features need Gym Book Pro. Off: they're free for everyone (still within the AI limits), e.g. before
    /// the stores are set up.
    /// </summary>
    public async Task<bool> ProRequiredAsync(CancellationToken ct) => await GetAsync(ProRequiredKey, ct) != "false";

    public Task SetProRequiredAsync(bool required, CancellationToken ct) => SetAsync(ProRequiredKey, required ? "true" : "false", ct);

    async Task<string?> GetAsync(string key, CancellationToken ct) =>
        await db.Settings.Where(s => s.Key == key).Select(s => s.Value).FirstOrDefaultAsync(ct);

    async Task SetAsync(string key, string value, CancellationToken ct)
    {
        var row = await db.Settings.FirstOrDefaultAsync(s => s.Key == key, ct);
        if (row == null)
            db.Settings.Add(row = new ServerSetting { Key = key });
        row.Value = value;
        row.UpdatedAt = time.GetUtcNow();
        await db.SaveChangesAsync(ct);
    }
}

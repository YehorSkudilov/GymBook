using Microsoft.AspNetCore.SignalR;

namespace GymBook.Api.Sync;

/// <summary>
/// The live side of sync (SignalR: a WebSocket where the connection allows, else server-sent events or long polling):
/// each signed-in app stays connected while it's open, and when one of the user's devices pushes changes, the others
/// are told straight away (<see cref="SyncNotifier"/>) and sync within a second instead of at their next resume. The
/// data itself still goes through POST api/sync; this only says when. Same access rules as sync: a verified account.
/// </summary>
public class SyncHub : Hub
{
    /// <summary>The message the apps listen for: "your data changed elsewhere", with the server's new cursor.</summary>
    public const string Changed = "changed";

    /// <summary>Header an app sends with its sync request: its own connection, which isn't told about its own changes.</summary>
    public const string ConnectionHeader = "X-Sync-Connection";

    public static string Group(string userId) => $"user:{userId}";

    public override async Task OnConnectedAsync()
    {
        if (Context.User?.FindFirst("sub")?.Value is { } userId)
            await Groups.AddToGroupAsync(Context.ConnectionId, Group(userId));
        await base.OnConnectedAsync();
    }
}

/// <summary>Tells a user's other connected devices that their data changed.</summary>
public class SyncNotifier(IHubContext<SyncHub> hub, ILogger<SyncNotifier> log)
{
    public async Task ChangedAsync(string userId, long cursor, string? exceptConnection)
    {
        try
        {
            var group = SyncHub.Group(userId);
            var clients = string.IsNullOrEmpty(exceptConnection) ? hub.Clients.Group(group) : hub.Clients.GroupExcept(group, exceptConnection);
            await clients.SendAsync(SyncHub.Changed, cursor);
        }
        catch (Exception e)
        {
            // Best effort: the other devices still catch up at their next sync.
            log.LogWarning(e, "Couldn't notify the other devices of {User}", userId);
        }
    }
}

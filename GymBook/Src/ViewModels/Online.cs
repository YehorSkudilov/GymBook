using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.ViewModels;

/// <summary>
/// Calls to the API from the social pages (ranks, public profile, shared plans), which only work online: a refusal or
/// a lost connection is shown to the user, and the call comes back null.
/// </summary>
public static class Online
{
    public static async Task<T?> Try<T>(DialogService dialogs, string failTitle, Func<Task<T>> call) where T : class
    {
        try
        {
            return await call();
        }
        catch (Exception e) when (Message(e) is { } message)
        {
            await dialogs.Alert(failTitle, message);
            return null;
        }
    }

    /// <summary>For calls with nothing to return: true when it went through.</summary>
    public static async Task<bool> Try(DialogService dialogs, string failTitle, Func<Task> call) =>
        await Try(dialogs, failTitle, async () =>
        {
            await call();
            return new object();
        }) != null;

    /// <summary>What to tell the user about a failed call; null for exceptions that aren't about the call failing.</summary>
    public static string? Message(Exception e) => e switch
    {
        ApiException or SessionExpiredException => e.Message,
        HttpRequestException or TaskCanceledException => "Couldn't reach Gym Book. Check your connection and try again.",
        _ => null,
    };
}

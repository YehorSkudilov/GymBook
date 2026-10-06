using System.Collections.Concurrent;

namespace GymBook.Controls.RichText;

/// <summary>
/// The pictures shown in a <see cref="RichEditor"/>, downloaded once each and kept while the app runs. Attachments come
/// from the API's files address, which needs no sign-in.
/// </summary>
public static class RichImages
{
    /// <summary>The widest a picture is shown, in the platform's units.</summary>
    public const double MaxWidth = 240;

    static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(30) };
    static readonly ConcurrentDictionary<string, Task<byte[]?>> Downloads = new();

    /// <summary>The picture's bytes; null if it couldn't be had.</summary>
    public static Task<byte[]?> GetAsync(string url) => Downloads.GetOrAdd(url, async u =>
    {
        try
        {
            return await Http.GetByteArrayAsync(u);
        }
        catch (Exception)
        {
            // Tried again next time it's shown.
            Downloads.TryRemove(u, out _);
            return null;
        }
    });

    /// <summary>The bytes if they're here already; otherwise starts the download and calls <paramref name="arrived"/> (on the main thread) when it's done.</summary>
    public static byte[]? Get(string url, Action arrived)
    {
        var download = GetAsync(url);
        if (download.IsCompletedSuccessfully)
            return download.Result;
        _ = download.ContinueWith(t =>
        {
            if (t.IsCompletedSuccessfully && t.Result != null)
                MainThread.BeginInvokeOnMainThread(arrived);
        }, TaskScheduler.Default);
        return null;
    }
}

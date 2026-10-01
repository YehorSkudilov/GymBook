namespace GymBook.Controls;

/// <summary>
/// YouTube's embedded player for one video. YouTube refuses to play (error 153) unless it's told which site embeds
/// it, so the player page is opened directly with a Referer header on each platform, instead of an iframe in a page
/// of our own (which has no origin a web view will send).
/// The web view stays invisible until the player page has loaded, so a platform's connection error page is never
/// shown: when the page can't load, it's cleared and <see cref="LoadFailed"/> is raised instead.
/// </summary>
public class YouTubePlayer : WebView
{
    const string Origin = "https://gymbook.app";
    const string Blank = "about:blank";

    /// <summary>The player page couldn't be loaded (no connection, a timeout); the player has been cleared.</summary>
    public event EventHandler? LoadFailed;

    public YouTubePlayer()
    {
        Opacity = 0;
        Navigated += OnNavigated;
    }

    public static readonly BindableProperty UrlProperty =
        BindableProperty.Create(nameof(Url), typeof(string), typeof(YouTubePlayer), null, propertyChanged: (b, _, _) => ((YouTubePlayer)b).Load());

    /// <summary>The embed URL (<see cref="Models.ExerciseVideo.EmbedUrl"/>); null stops and clears the player.</summary>
    public string? Url
    {
        get => (string?)GetValue(UrlProperty);
        set => SetValue(UrlProperty, value);
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();
        Load();
    }

    void Load() => Open(Url);

    /// <summary>Opens the player for <paramref name="embedUrl"/>, or a blank page for null.</summary>
    void Open(string? embedUrl)
    {
        // Hidden until this page has loaded (OnNavigated), so nothing half-loaded or failed is ever seen.
        Opacity = 0;
        if (Handler?.PlatformView == null)
            return;
        var url = embedUrl == null ? Blank : $"{embedUrl}&origin={Uri.EscapeDataString(Origin)}&widget_referrer={Uri.EscapeDataString(Origin + "/")}";
#if ANDROID
        if (Handler.PlatformView is Android.Webkit.WebView view)
        {
            view.Settings.JavaScriptEnabled = true;
            view.Settings.DomStorageEnabled = true;
            view.Settings.MediaPlaybackRequiresUserGesture = false;
            view.LoadUrl(url, new Dictionary<string, string> { ["Referer"] = Origin + "/" });
        }
#elif WINDOWS
        if (Handler.PlatformView is Microsoft.UI.Xaml.Controls.WebView2 view)
            _ = LoadWindows(view, url);
#elif IOS || MACCATALYST
        if (Handler.PlatformView is WebKit.WKWebView view)
        {
            var request = new Foundation.NSMutableUrlRequest(new Foundation.NSUrl(url));
            request["Referer"] = Origin + "/";
            view.LoadRequest(request);
        }
#endif
    }

    void OnNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (Url == null || e.Url == null || e.Url.StartsWith(Blank, StringComparison.OrdinalIgnoreCase))
            return;
        if (e.Result == WebNavigationResult.Success)
        {
            Opacity = 1;
            return;
        }
        if (e.Result == WebNavigationResult.Cancel)
            return;
        // Failure or timeout: drop the error page the platform shows, before it can be seen. (Url itself is left to
        // its binding: setting it here would clear the binding.)
        Open(null);
        LoadFailed?.Invoke(this, EventArgs.Empty);
    }

#if WINDOWS
    /// <summary>
    /// WebView2 drops a Referer set by hand, so on Windows the player is an iframe in a local page served under the
    /// app's own https host (a virtual host mapped to a folder): YouTube then sees a real embedding site.
    /// </summary>
    static async Task LoadWindows(Microsoft.UI.Xaml.Controls.WebView2 view, string url)
    {
        await view.EnsureCoreWebView2Async();
        if (url == Blank)
        {
            view.CoreWebView2.Navigate(url);
            return;
        }
        var folder = Path.Combine(FileSystem.CacheDirectory, "player");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "player.html"), $$"""
            <!DOCTYPE html>
            <html><head><meta charset="utf-8"><meta name="referrer" content="strict-origin-when-cross-origin">
            <style>html,body{margin:0;height:100%;background:#000;overflow:hidden}iframe{border:0;width:100%;height:100%}</style>
            </head><body>
            <script>
            // Only with a connection, and gone the moment it's lost: the iframe would show the browser's offline page.
            const src = "{{System.Web.HttpUtility.JavaScriptStringEncode(url)}}";
            function show() {
              if (!navigator.onLine || document.querySelector("iframe")) return;
              const f = document.createElement("iframe");
              f.src = src;
              f.allow = "autoplay; encrypted-media; picture-in-picture; fullscreen";
              f.allowFullscreen = true;
              document.body.appendChild(f);
            }
            addEventListener("online", show);
            addEventListener("offline", () => document.querySelector("iframe")?.remove());
            show();
            </script>
            </body></html>
            """);
        view.CoreWebView2.SetVirtualHostNameToFolderMapping("gymbook.app", folder, Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
        // A fresh query each time so the page isn't served from cache with the previous video.
        view.CoreWebView2.Navigate($"{Origin}/player.html?v={Guid.NewGuid():N}");
    }
#endif
}

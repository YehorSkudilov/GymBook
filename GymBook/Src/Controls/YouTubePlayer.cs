namespace GymBook.Controls;

/// <summary>
/// YouTube's embedded player for one video. YouTube refuses to play (error 153) unless it's told which site embeds
/// it, so the player page is opened directly with a Referer header on each platform, instead of an iframe in a page
/// of our own (which has no origin a web view will send).
/// </summary>
public class YouTubePlayer : WebView
{
    const string Origin = "https://gymbook.app";

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
#if ANDROID
        if (Handler?.PlatformView is Android.Webkit.WebView web)
        {
            web.Touch -= OnTouch;
            web.Touch += OnTouch;
        }
#endif
        Load();
    }

#if ANDROID
    float _downX, _downY;
    bool _pageScrolls;

    // The player takes every touch that lands on it, so a swipe over it wouldn't scroll the page. Once a drag is mostly
    // up or down, the player lets go of it (and doesn't see its moves, so it can't take it back), and the page's scroll
    // view takes over from there, fling and all. Taps and sideways drags (seeking) still go to the player.
    void OnTouch(object? sender, Android.Views.View.TouchEventArgs e)
    {
        e.Handled = false;
        if (sender is not Android.Webkit.WebView web || e.Event is not { } ev)
            return;
        switch (ev.ActionMasked)
        {
            case Android.Views.MotionEventActions.Down:
                (_downX, _downY, _pageScrolls) = (ev.GetX(), ev.GetY(), false);
                break;
            case Android.Views.MotionEventActions.Move:
                var dx = Math.Abs(ev.GetX() - _downX);
                var dy = Math.Abs(ev.GetY() - _downY);
                var slop = Android.Views.ViewConfiguration.Get(web.Context)?.ScaledTouchSlop ?? 16;
                if (_pageScrolls || (dy > slop && dy > dx))
                {
                    _pageScrolls = true;
                    web.Parent?.RequestDisallowInterceptTouchEvent(false);
                    e.Handled = true;
                }
                break;
        }
    }
#endif

    void Load()
    {
        if (Handler?.PlatformView == null)
            return;
        var url = Url == null ? "about:blank" : $"{Url}&origin={Uri.EscapeDataString(Origin)}&widget_referrer={Uri.EscapeDataString(Origin + "/")}";
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

#if WINDOWS
    /// <summary>
    /// WebView2 drops a Referer set by hand, so on Windows the player is an iframe in a local page served under the
    /// app's own https host (a virtual host mapped to a folder): YouTube then sees a real embedding site.
    /// </summary>
    static async Task LoadWindows(Microsoft.UI.Xaml.Controls.WebView2 view, string url)
    {
        await view.EnsureCoreWebView2Async();
        if (url == "about:blank")
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
            <iframe src="{{System.Net.WebUtility.HtmlEncode(url)}}" allow="autoplay; encrypted-media; picture-in-picture; fullscreen" allowfullscreen></iframe>
            </body></html>
            """);
        view.CoreWebView2.SetVirtualHostNameToFolderMapping("gymbook.app", folder, Microsoft.Web.WebView2.Core.CoreWebView2HostResourceAccessKind.Allow);
        // A fresh query each time so the page isn't served from cache with the previous video.
        view.CoreWebView2.Navigate($"{Origin}/player.html?v={Guid.NewGuid():N}");
    }
#endif
}

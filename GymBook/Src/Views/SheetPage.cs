namespace GymBook.Views;

/// <summary>
/// A modal page whose content slides up from the bottom over a fading <see cref="Backdrop"/>, and slides back down
/// before any navigation that removes it. Present it with Shell.PresentationMode="ModalNotAnimated" so the platform
/// doesn't animate it as well.
/// </summary>
public class SheetPage : BasePage
{
    const uint InLength = 320, OutLength = 240;
    const int FirstFrameDelay = 60;

    public static readonly BindableProperty BackdropProperty =
        BindableProperty.Create(nameof(Backdrop), typeof(Color), typeof(SheetPage), Colors.Transparent);

    /// <summary>The colour behind the sheet once it's up; faded in and out with it.</summary>
    public Color Backdrop
    {
        get => (Color)GetValue(BackdropProperty);
        set => SetValue(BackdropProperty, value);
    }

    bool _shown, _closing;

    public SheetPage()
    {
        BackgroundColor = Colors.Transparent;
    }

    // Start below the screen, so the first frame doesn't flash the sheet in place.
    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName == nameof(Content) && Content != null && !_shown)
            Content.TranslationY = OffScreen;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        // No Shell during first-run onboarding: then only CloseAsync closes the sheet.
        if (Shell.Current is { } shell)
            shell.Navigating += OnShellNavigating;
        if (Content == null)
            return;
        if (_shown)
        {
            // Back from a page on top, or a navigation away that didn't happen: the sheet belongs on screen.
            _closing = false;
            Content.TranslationY = 0;
            BackgroundColor = Backdrop;
            return;
        }
        _shown = true;
        Content.TranslationY = OffScreen;
        SlideIn(Content);
    }

    // A heavy page (the workout) can spend longer than the whole animation on its first layout and frame, which would
    // make the sheet jump straight into place. So wait until it's laid out and drawn once, then slide.
    async void SlideIn(View content)
    {
        if (content.Height <= 0)
        {
            var laidOut = new TaskCompletionSource();
            void OnSized(object? sender, EventArgs e)
            {
                if (content.Height > 0)
                    laidOut.TrySetResult();
            }
            content.SizeChanged += OnSized;
            await laidOut.Task;
            content.SizeChanged -= OnSized;
        }
        await Task.Delay(FirstFrameDelay);
        if (_closing)
            return;
        content.TranslationY = content.Height;
        this.Animate("backdrop", a => BackgroundColor = Backdrop.WithAlpha(Backdrop.Alpha * (float)a), length: InLength);
        await content.TranslateToAsync(0, 0, InLength, Easing.CubicOut);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        if (Shell.Current is { } shell)
            shell.Navigating -= OnShellNavigating;
    }

    // Holds up any navigation that takes this page off the stack (back, close, or replacing it) until the sheet is down.
    // Opening a page on top of this one (a route, or a modal like the exercise picker) leaves the sheet where it is,
    // and so does closing that page again.
    async void OnShellNavigating(object? sender, ShellNavigatingEventArgs e)
    {
        if (_closing || Content == null || e.Current == null || e.Source == ShellNavigationSource.Push || !IsOnTop())
            return;
        var current = Path(e.Current.Location);
        if (Path(e.Target.Location).StartsWith(current, StringComparison.OrdinalIgnoreCase))
            return;

        _closing = true;
        var deferral = e.GetDeferral();
        try
        {
            await SlideOutAsync();
        }
        finally
        {
            deferral.Complete();
        }
    }

    async Task SlideOutAsync()
    {
        if (Content == null)
            return;
        this.AbortAnimation("backdrop");
        var from = BackgroundColor;
        this.Animate("backdrop", a => BackgroundColor = from.WithAlpha(from.Alpha * (float)(1 - a)), length: OutLength);
        await Content.TranslateToAsync(0, Height > 0 ? Height : OffScreen, OutLength, Easing.CubicIn);
    }

    /// <summary>
    /// Slides the sheet down, then closes it. For sheets opened with Navigation.PushModalAsync(page, false) rather than a
    /// Shell route, whose closing Shell doesn't announce.
    /// </summary>
    public async Task CloseAsync()
    {
        if (_closing)
            return;
        _closing = true;
        await SlideOutAsync();
        await Navigation.PopModalAsync(false);
    }

    // The topmost modal, which may be wrapped in a NavigationPage; Shell's CurrentPage doesn't count modals pushed directly.
    bool IsOnTop()
    {
        var top = (Shell.Current?.Navigation ?? Navigation).ModalStack.LastOrDefault();
        return top == this || (top is NavigationPage nav && nav.CurrentPage == this);
    }

    static string Path(Uri location)
    {
        var path = location.OriginalString;
        var query = path.IndexOf('?');
        return (query < 0 ? path : path[..query]).TrimEnd('/') + "/";
    }

    static double OffScreen
    {
        get
        {
            var display = DeviceDisplay.MainDisplayInfo;
            return display.Density > 0 ? display.Height / display.Density : 2000;
        }
    }
}

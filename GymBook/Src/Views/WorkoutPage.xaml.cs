using System.Collections.ObjectModel;
using AppSkeleton;
using GymBook.ViewModels;

namespace GymBook.Views;

public partial class WorkoutPage : SheetPage
{
    // A page per exercise: what the exercise pager (a CView, like the app's tabs) swipes between. Kept per exercise, so
    // one moved or added keeps its page (and where it's scrolled to).
    readonly ObservableCollection<CNavItem> _pages = [];
    readonly Dictionary<WorkoutExerciseViewModel, CNavItem> _pageFor = [];
    bool _showQueued;

    public WorkoutPage(WorkoutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        HorizontalMouseScroll.Attach(Strip);
        viewModel.ExerciseFinished += (exercise, all) => Dispatcher.Dispatch(() => _ = CelebrateAsync(exercise.Name, all));
        // Rest over: the banner stays until it's swiped away, or the rest isn't over any more (the next set started or
        // ticked, or more rest added).
        viewModel.RestFinished += () => Dispatcher.Dispatch(() => _ = ShowBannerAsync("timer", "#3F7DFF", "Rest over", "Time for your next set", stay: true));

        ExercisePager.CNavIconItems = _pages;
        // Swiped to an exercise: the pager is already showing it, so it just becomes the current one.
        ExercisePager.SwipeNavigationCommand = new Command<CNavItem>(item => _ = viewModel.SelectExercise((WorkoutExerciseViewModel)item.Page.BindingContext));
        viewModel.Exercises.CollectionChanged += (_, _) =>
        {
            SyncPages(viewModel);
            QueueShowCurrent(viewModel);
        };
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkoutViewModel.CurrentIndex))
                QueueShowCurrent(viewModel);
            if (e.PropertyName == nameof(WorkoutViewModel.IsRestOver) && !viewModel.IsRestOver && _bannerStays)
                _ = HideBannerAsync(_bannerId);
        };
        SyncPages(viewModel);
        QueueShowCurrent(viewModel);
    }

    // The pages in the exercises' order: the same page for an exercise already there, a new one for one just added.
    void SyncPages(WorkoutViewModel vm)
    {
        foreach (var gone in _pageFor.Keys.Except(vm.Exercises).ToList())
            _pageFor.Remove(gone);
        var wanted = vm.Exercises.Select(e =>
        {
            if (!_pageFor.TryGetValue(e, out var item))
                _pageFor[e] = item = new CNavItem { PageName = e.Name, Page = new WorkoutExerciseView { BindingContext = e } };
            return item;
        }).ToList();
        if (wanted.SequenceEqual(_pages))
            return;
        _pages.Clear();
        foreach (var item in wanted)
            _pages.Add(item);
    }

    // After whatever changed the exercises or the current one has finished (the current index can follow a change to the
    // list a moment later): the pager slides over to the current exercise, and its photo scrolls into view. The first
    // one, or one after the one on screen was removed, comes in without a slide.
    void QueueShowCurrent(WorkoutViewModel vm)
    {
        if (_showQueued)
            return;
        _showQueued = true;
        Dispatcher.Dispatch(() =>
        {
            _showQueued = false;
            if (vm.CurrentExercise is not { } current || !_pageFor.TryGetValue(current, out var item))
                return;
            ExercisePager.SetContent(item);
            ScrollStripTo(vm.CurrentIndex);
        });
    }

    int _celebrationId;

    // An exercise done: a burst of confetti and a banner (a bigger one when that was the whole workout).
    async Task CelebrateAsync(string exercise, bool workoutDone)
    {
        var id = ++_celebrationId;
        Confetti.CancelAnimations();
        Confetti.Restart();
        Confetti.Opacity = 1;
        Confetti.IsVisible = true;
        Confetti.IsRunning = true;
        _ = workoutDone
            ? ShowBannerAsync("emoji_events", "#FFB020", "All sets done!", "Tap Finish to wrap up the workout")
            : ShowBannerAsync("check_circle", "#2ED47A", $"{exercise} done", "Nice work, on to the next one");
        await Task.Delay(workoutDone ? 3200 : 2200);
        if (id != _celebrationId)
            return;
        await Confetti.FadeTo(0, 500, Easing.CubicIn);
        if (id != _celebrationId)
            return;
        Confetti.IsRunning = false;
        Confetti.IsVisible = false;
    }

    int _bannerId;
    /// <summary>The banner showing stays until it's swiped away (rest over), rather than going by itself.</summary>
    bool _bannerStays;

    // Drops in from the top with a little bounce, stays a moment (or until swiped away), then floats back up and fades.
    async Task ShowBannerAsync(string glyph, string color, string title, string text, bool stay = false)
    {
        var id = ++_bannerId;
        _bannerStays = stay;
        BannerIcon.Source = new FontImageSource { FontFamily = "OutlinedIcons", Glyph = glyph, Color = Color.FromArgb(color), Size = 48 };
        BannerTitle.Text = title;
        BannerText.Text = text;
        Banner.AbortAnimation("banner");
        Banner.TranslationX = 0;
        Banner.TranslationY = -40;
        Banner.Scale = 0.9;
        Banner.Opacity = 0;
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // Not every device has haptics.
        }
        // Takes touches (the swipe) only while it's up.
        Banner.InputTransparent = false;
        await Task.WhenAll(
            Banner.FadeTo(1, 220, Easing.CubicOut),
            Banner.TranslateTo(0, 0, 420, Easing.SpringOut),
            Banner.ScaleTo(1, 420, Easing.SpringOut));
        if (stay)
            return;
        await Task.Delay(1800);
        await HideBannerAsync(id);
    }

    /// <summary>Floats banner <paramref name="id"/> back up and away, unless a newer one took over meanwhile.</summary>
    async Task HideBannerAsync(int id)
    {
        if (id != _bannerId)
            return;
        _bannerStays = false;
        Banner.InputTransparent = true;
        await Task.WhenAll(Banner.FadeTo(0, 260, Easing.CubicIn), Banner.TranslateTo(Banner.TranslationX, -24, 260, Easing.CubicIn));
    }

    // Follows the finger up or sideways; let go far enough and it's gone, otherwise it springs back.
    async void OnBannerPanned(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                Banner.TranslationX = e.TotalX;
                Banner.TranslationY = Math.Min(0, e.TotalY);
                Banner.Opacity = Math.Clamp(1 - Math.Max(Math.Abs(e.TotalX) / 220, -Math.Min(0, e.TotalY) / 120), 0.2, 1);
                break;
            case GestureStatus.Completed or GestureStatus.Canceled:
                var id = _bannerId;
                if (Math.Abs(Banner.TranslationX) > 70 || Banner.TranslationY < -30)
                {
                    _bannerStays = false;
                    Banner.InputTransparent = true;
                    var sideways = Math.Abs(Banner.TranslationX) > -Banner.TranslationY;
                    await Task.WhenAll(
                        Banner.FadeTo(0, 180, Easing.CubicIn),
                        Banner.TranslateTo(sideways ? Math.Sign(Banner.TranslationX) * 400 : Banner.TranslationX, sideways ? Banner.TranslationY : -120, 180, Easing.CubicIn));
                    if (id == _bannerId)
                        Banner.TranslationX = 0;
                }
                else
                {
                    await Task.WhenAll(Banner.TranslateTo(0, 0, 250, Easing.SpringOut), Banner.FadeTo(1, 150));
                }
                break;
        }
    }

    // Keeps the current exercise's photo in view as you swipe through the workout.
    void ScrollStripTo(int index)
    {
        if (BindingContext is WorkoutViewModel vm && index >= 0 && index < vm.Exercises.Count)
            Strip.ScrollTo(index, position: ScrollToPosition.Center, animate: true);
    }
}

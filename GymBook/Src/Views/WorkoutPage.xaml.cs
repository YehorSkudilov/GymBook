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
        viewModel.RestFinished += () => Dispatcher.Dispatch(() => _ = ShowBannerAsync("timer", "#3F7DFF", "Rest over", "Time for your next set"));

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

    // Drops in from the top with a little bounce, stays a moment, then floats back up and fades.
    async Task ShowBannerAsync(string glyph, string color, string title, string text)
    {
        var id = ++_bannerId;
        BannerIcon.Source = new FontImageSource { FontFamily = "OutlinedIcons", Glyph = glyph, Color = Color.FromArgb(color), Size = 48 };
        BannerTitle.Text = title;
        BannerText.Text = text;
        Banner.AbortAnimation("banner");
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
        await Task.WhenAll(
            Banner.FadeTo(1, 220, Easing.CubicOut),
            Banner.TranslateTo(0, 0, 420, Easing.SpringOut),
            Banner.ScaleTo(1, 420, Easing.SpringOut));
        await Task.Delay(1800);
        // A newer banner took over meanwhile: leave it be.
        if (id != _bannerId)
            return;
        await Task.WhenAll(Banner.FadeTo(0, 260, Easing.CubicIn), Banner.TranslateTo(0, -24, 260, Easing.CubicIn));
    }

    // Keeps the current exercise's photo in view as you swipe through the workout.
    void ScrollStripTo(int index)
    {
        if (BindingContext is WorkoutViewModel vm && index >= 0 && index < vm.Exercises.Count)
            Strip.ScrollTo(index, position: ScrollToPosition.Center, animate: true);
    }
}

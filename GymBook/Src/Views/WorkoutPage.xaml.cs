using GymBook.ViewModels;

namespace GymBook.Views;

public partial class WorkoutPage : SheetPage
{
    // How far the next exercise slides in from, and for how long.
    const double SlideDistance = 48;
    const uint SlideLength = 240;

    int _shownIndex = -1;

    public WorkoutPage(WorkoutViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        HorizontalMouseScroll.Attach(Strip);
        viewModel.ExerciseFinished += (exercise, all) => Dispatcher.Dispatch(() => _ = CelebrateAsync(exercise.Name, all));
        viewModel.RestFinished += () => Dispatcher.Dispatch(() => _ = ShowBannerAsync("timer", "#3F7DFF", "Rest over", "Time for your next set"));
        viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName != nameof(WorkoutViewModel.CurrentIndex))
                return;
            var index = viewModel.CurrentIndex;
            // Before the new exercise is bound in: hide the old one and park the body on the side it comes from.
            var animate = _shownIndex >= 0 && index != _shownIndex && ExerciseBody.Width > 0;
            if (animate)
            {
                ExerciseBody.AbortAnimation("slide");
                ExerciseBody.Opacity = 0;
                ExerciseBody.TranslationX = index > _shownIndex ? SlideDistance : -SlideDistance;
            }
            _shownIndex = index;
            Dispatcher.Dispatch(() =>
            {
                ScrollStripTo(index);
                // Each exercise starts at its top.
                _ = ExerciseScroll.ScrollToAsync(0, 0, false);
                if (animate)
                    SlideIn();
            });
        };
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

    void SlideIn()
    {
        var slide = new Animation();
        slide.Add(0, 1, new Animation(v => ExerciseBody.TranslationX = v, ExerciseBody.TranslationX, 0, Easing.CubicOut));
        slide.Add(0, 0.8, new Animation(v => ExerciseBody.Opacity = v, 0, 1));
        slide.Commit(ExerciseBody, "slide", length: SlideLength, finished: (_, _) =>
        {
            ExerciseBody.TranslationX = 0;
            ExerciseBody.Opacity = 1;
        });
    }

    // Keeps the current exercise's photo in view as you swipe through the workout.
    void ScrollStripTo(int index)
    {
        if (BindingContext is WorkoutViewModel vm && index >= 0 && index < vm.Exercises.Count)
            Strip.ScrollTo(index, position: ScrollToPosition.Center, animate: true);
    }
}

using GymBook.ViewModels;

namespace GymBook.Views;

public partial class WorkoutDonePage : BasePage
{
    bool _played;

    // Its own show plays instead.
    protected override bool AnimatesIn => false;

    public WorkoutDonePage(WorkoutDoneViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        if (_played)
            return;
        _played = true;
        // After the first frame, so the page is laid out and the animation runs smoothly from its start.
        Dispatcher.DispatchDelayed(TimeSpan.FromMilliseconds(60), () => _ = PlayAsync());
    }

    // The screen fades in, the trophy pops up with a wiggle, the words and numbers rise in one after the other while the
    // numbers count up, then a personal-best pill and Continue.
    async Task PlayAsync()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.LongPress);
        }
        catch
        {
            // Not every device has haptics.
        }
        await Root.FadeTo(1, 200, Easing.CubicOut);

        await Trophy.ScaleTo(1.15, 380, Easing.CubicOut);
        _ = Trophy.ScaleTo(1, 220, Easing.SpringOut);
        _ = Wiggle();

        _ = Rise(Heading, 0);
        _ = Rise(Stats, 120);
        await Task.Delay(200);
        if (BindingContext is WorkoutDoneViewModel vm)
            new Animation(v => vm.Reveal = v, 0, 1, Easing.CubicOut).Commit(this, "count", length: 1100);

        await Task.Delay(900);
        if (RecordsPill.IsVisible)
        {
            await RecordsPill.ScaleTo(1.12, 260, Easing.CubicOut);
            await RecordsPill.ScaleTo(1, 160, Easing.SpringOut);
        }
        await Rise(ContinueButton, 0);
        _ = Breathe();
    }

    static async Task Rise(VisualElement view, int delay)
    {
        if (delay > 0)
            await Task.Delay(delay);
        await Task.WhenAll(view.FadeTo(1, 320, Easing.CubicOut), view.TranslateTo(0, 0, 380, Easing.CubicOut));
    }

    async Task Wiggle()
    {
        foreach (var angle in new[] { -12.0, 10, -6, 3, 0 })
            await TrophyIcon.RotateTo(angle, 110, Easing.SinInOut);
    }

    // The trophy keeps gently breathing while the screen is up.
    async Task Breathe()
    {
        while (IsLoaded)
        {
            await TrophyCircle.ScaleTo(1.05, 1200, Easing.SinInOut);
            await TrophyCircle.ScaleTo(1, 1200, Easing.SinInOut);
        }
    }

    // Back does what Continue does: the workout is saved, so there's nothing to go back to.
    protected override bool OnBackButtonPressed()
    {
        if (BindingContext is WorkoutDoneViewModel vm)
            vm.ContinueCommand.Execute(null);
        return true;
    }
}

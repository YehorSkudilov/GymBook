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

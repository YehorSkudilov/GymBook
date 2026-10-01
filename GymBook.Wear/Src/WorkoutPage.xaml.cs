namespace GymBook.Wear;

/// <summary>A workout started on the watch (see <see cref="WorkoutViewModel"/>).</summary>
public partial class WorkoutPage : ContentPage, ILivePage
{
    readonly WorkoutViewModel _vm;

    public WorkoutPage(WorkoutViewModel vm)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        BindingContext = _vm = vm;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _vm.Ended += OnEnded;
        await _vm.StartAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _vm.Ended -= OnEnded;
        _vm.Stop();
    }

    public Task ResumeAsync() => _vm.StartAsync();

    public void Pause() => _vm.Stop();

    // Finished or discarded: back to the watch's home, which syncs it.
    async void OnEnded()
    {
        _vm.Ended -= OnEnded;
        if (Navigation.NavigationStack.LastOrDefault() == this)
            await Navigation.PopAsync();
    }
}

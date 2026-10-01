namespace GymBook.Wear;

/// <summary>The workout in progress on the phone, mirrored (see <see cref="CompanionViewModel"/>).</summary>
public partial class CompanionPage : ContentPage, ILivePage
{
    readonly CompanionViewModel _vm;

    public CompanionPage(CompanionViewModel vm)
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

    // Finished or discarded on the phone: back to the watch's home.
    async void OnEnded()
    {
        _vm.Ended -= OnEnded;
        if (Navigation.NavigationStack.LastOrDefault() == this)
            await Navigation.PopAsync();
    }
}

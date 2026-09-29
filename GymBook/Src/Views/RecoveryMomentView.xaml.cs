using GymBook.ViewModels;

namespace GymBook.Views;

public partial class RecoveryMomentView : ContentView
{
    RecoveryMoment? _moment;

    public RecoveryMomentView()
    {
        InitializeComponent();
        HorizontalMouseScroll.Attach(Strip);
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        if (_moment != null)
            _moment.Changed -= OnMomentChanged;
        _moment = BindingContext as RecoveryMoment;
        if (_moment != null)
            _moment.Changed += OnMomentChanged;
    }

    // Keep the stop being shown in view as the slider moves along the strip.
    void OnMomentChanged(object? sender, EventArgs e) => Dispatcher.Dispatch(async () =>
    {
        if (_moment == null || StripItems.Children.ElementAtOrDefault((int)Math.Round(_moment.Index)) is not Element item)
            return;
        await Strip.ScrollToAsync(item, ScrollToPosition.Center, true);
    });
}

namespace GymBook.Views;

public partial class AccountCard : ContentView
{
    public AccountCard()
    {
        InitializeComponent();
        if (DeviceInfo.Idiom == DeviceIdiom.Desktop)
            SizeChanged += (_, _) => FitColumns();
    }

    // Desktop: the app's side next to the form when there's room for both, the form alone when there isn't.
    bool? _wide;

    void FitColumns()
    {
        var wide = Width >= 750;
        if (_wide == wide)
            return;
        _wide = wide;
        Brand.IsVisible = wide;
        Columns.ColumnSpacing = wide ? 10 : 0;
        Columns.ColumnDefinitions[0].Width = wide ? new GridLength(0.42, GridUnitType.Star) : new GridLength(0);
        Columns.ColumnDefinitions[1].Width = wide ? new GridLength(0.58, GridUnitType.Star) : GridLength.Star;
    }
}

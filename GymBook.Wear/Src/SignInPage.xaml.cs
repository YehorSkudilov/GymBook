namespace GymBook.Wear;

/// <summary>Email sign-in (see <see cref="SignInViewModel"/>).</summary>
public partial class SignInPage : ContentPage
{
    public SignInPage(SignInViewModel vm)
    {
        InitializeComponent();
        NavigationPage.SetHasNavigationBar(this, false);
        BindingContext = vm;
        vm.SignedIn += async () => await Navigation.PopAsync();
    }
}

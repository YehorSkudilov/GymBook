using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Services.Sync;

namespace GymBook.Wear;

/// <summary>Email and password sign-in on the watch's keyboard, for when "Sign in with phone" can't be used.</summary>
public partial class SignInViewModel(WatchAccount account) : ObservableObject
{
    [ObservableProperty] string email = "";
    [ObservableProperty] string password = "";
    [ObservableProperty] string message = "";
    [ObservableProperty] bool isBusy;

    /// <summary>Signed in: the page goes back to the home.</summary>
    public event Action? SignedIn;

    [RelayCommand]
    async Task SignIn()
    {
        if (IsBusy)
            return;
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrEmpty(Password))
        {
            Message = "Enter your email and password.";
            return;
        }
        IsBusy = true;
        Message = "";
        try
        {
            await account.SignInAsync(Email, Password);
            Password = "";
            SignedIn?.Invoke();
        }
        catch (ApiException e)
        {
            Message = e.Message;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            Message = "Couldn't reach the server. Check the watch's connection.";
        }
        finally
        {
            IsBusy = false;
        }
    }
}

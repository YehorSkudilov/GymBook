using CommunityToolkit.Mvvm.ComponentModel;
using GymBook.Admin.Services;

namespace GymBook.Admin.ViewModels;

public abstract partial class BaseViewModel : ObservableObject
{
    [ObservableProperty] bool isBusy;

    /// <summary>Why the last load or action failed; shown on the page, empty when it worked.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    string error = "";

    public bool HasError => Error.Length > 0;

    /// <summary>Called every time the page appears.</summary>
    public virtual Task OnAppearingAsync() => Task.CompletedTask;

    /// <summary>Runs <paramref name="work"/> with the busy flag up, turning failures into <see cref="Error"/>. False when it failed.</summary>
    protected async Task<bool> RunAsync(Func<Task> work)
    {
        IsBusy = true;
        Error = "";
        try
        {
            await work();
            return true;
        }
        catch (SessionExpiredException)
        {
            // AdminApi raised SessionExpired, and the app is already on its way back to sign-in.
            return false;
        }
        catch (ApiException e)
        {
            Error = e.Message;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            Error = "Couldn't reach the Gym Book server. Check your connection and try again.";
        }
        finally
        {
            IsBusy = false;
        }
        return false;
    }

    protected static Page CurrentPage => Shell.Current?.CurrentPage ?? Application.Current!.Windows[0].Page!;

    protected static Task<bool> ConfirmAsync(string title, string message, string accept) =>
        CurrentPage.DisplayAlertAsync(title, message, accept, "Cancel");

    protected static Task AlertAsync(string title, string message) => CurrentPage.DisplayAlertAsync(title, message, "OK");
}

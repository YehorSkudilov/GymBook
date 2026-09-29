using Android.Gms.Auth.Api.SignIn;
using GymBook.Services.Sync;

namespace GymBook;

/// <summary>
/// Google's account picker via Play Services. The picker runs as an activity whose result comes back through
/// <see cref="MainActivity.OnActivityResult"/>, which hands it to <see cref="HandleActivityResult"/>.
/// </summary>
public class GoogleSignInService : IGoogleSignIn
{
    const int RequestCode = 9001;
    // GoogleSignInStatusCodes.SignInCancelled: the user backed out of the picker.
    const int Cancelled = 12501;

    TaskCompletionSource<string?>? _pending;
    GoogleSignInClient? _client;

    public bool IsAvailable => !string.IsNullOrWhiteSpace(GoogleConfig.WebClientId);

    GoogleSignInClient Client(Android.App.Activity activity) => _client ??= GoogleSignIn.GetClient(activity,
        new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
            // Addressed to the Web client, which is what the API checks the token's audience against.
            .RequestIdToken(GoogleConfig.WebClientId)
            .RequestEmail()
            .Build());

    public Task<string?> SignInAsync()
    {
        if (!IsAvailable || Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is not { } activity)
            throw new GoogleSignInException("Google sign-in isn't available on this device.");

        _pending?.TrySetResult(null);
        _pending = new TaskCompletionSource<string?>();
        activity.StartActivityForResult(Client(activity).SignInIntent, RequestCode);
        return _pending.Task;
    }

    public Task SignOutAsync()
    {
        // Play Services remembers the account across app restarts, so this may need a client that hasn't been made yet.
        if (!IsAvailable || Microsoft.Maui.ApplicationModel.Platform.CurrentActivity is not { } activity)
            return Task.CompletedTask;
        var done = new TaskCompletionSource();
        Client(activity).SignOut().AddOnCompleteListener(new SignOutListener(done));
        return done.Task;
    }

    /// <summary>True when the result was the picker's.</summary>
    public bool HandleActivityResult(int requestCode, Android.Content.Intent? data)
    {
        if (requestCode != RequestCode || _pending is not { } pending)
            return false;
        _pending = null;

        try
        {
            var account = GoogleSignIn.GetSignedInAccountFromIntent(data)
                .GetResult(Java.Lang.Class.FromType(typeof(Android.Gms.Common.Apis.ApiException))) as GoogleSignInAccount;
            if (account?.IdToken is { } token)
                pending.TrySetResult(token);
            else
                pending.TrySetException(new GoogleSignInException("Google didn't send back a sign-in token. Please try again."));
        }
        catch (Android.Gms.Common.Apis.ApiException e) when (e.StatusCode == Cancelled)
        {
            pending.TrySetResult(null);
        }
        catch (Android.Gms.Common.Apis.ApiException e)
        {
            pending.TrySetException(new GoogleSignInException($"Google sign-in failed (code {e.StatusCode}). Please try again."));
        }
        return true;
    }

    sealed class SignOutListener(TaskCompletionSource done) : Java.Lang.Object, Android.Gms.Tasks.IOnCompleteListener
    {
        public void OnComplete(Android.Gms.Tasks.Task task) => done.TrySetResult();
    }
}

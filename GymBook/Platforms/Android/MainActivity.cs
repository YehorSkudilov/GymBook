using Android.App;
using Android.Content.PM;
using Android.OS;

namespace GymBook
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        // The Google account picker reports back here.
        protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            (IPlatformApplication.Current?.Services.GetService<Services.Sync.IGoogleSignIn>() as GoogleSignInService)
                ?.HandleActivityResult(requestCode, data);
        }
    }
}

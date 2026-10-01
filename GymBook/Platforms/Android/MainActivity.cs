using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using GymBook.Services;

namespace GymBook
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            OpenWorkoutIfAsked(Intent);
        }

        // The workout notification tapped while the app was already running.
        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            OpenWorkoutIfAsked(intent);
        }

        static void OpenWorkoutIfAsked(Intent? intent)
        {
            if (WorkoutNotifier.IsOpenWorkout(intent))
                WorkoutLaunch.Request();
        }

        // Rest alerts are only for when the app isn't on screen.
        protected override void OnResume()
        {
            base.OnResume();
            WorkoutNotifier.IsAppVisible = true;
        }

        protected override void OnPause()
        {
            base.OnPause();
            WorkoutNotifier.IsAppVisible = false;
        }

        // The Google account picker reports back here.
        protected override void OnActivityResult(int requestCode, Result resultCode, Android.Content.Intent? data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            (IPlatformApplication.Current?.Services.GetService<Services.Sync.IGoogleSignIn>() as GoogleSignInService)
                ?.HandleActivityResult(requestCode, data);
        }
    }
}

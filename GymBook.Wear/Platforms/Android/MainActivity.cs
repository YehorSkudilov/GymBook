using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Wear.Ambient;

namespace GymBook.Wear;

// A fixed Java name (not MAUI's generated one), so the Tile can open it by name.
[Activity(Name = "com.yehorskudilov.gymbook.MainActivity", Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // Ambient mode (see Ambient.cs): stay on screen, dimmed, rather than going to the watch face.
        Lifecycle.AddObserver(AmbientLifecycleObserverKt.AmbientLifecycleObserver(this, new AmbientCallback()));
    }

    sealed class AmbientCallback : Java.Lang.Object, IAmbientLifecycleObserverAmbientLifecycleCallback
    {
        public void OnEnterAmbient(AmbientLifecycleObserverAmbientDetails ambientDetails) => Ambient.Set(true);

        public void OnExitAmbient() => Ambient.Set(false);

        public void OnUpdateAmbient() => Ambient.Update();
    }
}

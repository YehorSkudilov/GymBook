using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Android.App;
using Android.Content;
using Android.Gms.Wearable;
using Android.Runtime;
using GymBook.Contracts;
using GymBook.Serialization;

namespace GymBook.Wear;

/// <summary>
/// The phone's news, also while the watch app isn't running (Wear OS starts it for them): a workout started on the
/// phone opens the app (<see cref="WearPaths.OpenApp"/>, as Google's Data Layer sample does), and every change to the
/// phone's workout keeps the Ongoing Activity on the watch face right.
/// </summary>
[Service(Exported = true)]
[IntentFilter(new[] { "com.google.android.gms.wearable.MESSAGE_RECEIVED" }, DataScheme = "wear", DataHost = "*", DataPathPrefix = "/gymbook")]
[IntentFilter(new[] { "com.google.android.gms.wearable.DATA_CHANGED" }, DataScheme = "wear", DataHost = "*", DataPathPrefix = "/gymbook")]
public class PhoneListenerService : WearableListenerService
{
    static readonly JsonTypeInfo<WearWorkout> WorkoutJson = (JsonTypeInfo<WearWorkout>)GymBookJson.Options.GetTypeInfo(typeof(WearWorkout));

    public override void OnMessageReceived(IMessageEvent message)
    {
        if (message.Path != WearPaths.OpenApp)
            return;
        try
        {
            var open = new Intent(this, typeof(MainActivity)).AddFlags(ActivityFlags.NewTask | ActivityFlags.SingleTop);
            StartActivity(open);
        }
        catch (Exception e)
        {
            // Not allowed to come to the front right now: the Ongoing Activity still offers it on the watch face.
            System.Diagnostics.Debug.WriteLine($"GymBook.Wear: couldn't open the app: {e.Message}");
        }
    }

    public override void OnDataChanged(DataEventBuffer dataEvents)
    {
        for (var i = 0; i < dataEvents.Count; i++)
        {
            var e = dataEvents.Get(i)!.JavaCast<IDataEvent>()!;
            if (e.Type != DataEvent.TypeChanged || e.DataItem?.Uri?.Path != WearPaths.Workout || e.DataItem.GetData() is not { } data)
                continue;
            try
            {
                if (JsonSerializer.Deserialize(data, WorkoutJson) is { } workout)
                    MainThread.BeginInvokeOnMainThread(() => WatchOngoing.Refresh(workout));
            }
            catch (JsonException)
            {
            }
        }
    }
}

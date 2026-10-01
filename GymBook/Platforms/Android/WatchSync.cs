using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Android.App;
using Android.Gms.Extensions;
using Android.Gms.Wearable;
using GymBook.Contracts;
using GymBook.Serialization;
using GymBook.Services;

namespace GymBook;

/// <summary>
/// Keeps the Wear OS app's copy of the workout current: rewrites the <see cref="WearPaths.Workout"/> data item whenever the
/// data changes. Ticks coming back from the watch arrive in <see cref="WatchListenerService"/>.
/// </summary>
public class WatchSync(DataStore store, WatchLink link)
{
    static readonly JsonTypeInfo<WearWorkout> WorkoutJson = (JsonTypeInfo<WearWorkout>)GymBookJson.Options.GetTypeInfo(typeof(WearWorkout));

    byte[]? _last;

    public void Start()
    {
        store.Changed += (_, _) => Push();
        Push();
    }

    void Push() => MainThread.BeginInvokeOnMainThread(async () =>
    {
        // Built on the main thread, where the workout changes; only sent when it's different from last time.
        var json = JsonSerializer.SerializeToUtf8Bytes(link.Snapshot(), WorkoutJson);
        if (_last != null && json.AsSpan().SequenceEqual(_last))
            return;
        _last = json;
        try
        {
            var request = PutDataRequest.Create(WearPaths.Workout).SetData(json).SetUrgent();
            await WearableClass.GetDataClient(Android.App.Application.Context).PutDataItem(request).AsAsync<Java.Lang.Object>();
        }
        catch (Exception e)
        {
            // No Google Play services or no Wear OS API on this phone: nothing to sync with. Try again next change.
            _last = null;
            System.Diagnostics.Debug.WriteLine($"Wear sync failed: {e.Message}");
        }
    });
}

/// <summary>
/// Receives the watch's messages, also when the app isn't open (Android starts it for them). Ticks are handed to
/// <see cref="WatchLink"/> on the main thread.
/// </summary>
[Service(Exported = true)]
[IntentFilter(new[] { "com.google.android.gms.wearable.MESSAGE_RECEIVED" }, DataScheme = "wear", DataHost = "*", DataPathPrefix = "/gymbook")]
public class WatchListenerService : WearableListenerService
{
    static readonly JsonTypeInfo<WearCompleteSet> CompleteSetJson = (JsonTypeInfo<WearCompleteSet>)GymBookJson.Options.GetTypeInfo(typeof(WearCompleteSet));

    public override void OnMessageReceived(IMessageEvent message)
    {
        if (message.Path != WearPaths.CompleteSet || message.GetData() is not { } data)
            return;
        WearCompleteSet? request;
        try
        {
            request = JsonSerializer.Deserialize(data, CompleteSetJson);
        }
        catch (JsonException)
        {
            return;
        }
        if (request != null && IPlatformApplication.Current?.Services.GetService<WatchLink>() is { } link)
            MainThread.BeginInvokeOnMainThread(() => link.CompleteSet(request));
    }
}

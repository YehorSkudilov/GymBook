using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Android.App;
using Android.Gms.Extensions;
using Android.Gms.Wearable;
using Android.Runtime;
using GymBook.Contracts;
using GymBook.Serialization;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook;

/// <summary>
/// Keeps the Wear OS app's copy of the workout current: rewrites the <see cref="WearPaths.Workout"/> data item whenever the
/// data changes. Ticks coming back from the watch arrive in <see cref="WatchListenerService"/>. And once a change made
/// here (or pulled from elsewhere) is synced to the account, tells the watch to sync too (<see cref="WearPaths.SyncNow"/>),
/// so plans, history and everything else on it follow the phone without opening the watch app or signing it in by hand.
/// </summary>
public class WatchSync(DataStore store, WatchLink link, SyncService sync)
{
    // Data changed since the watch was last told; it's told once that change is on the account.
    bool _changed = true;

    static readonly JsonTypeInfo<WearWorkout> WorkoutJson = (JsonTypeInfo<WearWorkout>)GymBookJson.Options.GetTypeInfo(typeof(WearWorkout));

    byte[]? _last;
    string? _activeSession;
    bool _started;

    public void Start()
    {
        store.Changed += (_, _) =>
        {
            _changed = true;
            Push();
        };
        sync.StatusChanged += (_, _) =>
        {
            if (sync.State == SyncState.UpToDate && _changed)
            {
                _changed = false;
                _ = TellWatchToSyncAsync();
            }
        };
        Push();
    }

    /// <summary>Asks every connected watch to sync (its PhoneListenerService does, signing in through the phone if need be). Best effort.</summary>
    static async Task TellWatchToSyncAsync()
    {
        try
        {
            var context = Android.App.Application.Context;
            var nodes = await WearableClass.GetNodeClient(context).GetConnectedNodes().AsAsync<JavaList>();
            var messages = WearableClass.GetMessageClient(context);
            foreach (var node in nodes.OfType<Java.Lang.Object>().Select(n => n.JavaCast<INode>()))
                if (node?.Id != null)
                    await messages.SendMessage(node.Id, WearPaths.SyncNow, []).AsAsync<Java.Lang.Object>();
        }
        catch (Exception e)
        {
            // No watch, or no Wear OS API on this phone.
            System.Diagnostics.Debug.WriteLine($"Couldn't ask the watch to sync: {e.Message}");
        }
    }

    void Push() => MainThread.BeginInvokeOnMainThread(async () =>
    {
        // Built on the main thread, where the workout changes; only sent when it's different from last time.
        var workout = link.Snapshot();
        // A workout just started here (not one already running when the app opened): open the watch app to follow it.
        var justStarted = _started && workout.IsActive && workout.SessionId != _activeSession;
        _activeSession = workout.IsActive ? workout.SessionId : null;
        _started = true;
        if (justStarted)
            _ = OpenWatchAppAsync();
        var json = JsonSerializer.SerializeToUtf8Bytes(workout, WorkoutJson);
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

    /// <summary>Asks every connected watch to open Gym Book (its PhoneListenerService does). Best effort.</summary>
    static async Task OpenWatchAppAsync()
    {
        try
        {
            var context = Android.App.Application.Context;
            var nodes = await WearableClass.GetNodeClient(context).GetConnectedNodes().AsAsync<JavaList>();
            var messages = WearableClass.GetMessageClient(context);
            foreach (var node in nodes.OfType<Java.Lang.Object>().Select(n => n.JavaCast<INode>()))
                if (node?.Id != null)
                    await messages.SendMessage(node.Id, WearPaths.OpenApp, []).AsAsync<Java.Lang.Object>();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"Opening the watch app failed: {e.Message}");
        }
    }
}

/// <summary>
/// Receives the watch's messages, also when the app isn't open (Android starts it for them): ticks, handed to
/// <see cref="WatchLink"/> on the main thread, and "Sign in with phone", answered with a session of the watch's own.
/// </summary>
[Service(Exported = true)]
[IntentFilter(new[] { "com.google.android.gms.wearable.MESSAGE_RECEIVED" }, DataScheme = "wear", DataHost = "*", DataPathPrefix = "/gymbook")]
public class WatchListenerService : WearableListenerService
{
    static readonly JsonTypeInfo<WearCompleteSet> CompleteSetJson = (JsonTypeInfo<WearCompleteSet>)GymBookJson.Options.GetTypeInfo(typeof(WearCompleteSet));
    static readonly JsonTypeInfo<WearStartWorkout> StartJson = (JsonTypeInfo<WearStartWorkout>)GymBookJson.Options.GetTypeInfo(typeof(WearStartWorkout));
    static readonly JsonTypeInfo<WearFinishWorkout> FinishJson = (JsonTypeInfo<WearFinishWorkout>)GymBookJson.Options.GetTypeInfo(typeof(WearFinishWorkout));
    static readonly JsonTypeInfo<WearSession> SessionJson = (JsonTypeInfo<WearSession>)GymBookJson.Options.GetTypeInfo(typeof(WearSession));

    public override void OnMessageReceived(IMessageEvent message)
    {
        switch (message.Path)
        {
            case WearPaths.CompleteSet:
                CompleteSet(message.GetData());
                break;
            case WearPaths.StartWorkout:
                Handle(message.GetData(), StartJson, (link, request) => link.Start(request));
                break;
            case WearPaths.FinishWorkout:
                Handle(message.GetData(), FinishJson, (link, request) => link.Finish(request));
                break;
            case WearPaths.RequestSession when message.SourceNodeId is { } watch:
                _ = SendSessionAsync(watch);
                break;
            case WearPaths.HeartRate when message.GetData() is { } data
                && int.TryParse(System.Text.Encoding.ASCII.GetString(data), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var bpm):
                if (IPlatformApplication.Current?.Services.GetService<WatchLink>() is { } link)
                    MainThread.BeginInvokeOnMainThread(() => link.ReportHeartRate(bpm));
                break;
        }
    }

    /// <summary>A request from the watch, read and handed to <see cref="WatchLink"/> on the main thread; unreadable ones are ignored.</summary>
    static void Handle<T>(byte[]? data, JsonTypeInfo<T> json, Action<WatchLink, T> act) where T : class
    {
        if (data == null)
            return;
        T? request;
        try
        {
            request = JsonSerializer.Deserialize(data, json);
        }
        catch (JsonException)
        {
            return;
        }
        if (request != null && IPlatformApplication.Current?.Services.GetService<WatchLink>() is { } link)
            MainThread.BeginInvokeOnMainThread(() => act(link, request));
    }

    static void CompleteSet(byte[]? data)
    {
        if (data == null)
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

    /// <summary>
    /// A new session for the watch from the API (not a copy of this one: sharing a refresh token signs both out at the
    /// first refresh), or why there's none. Only this app's own watch app, signed with the same key, can ask.
    /// </summary>
    static async Task SendSessionAsync(string watch)
    {
        WearSession reply;
        var services = IPlatformApplication.Current?.Services;
        var session = services?.GetService<AuthSession>();
        var api = services?.GetService<ApiClient>();
        if (session == null || api == null)
            reply = new(null, "Open Gym Book on your phone and try again.");
        else
        {
            await session.EnsureLoadedAsync();
            if (!session.IsSignedIn)
                reply = new(null, "Sign in to Gym Book on your phone first.");
            else
            {
                try
                {
                    reply = new(await api.CreateDeviceSessionAsync(), null);
                }
                catch (ApiException e)
                {
                    reply = new(null, e.Message);
                }
                catch (SessionExpiredException)
                {
                    reply = new(null, "Sign in to Gym Book on your phone again first.");
                }
                catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
                {
                    reply = new(null, "Your phone is offline. Try again when it's connected.");
                }
            }
        }
        try
        {
            var data = JsonSerializer.SerializeToUtf8Bytes(reply, SessionJson);
            await WearableClass.GetMessageClient(Android.App.Application.Context).SendMessage(watch, WearPaths.Session, data).AsAsync<Java.Lang.Object>();
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"Wear sign-in reply failed: {e.Message}");
        }
    }
}

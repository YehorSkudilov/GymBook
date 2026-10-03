using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Android.Gms.Extensions;
using Android.Gms.Wearable;
using Android.Runtime;
using GymBook.Contracts;
using GymBook.Serialization;

namespace GymBook.Wear;

/// <summary>
/// The watch's side of the Wearable Data Layer: the phone's workout (the <see cref="WearPaths.Workout"/> data item, read
/// when the app opens and followed while it's open), ticks sent back as messages, and "Sign in with phone". See the
/// phone's WatchSync.cs.
/// </summary>
public class PhoneLink : Java.Lang.Object, DataClient.IOnDataChangedListener, MessageClient.IOnMessageReceivedListener
{
    static readonly JsonTypeInfo<WearSession> SessionJson = (JsonTypeInfo<WearSession>)GymBookJson.Options.GetTypeInfo(typeof(WearSession));
    static readonly TimeSpan SessionTimeout = TimeSpan.FromSeconds(20);

    TaskCompletionSource<WearSession>? _sessionReply;

    static readonly JsonTypeInfo<WearWorkout> WorkoutJson = (JsonTypeInfo<WearWorkout>)GymBookJson.Options.GetTypeInfo(typeof(WearWorkout));
    static readonly JsonTypeInfo<WearCompleteSet> CompleteSetJson = (JsonTypeInfo<WearCompleteSet>)GymBookJson.Options.GetTypeInfo(typeof(WearCompleteSet));
    static readonly JsonTypeInfo<WearStartWorkout> StartJson = (JsonTypeInfo<WearStartWorkout>)GymBookJson.Options.GetTypeInfo(typeof(WearStartWorkout));
    static readonly JsonTypeInfo<WearFinishWorkout> FinishJson = (JsonTypeInfo<WearFinishWorkout>)GymBookJson.Options.GetTypeInfo(typeof(WearFinishWorkout));

    /// <summary>
    /// The phone could be reached the last time it was checked (<see cref="CheckConnectedAsync"/>). While it can, the
    /// watch is the phone's companion: workouts start and run on the phone, the watch follows and drives them.
    /// </summary>
    public bool IsConnected { get; private set; }

    /// <summary>Whether a phone can be reached now (remembered in <see cref="IsConnected"/>). False without Google Play services.</summary>
    public async Task<bool> CheckConnectedAsync()
    {
        try
        {
            var nodes = await WearableClass.GetNodeClient(Context).GetConnectedNodes().AsAsync<JavaList>();
            IsConnected = nodes.OfType<Java.Lang.Object>().Any(n => n.JavaCast<INode>()?.Id != null);
        }
        catch (Exception)
        {
            IsConnected = false;
        }
        return IsConnected;
    }

    /// <summary>Asks the phone to start a workout. False when no phone could be reached.</summary>
    public Task<bool> StartOnPhoneAsync(WearStartWorkout request) =>
        SendToPhonesAsync(WearPaths.StartWorkout, JsonSerializer.SerializeToUtf8Bytes(request, StartJson));

    /// <summary>Asks the phone to finish or discard its workout. False when no phone could be reached.</summary>
    public Task<bool> FinishOnPhoneAsync(WearFinishWorkout request) =>
        SendToPhonesAsync(WearPaths.FinishWorkout, JsonSerializer.SerializeToUtf8Bytes(request, FinishJson));

    /// <summary>Waits for the phone to send a workout in progress (after asking it to start one); false when none comes in time.</summary>
    public async Task<bool> WaitForPhoneWorkoutAsync(TimeSpan timeout)
    {
        if (Workout.IsActive)
            return true;
        var arrived = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void OnChanged(WearWorkout w)
        {
            if (w.IsActive)
                arrived.TrySetResult(true);
        }
        WorkoutChanged += OnChanged;
        try
        {
            return await Task.WhenAny(arrived.Task, Task.Delay(timeout)) == arrived.Task;
        }
        finally
        {
            WorkoutChanged -= OnChanged;
        }
    }

    static Android.Content.Context Context => Android.App.Application.Context;

    bool _listening;

    /// <summary>The workout the phone last sent; <see cref="WearWorkout.None"/> until one arrives.</summary>
    public WearWorkout Workout { get; private set; } = WearWorkout.None;

    /// <summary>The phone sent a new workout (or that there's none). Raised on the main thread.</summary>
    public event Action<WearWorkout>? WorkoutChanged;

    /// <summary>Starts following the phone's workout and reads the one it last sent.</summary>
    public async Task StartAsync()
    {
        var client = WearableClass.GetDataClient(Context);
        if (!_listening)
        {
            await client.AddListener(this).AsAsync<Java.Lang.Object>();
            _listening = true;
        }
        var items = await client.GetDataItems().AsAsync<DataItemBuffer>();
        try
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items.Get(i)!.JavaCast<IDataItem>()!;
                if (item.Uri?.Path == WearPaths.Workout)
                    Publish(item.GetData());
            }
        }
        finally
        {
            items.Release();
        }
    }

    public void Stop()
    {
        if (!_listening)
            return;
        WearableClass.GetDataClient(Context).RemoveListener(this);
        _listening = false;
    }

    public void OnDataChanged(DataEventBuffer dataEvents)
    {
        for (var i = 0; i < dataEvents.Count; i++)
        {
            var e = dataEvents.Get(i)!.JavaCast<IDataEvent>()!;
            if (e.Type == DataEvent.TypeChanged && e.DataItem?.Uri?.Path == WearPaths.Workout)
                Publish(e.DataItem.GetData());
        }
    }

    void Publish(byte[]? data)
    {
        if (data == null)
            return;
        WearWorkout? workout;
        try
        {
            workout = JsonSerializer.Deserialize(data, WorkoutJson);
        }
        catch (JsonException)
        {
            // From a newer or older phone app this one can't read: keep what's on screen.
            return;
        }
        if (workout != null)
            MainThread.BeginInvokeOnMainThread(() =>
            {
                Workout = workout;
                WorkoutChanged?.Invoke(workout);
            });
    }

    DateTime _heartRateSent;

    /// <summary>
    /// Passes the heart rate on to the phone, which shows it while the workout's going: at most every 2 seconds, and
    /// quietly not at all when no phone can be reached.
    /// </summary>
    public async void SendHeartRate(int bpm)
    {
        var now = DateTime.UtcNow;
        if (now - _heartRateSent < TimeSpan.FromSeconds(2))
            return;
        _heartRateSent = now;
        try
        {
            await SendToPhonesAsync(WearPaths.HeartRate, System.Text.Encoding.ASCII.GetBytes(bpm.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        }
        catch (Exception)
        {
            // Out of range or the phone app isn't installed: the phone just shows no heart rate.
        }
    }

    /// <summary>Asks the phone to tick a set. False when no phone could be reached.</summary>
    public Task<bool> CompleteSetAsync(WearCompleteSet request) =>
        SendToPhonesAsync(WearPaths.CompleteSet, JsonSerializer.SerializeToUtf8Bytes(request, CompleteSetJson));

    /// <summary>
    /// Asks the phone app for a session of the watch's own ("Sign in with phone"). The answer says why not when the
    /// phone isn't reachable or isn't signed in.
    /// </summary>
    public async Task<WearSession> RequestSessionAsync()
    {
        var messages = WearableClass.GetMessageClient(Context);
        var reply = _sessionReply = new TaskCompletionSource<WearSession>(TaskCreationOptions.RunContinuationsAsynchronously);
        await messages.AddListener(this).AsAsync<Java.Lang.Object>();
        try
        {
            if (!await SendToPhonesAsync(WearPaths.RequestSession, []))
                return new(null, "Your phone isn't connected. Check Bluetooth, or sign in with email.");
            var done = await Task.WhenAny(reply.Task, Task.Delay(SessionTimeout));
            return done == reply.Task
                ? reply.Task.Result
                : new(null, "Your phone didn't answer. Is Gym Book installed on it?");
        }
        finally
        {
            _sessionReply = null;
            messages.RemoveListener(this);
        }
    }

    public void OnMessageReceived(IMessageEvent message)
    {
        if (message.Path != WearPaths.Session || message.GetData() is not { } data)
            return;
        try
        {
            if (JsonSerializer.Deserialize(data, SessionJson) is { } session)
                _sessionReply?.TrySetResult(session);
        }
        catch (JsonException)
        {
            _sessionReply?.TrySetResult(new(null, "Update Gym Book on your phone and try again."));
        }
    }

    /// <summary>Sends a message to every connected phone (in practice the one it's paired with). False when there's none.</summary>
    static async Task<bool> SendToPhonesAsync(string path, byte[] data)
    {
        var nodes = await WearableClass.GetNodeClient(Context).GetConnectedNodes().AsAsync<JavaList>();
        var messages = WearableClass.GetMessageClient(Context);
        var sent = false;
        foreach (var node in nodes.OfType<Java.Lang.Object>().Select(n => n.JavaCast<INode>()))
        {
            if (node?.Id == null)
                continue;
            await messages.SendMessage(node.Id, path, data).AsAsync<Java.Lang.Object>();
            sent = true;
        }
        return sent;
    }
}

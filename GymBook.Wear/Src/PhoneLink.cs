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
/// when the app opens and followed while it's open), and ticks sent back as messages. See the phone's WatchSync.cs.
/// </summary>
public class PhoneLink : Java.Lang.Object, DataClient.IOnDataChangedListener
{
    static readonly JsonTypeInfo<WearWorkout> WorkoutJson = (JsonTypeInfo<WearWorkout>)GymBookJson.Options.GetTypeInfo(typeof(WearWorkout));
    static readonly JsonTypeInfo<WearCompleteSet> CompleteSetJson = (JsonTypeInfo<WearCompleteSet>)GymBookJson.Options.GetTypeInfo(typeof(WearCompleteSet));

    static Android.Content.Context Context => Android.App.Application.Context;

    bool _listening;

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
            MainThread.BeginInvokeOnMainThread(() => WorkoutChanged?.Invoke(workout));
    }

    /// <summary>Asks the phone to tick a set. False when no phone could be reached.</summary>
    public async Task<bool> CompleteSetAsync(WearCompleteSet request)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(request, CompleteSetJson);
        var nodes = await WearableClass.GetNodeClient(Context).GetConnectedNodes().AsAsync<JavaList>();
        var messages = WearableClass.GetMessageClient(Context);
        var sent = false;
        foreach (var node in nodes.OfType<Java.Lang.Object>().Select(n => n.JavaCast<INode>()))
        {
            if (node?.Id == null)
                continue;
            await messages.SendMessage(node.Id, WearPaths.CompleteSet, data).AsAsync<Java.Lang.Object>();
            sent = true;
        }
        return sent;
    }
}

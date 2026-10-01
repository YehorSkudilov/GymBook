using System.Net.Security;
using Microsoft.AspNetCore.SignalR.Client;

namespace GymBook.Services.Sync;

/// <summary>
/// The live sync connection (the API's SyncHub, over a WebSocket where the network allows, else SignalR's fallbacks):
/// while the app is open, signed in and online, the server says the moment another of the user's devices pushed
/// changes, and this syncs straight away. The data still goes through <see cref="SyncService"/>; without a connection
/// (offline, or a network that blocks it) sync simply works as before: on start, resume, edits and network changes.
/// Shared by the phone and the Wear OS app.
/// </summary>
public class LiveSync
{
    // The API's SyncHub.Changed.
    const string Changed = "changed";
    static readonly TimeSpan RetryAfterClose = TimeSpan.FromSeconds(30);

    readonly SyncService _sync;
    readonly ApiClient _api;
    readonly AuthSession _session;
    readonly SemaphoreSlim _gate = new(1, 1);
    HubConnection? _hub;
    // Whether the app wants it connected: while it's on screen.
    bool _wanted;

    public LiveSync(SyncService sync, ApiClient api, AuthSession session)
    {
        (_sync, _api, _session) = (sync, api, session);
        // Signed in or out: connect for the new account, or not at all.
        session.Changed += (_, _) => _ = RestartAsync();
        Connectivity.Current.ConnectivityChanged += (_, e) =>
        {
            if (e.NetworkAccess == NetworkAccess.Internet && _wanted)
                _ = StartAsync();
        };
    }

    public bool IsConnected => _hub?.State == HubConnectionState.Connected;

    /// <summary>Connects if it can (signed in, verified, online). Never throws: without it, sync works as before.</summary>
    public async Task StartAsync()
    {
        _wanted = true;
        await _gate.WaitAsync();
        try
        {
            await _session.EnsureLoadedAsync();
            if (!_session.IsSignedIn || !_session.EmailVerified || Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                return;
            if (_hub is { State: not HubConnectionState.Disconnected })
                return;
            _hub ??= Build();
            await _hub.StartAsync();
            _sync.LiveConnectionId = _hub.ConnectionId;
            // Anything that changed while it wasn't connected.
            _sync.Schedule(TimeSpan.Zero);
        }
        catch (Exception e)
        {
            // Offline after all, the server is down, or the network won't carry it: retried on the next start or
            // network change.
            System.Diagnostics.Debug.WriteLine($"Live sync couldn't connect: {e.Message}");
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>Disconnects, e.g. when the app leaves the screen (saves the battery; resuming syncs anyway).</summary>
    public async Task StopAsync()
    {
        _wanted = false;
        await DisconnectAsync();
    }

    async Task RestartAsync()
    {
        await DisconnectAsync();
        // A connection belongs to one account: the next one is made for whoever is signed in now.
        _hub = null;
        if (_wanted)
            await StartAsync();
    }

    async Task DisconnectAsync()
    {
        _sync.LiveConnectionId = null;
        await _gate.WaitAsync();
        try
        {
            if (_hub != null)
                await _hub.StopAsync();
        }
        catch (Exception)
        {
        }
        finally
        {
            _gate.Release();
        }
    }

    HubConnection Build()
    {
        var hub = new HubConnectionBuilder()
            .WithUrl(new Uri(ApiConfig.BaseAddress, "hubs/sync"), options =>
            {
                // The access token from the API client, renewed when it's about to expire. None (signed out, expired
                // session) just fails the connection.
                options.AccessTokenProvider = async () =>
                {
                    try
                    {
                        return await _api.AccessTokenAsync();
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                };
#if DEBUG
                // As ApiConfig: Debug builds accept the local API's development certificate (for the fallbacks over HTTP).
                options.HttpMessageHandlerFactory = handler =>
                {
                    if (handler is HttpClientHandler client)
                        client.ServerCertificateCustomValidationCallback = (request, _, _, errors) =>
                            errors == SslPolicyErrors.None || request.RequestUri?.Host is "localhost" or "10.0.2.2";
                    return handler;
                };
#endif
            })
            .WithAutomaticReconnect()
            .Build();

        // Another device pushed changes: fetch them now.
        hub.On<long>(Changed, _ => _sync.Schedule(TimeSpan.Zero));
        hub.Reconnecting += _ =>
        {
            _sync.LiveConnectionId = null;
            return Task.CompletedTask;
        };
        hub.Reconnected += connectionId =>
        {
            _sync.LiveConnectionId = connectionId;
            // What changed while the connection was down.
            _sync.Schedule(TimeSpan.Zero);
            return Task.CompletedTask;
        };
        // Gave up reconnecting (e.g. a long time offline): try again a little later if the app still wants it.
        hub.Closed += async _ =>
        {
            _sync.LiveConnectionId = null;
            if (!_wanted)
                return;
            await Task.Delay(RetryAfterClose);
            if (_wanted)
                await StartAsync();
        };
        return hub;
    }
}

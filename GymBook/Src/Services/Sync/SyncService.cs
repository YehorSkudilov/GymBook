using GymBook.Contracts;
using GymBook.Models;

namespace GymBook.Services.Sync;

public enum SyncState { SignedOut, Syncing, UpToDate, Offline, Failed, SignInRequired }

/// <summary>A change still waiting to upload. <paramref name="Blocked"/>: the server would reject it (e.g. a name that's too long).</summary>
public record SyncPendingItem(string Kind, string Name, bool IsDeletion, bool Blocked);

/// <summary>
/// Pushes local changes and pulls the account's changes whenever the app starts or resumes, the network
/// comes back, or the user edits something (debounced). Everything works offline; this catches up later.
/// </summary>
public class SyncService
{
    /// <summary>Records of each kind per request; keeps requests well under the API's size limit.</summary>
    const int PushBatch = 100;

    readonly DataStore _store;
    readonly ApiClient _api;
    readonly AuthSession _session;
    readonly SemaphoreSlim _gate = new(1, 1);
    CancellationTokenSource? _debounce;
    bool _rerun;

    public SyncService(DataStore store, ApiClient api, AuthSession session)
    {
        (_store, _api, _session) = (store, api, session);
        store.Saved += (_, _) => Schedule(TimeSpan.FromSeconds(3));
        Connectivity.Current.ConnectivityChanged += (_, e) =>
        {
            if (e.NetworkAccess == NetworkAccess.Internet)
                Schedule(TimeSpan.Zero);
        };
    }

    public SyncState State { get; private set; } = SyncState.SignedOut;
    public string Status { get; private set; } = "";
    public DateTimeOffset? LastSyncedAt { get; private set; }

    public event EventHandler? StatusChanged;

    /// <summary>Syncs after <paramref name="delay"/>, collapsing repeated requests into one.</summary>
    public void Schedule(TimeSpan delay)
    {
        _debounce?.Cancel();
        var cts = _debounce = new CancellationTokenSource();
        _ = Task.Delay(delay, cts.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
                MainThread.BeginInvokeOnMainThread(() => _ = SyncNowAsync());
        }, TaskScheduler.Default);
    }

    /// <summary>Runs a full sync. Returns false if it couldn't complete (offline, signed out, rejected).</summary>
    public async Task<bool> SyncNowAsync()
    {
        await _session.EnsureLoadedAsync();
        if (!_session.IsSignedIn)
        {
            if (State != SyncState.SignedOut)
                SetStatus(SyncState.SignedOut, "");
            return false;
        }
        if (_store.Local.AccountId != _session.UserId)
        {
            // Secure storage can outlive the database (e.g. iOS keeps the Keychain across reinstalls).
            // Unowned local data joins the signed-in account; data owned by someone else means the stored
            // session is stale, so ask for a fresh sign-in, which sorts out the data safely.
            if (_store.Local.AccountId == null && _session.UserId != null)
            {
                _store.Local.AttachToAccount(_session.UserId);
            }
            else
            {
                _session.Clear();
                SetStatus(SyncState.SignInRequired, "Please sign in again.");
                return false;
            }
        }
        if (!await _gate.WaitAsync(0))
        {
            // One is already running; go again when it finishes so the latest edits aren't left behind.
            _rerun = true;
            return false;
        }

        try
        {
            SetStatus(SyncState.Syncing, "Syncing…");
            var skipped = 0;
            for (var round = 0; round < 50; round++)
            {
                var pending = _store.Local.GetPendingChanges(PushBatch);
                skipped = HoldBackInvalid(pending);
                var response = await _api.SyncAsync(new SyncRequest { Since = _store.Local.SyncCursor, Changes = pending });
                if (_store.Local.ApplySync(pending, response))
                    _store.RaiseChanged();

                var morePending = pending.Plans.Count == PushBatch || pending.Sessions.Count == PushBatch
                    || pending.CustomExercises.Count == PushBatch || pending.BodyWeights.Count == PushBatch;
                if (!response.HasMore && !morePending)
                    break;
            }
            LastSyncedAt = DateTimeOffset.Now;
            SetStatus(skipped > 0 ? SyncState.Failed : SyncState.UpToDate, skipped > 0 ? $"{skipped} item(s) are too large to sync" : "Up to date");
            return true;
        }
        catch (SessionExpiredException e)
        {
            SetStatus(SyncState.SignInRequired, e.Message);
        }
        catch (ApiException e)
        {
            SetStatus(SyncState.Failed, e.Message);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            SetStatus(SyncState.Offline, "Offline. Changes will sync when you're back online.");
        }
        finally
        {
            _gate.Release();
            if (_rerun)
            {
                _rerun = false;
                Schedule(TimeSpan.FromSeconds(1));
            }
        }
        return false;
    }

    /// <summary>Local changes not on the server yet, for the sync details list; <c>Blocked</c> ones won't sync until fixed.</summary>
    public List<SyncPendingItem> PendingItems()
    {
        var pending = _store.Local.GetPendingChanges(1000);
        var items = new List<SyncPendingItem>();
        if (pending.Profile != null)
            items.Add(Item("Profile", "Profile and settings", pending.Profile, deleted: false));
        items.AddRange(pending.Plans.Select(p => Item("Plan", p.Name, p, p.IsDeleted)));
        items.AddRange(pending.Sessions.Select(s => Item(s.EndedAt == null ? "Workout in progress" : "Workout",
            $"{s.Name} · {s.StartedAt:ddd d MMM, HH:mm}", s, s.IsDeleted)));
        items.AddRange(pending.CustomExercises.Select(e => Item("Custom exercise", e.Name, e, e.IsDeleted)));
        items.AddRange(pending.BodyWeights.Select(b => Item("Body weight", $"{b.Date:d MMM yyyy}", b, b.IsDeleted)));
        return items;

        static SyncPendingItem Item(string kind, string name, object record, bool deleted) =>
            new(kind, string.IsNullOrWhiteSpace(name) ? "(unnamed)" : name, deleted, !ModelValidator.IsValid(record));
    }

    /// <summary>
    /// Drops records the server would reject (e.g. a name over the length limit) from the request so they don't
    /// block everything else. They stay pending locally.
    /// </summary>
    static int HoldBackInvalid(SyncChanges changes)
    {
        var removed = changes.Plans.RemoveAll(p => !ModelValidator.IsValid(p))
            + changes.Sessions.RemoveAll(s => !ModelValidator.IsValid(s))
            + changes.CustomExercises.RemoveAll(e => !ModelValidator.IsValid(e))
            + changes.BodyWeights.RemoveAll(b => !ModelValidator.IsValid(b));
        if (changes.Profile != null && !ModelValidator.IsValid(changes.Profile))
        {
            changes.Profile = null;
            removed++;
        }
        return removed;
    }

    void SetStatus(SyncState state, string status)
    {
        State = state;
        Status = status;
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }
}

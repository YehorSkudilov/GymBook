using System.Globalization;
using System.Text.Json;
using GymBook.Models;

namespace GymBook.Services.Health;

public enum HealthWriteKind { Workout, Food, Body }

/// <summary>
/// Something of Gym Book's to send to the health app, under <see cref="ClientId"/> (Gym Book's own id for it there, so
/// it can be changed or taken back out). <see cref="Signature"/> changes when what's sent would.
/// </summary>
public abstract record HealthWrite(string ClientId, HealthWriteKind Kind)
{
    public abstract string Signature { get; }

    protected static string N(double v) => Math.Round(v, 1).ToString(CultureInfo.InvariantCulture);
}

/// <summary>A finished workout: a strength-training session, with the calories it burned on top of resting.</summary>
public sealed record WorkoutWrite(string ClientId, string Title, DateTime Start, DateTime End, double ActiveKcal) : HealthWrite(ClientId, HealthWriteKind.Workout)
{
    public override string Signature => $"{Title}|{Start:O}|{End:O}|{N(ActiveKcal)}";
}

/// <summary>A food logged in Gym Book.</summary>
public sealed record FoodWrite(string ClientId, DateTime Time, MealType Meal, string Name, double Kcal, double ProteinG, double CarbsG, double FatG)
    : HealthWrite(ClientId, HealthWriteKind.Food)
{
    public override string Signature => $"{Time:O}|{Meal}|{Name}|{N(Kcal)}|{N(ProteinG)}|{N(CarbsG)}|{N(FatG)}";
}

/// <summary>A weight entered in Gym Book, and its body fat if any.</summary>
public sealed record BodyWrite(string ClientId, DateTime Time, double WeightKg, double? BodyFatPercent) : HealthWrite(ClientId, HealthWriteKind.Body)
{
    public override string Signature => $"{Time:O}|{N(WeightKg)}|{(BodyFatPercent is { } f ? N(f) : "")}";
}

/// <summary>A health app Gym Book can send its data to (Samsung Health itself, or Health Connect).</summary>
public interface IHealthWriter
{
    /// <summary>"Samsung Health", "Health Connect".</summary>
    string WriterName { get; }

    /// <summary>It's on this phone and can take data.</summary>
    bool CanWrite { get; }

    /// <summary>
    /// Asks to write (the app's own permission screen); true if allowed. Throws <see cref="HealthWriteRefusedException"/>
    /// when the app won't take data from Gym Book at all (Samsung Health, until it approves Gym Book as a partner).
    /// </summary>
    Task<bool> RequestWriteAsync();

    /// <summary>
    /// Sends <paramref name="upserts"/> (new, or changed since: they replace what was sent under the same id) and takes
    /// <paramref name="deletes"/> back out. Throws <see cref="HealthWriteRefusedException"/> as above.
    /// </summary>
    Task WriteAsync(IReadOnlyList<HealthWrite> upserts, IReadOnlyList<(HealthWriteKind Kind, string ClientId)> deletes);
}

/// <summary>The health app won't take data from Gym Book (not approved to write yet).</summary>
public class HealthWriteRefusedException(string message) : Exception(message);

/// <summary>
/// Sends what's done in Gym Book to the health app it reads from: finished workouts (as strength-training sessions with
/// their duration and calories), food logged in Gym Book, and weights entered in it, over the last
/// <see cref="HealthSyncService.Days"/> days; each kept up to date (changed or deleted here, changed or deleted there).
/// <para>
/// Samsung Health takes data straight from Gym Book only once Samsung approves it as a partner app. Until then (and on
/// phones without it) everything goes to Health Connect instead, which Samsung Health brings in when its Health Connect
/// sync is on; Samsung Health is tried again now and then. What went where is remembered on this device, so nothing's
/// sent twice; reading skips Gym Book's own records (see <see cref="HealthSyncService"/>).
/// </para>
/// </summary>
public class HealthWriteBack(DataStore store, IEnumerable<IHealthPlatform> platforms)
{
    const string EnabledKey = "health.writeback", LedgerKey = "health.writeback.sent", RefusedKey = "health.writeback.samsungRefusedAt";

    /// <summary>A workout's average effort, resistance training with rests (Compendium of Physical Activities: 3.5 METs), less resting.</summary>
    const double ActiveMets = 2.5;

    static readonly TimeSpan RetrySamsung = TimeSpan.FromDays(3);

    readonly SemaphoreSlim _gate = new(1, 1);
    CancellationTokenSource? _soon;

    IEnumerable<IHealthWriter> Writers => platforms.OfType<IHealthWriter>().Where(w => w.CanWrite);
    IHealthWriter? Samsung => Writers.FirstOrDefault(w => w is IHealthPlatform { Source: HealthSource.SamsungHealth });
    IHealthWriter? HealthConnect => Writers.FirstOrDefault(w => w is IHealthPlatform { Source: HealthSource.HealthConnect });

    /// <summary>Sending is on (on this device).</summary>
    public bool IsEnabled => Preferences.Default.Get(EnabledKey, false);

    /// <summary>Some health app here can take Gym Book's data.</summary>
    public bool IsAvailable => Writers.Any();

    /// <summary>Samsung Health said no (not approved yet), lately: Health Connect is used meanwhile.</summary>
    bool SamsungRefused => Preferences.Default.Get(RefusedKey, 0L) is var at && at > 0
        && DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(at) < RetrySamsung;

    /// <summary>Where it's going, or why it isn't, to show.</summary>
    public string Status { get; private set; } = "";

    public DateTimeOffset? LastSentAt { get; private set; }

    /// <summary>
    /// Turns sending on: asks Samsung Health to take Gym Book's data (and, if it won't yet, Health Connect), then sends.
    /// False with <see cref="Status"/> saying why when nothing would take it.
    /// </summary>
    public async Task<bool> EnableAsync()
    {
        var allowed = false;
        if (Samsung is { } samsung && store.Profile.HealthSource == HealthSource.SamsungHealth)
        {
            try
            {
                allowed = await samsung.RequestWriteAsync();
                Preferences.Default.Remove(RefusedKey);
            }
            catch (HealthWriteRefusedException)
            {
                MarkSamsungRefused();
            }
        }
        if (!allowed && HealthConnect is { } connect)
        {
            try
            {
                allowed = await connect.RequestWriteAsync();
            }
            catch (HealthWriteRefusedException e)
            {
                Status = e.Message;
            }
        }
        if (!allowed)
        {
            Status = Status.Length > 0 ? Status : "Nothing was allowed to take Gym Book's data.";
            return false;
        }
        Preferences.Default.Set(EnabledKey, true);
        await SendAsync();
        return true;
    }

    /// <summary>Stops sending. What was sent stays in the health app.</summary>
    public void Disable()
    {
        Preferences.Default.Set(EnabledKey, false);
        Status = "";
    }

    /// <summary>Sends a little after something changes (a food logged, a workout finished), so a burst of changes goes at once.</summary>
    public void SendSoon()
    {
        if (!IsEnabled)
            return;
        _soon?.Cancel();
        var cts = _soon = new CancellationTokenSource();
        _ = Task.Delay(TimeSpan.FromSeconds(8), cts.Token).ContinueWith(t =>
        {
            if (!t.IsCanceled)
                MainThread.BeginInvokeOnMainThread(() => _ = SendAsync());
        }, TaskScheduler.Default);
    }

    /// <summary>Sends what's new or changed and takes back what was deleted. Call on the UI thread.</summary>
    public async Task SendAsync()
    {
        if (!IsEnabled || !await _gate.WaitAsync(0))
            return;
        try
        {
            var wanted = Wanted();
            // Samsung Health directly when it takes the data; else Health Connect.
            if (store.Profile.HealthSource == HealthSource.SamsungHealth && !SamsungRefused && Samsung is { } samsung)
            {
                try
                {
                    await SendTo(samsung, wanted);
                    Status = $"Sending to {samsung.WriterName}";
                    return;
                }
                catch (HealthWriteRefusedException)
                {
                    MarkSamsungRefused();
                }
            }
            if (HealthConnect is { } connect)
            {
                await SendTo(connect, wanted);
                Status = store.Profile.HealthSource == HealthSource.SamsungHealth
                    ? "Samsung Health doesn't take data from Gym Book until Samsung approves it: sending through Health Connect, which Samsung Health brings in (Samsung Health › Settings › Health Connect)."
                    : $"Sending to {connect.WriterName}";
                return;
            }
            Status = "Samsung Health doesn't take data from Gym Book yet, and Health Connect isn't available to send through.";
        }
        catch (Exception e)
        {
            Status = $"Couldn't send: {e.Message}";
        }
        finally
        {
            _gate.Release();
        }
    }

    void MarkSamsungRefused() => Preferences.Default.Set(RefusedKey, DateTimeOffset.UtcNow.ToUnixTimeSeconds());

    /// <summary>Sends to <paramref name="writer"/> what it doesn't have yet (or has an older version of), and takes back what's gone.</summary>
    async Task SendTo(IHealthWriter writer, List<HealthWrite> wanted)
    {
        var ledger = Ledger();
        var target = writer.WriterName;
        var upserts = wanted.Where(w => !ledger.TryGetValue(Key(target, w.ClientId), out var sent) || sent.Signature != w.Signature).ToList();
        var ids = wanted.Select(w => w.ClientId).ToHashSet();
        var deletes = ledger.Where(e => e.Key.StartsWith(target + "|", StringComparison.Ordinal) && !ids.Contains(e.Value.ClientId)
                && e.Value.Time >= DateTime.Today.AddDays(1 - HealthSyncService.Days))
            .Select(e => (e.Value.Kind, e.Value.ClientId)).ToList();
        if (upserts.Count > 0 || deletes.Count > 0)
            await writer.WriteAsync(upserts, deletes);
        foreach (var (_, id) in deletes)
            ledger.Remove(Key(target, id));
        foreach (var w in upserts)
            ledger[Key(target, w.ClientId)] = new Sent(w.ClientId, w.Kind, w.Signature, TimeOf(w));
        // Forget what's older than the window: it isn't looked at again.
        foreach (var old in ledger.Where(e => e.Value.Time < DateTime.Today.AddDays(-HealthSyncService.Days - 7)).Select(e => e.Key).ToList())
            ledger.Remove(old);
        Save(ledger);
        LastSentAt = DateTimeOffset.Now;
    }

    /// <summary>Everything of Gym Book's to have in the health app: the last days' workouts, food logged here and weights entered here.</summary>
    List<HealthWrite> Wanted()
    {
        var from = DateTime.Today.AddDays(1 - HealthSyncService.Days);
        var data = store.Data;
        var writes = new List<HealthWrite>();
        foreach (var s in store.History.Where(s => s.StartedAt >= from && s.EndedAt is { } end && end > s.StartedAt))
        {
            var hours = (s.EndedAt!.Value - s.StartedAt).TotalHours;
            var kg = WeightOn(s.StartedAt);
            writes.Add(new WorkoutWrite($"gymbook-workout-{s.Id}", $"Gym Book · {s.Name}", s.StartedAt, s.EndedAt.Value, ActiveMets * kg * hours));
        }
        foreach (var f in data.FoodEntries.Where(f => f.Source == null && f.Date >= from && (f.Calories > 0 || f.ProteinG + f.CarbsG + f.FatG > 0)))
        {
            // Logged for a day other than when it was logged: at noon that day, so it lands on the right day.
            var time = f.LoggedAt.Date == f.Date.Date ? f.LoggedAt : f.Date.Date.AddHours(12);
            writes.Add(new FoodWrite($"gymbook-food-{f.Id}", time, f.Meal, f.Name, f.Calories, f.ProteinG, f.CarbsG, f.FatG));
        }
        foreach (var b in data.BodyWeights.Where(b => b.Source == null && b.Date >= from && b.WeightKg > 0))
            writes.Add(new BodyWrite($"gymbook-body-{b.Id}", b.Date.Date.AddHours(8), b.WeightKg, b.BodyFatPercent));
        return writes;
    }

    double WeightOn(DateTime date) =>
        store.Data.BodyWeights.Where(b => b.Date.Date <= date.Date && b.WeightKg > 0).OrderBy(b => b.Date).LastOrDefault()?.WeightKg
        ?? (store.Profile.BodyWeightKg > 0 ? store.Profile.BodyWeightKg : 75);

    static DateTime TimeOf(HealthWrite w) => w switch
    {
        WorkoutWrite x => x.Start,
        FoodWrite x => x.Time,
        BodyWrite x => x.Time,
        _ => DateTime.Today,
    };

    // ---------- What was sent where (on this device) ----------

    internal sealed record Sent(string ClientId, HealthWriteKind Kind, string Signature, DateTime Time);

    static string Key(string target, string clientId) => $"{target}|{clientId}";

    static Dictionary<string, Sent> Ledger()
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, Sent>>(Preferences.Default.Get(LedgerKey, "{}")) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    static void Save(Dictionary<string, Sent> ledger) => Preferences.Default.Set(LedgerKey, JsonSerializer.Serialize(ledger));
}

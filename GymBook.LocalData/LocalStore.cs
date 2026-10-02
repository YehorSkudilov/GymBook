using GymBook.Contracts;
using GymBook.Models;
using Microsoft.EntityFrameworkCore;

namespace GymBook.LocalData;

/// <summary>
/// Keeps <see cref="AppData"/> in memory and persists it to the device database. The app mutates
/// <see cref="Data"/> freely and calls <see cref="Save"/>; each record's JSON is compared with what was last
/// written, and only changed records are written, stamped with UpdatedAt and marked dirty for sync. Removed
/// records become tombstones so the deletion syncs too. The workout in progress is stored and synced as a session
/// without EndedAt, so it follows the account to other devices and reinstalls.
/// </summary>
/// <remarks>Mutating calls (<see cref="Save"/>, <see cref="ApplySync"/>, ...) must run on the UI thread, like the rest of the app's access to <see cref="Data"/>.</remarks>
public sealed class LocalStore
{
    /// <summary>Where the workout in progress was kept before it synced; read once to carry it over.</summary>
    const string LegacyActiveSessionKey = "active_session";
    const string CursorKey = "sync.cursor";
    const string AccountKey = "sync.account";

    readonly DbContextOptions<LocalDbContext> _options;
    readonly Lock _gate = new();

    readonly Table<WorkoutPlan> _plans = new();
    readonly Table<WorkoutSession> _sessions = new();
    readonly Table<Exercise> _exercises = new();
    readonly Table<BodyWeightEntry> _weights = new();
    readonly Table<FoodEntry> _foods = new();
    readonly Table<HealthDay> _healthDays = new();
    readonly Dictionary<string, string> _settings = [];
    string? _profileJson;
    DateTimeOffset _profileStamp;
    bool _profileDirty;

    public AppData Data { get; private set; } = new();

    public LocalStore(string databasePath, string? legacyJsonPath = null)
    {
        _options = LocalDbContext.CreateOptions(databasePath);
        using (var db = NewDb())
        {
            db.Database.Migrate();
            Load(db);
        }
        if (legacyJsonPath != null && File.Exists(legacyJsonPath))
            ImportLegacy(legacyJsonPath);
    }

    /// <summary>Server cursor from the last sync.</summary>
    public long SyncCursor => long.TryParse(Setting(CursorKey), out var v) ? v : 0;

    /// <summary>The account this device's data belongs to, or null if it has never been synced.</summary>
    public string? AccountId => Setting(AccountKey);

    public bool HasPendingChanges
    {
        get
        {
            lock (_gate)
                return _profileDirty || _plans.Dirty.Count + _sessions.Dirty.Count + _exercises.Dirty.Count + _weights.Dirty.Count
                    + _foods.Dirty.Count + _healthDays.Dirty.Count > 0;
        }
    }

    public void Save()
    {
        lock (_gate)
        {
            var now = Now();
            using var db = NewDb();
            var commit = new List<Action>();

            var profileJson = LocalJson.Serialize(Data.Profile);
            if (profileJson != _profileJson)
            {
                Data.Profile.UpdatedAt = now;
                profileJson = LocalJson.Serialize(Data.Profile);
                StageProfile(db, LocalJson.Deserialize<UserProfile>(profileJson), profileJson, dirty: true, commit);
            }
            StageTable(db, _plans, Data.Plans, now, commit);
            StageTable(db, _sessions, WithActive(Data), now, commit);
            StageTable(db, _exercises, Data.CustomExercises, now, commit);
            StageTable(db, _weights, Data.BodyWeights, now, commit);
            StageTable(db, _foods, Data.FoodEntries, now, commit);
            StageTable(db, _healthDays, Data.HealthDays, now, commit);
            StageSetting(db, LegacyActiveSessionKey, null, commit);

            db.SaveChanges();
            commit.ForEach(a => a());
        }
    }

    /// <summary>Deletes everything; the deletions sync to the account if signed in.</summary>
    public void Reset()
    {
        Data = new AppData();
        Save();
    }

    /// <summary>Removes all data from this device only (sign-out). Nothing is sent to the server.</summary>
    public void WipeDevice()
    {
        lock (_gate)
        {
            using (var db = NewDb())
            using (var tx = db.Database.BeginTransaction())
            {
                db.Plans.ExecuteDelete();
                db.Sessions.ExecuteDelete();
                db.CustomExercises.ExecuteDelete();
                db.BodyWeights.ExecuteDelete();
                db.FoodEntries.ExecuteDelete();
                db.HealthDays.ExecuteDelete();
                db.Profiles.ExecuteDelete();
                db.Settings.ExecuteDelete();
                tx.Commit();
            }
            foreach (var t in new ITable[] { _plans, _sessions, _exercises, _weights, _foods, _healthDays })
                t.Clear();
            _settings.Clear();
            _profileJson = null;
            _profileDirty = false;
            Data = new AppData();
        }
    }

    /// <summary>Hands this device's data to an account: everything is marked for upload and syncing starts from scratch.</summary>
    public void AttachToAccount(string accountId)
    {
        lock (_gate)
        {
            using (var db = NewDb())
            using (var tx = db.Database.BeginTransaction())
            {
                db.Plans.ExecuteUpdate(s => s.SetProperty(e => EF.Property<bool>(e, LocalDbContext.Dirty), true));
                db.Sessions.ExecuteUpdate(s => s.SetProperty(e => EF.Property<bool>(e, LocalDbContext.Dirty), true));
                db.CustomExercises.ExecuteUpdate(s => s.SetProperty(e => EF.Property<bool>(e, LocalDbContext.Dirty), true));
                db.BodyWeights.ExecuteUpdate(s => s.SetProperty(e => EF.Property<bool>(e, LocalDbContext.Dirty), true));
                db.FoodEntries.ExecuteUpdate(s => s.SetProperty(e => EF.Property<bool>(e, LocalDbContext.Dirty), true));
                db.HealthDays.ExecuteUpdate(s => s.SetProperty(e => EF.Property<bool>(e, LocalDbContext.Dirty), true));
                db.Profiles.ExecuteUpdate(s => s.SetProperty(e => EF.Property<bool>(e, LocalDbContext.Dirty), true));
                var commit = new List<Action>();
                StageSetting(db, AccountKey, accountId, commit);
                StageSetting(db, CursorKey, null, commit);
                db.SaveChanges();
                tx.Commit();
                commit.ForEach(a => a());
            }
            foreach (var t in new ITable[] { _plans, _sessions, _exercises, _weights, _foods, _healthDays })
                t.MarkAllDirty();
            _profileDirty = _profileJson != null;
        }
    }

    /// <summary>Up to <paramref name="max"/> unsynced records of each kind, as fresh copies safe to use off the UI thread.</summary>
    public SyncChanges GetPendingChanges(int max)
    {
        lock (_gate)
        {
            using var db = NewDb();
            return new SyncChanges
            {
                Profile = _profileDirty ? db.Profiles.AsNoTracking().FirstOrDefault() : null,
                Plans = Pending(db.Plans),
                Sessions = Pending(db.Sessions),
                CustomExercises = Pending(db.CustomExercises),
                BodyWeights = Pending(db.BodyWeights),
                FoodEntries = Pending(db.FoodEntries),
                HealthDays = Pending(db.HealthDays),
            };
        }

        List<T> Pending<T>(DbSet<T> set) where T : class =>
            [.. set.AsNoTracking().Where(e => EF.Property<bool>(e, LocalDbContext.Dirty)).Take(max)];
    }

    /// <summary>
    /// Records the outcome of a sync: pushed records the user hasn't touched since are clean, and server changes
    /// are merged in (a local edit still waiting to go up wins only if it is newer). Returns whether
    /// <see cref="Data"/> changed.
    /// </summary>
    public bool ApplySync(SyncChanges pushed, SyncResponse response)
    {
        lock (_gate)
        {
            using var db = NewDb();
            using var tx = db.Database.BeginTransaction();
            var commit = new List<Action>();
            var changed = false;

            changed |= Merge(db, _plans, Data.Plans, pushed.Plans, response.Changes.Plans, commit);
            // Merged together with the workout in progress, then split again: the server may have finished it,
            // discarded it, or sent one started on another device.
            var sessions = WithActive(Data);
            changed |= Merge(db, _sessions, sessions, pushed.Sessions, response.Changes.Sessions, commit);
            commit.Add(() => SplitActive(Data, sessions));
            changed |= Merge(db, _exercises, Data.CustomExercises, pushed.CustomExercises, response.Changes.CustomExercises, commit);
            changed |= Merge(db, _weights, Data.BodyWeights, pushed.BodyWeights, response.Changes.BodyWeights, commit);
            changed |= Merge(db, _foods, Data.FoodEntries, pushed.FoodEntries, response.Changes.FoodEntries, commit);
            changed |= Merge(db, _healthDays, Data.HealthDays, pushed.HealthDays, response.Changes.HealthDays, commit);

            var profileClean = pushed.Profile != null && pushed.Profile.UpdatedAt == _profileStamp;
            if (profileClean)
            {
                db.Profiles.ExecuteUpdate(s => s.SetProperty(e => EF.Property<bool>(e, LocalDbContext.Dirty), false));
                commit.Add(() => _profileDirty = false);
            }
            if (response.Changes.Profile is { } remote && !(_profileDirty && !profileClean && _profileStamp > remote.UpdatedAt))
            {
                var json = LocalJson.Serialize(remote);
                if (json != _profileJson)
                {
                    StageProfile(db, remote, json, dirty: false, commit);
                    commit.Add(() => Data.Profile = LocalJson.Deserialize<UserProfile>(json));
                    changed = true;
                }
            }

            StageSetting(db, CursorKey, response.Cursor.ToString(), commit);
            db.SaveChanges();
            tx.Commit();
            commit.ForEach(a => a());
            return changed;
        }
    }

    public string? Setting(string key)
    {
        lock (_gate)
            return _settings.GetValueOrDefault(key);
    }

    void Load(LocalDbContext db)
    {
        var data = new AppData();
        LoadTable(db.Plans, _plans, data.Plans);
        LoadTable(db.Sessions, _sessions, data.Sessions);
        LoadTable(db.CustomExercises, _exercises, data.CustomExercises);
        LoadTable(db.BodyWeights, _weights, data.BodyWeights);
        LoadTable(db.FoodEntries, _foods, data.FoodEntries);
        LoadTable(db.HealthDays, _healthDays, data.HealthDays);

        if (db.Profiles.AsNoTracking().FirstOrDefault() is { } profile)
        {
            data.Profile = profile;
            _profileJson = LocalJson.Serialize(profile);
            _profileStamp = profile.UpdatedAt;
            _profileDirty = db.Profiles.Any(p => EF.Property<bool>(p, LocalDbContext.Dirty));
        }

        foreach (var s in db.Settings.AsNoTracking())
            if (s.Value != null)
                _settings[s.Key] = s.Value;
        SplitActive(data, [.. data.Sessions]);
        if (data.ActiveSession == null && _settings.GetValueOrDefault(LegacyActiveSessionKey) is { } active)
        {
            try
            {
                // Saved into the sessions table (and so synced) on the next save.
                data.ActiveSession = LocalJson.Deserialize<WorkoutSession>(active);
            }
            catch (System.Text.Json.JsonException)
            {
                // A half-logged workout isn't worth failing startup over.
            }
        }
        Data = data;

        static void LoadTable<T>(DbSet<T> set, Table<T> t, List<T> target) where T : class, ISyncEntity
        {
            var dirty = set.Where(e => EF.Property<bool>(e, LocalDbContext.Dirty)).Select(e => e.Id).ToHashSet();
            foreach (var e in set.AsNoTracking())
            {
                t.Stamps[e.Id] = e.UpdatedAt;
                if (dirty.Contains(e.Id))
                    t.Dirty.Add(e.Id);
                if (!e.IsDeleted)
                {
                    target.Add(e);
                    t.Live[e.Id] = LocalJson.Serialize(e);
                }
            }
        }
    }

    /// <summary>Finished sessions plus the workout in progress: every session record that's stored and synced.</summary>
    static List<WorkoutSession> WithActive(AppData data) =>
        data.ActiveSession is { } active && data.Sessions.All(s => s.Id != active.Id) ? [.. data.Sessions, active] : [.. data.Sessions];

    /// <summary>
    /// Sorts <paramref name="all"/> into the finished sessions and the workout in progress. The one already in progress
    /// on this device stays current; if another device started a different one meanwhile, its completed sets are kept
    /// as a finished workout (and an empty one is dropped), since only one workout can be in progress.
    /// </summary>
    static void SplitActive(AppData data, List<WorkoutSession> all)
    {
        var running = all.Where(s => s.EndedAt == null).OrderByDescending(s => s.StartedAt).ToList();
        data.Sessions.Clear();
        data.Sessions.AddRange(all.Where(s => s.EndedAt != null));
        var current = running.FirstOrDefault(s => s.Id == data.ActiveSession?.Id) ?? running.FirstOrDefault();
        data.ActiveSession = current;
        foreach (var other in running.Where(s => s != current))
        {
            var completed = other.Exercises.SelectMany(e => e.Sets).Where(s => s.IsCompleted).ToList();
            if (completed.Count == 0)
                continue;
            other.EndedAt = completed.Max(s => s.CompletedAt) ?? other.StartedAt;
            foreach (var e in other.Exercises)
                e.Sets.RemoveAll(s => !s.IsCompleted);
            other.Exercises.RemoveAll(e => e.Sets.Count == 0);
            data.Sessions.Add(other);
        }
    }

    /// <summary>One-time move from the single gymbook.json file used before the database existed.</summary>
    void ImportLegacy(string path)
    {
        var isEmpty = _profileJson == null && _plans.Stamps.Count == 0 && _sessions.Stamps.Count == 0;
        if (isEmpty)
        {
            Data = LocalJson.Deserialize<AppData>(File.ReadAllText(path));
            Save();
        }
        File.Move(path, path + ".imported", overwrite: true);
    }

    void StageTable<T>(LocalDbContext db, Table<T> t, List<T> items, DateTimeOffset now, List<Action> commit) where T : class, ISyncEntity
    {
        var live = new Dictionary<string, string>();
        foreach (var item in items)
        {
            if (live.ContainsKey(item.Id))
                continue;
            var json = LocalJson.Serialize(item);
            if (!t.Live.TryGetValue(item.Id, out var old) || old != json)
            {
                item.UpdatedAt = now;
                item.IsDeleted = false;
                json = LocalJson.Serialize(item);
                Stage(db, t, LocalJson.Deserialize<T>(json), dirty: true, commit);
            }
            live[item.Id] = json;
        }
        foreach (var (id, old) in t.Live)
        {
            if (live.ContainsKey(id))
                continue;
            var tombstone = LocalJson.Deserialize<T>(old);
            tombstone.IsDeleted = true;
            tombstone.UpdatedAt = now;
            Stage(db, t, tombstone, dirty: true, commit);
        }
        commit.Add(() => t.Live = live);
    }

    bool Merge<T>(LocalDbContext db, Table<T> t, List<T> list, List<T> pushed, List<T> remote, List<Action> commit) where T : class, ISyncEntity
    {
        // Pushed records are now on the server, unless they were edited again while the sync was in flight.
        var clean = pushed.Where(p => t.Stamps.TryGetValue(p.Id, out var stamp) && stamp == p.UpdatedAt).Select(p => p.Id).ToList();
        if (clean.Count > 0)
        {
            db.Set<T>().Where(e => clean.Contains(e.Id)).ExecuteUpdate(s => s.SetProperty(e => EF.Property<bool>(e, LocalDbContext.Dirty), false));
            commit.Add(() => t.Dirty.ExceptWith(clean));
        }

        var changed = false;
        foreach (var r in remote)
        {
            var pendingLocal = t.Dirty.Contains(r.Id) && !clean.Contains(r.Id);
            if (pendingLocal && t.Stamps[r.Id] > r.UpdatedAt)
                continue;

            var json = LocalJson.Serialize(r);
            Stage(db, t, r, dirty: false, commit);
            var id = r.Id;
            if (r.IsDeleted)
            {
                if (!t.Live.ContainsKey(id))
                    continue;
                commit.Add(() =>
                {
                    list.RemoveAll(x => x.Id == id);
                    t.Live.Remove(id);
                });
            }
            else
            {
                if (t.Live.TryGetValue(id, out var current) && current == json)
                    continue;
                commit.Add(() =>
                {
                    var copy = LocalJson.Deserialize<T>(json);
                    var index = list.FindIndex(x => x.Id == id);
                    if (index >= 0)
                        list[index] = copy;
                    else
                        list.Add(copy);
                    t.Live[id] = json;
                });
            }
            changed = true;
        }
        return changed;
    }

    static void Stage<T>(LocalDbContext db, Table<T> t, T entity, bool dirty, List<Action> commit) where T : class, ISyncEntity
    {
        var entry = db.Entry(entity);
        entry.Property(LocalDbContext.Dirty).CurrentValue = dirty;
        entry.State = t.Stamps.ContainsKey(entity.Id) ? EntityState.Modified : EntityState.Added;
        var (id, stamp) = (entity.Id, entity.UpdatedAt);
        commit.Add(() =>
        {
            t.Stamps[id] = stamp;
            if (dirty)
                t.Dirty.Add(id);
            else
                t.Dirty.Remove(id);
        });
    }

    void StageProfile(LocalDbContext db, UserProfile profile, string json, bool dirty, List<Action> commit)
    {
        var entry = db.Entry(profile);
        entry.Property(LocalDbContext.ProfileKey).CurrentValue = 1;
        entry.Property(LocalDbContext.Dirty).CurrentValue = dirty;
        entry.State = _profileJson == null ? EntityState.Added : EntityState.Modified;
        commit.Add(() =>
        {
            _profileJson = json;
            _profileStamp = profile.UpdatedAt;
            _profileDirty = dirty;
        });
    }

    void StageSetting(LocalDbContext db, string key, string? value, List<Action> commit)
    {
        var exists = _settings.TryGetValue(key, out var current);
        if (current == value)
            return;
        var entry = db.Entry(new LocalSetting { Key = key, Value = value });
        entry.State = value == null ? EntityState.Deleted : exists ? EntityState.Modified : EntityState.Added;
        commit.Add(() =>
        {
            if (value == null)
                _settings.Remove(key);
            else
                _settings[key] = value;
        });
    }

    LocalDbContext NewDb() => new(_options);

    /// <summary>Microsecond precision, matching what the server stores, so timestamps compare exactly after a round trip.</summary>
    static DateTimeOffset Now()
    {
        var now = DateTimeOffset.UtcNow;
        return new DateTimeOffset(now.UtcTicks - now.UtcTicks % 10, TimeSpan.Zero);
    }

    interface ITable
    {
        void Clear();
        void MarkAllDirty();
    }

    sealed class Table<T> : ITable where T : ISyncEntity
    {
        /// <summary>JSON of each non-deleted record as last written; the baseline <see cref="Save"/> diffs against.</summary>
        public Dictionary<string, string> Live = [];

        /// <summary>UpdatedAt of every stored record, tombstones included.</summary>
        public readonly Dictionary<string, DateTimeOffset> Stamps = [];

        /// <summary>Records not yet on the server.</summary>
        public readonly HashSet<string> Dirty = [];

        public void Clear()
        {
            Live = [];
            Stamps.Clear();
            Dirty.Clear();
        }

        public void MarkAllDirty() => Dirty.UnionWith(Stamps.Keys);
    }
}

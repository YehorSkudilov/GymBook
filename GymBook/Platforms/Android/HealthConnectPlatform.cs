using System.Runtime.Versioning;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Health.Connect;
using Android.Health.Connect.DataTypes;
using Android.Health.Connect.DataTypes.Units;
using Android.OS;
using Android.Runtime;
using GymBook.Models;
using GymBook.Services.Health;
using Java.Time;

namespace GymBook;

/// <summary>
/// Health data through Android's Health Connect, built into Android 14 and later (its framework API, so no extra library).
/// A general connector: reads every app's records (Google Fit, Fitbit, Garmin, Withings, Samsung Health, scales, the phone's own
/// step counter, ...), or only those of the apps picked, as they recorded them; calories burned are worked out from what
/// they share (see NutritionService.Burned). Samsung Health's own totals come from reading it directly instead
/// (SamsungHealthPlatform). As an <see cref="IHealthWriter"/> it also takes Gym Book's workouts, food and weights (when
/// sending is on), each under Gym Book's own id so it can be updated.
/// </summary>
public class HealthConnectPlatform : IHealthPlatform, IHealthWriter
{

    /// <summary>
    /// Everything the Nutrition tab reads (HealthPermissions' values, spelled out so they can be listed on older Android
    /// too). Each is also declared in AndroidManifest.xml.
    /// </summary>
    public static readonly string[] ReadPermissions =
    [
        "android.permission.health.READ_TOTAL_CALORIES_BURNED",
        "android.permission.health.READ_ACTIVE_CALORIES_BURNED",
        "android.permission.health.READ_BASAL_METABOLIC_RATE",
        "android.permission.health.READ_STEPS",
        "android.permission.health.READ_NUTRITION",
        "android.permission.health.READ_WEIGHT",
        "android.permission.health.READ_BODY_FAT",
        "android.permission.health.READ_LEAN_BODY_MASS",
        "android.permission.health.READ_BONE_MASS",
        "android.permission.health.READ_BODY_WATER_MASS",
        "android.permission.health.READ_HEIGHT",
        // Sleep, for recovery.
        ReadSleepPermission,
    ];

    const string ReadSleepPermission = "android.permission.health.READ_SLEEP";

    /// <summary>
    /// Reading what was recorded more than 30 days before access was first given (without it, Health Connect shares no
    /// further back than that). Asked for along with the rest, where Health Connect knows it; on its own it reads nothing.
    /// </summary>
    const string ReadHistoryPermission = "android.permission.health.READ_HEALTH_DATA_HISTORY";

    static Context Context => Android.App.Application.Context;

    public HealthSource Source => HealthSource.HealthConnect;

    public HealthAvailability Availability =>
        OperatingSystem.IsAndroidVersionAtLeast(34) ? HealthAvailability.Available : HealthAvailability.NeedsNewerAndroid;

    public bool HasAnyPermission => ReadPermissions.Any(Granted);

    static bool Granted(string permission) => Context.CheckSelfPermission(permission) == Permission.Granted;

    public async Task<bool> RequestPermissionsAsync()
    {
        if (Availability != HealthAvailability.Available)
            return false;
        // On Android 14+ health permissions are runtime permissions; asking opens Health Connect's own screen, where
        // the user can allow some and not others. Whatever was allowed is read, the rest is left out.
        await MainThread.InvokeOnMainThreadAsync(() => Permissions.RequestAsync<HealthReadPermissions>());
        return HasAnyPermission;
    }

    public string? PermissionProblem => null;

    public Task<HealthReadResult> ReadAsync(DateTime from, DateTime to, IReadOnlyCollection<string>? apps, CancellationToken ct = default)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(34))
            return Task.FromResult(new HealthReadResult([], [], null));
        return new Reader(apps).ReadAsync(from, to, ct);
    }

    public async Task<IReadOnlyList<HealthApp>> FindAppsAsync(CancellationToken ct = default)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(34))
            return [];
        var packages = await new Reader(null).FindAppsAsync(DateTime.Today.AddDays(-30), DateTime.Today.AddDays(1), ct);
        return [.. packages.Where(p => p != Context.PackageName).Select(p => new HealthApp(p, AppName(p))).OrderBy(a => a.Name, StringComparer.CurrentCultureIgnoreCase)];
    }

    /// <summary>
    /// The app's name as Android shows it. Apps that share with Health Connect are visible to us (see the queries in
    /// AndroidManifest.xml); anything else shows its package.
    /// </summary>
    public string AppName(string package) => Label(package);

    /// <inheritdoc cref="AppName"/>
    public static string Label(string package)
    {
        // Health Connect's own data, such as the steps the phone counts itself, comes from a package of its own
        // (com.android.healthconnect.phone.<id>) with no app name.
        if (package.StartsWith("com.android.healthconnect", StringComparison.Ordinal)
            || package.StartsWith("com.google.android.healthconnect", StringComparison.Ordinal))
            return "This phone";
        try
        {
            var pm = Context.PackageManager!;
            var label = pm.GetApplicationLabel(pm.GetApplicationInfo(package, 0));
            if (!string.IsNullOrWhiteSpace(label))
                return label;
        }
        catch (PackageManager.NameNotFoundException)
        {
        }
        return package;
    }

    public void OpenSettings()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(34))
            return;
        var intent = new Intent(HealthConnectManager.ActionManageHealthPermissions)
            .PutExtra(Intent.ExtraPackageName, Context.PackageName)
            .AddFlags(ActivityFlags.NewTask);
        try
        {
            Context.StartActivity(intent);
        }
        catch (ActivityNotFoundException)
        {
            // Health Connect's home screen (HealthConnectManager.ACTION_HEALTH_HOME_SETTINGS, which isn't bound).
            Context.StartActivity(new Intent("android.health.connect.action.HEALTH_HOME_SETTINGS").AddFlags(ActivityFlags.NewTask));
        }
    }

    // ---------- Writing ----------

    /// <summary>What Gym Book writes (also declared in AndroidManifest.xml).</summary>
    public static readonly string[] WritePermissions =
    [
        "android.permission.health.WRITE_EXERCISE",
        "android.permission.health.WRITE_ACTIVE_CALORIES_BURNED",
        "android.permission.health.WRITE_NUTRITION",
        "android.permission.health.WRITE_WEIGHT",
        "android.permission.health.WRITE_BODY_FAT",
    ];

    public string WriterName => "Health Connect";

    public bool CanWrite => Availability == HealthAvailability.Available;

    public async Task<bool> RequestWriteAsync()
    {
        if (!CanWrite)
            return false;
        await MainThread.InvokeOnMainThreadAsync(() => Permissions.RequestAsync<HealthWritePermissions>());
        return WritePermissions.All(Granted);
    }

    public Task WriteAsync(IReadOnlyList<HealthWrite> upserts, IReadOnlyList<(HealthWriteKind Kind, string ClientId)> deletes)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(34))
            return Task.CompletedTask;
        if (!WritePermissions.All(Granted))
            throw new HealthWriteRefusedException("Health Connect hasn't allowed Gym Book to write: allow it in Health Connect's settings.");
        return new Writer().WriteAsync(upserts, deletes);
    }

    sealed class HealthWritePermissions : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            [.. WritePermissions.Select(p => (p, true))];
    }

    [SupportedOSPlatform("android34.0")]
    sealed class Writer
    {
        readonly HealthConnectManager _manager = Context.GetSystemService(Context.HealthconnectService).JavaCast<HealthConnectManager>()
            ?? throw new InvalidOperationException("Health Connect isn't available on this phone.");

        /// <summary>
        /// Writes each (a record with the same client id replaces the one there, its version being newer) and deletes the
        /// rest by their client ids.
        /// </summary>
        public async Task WriteAsync(IReadOnlyList<HealthWrite> upserts, IReadOnlyList<(HealthWriteKind Kind, string ClientId)> deletes)
        {
            var version = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            if (deletes.Count > 0)
            {
                var filters = deletes.SelectMany(d => Types(d.Kind).Select(t => RecordIdFilter.FromClientRecordId(Java.Lang.Class.FromType(t.Type), d.ClientId + t.Suffix)!))
                    .ToList();
                await Call(receiver => _manager.DeleteRecords(filters, Context.MainExecutor!, receiver));
            }
            var records = upserts.SelectMany(u => Records(u, version)).ToList();
            // A few hundred at a time at most.
            foreach (var chunk in records.Chunk(200))
                await Call(receiver => _manager.InsertRecords([.. chunk], Context.MainExecutor!, receiver));
        }

        /// <summary>The record types (and client id suffixes) each kind is written as.</summary>
        static (Type Type, string Suffix)[] Types(HealthWriteKind kind) => kind switch
        {
            HealthWriteKind.Workout => [(typeof(ExerciseSessionRecord), ""), (typeof(ActiveCaloriesBurnedRecord), "-kcal")],
            HealthWriteKind.Food => [(typeof(NutritionRecord), "")],
            _ => [(typeof(WeightRecord), ""), (typeof(BodyFatRecord), "-fat")],
        };

        static IEnumerable<Record> Records(HealthWrite w, long version)
        {
            Metadata Meta(string id) => new Metadata.Builder().SetClientRecordId(id)!.SetClientRecordVersion(version)!.Build()!;
            switch (w)
            {
                case WorkoutWrite x:
                    yield return new ExerciseSessionRecord.Builder(Meta(x.ClientId), Instant(x.Start), Instant(x.End),
                        ExerciseSessionTypeEnum.StrengthTraining).SetTitle(x.Title)!.Build()!;
                    if (x.ActiveKcal > 0)
                        yield return new ActiveCaloriesBurnedRecord.Builder(Meta(x.ClientId + "-kcal"), Instant(x.Start), Instant(x.End),
                            Energy.FromCalories(x.ActiveKcal * 1000)!).Build()!;
                    break;
                case FoodWrite x:
                    yield return new NutritionRecord.Builder(Meta(x.ClientId), Instant(x.Time), Instant(x.Time.AddMinutes(1)))
                        .SetMealName(x.Name)!
                        .SetMealType(x.Meal switch
                        {
                            Models.MealType.Breakfast => HealthMealType.Breakfast,
                            Models.MealType.Lunch => HealthMealType.Lunch,
                            Models.MealType.Dinner => HealthMealType.Dinner,
                            _ => HealthMealType.Snack,
                        })!
                        .SetEnergy(Energy.FromCalories(x.Kcal * 1000))!
                        .SetProtein(Mass.FromGrams(x.ProteinG))!
                        .SetTotalCarbohydrate(Mass.FromGrams(x.CarbsG))!
                        .SetTotalFat(Mass.FromGrams(x.FatG))!
                        .Build()!;
                    break;
                case BodyWrite x:
                    yield return new WeightRecord.Builder(Meta(x.ClientId), Instant(x.Time), Mass.FromGrams(x.WeightKg * 1000)!).Build()!;
                    if (x.BodyFatPercent is { } fat)
                        yield return new BodyFatRecord.Builder(Meta(x.ClientId + "-fat"), Instant(x.Time), Percentage.FromValue(fat)!).Build()!;
                    break;
            }
        }

        static Java.Time.Instant Instant(DateTime local) =>
            Java.Time.Instant.OfEpochMilli(new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Local)).ToUnixTimeMilliseconds())!;

        static Task<Java.Lang.Object> Call(Action<IOutcomeReceiver> start)
        {
            var receiver = new Receiver();
            start(receiver);
            return receiver.Task;
        }
    }

    sealed class HealthReadPermissions : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            [.. ReadPermissions.Append(ReadHistoryPermission).Select(p => (p, true))];
    }

    [SupportedOSPlatform("android34.0")]
    sealed class Reader(IReadOnlyCollection<string>? packages)
    {
        readonly HealthConnectManager _manager = Context.GetSystemService(Context.HealthconnectService).JavaCast<HealthConnectManager>()
            ?? throw new InvalidOperationException("Health Connect isn't available on this phone.");

        /// <summary>The apps to read from; none: every app.</summary>
        IEnumerable<DataOrigin> Origins => (packages ?? []).Select(p => new DataOrigin.Builder().SetPackageName(p).Build()!);

        /// <summary>
        /// Every app that recorded something Gym Book reads between the two times: from the day totals (Health Connect
        /// says whose data went into them) and from each measurement.
        /// </summary>
        public async Task<HashSet<string>> FindAppsAsync(DateTime from, DateTime to, CancellationToken ct)
        {
            var found = new HashSet<string>();
            var types = new List<AggregationType>();
            if (Granted(HealthPermissions.ReadTotalCaloriesBurned)) types.Add(TotalCaloriesBurnedRecord.EnergyTotal!);
            if (Granted(HealthPermissions.ReadActiveCaloriesBurned)) types.Add(ActiveCaloriesBurnedRecord.ActiveCaloriesTotal!);
            if (Granted(HealthPermissions.ReadSteps)) types.Add(StepsRecord.StepsCountTotal!);
            if (Granted(HealthPermissions.ReadNutrition)) types.Add(NutritionRecord.EnergyTotal!);
            foreach (var type in types)
            {
                // One at a time: a type nobody recorded mustn't hide the others.
                try
                {
                    var request = new AggregateRecordsRequest.Builder(new TimeInstantRangeFilter.Builder()
                        .SetStartTime(Instant(from)).SetEndTime(Instant(to)).Build()).AddAggregationType(type)!.Build()!;
                    var result = await Call(receiver => _manager.Aggregate(request, Context.MainExecutor!, receiver));
                    foreach (var origin in result.JavaCast<AggregateRecordsResponse>()!.GetDataOrigins(type))
                        if (origin.PackageName is { } name)
                            found.Add(name);
                }
                catch (InvalidOperationException)
                {
                }
                ct.ThrowIfCancellationRequested();
            }
            async Task Measurements<T>(string permission) where T : InstantRecord
            {
                if (!Granted(permission))
                    return;
                foreach (var name in await OriginsOf<T>(from, to))
                    found.Add(name);
            }
            await Measurements<WeightRecord>(HealthPermissions.ReadWeight);
            await Measurements<BodyFatRecord>(HealthPermissions.ReadBodyFat);
            await Measurements<BasalMetabolicRateRecord>(HealthPermissions.ReadBasalMetabolicRate);
            await Measurements<LeanBodyMassRecord>(HealthPermissions.ReadLeanBodyMass);
            return found;
        }

        /// <summary>The apps that wrote any record of one kind between the two times.</summary>
        async Task<HashSet<string>> OriginsOf<T>(DateTime from, DateTime to) where T : InstantRecord
        {
            var names = new HashSet<string>();
            var request = new ReadRecordsRequestUsingFilters.Builder(Java.Lang.Class.FromType(typeof(T)))
                .SetTimeRangeFilter(new TimeInstantRangeFilter.Builder().SetStartTime(Instant(from)).SetEndTime(Instant(to)).Build())!
                .SetPageSize(1000)!
                .Build()!;
            var result = await Call(receiver => _manager.ReadRecords(request, Context.MainExecutor!, receiver));
            foreach (var record in result.JavaCast<ReadRecordsResponse>()!.Records)
                if (record is Java.Lang.Object o && o.JavaCast<T>()?.Metadata?.DataOrigin?.PackageName is { } name)
                    names.Add(name);
            return names;
        }

        public async Task<HealthReadResult> ReadAsync(DateTime from, DateTime to, CancellationToken ct)
        {
            var days = await ReadDaysAsync(from, to);
            ct.ThrowIfCancellationRequested();
            var body = await ReadBodyAsync(from, to);
            ct.ThrowIfCancellationRequested();
            // Height is rarely measured, so look further back for it.
            var heights = Granted(HealthPermissions.ReadHeight)
                ? await ReadAsync<HeightRecord>(to.AddYears(-10), to, r => r.Height.InMeters * 100)
                : [];
            ct.ThrowIfCancellationRequested();
            var foods = Granted(HealthPermissions.ReadNutrition) ? await ReadFoodsAsync(from, to) : null;
            ct.ThrowIfCancellationRequested();
            // Sleep, by the day it ended: a failure there doesn't lose the rest.
            if (Granted(ReadSleepPermission))
            {
                try
                {
                    days = HealthSleep.Merge(days, await ReadSleepAsync(from, to));
                }
                catch (InvalidOperationException e)
                {
                    System.Diagnostics.Debug.WriteLine($"Health Connect sleep couldn't be read: {e.Message}");
                }
            }
            return new HealthReadResult(days, body, heights.Count > 0 ? heights.MaxBy(h => h.Time).Value : null, foods);
        }

        /// <summary>
        /// Minutes asleep each day between the two local times, by the day each sleep ended on, from every sleep
        /// session recorded (by the apps picked): overlapping ones from different apps count once.
        /// </summary>
        async Task<Dictionary<DateTime, int>> ReadSleepAsync(DateTime from, DateTime to)
        {
            var sleeps = new List<(DateTime Start, DateTime End)>();
            long? page = null;
            for (var i = 0; i < 20; i++)
            {
                // From the day before: a night that began then ends on the first day.
                var builder = new ReadRecordsRequestUsingFilters.Builder(Java.Lang.Class.FromType(typeof(SleepSessionRecord)))
                    .SetTimeRangeFilter(new TimeInstantRangeFilter.Builder()
                        .SetStartTime(Instant(from.AddDays(-1)))
                        .SetEndTime(Instant(to))
                        .Build())!
                    .SetPageSize(1000)!;
                foreach (var origin in Origins)
                    builder.AddDataOrigins(origin);
                if (page is { } token)
                    builder.SetPageToken(token);

                var result = await Call(receiver => _manager.ReadRecords(builder.Build()!, Context.MainExecutor!, receiver));
                var response = result.JavaCast<ReadRecordsResponse>()!;
                foreach (var record in response.Records)
                    if (record is Java.Lang.Object o && o.JavaCast<SleepSessionRecord>() is { } r)
                        sleeps.Add((LocalTime(r.StartTime), LocalTime(r.EndTime)));
                if (response.NextPageToken == -1)
                    break;
                page = response.NextPageToken;
            }
            return HealthSleep.ByWakeDay(sleeps, from, to);

            static DateTime LocalTime(Java.Time.Instant t) => DateTimeOffset.FromUnixTimeMilliseconds(t.ToEpochMilli()).LocalDateTime;
        }

        /// <summary>Each food logged between the two local times, with the meal it was logged under.</summary>
        async Task<List<FoodReading>> ReadFoodsAsync(DateTime from, DateTime to)
        {
            var foods = new List<FoodReading>();
            long? page = null;
            for (var i = 0; i < 20; i++)
            {
                var builder = new ReadRecordsRequestUsingFilters.Builder(Java.Lang.Class.FromType(typeof(NutritionRecord)))
                    .SetTimeRangeFilter(new TimeInstantRangeFilter.Builder()
                        .SetStartTime(Instant(from))
                        .SetEndTime(Instant(to))
                        .Build())!
                    .SetPageSize(1000)!;
                foreach (var origin in Origins)
                    builder.AddDataOrigins(origin);
                if (page is { } token)
                    builder.SetPageToken(token);

                var result = await Call(receiver => _manager.ReadRecords(builder.Build()!, Context.MainExecutor!, receiver));
                var response = result.JavaCast<ReadRecordsResponse>()!;
                foreach (var record in response.Records)
                {
                    if (record is not Java.Lang.Object o || o.JavaCast<NutritionRecord>() is not { } r || r.Metadata?.Id is not { Length: > 0 } id)
                        continue;
                    var time = DateTimeOffset.FromUnixTimeMilliseconds(r.StartTime.ToEpochMilli()).LocalDateTime;
                    // Health Connect's meal types: 1 breakfast, 2 lunch, 3 dinner, 4 snack, 0 not said.
                    Models.MealType? meal = (int)r.MealType switch
                    {
                        1 => Models.MealType.Breakfast,
                        2 => Models.MealType.Lunch,
                        3 => Models.MealType.Dinner,
                        4 => Models.EnumDisplay.SnackAt(time),
                        _ => null,
                    };
                    foods.Add(new FoodReading(id, time, meal, r.MealName,
                        r.Energy is { } energy ? energy.InCalories / 1000 : null,
                        r.Protein?.InGrams, r.TotalCarbohydrate?.InGrams, r.TotalFat?.InGrams,
                        r.Metadata.DataOrigin?.PackageName));
                }
                if (response.NextPageToken == -1)
                    break;
                page = response.NextPageToken;
            }
            return foods;
        }

        /// <summary>Each day's totals, added up by Health Connect itself (which also avoids counting overlapping apps twice).</summary>
        async Task<List<HealthDayReading>> ReadDaysAsync(DateTime from, DateTime to)
        {
            // Asking for a total without its permission fails the whole request, so only ask for what was allowed.
            var types = new List<AggregationType>();
            void Add(string permission, params AggregationType[] aggregations)
            {
                if (Granted(permission))
                    types.AddRange(aggregations);
            }
            Add(HealthPermissions.ReadTotalCaloriesBurned, TotalCaloriesBurnedRecord.EnergyTotal);
            Add(HealthPermissions.ReadActiveCaloriesBurned, ActiveCaloriesBurnedRecord.ActiveCaloriesTotal);
            Add(HealthPermissions.ReadBasalMetabolicRate, BasalMetabolicRateRecord.BasalCaloriesTotal);
            Add(HealthPermissions.ReadSteps, StepsRecord.StepsCountTotal);
            Add(HealthPermissions.ReadNutrition, NutritionRecord.EnergyTotal, NutritionRecord.ProteinTotal,
                NutritionRecord.TotalCarbohydrateTotal, NutritionRecord.TotalFatTotal);
            if (types.Count == 0)
                return [];

            // Up to now, not the end of today: where an app keeps no total of its own, Health Connect works the total
            // out as active plus resting calories over the time asked for, and resting calories for hours still to
            // come aren't burned yet (they made today's total far too high).
            var now = DateTime.Now;
            var request = new AggregateRecordsRequest.Builder(new LocalTimeRangeFilter.Builder()
                .SetStartTime(Local(from))
                .SetEndTime(Local(to > now ? now : to))
                .Build());
            foreach (var type in types)
                request.AddAggregationType(type);
            foreach (var origin in Origins)
                request.AddDataOriginsFilter(origin);

            var result = await Call(receiver => _manager.AggregateGroupByPeriod(request.Build(), Period.OfDays(1)!, Context.MainExecutor!, receiver));
            var groups = result.JavaCast<Java.Util.IList>()!;
            var days = new List<HealthDayReading>();
            for (var i = 0; i < groups.Size(); i++)
            {
                var group = groups.Get(i)!.JavaCast<AggregateRecordsGroupedByPeriodResponse>()!;
                var start = group.StartTime!;
                days.Add(new HealthDayReading(
                    new DateTime(start.Year, start.MonthValue, start.DayOfMonth),
                    Kcal(group, TotalCaloriesBurnedRecord.EnergyTotal),
                    Kcal(group, ActiveCaloriesBurnedRecord.ActiveCaloriesTotal),
                    Kcal(group, BasalMetabolicRateRecord.BasalCaloriesTotal),
                    types.Contains(StepsRecord.StepsCountTotal!) && group.Get(StepsRecord.StepsCountTotal!) is { } steps
                        ? (int)Math.Min(int.MaxValue, steps.JavaCast<Java.Lang.Number>()!.LongValue())
                        : null,
                    Kcal(group, NutritionRecord.EnergyTotal),
                    Grams(group, NutritionRecord.ProteinTotal),
                    Grams(group, NutritionRecord.TotalCarbohydrateTotal),
                    Grams(group, NutritionRecord.TotalFatTotal)));
            }
            return days;

            // Health Connect's Energy is in small calories.
            double? Kcal(AggregateRecordsGroupedByPeriodResponse group, AggregationType? type) =>
                type != null && types.Contains(type) && group.Get(type) is { } v ? v.JavaCast<Energy>()!.InCalories / 1000 : null;

            double? Grams(AggregateRecordsGroupedByPeriodResponse group, AggregationType? type) =>
                type != null && types.Contains(type) && group.Get(type) is { } v ? v.JavaCast<Mass>()!.InGrams : null;
        }

        /// <summary>Weight and body composition, the day's last measurement of each.</summary>
        async Task<List<BodyReading>> ReadBodyAsync(DateTime from, DateTime to)
        {
            var weight = await ReadIfAllowed<WeightRecord>(HealthPermissions.ReadWeight, r => r.Weight.InGrams / 1000);
            var fat = await ReadIfAllowed<BodyFatRecord>(HealthPermissions.ReadBodyFat, r => r.Percentage.Value);
            var lean = await ReadIfAllowed<LeanBodyMassRecord>(HealthPermissions.ReadLeanBodyMass, r => r.Mass.InGrams / 1000);
            var bone = await ReadIfAllowed<BoneMassRecord>(HealthPermissions.ReadBoneMass, r => r.Mass.InGrams / 1000);
            var water = await ReadIfAllowed<BodyWaterMassRecord>(HealthPermissions.ReadBodyWaterMass, r => r.BodyWaterMass.InGrams / 1000);
            // Watts, as an average over the day: × 86 400 s ÷ 4 184 J per kcal.
            var bmr = await ReadIfAllowed<BasalMetabolicRateRecord>(HealthPermissions.ReadBasalMetabolicRate, r => r.BasalMetabolicRate.InWatts * 86400 / 4184);

            var all = new[] { weight, fat, lean, bone, water, bmr };
            return [.. all.SelectMany(list => list.Select(m => m.Time.Date)).Distinct().Order().Select(day => new BodyReading(
                day, Last(weight, day), Last(fat, day), Last(lean, day), Last(bone, day), Last(water, day), Last(bmr, day)))];

            static double? Last(List<(DateTime Time, double Value)> list, DateTime day) =>
                list.Where(m => m.Time.Date == day).OrderBy(m => m.Time).Select(m => (double?)m.Value).LastOrDefault();

            Task<List<(DateTime Time, double Value)>> ReadIfAllowed<T>(string permission, Func<T, double> value) where T : InstantRecord =>
                Granted(permission) ? ReadAsync(from, to, value) : Task.FromResult(new List<(DateTime, double)>());
        }

        /// <summary>Every record of one kind between the two local times, each as its local time and value.</summary>
        async Task<List<(DateTime Time, double Value)>> ReadAsync<T>(DateTime from, DateTime to, Func<T, double> value) where T : InstantRecord
        {
            var values = new List<(DateTime, double)>();
            long? page = null;
            // A few pages at most: months of even several weigh-ins a day are far under one.
            for (var i = 0; i < 20; i++)
            {
                var builder = new ReadRecordsRequestUsingFilters.Builder(Java.Lang.Class.FromType(typeof(T)))
                    .SetTimeRangeFilter(new TimeInstantRangeFilter.Builder()
                        .SetStartTime(Instant(from))
                        .SetEndTime(Instant(to))
                        .Build())!
                    .SetPageSize(1000)!;
                foreach (var origin in Origins)
                    builder.AddDataOrigins(origin);
                if (page is { } token)
                    builder.SetPageToken(token);

                var result = await Call(receiver => _manager.ReadRecords(builder.Build()!, Context.MainExecutor!, receiver));
                var response = result.JavaCast<ReadRecordsResponse>()!;
                foreach (var record in response.Records)
                {
                    if (record is not Java.Lang.Object o || o.JavaCast<T>() is not { } r)
                        continue;
                    var time = DateTimeOffset.FromUnixTimeMilliseconds(r.Time.ToEpochMilli()).LocalDateTime;
                    values.Add((time, value(r)));
                }
                if (response.NextPageToken == -1)
                    break;
                page = response.NextPageToken;
            }
            return values;
        }

        static LocalDateTime Local(DateTime t) => LocalDateTime.Of(t.Year, t.Month, t.Day, t.Hour, t.Minute)!;

        static Java.Time.Instant Instant(DateTime local) =>
            Java.Time.Instant.OfEpochMilli(new DateTimeOffset(DateTime.SpecifyKind(local, DateTimeKind.Local)).ToUnixTimeMilliseconds())!;

        /// <summary>One Health Connect call, its callback turned into a task.</summary>
        static Task<Java.Lang.Object> Call(Action<IOutcomeReceiver> start)
        {
            var receiver = new Receiver();
            start(receiver);
            return receiver.Task;
        }
    }

    sealed class Receiver : Java.Lang.Object, IOutcomeReceiver
    {
        readonly TaskCompletionSource<Java.Lang.Object> _done = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<Java.Lang.Object> Task => _done.Task;

        public void OnResult(Java.Lang.Object? result) => _done.TrySetResult(result!);

        public void OnError(Java.Lang.Object error) =>
            _done.TrySetException(new InvalidOperationException(error.JavaCast<Java.Lang.Throwable>()?.Message ?? "Health Connect couldn't be read."));
    }
}

/// <summary>
/// Opened by Health Connect's "read privacy policy" link while asking for access (Android requires an app that asks for
/// health data to have one): shows Gym Book's privacy policy and closes.
/// </summary>
[Activity(Exported = true, NoHistory = true, Theme = "@android:style/Theme.Translucent.NoTitleBar",
    Permission = "android.permission.START_VIEW_PERMISSION_USAGE")]
[IntentFilter(["android.intent.action.VIEW_PERMISSION_USAGE"], Categories = ["android.intent.category.HEALTH_PERMISSIONS"])]
public class HealthPrivacyActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        try
        {
            var url = new Uri(Services.Sync.ApiConfig.BaseAddress, "privacy").ToString();
            StartActivity(new Intent(Intent.ActionView, Android.Net.Uri.Parse(url)));
        }
        catch (ActivityNotFoundException)
        {
            // No browser: nothing to show it in.
        }
        Finish();
    }
}

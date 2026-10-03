using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using GymBook.Models;
using GymBook.Services.Health;

namespace GymBook;

/// <summary>
/// Samsung Health read directly, through its Data SDK (Platforms/Android/Libs/samsung-health-data-api-*.aar): each day's
/// calories burned, activity calories and steps exactly as its Daily activity screen shows them, the food in its diary,
/// body composition and height. (Through Health Connect it shares only steps and a resting burn from the BMR, not those
/// totals.)
///
/// The SDK is a Kotlin library; rather than generate a binding for all of it, the few calls used here go straight
/// through JNI (signatures from javap on the .aar's classes.jar). Its async calls return an AsyncSingleFuture whose
/// callbacks are java.util.function.Consumers.
///
/// Until Samsung approves Gym Book as a partner app, the SDK only answers on phones with Samsung Health's developer
/// mode on (Samsung Health › Settings › About Samsung Health, tap the version 10 times, Developer mode › Data read).
/// </summary>
sealed class SamsungHealthData
{
    public const string Package = "com.sec.android.app.shealth";
    const string Sdk = "com/samsung/android/sdk/health/data/";
    const string Request = Sdk + "request/";
    const string Future = Sdk + "response/AsyncSingleFuture";
    const string DualTime = Request + "ReadDataRequest$DualTimeBuilder";

    // What's read: the DataTypes field and its DataType class.
    public const string Activity = "ACTIVITY_SUMMARY", Steps = "STEPS", Nutrition = "NUTRITION", Body = "BODY_COMPOSITION", Profile = "USER_PROFILE";

    static readonly (string Kind, string Type)[] Kinds =
    [
        (Activity, "DataType$ActivitySummaryType"),
        (Steps, "DataType$StepsType"),
        (Nutrition, "DataType$NutritionType"),
        (Body, "DataType$BodyCompositionType"),
        (Profile, "DataType$UserProfileDataType"),
    ];

    // The SDK's error codes (com.samsung.android.sdk.health.data.error.ErrorCode).
    const int ErrInvalidCaller = 1002, ErrNoUserPermission = 2000, ErrAccessControl = 2003,
        ErrPlatformNotInstalled = 3000, ErrOldVersionPlatform = 3001, ErrPlatformDisabled = 3002, ErrPlatformNotInitialized = 3003;

    Java.Lang.Object? _store;

    static Context Context => Android.App.Application.Context;

    /// <summary>Samsung Health is on this phone, and it's new enough for the SDK (Android 10+).</summary>
    public static bool IsInstalled
    {
        get
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(29))
                return false;
            try
            {
                return Context.PackageManager!.GetApplicationInfo(Package, 0).Enabled;
            }
            catch (PackageManager.NameNotFoundException)
            {
                return false;
            }
        }
    }

    /// <summary>What reading is allowed (of <see cref="Activity"/>, <see cref="Steps"/> and the rest).</summary>
    public async Task<HashSet<string>> AllowedAsync()
    {
        var permissions = Permissions();
        var granted = await Await(Call(Store, Sdk + "HealthDataStore", "getGrantedPermissionsAsync",
            "(Ljava/util/Set;)L" + Future + ";", new JValue(ToSet(permissions))));
        return Allowed(permissions, granted);
    }

    /// <summary>
    /// Shows Samsung Health's screen for allowing Gym Book to read; what was allowed. When Samsung Health needs
    /// something first (installing, updating, setting up), it shows that instead.
    /// </summary>
    public async Task<HashSet<string>> RequestAsync(Android.App.Activity activity)
    {
        try
        {
            var permissions = Permissions();
            var granted = await Await(Call(Store, Sdk + "HealthDataStore", "requestPermissionsAsync",
                "(Ljava/util/Set;Landroid/app/Activity;)L" + Future + ";", new JValue(ToSet(permissions)), new JValue(activity)));
            return Allowed(permissions, granted);
        }
        catch (SamsungHealthException e) when (e.Resolvable)
        {
            Resolve(e.Error, activity);
            throw;
        }
    }

    /// <summary>
    /// Each day's totals from <paramref name="from"/> (a local midnight) up to <paramref name="to"/> (now, for today):
    /// calories burned and activity calories when <paramref name="activity"/>, steps when <paramref name="steps"/>.
    /// </summary>
    public async Task<List<HealthDayReading>> ReadDaysAsync(DateTime from, DateTime to, bool activity, bool steps)
    {
        var summary = Request + "DataType$ActivitySummaryType";
        var operation = "L" + Sdk + "data/AggregateOperation;";
        Dictionary<DateTime, double> total = new(), active = new(), walked = new();
        if (activity)
        {
            total = await DailyAsync(StaticField(summary, "TOTAL_CALORIES_BURNED", operation), from, to);
            active = await DailyAsync(StaticField(summary, "TOTAL_ACTIVE_CALORIES_BURNED", operation), from, to);
        }
        if (steps)
            walked = await DailyAsync(StaticField(Request + "DataType$StepsType", "TOTAL", operation), from, to);

        // No basal: the total is Samsung Health's own and complete, used as it is.
        return [.. total.Keys.Union(active.Keys).Union(walked.Keys).Order().Select(day => new HealthDayReading(
            day,
            total.TryGetValue(day, out var all) ? (double?)all : null,
            active.TryGetValue(day, out var moving) ? (double?)moving : null,
            null,
            walked.TryGetValue(day, out var count) ? (int?)(int)Math.Min(int.MaxValue, count) : null,
            null, null, null, null))];
    }

    /// <summary>Each food in Samsung Health's diary between the two local times.</summary>
    public async Task<List<FoodReading>> ReadFoodsAsync(DateTime from, DateTime to)
    {
        const string type = "DataType$NutritionType";
        var calories = Field(type, "CALORIES");
        var protein = Field(type, "PROTEIN");
        var carbs = Field(type, "CARBOHYDRATE");
        var fat = Field(type, "TOTAL_FAT");
        var title = Field(type, "TITLE");
        var mealType = Field(type, "MEAL_TYPE");

        var foods = new List<FoodReading>();
        foreach (var point in await ReadAsync(Nutrition, type, from, to))
        {
            if (CallOrNull(point, Sdk + "data/HealthDataPoint", "getUid", "()Ljava/lang/String;")?.ToString() is not { Length: > 0 } id
                || StartTime(point) is not { } time)
                continue;
            MealType? meal = Value(point, mealType)?.ToString() switch
            {
                "BREAKFAST" => MealType.Breakfast,
                "LUNCH" => MealType.Lunch,
                "DINNER" => MealType.Dinner,
                "MORNING_SNACK" or "AFTERNOON_SNACK" or "EVENING_SNACK" => MealType.Snack,
                _ => null,
            };
            var source = CallOrNull(point, Sdk + "data/HealthDataPoint", "getDataSource", "()L" + Sdk + "data/DataSource;") is { } dataSource
                ? CallOrNull(dataSource, Sdk + "data/DataSource", "getAppId", "()Ljava/lang/String;")?.ToString()
                : null;
            foods.Add(new FoodReading(id, time, meal, Value(point, title)?.ToString(),
                Number(point, calories), Number(point, protein), Number(point, carbs), Number(point, fat), source ?? Package));
        }
        return foods;
    }

    /// <summary>Weight and body composition between the two local times, each day's last of each.</summary>
    public async Task<List<BodyReading>> ReadBodyAsync(DateTime from, DateTime to)
    {
        const string type = "DataType$BodyCompositionType";
        var weight = Field(type, "WEIGHT");
        var bodyFat = Field(type, "BODY_FAT");
        var fatFree = Field(type, "FAT_FREE_MASS");
        var water = Field(type, "TOTAL_BODY_WATER");
        var bmr = Field(type, "BASAL_METABOLIC_RATE");

        var measured = new List<(DateTime Time, double? Weight, double? Fat, double? Lean, double? Water, double? Bmr)>();
        foreach (var point in await ReadAsync(Body, type, from, to))
            if (StartTime(point) is { } time)
                measured.Add((time, Number(point, weight), Number(point, bodyFat), Number(point, fatFree), Number(point, water), Number(point, bmr)));

        return [.. measured.GroupBy(m => m.Time.Date).OrderBy(g => g.Key).Select(day =>
        {
            var ordered = day.OrderBy(m => m.Time).ToList();
            double? Last(Func<(DateTime, double?, double?, double?, double?, double?), double?> value) =>
                ordered.Select(m => value(m)).LastOrDefault(v => v != null);
            return new BodyReading(day.Key, Last(m => m.Item2), Last(m => m.Item3), Last(m => m.Item4), null, Last(m => m.Item5), Last(m => m.Item6));
        })];
    }

    /// <summary>The height on Samsung Health's profile, in cm.</summary>
    public async Task<double?> ReadHeightAsync()
    {
        var type = Request + "DataType$UserProfileDataType";
        var dataType = StaticField(Request + "DataTypes", Profile, "L" + type + ";");
        var builder = Call(dataType, type, "getReadDataRequestBuilder", "()L" + Request + "ReadDataRequest$UserProfileBuilder;");
        var request = Call(builder, Request + "ReadDataRequest$UserProfileBuilder", "build", "()L" + Request + "ReadDataRequest;");
        var response = await Await(Call(Store, Sdk + "HealthDataStore", "readDataAsync",
            "(L" + Request + "ReadDataRequest;)L" + Future + ";", new JValue(request)));
        if (response == null)
            return null;
        var height = StaticField(type, "HEIGHT", "L" + Sdk + "data/Field;");
        var list = Call(response, Sdk + "response/DataResponse", "getDataList", "()Ljava/util/List;").JavaCast<Java.Util.IList>()!;
        for (var i = list.Size() - 1; i >= 0; i--)
            if (CallOrNull(list.Get(i)!, Sdk + "data/UserDataPoint", "getValue", "(L" + Sdk + "data/Field;)Ljava/lang/Object;", new JValue(height))
                    ?.JavaCast<Java.Lang.Number>() is { } cm)
                return cm.DoubleValue();
        return null;
    }

    /// <summary>Every record of one kind between the two local times, page by page.</summary>
    async Task<List<Java.Lang.Object>> ReadAsync(string kind, string type, DateTime from, DateTime to)
    {
        var points = new List<Java.Lang.Object>();
        string? token = null;
        // A few pages at most: a month of food or weigh-ins is far under that.
        for (var page = 0; page < 20; page++)
        {
            var dataType = StaticField(Request + "DataTypes", kind, "L" + Request + type + ";");
            var builder = Call(dataType, Request + type, "getReadDataRequestBuilder", "()L" + DualTime + ";");
            builder = Call(builder, DualTime, "setLocalTimeFilter", "(L" + Request + "LocalTimeFilter;)L" + DualTime + ";", new JValue(Filter(from, to)));
            if (token != null)
                builder = Call(builder, DualTime, "setPageToken", "(Ljava/lang/String;)L" + DualTime + ";", new JValue(new Java.Lang.String(token)));
            var request = Call(builder, DualTime, "build", "()L" + Request + "ReadDataRequest;");
            var response = await Await(Call(Store, Sdk + "HealthDataStore", "readDataAsync",
                "(L" + Request + "ReadDataRequest;)L" + Future + ";", new JValue(request)));
            if (response == null)
                break;
            var list = Call(response, Sdk + "response/DataResponse", "getDataList", "()Ljava/util/List;").JavaCast<Java.Util.IList>()!;
            for (var i = 0; i < list.Size(); i++)
                if (list.Get(i) is { } point)
                    points.Add(point);
            token = CallOrNull(response, Sdk + "response/DataResponse", "getPageToken", "()Ljava/lang/String;")?.ToString();
            if (string.IsNullOrEmpty(token))
                break;
        }
        return points;
    }

    /// <summary>One aggregate (a total) for each local day between the two times, by date; days with nothing left out.</summary>
    async Task<Dictionary<DateTime, double>> DailyAsync(Java.Lang.Object operation, DateTime from, DateTime to)
    {
        var group = StaticCall(Request + "LocalTimeGroup", "of",
            "(L" + Request + "LocalTimeGroupUnit;I)L" + Request + "LocalTimeGroup;",
            new JValue(StaticField(Request + "LocalTimeGroupUnit", "DAILY", "L" + Request + "LocalTimeGroupUnit;")), new JValue(1));

        var builder = Call(operation, Sdk + "data/AggregateOperation", "getRequestBuilder", "()L" + Request + "AggregateRequest$Builder;");
        builder = Call(builder, Request + "AggregateRequest$LocalTimeBuilder", "setLocalTimeFilterWithGroup",
            "(L" + Request + "LocalTimeFilter;L" + Request + "LocalTimeGroup;)L" + Request + "AggregateRequest$LocalTimeBuilder;",
            new JValue(Filter(from, to)), new JValue(group));
        var request = Call(builder, Request + "AggregateRequest$LocalTimeBuilder", "build", "()L" + Request + "AggregateRequest;");

        var response = await Await(Call(Store, Sdk + "HealthDataStore", "aggregateDataAsync",
            "(L" + Request + "AggregateRequest;)L" + Future + ";", new JValue(request)));
        var days = new Dictionary<DateTime, double>();
        if (response == null)
            return days;
        var list = Call(response, Sdk + "response/DataResponse", "getDataList", "()Ljava/util/List;").JavaCast<Java.Util.IList>()!;
        for (var i = 0; i < list.Size(); i++)
        {
            var data = list.Get(i)!;
            var value = CallOrNull(data, Sdk + "data/AggregatedData", "getValue", "()Ljava/lang/Object;");
            var start = CallOrNull(data, Sdk + "data/AggregatedData", "getStartLocalDateTime", "()Ljava/time/LocalDateTime;")?.JavaCast<Java.Time.LocalDateTime>();
            if (value?.JavaCast<Java.Lang.Number>() is not { } number || start == null)
                continue;
            days[new DateTime(start.Year, start.MonthValue, start.DayOfMonth)] = number.DoubleValue();
        }
        return days;
    }

    static Java.Lang.Object Filter(DateTime from, DateTime to) => StaticCall(Request + "LocalTimeFilter", "of",
        "(Ljava/time/LocalDateTime;Ljava/time/LocalDateTime;)L" + Request + "LocalTimeFilter;",
        new JValue(LocalDateTime(from)), new JValue(LocalDateTime(to)));

    static Java.Lang.Object Field(string type, string name) => StaticField(Request + type, name, "L" + Sdk + "data/Field;");

    static Java.Lang.Object? Value(Java.Lang.Object point, Java.Lang.Object field) =>
        CallOrNull(point, Sdk + "data/HealthDataPoint", "getValue", "(L" + Sdk + "data/Field;)Ljava/lang/Object;", new JValue(field));

    static double? Number(Java.Lang.Object point, Java.Lang.Object field) =>
        Value(point, field)?.JavaCast<Java.Lang.Number>()?.DoubleValue();

    /// <summary>When a record was measured or eaten, in the phone's local time.</summary>
    static DateTime? StartTime(Java.Lang.Object point) =>
        CallOrNull(point, Sdk + "data/HealthDataPoint", "getStartLocalDateTime", "()Ljava/time/LocalDateTime;")?.JavaCast<Java.Time.LocalDateTime>() is { } t
            ? new DateTime(t.Year, t.MonthValue, t.DayOfMonth, t.Hour, t.Minute, t.Second)
            : null;

    /// <summary>Read access to each of <see cref="Kinds"/>, by kind.</summary>
    static Dictionary<string, Java.Lang.Object> Permissions()
    {
        var read = StaticField(Sdk + "permission/AccessType", "READ", "L" + Sdk + "permission/AccessType;");
        return Kinds.ToDictionary(k => k.Kind, k => StaticCall(Sdk + "permission/Permission", "of",
            "(L" + Request + "DataType;L" + Sdk + "permission/AccessType;)L" + Sdk + "permission/Permission;",
            new JValue(StaticField(Request + "DataTypes", k.Kind, "L" + Request + k.Type + ";")), new JValue(read)));
    }

    static Java.Util.HashSet ToSet(Dictionary<string, Java.Lang.Object> permissions)
    {
        var set = new Java.Util.HashSet();
        foreach (var permission in permissions.Values)
            set.Add(permission);
        return set;
    }

    /// <summary>The kinds whose permission is in <paramref name="granted"/> (a Set of Permissions, which compare by value).</summary>
    static HashSet<string> Allowed(Dictionary<string, Java.Lang.Object> permissions, Java.Lang.Object? granted) =>
        granted?.JavaCast<Java.Util.ISet>() is { } set ? [.. permissions.Where(p => set.Contains(p.Value)).Select(p => p.Key)] : [];

    Java.Lang.Object Store => _store ??= StaticCall(Sdk + "HealthDataService", "getStore",
        "(Landroid/content/Context;)L" + Sdk + "HealthDataStore;", new JValue(Context));

    static Java.Time.LocalDateTime LocalDateTime(DateTime t) =>
        Java.Time.LocalDateTime.Of(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second)!;

    /// <summary>Waits for an AsyncSingleFuture: its result, or its error as a <see cref="SamsungHealthException"/>.</summary>
    static Task<Java.Lang.Object?> Await(Java.Lang.Object future)
    {
        var done = new TaskCompletionSource<Java.Lang.Object?>(TaskCreationOptions.RunContinuationsAsynchronously);
        CallOrNull(future, Sdk + "response/AsyncSingleFuture", "setCallback",
            "(Landroid/os/Looper;Ljava/util/function/Consumer;Ljava/util/function/Consumer;)V",
            new JValue(Looper.MainLooper!),
            new JValue(new Consumer(result => done.TrySetResult(result))),
            new JValue(new Consumer(error => done.TrySetException(SamsungHealthException.From(error?.JavaCast<Java.Lang.Throwable>())))));
        return done.Task;
    }

    /// <summary>Lets Samsung Health fix what it can (install, update, finish setting up) on its own screen.</summary>
    static void Resolve(Java.Lang.Throwable error, Android.App.Activity activity)
    {
        try
        {
            CallOrNull(error, Sdk + "error/ResolvablePlatformException", "resolve", "(Landroid/app/Activity;)V", new JValue(activity));
        }
        catch (Exception)
        {
        }
    }

    sealed class Consumer(Action<Java.Lang.Object?> accept) : Java.Lang.Object, Java.Util.Functions.IConsumer
    {
        public void Accept(Java.Lang.Object? t) => accept(t);
    }

    // JNI helpers. JNIEnv.FindClass returns a global reference (it also finds the app's own classes off the main
    // thread); results come back as local references, owned by the wrappers made here.

    static Java.Lang.Object StaticField(string type, string name, string signature)
    {
        var cls = JNIEnv.FindClass(type);
        try
        {
            var field = JNIEnv.GetStaticFieldID(cls, name, signature);
            return Wrap(JNIEnv.GetStaticObjectField(cls, field), $"{type}.{name}");
        }
        finally
        {
            JNIEnv.DeleteGlobalRef(cls);
        }
    }

    static Java.Lang.Object StaticCall(string type, string name, string signature, params JValue[] args)
    {
        var cls = JNIEnv.FindClass(type);
        try
        {
            var method = JNIEnv.GetStaticMethodID(cls, name, signature);
            return Wrap(JNIEnv.CallStaticObjectMethod(cls, method, args), $"{type}.{name}");
        }
        finally
        {
            JNIEnv.DeleteGlobalRef(cls);
        }
    }

    static Java.Lang.Object Call(IJavaObject target, string type, string name, string signature, params JValue[] args) =>
        CallOrNull(target, type, name, signature, args) ?? throw new InvalidOperationException($"{type}.{name} returned nothing.");

    static Java.Lang.Object? CallOrNull(IJavaObject target, string type, string name, string signature, params JValue[] args)
    {
        var cls = JNIEnv.FindClass(type);
        try
        {
            var method = JNIEnv.GetMethodID(cls, name, signature);
            if (signature.EndsWith(")V", StringComparison.Ordinal))
            {
                JNIEnv.CallVoidMethod(target.Handle, method, args);
                return null;
            }
            return Java.Lang.Object.GetObject<Java.Lang.Object>(JNIEnv.CallObjectMethod(target.Handle, method, args), JniHandleOwnership.TransferLocalRef);
        }
        finally
        {
            JNIEnv.DeleteGlobalRef(cls);
        }
    }

    static Java.Lang.Object Wrap(IntPtr handle, string what) =>
        Java.Lang.Object.GetObject<Java.Lang.Object>(handle, JniHandleOwnership.TransferLocalRef)
        ?? throw new InvalidOperationException($"{what} is null.");

    /// <summary>The SDK's error (a HealthDataException), with its code and a message to show.</summary>
    public sealed class SamsungHealthException(Java.Lang.Throwable error, int? code, bool resolvable, string message) : Exception(message)
    {
        public Java.Lang.Throwable Error { get; } = error;
        public int? Code { get; } = code;

        /// <summary>Samsung Health can fix it on its own screen (see <see cref="Resolve"/>).</summary>
        public bool Resolvable { get; } = resolvable;

        public static SamsungHealthException From(Java.Lang.Throwable? error)
        {
            error ??= new Java.Lang.Throwable("Samsung Health didn't answer.");
            int? code = null;
            var resolvable = false;
            try
            {
                if (JNIEnv.IsInstanceOf(error.Handle, Class(Sdk + "error/HealthDataException")))
                    code = CallOrNull(error, Sdk + "error/HealthDataException", "getErrorCode", "()Ljava/lang/Integer;")?.JavaCast<Java.Lang.Integer>()?.IntValue();
                if (JNIEnv.IsInstanceOf(error.Handle, Class(Sdk + "error/ResolvablePlatformException")))
                    resolvable = CallBoolean(error, Sdk + "error/ResolvablePlatformException", "getHasResolution");
            }
            catch (Exception)
            {
            }
            var message = code switch
            {
                ErrInvalidCaller or ErrAccessControl =>
                    "Samsung Health doesn't share with Gym Book yet: turn on its developer mode (Samsung Health › Settings › About Samsung Health, tap the version 10 times, then Developer mode › Data read).",
                ErrNoUserPermission => "Not allowed in Samsung Health.",
                ErrPlatformNotInstalled => "Samsung Health isn't installed.",
                ErrOldVersionPlatform => "Samsung Health needs updating.",
                ErrPlatformDisabled => "Samsung Health is turned off.",
                ErrPlatformNotInitialized => "Open Samsung Health and finish setting it up.",
                _ => error.Message is { Length: > 0 } m ? m : "Samsung Health didn't answer.",
            };
            return new SamsungHealthException(error, code, resolvable, message);
        }

        static IntPtr _healthDataException, _resolvable;

        static IntPtr Class(string type) => type.EndsWith("ResolvablePlatformException", StringComparison.Ordinal)
            ? _resolvable != IntPtr.Zero ? _resolvable : _resolvable = JNIEnv.FindClass(type)
            : _healthDataException != IntPtr.Zero ? _healthDataException : _healthDataException = JNIEnv.FindClass(type);

        static bool CallBoolean(IJavaObject target, string type, string name)
        {
            var cls = JNIEnv.FindClass(type);
            try
            {
                return JNIEnv.CallBooleanMethod(target.Handle, JNIEnv.GetMethodID(cls, name, "()Z"));
            }
            finally
            {
                JNIEnv.DeleteGlobalRef(cls);
            }
        }
    }
}

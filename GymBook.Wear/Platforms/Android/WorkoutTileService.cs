using Android.App;
using AndroidX.Concurrent.Futures;
using AndroidX.Wear.Tiles;
// The Tiles library still ships deprecated copies of these builders under the same names; the Tile uses ProtoLayout's.
using ActionBuilders = AndroidX.Wear.ProtoLayout.ActionBuilders;
using ColorBuilders = AndroidX.Wear.ProtoLayout.ColorBuilders;
using LayoutElementBuilders = AndroidX.Wear.ProtoLayout.LayoutElementBuilders;
using ModifiersBuilders = AndroidX.Wear.ProtoLayout.ModifiersBuilders;
using ResourceBuilders = AndroidX.Wear.ProtoLayout.ResourceBuilders;
using TimelineBuilders = AndroidX.Wear.ProtoLayout.TimelineBuilders;
using Google.Common.Util.Concurrent;
using GymBook.Services;
using GymBook.Services.Sync;

namespace GymBook.Wear;

/// <summary>
/// Gym Book's Tile, one swipe from the watch face: the workout in progress (the watch's own), otherwise the active
/// plan's next workout, otherwise a prompt to open the app. Tapping it opens the app, which picks up from there.
/// Refreshed when the data changes (<see cref="RequestUpdate"/>) and every 15 minutes.
/// </summary>
[Service(Name = "com.yehorskudilov.gymbook.WorkoutTileService", Exported = true, Label = "Gym Book",
    Icon = "@mipmap/appicon", Permission = "com.google.android.wearable.permission.BIND_TILE_PROVIDER")]
[IntentFilter(new[] { "androidx.wear.tiles.action.BIND_TILE_PROVIDER" })]
[MetaData("androidx.wear.tiles.PREVIEW", Resource = "@mipmap/appicon")]
public class WorkoutTileService : TileService
{
    const string ResourcesVersion = "1";
    const long FreshnessMillis = 15 * 60 * 1000;

    static readonly int Accent = unchecked((int)0xFF3F7DFF);
    static readonly int Primary = unchecked((int)0xFFF4F6FB);
    static readonly int Secondary = unchecked((int)0xFF9AA3B5);
    static readonly int Success = unchecked((int)0xFF2ED47A);

    /// <summary>Asks Wear OS to redraw the Tile soon (it throttles this), e.g. after a workout started or finished.</summary>
    public static void RequestUpdate()
    {
        try
        {
            GetUpdater(Android.App.Application.Context).RequestUpdate(Java.Lang.Class.FromType(typeof(WorkoutTileService)));
        }
        catch (Exception e)
        {
            System.Diagnostics.Debug.WriteLine($"GymBook.Wear: tile update failed: {e.Message}");
        }
    }

    public override IListenableFuture OnTileRequest(RequestBuilders.TileRequest request)
    {
        var future = ResolvableFuture.Create();
        try
        {
            var device = request.DeviceConfiguration;
            var (title, line, detail, color) = Content();
            var column = new LayoutElementBuilders.Column.Builder()
                .SetHorizontalAlignment(LayoutElementBuilders.HorizontalAlignCenter)
                .AddContent(Text(title, LayoutElementBuilders.FontStyles.Caption1(device), Accent))
                .AddContent(Text(line, LayoutElementBuilders.FontStyles.Title2(device), color))
                .AddContent(Text(detail, LayoutElementBuilders.FontStyles.Caption1(device), Secondary))
                .SetModifiers(new ModifiersBuilders.Modifiers.Builder().SetClickable(OpenApp()).Build())
                .Build();
            var tile = new TileBuilders.Tile.Builder()
                .SetResourcesVersion(ResourcesVersion)
                .SetFreshnessIntervalMillis(FreshnessMillis)
                .SetTileTimeline(TimelineBuilders.Timeline.FromLayoutElement(column))
                .Build();
            future.Set(tile);
        }
        catch (Exception e)
        {
            future.SetException(new Java.Lang.RuntimeException(e.Message));
        }
        return future;
    }

    public override IListenableFuture OnTileResourcesRequest(RequestBuilders.ResourcesRequest requestParams)
    {
        var future = ResolvableFuture.Create();
        future.Set(new ResourceBuilders.Resources.Builder().SetVersion(ResourcesVersion).Build());
        return future;
    }

    /// <summary>What the Tile says, from the watch's own data (the app's services, shared with this service's process).</summary>
    static (string Title, string Line, string Detail, int Color) Content()
    {
        var services = IPlatformApplication.Current?.Services;
        var store = services?.GetService<DataStore>();
        var workouts = services?.GetService<WorkoutService>();
        if (store == null || workouts == null)
            return ("GYM BOOK", "Start a workout", "Tap to open", Primary);

        if (workouts.Active is { } active)
        {
            var working = active.Exercises.SelectMany(e => e.Sets).Where(s => !s.IsWarmup).ToList();
            return ("IN PROGRESS", active.Name, $"{working.Count(s => s.IsCompleted)}/{working.Count} sets · tap to resume", Success);
        }
        var signedIn = services?.GetService<AuthSession>()?.IsSignedIn == true;
        if (signedIn && store.ActivePlan is { Workouts.Count: > 0 } plan)
        {
            var next = plan.Workouts[Math.Clamp(plan.NextWorkoutIndex, 0, plan.Workouts.Count - 1)];
            return ("NEXT WORKOUT", next.Name, $"{next.Exercises.Count} exercises · tap to start", Primary);
        }
        return ("GYM BOOK", "Quick workout", "Tap to open", Primary);
    }

    static LayoutElementBuilders.Text Text(string text, LayoutElementBuilders.FontStyle.Builder style, int color) =>
        new LayoutElementBuilders.Text.Builder()
            .SetText(text)
            .SetMaxLines(2)
            .SetFontStyle(style.SetColor(ColorBuilders.Argb(color)).Build())
            .Build();

    // Opens the app's activity by its fixed name (see MainActivity).
    ModifiersBuilders.Clickable OpenApp() =>
        new ModifiersBuilders.Clickable.Builder()
            .SetId("open")
            .SetOnClick(new ActionBuilders.LaunchAction.Builder()
                .SetAndroidActivity(new ActionBuilders.AndroidActivity.Builder()
                    .SetPackageName(PackageName!)
                    .SetClassName("com.yehorskudilov.gymbook.MainActivity")
                    .Build())
                .Build())
            .Build();
}

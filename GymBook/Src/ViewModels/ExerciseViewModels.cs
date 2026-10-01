using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Controls;
using GymBook.Models;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>
/// The exercise list shared by the library tab and the picker: ranked search (<see cref="ExerciseSearch"/>), quick
/// muscle and kind chips, and the full filters (<see cref="ExerciseFilter"/>) behind the filter button.
/// </summary>
public abstract partial class ExerciseListViewModel : BaseViewModel
{
    protected readonly DataStore Store;

    protected ExerciseListViewModel(DataStore store)
    {
        Store = store;
        Chips.Add(new ChipItem("All", null, SelectChip) { IsSelected = true });
        foreach (var m in Enum.GetValues<MuscleGroup>())
            Chips.Add(new ChipItem(m.Display(), m, SelectChip));
        foreach (var kind in ExerciseFilter.Kinds)
            Chips.Add(new ChipItem(kind.Title, kind.Categories, SelectChip));
        Filters.Changed += (_, _) => Filter();
    }

    public ExerciseFilter Filters { get; } = new();

    public ObservableCollection<ChipItem> Chips { get; } = [];

    [ObservableProperty] string searchText = "";
    [ObservableProperty] List<ExerciseItem> items = [];
    [ObservableProperty] string countText = "";

    /// <summary>How many filters are on beyond the chips, shown on the filter button.</summary>
    [ObservableProperty] string filterBadge = "";
    [ObservableProperty] bool hasFilterBadge;
    [ObservableProperty] bool canClearFilters;

    partial void OnSearchTextChanged(string value) => Filter();

    // A chip toggles its muscle or kind in the filter, so several can be on; "All" clears them.
    void SelectChip(ChipItem chip)
    {
        switch (chip.Value)
        {
            case MuscleGroup m:
                if (!Filters.Muscles.Remove(m))
                    Filters.Muscles.Add(m);
                break;
            case ExerciseCategory[] kinds:
                if (kinds.All(Filters.Categories.Contains))
                    Filters.Categories.ExceptWith(kinds);
                else
                    Filters.Categories.UnionWith(kinds);
                break;
            default:
                Filters.Muscles.Clear();
                Filters.Categories.Clear();
                break;
        }
        Filters.Notify();
    }

    /// <summary>What the list starts from, before searching and filtering.</summary>
    protected virtual IEnumerable<Exercise> Source => Store.AllExercises.Where(e => !e.IsDeleted);

    /// <summary>The source is already in a meaningful order (most alike first) that "best match" keeps.</summary>
    protected virtual bool IsRanked => false;

    protected void Filter()
    {
        foreach (var c in Chips)
            c.IsSelected = c.Value switch
            {
                MuscleGroup m => Filters.Muscles.Contains(m),
                ExerciseCategory[] kinds => kinds.All(Filters.Categories.Contains),
                _ => Filters.Muscles.Count == 0 && Filters.Categories.Count == 0,
            };

        var query = SearchText.Trim();
        var list = Source;
        if (query.Length > 0)
            list = ExerciseSearch.Search(list, query);
        Items = Filters.Apply(list, Store, searching: query.Length > 0 || IsRanked)
            .Select(e => new ExerciseItem(e, OnTap) { IsSelected = IsSelected(e) })
            .ToList();
        CountText = Items.Count == 1 ? "1 exercise" : $"{Items.Count} exercises";
        FilterBadge = Filters.ActiveCount.ToString();
        HasFilterBadge = Filters.ActiveCount > 0;
        CanClearFilters = !Filters.IsEmpty;
    }

    [RelayCommand]
    Task OpenFilters() => Views.ExerciseFilterPage.ShowAsync(this);

    [RelayCommand]
    void ClearFilters() => Filters.Clear();

    protected virtual bool IsSelected(Exercise e) => false;

    protected abstract void OnTap(ExerciseItem item);
}

public partial class ExercisesViewModel(DataStore store, DialogService dialogs) : ExerciseListViewModel(store)
{
    public override Task OnAppearingAsync()
    {
        Filter();
        return Task.CompletedTask;
    }

    protected override void OnTap(ExerciseItem item) => _ = GoTo($"{Routes.Exercise}?id={item.Exercise.Id}");

    [RelayCommand]
    async Task CreateCustom()
    {
        var name = await dialogs.Prompt("New exercise", "Exercise name", accept: "Next");
        if (string.IsNullOrWhiteSpace(name))
            return;
        var muscle = await dialogs.ActionSheet("Primary muscle", null, [.. Enum.GetValues<MuscleGroup>().Select(m => m.Display())]);
        if (muscle == null)
            return;
        var equipment = await dialogs.ActionSheet("Equipment", null, [.. Enum.GetValues<Equipment>().Select(e => e.Display())]);
        if (equipment == null)
            return;
        var mechanic = await dialogs.ActionSheet("Type", null, "Compound (multi-joint)", "Isolation (single-joint)");
        var ex = new Exercise
        {
            Id = "custom_" + Guid.NewGuid().ToString("N")[..8],
            Name = name.Trim(),
            PrimaryMuscle = Enum.GetValues<MuscleGroup>().First(m => m.Display() == muscle),
            Equipment = Enum.GetValues<Equipment>().First(e => e.Display() == equipment),
            Mechanic = mechanic?.StartsWith("Compound") == true ? Mechanic.Compound : Mechanic.Isolation,
            Instructions = "Custom exercise.",
            IsCustom = true,
        };
        Store.Data.CustomExercises.Add(ex);
        Store.Save();
        Filter();
    }
}

public partial class ExercisePickerViewModel(DataStore store) : ExerciseListViewModel(store)
{
    readonly List<Exercise> _selected = [];

    public Action<List<Exercise>>? Completed { get; set; }

    [ObservableProperty] string addText = "Add";
    [ObservableProperty] bool canAdd;
    [ObservableProperty] string title = "Add exercises";

    /// <summary>Picking one exercise (e.g. a replacement): a tap picks it straight away, without the Add button.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMultiple))]
    bool isSingle;

    public bool IsMultiple => !IsSingle;

    // Replacing an exercise: what it's replacing, so the list can show just the ones like it.
    Exercise? _similarTo;

    /// <summary>Showing only exercises like the one being replaced (same muscle and movement first), not the whole library.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SimilarBackground), nameof(SimilarTextColor), nameof(AllBackground), nameof(AllTextColor))]
    bool isFocused;

    /// <summary>There's an exercise to be like (a replacement), so the Similar / All switch shows.</summary>
    [ObservableProperty] bool canFocus;
    [ObservableProperty] string focusHint = "";

    public Color SimilarBackground => IsFocused ? Color.FromArgb("#3F7DFF") : Colors.Transparent;
    public Color SimilarTextColor => IsFocused ? Colors.White : Color.FromArgb("#9AA3B5");
    public Color AllBackground => IsFocused ? Colors.Transparent : Color.FromArgb("#3F7DFF");
    public Color AllTextColor => IsFocused ? Color.FromArgb("#9AA3B5") : Colors.White;

    /// <summary>
    /// Ready for the next pick: several exercises to add, or with <paramref name="single"/> one, under <paramref name="title"/>.
    /// With <paramref name="similarTo"/> (replacing it) the list starts on the exercises like it, with the full list a tap away.
    /// </summary>
    public void Reset(bool single = false, string title = "Add exercises", Exercise? similarTo = null)
    {
        IsSingle = single;
        Title = title;
        _similarTo = similarTo;
        CanFocus = similarTo != null;
        IsFocused = CanFocus;
        FocusHint = similarTo == null ? "" : $"Same muscles and movement as {similarTo.Name}, closest first";
        _selected.Clear();
        SearchText = "";
        Filters.Clear();
        UpdateAdd();
        Filter();
    }

    protected override IEnumerable<Exercise> Source => IsFocused && _similarTo is { } original
        ? base.Source
            .Select(e => (e, score: ExerciseSearch.Similarity(original, e)))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.e.Name)
            .Select(x => x.e)
        : base.Source;

    protected override bool IsRanked => IsFocused;

    [RelayCommand]
    void ShowSimilar()
    {
        IsFocused = true;
        Filter();
    }

    [RelayCommand]
    void ShowAll()
    {
        IsFocused = false;
        Filter();
    }

    protected override bool IsSelected(Exercise e) => _selected.Contains(e);

    protected override void OnTap(ExerciseItem item)
    {
        if (IsSingle)
        {
            Completed?.Invoke([item.Exercise]);
            return;
        }
        if (!_selected.Remove(item.Exercise))
            _selected.Add(item.Exercise);
        item.IsSelected = _selected.Contains(item.Exercise);
        UpdateAdd();
    }

    void UpdateAdd()
    {
        CanAdd = _selected.Count > 0;
        AddText = _selected.Count > 0 ? $"Add ({_selected.Count})" : "Add";
    }

    [RelayCommand]
    void Add() => Completed?.Invoke([.. _selected]);

    [RelayCommand]
    public void Cancel() => Completed?.Invoke([]);
}

public partial class ExerciseDetailViewModel(DataStore store, StatsService stats, Units units, DialogService dialogs, ProgressionEngine progression)
    : BaseViewModel, IQueryAttributable
{
    // Coaching for the active plan's goal (or the profile's)
    [ObservableProperty] string goalTitle = "";
    [ObservableProperty] string goalTarget = "";
    [ObservableProperty] List<string> goalTips = [];
    [ObservableProperty] string startingWeight = "";
    [ObservableProperty] bool hasStartingWeight;

    string? _id;

    [ObservableProperty] string name = "";
    [ObservableProperty] string subtitle = "";
    [ObservableProperty] string instructions = "";
    [ObservableProperty] string primaryMuscle = "";
    [ObservableProperty] string secondaryMuscles = "";
    [ObservableProperty] bool hasSecondary;
    [ObservableProperty] string mechanic = "";
    [ObservableProperty] IDrawable muscleMap = MuscleMapDrawable.Empty;
    [ObservableProperty] bool hasHistory;
    [ObservableProperty] string bestE1Rm = "—";
    [ObservableProperty] string bestSet = "—";
    [ObservableProperty] string timesPerformed = "0";
    [ObservableProperty] IDrawable? chart;
    [ObservableProperty] List<LineItem> history = [];
    [ObservableProperty] bool isCustom;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(WatchText))]
    bool hasVideo;

    public string WatchText => HasVideo ? "Open in YouTube" : "Search form videos";
    ExerciseVideo? _video;

    // The demonstration: a looping animation shipped in the app (or the exercise's picture until it has one), and
    // the YouTube video. The animation shows first every time the page opens; the video only with a connection,
    // so the web view never gets to show a connection error.
    [ObservableProperty] bool hasMedia;
    [ObservableProperty] bool hasMediaChoice;
    [ObservableProperty] bool hasAnimation;
    [ObservableProperty] ImageSource? animation;
    [ObservableProperty] bool showVideo;
    [ObservableProperty] bool canShowVideo;
    [ObservableProperty] bool showsVideo;
    [ObservableProperty] bool showsAnimation;
    [ObservableProperty] bool videoOffline;
    /// <summary>The player's embed URL; null whenever the video isn't on screen (offline, the animation chosen, the page left).</summary>
    [ObservableProperty] string? videoUrl;
    string? _animationFor;
    bool _visible, _videoFailed;
    [ObservableProperty] string tags = "";
    [ObservableProperty] bool hasTags;
    [ObservableProperty] List<LineItem> steps = [];
    [ObservableProperty] bool hasSteps;
    [ObservableProperty] List<string> formTips = [];
    [ObservableProperty] bool hasFormTips;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _id = query["id"]?.ToString();
        // Opening an exercise always starts on the animation.
        ShowVideo = false;
    }

    public override Task OnAppearingAsync()
    {
        var ex = store.GetExercise(_id ?? "");
        if (ex == null)
            return GoBack();

        _visible = true;
        _videoFailed = false;
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        Connectivity.Current.ConnectivityChanged += OnConnectivityChanged;

        Name = ex.Name;
        Subtitle = ex.Subtitle;
        Instructions = ex.Instructions;
        PrimaryMuscle = ex.PrimaryMuscle.Display();
        SecondaryMuscles = string.Join(", ", ex.SecondaryMuscles.Select(m => m.Display()));
        HasSecondary = ex.SecondaryMuscles.Count > 0;
        Mechanic = ex.Mechanic == Models.Mechanic.Compound ? "Compound" : "Isolation";
        MuscleMap = MuscleMapDrawable.ForExercise(ex);
        IsCustom = ex.IsCustom;

        var details = ExerciseLibrary.Details(ex.Id);
        _video = details?.Video;
        HasVideo = _video != null;
        HasAnimation = ExerciseLibrary.Animation(ex.Id) != null || ExerciseLibrary.Thumbnail(ex.Id) != null;
        if (_animationFor != ex.Id)
        {
            _animationFor = ex.Id;
            Animation = null;
            _ = LoadAnimationAsync(ex.Id);
        }
        UpdateMedia();
        Tags = details == null ? "" : $"{details.Category} · {details.Level}";
        HasTags = details != null;
        Steps = details?.Steps.Select((s, i) => new LineItem { Title = (i + 1).ToString(), Detail = s }).ToList() ?? [];
        HasSteps = Steps.Count > 0;
        FormTips = [.. details?.Tips ?? []];
        HasFormTips = FormTips.Count > 0;

        var sessions = store.History
            .Select(s => (s, e: s.Exercises.FirstOrDefault(x => x.ExerciseId == ex.Id)))
            .Where(x => x.e != null)
            .ToList();
        HasHistory = sessions.Count > 0;
        TimesPerformed = sessions.Count.ToString();

        var goal = store.ActivePlan?.Goal ?? store.Profile.Goal;
        var target = TrainingGoals.Prescription(goal, store.Profile.Experience, ex, store.Profile);
        GoalTitle = $"For {goal.Display().ToLowerInvariant()}";
        GoalTarget = $"{target.Sets} sets · min {target.RepMin}, max {target.RepMax} reps · {target.TargetRir} in reserve · rest {Units.Rest(target.RestSeconds)}";
        GoalTips = TrainingGoals.Tips(goal, ex, target.RepMin, target.RepMax, target.RestSeconds);
        // Before the first time: a starting weight estimated from the user's strength, age and build.
        var (startKg, basis) = sessions.Count == 0 ? progression.StartingWeight(ex, target.RepMin, target.TargetRir) : (0, "");
        StartingWeight = startKg > 0 ? $"Try {units.FormatWithUnit(startKg)} for your first sets, {basis}." : "";
        HasStartingWeight = startKg > 0;

        var best = stats.BestFor(ex.Id);
        BestE1Rm = best == null || ex.IsBodyweight ? "—" : units.FormatWithUnit(best.E1RmKg);
        BestSet = best == null ? "—" : $"{(ex.IsBodyweight && best.WeightKg <= 0 ? "BW" : units.Format(best.WeightKg))} × {best.Reps}";

        var points = stats.E1RmHistory(ex.Id).Select(p => p with { Value = units.ToDisplay(p.Value) }).ToList();
        Chart = new LineChartDrawable(points, Color.FromArgb("#3F7DFF"), v => $"{v:0.#}");

        History = sessions.Take(20).Select(x => new LineItem
        {
            Title = x.s.StartedAt.ToString("ddd, d MMM yyyy"),
            Detail = string.Join("   ", x.e!.Sets.Where(s => !s.IsWarmup).Select(s => $"{units.Format(s.WeightKg)}×{s.Reps}")),
            Value = x.s.Name,
        }).ToList();
        return Task.CompletedTask;
    }

    /// <summary>Stops the video when the page is left (it's loaded again on coming back).</summary>
    public override void OnDisappearing()
    {
        base.OnDisappearing();
        _visible = false;
        Connectivity.Current.ConnectivityChanged -= OnConnectivityChanged;
        UpdateMedia();
    }

    partial void OnShowVideoChanged(bool value) => UpdateMedia();

    void OnConnectivityChanged(object? sender, ConnectivityChangedEventArgs e) => MainThread.BeginInvokeOnMainThread(() =>
    {
        // A new connection gets a new try at the video.
        _videoFailed = false;
        UpdateMedia();
    });

    /// <summary>The player couldn't load the video (its page was cleared before showing): back to the animation.</summary>
    public void VideoLoadFailed()
    {
        _videoFailed = true;
        UpdateMedia();
    }

    /// <summary>
    /// Which of the animation and the video shows. Without a connection the animation does and the video can't be
    /// chosen; when the connection is back the video can be chosen again and loads afresh (its URL went through null).
    /// </summary>
    void UpdateMedia()
    {
        CanShowVideo = HasVideo && !_videoFailed && Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
        HasMedia = HasVideo || HasAnimation;
        HasMediaChoice = HasVideo && HasAnimation;
        if (!CanShowVideo && HasAnimation)
            ShowVideo = false;
        ShowsVideo = HasVideo && (ShowVideo || !HasAnimation);
        ShowsAnimation = HasAnimation && !ShowsVideo;
        VideoOffline = ShowsVideo && !CanShowVideo;
        VideoUrl = _visible && ShowsVideo && CanShowVideo ? _video?.EmbedUrl : null;
    }

    async Task LoadAnimationAsync(string id)
    {
        var source = await AnimationSourceAsync(id);
        if (_animationFor == id)
            Animation = source;
    }

    /// <summary>The exercise's animation, or its picture while this build has no animation for it.</summary>
    static async Task<ImageSource?> AnimationSourceAsync(string id)
    {
        if (ExerciseLibrary.Animation(id) is { } gif && await UnpackAsync(gif) is { } file)
            return ImageSource.FromFile(file);
        return ExerciseLibrary.Thumbnail(id) is { } picture
            ? ImageSource.FromStream(async _ => await FileSystem.OpenAppPackageFileAsync(picture))
            : null;
    }

    /// <summary>
    /// A packaged animation as a file of its own, which animates on every platform (a stream may not): copied out of
    /// the app package once per build.
    /// </summary>
    static async Task<string?> UnpackAsync(string asset)
    {
        var file = Path.Combine(FileSystem.CacheDirectory, "exercise-animations", AppInfo.BuildString, Path.GetFileName(asset));
        if (File.Exists(file))
            return file;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var temp = file + ".tmp";
            await using (var source = await FileSystem.OpenAppPackageFileAsync(asset))
            await using (var target = File.Create(temp))
                await source.CopyToAsync(target);
            File.Move(temp, file, true);
            return file;
        }
        catch (Exception)
        {
            // Not in this build or no room to copy it: the picture shows instead.
            return null;
        }
    }

    /// <summary>The demonstration in the YouTube app; without one, a search for form videos.</summary>
    [RelayCommand]
    Task WatchVideo() => Browser.Default.OpenAsync(
        _video?.WatchUrl ?? $"https://www.youtube.com/results?search_query={Uri.EscapeDataString($"{Name} exercise proper form")}",
        BrowserLaunchMode.External);

    [RelayCommand]
    async Task Delete()
    {
        var ex = store.Data.CustomExercises.FirstOrDefault(e => e.Id == _id);
        if (ex == null || !await dialogs.Confirm("Delete exercise?", $"Delete \"{ex.Name}\"? It will also be removed from your plans.", "Delete"))
            return;
        store.Data.CustomExercises.Remove(ex);
        foreach (var w in store.Data.Plans.SelectMany(p => p.Workouts))
            w.Exercises.RemoveAll(e => e.ExerciseId == ex.Id);
        store.Save();
        await GoBack();
    }
}

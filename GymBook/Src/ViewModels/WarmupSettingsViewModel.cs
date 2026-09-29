using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GymBook.Services;

namespace GymBook.ViewModels;

/// <summary>
/// Editing a set of warm-up settings (see <see cref="WarmupSettings"/>), the same for the profile's (the defaults for
/// every plan) and a plan's own: on or off, each kind of exercise's ramp, the lighter one for muscles already warm, the
/// lightest working weight that gets warm-ups, the rest after them, and a way back to the defaults they came from.
/// </summary>
public partial class WarmupSettingsViewModel : ObservableObject
{
    readonly Func<WarmupSettings> _current;
    readonly Func<WarmupSettings> _defaults;
    readonly Action<WarmupSettings?> _save;
    readonly DialogService _dialogs;
    readonly Units _units;
    bool _loading;

    /// <param name="current">The settings in use now.</param>
    /// <param name="defaults">What a reset goes back to: the scientific defaults, or for a plan the profile's.</param>
    /// <param name="save">Saves changed settings; null resets them (e.g. a plan follows the profile again).</param>
    /// <param name="isOwn">Whether the settings are changed from their defaults, so a reset means something.</param>
    public WarmupSettingsViewModel(Func<WarmupSettings> current, Func<WarmupSettings> defaults, Action<WarmupSettings?> save, Func<bool> isOwn,
        string resetText, string defaultsName, DialogService dialogs, Units units)
    {
        (_current, _defaults, _save, _isOwn, _dialogs, _units) = (current, defaults, save, isOwn, dialogs, units);
        ResetText = resetText;
        _defaultsName = defaultsName;
        Refresh();
    }

    readonly Func<bool> _isOwn;
    readonly string _defaultsName;

    public string ResetText { get; }

    [ObservableProperty] bool enabled;
    [ObservableProperty] string barbellText = "";
    [ObservableProperty] string compoundText = "";
    [ObservableProperty] string isolationText = "";
    [ObservableProperty] string alreadyWarmText = "";
    [ObservableProperty] string minWeightText = "";
    [ObservableProperty] string restText = "";
    /// <summary>Changed from the defaults: the reset row shows, and says where the settings came from.</summary>
    [ObservableProperty] bool isOwn;
    [ObservableProperty] string sourceText = "";

    public void Refresh()
    {
        _loading = true;
        var s = _current();
        Enabled = s.Enabled;
        BarbellText = WarmupSettings.Describe(s.Barbell);
        CompoundText = WarmupSettings.Describe(s.Compound);
        IsolationText = WarmupSettings.Describe(s.Isolation);
        AlreadyWarmText = WarmupSettings.Describe(s.AlreadyWarm);
        MinWeightText = s.MinWorkingKg <= 0 ? "Always" : $"Under {_units.FormatWithUnit(s.MinWorkingKg)}";
        RestText = Units.Rest(s.RestSeconds);
        IsOwn = _isOwn();
        SourceText = IsOwn ? "Changed from " + _defaultsName : "Using " + _defaultsName;
        _loading = false;
    }

    void Change(Action<WarmupSettings> change)
    {
        var s = _current().Clone();
        change(s);
        _save(s);
        Refresh();
    }

    partial void OnEnabledChanged(bool value)
    {
        if (!_loading)
            Change(s => s.Enabled = value);
    }

    // Ramps from none to four sets, lightest first; most set-ups are one of these.
    static readonly WarmupStep[][] Presets =
    [
        [],
        [new(50, 12)],
        [new(70, 4)],
        [new(50, 8), new(75, 4)],
        [new(40, 8), new(60, 5), new(80, 3)],
        [new(40, 8), new(55, 5), new(70, 3), new(85, 1)],
    ];

    const string Custom = "Custom…";

    [RelayCommand] Task EditBarbell() => EditSteps(WarmupKind.Barbell, "Barbell compounds");
    [RelayCommand] Task EditCompound() => EditSteps(WarmupKind.Compound, "Other compound lifts");
    [RelayCommand] Task EditIsolation() => EditSteps(WarmupKind.Isolation, "Isolation exercises");
    [RelayCommand] Task EditAlreadyWarm() => EditSteps(WarmupKind.AlreadyWarm, "Muscles already warm");

    /// <summary>Picks the warm-up sets for one kind of exercise: a common ramp, or typed in.</summary>
    async Task EditSteps(WarmupKind kind, string title)
    {
        var fallback = _defaults().Steps(kind);
        string Option(IReadOnlyList<WarmupStep> steps) =>
            (steps.Count == 0 ? "None" : $"{steps.Count} set{(steps.Count == 1 ? "" : "s")}: {WarmupSettings.Describe(steps)}")
            + (Same(steps, fallback) ? " (default)" : "");
        var presets = Presets.Select(p => (IReadOnlyList<WarmupStep>)p).ToList();
        if (!presets.Any(p => Same(p, fallback)))
            presets.Insert(0, fallback);
        var labels = presets.Select(Option).ToList();
        var choice = await _dialogs.ActionSheet($"{title} · % of the working weight × reps", null, [.. labels, Custom]);
        if (choice == null)
            return;
        List<WarmupStep>? steps;
        if (choice == Custom)
        {
            var text = await _dialogs.Prompt(title, "Each set as percent × reps, lightest first, e.g. 40x8, 60x5, 80x3",
                string.Join(", ", _current().Steps(kind).Select(s => $"{s.Percent}x{s.Reps}")), Keyboard.Text, "Save");
            if (text == null)
                return;
            steps = Parse(text);
            if (steps == null)
            {
                await _dialogs.Alert("Couldn't read that", "Write each set as percent x reps (10–95% and 1–20 reps, up to 5 sets), e.g. 50x8, 75x4. Leave it empty for none.");
                return;
            }
        }
        else
        {
            steps = [.. presets[labels.IndexOf(choice)]];
        }
        Change(s => s.SetSteps(kind, steps));
    }

    static bool Same(IReadOnlyList<WarmupStep> a, IReadOnlyList<WarmupStep> b) => a.SequenceEqual(b);

    static readonly Regex Step = new(@"(\d+)\s*%?\s*[x×*]\s*(\d+)", RegexOptions.IgnoreCase);

    /// <summary>"40x8, 60x5" into warm-up sets, lightest first; null when it doesn't make sense.</summary>
    static List<WarmupStep>? Parse(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Equals("none", StringComparison.OrdinalIgnoreCase))
            return [];
        var matches = Step.Matches(text);
        if (matches.Count is 0 or > 5)
            return null;
        var steps = matches.Select(m => new WarmupStep(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value))).ToList();
        if (steps.Any(s => s.Percent is < 10 or > 95 || s.Reps is < 1 or > 20))
            return null;
        return [.. steps.OrderBy(s => s.Percent)];
    }

    [RelayCommand]
    async Task EditMinWeight()
    {
        double[] weights = [0, 10, 20, 30, 40, 60];
        var fallback = _defaults().MinWorkingKg;
        string Option(double kg) => (kg <= 0 ? "Always warm up" : $"Skip under {_units.FormatWithUnit(kg)}") + (Math.Abs(kg - fallback) < 0.01 ? " (default)" : "");
        var labels = weights.Select(Option).ToList();
        var choice = await _dialogs.ActionSheet("Skip warm-ups when the working weight is light", null, [.. labels]);
        var index = choice == null ? -1 : labels.IndexOf(choice);
        if (index >= 0)
            Change(s => s.MinWorkingKg = weights[index]);
    }

    [RelayCommand]
    async Task EditRest()
    {
        int[] times = [30, 45, 60, 75, 90, 120, 150];
        var fallback = _defaults().RestSeconds;
        var labels = times.Select(t => Units.Rest(t) + (t == fallback ? " (default)" : "")).ToList();
        var choice = await _dialogs.ActionSheet("Rest after a warm-up set", null, [.. labels]);
        var index = choice == null ? -1 : labels.IndexOf(choice);
        if (index >= 0)
            Change(s => s.RestSeconds = times[index]);
    }

    [RelayCommand]
    async Task Reset()
    {
        if (!await _dialogs.Confirm(ResetText + "?", $"Every warm-up setting goes back to {_defaultsName}.", "Reset"))
            return;
        _save(null);
        Refresh();
    }
}

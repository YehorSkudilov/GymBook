using System.Text.Json;
using GymBook.Models;

namespace GymBook.Services;

/// <summary>One warm-up set: a share of the first working set's weight, for a number of reps.</summary>
public record WarmupStep(int Percent, int Reps)
{
    public override string ToString() => $"{Percent}%×{Reps}";
}

/// <summary>The kinds of exercise that warm up differently.</summary>
public enum WarmupKind { Barbell, Compound, Isolation, AlreadyWarm }

/// <summary>
/// How warm-up sets are added to a workout: on or off, the ramp for each kind of exercise, a lighter one for muscles an
/// earlier exercise already worked, the lightest working weight still worth warming up for, and the rest after them.
/// The profile's are the defaults for every plan; a plan can have its own (<see cref="WorkoutPlan.Warmups"/>).
/// <para>
/// The defaults follow the usual evidence on warming up for strength work: a few ramping sets of falling reps before
/// heavy barbell lifts (about 40, 60 and 80% of the working weight), fewer for other compounds, one light set for
/// isolation work, and a single feeler set once the muscle is already warm, so the warm-up primes without tiring.
/// </para>
/// </summary>
public class WarmupSettings
{
    public bool Enabled { get; set; } = true;
    public int RestSeconds { get; set; } = 60;
    /// <summary>Squats, bench, deadlifts and other barbell compounds.</summary>
    public List<WarmupStep> Barbell { get; set; } = [new(40, 8), new(60, 5), new(80, 3)];
    /// <summary>Dumbbell, machine, cable and other compound lifts.</summary>
    public List<WarmupStep> Compound { get; set; } = [new(50, 8), new(75, 4)];
    /// <summary>Curls, raises, extensions and other single-joint moves.</summary>
    public List<WarmupStep> Isolation { get; set; } = [new(50, 12)];
    /// <summary>Any exercise whose main muscle an earlier exercise in the workout already worked.</summary>
    public List<WarmupStep> AlreadyWarm { get; set; } = [new(70, 4)];
    /// <summary>No warm-ups when the first working set is lighter than this.</summary>
    public double MinWorkingKg { get; set; } = 20;

    public static WarmupSettings Defaults() => new();

    static readonly JsonSerializerOptions Json = new();

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    static WarmupSettings? Parse(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;
        try
        {
            return JsonSerializer.Deserialize<WarmupSettings>(json, Json);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public WarmupSettings Clone() => Parse(ToJson()) ?? new();

    /// <summary>The profile's settings: the defaults for every plan.</summary>
    public static WarmupSettings Global(UserProfile profile)
    {
        var s = Parse(profile.Warmups) ?? Defaults();
        s.Enabled = profile.WarmupSuggestions;
        s.RestSeconds = profile.WarmupRestSeconds;
        return s;
    }

    /// <summary>Saves <paramref name="settings"/> as the profile's (null: back to the defaults).</summary>
    public static void SaveGlobal(UserProfile profile, WarmupSettings? settings)
    {
        profile.Warmups = settings?.ToJson();
        profile.WarmupSuggestions = settings?.Enabled ?? true;
        profile.WarmupRestSeconds = settings?.RestSeconds ?? Defaults().RestSeconds;
    }

    /// <summary>The settings a workout of <paramref name="plan"/> uses: its own, or the profile's.</summary>
    public static WarmupSettings For(WorkoutPlan? plan, UserProfile profile) => Parse(plan?.Warmups) ?? Global(profile);

    public static WarmupKind KindOf(Exercise ex, bool alreadyWarm) =>
        alreadyWarm ? WarmupKind.AlreadyWarm
        : ex.Mechanic == Mechanic.Isolation ? WarmupKind.Isolation
        : ex.Equipment == Equipment.Barbell ? WarmupKind.Barbell
        : WarmupKind.Compound;

    public List<WarmupStep> Steps(WarmupKind kind) => kind switch
    {
        WarmupKind.Barbell => Barbell,
        WarmupKind.Compound => Compound,
        WarmupKind.Isolation => Isolation,
        _ => AlreadyWarm,
    };

    public void SetSteps(WarmupKind kind, List<WarmupStep> steps)
    {
        switch (kind)
        {
            case WarmupKind.Barbell: Barbell = steps; break;
            case WarmupKind.Compound: Compound = steps; break;
            case WarmupKind.Isolation: Isolation = steps; break;
            default: AlreadyWarm = steps; break;
        }
    }

    public static string Describe(IReadOnlyList<WarmupStep> steps) =>
        steps.Count == 0 ? "None" : string.Join(" · ", steps);

    /// <summary>Whether these are the same settings (e.g. a plan's own against the profile's).</summary>
    public bool SameAs(WarmupSettings other) => ToJson() == other.ToJson();
}

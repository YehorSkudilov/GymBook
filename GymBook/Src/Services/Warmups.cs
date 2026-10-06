using System.Text.Json;
using System.Text.RegularExpressions;
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
/// Warm-up sets belong to a plan's exercises (<see cref="PlanExercise.Warmups"/>): each gets its own when the plan is made
/// (or the exercise added to it), and after that only changing them on the plan, or saving a workout's changes to it,
/// changes them. There are no warm-up settings that put them back.
/// <para>
/// What a new exercise gets follows the usual evidence on warming up for strength work: a few ramping sets of falling
/// reps before heavy barbell lifts (about 40, 60 and 80% of the working weight), fewer for other compounds, one light
/// set for isolation work, and a single feeler set once the muscle is already warm, so the warm-up primes without tiring.
/// </para>
/// </summary>
public static class Warmups
{
    /// <summary>No warm-ups when the first working set is lighter than this.</summary>
    public const double MinWorkingKg = 20;

    static readonly WarmupStep[] Barbell = [new(40, 8), new(60, 5), new(80, 3)];
    static readonly WarmupStep[] Compound = [new(50, 8), new(75, 4)];
    static readonly WarmupStep[] Isolation = [new(50, 12)];
    static readonly WarmupStep[] AlreadyWarm = [new(70, 4)];

    /// <summary>Ramps from none to four sets, lightest first, to pick from for an exercise.</summary>
    public static readonly WarmupStep[][] Presets =
    [
        [],
        [new(50, 12)],
        [new(70, 4)],
        [new(50, 8), new(75, 4)],
        [new(40, 8), new(60, 5), new(80, 3)],
        [new(40, 8), new(55, 5), new(70, 3), new(85, 1)],
    ];

    public static WarmupKind KindOf(Exercise ex, bool alreadyWarm) =>
        alreadyWarm ? WarmupKind.AlreadyWarm
        : ex.Mechanic == Mechanic.Isolation ? WarmupKind.Isolation
        : ex.Equipment == Equipment.Barbell ? WarmupKind.Barbell
        : WarmupKind.Compound;

    /// <summary>
    /// The warm-ups an exercise gets when it's put in a plan (or added to a workout on the fly): by its kind, lighter
    /// once an earlier exercise worked the same muscle; none for bodyweight moves.
    /// </summary>
    public static List<WarmupStep> Recommended(Exercise ex, bool alreadyWarm) =>
        ex.IsBodyweight ? [] : [.. KindOf(ex, alreadyWarm) switch
        {
            WarmupKind.Barbell => Barbell,
            WarmupKind.Compound => Compound,
            WarmupKind.Isolation => Isolation,
            _ => AlreadyWarm,
        }];

    /// <summary>
    /// The warm-ups before <paramref name="ex"/>: the plan exercise's own, or what it would get when there's none yet
    /// (an exercise added on the fly, or a plan synced in before they were decided; see <see cref="Decide"/>).
    /// </summary>
    public static IReadOnlyList<WarmupStep> Of(PlanExercise? pe, Exercise ex, bool alreadyWarm) =>
        ParseSteps(pe?.Warmups) ?? Recommended(ex, alreadyWarm);

    /// <summary>
    /// Gives every exercise of <paramref name="plan"/> without warm-ups yet the recommended ones, by its place in its
    /// workout. Saving is up to the caller.
    /// </summary>
    /// <returns>Whether anything was changed.</returns>
    public static bool Decide(WorkoutPlan plan, Func<string, Exercise?> exercise) =>
        Decide(plan, exercise, Recommended);

    static bool Decide(WorkoutPlan plan, Func<string, Exercise?> exercise, Func<Exercise, bool, IReadOnlyList<WarmupStep>> steps)
    {
        var changed = false;
        foreach (var workout in plan.Workouts)
        {
            var worked = new HashSet<MuscleGroup>();
            foreach (var pe in workout.Exercises)
            {
                if (exercise(pe.ExerciseId) is not { } ex)
                    continue;
                if (pe.Warmups == null)
                {
                    pe.Warmups = FormatSteps(steps(ex, worked.Contains(ex.PrimaryMuscle)));
                    changed = true;
                }
                worked.Add(ex.PrimaryMuscle);
            }
        }
        return changed;
    }

    /// <summary>
    /// The warm-up sets of <paramref name="sets"/> (a workout's exercise) as steps of its first working set's weight;
    /// null when that has no weight to take shares of.
    /// </summary>
    public static List<WarmupStep>? FromSets(IReadOnlyList<SetEntry> sets)
    {
        var working = sets.FirstOrDefault(s => !s.IsWarmup)?.WeightKg ?? 0;
        if (working <= 0)
            return null;
        return [.. sets.Where(s => s.IsWarmup)
            .Select(s => new WarmupStep(Math.Clamp((int)Math.Round(s.WeightKg / working * 20) * 5, 10, 95), Math.Clamp(s.Reps, 1, 20)))
            .Take(5)
            .OrderBy(s => s.Percent)];
    }

    static readonly Regex StepPattern = new(@"(\d+)\s*%?\s*[x×*]\s*(\d+)", RegexOptions.IgnoreCase);

    /// <summary>
    /// "40x8, 60x5" into warm-up sets, lightest first ("" or "none" for none); null when it doesn't make sense: 10–95%
    /// and 1–20 reps, up to 5 sets.
    /// </summary>
    public static List<WarmupStep>? ParseSteps(string? text)
    {
        if (text == null)
            return null;
        if (string.IsNullOrWhiteSpace(text) || text.Trim().Equals("none", StringComparison.OrdinalIgnoreCase))
            return [];
        var matches = StepPattern.Matches(text);
        if (matches.Count is 0 or > 5)
            return null;
        var steps = matches.Select(m => new WarmupStep(int.Parse(m.Groups[1].Value), int.Parse(m.Groups[2].Value))).ToList();
        if (steps.Any(s => s.Percent is < 10 or > 95 || s.Reps is < 1 or > 20))
            return null;
        return [.. steps.OrderBy(s => s.Percent)];
    }

    /// <summary>Warm-up sets as they're typed and stored: "40x8, 60x5"; empty for none.</summary>
    public static string FormatSteps(IEnumerable<WarmupStep> steps) => string.Join(", ", steps.Select(s => $"{s.Percent}x{s.Reps}"));

    public static string Describe(IReadOnlyList<WarmupStep> steps) =>
        steps.Count == 0 ? "None" : string.Join(" · ", steps);

    /// <summary>
    /// The warm-up settings there used to be (the profile's, and a plan's own) become each plan exercise's own warm-ups,
    /// once: what they'd have given it, none where they were off. Then they're gone. Saving is up to the caller.
    /// </summary>
    /// <returns>Whether anything was changed.</returns>
    public static bool MoveSettingsIntoPlans(IEnumerable<WorkoutPlan> plans, UserProfile profile, Func<string, Exercise?> exercise)
    {
        var all = plans.ToList();
        if (profile.Warmups == null && profile.WarmupSuggestions && all.All(p => p.Warmups == null))
            return false;
        var global = Legacy.Parse(profile.Warmups) ?? new Legacy();
        global.Enabled = profile.WarmupSuggestions;
        foreach (var plan in all)
        {
            var settings = Legacy.Parse(plan.Warmups) ?? global;
            Decide(plan, exercise, (ex, warm) => !settings.Enabled || ex.IsBodyweight ? [] : settings.Steps(KindOf(ex, warm)));
            plan.Warmups = null;
        }
        (profile.Warmups, profile.WarmupSuggestions) = (null, true);
        return true;
    }

    /// <summary>The warm-up settings as they were stored (JSON on the profile and on plans), read only to move them.</summary>
    sealed class Legacy
    {
        public bool Enabled { get; set; } = true;
        public List<WarmupStep> Barbell { get; set; } = [.. Warmups.Barbell];
        public List<WarmupStep> Compound { get; set; } = [.. Warmups.Compound];
        public List<WarmupStep> Isolation { get; set; } = [.. Warmups.Isolation];
        public List<WarmupStep> AlreadyWarm { get; set; } = [.. Warmups.AlreadyWarm];

        public List<WarmupStep> Steps(WarmupKind kind) => kind switch
        {
            WarmupKind.Barbell => Barbell,
            WarmupKind.Compound => Compound,
            WarmupKind.Isolation => Isolation,
            _ => AlreadyWarm,
        };

        public static Legacy? Parse(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                return JsonSerializer.Deserialize<Legacy>(json);
            }
            catch (JsonException)
            {
                return null;
            }
        }
    }
}

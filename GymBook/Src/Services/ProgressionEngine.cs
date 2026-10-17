using GymBook.Models;

namespace GymBook.Services;

public record Suggestion(List<SetEntry> Sets, string Note);

/// <summary>
/// Double progression driven by reps in reserve: add reps inside the range until every set reaches the top,
/// then add weight and drop back to the bottom of the range; fall below the bottom and the weight comes down.
/// Weights are predicted from estimated one-rep maxes, and scaled back after time away from training. A workout's sets
/// start as exactly what was done last time; the progression is given as advice (<see cref="Suggestion.Note"/>).
/// </summary>
public class ProgressionEngine(DataStore store, Units units)
{
    public SessionExercise? LastPerformance(string exerciseId, string? excludeSessionId = null) =>
        LastSession(exerciseId, excludeSessionId)?.Exercise;

    /// <summary>The most recent finished session with working sets of <paramref name="exerciseId"/>, and that exercise in it.</summary>
    public (WorkoutSession Session, SessionExercise Exercise)? LastSession(string exerciseId, string? excludeSessionId = null) =>
        store.History
            .Where(s => s.Id != excludeSessionId)
            .Select(s => (s, e: s.Exercises.FirstOrDefault(e => e.ExerciseId == exerciseId && e.Sets.Any(x => x.IsCompleted && !x.IsWarmup))))
            .Where(x => x.e != null)
            .Select(x => ((WorkoutSession, SessionExercise)?)(x.s, x.e!))
            .FirstOrDefault();

    /// <summary>
    /// The sets to start <paramref name="ex"/> with, and advice on progressing. <paramref name="unit"/> is what the sets
    /// count, as kept with them (see TrainingGoals.UnitOf): seconds, meters or steps progress by the weight's step, not by
    /// estimated maxes.
    /// </summary>
    public Suggestion Suggest(Exercise ex, int sets, int repMin, int repMax, int targetRir, RepUnit? unit = null)
    {
        var result = new List<SetEntry>();
        var last = LastSession(ex.Id);
        var counted = TrainingGoals.UnitOf(ex, unit);
        if (counted != RepUnit.Reps)
            return SuggestCounted(ex, sets, repMin, repMax, counted, last);

        if (last == null)
        {
            var (start, basis) = StartingWeight(ex, repMin, targetRir);
            for (var i = 0; i < sets; i++)
                result.Add(new SetEntry { WeightKg = start, Reps = repMin });
            var note = ex.IsBodyweight || start <= 0
                ? $"First time: find a rep count that leaves {targetRir} reps in reserve."
                : $"First time: start around {units.FormatWithUnit(start)} ({basis}) and adjust so you have {targetRir} reps left in the tank.";
            return new Suggestion(result, note);
        }

        var work = last.Value.Exercise.Sets.Where(s => s.IsCompleted && !s.IsWarmup).ToList();
        var loaded = !ex.IsBodyweight || work.Max(s => s.WeightKg) > 0;

        // The sets start as exactly what was done last time, set for set (the last one repeated if there are more now);
        // what to change is only advice, in the note, for the user to act on.
        foreach (var p in Expand(work, sets))
            result.Add(new SetEntry { WeightKg = p.WeightKg, Reps = p.Reps });

        // Time away: suggest coming back lighter and rebuilding from the bottom of the range.
        if (Detraining(last.Value.Session.StartedAt) is { Reduction: > 0 } away && loaded)
        {
            var lighter = units.Round(work[0].WeightKg * (1 - away.Reduction), ex);
            return new Suggestion(result,
                $"Welcome back: {away.Reason}. Consider going about {away.Reduction:P0} lighter ({units.FormatWithUnit(lighter)}) for {repMin} reps, and build back up over the next few sessions.");
        }

        var avgRir = work.Where(s => s.Rir.HasValue).Select(s => (double)s.Rir!.Value).DefaultIfEmpty(targetRir).Average();
        var allAtTop = work.All(s => s.Reps >= repMax);
        var belowMin = work.Count(s => s.Reps < repMin);
        var bestE1Rm = work.Max(s => E1Rm(s.WeightKg, s.Reps, s.Rir ?? targetRir));
        string summary;

        if (allAtTop && avgRir >= targetRir - 0.5)
        {
            if (!loaded)
            {
                summary = "You hit the top of the range last time. Push for one more rep per set.";
            }
            else
            {
                // The weight that puts you back at the bottom of the range, at least one step up.
                var predicted = WeightFor(bestE1Rm, repMin, targetRir, ex);
                var top = work[0].WeightKg;
                var next = Math.Clamp(predicted, units.Step(top, ex, 1), Math.Max(units.Step(top, ex, 1), units.Round(top * 1.1, ex)));
                summary = $"Try {units.FormatWithUnit(next)}. You hit {repMax}+ reps on every set last time, so this should give you about {repMin}.";
            }
        }
        else if (belowMin * 2 >= work.Count && loaded)
        {
            // Under the minimum: the weight that gets you back into the range.
            var fewest = work.Min(s => s.Reps);
            var next = Math.Min(WeightFor(bestE1Rm, repMin, targetRir, ex), units.Step(work[0].WeightKg, ex, -1));
            summary = $"Consider {units.FormatWithUnit(next)}. You got {fewest} reps last time, under your minimum of {repMin}; that weight should put you back in the range.";
        }
        else
        {
            summary = "Same weight as last time. Aim for one more rep per set.";
        }

        return new Suggestion(result, summary);
    }

    /// <summary>
    /// <see cref="Suggest"/> for sets counted in seconds, meters or steps: last time's sets again, with the weight a step
    /// up once every set reached the top of the range, or a step down when most fell short of the bottom.
    /// </summary>
    Suggestion SuggestCounted(Exercise ex, int sets, int repMin, int repMax, RepUnit unit, (WorkoutSession Session, SessionExercise Exercise)? last)
    {
        var range = unit.Range(repMin, repMax);
        var result = new List<SetEntry>();
        // Last time counted another way: its weights are still a start, its counts aren't.
        var sameUnit = last != null && TrainingGoals.UnitOf(ex, last.Value.Exercise.Unit) == unit;
        if (last == null)
        {
            for (var i = 0; i < sets; i++)
                result.Add(new SetEntry { Reps = repMin });
            return new Suggestion(result, $"First time: pick a weight you can hold good posture with for {range}.");
        }

        var work = last.Value.Exercise.Sets.Where(s => s.IsCompleted && !s.IsWarmup).ToList();
        foreach (var p in Expand(work, sets))
            result.Add(new SetEntry { WeightKg = p.WeightKg, Reps = sameUnit ? p.Reps : repMin });
        var top = work.Max(s => s.WeightKg);
        if (!sameUnit || top <= 0)
            return new Suggestion(result, $"Aim for {range} a set.");
        if (work.All(s => s.Reps >= repMax))
            return new Suggestion(result,
                $"Try {units.FormatWithUnit(units.Step(top, ex, 1))}. You went {repMax}+ {unit.Display()} on every set last time.");
        if (work.Count(s => s.Reps < repMin) * 2 >= work.Count)
            return new Suggestion(result,
                $"Consider {units.FormatWithUnit(units.Step(top, ex, -1))}. You got {unit.Count(work.Min(s => s.Reps))} last time, under your minimum of {repMin}.");
        return new Suggestion(result, $"Same weight as last time. Aim for a little more on each set, up to {unit.Count(repMax)}.");
    }

    /// <summary>
    /// Advice after a working set, with the weight it suggests for the next one: lower after falling short of the minimum,
    /// higher after going past the maximum with reps to spare. Null when the set was in range. <paramref name="unit"/> is
    /// what the sets count, as kept with them; in seconds, meters or steps the weight moves a step at a time.
    /// </summary>
    public (string Advice, double? NextKg)? AfterSet(Exercise ex, SetEntry set, int repMin, int repMax, int targetRir, RepUnit? unit = null)
    {
        if (set.IsWarmup || set.Reps <= 0)
            return null;
        var loaded = set.WeightKg > 0;
        var counted = TrainingGoals.UnitOf(ex, unit);
        if (counted != RepUnit.Reps)
        {
            var done = counted.Count(set.Reps);
            if (set.Reps < repMin)
                return loaded
                    ? ($"{done} is under your {repMin} minimum. Consider {units.FormatWithUnit(units.Step(set.WeightKg, ex, -1))} for the next set.", units.Step(set.WeightKg, ex, -1))
                    : ($"{done} is under your {repMin} minimum. Rest a little longer, or use an easier variation.", null);
            if (set.Reps > repMax)
                return loaded
                    ? ($"{done} is over your {repMax} maximum. Consider {units.FormatWithUnit(units.Step(set.WeightKg, ex, 1))} for the next set.", units.Step(set.WeightKg, ex, 1))
                    : ($"{done} is over your {repMax} maximum. Add weight or a harder variation next time.", null);
            return null;
        }
        if (set.Reps < repMin)
        {
            if (!loaded)
                return ($"{set.Reps} reps is under your {repMin} minimum. Rest a little longer, or use an easier variation.", null);
            var next = Math.Min(WeightFor(E1Rm(set.WeightKg, set.Reps, set.Rir ?? 0), repMin, targetRir, ex), units.Step(set.WeightKg, ex, -1));
            return ($"{set.Reps} reps is under your {repMin} minimum. Consider {units.FormatWithUnit(next)} for the next set.", next);
        }
        if (set.Reps > repMax && (set.Rir ?? targetRir) >= targetRir)
        {
            if (!loaded)
                return ($"{set.Reps} reps is over your {repMax} maximum. Add weight or a harder variation next time.", null);
            var mid = (repMin + repMax) / 2;
            var next = Math.Max(WeightFor(E1Rm(set.WeightKg, set.Reps, set.Rir ?? targetRir), mid, targetRir, ex), units.Step(set.WeightKg, ex, 1));
            return ($"{set.Reps} reps is over your {repMax} maximum. Consider {units.FormatWithUnit(next)} for the next set.", next);
        }
        return null;
    }

    static IEnumerable<SetEntry> Expand(List<SetEntry> work, int sets)
    {
        for (var i = 0; i < sets; i++)
            yield return work[Math.Min(i, work.Count - 1)];
    }

    /// <summary>The weight for <paramref name="reps"/> reps with <paramref name="rir"/> in reserve, from an estimated one-rep max.</summary>
    double WeightFor(double e1Rm, int reps, int rir, Exercise ex) => Math.Max(0, units.Round(e1Rm / (1 + (reps + rir) / 30.0), ex));

    /// <summary>
    /// How much lighter to go after time away. The gap since any workout matters most; a lift that hasn't been done
    /// for a while loses a little even while training otherwise continues.
    /// </summary>
    public (double Reduction, string Reason)? Detraining(DateTime lastDoneAt)
    {
        var now = DateTime.Now;
        var lastWorkout = store.History.FirstOrDefault()?.StartedAt ?? lastDoneAt;
        var away = (now - lastWorkout).TotalDays;
        var sinceLift = (now - lastDoneAt).TotalDays;
        var reduction = away switch
        {
            <= 10 => 0,
            <= 21 => 0.1,
            <= 42 => 0.15,
            <= 90 => 0.25,
            _ => 0.35,
        };
        if (reduction > 0)
            return (reduction, $"it's been {Weeks(away)} since your last workout");
        if (sinceLift > 28)
            return (sinceLift > 60 ? 0.1 : 0.05, $"you haven't done this exercise for {Weeks(sinceLift)}");
        return null;
    }

    /// <summary>Days since the last workout, when long enough that weights are being scaled back.</summary>
    public int? DaysAway()
    {
        var last = store.History.FirstOrDefault()?.StartedAt;
        var days = last == null ? 0 : (int)(DateTime.Now - last.Value).TotalDays;
        return days > 10 ? days : null;
    }

    static string Weeks(double days) => days < 14 ? $"{(int)days} days" : $"{(int)Math.Round(days / 7)} weeks";

    /// <summary>
    /// A first working weight for <paramref name="ex"/>: the typical intermediate's weight for it, scaled by how the
    /// user's own lifts compare to typical (same muscle group first), or by training age, and by age and lean mass.
    /// Kept a little conservative, since the movement is new.
    /// </summary>
    public (double Kg, string Basis) StartingWeight(Exercise ex, int reps = 10, int rir = 2)
    {
        var standard = StandardE1Rm(ex);
        if (standard <= 0)
            return (0, "");
        var (level, basis) = StrengthLevel(ex.PrimaryMuscle);
        var kg = WeightFor(standard * level * AgeFactor() * 0.9, reps, rir, ex);
        if (ex.Equipment == Equipment.Barbell)
            kg = Math.Max(20, kg);
        return (kg, basis);
    }

    /// <summary>
    /// How strong the user is relative to a typical intermediate lifter (1.0), from their best recent lift on each exercise
    /// against that exercise's typical e1RM. Lifts of the same muscle group count first; without any history,
    /// time spent training (or the stated experience) decides.
    /// </summary>
    public (double Level, string Basis) StrengthLevel(MuscleGroup? muscle = null)
    {
        var since = DateTime.Now.AddDays(-120);
        var ratios = store.History.Where(s => s.StartedAt >= since)
            .SelectMany(s => s.Exercises)
            // Sets counted in seconds or meters (carries) give no 1RM.
            .Where(e => TrainingGoals.UnitOf(store.GetExercise(e.ExerciseId), e.Unit) == RepUnit.Reps)
            .GroupBy(e => e.ExerciseId)
            .Select(g => (ex: store.GetExercise(g.Key), best: g.SelectMany(e => e.Sets).Where(s => s.IsCompleted && !s.IsWarmup && s.WeightKg > 0)
                .Select(s => E1Rm(s.WeightKg, s.Reps, s.Rir)).DefaultIfEmpty(0).Max()))
            .Where(x => x.ex != null && x.best > 0 && StandardE1Rm(x.ex) > 0)
            .Select(x => (muscle: x.ex!.PrimaryMuscle, ratio: x.best / (StandardE1Rm(x.ex) * AgeFactor())))
            .ToList();

        var sameMuscle = ratios.Where(r => r.muscle == muscle).Select(r => r.ratio).ToList();
        if (sameMuscle.Count > 0)
            return (Math.Clamp(Median(sameMuscle), 0.3, 2.5), "based on your other lifts for this muscle");
        if (ratios.Count > 0)
            return (Math.Clamp(Median(ratios.Select(r => r.ratio).ToList()), 0.3, 2.5), "based on your other lifts");

        var profile = store.Profile;
        if (profile.TrainingSince is { } start)
        {
            var years = (DateTime.Now - start).TotalDays / 365;
            var level = years switch { < 0.25 => 0.6, < 1 => 0.75, < 2 => 0.95, < 4 => 1.1, _ => 1.25 };
            return (level, "based on your body weight and training history");
        }
        return (profile.Experience switch { Experience.Beginner => 0.7, Experience.Intermediate => 1.0, _ => 1.3 },
            "based on your body weight and experience");
    }

    static double Median(List<double> values)
    {
        values.Sort();
        var mid = values.Count / 2;
        return values.Count % 2 == 1 ? values[mid] : (values[mid - 1] + values[mid]) / 2;
    }

    /// <summary>Strength peaks in the 20s and 30s and declines gradually after 40.</summary>
    double AgeFactor()
    {
        if (store.Profile.BirthYear is not { } year)
            return 1;
        var age = DateTime.Now.Year - year;
        return age switch { < 16 => 0.75, < 18 => 0.85, < 40 => 1, < 50 => 0.95, < 60 => 0.88, < 70 => 0.8, _ => 0.7 };
    }

    /// <summary>
    /// A typical intermediate lifter's e1RM for <paramref name="ex"/>: the same standard the ranks go by
    /// (<see cref="Ranks.StandardRatio"/>, by equipment, movement type, muscle and gender) times body weight. Lean mass
    /// stands in for body weight when body fat is known.
    /// </summary>
    double StandardE1Rm(Exercise ex)
    {
        var profile = store.Profile;
        var bw = profile.BodyWeightKg > 0 ? profile.BodyWeightKg : 75;
        // Normalised to a typical 15% body fat, so leaner lifters are expected to lift more for their weight.
        if (profile.BodyFatPercent is { } bf and > 0)
            bw = bw * (1 - bf / 100) / 0.85;
        return bw * Ranks.StandardRatio(ex.Equipment, ex.Mechanic, ex.PrimaryMuscle, profile.Sex);
    }

    /// <summary>
    /// Warm-up sets <paramref name="steps"/> before a first working set of <paramref name="workingKg"/> (see
    /// <see cref="Services.Warmups.Of"/>). None for bodyweight moves, or when the working weight is too light to need them
    /// (<see cref="Services.Warmups.MinWorkingKg"/>).
    /// </summary>
    public List<SetEntry> Warmups(Exercise ex, double workingKg, IReadOnlyList<WarmupStep> steps)
    {
        var list = new List<SetEntry>();
        if (ex.IsBodyweight || workingKg <= 0 || workingKg < Services.Warmups.MinWorkingKg)
            return list;

        // Never lighter than the empty bar.
        var floor = ex.Equipment == Equipment.Barbell ? 20 : 0;
        foreach (var step in steps)
        {
            var (pct, reps) = (step.Percent / 100.0, step.Reps);
            var w = Math.Max(floor, units.Round(workingKg * pct, ex));
            if (w >= workingKg || list.Any(s => Math.Abs(s.WeightKg - w) < 0.01))
                continue;
            list.Add(new SetEntry { WeightKg = w, Reps = reps, IsWarmup = true });
        }
        return list;
    }

    /// <summary>Estimated one-rep max (Epley), counting reps in reserve as reps you could have done.</summary>
    public static double E1Rm(double kg, int reps, int? rir) => Ranks.E1Rm(kg, reps, rir);
}

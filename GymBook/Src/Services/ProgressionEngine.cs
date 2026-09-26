using GymBook.Models;

namespace GymBook.Services;

public record Suggestion(List<SetEntry> Sets, string Note);

/// <summary>
/// Double progression driven by reps in reserve: add reps inside the range until every set reaches the top,
/// then add weight and drop back to the bottom of the range.
/// </summary>
public class ProgressionEngine(DataStore store, Units units)
{
    public SessionExercise? LastPerformance(string exerciseId, string? excludeSessionId = null) =>
        store.History
            .Where(s => s.Id != excludeSessionId)
            .Select(s => s.Exercises.FirstOrDefault(e => e.ExerciseId == exerciseId && e.Sets.Any(x => x.IsCompleted && !x.IsWarmup)))
            .FirstOrDefault(e => e != null);

    public Suggestion Suggest(Exercise ex, int sets, int repMin, int repMax, int targetRir)
    {
        var result = new List<SetEntry>();
        var last = LastPerformance(ex.Id);

        if (last == null)
        {
            var start = StartingWeight(ex);
            for (var i = 0; i < sets; i++)
                result.Add(new SetEntry { WeightKg = start, Reps = repMin });
            var note = ex.IsBodyweight
                ? "First time: find a rep count that leaves 2–3 reps in reserve."
                : $"First time: start around {units.FormatWithUnit(start)} and adjust so you have {targetRir} reps left in the tank.";
            return new Suggestion(result, note);
        }

        var work = last.Sets.Where(s => s.IsCompleted && !s.IsWarmup).ToList();
        var avgRir = work.Where(s => s.Rir.HasValue).Select(s => (double)s.Rir!.Value).DefaultIfEmpty(targetRir).Average();
        var allAtTop = work.All(s => s.Reps >= repMax);
        var mostlyFailed = work.Count(s => s.Reps < repMin) * 2 > work.Count && avgRir <= 0.5;
        var steps = avgRir >= targetRir + 2 ? 2 : 1;
        string summary;

        if (allAtTop && avgRir >= targetRir - 0.5)
        {
            if (ex.IsBodyweight && work.Max(s => s.WeightKg) <= 0)
            {
                summary = "You hit the top of the range. Push for one more rep per set.";
                foreach (var p in Expand(work, sets))
                    result.Add(new SetEntry { WeightKg = 0, Reps = p.Reps + 1 });
            }
            else
            {
                var up = units.Step(work[0].WeightKg, ex, steps) - work[0].WeightKg;
                summary = $"Increase weight by {units.FormatWithUnit(up)}. You hit {repMax} reps on every set last time.";
                foreach (var p in Expand(work, sets))
                    result.Add(new SetEntry { WeightKg = units.Step(p.WeightKg, ex, steps), Reps = repMin });
            }
        }
        else if (mostlyFailed && !ex.IsBodyweight)
        {
            summary = "Reduce the weight slightly. Most sets fell short of the rep range last time.";
            foreach (var p in Expand(work, sets))
                result.Add(new SetEntry { WeightKg = units.Step(p.WeightKg, ex, -1), Reps = repMin });
        }
        else
        {
            summary = "Same weight. Aim for one more rep per set than last time.";
            foreach (var p in Expand(work, sets))
                result.Add(new SetEntry { WeightKg = p.WeightKg, Reps = Math.Clamp(p.Reps + 1, repMin, Math.Max(repMax, p.Reps)) });
        }

        return new Suggestion(result, summary);
    }

    static IEnumerable<SetEntry> Expand(List<SetEntry> work, int sets)
    {
        for (var i = 0; i < sets; i++)
            yield return work[Math.Min(i, work.Count - 1)];
    }

    /// <summary>A conservative guess for a first session, scaled by body weight and experience.</summary>
    public double StartingWeight(Exercise ex)
    {
        var profile = store.Profile;
        var bw = profile.BodyWeightKg > 0 ? profile.BodyWeightKg : 75;
        var factor = (ex.Equipment, ex.Mechanic) switch
        {
            (Equipment.Barbell, Mechanic.Compound) => 0.55,
            (Equipment.Barbell, _) => 0.3,
            (Equipment.EzBar, _) => 0.3,
            (Equipment.Dumbbell, Mechanic.Compound) => 0.16,
            (Equipment.Dumbbell, _) => 0.08,
            (Equipment.Machine, Mechanic.Compound) => 0.6,
            (Equipment.Machine, _) => 0.35,
            (Equipment.Cable, Mechanic.Compound) => 0.45,
            (Equipment.Cable, _) => 0.15,
            (Equipment.Kettlebell, _) => 0.16,
            _ => 0,
        };
        var level = profile.Experience switch
        {
            Experience.Beginner => 0.7,
            Experience.Intermediate => 1.0,
            _ => 1.3,
        };
        var kg = bw * factor * level;
        if (ex.Equipment == Equipment.Barbell)
            kg = Math.Max(20, kg);
        return kg <= 0 ? 0 : units.Round(kg, ex);
    }

    /// <summary>Ramp-up sets for compound lifts: 50%, 70% and 85% of the working weight.</summary>
    public List<SetEntry> Warmups(Exercise ex, double workingKg)
    {
        var list = new List<SetEntry>();
        if (ex.Mechanic != Mechanic.Compound || ex.IsBodyweight || workingKg < 20)
            return list;

        var floor = ex.Equipment == Equipment.Barbell ? 20 : 0;
        foreach (var (pct, reps) in new[] { (0.5, 8), (0.7, 5), (0.85, 3) })
        {
            var w = Math.Max(floor, units.Round(workingKg * pct, ex));
            if (w >= workingKg || list.Any(s => Math.Abs(s.WeightKg - w) < 0.01))
                continue;
            list.Add(new SetEntry { WeightKg = w, Reps = reps, IsWarmup = true });
        }
        return list;
    }

    /// <summary>Estimated one-rep max (Epley), counting reps in reserve as reps you could have done.</summary>
    public static double E1Rm(double kg, int reps, int? rir)
    {
        var r = reps + (rir ?? 0);
        if (r <= 0 || kg <= 0)
            return 0;
        return r == 1 ? kg : kg * (1 + r / 30.0);
    }
}

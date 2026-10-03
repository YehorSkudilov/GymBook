using System.Globalization;
using System.Text.RegularExpressions;

namespace GymBook.Services.Import;

/// <summary>An exercise as the other app names it: "Incline Bench Press" with "Dumbbells".</summary>
public record ImportedExerciseRef(string Name, string EquipmentText)
{
    public string Key => EquipmentText.Length == 0 ? Name : $"{Name} · {EquipmentText}";
}

public record ImportedPlanExercise(ImportedExerciseRef Exercise, int Sets, int RepMin, int RepMax);

public record ImportedDay(string Name, List<ImportedPlanExercise> Exercises);

public record ImportedPlan(string Name, DateTime CreatedAt, List<ImportedDay> Days);

public record ImportedSet(double Kg, int Reps);

public record ImportedWorkoutExercise(ImportedExerciseRef Exercise, int TargetReps, List<ImportedSet> Warmups, List<ImportedSet> Sets);

/// <summary>A workout done; <see cref="PlanName"/> and the day and week when it was one of a plan's.</summary>
public record ImportedWorkout(string Name, string? PlanName, int? Day, int? Week, DateTime StartedAt, TimeSpan Duration,
    List<ImportedWorkoutExercise> Exercises);

/// <summary>
/// Reads the CSV exports of plans and of workout history from another workout app (semicolon-separated, weights in lb
/// or kg). The two come as separate files:
/// <code>
/// Plans:                                           Workouts:
///   PPLPPL;2026-06-29                                "Pull B · Day 5 · Week 9 · PPLPPL";"2026-09-24 10:58 pm";"1:07 hr"
///   "Day 1 · Push A"                                 "1. Bent-Over Rows · Barbell · 10 reps";"WU2 · 105 lbs · 8 reps"
///   "1. Incline Bench Press · Dumbbells";"4 sets";"8 reps"   #;LB;REPS
///                                                    1;225;7      (+70: added weight; -;-: not done)
/// </code>
/// A workout outside a plan is "Push · Standalone workout".
/// </summary>
public static partial class CsvExportFormat
{
    const double KgPerLb = 0.45359237;
    const string Standalone = "Standalone workout";

    [GeneratedRegex(@"^(\d+)\.\s+(.+)$")]
    private static partial Regex Numbered();

    [GeneratedRegex(@"^Day\s+(\d+)\s+·\s+(.+)$")]
    private static partial Regex DayHeader();

    [GeneratedRegex(@"(\d+)(?:\s*[-–]\s*(\d+))?")]
    private static partial Regex Count();

    [GeneratedRegex(@"^WU\d*\s*·\s*([\d.,]+)\s*(lbs?|kg)\s*·\s*(\d+)\s*reps?", RegexOptions.IgnoreCase)]
    private static partial Regex Warmup();

    /// <summary>The plans in a plans export. Throws <see cref="FormatException"/> when it isn't one.</summary>
    public static List<ImportedPlan> ParsePlans(string text)
    {
        var plans = new List<ImportedPlan>();
        ImportedPlan? plan = null;
        ImportedDay? day = null;
        foreach (var record in CsvRecords.Parse(text))
        {
            var first = record.FirstOrDefault()?.Trim() ?? "";
            if (first.Length == 0)
                continue;
            if (DayHeader().Match(first) is { Success: true } d && plan != null)
            {
                day = new ImportedDay(d.Groups[2].Value.Trim(), []);
                plan.Days.Add(day);
            }
            else if (Numbered().Match(first) is { Success: true } n && day != null)
            {
                var (sets, _) = Range(record.ElementAtOrDefault(1));
                var (repMin, repMax) = Range(record.ElementAtOrDefault(2));
                // "0 sets": listed but not done; nothing to carry over.
                if (sets > 0)
                    day.Exercises.Add(new ImportedPlanExercise(Reference(n.Groups[2].Value), sets, Math.Max(1, repMin), Math.Max(1, repMax)));
            }
            else if (record.Count >= 2 && DateTime.TryParseExact(record[1].Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var created))
            {
                plan = new ImportedPlan(first, created, []);
                plans.Add(plan);
                day = null;
            }
        }
        plans.RemoveAll(p => p.Days.Count == 0);
        if (plans.Count == 0)
            throw new FormatException("No plans found. Pick the plans export (a name and date, then \"Day 1 · …\" and its exercises).");
        return plans;
    }

    /// <summary>The workouts in a workout history export. Throws <see cref="FormatException"/> when it isn't one.</summary>
    public static List<ImportedWorkout> ParseWorkouts(string text)
    {
        var workouts = new List<ImportedWorkout>();
        List<ImportedWorkoutExercise>? exercises = null;
        ImportedWorkoutExercise? exercise = null;
        var inPounds = true;
        foreach (var record in CsvRecords.Parse(text))
        {
            var first = record.FirstOrDefault()?.Trim() ?? "";
            if (first.Length == 0)
                continue;
            if (record.Count >= 2 && ParseStart(record[1]) is { } started)
            {
                var (name, planName, dayNumber, week) = Title(first);
                exercises = [];
                exercise = null;
                workouts.Add(new ImportedWorkout(name, planName, dayNumber, week, started, ParseDuration(record.ElementAtOrDefault(2)), exercises));
            }
            else if (first == "#" && record.Count >= 3)
            {
                // The sets' table: its weight column says the unit.
                inPounds = !record[1].Trim().Equals("KG", StringComparison.OrdinalIgnoreCase);
            }
            else if (exercises != null && Numbered().Match(first) is { Success: true } n)
            {
                // "Bent-Over Rows · Barbell · 10 reps": the target reps last.
                var parts = n.Groups[2].Value.Split(" · ").Select(p => p.Trim()).ToList();
                var target = 0;
                if (parts.Count > 1 && parts[^1].EndsWith("reps", StringComparison.OrdinalIgnoreCase))
                {
                    target = Range(parts[^1]).Min;
                    parts.RemoveAt(parts.Count - 1);
                }
                var reference = parts.Count > 1 ? new ImportedExerciseRef(string.Join(" · ", parts[..^1]), parts[^1]) : new ImportedExerciseRef(parts[0], "");
                exercise = new ImportedWorkoutExercise(reference, target, Warmups(record.ElementAtOrDefault(1)), []);
                exercises.Add(exercise);
            }
            else if (exercise != null && record.Count >= 3 && int.TryParse(first, out _))
            {
                // "1;225;7", "1;+70;12" (added to the bodyweight), "1;-;-" (not done: left out).
                var weight = record[1].Trim().TrimStart('+');
                if (weight == "-" || !int.TryParse(record[2].Trim(), out var reps) || reps <= 0)
                    continue;
                var amount = double.TryParse(weight, NumberStyles.Float, CultureInfo.InvariantCulture, out var w) ? w : 0;
                exercise.Sets.Add(new ImportedSet(inPounds ? amount * KgPerLb : amount, reps));
            }
        }
        // Workouts with nothing done in them don't count.
        foreach (var workout in workouts)
            workout.Exercises.RemoveAll(e => e.Sets.Count == 0);
        workouts.RemoveAll(w => w.Exercises.Count == 0);
        if (workouts.Count == 0)
            throw new FormatException("No workouts found. Pick the workouts export (a workout's name, date and time, then its exercises and sets).");
        return workouts;
    }

    /// <summary>"Incline Bench Press · Dumbbells".</summary>
    static ImportedExerciseRef Reference(string text)
    {
        var at = text.LastIndexOf(" · ", StringComparison.Ordinal);
        return at < 0 ? new(text.Trim(), "") : new(text[..at].Trim(), text[(at + 3)..].Trim());
    }

    /// <summary>"Pull B · Day 5 · Week 9 · PPLPPL", or "Pull · Standalone workout".</summary>
    static (string Name, string? Plan, int? Day, int? Week) Title(string title)
    {
        var parts = title.Split(" · ").Select(p => p.Trim()).ToList();
        if (parts.Count >= 2 && parts[1].Equals(Standalone, StringComparison.OrdinalIgnoreCase))
            return (parts[0], null, null, null);
        int? day = null, week = null;
        var rest = new List<string>();
        foreach (var part in parts.Skip(1))
        {
            if (part.StartsWith("Day ", StringComparison.OrdinalIgnoreCase) && int.TryParse(part[4..], out var d))
                day = d;
            else if (part.StartsWith("Week ", StringComparison.OrdinalIgnoreCase) && int.TryParse(part[5..], out var w))
                week = w;
            else
                rest.Add(part);
        }
        return (parts[0], rest.Count > 0 ? string.Join(" · ", rest) : null, day, week);
    }

    /// <summary>"2026-09-24 10:58 pm".</summary>
    static DateTime? ParseStart(string text) =>
        DateTime.TryParseExact(text.Trim().ToUpperInvariant(), ["yyyy-MM-dd h:mm tt", "yyyy-MM-dd hh:mm tt", "yyyy-MM-dd H:mm", "yyyy-MM-dd HH:mm"],
            CultureInfo.InvariantCulture, DateTimeStyles.None, out var at) ? at : null;

    /// <summary>"1:07 hr", "2 hr", "45 min".</summary>
    static TimeSpan ParseDuration(string? text)
    {
        var t = (text ?? "").Trim().ToLowerInvariant();
        var number = t.Split(' ')[0];
        if (t.EndsWith("min") && double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var minutes))
            return TimeSpan.FromMinutes(minutes);
        if (number.Split(':') is [var h, var m] && int.TryParse(h, out var hours) && int.TryParse(m, out var mins))
            return new TimeSpan(hours, mins, 0);
        if (double.TryParse(number, NumberStyles.Float, CultureInfo.InvariantCulture, out var hrs))
            return TimeSpan.FromHours(hrs);
        return TimeSpan.FromHours(1);
    }

    /// <summary>"8 reps", "4 sets", "8-10 reps": the low and high end.</summary>
    static (int Min, int Max) Range(string? text)
    {
        if (text == null || Count().Match(text) is not { Success: true } m)
            return (0, 0);
        var min = int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        var max = m.Groups[2].Success ? int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture) : min;
        return (Math.Min(min, max), Math.Max(min, max));
    }

    /// <summary>"WU2 · 105 lbs · 8 reps", one per line.</summary>
    static List<ImportedSet> Warmups(string? text)
    {
        var sets = new List<ImportedSet>();
        foreach (var line in (text ?? "").Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Warmup().Match(line) is not { Success: true } m
                || !double.TryParse(m.Groups[1].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var weight))
                continue;
            var kg = m.Groups[2].Value.StartsWith("lb", StringComparison.OrdinalIgnoreCase) ? weight * KgPerLb : weight;
            sets.Add(new ImportedSet(kg, int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture)));
        }
        return sets;
    }

    // Writing: the same two exports, so Gym Book's own can be imported again (here or in a new account) like the other
    // app's. Exercises are written by name and equipment, which the importer matches back to the app's.

    /// <summary>A plans export of <paramref name="plans"/>: each plan's days in order, rest days as "Day 3 · Rest".</summary>
    public static string WritePlans(IEnumerable<GymBook.Models.WorkoutPlan> plans, Func<string, GymBook.Models.Exercise?> exercise)
    {
        var csv = new System.Text.StringBuilder();
        foreach (var plan in plans)
        {
            csv.Append(Quote(plan.Name)).Append(';').Append(plan.CreatedAt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append('\n');
            var days = PlanSchedule.Days(plan);
            for (var i = 0; i < days.Count; i++)
            {
                csv.Append(Quote($"Day {i + 1} · {days[i]?.Name ?? "Rest"}")).Append('\n');
                var number = 0;
                foreach (var pe in days[i]?.Exercises ?? [])
                    csv.Append(Quote($"{++number}. {Reference(exercise(pe.ExerciseId))}")).Append(';')
                        .Append(Quote($"{pe.Sets} sets")).Append(';').Append(Quote($"{Reps(pe.RepMin, pe.RepMax)} reps")).Append('\n');
            }
            csv.Append('\n');
        }
        return csv.ToString();
    }

    /// <summary>
    /// A workouts export of <paramref name="sessions"/>: done sets only, weights in <paramref name="unit"/>. A workout of
    /// a plan still in <paramref name="plans"/> says its day, week and plan; any other is a standalone workout.
    /// </summary>
    public static string WriteWorkouts(IEnumerable<GymBook.Models.WorkoutSession> sessions, IReadOnlyCollection<GymBook.Models.WorkoutPlan> plans,
        Func<string, GymBook.Models.Exercise?> exercise, GymBook.Models.WeightUnit unit)
    {
        var pounds = unit == GymBook.Models.WeightUnit.Lbs;
        string Weight(double kg) => (pounds ? kg / KgPerLb : kg).ToString("0.##", CultureInfo.InvariantCulture);
        var csv = new System.Text.StringBuilder();
        foreach (var session in sessions)
        {
            var exercises = session.Exercises.Where(e => e.Sets.Any(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0)).ToList();
            if (exercises.Count == 0)
                continue;

            var plan = plans.FirstOrDefault(p => p.Id == session.PlanId);
            var day = plan == null ? -1 : PlanSchedule.Days(plan).FindIndex(w => w != null && w.Id == session.PlanWorkoutId);
            var title = plan == null || day < 0
                ? $"{session.Name} · {Standalone}"
                : $"{session.Name} · Day {day + 1}{(session.PlanWeek is { } week ? $" · Week {week}" : "")} · {plan.Name}";
            var duration = (session.EndedAt ?? session.StartedAt) - session.StartedAt;
            csv.Append(Quote(title)).Append(';')
                .Append(Quote(session.StartedAt.ToString("yyyy-MM-dd h:mm tt", CultureInfo.InvariantCulture).ToLowerInvariant())).Append(';')
                .Append(Quote(duration.TotalHours >= 1 ? $"{(int)duration.TotalHours}:{duration.Minutes:00} hr" : $"{Math.Max(1, (int)Math.Round(duration.TotalMinutes))} min"))
                .Append('\n');

            var number = 0;
            foreach (var se in exercises)
            {
                var warmups = se.Sets.Where(s => s.IsWarmup && s.IsCompleted && s.Reps > 0)
                    .Select((s, i) => $"WU{i + 1} · {Weight(s.WeightKg)} {(pounds ? "lbs" : "kg")} · {s.Reps} reps");
                csv.Append(Quote($"{++number}. {Reference(exercise(se.ExerciseId))} · {Reps(se.RepMin, se.RepMax)} reps")).Append(';')
                    .Append(Quote(string.Join("\n", warmups))).Append('\n');
                csv.Append(pounds ? "#;LB;REPS" : "#;KG;REPS").Append('\n');
                var set = 0;
                foreach (var s in se.Sets.Where(s => s.IsCompleted && !s.IsWarmup && s.Reps > 0))
                    csv.Append(++set).Append(';').Append(Weight(s.WeightKg)).Append(';').Append(s.Reps).Append('\n');
            }
            csv.Append('\n');
        }
        return csv.ToString();
    }

    /// <summary>"Incline Bench Press · Dumbbells": the name, and the equipment as the importer reads it back.</summary>
    static string Reference(GymBook.Models.Exercise? exercise)
    {
        if (exercise == null)
            return "Unknown exercise";
        var equipment = exercise.Equipment switch
        {
            GymBook.Models.Equipment.Barbell => "Barbell",
            GymBook.Models.Equipment.Dumbbell => "Dumbbells",
            GymBook.Models.Equipment.Machine => "Machine",
            GymBook.Models.Equipment.Cable => "Cable",
            GymBook.Models.Equipment.Bodyweight => "Bodyweight",
            GymBook.Models.Equipment.Kettlebell => "Kettlebell",
            GymBook.Models.Equipment.EzBar => "EZ Bar",
            GymBook.Models.Equipment.Band => "Band",
            _ => "",
        };
        return equipment.Length == 0 ? exercise.Name : $"{exercise.Name} · {equipment}";
    }

    static string Reps(int min, int max) => min == max ? $"{min}" : $"{Math.Min(min, max)}-{Math.Max(min, max)}";

    /// <summary>A field in quotes, its own quotes doubled.</summary>
    static string Quote(string text) => $"\"{text.Replace("\"", "\"\"")}\"";
}

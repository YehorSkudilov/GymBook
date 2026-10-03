using GymBook.Contracts;
using GymBook.Models;

namespace GymBook.Services.Import;

/// <summary>
/// What one of the other app's exercises becomes: <see cref="Target"/>, one of the app's (found by
/// <see cref="ExerciseMatcher"/>, or picked by hand), or when it's null a new custom exercise with the other app's name.
/// </summary>
public class ExerciseMapping(ImportedExerciseRef source, int uses, ExerciseMatch match)
{
    public ImportedExerciseRef Source { get; } = source;
    public int Uses { get; } = uses;
    public ExerciseMatch Match { get; } = match;
    public Exercise? Target { get; set; } = match.Confidence == MatchConfidence.None ? null : match.Exercise;
}

/// <summary>
/// Brings another app's CSV exports into the app (see <see cref="CsvExportFormat"/>): plans, and separately the workout
/// history, with each exercise mapped to one of the app's (<see cref="ExerciseMapping"/>). Everything imported is
/// saved like anything made in the app, so it syncs to the account. Importing the same file again adds nothing twice.
/// </summary>
public class CsvImporter(DataStore store, AiPlanService ai)
{
    const int RestSeconds = 120;

    /// <summary>
    /// Every exercise the import uses, once, with the app's best match; most used first. The word matcher goes first;
    /// every name it isn't sure about goes to the AI, which knows gym names and variants far better. Without the AI
    /// (signed out, offline, or it fails) the word matcher's results stand, and <c>AiChecked</c> is false.
    /// </summary>
    public async Task<(List<ExerciseMapping> Mappings, bool AiChecked)> MappingsAsync(IEnumerable<ImportedExerciseRef> used, CancellationToken ct = default)
    {
        var matcher = new ExerciseMatcher(store.AllExercises);
        var groups = used.GroupBy(r => r.Key)
            .Select(g => (Source: g.First(), Uses: g.Count(), Match: matcher.Match(g.First().Name, ExerciseMatcher.ParseEquipment(g.First().EquipmentText))))
            .ToList();

        var unsure = groups.Where(g => g.Match.Confidence != MatchConfidence.Sure).ToList();
        var aiChecked = unsure.Count == 0;
        if (unsure.Count > 0 && ai.IsAvailable)
        {
            try
            {
                var names = unsure.Select(g => new ImportedName { Key = g.Source.Key, Name = g.Source.Name, Equipment = g.Source.EquipmentText }).ToList();
                var found = await ai.MatchExercisesAsync(names, ct);
                for (var i = 0; i < groups.Count; i++)
                {
                    var g = groups[i];
                    if (g.Match.Confidence == MatchConfidence.Sure || !found.TryGetValue(g.Source.Key, out var exercise))
                        continue;
                    groups[i] = (g.Source, g.Uses, exercise != null ? new ExerciseMatch(exercise, 1, MatchSource.Ai) : g.Match with { Source = MatchSource.AiFoundNone });
                }
                aiChecked = true;
            }
            catch (Exception e) when (e is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                // Offline or the AI is unavailable: the word matcher's results stand.
            }
        }
        return ([.. groups.Select(g => new ExerciseMapping(g.Source, g.Uses, g.Match)).OrderByDescending(m => m.Uses)], aiChecked);
    }

    /// <summary>Adds the plans not already in the app (by name and date). The first becomes the active one if there's none.</summary>
    public int ImportPlans(List<ImportedPlan> plans, List<ExerciseMapping> mappings)
    {
        var resolve = Resolver(mappings);
        var added = 0;
        foreach (var source in plans)
        {
            if (store.Data.Plans.Any(p => !p.IsDeleted && p.Name == source.Name && p.CreatedAt.Date == source.CreatedAt.Date))
                continue;
            // A day with no exercises ("Day 3 · Rest") is a rest day of the plan, not a workout with nothing in it.
            if (source.Days.All(d => d.Exercises.Count == 0))
                continue;
            var plan = new WorkoutPlan
            {
                Name = source.Name,
                Goal = store.Profile.Goal,
                DaysPerWeek = Math.Clamp(source.Days.Count(d => d.Exercises.Count > 0), 1, 7),
                CreatedAt = source.CreatedAt,
                // Positions over all the days, workouts and rest days together, as the export lists them.
                RestDays = [.. source.Days.Select((d, i) => (d, i)).Where(x => x.d.Exercises.Count == 0).Select(x => x.i)],
            };
            // Training follows the profile's defaults, like any new plan.
            PlanTraining.Apply(plan, store.Profile);
            foreach (var day in source.Days.Where(d => d.Exercises.Count > 0))
            {
                var workout = new PlanWorkout { Name = day.Name };
                foreach (var item in day.Exercises)
                {
                    var exercise = resolve(item.Exercise);
                    // Set up as an added exercise would be, then given the other app's sets and reps.
                    var pe = TrainingGoals.Prescription(plan.Goal, store.Profile.Experience, exercise, store.Profile);
                    pe.RestSeconds = PlanRest.DefaultFor(plan, store.Profile, exercise);
                    pe.TargetRir = PlanTraining.RirFor(plan, store.Profile, exercise);
                    pe.Sets = item.Sets;
                    pe.RepMin = item.RepMin;
                    pe.RepMax = item.RepMax;
                    workout.Exercises.Add(pe);
                }
                plan.Workouts.Add(workout);
            }
            store.Data.Plans.Add(plan);
            store.Data.ActivePlanId ??= plan.Id;
            added++;
        }
        if (added > 0)
            store.Save();
        return added;
    }

    /// <summary>The plans in the app, to import workouts for one of them.</summary>
    public List<WorkoutPlan> Plans => [.. store.Data.Plans.Where(p => !p.IsDeleted)];

    /// <summary>
    /// The day of <paramref name="plan"/> a workout from the file was: the one with its name, else (when the file says it
    /// was this plan) its day number, which counts rest days too ("Day 5" of Push, Rest, Pull… is the plan's fifth day).
    /// </summary>
    public static PlanWorkout? DayIn(WorkoutPlan plan, ImportedWorkout source) =>
        plan.Workouts.FirstOrDefault(w => w.Name.Equals(source.Name, StringComparison.OrdinalIgnoreCase))
        ?? (source.Day is { } d && source.PlanName?.Equals(plan.Name, StringComparison.OrdinalIgnoreCase) == true
            ? PlanSchedule.Days(plan).ElementAtOrDefault(d - 1)
            : null);

    /// <summary>
    /// Adds the workouts not already in the app (by name and start time), linked to their plan, day and week when the
    /// plan is in the app (import the plans first for that). With <paramref name="only"/>, just the workouts that match
    /// one of its days (see <see cref="DayIn"/>), whatever plan the file says, each linked to it.
    /// </summary>
    public int ImportWorkouts(List<ImportedWorkout> workouts, List<ExerciseMapping> mappings, WorkoutPlan? only = null)
    {
        var resolve = Resolver(mappings);
        var added = 0;
        foreach (var source in workouts.OrderBy(w => w.StartedAt))
        {
            if (store.Data.Sessions.Any(s => !s.IsDeleted && s.Name == source.Name && Math.Abs((s.StartedAt - source.StartedAt).TotalMinutes) < 1))
                continue;
            var plan = only ?? (source.PlanName == null ? null : store.Data.Plans
                .Where(p => !p.IsDeleted && p.Name.Equals(source.PlanName, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(p => p.CreatedAt <= source.StartedAt)
                .ThenByDescending(p => p.CreatedAt)
                .FirstOrDefault());
            var planWorkout = plan == null ? null : DayIn(plan, source);
            if (only != null && planWorkout == null)
                continue;
            var session = new WorkoutSession
            {
                Name = source.Name,
                PlanId = planWorkout == null ? null : plan!.Id,
                PlanWorkoutId = planWorkout?.Id,
                PlanWeek = planWorkout == null ? null : source.Week,
                StartedAt = source.StartedAt,
                EndedAt = source.StartedAt + source.Duration,
            };
            // The export has no times per set: spread them over the workout, in order.
            var total = Math.Max(1, source.Exercises.Sum(e => e.Warmups.Count + e.Sets.Count));
            var step = source.Duration / total;
            var at = source.StartedAt;
            foreach (var item in source.Exercises)
            {
                var exercise = resolve(item.Exercise);
                var se = new SessionExercise
                {
                    ExerciseId = exercise.Id,
                    RepMin = item.TargetReps > 0 ? item.TargetReps : item.Sets.Min(s => s.Reps),
                    RepMax = item.TargetReps > 0 ? item.TargetReps : item.Sets.Max(s => s.Reps),
                    RestSeconds = RestSeconds,
                };
                foreach (var (set, warmup) in item.Warmups.Select(s => (s, true)).Concat(item.Sets.Select(s => (s, false))))
                {
                    at += step;
                    se.Sets.Add(new SetEntry
                    {
                        WeightKg = Math.Round(set.Kg, 2),
                        Reps = set.Reps,
                        IsWarmup = warmup,
                        IsCompleted = true,
                        CompletedAt = at,
                    });
                }
                session.Exercises.Add(se);
            }
            store.Data.Sessions.Add(session);
            added++;
        }
        if (added > 0)
        {
            // Each plan picks up after the newest workout imported for it.
            foreach (var plan in store.Data.Plans.Where(p => !p.IsDeleted && p.Workouts.Count > 0))
                if (store.History.FirstOrDefault(s => s.PlanId == plan.Id) is { } last
                    && plan.Workouts.FindIndex(w => w.Id == last.PlanWorkoutId) is >= 0 and var index)
                    plan.NextWorkoutIndex = (index + 1) % plan.Workouts.Count;
            store.Save();
        }
        return added;
    }

    /// <summary>The app's exercise for each of the other app's: the mapped one, or a custom exercise made once per import.</summary>
    Func<ImportedExerciseRef, Exercise> Resolver(List<ExerciseMapping> mappings)
    {
        var byKey = mappings.ToDictionary(m => m.Source.Key);
        var created = new Dictionary<string, Exercise>();
        return source =>
        {
            if (byKey.GetValueOrDefault(source.Key)?.Target is { } target)
                return target;
            if (created.TryGetValue(source.Key, out var made))
                return made;
            // Already made by an earlier import of the same data: reuse it.
            var name = Name(source);
            if (store.Data.CustomExercises.FirstOrDefault(e => !e.IsDeleted && e.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) is { } existing)
                return created[source.Key] = existing;
            var guess = byKey.GetValueOrDefault(source.Key)?.Match.Exercise;
            var exercise = new Exercise
            {
                Id = "custom_" + Guid.NewGuid().ToString("N")[..8],
                Name = name,
                // The closest match's muscles, even when it wasn't close enough to use: better than nothing.
                PrimaryMuscle = guess?.PrimaryMuscle ?? MuscleGroup.Chest,
                SecondaryMuscles = guess?.SecondaryMuscles.ToList() ?? [],
                Equipment = ExerciseMatcher.ParseEquipment(source.EquipmentText) ?? Equipment.Other,
                Mechanic = guess?.Mechanic ?? Mechanic.Isolation,
                IsCustom = true,
            };
            store.Data.CustomExercises.Add(exercise);
            return created[source.Key] = exercise;
        };
    }

    /// <summary>A custom exercise's name: the other app's, with the equipment when it isn't in the name already.</summary>
    public static string Name(ImportedExerciseRef source) =>
        source.EquipmentText.Length == 0 || source.Name.Contains(source.EquipmentText, StringComparison.OrdinalIgnoreCase)
            ? source.Name
            : $"{source.Name} ({source.EquipmentText})";
}

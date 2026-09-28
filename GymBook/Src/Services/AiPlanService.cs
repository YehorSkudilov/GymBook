using GymBook.Contracts;
using GymBook.Models;
using GymBook.Services.Sync;

namespace GymBook.Services;

/// <summary>
/// Asks the API to build a plan with AI (ChatGPT, server side) from the wizard's answers. Only for signed-in users;
/// the wizard falls back to <see cref="PlanGenerator"/> otherwise, or when this fails.
/// </summary>
public class AiPlanService(ApiClient api, AccountService account, DataStore store)
{
    public bool IsAvailable => account.IsSignedIn;

    /// <summary>The last known quota, from <see cref="RefreshQuotaAsync"/> or the last generated plan; null when unknown.</summary>
    public PlanQuotaResponse? Quota { get; private set; }

    /// <summary>All AI plans used up, and none frees up yet.</summary>
    public bool IsQuotaUsedUp => Quota is { Remaining: 0 } q && (q.NextAvailableAt is not { } next || next > DateTimeOffset.UtcNow);

    /// <summary>Fetches how many AI plans are left. Keeps the last known value when offline.</summary>
    public async Task<PlanQuotaResponse?> RefreshQuotaAsync()
    {
        if (!IsAvailable)
            return Quota = null;
        try
        {
            Quota = await api.GetPlanQuotaAsync();
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or ApiException or SessionExpiredException)
        {
        }
        return Quota;
    }

    /// <summary>"3 of 5 AI plans left per day", or when the next one frees up.</summary>
    public static string Describe(PlanQuotaResponse q)
    {
        if (q.Remaining > 0)
            return $"{q.Remaining} of {q.Limit} AI plan{(q.Limit == 1 ? "" : "s")} left per {q.Period}";
        if (q.NextAvailableAt is not { } next)
            return "No AI plans left";
        var local = next.ToLocalTime().DateTime;
        var when = local.Date == DateTime.Today ? $"at {local:t}" : local.Date == DateTime.Today.AddDays(1) ? $"tomorrow at {local:t}" : $"on {local:ddd d MMM}";
        return $"No AI plans left. Your next one is available {when}";
    }

    /// <summary>A plan for <paramref name="answers"/>, with the user's own rest times applied. Throws on any failure.</summary>
    public async Task<WorkoutPlan> GenerateAsync(UserProfile answers, CancellationToken ct = default)
    {
        var candidates = store.AllExercises
            .Where(e => !e.IsDeleted && answers.EquipmentAccess.Allows(e.Equipment))
            .Where(e => answers.TrainNeck || e.PrimaryMuscle != MuscleGroup.Neck)
            .Take(PlanLimits.MaxCandidates)
            .ToList();
        var request = new GeneratePlanRequest
        {
            Goal = answers.Goal,
            Experience = answers.Experience,
            DaysPerWeek = answers.DaysPerWeek,
            SessionMinutes = answers.SessionMinutes,
            EquipmentAccess = answers.EquipmentAccess,
            TrainNeck = answers.TrainNeck,
            Age = answers.BirthYear is { } year ? Math.Clamp(DateTime.Now.Year - year, 10, 100) : null,
            BodyWeightKg = answers.BodyWeightKg is > 20 and < 400 ? answers.BodyWeightKg : null,
            TrainingYears = answers.TrainingSince is { } since ? Math.Clamp((DateTime.Now - since).TotalDays / 365, 0, 80) : null,
            Exercises = [.. candidates.Select(e => new PlanCandidate
            {
                Id = e.Id,
                Name = e.Name,
                PrimaryMuscle = e.PrimaryMuscle,
                Mechanic = e.Mechanic,
                Equipment = e.Equipment,
            })],
        };

        GeneratePlanResponse response;
        try
        {
            response = await api.GeneratePlanAsync(request, ct);
        }
        catch (ApiException e) when (e.Status == System.Net.HttpStatusCode.TooManyRequests)
        {
            // Quota used up (e.g. on another device): pick up when the next one is available.
            await RefreshQuotaAsync();
            throw;
        }
        Quota = response.Quota ?? Quota;

        var plan = new WorkoutPlan
        {
            Name = response.Name,
            Description = response.Description,
            Goal = answers.Goal,
            DaysPerWeek = answers.DaysPerWeek,
        };
        foreach (var w in response.Workouts)
        {
            var workout = new PlanWorkout { Name = w.Name };
            foreach (var e in w.Exercises)
            {
                // The server only returns ids it was sent, but an exercise could have been deleted in the meantime.
                if (store.GetExercise(e.ExerciseId) is not { } ex)
                    continue;
                workout.Exercises.Add(new PlanExercise
                {
                    ExerciseId = ex.Id,
                    Sets = e.Sets,
                    RepMin = e.RepMin,
                    RepMax = e.RepMax,
                    TargetRir = e.TargetRir,
                    RestSeconds = TrainingGoals.RestOverride(answers, ex) ?? e.RestSeconds,
                });
            }
            if (workout.Exercises.Count > 0)
                plan.Workouts.Add(workout);
        }
        if (plan.Workouts.Count == 0)
            throw new InvalidOperationException("The generated plan has no workouts.");
        plan.RestDays = [.. PlanSchedule.DefaultRestDays(plan.Workouts.Count).Order()];
        return plan;
    }
}

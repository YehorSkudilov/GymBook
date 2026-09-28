using GymBook.Contracts;
using GymBook.Models;
using GymBook.Services.Sync;

namespace GymBook.Services;

/// <summary>
/// The AI side of plans (ChatGPT, server side): follow-up questions for the wizard's answers, generating a plan from
/// them, and a chat that changes a plan. Only for signed-in users; the wizard falls back to <see cref="PlanGenerator"/>
/// otherwise, or when this fails.
/// </summary>
public class AiPlanService(ApiClient api, AccountService account, DataStore store)
{
    public bool IsAvailable => account.IsSignedIn;

    /// <summary>The last known quota, from <see cref="RefreshQuotaAsync"/> or the last generated plan; null when unknown.</summary>
    public PlanQuotaResponse? Quota { get; private set; }

    /// <summary>The chat's own quota, as of the last message; null until one is sent.</summary>
    public PlanQuotaResponse? ChatQuota { get; private set; }

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

    /// <summary>"9/10 AI plans left · resets in 45 min" (when the oldest one used frees up again).</summary>
    public static string Describe(PlanQuotaResponse q, string what = "AI plans")
    {
        var text = $"{q.Remaining}/{q.Limit} {what} left";
        return q.NextAvailableAt is { } next && q.Remaining < q.Limit ? $"{text} · resets {Until(next)}" : text;
    }

    /// <summary>"in 45 min", "in 5 h", "tomorrow" or "on Mon 5 Oct".</summary>
    static string Until(DateTimeOffset at)
    {
        var left = at - DateTimeOffset.UtcNow;
        if (left <= TimeSpan.FromMinutes(1))
            return "in a minute";
        if (left < TimeSpan.FromHours(1))
            return $"in {Math.Ceiling(left.TotalMinutes):0} min";
        if (left < TimeSpan.FromHours(24))
            return $"in {Math.Round(left.TotalHours):0} h";
        var local = at.ToLocalTime().Date;
        return local == DateTime.Today.AddDays(1) ? "tomorrow" : $"on {local:ddd d MMM}";
    }

    /// <summary>A few questions the AI wants answered before it builds the plan.</summary>
    public async Task<List<PlanQuestion>> QuestionsAsync(UserProfile answers, CancellationToken ct = default)
    {
        var response = await api.GetPlanQuestionsAsync(Fill(new PlanAnswers(), answers), ct);
        return response.Questions;
    }

    /// <summary>A plan for <paramref name="answers"/> and the follow-up answers, with the user's own rest times applied. Throws on any failure.</summary>
    public async Task<WorkoutPlan> GenerateAsync(UserProfile answers, List<PlanAnswer> extraAnswers, CancellationToken ct = default)
    {
        var request = Fill(new GeneratePlanRequest(), answers);
        request.Exercises = Candidates(answers);
        request.ExtraAnswers = extraAnswers;

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
        plan.Workouts = ToWorkouts(response, answers, []);
        if (plan.Workouts.Count == 0)
            throw new InvalidOperationException("The generated plan has no workouts.");
        plan.RestDays = [.. PlanSchedule.DefaultRestDays(plan.Workouts.Count).Order()];
        return plan;
    }

    /// <summary>A plan read by the AI from pasted text, a link or a file, rebuilt from the app's exercises.</summary>
    public async Task<WorkoutPlan> ImportAsync(string? text, string? link, ImportFile? file, CancellationToken ct = default)
    {
        var answers = store.Profile;
        var request = Fill(new ImportPlanRequest(), answers);
        request.Text = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        request.Link = string.IsNullOrWhiteSpace(link) ? null : link.Trim();
        request.File = file;
        // Any exercise can appear in someone else's plan, so the whole library is offered, not just the user's equipment.
        request.Exercises = Candidates(new UserProfile { EquipmentAccess = EquipmentAccess.FullGym, TrainNeck = true });

        GeneratePlanResponse response;
        try
        {
            response = await api.ImportPlanAsync(request, ct);
        }
        catch (ApiException e) when (e.Status == System.Net.HttpStatusCode.TooManyRequests)
        {
            await RefreshQuotaAsync();
            throw;
        }
        Quota = response.Quota ?? Quota;

        var plan = new WorkoutPlan { Name = response.Name, Description = response.Description, Goal = answers.Goal };
        plan.Workouts = ToWorkouts(response, answers, []);
        if (plan.Workouts.Count == 0)
            throw new InvalidOperationException("The imported plan has no workouts.");
        plan.DaysPerWeek = plan.Workouts.Count;
        plan.RestDays = [.. PlanSchedule.DefaultRestDays(plan.Workouts.Count).Order()];
        return plan;
    }

    /// <summary>
    /// Sends the conversation about <paramref name="plan"/> to the AI. Returns its reply, and whether it changed the
    /// plan, in which case <paramref name="plan"/> already holds the change (the caller saves it).
    /// </summary>
    public async Task<(string Reply, bool Changed)> ChatAsync(WorkoutPlan plan, UserProfile answers, List<PlanChatMessage> messages, CancellationToken ct = default)
    {
        var request = Fill(new PlanChatRequest(), answers);
        request.Plan = ToResponse(plan);
        request.Messages = messages;
        request.Exercises = Candidates(answers, plan);

        var response = await api.ChatAboutPlanAsync(request, ct);
        ChatQuota = response.Quota ?? ChatQuota;
        if (response.Plan is not { } changed)
            return (response.Reply, false);

        var workouts = ToWorkouts(changed, answers, plan.Workouts);
        if (workouts.Count == 0)
            return (response.Reply, false);
        plan.Name = changed.Name;
        plan.Description = changed.Description;
        var restDaysMatch = workouts.Count == plan.Workouts.Count;
        plan.Workouts = workouts;
        plan.DaysPerWeek = workouts.Count;
        if (!restDaysMatch)
            plan.RestDays = [.. PlanSchedule.DefaultRestDays(workouts.Count).Order()];
        return (response.Reply, true);
    }

    /// <summary>The wizard's answers the AI needs, for a saved plan: its own goal and days, the rest from the profile.</summary>
    public UserProfile AnswersFor(WorkoutPlan plan)
    {
        var p = store.Profile;
        return new UserProfile
        {
            Goal = plan.Goal,
            Experience = p.Experience,
            DaysPerWeek = plan.Workouts.Count > 0 ? plan.Workouts.Count : plan.DaysPerWeek,
            SessionMinutes = p.SessionMinutes,
            EquipmentAccess = p.EquipmentAccess,
            TrainNeck = p.TrainNeck,
            BodyWeightKg = p.BodyWeightKg,
            BirthYear = p.BirthYear,
            TrainingSince = p.TrainingSince,
            CompoundRestSeconds = p.CompoundRestSeconds,
            IsolationRestSeconds = p.IsolationRestSeconds,
        };
    }

    static T Fill<T>(T target, UserProfile answers) where T : PlanAnswers
    {
        target.Goal = answers.Goal;
        target.Experience = answers.Experience;
        target.DaysPerWeek = answers.DaysPerWeek;
        target.SessionMinutes = answers.SessionMinutes;
        target.EquipmentAccess = answers.EquipmentAccess;
        target.TrainNeck = answers.TrainNeck;
        target.Age = answers.BirthYear is { } year ? Math.Clamp(DateTime.Now.Year - year, 10, 100) : null;
        target.BodyWeightKg = answers.BodyWeightKg is > 20 and < 400 ? answers.BodyWeightKg : null;
        target.TrainingYears = answers.TrainingSince is { } since ? Math.Clamp((DateTime.Now - since).TotalDays / 365, 0, 80) : null;
        return target;
    }

    /// <summary>The exercises the AI may use: what the equipment allows, plus anything already in <paramref name="plan"/>.</summary>
    List<PlanCandidate> Candidates(UserProfile answers, WorkoutPlan? plan = null)
    {
        var inPlan = plan?.Workouts.SelectMany(w => w.Exercises).Select(e => e.ExerciseId).ToHashSet() ?? [];
        return [.. store.AllExercises
            .Where(e => !e.IsDeleted)
            .Where(e => inPlan.Contains(e.Id) || answers.EquipmentAccess.Allows(e.Equipment) && (answers.TrainNeck || e.PrimaryMuscle != MuscleGroup.Neck))
            .OrderByDescending(e => inPlan.Contains(e.Id))
            .Take(PlanLimits.MaxCandidates)
            .Select(e => new PlanCandidate
            {
                Id = e.Id,
                Name = e.Name,
                PrimaryMuscle = e.PrimaryMuscle,
                Mechanic = e.Mechanic,
                Equipment = e.Equipment,
            })];
    }

    static GeneratePlanResponse ToResponse(WorkoutPlan plan) => new()
    {
        Name = plan.Name,
        Description = plan.Description,
        Workouts = [.. plan.Workouts.Select(w => new GeneratedWorkout
        {
            Name = w.Name,
            Exercises = [.. w.Exercises.Select(e => new GeneratedExercise
            {
                ExerciseId = e.ExerciseId,
                Sets = e.Sets,
                RepMin = e.RepMin,
                RepMax = e.RepMax,
                TargetRir = e.TargetRir,
                RestSeconds = e.RestSeconds,
            })],
        })],
    };

    /// <summary>
    /// The AI's workouts as plan workouts. Workouts keep the ids of the ones at the same position in
    /// <paramref name="previous"/>, so logged sessions still count toward the plan's weeks after a change.
    /// </summary>
    List<PlanWorkout> ToWorkouts(GeneratePlanResponse response, UserProfile answers, List<PlanWorkout> previous)
    {
        var workouts = new List<PlanWorkout>();
        foreach (var w in response.Workouts)
        {
            var workout = new PlanWorkout { Name = w.Name };
            if (workouts.Count < previous.Count)
                workout.Id = previous[workouts.Count].Id;
            foreach (var e in w.Exercises)
            {
                // The server only returns ids it was sent, but an exercise could have been deleted in the meantime.
                if (store.GetExercise(e.ExerciseId) is not { } ex)
                    continue;
                // A rest time the user set wins, except where the AI was asked to change it on a plan being edited.
                var rest = previous.Count == 0 ? TrainingGoals.RestOverride(answers, ex) ?? e.RestSeconds : e.RestSeconds;
                workout.Exercises.Add(new PlanExercise
                {
                    ExerciseId = ex.Id,
                    Sets = e.Sets,
                    RepMin = e.RepMin,
                    RepMax = e.RepMax,
                    TargetRir = e.TargetRir,
                    RestSeconds = rest,
                });
            }
            if (workout.Exercises.Count > 0)
                workouts.Add(workout);
        }
        return workouts;
    }
}

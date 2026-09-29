using GymBook.Contracts;
using GymBook.Models;
using GymBook.Services.Sync;

namespace GymBook.Services;

/// <summary>
/// The AI side of plans (ChatGPT, server side): follow-up questions for the wizard's answers, generating a plan from
/// them, and a chat that changes a plan. Only for signed-in users; the wizard falls back to <see cref="PlanGenerator"/>
/// otherwise, or when this fails.
/// </summary>
public class AiPlanService(ApiClient api, AccountService account, DataStore store, StatsService stats)
{
    // The active plan is looked at again once a week, and only when there's something new to look at.
    static readonly TimeSpan CheckInterval = TimeSpan.FromDays(7);
    // A failed check (offline, quota used up) is retried after this, not on every resume.
    static readonly TimeSpan RetryInterval = TimeSpan.FromHours(6);
    const int MinSessionsForCheck = 3;
    // Reviews are kept per plan on this device, so the weekly check's result (and a "Next week") survives restarts.
    const string ReviewKey = "ai.review.";
    const string ReviewedPlansKey = "ai.review.plans";

    readonly Dictionary<string, DateTime> _lastTry = [];
    bool _checking;

    /// <summary>A review was stored or dismissed: badges should refresh.</summary>
    public event EventHandler? SuggestionsChanged;

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

        // Run exactly as written: its own rest times (not the profile's), its own rest days when the source gives them,
        // and no deloads or periodization changing its weeks (they can be switched on in its training options).
        var plan = new WorkoutPlan
        {
            Name = response.Name,
            Description = response.Description,
            Goal = answers.Goal,
            OwnTraining = true,
            Deloads = false,
            Periodization = false,
        };
        plan.Workouts = ToWorkouts(response, answers, [], keepRest: true);
        if (plan.Workouts.Count == 0)
            throw new InvalidOperationException("The imported plan has no workouts.");
        plan.DaysPerWeek = plan.Workouts.Count;
        var days = plan.Workouts.Count + response.RestDays.Count;
        plan.RestDays = response.RestDays.Count > 0 && days <= PlanLimits.MaxDays && response.RestDays.All(d => d >= 0 && d < days)
            ? [.. response.RestDays.Distinct().Order()]
            : [.. PlanSchedule.DefaultRestDays(plan.Workouts.Count).Order()];
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

    /// <summary>Finished workouts of <paramref name="plan"/>, newest first: what a review has to go on.</summary>
    public List<WorkoutSession> SessionsOf(WorkoutPlan plan) => [.. store.History.Where(s => s.PlanId == plan.Id)];

    /// <summary>The last review of <paramref name="plan"/> on this device, so reopening it doesn't spend another request.</summary>
    public (PlanReviewResponse Review, DateTime At)? LastReview(WorkoutPlan plan)
    {
        var json = Preferences.Default.Get(Key(plan, "json"), "");
        if (json.Length == 0)
            return null;
        try
        {
            var review = System.Text.Json.JsonSerializer.Deserialize(json, ReviewJson);
            return review == null ? null : (review, new DateTime(Preferences.Default.Get(Key(plan, "at"), 0L)));
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// How many suggestions that change the plan are waiting for the active plan: the weekly check found them and the
    /// user hasn't said "Next week" or applied them all. 0 for any other plan, which only gets reviews when asked.
    /// </summary>
    public int PendingSuggestions(WorkoutPlan plan)
    {
        if (plan.Id != store.Data.ActivePlanId || !IsAvailable || Preferences.Default.Get(Key(plan, "dismissed"), false))
            return 0;
        return LastReview(plan)?.Review.Suggestions.Count(s => s.Changes.Count > 0) ?? 0;
    }

    /// <summary>"Next week", or everything applied: no badge until the next weekly check finds something.</summary>
    public void Dismiss(WorkoutPlan plan)
    {
        Preferences.Default.Set(Key(plan, "dismissed"), true);
        SuggestionsChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>The AI's suggestions for <paramref name="plan"/>, from a summary of the workouts logged on it.</summary>
    public async Task<PlanReviewResponse> ReviewAsync(WorkoutPlan plan, CancellationToken ct = default)
    {
        var answers = AnswersFor(plan);
        var request = Fill(new PlanReviewRequest(), answers);
        request.Plan = ToResponse(plan);
        request.Exercises = Candidates(answers, plan);
        request.Performance = Performance(plan);

        var response = await api.ReviewPlanAsync(request, ct);
        ChatQuota = response.Quota ?? ChatQuota;
        Store(plan, response);
        return response;
    }

    /// <summary>
    /// The weekly check of the active plan, run when the app opens or comes back: at most once a week, only with
    /// workouts logged since the last one, and quietly (a failure just waits for a later try). Any suggestions that
    /// change the plan show up as a badge.
    /// </summary>
    public async Task CheckActivePlanAsync()
    {
        await account.EnsureLoadedAsync();
        // Unverified accounts are refused by the API anyway.
        if (_checking || !IsAvailable || account.NeedsEmailVerification || store.ActivePlan is not { } plan)
            return;
        if (_lastTry.TryGetValue(plan.Id, out var tried) && DateTime.Now - tried < RetryInterval)
            return;
        var sessions = SessionsOf(plan).Count;
        if (sessions < MinSessionsForCheck)
            return;
        if (LastReview(plan) is { } last && (DateTime.Now - last.At < CheckInterval || sessions <= Preferences.Default.Get(Key(plan, "sessions"), 0)))
            return;

        _checking = true;
        _lastTry[plan.Id] = DateTime.Now;
        try
        {
            await ReviewAsync(plan);
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException or ApiException or SessionExpiredException)
        {
        }
        finally
        {
            _checking = false;
        }
    }

    /// <summary>Forgets every stored review, e.g. on signing out: they belong to the account.</summary>
    public void ClearReviews()
    {
        foreach (var id in Preferences.Default.Get(ReviewedPlansKey, "").Split(',', StringSplitOptions.RemoveEmptyEntries))
            foreach (var part in new[] { "json", "at", "sessions", "dismissed" })
                Preferences.Default.Remove($"{ReviewKey}{id}.{part}");
        Preferences.Default.Remove(ReviewedPlansKey);
        _lastTry.Clear();
        SuggestionsChanged?.Invoke(this, EventArgs.Empty);
    }

    void Store(WorkoutPlan plan, PlanReviewResponse review)
    {
        Preferences.Default.Set(Key(plan, "json"), System.Text.Json.JsonSerializer.Serialize(review, ReviewJson));
        Preferences.Default.Set(Key(plan, "at"), DateTime.Now.Ticks);
        Preferences.Default.Set(Key(plan, "sessions"), SessionsOf(plan).Count);
        // A new review is new news, even if the last one was dismissed.
        Preferences.Default.Set(Key(plan, "dismissed"), false);
        var ids = Preferences.Default.Get(ReviewedPlansKey, "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        if (ids.Add(plan.Id))
            Preferences.Default.Set(ReviewedPlansKey, string.Join(',', ids));
        SuggestionsChanged?.Invoke(this, EventArgs.Empty);
    }

    static string Key(WorkoutPlan plan, string part) => $"{ReviewKey}{plan.Id}.{part}";

    static System.Text.Json.Serialization.Metadata.JsonTypeInfo<PlanReviewResponse> ReviewJson =>
        (System.Text.Json.Serialization.Metadata.JsonTypeInfo<PlanReviewResponse>)GymBook.Serialization.GymBookJson.Options.GetTypeInfo(typeof(PlanReviewResponse));

    /// <summary>
    /// What the AI needs to know about the training on the plan: attendance, workout length, weekly volume per muscle,
    /// and the recent sets of each of its exercises (on any plan, since the same lift carries over).
    /// </summary>
    PlanPerformance Performance(WorkoutPlan plan)
    {
        var sessions = SessionsOf(plan);
        var weeks = sessions.Count == 0 ? 0 : Math.Max(1, (int)Math.Ceiling((DateTime.Now - sessions[^1].StartedAt).TotalDays / 7));
        var recent = sessions.Take(10).ToList();
        var from = DateTime.Today.AddDays(-28);

        var ids = plan.Workouts.SelectMany(w => w.Exercises).Select(e => e.ExerciseId).Distinct().Take(PlanReviewLimits.MaxExercises);
        var history = ids.Select(id => new ExerciseHistory
        {
            ExerciseId = id,
            Sessions = [.. store.History
                .SelectMany(s => s.Exercises.Where(e => e.ExerciseId == id && e.Sets.Any(x => !x.IsWarmup)).Take(1).Select(e => (s, e)))
                .Take(PlanReviewLimits.MaxSessionsPerExercise)
                .Select(x => new ExerciseSessionSummary
                {
                    DaysAgo = Math.Max(0, (DateTime.Today - x.s.StartedAt.Date).Days),
                    RepMin = x.e.RepMin,
                    RepMax = x.e.RepMax,
                    TargetRir = x.e.TargetRir,
                    SkippedSets = Math.Min(100, x.e.Sets.Count(set => !set.IsWarmup && !set.IsCompleted)),
                    Sets = [.. x.e.Sets.Where(set => set.IsCompleted && !set.IsWarmup).Take(PlanReviewLimits.MaxSetsPerSession).Select(set => new LoggedSet
                    {
                        WeightKg = Math.Clamp(Math.Round(set.WeightKg, 2), 0, 2000),
                        Reps = Math.Clamp(set.Reps, 0, 1000),
                        Rir = set.Rir is { } rir ? Math.Clamp(rir, 0, 10) : null,
                    })],
                })],
        }).ToList();

        return new PlanPerformance
        {
            WeeksOnPlan = Math.Min(weeks, 1000),
            WorkoutsDone = Math.Min(sessions.Count, 10000),
            WorkoutsPlanned = Math.Min(weeks * plan.Workouts.Count, 10000),
            AverageSessionMinutes = recent.Count == 0 ? null : Math.Clamp((int)recent.Average(s => s.Duration.TotalMinutes), 0, 600),
            WeeklySets = [.. stats.SetsPerMuscleSince(from).Where(m => m.Value > 0)
                .Select(m => new MuscleVolume { Muscle = m.Key, Sets = Math.Min(200, Math.Round(m.Value / 4, 1)) })],
            History = history,
        };
    }

    /// <summary>
    /// Makes the edits of <paramref name="suggestion"/> to <paramref name="plan"/> (the caller saves). Edits that no
    /// longer fit, e.g. the plan was changed since the review, are skipped. Returns how many were made.
    /// </summary>
    public int Apply(WorkoutPlan plan, PlanSuggestion suggestion)
    {
        var applied = 0;
        foreach (var c in suggestion.Changes)
        {
            if (c.WorkoutIndex < 0 || c.WorkoutIndex >= plan.Workouts.Count)
                continue;
            var workout = plan.Workouts[c.WorkoutIndex];
            var current = workout.Exercises.FirstOrDefault(e => e.ExerciseId == c.ExerciseId);
            var incoming = store.GetExercise(c.NewExerciseId);
            var alreadyThere = workout.Exercises.Any(e => e.ExerciseId == c.NewExerciseId);
            switch (c.Action)
            {
                case PlanChangeActions.Update when current != null:
                    SetNumbers(current, c);
                    break;
                case PlanChangeActions.Replace when current != null && incoming != null && !alreadyThere:
                    current.ExerciseId = incoming.Id;
                    SetNumbers(current, c);
                    break;
                case PlanChangeActions.Add when incoming != null && !alreadyThere:
                    // Anything the AI left out comes from the goal's usual prescription.
                    var added = TrainingGoals.Prescription(plan.Goal, store.Profile.Experience, incoming, store.Profile);
                    SetNumbers(added, c);
                    workout.Exercises.Add(added);
                    break;
                case PlanChangeActions.Remove when current != null:
                    workout.Exercises.Remove(current);
                    break;
                default:
                    continue;
            }
            applied++;
        }
        return applied;
    }

    static void SetNumbers(PlanExercise e, PlanChange c)
    {
        if (c.Sets > 0)
            e.Sets = c.Sets;
        if (c.RepMin > 0)
            e.RepMin = c.RepMin;
        if (c.RepMax > 0)
            e.RepMax = c.RepMax;
        e.RepMax = Math.Max(e.RepMax, e.RepMin);
        if (c.TargetRir >= 0)
            e.TargetRir = c.TargetRir;
        if (c.RestSeconds > 0)
            (e.RestSeconds, e.CustomRest) = (c.RestSeconds, true);
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
    /// <paramref name="keepRest"/>: an imported plan keeps the rest times it came with.
    /// </summary>
    List<PlanWorkout> ToWorkouts(GeneratePlanResponse response, UserProfile answers, List<PlanWorkout> previous, bool keepRest = false)
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
                var rest = previous.Count == 0 && !keepRest ? TrainingGoals.RestOverride(answers, ex) ?? e.RestSeconds : e.RestSeconds;
                workout.Exercises.Add(new PlanExercise
                {
                    ExerciseId = ex.Id,
                    Sets = e.Sets,
                    RepMin = e.RepMin,
                    RepMax = e.RepMax,
                    TargetRir = e.TargetRir,
                    RestSeconds = rest,
                    // An imported plan's rest times are as written: each exercise keeps its own.
                    CustomRest = keepRest,
                    CustomRir = keepRest,
                });
            }
            if (workout.Exercises.Count > 0)
                workouts.Add(workout);
        }
        return workouts;
    }
}

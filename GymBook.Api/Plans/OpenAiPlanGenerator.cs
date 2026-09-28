using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using GymBook.Contracts;
using GymBook.Models;

namespace GymBook.Api.Plans;

public class OpenAiOptions
{
    /// <summary>OpenAI__ApiKey in .env. Unset turns generation off.</summary>
    public string? ApiKey { get; set; }
    public string Model { get; set; } = "gpt-5-mini";
    /// <summary>Sent as reasoning_effort for reasoning models; leave empty for models that don't take it.</summary>
    public string? ReasoningEffort { get; set; } = "low";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1/";
    public int TimeoutSeconds { get; set; } = 90;
}

/// <summary>The AI couldn't produce a usable plan; <see cref="Exception.Message"/> is safe to show.</summary>
public class PlanGenerationException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>
/// Builds a workout plan with ChatGPT from the wizard's answers. The model only picks from the exercises the app
/// sent, and everything it returns is checked and clamped before it goes back, so a bad answer can't produce a
/// broken plan.
/// </summary>
public class OpenAiPlanGenerator(HttpClient http, OpenAiOptions options, ILogger<OpenAiPlanGenerator> logger)
{
    public bool IsConfigured => !string.IsNullOrWhiteSpace(options.ApiKey);

    public async Task<GeneratePlanResponse> GenerateAsync(GeneratePlanRequest request, CancellationToken ct)
    {
        var body = new JsonObject
        {
            ["model"] = options.Model,
            ["messages"] = new JsonArray(
                new JsonObject { ["role"] = "system", ["content"] = SystemPrompt },
                new JsonObject { ["role"] = "user", ["content"] = UserPrompt(request) }),
            ["response_format"] = new JsonObject
            {
                ["type"] = "json_schema",
                ["json_schema"] = new JsonObject { ["name"] = "workout_plan", ["strict"] = true, ["schema"] = Schema() },
            },
        };
        if (!string.IsNullOrWhiteSpace(options.ReasoningEffort))
            body["reasoning_effort"] = options.ReasoningEffort;

        using var message = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
        {
            Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);

        string content;
        try
        {
            using var response = await http.SendAsync(message, ct);
            var text = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("OpenAI returned {Status}: {Body}", (int)response.StatusCode, Truncate(text, 2000));
                throw new PlanGenerationException("The plan generator is unavailable right now. Please try again later.");
            }
            var json = JsonNode.Parse(text);
            var choice = json?["choices"]?[0];
            if (choice?["message"]?["refusal"]?.GetValue<string>() is { Length: > 0 } refusal)
            {
                logger.LogWarning("OpenAI refused the plan: {Refusal}", refusal);
                throw new PlanGenerationException("The plan generator couldn't make a plan for these answers.");
            }
            content = choice?["message"]?["content"]?.GetValue<string>() ?? throw new PlanGenerationException("The plan generator sent an empty answer.");
            logger.LogInformation("Generated a plan with {Model}; tokens {Usage}", options.Model, json?["usage"]?["total_tokens"]?.ToString() ?? "?");
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException && !ct.IsCancellationRequested)
        {
            logger.LogWarning(e, "Calling OpenAI failed");
            throw new PlanGenerationException("The plan generator didn't answer in time. Please try again.", e);
        }

        GeneratePlanResponse? plan;
        try
        {
            plan = JsonSerializer.Deserialize<GeneratePlanResponse>(content, JsonOptions);
        }
        catch (JsonException e)
        {
            logger.LogWarning(e, "OpenAI sent a plan that isn't valid JSON: {Content}", Truncate(content, 2000));
            throw new PlanGenerationException("The plan generator sent a plan that couldn't be read.", e);
        }
        return Sanitize(plan ?? throw new PlanGenerationException("The plan generator sent an empty plan."), request);
    }

    static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    const string SystemPrompt = """
        You are an experienced strength and conditioning coach writing a weekly gym plan for the GymBook app.
        Rules:
        - Make exactly the requested number of workouts, one per training day of the week, in the order they should be done.
          Choose a split that suits the number of days and the experience (e.g. full body for 2-3 days, upper/lower for 4,
          push/pull/legs for 6), and balance the muscle groups across the week.
        - Use only exercises from the provided list, referring to them by their exact id. Never repeat an exercise within a workout.
          Where a split repeats (e.g. Upper A / Upper B), vary the exercises between the two days.
        - Fit each workout into the session length, counting warm-ups, sets and rest: roughly 4 exercises for 30 minutes,
          5-6 for 45-60 minutes, up to 8 for 90 minutes. Start each workout with the most demanding compound lifts.
        - Match sets, rep range (repMin-repMax), reps in reserve (targetRir, 0-4) and rest to the goal and experience:
          strength and power favour heavy compounds with low reps and long rest; muscle building uses moderate reps;
          fat loss and general fitness use higher reps and shorter rest. Beginners get fewer sets and more reps in reserve.
        - For the power goal, open each workout with an explosive movement if the list has one.
        - Only include neck exercises when neck training is requested; then add one or two to most non-leg days.
        - Name each workout briefly (e.g. "Upper A", "Push", "Full Body B"). Give the plan a short name (at most 40 characters)
          and a one-sentence description of who it's for and how it's structured.
        """;

    static string UserPrompt(GeneratePlanRequest r)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Goal: {r.Goal.Display()}");
        sb.AppendLine($"Experience: {r.Experience.Display()}");
        sb.AppendLine($"Training days per week: {r.DaysPerWeek}");
        sb.AppendLine($"Session length: {r.SessionMinutes} minutes");
        sb.AppendLine($"Equipment: {r.EquipmentAccess.Display()}");
        sb.AppendLine($"Train the neck: {(r.TrainNeck ? "yes" : "no")}");
        if (r.Age is { } age)
            sb.AppendLine($"Age: {age}");
        if (r.BodyWeightKg is { } kg)
            sb.AppendLine($"Body weight: {kg:0.#} kg");
        if (r.TrainingYears is { } years)
            sb.AppendLine($"Training for: {years:0.#} years");
        sb.AppendLine();
        sb.AppendLine("Available exercises (id | name | primary muscle | mechanic | equipment):");
        foreach (var e in r.Exercises)
            sb.AppendLine($"{e.Id} | {e.Name} | {e.PrimaryMuscle} | {e.Mechanic} | {e.Equipment}");
        return sb.ToString();
    }

    static JsonObject Schema()
    {
        static JsonObject Obj(params (string Name, JsonNode Type)[] properties) => new()
        {
            ["type"] = "object",
            ["additionalProperties"] = false,
            ["required"] = new JsonArray([.. properties.Select(p => (JsonNode)p.Name)]),
            ["properties"] = new JsonObject(properties.Select(p => KeyValuePair.Create(p.Name, (JsonNode?)p.Type))),
        };
        static JsonObject Str() => new() { ["type"] = "string" };
        static JsonObject Int() => new() { ["type"] = "integer" };
        static JsonObject Arr(JsonNode items) => new() { ["type"] = "array", ["items"] = items };

        var exercise = Obj(("exerciseId", Str()), ("sets", Int()), ("repMin", Int()), ("repMax", Int()), ("targetRir", Int()), ("restSeconds", Int()));
        var workout = Obj(("name", Str()), ("exercises", Arr(exercise)));
        return Obj(("name", Str()), ("description", Str()), ("workouts", Arr(workout)));
    }

    /// <summary>Drops unknown or repeated exercises, clamps the numbers to sane ranges, and fits the names to the sync limits.</summary>
    static GeneratePlanResponse Sanitize(GeneratePlanResponse plan, GeneratePlanRequest request)
    {
        var known = request.Exercises.Select(e => e.Id).ToHashSet(StringComparer.Ordinal);
        var workouts = new List<GeneratedWorkout>();
        foreach (var w in plan.Workouts.Take(request.DaysPerWeek))
        {
            var used = new HashSet<string>(StringComparer.Ordinal);
            var exercises = new List<GeneratedExercise>();
            foreach (var e in w.Exercises)
            {
                if (!known.Contains(e.ExerciseId) || !used.Add(e.ExerciseId) || exercises.Count >= PlanLimits.MaxExercisesPerDay)
                    continue;
                var repMin = Math.Clamp(e.RepMin, 1, 50);
                exercises.Add(new GeneratedExercise
                {
                    ExerciseId = e.ExerciseId,
                    Sets = Math.Clamp(e.Sets, 1, 10),
                    RepMin = repMin,
                    RepMax = Math.Clamp(e.RepMax, repMin, 60),
                    TargetRir = Math.Clamp(e.TargetRir, 0, 5),
                    // In 15-second steps, like the app's rest picker.
                    RestSeconds = Math.Clamp((int)Math.Round(e.RestSeconds / 15.0) * 15, 15, 600),
                });
            }
            if (exercises.Count > 0)
                workouts.Add(new GeneratedWorkout { Name = Fit(w.Name, SyncLimits.NameLength, $"Day {workouts.Count + 1}"), Exercises = exercises });
        }
        if (workouts.Count < request.DaysPerWeek)
            throw new PlanGenerationException("The plan generator made an incomplete plan. Please try again.");

        return new GeneratePlanResponse
        {
            Name = Fit(plan.Name, 60, "My plan"),
            Description = Fit(plan.Description, 300, ""),
            Workouts = workouts,
        };
    }

    static string Fit(string? value, int max, string fallback)
    {
        var text = string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        return text.Length <= max ? text : text[..max].TrimEnd();
    }

    static string Truncate(string text, int max) => text.Length <= max ? text : text[..max] + "…";
}

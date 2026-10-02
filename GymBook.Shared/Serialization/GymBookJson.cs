using System.Text.Json;
using System.Text.Json.Serialization;
using GymBook.Contracts;
using GymBook.Models;

namespace GymBook.Serialization;

/// <summary>The JSON settings used on the wire. The API and the app both call <see cref="Configure"/> so they always agree.</summary>
public static class GymBookJson
{
    public static JsonSerializerOptions Options { get; } = Create();

    public static void Configure(JsonSerializerOptions options)
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.PropertyNameCaseInsensitive = true;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        // A null where the model says non-null (e.g. "plans": null) is rejected instead of reaching code that
        // assumes it can't happen.
        options.RespectNullableAnnotations = true;
        options.Converters.Add(new JsonStringEnumConverter());
        options.Converters.Add(new WallClockDateTimeConverter());
        options.TypeInfoResolverChain.Insert(0, SharedJsonContext.Default);
    }

    static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        Configure(options);
        options.MakeReadOnly();
        return options;
    }
}

[JsonSerializable(typeof(SyncRequest))]
[JsonSerializable(typeof(SyncResponse))]
[JsonSerializable(typeof(RegisterRequest))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(RefreshRequest))]
[JsonSerializable(typeof(DeleteAccountRequest))]
[JsonSerializable(typeof(GoogleSignInRequest))]
[JsonSerializable(typeof(ForgotPasswordRequest))]
[JsonSerializable(typeof(ResetPasswordRequest))]
[JsonSerializable(typeof(ChangePasswordRequest))]
[JsonSerializable(typeof(ChangeEmailRequest))]
[JsonSerializable(typeof(ConfirmEmailChangeRequest))]
[JsonSerializable(typeof(VerifyEmailRequest))]
[JsonSerializable(typeof(AuthResponse))]
[JsonSerializable(typeof(AccountResponse))]
[JsonSerializable(typeof(ApiProblem))]
[JsonSerializable(typeof(GeneratePlanRequest))]
[JsonSerializable(typeof(GeneratePlanResponse))]
[JsonSerializable(typeof(PlanQuotaResponse))]
[JsonSerializable(typeof(PlanAnswers))]
[JsonSerializable(typeof(PlanQuestionsResponse))]
[JsonSerializable(typeof(PlanChatRequest))]
[JsonSerializable(typeof(PlanChatResponse))]
[JsonSerializable(typeof(PlanReviewRequest))]
[JsonSerializable(typeof(PlanReviewResponse))]
[JsonSerializable(typeof(ImportPlanRequest))]
[JsonSerializable(typeof(WorkoutSession))]
[JsonSerializable(typeof(WorkoutPlan))]
[JsonSerializable(typeof(Exercise))]
[JsonSerializable(typeof(BodyWeightEntry))]
[JsonSerializable(typeof(FoodEntry))]
[JsonSerializable(typeof(HealthDay))]
[JsonSerializable(typeof(SupplementDose))]
[JsonSerializable(typeof(UserProfile))]
[JsonSerializable(typeof(DashboardResponse))]
[JsonSerializable(typeof(List<UserRow>))]
[JsonSerializable(typeof(UserDetail))]
[JsonSerializable(typeof(AiUsageResponse))]
[JsonSerializable(typeof(SetRoleRequest))]
[JsonSerializable(typeof(SetDisabledRequest))]
[JsonSerializable(typeof(SetEmailVerifiedRequest))]
[JsonSerializable(typeof(WearWorkout))]
[JsonSerializable(typeof(WearCompleteSet))]
[JsonSerializable(typeof(WearSession))]
internal partial class SharedJsonContext : JsonSerializerContext;

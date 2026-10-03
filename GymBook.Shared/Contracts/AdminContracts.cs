namespace GymBook.Contracts;

// What the admin app (GymBook.Admin) sends to and receives from the API's /api/admin endpoints. Weights are in kg; the
// app converts for display.

public record DashboardResponse(
    int TotalUsers,
    int VerifiedUsers,
    int NewUsers7d,
    int NewUsers30d,
    int ActiveUsers7d,
    int DisabledUsers,
    int Admins,
    int WorkoutsTotal,
    int Workouts7d,
    int PlansTotal,
    int AiPlans7d,
    int AiChats7d,
    List<DayCount> SignupsByDay,
    List<DayCount> WorkoutsByDay,
    List<UserRow> RecentUsers,
    List<TopUser> MostActive7d);

public record DayCount(DateOnly Date, int Count);

public record TopUser(string Id, string Email, int Count);

public record UserRow(
    string Id,
    string Email,
    string? Name,
    DateTimeOffset CreatedAt,
    bool EmailVerified,
    bool HasGoogle,
    string? Role,
    string Status,
    int Workouts,
    int Plans,
    DateTimeOffset? LastActiveAt);

public record UserDetail(
    UserRow User,
    bool HasPassword,
    int ActiveSessions,
    ProfileInfo? Profile,
    TrainingStats Stats,
    List<PlanRow> Plans,
    List<WorkoutRow> RecentWorkouts,
    AiUsage Ai);

public record ProfileInfo(
    string Name,
    string Goal,
    string Experience,
    int DaysPerWeek,
    int SessionMinutes,
    string EquipmentAccess,
    string Unit,
    double BodyWeightKg,
    int? BirthYear,
    double? BodyFatPercent,
    DateTime? TrainingSince,
    bool OnboardingDone,
    DateTimeOffset UpdatedAt);

public record TrainingStats(
    int Workouts,
    int Workouts30d,
    int WorkingSets,
    double VolumeKg,
    double TotalHours,
    DateTime? FirstWorkoutAt,
    DateTime? LastWorkoutAt,
    int CustomExercises,
    int BodyWeightEntries,
    double? LatestBodyWeightKg,
    List<WeekCount> WorkoutsByWeek);

public record WeekCount(DateOnly WeekStart, int Count);

public record PlanRow(string Id, string Name, string Goal, int DaysPerWeek, int Days, int Exercises, bool IsActive, DateTime CreatedAt);

public record WorkoutRow(
    string Id,
    string Name,
    DateTime StartedAt,
    DateTime? EndedAt,
    int Exercises,
    int WorkingSets,
    double VolumeKg,
    int? PlanWeek);

public record AiUsage(QuotaUse Plan, QuotaUse Chat, int Plans30d, int Chats30d);

public record QuotaUse(int Used, int Limit, string Period);

public record AiUsageResponse(
    int Days,
    int Plans,
    int Chats,
    int Users,
    QuotaSetting PlanQuota,
    QuotaSetting ChatQuota,
    List<AiDay> ByDay,
    List<AiTopUser> TopUsers);

public record AiDay(DateOnly Date, int Plans, int Chats);

public record AiTopUser(string Id, string Email, int Plans, int Chats);

public record QuotaSetting(int Limit, string Period);

public record SetRoleRequest(string? Role);

public record SetDisabledRequest(bool Disabled);

public record SetEmailVerifiedRequest(bool Verified);

/// <summary>A report about someone's ranked lift or public profile, newest first in the admin app.</summary>
/// <param name="OpenAboutTarget">Open reports about the same user, this one included.</param>
public record RankReportRow(
    Guid Id,
    string Reporter,
    string TargetUserId,
    string TargetUsername,
    string TargetEmail,
    string? Lift,
    string Reason,
    DateTimeOffset CreatedAt,
    bool TargetHidden,
    int OpenAboutTarget);

public record SetRankHiddenRequest(bool Hidden);

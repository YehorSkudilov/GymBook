using System.ComponentModel.DataAnnotations;
using GymBook.Models;

namespace GymBook.Contracts;

// The social side of Gym Book: a public profile (username, picture, bio), strength ranks, friends and shared plans.
// Nothing here shows a user's email or their private profile; the username is the only name others see.

public static class SocialLimits
{
    public const int UsernameMinLength = 3;
    public const int UsernameMaxLength = 20;
    /// <summary>Letters, digits, underscores and dots.</summary>
    public const string UsernamePattern = "^[A-Za-z0-9_.]+$";
    public const int BioLength = 160;
    public const int HomeGymLength = 60;
    public const int ReportReasonLength = 300;
    /// <summary>The picture as sent: the app shrinks it to a small square JPEG first, far under this.</summary>
    public const int AvatarBytes = 512 * 1024;
    public const int AvatarBase64Length = (AvatarBytes + 2) / 3 * 4;
    public const int FriendCodeLength = 8;
    public const int LeaderboardSize = 100;
    public const int MaxFriends = 200;
    public const int MaxShareMembers = 50;
}

/// <summary>The signed-in user's public profile. <see cref="Exists"/> is false until they pick a username.</summary>
public class MyPublicProfile
{
    public bool Exists { get; set; }
    public string Username { get; set; } = "";
    public string Bio { get; set; } = "";
    public string HomeGym { get; set; } = "";
    /// <summary>Path on the API (see <see cref="ProfileCard.AvatarPath"/>); null without a picture.</summary>
    public string? AvatarPath { get; set; }
    /// <summary>Given to friends so they can add this user.</summary>
    public string FriendCode { get; set; } = "";
    public bool InRanks { get; set; }
    /// <summary>The sex the user ranks against; null until they join the ranks.</summary>
    public Sex? Sex { get; set; }
    /// <summary>Taken off the leaderboards by an admin after reports.</summary>
    public bool RankHidden { get; set; }
}

public class SavePublicProfileRequest
{
    [Required, MinLength(SocialLimits.UsernameMinLength), MaxLength(SocialLimits.UsernameMaxLength), RegularExpression(SocialLimits.UsernamePattern, ErrorMessage = "A username can only have letters, numbers, dots and underscores.")]
    public string Username { get; set; } = "";
    [MaxLength(SocialLimits.BioLength)]
    public string Bio { get; set; } = "";
    [MaxLength(SocialLimits.HomeGymLength)]
    public string HomeGym { get; set; } = "";
}

public class SetAvatarRequest
{
    /// <summary>A JPEG or PNG, base64.</summary>
    [Required, MaxLength(SocialLimits.AvatarBase64Length)]
    public string ImageBase64 { get; set; } = "";
}

public class JoinRanksRequest
{
    /// <summary>Which strength standards to rank against.</summary>
    public Sex Sex { get; set; }
}

/// <summary>Another user as anyone may see them.</summary>
public class ProfileCard
{
    public string Username { get; set; } = "";
    public string Bio { get; set; } = "";
    public string HomeGym { get; set; } = "";
    /// <summary>The picture, relative to the API's address ("api/social/avatars/name?v=3"); null without one.</summary>
    public string? AvatarPath { get; set; }
    /// <summary>Their overall strength, when they're in the ranks.</summary>
    public RankTier? Tier { get; set; }
    public double? Score { get; set; }
    public List<LiftRank> Lifts { get; set; } = [];
    public bool IsFriend { get; set; }
    public bool IsMe { get; set; }
}

/// <summary>One lift of a ranked user. Unranked until it's in <see cref="Ranks.MinSessions"/> workouts.</summary>
public class LiftRank
{
    public RankLift Lift { get; set; }
    public bool Ranked { get; set; }
    public double E1RmKg { get; set; }
    public double BodyWeightKg { get; set; }
    public double Ratio { get; set; }
    public double Score { get; set; }
    public RankTier Tier { get; set; }
    /// <summary>The set it's from.</summary>
    public double WeightKg { get; set; }
    public int Reps { get; set; }
    public DateTime? Date { get; set; }
    /// <summary>Workouts in the last year with the lift.</summary>
    public int Sessions { get; set; }
}

/// <summary>The signed-in user's standing.</summary>
public class RankStatusResponse
{
    public bool InRanks { get; set; }
    public double Score { get; set; }
    public RankTier Tier { get; set; }
    /// <summary>Place on the global board; null when not on it (no ranked lift yet, or hidden).</summary>
    public int? Position { get; set; }
    public int Competitors { get; set; }
    public List<LiftRank> Lifts { get; set; } = [];
}

public class LeaderboardEntry
{
    public int Position { get; set; }
    public string Username { get; set; } = "";
    public string? AvatarPath { get; set; }
    public double Score { get; set; }
    public RankTier Tier { get; set; }
    /// <summary>On a lift's board: the lift's estimated 1RM and its ratio to body weight.</summary>
    public double? E1RmKg { get; set; }
    public double? Ratio { get; set; }
    public bool IsMe { get; set; }
    public bool IsFriend { get; set; }
}

public class LeaderboardResponse
{
    public List<LeaderboardEntry> Entries { get; set; } = [];
    /// <summary>The signed-in user's own entry when it's below the top of the board.</summary>
    public LeaderboardEntry? Me { get; set; }
}

public class AddFriendRequest
{
    [Required, MaxLength(32)]
    public string Code { get; set; } = "";
}

public class UsernameRequest
{
    [Required, MaxLength(SocialLimits.UsernameMaxLength)]
    public string Username { get; set; } = "";
}

public class ReportRequest
{
    [Required, MaxLength(SocialLimits.UsernameMaxLength)]
    public string Username { get; set; } = "";
    /// <summary>The lift that looks wrong; null for the profile itself (a username, picture or bio).</summary>
    public RankLift? Lift { get; set; }
    [MaxLength(SocialLimits.ReportReasonLength)]
    public string Reason { get; set; } = "";
}

// ---- Shared plans ----

public class CreatePlanShareRequest
{
    [Required, MaxLength(SyncLimits.IdLength)]
    public string PlanId { get; set; } = "";
}

public class UpdatePlanShareRequest
{
    /// <summary>Anyone with the link can see the plan on the web, without the app or an account.</summary>
    public bool IsPublic { get; set; }
    /// <summary>People who can see the plan may save their own copy of it.</summary>
    public bool AllowCopy { get; set; } = true;
}

public class ShareInviteRequest
{
    [Required, MaxLength(SocialLimits.UsernameMaxLength)]
    public string Username { get; set; } = "";
    public PlanShareRole Role { get; set; } = PlanShareRole.Viewer;
}

public class PlanShareMember
{
    public string Username { get; set; } = "";
    public string? AvatarPath { get; set; }
    public PlanShareRole Role { get; set; }
    /// <summary>False while the invite hasn't been accepted.</summary>
    public bool Joined { get; set; }
}

/// <summary>A share as its owner and members see it.</summary>
public class PlanShareResponse
{
    public string Id { get; set; } = "";
    public string PlanName { get; set; } = "";
    public bool IsPublic { get; set; }
    public bool AllowCopy { get; set; }
    /// <summary>The web page anyone can open; null while the share isn't public.</summary>
    public string? PublicUrl { get; set; }
    public string Owner { get; set; } = "";
    public PlanShareRole MyRole { get; set; }
    public List<PlanShareMember> Members { get; set; } = [];
}

/// <summary>A plan someone shared with the signed-in user, waiting for them to accept.</summary>
public class PlanShareInvite
{
    public string ShareId { get; set; } = "";
    public string PlanName { get; set; } = "";
    public string Owner { get; set; } = "";
    public string? OwnerAvatarPath { get; set; }
    public PlanShareRole Role { get; set; }
    public int Workouts { get; set; }
}

public class SharedExerciseView
{
    public string ExerciseId { get; set; } = "";
    public string Name { get; set; } = "";
    public int Sets { get; set; }
    public int RepMin { get; set; }
    public int RepMax { get; set; }
    public int? TargetRir { get; set; }
    public int RestSeconds { get; set; }
}

public class SharedWorkoutView
{
    public string Name { get; set; } = "";
    public List<SharedExerciseView> Exercises { get; set; } = [];
}

/// <summary>A shared plan as it reads, with exercise names, for the web page and for previewing a link in the app.</summary>
public class SharedPlanView
{
    public string ShareId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public Goal Goal { get; set; }
    public int DaysPerWeek { get; set; }
    public List<int> RestDays { get; set; } = [];
    public List<SharedWorkoutView> Workouts { get; set; } = [];
    public ProfileCard? Owner { get; set; }
    public bool AllowCopy { get; set; }
    /// <summary>The viewer's part in it, when they're the owner or a member.</summary>
    public PlanShareRole? MyRole { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public class CopyPlanResponse
{
    /// <summary>The new plan in the user's own plans; it reaches the device with the next sync.</summary>
    public string PlanId { get; set; } = "";
    public string Name { get; set; } = "";
}

using System.Windows.Input;
using GymBook.Admin.Services;
using GymBook.Contracts;

namespace GymBook.Admin.ViewModels;

/// <summary>A user in a list: who, what they've done, and tags for anything unusual.</summary>
public class UserItem(UserRow row, ICommand open)
{
    public string Id => row.Id;
    public string Title => string.IsNullOrWhiteSpace(row.Name) ? row.Email : row.Name;
    public string Subtitle => string.IsNullOrWhiteSpace(row.Name) ? $"Joined {Format.Date(row.CreatedAt)}" : row.Email;
    public string Activity => $"{Format.Number(row.Workouts)} workouts · {Format.Number(row.Plans)} plans · active {Format.Ago(row.LastActiveAt)}";
    public IReadOnlyList<Badge> Badges { get; } = Badge.For(row);
    public ICommand OpenCommand => open;
}

/// <summary>A small coloured tag, e.g. "SuperAdmin" or "Disabled".</summary>
public record Badge(string Text, Color Color, Color Background)
{
    static readonly Color Accent = Color.FromArgb("#3F7DFF"), AccentSoft = Color.FromArgb("#1A2A4F");
    static readonly Color Warning = Color.FromArgb("#FFB020"), WarningSoft = Color.FromArgb("#3A2C10");
    static readonly Color Danger = Color.FromArgb("#FF4D5E"), DangerSoft = Color.FromArgb("#3A1419");
    static readonly Color Neutral = Color.FromArgb("#9AA3B5"), NeutralSoft = Color.FromArgb("#1D212C");

    public static IReadOnlyList<Badge> For(UserRow row)
    {
        var badges = new List<Badge>();
        if (row.Role != null)
            badges.Add(new(row.Role, Accent, AccentSoft));
        if (row.Status == "Disabled")
            badges.Add(new("Disabled", Danger, DangerSoft));
        else if (row.Status == "Locked")
            badges.Add(new("Locked out", Warning, WarningSoft));
        if (!row.EmailVerified)
            badges.Add(new("Unverified", Warning, WarningSoft));
        if (row.HasGoogle)
            badges.Add(new("Google", Neutral, NeutralSoft));
        return badges;
    }
}

/// <summary>A number on the dashboard with its label and an optional note underneath.</summary>
public record StatTile(string Label, string Value, string Note = "");

/// <summary>A label and its value, e.g. "Goal" / "Build muscle".</summary>
public record InfoRow(string Label, string Value);

/// <summary>A ranked user, e.g. most workouts this week. Tapping opens them.</summary>
public record RankedUser(string Id, string Email, string Value, ICommand OpenCommand);

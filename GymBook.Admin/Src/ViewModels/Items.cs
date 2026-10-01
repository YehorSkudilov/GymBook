using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using GymBook.Admin.Services;
using GymBook.Contracts;

namespace GymBook.Admin.ViewModels;

/// <summary>The app's colours, for view models that pick one (they match Resources/Styles/Colors.xaml).</summary>
public static class Palette
{
    public static readonly Color Accent = Color.FromArgb("#3F7DFF"), AccentSoft = Color.FromArgb("#1A2A4F");
    public static readonly Color Accent2 = Color.FromArgb("#7C5CFF"), Accent2Soft = Color.FromArgb("#2A2150");
    public static readonly Color Success = Color.FromArgb("#2ED47A"), SuccessSoft = Color.FromArgb("#133526");
    public static readonly Color Warning = Color.FromArgb("#FFB020"), WarningSoft = Color.FromArgb("#3A2C10");
    public static readonly Color Danger = Color.FromArgb("#FF4D5E"), DangerSoft = Color.FromArgb("#3A1419");
    public static readonly Color TextPrimary = Color.FromArgb("#F4F6FB"), TextSecondary = Color.FromArgb("#9AA3B5");
    public static readonly Color Surface2 = Color.FromArgb("#1D212C"), Surface3 = Color.FromArgb("#272C39");
}

/// <summary>A user in a list: their initial, who, what they've done, and tags for anything unusual.</summary>
public class UserItem(UserRow row, ICommand open)
{
    public string Id => row.Id;
    public string Initial => Initials.Of(row.Name, row.Email);
    public string Title => string.IsNullOrWhiteSpace(row.Name) ? row.Email : row.Name;
    public string Subtitle => string.IsNullOrWhiteSpace(row.Name) ? $"Joined {Format.Date(row.CreatedAt)}" : row.Email;
    public string Activity => $"{Format.Number(row.Workouts)} workouts · {Format.Number(row.Plans)} plans · active {Format.Ago(row.LastActiveAt)}";
    public IReadOnlyList<Badge> Badges { get; } = Badge.For(row);
    public bool HasBadges => Badges.Count > 0;
    public ICommand OpenCommand => open;
}

public static class Initials
{
    /// <summary>The first letter of the name, or of the email without one, as on the GymBook app's Profile.</summary>
    public static string Of(string? name, string email)
    {
        var source = string.IsNullOrWhiteSpace(name) ? email : name;
        return source.Length == 0 ? "?" : char.ToUpperInvariant(source.Trim()[0]).ToString();
    }
}

/// <summary>A small coloured tag, e.g. "SuperAdmin" or "Disabled".</summary>
public record Badge(string Text, Color Color, Color Background)
{
    public static IReadOnlyList<Badge> For(UserRow row)
    {
        var badges = new List<Badge>();
        if (row.Role != null)
            badges.Add(new(row.Role, Palette.Accent, Palette.AccentSoft));
        if (row.Status == "Disabled")
            badges.Add(new("Disabled", Palette.Danger, Palette.DangerSoft));
        else if (row.Status == "Locked")
            badges.Add(new("Locked out", Palette.Warning, Palette.WarningSoft));
        if (!row.EmailVerified)
            badges.Add(new("Unverified", Palette.Warning, Palette.WarningSoft));
        if (row.HasGoogle)
            badges.Add(new("Google", Palette.TextSecondary, Palette.Surface2));
        return badges;
    }
}

/// <summary>A number with its label and an optional note. With a command it's tappable (e.g. opens the matching users).</summary>
public record StatTile(string Label, string Value, string Note = "", ICommand? Command = null)
{
    public bool IsTappable => Command != null;
}

/// <summary>A label and its value, e.g. "Goal" / "Build muscle".</summary>
public record InfoRow(string Label, string Value);

/// <summary>A ranked user, e.g. most workouts this week. Tapping opens them.</summary>
public record RankedUser(string Id, string Email, string Value, ICommand OpenCommand)
{
    public string Initial => Initials.Of(null, Email);
}

/// <summary>An action on a user, drawn like the GymBook app's Profile rows: an icon on a soft tile, a title and what it does.</summary>
public record ActionItem(string Glyph, Color Color, Color Background, string Title, string Detail, ICommand Command)
{
    public Color TitleColor => Color == Palette.Danger ? Palette.Danger : Palette.TextPrimary;
}

/// <summary>One choice in a row of filter chips; the selected one is filled with the accent.</summary>
public partial class ChipItem(string title, string? value, ICommand select) : ObservableObject
{
    public string Title => title;

    /// <summary>What the API calls this filter; null for "everything".</summary>
    public string? Value => value;

    public ICommand SelectCommand => select;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Background), nameof(TextColor))]
    bool isSelected;

    public Color Background => IsSelected ? Palette.Accent : Palette.Surface2;
    public Color TextColor => IsSelected ? Colors.White : Palette.TextSecondary;
}

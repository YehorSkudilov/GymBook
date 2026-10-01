using System.Globalization;

namespace GymBook.Admin.Services;

/// <summary>How numbers and dates read in the app.</summary>
public static class Format
{
    static readonly CultureInfo Culture = CultureInfo.CurrentCulture;

    public static string Number(int value) => value.ToString("N0", Culture);

    public static string Number(double value) => value.ToString("N0", Culture);

    /// <summary>Training volume: kg up to ten tonnes, then tonnes.</summary>
    public static string Volume(double kg) => kg >= 10_000 ? $"{(kg / 1000).ToString("N1", Culture)} t" : $"{Number(kg)} kg";

    public static string Weight(double kg) => $"{kg.ToString("0.#", Culture)} kg";

    public static string Date(DateTimeOffset value) => value.ToLocalTime().ToString("d MMM yyyy", Culture);

    public static string Date(DateTime value) => value.ToString("d MMM yyyy", Culture);

    public static string Date(DateOnly value) => value.ToString("d MMM", Culture);

    public static string DateAndTime(DateTimeOffset value) => value.ToLocalTime().ToString("d MMM yyyy, HH:mm", Culture);

    public static string Hours(double hours) => $"{hours.ToString("0.#", Culture)} h";

    /// <summary>"just now", "5 min ago", "3 days ago", or the date for anything older than a month.</summary>
    public static string Ago(DateTimeOffset? value)
    {
        if (value is not { } when)
            return "never";
        var age = DateTimeOffset.UtcNow - when;
        return age.TotalMinutes switch
        {
            < 1 => "just now",
            < 60 => $"{(int)age.TotalMinutes} min ago",
            < 60 * 24 => $"{(int)age.TotalHours} h ago",
            < 60 * 24 * 2 => "yesterday",
            < 60 * 24 * 31 => $"{(int)age.TotalDays} days ago",
            _ => Date(when),
        };
    }

    public static string Ago(DateTime? value) => Ago(value is { } v ? new DateTimeOffset(v) : null);

    /// <summary>"3 / 5 this week" style quota use.</summary>
    public static string Quota(int used, int limit, string period) => $"{used} / {limit} {Period(period)}";

    /// <summary>The API's quota window ("hour", "day", "3 days") as "per hour", "per 3 days".</summary>
    public static string Period(string period) => $"per {period}";

    /// <summary>Enum-ish API values (e.g. "BuildMuscle") as words ("Build muscle").</summary>
    public static string Words(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;
        var text = new System.Text.StringBuilder(value.Length + 4);
        for (var i = 0; i < value.Length; i++)
        {
            var c = value[i];
            if (i > 0 && char.IsUpper(c) && !char.IsUpper(value[i - 1]))
                text.Append(' ').Append(char.ToLowerInvariant(c));
            else
                text.Append(c);
        }
        return text.ToString();
    }
}

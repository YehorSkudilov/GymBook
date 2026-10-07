namespace GymBook.Services;

/// <summary>
/// Pages on Gym Book's website (GymBook.Web) the app links to: the paywall's terms and privacy links, which the App Store
/// requires to work, and support.
/// </summary>
public static class SiteLinks
{
    const string Site = "https://gymbook.yehorskudilov.com";

    public static Uri Terms { get; } = new($"{Site}/terms/");
    public static Uri Privacy { get; } = new($"{Site}/privacy/");
    public static Uri Support { get; } = new($"{Site}/support/");
}

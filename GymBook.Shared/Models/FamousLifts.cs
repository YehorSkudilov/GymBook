namespace GymBook.Models;

/// <summary>
/// A famous person's best lift in competition: what a set is compared to. <paramref name="BodyweightKg"/> null when the
/// meet didn't record it. <paramref name="Movement"/> names it when it isn't quite the gym lift (a log press for the
/// overhead press).
/// </summary>
public record FamousLift(
    string Person, string Known, RankLift Lift, double Kg, double? BodyweightKg, string Event, DateOnly Date, string Source,
    string? Movement = null);

/// <summary>
/// Competition records only, each with where it's from, so nothing said about a real person is made up: bench, squat
/// and deadlift are raw (no suits or wraps) from OpenPowerlifting's meet results, the overhead press from Olympic
/// results (the press was contested until 1972) and strongman's log press. Who each person is says no more than is
/// well known. Checked October 2026; a lifter's best can have moved on since.
/// </summary>
public static class FamousLifts
{
    const string Opl = "https://www.openpowerlifting.org/u/";
    const string Munich = "https://en.wikipedia.org/wiki/Weightlifting_at_the_1972_Summer_Olympics_%E2%80%93_Men%27s_";

    static readonly (string Person, string Known, string Opl)[] People =
    [
        ("Heather Connor", "Powerlifter (47 kg class)", "heatherconnor"),
        ("Jessica Buettner", "Powerlifter", "jessicabuettner"),
        ("Marianna Gasparyan", "Powerlifter", "mariannagasparyan"),
        ("Amanda Lawrence", "Powerlifter", "amandalawrence1"),
        ("Jennifer Thompson", "Powerlifter, bench press specialist", "jenniferthompson1"),
        ("Alan Thrall", "Fitness YouTuber", "alanthrall"),
        ("Jeff Nippard", "Fitness YouTuber", "jeffnippard"),
        ("Arnold Schwarzenegger", "Bodybuilder and actor", "arnoldschwarzenegger"),
        ("Mike Israetel", "Sport scientist and YouTuber", "mikeisraetel"),
        ("Hafþór Júlíus Björnsson", "Strongman, \"The Mountain\" in Game of Thrones", "hafthorjuliusbjornsson"),
        ("Taylor Atwood", "Powerlifter", "tayloratwood"),
        ("Russel Orhii", "Powerlifter", "russelorhii"),
        ("Ray Williams", "Powerlifter", "raywilliams1"),
        ("John Haack", "Powerlifter", "johnhaack"),
        ("Stan Efferding", "Pro bodybuilder and powerlifter", "stanefferding"),
        ("Jesus Olivares", "Powerlifter", "jesusolivares"),
        ("Kirill Sarychev", "Powerlifter", "kirillsarychev"),
        ("Julius Maddox", "Bench press specialist", "juliusmaddox"),
        ("Jamal Browner", "Powerlifter", "jamalbrowner"),
        ("Blaine Sumner", "Powerlifter", "blainesumner"),
        ("Danny Grigsby", "Powerlifter", "dannygrigsby"),
    ];

    static FamousLift Meet(string person, RankLift lift, double kg, double? bodyweight, string meet, string date)
    {
        var p = People.First(x => x.Person == person);
        return new(p.Person, p.Known, lift, kg, bodyweight, meet, DateOnly.Parse(date), Opl + p.Opl);
    }

    /// <summary>Every lift, lightest first within each.</summary>
    public static readonly FamousLift[] All =
    [
        .. new[]
        {
            Meet("Heather Connor", RankLift.Bench, 77.5, 46.74, "IPF Sheffield Powerlifting Championships", "2026-01-31"),
            Meet("Jessica Buettner", RankLift.Bench, 112.5, 83, "IPF World Classic Powerlifting Championships", "2026-06-13"),
            Meet("Marianna Gasparyan", RankLift.Bench, 133, 58.5, "WRPF Nikolai Kagansky Memorial Cup", "2026-07-11"),
            Meet("Amanda Lawrence", RankLift.Bench, 137.5, 88.7, "AMP Rising Tide", "2025-08-16"),
            Meet("Jennifer Thompson", RankLift.Bench, 148.5, 67.55, "USAPL Carolina Primetime", "2022-10-15"),
            Meet("Alan Thrall", RankLift.Bench, 155, 109.65, "USPA Old Skool Iron Classic", "2018-05-19"),
            Meet("Jeff Nippard", RankLift.Bench, 160, 70.3, "CPU New Brunswick Provincials", "2015-07-13"),
            Meet("Arnold Schwarzenegger", RankLift.Bench, 165, null, "DBKV Internationale Meisterschaft Kraftdreikampf", "1966-10-30"),
            Meet("Mike Israetel", RankLift.Bench, 167.83, 82.1, "APA Southern States Battle of the Bad", "2004-02-27"),
            Meet("Hafþór Júlíus Björnsson", RankLift.Bench, 215, 180.8, "IKF meet, Iceland", "2010-09-22"),
            Meet("Taylor Atwood", RankLift.Bench, 218.5, 82.65, "NAPF Pan-American Powerlifting Championships", "2025-07-27"),
            Meet("Ray Williams", RankLift.Bench, 247.5, 190.4, "USAPL Arnold SBD Pro American", "2019-03-02"),
            Meet("John Haack", RankLift.Bench, 272.5, 95.6, "USPA Pro Raw Championships", "2022-09-24"),
            Meet("Stan Efferding", RankLift.Bench, 275, 124.74, "SPF California State Championships", "2011-05-15"),
            Meet("Jesus Olivares", RankLift.Bench, 281.5, 190.4, "AMP Avancus Houston", "2026-10-04"),
            Meet("Kirill Sarychev", RankLift.Bench, 335, 172.9, "WPC-RUS SN PRO Open Cup", "2015-11-21"),
            Meet("Julius Maddox", RankLift.Bench, 355, 203, "WRPF Hybrid Showdown III", "2021-02-20"),

            Meet("Arnold Schwarzenegger", RankLift.Squat, 140, null, "DBKV Internationale Meisterschaft Kraftdreikampf", "1966-10-30"),
            Meet("Heather Connor", RankLift.Squat, 150, 46.74, "IPF Sheffield Powerlifting Championships", "2026-01-31"),
            Meet("Jennifer Thompson", RankLift.Squat, 162.5, 65.92, "INTDFPA Open & Masters World Championships", "2025-12-11"),
            Meet("Mike Israetel", RankLift.Squat, 213.19, 98.88, "RAW Powerlifting National Championships", "2008-07-12"),
            Meet("Jessica Buettner", RankLift.Squat, 225, 83, "IPF World Classic Powerlifting Championships", "2026-06-13"),
            Meet("Jeff Nippard", RankLift.Squat, 227.5, 73.1, "CPU Canadian Championship", "2014-03-31"),
            Meet("Marianna Gasparyan", RankLift.Squat, 230.5, 58.5, "WRPF Nikolai Kagansky Memorial Cup", "2026-07-11"),
            Meet("Alan Thrall", RankLift.Squat, 242.5, 109.65, "USPA Old Skool Iron Classic", "2018-05-19"),
            Meet("Amanda Lawrence", RankLift.Squat, 256, 83.7, "USAPL Gainsgiving Open", "2020-11-21"),
            Meet("Taylor Atwood", RankLift.Squat, 317.5, 82.48, "AMP Classic Open Nationals", "2025-04-03"),
            Meet("Russel Orhii", RankLift.Squat, 356, 92.44, "IPF Sheffield Powerlifting Championships", "2026-01-31"),
            Meet("Jamal Browner", RankLift.Squat, 370, 109.4, "USPA Pro Raw Championships", "2022-09-24"),
            Meet("John Haack", RankLift.Squat, 380, 99.1, "PLU American Pro Invitational", "2026-08-08"),
            Meet("Stan Efferding", RankLift.Squat, 387.5, 124.74, "SPF California State Championships", "2011-05-15"),
            Meet("Blaine Sumner", RankLift.Squat, 415, 166.65, "PA Pacific Invitational II", "2015-09-12"),
            Meet("Jesus Olivares", RankLift.Squat, 485, 190.4, "AMP Avancus Houston", "2026-10-04"),
            Meet("Ray Williams", RankLift.Squat, 490, 190.4, "USAPL Arnold SBD Pro American", "2019-03-02"),

            Meet("Jennifer Thompson", RankLift.Deadlift, 207.5, 62.3, "USAPL Raw Nationals", "2018-10-11"),
            Meet("Heather Connor", RankLift.Deadlift, 212.5, 46.85, "IPF World Classic Powerlifting Championships", "2025-06-08"),
            Meet("Marianna Gasparyan", RankLift.Deadlift, 217.5, 57.7, "USPA The Tribute", "2019-08-03"),
            Meet("Mike Israetel", RankLift.Deadlift, 226.8, 98.88, "RAW Powerlifting National Championships", "2008-07-12"),
            Meet("Jeff Nippard", RankLift.Deadlift, 235, 73.1, "CPU Canadian Championship", "2014-03-31"),
            Meet("Arnold Schwarzenegger", RankLift.Deadlift, 250, null, "DBKV Internationale Meisterschaft Kraftdreikampf", "1966-10-30"),
            Meet("Jessica Buettner", RankLift.Deadlift, 261.5, 75.25, "IPF World Classic Powerlifting Championships", "2022-06-06"),
            Meet("Alan Thrall", RankLift.Deadlift, 265, 109.65, "USPA Old Skool Iron Classic", "2018-05-19"),
            Meet("Amanda Lawrence", RankLift.Deadlift, 269, 83.63, "IPF Sheffield Powerlifting Championships", "2025-01-26"),
            Meet("Hafþór Júlíus Björnsson", RankLift.Deadlift, 330, 157.05, "WPC-Iceland Icelandic Championships", "2010-03-20"),
            Meet("Taylor Atwood", RankLift.Deadlift, 342.5, 82.48, "AMP Classic Open Nationals", "2025-04-03"),
            Meet("Stan Efferding", RankLift.Deadlift, 347.5, 124.74, "SPF California State Championships", "2011-05-15"),
            Meet("Russel Orhii", RankLift.Deadlift, 355, 91.8, "NAPF Pan-American Powerlifting Championships", "2025-07-27"),
            Meet("Ray Williams", RankLift.Deadlift, 398.5, 189.2, "IPF World Classic Powerlifting Championships", "2018-06-06"),
            Meet("John Haack", RankLift.Deadlift, 426, 93.45, "WRPF The Ghost Clash 3", "2024-04-06"),
            Meet("Jamal Browner", RankLift.Deadlift, 460, 109.45, "WRPF The Ghost Clash 3", "2024-04-06"),
            Meet("Danny Grigsby", RankLift.Deadlift, 487.5, 121.7, "WRPF American Pro", "2022-07-29"),
        },
        new("Zygmunt Smalcerz", "Olympic weightlifting champion (52 kg)", RankLift.Ohp, 112.5, 51.6,
            "1972 Summer Olympics, Munich", new(1972, 8, 27), Munich + "52_kg", "Olympic press"),
        new("Yordan Bikov", "Olympic weightlifting champion (75 kg)", RankLift.Ohp, 160, 74.5,
            "1972 Summer Olympics, Munich", new(1972, 8, 31), Munich + "75_kg", "Olympic press"),
        new("Iron Biby", "Strongman, log press world record holder", RankLift.Ohp, 230, null,
            "Giants Live World Tour Finals, Glasgow", new(2023, 10, 21),
            "https://barbend.com/news/cheick-iron-biby-sanou-log-lift-world-record-230-kilograms/", "Log press"),
        new("Vasily Alekseyev", "Olympic weightlifting champion", RankLift.Ohp, 235, 152.8,
            "1972 Summer Olympics, Munich", new(1972, 9, 6), Munich + "%2B110_kg", "Olympic press"),
    ];

    public static IEnumerable<FamousLift> For(RankLift lift) => All.Where(f => f.Lift == lift).OrderBy(f => f.Kg);

    /// <summary>
    /// Where an estimated one-rep max of <paramref name="kg"/> stands: the heaviest famous lift it has passed (null:
    /// none yet) and the next one up (null: past them all).
    /// </summary>
    public static (FamousLift? Passed, FamousLift? Next) Compare(RankLift lift, double kg)
    {
        var all = For(lift).ToList();
        return (all.LastOrDefault(f => f.Kg <= kg), all.FirstOrDefault(f => f.Kg > kg));
    }

    public static string LiftName(RankLift lift) => lift switch
    {
        RankLift.Bench => "bench press",
        RankLift.Squat => "squat",
        RankLift.Deadlift => "deadlift",
        _ => "overhead press",
    };
}

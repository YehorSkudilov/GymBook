using System.Globalization;
using System.Text;
using GymBook.Models;

namespace GymBook.Services;

/// <summary>
/// Ranked exercise search: every word of the query has to match something about an exercise (its name first, then its
/// muscles, equipment, kind of training and description), with gym shorthand ("db", "rdl", "ohp", "pecs") understood,
/// small typos forgiven and word starts matched as you type. Also how alike two exercises are, for swapping one out.
/// </summary>
public static class ExerciseSearch
{
    // Shorthand and other words for the same thing: each query word also matches what it expands to.
    static readonly Dictionary<string, string[]> Synonyms = new()
    {
        ["db"] = ["dumbbell"], ["dbs"] = ["dumbbell"], ["dumbell"] = ["dumbbell"],
        ["bb"] = ["barbell"], ["kb"] = ["kettlebell"], ["ez"] = ["ezbar"], ["ezbar"] = ["ez"],
        ["bw"] = ["bodyweight"], ["calisthenics"] = ["bodyweight"], ["band"] = ["banded", "resistance"],
        ["smith"] = ["machine"], ["cable"] = ["pulley"], ["trx"] = ["suspension"], ["suspension"] = ["trx"],
        ["ohp"] = ["overhead", "press"], ["rdl"] = ["romanian", "deadlift"], ["sldl"] = ["stiff", "deadlift"],
        ["bss"] = ["bulgarian", "split", "squat"], ["hspu"] = ["handstand", "push"], ["ghr"] = ["glute", "ham", "raise"],
        ["pullup"] = ["pull", "up"], ["pullups"] = ["pull", "up"], ["chinup"] = ["chin", "up"], ["pushup"] = ["push", "up"],
        ["pushups"] = ["push", "up"], ["situp"] = ["sit", "up"], ["burpees"] = ["burpee"],
        ["pecs"] = ["chest"], ["pec"] = ["chest"], ["lats"] = ["back"], ["lat"] = ["back"], ["delts"] = ["shoulders"],
        ["delt"] = ["shoulders"], ["shoulder"] = ["shoulders"], ["bis"] = ["biceps"], ["bicep"] = ["biceps"],
        ["tris"] = ["triceps"], ["tricep"] = ["triceps"], ["quad"] = ["quads"], ["quadriceps"] = ["quads"],
        ["hams"] = ["hamstrings"], ["hamstring"] = ["hamstrings"], ["glute"] = ["glutes"], ["butt"] = ["glutes"],
        ["booty"] = ["glutes"], ["calf"] = ["calves"], ["trap"] = ["traps"], ["core"] = ["abs"], ["ab"] = ["abs"],
        ["abdominals"] = ["abs"], ["obliques"] = ["abs"], ["forearm"] = ["forearms"], ["grip"] = ["forearms"],
        ["legs"] = ["quads", "hamstrings", "glutes", "calves"], ["leg"] = ["quads", "hamstrings", "glutes", "calves"],
        ["arms"] = ["biceps", "triceps", "forearms"], ["arm"] = ["biceps", "triceps", "forearms"],
        ["stretching"] = ["stretch"], ["stretches"] = ["stretch"], ["plyo"] = ["plyometric", "jump"], ["plyos"] = ["plyometric"],
        ["jumps"] = ["jump"], ["olympic"] = ["clean", "snatch", "jerk"], ["oly"] = ["olympic"], ["hiit"] = ["cardio"],
        ["conditioning"] = ["cardio"], ["run"] = ["running"], ["running"] = ["run"], ["bike"] = ["cycling"],
        ["cycling"] = ["bike"], ["row"] = ["rowing"], ["warmup"] = ["mobility"],
        ["rehab"] = ["mobility"], ["flexibility"] = ["stretch"], ["fly"] = ["flye"], ["flye"] = ["fly"], ["flyes"] = ["fly"],
        ["flys"] = ["fly"], ["rdls"] = ["romanian", "deadlift"], ["hip"] = ["hips"], ["hips"] = ["hip"],
    };

    // Words that say nothing on their own in a search.
    static readonly HashSet<string> StopWords = ["the", "a", "an", "with", "and", "for", "on", "of", "to", "exercise", "exercises"];

    sealed record Entry(string[] Name, string[] Muscles, string[] Secondary, string[] Kind, string[] Text);

    static readonly Dictionary<string, Entry> Cache = [];

    static Entry Index(Exercise e)
    {
        if (!e.IsCustom && Cache.TryGetValue(e.Id, out var cached))
            return cached;
        var details = ExerciseLibrary.Details(e.Id);
        var kind = new List<string> { e.Equipment.Display(), e.Mechanic.ToString() };
        if (details != null)
        {
            kind.Add(details.Category.ToString());
            kind.Add(details.Level.ToString());
            if (details.Hold)
                kind.Add("hold isometric");
        }
        var entry = new Entry(
            Words(e.Name),
            Words(e.PrimaryMuscle.Display()),
            [.. e.SecondaryMuscles.SelectMany(m => Words(m.Display()))],
            Words(string.Join(' ', kind)),
            Words(e.Instructions));
        if (!e.IsCustom)
            Cache[e.Id] = entry;
        return entry;
    }

    /// <summary>The exercises that match every word of <paramref name="query"/>, best first; all of them for an empty query.</summary>
    public static List<Exercise> Search(IEnumerable<Exercise> exercises, string query)
    {
        var words = Words(query).Where(w => !StopWords.Contains(w)).ToArray();
        if (words.Length == 0)
            return [.. exercises.OrderBy(e => e.Name)];
        var phrase = string.Join(' ', words);
        return [.. exercises
            .Select(e => (e, score: Score(e, words, phrase)))
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .ThenBy(x => x.e.Name.Length)
            .ThenBy(x => x.e.Name)
            .Select(x => x.e)];
    }

    static double Score(Exercise e, string[] words, string phrase)
    {
        var entry = Index(e);
        double total = 0;
        foreach (var word in words)
        {
            var best = Match(entry, word);
            foreach (var alt in Synonyms.GetValueOrDefault(word, []))
                best = Math.Max(best, 0.9 * Match(entry, alt));
            if (best <= 0)
                return 0;
            total += best;
        }
        // The query as typed, in order, inside the name: "bench press" ranks Bench Press above Press Around the Bench.
        var name = string.Join(' ', entry.Name);
        if (name.StartsWith(phrase, StringComparison.Ordinal))
            total += 6;
        else if (name.Contains(phrase, StringComparison.Ordinal))
            total += 3;
        return total;
    }

    // How well one query word matches: the name counts most, then the primary muscle, the rest a little.
    static double Match(Entry entry, string word) => Math.Max(Math.Max(
        10 * Best(entry.Name, word), 7 * Best(entry.Muscles, word)), Math.Max(
        4 * Best(entry.Kind, word), Math.Max(3 * Best(entry.Secondary, word), 1.5 * Best(entry.Text, word))));

    // 1 for the same word, a bit less for the start of one (typing in progress) or a word with a small typo.
    static double Best(string[] fieldWords, string word)
    {
        double best = 0;
        foreach (var w in fieldWords)
        {
            if (w == word || Stem(w) == Stem(word))
                return 1;
            if (word.Length >= 2 && w.StartsWith(word, StringComparison.Ordinal))
                best = Math.Max(best, 0.8);
            else if (word.Length >= 4 && Math.Abs(w.Length - word.Length) <= 2 && Distance(w, word) <= (word.Length >= 8 ? 2 : 1))
                best = Math.Max(best, 0.6);
        }
        return best;
    }

    /// <summary>
    /// How good a swap <paramref name="candidate"/> is for <paramref name="original"/>, 0 when it isn't one: the same
    /// main muscle above all, then the same kind of movement (by name: press, row, curl...), equipment and training.
    /// </summary>
    public static double Similarity(Exercise original, Exercise candidate)
    {
        if (candidate.Id == original.Id)
            return 0;
        var a = Index(original);
        var b = Index(candidate);
        double score = 0;
        if (candidate.PrimaryMuscle == original.PrimaryMuscle)
            score += 10;
        else if (original.SecondaryMuscles.Contains(candidate.PrimaryMuscle) || candidate.SecondaryMuscles.Contains(original.PrimaryMuscle))
            score += 3;
        score += 1.5 * candidate.SecondaryMuscles.Intersect(original.SecondaryMuscles).Count();
        // Shared movement words in the names ("Incline Dumbbell Press" ~ "Machine Chest Press"), not equipment words.
        var equipmentWords = new HashSet<string>(Enum.GetValues<Equipment>().SelectMany(x => Words(x.Display())).Select(Stem));
        var movement = a.Name.Select(Stem).Where(w => !equipmentWords.Contains(w) && !StopWords.Contains(w)).ToHashSet();
        score += 4 * b.Name.Select(Stem).Count(movement.Contains);
        if (candidate.Mechanic == original.Mechanic)
            score += 2;
        if (candidate.Equipment == original.Equipment)
            score += 1;
        var da = ExerciseLibrary.Details(original.Id);
        var dc = ExerciseLibrary.Details(candidate.Id);
        if (da != null && dc != null)
            score += da.Category == dc.Category ? 3 : -6;
        return score >= 10 ? score : 0;
    }

    /// <summary>Lowercase words without accents or punctuation: "Push-Up (Wide)" → push, up, wide.</summary>
    public static string[] Words(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            sb.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
        }
        return sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    // Plurals and the like, so "curls" finds "Curl" and "presses" finds "Press".
    static string Stem(string w) =>
        w.Length > 4 && w.EndsWith("ies") ? w[..^3] + "y"
        : w.Length > 4 && (w.EndsWith("ses") || w.EndsWith("xes") || w.EndsWith("hes")) ? w[..^2]
        : w.Length > 3 && w.EndsWith('s') && !w.EndsWith("ss") ? w[..^1]
        : w;

    // Edits between two words (insert, delete, change, swap neighbours), for forgiving typos.
    static int Distance(string a, string b)
    {
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
        return d[a.Length, b.Length];
    }
}

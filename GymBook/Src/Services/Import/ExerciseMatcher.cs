using GymBook.Models;

namespace GymBook.Services.Import;

/// <summary>How sure a match is, which decides what an import does with it by default.</summary>
public enum MatchConfidence { Sure, Check, None }

/// <summary>
/// Who decided a match: the word matcher alone, the AI picking an exercise, or the AI finding none (the word matcher's
/// guess is then kept only as a suggestion).
/// </summary>
public enum MatchSource { Words, Ai, AiFoundNone }

public record ExerciseMatch(Exercise? Exercise, double Score, MatchSource Source = MatchSource.Words)
{
    public MatchConfidence Confidence => Source switch
    {
        MatchSource.Ai => Exercise == null ? MatchConfidence.None : MatchConfidence.Sure,
        MatchSource.AiFoundNone => MatchConfidence.None,
        _ => Exercise == null || Score < ExerciseMatcher.CheckScore ? MatchConfidence.None
            : Score < ExerciseMatcher.SureScore ? MatchConfidence.Check
            : MatchConfidence.Sure,
    };
}

/// <summary>
/// Finds the app's exercise for one named by another app ("Incline Bench Press" with "Dumbbells"): the words they share,
/// weighted by how rare each is across the catalogue (so "incline" counts for more than "press"), plus the equipment.
/// Words like "grip", "with" and the equipment itself don't count towards the name.
/// </summary>
public class ExerciseMatcher
{
    public const double SureScore = 0.75, CheckScore = 0.55;
    const double EquipmentMatch = 0.15, EquipmentMismatch = 0.15;

    static readonly HashSet<string> Stop = ["with", "the", "on", "a", "of", "and", "grip", "to", "in"];
    static readonly HashSet<string> EquipmentWords = ["cable", "machine", "barbell", "dumbbell", "ez", "bar", "ezbar", "band", "resistance", "smith", "bodyweight", "kettlebell"];
    static readonly Dictionary<string, string> Synonyms = new() { ["butterfly"] = "fly", ["flye"] = "fly" };

    readonly List<(Exercise Exercise, HashSet<string> Words)> _library;
    readonly Dictionary<string, int> _documentFrequency = [];

    public ExerciseMatcher(IEnumerable<Exercise> exercises)
    {
        _library = exercises.Select(e => (e, Words(e.Name))).ToList();
        foreach (var (_, words) in _library)
            foreach (var w in words)
                _documentFrequency[w] = _documentFrequency.GetValueOrDefault(w) + 1;
    }

    public ExerciseMatch Match(string name, Equipment? equipment)
    {
        var words = Words(name);
        if (words.Count == 0)
            return new(null, 0);
        var weight = words.Sum(Rarity);
        Exercise? best = null;
        double bestScore = 0;
        foreach (var (exercise, candidate) in _library)
        {
            var shared = words.Where(candidate.Contains).Sum(Rarity);
            if (shared == 0)
                continue;
            var score = shared / Math.Sqrt(weight * candidate.Sum(Rarity));
            if (equipment is { } e)
                score += exercise.Equipment == e ? EquipmentMatch : -EquipmentMismatch;
            if (score > bestScore)
                (best, bestScore) = (exercise, score);
        }
        return new(best, bestScore);
    }

    double Rarity(string word) => Math.Log((_library.Count + 1.0) / (_documentFrequency.GetValueOrDefault(word) + 1)) + 1;

    static HashSet<string> Words(string name)
    {
        var text = name.ToLowerInvariant()
            .Replace("sit-ups", "situp").Replace("sit-up", "situp")
            .Replace("push-ups", "pushup").Replace("push-up", "pushup")
            .Replace("pull-ups", "pullup").Replace("pull-up", "pullup");
        var cleaned = new string(text.Select(c => char.IsLetterOrDigit(c) ? c : ' ').ToArray());
        return cleaned.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(w => !Stop.Contains(w))
            .Select(Stem)
            .Where(w => !EquipmentWords.Contains(w))
            .ToHashSet();
    }

    // Enough of English plurals for exercise names: curls, raises, crunches, flies/flys.
    static string Stem(string word)
    {
        if (word is "flys" or "flies")
            return "fly";
        if (word.EndsWith("ches"))
            word = word[..^2];
        else if (word.EndsWith("ies"))
            word = word[..^3] + "y";
        else if (word.EndsWith('s') && !word.EndsWith("ss") && word.Length > 3)
            word = word[..^1];
        return Synonyms.GetValueOrDefault(word, word);
    }

    /// <summary>Equipment as other apps write it ("Dumbbells", "EZ bar", "Resistance bands"…).</summary>
    public static Equipment? ParseEquipment(string text) => text.Trim().ToLowerInvariant() switch
    {
        "barbell" => Equipment.Barbell,
        "dumbbell" or "dumbbells" => Equipment.Dumbbell,
        "machine" or "smith machine" => Equipment.Machine,
        "cable" or "cables" => Equipment.Cable,
        "bodyweight" or "body weight" or "none" => Equipment.Bodyweight,
        "kettlebell" or "kettlebells" => Equipment.Kettlebell,
        "ez bar" or "ez-bar" or "ezbar" => Equipment.EzBar,
        "band" or "bands" or "resistance band" or "resistance bands" => Equipment.Band,
        "" => null,
        _ => Equipment.Other,
    };
}

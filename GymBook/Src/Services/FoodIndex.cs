using System.Globalization;
using System.Text;
using GymBook.Contracts;

namespace GymBook.Services;

/// <summary>
/// A food found by <see cref="FoodSearchService"/>: its amounts per <see cref="PerText"/> (per 100 g when
/// <see cref="PerGrams"/> is 100; per serving when its weight isn't known), a usual serving, and where it's from.
/// </summary>
public record FoodMatch(string Name, string? Brand, double Kcal, double Protein, double Carbs, double Fat,
    string PerText, double? PerGrams, double? ServingG, string? ServingText, string Source)
{
    public string Title => Brand is { Length: > 0 } b && !Name.Contains(b, StringComparison.OrdinalIgnoreCase) ? $"{Name} · {b}" : Name;

    /// <summary>Amounts by weight; false when only a serving's are known (picked by servings instead).</summary>
    public bool ByWeight => PerGrams is > 0;

    /// <summary>The amounts in <paramref name="amount"/> grams (by weight) or servings.</summary>
    public (double Kcal, double Protein, double Carbs, double Fat) For(double amount)
    {
        var factor = ByWeight ? amount / PerGrams!.Value : amount;
        return (Kcal * factor, Protein * factor, Carbs * factor, Fat * factor);
    }

    public static FoodMatch From(FoodInfo f) =>
        new(f.Name, f.Brand, f.Kcal, f.ProteinG, f.CarbsG, f.FatG, f.PerText, f.PerGrams, f.ServingG, f.ServingText, f.Source);
}

/// <summary>What a search found, and whose credit to show with it.</summary>
public record FoodSearchResult(List<FoodMatch> Foods, bool PoweredByFatSecret, bool Offline);

/// <summary>
/// Finds foods by name the way people type: words in any order, half-typed ("chick bre"), plurals, and typos
/// ("chiken brest", "yoghurt", "bananna"). Each name is split into words once, and every distinct word is kept once in a
/// vocabulary, so a search scores the query against the vocabulary (a few thousand short words) and then each food just
/// looks its words' scores up: well under a millisecond per thousand foods.
/// </summary>
public sealed class FoodIndex
{
    // Words a query can have that a name needn't ("chicken with rice" finds "Chicken and rice").
    static readonly HashSet<string> StopWords = ["a", "an", "and", "the", "of", "with", "w", "in", "on", "or", "for", "to"];

    readonly FoodMatch[] _foods;
    readonly int[][] _words;
    readonly string[] _vocab;
    readonly int[] _uses;
    readonly double[] _boosts;
    readonly Dictionary<string, int> _ids = [];

    /// <param name="boost">A head start for some foods over others that match as well.</param>
    public FoodIndex(IEnumerable<FoodMatch> foods, Func<FoodMatch, double>? boost = null)
    {
        _foods = [.. foods];
        _boosts = [.. _foods.Select(f => boost?.Invoke(f) ?? 0)];
        _words = new int[_foods.Length][];
        var vocab = new List<string>();
        var uses = new List<int>();
        for (var i = 0; i < _foods.Length; i++)
        {
            var words = Words(_foods[i].Title);
            var ids = new int[words.Count];
            for (var w = 0; w < words.Count; w++)
            {
                if (!_ids.TryGetValue(words[w], out var id))
                {
                    _ids[words[w]] = id = vocab.Count;
                    vocab.Add(words[w]);
                    uses.Add(0);
                }
                uses[id]++;
                ids[w] = id;
            }
            _words[i] = ids;
        }
        _vocab = [.. vocab];
        _uses = [.. uses];
    }

    public int Count => _foods.Length;

    /// <summary>
    /// The foods matching every word of <paramref name="query"/> (allowing typos and the last word half-typed), best first,
    /// each with its score (about 0–1.6). <paramref name="lenient"/> keeps foods missing a word too, scored lower: for
    /// results another database already picked.
    /// </summary>
    public List<(FoodMatch Food, double Score)> Search(string query, int max, double boost = 0, bool lenient = false)
    {
        var q = Words(query);
        var found = new List<(FoodMatch Food, double Score)>();
        if (q.Count == 0 || _foods.Length == 0)
            return found;

        // How well each query word matches each word in the vocabulary, worked out once.
        var scores = new float[q.Count][];
        for (var k = 0; k < q.Count; k++)
        {
            var row = scores[k] = new float[_vocab.Length];
            var last = k == q.Count - 1;
            for (var v = 0; v < _vocab.Length; v++)
                row[v] = (float)WordScore(q[k], _vocab[v], last);
        }

        var required = q.Count(w => !StopWords.Contains(w));
        for (var i = 0; i < _foods.Length; i++)
        {
            var ids = _words[i];
            if (ids.Length == 0)
                continue;
            double total = 0;
            int matched = 0, missed = 0;
            var firstWordFirst = false;
            ulong used = 0;
            for (var k = 0; k < q.Count; k++)
            {
                var row = scores[k];
                float best = 0;
                var at = -1;
                for (var w = 0; w < ids.Length; w++)
                    if (row[ids[w]] > best)
                        (best, at) = (row[ids[w]], w);
                if (best > 0)
                {
                    total += best;
                    matched++;
                    if (at < 64)
                        used |= 1UL << at;
                    if (k == 0 && at == 0)
                        firstWordFirst = true;
                }
                else if (!StopWords.Contains(q[k]) || required == 0)
                    missed++;
            }
            if (matched == 0 || (missed > 0 && !lenient))
                continue;
            var score = total / q.Count;
            // The name starts with what was typed first: "Banana" before "Bread, banana".
            if (firstWordFirst)
                score += 0.25;
            // How much of the name the query covers: "Egg, whole" before "Egg noodles, cooked, enriched, with salt".
            score += 0.3 * ulong.PopCount(used) / Math.Min(ids.Length, 64);
            found.Add((_foods[i], score + boost + _boosts[i]));
        }
        return [.. found.OrderByDescending(f => f.Score).ThenBy(f => f.Food.Title.Length).Take(max)];
    }

    /// <summary>
    /// The query with its misspelt words swapped for the closest word any food has ("chiken brest" → "chicken breast"), for
    /// databases that only match exactly. Words already known (or the start of one, for the last) stay as typed.
    /// </summary>
    public string Correct(string query)
    {
        var q = Words(query);
        for (var k = 0; k < q.Count; k++)
        {
            var last = k == q.Count - 1;
            if (_ids.ContainsKey(q[k]) || q[k].Length < 3 || q[k].Any(char.IsDigit))
                continue;
            if (last && _vocab.Any(v => v.StartsWith(q[k], StringComparison.Ordinal)))
                continue;
            var best = -1;
            double bestScore = 0;
            for (var v = 0; v < _vocab.Length; v++)
            {
                var s = WordScore(q[k], _vocab[v], false);
                if (s > bestScore || (s == bestScore && best >= 0 && s > 0 && _uses[v] > _uses[best]))
                    (best, bestScore) = (v, s);
            }
            if (best >= 0)
                q[k] = _vocab[best];
        }
        return string.Join(' ', q);
    }

    /// <summary>
    /// How well a typed word matches a word of a name: 1 the same, a bit less for a plural or the word's start (more for the
    /// last word, still being typed), less again for a typo or two (one in a word up to 6 letters); 0 for no match.
    /// </summary>
    public static double WordScore(string typed, string word, bool last)
    {
        if (typed == word)
            return 1;
        if (IsPlural(typed, word) || IsPlural(word, typed))
            return 0.95;
        if (word.StartsWith(typed, StringComparison.Ordinal))
            return last ? 0.9 : typed.Length >= 3 ? 0.7 : 0;
        var allowed = typed.Length <= 2 ? 0 : typed.Length <= 6 ? 1 : 2;
        if (allowed == 0)
            return 0;
        // Typos in a whole word: a letter wrong, missing, extra or two swapped.
        if (Math.Abs(word.Length - typed.Length) <= allowed)
        {
            var d = Distance(typed, word, allowed);
            if (d <= allowed)
                return 0.8 - 0.15 * d;
        }
        // Typos in a word still being typed: "chikc" is how "chicken" starts.
        if (last && word.Length > typed.Length && typed.Length >= 4 && word[0] == typed[0])
        {
            var d = Math.Min(Distance(typed, word[..typed.Length], 1),
                Math.Min(Distance(typed, word[..(typed.Length + 1)], 1), Distance(typed, word[..(typed.Length - 1)], 1)));
            if (d <= 1)
                return 0.6 - 0.1 * d;
        }
        return 0;
    }

    static bool IsPlural(string a, string b) =>
        a.Length == b.Length + 1 && a[^1] == 's' && a.StartsWith(b, StringComparison.Ordinal)
        || a.Length == b.Length + 2 && a.EndsWith("es", StringComparison.Ordinal) && a.StartsWith(b, StringComparison.Ordinal)
        || a.Length == b.Length + 2 && a.EndsWith("ies", StringComparison.Ordinal) && b[^1] == 'y' && a.StartsWith(b[..^1], StringComparison.Ordinal);

    /// <summary>
    /// Edits from <paramref name="a"/> to <paramref name="b"/> (a letter changed, added, removed, or two next to each other
    /// swapped), giving up once it's more than <paramref name="max"/>.
    /// </summary>
    static int Distance(string a, string b, int max)
    {
        if (Math.Abs(a.Length - b.Length) > max)
            return max + 1;
        Span<int> prev2 = stackalloc int[b.Length + 1], prev = stackalloc int[b.Length + 1], cur = stackalloc int[b.Length + 1];
        for (var j = 0; j <= b.Length; j++)
            prev[j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            cur[0] = i;
            var rowMin = cur[0];
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                var d = Math.Min(Math.Min(prev[j] + 1, cur[j - 1] + 1), prev[j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    d = Math.Min(d, prev2[j - 2] + 1);
                cur[j] = d;
                rowMin = Math.Min(rowMin, d);
            }
            if (rowMin > max)
                return max + 1;
            var t = prev2;
            prev2 = prev;
            prev = cur;
            cur = t;
        }
        return prev[b.Length];
    }

    /// <summary>A name's words, lower case without accents ("Crème brûlée" → crème → "creme", "brulee"); numbers kept.</summary>
    public static List<string> Words(string text)
    {
        var words = new List<string>();
        var sb = new StringBuilder();
        foreach (var c in text.Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(c))
                sb.Append(char.ToLowerInvariant(c));
            else if (c != '\'' && sb.Length > 0)
            {
                words.Add(sb.ToString());
                sb.Clear();
            }
        }
        if (sb.Length > 0)
            words.Add(sb.ToString());
        return words;
    }
}

using System.Globalization;
using System.Text.RegularExpressions;

namespace GymBook.Services;

/// <summary>
/// Tidies what the food databases give before it's shown: brands out of USDA's "McDONALD'S, BIG MAC" names, servings
/// readable and weighed, impossible nutrition dropped, and the same food from several places (or entered several times in
/// Open Food Facts) shown once, from the source most likely to be right.
/// </summary>
public static partial class FoodCleanup
{
    /// <summary>
    /// USDA names a branded item "BRAND, ITEM" in capitals ("McDONALD'S, BIG MAC"): that's "Big Mac" by "McDonald's".
    /// Other names come back as they were.
    /// </summary>
    public static (string Name, string? Brand) SplitBrand(string name)
    {
        var comma = name.IndexOf(", ", StringComparison.Ordinal);
        if (comma < 2)
            return (name, null);
        var first = name[..comma];
        var letters = first.Where(char.IsLetter).ToList();
        if (letters.Count < 3 || first.Any(char.IsDigit) || first.Contains('"')
            || letters.Count(char.IsUpper) < letters.Count * 0.85)
            return (name, null);
        var item = Words(name[(comma + 2)..]);
        return (char.ToUpperInvariant(item[0]) + item[1..], Words(first));

        // Shouting words in normal case ("BIG MAC" → "Big Mac"), short ones without vowels kept ("BBQ", "KFC").
        static string Words(string text) => string.Join(' ', text.Split(' ').Select(w =>
        {
            var l = w.Where(char.IsLetter).ToList();
            if (l.Count == 0 || l.Any(char.IsLower) || (l.Count <= 3 && !l.Any(c => "AEIOUY".Contains(c))) || w.Contains('.'))
                return w.StartsWith("Mc") && w.Length > 3 ? "Mc" + Cap(w[2..]) : w;
            return Cap(w);
        }));

        static string Cap(string w)
        {
            var lower = w.ToLowerInvariant();
            return char.ToUpperInvariant(lower[0]) + lower[1..];
        }
    }

    /// <summary>
    /// A serving's description without the weight tacked on, which is shown beside it anyway ("1 item 7.6 oz" → "1 item",
    /// "1 burger (219 g)" → "1 burger", "30 g" → null).
    /// </summary>
    public static string? CleanServing(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var t = TrailingWeight().Replace(text.Trim(), "").Trim();
        t = WeightInBrackets().Replace(t, "").Trim();
        if (OnlyWeight().IsMatch(t))
            return null;
        return t.Length > 0 ? t : null;
    }

    /// <summary>The grams in a serving's description ("1 burger (219 g)", "30g"), when it says.</summary>
    public static double? GramsIn(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;
        var m = Grams().Match(text);
        return m.Success && double.TryParse(m.Groups[1].Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var g)
            && g is > 0 and < 2000 ? g : null;
    }

    /// <summary>
    /// Whether the numbers can be real: per 100 g no more than pure fat's 900 kcal or 100 g of macros, and not more
    /// calories in the macros than the food has in all (more is fine: alcohol and fibre aren't counted in them).
    /// </summary>
    public static bool Plausible(FoodMatch f)
    {
        if (f.Kcal < 0 || f.Protein < 0 || f.Carbs < 0 || f.Fat < 0 || f.Kcal + f.Protein + f.Carbs + f.Fat <= 0)
            return false;
        if (f.ByWeight)
        {
            var per100 = 100 / f.PerGrams!.Value;
            if (f.Kcal * per100 > 920 || (f.Protein + f.Carbs + f.Fat) * per100 > 105)
                return false;
        }
        var fromMacros = 4 * f.Protein + 4 * f.Carbs + 9 * f.Fat;
        return fromMacros <= f.Kcal * 1.35 + 40;
    }

    /// <summary>
    /// The same food once. Foods are the same when their names say the same thing apart from the brand ("Big Mac",
    /// "Big Mac (McDonalds)", "Big Mac · McDonald's") and the brands don't differ (no brand joins the one brand that
    /// has it). Kept: one's own first, then the everyday foods, FatSecret, USDA, Open Food Facts; among equals one with a
    /// brand and a serving whose calories are closest to what the rest say (so a serving typed in as 100 g loses). It
    /// borrows a serving from the others when it has none, and keeps the best score among them.
    /// </summary>
    public static List<(FoodMatch Food, double Score)> Dedupe(List<(FoodMatch Food, double Score)> found)
    {
        var brandWords = found.SelectMany(f => FoodIndex.Words(f.Food.Brand ?? "")).Where(w => w.Length > 2).ToHashSet();
        var keyed = found.Select(f => (f.Food, f.Score, Key: Key(f.Food, brandWords))).ToList();

        // An unbranded food joins the one brand with its name: "Big Mac" is McDonald's.
        var brandsByName = keyed.Where(k => k.Key.Brand.Length > 0).GroupBy(k => k.Key.Name)
            .ToDictionary(g => g.Key, g => g.Select(k => k.Key.Brand).Distinct().ToList());
        var groups = keyed.GroupBy(k => k.Key.Brand.Length == 0 && brandsByName.TryGetValue(k.Key.Name, out var brands) && brands.Count == 1
            ? (k.Key.Name, brands[0])
            : (k.Key.Name, k.Key.Brand));

        var result = new List<(FoodMatch, double)>();
        foreach (var group in groups)
        {
            var members = group.ToList();
            if (members.Count == 1 || group.Key.Item1.Length == 0)
            {
                result.AddRange(members.Select(m => (m.Food, m.Score)));
                continue;
            }
            var per100 = members.Where(m => m.Food.ByWeight).Select(m => m.Food.Kcal * 100 / m.Food.PerGrams!.Value).Order().ToList();
            double? median = per100.Count > 0 ? per100[per100.Count / 2] : null;
            var best = members
                .OrderBy(m => Rank(m.Food.Source))
                .ThenBy(m => m.Food.Brand is { Length: > 0 } ? 0 : 1)
                .ThenBy(m => m.Food.ServingG is > 0 || !m.Food.ByWeight ? 0 : 1)
                .ThenBy(m => median is { } md && m.Food.ByWeight ? Math.Abs(m.Food.Kcal * 100 / m.Food.PerGrams!.Value - md) : 0)
                .First();
            var food = best.Food;
            if (food.ByWeight && food.ServingG == null
                && members.Select(m => m.Food).FirstOrDefault(m => m.ServingG is > 0) is { } withServing)
                food = food with { ServingG = withServing.ServingG, ServingText = withServing.ServingText };
            result.Add((food, members.Max(m => m.Score)));
        }
        return result;
    }

    /// <summary>A food's name and brand as words, sorted, without the brand's words or the brands named elsewhere.</summary>
    static (string Name, string Brand) Key(FoodMatch f, HashSet<string> brandWords)
    {
        var brand = FoodIndex.Words(f.Brand ?? "").ToHashSet();
        var words = FoodIndex.Words(f.Name);
        var name = words.Where(w => !brand.Contains(w) && !brandWords.Contains(w)).ToList();
        if (name.Count == 0)
            name = words;
        // A brand only said in the name: "Big Mac (McDonalds)".
        if (brand.Count == 0)
            brand = [.. words.Where(brandWords.Contains)];
        return (string.Join(' ', name.Order()), string.Join(' ', brand.Where(w => w.Length > 2).Order()));
    }

    static int Rank(string source) => source switch
    {
        FoodSearchService.MyFoodSource or FoodSearchService.FavoriteSource => 0,
        FoodSearchService.EverydaySource => 1,
        "FatSecret" => 2,
        "USDA" => 3,
        _ => 4,
    };

    [GeneratedRegex(@"\s*\(?\d+(\.\d+)?\s*(oz|fl oz)\)?$", RegexOptions.IgnoreCase)]
    private static partial Regex TrailingWeight();

    [GeneratedRegex(@"\s*\(\s*\d+(?:[.,]\d+)?\s*(?:g|gr|grams?|ml)\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex WeightInBrackets();

    [GeneratedRegex(@"^\d+(?:[.,]\d+)?\s*(?:g|gr|grams?|ml|oz)?$", RegexOptions.IgnoreCase)]
    private static partial Regex OnlyWeight();

    [GeneratedRegex(@"(\d+(?:[.,]\d+)?)\s*(?:g|gr|grams?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Grams();
}

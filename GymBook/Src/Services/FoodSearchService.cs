using System.Globalization;
using System.Text.Json;

namespace GymBook.Services;

/// <summary>A food found by <see cref="FoodSearchService"/>: its amounts per 100 g, and a usual serving when known.</summary>
public record FoodMatch(string Name, string? Brand, double Kcal100, double Protein100, double Carbs100, double Fat100,
    double? ServingG, string? ServingText)
{
    public string Title => Brand is { Length: > 0 } b && !Name.Contains(b, StringComparison.OrdinalIgnoreCase) ? $"{Name} · {b}" : Name;

    /// <summary>The amounts in <paramref name="grams"/> of it.</summary>
    public (double Kcal, double Protein, double Carbs, double Fat) For(double grams) =>
        (Kcal100 * grams / 100, Protein100 * grams / 100, Carbs100 * grams / 100, Fat100 * grams / 100);
}

/// <summary>
/// Foods with their nutrition already filled in, to log by name: common everyday foods (typical values, built in, so
/// they're found offline too) first, then packaged foods from Open Food Facts, the free, open food database.
/// </summary>
public class FoodSearchService
{
    static readonly HttpClient Http = CreateClient();

    static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        // Open Food Facts asks apps to name themselves.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GymBook/1.0 (Android; food search)");
        return client;
    }

    /// <summary>Up to about 30 foods matching <paramref name="query"/>, common ones first. Throws when offline and nothing common matches.</summary>
    public async Task<List<FoodMatch>> SearchAsync(string query, CancellationToken ct)
    {
        query = query.Trim();
        var results = Common(query).Take(8).ToList();
        if (query.Length < 2)
            return results;
        try
        {
            results.AddRange(await OpenFoodFactsAsync(query, ct));
        }
        catch (Exception) when (results.Count > 0 && !ct.IsCancellationRequested)
        {
            // Offline: the common foods are still there.
        }
        return results;
    }

    static IEnumerable<FoodMatch> Common(string query)
    {
        var words = query.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return [];
        return CommonFoods
            .Where(f => words.All(w => f.Name.Contains(w, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(f => f.Name.StartsWith(words[0], StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(f => f.Name.Length);
    }

    static async Task<List<FoodMatch>> OpenFoodFactsAsync(string query, CancellationToken ct)
    {
        var url = "https://search.openfoodfacts.org/search?page_size=25&fields=product_name,brands,nutriments,serving_quantity,serving_size&q="
            + Uri.EscapeDataString(query);
        using var response = await Http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        var found = new List<FoodMatch>();
        if (!doc.RootElement.TryGetProperty("hits", out var hits) || hits.ValueKind != JsonValueKind.Array)
            return found;
        foreach (var hit in hits.EnumerateArray())
        {
            var name = Text(hit, "product_name")?.Trim();
            if (string.IsNullOrEmpty(name) || !hit.TryGetProperty("nutriments", out var n) || n.ValueKind != JsonValueKind.Object)
                continue;
            var kcal = Number(n, "energy-kcal_100g") ?? Number(n, "energy-kj_100g") / 4.184;
            var protein = Number(n, "proteins_100g");
            var carbs = Number(n, "carbohydrates_100g");
            var fat = Number(n, "fat_100g");
            // Only foods with the nutrition filled in.
            if (kcal is not { } k || k < 0 || k > 950 || protein == null || carbs == null || fat == null)
                continue;
            var brand = hit.TryGetProperty("brands", out var brands) && brands.ValueKind == JsonValueKind.Array
                ? brands.EnumerateArray().Select(b => b.ValueKind == JsonValueKind.String ? b.GetString() : null).FirstOrDefault(b => !string.IsNullOrWhiteSpace(b))
                : Text(hit, "brands");
            var serving = Number(hit, "serving_quantity");
            found.Add(new FoodMatch(name, brand?.Trim(), k, protein.Value, carbs.Value, fat.Value,
                serving is > 0 and < 2000 ? serving : null, Text(hit, "serving_size")));
        }
        // The same product listed twice (it happens) only once.
        return [.. found.DistinctBy(f => (f.Title.ToLowerInvariant(), Math.Round(f.Kcal100)))];
    }

    static string? Text(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    static double? Number(JsonElement e, string name) =>
        !e.TryGetProperty(name, out var v) ? null
        : v.ValueKind == JsonValueKind.Number ? v.GetDouble()
        : v.ValueKind == JsonValueKind.String && double.TryParse(v.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d
        : null;

    static FoodMatch F(string name, double kcal, double protein, double carbs, double fat, double? servingG = null, string? serving = null) =>
        new(name, null, kcal, protein, carbs, fat, servingG, serving);

    /// <summary>Everyday foods with typical values per 100 g (from USDA FoodData Central), and a usual serving.</summary>
    static readonly FoodMatch[] CommonFoods =
    [
        F("Chicken breast, cooked", 165, 31, 0, 3.6, 150, "1 breast"),
        F("Chicken breast, raw", 120, 22.5, 0, 2.6, 170, "1 breast"),
        F("Chicken thigh, cooked", 209, 26, 0, 10.9, 100, "1 thigh"),
        F("Turkey breast, cooked", 135, 30, 0, 1, 100),
        F("Ground beef 90% lean, cooked", 217, 26, 0, 11.7, 100),
        F("Ground beef 80% lean, cooked", 254, 25.9, 0, 16, 100),
        F("Beef steak (sirloin), cooked", 201, 29, 0, 9, 200, "1 steak"),
        F("Pork loin, cooked", 206, 28, 0, 10, 150),
        F("Bacon, cooked", 541, 37, 1.4, 42, 8, "1 slice"),
        F("Ham, sliced", 145, 21, 1.5, 5.5, 28, "1 slice"),
        F("Salmon, cooked", 206, 22, 0, 12.4, 150, "1 fillet"),
        F("Tuna, canned in water", 116, 25.5, 0, 0.8, 120, "1 can, drained"),
        F("Cod, cooked", 105, 23, 0, 0.9, 150, "1 fillet"),
        F("Shrimp, cooked", 99, 24, 0.2, 0.3, 85),
        F("Egg, whole", 143, 12.6, 0.7, 9.5, 50, "1 large egg"),
        F("Egg white", 52, 10.9, 0.7, 0.2, 33, "1 large egg's"),
        F("Tofu, firm", 144, 17.3, 2.8, 8.7, 100),
        F("Milk, whole", 61, 3.2, 4.8, 3.3, 244, "1 cup"),
        F("Milk, 2%", 50, 3.3, 4.8, 2, 244, "1 cup"),
        F("Milk, skim", 34, 3.4, 5, 0.1, 244, "1 cup"),
        F("Greek yogurt, plain, nonfat", 59, 10.2, 3.6, 0.4, 170, "1 pot"),
        F("Greek yogurt, plain, whole", 97, 9, 4, 5, 170, "1 pot"),
        F("Cottage cheese, low fat", 84, 11, 4.3, 2.3, 113, "1/2 cup"),
        F("Cheddar cheese", 403, 25, 1.3, 33, 28, "1 slice"),
        F("Mozzarella", 280, 28, 3.1, 17, 28),
        F("Parmesan", 431, 38, 4.1, 29, 10, "1 tbsp grated"),
        F("Butter", 717, 0.9, 0.1, 81, 14, "1 tbsp"),
        F("Olive oil", 884, 0, 0, 100, 13.5, "1 tbsp"),
        F("Peanut butter", 588, 25, 20, 50, 32, "2 tbsp"),
        F("Almonds", 579, 21, 22, 50, 28, "a handful"),
        F("Walnuts", 654, 15, 14, 65, 28, "a handful"),
        F("Cashews", 553, 18, 30, 44, 28, "a handful"),
        F("White rice, cooked", 130, 2.7, 28, 0.3, 158, "1 cup"),
        F("Brown rice, cooked", 123, 2.7, 25.6, 1, 158, "1 cup"),
        F("Pasta, cooked", 158, 5.8, 31, 0.9, 140, "1 cup"),
        F("Oats, dry", 389, 16.9, 66, 6.9, 40, "1/2 cup"),
        F("Bread, white", 265, 9, 49, 3.2, 28, "1 slice"),
        F("Bread, whole wheat", 252, 12.5, 43, 3.5, 32, "1 slice"),
        F("Bagel, plain", 250, 10, 49, 1.5, 105, "1 bagel"),
        F("Tortilla, flour", 306, 8.2, 50, 8, 45, "1 tortilla"),
        F("Potato, baked", 93, 2.5, 21, 0.1, 173, "1 medium"),
        F("Sweet potato, baked", 90, 2, 20.7, 0.2, 114, "1 medium"),
        F("French fries", 312, 3.4, 41, 15, 117, "1 medium serving"),
        F("Quinoa, cooked", 120, 4.4, 21.3, 1.9, 185, "1 cup"),
        F("Banana", 89, 1.1, 22.8, 0.3, 118, "1 medium"),
        F("Apple", 52, 0.3, 13.8, 0.2, 182, "1 medium"),
        F("Orange", 47, 0.9, 11.8, 0.1, 131, "1 medium"),
        F("Strawberries", 32, 0.7, 7.7, 0.3, 152, "1 cup"),
        F("Blueberries", 57, 0.7, 14.5, 0.3, 148, "1 cup"),
        F("Grapes", 69, 0.7, 18, 0.2, 151, "1 cup"),
        F("Avocado", 160, 2, 8.5, 14.7, 150, "1 avocado"),
        F("Broccoli, cooked", 35, 2.4, 7.2, 0.4, 156, "1 cup"),
        F("Spinach, raw", 23, 2.9, 3.6, 0.4, 30, "1 cup"),
        F("Carrot, raw", 41, 0.9, 9.6, 0.2, 61, "1 medium"),
        F("Tomato", 18, 0.9, 3.9, 0.2, 123, "1 medium"),
        F("Cucumber", 16, 0.7, 3.6, 0.1, 100),
        F("Lettuce", 15, 1.4, 2.9, 0.2, 50),
        F("Onion", 40, 1.1, 9.3, 0.1, 110, "1 medium"),
        F("Bell pepper", 31, 1, 6, 0.3, 120, "1 medium"),
        F("Corn, cooked", 96, 3.4, 21, 1.5, 145, "1 cup"),
        F("Green beans, cooked", 35, 1.9, 7.9, 0.3, 125, "1 cup"),
        F("Peas, cooked", 84, 5.4, 15.6, 0.2, 160, "1 cup"),
        F("Black beans, cooked", 132, 8.9, 23.7, 0.5, 172, "1 cup"),
        F("Chickpeas, cooked", 164, 8.9, 27.4, 2.6, 164, "1 cup"),
        F("Lentils, cooked", 116, 9, 20, 0.4, 198, "1 cup"),
        F("Hummus", 166, 7.9, 14.3, 9.6, 30, "2 tbsp"),
        F("Whey protein powder", 400, 80, 8, 6, 30, "1 scoop"),
        F("Honey", 304, 0.3, 82, 0, 21, "1 tbsp"),
        F("Sugar", 387, 0, 100, 0, 4, "1 tsp"),
        F("Dark chocolate 70%", 598, 7.8, 46, 43, 20, "2 squares"),
        F("Pizza, cheese", 266, 11, 33, 10, 107, "1 slice"),
        F("Orange juice", 45, 0.7, 10.4, 0.2, 248, "1 glass"),
        F("Cola", 42, 0, 10.6, 0, 330, "1 can"),
        F("Beer", 43, 0.5, 3.6, 0, 355, "1 bottle"),
        F("Wine, red", 85, 0.1, 2.6, 0, 150, "1 glass"),
        F("Coffee, black", 1, 0.1, 0, 0, 240, "1 cup"),
        F("Granola", 471, 10, 64, 20, 50),
        F("Corn flakes", 357, 7.5, 84, 0.4, 30, "1 bowl"),
        F("Ice cream, vanilla", 207, 3.5, 24, 11, 66, "1 scoop"),
        F("Rice cakes", 387, 8, 81.5, 2.8, 9, "1 cake"),
        F("Mayonnaise", 680, 1, 0.6, 75, 14, "1 tbsp"),
        F("Ketchup", 101, 1, 27, 0.1, 17, "1 tbsp"),
    ];
}

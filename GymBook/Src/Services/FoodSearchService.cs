using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using GymBook.Contracts;
using GymBook.Models;
using GymBook.Services.Sync;

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
/// Foods with their nutrition filled in, to log by name or barcode. In order: the user's own saved foods, everyday foods
/// (a short list of typical values, then USDA FoodData Central's ~13,000 generic foods and dishes, built in so they're
/// found offline), then online: USDA's branded foods and FatSecret (searched by the Gym Book server, which holds their
/// keys) and Open Food Facts, the free, open database of packaged foods.
/// </summary>
public class FoodSearchService(DataStore store, ApiClient api)
{
    const int LocalMax = 15, OnlineMax = 25;

    static readonly HttpClient Http = CreateClient();
    static List<FoodMatch>? _usda;
    static readonly SemaphoreSlim UsdaGate = new(1, 1);

    static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        // Open Food Facts asks apps to name themselves.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GymBook/1.0 (Android; food search)");
        return client;
    }

    public async Task<FoodSearchResult> SearchAsync(string query, CancellationToken ct)
    {
        query = query.Trim();
        var words = Words(query);
        var results = new List<FoodMatch>();
        if (words.Length == 0)
            return new(results, false, false);

        results.AddRange(Ranked(MyFoods(), words).Take(LocalMax));
        results.AddRange(Ranked(CommonFoods, words).Take(8));
        results.AddRange(Ranked(await UsdaAsync(), words).Take(LocalMax));
        if (query.Length < 2)
            return new(results, false, false);

        // Online, side by side; whichever fails (offline, server down) just adds nothing.
        var server = Try(() => api.SearchFoodsAsync(query, ct));
        var openFoodFacts = Try(() => OpenFoodFactsAsync(query, ct));
        var fromServer = await server;
        var fromOff = await openFoodFacts;
        ct.ThrowIfCancellationRequested();
        if (fromServer != null)
            results.AddRange(fromServer.Foods.Take(OnlineMax).Select(FoodMatch.From));
        if (fromOff != null)
            results.AddRange(fromOff.Take(OnlineMax));
        // The same food from two places (or listed twice) only once.
        results = [.. results.DistinctBy(f => (f.Title.ToLowerInvariant(), Math.Round(f.Kcal), f.PerText))];
        return new(results, fromServer?.PoweredByFatSecret == true, fromServer == null && fromOff == null);
    }

    /// <summary>The packaged food with this barcode: Open Food Facts first, then the server's databases. Null when unknown.</summary>
    public async Task<FoodMatch?> BarcodeAsync(string code, CancellationToken ct)
    {
        var off = await Try(() => OpenFoodFactsProductAsync(code, ct));
        if (off != null)
            return off;
        var server = await Try(() => api.FoodByBarcodeAsync(code, ct));
        return server != null ? FoodMatch.From(server) : null;
    }

    static async Task<T?> Try<T>(Func<Task<T>> call) where T : class
    {
        try
        {
            return await call();
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            return null;
        }
    }

    // ---------- My foods ----------

    /// <summary>My foods, as found in the search: a serving's amounts, by weight too when the serving's weight is known.</summary>
    public IEnumerable<FoodMatch> MyFoods() => store.Profile.MyFoods.Select(Match);

    public const string MyFoodSource = "My food";

    static FoodMatch Match(SavedFood f) =>
        new(f.Name, f.Brand, f.Calories, f.ProteinG, f.CarbsG, f.FatG, f.ServingText ?? "1 serving", null, f.ServingG, f.ServingText, MyFoodSource);

    /// <summary>
    /// Saves a food to My foods (replacing one with the same name and brand) with what one serving has, what a serving is
    /// and its weight when known. Returns it as the search finds it.
    /// </summary>
    public FoodMatch Save(string name, string? brand, double kcal, double protein, double carbs, double fat, string? servingText = null, double? servingG = null)
    {
        static string? Clip(string? text) => text?.Trim() is { Length: > 0 } t ? t.Length > SyncLimits.NameLength ? t[..SyncLimits.NameLength] : t : null;
        var food = new SavedFood
        {
            Name = Clip(name) ?? "My food",
            Brand = Clip(brand),
            Calories = Math.Round(kcal, 1),
            ProteinG = Math.Round(protein, 1),
            CarbsG = Math.Round(carbs, 1),
            FatG = Math.Round(fat, 1),
            ServingText = Clip(servingText),
            ServingG = servingG is > 0 and <= 5000 ? Math.Round(servingG.Value, 1) : null,
        };
        var foods = store.Profile.MyFoods;
        foods.RemoveAll(f => IsSame(f, food.Name, food.Brand));
        foods.Insert(0, food);
        if (foods.Count > SavedFood.Max)
            foods.RemoveRange(SavedFood.Max, foods.Count - SavedFood.Max);
        store.Save();
        return Match(food);
    }

    /// <summary>Whether a food by this name (and brand) is in My foods.</summary>
    public bool IsSaved(string name, string? brand) => store.Profile.MyFoods.Any(f => IsSame(f, name, brand));

    static bool IsSame(SavedFood f, string name, string? brand) =>
        string.Equals(f.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)
        && string.Equals(f.Brand ?? "", brand?.Trim() ?? "", StringComparison.OrdinalIgnoreCase);

    /// <summary>Only what's on the phone (My foods, everyday foods, USDA's generic ones): instant, for filling in as it's typed.</summary>
    public async Task<List<FoodMatch>> LocalAsync(string query)
    {
        var words = Words(query.Trim());
        if (words.Length == 0)
            return [];
        return [.. Ranked(MyFoods(), words).Take(LocalMax).Concat(Ranked(CommonFoods, words).Take(8)).Concat(Ranked(await UsdaAsync(), words).Take(LocalMax))];
    }

    /// <summary>Takes a food out of My foods.</summary>
    public void Forget(string name, string? brand)
    {
        if (store.Profile.MyFoods.RemoveAll(f => IsSame(f, name, brand)) > 0)
            store.Save();
    }

    // ---------- Matching ----------

    static string[] Words(string query) => query.ToLowerInvariant().Split([' ', ',', '-'], StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Foods with every word in their name: names starting with the first word first, then the shortest.</summary>
    static IEnumerable<FoodMatch> Ranked(IEnumerable<FoodMatch> foods, string[] words) => foods
        .Where(f => words.All(w => f.Title.Contains(w, StringComparison.OrdinalIgnoreCase)))
        .OrderBy(f => f.Title.StartsWith(words[0], StringComparison.OrdinalIgnoreCase) ? 0 : 1)
        .ThenBy(f => f.Title.Length);

    // ---------- USDA generic foods (built in) ----------

    /// <summary>Loaded the first time it's searched (Resources/Raw/foods/usda_foods.tsv.gz, made by tools/build_usda_foods.py).</summary>
    static async Task<List<FoodMatch>> UsdaAsync()
    {
        if (_usda != null)
            return _usda;
        await UsdaGate.WaitAsync();
        try
        {
            if (_usda != null)
                return _usda;
            var foods = new List<FoodMatch>(14000);
            try
            {
                await using var file = await FileSystem.OpenAppPackageFileAsync("foods/usda_foods.tsv.gz");
                await using var gzip = new GZipStream(file, CompressionMode.Decompress);
                using var reader = new StreamReader(gzip);
                await Task.Run(() =>
                {
                    while (reader.ReadLine() is { } line)
                    {
                        var p = line.Split('\t');
                        if (p.Length < 7 || !Num(p[1], out var kcal) || !Num(p[2], out var protein) || !Num(p[3], out var carbs) || !Num(p[4], out var fat))
                            continue;
                        double? serving = Num(p[5], out var g) && g > 0 ? g : null;
                        foods.Add(new FoodMatch(p[0], null, kcal, protein, carbs, fat, "100 g", 100, serving,
                            p[6].Length > 0 ? p[6] : null, "USDA"));
                    }
                });
            }
            catch (Exception)
            {
                // Missing from the package: the other sources still search.
            }
            return _usda = foods;
        }
        finally
        {
            UsdaGate.Release();
        }

        static bool Num(string text, out double value) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    // ---------- Open Food Facts ----------

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
            if (OpenFoodFactsProduct(hit) is { } food)
                found.Add(food);
        return found;
    }

    static async Task<FoodMatch?> OpenFoodFactsProductAsync(string code, CancellationToken ct)
    {
        var url = $"https://world.openfoodfacts.org/api/v2/product/{Uri.EscapeDataString(code)}.json?fields=product_name,brands,nutriments,serving_quantity,serving_size";
        using var response = await Http.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
            return null;
        await using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: ct);
        return doc.RootElement.TryGetProperty("product", out var product) ? OpenFoodFactsProduct(product) : null;
    }

    /// <summary>A product with its nutrition filled in, or null.</summary>
    static FoodMatch? OpenFoodFactsProduct(JsonElement p)
    {
        var name = Text(p, "product_name")?.Trim();
        if (string.IsNullOrEmpty(name) || !p.TryGetProperty("nutriments", out var n) || n.ValueKind != JsonValueKind.Object)
            return null;
        var kcal = Number(n, "energy-kcal_100g") ?? Number(n, "energy-kj_100g") / 4.184;
        var protein = Number(n, "proteins_100g");
        var carbs = Number(n, "carbohydrates_100g");
        var fat = Number(n, "fat_100g");
        if (kcal is not { } k || k < 0 || k > 950 || protein == null || carbs == null || fat == null)
            return null;
        var brand = p.TryGetProperty("brands", out var brands) && brands.ValueKind == JsonValueKind.Array
            ? brands.EnumerateArray().Select(b => b.ValueKind == JsonValueKind.String ? b.GetString() : null).FirstOrDefault(b => !string.IsNullOrWhiteSpace(b))
            : Text(p, "brands")?.Split(',')[0];
        var serving = Number(p, "serving_quantity");
        return new FoodMatch(name, brand?.Trim(), k, protein.Value, carbs.Value, fat.Value, "100 g", 100,
            serving is > 0 and < 2000 ? serving : null, Text(p, "serving_size"), "Open Food Facts");
    }

    static string? Text(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    static double? Number(JsonElement e, string name) =>
        !e.TryGetProperty(name, out var v) ? null
        : v.ValueKind == JsonValueKind.Number ? v.GetDouble()
        : v.ValueKind == JsonValueKind.String && double.TryParse(v.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d
        : null;

    static FoodMatch F(string name, double kcal, double protein, double carbs, double fat, double? servingG = null, string? serving = null) =>
        new(name, null, kcal, protein, carbs, fat, "100 g", 100, servingG, serving, "Typical");

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

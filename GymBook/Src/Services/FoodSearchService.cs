using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using GymBook.Contracts;
using GymBook.Models;
using GymBook.Services.Sync;

namespace GymBook.Services;

/// <summary>
/// Foods with their nutrition filled in, to log by name or barcode: the user's favourites and the foods they made, everyday
/// foods (a short list of typical values, then USDA FoodData Central's ~13,000 generic foods and dishes, built in so they're
/// found offline), then online: USDA's branded foods and FatSecret (searched by the Gym Book server, which holds their
/// keys) and Open Food Facts, the free, open database of packaged foods. Everything is ranked together by
/// <see cref="FoodIndex"/>, which forgives typos; a misspelt query is also sent online spelt right.
/// </summary>
public class FoodSearchService(DataStore store, ApiClient api)
{
    const int LocalMax = 40, OnlineMax = 25, ShownMax = 70;
    // Ahead of a database food that matches as well: one's own first, then the everyday ones.
    const double SavedBoost = 0.35, EverydayBoost = 0.2;

    static readonly HttpClient Http = CreateClient();
    static FoodIndex? _builtIn;
    static readonly SemaphoreSlim BuiltInGate = new(1, 1);

    static HttpClient CreateClient()
    {
        var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        // Open Food Facts asks apps to name themselves.
        client.DefaultRequestHeaders.UserAgent.ParseAdd("GymBook/1.0 (Android; food search)");
        return client;
    }

    /// <summary>Loads the built-in foods ahead of the first search, so it's instant.</summary>
    public static Task WarmUpAsync() => BuiltInAsync();

    /// <summary>Only what's on the phone (saved foods, everyday foods, USDA's generic ones): instant, for filling in as it's typed.</summary>
    public async Task<List<FoodMatch>> LocalAsync(string query)
    {
        var builtIn = await BuiltInAsync();
        var saved = SavedFoods().ToList();
        return await Task.Run(() => Merge(Local(query, builtIn, saved)));
    }

    /// <summary>Everything: what's on the phone, then the online databases, ranked together.</summary>
    public async Task<FoodSearchResult> SearchAsync(string query, CancellationToken ct)
    {
        query = query.Trim();
        var builtIn = await BuiltInAsync();
        var saved = SavedFoods().ToList();
        var found = await Task.Run(() => Local(query, builtIn, saved), ct);
        if (query.Length < 2)
            return new(Merge(found), false, false);

        // Online, side by side; whichever fails (offline, server down) just adds nothing. These databases only match words
        // exactly, so when little is found for a misspelt query, it's asked again spelt right.
        var (fromServer, fromOff) = await OnlineAsync(query, ct);
        var corrected = builtIn.Correct(query);
        if (corrected != string.Join(' ', FoodIndex.Words(query)) && (fromServer?.Foods.Count ?? 0) + (fromOff?.Count ?? 0) < 8)
        {
            var (server2, off2) = await OnlineAsync(corrected, ct);
            if (server2 != null)
                fromServer = fromServer == null ? server2
                    : new() { Foods = [.. server2.Foods, .. fromServer.Foods], PoweredByFatSecret = server2.PoweredByFatSecret || fromServer.PoweredByFatSecret };
            if (off2 != null)
                fromOff = [.. off2.Concat(fromOff ?? [])];
        }
        ct.ThrowIfCancellationRequested();
        var online = (fromServer?.Foods.Take(OnlineMax).Select(FoodMatch.From) ?? []).Concat(fromOff?.Take(OnlineMax) ?? []).ToList();
        if (online.Count > 0)
        {
            // Scored against what was typed or what was meant (a brand isn't a typo), whichever fits better; a food the
            // database found but whose name doesn't say it still shows, lower.
            var index = new FoodIndex(online);
            var best = new Dictionary<FoodMatch, double>();
            foreach (var food in online)
                best.TryAdd(food, 0.1);
            foreach (var (food, score) in index.Search(query, online.Count, lenient: true).Concat(index.Search(corrected, online.Count, lenient: true)))
                best[food] = Math.Max(best[food], score);
            found.AddRange(best.Select(b => (b.Key, b.Value)));
        }
        var foods = Merge(found);
        var poweredByFatSecret = fromServer?.PoweredByFatSecret == true && foods.Any(f => f.Source == "FatSecret");
        return new(foods, poweredByFatSecret, fromServer == null && fromOff == null);
    }

    async Task<(FoodSearchResponse? Server, List<FoodMatch>? Off)> OnlineAsync(string query, CancellationToken ct)
    {
        var server = Try(() => api.SearchFoodsAsync(query, ct));
        var openFoodFacts = Try(() => OpenFoodFactsAsync(query, ct));
        return (await server, await openFoodFacts);
    }

    static List<(FoodMatch Food, double Score)> Local(string query, FoodIndex builtIn, List<FoodMatch> saved)
    {
        var found = new FoodIndex(saved).Search(query, LocalMax, SavedBoost);
        found.AddRange(builtIn.Search(query, LocalMax));
        return found;
    }

    /// <summary>Best first, the same food from two places (or listed twice) only once.</summary>
    static List<FoodMatch> Merge(IEnumerable<(FoodMatch Food, double Score)> found) =>
        [.. found.OrderByDescending(f => f.Score).ThenBy(f => f.Food.Title.Length)
            .Select(f => f.Food)
            .DistinctBy(f => (f.Title.ToLowerInvariant(), Math.Round(f.Kcal), f.PerText))
            .Take(ShownMax)];

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

    // ---------- Favourites and My foods ----------

    /// <summary>Foods the user made (<see cref="SavedFood.Created"/>).</summary>
    public const string MyFoodSource = "My food";
    /// <summary>Foods starred from anywhere else.</summary>
    public const string FavoriteSource = "Favourite";
    public const string EverydaySource = "Everyday";

    /// <summary>Starred foods, latest first: from any database, or made by the user.</summary>
    public IEnumerable<FoodMatch> Favorites() => store.Profile.MyFoods.Where(IsFavorite).Select(Match);

    /// <summary>Foods the user made, latest first.</summary>
    public IEnumerable<FoodMatch> Created() => store.Profile.MyFoods.Where(f => f.Created == true).Select(Match);

    /// <summary>The everyday foods (typical values), for browsing.</summary>
    public static IReadOnlyList<FoodMatch> Everyday => CommonFoods;

    /// <summary>Favourites and foods made by the user, as found in the search: a serving's amounts, by weight too when the serving's weight is known.</summary>
    IEnumerable<FoodMatch> SavedFoods() => store.Profile.MyFoods.Where(f => f.Created == true || IsFavorite(f)).Select(Match);

    // Saved before favourites and made foods were told apart: those were all starred or made, so they count as favourites.
    static bool IsFavorite(SavedFood f) => f.Favorite ?? f.Created != true;

    static FoodMatch Match(SavedFood f) =>
        new(f.Name, f.Brand, f.Calories, f.ProteinG, f.CarbsG, f.FatG, f.ServingText ?? "1 serving", null, f.ServingG, f.ServingText,
            f.Created == true ? MyFoodSource : FavoriteSource);

    /// <summary>
    /// Makes a food of the user's own (replacing one with the same name and brand) with what one serving has, what a
    /// serving is and its weight when known. Returns it as the search finds it.
    /// </summary>
    public FoodMatch SaveCreated(string name, string? brand, double kcal, double protein, double carbs, double fat, string? servingText = null, double? servingG = null)
    {
        var food = Saved(name, brand, kcal, protein, carbs, fat, servingText, servingG);
        var old = store.Profile.MyFoods.FirstOrDefault(f => IsSame(f, food.Name, food.Brand));
        food.Created = true;
        food.Favorite = old != null && IsFavorite(old);
        Keep(food);
        return Match(food);
    }

    /// <summary>Stars a food (one serving of it, or 100 g): it's kept in Favourites.</summary>
    public void AddFavorite(string name, string? brand, double kcal, double protein, double carbs, double fat, string? servingText = null, double? servingG = null)
    {
        var old = store.Profile.MyFoods.FirstOrDefault(f => IsSame(f, name, brand));
        if (old?.Created == true)
        {
            // One the user made keeps its own amounts.
            old.Favorite = true;
            store.Save();
            return;
        }
        var food = Saved(name, brand, kcal, protein, carbs, fat, servingText, servingG);
        food.Favorite = true;
        Keep(food);
    }

    /// <summary>Unstars a food: gone from Favourites (one the user made stays in My foods).</summary>
    public void RemoveFavorite(string name, string? brand)
    {
        var foods = store.Profile.MyFoods;
        foreach (var f in foods.Where(f => IsSame(f, name, brand)).ToList())
        {
            if (f.Created == true)
                f.Favorite = false;
            else
                foods.Remove(f);
        }
        store.Save();
    }

    /// <summary>Whether a food by this name (and brand) is starred.</summary>
    public bool IsFavorite(string name, string? brand) => store.Profile.MyFoods.Any(f => IsSame(f, name, brand) && IsFavorite(f));

    /// <summary>Deletes a food the user made (from Favourites too).</summary>
    public void Delete(string name, string? brand)
    {
        if (store.Profile.MyFoods.RemoveAll(f => IsSame(f, name, brand)) > 0)
            store.Save();
    }

    static SavedFood Saved(string name, string? brand, double kcal, double protein, double carbs, double fat, string? servingText, double? servingG)
    {
        static string? Clip(string? text) => text?.Trim() is { Length: > 0 } t ? t.Length > SyncLimits.NameLength ? t[..SyncLimits.NameLength] : t : null;
        return new SavedFood
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
    }

    /// <summary>Puts a saved food first (in place of one with the same name and brand), within the limit.</summary>
    void Keep(SavedFood food)
    {
        var foods = store.Profile.MyFoods;
        foods.RemoveAll(f => IsSame(f, food.Name, food.Brand));
        foods.Insert(0, food);
        if (foods.Count > SavedFood.Max)
            foods.RemoveRange(SavedFood.Max, foods.Count - SavedFood.Max);
        store.Save();
    }

    static bool IsSame(SavedFood f, string name, string? brand) =>
        string.Equals(f.Name, name.Trim(), StringComparison.OrdinalIgnoreCase)
        && string.Equals(f.Brand ?? "", brand?.Trim() ?? "", StringComparison.OrdinalIgnoreCase);

    // ---------- USDA generic foods (built in) ----------

    /// <summary>
    /// The everyday foods and USDA's, indexed the first time they're needed (Resources/Raw/foods/usda_foods.tsv.gz, made by
    /// tools/build_usda_foods.py).
    /// </summary>
    static async Task<FoodIndex> BuiltInAsync()
    {
        if (_builtIn != null)
            return _builtIn;
        await BuiltInGate.WaitAsync();
        try
        {
            if (_builtIn != null)
                return _builtIn;
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
                // Missing from the package: the other foods still search.
            }
            // The everyday foods get a head start on USDA's longer names.
            return _builtIn = await Task.Run(() => new FoodIndex([.. CommonFoods, .. foods], f => f.Source == EverydaySource ? EverydayBoost : 0));
        }
        finally
        {
            BuiltInGate.Release();
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
        new(name, null, kcal, protein, carbs, fat, "100 g", 100, servingG, serving, EverydaySource);

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

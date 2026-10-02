using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using GymBook.Contracts;
using Microsoft.Extensions.Caching.Memory;

namespace GymBook.Api.Foods;

/// <summary>Usda__ApiKey in .env: a free key from api.data.gov. Unset leaves USDA branded foods out of the search.</summary>
public class UsdaOptions
{
    public string? ApiKey { get; set; }
}

/// <summary>
/// FatSecret__ClientId and FatSecret__ClientSecret in .env (a free FatSecret Platform API account; the server's IP has
/// to be on its allowed list). Unset leaves FatSecret out of the search.
/// </summary>
public class FatSecretOptions
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}

/// <summary>
/// Searches the food databases that need a key, which stays here rather than in the app: USDA FoodData Central's branded
/// (packaged) foods and FatSecret. Each is optional, and one failing doesn't stop the other. Results are cached a day.
/// </summary>
public partial class FoodDatabases(HttpClient http, UsdaOptions usda, FatSecretOptions fatSecret, IMemoryCache cache, ILogger<FoodDatabases> log)
{
    const int PerSource = 25;
    static readonly TimeSpan CacheFor = TimeSpan.FromDays(1);

    bool HasUsda => !string.IsNullOrWhiteSpace(usda.ApiKey);
    bool HasFatSecret => !string.IsNullOrWhiteSpace(fatSecret.ClientId) && !string.IsNullOrWhiteSpace(fatSecret.ClientSecret);

    public async Task<FoodSearchResponse> SearchAsync(string query, CancellationToken ct)
    {
        query = query.Trim();
        var key = "foods:" + query.ToLowerInvariant();
        if (cache.TryGetValue(key, out FoodSearchResponse? cached) && cached != null)
            return cached;
        var usdaTask = HasUsda ? Safely("USDA", () => UsdaSearchAsync(query, ct)) : Task.FromResult(new List<FoodInfo>());
        var fatSecretTask = HasFatSecret ? Safely("FatSecret", () => FatSecretSearchAsync(query, ct)) : Task.FromResult(new List<FoodInfo>());
        var (fromUsda, fromFatSecret) = (await usdaTask, await fatSecretTask);
        // Taking turns, so the first screen has some of each.
        var foods = new List<FoodInfo>();
        for (var i = 0; i < Math.Max(fromUsda.Count, fromFatSecret.Count); i++)
        {
            if (i < fromFatSecret.Count)
                foods.Add(fromFatSecret[i]);
            if (i < fromUsda.Count)
                foods.Add(fromUsda[i]);
        }
        var response = new FoodSearchResponse { Foods = foods, PoweredByFatSecret = fromFatSecret.Count > 0 };
        cache.Set(key, response, CacheFor);
        return response;
    }

    /// <summary>The packaged food with this barcode (UPC/EAN) in USDA's branded foods, or null.</summary>
    public async Task<FoodInfo?> BarcodeAsync(string code, CancellationToken ct)
    {
        if (!HasUsda)
            return null;
        var key = "barcode:" + code;
        if (cache.TryGetValue(key, out FoodInfo? cached))
            return cached;
        var wanted = code.TrimStart('0');
        var found = (await Safely("USDA", () => UsdaSearchAsync(code, ct, barcode: wanted))).FirstOrDefault();
        cache.Set(key, found, CacheFor);
        return found;
    }

    async Task<List<FoodInfo>> Safely(string name, Func<Task<List<FoodInfo>>> search)
    {
        try
        {
            return await search();
        }
        catch (Exception e) when (e is HttpRequestException or JsonException or TaskCanceledException or InvalidOperationException)
        {
            log.LogWarning(e, "{Source} food search failed", name);
            return [];
        }
    }

    // ---------- USDA FoodData Central (branded foods) ----------

    async Task<List<FoodInfo>> UsdaSearchAsync(string query, CancellationToken ct, string? barcode = null)
    {
        var url = "https://api.nal.usda.gov/fdc/v1/foods/search?dataType=Branded&requireAllWords=true&pageSize=" + PerSource
            + "&query=" + Uri.EscapeDataString(query) + "&api_key=" + Uri.EscapeDataString(usda.ApiKey!);
        using var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        var foods = new List<FoodInfo>();
        if (!doc.RootElement.TryGetProperty("foods", out var list) || list.ValueKind != JsonValueKind.Array)
            return foods;
        foreach (var f in list.EnumerateArray())
        {
            if (barcode != null && Text(f, "gtinUpc")?.TrimStart('0') != barcode)
                continue;
            var name = Text(f, "description");
            if (string.IsNullOrWhiteSpace(name) || !f.TryGetProperty("foodNutrients", out var nutrients) || nutrients.ValueKind != JsonValueKind.Array)
                continue;
            double? kcal = null, protein = null, carbs = null, fat = null;
            foreach (var n in nutrients.EnumerateArray())
            {
                var id = n.TryGetProperty("nutrientId", out var i) && i.ValueKind == JsonValueKind.Number ? i.GetInt32() : 0;
                var value = n.TryGetProperty("value", out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : (double?)null;
                switch (id)
                {
                    case 1008 or 2047 or 2048 when kcal == null && Text(n, "unitName")?.Equals("KCAL", StringComparison.OrdinalIgnoreCase) != false:
                        kcal = value;
                        break;
                    case 1003:
                        protein = value;
                        break;
                    case 1005:
                        carbs = value;
                        break;
                    case 1004:
                        fat = value;
                        break;
                }
            }
            if (kcal is not { } k || protein == null || carbs == null || fat == null)
                continue;
            var unit = Text(f, "servingSizeUnit")?.ToLowerInvariant();
            var serving = Number(f, "servingSize") is { } s && s > 0 && unit is "g" or "grm" ? s : (double?)null;
            foods.Add(new FoodInfo
            {
                Name = Tidy(name),
                Brand = Text(f, "brandName") is { Length: > 0 } brand ? Tidy(brand) : Text(f, "brandOwner") is { Length: > 0 } owner ? Tidy(owner) : null,
                Kcal = k,
                ProteinG = protein.Value,
                CarbsG = carbs.Value,
                FatG = fat.Value,
                ServingG = serving,
                ServingText = Text(f, "householdServingFullText") is { Length: > 0 } household ? household.ToLowerInvariant() : null,
                Source = "USDA",
            });
        }
        return foods;
    }

    /// <summary>USDA's branded names are often in capitals: "PROTEIN BAR" reads better as "Protein bar".</summary>
    static string Tidy(string text)
    {
        text = text.Trim();
        if (text.Any(char.IsLower))
            return text;
        var lower = text.ToLowerInvariant();
        return char.ToUpperInvariant(lower[0]) + lower[1..];
    }

    // ---------- FatSecret Platform API ----------

    // A typed HttpClient's service is made afresh for each request, so the token lives in the cache.
    static readonly SemaphoreSlim TokenGate = new(1, 1);
    const string TokenKey = "fatsecret:token";

    async Task<string> FatSecretTokenAsync(CancellationToken ct)
    {
        await TokenGate.WaitAsync(ct);
        try
        {
            if (cache.TryGetValue(TokenKey, out string? saved) && saved != null)
                return saved;
            using var request = new HttpRequestMessage(HttpMethod.Post, "https://oauth.fatsecret.com/connect/token")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["grant_type"] = "client_credentials", ["scope"] = "basic" }),
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic",
                Convert.ToBase64String(Encoding.UTF8.GetBytes($"{fatSecret.ClientId}:{fatSecret.ClientSecret}")));
            using var response = await http.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
            var token = Text(doc.RootElement, "access_token") ?? throw new InvalidOperationException("FatSecret sent no token.");
            var seconds = Number(doc.RootElement, "expires_in") ?? 3600;
            cache.Set(TokenKey, token, TimeSpan.FromSeconds(Math.Max(60, seconds - 120)));
            return token;
        }
        finally
        {
            TokenGate.Release();
        }
    }

    async Task<List<FoodInfo>> FatSecretSearchAsync(string query, CancellationToken ct)
    {
        var token = await FatSecretTokenAsync(ct);
        using var request = new HttpRequestMessage(HttpMethod.Get,
            "https://platform.fatsecret.com/rest/server.api?method=foods.search&format=json&max_results=" + PerSource
            + "&search_expression=" + Uri.EscapeDataString(query));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(ct));
        var foods = new List<FoodInfo>();
        if (!doc.RootElement.TryGetProperty("foods", out var wrapper) || !wrapper.TryGetProperty("food", out var list))
            return foods;
        // One result comes as an object, several as an array.
        var items = list.ValueKind == JsonValueKind.Array ? list.EnumerateArray().ToList() : list.ValueKind == JsonValueKind.Object ? [list] : [];
        foreach (var f in items)
        {
            var name = Text(f, "food_name");
            if (string.IsNullOrWhiteSpace(name) || Text(f, "food_description") is not { } description
                || DescriptionPattern().Match(description) is not { Success: true } m)
                continue;
            var per = m.Groups["per"].Value.Trim();
            foods.Add(new FoodInfo
            {
                Name = name.Trim(),
                Brand = Text(f, "brand_name")?.Trim(),
                Kcal = Parse(m.Groups["kcal"].Value),
                FatG = Parse(m.Groups["fat"].Value),
                CarbsG = Parse(m.Groups["carbs"].Value),
                ProteinG = Parse(m.Groups["protein"].Value),
                PerText = per.Replace("100g", "100 g"),
                PerGrams = Grams(per),
                Source = "FatSecret",
            });
        }
        return foods;
    }

    // "Per 100g - Calories: 165kcal | Fat: 3.57g | Carbs: 0.00g | Protein: 31.02g"
    [GeneratedRegex(@"^Per (?<per>.+?) - Calories: (?<kcal>[\d.]+)kcal \| Fat: (?<fat>[\d.]+)g \| Carbs: (?<carbs>[\d.]+)g \| Protein: (?<protein>[\d.]+)g")]
    private static partial Regex DescriptionPattern();

    // "100g", "1 serving (28 g)", "2 slices (56g)": the grams, when given.
    [GeneratedRegex(@"(?:^|\()\s*(?<g>\d+(?:\.\d+)?)\s*g\s*\)?$")]
    private static partial Regex GramsPattern();

    static double? Grams(string per) =>
        GramsPattern().Match(per) is { Success: true } m && double.TryParse(m.Groups["g"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var g) && g > 0
            ? g
            : null;

    static double Parse(string text) => double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

    static string? Text(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    static double? Number(JsonElement e, string name) =>
        e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v)
            ? v.ValueKind == JsonValueKind.Number ? v.GetDouble()
            : v.ValueKind == JsonValueKind.String && double.TryParse(v.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var d) ? d
            : null
            : null;
}

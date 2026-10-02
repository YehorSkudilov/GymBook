namespace GymBook.Contracts;

/// <summary>
/// A food with its nutrition, found in a food database: amounts per <see cref="PerText"/> (usually 100 g, then
/// <see cref="PerGrams"/> is 100; or per serving when its weight isn't known), and a usual serving when there is one.
/// </summary>
public class FoodInfo
{
    public string Name { get; set; } = "";
    public string? Brand { get; set; }
    public double Kcal { get; set; }
    public double ProteinG { get; set; }
    public double CarbsG { get; set; }
    public double FatG { get; set; }
    /// <summary>What the amounts are for: "100 g", "1 cup", "1 bar".</summary>
    public string PerText { get; set; } = "100 g";
    /// <summary>The weight the amounts are for; null when only per serving is known.</summary>
    public double? PerGrams { get; set; } = 100;
    public double? ServingG { get; set; }
    public string? ServingText { get; set; }
    /// <summary>Where it's from: "USDA", "FatSecret", "Open Food Facts".</summary>
    public string Source { get; set; } = "";
}

/// <summary>Foods from the databases the API searches (USDA branded foods, FatSecret), with whose credit to show.</summary>
public class FoodSearchResponse
{
    public List<FoodInfo> Foods { get; set; } = [];
    /// <summary>FatSecret's terms ask for "Powered by FatSecret" wherever its foods are shown.</summary>
    public bool PoweredByFatSecret { get; set; }
}

using System.Globalization;
using GymBook.Models;

namespace GymBook.Services;

/// <summary>Weights are stored in kg; this converts to and from the user's display unit.</summary>
public class Units(DataStore store)
{
    const double LbsPerKg = 2.2046226218;

    public WeightUnit Unit => store.Profile.Unit;

    public string Label => Unit == WeightUnit.Kg ? "kg" : "lbs";

    public double ToDisplay(double kg) => Unit == WeightUnit.Kg ? kg : kg * LbsPerKg;

    public double FromDisplay(double value) => Unit == WeightUnit.Kg ? value : value / LbsPerKg;

    public string Format(double kg)
    {
        var v = Math.Round(ToDisplay(kg), 1);
        return v.ToString(v % 1 == 0 ? "0" : "0.#", CultureInfo.CurrentCulture);
    }

    public string FormatWithUnit(double kg) => $"{Format(kg)} {Label}";

    public string FormatVolume(double kg)
    {
        var v = ToDisplay(kg);
        return v >= 10000 ? $"{(v / 1000).ToString("0.#", CultureInfo.CurrentCulture)}k {Label}" : $"{v:0} {Label}";
    }

    public bool TryParse(string? text, out double kg)
    {
        kg = 0;
        if (string.IsNullOrWhiteSpace(text))
            return false;
        text = text.Trim().Replace(',', '.');
        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || value < 0)
            return false;
        kg = FromDisplay(value);
        return true;
    }

    /// <summary>Smallest sensible weight jump for the exercise, in display units.</summary>
    public double Increment(Exercise ex) => (Unit, ex.Equipment) switch
    {
        (WeightUnit.Kg, Equipment.Dumbbell) => 2,
        (WeightUnit.Kg, Equipment.Machine) => 5,
        (WeightUnit.Kg, Equipment.Kettlebell) => 4,
        (WeightUnit.Kg, _) => 2.5,
        (WeightUnit.Lbs, Equipment.Machine) => 10,
        (WeightUnit.Lbs, Equipment.Kettlebell) => 10,
        _ => 5,
    };

    public double Round(double kg, Exercise ex)
    {
        var inc = Increment(ex);
        return FromDisplay(Math.Round(ToDisplay(kg) / inc) * inc);
    }

    public double Step(double kg, Exercise ex, int steps) => Math.Max(0, FromDisplay(Math.Round(ToDisplay(kg) / Increment(ex)) * Increment(ex) + Increment(ex) * steps));

    public static string Duration(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}h {t.Minutes:00}m" : $"{t.Minutes}m";

    public static string Clock(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:00}:{t.Seconds:00}" : $"{t.Minutes:00}:{t.Seconds:00}";

    public static string Rest(int seconds) => $"{seconds / 60}:{seconds % 60:00}";
}

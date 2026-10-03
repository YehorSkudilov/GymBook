using System.Globalization;
using System.Text;
using GymBook.Models;
using GymBook.Services.Import;

namespace GymBook.Services;

/// <summary>A kind of export: what's in it and its file.</summary>
public enum ExportKind { Workouts, Plans, Food, Body, Health, Everything }

/// <summary>
/// The user's data as files to share. Workouts and plans are written in the same CSV layout the importer reads
/// (<see cref="CsvExportFormat"/>), so they can be imported again; food, body measurements and calories burned are plain
/// spreadsheet CSVs; everything at once is the JSON backup.
/// </summary>
public class DataExport(DataStore store)
{
    public static string Label(ExportKind kind) => kind switch
    {
        ExportKind.Workouts => "Workout history (CSV, importable)",
        ExportKind.Plans => "Plans (CSV, importable)",
        ExportKind.Food => "Food log (CSV)",
        ExportKind.Body => "Body measurements (CSV)",
        ExportKind.Health => "Calories burned and steps (CSV)",
        _ => "Everything (JSON)",
    };

    /// <summary>Writes the export to a file to share; its path and media type, or null when there's nothing in it.</summary>
    public (string Path, string ContentType)? Write(ExportKind kind)
    {
        var data = store.Data;
        var stamp = DateTime.Today.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        switch (kind)
        {
            case ExportKind.Everything:
                return (store.ExportJson(), "application/json");
            case ExportKind.Workouts:
                var sessions = store.History.OrderBy(s => s.StartedAt).ToList();
                return sessions.Count == 0 ? null
                    : Csv($"gymbook-workouts-{stamp}.csv", CsvExportFormat.WriteWorkouts(sessions, data.Plans, store.GetExercise, store.Profile.Unit));
            case ExportKind.Plans:
                return data.Plans.Count == 0 ? null
                    : Csv($"gymbook-plans-{stamp}.csv", CsvExportFormat.WritePlans(data.Plans.OrderBy(p => p.CreatedAt), store.GetExercise));
            case ExportKind.Food:
                var foods = data.FoodEntries.OrderBy(f => f.Date).ThenBy(f => f.LoggedAt).ToList();
                return foods.Count == 0 ? null : Csv($"gymbook-food-{stamp}.csv", Table(
                    ["Date", "Time", "Meal", "Food", "Calories", "Protein (g)", "Carbs (g)", "Fat (g)", "From"],
                    foods.Select(f => new object?[] { Day(f.Date), f.LoggedAt.ToString("HH:mm", CultureInfo.InvariantCulture), f.Meal, f.Name,
                        f.Calories, f.ProteinG, f.CarbsG, f.FatG, f.Source ?? "Gym Book" })));
            case ExportKind.Body:
                var pounds = store.Profile.Unit == WeightUnit.Lbs;
                var unit = pounds ? "lbs" : "kg";
                double? Mass(double? kg) => kg is { } k ? Math.Round(pounds ? k * 2.20462262 : k, 2) : null;
                var body = data.BodyWeights.OrderBy(b => b.Date).ToList();
                return body.Count == 0 ? null : Csv($"gymbook-body-{stamp}.csv", Table(
                    ["Date", $"Weight ({unit})", "Body fat (%)", $"Lean mass ({unit})", $"Bone mass ({unit})", $"Body water ({unit})", "BMR (kcal)", "From"],
                    body.Select(b => new object?[] { Day(b.Date), Mass(b.WeightKg), b.BodyFatPercent, Mass(b.LeanMassKg), Mass(b.BoneMassKg),
                        Mass(b.BodyWaterKg), b.BmrKcal, b.Source ?? "Gym Book" })));
            case ExportKind.Health:
                var days = data.HealthDays.OrderBy(d => d.Date).ToList();
                return days.Count == 0 ? null : Csv($"gymbook-activity-{stamp}.csv", Table(
                    ["Date", "Total burned (kcal)", "Active (kcal)", "Resting (kcal)", "Steps", "Food from health apps (kcal)", "From"],
                    days.Select(d => new object?[] { Day(d.Date), d.TotalBurnedKcal, d.ActiveBurnedKcal, d.BasalBurnedKcal, d.Steps, d.FoodKcal, d.Source })));
            default:
                return null;
        }
    }

    static string Day(DateTime date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>A comma-separated table with a header row; numbers written invariantly, text quoted when it needs to be.</summary>
    static string Table(string[] header, IEnumerable<object?[]> rows)
    {
        var csv = new StringBuilder();
        csv.AppendJoin(',', header.Select(Field)).Append('\n');
        foreach (var row in rows)
            csv.AppendJoin(',', row.Select(v => v switch
            {
                null => "",
                double d => Math.Round(d, 2).ToString(CultureInfo.InvariantCulture),
                IFormattable f => Field(f.ToString(null, CultureInfo.InvariantCulture)),
                _ => Field(v.ToString() ?? ""),
            })).Append('\n');
        return csv.ToString();

        static string Field(string text) =>
            text.IndexOfAny([',', '"', '\n', '\r']) >= 0 ? $"\"{text.Replace("\"", "\"\"")}\"" : text;
    }

    /// <summary>Saved as UTF-8 with a byte order mark, so spreadsheets read the "·" and accents right (the importer skips it).</summary>
    static (string, string) Csv(string name, string text)
    {
        var path = Path.Combine(FileSystem.CacheDirectory, name);
        File.WriteAllText(path, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
        return (path, "text/csv");
    }
}

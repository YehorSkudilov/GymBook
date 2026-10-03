namespace GymBook.Services.Health;

/// <summary>Sleep from the health apps, as minutes a day: by the day it ended on (the night woken up from, plus naps).</summary>
public static class HealthSleep
{
    /// <summary>
    /// Minutes asleep each day from <paramref name="from"/> to <paramref name="to"/> (local midnights), by the day each
    /// sleep ended on. Sleeps that overlap (two apps recording the same night, a phone and a watch) count once.
    /// </summary>
    public static Dictionary<DateTime, int> ByWakeDay(IEnumerable<(DateTime Start, DateTime End)> sleeps, DateTime from, DateTime to)
    {
        var merged = new List<(DateTime Start, DateTime End)>();
        foreach (var (start, end) in sleeps.Where(s => s.End > s.Start).OrderBy(s => s.Start))
        {
            if (merged.Count > 0 && start <= merged[^1].End)
                merged[^1] = (merged[^1].Start, end > merged[^1].End ? end : merged[^1].End);
            else
                merged.Add((start, end));
        }
        var days = new Dictionary<DateTime, int>();
        foreach (var (start, end) in merged.Where(s => s.End >= from && s.End < to))
            days[end.Date] = Math.Min(1440, days.GetValueOrDefault(end.Date) + (int)(end - start).TotalMinutes);
        return days;
    }

    /// <summary>
    /// <paramref name="days"/> with each day's sleep added: a day already read gets it, a day with only sleep gets a
    /// reading of its own.
    /// </summary>
    public static List<HealthDayReading> Merge(IReadOnlyList<HealthDayReading> days, IReadOnlyDictionary<DateTime, int> sleep)
    {
        var result = days.Select(d => sleep.TryGetValue(d.Date.Date, out var minutes) ? d with { SleepMinutes = minutes } : d).ToList();
        foreach (var (date, minutes) in sleep.Where(s => days.All(d => d.Date.Date != s.Key)))
            result.Add(new HealthDayReading(date, null, null, null, null, null, null, null, null, minutes));
        return result;
    }
}

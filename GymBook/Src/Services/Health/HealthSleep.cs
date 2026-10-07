namespace GymBook.Services.Health;

/// <summary>
/// Sleep from the health apps, a day at a time: minutes asleep by the day it ended on (the night woken up from, plus naps),
/// and when the night's sleep ended (bedtime learns the usual wake-up time from it).
/// </summary>
public static class HealthSleep
{
    /// <summary>
    /// Each day from <paramref name="from"/> to <paramref name="to"/> (local midnights), by the day each sleep ended on:
    /// minutes asleep, and when its longest sleep ended. Sleeps that overlap (two apps recording the same night, a phone
    /// and a watch) count once.
    /// </summary>
    public static Dictionary<DateTime, SleepDay> ByWakeDay(IEnumerable<(DateTime Start, DateTime End)> sleeps, DateTime from, DateTime to)
    {
        var merged = new List<(DateTime Start, DateTime End)>();
        foreach (var (start, end) in sleeps.Where(s => s.End > s.Start).OrderBy(s => s.Start))
        {
            if (merged.Count > 0 && start <= merged[^1].End)
                merged[^1] = (merged[^1].Start, end > merged[^1].End ? end : merged[^1].End);
            else
                merged.Add((start, end));
        }
        return merged.Where(s => s.End >= from && s.End < to).GroupBy(s => s.End.Date).ToDictionary(
            g => g.Key,
            g => new SleepDay(Math.Min(1440, (int)g.Sum(s => (s.End - s.Start).TotalMinutes)), g.MaxBy(s => s.End - s.Start).End));
    }

    /// <summary>
    /// <paramref name="days"/> with each day's sleep added: a day already read gets it, a day with only sleep gets a
    /// reading of its own.
    /// </summary>
    public static List<HealthDayReading> Merge(IReadOnlyList<HealthDayReading> days, IReadOnlyDictionary<DateTime, SleepDay> sleep)
    {
        var result = days.Select(d => sleep.TryGetValue(d.Date.Date, out var s) ? d with { SleepMinutes = s.Minutes, WokeAt = s.WokeAt } : d).ToList();
        foreach (var (date, s) in sleep.Where(s => days.All(d => d.Date.Date != s.Key)))
            result.Add(new HealthDayReading(date, null, null, null, null, null, null, null, null, s.Minutes, s.WokeAt));
        return result;
    }
}

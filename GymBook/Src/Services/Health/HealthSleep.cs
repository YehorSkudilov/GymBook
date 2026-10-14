namespace GymBook.Services.Health;

/// <summary>
/// Sleep from the health apps, a day at a time: minutes asleep by the day it ended on (the night woken up from, plus naps),
/// its deep and REM sleep and sleep score when recorded, and when the night's sleep ended (bedtime learns the usual
/// wake-up time from it).
/// </summary>
public static class HealthSleep
{
    /// <summary>
    /// Each day from <paramref name="from"/> to <paramref name="to"/> (local midnights), by the day each sleep ended on:
    /// minutes asleep, when its longest sleep ended, and the minutes of deep and REM sleep. Sleeps that overlap (two apps
    /// recording the same night, a phone and a watch) count once, with the stages of the one that recorded the most of
    /// them; time awake in those stages isn't counted as asleep.
    /// </summary>
    public static Dictionary<DateTime, SleepDay> ByWakeDay(IEnumerable<SleepRecord> sleeps, DateTime from, DateTime to)
    {
        var merged = new List<(DateTime Start, DateTime End, List<SleepRecord> Records)>();
        foreach (var sleep in sleeps.Where(s => s.End > s.Start).OrderBy(s => s.Start))
        {
            if (merged.Count > 0 && sleep.Start <= merged[^1].End)
            {
                var last = merged[^1];
                last.Records.Add(sleep);
                merged[^1] = (last.Start, sleep.End > last.End ? sleep.End : last.End, last.Records);
            }
            else
                merged.Add((sleep.Start, sleep.End, [sleep]));
        }
        return merged.Where(s => s.End >= from && s.End < to).GroupBy(s => s.End.Date).ToDictionary(
            g => g.Key,
            g =>
            {
                int asleep = 0, deep = 0, rem = 0;
                var staged = false;
                foreach (var night in g)
                {
                    var length = (night.End - night.Start).TotalMinutes;
                    var best = night.Records.MaxBy(r => r.Stages.Sum(st => (st.End - st.Start).TotalMinutes))!;
                    if (best.Stages.Count == 0)
                    {
                        asleep += (int)length;
                        continue;
                    }
                    staged = true;
                    double Minutes(params SleepStageKind[] kinds) =>
                        best.Stages.Where(st => kinds.Contains(st.Kind) && st.End > st.Start).Sum(st => (st.End - st.Start).TotalMinutes);
                    asleep += (int)Math.Max(0, length - Minutes(SleepStageKind.Awake));
                    deep += (int)Minutes(SleepStageKind.Deep);
                    rem += (int)Minutes(SleepStageKind.Rem);
                }
                return new SleepDay(Math.Min(1440, asleep), g.MaxBy(s => s.End - s.Start).End,
                    staged ? Math.Min(1440, deep) : null, staged ? Math.Min(1440, rem) : null);
            });
    }

    /// <summary>
    /// <paramref name="days"/> with each day's sleep added: a day already read gets it, a day with only sleep gets a
    /// reading of its own.
    /// </summary>
    public static List<HealthDayReading> Merge(IReadOnlyList<HealthDayReading> days, IReadOnlyDictionary<DateTime, SleepDay> sleep)
    {
        var result = days.Select(d => sleep.TryGetValue(d.Date.Date, out var s)
            ? d with { SleepMinutes = s.Minutes, WokeAt = s.WokeAt, SleepDeepMinutes = s.DeepMinutes, SleepRemMinutes = s.RemMinutes, SleepScore = s.Score }
            : d).ToList();
        foreach (var (date, s) in sleep.Where(s => days.All(d => d.Date.Date != s.Key)))
            result.Add(new HealthDayReading(date, null, null, null, null, null, null, null, null, s.Minutes, s.WokeAt, s.DeepMinutes, s.RemMinutes, s.Score));
        return result;
    }
}

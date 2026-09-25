using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkLedger.WPF
{
    /// <summary>
    /// Результат агрегации статистики за период.
    /// </summary>
    internal sealed class AggregatedStats
    {
        public long TotalUptimeSeconds { get; set; }
        public long TotalTrackedSeconds { get; set; }
        public Dictionary<string, long> AppTimes { get; set; } = new();
        public string? TopAppName { get; set; }
        public long TopAppSeconds { get; set; }
        public int DaysWithData { get; set; }

        public List<KeyValuePair<string, long>> GetTopApps(int count)
        {
            return AppTimes
                .OrderByDescending(kv => kv.Value)
                .Take(count)
                .ToList();
        }

        public long GetAppsTotalSeconds()
        {
            return AppTimes.Values.Sum();
        }
    }

    /// <summary>
    /// Агрегирует статистику за период.
    /// </summary>
    internal static class StatsAggregator
    {
        public static AggregatedStats Aggregate(StatsPeriod period)
        {
            var (from, to) = GetDateRange(period);
            var allStats = StorageManager.LoadRange(from, to);
            return AggregateFromStats(allStats);
        }

        public static AggregatedStats AggregateFromStats(List<DailyStats> statsList)
        {
            var result = new AggregatedStats();

            foreach (var day in statsList)
            {
                if (day == null) continue;

                result.TotalUptimeSeconds += day.TotalUptimeSeconds;
                result.TotalTrackedSeconds += day.TotalTrackedSeconds;

                if (day.AppTimes.Count > 0)
                    result.DaysWithData++;

                foreach (var kv in day.AppTimes)
                {
                    if (result.AppTimes.TryGetValue(kv.Key, out long current))
                        result.AppTimes[kv.Key] = current + kv.Value;
                    else
                        result.AppTimes[kv.Key] = kv.Value;
                }
            }

            if (result.AppTimes.Count > 0)
            {
                var top = result.AppTimes.OrderByDescending(kv => kv.Value).First();
                result.TopAppName = top.Key;
                result.TopAppSeconds = top.Value;
            }

            return result;
        }

        public static List<AppSession> AggregateSessions(StatsPeriod period)
        {
            var (from, to) = GetDateRange(period);
            var allStats = StorageManager.LoadRange(from, to);

            var sessions = new List<AppSession>();
            foreach (var day in allStats)
            {
                if (day?.Sessions == null) continue;
                sessions.AddRange(day.Sessions);
            }

            sessions.Sort((a, b) => b.Start.CompareTo(a.Start));
            return sessions;
        }

        public static (DateTime from, DateTime to) GetDateRange(StatsPeriod period)
        {
            var today = DateTime.Now.Date;

            return period switch
            {
                StatsPeriod.Today => (today, today),
                StatsPeriod.Yesterday => (today.AddDays(-1), today.AddDays(-1)),
                StatsPeriod.Last7Days => (today.AddDays(-6), today),
                StatsPeriod.Last30Days => (today.AddDays(-29), today),
                StatsPeriod.AllTime => (FindEarliestDate(), today),
                _ => (today, today)
            };
        }

        private static DateTime FindEarliestDate()
        {
            var dates = StorageManager.GetAllDates();
            if (dates.Count == 0) return DateTime.Now.Date;
            return dates.Min();
        }
    }
}
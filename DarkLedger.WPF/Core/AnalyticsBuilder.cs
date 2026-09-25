using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkLedger.WPF
{
    /// <summary>
    /// Данные для heatmap 24×7.
    /// </summary>
    internal sealed class HeatmapData
    {
        public long[,] Cells { get; } = new long[7, 24];
        public long MaxValue { get; set; }
        public long TotalSeconds { get; set; }

        public void Add(int dayOfWeek, int hour, long seconds)
        {
            if (dayOfWeek < 0 || dayOfWeek > 6) return;
            if (hour < 0 || hour > 23) return;

            Cells[dayOfWeek, hour] += seconds;
            TotalSeconds += seconds;

            if (Cells[dayOfWeek, hour] > MaxValue)
                MaxValue = Cells[dayOfWeek, hour];
        }
    }

    /// <summary>
    /// Индекс фокуса за период.
    /// </summary>
    internal sealed class FocusIndex
    {
        public int TotalSessions { get; set; }
        public int FocusSessions { get; set; }
        public long FocusSeconds { get; set; }
        public long TotalSeconds { get; set; }

        public double Percent
        {
            get
            {
                if (TotalSeconds <= 0) return 0;
                return Math.Round(FocusSeconds * 100.0 / TotalSeconds, 1);
            }
        }

        public double AverageSessionSeconds
        {
            get
            {
                if (TotalSessions <= 0) return 0;
                return Math.Round((double)TotalSeconds / TotalSessions, 0);
            }
        }
    }

    /// <summary>
    /// Сравнение двух периодов.
    /// </summary>
    internal sealed class PeriodComparison
    {
        public long CurrentSeconds { get; set; }
        public long PreviousSeconds { get; set; }

        public double DeltaPercent
        {
            get
            {
                if (PreviousSeconds <= 0) return CurrentSeconds > 0 ? 100 : 0;
                return Math.Round((CurrentSeconds - PreviousSeconds) * 100.0 / PreviousSeconds, 1);
            }
        }

        public string DeltaText
        {
            get
            {
                if (PreviousSeconds <= 0 && CurrentSeconds <= 0) return "—";
                if (PreviousSeconds <= 0) return "+100%";
                double d = DeltaPercent;
                if (d > 0) return $"+{d:F1}%";
                if (d < 0) return $"{d:F1}%";
                return "0%";
            }
        }
    }

    /// <summary>
    /// Строит данные для вкладки "Аналитика".
    /// </summary>
    internal static class AnalyticsBuilder
    {
        public static HeatmapData BuildHeatmap(int days = 30)
        {
            var result = new HeatmapData();

            var today = DateTime.Now.Date;
            var from = today.AddDays(-(days - 1));

            var allStats = StorageManager.LoadRange(from, today);

            foreach (var day in allStats)
            {
                if (day?.Sessions == null) continue;

                foreach (var session in day.Sessions)
                {
                    if (session.DurationSeconds <= 0) continue;

                    var start = session.Start;
                    var end = session.End;

                    if (end.Date > start.Date)
                        end = start.Date.AddDays(1).AddSeconds(-1);

                    var cursor = start;

                    while (cursor < end)
                    {
                        var hourEnd = new DateTime(cursor.Year, cursor.Month, cursor.Day,
                            cursor.Hour, 59, 59).AddSeconds(1);

                        var segmentEnd = hourEnd < end ? hourEnd : end;
                        long segmentSeconds = (long)(segmentEnd - cursor).TotalSeconds;

                        if (segmentSeconds > 0)
                        {
                            int dow = ((int)cursor.DayOfWeek + 6) % 7;
                            int hour = cursor.Hour;
                            result.Add(dow, hour, segmentSeconds);
                        }

                        cursor = hourEnd;
                    }
                }
            }

            return result;
        }

        public static FocusIndex BuildFocusIndex(StatsPeriod period)
        {
            var result = new FocusIndex();
            const int FocusThresholdSeconds = 25 * 60;

            var (from, to) = StatsAggregator.GetDateRange(period);
            var allStats = StorageManager.LoadRange(from, to);

            foreach (var day in allStats)
            {
                if (day?.Sessions == null) continue;

                foreach (var session in day.Sessions)
                {
                    if (session.DurationSeconds <= 0) continue;

                    result.TotalSessions++;
                    result.TotalSeconds += session.DurationSeconds;

                    if (session.DurationSeconds >= FocusThresholdSeconds)
                    {
                        result.FocusSessions++;
                        result.FocusSeconds += session.DurationSeconds;
                    }
                }
            }

            return result;
        }

        public static PeriodComparison BuildWeekComparison()
        {
            var today = DateTime.Now.Date;

            int dow = ((int)today.DayOfWeek + 6) % 7;
            var thisWeekStart = today.AddDays(-dow);
            var thisWeekEnd = thisWeekStart.AddDays(6);

            var prevWeekStart = thisWeekStart.AddDays(-7);
            var prevWeekEnd = thisWeekStart.AddDays(-1);

            long current = SumTrackedSeconds(thisWeekStart, thisWeekEnd);
            long previous = SumTrackedSeconds(prevWeekStart, prevWeekEnd);

            return new PeriodComparison
            {
                CurrentSeconds = current,
                PreviousSeconds = previous
            };
        }

        private static long SumTrackedSeconds(DateTime from, DateTime to)
        {
            var stats = StorageManager.LoadRange(from, to);
            long total = 0;
            foreach (var day in stats)
            {
                if (day != null) total += day.TotalTrackedSeconds;
            }
            return total;
        }

        public static List<KeyValuePair<string, long>> GetTopApps(StatsPeriod period, int count = 5)
        {
            var stats = StatsAggregator.Aggregate(period);
            return stats.GetTopApps(count);
        }

        public static readonly string[] DayNames =
        {
            "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс"
        };
    }
}
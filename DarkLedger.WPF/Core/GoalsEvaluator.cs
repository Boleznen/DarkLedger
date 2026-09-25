using System;
using System.Collections.Generic;
using System.Linq;

namespace DarkLedger.WPF
{
    internal sealed class GoalResult
    {
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsViolation { get; set; }
        public bool IsWarning { get; set; }
        public double Percent { get; set; }
    }

    internal static class GoalsEvaluator
    {
        public static GoalResult CheckDailyLimit()
        {
            var settings = SettingsManager.Current;

            if (settings.DailyLimitHours <= 0 || string.IsNullOrWhiteSpace(settings.LimitedApps))
            {
                return new GoalResult
                {
                    Title = "Лимит дня",
                    Description = "Не настроен",
                    IsViolation = false,
                    IsWarning = false,
                    Percent = 0
                };
            }

            var limited = settings.LimitedApps
                .Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrEmpty(x))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var stats = StatsAggregator.Aggregate(StatsPeriod.Today);

            long total = 0;
            foreach (var kv in stats.AppTimes)
            {
                if (limited.Contains(kv.Key))
                    total += kv.Value;
            }

            long limitSeconds = settings.DailyLimitHours * 3600L;
            double percent = limitSeconds > 0
                ? Math.Round(total * 100.0 / limitSeconds, 1)
                : 0;

            return new GoalResult
            {
                Title = "Лимит дня",
                Description = $"{StatsFormatter.FormatDuration(total)} из " +
                              $"{settings.DailyLimitHours}ч",
                IsViolation = total >= limitSeconds,
                IsWarning = total >= limitSeconds * 0.8 && total < limitSeconds,
                Percent = percent
            };
        }

        public static GoalResult CheckMinFocus()
        {
            var settings = SettingsManager.Current;

            if (settings.MinFocusMinutes <= 0)
            {
                return new GoalResult
                {
                    Title = "Минимум фокуса",
                    Description = "Не настроен",
                    IsViolation = false,
                    IsWarning = false,
                    Percent = 0
                };
            }

            var focus = AnalyticsBuilder.BuildFocusIndex(StatsPeriod.Today);
            long focusSeconds = focus.FocusSeconds;
            long requiredSeconds = settings.MinFocusMinutes * 60L;

            double percent = requiredSeconds > 0
                ? Math.Round(focusSeconds * 100.0 / requiredSeconds, 1)
                : 0;

            return new GoalResult
            {
                Title = "Минимум фокуса",
                Description = $"{StatsFormatter.FormatDuration(focusSeconds)} из " +
                              $"{settings.MinFocusMinutes}м",
                IsViolation = focusSeconds < requiredSeconds,
                IsWarning = false,
                Percent = Math.Min(percent, 100)
            };
        }

        public static List<GoalResult> GetAllGoals()
        {
            return new List<GoalResult>
            {
                CheckDailyLimit(),
                CheckMinFocus()
            };
        }
    }
}
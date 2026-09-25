using System;

namespace DarkLedger.WPF
{
    public static class StatsFormatter
    {
        public static string FormatDuration(long totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;

            long hours = totalSeconds / 3600;
            long minutes = (totalSeconds % 3600) / 60;
            long seconds = totalSeconds % 60;

            if (hours > 0)
                return $"{hours}ч {minutes}м";
            if (minutes > 0)
                return $"{minutes}м {seconds}с";
            return $"{seconds}с";
        }

        public static string FormatUptime(long totalSeconds)
        {
            if (totalSeconds < 0) totalSeconds = 0;

            long hours = totalSeconds / 3600;
            long minutes = (totalSeconds % 3600) / 60;
            long seconds = totalSeconds % 60;

            return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
        }

        public static double GetPercent(long part, long total)
        {
            if (total <= 0) return 0;
            return Math.Round(part * 100.0 / total, 1);
        }

        public static string FormatPercent(double percent)
        {
            return $"{percent:F1}%";
        }
    }
}
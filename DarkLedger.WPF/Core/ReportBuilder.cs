using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DarkLedger.WPF
{
    internal static class ReportBuilder
    {
        public class ReportRow
        {
            public string AppName { get; set; } = "";
            public string DisplayName { get; set; } = "";
            public long Seconds { get; set; }
            public double Percent { get; set; }
            public string DurationText => StatsFormatter.FormatDuration(Seconds);
            public string PercentText => StatsFormatter.FormatPercent(Percent);
        }

        public static List<ReportRow> BuildRows(StatsPeriod period)
        {
            var stats = StatsAggregator.Aggregate(period);
            long total = stats.GetAppsTotalSeconds();

            var rows = new List<ReportRow>();
            foreach (var kv in stats.AppTimes.OrderByDescending(x => x.Value))
            {
                rows.Add(new ReportRow
                {
                    AppName = kv.Key,
                    DisplayName = ProcessNameMapper.GetDisplayName(kv.Key),
                    Seconds = kv.Value,
                    Percent = StatsFormatter.GetPercent(kv.Value, total)
                });
            }
            return rows;
        }

        public static string BuildTextReport(StatsPeriod period)
        {
            var stats = StatsAggregator.Aggregate(period);
            var rows = BuildRows(period);
            var (from, to) = StatsAggregator.GetDateRange(period);

            var sb = new StringBuilder();

            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine("           DARK LEDGER — ОТЧЁТ");
            sb.AppendLine("═══════════════════════════════════════════════════════");
            sb.AppendLine();
            sb.AppendLine($"Период: {period.ToDisplayName()}");
            sb.AppendLine($"Даты:   {from:dd.MM.yyyy} — {to:dd.MM.yyyy}");
            sb.AppendLine($"Сформирован: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            sb.AppendLine();

            sb.AppendLine("─── СВОДКА ────────────────────────────────────────────");
            sb.AppendLine($"Uptime:           {StatsFormatter.FormatDuration(stats.TotalUptimeSeconds)}");
            sb.AppendLine($"Отслежено:        {StatsFormatter.FormatDuration(stats.TotalTrackedSeconds)}");
            sb.AppendLine($"Приложений:       {stats.AppTimes.Count}");
            sb.AppendLine($"Дней с данными:   {stats.DaysWithData}");
            sb.AppendLine();

            if (stats.TopAppName != null)
            {
                sb.AppendLine("─── ГЛАВНОЕ ПРИЛОЖЕНИЕ ────────────────────────────────");
                sb.AppendLine($"  {ProcessNameMapper.GetDisplayName(stats.TopAppName)} — {StatsFormatter.FormatDuration(stats.TopAppSeconds)}");
                sb.AppendLine();
            }

            sb.AppendLine("─── ТОП ПРИЛОЖЕНИЙ ────────────────────────────────────");
            sb.AppendLine();
            sb.AppendLine($"{"#",-4} {"Приложение",-32} {"Время",-14} {"Процент",8}");
            sb.AppendLine(new string('─', 62));

            int i = 1;
            foreach (var row in rows.Take(50))
            {
                string num = $"{i++}.";
                sb.AppendLine($"{num,-4} {Truncate(row.DisplayName, 32),-32} {row.DurationText,-14} {row.PercentText,8}");
            }

            if (rows.Count > 50)
            {
                sb.AppendLine();
                sb.AppendLine($"... и ещё {rows.Count - 50} приложений");
            }

            sb.AppendLine();
            sb.AppendLine("───────────────────────────────────────────────────────");
            sb.AppendLine("\"Ledger помнит всё. Но никому не расскажет.\"");
            sb.AppendLine("Dark Ledger v2.0.0 — @Boleznen");
            sb.AppendLine("═══════════════════════════════════════════════════════");

            return sb.ToString();
        }

        public static string BuildCsvReport(StatsPeriod period)
        {
            var stats = StatsAggregator.Aggregate(period);
            var rows = BuildRows(period);

            var sb = new StringBuilder();

            sb.AppendLine("Application;DisplayName;Seconds;Duration;Percent");

            foreach (var row in rows)
            {
                string appName = EscapeCsv(row.AppName);
                string display = EscapeCsv(row.DisplayName);
                sb.AppendLine($"{appName};{display};{row.Seconds};{row.DurationText};{row.PercentText}");
            }

            sb.AppendLine();
            sb.AppendLine($"TOTAL;;;;;{StatsFormatter.FormatDuration(stats.GetAppsTotalSeconds())}");

            return sb.ToString();
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength - 1) + "…";
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }
    }
}
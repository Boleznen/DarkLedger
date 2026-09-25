using System;

namespace DarkLedger.WPF
{
    /// <summary>
    /// Одна сессия активности приложения.
    /// </summary>
    public class AppSession
    {
        public string App { get; set; } = "";
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public long DurationSeconds { get; set; }

        public string StartText => Start.ToString("HH:mm:ss");
        public string EndText => End.ToString("HH:mm:ss");
        public string DisplayName => ProcessNameMapper.GetDisplayName(App);
        public string DurationText => StatsFormatter.FormatDuration(DurationSeconds);
    }
}
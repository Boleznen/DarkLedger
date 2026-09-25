using System;
using System.Collections.Generic;

namespace DarkLedger.WPF
{
    /// <summary>
    /// Статистика за один день. Сохраняется в %APPDATA%\DarkLedger\Stats\yyyy-MM-dd.json
    /// </summary>
    public class DailyStats
    {
        public string Date { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");
        public long TotalUptimeSeconds { get; set; }
        public long TotalTrackedSeconds { get; set; }
        public Dictionary<string, long> AppTimes { get; set; } = new();
        public List<AppSession> Sessions { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }
}
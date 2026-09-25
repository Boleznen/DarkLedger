namespace DarkLedger.WPF
{
    /// <summary>
    /// Настройки приложения. Сохраняются в %APPDATA%\DarkLedger\settings.json
    /// </summary>
    public class AppSettings
    {
        // ==================== ТРЕКИНГ ====================
        public int PollingIntervalMs { get; set; } = 1000;
        public int IdleThresholdSeconds { get; set; } = 300;
        public bool TrackIdle { get; set; } = true;

        // ==================== ОКНО / ТРЕЙ ====================
        public bool MinimizeToTray { get; set; } = false;
        public bool CloseToTray { get; set; } = false;
        public bool StartWithWindows { get; set; } = false;

        // ==================== ЛОГИ ====================
        public int MaxLogFiles { get; set; } = 30;

        // ==================== UI ====================
        public StatsPeriod LastPeriod { get; set; } = StatsPeriod.Today;

        // ==================== ЦЕЛИ И ЛИМИТЫ ====================
        public int DailyLimitHours { get; set; } = -1;
        public string LimitedApps { get; set; } = "";
        public int MinFocusMinutes { get; set; } = -1;
        public bool ShowLimitNotifications { get; set; } = true;
    }
}
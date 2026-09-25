using System;
using System.Windows.Threading;

namespace DarkLedger.WPF
{
    /// <summary>
    /// Счётчик времени по приложениям + запись сессий.
    /// Раз в секунду спрашивает активное окно и накапливает секунды.
    /// WPF-версия — использует DispatcherTimer.
    /// </summary>
    internal sealed class UsageTracker : IDisposable
    {
        private readonly DispatcherTimer _timer;
        private DailyStats _todayStats;
        private string _lastDate;
        private DateTime _sessionStart;
        private bool _isPaused;
        private bool _disposed;

        private AppSession? _currentSession;
        private string? _currentSessionApp;

        private const int SaveEveryNTicks = 30;
        private int _tickCounter;

        public event Action? StatsUpdated;

        public UsageTracker()
        {
            _todayStats = StorageManager.Load(DateTime.Now);
            _lastDate = DateTime.Now.ToString("yyyy-MM-dd");
            _sessionStart = DateTime.Now;

            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(SettingsManager.Current.PollingIntervalMs)
            };
            _timer.Tick += OnTick;
        }

        public void Start()
        {
            if (_disposed) return;
            _isPaused = false;
            _timer.Start();
            Logger.Info("Трекинг запущен");
        }

        public void Pause()
        {
            _isPaused = true;
            _timer.Stop();
            CloseCurrentSession();
            Logger.Info("Трекинг приостановлен");
        }

        public void Resume()
        {
            if (_disposed) return;
            _isPaused = false;
            _timer.Start();
            Logger.Info("Трекинг возобновлён");
        }

        public bool IsPaused => _isPaused;

        public DailyStats TodayStats => _todayStats;
        public DateTime SessionStart => _sessionStart;

        private void OnTick(object? sender, EventArgs e)
        {
            try
            {
                CheckDateRollover();

                var settings = SettingsManager.Current;

                bool idle = settings.TrackIdle && WindowTracker.IsUserIdle(settings.IdleThresholdSeconds);
                if (idle)
                {
                    CloseCurrentSession();
                    _todayStats.TotalUptimeSeconds++;
                    NotifyUpdated();
                    return;
                }

                string? processName = WindowTracker.GetActiveProcessName();

                _todayStats.TotalUptimeSeconds++;
                _todayStats.TotalTrackedSeconds++;

                if (!string.IsNullOrEmpty(processName))
                {
                    if (_todayStats.AppTimes.TryGetValue(processName, out long current))
                        _todayStats.AppTimes[processName] = current + 1;
                    else
                        _todayStats.AppTimes[processName] = 1;

                    UpdateSession(processName);
                }

                _tickCounter++;
                if (_tickCounter >= SaveEveryNTicks)
                {
                    _tickCounter = 0;
                    StorageManager.Save(_todayStats);
                }

                NotifyUpdated();
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка в тике UsageTracker", ex);
            }
        }

        private void UpdateSession(string processName)
        {
            var now = DateTime.Now;

            if (_currentSession != null && _currentSessionApp == processName)
            {
                _currentSession.End = now;
                _currentSession.DurationSeconds++;
                return;
            }

            CloseCurrentSession();

            _currentSessionApp = processName;
            _currentSession = new AppSession
            {
                App = processName,
                Start = now,
                End = now,
                DurationSeconds = 1
            };

            _todayStats.Sessions.Add(_currentSession);
        }

        private void CloseCurrentSession()
        {
            if (_currentSession == null) return;

            if (_currentSession.DurationSeconds < 3)
            {
                _todayStats.Sessions.Remove(_currentSession);
            }

            _currentSession = null;
            _currentSessionApp = null;
        }

        private void CheckDateRollover()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            if (today == _lastDate) return;

            CloseCurrentSession();

            Logger.Info($"Смена дня: {_lastDate} → {today}");
            StorageManager.Save(_todayStats);

            _lastDate = today;
            _todayStats = new DailyStats { Date = today };
            _tickCounter = 0;
        }

        private void NotifyUpdated()
        {
            try { StatsUpdated?.Invoke(); } catch { }
        }

        public void SaveNow()
        {
            try { StorageManager.Save(_todayStats); } catch { }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            try
            {
                _timer.Stop();
                CloseCurrentSession();
                SaveNow();
                Logger.Info("Трекинг остановлен, статистика сохранена");
            }
            catch { }
        }
    }
}
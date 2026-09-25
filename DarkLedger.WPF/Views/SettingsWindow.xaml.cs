using System;
using System.Windows;
using System.Windows.Input;

namespace DarkLedger.WPF.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettingsToUi();
        }

        // ==================== TITLE BAR ====================

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { this.DragMove(); } catch { }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // ==================== ЗАГРУЗКА ====================

        private void LoadSettingsToUi()
        {
            var s = SettingsManager.Current;

            PollingIntervalBox.Text = Math.Max(100, Math.Min(10000, s.PollingIntervalMs)).ToString();
            IdleThresholdBox.Text = Math.Max(30, Math.Min(3600, s.IdleThresholdSeconds)).ToString();
            TrackIdleCheck.IsChecked = s.TrackIdle;

            MinimizeToTrayCheck.IsChecked = s.MinimizeToTray;
            CloseToTrayCheck.IsChecked = s.CloseToTray;
            StartWithWindowsCheck.IsChecked = s.StartWithWindows;

            MaxLogFilesBox.Text = Math.Max(5, Math.Min(500, s.MaxLogFiles)).ToString();

            DailyLimitCheck.IsChecked = s.DailyLimitHours > 0;
            DailyLimitBox.Text = (s.DailyLimitHours > 0
                ? Math.Max(1, Math.Min(24, s.DailyLimitHours))
                : 2).ToString();
            LimitedAppsBox.Text = s.LimitedApps;

            MinFocusCheck.IsChecked = s.MinFocusMinutes > 0;
            MinFocusBox.Text = (s.MinFocusMinutes > 0
                ? Math.Max(1, Math.Min(1440, s.MinFocusMinutes))
                : 240).ToString();

            ShowLimitNotificationsCheck.IsChecked = s.ShowLimitNotifications;
        }

        // ==================== СОХРАНЕНИЕ ====================

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var s = SettingsManager.Current;

            s.PollingIntervalMs = ParseInt(PollingIntervalBox.Text, 1000, 100, 10000);
            s.IdleThresholdSeconds = ParseInt(IdleThresholdBox.Text, 300, 30, 3600);
            s.TrackIdle = TrackIdleCheck.IsChecked == true;

            s.MinimizeToTray = MinimizeToTrayCheck.IsChecked == true;
            s.CloseToTray = CloseToTrayCheck.IsChecked == true;

            bool oldStartup = s.StartWithWindows;
            bool newStartup = StartWithWindowsCheck.IsChecked == true;
            if (oldStartup != newStartup)
            {
                if (StartupManager.SetEnabled(newStartup))
                    s.StartWithWindows = newStartup;
                else
                    DarkMessageBox.ShowWarning("Не удалось изменить автозагрузку.", "Настройки");
            }

            s.MaxLogFiles = ParseInt(MaxLogFilesBox.Text, 30, 5, 500);

            s.DailyLimitHours = DailyLimitCheck.IsChecked == true
                ? ParseInt(DailyLimitBox.Text, 2, 1, 24)
                : -1;
            s.LimitedApps = LimitedAppsBox.Text.Trim();

            s.MinFocusMinutes = MinFocusCheck.IsChecked == true
                ? ParseInt(MinFocusBox.Text, 240, 1, 1440)
                : -1;

            s.ShowLimitNotifications = ShowLimitNotificationsCheck.IsChecked == true;

            SettingsManager.SaveCurrent();
            Logger.Info("Настройки сохранены через форму");

            DialogResult = true;
            Close();
        }

        // ==================== СБРОС / ЭКСПОРТ / ИМПОРТ ====================

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            var answer = DarkMessageBox.ShowQuestion("Сбросить все настройки?", "Сброс");
            if (answer != MessageBoxResult.Yes) return;

            SettingsManager.ResetToDefaults();
            LoadSettingsToUi();
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "JSON (*.json)|*.json",
                FileName = $"darkledger_settings_{DateTime.Now:yyyy-MM-dd}.json",
                Title = "Экспорт настроек"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                string json = System.Text.Json.JsonSerializer.Serialize(
                    SettingsManager.Current,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        WriteIndented = true,
                        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                    });

                System.IO.File.WriteAllText(dlg.FileName, json);
                DarkMessageBox.ShowInfo($"Настройки экспортированы:\n{dlg.FileName}", "Готово");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка экспорта настроек", ex);
                DarkMessageBox.ShowError("Не удалось экспортировать.", "Ошибка");
            }
        }

        private void Import_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "JSON (*.json)|*.json",
                Title = "Импорт настроек"
            };

            if (dlg.ShowDialog() != true) return;

            try
            {
                string json = System.IO.File.ReadAllText(dlg.FileName);
                var loaded = System.Text.Json.JsonSerializer.Deserialize<AppSettings>(json,
                    new System.Text.Json.JsonSerializerOptions
                    {
                        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
                    });

                if (loaded == null)
                {
                    DarkMessageBox.ShowError("Не удалось импортировать.", "Ошибка");
                    return;
                }

                SettingsManager.Save(loaded);
                LoadSettingsToUi();
                DarkMessageBox.ShowInfo("Настройки импортированы.", "Готово");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка импорта настроек", ex);
                DarkMessageBox.ShowError("Не удалось импортировать.", "Ошибка");
            }
        }

        private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string dir = SettingsManager.GetDataFolderPath();
                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{dir}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Не удалось открыть папку данных", ex);
            }
        }

        // ==================== УТИЛИТА ====================

        private static int ParseInt(string text, int fallback, int min, int max)
        {
            if (!int.TryParse(text, out int val)) return fallback;
            if (val < min) val = min;
            if (val > max) val = max;
            return val;
        }
    }
}
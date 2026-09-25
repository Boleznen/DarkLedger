using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DarkLedger.WPF.Views;

namespace DarkLedger.WPF
{
    public partial class MainWindow : Window
    {
        // ==================== СЕРВИСЫ ====================
        private readonly UsageTracker _tracker;
        private readonly DispatcherTimer _uptimeTimer;
        private readonly DispatcherTimer _goalsTimer;
        private readonly TrayManager _trayManager;

        // ==================== СОСТОЯНИЕ ====================
        private DateTime _lastGoalsCheck = DateTime.MinValue;
        private bool _realClose;

        // ==================== КОНСТРУКТОР ====================
        public MainWindow()
        {
            InitializeComponent();

            // Иконка
            try
            {
                var iconUri = new Uri("pack://application:,,,/darkledger.ico");
                this.Icon = new BitmapImage(iconUri);
                TitleIcon.Source = new BitmapImage(iconUri);
            }
            catch (Exception ex)
            {
                Logger.Error("Не удалось загрузить иконку", ex);
            }

            // Трекер
            _tracker = new UsageTracker();
            _tracker.StatsUpdated += Tracker_StatsUpdated;
            _tracker.Start();

            // Uptime таймер
            _uptimeTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _uptimeTimer.Tick += UptimeTimer_Tick;
            _uptimeTimer.Start();

            // Goals таймер
            _goalsTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
            _goalsTimer.Tick += (s, e) => CheckGoals();
            _goalsTimer.Start();

            // Горячие клавиши
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            Logger.Info("MainWindow создан, трекер запущен");

            // Трей
            _trayManager = new TrayManager();
            _trayManager.Initialize();
            _trayManager.OnShowRequested += () => Dispatcher_ShowMain();
            _trayManager.OnSettingsRequested += () => Dispatcher_OpenSettings();
            _trayManager.OnToggleTrackingRequested += () => Dispatcher_ToggleTracking();
            _trayManager.OnExitRequested += () => Dispatcher_Exit();
            if (SettingsManager.Current.MinimizeToTray || SettingsManager.Current.CloseToTray)
                _trayManager.Show();

            // Подключаем все вкладки
            OverviewTabHost.Children.Add(new TodayTab());
            ChronicleTabHost.Children.Add(new ChronicleTab());
            AnalyticsTabHost.Children.Add(new AnalyticsTab());
            DiaryTabHost.Children.Add(new DiaryTab());
            ReportsTabHost.Children.Add(new ReportsTab());

            UpdateStatusBar();
        }

        // ==================== TITLE BAR ====================

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                ToggleMaximize();
                return;
            }

            if (e.ButtonState == MouseButtonState.Pressed)
            {
                try { this.DragMove(); } catch { }
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            ToggleMaximize();
        }

        private void ToggleMaximize()
        {
            this.WindowState = this.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;

            MaximizeButton.Content = this.WindowState == WindowState.Maximized ? "❐" : "☐";
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // ==================== FOLDERS ====================

        private void FoldersButton_Click(object sender, RoutedEventArgs e)
        {
            var menu = new System.Windows.Controls.ContextMenu
            {
                Background = (System.Windows.Media.Brush)FindResource("BackgroundCardBrush"),
                Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush"),
                BorderBrush = (System.Windows.Media.Brush)FindResource("BorderBrush"),
                BorderThickness = new Thickness(1)
            };

            AddFolderMenuItem(menu, "📊 Папка статистики", StorageManager.GetStatsFolderPath());
            AddFolderMenuItem(menu, "📝 Папка заметок", GetNotesFolderPath());
            AddFolderMenuItem(menu, "📋 Папка логов", Logger.GetLogsFolderPath());
            AddFolderMenuItem(menu, "📈 Папка отчётов", ReportExporter.GetReportsFolderPath());
            AddFolderMenuItem(menu, "📁 Папка данных", SettingsManager.GetDataFolderPath());
            AddFolderMenuItem(menu, "⚙️ Файл настроек", SettingsManager.GetSettingsFilePath(), isFile: true);

            menu.PlacementTarget = FoldersButton;
            menu.IsOpen = true;
        }

        private void AddFolderMenuItem(System.Windows.Controls.ContextMenu menu, string text, string path, bool isFile = false)
        {
            var item = new System.Windows.Controls.MenuItem
            {
                Header = text,
                Foreground = (System.Windows.Media.Brush)FindResource("TextPrimaryBrush")
            };
            item.Click += (s, e) =>
            {
                try
                {
                    if (isFile)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"/select,\"{path}\"",
                            UseShellExecute = true
                        });
                    }
                    else
                    {
                        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"\"{path}\"",
                            UseShellExecute = true
                        });
                    }
                }
                catch (Exception ex) { Logger.Error($"Не удалось открыть: {path}", ex); }
            };
            menu.Items.Add(item);
        }

        private static string GetNotesFolderPath()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string notes = Path.Combine(appData, "DarkLedger", "Notes");
            Directory.CreateDirectory(notes);
            return notes;
        }

        // ==================== MENU ====================

        private void FileExit_Click(object sender, RoutedEventArgs e)
        {
            _realClose = true;
            Close();
        }

        private void ExportZipMenu_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "ZIP-архив (*.zip)|*.zip",
                FileName = DataExporter.GetDefaultFileName(),
                Title = "Экспорт всей статистики Dark Ledger"
            };

            if (dlg.ShowDialog() != true) return;

            var result = DataExporter.ExportAll(dlg.FileName);
            if (result.Success)
            {
                DarkMessageBox.ShowInfo(
                    $"Экспорт завершён!\n\nФайлов: {result.FileCount}\nРазмер: {FormatSize(result.TotalBytes)}\nПуть: {result.Path}",
                    "Готово");
            }
            else
            {
                DarkMessageBox.ShowError(
                    $"Не удалось экспортировать.\n\n{result.ErrorMessage}",
                    "Ошибка");
            }
        }

        private void ViewRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshCurrentTab();
            UpdateStatusBar();
        }

        private void TrackingPause_Click(object sender, RoutedEventArgs e)
        {
            _tracker.Pause();
            UpdateStatusBar();
        }

        private void TrackingResume_Click(object sender, RoutedEventArgs e)
        {
            _tracker.Resume();
            UpdateStatusBar();
        }

        private void SettingsMenu_Click(object sender, RoutedEventArgs e)
        {
            OpenSettings();
        }

        private void OpenSettings()
        {
            try
            {
                var win = new SettingsWindow { Owner = this };
                win.ShowDialog();
                UpdateStatusBar();
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка открытия настроек", ex);
                DarkMessageBox.ShowError($"Не удалось открыть настройки.\n\n{ex.Message}", "Ошибка");
            }
        }

        private void HelpShortcuts_Click(object sender, RoutedEventArgs e)
        {
            DarkMessageBox.ShowInfo(
                "⌨️ ГОРЯЧИЕ КЛАВИШИ DARK LEDGER\n\n" +
                "─── ВКЛАДКИ ─────────────────────────────\n" +
                "  Ctrl+1        — Обзор\n" +
                "  Ctrl+2        — Хроника\n" +
                "  Ctrl+3        — Аналитика\n" +
                "  Ctrl+4        — Дневник\n" +
                "  Ctrl+5        — Отчёты\n" +
                "  Ctrl+Tab      — следующая вкладка\n" +
                "  Ctrl+Shift+Tab— предыдущая вкладка\n\n" +
                "─── ДЕЙСТВИЯ ────────────────────────────\n" +
                "  Ctrl+E        — Экспорт ZIP\n" +
                "  Ctrl+R        — Обновить вкладку\n" +
                "  Ctrl+S        — Настройки\n" +
                "  Ctrl+Q        — Выход\n" +
                "  F1            — Эта справка",
                "Горячие клавиши");
        }

        private void CheckUpdatesButton_Click(object sender, RoutedEventArgs e)
        {
            CheckUpdatesAsync();
        }

        private async void CheckUpdatesAsync()
        {
            try
            {
                Cursor = Cursors.Wait;

                var result = await UpdateChecker.CheckAsync();

                Cursor = Cursors.Arrow;

                if (result.CheckFailed)
                {
                    var openManual = DarkMessageBox.ShowQuestion(
                        $"{result.Message}\n\nОткрыть страницу релизов вручную?",
                        "Проверка обновлений");

                    if (openManual == MessageBoxResult.Yes)
                        UpdateChecker.OpenReleasesPage();
                    return;
                }

                if (!result.HasUpdate)
                {
                    DarkMessageBox.ShowInfo(
                        $"У вас последняя версия: v{result.CurrentVersion}",
                        "Обновления не найдены");
                    return;
                }

                var openPage = DarkMessageBox.ShowQuestion(
                    $"🎉 Доступна новая версия v{result.LatestVersion}!\n\n" +
                    $"Ваша версия: v{result.CurrentVersion}\n\nОткрыть страницу загрузки?",
                    "Доступно обновление");

                if (openPage == MessageBoxResult.Yes)
                    UpdateChecker.OpenReleaseUrl(result.HtmlUrl);
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Arrow;
                Logger.Error("Ошибка проверки обновлений", ex);
                DarkMessageBox.ShowError(
                    $"Не удалось проверить обновления.\n\n{ex.Message}", "Ошибка");
            }
        }

        private void HelpContact_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new ContactWindow { Owner = this };
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка открытия формы Связь", ex);
            }
        }

        private void HelpAbout_Click(object sender, RoutedEventArgs e)
        {
            DarkMessageBox.ShowInfo(
                "Dark Ledger — Тёмный реестр\n" +
                "Локальный трекер активности для Windows 10/11\n\n" +
                "\"Ledger помнит всё. Но никому не расскажет.\"\n\n" +
                $"Версия: {UpdateChecker.GetCurrentVersion()}\n" +
                $"Разработчик: @Boleznen (Telegram)\n" +
                $"ОС: {Environment.OSVersion}\n" +
                $".NET: {Environment.Version}",
                "О программе Dark Ledger");
        }

        private void StatusContact_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://t.me/Boleznen",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Не удалось открыть Telegram", ex);
            }
        }

        // ==================== ГОРЯЧИЕ КЛАВИШИ ====================

        private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            try
            {
                bool ctrl = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);

                if (ctrl && e.Key >= Key.D1 && e.Key <= Key.D5)
                {
                    int idx = e.Key - Key.D1;
                    if (idx >= 0 && idx < MainTabControl.Items.Count)
                        MainTabControl.SelectedIndex = idx;
                    e.Handled = true;
                    return;
                }

                if (ctrl && e.Key == Key.E) { ExportZipMenu_Click(sender, e); e.Handled = true; return; }
                if (ctrl && e.Key == Key.R) { RefreshCurrentTab(); e.Handled = true; return; }
                if (ctrl && e.Key == Key.S) { OpenSettings(); e.Handled = true; return; }
                if (ctrl && e.Key == Key.Q) { _realClose = true; Close(); e.Handled = true; return; }
                if (e.Key == Key.F1) { HelpShortcuts_Click(sender, e); e.Handled = true; return; }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка обработки горячих клавиш", ex);
            }
        }

        // ==================== ОБНОВЛЕНИЕ ТЕКУЩЕЙ ВКЛАДКИ ====================

        private void RefreshCurrentTab()
        {
            try
            {
                var selectedItem = MainTabControl.SelectedItem as System.Windows.Controls.TabItem;
                if (selectedItem == null) return;

                var content = selectedItem.Content;
                if (content is System.Windows.Controls.Grid host)
                {
                    foreach (UIElement child in host.Children)
                    {
                        var method = child.GetType().GetMethod("RefreshData",
                            System.Reflection.BindingFlags.Public |
                            System.Reflection.BindingFlags.Instance);

                        if (method != null)
                        {
                            method.Invoke(child, null);
                            Logger.Info($"Обновлена вкладка: {selectedItem.Header}");
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка RefreshCurrentTab", ex);
            }
        }

        // ==================== UI ====================

        private void Tracker_StatsUpdated() => UpdateStatusBar();
        private void UptimeTimer_Tick(object? sender, EventArgs e) => UpdateStatusBar();

        private void UpdateStatusBar()
        {
            try
            {
                long uptime = NativeMethods.GetSystemUptimeSeconds();
                StatusUptime.Text = $"Uptime: {StatsFormatter.FormatUptime(uptime)}";
                StatusTracking.Text = _tracker.IsPaused ? "● Трекинг на паузе" : "● Трекинг активен";
            }
            catch { }
        }

        // ==================== МЕТОДЫ ДЛЯ ТРЕЯ ====================

        private void Dispatcher_ShowMain()
        {
            Show();
            WindowState = WindowState.Normal;
            Activate();
            _trayManager.Hide();
        }

        private void Dispatcher_OpenSettings()
        {
            OpenSettings();
        }

        private void Dispatcher_ToggleTracking()
        {
            if (_tracker.IsPaused) _tracker.Resume();
            else _tracker.Pause();
            UpdateStatusBar();
        }

        private void Dispatcher_Exit()
        {
            _realClose = true;
            Close();
        }

        // ==================== ЦЕЛИ ====================

        private void CheckGoals()
        {
            try
            {
                var settings = SettingsManager.Current;
                if (!settings.ShowLimitNotifications) return;
                if ((DateTime.Now - _lastGoalsCheck).TotalMinutes < 10) return;
                _lastGoalsCheck = DateTime.Now;

                var limit = GoalsEvaluator.CheckDailyLimit();
                if (limit.IsViolation)
                    Logger.Info($"Лимит превышен: {limit.Description}");
            }
            catch (Exception ex) { Logger.Error("Ошибка проверки целей", ex); }
        }

        // ==================== СОСТОЯНИЕ ОКНА ====================

        protected override void OnStateChanged(EventArgs e)
        {
            base.OnStateChanged(e);

            var s = SettingsManager.Current;
            if (WindowState == WindowState.Minimized && s.MinimizeToTray)
            {
                _trayManager.Show();
                Hide();
                _trayManager.ShowNotification("Dark Ledger",
                    "Свёрнуто в трей. Двойной клик — открыть.");
            }
        }

        // ==================== ЗАКРЫТИЕ ====================

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!_realClose)
            {
                var s = SettingsManager.Current;
                if (s.CloseToTray && _trayManager.IsVisible)
                {
                    e.Cancel = true;
                    Hide();
                    _trayManager.Show();
                    _trayManager.ShowNotification("Dark Ledger",
                        "Свёрнуто в трей. Двойной клик — открыть.");
                    return;
                }
            }

            try
            {
                _uptimeTimer?.Stop();
                _goalsTimer?.Stop();
                _tracker?.Dispose();
                _trayManager?.Dispose();
                Logger.Info("MainWindow закрыт");
            }
            catch { }
        }

        // ==================== УТИЛИТА ====================

        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} байт";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F2} КБ";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / 1024.0 / 1024.0:F2} МБ";
            return $"{bytes / 1024.0 / 1024.0 / 1024.0:F2} ГБ";
        }
    }
}
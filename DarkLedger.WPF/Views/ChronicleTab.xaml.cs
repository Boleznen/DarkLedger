using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DarkLedger.WPF.Views
{
    public partial class ChronicleTab : UserControl
    {
        // ==================== ТАЙМЕРЫ ====================
        private readonly DispatcherTimer _refreshTimer;
        private readonly DispatcherTimer _searchTimer;

        // ==================== СОСТОЯНИЕ ====================
        private StatsPeriod _currentPeriod = StatsPeriod.Today;
        private List<AppSession> _allSessions = new();
        private DateTime _lastLoadedTime = DateTime.MinValue;
        private bool _isLoadedOnce;

        private const int RefreshIntervalSeconds = 30;
        private const int MaxRowsToRender = 500;

        // ==================== КОНСТРУКТОР ====================
        public ChronicleTab()
        {
            InitializeComponent();

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(RefreshIntervalSeconds) };
            _refreshTimer.Tick += (s, e) => RefreshData();
            _refreshTimer.Start();

            _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _searchTimer.Tick += (s, e) =>
            {
                _searchTimer.Stop();
                ApplyFilters();
            };

            Loaded += (s, e) =>
            {
                // Первый раз — загружаем. Потом — только если данные устарели
                if (!_isLoadedOnce || (DateTime.Now - _lastLoadedTime).TotalSeconds > RefreshIntervalSeconds)
                    RefreshData();
            };

            Unloaded += (s, e) =>
            {
                _refreshTimer.Stop();
                _searchTimer.Stop();
            };
        }

        // ==================== ОБНОВЛЕНИЕ ====================

        public void RefreshData()
        {
            try
            {
                _allSessions = StatsAggregator.AggregateSessions(_currentPeriod);
                _lastLoadedTime = DateTime.Now;
                _isLoadedOnce = true;

                UpdateAppFilterList();
                UpdateStatCards();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка RefreshData на ChronicleTab", ex);
            }
        }

        private void UpdateAppFilterList()
        {
            string prev = AppFilterCombo.SelectedItem?.ToString() ?? "(все)";

            AppFilterCombo.Items.Clear();
            AppFilterCombo.Items.Add("(все)");

            var apps = _allSessions
                .Select(s => s.App)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(a => _allSessions.Where(x => x.App == a).Sum(x => x.DurationSeconds))
                .Take(20)
                .ToList();

            foreach (var app in apps)
                AppFilterCombo.Items.Add(ProcessNameMapper.GetDisplayName(app));

            if (AppFilterCombo.Items.Contains(prev))
                AppFilterCombo.SelectedItem = prev;
            else
                AppFilterCombo.SelectedIndex = 0;
        }

        private void UpdateStatCards()
        {
            SessionsValue.Text = _allSessions.Count.ToString();
            SessionsSub.Text = _currentPeriod.ToDisplayName();

            long totalSec = _allSessions.Sum(s => s.DurationSeconds);
            TotalTimeValue.Text = StatsFormatter.FormatDuration(totalSec);
            TotalTimeSub.Text = totalSec > 0
                ? $"ср. {StatsFormatter.FormatDuration(totalSec / Math.Max(1, _allSessions.Count))}/сессия"
                : "нет данных";

            if (_allSessions.Count > 0)
            {
                var top = _allSessions
                    .GroupBy(s => s.App)
                    .Select(g => new { App = g.Key, Sec = g.Sum(x => x.DurationSeconds) })
                    .OrderByDescending(x => x.Sec)
                    .First();

                TopAppValue.Text = ProcessNameMapper.GetDisplayName(top.App);
                double percent = StatsFormatter.GetPercent(top.Sec, totalSec);
                TopAppSub.Text = $"{StatsFormatter.FormatDuration(top.Sec)} • {StatsFormatter.FormatPercent(percent)}";
            }
            else
            {
                TopAppValue.Text = "—";
                TopAppSub.Text = "нет данных";
            }

            int focusCount = _allSessions.Count(s => s.DurationSeconds >= 25 * 60);
            long focusSec = _allSessions.Where(s => s.DurationSeconds >= 25 * 60).Sum(s => s.DurationSeconds);

            FocusValue.Text = focusCount.ToString();
            if (totalSec > 0)
            {
                double percent = StatsFormatter.GetPercent(focusSec, totalSec);
                FocusSub.Text = $"{StatsFormatter.FormatDuration(focusSec)} • {StatsFormatter.FormatPercent(percent)}";
            }
            else
            {
                FocusSub.Text = "нет данных";
            }
        }

        private void ApplyFilters()
        {
            try
            {
                var filtered = _allSessions.AsEnumerable();

                string query = SearchBox.Text.Trim();
                if (!string.IsNullOrEmpty(query))
                {
                    filtered = filtered.Where(s =>
                        s.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                        s.App.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
                }

                if (AppFilterCombo.SelectedIndex > 0)
                {
                    string selected = AppFilterCombo.SelectedItem?.ToString() ?? "";
                    filtered = filtered.Where(s =>
                        s.DisplayName == selected ||
                        ProcessNameMapper.GetDisplayName(s.App) == selected);
                }

                var list = filtered.ToList();
                UpdateSessionsList(list);

                long totalSec = list.Sum(s => s.DurationSeconds);
                StatusText.Text = $"Показано: {list.Count} сессий  •  Суммарно: {StatsFormatter.FormatDuration(totalSec)}";
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка ApplyFilters на ChronicleTab", ex);
            }
        }

        private void UpdateSessionsList(List<AppSession> sessions)
        {
            SessionsContainer.Children.Clear();

            if (sessions.Count == 0)
            {
                SessionsContainer.Children.Add(new TextBlock
                {
                    Text = "Пока нет сессий. Поработайте за компьютером — они появятся здесь.",
                    Foreground = (Brush)FindResource("TextMutedBrush"),
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                });
                return;
            }

            bool groupByDate = _currentPeriod != StatsPeriod.Today &&
                               _currentPeriod != StatsPeriod.Yesterday;

            string? lastDate = null;
            int rendered = 0;

            foreach (var session in sessions)
            {
                if (rendered >= MaxRowsToRender) break;

                if (groupByDate)
                {
                    string dateKey = session.Start.ToString("dd.MM.yyyy");
                    if (dateKey != lastDate)
                    {
                        var dateLabel = new TextBlock
                        {
                            Text = $"{dateKey} — {GetRussianDayOfWeek(session.Start.DayOfWeek)}",
                            Foreground = (Brush)FindResource("AccentRedBrush"),
                            FontSize = 13,
                            FontWeight = FontWeights.Bold,
                            Margin = new Thickness(4, 12, 0, 6)
                        };
                        SessionsContainer.Children.Add(dateLabel);
                        lastDate = dateKey;
                    }
                }

                var row = CreateSessionRow(session);
                SessionsContainer.Children.Add(row);
                rendered++;
            }

            if (sessions.Count > MaxRowsToRender)
            {
                SessionsContainer.Children.Add(new TextBlock
                {
                    Text = $"... и ещё {sessions.Count - MaxRowsToRender} сессий (уточните фильтр или выберите короткий период)",
                    Foreground = (Brush)FindResource("TextMutedBrush"),
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 12, 0, 6)
                });
            }
        }

        private Border CreateSessionRow(AppSession session)
        {
            var border = new Border
            {
                Background = (Brush)FindResource("BackgroundDarkBrush"),
                Padding = new Thickness(4, 6, 4, 6),
                Margin = new Thickness(0, 0, 0, 2)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(180) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var dot = new Ellipse
            {
                Width = 8,
                Height = 8,
                Fill = GetAppBrush(session.App),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(2, 0, 0, 0)
            };
            Grid.SetColumn(dot, 0);
            grid.Children.Add(dot);

            var time = new TextBlock
            {
                Text = $"{session.StartText} → {session.EndText}",
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                FontFamily = (FontFamily)FindResource("FontFamilyMono"),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(time, 1);
            grid.Children.Add(time);

            if (session.DurationSeconds >= 25 * 60)
            {
                var focusDot = new TextBlock
                {
                    Text = "●",
                    Foreground = (Brush)FindResource("AccentRedBrush"),
                    FontSize = 12,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                Grid.SetColumn(focusDot, 2);
                grid.Children.Add(focusDot);
            }

            var name = new TextBlock
            {
                Text = session.DisplayName,
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(name, 3);
            grid.Children.Add(name);

            var duration = new TextBlock
            {
                Text = session.DurationText,
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(duration, 4);
            grid.Children.Add(duration);

            border.Child = grid;
            return border;
        }

        private static Brush GetAppBrush(string appName)
        {
            if (string.IsNullOrEmpty(appName)) appName = "unknown";

            var palette = new[]
            {
                Color.FromRgb(0x8C, 0x14, 0x1E),
                Color.FromRgb(0x5A, 0x0F, 0x19),
                Color.FromRgb(0x1E, 0x32, 0x6E),
                Color.FromRgb(0x4B, 0x28, 0x6E)
            };

            int hash = 0;
            foreach (char c in appName) hash = (hash * 31 + c) & 0x7FFFFFFF;
            return new SolidColorBrush(palette[hash % palette.Length]);
        }

        private static string GetRussianDayOfWeek(DayOfWeek dow)
        {
            return dow switch
            {
                DayOfWeek.Monday => "Понедельник",
                DayOfWeek.Tuesday => "Вторник",
                DayOfWeek.Wednesday => "Среда",
                DayOfWeek.Thursday => "Четверг",
                DayOfWeek.Friday => "Пятница",
                DayOfWeek.Saturday => "Суббота",
                DayOfWeek.Sunday => "Воскресенье",
                _ => ""
            };
        }

        // ==================== ФИЛЬТРЫ ====================

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not RadioButton rb || rb.Tag is not string tag) return;

            _currentPeriod = tag switch
            {
                "Today" => StatsPeriod.Today,
                "Yesterday" => StatsPeriod.Yesterday,
                "Last7Days" => StatsPeriod.Last7Days,
                "Last30Days" => StatsPeriod.Last30Days,
                _ => StatsPeriod.Today
            };

            RefreshData();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void AppFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }

        private void ClearFilters_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            if (AppFilterCombo.Items.Count > 0)
                AppFilterCombo.SelectedIndex = 0;
            ApplyFilters();
        }

        // ==================== ЭКСПОРТ ====================

        private List<AppSession> GetFilteredSessions()
        {
            var filtered = _allSessions.AsEnumerable();

            string query = SearchBox.Text.Trim();
            if (!string.IsNullOrEmpty(query))
            {
                filtered = filtered.Where(s =>
                    s.DisplayName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    s.App.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            if (AppFilterCombo.SelectedIndex > 0)
            {
                string selected = AppFilterCombo.SelectedItem?.ToString() ?? "";
                filtered = filtered.Where(s =>
                    s.DisplayName == selected ||
                    ProcessNameMapper.GetDisplayName(s.App) == selected);
            }

            return filtered.ToList();
        }

        private void ExportCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sessions = GetFilteredSessions();
                if (sessions.Count == 0)
                {
                    DarkMessageBox.ShowInfo("Нет данных для экспорта.", "Экспорт");
                    return;
                }

                string dir = ReportExporter.GetReportsFolderPath();
                string fileName = $"chronicle_{_currentPeriod}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
                string path = System.IO.Path.Combine(dir, fileName);

                var sb = new StringBuilder();
                sb.AppendLine("Date;Start;End;Duration;App;DisplayName");

                foreach (var s in sessions.OrderBy(x => x.Start))
                {
                    sb.AppendLine(
                        $"{s.Start:yyyy-MM-dd};{s.StartText};{s.EndText};" +
                        $"{s.DurationSeconds};{EscapeCsv(s.App)};{EscapeCsv(s.DisplayName)}");
                }

                System.IO.File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

                StatusText.Text = $"Экспорт CSV: {System.IO.Path.GetFileName(path)}";
                Logger.Info($"Хроника экспортирована в {path}");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка экспорта CSV", ex);
                StatusText.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void ExportTxt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sessions = GetFilteredSessions();
                if (sessions.Count == 0)
                {
                    DarkMessageBox.ShowInfo("Нет данных для экспорта.", "Экспорт");
                    return;
                }

                string dir = ReportExporter.GetReportsFolderPath();
                string fileName = $"chronicle_{_currentPeriod}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
                string path = System.IO.Path.Combine(dir, fileName);

                var sb = new StringBuilder();
                sb.AppendLine("═══════════════════════════════════════════════════");
                sb.AppendLine("       DARK LEDGER — ХРОНИКА СОБЫТИЙ");
                sb.AppendLine("═══════════════════════════════════════════════════");
                sb.AppendLine($"Период: {_currentPeriod.ToDisplayName()}");
                sb.AppendLine($"Сформировано: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                sb.AppendLine($"Сессий: {sessions.Count}");
                sb.AppendLine();

                string? lastDate = null;
                foreach (var s in sessions.OrderBy(x => x.Start))
                {
                    string dateKey = s.Start.ToString("dd.MM.yyyy");
                    if (dateKey != lastDate)
                    {
                        sb.AppendLine();
                        sb.AppendLine($"─── {dateKey} — {GetRussianDayOfWeek(s.Start.DayOfWeek)} ───");
                        lastDate = dateKey;
                    }

                    string focus = s.DurationSeconds >= 25 * 60 ? " ●" : "";
                    sb.AppendLine(
                        $"  {s.StartText} → {s.EndText}  |  {s.DisplayName,-30}  |  {s.DurationText}{focus}");
                }

                sb.AppendLine();
                sb.AppendLine("═══════════════════════════════════════════════════");
                sb.AppendLine("\"Ledger помнит всё. Но никому не расскажет.\"");

                System.IO.File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

                StatusText.Text = $"Экспорт TXT: {System.IO.Path.GetFileName(path)}";
                Logger.Info($"Хроника экспортирована в {path}");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка экспорта TXT", ex);
                StatusText.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sessions = GetFilteredSessions();
                if (sessions.Count == 0) return;

                var sb = new StringBuilder();
                foreach (var s in sessions.OrderBy(x => x.Start))
                {
                    sb.AppendLine($"{s.Start:dd.MM.yyyy HH:mm:ss} → {s.EndText}  |  {s.DisplayName}  |  {s.DurationText}");
                }

                Clipboard.SetText(sb.ToString());
                StatusText.Text = $"Скопировано {sessions.Count} сессий в буфер";
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка копирования в буфер", ex);
            }
        }

        private static string EscapeCsv(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            value = value.Replace("\"", "\"\"");
            return $"\"{value}\"";
        }
    }
}
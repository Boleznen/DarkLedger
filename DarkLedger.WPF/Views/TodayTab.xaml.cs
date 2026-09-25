using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace DarkLedger.WPF.Views
{
    public partial class TodayTab : UserControl
    {
        private readonly DispatcherTimer _refreshTimer;
        private StatsPeriod _currentPeriod = StatsPeriod.Today;

        public TodayTab()
        {
            InitializeComponent();

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _refreshTimer.Tick += (s, e) => RefreshData();
            _refreshTimer.Start();

            Loaded += (s, e) => RefreshData();
            Unloaded += (s, e) => _refreshTimer.Stop();
        }

        // ==================== ОБНОВЛЕНИЕ ====================

        public void RefreshData()
        {
            try
            {
                var stats = StatsAggregator.Aggregate(_currentPeriod);
                UpdateCards(stats);
                UpdateGoals();
                UpdateAppsList(stats);
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка RefreshData на TodayTab", ex);
            }
        }

        private void UpdateCards(AggregatedStats stats)
        {
            UptimeValue.Text = StatsFormatter.FormatDuration(stats.TotalUptimeSeconds);
            TrackedValue.Text = StatsFormatter.FormatDuration(stats.TotalTrackedSeconds);

            if (stats.TopAppName != null)
            {
                TopAppValue.Text = ProcessNameMapper.GetDisplayName(stats.TopAppName);

                double percent = StatsFormatter.GetPercent(
                    stats.TopAppSeconds,
                    stats.GetAppsTotalSeconds());

                TopAppSub.Text = $"{StatsFormatter.FormatDuration(stats.TopAppSeconds)}  •  {StatsFormatter.FormatPercent(percent)}";
            }
            else
            {
                TopAppValue.Text = "—";
                TopAppSub.Text = "Нет данных";
            }
        }

        private void UpdateGoals()
        {
            GoalsContainer.Children.Clear();

            var goals = GoalsEvaluator.GetAllGoals()
                .Where(g => !string.IsNullOrEmpty(g.Description) && g.Description != "Не настроен")
                .ToList();

            if (goals.Count == 0)
            {
                GoalsCard.Visibility = Visibility.Collapsed;
                return;
            }

            GoalsCard.Visibility = Visibility.Visible;

            foreach (var goal in goals)
            {
                var row = CreateGoalRow(goal);
                GoalsContainer.Children.Add(row);
            }
        }

        private Border CreateGoalRow(GoalResult goal)
        {
            var border = new Border
            {
                Background = (Brush)FindResource("BackgroundCardBrush"),
                Margin = new Thickness(0, 0, 0, 6),
                Padding = new Thickness(0, 4, 0, 4)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(6) });

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var title = new TextBlock
            {
                Text = goal.Title,
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(title, 0);

            var desc = new TextBlock
            {
                Text = goal.Description,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (goal.IsViolation)
                desc.Foreground = (Brush)FindResource("ErrorBrush");
            else if (goal.IsWarning)
                desc.Foreground = (Brush)FindResource("WarningBrush");
            else
                desc.Foreground = (Brush)FindResource("TextPrimaryBrush");

            Grid.SetColumn(desc, 1);

            headerGrid.Children.Add(title);
            headerGrid.Children.Add(desc);
            Grid.SetRow(headerGrid, 0);
            grid.Children.Add(headerGrid);

            var progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Value = Math.Min(100, goal.Percent),
                Height = 6,
                Margin = new Thickness(0, 6, 0, 0)
            };

            if (goal.IsViolation)
                progressBar.Foreground = (Brush)FindResource("ErrorBrush");
            else if (goal.IsWarning)
                progressBar.Foreground = (Brush)FindResource("WarningBrush");
            else
                progressBar.Foreground = (Brush)FindResource("SuccessBrush");

            Grid.SetRow(progressBar, 1);
            grid.Children.Add(progressBar);

            border.Child = grid;
            return border;
        }

        private void UpdateAppsList(AggregatedStats stats)
        {
            AppsContainer.Children.Clear();

            var top = stats.GetTopApps(50);
            long total = stats.GetAppsTotalSeconds();

            if (top.Count == 0)
            {
                AppsContainer.Children.Add(new TextBlock
                {
                    Text = "Пока нет данных. Поработайте за компьютером — статистика появится.",
                    Foreground = (Brush)FindResource("TextMutedBrush"),
                    FontSize = 13,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 40, 0, 0)
                });
                return;
            }

            foreach (var kv in top)
            {
                var row = CreateAppRow(kv.Key, kv.Value, total);
                AppsContainer.Children.Add(row);
            }
        }

        private Border CreateAppRow(string appName, long seconds, long totalSeconds)
        {
            var border = new Border
            {
                Background = (Brush)FindResource("BackgroundDarkBrush"),
                Padding = new Thickness(4, 8, 4, 8),
                Margin = new Thickness(0, 0, 0, 2)
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(8) });

            // Верхняя строка: имя + время
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var name = new TextBlock
            {
                Text = ProcessNameMapper.GetDisplayName(appName),
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(name, 0);

            var time = new TextBlock
            {
                Text = StatsFormatter.FormatDuration(seconds),
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(time, 1);

            headerGrid.Children.Add(name);
            headerGrid.Children.Add(time);
            Grid.SetRow(headerGrid, 0);
            grid.Children.Add(headerGrid);

            // Цветная полоска прогресса
            double percent = StatsFormatter.GetPercent(seconds, totalSeconds);
            var barGrid = new Grid
            {
                Height = 8,
                Margin = new Thickness(0, 6, 0, 0)
            };

            var bgBar = new Border
            {
                Background = (Brush)FindResource("BackgroundPanelBrush"),
                CornerRadius = new CornerRadius(4)
            };
            barGrid.Children.Add(bgBar);

            var fillBar = new Border
            {
                Background = GetAppBrush(appName),
                HorizontalAlignment = HorizontalAlignment.Left,
                CornerRadius = new CornerRadius(4),
                Width = 0
            };
            barGrid.Children.Add(fillBar);

            barGrid.SizeChanged += (s, e) =>
            {
                fillBar.Width = barGrid.ActualWidth * percent / 100.0;
            };

            Grid.SetRow(barGrid, 1);
            grid.Children.Add(barGrid);

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
                "AllTime" => StatsPeriod.AllTime,
                _ => StatsPeriod.Today
            };

            RefreshData();
        }
    }
}
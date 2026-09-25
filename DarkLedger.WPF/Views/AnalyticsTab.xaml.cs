using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DarkLedger.WPF.Views
{
    public partial class AnalyticsTab : UserControl
    {
        private readonly DispatcherTimer _refreshTimer;

        private const int CellSize = 24;
        private const int CellGap = 2;
        private const int LeftPadding = 32;
        private const int TopPadding = 20;

        public AnalyticsTab()
        {
            InitializeComponent();

            _refreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _refreshTimer.Tick += (s, e) => RefreshData();
            _refreshTimer.Start();

            Loaded += (s, e) => RefreshData();
            Unloaded += (s, e) => _refreshTimer.Stop();
        }

        public void RefreshData()
        {
            try
            {
                var heatmap = AnalyticsBuilder.BuildHeatmap(30);
                DrawHeatmap(heatmap);

                var focus = AnalyticsBuilder.BuildFocusIndex(StatsPeriod.Last30Days);
                UpdateFocusCard(focus);

                var compare = AnalyticsBuilder.BuildWeekComparison();
                UpdateCompareCard(compare);

                var top = AnalyticsBuilder.GetTopApps(StatsPeriod.Last30Days, 5);
                UpdateTopApps(top);
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка RefreshData на AnalyticsTab", ex);
            }
        }

        // ==================== HEATMAP ====================

        private void DrawHeatmap(HeatmapData data)
        {
            HeatmapHost.Children.Clear();

            var canvas = new Canvas
            {
                Width = LeftPadding + 24 * (CellSize + CellGap),
                Height = TopPadding + 7 * (CellSize + CellGap)
            };

            for (int h = 0; h < 24; h++)
            {
                if (h % 3 != 0) continue;

                var lbl = new TextBlock
                {
                    Text = h.ToString("D2"),
                    Foreground = (Brush)FindResource("TextMutedBrush"),
                    FontSize = 10
                };
                Canvas.SetLeft(lbl, LeftPadding + h * (CellSize + CellGap));
                Canvas.SetTop(lbl, 2);
                canvas.Children.Add(lbl);
            }

            for (int d = 0; d < 7; d++)
            {
                var lbl = new TextBlock
                {
                    Text = AnalyticsBuilder.DayNames[d],
                    Foreground = (Brush)FindResource("TextSecondaryBrush"),
                    FontSize = 11,
                    FontWeight = FontWeights.SemiBold
                };
                Canvas.SetLeft(lbl, 4);
                Canvas.SetTop(lbl, TopPadding + d * (CellSize + CellGap) + 5);
                canvas.Children.Add(lbl);
            }

            for (int d = 0; d < 7; d++)
            {
                for (int h = 0; h < 24; h++)
                {
                    long value = data.Cells[d, h];
                    var color = GetCellColor(value, data.MaxValue);

                    var rect = new Rectangle
                    {
                        Width = CellSize,
                        Height = CellSize,
                        Fill = new SolidColorBrush(color),
                        Stroke = (Brush)FindResource("BackgroundDarkBrush"),
                        StrokeThickness = 1
                    };

                    string tipText = $"{AnalyticsBuilder.DayNames[d]}, {h:D2}:00\n{StatsFormatter.FormatDuration(value)}";
                    rect.ToolTip = tipText;

                    Canvas.SetLeft(rect, LeftPadding + h * (CellSize + CellGap));
                    Canvas.SetTop(rect, TopPadding + d * (CellSize + CellGap));
                    canvas.Children.Add(rect);
                }
            }

            HeatmapHost.Children.Add(canvas);
        }

        private static Color GetCellColor(long value, long max)
        {
            var bg = Color.FromRgb(0x23, 0x20, 0x2D);

            if (value <= 0 || max <= 0) return bg;

            double t = (double)value / max;
            if (t > 1) t = 1;

            var low = Color.FromRgb(0x5A, 0x0F, 0x19);
            var high = Color.FromRgb(0x8C, 0x14, 0x1E);

            int r = (int)(low.R + (high.R - low.R) * t);
            int g = (int)(low.G + (high.G - low.G) * t);
            int b = (int)(low.B + (high.B - low.B) * t);

            if (t < 0.15)
            {
                r = (int)(r * 0.5);
                g = (int)(g * 0.5);
                b = (int)(b * 0.5);
            }

            return Color.FromRgb((byte)r, (byte)g, (byte)b);
        }

        // ==================== КАРТОЧКИ ====================

        private void UpdateFocusCard(FocusIndex focus)
        {
            FocusValue.Text = $"{focus.Percent:F1}%";

            if (focus.TotalSessions == 0)
            {
                FocusSub.Text = "Нет данных";
            }
            else
            {
                long avgMin = (long)(focus.AverageSessionSeconds / 60);
                FocusSub.Text = $"{focus.FocusSessions} из {focus.TotalSessions} сессий >25м  •  ср. {avgMin}м";
            }
        }

        private void UpdateCompareCard(PeriodComparison compare)
        {
            CompareValue.Text = compare.DeltaText;

            if (compare.DeltaPercent > 0)
                CompareValue.Foreground = new SolidColorBrush(Color.FromRgb(0x22, 0xC5, 0x5E));
            else if (compare.DeltaPercent < 0)
                CompareValue.Foreground = (Brush)FindResource("AccentRedBrush");
            else
                CompareValue.Foreground = (Brush)FindResource("TextPrimaryBrush");

            CompareSub.Text =
                $"Сейчас: {StatsFormatter.FormatDuration(compare.CurrentSeconds)}  •  " +
                $"Было: {StatsFormatter.FormatDuration(compare.PreviousSeconds)}";
        }

        // ==================== ТОП-5 ====================

        private void UpdateTopApps(List<KeyValuePair<string, long>> top)
        {
            TopAppsContainer.Children.Clear();

            if (top.Count == 0)
            {
                TopAppsContainer.Children.Add(new TextBlock
                {
                    Text = "Нет данных",
                    Foreground = (Brush)FindResource("TextMutedBrush"),
                    FontSize = 12,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 20, 0, 20)
                });
                return;
            }

            long maxValue = top[0].Value;

            foreach (var kv in top)
            {
                var row = CreateTopAppRow(kv.Key, kv.Value, maxValue);
                TopAppsContainer.Children.Add(row);
            }
        }

        private Border CreateTopAppRow(string appName, long seconds, long maxSeconds)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 4) };
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(4) });

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var name = new TextBlock
            {
                Text = ProcessNameMapper.GetDisplayName(appName),
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            Grid.SetColumn(name, 0);

            var time = new TextBlock
            {
                Text = StatsFormatter.FormatDuration(seconds),
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                FontSize = 12,
                Margin = new Thickness(8, 0, 0, 0)
            };
            Grid.SetColumn(time, 1);

            headerGrid.Children.Add(name);
            headerGrid.Children.Add(time);
            Grid.SetRow(headerGrid, 0);
            grid.Children.Add(headerGrid);

            var barGrid = new Grid { Height = 4, Margin = new Thickness(0, 4, 0, 0) };

            var bgBar = new Border
            {
                Background = (Brush)FindResource("BackgroundPanelBrush")
            };
            barGrid.Children.Add(bgBar);

            double percent = maxSeconds > 0 ? (double)seconds / maxSeconds : 0;
            var fillBar = new Border
            {
                Background = (Brush)FindResource("AccentRedBrush"),
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 0
            };
            barGrid.Children.Add(fillBar);

            barGrid.SizeChanged += (s, e) =>
            {
                fillBar.Width = barGrid.ActualWidth * percent;
            };

            Grid.SetRow(barGrid, 1);
            grid.Children.Add(barGrid);

            return new Border
            {
                Child = grid,
                Padding = new Thickness(0, 2, 0, 2)
            };
        }
    }
}
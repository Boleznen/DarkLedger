using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace DarkLedger.WPF.Views
{
    public partial class DiaryTab : UserControl
    {
        // ==================== СОСТОЯНИЕ ====================
        private string _currentDate = "";
        private bool _isLoading;
        private bool _hideEmptyDays;
        private DateTime _viewMonth = DateTime.Now;

        private readonly DispatcherTimer _saveTimer;
        private readonly DispatcherTimer _searchTimer;

        private static readonly string NotesFolder;

        static DiaryTab()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            NotesFolder = Path.Combine(appData, "DarkLedger", "Notes");
            Directory.CreateDirectory(NotesFolder);
        }

        public DiaryTab()
        {
            InitializeComponent();

            _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _saveTimer.Tick += (s, e) =>
            {
                _saveTimer.Stop();
                SaveCurrentNote();
            };

            _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
            _searchTimer.Tick += (s, e) =>
            {
                _searchTimer.Stop();
                ApplySearchFilter();
            };

            Loaded += (s, e) =>
            {
                DrawCalendar();
                LoadDaysList();
                SelectToday();
            };
        }

        // ==================== КАЛЕНДАРЬ ====================

        private void DrawCalendar()
        {
            CalendarHost.Children.Clear();

            var grid = new Grid();
            // 7 колонок (дни) + 1 строка заголовка
            for (int i = 0; i < 7; i++)
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Заголовок: месяц + стрелки
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(28) });
            // Дни недели
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(22) });
            // 6 строк дней
            for (int i = 0; i < 6; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            // ---- Заголовок (на все 7 колонок) ----
            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(28) });

            var prevBtn = new Button
            {
                Content = "◀",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                Cursor = Cursors.Hand,
                FontSize = 12
            };
            prevBtn.Click += (s, e) =>
            {
                _viewMonth = _viewMonth.AddMonths(-1);
                DrawCalendar();
            };
            Grid.SetColumn(prevBtn, 0);
            headerGrid.Children.Add(prevBtn);

            var monthLabel = new TextBlock
            {
                Text = _viewMonth.ToString("MMMM yyyy", new CultureInfo("ru-RU")),
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("TextPrimaryBrush"),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(monthLabel, 1);
            headerGrid.Children.Add(monthLabel);

            var nextBtn = new Button
            {
                Content = "▶",
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = (Brush)FindResource("TextSecondaryBrush"),
                Cursor = Cursors.Hand,
                FontSize = 12
            };
            nextBtn.Click += (s, e) =>
            {
                _viewMonth = _viewMonth.AddMonths(1);
                DrawCalendar();
            };
            Grid.SetColumn(nextBtn, 2);
            headerGrid.Children.Add(nextBtn);

            Grid.SetRow(headerGrid, 0);
            Grid.SetColumnSpan(headerGrid, 7);
            grid.Children.Add(headerGrid);

            // ---- Дни недели ----
            var dayNames = new[] { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
            for (int i = 0; i < 7; i++)
            {
                var lbl = new TextBlock
                {
                    Text = dayNames[i],
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)FindResource("TextMutedBrush"),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetRow(lbl, 1);
                Grid.SetColumn(lbl, i);
                grid.Children.Add(lbl);
            }

            // ---- Дни ----
            var today = DateTime.Now.Date;
            var first = new DateTime(_viewMonth.Year, _viewMonth.Month, 1);
            int startOffset = ((int)first.DayOfWeek + 6) % 7; // Пн = 0
            var current = first.AddDays(-startOffset);

            var selectedDate = string.IsNullOrEmpty(_currentDate)
                ? today
                : DateTime.ParseExact(_currentDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 7; col++)
                {
                    var date = current;
                    bool isCurrentMonth = date.Month == _viewMonth.Month;
                    bool isToday = date == today;
                    bool isSelected = date == selectedDate;
                    bool hasNote = File.Exists(GetNotePath(date));

                    var cell = CreateDayCell(date, isCurrentMonth, isToday, isSelected, hasNote);
                    Grid.SetRow(cell, row + 2);
                    Grid.SetColumn(cell, col);
                    grid.Children.Add(cell);

                    current = current.AddDays(1);
                }
            }

            CalendarHost.Children.Add(grid);
        }

        private Border CreateDayCell(DateTime date, bool isCurrentMonth, bool isToday, bool isSelected, bool hasNote)
        {
            Color bg;
            if (isSelected) bg = Color.FromRgb(0x8C, 0x14, 0x1E);
            else if (isToday) bg = Color.FromRgb(0x5A, 0x0F, 0x19);
            else if (!isCurrentMonth) bg = Color.FromRgb(0x14, 0x14, 0x1C);
            else bg = Color.FromRgb(0x23, 0x20, 0x2D);

            var border = new Border
            {
                Background = new SolidColorBrush(bg),
                Margin = new Thickness(1),
                CornerRadius = new CornerRadius(3),
                Cursor = Cursors.Hand
            };

            var grid = new Grid();

            if (hasNote)
            {
                var dot = new System.Windows.Shapes.Ellipse
                {
                    Width = 4,
                    Height = 4,
                    Fill = isSelected
                        ? (Brush)FindResource("TextPrimaryBrush")
                        : (Brush)FindResource("AccentRedBrush"),
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 3, 3, 0)
                };
                grid.Children.Add(dot);
            }

            Color textColor;
            if (isSelected) textColor = Color.FromRgb(0xE6, 0xE6, 0xF0);
            else if (!isCurrentMonth) textColor = Color.FromRgb(0x64, 0x64, 0x78);
            else textColor = Color.FromRgb(0xE6, 0xE6, 0xF0);

            var text = new TextBlock
            {
                Text = date.Day.ToString(),
                FontSize = 11,
                Foreground = new SolidColorBrush(textColor),
                FontWeight = isToday ? FontWeights.Bold : FontWeights.Normal,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            grid.Children.Add(text);

            border.Child = grid;

            border.MouseLeftButtonDown += (s, e) =>
            {
                if (_currentDate == date.ToString("yyyy-MM-dd")) return;
                SaveCurrentNote();
                LoadNote(date);
                DrawCalendar();
            };

            return border;
        }

        // ==================== СПИСОК ДНЕЙ ====================

        private void LoadDaysList()
        {
            DaysContainer.Children.Clear();

            var today = DateTime.Now.Date;

            for (int i = 0; i < 30; i++)
            {
                var date = today.AddDays(-i);

                if (_hideEmptyDays && !File.Exists(GetNotePath(date))) continue;

                var row = CreateDayRow(date);
                DaysContainer.Children.Add(row);
            }
        }

        private Border CreateDayRow(DateTime date)
        {
            string dateKey = date.ToString("yyyy-MM-dd");
            bool isSelected = dateKey == _currentDate;
            bool hasNote = File.Exists(GetNotePath(date));

            Color bg;
            if (isSelected) bg = Color.FromRgb(0x5A, 0x0F, 0x19);
            else bg = Color.FromRgb(0x23, 0x20, 0x2D);

            var border = new Border
            {
                Background = new SolidColorBrush(bg),
                Padding = new Thickness(8, 6, 8, 6),
                Margin = new Thickness(0, 0, 0, 2),
                Cursor = Cursors.Hand,
                CornerRadius = new CornerRadius(3)
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = date.ToString("dd.MM.yyyy"),
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("TextPrimaryBrush")
            });
            stack.Children.Add(new TextBlock
            {
                Text = GetRussianDayOfWeek(date.DayOfWeek),
                FontSize = 10,
                Foreground = (Brush)FindResource("TextSecondaryBrush")
            });
            Grid.SetColumn(stack, 0);
            grid.Children.Add(stack);

            if (hasNote)
            {
                var dot = new System.Windows.Shapes.Ellipse
                {
                    Width = 8,
                    Height = 8,
                    Fill = (Brush)FindResource("AccentRedBrush"),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(dot, 1);
                grid.Children.Add(dot);
            }

            border.Child = grid;

            border.MouseLeftButtonDown += (s, e) =>
            {
                if (_currentDate == dateKey) return;
                SaveCurrentNote();
                LoadNote(date);
                DrawCalendar();
                LoadDaysList();
            };

            return border;
        }

        // ==================== ЗАГРУЗКА / СОХРАНЕНИЕ ====================

        private void LoadNote(DateTime date)
        {
            _isLoading = true;
            try
            {
                _currentDate = date.ToString("yyyy-MM-dd");
                DateHeader.Text = date.ToString("dd MMMM yyyy, dddd", new CultureInfo("ru-RU"));

                string path = GetNotePath(date);
                if (File.Exists(path))
                {
                    NotesBox.Text = File.ReadAllText(path);
                    StatusText.Text = $"Загружено: {Path.GetFileName(path)}";
                }
                else
                {
                    NotesBox.Text = "";
                    StatusText.Text = "Новая заметка";
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Ошибка загрузки заметки за {date:yyyy-MM-dd}", ex);
                NotesBox.Text = "";
                StatusText.Text = "Ошибка загрузки";
            }
            finally
            {
                _isLoading = false;
            }
        }

        private void SaveCurrentNote()
        {
            if (string.IsNullOrEmpty(_currentDate)) return;

            try
            {
                string path = Path.Combine(NotesFolder, $"{_currentDate}.txt");
                string content = NotesBox.Text ?? "";

                if (string.IsNullOrWhiteSpace(content))
                {
                    if (File.Exists(path)) File.Delete(path);
                    StatusText.Text = "Пусто";
                }
                else
                {
                    File.WriteAllText(path, content);
                    StatusText.Text = $"Сохранено в {DateTime.Now:HH:mm:ss}";
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка сохранения заметки", ex);
                StatusText.Text = "Ошибка сохранения";
            }
        }

        private static string GetNotePath(DateTime date)
            => Path.Combine(NotesFolder, $"{date:yyyy-MM-dd}.txt");

        // ==================== ПОИСК ====================

        private void ApplySearchFilter()
        {
            string query = SearchBox.Text.Trim();

            DaysContainer.Children.Clear();

            if (string.IsNullOrEmpty(query))
            {
                LoadDaysList();
                return;
            }

            try
            {
                var files = Directory.GetFiles(NotesFolder, "*.txt")
                    .OrderByDescending(f => f)
                    .ToList();

                foreach (var file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    if (!DateTime.TryParseExact(fileName, "yyyy-MM-dd",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
                        continue;

                    try
                    {
                        string content = File.ReadAllText(file);
                        if (content.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            DaysContainer.Children.Add(CreateDayRow(date));
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка поиска по заметкам", ex);
            }
        }

        // ==================== КНОПКИ ====================

        private void SelectToday()
        {
            var today = DateTime.Now.Date;
            _viewMonth = new DateTime(today.Year, today.Month, 1);

            if (_currentDate != today.ToString("yyyy-MM-dd"))
            {
                SaveCurrentNote();
                LoadNote(today);
            }

            DrawCalendar();
            LoadDaysList();
        }

        private void TodayButton_Click(object sender, RoutedEventArgs e)
        {
            SelectToday();
        }

        private void HideEmptyButton_Click(object sender, RoutedEventArgs e)
        {
            _hideEmptyDays = !_hideEmptyDays;
            HideEmptyButton.Content = _hideEmptyDays ? "👁 Показать все" : "👁 Скрыть пустые";
            LoadDaysList();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private void NotesBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isLoading) return;

            _saveTimer.Stop();
            _saveTimer.Start();
            StatusText.Text = "Изменено... (сохранение через 1 сек)";
        }

        // ==================== УТИЛИТЫ ====================

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
    }
}
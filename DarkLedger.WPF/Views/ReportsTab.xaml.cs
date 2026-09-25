using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace DarkLedger.WPF.Views
{
    public partial class ReportsTab : UserControl
    {
        private StatsPeriod _currentPeriod = StatsPeriod.Today;
        private readonly DispatcherTimer _refreshTimer;

        public ReportsTab()
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
                string report = ReportBuilder.BuildTextReport(_currentPeriod);
                ReportText.Text = report;
                StatusText.Text = $"Обновлено: {DateTime.Now:HH:mm:ss}";
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка RefreshData на ReportsTab", ex);
                ReportText.Text = "Ошибка формирования отчёта:\r\n" + ex.Message;
            }
        }

        private void Period_Click(object sender, RoutedEventArgs e)
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

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            if (ReportExporter.CopyToClipboard(_currentPeriod))
                StatusText.Text = $"Отчёт скопирован в буфер — {DateTime.Now:HH:mm:ss}";
            else
                StatusText.Text = "Не удалось скопировать в буфер";
        }

        private void SaveTxt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = ReportExporter.ExportToTxt(_currentPeriod);
                StatusText.Text = $"Сохранено: {System.IO.Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка сохранения TXT", ex);
                StatusText.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void SaveCsv_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = ReportExporter.ExportToCsv(_currentPeriod);
                StatusText.Text = $"Сохранено: {System.IO.Path.GetFileName(path)}";
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка сохранения CSV", ex);
                StatusText.Text = $"Ошибка: {ex.Message}";
            }
        }

        private void OpenFolder_Click(object sender, RoutedEventArgs e)
        {
            ReportExporter.OpenReportsFolder();
            StatusText.Text = "Открыта папка отчётов";
        }

        private void DeleteAll_Click(object sender, RoutedEventArgs e)
        {
            var answer = MessageBox.Show(
                "Удалить все сохранённые отчёты?\n\nЭто действие необратимо.",
                "Удалить отчёты?", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes) return;

            var (deleted, freed) = ReportExporter.DeleteAllReports();
            StatusText.Text = $"Удалено файлов: {deleted}. Освобождено: {FormatSize(freed)}";
        }

        private void ExportAllData_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "ZIP-архив (*.zip)|*.zip",
                FileName = DataExporter.GetDefaultFileName(),
                Title = "Экспорт всей статистики Dark Ledger"
            };

            if (dlg.ShowDialog() != true) return;

            StatusText.Text = "Собираю архив...";

            var result = DataExporter.ExportAll(dlg.FileName);

            if (result.Success)
            {
                StatusText.Text =
                    $"Экспортировано {result.FileCount} файлов — {FormatSize(result.TotalBytes)}";

                var openAnswer = MessageBox.Show(
                    $"Экспорт завершён успешно!\n\n" +
                    $"Файлов: {result.FileCount}\n" +
                    $"Размер: {FormatSize(result.TotalBytes)}\n" +
                    $"Путь: {result.Path}\n\n" +
                    "Открыть папку с архивом?",
                    "Готово", MessageBoxButton.YesNo, MessageBoxImage.Information);

                if (openAnswer == MessageBoxResult.Yes)
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = "explorer.exe",
                            Arguments = $"/select,\"{result.Path}\"",
                            UseShellExecute = true
                        });
                    }
                    catch { }
                }
            }
            else
            {
                StatusText.Text = $"Ошибка экспорта: {result.ErrorMessage}";
                MessageBox.Show(
                    $"Не удалось экспортировать данные.\n\n{result.ErrorMessage}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Форматирование размера (было в CollectionSummary в WinForms)
        private static string FormatSize(long bytes)
        {
            if (bytes < 1024) return $"{bytes} байт";
            if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F2} КБ";
            if (bytes < 1024L * 1024 * 1024) return $"{bytes / 1024.0 / 1024.0:F2} МБ";
            return $"{bytes / 1024.0 / 1024.0 / 1024.0:F2} ГБ";
        }
    }
}
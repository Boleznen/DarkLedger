using System;
using System.IO;
using System.Text;

namespace DarkLedger.WPF
{
    internal static class ReportExporter
    {
        private static readonly string ReportsFolder;

        static ReportExporter()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            ReportsFolder = Path.Combine(appData, "DarkLedger", "Reports");
            Directory.CreateDirectory(ReportsFolder);
        }

        public static string GetReportsFolderPath() => ReportsFolder;

        public static string ExportToTxt(StatsPeriod period)
        {
            string fileName = $"report_{period}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.txt";
            string path = Path.Combine(ReportsFolder, fileName);
            string content = ReportBuilder.BuildTextReport(period);

            File.WriteAllText(path, content, Encoding.UTF8);
            return path;
        }

        public static string ExportToCsv(StatsPeriod period)
        {
            string fileName = $"report_{period}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.csv";
            string path = Path.Combine(ReportsFolder, fileName);
            string content = ReportBuilder.BuildCsvReport(period);

            File.WriteAllText(path, content, Encoding.UTF8);
            return path;
        }

        public static void OpenReportsFolder()
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{ReportsFolder}\"",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Не удалось открыть папку отчётов", ex);
            }
        }

        public static (int deleted, long freedBytes) DeleteAllReports()
        {
            int count = 0;
            long bytes = 0;

            try
            {
                foreach (var file in Directory.GetFiles(ReportsFolder))
                {
                    try
                    {
                        var fi = new FileInfo(file);
                        bytes += fi.Length;
                        fi.Delete();
                        count++;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка очистки отчётов", ex);
            }

            return (count, bytes);
        }

        /// <summary>
        /// Копирует отчёт в буфер обмена (WPF).
        /// </summary>
        public static bool CopyToClipboard(StatsPeriod period)
        {
            try
            {
                string content = ReportBuilder.BuildTextReport(period);
                System.Windows.Clipboard.SetText(content);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Не удалось скопировать отчёт в буфер", ex);
                return false;
            }
        }
    }
}
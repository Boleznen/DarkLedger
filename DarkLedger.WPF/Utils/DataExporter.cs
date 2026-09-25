using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace DarkLedger.WPF
{
    /// <summary>
    /// Экспорт ВСЕХ данных Dark Ledger в один ZIP-архив.
    /// Внутри: Stats/ + Notes/ + Reports/ + Logs/ + settings.json + README.txt
    /// </summary>
    internal static class DataExporter
    {
        public sealed class ExportResult
        {
            public bool Success { get; set; }
            public string Path { get; set; } = "";
            public long TotalBytes { get; set; }
            public int FileCount { get; set; }
            public string ErrorMessage { get; set; } = "";
        }

        public static ExportResult ExportAll(string zipPath)
        {
            var result = new ExportResult();

            try
            {
                if (File.Exists(zipPath))
                    File.Delete(zipPath);

                int fileCount = 0;

                using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
                {
                    string appData = SettingsManager.GetDataFolderPath();

                    fileCount += AddFolder(zip, Path.Combine(appData, "Stats"), "Stats");
                    fileCount += AddFolder(zip, Path.Combine(appData, "Notes"), "Notes");
                    fileCount += AddFolder(zip, Path.Combine(appData, "Reports"), "Reports");
                    fileCount += AddFolder(zip, Path.Combine(appData, "Logs"), "Logs");

                    string settingsFile = SettingsManager.GetSettingsFilePath();
                    if (File.Exists(settingsFile))
                    {
                        zip.CreateEntryFromFile(settingsFile, "settings.json");
                        fileCount++;
                    }

                    var readmeEntry = zip.CreateEntry("README.txt");
                    using (var stream = readmeEntry.Open())
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        writer.WriteLine("═══════════════════════════════════════════════════");
                        writer.WriteLine("           DARK LEDGER — ЭКСПОРТ ДАННЫХ");
                        writer.WriteLine("═══════════════════════════════════════════════════");
                        writer.WriteLine();
                        writer.WriteLine($"Дата экспорта: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                        writer.WriteLine($"Версия: 2.0.0");
                        writer.WriteLine($"Машина: {Environment.MachineName}");
                        writer.WriteLine($"Пользователь: {Environment.UserName}");
                        writer.WriteLine($"ОС: {Environment.OSVersion}");
                        writer.WriteLine($".NET: {Environment.Version}");
                        writer.WriteLine();
                        writer.WriteLine("─── СОДЕРЖИМОЕ ────────────────────────────────────");
                        writer.WriteLine("  Stats/         — JSON со статистикой по дням");
                        writer.WriteLine("  Notes/         — заметки Дневника (yyyy-MM-dd.txt)");
                        writer.WriteLine("  Reports/       — сохранённые отчёты (CSV / TXT)");
                        writer.WriteLine("  Logs/          — логи сессий");
                        writer.WriteLine("  settings.json  — настройки приложения");
                        writer.WriteLine();
                        writer.WriteLine("═══════════════════════════════════════════════════");
                        writer.WriteLine("\"Ledger помнит всё. Но никому не расскажет.\"");
                        writer.WriteLine("Dark Ledger — @Boleznen");
                        writer.WriteLine("═══════════════════════════════════════════════════");
                    }
                    fileCount++;
                }

                var fi = new FileInfo(zipPath);
                result.Success = true;
                result.Path = zipPath;
                result.TotalBytes = fi.Length;
                result.FileCount = fileCount;

                Logger.Info($"Экспорт данных: {zipPath} ({fileCount} файлов, {fi.Length} байт)");
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка экспорта данных в ZIP", ex);
                result.Success = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        private static int AddFolder(ZipArchive zip, string sourceFolder, string entryPrefix)
        {
            if (!Directory.Exists(sourceFolder)) return 0;

            int count = 0;

            try
            {
                foreach (var file in Directory.GetFiles(sourceFolder, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        string relative = Path.GetRelativePath(sourceFolder, file)
                            .Replace('\\', '/');

                        string entryName = $"{entryPrefix}/{relative}";
                        zip.CreateEntryFromFile(file, entryName);
                        count++;
                    }
                    catch (Exception ex)
                    {
                        Logger.Error($"Не удалось добавить файл в архив: {file}", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error($"Не удалось прочитать папку {sourceFolder}", ex);
            }

            return count;
        }

        public static string GetDefaultFileName()
        {
            return $"DarkLedger_Export_{Environment.MachineName}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.zip";
        }
    }
}
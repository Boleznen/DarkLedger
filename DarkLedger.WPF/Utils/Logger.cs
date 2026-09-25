using System;
using System.IO;
using System.Linq;
using System.Text;

namespace DarkLedger.WPF
{
    /// <summary>
    /// Логи по сессиям. Файлы: %APPDATA%\DarkLedger\Logs\session_yyyy-MM-dd_HH-mm-ss.log
    /// </summary>
    internal static class Logger
    {
        private static readonly object LockObj = new object();
        private static readonly string LogsDir;
        private static readonly string SessionFileName;

        static Logger()
        {
            try
            {
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                LogsDir = Path.Combine(appData, "DarkLedger", "Logs");
                Directory.CreateDirectory(LogsDir);

                SessionFileName = $"session_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
                CleanOldLogs();
            }
            catch
            {
                LogsDir = Path.GetTempPath();
                SessionFileName = "darkledger_fallback.log";
            }
        }

        public static string GetLogsFolderPath() => LogsDir;
        public static string GetCurrentLogFileName() => SessionFileName;

        private static string GetLogFilePath() => Path.Combine(LogsDir, SessionFileName);

        public static void Info(string message) => Write("INFO", message, null);
        public static void Warn(string message) => Write("WARN", message, null);
        public static void Error(string message) => Write("ERROR", message, null);
        public static void Error(string message, Exception ex) => Write("ERROR", message, ex);

        private static void Write(string level, string message, Exception? ex)
        {
            lock (LockObj)
            {
                try
                {
                    var sb = new StringBuilder();
                    sb.Append($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}");
                    if (ex != null)
                    {
                        sb.AppendLine();
                        sb.Append($"  → {ex.GetType().Name}: {ex.Message}");
                        if (!string.IsNullOrEmpty(ex.StackTrace))
                        {
                            sb.AppendLine();
                            sb.Append($"  → {ex.StackTrace}");
                        }
                    }
                    sb.AppendLine();

                    File.AppendAllText(GetLogFilePath(), sb.ToString(), Encoding.UTF8);
                }
                catch { }
            }
        }

        private static void CleanOldLogs()
        {
            try
            {
                var files = Directory.GetFiles(LogsDir, "session_*.log")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .ToList();

                int maxFiles = 30;
                for (int i = maxFiles; i < files.Count; i++)
                {
                    try { files[i].Delete(); } catch { }
                }
            }
            catch { }
        }

        public static string GetCurrentLogContents()
        {
            try
            {
                string path = GetLogFilePath();
                return File.Exists(path)
                    ? File.ReadAllText(path, Encoding.UTF8)
                    : "Записей в журнале текущей сессии пока нет.";
            }
            catch (Exception ex)
            {
                return $"Ошибка чтения журнала: {ex.Message}";
            }
        }
    }
}
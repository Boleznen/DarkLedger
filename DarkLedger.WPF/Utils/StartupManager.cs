using System;
using Microsoft.Win32;

namespace DarkLedger.WPF
{
    /// <summary>
    /// Управление автозагрузкой Windows через реестр.
    /// HKCU\Software\Microsoft\Windows\CurrentVersion\Run
    /// </summary>
    internal static class StartupManager
    {
        private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "DarkLedger";

        /// <summary>
        /// Проверяет, добавлено ли приложение в автозагрузку.
        /// </summary>
        public static bool IsEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: false);
                if (key == null) return false;

                var value = key.GetValue(AppName)?.ToString();
                return !string.IsNullOrEmpty(value);
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка проверки автозагрузки", ex);
                return false;
            }
        }

        /// <summary>
        /// Включает / выключает автозагрузку.
        /// </summary>
        public static bool SetEnabled(bool enabled)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
                if (key == null)
                {
                    Logger.Error("Не удалось открыть ключ Run для записи");
                    return false;
                }

                if (enabled)
                {
                    string exePath = Environment.ProcessPath ?? "";
                    if (string.IsNullOrEmpty(exePath))
                    {
                        Logger.Error("Не удалось получить путь к .exe");
                        return false;
                    }

                    key.SetValue(AppName, $"\"{exePath}\"");
                    Logger.Info($"Автозагрузка включена: {exePath}");
                }
                else
                {
                    key.DeleteValue(AppName, throwOnMissingValue: false);
                    Logger.Info("Автозагрузка выключена");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка изменения автозагрузки", ex);
                return false;
            }
        }
    }
}
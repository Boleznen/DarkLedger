using System;
using System.Diagnostics;

namespace DarkLedger.WPF
{
    /// <summary>
    /// Определяет, какое приложение сейчас в фокусе.
    /// </summary>
    internal static class WindowTracker
    {
        /// <summary>
        /// Имя процесса активного окна (например, "chrome", "Code", "Telegram").
        /// Возвращает null, если окно не удалось определить.
        /// </summary>
        public static string? GetActiveProcessName()
        {
            try
            {
                IntPtr hwnd = NativeMethods.GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return null;

                NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid == 0) return null;

                using var process = Process.GetProcessById((int)pid);
                return process.ProcessName;
            }
            catch
            {
                // Процесс мог умереть между GetForegroundWindow и GetProcessById
                return null;
            }
        }

        /// <summary>
        /// Полный путь к .exe активного процесса.
        /// На системных процессах может кинуть — возвращает null.
        /// </summary>
        public static string? GetActiveProcessPath()
        {
            try
            {
                IntPtr hwnd = NativeMethods.GetForegroundWindow();
                if (hwnd == IntPtr.Zero) return null;

                NativeMethods.GetWindowThreadProcessId(hwnd, out uint pid);
                if (pid == 0) return null;

                using var process = Process.GetProcessById((int)pid);
                return process.MainModule?.FileName;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Проверка: не спит ли пользователь (нет ввода дольше порога).
        /// </summary>
        /// <param name="thresholdSeconds">Порог в секундах</param>
        public static bool IsUserIdle(int thresholdSeconds)
        {
            try
            {
                uint idleMs = NativeMethods.GetIdleMilliseconds();
                return idleMs >= (uint)(thresholdSeconds * 1000);
            }
            catch
            {
                return false;
            }
        }
    }
}
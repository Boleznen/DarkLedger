using System;
using System.Runtime.InteropServices;

namespace DarkLedger.WPF
{
    internal static class NativeMethods
    {
        // ==================== АКТИВНОЕ ОКНО ====================

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        // ==================== ПРОСТОЙ (IDLE) ====================

        [StructLayout(LayoutKind.Sequential)]
        internal struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        internal static uint GetIdleMilliseconds()
        {
            var info = new LASTINPUTINFO();
            info.cbSize = (uint)Marshal.SizeOf(info);
            if (!GetLastInputInfo(ref info)) return 0;

            return (uint)Environment.TickCount - info.dwTime;
        }

        // ==================== UPTIME ====================

        internal static long GetSystemUptimeSeconds()
        {
            return Environment.TickCount64 / 1000;
        }
    }
}
using System;
using System.Collections.Generic;

namespace DarkLedger.WPF
{
    /// <summary>
    /// Преобразует имя процесса (devenv, chrome, Code) в человеческое название.
    /// </summary>
    internal static class ProcessNameMapper
    {
        private static readonly Dictionary<string, string> Map =
            new(StringComparer.OrdinalIgnoreCase)
        {
            // ==================== РАЗРАБОТКА ====================
            { "devenv", "Visual Studio" },
            { "Code", "Visual Studio Code" },
            { "rider64", "Rider" },
            { "pycharm64", "PyCharm" },
            { "webstorm64", "WebStorm" },
            { "idea64", "IntelliJ IDEA" },
            { "clion64", "CLion" },
            { "datagrip64", "DataGrip" },
            { "sublime_text", "Sublime Text" },
            { "notepad++", "Notepad++" },
            { "notepad", "Блокнот" },
            { "WindowsTerminal", "Windows Terminal" },
            { "powershell", "PowerShell" },
            { "pwsh", "PowerShell 7" },
            { "cmd", "Командная строка" },
            { "git-bash", "Git Bash" },
            { "wsl", "WSL" },
            { "docker", "Docker Desktop" },
            { "postman", "Postman" },
            { "insomnia", "Insomnia" },
            { "ssms", "SQL Server Management Studio" },
            { "pgadmin4", "pgAdmin" },
            { "dbeaver", "DBeaver" },
            { "Fiddler", "Fiddler" },
            { "dnSpy", "dnSpy" },
            { "ilspy", "ILSpy" },

            // ==================== БРАУЗЕРЫ ====================
            { "chrome", "Google Chrome" },
            { "msedge", "Microsoft Edge" },
            { "firefox", "Mozilla Firefox" },
            { "opera", "Opera" },
            { "opera_gx", "Opera GX" },
            { "brave", "Brave" },
            { "vivaldi", "Vivaldi" },
            { "Yandex", "Яндекс.Браузер" },
            { "browser", "Яндекс.Браузер" },
            { "tor", "Tor Browser" },
            { "zen", "Zen Browser" },

            // ==================== МЕССЕНДЖЕРЫ ====================
            { "Telegram", "Telegram" },
            { "Discord", "Discord" },
            { "WhatsApp", "WhatsApp" },
            { "Zoom", "Zoom" },
            { "Skype", "Skype" },
            { "Slack", "Slack" },
            { "Teams", "Microsoft Teams" },
            { "ms-teams", "Microsoft Teams" },
            { "Signal", "Signal" },
            { "Viber", "Viber" },

            // ==================== МЕДИА ====================
            { "Spotify", "Spotify" },
            { "SpotifyAB", "Spotify" },
            { "vlc", "VLC" },
            { "mpv", "mpv" },
            { "PotPlayerMini64", "PotPlayer" },
            { "obs64", "OBS Studio" },
            { "obs32", "OBS Studio" },
            { "audacity", "Audacity" },
            { "Photoshop", "Adobe Photoshop" },
            { "Illustrator", "Adobe Illustrator" },
            { "Premiere Pro", "Adobe Premiere Pro" },
            { "AfterFX", "Adobe After Effects" },
            { "figma", "Figma" },
            { "gimp-2.10", "GIMP" },
            { "Inkscape", "Inkscape" },
            { "blender", "Blender" },

            // ==================== ИГРЫ / ЛАУНЧЕРЫ ====================
            { "steam", "Steam" },
            { "steamwebhelper", "Steam" },
            { "EpicGamesLauncher", "Epic Games" },
            { "Battle.net", "Battle.net" },
            { "GalaxyClient", "GOG Galaxy" },
            { "RiotClientServices", "Riot Client" },
            { "Origin", "EA Origin" },
            { "EADesktop", "EA Desktop" },
            { "UbisoftConnect", "Ubisoft Connect" },
            { "DiscordCanary", "Discord Canary" },

            // ==================== СИСТЕМА ====================
            { "explorer", "Проводник" },
            { "calc", "Калькулятор" },
            { "ApplicationFrameHost", "Приложение UWP" },
            { "SystemSettings", "Параметры Windows" },
            { "ScreenClippingHost", "Скриншот (Win+Shift+S)" },
            { "SnippingTool", "Ножницы" },
            { "mspaint", "Paint" },
            { "ShellExperienceHost", "Оболочка Windows" },
            { "SearchHost", "Поиск Windows" },
            { "StartMenuExperienceHost", "Меню Пуск" },
            { "Widgets", "Виджеты Windows" },
            { "Taskmgr", "Диспетчер задач" },
            { "regedit", "Редактор реестра" },
            { "mmc", "Консоль управления" },
            { "control", "Панель управления" },

            // ==================== ОФИС ====================
            { "WINWORD", "Microsoft Word" },
            { "EXCEL", "Microsoft Excel" },
            { "POWERPNT", "Microsoft PowerPoint" },
            { "OUTLOOK", "Microsoft Outlook" },
            { "ONENOTE", "Microsoft OneNote" },
            { "Acrobat", "Adobe Acrobat" },
            { "AcroRd32", "Adobe Reader" },
            { "SumatraPDF", "SumatraPDF" },
            { "Notion", "Notion" },
            { "Obsidian", "Obsidian" },
            { "Evernote", "Evernote" },
            { "TickTick", "TickTick" },
            { "Todoist", "Todoist" },

            // ==================== УТИЛИТЫ ====================
            { "7zFM", "7-Zip" },
            { "WinRAR", "WinRAR" },
            { "Everything", "Everything" },
            { "PowerToys", "PowerToys" },
            { "ShareX", "ShareX" },
            { "Greenshot", "Greenshot" },
            { "Lightshot", "Lightshot" },
            { "TeamViewer", "TeamViewer" },
            { "AnyDesk", "AnyDesk" },
            { "RustDesk", "RustDesk" },
            { "processhacker", "Process Hacker" },
            { "procexp64", "Process Explorer" },
            { "autoruns64", "Autoruns" },
            { "CrystalDiskMark", "CrystalDiskMark" },
            { "HWiNFO64", "HWiNFO" },
            { "GPU-Z", "GPU-Z" },
            { "CPU-Z", "CPU-Z" },
            { "MSIAfterburner", "MSI Afterburner" },
            { "RivaTuner64", "RivaTuner" },
            { "qBittorrent", "qBittorrent" },
            { "utorrent", "µTorrent" },
            { "transmission-qt", "Transmission" },

            // ==================== VPN ====================
            { "Happ", "Happ VPN" },
        };

        /// <summary>
        /// Возвращает человеческое название приложения.
        /// Если не знаем — возвращает исходное имя как есть.
        /// </summary>
        public static string GetDisplayName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return "—";

            if (Map.TryGetValue(processName, out var display))
                return display;

            return processName;
        }
    }
}
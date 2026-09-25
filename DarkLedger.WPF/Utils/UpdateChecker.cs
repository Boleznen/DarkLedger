using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

namespace DarkLedger.WPF
{
    /// <summary>
    /// Проверка обновлений через GitHub Releases API.
    /// </summary>
    internal static class UpdateChecker
    {
        // ==================== НАСТРОЙКИ ====================

        /// <summary>
        /// URL репозитория на GitHub.
        /// </summary>
        public const string GitHubRepoUrl = "https://github.com/Boleznen/DarkLedger";

        /// <summary>
        /// GitHub API endpoint для последнего релиза.
        /// </summary>
        private const string ApiUrl = "https://api.github.com/repos/Boleznen/DarkLedger/releases/latest";

        // ==================== ОСНОВНОЙ МЕТОД ====================

        /// <summary>
        /// Асинхронно проверяет обновления.
        /// </summary>
        public static async Task<UpdateCheckResult> CheckAsync()
        {
            var result = new UpdateCheckResult
            {
                CurrentVersion = GetCurrentVersion()
            };

            try
            {
                using var http = new HttpClient();
                http.DefaultRequestHeaders.Add("User-Agent", "DarkLedger-UpdateChecker");
                http.Timeout = TimeSpan.FromSeconds(10);

                string json = await http.GetStringAsync(ApiUrl);

                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string tag = root.TryGetProperty("tag_name", out var tagProp)
                    ? tagProp.GetString() ?? ""
                    : "";

                string latest = tag.TrimStart('v', 'V').Trim();

                if (string.IsNullOrEmpty(latest))
                {
                    result.CheckFailed = true;
                    result.Message = "Не удалось определить версию последнего релиза.";
                    return result;
                }

                result.LatestVersion = latest;

                result.HtmlUrl = root.TryGetProperty("html_url", out var htmlProp)
                    ? htmlProp.GetString() ?? $"{GitHubRepoUrl}/releases"
                    : $"{GitHubRepoUrl}/releases";

                result.ReleaseNotes = root.TryGetProperty("body", out var bodyProp)
                    ? bodyProp.GetString() ?? ""
                    : "";

                if (root.TryGetProperty("assets", out var assets))
                {
                    foreach (var asset in assets.EnumerateArray())
                    {
                        string name = asset.TryGetProperty("name", out var n)
                            ? n.GetString() ?? ""
                            : "";

                        if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ||
                            name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                        {
                            result.DownloadUrl = asset.TryGetProperty("browser_download_url", out var d)
                                ? d.GetString() ?? ""
                                : "";
                            break;
                        }
                    }
                }

                result.HasUpdate = IsNewer(latest, result.CurrentVersion);
            }
            catch (HttpRequestException httpEx)
            {
                result.CheckFailed = true;
                result.Message = $"Ошибка сети: {httpEx.Message}";
            }
            catch (TaskCanceledException)
            {
                result.CheckFailed = true;
                result.Message = "Превышено время ожидания запроса.";
            }
            catch (Exception ex)
            {
                result.CheckFailed = true;
                result.Message = $"Ошибка проверки: {ex.Message}";
            }

            return result;
        }

        // ==================== ОТКРЫТИЕ СТРАНИЦЫ РЕЛИЗОВ ====================

        public static void OpenReleasesPage()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = $"{GitHubRepoUrl}/releases",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Не удалось открыть страницу релизов", ex);
            }
        }

        public static void OpenReleaseUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                url = $"{GitHubRepoUrl}/releases";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logger.Error("Не удалось открыть страницу релиза", ex);
            }
        }

        // ==================== ВЕРСИЯ ====================

        public static string GetCurrentVersion()
        {
            try
            {
                var v = Assembly.GetExecutingAssembly().GetName().Version;
                if (v != null)
                {
                    if (v.Build >= 0 && v.Revision > 0)
                        return $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
                    if (v.Build >= 0)
                        return $"{v.Major}.{v.Minor}.{v.Build}";
                    return $"{v.Major}.{v.Minor}";
                }
            }
            catch { }
            return "1.0.0";
        }

        // ==================== СРАВНЕНИЕ ВЕРСИЙ ====================

        private static bool IsNewer(string latest, string current)
        {
            try
            {
                var l = Version.Parse(NormalizeVersion(latest));
                var c = Version.Parse(NormalizeVersion(current));
                return l > c;
            }
            catch
            {
                return !string.Equals(latest, current, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string NormalizeVersion(string v)
        {
            int dotCount = 0;
            foreach (char c in v)
                if (c == '.') dotCount++;

            while (dotCount < 2)
            {
                v += ".0";
                dotCount++;
            }
            return v;
        }

        // ==================== РЕЗУЛЬТАТ ====================

        public sealed class UpdateCheckResult
        {
            public string CurrentVersion { get; set; } = "";
            public string LatestVersion { get; set; } = "";
            public string ReleaseNotes { get; set; } = "";
            public string HtmlUrl { get; set; } = "";
            public string DownloadUrl { get; set; } = "";
            public bool HasUpdate { get; set; }
            public bool CheckFailed { get; set; }
            public bool CheckSkipped { get; set; }
            public string Message { get; set; } = "";
        }
    }
}
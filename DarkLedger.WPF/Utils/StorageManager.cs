using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DarkLedger.WPF
{
    internal static class StorageManager
    {
        private static readonly string StatsDir;
        private static readonly object LockObj = new object();

        static StorageManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            StatsDir = Path.Combine(appData, "DarkLedger", "Stats");
        }

        public static string GetStatsFolderPath() => StatsDir;

        private static string GetFilePathForDate(DateTime date)
            => Path.Combine(StatsDir, $"{date:yyyy-MM-dd}.json");

        private static JsonSerializerOptions CreateOptions()
        {
            return new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter() }
            };
        }

        public static void Save(DailyStats stats)
        {
            lock (LockObj)
            {
                try
                {
                    Directory.CreateDirectory(StatsDir);
                    stats.LastUpdated = DateTime.Now;

                    string json = JsonSerializer.Serialize(stats, CreateOptions());
                    File.WriteAllText(GetFilePathForDate(DateTime.Now), json);
                }
                catch (Exception ex)
                {
                    Logger.Error("Не удалось сохранить статистику", ex);
                }
            }
        }

        public static DailyStats Load(DateTime date)
        {
            lock (LockObj)
            {
                try
                {
                    string path = GetFilePathForDate(date);
                    if (!File.Exists(path)) return new DailyStats { Date = date.ToString("yyyy-MM-dd") };

                    string json = File.ReadAllText(path);
                    var loaded = JsonSerializer.Deserialize<DailyStats>(json, CreateOptions());
                    return loaded ?? new DailyStats { Date = date.ToString("yyyy-MM-dd") };
                }
                catch (Exception ex)
                {
                    Logger.Error($"Не удалось загрузить статистику за {date:yyyy-MM-dd}", ex);
                    return new DailyStats { Date = date.ToString("yyyy-MM-dd") };
                }
            }
        }

        public static List<DateTime> GetAllDates()
        {
            var result = new List<DateTime>();

            try
            {
                if (!Directory.Exists(StatsDir)) return result;

                foreach (var file in Directory.GetFiles(StatsDir, "*.json"))
                {
                    string name = Path.GetFileNameWithoutExtension(file);
                    if (DateTime.TryParseExact(name, "yyyy-MM-dd",
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None,
                        out var date))
                    {
                        result.Add(date);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Ошибка чтения списка дней", ex);
            }

            return result.OrderByDescending(d => d).ToList();
        }

        public static List<DailyStats> LoadRange(DateTime from, DateTime to)
        {
            var result = new List<DailyStats>();
            for (var d = from.Date; d <= to.Date; d = d.AddDays(1))
            {
                result.Add(Load(d));
            }
            return result;
        }
    }
}
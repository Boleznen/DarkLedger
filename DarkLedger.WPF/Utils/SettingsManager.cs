using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DarkLedger.WPF
{
    internal static class SettingsManager
    {
        private static readonly string SettingsDir;
        private static readonly string SettingsFile;
        private static AppSettings? _current;

        static SettingsManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            SettingsDir = Path.Combine(appData, "DarkLedger");
            SettingsFile = Path.Combine(SettingsDir, "settings.json");
        }

        public static AppSettings Current
        {
            get
            {
                if (_current == null) _current = Load();
                return _current;
            }
        }

        public static string GetSettingsFilePath() => SettingsFile;
        public static string GetDataFolderPath() => SettingsDir;

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFile))
                {
                    string json = File.ReadAllText(SettingsFile);
                    var options = new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() }
                    };
                    var loaded = JsonSerializer.Deserialize<AppSettings>(json, options);
                    if (loaded != null)
                    {
                        Logger.Info("Настройки загружены из settings.json");
                        return loaded;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error("Не удалось загрузить настройки — использую дефолтные", ex);
            }

            var fresh = new AppSettings();
            Save(fresh);
            Logger.Info("Созданы настройки по умолчанию");
            return fresh;
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                Directory.CreateDirectory(SettingsDir);

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new JsonStringEnumConverter() }
                };

                string json = JsonSerializer.Serialize(settings, options);
                File.WriteAllText(SettingsFile, json);
                _current = settings;
            }
            catch (Exception ex)
            {
                Logger.Error("Не удалось сохранить настройки", ex);
            }
        }

        public static void SaveCurrent() => Save(Current);

        public static void ResetToDefaults()
        {
            var fresh = new AppSettings();
            Save(fresh);
            Logger.Info("Настройки сброшены к дефолтным");
        }
    }
}
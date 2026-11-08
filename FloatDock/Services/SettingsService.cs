using System;
using System.IO;
using System.Text.Json;
using FloatDock.Models;

namespace FloatDock.Services
{
    public class SettingsService
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        private readonly string _settingsFilePath;

        public AppSettings CurrentSettings { get; private set; }

        public event EventHandler<AppSettings>? SettingsChanged;

        public SettingsService()
        {
            string appDataDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FloatDock"
            );

            try
            {
                if (!Directory.Exists(appDataDir))
                {
                    Directory.CreateDirectory(appDataDir);
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Failed to create app settings directory", ex);
            }

            _settingsFilePath = Path.Combine(appDataDir, "settings.json");
            CurrentSettings = LoadSettings();
        }

        public AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    string json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);
                    if (settings != null)
                    {
                        settings.Validate();
                        LoggerService.LogInfo("Settings loaded successfully from disk.");
                        return settings;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Failed to parse settings.json. Restoring defaults.", ex);
            }

            var defaultSettings = new AppSettings();
            defaultSettings.Validate();
            SaveSettings(defaultSettings);
            return defaultSettings;
        }

        public void SaveSettings(AppSettings settings)
        {
            try
            {
                settings.Validate();
                string json = JsonSerializer.Serialize(settings, JsonOptions);
                File.WriteAllText(_settingsFilePath, json);
                CurrentSettings = settings;
                SettingsChanged?.Invoke(this, CurrentSettings);
                LoggerService.LogInfo("Settings saved successfully.");
            }
            catch (Exception ex)
            {
                LoggerService.LogError("Failed to write settings.json to disk.", ex);
            }
        }
    }
}

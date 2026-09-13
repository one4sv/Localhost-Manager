using LocalhostManager.Models;
using System;
using System.IO;
using System.Text.Json;

namespace LocalhostManager.Services
{
    public class SettingsManager
    {
        private readonly string _filePath;

        public SettingsManager()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LocalhostManager"
            );

            Directory.CreateDirectory(appDataPath);

            _filePath = Path.Combine(appDataPath, "settings.json");
        }

        public AppSettings Load()
        {
            if (!File.Exists(_filePath))
            {
                return new AppSettings();
            }

            try
            {
                var json = File.ReadAllText(_filePath);

                var settings = JsonSerializer.Deserialize<AppSettings>(json);

                return settings ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void Save(AppSettings settings)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(settings, options);

            File.WriteAllText(_filePath, json);
        }
    }
}
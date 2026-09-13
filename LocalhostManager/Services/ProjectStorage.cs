using LocalhostManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace LocalhostManager.Services
{
    public class ProjectStorage
    {
        private readonly string _filePath;

        public ProjectStorage()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LocalhostManager"
            );

            Directory.CreateDirectory(appDataPath);

            _filePath = Path.Combine(appDataPath, "projects.json");
        }

        public List<Project> Load()
        {
            if (!File.Exists(_filePath))
            {
                return [];
            }

            try
            {
                var json = File.ReadAllText(_filePath);

                return JsonSerializer.Deserialize<List<Project>>(json) ?? [];
            }
            catch
            {
                return [];
            }
        }

        public void Save(IEnumerable<Project> projects)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(projects, options);

            File.WriteAllText(_filePath, json);
        }
    }
}
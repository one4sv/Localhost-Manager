using System.IO;
using System.Text.Json;
using LocalhostManager.Models;

namespace LocalhostManager.Services;

public static class ProjectArchive
{
    public static void Export(string path, IEnumerable<Project> projects)
    {
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(projects, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, path, overwrite: true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public static List<Project> Read(string path, bool restore)
    {
        var projects = JsonSerializer.Deserialize<List<Project>>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Неверный формат файла.");
        foreach (var project in projects)
        {
            if (project == null || string.IsNullOrWhiteSpace(project.Name) || project.Localhosts == null)
                throw new InvalidDataException("В файле есть проект без названия или списка сервисов.");
            if (!restore) project.AutoStart = false;
            foreach (var service in project.Localhosts)
            {
                if (service == null || string.IsNullOrWhiteSpace(service.Name) ||
                    string.IsNullOrWhiteSpace(service.Path) || string.IsNullOrWhiteSpace(service.Command))
                    throw new InvalidDataException("Для каждого сервиса нужны название, папка и команда.");
                if (!string.IsNullOrWhiteSpace(service.Port) &&
                    (!int.TryParse(service.Port, out var port) || port is < 1 or > 65535))
                    throw new InvalidDataException($"Неверный порт у сервиса {service.Name}.");
                if (!string.IsNullOrWhiteSpace(service.HealthUrl) &&
                    (!Uri.TryCreate(service.HealthUrl, UriKind.Absolute, out var url) || url.Scheme is not ("http" or "https")))
                    throw new InvalidDataException($"Неверный HTTP-адрес у сервиса {service.Name}.");
                if (!restore) service.Id = Guid.NewGuid().ToString("N");
            }
        }
        return projects;
    }
}

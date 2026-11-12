using System.IO;
using System.Text.Json;
namespace LocalhostManager.Services;
public static class ProjectDiscovery
{
    public static IReadOnlyList<string> Detect(string directory)
    {
        var commands = new List<string>();
        var package = Path.Combine(directory, "package.json");
        if (File.Exists(package))
        {
            using var json = JsonDocument.Parse(File.ReadAllText(package));
            var manager = File.Exists(Path.Combine(directory, "pnpm-lock.yaml")) ? "pnpm" :
                File.Exists(Path.Combine(directory, "yarn.lock")) ? "yarn" :
                File.Exists(Path.Combine(directory, "bun.lock")) || File.Exists(Path.Combine(directory, "bun.lockb")) ? "bun" : "npm";
            if (json.RootElement.TryGetProperty("scripts", out var scripts))
                foreach (var script in scripts.EnumerateObject().OrderBy(s => s.Name == "dev" ? 0 : s.Name == "start" ? 1 : 2))
                    commands.Add($"{manager} run \"{script.Name.Replace("\"", "")}\"");
        }
        foreach (var project in Directory.EnumerateFiles(directory, "*.csproj"))
            commands.Add($"dotnet run --project \"{Path.GetFileName(project)}\"");
        if (File.Exists(Path.Combine(directory, "manage.py"))) commands.Add("python manage.py runserver");
        if (File.Exists(Path.Combine(directory, "app.py"))) commands.Add("python app.py");
        if (File.Exists(Path.Combine(directory, "main.py"))) commands.Add("python main.py");
        return commands;
    }
}

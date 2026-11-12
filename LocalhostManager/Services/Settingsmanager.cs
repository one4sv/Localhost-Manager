using LocalhostManager.Models;
namespace LocalhostManager.Services;
public class SettingsManager(string? directory = null)
{
    private readonly JsonFileStore<AppSettings> _store = new("settings.json", directory);
    public string? Warning => _store.Warning;
    public AppSettings Load() => _store.Load();
    public void Save(AppSettings settings) => _store.Save(settings);
}

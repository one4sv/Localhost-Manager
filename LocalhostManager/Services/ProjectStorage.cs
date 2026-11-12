using LocalhostManager.Models;
namespace LocalhostManager.Services;
public class ProjectStorage(string? directory = null)
{
    private readonly JsonFileStore<List<Project>> _store = new("projects.json", directory);
    public string BackupPath => _store.BackupPath;
    public string? Warning => _store.Warning;
    public List<Project> Load() => _store.Load();
    public void Save(IEnumerable<Project> projects) => _store.Save(projects.ToList());
}

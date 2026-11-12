using System.IO;
using System.Text.Json;

namespace LocalhostManager.Services;

public sealed class JsonFileStore<T> where T : new()
{
    private readonly string _path;
    public string BackupPath => _path + ".bak";
    public string? Warning { get; private set; }

    public JsonFileStore(string fileName, string? directory = null)
    {
        directory ??= Environment.GetEnvironmentVariable("LOCALHOST_MANAGER_DATA_DIR")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "LocalhostManager");
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, fileName);
    }

    public T Load()
    {
        if (!File.Exists(_path)) return new T();
        try { return Read(_path); }
        catch (JsonException)
        {
            // Preserve the original before allowing any subsequent save.
            var damaged = _path + ".corrupt-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            File.Copy(_path, damaged);
            Warning = $"Повреждён файл {_path}. Исходная копия сохранена: {damaged}.";
            if (File.Exists(_path + ".bak"))
            {
                try
                {
                    var recovered = Read(_path + ".bak");
                    Warning += " Загружена резервная копия.";
                    File.Delete(_path);
                    Save(recovered);
                    return recovered;
                }
                catch (JsonException) { }
            }
            File.Delete(_path);
            Warning += " Резервная копия недоступна; загружены пустые данные.";
            return new T();
        }
    }

    private static T Read(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path))
        ?? throw new JsonException("JSON contains null.");

    public void Save(T value)
    {
        var temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                JsonSerializer.Serialize(stream, value, new JsonSerializerOptions { WriteIndented = true });
                stream.Flush(flushToDisk: true);
            }
            if (File.Exists(_path)) File.Replace(temporary, _path, _path + ".bak");
            else File.Move(temporary, _path);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
}

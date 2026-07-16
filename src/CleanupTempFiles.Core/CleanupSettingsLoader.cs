using System.Text.Json;

namespace CleanupTempFiles;

public static class CleanupSettingsLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static CleanupSettings Load(string path)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException($"Konfigurationsdatei nicht gefunden: '{path}'.");

        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<CleanupSettings>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Konnte '{path}' nicht als Konfiguration lesen.");

        if (settings.Directories.Count == 0)
            throw new InvalidOperationException("Konfiguration enthält keine Directories.");

        return settings;
    }
}

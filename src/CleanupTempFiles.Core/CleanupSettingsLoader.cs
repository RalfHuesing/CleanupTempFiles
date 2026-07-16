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

        if (string.IsNullOrWhiteSpace(settings.MarkerFileName))
            throw new InvalidOperationException("Konfiguration enthält keinen markerFileName.");

        if (settings.Directories is not { Count: > 0 })
            throw new InvalidOperationException("Konfiguration enthält keine Directories.");

        foreach (var directory in settings.Directories)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("Konfiguration enthält einen leeren Directory-Eintrag.");

            if (!Path.IsPathRooted(directory))
                throw new InvalidOperationException($"Directory-Eintrag ist kein absoluter Pfad: '{directory}'.");
        }

        return settings;
    }
}

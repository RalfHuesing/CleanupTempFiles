using System.Text.Json;

namespace CleanupTempFiles;

public static class CleanupSettingsLoader
{
    private const int DefaultRunTimeoutMinutes = 30;

    private static readonly IReadOnlyList<string> DefaultDeniedDirectories =
    [
        @"C:\Windows",
        @"C:\Program Files",
        @"C:\Program Files (x86)",
        @"C:\ProgramData",
        @"C:\Users",
    ];

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static CleanupSettings Load(string path)
    {
        if (!File.Exists(path))
            throw new InvalidOperationException($"Konfigurationsdatei nicht gefunden: '{path}'.");

        var json = File.ReadAllText(path);
        var dto = JsonSerializer.Deserialize<CleanupSettingsDto>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Konnte '{path}' nicht als Konfiguration lesen.");

        if (string.IsNullOrWhiteSpace(dto.MarkerFileName))
            throw new InvalidOperationException("Konfiguration enthält keinen markerFileName.");

        if (dto.Directories is not { Count: > 0 })
            throw new InvalidOperationException("Konfiguration enthält keine Directories.");

        foreach (var directory in dto.Directories)
        {
            if (string.IsNullOrWhiteSpace(directory))
                throw new InvalidOperationException("Konfiguration enthält einen leeren Directory-Eintrag.");

            if (!Path.IsPathRooted(directory))
                throw new InvalidOperationException($"Directory-Eintrag ist kein absoluter Pfad: '{directory}'.");
        }

        var deniedDirectories = dto.DeniedDirectories ?? DefaultDeniedDirectories;
        foreach (var directory in dto.Directories)
        {
            if (DangerousDirectoryGuard.IsDangerous(directory, deniedDirectories))
                throw new InvalidOperationException($"Verzeichnis '{directory}' ist als gefährliches Wurzelverzeichnis gesperrt.");
        }

        var runTimeoutMinutes = dto.RunTimeoutMinutes ?? DefaultRunTimeoutMinutes;
        if (runTimeoutMinutes <= 0)
            throw new InvalidOperationException("runTimeoutMinutes muss größer als 0 sein.");

        return new CleanupSettings(dto.MarkerFileName, dto.Directories, deniedDirectories, runTimeoutMinutes);
    }

    private sealed record CleanupSettingsDto(
        string? MarkerFileName,
        List<string>? Directories,
        List<string>? DeniedDirectories,
        int? RunTimeoutMinutes);
}

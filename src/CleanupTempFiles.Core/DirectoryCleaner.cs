using Serilog;

namespace CleanupTempFiles;

public static class DirectoryCleaner
{
    public static void CleanAll(IEnumerable<string> directories, string markerFileName, bool execute)
    {
        foreach (var directory in directories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                Clean(directory, markerFileName, execute);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unerwarteter Fehler beim Bearbeiten von {Directory}. Überspringe.", directory);
            }
        }
    }

    public static void Clean(string directoryPath, string markerFileName, bool execute)
    {
        if (!Directory.Exists(directoryPath))
        {
            Log.Warning("Verzeichnis {Directory} existiert nicht. Überspringe.", directoryPath);
            return;
        }

        var marker = MarkerFileLoader.TryLoad(directoryPath, markerFileName, out var error);
        if (marker is null)
        {
            if (error is not null)
                Log.Warning("Marker-Datei in {Directory} fehlerhaft: {Error}. Überspringe.", directoryPath, error);
            else
                Log.Debug("Keine Marker-Datei in {Directory}. Überspringe.", directoryPath);
            return;
        }

        var markerFilePath = Path.Combine(directoryPath, markerFileName);

        // Standardmaessig ueberspringt Directory.EnumerateFiles Hidden/System-Dateien - genau die Art
        // Datei, die sich in Temp-Verzeichnissen ansammelt. AttributesToSkip = ReparsePoint verhindert
        // im Gegenzug, dass ein rekursiver Lauf ueber Symlinks/Junctions das Zielverzeichnis verlaesst.
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = marker.Recursive,
            AttributesToSkip = FileAttributes.ReparsePoint,
        };

        var candidates = Directory.EnumerateFiles(directoryPath, "*", enumerationOptions)
            .Where(path => !string.Equals(path, markerFilePath, StringComparison.OrdinalIgnoreCase))
            .Select(path => new FileCandidate(path, File.GetLastWriteTimeUtc(path)));

        var nowUtc = DateTime.UtcNow;
        var toDelete = RuleMatcher.SelectFilesToDelete(candidates, marker.Rules, nowUtc);

        foreach (var file in toDelete)
            DeleteOrLog(file, execute);
    }

    private static void DeleteOrLog(FileCandidate file, bool execute)
    {
        if (!execute)
        {
            Log.Information("Würde löschen: {Path}", file.FullPath);
            return;
        }

        try
        {
            File.Delete(file.FullPath);
            Log.Information("Gelöscht: {Path}", file.FullPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning(ex, "Konnte {Path} nicht löschen. Überspringe.", file.FullPath);
        }
    }
}

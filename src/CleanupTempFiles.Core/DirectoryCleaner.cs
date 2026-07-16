using Serilog;

namespace CleanupTempFiles;

public static class DirectoryCleaner
{
    public static CleanupSummary CleanAll(IEnumerable<string> directories, string markerFileName, bool execute)
    {
        var directoriesProcessed = 0;
        var filesAffected = 0;
        long bytesAffected = 0;
        var emptyDirectoriesRemoved = 0;
        var directoriesWithErrors = 0;

        foreach (var directory in directories.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            directoriesProcessed++;
            try
            {
                var result = Clean(directory, markerFileName, execute);
                filesAffected += result.FilesAffected;
                bytesAffected += result.BytesAffected;
                emptyDirectoriesRemoved += result.EmptyDirectoriesRemoved;
                if (result.HadError)
                    directoriesWithErrors++;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unerwarteter Fehler beim Bearbeiten von {Directory}. Überspringe.", directory);
                directoriesWithErrors++;
            }
        }

        return new CleanupSummary(directoriesProcessed, filesAffected, bytesAffected, emptyDirectoriesRemoved, directoriesWithErrors);
    }

    public static DirectoryCleanupResult Clean(string directoryPath, string markerFileName, bool execute)
    {
        if (!Directory.Exists(directoryPath))
        {
            Log.Warning("Verzeichnis {Directory} existiert nicht. Überspringe.", directoryPath);
            return new DirectoryCleanupResult(0, 0, 0, HadError: true);
        }

        var marker = MarkerFileLoader.TryLoad(directoryPath, markerFileName, out var error);
        if (marker is null)
        {
            if (error is not null)
            {
                Log.Warning("Marker-Datei in {Directory} fehlerhaft: {Error}. Überspringe.", directoryPath, error);
                return new DirectoryCleanupResult(0, 0, 0, HadError: true);
            }

            Log.Debug("Keine Marker-Datei in {Directory}. Überspringe.", directoryPath);
            return new DirectoryCleanupResult(0, 0, 0, HadError: false);
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
            .Select(path =>
            {
                var info = new FileInfo(path);
                return new FileCandidate(path, info.LastWriteTimeUtc, info.Length);
            });

        var nowUtc = DateTime.UtcNow;
        var toDelete = RuleMatcher.SelectFilesToDelete(candidates, marker.Rules, marker.ExcludePatterns, nowUtc);

        var filesAffected = 0;
        long bytesAffected = 0;
        var hadError = false;

        foreach (var file in toDelete)
        {
            if (DeleteOrLog(file, execute))
            {
                filesAffected++;
                bytesAffected += file.Length;
            }
            else
            {
                hadError = true;
            }
        }

        // Nur bei echtem --execute: im Dry-Run wurden die Dateien nie wirklich geloescht, ein Verzeichnis
        // koennte also nie tatsaechlich leer sein. Eine simulierte Vorschau waere hier nur eine Untergrenze
        // und damit irrefuehrend - lieber ehrlich nichts melden als eine falsche Zahl.
        var emptyDirectoriesRemoved = 0;
        if (marker.Recursive && execute)
        {
            var (removed, removalHadError) = RemoveEmptyDirectories(directoryPath);
            emptyDirectoriesRemoved = removed;
            hadError |= removalHadError;
        }

        return new DirectoryCleanupResult(filesAffected, bytesAffected, emptyDirectoriesRemoved, hadError);
    }

    private static bool DeleteOrLog(FileCandidate file, bool execute)
    {
        if (!execute)
        {
            Log.Information("Würde löschen: {Path}", file.FullPath);
            return true;
        }

        try
        {
            File.Delete(file.FullPath);
            Log.Information("Gelöscht: {Path}", file.FullPath);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            Log.Warning(ex, "Konnte {Path} nicht löschen. Überspringe.", file.FullPath);
            return false;
        }
    }

    private static (int Removed, bool HadError) RemoveEmptyDirectories(string rootPath)
    {
        var enumerationOptions = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            AttributesToSkip = FileAttributes.ReparsePoint,
        };

        // Tiefste Verzeichnisse zuerst: OrderByDescending materialisiert die Liste vollstaendig, bevor
        // die erste Loeschung passiert, und laesst ein leer gewordenes Kind sein Elternverzeichnis
        // im selben Durchlauf ebenfalls leer werden.
        var subdirectories = Directory.EnumerateDirectories(rootPath, "*", enumerationOptions)
            .OrderByDescending(path => path.Count(c => c == Path.DirectorySeparatorChar));

        var removed = 0;
        var hadError = false;

        foreach (var directory in subdirectories)
        {
            if (Directory.EnumerateFileSystemEntries(directory).Any())
                continue;

            try
            {
                Directory.Delete(directory);
                Log.Information("Leeres Verzeichnis entfernt: {Directory}", directory);
                removed++;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.Warning(ex, "Konnte leeres Verzeichnis {Directory} nicht entfernen. Überspringe.", directory);
                hadError = true;
            }
        }

        return (removed, hadError);
    }
}

namespace CleanupTempFiles;

public static class DangerousDirectoryGuard
{
    // Laufwerkswurzeln sind immer gesperrt, unabhaengig von appsettings - das ist die Notbremse
    // gegen den denkbar schlimmsten Konfigurationsfehler. Unterverzeichnisse (z.B. C:\Windows\Temp)
    // bleiben davon unberuehrt, nur exakte Treffer zaehlen.
    public static bool IsDangerous(string directory, IReadOnlyList<string> deniedDirectories)
    {
        var normalized = Normalize(directory);

        var root = Path.GetPathRoot(directory);
        if (!string.IsNullOrEmpty(root) && Normalize(root) == normalized)
            return true;

        return deniedDirectories.Any(denied => Normalize(denied) == normalized);
    }

    private static string Normalize(string path) => path.TrimEnd('\\', '/').ToUpperInvariant();
}

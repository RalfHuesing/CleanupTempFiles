using System.IO.Enumeration;

namespace CleanupTempFiles;

public static class RuleMatcher
{
    // Erste passende Regel (nach Reihenfolge in der Marker-Datei) entscheidet über den Alters-Schwellwert.
    // exclude-Muster werden zuerst geprüft und gewinnen immer, unabhängig von den Regeln.
    public static IEnumerable<FileCandidate> SelectFilesToDelete(
        IEnumerable<FileCandidate> files,
        IReadOnlyList<CleanupRule> rules,
        IReadOnlyList<string> excludePatterns,
        DateTime nowUtc)
    {
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file.FullPath);

            if (excludePatterns.Any(pattern => MatchesPattern(fileName, pattern)))
                continue;

            var rule = FindMatchingRule(fileName, rules);
            if (rule is null)
                continue;

            var age = nowUtc - file.LastWriteTimeUtc;
            if (age >= rule.OlderThan)
                yield return file;
        }
    }

    private static CleanupRule? FindMatchingRule(string fileName, IReadOnlyList<CleanupRule> rules)
    {
        foreach (var rule in rules)
        {
            if (MatchesPattern(fileName, rule.Pattern))
                return rule;
        }

        return null;
    }

    private static bool MatchesPattern(string fileName, string pattern)
    {
        // "*.*" gilt - wie unter Windows historisch ueblich - als "alle Dateien", auch ohne Dateiendung.
        // FileSystemName.MatchesSimpleExpression folgt dagegen strikter Glob-Semantik und würde das
        // ohne diese Sonderbehandlung ablehnen, wenn der Dateiname keinen Punkt enthält.
        if (pattern == "*.*")
            return true;

        return FileSystemName.MatchesSimpleExpression(pattern, fileName);
    }
}

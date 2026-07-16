using System.IO.Enumeration;

namespace CleanupTempFiles;

public static class RuleMatcher
{
    // Erste passende Regel (nach Reihenfolge in der Marker-Datei) entscheidet über den Alters-Schwellwert.
    public static IEnumerable<FileCandidate> SelectFilesToDelete(
        IEnumerable<FileCandidate> files,
        IReadOnlyList<CleanupRule> rules,
        DateTime nowUtc)
    {
        foreach (var file in files)
        {
            var rule = FindMatchingRule(file.FullPath, rules);
            if (rule is null)
                continue;

            var age = nowUtc - file.LastWriteTimeUtc;
            if (age >= rule.OlderThan)
                yield return file;
        }
    }

    private static CleanupRule? FindMatchingRule(string filePath, IReadOnlyList<CleanupRule> rules)
    {
        var fileName = Path.GetFileName(filePath);
        foreach (var rule in rules)
        {
            if (FileSystemName.MatchesSimpleExpression(rule.Pattern, fileName))
                return rule;
        }

        return null;
    }
}

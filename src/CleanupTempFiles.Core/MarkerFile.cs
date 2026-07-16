namespace CleanupTempFiles;

public sealed record MarkerFile(bool Recursive, IReadOnlyList<CleanupRule> Rules, IReadOnlyList<string> ExcludePatterns);

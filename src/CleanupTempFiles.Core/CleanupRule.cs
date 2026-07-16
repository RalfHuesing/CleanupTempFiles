namespace CleanupTempFiles;

public sealed record CleanupRule(string Pattern, TimeSpan OlderThan);

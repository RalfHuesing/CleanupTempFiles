namespace CleanupTempFiles.Tests;

internal static class TestData
{
    public static FileCandidate File(string path, DateTime lastWriteUtc) => new(path, lastWriteUtc);

    public static CleanupRule Rule(string pattern, string olderThan) => new(pattern, TimeSpan.Parse(olderThan));
}

namespace CleanupTempFiles.Tests;

internal static class TestData
{
    public static FileCandidate File(string path, DateTime lastWriteUtc, long length = 0) => new(path, lastWriteUtc, length);

    public static CleanupRule Rule(string pattern, string olderThan) => new(pattern, TimeSpan.Parse(olderThan));
}

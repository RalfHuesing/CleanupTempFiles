namespace CleanupTempFiles.Tests;

// Kleine Test-Infrastruktur: echtes, isoliertes Verzeichnis pro Test statt Mocking-Framework.
internal sealed class TempDirectory : IDisposable
{
    public string DirectoryPath { get; }

    public TempDirectory()
    {
        DirectoryPath = Path.Combine(Path.GetTempPath(), "CleanupTempFiles.Tests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(DirectoryPath);
    }

    public string WriteFile(string relativePath, DateTime lastWriteUtc, string content = "")
    {
        var fullPath = Path.Combine(DirectoryPath, relativePath);
        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(fullPath, content);
        File.SetLastWriteTimeUtc(fullPath, lastWriteUtc);
        return fullPath;
    }

    public void WriteMarker(string json) => WriteFile(".cleanuptempfiles.json", DateTime.UtcNow, json);

    public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);
}

namespace CleanupTempFiles.Tests;

public class DirectoryCleanerTests
{
    [Fact]
    public void DryRun_DoesNotDeleteFiles()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""{ "recursive": false, "rules": [ { "pattern": "*.tmp", "olderThan": "00:00:01" } ] }""");
        var file = dir.WriteFile("old.tmp", DateTime.UtcNow.AddDays(-1));

        DirectoryCleaner.Clean(dir.DirectoryPath, ".cleanuptempfiles.json", execute: false);

        Assert.True(File.Exists(file));
    }

    [Fact]
    public void Execute_DeletesMatchingFiles()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""{ "recursive": false, "rules": [ { "pattern": "*.tmp", "olderThan": "00:00:01" } ] }""");
        var file = dir.WriteFile("old.tmp", DateTime.UtcNow.AddDays(-1));

        DirectoryCleaner.Clean(dir.DirectoryPath, ".cleanuptempfiles.json", execute: true);

        Assert.False(File.Exists(file));
    }

    [Fact]
    public void Execute_NeverDeletesMarkerFileItself()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""{ "recursive": false, "rules": [ { "pattern": "*.*", "olderThan": "00:00:00" } ] }""");

        DirectoryCleaner.Clean(dir.DirectoryPath, ".cleanuptempfiles.json", execute: true);

        Assert.True(File.Exists(Path.Combine(dir.DirectoryPath, ".cleanuptempfiles.json")));
    }

    [Fact]
    public void NonRecursive_IgnoresFilesInSubdirectories()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""{ "recursive": false, "rules": [ { "pattern": "*.tmp", "olderThan": "00:00:00" } ] }""");
        var nested = dir.WriteFile(Path.Combine("sub", "old.tmp"), DateTime.UtcNow.AddDays(-1));

        DirectoryCleaner.Clean(dir.DirectoryPath, ".cleanuptempfiles.json", execute: true);

        Assert.True(File.Exists(nested));
    }

    [Fact]
    public void Recursive_DeletesFilesInSubdirectories()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""{ "recursive": true, "rules": [ { "pattern": "*.tmp", "olderThan": "00:00:00" } ] }""");
        var nested = dir.WriteFile(Path.Combine("sub", "old.tmp"), DateTime.UtcNow.AddDays(-1));

        DirectoryCleaner.Clean(dir.DirectoryPath, ".cleanuptempfiles.json", execute: true);

        Assert.False(File.Exists(nested));
    }

    [Fact]
    public void MissingMarkerFile_DoesNotDeleteAnything()
    {
        using var dir = new TempDirectory();
        var file = dir.WriteFile("old.tmp", DateTime.UtcNow.AddDays(-30));

        DirectoryCleaner.Clean(dir.DirectoryPath, ".cleanuptempfiles.json", execute: true);

        Assert.True(File.Exists(file));
    }
}

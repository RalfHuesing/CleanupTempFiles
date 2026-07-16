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

    [Fact]
    public void Execute_DeletesHiddenAndSystemFiles()
    {
        // Temp-Ordner enthalten oft Hidden/System-Dateien (Browser-Cache etc.) - die muessen
        // erfasst werden, sonst haeuft sich genau der Muell an, den das Tool eigentlich loeschen soll.
        using var dir = new TempDirectory();
        dir.WriteMarker("""{ "recursive": false, "rules": [ { "pattern": "*.tmp", "olderThan": "00:00:00" } ] }""");
        var hidden = dir.WriteFile("hidden.tmp", DateTime.UtcNow.AddDays(-1));
        File.SetAttributes(hidden, FileAttributes.Hidden);
        var system = dir.WriteFile("system.tmp", DateTime.UtcNow.AddDays(-1));
        File.SetAttributes(system, FileAttributes.System);

        DirectoryCleaner.Clean(dir.DirectoryPath, ".cleanuptempfiles.json", execute: true);

        Assert.False(File.Exists(hidden));
        Assert.False(File.Exists(system));
    }

    [Fact]
    public void Recursive_DoesNotFollowJunctionsOutsideTargetDirectory()
    {
        using var outside = new TempDirectory();
        var untouchedFile = outside.WriteFile("untouched.tmp", DateTime.UtcNow.AddDays(-10));

        using var dir = new TempDirectory();
        dir.WriteMarker("""{ "recursive": true, "rules": [ { "pattern": "*.tmp", "olderThan": "00:00:00" } ] }""");

        var junctionPath = Path.Combine(dir.DirectoryPath, "linked");
        var mklink = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c mklink /J \"{junctionPath}\" \"{outside.DirectoryPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
        })!;
        mklink.WaitForExit();
        Assert.Equal(0, mklink.ExitCode);

        DirectoryCleaner.Clean(dir.DirectoryPath, ".cleanuptempfiles.json", execute: true);

        Assert.True(File.Exists(untouchedFile));

        // Junction vor dem TempDirectory-Teardown entfernen: rekursives Directory.Delete
        // ueber eine noch bestehende Junction hinweg verhaelt sich unter Windows nicht zuverlaessig.
        Directory.Delete(junctionPath);
    }

    [Fact]
    public void CleanAll_OneFaultyDirectory_StillProcessesTheOthers()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""{ "recursive": false, "rules": [ { "pattern": "*.tmp", "olderThan": "00:00:00" } ] }""");
        var file = dir.WriteFile("old.tmp", DateTime.UtcNow.AddDays(-1));

        // Pfad mit eingebettetem NUL-Zeichen laesst Directory.Exists mit einer ArgumentException scheitern -
        // simuliert eine kaputte/unzugaengliche Konfiguration, ohne echte Berechtigungen manipulieren zu muessen.
        var faultyDirectory = dir.DirectoryPath + "\0bad";

        DirectoryCleaner.CleanAll([faultyDirectory, dir.DirectoryPath], ".cleanuptempfiles.json", execute: true);

        Assert.False(File.Exists(file));
    }

    [Fact]
    public void Exclude_ProtectsMatchingFilesEvenIfARuleWouldDeleteThem()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""
            { "recursive": false, "rules": [ { "pattern": "*.*", "olderThan": "00:00:00" } ], "exclude": [ "important.log" ] }
            """);
        var protectedFile = dir.WriteFile("important.log", DateTime.UtcNow.AddDays(-10));
        var otherFile = dir.WriteFile("other.log", DateTime.UtcNow.AddDays(-10));

        DirectoryCleaner.Clean(dir.DirectoryPath, ".cleanuptempfiles.json", execute: true);

        Assert.True(File.Exists(protectedFile));
        Assert.False(File.Exists(otherFile));
    }

    [Fact]
    public void Clean_ReturnsFileCountAndByteSum()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""{ "recursive": false, "rules": [ { "pattern": "*.tmp", "olderThan": "00:00:00" } ] }""");
        dir.WriteFile("a.tmp", DateTime.UtcNow.AddDays(-1), "12345");
        dir.WriteFile("b.tmp", DateTime.UtcNow.AddDays(-1), "1234567890");

        var result = DirectoryCleaner.Clean(dir.DirectoryPath, ".cleanuptempfiles.json", execute: false);

        Assert.Equal(2, result.FilesAffected);
        Assert.Equal(15, result.BytesAffected);
        Assert.False(result.HadError);
    }

    [Fact]
    public void Clean_MissingDirectory_ReportsAsError()
    {
        var result = DirectoryCleaner.Clean(@"C:\this\does\not\exist\hopefully", ".cleanuptempfiles.json", execute: true);

        Assert.True(result.HadError);
        Assert.Equal(0, result.FilesAffected);
    }

    [Fact]
    public void Clean_InvalidMarkerFile_ReportsAsError()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("{ not valid json");

        var result = DirectoryCleaner.Clean(dir.DirectoryPath, ".cleanuptempfiles.json", execute: true);

        Assert.True(result.HadError);
    }

    [Fact]
    public void Clean_NoMarkerFile_IsNotReportedAsError()
    {
        using var dir = new TempDirectory();

        var result = DirectoryCleaner.Clean(dir.DirectoryPath, ".cleanuptempfiles.json", execute: true);

        Assert.False(result.HadError);
    }

    [Fact]
    public void CleanAll_AggregatesStatsAndErrorCountAcrossDirectories()
    {
        using var goodDir = new TempDirectory();
        goodDir.WriteMarker("""{ "recursive": false, "rules": [ { "pattern": "*.tmp", "olderThan": "00:00:00" } ] }""");
        goodDir.WriteFile("a.tmp", DateTime.UtcNow.AddDays(-1), "12345");

        using var brokenDir = new TempDirectory();
        brokenDir.WriteMarker("{ not valid json");

        var summary = DirectoryCleaner.CleanAll([goodDir.DirectoryPath, brokenDir.DirectoryPath], ".cleanuptempfiles.json", execute: true);

        Assert.Equal(2, summary.DirectoriesProcessed);
        Assert.Equal(1, summary.FilesAffected);
        Assert.Equal(5, summary.BytesAffected);
        Assert.Equal(1, summary.DirectoriesWithErrors);
    }
}

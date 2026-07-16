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
}

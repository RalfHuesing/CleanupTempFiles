namespace CleanupTempFiles.Tests;

public class CleanupSettingsLoaderTests
{
    [Fact]
    public void Load_ParsesDirectoriesAndMarkerFileName()
    {
        using var dir = new TempDirectory();
        var path = dir.WriteFile("appsettings.json", DateTime.UtcNow, """
            {
              "markerFileName": ".cleanuptempfiles.json",
              "directories": [ "C:\\Temp", "C:\\Windows\\Temp" ]
            }
            """);

        var settings = CleanupSettingsLoader.Load(path);

        Assert.Equal(".cleanuptempfiles.json", settings.MarkerFileName);
        Assert.Equal(["C:\\Temp", "C:\\Windows\\Temp"], settings.Directories);
        Assert.Equal(30, settings.RunTimeoutMinutes);
        Assert.NotEmpty(settings.DeniedDirectories);
    }

    [Fact]
    public void Load_Throws_WhenFileMissing()
    {
        using var dir = new TempDirectory();
        var path = Path.Combine(dir.DirectoryPath, "missing.json");

        Assert.Throws<InvalidOperationException>(() => CleanupSettingsLoader.Load(path));
    }

    [Fact]
    public void Load_Throws_WhenDirectoriesEmpty()
    {
        using var dir = new TempDirectory();
        var path = dir.WriteFile("appsettings.json", DateTime.UtcNow, """
            { "markerFileName": ".cleanuptempfiles.json", "directories": [] }
            """);

        Assert.Throws<InvalidOperationException>(() => CleanupSettingsLoader.Load(path));
    }

    [Fact]
    public void Load_Throws_WhenDirectoriesKeyIsMissingEntirely()
    {
        using var dir = new TempDirectory();
        var path = dir.WriteFile("appsettings.json", DateTime.UtcNow, """
            { "markerFileName": ".cleanuptempfiles.json" }
            """);

        Assert.Throws<InvalidOperationException>(() => CleanupSettingsLoader.Load(path));
    }

    [Fact]
    public void Load_Throws_WhenMarkerFileNameIsMissing()
    {
        using var dir = new TempDirectory();
        var path = dir.WriteFile("appsettings.json", DateTime.UtcNow, """
            { "directories": [ "C:\\Temp" ] }
            """);

        Assert.Throws<InvalidOperationException>(() => CleanupSettingsLoader.Load(path));
    }

    [Fact]
    public void Load_Throws_WhenDirectoryEntryIsNotRooted()
    {
        using var dir = new TempDirectory();
        var path = dir.WriteFile("appsettings.json", DateTime.UtcNow, """
            { "markerFileName": ".cleanuptempfiles.json", "directories": [ "relative\\path" ] }
            """);

        Assert.Throws<InvalidOperationException>(() => CleanupSettingsLoader.Load(path));
    }

    [Fact]
    public void Load_Throws_WhenDirectoryIsADriveRoot_EvenWithoutExplicitDenyList()
    {
        using var dir = new TempDirectory();
        var path = dir.WriteFile("appsettings.json", DateTime.UtcNow, """
            { "markerFileName": ".cleanuptempfiles.json", "directories": [ "C:\\" ] }
            """);

        Assert.Throws<InvalidOperationException>(() => CleanupSettingsLoader.Load(path));
    }

    [Fact]
    public void Load_Throws_WhenDirectoryMatchesDefaultDeniedEntry()
    {
        using var dir = new TempDirectory();
        var path = dir.WriteFile("appsettings.json", DateTime.UtcNow, """
            { "markerFileName": ".cleanuptempfiles.json", "directories": [ "C:\\Windows" ] }
            """);

        Assert.Throws<InvalidOperationException>(() => CleanupSettingsLoader.Load(path));
    }

    [Fact]
    public void Load_Allows_SubdirectoryOfDefaultDeniedEntry()
    {
        using var dir = new TempDirectory();
        var path = dir.WriteFile("appsettings.json", DateTime.UtcNow, """
            { "markerFileName": ".cleanuptempfiles.json", "directories": [ "C:\\Windows\\Temp" ] }
            """);

        var settings = CleanupSettingsLoader.Load(path);

        Assert.Equal(["C:\\Windows\\Temp"], settings.Directories);
    }

    [Fact]
    public void Load_Throws_WhenDirectoryMatchesCustomDeniedEntry()
    {
        using var dir = new TempDirectory();
        var path = dir.WriteFile("appsettings.json", DateTime.UtcNow, """
            {
              "markerFileName": ".cleanuptempfiles.json",
              "directories": [ "D:\\Data" ],
              "deniedDirectories": [ "D:\\Data" ]
            }
            """);

        Assert.Throws<InvalidOperationException>(() => CleanupSettingsLoader.Load(path));
    }

    [Fact]
    public void Load_UsesExplicitRunTimeoutMinutes()
    {
        using var dir = new TempDirectory();
        var path = dir.WriteFile("appsettings.json", DateTime.UtcNow, """
            { "markerFileName": ".cleanuptempfiles.json", "directories": [ "C:\\Temp" ], "runTimeoutMinutes": 5 }
            """);

        var settings = CleanupSettingsLoader.Load(path);

        Assert.Equal(5, settings.RunTimeoutMinutes);
    }

    [Fact]
    public void Load_Throws_WhenRunTimeoutMinutesIsZeroOrNegative()
    {
        using var dir = new TempDirectory();
        var path = dir.WriteFile("appsettings.json", DateTime.UtcNow, """
            { "markerFileName": ".cleanuptempfiles.json", "directories": [ "C:\\Temp" ], "runTimeoutMinutes": 0 }
            """);

        Assert.Throws<InvalidOperationException>(() => CleanupSettingsLoader.Load(path));
    }
}

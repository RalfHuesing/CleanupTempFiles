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
}

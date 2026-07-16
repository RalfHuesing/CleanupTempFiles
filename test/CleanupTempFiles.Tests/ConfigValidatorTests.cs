namespace CleanupTempFiles.Tests;

public class ConfigValidatorTests
{
    private const string MarkerFileName = ".cleanuptempfiles.json";

    private static CleanupSettings SettingsFor(params string[] directories) =>
        new(MarkerFileName, directories, [], RunTimeoutMinutes: 30);

    [Fact]
    public void ValidConfig_HasNoProblems()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""{ "recursive": false, "rules": [ { "pattern": "*.tmp", "olderThan": "1.00:00:00" } ] }""");

        var result = ConfigValidator.Validate(SettingsFor(dir.DirectoryPath));

        Assert.True(result.IsValid);
        Assert.Empty(result.Problems);
        Assert.Equal(1, result.DirectoriesChecked);
    }

    [Fact]
    public void MissingMarkerFile_IsNotAProblem()
    {
        using var dir = new TempDirectory();

        var result = ConfigValidator.Validate(SettingsFor(dir.DirectoryPath));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void NonExistentDirectory_IsNotAProblem()
    {
        var result = ConfigValidator.Validate(SettingsFor(@"C:\this\does\not\exist\hopefully"));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void InvalidMarkerFile_IsReportedAsProblem()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("{ not valid json");

        var result = ConfigValidator.Validate(SettingsFor(dir.DirectoryPath));

        Assert.False(result.IsValid);
        Assert.Single(result.Problems);
    }

    [Fact]
    public void MixedDirectories_ReportsOnlyTheBrokenOne()
    {
        using var goodDir = new TempDirectory();
        goodDir.WriteMarker("""{ "recursive": false, "rules": [ { "pattern": "*.tmp", "olderThan": "1.00:00:00" } ] }""");

        using var brokenDir = new TempDirectory();
        brokenDir.WriteMarker("""{ "recursive": false, "rules": [] }""");

        var result = ConfigValidator.Validate(SettingsFor(goodDir.DirectoryPath, brokenDir.DirectoryPath));

        Assert.False(result.IsValid);
        Assert.Single(result.Problems);
        Assert.Equal(2, result.DirectoriesChecked);
    }

    [Fact]
    public void DuplicateDirectories_AreCheckedOnce()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""{ "recursive": false, "rules": [ { "pattern": "*.tmp", "olderThan": "1.00:00:00" } ] }""");

        var result = ConfigValidator.Validate(SettingsFor(dir.DirectoryPath, dir.DirectoryPath));

        Assert.Equal(1, result.DirectoriesChecked);
    }
}

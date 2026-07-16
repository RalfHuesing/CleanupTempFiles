namespace CleanupTempFiles.Tests;

public class MarkerFileLoaderTests
{
    [Fact]
    public void ReturnsNull_WhenMarkerFileMissing()
    {
        using var dir = new TempDirectory();

        var marker = MarkerFileLoader.TryLoad(dir.DirectoryPath, ".cleanuptempfiles.json", out var error);

        Assert.Null(marker);
        Assert.Null(error);
    }

    [Fact]
    public void ParsesRulesAndRecursiveFlag()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""
            {
              "recursive": true,
              "rules": [
                { "pattern": "*.pdf", "olderThan": "2.00:00:00" },
                { "pattern": "*.tmp", "olderThan": "00:10:00" }
              ]
            }
            """);

        var marker = MarkerFileLoader.TryLoad(dir.DirectoryPath, ".cleanuptempfiles.json", out var error);

        Assert.Null(error);
        Assert.NotNull(marker);
        Assert.True(marker.Recursive);
        Assert.Equal(2, marker.Rules.Count);
        Assert.Equal("*.pdf", marker.Rules[0].Pattern);
        Assert.Equal(TimeSpan.FromDays(2), marker.Rules[0].OlderThan);
        Assert.Empty(marker.ExcludePatterns);
    }

    [Fact]
    public void ParsesExcludePatterns()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""
            {
              "recursive": false,
              "rules": [ { "pattern": "*.*", "olderThan": "1.00:00:00" } ],
              "exclude": [ "important.log", "keep-*" ]
            }
            """);

        var marker = MarkerFileLoader.TryLoad(dir.DirectoryPath, ".cleanuptempfiles.json", out var error);

        Assert.Null(error);
        Assert.NotNull(marker);
        Assert.Equal(["important.log", "keep-*"], marker.ExcludePatterns);
    }

    [Fact]
    public void ReturnsError_WhenExcludePatternIsBlank()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""
            {
              "recursive": false,
              "rules": [ { "pattern": "*.*", "olderThan": "1.00:00:00" } ],
              "exclude": [ "" ]
            }
            """);

        var marker = MarkerFileLoader.TryLoad(dir.DirectoryPath, ".cleanuptempfiles.json", out var error);

        Assert.Null(marker);
        Assert.NotNull(error);
    }

    [Fact]
    public void ReturnsError_WhenJsonIsInvalid()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("{ not valid json");

        var marker = MarkerFileLoader.TryLoad(dir.DirectoryPath, ".cleanuptempfiles.json", out var error);

        Assert.Null(marker);
        Assert.NotNull(error);
    }

    [Fact]
    public void ReturnsError_WhenRulesAreEmpty()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""{ "recursive": false, "rules": [] }""");

        var marker = MarkerFileLoader.TryLoad(dir.DirectoryPath, ".cleanuptempfiles.json", out var error);

        Assert.Null(marker);
        Assert.NotNull(error);
    }

    [Fact]
    public void ReturnsError_WhenRulesKeyIsMissingEntirely()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""{ "recursive": false }""");

        var marker = MarkerFileLoader.TryLoad(dir.DirectoryPath, ".cleanuptempfiles.json", out var error);

        Assert.Null(marker);
        Assert.NotNull(error);
    }

    [Fact]
    public void ReturnsError_WhenRulePatternIsMissing()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""{ "recursive": false, "rules": [ { "olderThan": "1.00:00:00" } ] }""");

        var marker = MarkerFileLoader.TryLoad(dir.DirectoryPath, ".cleanuptempfiles.json", out var error);

        Assert.Null(marker);
        Assert.NotNull(error);
    }

    [Fact]
    public void ReturnsError_WhenRuleOlderThanIsMissing()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""{ "recursive": false, "rules": [ { "pattern": "*.tmp" } ] }""");

        var marker = MarkerFileLoader.TryLoad(dir.DirectoryPath, ".cleanuptempfiles.json", out var error);

        Assert.Null(marker);
        Assert.NotNull(error);
    }

    [Fact]
    public void ReturnsError_WhenOlderThanIsNegative()
    {
        using var dir = new TempDirectory();
        dir.WriteMarker("""{ "recursive": false, "rules": [ { "pattern": "*.tmp", "olderThan": "-1.00:00:00" } ] }""");

        var marker = MarkerFileLoader.TryLoad(dir.DirectoryPath, ".cleanuptempfiles.json", out var error);

        Assert.Null(marker);
        Assert.NotNull(error);
    }
}

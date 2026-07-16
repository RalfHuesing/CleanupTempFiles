namespace CleanupTempFiles.Tests;

public class RuleMatcherTests
{
    private static readonly DateTime Now = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void SelectsFileOlderThanThreshold()
    {
        var file = TestData.File(@"C:\temp\report.pdf", Now - TimeSpan.FromDays(3));
        var rules = new[] { TestData.Rule("*.pdf", "2.00:00:00") };

        var result = RuleMatcher.SelectFilesToDelete([file], rules, [], Now);

        Assert.Equal([file], result);
    }

    [Fact]
    public void SkipsFileYoungerThanThreshold()
    {
        var file = TestData.File(@"C:\temp\report.pdf", Now - TimeSpan.FromHours(1));
        var rules = new[] { TestData.Rule("*.pdf", "2.00:00:00") };

        var result = RuleMatcher.SelectFilesToDelete([file], rules, [], Now);

        Assert.Empty(result);
    }

    [Fact]
    public void SkipsFileNotMatchingAnyPattern()
    {
        var file = TestData.File(@"C:\temp\report.docx", Now - TimeSpan.FromDays(10));
        var rules = new[] { TestData.Rule("*.pdf", "2.00:00:00") };

        var result = RuleMatcher.SelectFilesToDelete([file], rules, [], Now);

        Assert.Empty(result);
    }

    [Fact]
    public void FirstMatchingRuleInOrderWins()
    {
        // *.tmp braucht 10 Minuten, die allgemeinere *.* Regel dahinter nur 1 Tag - Reihenfolge entscheidet.
        var file = TestData.File(@"C:\temp\cache.tmp", Now - TimeSpan.FromMinutes(5));
        var rules = new[]
        {
            TestData.Rule("*.tmp", "00:10:00"),
            TestData.Rule("*.*", "1.00:00:00"),
        };

        var result = RuleMatcher.SelectFilesToDelete([file], rules, [], Now);

        Assert.Empty(result);
    }

    [Fact]
    public void ExactAgeThresholdIsInclusive()
    {
        var file = TestData.File(@"C:\temp\cache.tmp", Now - TimeSpan.FromMinutes(10));
        var rules = new[] { TestData.Rule("*.tmp", "00:10:00") };

        var result = RuleMatcher.SelectFilesToDelete([file], rules, [], Now);

        Assert.Equal([file], result);
    }

    [Fact]
    public void WildcardStarDotStar_AlsoMatchesFilesWithoutExtension()
    {
        // "*.*" hat unter Windows historisch eine Sonderbedeutung ("alle Dateien, auch ohne Endung").
        // Wer in der Marker-Datei "*.* " fuer "alles aufraeumen" nutzt, verlaesst sich darauf.
        var file = TestData.File(@"C:\temp\noextension", Now - TimeSpan.FromDays(2));
        var rules = new[] { TestData.Rule("*.*", "1.00:00:00") };

        var result = RuleMatcher.SelectFilesToDelete([file], rules, [], Now);

        Assert.Equal([file], result);
    }

    [Fact]
    public void ExcludePattern_WinsOverAMatchingRule()
    {
        var file = TestData.File(@"C:\temp\important.log", Now - TimeSpan.FromDays(10));
        var rules = new[] { TestData.Rule("*.log", "1.00:00:00") };

        var result = RuleMatcher.SelectFilesToDelete([file], rules, ["important.log"], Now);

        Assert.Empty(result);
    }

    [Fact]
    public void ExcludePattern_OnlyAffectsMatchingFiles()
    {
        var excluded = TestData.File(@"C:\temp\important.log", Now - TimeSpan.FromDays(10));
        var other = TestData.File(@"C:\temp\other.log", Now - TimeSpan.FromDays(10));
        var rules = new[] { TestData.Rule("*.log", "1.00:00:00") };

        var result = RuleMatcher.SelectFilesToDelete([excluded, other], rules, ["important.log"], Now);

        Assert.Equal([other], result);
    }

    [Fact]
    public void ExcludePattern_SupportsWildcards()
    {
        var file = TestData.File(@"C:\temp\keep-this.tmp", Now - TimeSpan.FromDays(10));
        var rules = new[] { TestData.Rule("*.tmp", "1.00:00:00") };

        var result = RuleMatcher.SelectFilesToDelete([file], rules, ["keep-*"], Now);

        Assert.Empty(result);
    }
}

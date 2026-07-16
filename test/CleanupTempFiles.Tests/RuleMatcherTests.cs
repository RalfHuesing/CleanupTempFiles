namespace CleanupTempFiles.Tests;

public class RuleMatcherTests
{
    private static readonly DateTime Now = new(2026, 7, 16, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void SelectsFileOlderThanThreshold()
    {
        var file = TestData.File(@"C:\temp\report.pdf", Now - TimeSpan.FromDays(3));
        var rules = new[] { TestData.Rule("*.pdf", "2.00:00:00") };

        var result = RuleMatcher.SelectFilesToDelete([file], rules, Now);

        Assert.Equal([file], result);
    }

    [Fact]
    public void SkipsFileYoungerThanThreshold()
    {
        var file = TestData.File(@"C:\temp\report.pdf", Now - TimeSpan.FromHours(1));
        var rules = new[] { TestData.Rule("*.pdf", "2.00:00:00") };

        var result = RuleMatcher.SelectFilesToDelete([file], rules, Now);

        Assert.Empty(result);
    }

    [Fact]
    public void SkipsFileNotMatchingAnyPattern()
    {
        var file = TestData.File(@"C:\temp\report.docx", Now - TimeSpan.FromDays(10));
        var rules = new[] { TestData.Rule("*.pdf", "2.00:00:00") };

        var result = RuleMatcher.SelectFilesToDelete([file], rules, Now);

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

        var result = RuleMatcher.SelectFilesToDelete([file], rules, Now);

        Assert.Empty(result);
    }

    [Fact]
    public void ExactAgeThresholdIsInclusive()
    {
        var file = TestData.File(@"C:\temp\cache.tmp", Now - TimeSpan.FromMinutes(10));
        var rules = new[] { TestData.Rule("*.tmp", "00:10:00") };

        var result = RuleMatcher.SelectFilesToDelete([file], rules, Now);

        Assert.Equal([file], result);
    }
}

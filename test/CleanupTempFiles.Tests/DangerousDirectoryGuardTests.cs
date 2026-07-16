namespace CleanupTempFiles.Tests;

public class DangerousDirectoryGuardTests
{
    [Theory]
    [InlineData(@"C:\")]
    [InlineData(@"D:\")]
    public void DriveRoot_IsAlwaysDangerous_EvenWithoutBeingInDeniedList(string driveRoot)
    {
        Assert.True(DangerousDirectoryGuard.IsDangerous(driveRoot, []));
    }

    [Fact]
    public void SubdirectoryOfDriveRoot_IsNotDangerous()
    {
        Assert.False(DangerousDirectoryGuard.IsDangerous(@"C:\Temp", []));
    }

    [Fact]
    public void EntryInDeniedList_IsDangerous()
    {
        Assert.True(DangerousDirectoryGuard.IsDangerous(@"C:\Windows", [@"C:\Windows"]));
    }

    [Fact]
    public void SubdirectoryOfDeniedEntry_IsNotDangerous()
    {
        // C:\Windows\Temp ist ein legitimes, dokumentiertes Ziel - die Denylist darf nur exakte
        // Treffer sperren, sonst waere der Hauptanwendungsfall des Tools kaputt.
        Assert.False(DangerousDirectoryGuard.IsDangerous(@"C:\Windows\Temp", [@"C:\Windows"]));
    }

    [Fact]
    public void ComparisonIsCaseInsensitive()
    {
        Assert.True(DangerousDirectoryGuard.IsDangerous(@"c:\windows", [@"C:\Windows"]));
    }

    [Fact]
    public void TrailingSlashDoesNotMatter()
    {
        Assert.True(DangerousDirectoryGuard.IsDangerous(@"C:\Windows\", [@"C:\Windows"]));
    }

    [Fact]
    public void DirectoryNotInDenyList_IsNotDangerous()
    {
        Assert.False(DangerousDirectoryGuard.IsDangerous(@"C:\Users\ralf\AppData\Local\Temp", [@"C:\Windows"]));
    }
}

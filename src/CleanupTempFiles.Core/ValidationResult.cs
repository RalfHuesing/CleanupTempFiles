namespace CleanupTempFiles;

public sealed record ValidationResult(int DirectoriesChecked, IReadOnlyList<string> Problems)
{
    public bool IsValid => Problems.Count == 0;
}

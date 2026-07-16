namespace CleanupTempFiles;

public sealed record CleanupSummary(int DirectoriesProcessed, int FilesAffected, long BytesAffected, int EmptyDirectoriesRemoved, int DirectoriesWithErrors);

namespace CleanupTempFiles;

public readonly record struct DirectoryCleanupResult(int FilesAffected, long BytesAffected, bool HadError);

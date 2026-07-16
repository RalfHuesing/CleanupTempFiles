namespace CleanupTempFiles;

public readonly record struct FileCandidate(string FullPath, DateTime LastWriteTimeUtc);

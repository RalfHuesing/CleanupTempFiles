namespace CleanupTempFiles;

public sealed record CleanupSettings(
    string MarkerFileName,
    IReadOnlyList<string> Directories,
    IReadOnlyList<string> DeniedDirectories,
    int RunTimeoutMinutes);

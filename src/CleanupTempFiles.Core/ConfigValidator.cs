using Serilog;

namespace CleanupTempFiles;

public static class ConfigValidator
{
    // Denylist/rooted-path-Verstoesse sind hier absichtlich nicht nochmal geprueft: CleanupSettingsLoader.Load
    // laesst so eine Konfiguration gar nicht erst durchkommen, bevor Validate() ueberhaupt aufgerufen wird.
    public static ValidationResult Validate(CleanupSettings settings)
    {
        var directories = settings.Directories.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var problems = new List<string>();

        foreach (var directory in directories)
        {
            if (!Directory.Exists(directory))
            {
                Log.Information("{Directory} existiert nicht (wird zur Laufzeit übersprungen).", directory);
                continue;
            }

            var marker = MarkerFileLoader.TryLoad(directory, settings.MarkerFileName, out var error);
            if (marker is null && error is not null)
                problems.Add($"{directory}: {error}");
        }

        return new ValidationResult(directories.Count, problems);
    }
}

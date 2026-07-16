using System.CommandLine;
using Serilog;

namespace CleanupTempFiles;

public static class Program
{
    private const string MutexName = "Global\\CleanupTempFiles";

    public static int Main(string[] args)
    {
        var executeOption = new Option<bool>("--execute")
        {
            Description = "Dateien tatsächlich löschen. Ohne diesen Schalter läuft das Tool nur als Dry-Run und protokolliert, was es tun würde."
        };

        var validateOption = new Option<bool>("--validate")
        {
            Description = "Prüft appsettings.json und alle Marker-Dateien, ohne etwas zu löschen (auch nicht im Dry-Run-Sinn)."
        };

        var rootCommand = new RootCommand("Löscht temporäre Dateien anhand von Marker-Dateien in konfigurierten Verzeichnissen.")
        {
            executeOption,
            validateOption,
        };

        rootCommand.SetAction(parseResult => Run(parseResult.GetValue(executeOption), parseResult.GetValue(validateOption)));

        return rootCommand.Parse(args).Invoke();
    }

    private static int Run(bool execute, bool validate)
    {
        ConfigureLogging();
        try
        {
            if (execute && validate)
            {
                Log.Error("--execute und --validate können nicht gemeinsam verwendet werden.");
                return 1;
            }

            try
            {
                CleanupSettings settings;
                try
                {
                    var settingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
                    settings = CleanupSettingsLoader.Load(settingsPath);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Konnte Konfiguration nicht laden.");
                    return 1;
                }

                return validate ? RunValidate(settings) : RunCleanup(settings, execute);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Unerwarteter Fehler.");
                return 1;
            }
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static int RunValidate(CleanupSettings settings)
    {
        Log.Information("CleanupTempFiles gestartet. Modus: VALIDATE");

        var result = ConfigValidator.Validate(settings);
        foreach (var problem in result.Problems)
            Log.Error("{Problem}", problem);

        Log.Information(
            "Validierung abgeschlossen: {Checked} Verzeichnis(se) geprüft, {ProblemCount} Problem(e).",
            result.DirectoriesChecked, result.Problems.Count);

        return result.IsValid ? 0 : 1;
    }

    private static int RunCleanup(CleanupSettings settings, bool execute)
    {
        // Nur der echte Aufräum-Lauf braucht die Mutex-Sperre - --validate liest nur und darf
        // parallel zu einem laufenden Aufräum-Vorgang benutzt werden, um die naechste Config zu pruefen.
        using var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
        if (!createdNew)
        {
            Log.Warning("Eine andere Instanz von CleanupTempFiles läuft bereits. Breche ab.");
            return 2;
        }

        try
        {
            Log.Information("CleanupTempFiles gestartet. Modus: {Mode}", execute ? "EXECUTE" : "DRY-RUN");

            var timeout = TimeSpan.FromMinutes(settings.RunTimeoutMinutes);
            var cleanupTask = Task.Run(() => DirectoryCleaner.CleanAll(settings.Directories, settings.MarkerFileName, execute));

            if (!cleanupTask.Wait(timeout))
            {
                Log.Error("Zeitüberschreitung nach {Timeout} - Lauf wird abgebrochen.", timeout);
                return 4;
            }

            var summary = cleanupTask.Result;
            Log.Information(
                "Lauf abgeschlossen: {Directories} Verzeichnis(se) verarbeitet, {Files} Datei(en) {Verb} ({Bytes} Bytes), {EmptyDirs} leere Verzeichnis(se) entfernt, {ErrorDirs} Verzeichnis(se) mit Problemen.",
                summary.DirectoriesProcessed,
                summary.FilesAffected,
                execute ? "gelöscht" : "würden gelöscht",
                summary.BytesAffected,
                summary.EmptyDirectoriesRemoved,
                summary.DirectoriesWithErrors);

            return summary.DirectoriesWithErrors > 0 ? 3 : 0;
        }
        finally
        {
            mutex.ReleaseMutex();
        }
    }

    private static void ConfigureLogging()
    {
        var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "log-.txt");
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14)
            .CreateLogger();
    }
}

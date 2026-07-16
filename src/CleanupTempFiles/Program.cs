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

        var rootCommand = new RootCommand("Löscht temporäre Dateien anhand von Marker-Dateien in konfigurierten Verzeichnissen.")
        {
            executeOption
        };

        rootCommand.SetAction(parseResult => Run(parseResult.GetValue(executeOption)));

        return rootCommand.Parse(args).Invoke();
    }

    private static int Run(bool execute)
    {
        ConfigureLogging();
        try
        {
            try
            {
                using var mutex = new Mutex(initiallyOwned: true, MutexName, out var createdNew);
                if (!createdNew)
                {
                    Log.Warning("Eine andere Instanz von CleanupTempFiles läuft bereits. Breche ab.");
                    return 2;
                }

                try
                {
                    Log.Information("CleanupTempFiles gestartet. Modus: {Mode}", execute ? "EXECUTE" : "DRY-RUN");

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

                    DirectoryCleaner.CleanAll(settings.Directories, settings.MarkerFileName, execute);

                    Log.Information("CleanupTempFiles beendet.");
                    return 0;
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
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

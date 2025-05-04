using System.IO.Compression;
using Serilog;

namespace Global;

/// <summary>
/// Provides Logging util utils
/// </summary>
public static class Log
{
    // Serilog instace
    private static volatile ILogger? _logger;

    // Lock for the initialization
    private static readonly object _syncRoot = new();

    /// <summary>
    /// True once SetupLogging has successfully run.
    /// </summary>
    public static bool IsInitialized
    {
        get
        {
            // safe read of volatile
            return _logger != null;
        }
    }

    /// <summary>
    /// Determines if the application is running in debug mode
    /// </summary>
    public static bool IsDebugMode
    {
        get
        {
#if DEBUG
            return true;
#else
            return System.Diagnostics.Debugger.IsAttached;
#endif
        }
    }

    /// <summary>
    /// Determines if a debugger is currently attached
    /// </summary>
    public static bool IsDebuggerAttached => System.Diagnostics.Debugger.IsAttached;

    /// <summary>
    /// The current runtime environment (Development, Staging, Production)
    /// </summary>
    public static string Environment =>
        System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

    /// <summary>
    /// Determines if the application is running in the Development environment
    /// </summary>
    public static bool IsDevelopment => Environment.Equals("Development", StringComparison.OrdinalIgnoreCase);


    public static void SetupLogging(string logsDirectory)
    {

        if (_logger != null)
            return;

        lock (_syncRoot)
        {


            if (_logger != null)
                return; //Double-check to avoid race condition


            if (string.IsNullOrWhiteSpace(logsDirectory))
                logsDirectory = Directory.GetCurrentDirectory();

            Directory.CreateDirectory(logsDirectory);

            var latest = Path.Combine(logsDirectory, "latest.txt");

            if (File.Exists(latest))
            {


                var lastModUtc = File.GetLastWriteTimeUtc(latest);
                string date = lastModUtc.ToString("yyyy-MM-dd");

                var pattern = $"{date}-*.zip";
                var existing = Directory.GetFiles(logsDirectory, pattern)
                                        .Select(Path.GetFileNameWithoutExtension)
                                        .Select(name =>
                                        {
                                            var parts = name.Split('-');
                                            if (parts.Length < 4) return 0;
                                            return int.TryParse(parts.Last(), out var i) ? i : 0;
                                        });

                int nextIndex = existing.DefaultIfEmpty(0).Max() + 1;

                string archiveName = $"{date}-{nextIndex}.zip";
                string archivePath = Path.Combine(logsDirectory, archiveName);

                using (var zip = ZipFile.Open(archivePath, ZipArchiveMode.Update))
                {
                    zip.CreateEntryFromFile(latest, "latest.txt", CompressionLevel.Optimal);
                    var entry = zip.GetEntry("latest.txt");
                    entry.LastWriteTime = lastModUtc;
                }
                File.Delete(latest);
            }

            var loggerConfig = new LoggerConfiguration()
                                       .MinimumLevel.Is(Log.IsDebugMode
                                                       ? Serilog.Events.LogEventLevel.Debug
                                                       : Serilog.Events.LogEventLevel.Information)
                                       .WriteTo.Console()
                                       .WriteTo.File(latest);

            _logger = loggerConfig.CreateLogger();
            Serilog.Log.Logger = _logger;
        }
    }

    /// <summary>
    /// The Global Logger.
    ///
    /// Writes the log file in the current directory if the SetupLogging methode was not called
    /// </summary>
    public static ILogger Logger
    {
        get
        {
            if (_logger == null)
            {
                SetupLogging(Directory.GetCurrentDirectory());
            }
            return _logger!;
        }
    }

    static Log()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, __) =>
        {
            if (IsInitialized) Serilog.Log.CloseAndFlush();
        };
    }
}

using System.IO.Compression;
using Serilog;
using Serilog.Core;
using Serilog.Exceptions;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

namespace Common;

/// <summary>
/// Provides Logging util utils
/// </summary>
public static class Log
{
    // Serilog instance
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

    /// <summary>
    /// Global logger instance. Creates the logger if it doesn't exist.
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

    public static void SetupLogging(string logsDirectory)
    {
        if (_logger != null)
            return;

        lock (_syncRoot)
        {
            if (_logger != null)
                return;

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
                    ? LogEventLevel.Debug
                    : LogEventLevel.Information)
                // Configure Microsoft logging levels - keeping important info but reducing noise
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
                // Add enrichers
                .Enrich.FromLogContext()
                .Enrich.WithExceptionDetails()
                // Configure output
                .WriteTo.Console(
                    outputTemplate: "[\x1b[38;2;120;220;255m{Timestamp:HH:mm:ss}\x1b[0m] [\x1b[1m\x1b[38;2;255;180;80m{Level:u3}\x1b[0m] [\x1b[38;2;180;255;120m{SourceContext}\x1b[0m] {Message:lj}{NewLine}{Exception}",
                    theme: AnsiConsoleTheme.Literate)
                .WriteTo.File(latest,
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}");

            _logger = loggerConfig.CreateLogger();
            Serilog.Log.Logger = _logger;

            // Log startup
            using (Serilog.Context.LogContext.PushProperty("SourceContext", "Logging"))
            {
                _logger.Information("Logging initialized with directory: {Directory}", logsDirectory);
            }
        }
    }

    static Log()
    {
        AppDomain.CurrentDomain.ProcessExit += (_, __) =>
        {
            if (IsInitialized)
            {
                using (Serilog.Context.LogContext.PushProperty("SourceContext", "Logging"))
                {
                    _logger?.Information("Application shutting down, flushing logs...");
                }
                Serilog.Log.CloseAndFlush();
            }
        };
    }
}

public class LoggerEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        if (!logEvent.Properties.ContainsKey("SourceContext"))
        {
            var sourceContext = "System";
            if (logEvent.Properties.ContainsKey("EventId"))
            {
                var eventId = logEvent.Properties["EventId"].ToString();
                if (eventId.Contains("Microsoft.Hosting.Lifetime"))
                    sourceContext = "API.Hosting";
                else if (eventId.Contains("Microsoft.EntityFrameworkCore"))
                    sourceContext = "Database.EF";
                else if (eventId.Contains("Microsoft"))
                    sourceContext = "API.Framework";
            }
            logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
                "SourceContext", sourceContext));
        }
    }
}

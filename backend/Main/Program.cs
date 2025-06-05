using System.Threading.Tasks;
using System.IO;
using System;
using System.Collections.Generic;
using System.Threading;
using Common;
using Tomlyn;
using Tomlyn.Model;
using Serilog.Context;

namespace Main;

public class Program
{
    private static CancellationTokenSource? _cancellationTokenSource;

    public static async Task Main(string[] args)
    {
        try
        {
            // Set up logging
            Log.SetupLogging("logs");
            using (LogContext.PushProperty("SourceContext", "Main.Startup"))
            {
                
            // Only works in terminals that support ANSI escape codes, so be aware lol
            var asciiArt =
                "\n\x1b[1m\x1b[38;2;247;111;83m" +
                "  ______      __    __    ______   .___________.    ___     \n" +
                " /  __  \\    |  |  |  |  /  __  \\  |           |   /   \\    \n" +
                "|  |  |  |   |  |  |  | |  |  |  | `---|  |----`  /  ^  \\   \n" +
                "|  |  |  |   |  |  |  | |  |  |  |     |  |      /  /_\\  \\  \n" +
                "|  `--'  '--.|  `--'  | |  `--'  |     |  |     /  _____  \\ \n" +
                " \\_____\\_____\\\\______/   \\______/      |__|    /__/     \\__\\\n" +
                "\x1b[0m" +
                "\n" +
                "\x1b[1m\x1b[38;2;255;255;255m" +
                "                Made by Randware with \u2764\uFE0F                " +
                "\x1b[0m\n";
                Log.Logger.Information("\n" + asciiArt);
                Log.Logger.Information("Starting Quota backend...");

                // Read config
                var toml = Toml.Parse(File.ReadAllText("./config.toml")).ToModel();
                
                // Get bot token
                var botSection = toml["bot"] as TomlTable ?? throw new Exception("Missing [bot] section in config.toml");
                var token = botSection["token"] as string ?? throw new Exception("Missing 'token' in [bot] section of config.toml");

                _cancellationTokenSource = new CancellationTokenSource();

                // Start API and Bot in separate tasks
                var tasks = new List<Task>
                {
                    Task.Run(() => API.Program.StartAsync(args), _cancellationTokenSource.Token),
                    Task.Run(() => Bot.Program.RunAsync(token), _cancellationTokenSource.Token)
                };

                // Handle shutdown gracefully
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true; // Prevent immediate termination
                    using (LogContext.PushProperty("SourceContext", "Main.Shutdown"))
                    {
                        Log.Logger.Information("Received shutdown signal, initiating graceful shutdown...");
                        _cancellationTokenSource?.Cancel();
                    }
                };

                AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                {
                    using (LogContext.PushProperty("SourceContext", "Main.Shutdown"))
                    {
                        Log.Logger.Information("Process exit requested, waiting for tasks to complete...");
                        _cancellationTokenSource?.Cancel();
                        Task.WhenAll(tasks).Wait(TimeSpan.FromSeconds(5));
                        Log.Logger.Information("Shutdown complete");
                    }
                };

                // Wait for both tasks to complete or cancellation
                await Task.WhenAll(tasks);
            }
        }
        catch (Exception ex)
        {
            using (LogContext.PushProperty("SourceContext", "Main.Startup"))
            {
                Log.Logger.Fatal(ex, "Fatal error in application startup");
            }
            throw;
        }
    }
}

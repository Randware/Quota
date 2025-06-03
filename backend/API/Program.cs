using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using API;
using Database;
using Common.OAuth;
using Tomlyn;
using Tomlyn.Model;
using Serilog;
using Common;
using Microsoft.Extensions.Logging;

namespace API;

public class Program
{
    public static async Task StartAsync(string[]? args = null)
    {
        var host = CreateHostBuilder(args ?? Array.Empty<string>()).Build();

        // Replace default logging with Serilog
        host.Services.GetRequiredService<IHostApplicationLifetime>()
            .ApplicationStarted.Register(() =>
            {
                // Remove default logging providers
                var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
                if (loggerFactory is Microsoft.Extensions.Logging.LoggerFactory factory)
                {
                    factory.AddSerilog(Serilog.Log.Logger, dispose: true);
                }
            });

        await host.RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args)
    {
        // Read port from config.toml
        int port = 5000;
        try
        {
            var toml = Toml.Parse(File.ReadAllText("./config.toml")).ToModel();
            if (toml.ContainsKey("server") && toml["server"] is TomlTable serverSection && serverSection.ContainsKey("port"))
            {
                var portValue = serverSection["port"];
                if (portValue is int)
                    port = (int)portValue;
                else if (portValue is long)
                    port = (int)(long)portValue;
                else if (portValue is string s && int.TryParse(s, out var parsed))
                    port = parsed;
            }
        }
        catch { /* fallback to default port */ }

        return Host.CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(webBuilder =>
            {
                webBuilder.UseStartup<Startup>();
                webBuilder.UseUrls($"http://localhost:{port}");
            })
            .UseSerilog(); // Use Serilog as the logging provider
    }
}

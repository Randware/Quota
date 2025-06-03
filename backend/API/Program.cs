using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using API;
using Database;
using Common.OAuth;
using Tomlyn;
using Tomlyn.Model;

namespace API;

public class Program
{

    public static async Task StartAsync(string[]? args = null)
    {
        var host = CreateHostBuilder(args ?? Array.Empty<string>()).Build();
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
            });
    }
}

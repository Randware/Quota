using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Database;
using Database.Model;
using Common;
using Serilog.Context;

namespace Bot;

public class Program
{
    private readonly IServiceProvider _serviceProvider;
    private readonly QuotaBot _bot;
    private readonly string _token;

    public Program(string token)
    {
        // Set up services
        var services = new ServiceCollection();
            
        // Add DbContext
        services.AddDbContext<QuotaContext>(options =>
            options.UseSqlite(DatabaseConfig.ConnectionString));
            
        // Add Storage
        services.AddScoped<Storage>();

        // Build service provider and get storage
        _serviceProvider = services.BuildServiceProvider();

        // Initialize bot with the service provider
        _bot = new QuotaBot(_serviceProvider);
        _token = token;
    }

    public static async Task RunAsync(string token)
    {
        try
        {
            using (LogContext.PushProperty("SourceContext", "Bot.Startup"))
            {
                Log.Logger.Information("Starting Discord bot...");
                var program = new Program(token);
                await program._bot.StartAsync(token);
                Log.Logger.Information("Discord bot is running");
            }
        }
        catch (Exception ex)
        {
            using (LogContext.PushProperty("SourceContext", "Bot.Startup"))
            {
                Log.Logger.Fatal(ex, "Fatal error in bot startup");
            }
            throw;
        }
    }

    public async Task StopAsync()
    {
        if (_bot != null)
        {
            using (LogContext.PushProperty("SourceContext", "Bot.Shutdown"))
            {
                await _bot.StopAsync();
                Log.Logger.Information("Bot has been stopped");
            }
        }
    }
}

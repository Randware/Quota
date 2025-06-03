using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Common;
using Database;
using Database.Model;
using Serilog.Context;

namespace Bot;

public class QuotaBot
{
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly IServiceProvider _services;
    private readonly Storage _storage;
    private bool _isReady = false;

    public QuotaBot(Storage storage)
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers,
            LogLevel = LogSeverity.Debug
        };

        _client = new DiscordSocketClient(config);
        _interactions = new InteractionService(_client);
        _storage = storage;

        var services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton(_interactions)
            .AddSingleton(_storage);
        
        _services = services.BuildServiceProvider();

        // Set up logging
        _client.Log += LogDiscordMessage;
        _interactions.Log += LogDiscordMessage;
    }

    private Task LogDiscordMessage(LogMessage msg)
    {
        var level = msg.Severity switch
        {
            LogSeverity.Critical => Serilog.Events.LogEventLevel.Fatal,
            LogSeverity.Error => Serilog.Events.LogEventLevel.Error,
            LogSeverity.Warning => Serilog.Events.LogEventLevel.Warning,
            LogSeverity.Info => Serilog.Events.LogEventLevel.Information,
            LogSeverity.Verbose => Serilog.Events.LogEventLevel.Verbose,
            LogSeverity.Debug => Serilog.Events.LogEventLevel.Debug,
            _ => Serilog.Events.LogEventLevel.Information
        };

        using (Serilog.Context.LogContext.PushProperty("SourceContext", $"Discord.{msg.Source}"))
        {
            Log.Logger.Write(level, msg.Exception, "{Message}", msg.Message);
        }
        return Task.CompletedTask;
    }

    public async Task StartAsync(string token)
    {
        // Register command modules
        await _interactions.AddModuleAsync<QuoteCommands>(_services);

        // Hook up events
        _client.Ready += OnReady;
        _client.JoinedGuild += OnJoinedGuild;
        _client.GuildAvailable += OnGuildAvailable;
        _client.UserUpdated += OnUserUpdated;
        _client.GuildMemberUpdated += OnGuildMemberUpdated;

        // Handle interactions after commands are registered
        _client.InteractionCreated += async (interaction) =>
        {
            if (!_isReady)
            {
                await interaction.RespondAsync("Bot is still starting up. Please try again in a moment.", ephemeral: true);
                return;
            }

            var ctx = new SocketInteractionContext(_client, interaction);
            await _interactions.ExecuteCommandAsync(ctx, _services);
        };

        // Start the client
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
    }

    private async Task OnReady()
    {
        try
        {
            using (LogContext.PushProperty("SourceContext", "Discord.Bot"))
            {
                Log.Logger.Information("Bot is connected and ready!");
                
                // Register commands globally
                await _interactions.RegisterCommandsGloballyAsync();
                Log.Logger.Information("Successfully registered global commands");

                // Initial sync of all guilds
                foreach (var guild in _client.Guilds)
                {
                    await SyncGuildPermissions(guild);
                }

                // Mark bot as ready to handle commands
                _isReady = true;
                Log.Logger.Information("Bot is now ready to handle commands");
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to complete ready sequence");
            _isReady = false;
        }
    }

    private async Task OnJoinedGuild(SocketGuild guild)
    {
        Log.Logger.Information("Joined new guild: {GuildName} ({GuildId})", guild.Name, guild.Id);
        await SyncGuildPermissions(guild);
    }

    private async Task OnGuildAvailable(SocketGuild guild)
    {
        Log.Logger.Information("Guild became available: {GuildName} ({GuildId})", guild.Name, guild.Id);
        await SyncGuildPermissions(guild);
    }

    private async Task OnUserUpdated(SocketUser before, SocketUser after)
    {
        foreach (var guild in _client.Guilds)
        {
            var guildUser = guild.GetUser(after.Id);
            if (guildUser != null)
            {
                await UpdateUserPermissions(guildUser);
            }
        }
    }

    private async Task OnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
    {
        await UpdateUserPermissions(after);
    }

    private async Task SyncGuildPermissions(SocketGuild guild)
    {
        try
        {
            var dbGuild = await _storage.GetGuildByDiscordIdAsync(guild.Id.ToString());
            if (dbGuild == null)
            {
                dbGuild = await _storage.CreateGuildAsync(new Guild
                {
                    DiscordID = guild.Id.ToString(),
                    Config = new GuildConfig
                    {
                        UpvoteEmoji = "👍",
                        DownvoteEmoji = "👎",
                        AllowVoting = true,
                        LockAllowedChannels = false,
                        Comments = true
                    }
                });
            }

            // Update permissions for all users with manage bot or admin permissions
            foreach (var user in guild.Users)
            {
                await UpdateUserPermissions(user);
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to sync permissions for guild {GuildId}", guild.Id);
        }
    }

    private async Task UpdateUserPermissions(SocketGuildUser user)
    {
        try
        {
            var guild = user.Guild;
            var dbGuild = await _storage.GetGuildByDiscordIdAsync(guild.Id.ToString());
            if (dbGuild?.Config == null) return;

            bool hasManageBot = user.GuildPermissions.ManageGuild || user.GuildPermissions.Administrator;
            
            // Check if permission already exists
            var existingPerm = dbGuild.Config.Permissions
                .FirstOrDefault(p => p.UserID == user.Id.ToString() && 
                                   p.PermissionType == PermissionType.SETTINGS);

            if (hasManageBot && existingPerm == null)
            {
                // Add permission
                dbGuild.Config.Permissions.Add(new Permission
                {
                    GuildConfigID = dbGuild.Config.ID,
                    UserID = user.Id.ToString(),
                    PermissionType = PermissionType.SETTINGS
                });
                await _storage.UpdateGuildAsync(dbGuild);
                Log.Logger.Information("Added SETTINGS permission for user {UserId} in guild {GuildId}", 
                    user.Id, guild.Id);
            }
            else if (!hasManageBot && existingPerm != null)
            {
                // Remove permission
                dbGuild.Config.Permissions.Remove(existingPerm);
                await _storage.UpdateGuildAsync(dbGuild);
                Log.Logger.Information("Removed SETTINGS permission for user {UserId} in guild {GuildId}", 
                    user.Id, guild.Id);
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to update permissions for user {UserId} in guild {GuildId}", 
                user.Id, user.Guild.Id);
        }
    }

    // API Helper Methods
    
    /// <summary>
    /// Gets all channels in a guild
    /// </summary>
    /// <param name="guildId">The Discord ID of the guild</param>
    /// <returns>A collection of channels, or null if the guild is not found</returns>
    public IEnumerable<IChannel>? GetGuildChannels(ulong guildId)
    {
        return _client.GetGuild(guildId)?.Channels;
    }

    /// <summary>
    /// Gets all text channels in a guild
    /// </summary>
    /// <param name="guildId">The Discord ID of the guild</param>
    /// <returns>A collection of text channels, or null if the guild is not found</returns>
    public IEnumerable<ITextChannel>? GetGuildTextChannels(ulong guildId)
    {
        return _client.GetGuild(guildId)?.TextChannels;
    }

    public async Task StopAsync()
    {
        await _client.StopAsync();
    }
}

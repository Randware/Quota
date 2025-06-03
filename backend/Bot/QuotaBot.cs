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

    public QuotaBot(IServiceProvider services)
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMembers,
            LogLevel = LogSeverity.Debug
        };

        _client = new DiscordSocketClient(config);
        _interactions = new InteractionService(_client);
        _services = services;
        _storage = _services.GetRequiredService<Storage>();

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
        _client.UserJoined += OnUserJoined;
        _client.UserLeft += OnUserLeft;

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

    private Task OnReady()
    {
        try
        {
            using (LogContext.PushProperty("SourceContext", "Discord.Bot"))
            {
                Log.Logger.Information("Bot is connected and ready!");
                _isReady = true;
                // Register commands globally
                _ = _interactions.RegisterCommandsGloballyAsync();
                Log.Logger.Information("Successfully registered global commands");
                // Initial sync of all guilds in background, each with its own scope
                foreach (var guild in _client.Guilds)
                {
                    _ = Task.Run(async () =>
                    {
                        using var scope = _services.CreateScope();
                        var storage = scope.ServiceProvider.GetRequiredService<Storage>();
                        await SyncGuildPermissions(guild, storage);
                    });
                }
                Log.Logger.Information("Bot is now ready to handle commands");
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to complete ready sequence");
            _isReady = false;
        }
        return Task.CompletedTask;
    }

    private async Task OnJoinedGuild(SocketGuild guild)
    {
        Log.Logger.Information("Joined new guild: {GuildName} ({GuildId})", guild.Name, guild.Id);
        await SyncGuildPermissions(guild, _storage);
    }

    private Task OnGuildAvailable(SocketGuild guild)
    {
        Log.Logger.Information("Guild became available: {GuildName} ({GuildId})", guild.Name, guild.Id);
        _ = Task.Run(async () =>
        {
            using var scope = _services.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<Storage>();
            await SyncGuildPermissions(guild, storage);
        });
        return Task.CompletedTask;
    }

    private async Task OnUserUpdated(SocketUser before, SocketUser after)
    {
        foreach (var guild in _client.Guilds)
        {
            var guildUser = guild.GetUser(after.Id);
            if (guildUser != null)
            {
                await UpdateUserPermissions(guildUser, _storage);
            }
        }
    }

    private async Task OnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
    {
        await UpdateUserPermissions(after, _storage);
    }

    private async Task OnUserJoined(SocketGuildUser user)
    {
        // Add user to permissions table if needed (e.g., default permissions or based on roles)
        await UpdateUserPermissions(user, _storage);
    }

    private async Task OnUserLeft(SocketGuild guild, SocketUser user)
    {
        // Remove user from permissions table
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuotaContext>();
        var dbGuildConfig = db.GuildConfigs.FirstOrDefault(gc => gc.Guild.DiscordID == guild.Id.ToString());
        if (dbGuildConfig == null) return;
        var permissions = db.Permissions.Where(p => p.GuildConfigID == dbGuildConfig.ID && p.UserID == user.Id.ToString());
        db.Permissions.RemoveRange(permissions);
        await db.SaveChangesAsync();
    }

    private async Task SyncGuildPermissions(SocketGuild guild, Storage storage)
    {
        try
        {
            await guild.DownloadUsersAsync(); 
            var dbGuild = await storage.GetGuildByDiscordIdAsync(guild.Id.ToString());
            if (dbGuild == null)
            {
                dbGuild = await storage.CreateGuildAsync(new Guild
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
                await UpdateUserPermissions(user, storage);
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to sync permissions for guild {GuildId}", guild.Id);
        }
    }

    private async Task UpdateUserPermissions(SocketGuildUser user, Storage storage)
    {
        try
        {
            var guild = user.Guild;
            var dbGuild = await storage.GetGuildByDiscordIdAsync(guild.Id.ToString());
            if (dbGuild?.Config == null) return;

            // Handle DASHBOARD and ADMIN permissions for users
            bool isAdmin = user.GuildPermissions.Administrator;
            bool hasDashboard = user.GuildPermissions.ManageGuild || isAdmin;

            // Remove old SETTINGS permission if present
            var oldSettingPerm = dbGuild.Config.Permissions.FirstOrDefault(p => p.UserID == user.Id.ToString() && p.PermissionType.ToString() == "SETTINGS");
            if (oldSettingPerm != null)
                dbGuild.Config.Permissions.Remove(oldSettingPerm);

            // DASHBOARD permission
            var dashboardPerm = dbGuild.Config.Permissions.FirstOrDefault(p => p.UserID == user.Id.ToString() && p.PermissionType == PermissionType.DASHBOARD);
            if (hasDashboard && dashboardPerm == null)
            {
                dbGuild.Config.Permissions.Add(new Permission
                {
                    GuildConfigID = dbGuild.Config.ID,
                    UserID = user.Id.ToString(),
                    PermissionType = PermissionType.DASHBOARD
                });
            }
            else if (!hasDashboard && dashboardPerm != null)
            {
                dbGuild.Config.Permissions.Remove(dashboardPerm);
            }

            // ADMIN permission
            var adminPerm = dbGuild.Config.Permissions.FirstOrDefault(p => p.UserID == user.Id.ToString() && p.PermissionType == PermissionType.ADMIN);
            if (isAdmin && adminPerm == null)
            {
                dbGuild.Config.Permissions.Add(new Permission
                {
                    GuildConfigID = dbGuild.Config.ID,
                    UserID = user.Id.ToString(),
                    PermissionType = PermissionType.ADMIN
                });
            }
            else if (!isAdmin && adminPerm != null)
            {
                dbGuild.Config.Permissions.Remove(adminPerm);
            }

            // Role-based permissions (example: MANAGE_QUOTES)
            foreach (var role in user.Roles)
            {
                // Example: assign MANAGE_QUOTES to a specific role name or ID
                if (role.Name == "Quote Manager")
                {
                    var rolePerm = dbGuild.Config.Permissions.FirstOrDefault(p => p.RoleID == role.Id.ToString() && p.PermissionType == PermissionType.MANAGE_QUOTES);
                    if (rolePerm == null)
                    {
                        dbGuild.Config.Permissions.Add(new Permission
                        {
                            GuildConfigID = dbGuild.Config.ID,
                            RoleID = role.Id.ToString(),
                            PermissionType = PermissionType.MANAGE_QUOTES
                        });
                    }
                }
            }

            await storage.UpdateGuildAsync(dbGuild);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to update permissions for user {UserId} in guild {GuildId}", user.Id, user.Guild.Id);
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




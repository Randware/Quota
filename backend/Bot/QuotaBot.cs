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
        _client.LeftGuild += OnLeftGuild;
        _client.GuildAvailable += OnGuildAvailable;
        _client.GuildUnavailable += OnGuildUnavailable;
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
        
        _ = Task.Run(async () =>
        {
            using var scope = _services.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<Storage>();
            await SyncGuildPermissions(guild, storage);
        });
    }

    private async Task OnLeftGuild(SocketGuild guild)
    {
        Log.Logger.Information("Left guild: {GuildName} ({GuildId})", guild.Name, guild.Id);
        
        // Only clean up user permissions but preserve guild configuration and quotes data
        _ = Task.Run(async () =>
        {
            using var scope = _services.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<Storage>();
            await CleanupGuildUserPermissions(guild.Id, storage);
        });
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

    private async Task OnGuildUnavailable(SocketGuild guild)
    {
        Log.Logger.Warning("Guild became unavailable: {GuildName} ({GuildId})", guild.Name, guild.Id);
        // Don't remove data here as the guild might come back online
    }

    private async Task OnUserUpdated(SocketUser before, SocketUser after)
    {
        // Update user in all mutual guilds
        foreach (var guild in _client.Guilds)
        {
            var guildUser = guild.GetUser(after.Id);
            if (guildUser != null)
            {
                using var scope = _services.CreateScope();
                var storage = scope.ServiceProvider.GetRequiredService<Storage>();
                await UpdateUserPermissions(guildUser, storage);
            }
        }
    }

    private async Task OnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
    {
        using var scope = _services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<Storage>();
        await UpdateUserPermissions(after, storage);
    }

    private async Task OnUserJoined(SocketGuildUser user)
    {
        Log.Logger.Information("User joined guild: {Username} ({UserId}) in {GuildName}", user.Username, user.Id, user.Guild.Name);
        
        using var scope = _services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<Storage>();
        await UpdateUserPermissions(user, storage);
    }

    private async Task OnUserLeft(SocketGuild guild, SocketUser user)
    {
        Log.Logger.Information("User left guild: {Username} ({UserId}) from {GuildName}", user.Username, user.Id, guild.Name);
        
        using var scope = _services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<Storage>();
        await RemoveUserPermissions(guild.Id, user.Id, storage);
    }

    private async Task SyncGuildPermissions(SocketGuild guild, Storage storage)
    {
        try
        {
            Log.Logger.Information("Syncing permissions for guild: {GuildName} ({GuildId})", guild.Name, guild.Id);
            
            // Download all users to ensure we have the latest data
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
                Log.Logger.Information("Created new guild config for {GuildName}", guild.Name);
            }

            // Remove permissions for users no longer in the guild
            await CleanupOrphanedPermissions(guild, dbGuild, storage);

            // Update permissions for all current users
            foreach (var user in guild.Users)
            {
                if (!user.IsBot) // Skip bots
                {
                    await UpdateUserPermissions(user, storage);
                }
            }

            Log.Logger.Information("Completed permission sync for guild: {GuildName}", guild.Name);
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

            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QuotaContext>();
            
            // Use a transaction to prevent race conditions
            using var transaction = await db.Database.BeginTransactionAsync();
            
            try
            {
                // Remove existing permissions first (with proper concurrency handling)
                var existingPermissions = db.Permissions
                    .Where(p => p.GuildConfigID == dbGuild.Config.ID && p.UserID == user.Id.ToString())
                    .ToList();

                if (existingPermissions.Any())
                {
                    db.Permissions.RemoveRange(existingPermissions);
                    await db.SaveChangesAsync();
                }

                // Determine permissions based on Discord permissions
                var permissionsToAdd = new List<PermissionType>();

                // Basic permissions - if user can read and send messages in general
                var generalChannel = guild.DefaultChannel ?? guild.TextChannels.FirstOrDefault();
                if (generalChannel != null)
                {
                    var perms = user.GetPermissions(generalChannel);
                    if (perms.ViewChannel && perms.SendMessages)
                    {
                        permissionsToAdd.Add(PermissionType.CREATE_QUOTES);
                    }
                    if (perms.ViewChannel)
                    {
                        permissionsToAdd.Add(PermissionType.READ_QUOTES);
                    }
                }

                // Management permissions
                if (user.GuildPermissions.ManageMessages)
                {
                    permissionsToAdd.Add(PermissionType.MANAGE_QUOTES);
                }

                // Dashboard permission - for users who can manage the guild or manage roles
                if (user.GuildPermissions.ManageGuild || user.GuildPermissions.ManageRoles)
                {
                    permissionsToAdd.Add(PermissionType.DASHBOARD);
                }

                // Admin permission
                if (user.GuildPermissions.Administrator)
                {
                    permissionsToAdd.Add(PermissionType.ADMIN);
                    // Admins should have all permissions
                    if (!permissionsToAdd.Contains(PermissionType.DASHBOARD))
                        permissionsToAdd.Add(PermissionType.DASHBOARD);
                    if (!permissionsToAdd.Contains(PermissionType.MANAGE_QUOTES))
                        permissionsToAdd.Add(PermissionType.MANAGE_QUOTES);
                    if (!permissionsToAdd.Contains(PermissionType.CREATE_QUOTES))
                        permissionsToAdd.Add(PermissionType.CREATE_QUOTES);
                    if (!permissionsToAdd.Contains(PermissionType.READ_QUOTES))
                        permissionsToAdd.Add(PermissionType.READ_QUOTES);
                }

                // Add new permissions (avoid duplicates by using Distinct)
                var uniquePermissions = permissionsToAdd.Distinct().ToList();
                
                foreach (var permissionType in uniquePermissions)
                {
                    // Double-check that this permission doesn't already exist
                    var existingPerm = db.Permissions.FirstOrDefault(p => 
                        p.GuildConfigID == dbGuild.Config.ID && 
                        p.UserID == user.Id.ToString() && 
                        p.PermissionType == permissionType);
                    
                    if (existingPerm == null)
                    {
                        db.Permissions.Add(new Permission
                        {
                            GuildConfigID = dbGuild.Config.ID,
                            UserID = user.Id.ToString(),
                            PermissionType = permissionType
                        });
                    }
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();
                
                Log.Logger.Debug("Updated permissions for user {Username} ({UserId}) in guild {GuildName}: {Permissions}", 
                    user.Username, user.Id, guild.Name, string.Join(", ", uniquePermissions));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to update permissions for user {UserId} in guild {GuildId}", user.Id, user.Guild.Id);
        }
    }

    private async Task RemoveUserPermissions(ulong guildId, ulong userId, Storage storage)
    {
        try
        {
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QuotaContext>();
            
            var dbGuildConfig = db.GuildConfigs.FirstOrDefault(gc => gc.Guild.DiscordID == guildId.ToString());
            if (dbGuildConfig == null) return;

            // Use a more robust approach to handle concurrency
            var permissions = db.Permissions
                .Where(p => p.GuildConfigID == dbGuildConfig.ID && p.UserID == userId.ToString())
                .ToList();

            if (permissions.Any())
            {
                // Remove each permission individually to handle concurrency better
                foreach (var permission in permissions)
                {
                    try
                    {
                        db.Permissions.Remove(permission);
                    }
                    catch (InvalidOperationException)
                    {
                        // Permission might have been removed already by another thread
                        // Reload the entity from database
                        var entry = db.Entry(permission);
                        if (entry.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                        {
                            entry.Reload();
                            if (entry.Entity != null)
                            {
                                db.Permissions.Remove(entry.Entity);
                            }
                        }
                    }
                }
                
                try
                {
                    await db.SaveChangesAsync();
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
                {
                    // Another thread already deleted these permissions, which is fine
                    Log.Logger.Debug("Permissions for user {UserId} in guild {GuildId} were already removed by another process", userId, guildId);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to remove permissions for user {UserId} in guild {GuildId}", userId, guildId);
        }
    }

private async Task CleanupOrphanedPermissions(SocketGuild guild, Guild dbGuild, Storage storage)
{
    try
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuotaContext>();
        
        // Get all permissions for this guild
        var guildPermissions = db.Permissions.Where(p => p.GuildConfigID == dbGuild.Config.ID).ToList();
        
        // Find permissions for users no longer in the guild
        var orphanedPermissions = new List<Permission>();
        
        foreach (var permission in guildPermissions)
        {
            if (!string.IsNullOrEmpty(permission.UserID))
            {
                if (ulong.TryParse(permission.UserID, out var userId))
                {
                    var user = guild.GetUser(userId);
                    if (user == null) // User no longer in guild
                    {
                        orphanedPermissions.Add(permission);
                    }
                }
            }
        }

        if (orphanedPermissions.Any())
        {
            try
            {
                db.Permissions.RemoveRange(orphanedPermissions);
                await db.SaveChangesAsync();
                Log.Logger.Information("Removed {Count} orphaned permissions from guild {GuildName}", 
                    orphanedPermissions.Count, guild.Name);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                Log.Logger.Debug("Some orphaned permissions were already removed by another process in guild {GuildName}", guild.Name);
            }
        }
    }
    catch (Exception ex)
    {
        Log.Logger.Error(ex, "Failed to cleanup orphaned permissions for guild {GuildId}", guild.Id);
    }
}

private async Task CleanupGuildUserPermissions(ulong guildId, Storage storage)
{
    try
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QuotaContext>();
        
        var dbGuildConfig = db.GuildConfigs.FirstOrDefault(gc => gc.Guild.DiscordID == guildId.ToString());
        if (dbGuildConfig == null) return;

        // Remove all user permissions for this guild
        var allPermissions = db.Permissions
            .Where(p => p.GuildConfigID == dbGuildConfig.ID)
            .ToList();

        if (allPermissions.Any())
        {
            try
            {
                db.Permissions.RemoveRange(allPermissions);
                await db.SaveChangesAsync();
                Log.Logger.Information("Cleaned up {Count} user permissions for guild {GuildId}", 
                    allPermissions.Count, guildId);
            }
            catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
            {
                Log.Logger.Debug("Some permissions were already removed by another process for guild {GuildId}", guildId);
            }
        }
    }
    catch (Exception ex)
    {
        Log.Logger.Error(ex, "Failed to cleanup user permissions for guild {GuildId}", guildId);
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

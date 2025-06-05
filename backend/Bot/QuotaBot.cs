using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Common;
using Database;
using Database.Model;
using Serilog.Context;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

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
            LogLevel = LogSeverity.Debug,
            MessageCacheSize = 1000 // Cache recent messages for button interactions
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
                try
                {
                    await interaction.RespondAsync("Bot is still starting up. Please try again in a moment.", ephemeral: true);
                }
                catch (Exception ex)
                {
                    Log.Logger.Warning(ex, "Failed to respond to interaction during startup");
                }
                return;
            }

            var ctx = new SocketInteractionContext(_client, interaction);
            try
            {
                await _interactions.ExecuteCommandAsync(ctx, _services);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to execute interaction command");
                try
                {
                    var response = interaction.HasResponded 
                        ? "Sorry, there was an error processing your request." 
                        : "Sorry, there was an error processing your request.";
                    
                    if (!interaction.HasResponded)
                    {
                        await interaction.RespondAsync(response, ephemeral: true);
                    }
                    else
                    {
                        await interaction.FollowupAsync(response, ephemeral: true);
                    }
                }
                catch (Exception followupEx)
                {
                    Log.Logger.Error(followupEx, "Failed to send error response for interaction");
                }
            }
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
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await _interactions.RegisterCommandsGloballyAsync();
                        Log.Logger.Information("Successfully registered global commands");
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, "Failed to register global commands");
                    }
                });
                
                // Initial sync of all guilds in background, each with its own scope
                foreach (var guild in _client.Guilds)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = _services.CreateScope();
                            var storage = scope.ServiceProvider.GetRequiredService<Storage>();
                            await SyncGuildPermissions(guild, storage);
                        }
                        catch (Exception ex)
                        {
                            Log.Logger.Error(ex, "Failed to sync permissions for guild {GuildId} during startup", guild.Id);
                        }
                    });
                }
                
                // Refresh recent quote buttons (optional feature)
                _ = Task.Run(RefreshRecentQuoteButtons);
                
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

    private async Task RefreshRecentQuoteButtons()
    {
        try
        {
            using (LogContext.PushProperty("SourceContext", "Discord.Bot"))
            {
                Log.Logger.Information("Starting refresh of recent quote buttons");
                
                using var scope = _services.CreateScope();
                var storage = scope.ServiceProvider.GetRequiredService<Storage>();
                
                // Get quotes from the last 24 hours that might still have active buttons
                var recentQuotes = await GetRecentQuotesWithMessageIds(storage, TimeSpan.FromHours(24));
                var refreshCount = 0;
                
                foreach (var quote in recentQuotes)
                {
                    try
                    {
                        if (ulong.TryParse(quote.GuildDiscordId, out var guildId) && 
                            ulong.TryParse(quote.MessageID, out var messageId))
                        {
                            var guild = _client.GetGuild(guildId);
                            if (guild != null)
                            {
                                // Try to find the message in all text channels
                                foreach (var channel in guild.TextChannels)
                                {
                                    try
                                    {
                                        var message = await channel.GetMessageAsync(messageId);
                                        if (message is IUserMessage userMessage && message.Author.Id == _client.CurrentUser.Id)
                                        {
                                            // This is our quote message, refresh the buttons
                                            await RefreshQuoteMessageButtons(userMessage, quote, storage);
                                            refreshCount++;
                                            break;
                                        }
                                    }
                                    catch (Exception)
                                    {
                                        // Message not in this channel, continue searching
                                        continue;
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Warning(ex, "Failed to refresh buttons for quote {QuoteId}", quote.ID);
                    }
                    
                    // Small delay to avoid rate limiting
                    await Task.Delay(100);
                }
                
                Log.Logger.Information("Refreshed buttons for {Count} recent quotes", refreshCount);
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to refresh recent quote buttons");
        }
    }

    private async Task<List<QuoteWithGuildInfo>> GetRecentQuotesWithMessageIds(Storage storage, TimeSpan timeSpan)
    {
        try
        {
            var db = _services.GetRequiredService<QuotaContext>();
            var cutoffTime = DateTime.UtcNow - timeSpan;
            var quotes = await db.Quotes
                .Where(q => q.CreatedAt >= cutoffTime && !string.IsNullOrEmpty(q.MessageID))
                .Select(q => new QuoteWithGuildInfo
                {
                    ID = q.ID,
                    MessageID = q.MessageID,
                    GuildDiscordId = q.Guild.DiscordID,
                    Upvotes = q.Upvotes,
                    Downvotes = q.Downvotes
                })
                .ToListAsync();
            return quotes;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to get recent quotes with message IDs");
            return new List<QuoteWithGuildInfo>();
        }
    }

    private async Task RefreshQuoteMessageButtons(IUserMessage message, QuoteWithGuildInfo quote, Storage storage)
    {
        try
        {
            if (ulong.TryParse(quote.GuildDiscordId, out var guildId))
            {
                var guild = await storage.GetGuildByDiscordIdAsync(quote.GuildDiscordId);
                if (guild?.Config != null)
                {
                    var components = new ComponentBuilder()
                        .WithButton(
                            label: $"Upvote ({quote.Upvotes})",
                            customId: $"quote:upvote:{quote.ID}",
                            style: ButtonStyle.Secondary,
                            emote: GetDiscordEmote(guild.Config.UpvoteEmojiConfig))
                        .WithButton(
                            label: $"Downvote ({quote.Downvotes})",
                            customId: $"quote:downvote:{quote.ID}",
                            style: ButtonStyle.Secondary,
                            emote: GetDiscordEmote(guild.Config.DownvoteEmojiConfig));

                    await message.ModifyAsync(msg =>
                    {
                        msg.Components = components.Build();
                    });
                }
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Warning(ex, "Failed to refresh buttons for message {MessageId}", message.Id);
        }
    }

    // Helper for Discord emote creation
    private static IEmote GetDiscordEmote(EmojiConfig emoji)
    {
        if (emoji == null) return new Emoji("❓");
        if (emoji.IsCustom && !string.IsNullOrEmpty(emoji.Id))
            return Emote.Parse($"<:{emoji.Name}:{emoji.Id}>");
        return new Emoji(emoji.Name);
    }

    private async Task OnJoinedGuild(SocketGuild guild)
    {
        Log.Logger.Information("Joined new guild: {GuildName} ({GuildId})", guild.Name, guild.Id);
        
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _services.CreateScope();
                var storage = scope.ServiceProvider.GetRequiredService<Storage>();
                await SyncGuildPermissions(guild, storage);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to sync permissions for newly joined guild {GuildId}", guild.Id);
            }
        });
    }

    private async Task OnLeftGuild(SocketGuild guild)
    {
        Log.Logger.Information("Left guild: {GuildName} ({GuildId})", guild.Name, guild.Id);
        
        // Only clean up user permissions but preserve guild configuration and quotes data
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _services.CreateScope();
                var storage = scope.ServiceProvider.GetRequiredService<Storage>();
                await CleanupGuildUserPermissions(guild.Id, storage);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to cleanup permissions for left guild {GuildId}", guild.Id);
            }
        });
    }

    private Task OnGuildAvailable(SocketGuild guild)
    {
        Log.Logger.Information("Guild became available: {GuildName} ({GuildId})", guild.Name, guild.Id);
        
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _services.CreateScope();
                var storage = scope.ServiceProvider.GetRequiredService<Storage>();
                await SyncGuildPermissions(guild, storage);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to sync permissions for available guild {GuildId}", guild.Id);
            }
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
        var tasks = new List<Task>();
        foreach (var guild in _client.Guilds)
        {
            var guildUser = guild.GetUser(after.Id);
            if (guildUser != null)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using var scope = _services.CreateScope();
                        var storage = scope.ServiceProvider.GetRequiredService<Storage>();
                        await UpdateUserPermissions(guildUser, storage);
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Error(ex, "Failed to update user permissions for {UserId} in guild {GuildId}", after.Id, guild.Id);
                    }
                }));
            }
        }
        await Task.WhenAll(tasks);
    }

    private async Task OnGuildMemberUpdated(Cacheable<SocketGuildUser, ulong> before, SocketGuildUser after)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _services.CreateScope();
                var storage = scope.ServiceProvider.GetRequiredService<Storage>();
                await UpdateUserPermissions(after, storage);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to update guild member permissions for {UserId} in guild {GuildId}", after.Id, after.Guild.Id);
            }
        });
    }

    private async Task OnUserJoined(SocketGuildUser user)
    {
        Log.Logger.Information("User joined guild: {Username} ({UserId}) in {GuildName}", user.Username, user.Id, user.Guild.Name);
        
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _services.CreateScope();
                var storage = scope.ServiceProvider.GetRequiredService<Storage>();
                await UpdateUserPermissions(user, storage);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to update permissions for joined user {UserId} in guild {GuildId}", user.Id, user.Guild.Id);
            }
        });
    }

    private async Task OnUserLeft(SocketGuild guild, SocketUser user)
    {
        Log.Logger.Information("User left guild: {Username} ({UserId}) from {GuildName}", user.Username, user.Id, guild.Name);
        
        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _services.CreateScope();
                var storage = scope.ServiceProvider.GetRequiredService<Storage>();
                await RemoveUserPermissions(guild.Id, user.Id, storage);
            }
            catch (Exception ex)
            {
                Log.Logger.Error(ex, "Failed to remove permissions for left user {UserId} in guild {GuildId}", user.Id, guild.Id);
            }
        });
    }

    private async Task SyncGuildPermissions(SocketGuild guild, Storage _)
    {
        try
        {
            Log.Logger.Information("Syncing permissions for guild: {GuildName} ({GuildId})", guild.Name, guild.Id);
            await guild.DownloadUsersAsync();

            // Always create a new scope and resolve Storage/DbContext for thread safety
            using var scope = _services.CreateScope();
            var storage = scope.ServiceProvider.GetRequiredService<Storage>();

            // Double-check existence before creating
            var dbGuild = await storage.GetGuildByDiscordIdAsync(guild.Id.ToString());
            if (dbGuild == null)
            {
                var db = scope.ServiceProvider.GetRequiredService<Database.Model.QuotaContext>();
                // Check for existing GuildConfig not linked to a Guild
                var existingConfig = await db.GuildConfigs
                    .Include(cfg => cfg.Guild)
                    .FirstOrDefaultAsync(cfg => cfg.Guild == null && db.Guilds.All(g => g.DiscordID != guild.Id.ToString()));
                if (existingConfig != null)
                {
                    dbGuild = new Guild
                    {
                        DiscordID = guild.Id.ToString(),
                        ConfigID = existingConfig.ID,
                        Config = existingConfig
                    };
                    db.Guilds.Add(dbGuild);
                    await db.SaveChangesAsync();
                }
                else
                {
                    dbGuild = await storage.CreateGuildAsync(new Guild
                    {
                        DiscordID = guild.Id.ToString(),
                        Config = new GuildConfig
                        {
                            UpvoteEmojiConfig = new EmojiConfig { Type = "unicode", Name = "👍" },
                            DownvoteEmojiConfig = new EmojiConfig { Type = "unicode", Name = "👎" },
                            AllowVoting = true,
                            LockAllowedChannels = false,
                            Comments = true
                        }
                    });
                }
            }

            // Remove permissions for users no longer in the guild
            await CleanupOrphanedPermissions(guild, dbGuild, storage);

            // Update permissions for all current users in batches to avoid overwhelming the database
            var users = guild.Users.Where(u => !u.IsBot).ToList();
            var batchSize = 10;
            for (int i = 0; i < users.Count; i += batchSize)
            {
                var batch = users.Skip(i).Take(batchSize);
                var tasks = batch.Select(user => {
                    using var userScope = _services.CreateScope();
                    var userStorage = userScope.ServiceProvider.GetRequiredService<Storage>();
                    return UpdateUserPermissions(user, userStorage);
                });
                await Task.WhenAll(tasks);
                if (i + batchSize < users.Count)
                {
                    await Task.Delay(100);
                }
            }
            Log.Logger.Information("Completed permission sync for guild: {GuildName} ({UserCount} users)", guild.Name, users.Count);
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
                var existingPermissions = await db.Permissions
                    .Where(p => p.GuildConfigID == dbGuild.Config.ID && p.UserID == user.Id.ToString())
                    .ToListAsync();

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
                    db.Permissions.Add(new Permission
                    {
                        GuildConfigID = dbGuild.Config.ID,
                        UserID = user.Id.ToString(),
                        PermissionType = permissionType
                    });
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

            var dbGuildConfig = await db.GuildConfigs
                .FirstOrDefaultAsync(gc => gc.Guild.DiscordID == guildId.ToString());
            if (dbGuildConfig == null) return;

            var permissions = await db.Permissions
                .Where(p => p.GuildConfigID == dbGuildConfig.ID && p.UserID == userId.ToString())
                .ToListAsync();

            if (permissions.Any())
            {
                db.Permissions.RemoveRange(permissions);
                await db.SaveChangesAsync();
            }

            Log.Logger.Debug("Removed {Count} permissions for user {UserId} in guild {GuildId}", permissions.Count, userId, guildId);
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
            if (dbGuild == null || dbGuild.Config == null)
            {
                Log.Logger.Warning("dbGuild or dbGuild.Config is null for guild {GuildId}, skipping orphaned permissions cleanup", guild.Id);
                return;
            }
            using var scope = _services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QuotaContext>();

            // Get all permissions for this guild
            var guildPermissions = await db.Permissions
                .Where(p => p.GuildConfigID == dbGuild.Config.ID)
                .ToListAsync();

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
                db.Permissions.RemoveRange(orphanedPermissions);
                await db.SaveChangesAsync();
                Log.Logger.Information("Removed {Count} orphaned permissions from guild {GuildName}",
                    orphanedPermissions.Count, guild.Name);
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

            var dbGuildConfig = await db.GuildConfigs
                .FirstOrDefaultAsync(gc => gc.Guild.DiscordID == guildId.ToString());
            if (dbGuildConfig == null) return;

            // Remove all user permissions for this guild
            var allPermissions = await db.Permissions
                .Where(p => p.GuildConfigID == dbGuildConfig.ID)
                .ToListAsync();

            if (allPermissions.Any())
            {
                db.Permissions.RemoveRange(allPermissions);
                try
                {
                    await db.SaveChangesAsync();
                    Log.Logger.Information("Cleaned up {Count} user permissions for guild {GuildId}",
                        allPermissions.Count, guildId);
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    Log.Logger.Warning(ex, "DbUpdateConcurrencyException: Some permissions for guild {GuildId} were already deleted (likely by cascade or concurrency)", guildId);
                    // Not a fatal error, just log and continue
                }
            }
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Failed to cleanup user permissions for guild {GuildId}", guildId);
        }
    }

    /// <summary>
    /// Gets all channels in a guild
    /// </summary>
    /// <param name="guildId">The Discord ID of the guild</param>
    /// <returns>A collection of channels, or an empty collection if the guild is not found</returns>
    public IEnumerable<IChannel> GetGuildChannels(ulong guildId)
    {
        return _client.GetGuild(guildId)?.Channels ?? Enumerable.Empty<IChannel>();
    }

    /// <summary>
    /// Gets all text channels in a guild
    /// </summary>
    /// <param name="guildId">The Discord ID of the guild</param>
    /// <returns>A collection of text channels, or an empty collection if the guild is not found</returns>
    public IEnumerable<ITextChannel> GetGuildTextChannels(ulong guildId)
    {
        return _client.GetGuild(guildId)?.TextChannels ?? Enumerable.Empty<ITextChannel>();
    }

    public async Task StopAsync()
    {
        _isReady = false;
        await _client.StopAsync();
    }
}

// Helper class for quote refresh functionality
public class QuoteWithGuildInfo
{
    public Guid ID { get; set; }
    public string MessageID { get; set; } = string.Empty;
    public string GuildDiscordId { get; set; } = string.Empty;
    public int Upvotes { get; set; }
    public int Downvotes { get; set; }
}

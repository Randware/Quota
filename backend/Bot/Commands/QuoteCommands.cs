using Discord;
using Discord.Interactions;
using Database;
using Database.Model;
using Common;
using Microsoft.EntityFrameworkCore;
using Serilog.Context;

namespace Bot;

public class QuoteCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly Storage _storage;
    private readonly QuotaContext _dbContext;

    public QuoteCommands(Storage storage, QuotaContext dbContext)
    {
        _storage = storage;
        _dbContext = dbContext;
    }

    [SlashCommand("quote", "Create a new quote")]
    public async Task CreateQuote(
        [Summary("quotee", "Who said this quote")] string quotee,
        [Summary("content", "The text content of the quote")] string? content = null,
        [Summary("image", "An image attachment for the quote")] IAttachment? image = null,
        [Summary("discord_user", "Tag the Discord user if they're in the server")] IUser? author = null)
    {
        // Quick validation first - keep it minimal to avoid timeout
        if (string.IsNullOrWhiteSpace(content) && image == null)
        {
            await QuotaBot.SendBeautifulErrorAsync(Context, "You must provide either text content or an image for the quote.");
            return;
        }

        // Get or create guild (needed for config check)
        var guild = await _storage.GetGuildByDiscordIdAsync(Context.Guild.Id.ToString());
        if (guild == null)
        {
            guild = await _storage.CreateGuildAsync(new Guild
            {
                DiscordID = Context.Guild.Id.ToString(),
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

        // Check if quotes are allowed in this channel BEFORE DeferAsync
        if (guild.Config?.LockAllowedChannels == true &&
            !guild.Config.AllowedChannels.Any(ac => ac.Channel == Context.Channel.Id.ToString()))
        {
            await QuotaBot.SendBeautifulErrorAsync(Context, "Quotes are not allowed in this channel.");
            return;
        }

        // Check basic permissions before starting
        if (!await CheckChannelPermissions())
        {
            return; // Error message already sent
        }

        // Defer immediately to prevent interaction timeout
        try
        {
            await DeferAsync();
        }
        catch (Discord.Net.HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownInteraction)
        {
            using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
            {
                Log.Logger.Warning("Interaction expired before defer in guild {GuildId}", Context.Guild.Id);
            }
            return;
        }

        try
        {
            using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
            {
                // Check if quotes are allowed in this channel
                if (guild.Config?.LockAllowedChannels == true &&
                    !guild.Config.AllowedChannels.Any(ac => ac.Channel == Context.Channel.Id.ToString()))
                {
                    await QuotaBot.SendBeautifulErrorAsync(Context, "Quotes are not allowed in this channel.");
                    return;
                }

                // Get or create the submitting user
                var submitter = await _storage.GetUserByDiscordIdAsync(Context.User.Id.ToString());
                if (submitter == null)
                {
                    submitter = await _storage.CreateUserAsync(Context.User.Id.ToString());
                }

                // Get or create the quoted user and quotee profile
                User? quotedUser = null;
                if (author != null)
                {
                    quotedUser = await _storage.GetUserByDiscordIdAsync(author.Id.ToString());
                    if (quotedUser == null)
                    {
                        quotedUser = await _storage.CreateUserAsync(author.Id.ToString());
                    }
                }

                // Create or get the quotee profile
                var quoteeProfile = quotedUser?.QuoteeProfiles?.FirstOrDefault();
                if (quoteeProfile == null)
                {
                    quoteeProfile = new Quotee
                    {
                        ID = Guid.NewGuid(),
                        Name = quotee,
                        UserID = quotedUser?.ID
                    };

                    if (quotedUser != null)
                    {
                        if (quotedUser.QuoteeProfiles == null)
                            quotedUser.QuoteeProfiles = new List<Quotee>();
                        quotedUser.QuoteeProfiles.Add(quoteeProfile);
                        var updatedUser = await _storage.UpdateUserAsync(quotedUser);
                        if (updatedUser == null || updatedUser.QuoteeProfiles == null || !updatedUser.QuoteeProfiles.Any())
                        {
                            await QuotaBot.SendBeautifulErrorAsync(Context, "Error creating quote: Could not update user profile.");
                            return;
                        }
                        quoteeProfile = updatedUser.QuoteeProfiles.First();
                    }
                }

                // Store the quote with media support
                string mediaUrl = null;
                if (image != null && (image.ContentType?.StartsWith("image/") == true || image.ContentType?.StartsWith("video/") == true))
                {
                    mediaUrl = image.Url;
                }

                var quote = new Quote
                {
                    ID = Guid.NewGuid(),
                    Content = string.IsNullOrWhiteSpace(content) ? null : content,
                    MediaUrls = mediaUrl,
                    GuildID = guild.ID,
                    SubmittedByID = submitter.ID,
                    CreatedAt = DateTime.UtcNow,
                    Upvotes = 0,
                    Downvotes = 0
                    // Do NOT set MessageID yet
                };

                // Create and assign QuoteQuotee relationship
                var quoteQuotee = new QuoteQuotee
                {
                    QuoteID = quote.ID,
                    Quote = quote,
                    QuoteeID = quoteeProfile.ID,
                    Quotee = quoteeProfile
                };
                quote.QuoteQuotees = new List<QuoteQuotee> { quoteQuotee };

                // Build the response embed (before saving to DB)
                bool isImage = false, isVideo = false;
                if (image != null)
                {
                    if (image.ContentType?.StartsWith("image/") == true)
                    {
                        isImage = true;
                    }
                    else if (image.ContentType?.StartsWith("video/") == true)
                    {
                        isVideo = true;
                    }
                }

                var displayContent = content;
                bool hasContent = !string.IsNullOrWhiteSpace(content);
                bool hasMedia = !string.IsNullOrWhiteSpace(mediaUrl);
                if (!hasContent && hasMedia)
                {
                    displayContent = isImage ? "" : mediaUrl;
                }
                else if (hasContent && !hasMedia)
                {
                    if (!displayContent.StartsWith("\"") || !displayContent.EndsWith("\""))
                    {
                        displayContent = $"\"{displayContent}\"";
                    }
                    displayContent = $"*{displayContent}*";
                }
                else if (hasContent && hasMedia)
                {
                    displayContent = isImage ? content : $"{content}\n{mediaUrl}";
                }

                var embed = new EmbedBuilder()
                    .WithDescription(displayContent)
                    .WithFooter($"Submitted by {Context.User.Username}", Context.User.GetAvatarUrl())
                    .WithTimestamp(quote.CreatedAt ?? DateTimeOffset.UtcNow)
                    .WithColor(Color.Blue);

                if (isImage)
                {
                    embed.WithImageUrl(mediaUrl);
                }

                if (author != null)
                {
                    embed.WithAuthor($"Quote from {quotee}", author.GetAvatarUrl());
                }
                else
                {
                    embed.WithAuthor($"Quote from {quotee}");
                }

                var components = new ComponentBuilder();
                if (guild.Config?.AllowVoting == true)
                {
                    components
                        .WithButton(
                            label: $"Upvote (0)",
                            customId: $"quote:upvote:{quote.ID}",
                            style: ButtonStyle.Secondary,
                            emote: GetDiscordEmote(guild.Config.UpvoteEmojiConfig))
                        .WithButton(
                            label: $"Downvote (0)",
                            customId: $"quote:downvote:{quote.ID}",
                            style: ButtonStyle.Secondary,
                            emote: GetDiscordEmote(guild.Config.DownvoteEmojiConfig));
                }

                // Send the message and get the message ID
                var sentMessage = await FollowupAsync(
                    embed: embed.Build(),
                    components: components.Build()
                );

                // Now set the MessageID and ChannelID, then save the quote
                quote.MessageID = sentMessage.Id.ToString();
                quote.ChannelID = Context.Channel.Id.ToString();
                quote = await _storage.CreateQuoteAsync(quote);

                // If it's a video, send the link as a plain message so Discord previews it
                if (isVideo && !string.IsNullOrWhiteSpace(mediaUrl))
                {
                    await FollowupAsync(mediaUrl, ephemeral: false);
                }

                // Create a thread for comments if enabled in config
                if (guild.Config?.Comments == true)
                {
                    try
                    {
                        // Fetch the message as an IMessage (required by CreateThreadAsync)
                        var channel = Context.Channel as ITextChannel;
                        if (channel != null)
                        {
                            var quoteMsg = await channel.GetMessageAsync(sentMessage.Id);
                            if (quoteMsg == null)
                            {
                                await QuotaBot.SendBeautifulErrorAsync(Context, "Could not fetch the quote message to create a thread.");
                            }
                            else
                            {
                                var thread = await channel.CreateThreadAsync(
                                    name: $"Discussion",
                                    autoArchiveDuration: ThreadArchiveDuration.OneHour,
                                    type: ThreadType.PublicThread,
                                    message: quoteMsg
                                );
                                // Attempt to restrict thread access to users with READ_QUOTES or CREATE_QUOTES permissions
                                // Discord does not support per-user thread permissions, but we can restrict by role if needed
                                // For now, just send a message in the thread explaining who should use it
                                var allowedUsers = await _dbContext.Permissions
                                    .Where(p => p.GuildConfigID == guild.Config.ID &&
                                        (p.PermissionType == PermissionType.READ_QUOTES || p.PermissionType == PermissionType.CREATE_QUOTES))
                                    .Select(p => p.UserID)
                                    .ToListAsync();
                                await thread.SendMessageAsync(
                                    $"Discussion thread for quote by {quotee}.\n");
                            }
                        }
                        else
                        {
                            await QuotaBot.SendBeautifulErrorAsync(Context, "Could not create a thread: Channel is not a text channel.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Warning(ex, "Failed to create or configure thread for quote {QuoteId}", quote.ID);
                        await QuotaBot.SendBeautifulErrorAsync(Context, "Failed to create a discussion thread for this quote.");
                    }
                }

                Log.Logger.Information("Created quote {QuoteId} in guild {GuildId}", quote.ID, Context.Guild.Id);
            }
        }
        catch (Discord.Net.HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownInteraction)
        {
            using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
            {
                Log.Logger.Warning("Interaction expired during quote creation in guild {GuildId}", Context.Guild.Id);
            }
        }
        catch (Exception ex)
        {
            await HandleGenericError(ex, "creating your quote", Context.Guild.Id);
            throw; // Rethrow for unexpected errors
        }
    }

    [ComponentInteraction("quote:upvote:*")]
    public async Task HandleUpvote(string quoteId)
    {
        await HandleVote(Guid.Parse(quoteId), true);
    }

    [ComponentInteraction("quote:downvote:*")]
    public async Task HandleDownvote(string quoteId)
    {
        await HandleVote(Guid.Parse(quoteId), false);
    }

    private async Task HandleVote(Guid quoteId, bool isUpvote)
    {
        try
        {
            using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
            {
                var quote = await _storage.GetQuoteAsync(quoteId);
                if (quote == null)
                {
                    await RespondAsync("This quote no longer exists.", ephemeral: true);
                    return;
                }

                // Get the voting user
                var user = await _storage.GetUserByDiscordIdAsync(Context.User.Id.ToString());
                if (user == null)
                {
                    await RespondAsync("User not found.", ephemeral: true);
                    return;
                }

                // Get the user's previous vote if any
                var prevVote = _dbContext.QuoteVotes.FirstOrDefault(qv => qv.QuoteID == quoteId && qv.UserID == user.ID);

                bool changed = false;
                bool userUpvoted = false;
                if (prevVote == null)
                {
                    // New vote
                    _dbContext.QuoteVotes.Add(new QuoteVote
                    {
                        QuoteID = quoteId,
                        UserID = user.ID,
                        IsUpvote = isUpvote
                    });
                    if (isUpvote) quote.Upvotes++;
                    else quote.Downvotes++;
                    changed = true;
                    userUpvoted = isUpvote;
                }
                else if (prevVote.IsUpvote != isUpvote)
                {
                    // Switch vote
                    if (isUpvote)
                    {
                        quote.Upvotes++;
                        quote.Downvotes--;
                    }
                    else
                    {
                        quote.Downvotes++;
                        quote.Upvotes--;
                    }
                    prevVote.IsUpvote = isUpvote;
                    changed = true;
                    userUpvoted = isUpvote;
                }
                else
                {
                    userUpvoted = prevVote.IsUpvote;
                }

                if (changed)
                {
                    _dbContext.Quotes.Update(quote);
                    await _dbContext.SaveChangesAsync();
                }

                // Get the guild config for emojis
                var guild = await _storage.GetGuildByDiscordIdAsync(Context.Guild.Id.ToString());
                if (guild?.Config == null)
                {
                    await QuotaBot.SendBeautifulErrorAsync(Context, "Error: Guild configuration not found.");
                    return;
                }

                // Build updated components
                var components = new ComponentBuilder()
                    .WithButton(
                        label: $"Upvote ({quote.Upvotes})",
                        customId: $"quote:upvote:{quote.ID}",
                        style: userUpvoted ? ButtonStyle.Success : ButtonStyle.Secondary,
                        emote: GetDiscordEmote(guild.Config.UpvoteEmojiConfig))
                    .WithButton(
                        label: $"Downvote ({quote.Downvotes})",
                        customId: $"quote:downvote:{quote.ID}",
                        style: !userUpvoted ? ButtonStyle.Danger : ButtonStyle.Secondary,
                        emote: GetDiscordEmote(guild.Config.DownvoteEmojiConfig));

                // Try to update the message
                await TryUpdateMessage(components, quote, changed);
            }
        }
        catch (Discord.Net.HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownInteraction)
        {
            using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
            {
                Log.Logger.Warning("Interaction expired during vote handling for quote {QuoteId} in guild {GuildId}", quoteId, Context.Guild.Id);
            }
        }
        catch (Exception ex)
        {
            await HandleGenericError(ex, "processing your vote", Context.Guild.Id, quoteId);
        }
    }

    private async Task TryUpdateMessage(ComponentBuilder components, Quote quote, bool voteChanged)
    {
        try
        {
            // Get the message from the component interaction
            var message = ((IComponentInteraction)Context.Interaction).Message;
            
            if (message != null)
            {
                // Check if we have permission to modify the message
                var botUser = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
                var channelPerms = botUser.GetPermissions(Context.Channel as IGuildChannel);

                if (!channelPerms.ManageMessages)
                {
                    if (!Context.Interaction.HasResponded)
                        await QuotaBot.SendBeautifulErrorAsync(Context,
                            $"✅ Vote recorded! This quote now has **{quote.Upvotes}** upvotes and **{quote.Downvotes}** downvotes.\n" +
                            "⚠️ *I need 'Manage Messages' permission in this channel to update button displays.*");
                    
                    using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
                    {
                        Log.Logger.Warning("Missing ManageMessages permission in channel {ChannelId} ({ChannelName}) in guild {GuildId}", 
                            Context.Channel.Id, (Context.Channel as IGuildChannel)?.Name, Context.Guild.Id);
                    }
                    return;
                }

                await message.ModifyAsync(msg =>
                {
                    msg.Components = components.Build();
                });

                if (!Context.Interaction.HasResponded)
                    await DeferAsync();
            }
            else
            {
                if (!Context.Interaction.HasResponded)
                    await QuotaBot.SendBeautifulErrorAsync(Context,
                        $"✅ Vote recorded! This quote now has **{quote.Upvotes}** upvotes and **{quote.Downvotes}** downvotes.");
            }
        }
        catch (Discord.Net.HttpException ex) when (ex.DiscordCode == DiscordErrorCode.MissingPermissions)
        {
            if (!Context.Interaction.HasResponded)
                await QuotaBot.SendBeautifulErrorAsync(Context,
                    $"✅ Vote recorded! This quote now has **{quote.Upvotes}** upvotes and **{quote.Downvotes}** downvotes.\n" +
                    "⚠️ *I'm missing permissions to update the button display in this channel.*");

            using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
            {
                Log.Logger.Warning("Missing permissions to modify message in channel {ChannelId} ({ChannelName}) in guild {GuildId}: {Error}", 
                    Context.Channel.Id, (Context.Channel as IGuildChannel)?.Name, Context.Guild.Id, ex.Message);
            }
        }
        catch (Discord.Net.HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownMessage)
        {
            if (!Context.Interaction.HasResponded)
                await QuotaBot.SendBeautifulErrorAsync(Context,
                    $"✅ Vote recorded! This quote now has **{quote.Upvotes}** upvotes and **{quote.Downvotes}** downvotes.\n" +
                    "ℹ️ *The original quote message no longer exists.*");

            using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
            {
                Log.Logger.Information("Original quote message deleted in channel {ChannelId} in guild {GuildId}", 
                    Context.Channel.Id, Context.Guild.Id);
            }
        }
        catch (Discord.Net.HttpException ex) when (ex.DiscordCode == DiscordErrorCode.UnknownInteraction)
        {
            // Interaction expired - log but don't try to respond
            using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
            {
                Log.Logger.Warning("Interaction expired while updating message in guild {GuildId}", Context.Guild.Id);
            }
        }
        catch (Discord.Net.HttpException ex)
        {
            if (!Context.Interaction.HasResponded)
                await QuotaBot.SendBeautifulErrorAsync(Context,
                    $"✅ Vote recorded! This quote now has **{quote.Upvotes}** upvotes and **{quote.Downvotes}** downvotes.\n" +
                    $"⚠️ *Unable to update display: {ex.Reason ?? "Unknown Discord API error"}*");

            using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
            {
                Log.Logger.Warning("Discord API error updating message in guild {GuildId}: {Error} (Code: {Code})", 
                    Context.Guild.Id, ex.Message, ex.DiscordCode);
            }
        }
        catch (Exception ex)
        {
            // Log and rethrow unexpected exceptions
            using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
            {
                Log.Logger.Error(ex, "Unexpected error updating message in guild {GuildId}", Context.Guild.Id);
            }
            throw;
        }
    }

    private async Task<bool> CheckChannelPermissions()
    {
        try
        {
            var botUser = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            var channelPerms = botUser.GetPermissions(Context.Channel as IGuildChannel);

            var missingPerms = new List<string>();

            if (!channelPerms.ViewChannel)
                missingPerms.Add("View Channel");
            if (!channelPerms.SendMessages)
                missingPerms.Add("Send Messages");
            if (!channelPerms.EmbedLinks)
                missingPerms.Add("Embed Links");

            if (missingPerms.Any())
            {
                var permissionList = string.Join(", ", missingPerms);
                await RespondAsync(
                    $"❌ I'm missing required permissions in this channel: **{permissionList}**\n" +
                    "Please ask a server administrator to grant these permissions.", 
                    ephemeral: true
                );

                using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
                {
                    Log.Logger.Warning("Missing required permissions in channel {ChannelId} ({ChannelName}) in guild {GuildId}: {MissingPerms}", 
                        Context.Channel.Id, (Context.Channel as IGuildChannel)?.Name, Context.Guild.Id, permissionList);
                }
                return false;
            }

            // Log useful permission info for debugging
            using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
            {
                Log.Logger.Debug("Channel permissions in {ChannelId} ({ChannelName}): ManageMessages={ManageMessages}, AddReactions={AddReactions}, UseExternalEmojis={UseExternalEmojis}", 
                    Context.Channel.Id, (Context.Channel as IGuildChannel)?.Name, channelPerms.ManageMessages, channelPerms.AddReactions, channelPerms.UseExternalEmojis);
            }

            return true;
        }
        catch (Exception ex)
        {
            using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
            {
                Log.Logger.Error(ex, "Error checking permissions in channel {ChannelId} in guild {GuildId}", 
                    Context.Channel.Id, Context.Guild.Id);
            }

            await QuotaBot.SendBeautifulErrorAsync(Context, "❌ Error checking permissions. Please try again.");
            return false;
        }
    }

    private async Task HandleGenericError(Exception ex, string action, ulong guildId, Guid? quoteId = null)
    {
        try
        {
            await QuotaBot.SendBeautifulErrorAsync(Context, $"Sorry, there was an error {action}.");
        }
        catch (Discord.Net.HttpException httpEx) when (httpEx.DiscordCode == DiscordErrorCode.UnknownInteraction)
        {
            // Can't send followup, interaction expired
        }
        catch
        {
            // Swallow to avoid double error
        }

        using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
        {
            if (quoteId.HasValue)
            {
                Log.Logger.Error(ex, "Error {Action} for quote {QuoteId} in guild {GuildId}", action, quoteId.Value, guildId);
            }
            else
            {
                Log.Logger.Error(ex, "Error {Action} in guild {GuildId}", action, guildId);
            }
        }
    }

    public IEnumerable<IChannel> GetGuildChannels(ulong guildId)
    {
        return Context.Client.GetGuild(guildId)?.Channels ?? Enumerable.Empty<IChannel>();
    }

    public IEnumerable<ITextChannel> GetGuildTextChannels(ulong guildId)
    {
        return Context.Client.GetGuild(guildId)?.TextChannels ?? Enumerable.Empty<ITextChannel>();
    }

    // Helper for Discord emote creation
    private IEmote GetDiscordEmote(EmojiConfig emoji)
    {
        if (emoji == null) return new Emoji("❓");
        if (emoji.IsCustom && !string.IsNullOrEmpty(emoji.Id))
        {
            // Try to find the custom emote in the current guild
            var guild = Context.Guild;
            var emote = guild.Emotes.FirstOrDefault(e => e.Id.ToString() == emoji.Id);
            if (emote != null)
                return emote;
            // If not found, fallback to unicode
            return new Emoji(emoji.Name ?? "❓");
        }
        return new Emoji(emoji.Name);
    }
}

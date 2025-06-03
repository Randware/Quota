using Discord;
using Discord.Interactions;
using Database;
using Database.Model;
using Common;
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
        try
        {
            // Validate that at least content or image is provided
            if (string.IsNullOrWhiteSpace(content) && image == null)
            {
                await RespondAsync("You must provide either text content or an image for the quote.", ephemeral: true);
                return;
            }

            using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
            {
                // Defer the response since we'll be doing database operations
                await DeferAsync();

                // Get or create guild
                var guild = await _storage.GetGuildByDiscordIdAsync(Context.Guild.Id.ToString());
                if (guild == null)
                {
                    guild = await _storage.CreateGuildAsync(new Guild
                    {
                        DiscordID = Context.Guild.Id.ToString(),
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

                // Check if quotes are allowed in this channel
                if (guild.Config?.LockAllowedChannels == true &&
                    !guild.Config.AllowedChannels.Any(ac => ac.Channel == Context.Channel.Id.ToString()))
                {
                    await FollowupAsync("Quotes are not allowed in this channel.", ephemeral: true);
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
                var quoteeProfile = quotedUser?.QuoteeProfiles.FirstOrDefault();
                if (quoteeProfile == null)
                {
                    // Create new quotee profile
                    quoteeProfile = new Quotee
                    {
                        ID = Guid.NewGuid(),
                        Name = quotee, // Use the provided quotee name
                        UserID = quotedUser?.ID // This will be null for external quotees
                    };

                    if (quotedUser != null)
                    {
                        quotedUser.QuoteeProfiles = new List<Quotee> { quoteeProfile };
                        var updatedUser = await _storage.UpdateUserAsync(quotedUser);
                        if (updatedUser == null)
                        {
                            await FollowupAsync("Error creating quote: Could not update user profile.", ephemeral: true);
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
                    MessageID = Context.Interaction.Id.ToString(),
                    Content = string.IsNullOrWhiteSpace(content) ? null : content,
                    MediaUrls = mediaUrl,
                    GuildID = guild.ID,
                    SubmittedByID = submitter.ID,
                    CreatedAt = DateTime.UtcNow,
                    Upvotes = 0,
                    Downvotes = 0
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

                // Save the quote
                quote = await _storage.CreateQuoteAsync(quote);

                // Build the response embed
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
                    // Only media: don't show the link if it's an image, just let Discord embed it
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
                    // Both: show text, and only show the link if it's a video
                    displayContent = isImage ? content : $"{content}\n{mediaUrl}";
                }

                var embed = new EmbedBuilder()
                    .WithDescription(displayContent)
                    .WithFooter($"Submitted by {Context.User.Username}")
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
                            emote: new Emoji(guild.Config.UpvoteEmoji))
                        .WithButton(
                            label: $"Downvote (0)",
                            customId: $"quote:downvote:{quote.ID}",
                            style: ButtonStyle.Secondary,
                            emote: new Emoji(guild.Config.DownvoteEmoji));
                }

                var message = await FollowupAsync(
                    embed: embed.Build(),
                    components: components.Build()
                );

                // If it's a video, send the link as a plain message so Discord previews it
                if (isVideo && !string.IsNullOrWhiteSpace(mediaUrl))
                {
                    await FollowupAsync(mediaUrl, ephemeral: false);
                }

                Log.Logger.Information("Created quote {QuoteId} in guild {GuildId}", quote.ID, Context.Guild.Id);
            }
        }
        catch (Exception ex)
        {
            using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
            {
                Log.Logger.Error(ex, "Error creating quote in guild {GuildId}", Context.Guild.Id);
                await FollowupAsync("Sorry, there was an error creating your quote.", ephemeral: true);
            }
        }
    }

    // Add upvote/downvote button handlers
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
                // else: same vote, do nothing

                if (changed)
                {
                    _dbContext.Quotes.Update(quote);
                    await _dbContext.SaveChangesAsync();
                }

                // Get the guild config for emojis
                var guild = await _storage.GetGuildByDiscordIdAsync(Context.Guild.Id.ToString());
                if (guild?.Config == null)
                {
                    await RespondAsync("Error updating vote display.", ephemeral: true);
                    return;
                }

                // Update the message with new vote counts and selected state
                var components = new ComponentBuilder()
                    .WithButton(
                        label: $"Upvote ({quote.Upvotes})",
                        customId: $"quote:upvote:{quote.ID}",
                        style: userUpvoted ? ButtonStyle.Success : ButtonStyle.Secondary,
                        emote: new Emoji(guild.Config.UpvoteEmoji))
                    .WithButton(
                        label: $"Downvote ({quote.Downvotes})",
                        customId: $"quote:downvote:{quote.ID}",
                        style: (!userUpvoted && prevVote != null && prevVote.IsUpvote == false) ? ButtonStyle.Danger : ButtonStyle.Secondary,
                        emote: new Emoji(guild.Config.DownvoteEmoji));

                // Get the message from the component interaction
                var message = ((IComponentInteraction)Context.Interaction).Message;
                await message.ModifyAsync(msg =>
                {
                    msg.Components = components.Build();
                });

                // Do not send a message/response
                await DeferAsync();
            }
        }
        catch (Exception ex)
        {
            using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
            {
                Log.Logger.Error(ex, "Error handling vote for quote {QuoteId}", quoteId);
                // Do not send a message
            }
        }
    }
}

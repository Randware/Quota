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

    public QuoteCommands(Storage storage)
    {
        _storage = storage;
    }

    [SlashCommand("quote", "Create a new quote")]
    public async Task CreateQuote(
        [Summary("content", "The text content of the quote")] string? content = null,
        [Summary("image", "An image attachment for the quote")] IAttachment? image = null,
        [Summary("author", "The author of the quote (defaults to you)")] IUser? author = null)
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
                var submitter = await _storage.GetUserByDiscordIdAsync(Context.User.Id.ToString()) 
                    ?? await _storage.CreateUserAsync(Context.User.Id.ToString());

                // Get or create the quoted user (author)
                author ??= Context.User;
                var quotedUser = await _storage.GetUserByDiscordIdAsync(author.Id.ToString());
                if (quotedUser == null)
                {
                    quotedUser = await _storage.CreateUserAsync(author.Id.ToString());
                }

                // Create or get the quotee profile
                var quotee = quotedUser.QuoteeProfiles.FirstOrDefault();
                if (quotee == null)
                {
                    // Create new Quotee directly in the context
                    quotee = new Quotee
                    {
                        ID = Guid.NewGuid(),
                        Name = author.Username,
                        UserID = quotedUser.ID,
                        User = quotedUser  // Set the navigation property
                    };
                    quotedUser.QuoteeProfiles.Add(quotee);

                    // Update user with new quotee profile
                    quotedUser = await _storage.UpdateUserAsync(quotedUser);
                    if (quotedUser == null)
                    {
                        await FollowupAsync("Error creating quote: Could not update user profile.", ephemeral: true);
                        return;
                    }
                }

                // Create the quote with proper content handling
                var quote = new Quote
                {
                    MessageID = Context.Interaction.Id.ToString(),
                    Content = string.IsNullOrWhiteSpace(content) 
                        ? "(Image quote)" 
                        : content + (image != null ? $"\n[Image: {image.Url}]" : ""),
                    GuildID = guild.ID,
                    SubmittedByID = submitter.ID,
                    CreatedAt = DateTime.UtcNow,
                    Upvotes = 0,
                    Downvotes = 0,
                    QuoteQuotees = new List<QuoteQuotee>
                    {
                        new QuoteQuotee
                        {
                            QuoteeID = quotee.ID
                        }
                    }
                };

                await _storage.CreateQuoteAsync(quote);

                // Build the response embed
                var embed = new EmbedBuilder()
                    .WithAuthor(author)
                    .WithDescription(content)
                    .WithFooter($"Quote ID: {quote.ID}")
                    .WithTimestamp(quote.CreatedAt ?? DateTimeOffset.UtcNow)
                    .WithColor(Color.Blue);

                if (image != null)
                {
                    embed.WithImageUrl(image.Url);
                }

                var components = new ComponentBuilder();
                if (guild.Config?.AllowVoting == true)
                {
                    components
                        .WithButton(
                            label: "Upvote",
                            customId: $"quote:upvote:{quote.ID}",
                            style: ButtonStyle.Secondary,
                            emote: new Emoji(guild.Config.UpvoteEmoji))
                        .WithButton(
                            label: "Downvote",
                            customId: $"quote:downvote:{quote.ID}",
                            style: ButtonStyle.Secondary,
                            emote: new Emoji(guild.Config.DownvoteEmoji));
                }

                await FollowupAsync(
                    embed: embed.Build(),
                    components: components.Build()
                );

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

                if (isUpvote)
                    quote.Upvotes++;
                else
                    quote.Downvotes++;

                await _storage.UpdateQuoteAsync(quote);
                await RespondAsync($"Your {(isUpvote ? "upvote" : "downvote")} has been recorded.", ephemeral: true);
            }
        }
        catch (Exception ex)
        {
            using (LogContext.PushProperty("SourceContext", "Discord.Commands"))
            {
                Log.Logger.Error(ex, "Error handling vote for quote {QuoteId}", quoteId);
                await RespondAsync("Sorry, there was an error recording your vote.", ephemeral: true);
            }
        }
    }
}

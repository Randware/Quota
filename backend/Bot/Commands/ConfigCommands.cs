using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Database;
using Database.Model;
using Common;
using Microsoft.EntityFrameworkCore;

namespace Bot;

[Group("settings", "Configure bot settings and data")]
[RequireUserPermission(GuildPermission.Administrator)]
public class ConfigCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly Storage _storage;
    private readonly QuotaContext _db;

    private static readonly Color EmbedColor = new(88, 101, 242); // Discord blurple

    public ConfigCommands(Storage storage, QuotaContext dbContext)
    {
        _storage = storage;
        _db = dbContext;
    }

    [SlashCommand("clear_quotes", "Deletes all quotes from the database for this server.")]
    public async Task ClearQuotesAsync()
    {
        await DeferAsync(ephemeral: true);
        var dbGuild = await _db.Guilds.FirstOrDefaultAsync(g => g.DiscordID == Context.Guild.Id.ToString());
        if (dbGuild == null) 
        { 
            await FollowupAsync("Guild not found in database."); 
            return; 
        }

        var quotes = _db.Quotes.Where(q => q.GuildID == dbGuild.ID).ToList();
        var quoteIds = quotes.Select(q => q.ID).ToList();
        
        var quotees = _db.Set<QuoteQuotee>().Where(qq => quoteIds.Contains(qq.QuoteID));
        var votes = _db.Set<QuoteVote>().Where(qv => quoteIds.Contains(qv.QuoteID));
        
        _db.Set<QuoteQuotee>().RemoveRange(quotees);
        _db.Set<QuoteVote>().RemoveRange(votes);
        _db.Quotes.RemoveRange(quotes);

        await _db.SaveChangesAsync();
        await FollowupAsync($"Successfully deleted {quotes.Count} quotes.", ephemeral: true);
    }

    [SlashCommand("reset", "Resets all bot data, quotes, settings, and permissions.")]
    public async Task ResetAsync()
    {
        await DeferAsync(ephemeral: true);
        var dbGuild = await _db.Guilds.Include(g => g.Config).FirstOrDefaultAsync(g => g.DiscordID == Context.Guild.Id.ToString());
        if (dbGuild == null) 
        { 
            await FollowupAsync("Guild not found in database."); 
            return; 
        }

        var quotes = _db.Quotes.Where(q => q.GuildID == dbGuild.ID).ToList();
        var quoteIds = quotes.Select(q => q.ID).ToList();
        
        _db.Set<QuoteQuotee>().RemoveRange(_db.Set<QuoteQuotee>().Where(qq => quoteIds.Contains(qq.QuoteID)));
        _db.Set<QuoteVote>().RemoveRange(_db.Set<QuoteVote>().Where(qv => quoteIds.Contains(qv.QuoteID)));
        _db.Quotes.RemoveRange(quotes);

        if (dbGuild.Config != null)
        {
            var cfg = dbGuild.Config;
            if (cfg.UpvoteEmojiConfig != null) cfg.UpvoteEmojiConfig = null;
            if (cfg.DownvoteEmojiConfig != null) cfg.DownvoteEmojiConfig = null;
            
            var channels = _db.AllowedChannels.Where(c => c.GuildConfigID == cfg.ID);
            _db.AllowedChannels.RemoveRange(channels);
            
            _db.GuildConfigs.Remove(cfg);
        }

        if (dbGuild.Config != null)
        {
            var perms = _db.Permissions.Where(p => p.GuildConfigID == dbGuild.Config.ID);
            _db.Permissions.RemoveRange(perms);
        }

        _db.Guilds.Remove(dbGuild);
        await _db.SaveChangesAsync();
        
        await FollowupAsync("Bot data has been completely reset.", ephemeral: true);
    }

    [SlashCommand("menu", "Opens the interactive configuration menu")]
    public async Task MenuAsync()
    {
        await DeferAsync(ephemeral: true);
        
        var dbGuild = await _db.Guilds
            .Include(g => g.Config)
            .ThenInclude(c => c.AllowedChannels)
            .Include(g => g.Config)
            .ThenInclude(c => c.UpvoteEmojiConfig)
            .Include(g => g.Config)
            .ThenInclude(c => c.DownvoteEmojiConfig)
            .FirstOrDefaultAsync(g => g.DiscordID == Context.Guild.Id.ToString());

        if (dbGuild?.Config == null)
        {
            await FollowupAsync("Guild not configured yet. Create a quote first to initialize.", ephemeral: true);
            return;
        }

        var embed = BuildGeneralPage(dbGuild.Config);
        var components = BuildNavigationButtons("general");

        await FollowupAsync(embed: embed, components: components, ephemeral: true);
    }

    // ── Page Builders ──

    private Embed BuildGeneralPage(GuildConfig cfg)
    {
        return new EmbedBuilder()
            .WithTitle("⚙️ Bot Configuration")
            .WithDescription("**General Settings**\nUse the buttons below to navigate between pages or toggle settings.")
            .WithColor(EmbedColor)
            .AddField("💬 Allow Voting", cfg.AllowVoting ? "✅ Enabled" : "❌ Disabled", inline: true)
            .AddField("💭 Allow Comments", cfg.Comments ? "✅ Enabled" : "❌ Disabled", inline: true)
            .AddField("🔒 Lock Channels", cfg.LockAllowedChannels ? "✅ Enabled" : "❌ Disabled", inline: true)
            .WithFooter("Page 1/3 • General")
            .WithTimestamp(DateTimeOffset.Now)
            .Build();
    }

    private Embed BuildEmojisPage(GuildConfig cfg)
    {
        var upvote = cfg.UpvoteEmojiConfig?.IsCustom == true
            ? $"<:{cfg.UpvoteEmojiConfig.Name}:{cfg.UpvoteEmojiConfig.Id}>"
            : (cfg.UpvoteEmojiConfig?.Name ?? "👍");
        var downvote = cfg.DownvoteEmojiConfig?.IsCustom == true
            ? $"<:{cfg.DownvoteEmojiConfig.Name}:{cfg.DownvoteEmojiConfig.Id}>"
            : (cfg.DownvoteEmojiConfig?.Name ?? "👎");

        return new EmbedBuilder()
            .WithTitle("⚙️ Bot Configuration")
            .WithDescription("**Emoji Settings**\nConfigure the emojis used for voting on quotes.")
            .WithColor(EmbedColor)
            .AddField("⬆️ Upvote Emoji", upvote, inline: true)
            .AddField("⬇️ Downvote Emoji", downvote, inline: true)
            .AddField("\u200b", "Use the **Set Upvote** / **Set Downvote** buttons to change emojis via a popup.", inline: false)
            .WithFooter("Page 2/3 • Emojis")
            .WithTimestamp(DateTimeOffset.Now)
            .Build();
    }

    private Embed BuildChannelsPage(GuildConfig cfg)
    {
        var channels = cfg.AllowedChannels;
        var channelList = channels.Count > 0
            ? string.Join("\n", channels.Select(c => $"• <#{c.Channel}>"))
            : "_No channels restricted — quotes allowed everywhere._";

        return new EmbedBuilder()
            .WithTitle("⚙️ Bot Configuration")
            .WithDescription("**Channel Settings**\nManage which channels quotes are allowed in.")
            .WithColor(EmbedColor)
            .AddField($"📝 Allowed Channels ({channels.Count})", channelList, inline: false)
            .AddField("🔒 Lock Mode", cfg.LockAllowedChannels
                ? "✅ **Locked** — Only allowed channels can have quotes."
                : "❌ **Unlocked** — Quotes allowed in all channels.", inline: false)
            .WithFooter("Page 3/3 • Channels")
            .WithTimestamp(DateTimeOffset.Now)
            .Build();
    }

    // ── Component Builders ──

    private MessageComponent BuildNavigationButtons(string activePage)
    {
        var builder = new ComponentBuilder();

        // Navigation row
        builder.WithButton("General", "config_page_general",
            style: activePage == "general" ? ButtonStyle.Primary : ButtonStyle.Secondary,
            emote: new Emoji("⚙️"), row: 0);
        builder.WithButton("Emojis", "config_page_emojis",
            style: activePage == "emojis" ? ButtonStyle.Primary : ButtonStyle.Secondary,
            emote: new Emoji("😄"), row: 0);
        builder.WithButton("Channels", "config_page_channels",
            style: activePage == "channels" ? ButtonStyle.Primary : ButtonStyle.Secondary,
            emote: new Emoji("📝"), row: 0);

        // Action row based on active page
        switch (activePage)
        {
            case "general":
                builder.WithButton("Toggle Voting", "config_toggle_voting", ButtonStyle.Success, emote: new Emoji("💬"), row: 1);
                builder.WithButton("Toggle Comments", "config_toggle_comments", ButtonStyle.Success, emote: new Emoji("💭"), row: 1);
                builder.WithButton("Toggle Lock", "config_toggle_lock", ButtonStyle.Success, emote: new Emoji("🔒"), row: 1);
                break;
            case "emojis":
                builder.WithButton("Set Upvote", "config_set_upvote", ButtonStyle.Success, emote: new Emoji("⬆️"), row: 1);
                builder.WithButton("Set Downvote", "config_set_downvote", ButtonStyle.Success, emote: new Emoji("⬇️"), row: 1);
                break;
            case "channels":
                builder.WithButton("Toggle Lock", "config_toggle_lock", ButtonStyle.Success, emote: new Emoji("🔒"), row: 1);
                break;
        }

        return builder.Build();
    }

    // ── Component Interaction Handlers ──

    [ComponentInteraction("config_page_*")]
    public async Task HandlePageNavigation(string pageName)
    {
        var interaction = Context.Interaction as SocketMessageComponent;

        var dbGuild = await _db.Guilds
            .Include(g => g.Config).ThenInclude(c => c.AllowedChannels)
            .Include(g => g.Config).ThenInclude(c => c.UpvoteEmojiConfig)
            .Include(g => g.Config).ThenInclude(c => c.DownvoteEmojiConfig)
            .FirstOrDefaultAsync(g => g.DiscordID == Context.Guild.Id.ToString());

        if (dbGuild?.Config == null) return;

        Embed embed = pageName switch
        {
            "emojis" => BuildEmojisPage(dbGuild.Config),
            "channels" => BuildChannelsPage(dbGuild.Config),
            _ => BuildGeneralPage(dbGuild.Config)
        };

        var components = BuildNavigationButtons(pageName);
        await interaction.UpdateAsync(msg =>
        {
            msg.Embed = embed;
            msg.Components = components;
        });
    }

    [ComponentInteraction("config_toggle_voting")]
    public async Task ToggleVoting()
    {
        var interaction = Context.Interaction as SocketMessageComponent;
        var dbGuild = await _db.Guilds.Include(g => g.Config)
            .ThenInclude(c => c.AllowedChannels)
            .FirstOrDefaultAsync(g => g.DiscordID == Context.Guild.Id.ToString());
        if (dbGuild?.Config == null) return;

        dbGuild.Config.AllowVoting = !dbGuild.Config.AllowVoting;
        await _db.SaveChangesAsync();

        await interaction!.UpdateAsync(msg =>
        {
            msg.Embed = BuildGeneralPage(dbGuild.Config);
            msg.Components = BuildNavigationButtons("general");
        });
    }

    [ComponentInteraction("config_toggle_comments")]
    public async Task ToggleComments()
    {
        var interaction = Context.Interaction as SocketMessageComponent;
        var dbGuild = await _db.Guilds.Include(g => g.Config)
            .ThenInclude(c => c.AllowedChannels)
            .FirstOrDefaultAsync(g => g.DiscordID == Context.Guild.Id.ToString());
        if (dbGuild?.Config == null) return;

        dbGuild.Config.Comments = !dbGuild.Config.Comments;
        await _db.SaveChangesAsync();

        await interaction!.UpdateAsync(msg =>
        {
            msg.Embed = BuildGeneralPage(dbGuild.Config);
            msg.Components = BuildNavigationButtons("general");
        });
    }

    [ComponentInteraction("config_toggle_lock")]
    public async Task ToggleLock()
    {
        var interaction = Context.Interaction as SocketMessageComponent;
        var dbGuild = await _db.Guilds.Include(g => g.Config)
            .ThenInclude(c => c.AllowedChannels)
            .FirstOrDefaultAsync(g => g.DiscordID == Context.Guild.Id.ToString());
        if (dbGuild?.Config == null) return;

        dbGuild.Config.LockAllowedChannels = !dbGuild.Config.LockAllowedChannels;
        await _db.SaveChangesAsync();

        // Determine which page we're on based on the current footer
        await interaction!.UpdateAsync(msg =>
        {
            msg.Embed = BuildGeneralPage(dbGuild.Config);
            msg.Components = BuildNavigationButtons("general");
        });
    }

    [ComponentInteraction("config_set_upvote")]
    public async Task SetUpvoteEmoji()
    {
        var modal = new ModalBuilder()
            .WithTitle("Set Upvote Emoji")
            .WithCustomId("config_modal_upvote")
            .AddTextInput("Emoji", "emoji_value", TextInputStyle.Short,
                placeholder: "Enter emoji (e.g. 👍 or custom emoji name)", required: true, maxLength: 50);

        await Context.Interaction.RespondWithModalAsync(modal.Build());
    }

    [ComponentInteraction("config_set_downvote")]
    public async Task SetDownvoteEmoji()
    {
        var modal = new ModalBuilder()
            .WithTitle("Set Downvote Emoji")
            .WithCustomId("config_modal_downvote")
            .AddTextInput("Emoji", "emoji_value", TextInputStyle.Short,
                placeholder: "Enter emoji (e.g. 👎 or custom emoji name)", required: true, maxLength: 50);

        await Context.Interaction.RespondWithModalAsync(modal.Build());
    }

    // ── Modal Handlers ──

    [ModalInteraction("config_modal_upvote")]
    public async Task HandleUpvoteModal(EmojiModal modal)
    {
        await DeferAsync(ephemeral: true);
        var emojiValue = modal.EmojiValue.Trim();

        var dbGuild = await _db.Guilds
            .Include(g => g.Config).ThenInclude(c => c.UpvoteEmojiConfig)
            .Include(g => g.Config).ThenInclude(c => c.DownvoteEmojiConfig)
            .Include(g => g.Config).ThenInclude(c => c.AllowedChannels)
            .FirstOrDefaultAsync(g => g.DiscordID == Context.Guild.Id.ToString());
        if (dbGuild?.Config == null) return;

        var emojiConfig = ParseEmojiInput(emojiValue);
        if (emojiConfig == null)
        {
            await FollowupAsync("❌ Invalid emoji. Please use a standard emoji or a custom emoji from this server.", ephemeral: true);
            return;
        }

        if (dbGuild.Config.UpvoteEmojiConfig != null)
            dbGuild.Config.UpvoteEmojiConfig = null;

        dbGuild.Config.UpvoteEmojiConfig = emojiConfig;
        await _db.SaveChangesAsync();

        await FollowupAsync($"✅ Upvote emoji set to {emojiValue}", ephemeral: true);
    }

    [ModalInteraction("config_modal_downvote")]
    public async Task HandleDownvoteModal(EmojiModal modal)
    {
        await DeferAsync(ephemeral: true);
        var emojiValue = modal.EmojiValue.Trim();

        var dbGuild = await _db.Guilds
            .Include(g => g.Config).ThenInclude(c => c.UpvoteEmojiConfig)
            .Include(g => g.Config).ThenInclude(c => c.DownvoteEmojiConfig)
            .Include(g => g.Config).ThenInclude(c => c.AllowedChannels)
            .FirstOrDefaultAsync(g => g.DiscordID == Context.Guild.Id.ToString());
        if (dbGuild?.Config == null) return;

        var emojiConfig = ParseEmojiInput(emojiValue);
        if (emojiConfig == null)
        {
            await FollowupAsync("❌ Invalid emoji. Please use a standard emoji or a custom emoji from this server.", ephemeral: true);
            return;
        }

        if (dbGuild.Config.DownvoteEmojiConfig != null)
            dbGuild.Config.DownvoteEmojiConfig = null;

        dbGuild.Config.DownvoteEmojiConfig = emojiConfig;
        await _db.SaveChangesAsync();

        await FollowupAsync($"✅ Downvote emoji set to {emojiValue}", ephemeral: true);
    }

    // ── Helpers ──

    private EmojiConfig? ParseEmojiInput(string input)
    {
        // Try to parse custom emoji format <:name:id> or <a:name:id>
        if (Emote.TryParse(input, out var emote))
        {
            return new EmojiConfig
            {
                Type = emote.Animated ? "animated" : "custom",
                Id = emote.Id.ToString(),
                Name = emote.Name,
                Animated = emote.Animated
            };
        }

        // Check if it's a single unicode emoji (simple check)
        if (input.Length <= 4 && !string.IsNullOrWhiteSpace(input))
        {
            return new EmojiConfig
            {
                Type = "unicode",
                Name = input
            };
        }

        return null;
    }
}

public class EmojiModal : IModal
{
    public string Title => "Set Emoji";

    [InputLabel("Emoji")]
    [ModalTextInput("emoji_value", TextInputStyle.Short, placeholder: "Enter emoji")]
    public string EmojiValue { get; set; } = "";
}

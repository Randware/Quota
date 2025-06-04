using Discord;

namespace Bot;

public static class BotPermissions
{
    /// <summary>
    /// The permissions required for the bot to function properly
    /// </summary>
    public static readonly GuildPermissions RequiredPermissions = new GuildPermissions(
        viewChannel: true,
        sendMessages: true,
        sendMessagesInThreads: true,
        manageThreads: true,
        manageMessages: true,
        embedLinks: true,
        useExternalEmojis: true,
        addReactions: true,
        viewGuildInsights: true,
        manageGuild: true,
        banMembers: true
    );

    /// <summary>
    /// Gets the join URL for adding the bot to a server with the required permissions
    /// </summary>
    /// <param name="clientId">The Discord application client ID</param>
    /// <returns>The OAuth2 URL for adding the bot</returns>
    public static string GetOAuth2Url(string clientId) =>
        $"https://discord.com/api/oauth2/authorize?client_id={clientId}&permissions={RequiredPermissions.RawValue}&scope=bot+applications.commands";
}

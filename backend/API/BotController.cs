using Microsoft.AspNetCore.Mvc;
using Common;
using Common.OAuth;

namespace API;

/// <summary>
/// Provides endpoints for Discord bot-related operations
/// </summary>
[ApiController]
[Route("bot")]
public class BotController : ControllerBase
{
    private readonly Client _discordClient;

    public BotController(Client discordClient)
    {
        _discordClient = discordClient;
    }

    /// <summary>
    /// Gets the OAuth2 URL for adding the Discord bot to a server
    /// </summary>
    /// <remarks>
    /// This endpoint generates an invite URL that includes all necessary permissions for the bot to function:
    /// - View Channels: To see channels where quotes can be posted
    /// - Send Messages: To send quote messages
    /// - Send Messages in Threads: To send messages in threads if needed
    /// - Embed Links: To send embedded quote messages
    /// - Use External Emojis: To use custom emojis for voting
    /// - Add Reactions: To add voting reactions
    /// - View Guild Insights: To track guild members and their permissions
    /// - Manage Guild: To manage bot configurations
    /// </remarks>
    /// <response code="200">Returns the bot's invite URL</response>
    /// <response code="500">If there was an error generating the URL</response>
    /// <returns>An object containing the bot's invite URL</returns>
    [HttpGet("invite")]
    [ProducesResponseType(typeof(BotInviteResponse), 200)]
    [ProducesResponseType(typeof(ErrorResponse), 500)]
    public IActionResult GetInviteUrl()
    {
        try
        {
            var inviteUrl = Bot.BotPermissions.GetOAuth2Url(_discordClient.ID);
            return Ok(new BotInviteResponse { InviteUrl = inviteUrl });
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error generating bot invite URL");
            return StatusCode(500, new ErrorResponse { Error = "Could not generate bot invite URL" });
        }
    }
}

/// <summary>
/// Response containing the bot's invite URL
/// </summary>
public class BotInviteResponse
{
    /// <summary>
    /// The URL that can be used to invite the bot to a Discord server
    /// </summary>
    /// <example>https://discord.com/api/oauth2/authorize?client_id=123456789&amp;permissions=8</example>
    public string InviteUrl { get; set; }
}

/// <summary>
/// Response for error cases
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Description of the error that occurred
    /// </summary>
    public string Error { get; set; }
}

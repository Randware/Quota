using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Common.OAuth;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Common;
using Database.Model;

namespace API.Controllers
{
    /// <summary>
    /// Controller for server (guild) specific endpoints.
    /// </summary>
    [ApiController]
    [Route("server/{id}")]
    public class ServerController : ControllerBase
    {
        private readonly QuotaContext _db;
        private readonly JwtService _jwtService;
        private readonly DiscordTokenService _discordTokenService;
        private readonly Common.OAuth.Client _discordClient;

        public ServerController(QuotaContext db, JwtService jwtService, DiscordTokenService discordTokenService, Common.OAuth.Client discordClient)
        {
            _db = db;
            _jwtService = jwtService;
            _discordTokenService = discordTokenService;
            _discordClient = discordClient;
        }

        /// <summary>
        /// Gets information about a specific Discord server (guild).
        /// </summary>
        /// <param name="id">The Discord guild ID</param>
        /// <returns>Guild information as returned by the Discord API</returns>
        /// <response code="200">Returns the guild info</response>
        /// <response code="401">If the JWT is missing, invalid, or the guild is not allowed</response>
        [HttpGet("info")]
        [Authorize]
        public async Task<IActionResult> GetGuildInfo([FromRoute] string id)
        {
            var jwtUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(jwtUserId))
                return Unauthorized("Missing user id in token");

            var allowedGuilds = User.FindFirst("allowed_guilds")?.Value;
            if (allowedGuilds == null || !allowedGuilds.Split(',').Contains(id))
            {
                return Unauthorized("You do not have access to this guild.");
            }

            var token = await _discordTokenService.GetValidTokenForUserAsync(jwtUserId, new[] { "guilds", "identify" });
            if (token == null)
                return StatusCode(403, "No valid Discord token found for user, or required scopes are missing, or token refresh failed.");

            Log.Logger.Information(_discordClient.BotToken);
            var response = await Common.OAuth.API.FetchGuildInfo(_discordClient, id);
            if (response == null)
                return StatusCode(502, "Failed to fetch guild info from Discord API.");
            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }

        /// <summary>
        /// Gets all channels for a specific Discord server (guild).
        /// </summary>
        /// <param name="id">The Discord guild ID</param>
        /// <returns>List of channels as returned by the Discord API</returns>
        /// <response code="200">Returns the list of channels</response>
        /// <response code="401">If the JWT is missing, invalid, or the guild is not allowed</response>
        [HttpGet("channels")]
        [Authorize]
        public async Task<IActionResult> GetGuildChannels([FromRoute] string id)
        {
            var jwtUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(jwtUserId))
                return Unauthorized("Missing user id in token");

            var allowedGuilds = User.FindFirst("allowed_guilds")?.Value;
            if (allowedGuilds == null || !allowedGuilds.Split(',').Contains(id))
            {
                return Unauthorized("You do not have access to this guild.");
            }

            var token = await _discordTokenService.GetValidTokenForUserAsync(jwtUserId, new[] { "guilds", "identify" });
            if (token == null)
                return StatusCode(403, "No valid Discord token found for user, or required scopes are missing, or token refresh failed.");

            var response = await Common.OAuth.API.FetchGuildChannels(_discordClient, id);
            if (response == null)
                return StatusCode(502, "Failed to fetch guild channels from Discord API.");
            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }
    }
}


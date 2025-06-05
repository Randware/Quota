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

        /// <summary>
        /// Gets or updates the configuration for a specific Discord server (guild).
        /// </summary>
        /// <param name="id">The Discord guild ID</param>
        /// <returns>Guild configuration as JSON</returns>
        /// <response code="200">Returns the guild config</response>
        /// <response code="401">If the JWT is missing, invalid, or the guild is not allowed</response>
        [HttpGet("config")]
        [Authorize]
        public async Task<IActionResult> GetGuildConfig([FromRoute] string id)
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

            var dbGuild = await _db.Guilds
                .Include(g => g.Config)
                .ThenInclude(cfg => cfg.AllowedChannels)
                .FirstOrDefaultAsync(g => g.DiscordID == id);
            if (dbGuild?.Config == null)
                return NotFound("Guild config not found");

            var cfg = dbGuild.Config;
            object upvoteEmoji = cfg.UpvoteEmojiConfig?.IsCustom == true
                ? new {
                    type = cfg.UpvoteEmojiConfig.Type,
                    id = cfg.UpvoteEmojiConfig.Id,
                    name = cfg.UpvoteEmojiConfig.Name,
                    animated = cfg.UpvoteEmojiConfig.Animated,
                    url = cfg.UpvoteEmojiConfig.ToFrontendUrl()
                }
                : (object)(cfg.UpvoteEmojiConfig?.Name ?? "👍");
            object downvoteEmoji = cfg.DownvoteEmojiConfig?.IsCustom == true
                ? new {
                    type = cfg.DownvoteEmojiConfig.Type,
                    id = cfg.DownvoteEmojiConfig.Id,
                    name = cfg.DownvoteEmojiConfig.Name,
                    animated = cfg.DownvoteEmojiConfig.Animated,
                    url = cfg.DownvoteEmojiConfig.ToFrontendUrl()
                }
                : (object)(cfg.DownvoteEmojiConfig?.Name ?? "👎");

            var result = new {
                allowVoting = cfg.AllowVoting,
                upvoteEmoji,
                downvoteEmoji,
                lockAllowedChannels = cfg.LockAllowedChannels,
                allowComments = cfg.Comments,
                allowedChannels = cfg.AllowedChannels?.Select(ac => ac.Channel).ToArray() ?? Array.Empty<string>()
            };
            return Ok(result);
        }

        [HttpPost("config")]
        [Authorize]
        public async Task<IActionResult> UpdateGuildConfig([FromRoute] string id, [FromBody] System.Text.Json.JsonElement body)
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

            var dbGuild = await _db.Guilds
                .Include(g => g.Config)
                .ThenInclude(cfg => cfg.AllowedChannels)
                .FirstOrDefaultAsync(g => g.DiscordID == id);
            if (dbGuild?.Config == null)
                return NotFound("Guild config not found");
            var cfg = dbGuild.Config;

            // Helper to check if a property is set in the request
            bool HasProp(string name) => body.ValueKind == System.Text.Json.JsonValueKind.Object && body.TryGetProperty(name, out _);

            // Validate and update fields if present
            if (HasProp("allowVoting"))
            {
                if (body.GetProperty("allowVoting").ValueKind == System.Text.Json.JsonValueKind.Null) return BadRequest("allowVoting cannot be null");
                cfg.AllowVoting = body.GetProperty("allowVoting").GetBoolean();
            }
            if (HasProp("lockAllowedChannels"))
            {
                if (body.GetProperty("lockAllowedChannels").ValueKind == System.Text.Json.JsonValueKind.Null) return BadRequest("lockAllowedChannels cannot be null");
                cfg.LockAllowedChannels = body.GetProperty("lockAllowedChannels").GetBoolean();
            }
            if (HasProp("allowComments"))
            {
                if (body.GetProperty("allowComments").ValueKind == System.Text.Json.JsonValueKind.Null) return BadRequest("allowComments cannot be null");
                cfg.Comments = body.GetProperty("allowComments").GetBoolean();
            }
            if (HasProp("upvoteEmoji"))
            {
                var upvoteEmoji = body.GetProperty("upvoteEmoji");
                if (upvoteEmoji.ValueKind == System.Text.Json.JsonValueKind.Null) return BadRequest("upvoteEmoji cannot be null");
                if (upvoteEmoji.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    cfg.UpvoteEmojiConfig = new EmojiConfig { Type = "unicode", Name = upvoteEmoji.GetString() };
                }
                else if (upvoteEmoji.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (!upvoteEmoji.TryGetProperty("type", out var typeProp) || !upvoteEmoji.TryGetProperty("name", out var nameProp))
                        return BadRequest("upvoteEmoji.type and name required");
                    cfg.UpvoteEmojiConfig = new EmojiConfig
                    {
                        Type = typeProp.GetString(),
                        Id = upvoteEmoji.TryGetProperty("id", out var idProp) && idProp.ValueKind != System.Text.Json.JsonValueKind.Null ? idProp.GetString() : null,
                        Name = nameProp.GetString(),
                        Animated = upvoteEmoji.TryGetProperty("animated", out var animProp) && animProp.ValueKind == System.Text.Json.JsonValueKind.True
                    };
                }
                else
                {
                    return BadRequest("upvoteEmoji must be a string or object");
                }
            }
            if (HasProp("downvoteEmoji"))
            {
                var downvoteEmoji = body.GetProperty("downvoteEmoji");
                if (downvoteEmoji.ValueKind == System.Text.Json.JsonValueKind.Null) return BadRequest("downvoteEmoji cannot be null");
                if (downvoteEmoji.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    cfg.DownvoteEmojiConfig = new EmojiConfig { Type = "unicode", Name = downvoteEmoji.GetString() };
                }
                else if (downvoteEmoji.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    if (!downvoteEmoji.TryGetProperty("type", out var typeProp) || !downvoteEmoji.TryGetProperty("name", out var nameProp))
                        return BadRequest("downvoteEmoji.type and name required");
                    cfg.DownvoteEmojiConfig = new EmojiConfig
                    {
                        Type = typeProp.GetString(),
                        Id = downvoteEmoji.TryGetProperty("id", out var idProp) && idProp.ValueKind != System.Text.Json.JsonValueKind.Null ? idProp.GetString() : null,
                        Name = nameProp.GetString(),
                        Animated = downvoteEmoji.TryGetProperty("animated", out var animProp) && animProp.ValueKind == System.Text.Json.JsonValueKind.True
                    };
                }
                else
                {
                    return BadRequest("downvoteEmoji must be a string or object");
                }
            }
            if (HasProp("allowedChannels"))
            {
                var allowedChannels = body.GetProperty("allowedChannels");
                if (allowedChannels.ValueKind == System.Text.Json.JsonValueKind.Null) return BadRequest("allowedChannels cannot be null");
                if (allowedChannels.ValueKind != System.Text.Json.JsonValueKind.Array) return BadRequest("allowedChannels must be an array");
                var channels = new List<string>();
                foreach (var ch in allowedChannels.EnumerateArray())
                {
                    if (ch.ValueKind == System.Text.Json.JsonValueKind.String)
                        channels.Add(ch.GetString());
                }
                cfg.AllowedChannels.Clear();
                foreach (var ch in channels)
                {
                    cfg.AllowedChannels.Add(new AllowedChannel { GuildConfigID = cfg.ID, Channel = ch });
                }
            }

            await _db.SaveChangesAsync();
            return Ok();
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using Database.Model;
using System.IdentityModel.Tokens.Jwt;
using API;
using Common.OAuth;
using Microsoft.AspNetCore.Authentication.OAuth;

namespace API.Controllers
{
    /// <summary>
    /// Controller for user-specific endpoints (permissions, servers, etc).
    /// </summary>
    [ApiController]
    [Route("user/{id}")]
    public class UserController : ControllerBase
    {
        private readonly QuotaContext _db;
        private readonly JwtService _jwtService;
        private readonly DiscordTokenService _discordTokenService;

        public UserController(QuotaContext db, JwtService jwtService, DiscordTokenService discordTokenService)
        {
            _db = db;
            _jwtService = jwtService;
            _discordTokenService = discordTokenService;
        }

        /// <summary>
        /// Gets all permissions for the user, grouped by server (guild).
        /// </summary>
        /// <param name="id">The Discord user ID</param>
        /// <returns>A dictionary mapping server_id to a list of permissions</returns>
        /// <response code="200">Returns the user's permissions per server</response>
        /// <response code="401">If the JWT is missing or invalid, or user id does not match</response>
        [HttpGet("permissions")]
        [Authorize]
        public async Task<IActionResult> GetPermissions([FromRoute] string id)
        {
            // Use ClaimsPrincipal from ASP.NET Core
            var jwtUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (jwtUserId != id)
            {
                return Unauthorized("User ID does not match token");
            }

            // Query permissions for this user
            var permissions = await _db.Permissions
                .Include(p => p.GuildConfig)
                .ThenInclude(cfg => cfg.Guild)
                .Where(p => p.UserID == id)
                .ToListAsync();

            var result = permissions
                .GroupBy(p => p.GuildConfig.Guild.DiscordID)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(p => p.PermissionType.ToString()).ToArray()
                );

            return Ok(result);
        }

        /// <summary>
        /// Gets all guilds the user is in, using the Discord API.
        /// </summary>
        /// <param name="id">The Discord user ID</param>
        /// <returns>An array of Discord guild objects</returns>
        /// <response code="200">Returns the list of servers</response>
        /// <response code="401">If the JWT is missing or invalid, or user id does not match</response>
        [HttpGet("guilds")]
        [Authorize]
        public async Task<IActionResult> GetGuilds([FromRoute] string id)
        {
            var jwtUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (jwtUserId != id)
            {
                return Unauthorized("User ID does not match token");
            }

            var token = await _discordTokenService.GetValidTokenForUserAsync(id, new[] { "guilds", "identify" });
            if (token == null)
                return StatusCode(403, "No valid Discord token found for user, or required scopes are missing, or token refresh failed.");

            var response = await Common.OAuth.API.FetchUserGuilds(token);
            if (response is null)
            {
                return StatusCode(500, "Failed to fetch servers from Discord API");
            }
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Failed to fetch servers from Discord API");
            }
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return Ok(doc.RootElement);
        }

        /// <summary>
        /// Gets the raw Discord user info for the specified user ID.
        /// </summary>
        /// <param name="id">The Discord user ID</param>
        /// <returns>The raw Discord user info as returned by the Discord API</returns>
        /// <response code="200">Returns the user's raw Discord info</response>
        /// <response code="401">If the JWT is missing or invalid, or user id does not match</response>
        [HttpGet("info")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo([FromRoute] string id)
        {
            var jwtUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (jwtUserId != id)
            {
                return Unauthorized("User ID does not match token");
            }

            var token = await _discordTokenService.GetValidTokenForUserAsync(id, new[] { "identify" });
            if (token == null)
                return StatusCode(403, "No valid Discord token found for user, or required scopes are missing, or token refresh failed.");

            var response = await Common.OAuth.API.FetchUserRaw(token);
            if (response is null)
            {
                return StatusCode(500, "Failed to fetch user info from Discord API");
            }
            if (!response.IsSuccessStatusCode)
            {
                return StatusCode((int)response.StatusCode, "Failed to fetch user info from Discord API");
            }
            var json = await response.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            return Ok(doc.RootElement);
        }
    }
}

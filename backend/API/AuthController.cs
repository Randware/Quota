using Microsoft.AspNetCore.Mvc;
using Database;
using Common.OAuth;
using OAuth = Common.OAuth;
using System.ComponentModel.DataAnnotations;
using Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;

namespace API;

/// <summary>
/// Handles authentication, session, and token management for the Randware Quota API.
/// </summary>
[ApiController]
[Route("auth")]
[AllowAnonymous]
[EnableRateLimiting("auth")]
public class AuthController : ControllerBase
{
    private readonly Storage _storage;
    private readonly JwtService _jwtService;
    private readonly OAuth.Client _discordClient;
    private readonly DiscordTokenService _discordTokenService;

    public AuthController(Storage storage, JwtService jwtService, OAuth.Client discordClient, DiscordTokenService discordTokenService)
    {
        _storage = storage;
        _jwtService = jwtService;
        _discordClient = discordClient;
        _discordTokenService = discordTokenService;
    }

    /// <summary>
    /// Authenticates a user with a Discord OAuth2 code and issues a JWT and refresh token for the app.
    /// </summary>
    /// <param name="request">The login request containing the Discord OAuth2 code and redirect URI.</param>
    /// <returns>JWT and refresh token for the app.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var token = await OAuth.API.GetToken(request.Code, request.RedirectUri, _discordClient);
        if (token == null)
            return BadRequest(new { error = "Invalid code or RedirectURI" });
        var discordUser = await OAuth.API.FetchUser(token);
        if (discordUser == null)
            return BadRequest(new { error = "Could not fetch Discord user" });

        var dbToken = await _storage.GetDiscordTokenByDiscordIdAsync(discordUser.Value.ID);
        if (dbToken == null)
        {
            dbToken = await _storage.CreateDiscordTokenAsync(discordUser.Value, token);
        }
        else
        {
            dbToken = await _storage.UpdateDiscordTokenAsync(dbToken, token);
        }

        // Generate a new random refresh token for the app session
        var appRefreshToken = Guid.NewGuid().ToString();
        var session = await _storage.CreateSessionAsync(dbToken.ID, appRefreshToken, token.ExpiresIn);
        // Get all guilds where user has DASHBOARD or ADMIN permission
        var allowedGuilds = await GetAllowedGuilds(discordUser.Value.ID);
        var jwt = _jwtService.GenerateJwt(discordUser.Value.ID, session.ID, allowedGuilds);

        return Ok(new AuthResponse { Jwt = jwt, RefreshToken = session.RefreshToken });
    }

    /// <summary>
    /// Refreshes the app's JWT and refresh token using a valid refresh token. Also refreshes the Discord token if needed.
    /// </summary>
    /// <param name="request">The refresh request containing the app's refresh token.</param>
    /// <returns>New JWT and refresh token for the app.</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        var session = await _storage.GetSessionByRefreshTokenAsync(request.RefreshToken);
        if (session == null || session.Revoked)
            return Unauthorized(new { error = "Session revoked or not found" });

        var dbToken = session.Token;
        // Step 1: Refresh Discord token if needed (use DiscordTokenService)
        var token = await _discordTokenService.GetValidTokenForUserAsync(dbToken.DiscordID);
        if (token == null)
            return Unauthorized(new { error = "Could not refresh Discord token" });
        dbToken = await _storage.UpdateDiscordTokenAsync(dbToken, token);

        // Step 2: Revoke the old app session
        session.Revoked = true;
        await _storage.UpdateSessionAsync(session);

        // Step 3: Create a new app session with a new random refresh token
        var newAppRefreshToken = Guid.NewGuid().ToString();
        var newSession = await _storage.CreateSessionAsync(dbToken.ID, newAppRefreshToken, (long)(dbToken.ExpiresAt - DateTime.UtcNow).TotalSeconds);

        // Step 4: Issue new JWT and refresh token for the app
        var allowedGuilds = await GetAllowedGuilds(dbToken.DiscordID);
        var jwt = _jwtService.GenerateJwt(dbToken.DiscordID, newSession.ID, allowedGuilds);
        return Ok(new AuthResponse { Jwt = jwt, RefreshToken = newSession.RefreshToken });
    }

    /// <summary>
    /// Verifies the validity of a JWT.
    /// </summary>
    /// <param name="request">The verify request containing the JWT.</param>
    /// <returns>Whether the JWT is valid and its claims.</returns>
    [HttpPost("verify")]
    [AllowAnonymous]
    public IActionResult Verify([FromBody] VerifyRequest request)
    {
        try
        {
            var principal = _jwtService.ValidateJwt(request.Jwt);
            if (principal == null)
                return Unauthorized(new { error = "Invalid or expired JWT" });
            return Ok(new { valid = true, claims = principal.Claims.Select(c => new { c.Type, c.Value }) });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Revokes a specific app session by its refresh token.
    /// </summary>
    /// <param name="request">The revoke request containing the refresh token.</param>
    /// <returns>Success or not found.</returns>
    [HttpPost("revoke")]
    public async Task<IActionResult> Revoke([FromBody] RevokeRequest request)
    {
        var result = await _storage.RevokeSessionByRefreshTokenAsync(request.RefreshToken);
        if (!result)
            return NotFound(new { error = "Session not found or already revoked" });
        return Ok(new { success = true });
    }

    /// <summary>
    /// Revokes all app sessions for the authenticated user (requires an active refresh token).
    /// </summary>
    /// <param name="request">The revoke all request containing a valid refresh token.</param>
    /// <returns>The number of sessions revoked.</returns>
    [HttpPost("revoke-all")]
    public async Task<IActionResult> RevokeAll([FromBody] RevokeAllRequest request)
    {
        var session = await _storage.GetSessionByRefreshTokenAsync(request.RefreshToken);
        if (session == null || session.Revoked)
            return Unauthorized(new { error = "Session revoked or not found" });

        // Get the Discord ID from the valid session's token
        var discordId = session.Token.DiscordID;
        
        // Revoke all sessions for this Discord ID
        var count = await _storage.RevokeAllSessionsByDiscordIdAsync(discordId);
        return Ok(new { revoked = count });
    }

    // Helper to get all guilds where user has DASHBOARD or ADMIN permission
    private async Task<IEnumerable<string>> GetAllowedGuilds(string discordId)
    {
        var allowedGuilds = new List<string>();
        var user = await _storage.GetUserByDiscordIdAsync(discordId);
        if (user == null)
        {
            // Ensure user exists in DB before checking permissions
            user = await _storage.CreateUserAsync(discordId);
        }
        var guilds = await _storage.GetAllGuildsWithPermissionsAsync();
        foreach (var guild in guilds)
        {
            if (guild.Config?.Permissions != null &&
                guild.Config.Permissions.Any(p => p.UserID == user.DiscordID && (p.PermissionType == Database.Model.PermissionType.DASHBOARD || p.PermissionType == Database.Model.PermissionType.ADMIN)))
            {
                allowedGuilds.Add(guild.DiscordID);
            }
        }
        return allowedGuilds;
    }
}

/// <summary>
/// Request for logging in with Discord OAuth2.
/// </summary>
public class LoginRequest
{
    /// <summary>The Discord OAuth2 code.</summary>
    [Required]
    public string Code { get; set; }
    /// <summary>The redirect URI used in the OAuth2 flow.</summary>
    [Required]
    public string RedirectUri { get; set; }
}

/// <summary>
/// Request for refreshing the app's JWT and refresh token.
/// </summary>
public class RefreshRequest
{
    /// <summary>The app's refresh token.</summary>
    [Required]
    public string RefreshToken { get; set; }
}

/// <summary>
/// Response containing the app's JWT and refresh token.
/// </summary>
public class AuthResponse
{
    /// <summary>The app's JWT (access token).</summary>
    public string Jwt { get; set; }
    /// <summary>The app's refresh token.</summary>
    public string RefreshToken { get; set; }
}

/// <summary>
/// Request for verifying a JWT.
/// </summary>
public class VerifyRequest
{
    /// <summary>The JWT to verify.</summary>
    [Required]
    public string Jwt { get; set; }
}

/// <summary>
/// Request for revoking a specific session by refresh token.
/// </summary>
public class RevokeRequest
{
    /// <summary>The refresh token of the session to revoke.</summary>
    [Required]
    public string RefreshToken { get; set; }
}

/// <summary>
/// Request for revoking all sessions for the authenticated user.
/// </summary>
public class RevokeAllRequest
{
    /// <summary>The refresh token of an active session.</summary>
    [Required]
    public string RefreshToken { get; set; }
}

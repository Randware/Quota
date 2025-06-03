using Microsoft.AspNetCore.Mvc;
using Database;
using Database.Model;
using Common.OAuth;
using OAuth = Common.OAuth;
using API = Common.OAuth.API;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace API;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly Storage _storage;
    private readonly JwtService _jwtService;
    private readonly OAuth.Client _discordClient;

    public AuthController(Storage storage, JwtService jwtService, OAuth.Client discordClient)
    {
        _storage = storage;
        _jwtService = jwtService;
        _discordClient = discordClient;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        // Exchange code for Discord tokens
        var token = await OAuth.API.GetToken(request.Code, request.RedirectUri, _discordClient);
        if (token == null)
            return BadRequest("Invalid code or Discord error");

        // Fetch Discord user info
        var discordUser = await OAuth.API.FetchUser(token.Value);
        if (discordUser == null)
            return BadRequest("Could not fetch Discord user");

        // Check if DiscordToken exists
        var dbToken = await _storage.GetDiscordTokenByDiscordIdAsync(discordUser.Value.ID);
        if (dbToken == null)
        {
            // Create new DiscordToken
            dbToken = await _storage.CreateDiscordTokenAsync(discordUser.Value, token.Value);
        }
        else
        {
            // Update tokens if needed
            dbToken = await _storage.UpdateDiscordTokenAsync(dbToken, token.Value);
        }

        // Create new session
        var session = await _storage.CreateSessionAsync(dbToken.ID, token.Value.RefreshToken, token.Value.ExpiresIn);

        // Generate JWT
        var jwt = _jwtService.GenerateJwt(discordUser.Value.ID, session.ID);

        return Ok(new AuthResponse { Jwt = jwt, RefreshToken = session.RefreshToken });
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
    {
        // Find session by refresh token
        var session = await _storage.GetSessionByRefreshTokenAsync(request.RefreshToken);
        if (session == null || session.Revoked)
            return Unauthorized("Session revoked or not found");

        // Check permissions (implement your logic here)
        if (!await _storage.HasPermissionAsync(session.Token.DiscordID, PermissionType.GET))
            return Forbid("Insufficient permissions");

        // Refresh Discord token if needed
        var dbToken = session.Token;
        if (dbToken.ExpiresAt <= DateTime.UtcNow)
        {
            var refreshed = await OAuth.API.RefreshToken(new Token
            {
                AccessToken = dbToken.AccessToken,
                RefreshToken = dbToken.RefreshToken,
                TokenType = "Bearer",
                ExpiresIn = (long)(dbToken.ExpiresAt - dbToken.CreatedAt).TotalSeconds,
                CreatedAt = dbToken.CreatedAt,
                Scope = new HashSet<string>()
            }, _discordClient);
            if (refreshed == null)
                return Unauthorized("Could not refresh Discord token");
            dbToken = await _storage.UpdateDiscordTokenAsync(dbToken, refreshed.Value);
        }

        // Issue new session and JWT
        var newSession = await _storage.CreateSessionAsync(dbToken.ID, dbToken.RefreshToken, (long)(dbToken.ExpiresAt - DateTime.UtcNow).TotalSeconds);
        var jwt = _jwtService.GenerateJwt(dbToken.DiscordID, newSession.ID);
        return Ok(new AuthResponse { Jwt = jwt, RefreshToken = newSession.RefreshToken });
    }
}

public class LoginRequest
{
    public string Code { get; set; }
    public string RedirectUri { get; set; }
}

public class RefreshRequest
{
    public string RefreshToken { get; set; }
}

public class AuthResponse
{
    public string Jwt { get; set; }
    public string RefreshToken { get; set; }
}


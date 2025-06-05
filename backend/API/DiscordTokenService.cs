using System;
using System.Linq;
using System.Threading.Tasks;
using Common.OAuth;
using Database.Model;
using Microsoft.EntityFrameworkCore;

namespace API
{
    public class DiscordTokenService
    {
        private readonly QuotaContext _db;
        private readonly string _clientId;
        private readonly string _clientSecret;

        public DiscordTokenService(QuotaContext db)
        {
            _db = db;
            _clientId = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID")!;
            _clientSecret = Environment.GetEnvironmentVariable("DISCORD_CLIENT_SECRET")!;
        }

        public async Task<Token?> GetValidTokenForUserAsync(string discordUserId, IEnumerable<string>? requiredScopes = null)
        {
            var discordToken = await _db.Sessions
                .Include(s => s.Token)
                .Where(s => s.Token.DiscordID == discordUserId && !s.Revoked && s.ExpiresAt > DateTime.UtcNow)
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => s.Token)
                .FirstOrDefaultAsync();

            if (discordToken == null)
                return null;

            var token = new Token
            {
                AccessToken = discordToken.AccessToken,
                RefreshToken = discordToken.RefreshToken,
                TokenType = "Bearer",
                ExpiresIn = (long)(discordToken.ExpiresAt - discordToken.CreatedAt).TotalSeconds,
                CreatedAt = discordToken.CreatedAt,
                Scope = new HashSet<string>((discordToken.Scope ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries))
            };

            // Scope validation
            if (requiredScopes != null && requiredScopes.Any())
            {
                var missing = requiredScopes.Where(scope => !token.Scope.Contains(scope)).ToList();
                if (missing.Count > 0)
                    return null;
            }

            // Refresh if token is expired or will expire in the next 60 seconds
            if (token.Remaining() < 60)
            {
                var client = new Client(_clientId, _clientSecret);
                var refreshed = await Common.OAuth.API.RefreshToken(token, client);
                if (refreshed == null)
                    return null;
                // Update token in memory
                token.AccessToken = refreshed.AccessToken;
                token.RefreshToken = refreshed.RefreshToken;
                token.TokenType = refreshed.TokenType;
                token.ExpiresIn = refreshed.ExpiresIn;
                token.CreatedAt = refreshed.CreatedAt;
                token.Scope = refreshed.Scope;
                // Update DB
                discordToken.AccessToken = token.AccessToken;
                discordToken.RefreshToken = token.RefreshToken;
                discordToken.ExpiresAt = token.CreatedAt.AddSeconds(token.ExpiresIn);
                discordToken.CreatedAt = token.CreatedAt;
                discordToken.Scope = string.Join(" ", token.Scope);
                await _db.SaveChangesAsync();
            }
            return token;
        }
    }
}


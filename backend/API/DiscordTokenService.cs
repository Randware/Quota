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
        private readonly string _botToken;

        public DiscordTokenService(QuotaContext db, string clientId, string clientSecret, string botToken)
        {
            _db = db;
            _clientId = clientId;
            _clientSecret = clientSecret;
            _botToken = botToken;
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
                Scope = new HashSet<string>((discordToken.Scope ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries))
            };

            // Scope validation
            var requiredScopesList = requiredScopes?.ToList();
            if (requiredScopesList != null && requiredScopesList.Count > 0)
            {
                var missing = requiredScopesList.Where(scope => !token.Scope.Contains(scope)).ToList();
                if (missing.Count > 0)
                    return null;
            }

            // Refresh if token is expired or will expire in the next 60 seconds
            if (token.Remaining() < 60)
            {
                var client = new Client(_clientId, _clientSecret, _botToken);
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

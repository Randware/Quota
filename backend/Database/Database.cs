using Microsoft.EntityFrameworkCore;
using Database.Model;
using Common.OAuth;
using Serilog;
using Serilog.Context;
using User = Database.Model.User;

namespace Database;

public class Storage
{
    private readonly QuotaContext _context;

    public Storage(QuotaContext context)
    {
        _context = context;
    }

    private async Task<T> LoggedOperation<T>(string operation, Func<Task<T>> action)
    {
        try
        {
            using (LogContext.PushProperty("SourceContext", "Database.Storage"))
            {
                return await action();
            }
        }
        catch (Exception ex)
        {
            using (LogContext.PushProperty("SourceContext", "Database.Storage"))
            {
                Log.Logger.Error(ex, "Error during {Operation}", operation);
                throw;
            }
        }
    }

    // USER CRUD
    public async Task<User?> GetUserByDiscordIdAsync(string discordId)
    {
        return await LoggedOperation("fetching user by DiscordID", async () =>
            await _context.Users
                .Include(u => u.QuoteeProfiles)
                .FirstOrDefaultAsync(u => u.DiscordID == discordId));
    }

    public async Task<User> CreateUserAsync(string discordId)
    {
        var user = new User { ID = Guid.NewGuid(), DiscordID = discordId };
        return await LoggedOperation("creating user", async () =>
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        });
    }

    public async Task<User?> UpdateUserAsync(User user)
    {
        return await LoggedOperation("updating user", async () =>
        {
            try
            {
                var existingUser = await _context.Users
                    .Include(u => u.QuoteeProfiles)
                    .FirstOrDefaultAsync(u => u.ID == user.ID);

                if (existingUser == null)
                {
                    Log.Logger.Warning("User {UserId} not found during update", user.ID);
                    return null;
                }

                // Update the existing user's properties
                _context.Entry(existingUser).CurrentValues.SetValues(user);

                // Handle quotee profiles
                foreach (var quotee in user.QuoteeProfiles)
                {
                    var existingQuotee = existingUser.QuoteeProfiles.FirstOrDefault(q => q.ID == quotee.ID);
                    if (existingQuotee == null)
                    {
                        existingUser.QuoteeProfiles.Add(quotee);
                    }
                }

                await _context.SaveChangesAsync();
                return existingUser;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                Log.Logger.Warning(ex, "Concurrency conflict while updating user {UserId}", user.ID);
                return null;
            }
        });
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        return await LoggedOperation("deleting user", async () =>
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        });
    }

    // QUOTE CRUD
    public async Task<Quote?> GetQuoteAsync(Guid quoteId)
    {
        return await LoggedOperation("fetching quote", async () =>
            await _context.Quotes.Include(q => q.QuoteQuotees).FirstOrDefaultAsync(q => q.ID == quoteId));
    }

    public async Task<Quote> CreateQuoteAsync(Quote quote)
    {
        return await LoggedOperation("creating quote", async () =>
        {
            quote.ID = Guid.NewGuid();
            _context.Quotes.Add(quote);
            await _context.SaveChangesAsync();
            return quote;
        });
    }

    public async Task<Quote?> UpdateQuoteAsync(Quote quote)
    {
        return await LoggedOperation("updating quote", async () =>
        {
            _context.Quotes.Update(quote);
            await _context.SaveChangesAsync();
            return quote;
        });
    }

    public async Task<bool> DeleteQuoteAsync(Guid quoteId)
    {
        return await LoggedOperation("deleting quote", async () =>
        {
            var quote = await _context.Quotes.FindAsync(quoteId);
            if (quote == null) return false;
            _context.Quotes.Remove(quote);
            await _context.SaveChangesAsync();
            return true;
        });
    }

    // SESSION CRUD
    public async Task<Session?> GetSessionAsync(Guid sessionId)
    {
        return await LoggedOperation("fetching session", async () =>
            await _context.Sessions.Include(s => s.Token).FirstOrDefaultAsync(s => s.ID == sessionId));
    }

    public async Task<Session> CreateSessionAsync(Session session)
    {
        return await LoggedOperation("creating session", async () =>
        {
            session.ID = Guid.NewGuid();
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        });
    }

    public async Task<Session?> UpdateSessionAsync(Session session)
    {
        return await LoggedOperation("updating session", async () =>
        {
            _context.Sessions.Update(session);
            await _context.SaveChangesAsync();
            return session;
        });
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId)
    {
        return await LoggedOperation("deleting session", async () =>
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null) return false;
            _context.Sessions.Remove(session);
            await _context.SaveChangesAsync();
            return true;
        });
    }

    public async Task<Session?> GetSessionByRefreshTokenAsync(string refreshToken)
    {
        return await LoggedOperation("fetching session by refresh token", async () =>
            await _context.Sessions.Include(s => s.Token)
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken));
    }

    public async Task<Session> CreateSessionAsync(Guid tokenId, string refreshToken, long expiresIn)
    {
        var session = new Session
        {
            ID = Guid.NewGuid(),
            TokenID = tokenId,
            RefreshToken = refreshToken,
            Revoked = false,
            ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn),
            CreatedAt = DateTime.UtcNow
        };
        return await LoggedOperation("creating session", async () =>
        {
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        });
    }

    // Revoke a specific session by refresh token
    public async Task<bool> RevokeSessionByRefreshTokenAsync(string refreshToken)
    {
        return await LoggedOperation("revoking session by refresh token", async () =>
        {
            var session = await _context.Sessions.FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);
            if (session == null) return false;
            session.Revoked = true;
            await _context.SaveChangesAsync();
            return true;
        });
    }

    // Revoke all sessions associated with a DiscordToken (by DiscordID)
    public async Task<int> RevokeAllSessionsByDiscordIdAsync(string discordId)
    {
        return await LoggedOperation("revoking all sessions by Discord ID", async () =>
        {
            var discordToken = await _context.Set<DiscordToken>().FirstOrDefaultAsync(dt => dt.DiscordID == discordId);
            if (discordToken == null) return 0;
            var sessions = _context.Sessions.Where(s => s.TokenID == discordToken.ID && !s.Revoked);
            int count = 0;
            await foreach (var session in sessions.AsAsyncEnumerable())
            {
                session.Revoked = true;
                count++;
            }
            await _context.SaveChangesAsync();
            return count;
        });
    }

    // GUILD CRUD
    public async Task<Guild?> GetGuildAsync(Guid guildId)
    {
        return await LoggedOperation("fetching guild", async () =>
            await _context.Guilds.Include(g => g.Config).FirstOrDefaultAsync(g => g.ID == guildId));
    }

    public async Task<Guild> CreateGuildAsync(Guild guild)
    {
        return await LoggedOperation("creating guild", async () =>
        {
            guild.ID = Guid.NewGuid();
            _context.Guilds.Add(guild);
            await _context.SaveChangesAsync();
            return guild;
        });
    }

    public async Task<Guild?> UpdateGuildAsync(Guild guild)
    {
        return await LoggedOperation("updating guild", async () =>
        {
            _context.Guilds.Update(guild);
            await _context.SaveChangesAsync();
            return guild;
        });
    }

    public async Task<bool> DeleteGuildAsync(Guid guildId)
    {
        return await LoggedOperation("deleting guild", async () =>
        {
            var guild = await _context.Guilds.FindAsync(guildId);
            if (guild == null) return false;
            _context.Guilds.Remove(guild);
            await _context.SaveChangesAsync();
            return true;
        });
    }

    public async Task<Guild?> GetGuildByDiscordIdAsync(string discordId)
    {
        return await LoggedOperation("fetching guild by Discord ID", async () =>
            await _context.Guilds
                .Include(g => g.Config)
                .ThenInclude(c => c.AllowedChannels)
                .Include(g => g.Config)
                .ThenInclude(c => c.Permissions)
                .FirstOrDefaultAsync(g => g.DiscordID == discordId));
    }

    // Returns all guilds with their configs and permissions included
    public async Task<List<Guild>> GetAllGuildsWithPermissionsAsync()
    {
        return await LoggedOperation("fetching all guilds with permissions", async () =>
            await _context.Guilds
                .Include(g => g.Config)
                .ThenInclude(cfg => cfg.Permissions)
                .ToListAsync());
    }

    // DISCORD TOKEN CRUD
    public async Task<DiscordToken?> GetDiscordTokenByDiscordIdAsync(string discordId)
    {
        return await LoggedOperation("fetching DiscordToken by DiscordID", async () =>
            await _context.Set<DiscordToken>().FirstOrDefaultAsync(dt => dt.DiscordID == discordId));
    }

    public async Task<DiscordToken> CreateDiscordTokenAsync(Common.OAuth.User discordUser, Common.OAuth.Token token)
    {
        var discordToken = new DiscordToken
        {
            ID = Guid.NewGuid(),
            DiscordID = discordUser.ID,
            AccessToken = token.AccessToken,
            RefreshToken = token.RefreshToken,
            ExpiresAt = token.CreatedAt.AddSeconds(token.ExpiresIn),
            CreatedAt = token.CreatedAt,
            Scope = string.Join(" ", token.Scope) // Store scope as space-separated string
        };
        return await LoggedOperation("creating DiscordToken", async () =>
        {
            _context.Set<DiscordToken>().Add(discordToken);
            await _context.SaveChangesAsync();
            return discordToken;
        });
    }

    public async Task<DiscordToken> UpdateDiscordTokenAsync(DiscordToken dbToken, Common.OAuth.Token token)
    {
        dbToken.AccessToken = token.AccessToken;
        dbToken.RefreshToken = token.RefreshToken;
        dbToken.ExpiresAt = token.CreatedAt.AddSeconds(token.ExpiresIn);
        dbToken.CreatedAt = token.CreatedAt;
        dbToken.Scope = string.Join(" ", token.Scope); // Update scope
        return await LoggedOperation("updating DiscordToken", async () =>
        {
            _context.Set<DiscordToken>().Update(dbToken);
            await _context.SaveChangesAsync();
            return dbToken;
        });
    }

    // Permission check: checks if the user has a given permission in any guild
    public async Task<bool> HasPermissionAsync(string discordId, PermissionType permissionType)
    {
        return await LoggedOperation("checking permissions", async () =>
        {
            // Find user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.DiscordID == discordId);
            if (user == null) return false;
            // Find all guilds and include config and permissions
            var guilds = await _context.Guilds
                .Include(g => g.Config)
                    .ThenInclude(cfg => cfg.Permissions)
                .ToListAsync();
            foreach (var guild in guilds)
            {
                if (guild.Config == null) continue;
                foreach (var perm in guild.Config.Permissions)
                {
                    if (perm.PermissionType == permissionType)
                        return true;
                }
            }
            return false;
        });
    }
}

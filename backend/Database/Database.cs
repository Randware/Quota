using Microsoft.EntityFrameworkCore;
using Database.Model;
using Common.OAuth;
using Serilog;
using User = Database.Model.User;

namespace Database;

public class Storage
{
    private readonly QuotaContext _context;

    public Storage(QuotaContext context)
    {
        _context = context;
    }

    // USER CRUD
    public async Task<User?> GetUserByDiscordIdAsync(string discordId)
    {
        try
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.DiscordID == discordId);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error fetching user by DiscordID");
            return null;
        }
    }

    public async Task<User> CreateUserAsync(string discordId)
    {
        var user = new User { ID = Guid.NewGuid(), DiscordID = discordId };
        try
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error creating user");
            throw;
        }
    }

    public async Task<User?> UpdateUserAsync(User user)
    {
        try
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return user;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error updating user");
            return null;
        }
    }

    public async Task<bool> DeleteUserAsync(Guid userId)
    {
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error deleting user");
            return false;
        }
    }

    // QUOTE CRUD
    public async Task<Quote?> GetQuoteAsync(Guid quoteId)
    {
        try
        {
            return await _context.Quotes.Include(q => q.QuoteQuotees).FirstOrDefaultAsync(q => q.ID == quoteId);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error fetching quote");
            return null;
        }
    }

    public async Task<Quote> CreateQuoteAsync(Quote quote)
    {
        try
        {
            quote.ID = Guid.NewGuid();
            _context.Quotes.Add(quote);
            await _context.SaveChangesAsync();
            return quote;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error creating quote");
            throw;
        }
    }

    public async Task<Quote?> UpdateQuoteAsync(Quote quote)
    {
        try
        {
            _context.Quotes.Update(quote);
            await _context.SaveChangesAsync();
            return quote;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error updating quote");
            return null;
        }
    }

    public async Task<bool> DeleteQuoteAsync(Guid quoteId)
    {
        try
        {
            var quote = await _context.Quotes.FindAsync(quoteId);
            if (quote == null) return false;
            _context.Quotes.Remove(quote);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error deleting quote");
            return false;
        }
    }

    // SESSION CRUD
    public async Task<Session?> GetSessionAsync(Guid sessionId)
    {
        try
        {
            return await _context.Sessions.Include(s => s.Token).FirstOrDefaultAsync(s => s.ID == sessionId);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error fetching session");
            return null;
        }
    }

    public async Task<Session> CreateSessionAsync(Session session)
    {
        try
        {
            session.ID = Guid.NewGuid();
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error creating session");
            throw;
        }
    }

    public async Task<Session?> UpdateSessionAsync(Session session)
    {
        try
        {
            _context.Sessions.Update(session);
            await _context.SaveChangesAsync();
            return session;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error updating session");
            return null;
        }
    }

    public async Task<bool> DeleteSessionAsync(Guid sessionId)
    {
        try
        {
            var session = await _context.Sessions.FindAsync(sessionId);
            if (session == null) return false;
            _context.Sessions.Remove(session);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error deleting session");
            return false;
        }
    }

    public async Task<Session?> GetSessionByRefreshTokenAsync(string refreshToken)
    {
        try
        {
            return await _context.Sessions.Include(s => s.Token)
                .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error fetching session by refresh token");
            return null;
        }
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
        try
        {
            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();
            return session;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error creating session");
            throw;
        }
    }

    // GUILD CRUD
    public async Task<Guild?> GetGuildAsync(Guid guildId)
    {
        try
        {
            return await _context.Guilds.Include(g => g.Config).FirstOrDefaultAsync(g => g.ID == guildId);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error fetching guild");
            return null;
        }
    }

    public async Task<Guild> CreateGuildAsync(Guild guild)
    {
        try
        {
            guild.ID = Guid.NewGuid();
            _context.Guilds.Add(guild);
            await _context.SaveChangesAsync();
            return guild;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error creating guild");
            throw;
        }
    }

    public async Task<Guild?> UpdateGuildAsync(Guild guild)
    {
        try
        {
            _context.Guilds.Update(guild);
            await _context.SaveChangesAsync();
            return guild;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error updating guild");
            return null;
        }
    }

    public async Task<bool> DeleteGuildAsync(Guid guildId)
    {
        try
        {
            var guild = await _context.Guilds.FindAsync(guildId);
            if (guild == null) return false;
            _context.Guilds.Remove(guild);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error deleting guild");
            return false;
        }
    }

    // DISCORD TOKEN CRUD
    public async Task<DiscordToken?> GetDiscordTokenByDiscordIdAsync(string discordId)
    {
        try
        {
            return await _context.Set<DiscordToken>().FirstOrDefaultAsync(dt => dt.DiscordID == discordId);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error fetching DiscordToken by DiscordID");
            return null;
        }
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
            CreatedAt = token.CreatedAt
        };
        try
        {
            _context.Set<DiscordToken>().Add(discordToken);
            await _context.SaveChangesAsync();
            return discordToken;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error creating DiscordToken");
            throw;
        }
    }

    public async Task<DiscordToken> UpdateDiscordTokenAsync(DiscordToken dbToken, Common.OAuth.Token token)
    {
        dbToken.AccessToken = token.AccessToken;
        dbToken.RefreshToken = token.RefreshToken;
        dbToken.ExpiresAt = token.CreatedAt.AddSeconds(token.ExpiresIn);
        dbToken.CreatedAt = token.CreatedAt;
        try
        {
            _context.Set<DiscordToken>().Update(dbToken);
            await _context.SaveChangesAsync();
            return dbToken;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error updating DiscordToken");
            throw;
        }
    }

    // Permission check: checks if the user has a given permission in any guild
    public async Task<bool> HasPermissionAsync(string discordId, PermissionType permissionType)
    {
        try
        {
            // Find user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.DiscordID == discordId);
            if (user == null) return false;
            // Find all guilds
            var guilds = await _context.Guilds.Include(g => g.Permissions).ToListAsync();
            foreach (var guild in guilds)
            {
                foreach (var perm in guild.Permissions)
                {
                    if (perm.PermissionType == permissionType)
                        return true;
                }
            }
            return false;
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, "Error checking permissions");
            return false;
        }
    }
}

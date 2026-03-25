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

        /// <summary>
        /// Gets all custom emotes for a specific Discord server (guild).
        /// </summary>
        /// <param name="id">The Discord guild ID</param>
        /// <returns>List of custom emotes in the specified format</returns>
        /// <response code="200">Returns the list of custom emotes</response>
        /// <response code="401">If the JWT is missing, invalid, or the guild is not allowed</response>
        [HttpGet("emotes")]
        [Authorize]
        public IActionResult GetGuildEmotes([FromRoute] string id)
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

            if (!ulong.TryParse(id, out var guildId))
                return BadRequest("Invalid guild id");

            // Fetch emotes using the static method in Bot.QuotaBot
            var emotes = Bot.QuotaBot.GetCustomEmotesForGuild(guildId);
            return Ok(emotes);
        }
        /// <summary>
        /// Gets statistics for a specific Discord server (guild).
        /// </summary>
        /// <param name="id">The Discord guild ID</param>
        /// <returns>Statistics including quote counts, votes, top quotees, and quotes over time</returns>
        /// <response code="200">Returns the guild statistics</response>
        /// <response code="401">If the JWT is missing, invalid, or the guild is not allowed</response>
        [HttpGet("stats")]
        [Authorize]
        public async Task<IActionResult> GetGuildStats([FromRoute] string id)
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
                .FirstOrDefaultAsync(g => g.DiscordID == id);
            if (dbGuild == null)
                return NotFound("Guild not found");

            var quotes = _db.Quotes.Where(q => q.GuildID == dbGuild.ID);

            var totalQuotes = await quotes.CountAsync();
            var totalUpvotes = await quotes.SumAsync(q => q.Upvotes);
            var totalDownvotes = await quotes.SumAsync(q => q.Downvotes);

            // Quotes by month (last 12 months)
            var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
            var quotesByMonth = await quotes
                .Where(q => q.CreatedAt != null && q.CreatedAt >= twelveMonthsAgo)
                .GroupBy(q => new { q.CreatedAt!.Value.Year, q.CreatedAt!.Value.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            // Fill in missing months
            var monthlyData = new List<object>();
            for (int i = 11; i >= 0; i--)
            {
                var date = DateTime.UtcNow.AddMonths(-i);
                var match = quotesByMonth.FirstOrDefault(m => m.Year == date.Year && m.Month == date.Month);
                monthlyData.Add(new { year = date.Year, month = date.Month, count = match?.Count ?? 0 });
            }

            // Top 5 quotees
            var topQuotees = await _db.QuoteQuotees
                .Where(qq => qq.Quote.GuildID == dbGuild.ID)
                .GroupBy(qq => qq.Quotee.Name)
                .Select(g => new { name = g.Key, count = g.Count() })
                .OrderByDescending(g => g.count)
                .Take(5)
                .ToListAsync();

            // Top 5 quotes by score (upvotes - downvotes)
            var topQuotes = await quotes
                .OrderByDescending(q => q.Upvotes - q.Downvotes)
                .Take(5)
                .Select(q => new
                {
                    content = q.Content ?? "",
                    upvotes = q.Upvotes,
                    downvotes = q.Downvotes,
                    score = q.Upvotes - q.Downvotes,
                    createdAt = q.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                totalQuotes,
                totalUpvotes,
                totalDownvotes,
                quotesByMonth = monthlyData,
                topQuotees,
                topQuotes
            });
        }

        // ========================
        // QUOTE MANAGEMENT ENDPOINTS
        // ========================

        /// <summary>
        /// Lists quotes for a guild with pagination and optional filtering.
        /// </summary>
        [HttpGet("quotes")]
        [Authorize]
        public async Task<IActionResult> GetGuildQuotes(
            [FromRoute] string id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? search = null)
        {
            var jwtUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(jwtUserId))
                return Unauthorized("Missing user id in token");

            var allowedGuilds = User.FindFirst("allowed_guilds")?.Value;
            if (allowedGuilds == null || !allowedGuilds.Split(',').Contains(id))
                return Unauthorized("You do not have access to this guild.");

            var dbGuild = await _db.Guilds.FirstOrDefaultAsync(g => g.DiscordID == id);
            if (dbGuild == null)
                return NotFound("Guild not found");

            var query = _db.Quotes
                .Where(q => q.GuildID == dbGuild.ID)
                .Include(q => q.QuoteQuotees)
                    .ThenInclude(qq => qq.Quotee)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(q =>
                    (q.Content != null && q.Content.Contains(search)) ||
                    q.QuoteQuotees.Any(qq => qq.Quotee.Name.Contains(search)));
            }

            var totalCount = await query.CountAsync();

            var quotes = await query
                .OrderByDescending(q => q.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(q => new
                {
                    id = q.ID,
                    content = q.Content ?? "",
                    mediaUrls = q.MediaUrls,
                    upvotes = q.Upvotes,
                    downvotes = q.Downvotes,
                    score = q.Upvotes - q.Downvotes,
                    createdAt = q.CreatedAt,
                    messageId = q.MessageID,
                    channelId = q.ChannelID,
                    quotees = q.QuoteQuotees.Select(qq => qq.Quotee.Name).ToList()
                })
                .ToListAsync();

            return Ok(new
            {
                quotes,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        /// <summary>
        /// Tries to find which channel a message is in by searching guild text channels.
        /// Caches the result on the quote's ChannelID field.
        /// </summary>
        private async Task<string?> DiscoverChannelForMessage(string guildDiscordId, string messageId, Quote quote)
        {
            // If we already have ChannelID, return it
            if (!string.IsNullOrEmpty(quote.ChannelID))
                return quote.ChannelID;

            try
            {
                using var httpClient = new HttpClient();
                httpClient.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bot", _discordClient.BotToken);

                // Get guild channels
                var channelsRes = await httpClient.GetAsync($"https://discord.com/api/v10/guilds/{guildDiscordId}/channels");
                if (!channelsRes.IsSuccessStatusCode) return null;

                var channelsJson = await channelsRes.Content.ReadAsStringAsync();
                using var doc = System.Text.Json.JsonDocument.Parse(channelsJson);

                foreach (var channel in doc.RootElement.EnumerateArray())
                {
                    // Only check text channels (type 0) and announcement channels (type 5)
                    var type = channel.GetProperty("type").GetInt32();
                    if (type != 0 && type != 5) continue;

                    var channelId = channel.GetProperty("id").GetString();
                    if (string.IsNullOrEmpty(channelId)) continue;

                    // Try to fetch the message from this channel
                    var msgRes = await httpClient.GetAsync(
                        $"https://discord.com/api/v10/channels/{channelId}/messages/{messageId}");

                    if (msgRes.IsSuccessStatusCode)
                    {
                        // Found it! Cache the ChannelID
                        quote.ChannelID = channelId;
                        _db.Quotes.Update(quote);
                        await _db.SaveChangesAsync();
                        Log.Logger.Information("Discovered ChannelID {ChannelId} for quote {QuoteId}", channelId, quote.ID);
                        return channelId;
                    }

                    // Small delay to avoid rate limiting
                    await Task.Delay(50);
                }
            }
            catch (Exception ex)
            {
                Log.Logger.Warning(ex, "Failed to discover channel for message {MessageId}", messageId);
            }

            return null;
        }

        /// <summary>
        /// Edits a quote's content and/or quotees, and updates the Discord message.
        /// </summary>
        [HttpPut("quote/{quoteId}")]
        [Authorize]
        public async Task<IActionResult> EditQuote(
            [FromRoute] string id,
            [FromRoute] Guid quoteId,
            [FromBody] System.Text.Json.JsonElement body)
        {
            var jwtUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(jwtUserId))
                return Unauthorized("Missing user id in token");

            var allowedGuilds = User.FindFirst("allowed_guilds")?.Value;
            if (allowedGuilds == null || !allowedGuilds.Split(',').Contains(id))
                return Unauthorized("You do not have access to this guild.");

            var dbGuild = await _db.Guilds.FirstOrDefaultAsync(g => g.DiscordID == id);
            if (dbGuild == null)
                return NotFound("Guild not found");

            var quote = await _db.Quotes
                .Include(q => q.QuoteQuotees)
                    .ThenInclude(qq => qq.Quotee)
                .FirstOrDefaultAsync(q => q.ID == quoteId && q.GuildID == dbGuild.ID);
            if (quote == null)
                return NotFound("Quote not found");

            // Update content if provided
            if (body.TryGetProperty("content", out var contentProp))
            {
                var newContent = contentProp.GetString();
                if (!string.IsNullOrWhiteSpace(newContent))
                    quote.Content = newContent;
            }

            // Update quotees if provided
            if (body.TryGetProperty("quotees", out var quoteesProp) && quoteesProp.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                var newQuoteeNames = quoteesProp.EnumerateArray()
                    .Select(q => q.GetString())
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToList();

                if (newQuoteeNames.Any())
                {
                    // Remove old QuoteQuotee links
                    var oldLinks = _db.Set<QuoteQuotee>().Where(qq => qq.QuoteID == quoteId);
                    _db.Set<QuoteQuotee>().RemoveRange(oldLinks);
                    await _db.SaveChangesAsync();

                    // Create or find quotees and link them
                    foreach (var name in newQuoteeNames)
                    {
                        var quotee = await _db.Quotees.FirstOrDefaultAsync(q => q.Name == name);
                        if (quotee == null)
                        {
                            quotee = new Quotee { ID = Guid.NewGuid(), Name = name! };
                            _db.Quotees.Add(quotee);
                        }
                        _db.Set<QuoteQuotee>().Add(new QuoteQuotee
                        {
                            QuoteID = quoteId,
                            QuoteeID = quotee.ID
                        });
                    }
                }
            }

            _db.Quotes.Update(quote);
            await _db.SaveChangesAsync();

            // Try to update the Discord message
            bool discordUpdated = false;
            if (!string.IsNullOrEmpty(quote.MessageID))
            {
                var channelId = await DiscoverChannelForMessage(id, quote.MessageID, quote);
                if (!string.IsNullOrEmpty(channelId))
                {
                    try
                    {
                        // Reload quotees after potential update
                        await _db.Entry(quote).Collection(q => q.QuoteQuotees).Query()
                            .Include(qq => qq.Quotee).LoadAsync();

                        var quoteeNames = quote.QuoteQuotees?.Select(qq => qq.Quotee.Name).ToList() ?? new List<string>();
                        var quoteeDisplay = quoteeNames.Any() ? string.Join(", ", quoteeNames) : "Unknown";

                        var displayContent = !string.IsNullOrWhiteSpace(quote.Content) ? $"*\"{quote.Content}\"*" : "";
                        var embedJson = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            embeds = new[]
                            {
                                new
                                {
                                    author = new { name = $"Quote from {quoteeDisplay}" },
                                    description = displayContent,
                                    color = 0x3498DB,
                                    timestamp = quote.CreatedAt?.ToString("o")
                                }
                            }
                        });

                        using var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bot", _discordClient.BotToken);
                        var response = await httpClient.PatchAsync(
                            $"https://discord.com/api/v10/channels/{channelId}/messages/{quote.MessageID}",
                            new StringContent(embedJson, System.Text.Encoding.UTF8, "application/json"));

                        discordUpdated = response.IsSuccessStatusCode;
                        if (!response.IsSuccessStatusCode)
                        {
                            var errBody = await response.Content.ReadAsStringAsync();
                            Log.Logger.Warning("Discord API returned {Status} when editing message: {Body}", response.StatusCode, errBody);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Warning(ex, "Failed to update Discord message for quote {QuoteId}", quoteId);
                    }
                }
            }

            return Ok(new { success = true, discordUpdated });
        }

        /// <summary>
        /// Deletes a quote from the database and the Discord message.
        /// </summary>
        [HttpDelete("quote/{quoteId}")]
        [Authorize]
        public async Task<IActionResult> DeleteQuote(
            [FromRoute] string id,
            [FromRoute] Guid quoteId)
        {
            var jwtUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value;
            if (string.IsNullOrEmpty(jwtUserId))
                return Unauthorized("Missing user id in token");

            var allowedGuilds = User.FindFirst("allowed_guilds")?.Value;
            if (allowedGuilds == null || !allowedGuilds.Split(',').Contains(id))
                return Unauthorized("You do not have access to this guild.");

            var dbGuild = await _db.Guilds.FirstOrDefaultAsync(g => g.DiscordID == id);
            if (dbGuild == null)
                return NotFound("Guild not found");

            var quote = await _db.Quotes
                .FirstOrDefaultAsync(q => q.ID == quoteId && q.GuildID == dbGuild.ID);
            if (quote == null)
                return NotFound("Quote not found");

            // Try to delete the Discord message
            bool discordDeleted = false;
            if (!string.IsNullOrEmpty(quote.MessageID))
            {
                var channelId = await DiscoverChannelForMessage(id, quote.MessageID, quote);
                if (!string.IsNullOrEmpty(channelId))
                {
                    try
                    {
                        using var httpClient = new HttpClient();
                        httpClient.DefaultRequestHeaders.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bot", _discordClient.BotToken);
                        var response = await httpClient.DeleteAsync(
                            $"https://discord.com/api/v10/channels/{channelId}/messages/{quote.MessageID}");

                        discordDeleted = response.IsSuccessStatusCode;
                    }
                    catch (Exception ex)
                    {
                        Log.Logger.Warning(ex, "Failed to delete Discord message for quote {QuoteId}", quoteId);
                    }
                }
            }

            // Delete related records
            var relatedQuotees = _db.Set<QuoteQuotee>().Where(qq => qq.QuoteID == quoteId);
            _db.Set<QuoteQuotee>().RemoveRange(relatedQuotees);

            var relatedVotes = _db.Set<QuoteVote>().Where(qv => qv.QuoteID == quoteId);
            _db.Set<QuoteVote>().RemoveRange(relatedVotes);

            _db.Quotes.Remove(quote);
            await _db.SaveChangesAsync();

            return Ok(new { success = true, discordDeleted });
        }
    }
}


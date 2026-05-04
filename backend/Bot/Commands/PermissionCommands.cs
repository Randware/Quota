using Discord;
using Discord.Interactions;
using Database;
using Database.Model;
using Microsoft.EntityFrameworkCore;

namespace Bot;

[Group("permissions", "Manage bot permissions for users and roles")]
[RequireUserPermission(GuildPermission.Administrator)]
public class PermissionCommands : InteractionModuleBase<SocketInteractionContext>
{
    private readonly QuotaContext _db;

    public PermissionCommands(QuotaContext dbContext)
    {
        _db = dbContext;
    }

    [SlashCommand("grant", "Grant a permission to a user or role")]
    public async Task GrantAsync(
        [Summary("type", "The permission type to grant")]
        [Choice("Dashboard", "DASHBOARD")]
        [Choice("Admin", "ADMIN")]
        [Choice("Manage Quotes", "MANAGE_QUOTES")]
        [Choice("Create Quotes", "CREATE_QUOTES")]
        [Choice("Read Quotes", "READ_QUOTES")]
        string permissionType,
        [Summary("user", "The user to grant the permission to")] IUser? user = null,
        [Summary("role", "The role to grant the permission to")] IRole? role = null)
    {
        await DeferAsync(ephemeral: true);

        if (user == null && role == null)
        {
            await FollowupAsync("You must specify either a user or a role.", ephemeral: true);
            return;
        }

        var dbGuild = await _db.Guilds
            .Include(g => g.Config)
            .ThenInclude(c => c.Permissions)
            .FirstOrDefaultAsync(g => g.DiscordID == Context.Guild.Id.ToString());

        if (dbGuild?.Config == null)
        {
            await FollowupAsync("Guild not configured. Run a quote first to initialize.", ephemeral: true);
            return;
        }

        var pType = Enum.Parse<PermissionType>(permissionType);
        var targetUserId = user?.Id.ToString();
        var targetRoleId = role?.Id.ToString();

        // Check if permission already exists
        var existing = dbGuild.Config.Permissions.FirstOrDefault(p =>
            p.PermissionType == pType &&
            (targetUserId != null ? p.UserID == targetUserId : p.RoleID == targetRoleId));

        if (existing != null)
        {
            var target = user != null ? $"<@{user.Id}>" : $"<@&{role!.Id}>";
            await FollowupAsync($"{target} already has **{permissionType}** permission.", ephemeral: true);
            return;
        }

        _db.Permissions.Add(new Permission
        {
            GuildConfigID = dbGuild.Config.ID,
            UserID = targetUserId,
            RoleID = targetRoleId,
            PermissionType = pType
        });

        await _db.SaveChangesAsync();

        var targetName = user != null ? $"<@{user.Id}>" : $"<@&{role!.Id}>";
        await FollowupAsync($"✅ Granted **{permissionType}** to {targetName}.", ephemeral: true);
    }

    [SlashCommand("revoke", "Revoke a permission from a user or role")]
    public async Task RevokeAsync(
        [Summary("type", "The permission type to revoke")]
        [Choice("Dashboard", "DASHBOARD")]
        [Choice("Admin", "ADMIN")]
        [Choice("Manage Quotes", "MANAGE_QUOTES")]
        [Choice("Create Quotes", "CREATE_QUOTES")]
        [Choice("Read Quotes", "READ_QUOTES")]
        string permissionType,
        [Summary("user", "The user to revoke the permission from")] IUser? user = null,
        [Summary("role", "The role to revoke the permission from")] IRole? role = null)
    {
        await DeferAsync(ephemeral: true);

        if (user == null && role == null)
        {
            await FollowupAsync("You must specify either a user or a role.", ephemeral: true);
            return;
        }

        var dbGuild = await _db.Guilds
            .Include(g => g.Config)
            .ThenInclude(c => c.Permissions)
            .FirstOrDefaultAsync(g => g.DiscordID == Context.Guild.Id.ToString());

        if (dbGuild?.Config == null)
        {
            await FollowupAsync("Guild not configured.", ephemeral: true);
            return;
        }

        var pType = Enum.Parse<PermissionType>(permissionType);
        var targetUserId = user?.Id.ToString();
        var targetRoleId = role?.Id.ToString();

        var existing = dbGuild.Config.Permissions.FirstOrDefault(p =>
            p.PermissionType == pType &&
            (targetUserId != null ? p.UserID == targetUserId : p.RoleID == targetRoleId));

        if (existing == null)
        {
            var target = user != null ? $"<@{user.Id}>" : $"<@&{role!.Id}>";
            await FollowupAsync($"{target} doesn't have **{permissionType}** permission.", ephemeral: true);
            return;
        }

        _db.Permissions.Remove(existing);
        await _db.SaveChangesAsync();

        var targetName = user != null ? $"<@{user.Id}>" : $"<@&{role!.Id}>";
        await FollowupAsync($"✅ Revoked **{permissionType}** from {targetName}.", ephemeral: true);
    }

    [SlashCommand("list", "List all permissions for this server")]
    public async Task ListAsync()
    {
        await DeferAsync(ephemeral: true);

        var dbGuild = await _db.Guilds
            .Include(g => g.Config)
            .ThenInclude(c => c.Permissions)
            .FirstOrDefaultAsync(g => g.DiscordID == Context.Guild.Id.ToString());

        if (dbGuild?.Config == null || dbGuild.Config.Permissions.Count == 0)
        {
            await FollowupAsync("No permissions configured for this server.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("🛡️ Server Permissions")
            .WithColor(new Color(88, 101, 242)) // Discord blurple
            .WithTimestamp(DateTimeOffset.Now);

        // Group by permission type
        var grouped = dbGuild.Config.Permissions
            .GroupBy(p => p.PermissionType)
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
        {
            var entries = group.Select(p =>
            {
                if (p.UserID != null) return $"<@{p.UserID}>";
                if (p.RoleID != null) return $"<@&{p.RoleID}>";
                return "Unknown";
            });

            embed.AddField(
                $"{group.Key}",
                string.Join(", ", entries),
                inline: false
            );
        }

        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }
}

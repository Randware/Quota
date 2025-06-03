namespace Database.Model;

public enum PermissionType
{
    DASHBOARD,
    ADMIN,
    MANAGE_QUOTES,
    CREATE_QUOTES,
    VIEW_QUOTES,
}

public class Permission
{
    public int ID { get; set; } // Surrogate primary key
    public Guid GuildConfigID { get; set; }
    public GuildConfig GuildConfig { get; set; }

    public string? UserID { get; set; } // Discord user ID (nullable)
    public string? RoleID { get; set; } // Discord role ID (nullable)
    public PermissionType PermissionType { get; set; }
}

namespace Model;

public enum PermissionType
{
    SETTINGS,
    CREATE,
    DELETE,
    GET
}

public class Permission
{
    public Guid GuildConfigID { get; set; }
    public GuildConfig GuildConfig { get; set; }

    public string Role { get; set; }
    public PermissionType PermissionType { get; set; }
}


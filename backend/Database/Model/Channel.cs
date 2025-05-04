namespace Model;

public class AllowedChannel
{
    public Guid GuildConfigID { get; set; }
    public GuildConfig GuildConfig { get; set; }

    public string Channel { get; set; }
}

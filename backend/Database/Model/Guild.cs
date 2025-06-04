namespace Database.Model;

public class Guild
{
    public Guid ID { get; set; }
    public string DiscordID { get; set; }

    public Guid ConfigID { get; set; }
    public GuildConfig Config { get; set; }

    public ICollection<Quote> Quotes { get; set; }
}

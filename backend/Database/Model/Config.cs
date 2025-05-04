namespace Model;

public class GuildConfig
{
    public Guid ID { get; set; }
    public string UpvoteEmoji { get; set; }
    public string DownvoteEmoji { get; set; }
    public bool LockAllowedChannels { get; set; }
    public bool Comments { get; set; }

    public Guild Guild { get; set; }
}

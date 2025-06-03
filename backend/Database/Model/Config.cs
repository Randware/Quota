namespace Database.Model;

public class GuildConfig
{
    public Guid ID { get; set; }
    public string UpvoteEmoji { get; set; }
    public string DownvoteEmoji { get; set; }
    public bool AllowVoting { get; set; }
    public bool LockAllowedChannels { get; set; }
    public bool Comments { get; set; }
    //TODO: discuss if there should be another option to allow comments for a certain time

    public Guild Guild { get; set; }
    public ICollection<AllowedChannel> AllowedChannels { get; set; } = new List<AllowedChannel>();
    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}

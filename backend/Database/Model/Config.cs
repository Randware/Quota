namespace Database.Model;

public class EmojiConfig
{
    public string Type { get; set; } // "unicode" or "custom"
    public string? Id { get; set; } // Only for custom
    public string Name { get; set; } // Name or unicode char
    public bool Animated { get; set; } = false;

    public bool IsCustom => Type == "custom" && !string.IsNullOrEmpty(Id);

    public string? ToFrontendUrl(UInt16 size = 128)
    {
        if (!IsCustom) return null;
        var ext = Animated ? "gif" : "webp";
        return $"https://cdn.discordapp.com/emojis/{Id}.{ext}?size={size}";
    }   
}

public class GuildConfig
{
    public Guid ID { get; set; }
    public EmojiConfig UpvoteEmojiConfig { get; set; }
    public EmojiConfig DownvoteEmojiConfig { get; set; }
    public bool AllowVoting { get; set; }
    public bool LockAllowedChannels { get; set; }
    public bool Comments { get; set; }
    //TODO: discuss if there should be another option to allow comments for a certain time

    public Guild Guild { get; set; }
    public ICollection<AllowedChannel> AllowedChannels { get; set; } = new List<AllowedChannel>();
    public ICollection<Permission> Permissions { get; set; } = new List<Permission>();
}

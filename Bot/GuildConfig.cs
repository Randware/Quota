using Discord;

namespace Bot;

public struct GuildConfig
{
    public Emojis Emoji { get; set; }
    public Permissions Permission { get; set; }
    public AccessManagment AccessManagment { get; set; }
    public Threads Threads { get; set; }


    public GuildConfig(Emojis emojis, Permissions permissions, AccessManagment accessManagment, Threads threads)
    {
        this.Emoji = emojis;
        this.Permission = permissions;
        this.AccessManagment = accessManagment;
        this.Threads = threads;
    }
    public GuildConfig() : this(new Emojis(), new Permissions(), new AccessManagment(), new Threads()) { }

}

//TODO: Somehow validate the IEmote
public struct Emojis
{

    public Discord.IEmote Upvote { get; set; }
    public Discord.IEmote Downvote { get; set; }

    public Emojis() : this(Discord.Emoji.Parse("👍"), Discord.Emoji.Parse("👎"))
    {

    }

    public Emojis(Discord.IEmote upvote, Discord.IEmote downvote)
    {


        Upvote = upvote;
        Downvote = downvote;
    }
}

public struct Permissions
{

}


public struct AccessManagment
{
    public List<Discord.ITextChannel> AllowedChannels { get; set; }
    public bool LockAllowedChannels { get; set; }

    public AccessManagment(bool lockAllowedChannels, params Discord.ITextChannel[] channels)
    {
        LockAllowedChannels = lockAllowedChannels;
        AllowedChannels = new();
        AllowedChannels.AddRange(channels);

    }

    public AccessManagment() : this(false)
    {

    }
}

public struct Threads
{
    public bool Enabled { get; set; }
    /* TODO:
     * The naming convetion of the different Threads that get created.
     * Supports variables like:
     *    {id}  - The unique identifier of the Thread
     *    {submitter} - The person who submitted the quote
     *    {content} - The content of the quote
     *    {from}  - The Provided quotee
     */
    public string Names { get; set; }
    public Threads(bool enabled, string names)
    {
        Enabled = enabled;
        Names = names;
    }

    public Threads() : this(false, "Discussion-{id}")
    {

    }

    public Threads Parse(string id = "", string submitter = "", string from = "", string content = "")
    {
        Names.Replace("{id}", id)
          .Replace("{submitter}", submitter)
          .Replace("{from}", from)
          .Replace("{content}", content);

        return this;
    }
}

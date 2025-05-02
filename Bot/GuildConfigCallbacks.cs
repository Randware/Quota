namespace Bot;

public struct ConfigCallback
{


}

public interface IEmoteConfigCallback
{
    public void EnabledCallback(bool old, bool current);
    public void UpvoteCallback(Discord.IEmote previouse, Discord.IEmote current);
    public void DownvoteCallback(Discord.IEmote previouse, Discord.IEmote current);
}

public interface IAccessManagementCallback
{
    public void AllowedChannelsCallback(List<Discord.ITextChannel> previouse, List<Discord.ITextChannel> current);
    public void LockAllowedChannelsCallback();
}

public interface IThreads
{
    public void EnabledCallback(bool previouse, bool current);
    public void NameCallack(string previouse, bool current);
}

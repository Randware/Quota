namespace Bot;

public struct ConfigCallback
{


}

public interface IEmoteConfigCallback
{
    public void EnabledCallback(bool old, bool current);
    public void UpvoteCallback(Discord.IEmote old, Discord.IEmote current);
    public void DownvoteCallback(Discord.IEmote old, Discord.IEmote current);
}

public interface IAccessManagementCallback
{
    public void AllowedChannelsCallback(List<Discord.ITextChannel> old, List<Discord.ITextChannel> current);
    public void LockAllowedChannelsCallback();
}

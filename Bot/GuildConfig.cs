using System.Text.Json;
using System.Text.Json.Serialization;
using Discord;

namespace Bot;

public struct GuildConfig
{
    [JsonPropertyName("emoji")]
    public Emojis Emoji { get; set; }
    [JsonPropertyName("permission")]
    public Dictionary<string, List<Permissions>> Permissions { get; set; }
    [JsonPropertyName("accessManagment")]
    public AccessManagment AccessManagment { get; set; }
    [JsonPropertyName("threads")]
    public Threads Threads { get; set; }


    public GuildConfig(Emojis emojis, Dictionary<string, List<Permissions>> permissions, AccessManagment accessManagment, Threads threads)
    {
        this.Emoji = emojis;
        this.Permissions = permissions;
        this.AccessManagment = accessManagment;
        this.Threads = threads;
    }
    public GuildConfig() : this(new Emojis(), new Dictionary<string, List<Bot.Permissions>> { { "everyone", new List<Bot.Permissions>() { Bot.Permissions.GET } } }, new AccessManagment(), new Threads()) { }

}

//TODO: Somehow validate the IEmote
public struct Emojis
{

    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
    [JsonPropertyName("upvote")]
    public Discord.IEmote Upvote { get; set; }
    [JsonPropertyName("downvote")]
    public Discord.IEmote Downvote { get; set; }

    public Emojis() : this(Discord.Emoji.Parse("👍"), Discord.Emoji.Parse("👎"), true)
    {

    }

    public Emojis(Discord.IEmote upvote, Discord.IEmote downvote, bool enabled)
    {
        Upvote = upvote;
        Downvote = downvote;
        Enabled = enabled;
    }
}

internal class EmoteConverter() : JsonConverter<Discord.IEmote>
{
    public override IEmote Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var emoteString = reader.GetString();
        return Emoji.TryParse(emoteString, out var emoji)
            ? emoji
            : Emote.TryParse(emoteString, out var customEmote)
                ? customEmote
                : throw new JsonException($"Invalid IEmote format in the Config: {emoteString}");
    }

    public override void Write(Utf8JsonWriter writer, IEmote value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/*public struct Permissions*/
/*{*/
/*    public Dictionary<string, List<Permission>> RolePermissions { get; set; }*/
/**/
/*    public Permissions(Dictionary<string, List<Permission>> rolePermissions)*/
/*    {*/
/*        RolePermissions = rolePermissions;*/
/*    }*/
/**/
/*    public Permissions() : this(new Dictionary<string, List<Permission>> { { "everyone", new List<Permission>() { Permission.GET } } })*/
/*    {*/
/**/
/*    }*/
/*}*/


public struct AccessManagment
{
    [JsonPropertyName("allowedChannels")]
    public List<Discord.ITextChannel> AllowedChannels { get; set; }
    [JsonPropertyName("lockAllowedChannels")]
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
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }
    /* TODO:
     * The naming convetion of the different Threads that get created.
     * Supports variables like:
     *    {id}  - The unique identifier of the Thread
     *    {submitter} - The person who submitted the quote
     *    {content} - The content of the quote
     *    {from}  - The Provided quotee
     */
    [JsonPropertyName("names")]
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

public enum Permissions
{
    SETTINGS,
    CREATE,
    DELETE,
    GET,

}

public class PermissionsConverter : JsonConverter<Permissions>
{
    public override Permissions Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var strEnumValue = reader.GetString();
        if (strEnumValue == null || !Enum.TryParse(strEnumValue, true, out Permissions result))
        {
            throw new JsonException($"Invalid value '{strEnumValue}' for the permissions!");
        }
        return result;
    }

    public override void Write(Utf8JsonWriter writer, Permissions value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToLower());
    }
}

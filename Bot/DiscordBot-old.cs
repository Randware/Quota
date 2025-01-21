using System.Xml;
using System.Xml.Linq;
using Discord;
using Discord.Net;
using Discord.WebSocket;

namespace Bot;

public class DiscordOld
{

    public const string credentialsPath = "./credentials.xml";
    private const string defaultPlaceholder = "YOUR DISCORD BOT TOKEN HERE";

    private static DiscordSocketClient _client;
    public static async Task MainOld()
    {
        var _config = new DiscordSocketConfig
        {
            MessageCacheSize = 100,
            GatewayIntents = GatewayIntents.All,

        };
        _client = new DiscordSocketClient(_config);
        _client.Log += Log;

        XmlDocument doc = new XmlDocument();
        if (File.Exists(credentialsPath))
        {
            doc.Load(credentialsPath);
        }
        else
        {
            XElement content = new XElement("Discord", new XElement("token", defaultPlaceholder));
            XmlNode node = doc.ReadNode(content.CreateReader());
            doc.AppendChild(node);
            doc.Save(credentialsPath);
        }

        var token = doc.DocumentElement?.SelectSingleNode("/Discord/token")?.InnerText;


        if (token == defaultPlaceholder)
        {
            throw new InvalidOperationException($"Please change the token in {credentialsPath} to your Discord bot token!");
        }

        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        /*_client.MessageUpdated += MessageUpdated;*/
        _client.MessageReceived += Pong;
        _client.GuildAvailable += OnGuildAvailable;
        _client.GuildUnavailable += OnGuildUnavailable;

        await BuildCommand();
        _client.SlashCommandExecuted += HandleQuoteCommandAsync;

        await Task.Delay(-1);
    }

    private static Task Log(LogMessage msg)
    {

        Console.WriteLine(msg.ToString());
        return Task.CompletedTask;
    }

    /*private static async Task MessageUpdated(Cacheable<IMessage, ulong> before, SocketMessage after, ISocketMessageChannel channel)*/
    /*{*/
    /*    // If the message was not in the cache, downloading it will result in getting a copy of `after`.*/
    /*    var message = await before.GetOrDownloadAsync();*/
    /*    Console.WriteLine($"{message.Content} -> {after.Content}");*/
    /*}*/

    private static async Task BuildCommand()
    {
        var globalCommand = new SlashCommandBuilder()
            .WithName("quote")
            .WithDescription("Creates a new Quote")
            .AddOption("from", ApplicationCommandOptionType.String, "The author of the quote", false)
            .AddOption("text", ApplicationCommandOptionType.String, "The content of the quote", true);

        try
        {
            await _client.CreateGlobalApplicationCommandAsync(globalCommand.Build());
        }
        catch (HttpException exception)
        {
            Console.WriteLine(exception.Message);
        }
    }

    private static async Task HandleQuoteCommandAsync(SocketSlashCommand command)
    {
        if (command.CommandName != "quote") return;

        // Get the options
        var fromOption = (string?)command.Data.Options.FirstOrDefault(x => x.Name == "from")?.Value;
        var textOption = (string?)command.Data.Options.FirstOrDefault(x => x.Name == "text")?.Value;

        // Prepare the values
        string quotee = string.IsNullOrWhiteSpace(fromOption) ? "Anonymous" : fromOption!;
        string text = textOption ?? "No quote provided."; // Shouldn't happen since it's required

        // Create the embed
        var embed = new EmbedBuilder()
            .WithColor(Color.Blue)
            .WithTitle("📜 A New Quote!")
            .WithDescription(text)
            .AddField("From", quotee, true)
            .AddField("Submitted By", command.User.Mention, true)
            .WithCurrentTimestamp();

        // Respond with the embed
        var message = await command.Channel.SendMessageAsync(embed: embed.Build());
        await message.AddReactionAsync(new Emoji("👍"));
        await message.AddReactionAsync(new Emoji("👎"));
    }

    private static async Task Pong(SocketMessage messageParam)
    {
        var message = messageParam as SocketUserMessage;
        if (message is null) return;

        if (!(message.Content.StartsWith("!") || message.Author.IsBot)) return;

        if (!(message.Content.ToLower().StartsWith("!say"))) return;

        var reply = message.Content.Split(" ");
        if (reply.Length < 2) return;
        await message.DeleteAsync();

        var finalRepl = string.Join(" ", reply.Skip(1));

        await message.Channel.SendMessageAsync(finalRepl);


    }

    private static async Task OnGuildAvailable(SocketGuild guild)
    {
        // Define the guild command
        var guildCommand = new SlashCommandBuilder()
            .WithName("quote")
            .WithDescription("Creates a new Quote")
            .AddOption("from", ApplicationCommandOptionType.String, "The author of the quote (optional)", false)
            .AddOption("text", ApplicationCommandOptionType.String, "The content of the quote", true);

        // Register the command to the guild
        await _client.Rest.CreateGuildCommand(guildCommand.Build(), guild.Id);

        Console.WriteLine($"Added 'quote' command to guild: {guild.Name} ({guild.Id})");
    }

    private static async Task OnGuildUnavailable(SocketGuild guild)
    {
        // Remove all commands for the guild (or specific commands if needed)
        var commands = await _client.Rest.GetGuildApplicationCommands(guild.Id);
        foreach (var command in commands)
        {
            await command.DeleteAsync();
        }
    }
}




namespace Bot;

using System.Text.Json;

public class DiscordBot
{
    public static async Task Main()
    {
        var conf = new GuildConfig();

        var options = new JsonSerializerOptions
        {
            WriteIndented = true // Enable pretty-printing
        };

        var json = JsonSerializer.Serialize(conf, options);
        Console.WriteLine(json);
    }
}

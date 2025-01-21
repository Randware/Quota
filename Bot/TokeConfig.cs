namespace Bot;

internal class DiscordCredentials
{
    public string Token { init; get; }

    public DiscordCredentials(string token)
    {
        Token = token;
    }
}

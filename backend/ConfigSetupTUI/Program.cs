using Spectre.Console;
using System.Security.Cryptography;
using TextCopy;

namespace ConfigSetupTUI;

class Program
{
    static void Main(string[] args)
    {
        AnsiConsole.Clear();
        
        // Title with gradient effect
        var title = new FigletText("Quota Setup")
            .Centered()
            .Color(Color.Blue);
        AnsiConsole.Write(title);
        
        AnsiConsole.Write(new Rule().RuleStyle("dim"));
        AnsiConsole.MarkupLine("[bold blue]✨ Welcome to the Randware Config Setup Wizard! ✨[/]");
        AnsiConsole.Write(new Rule().RuleStyle("dim"));
        AnsiConsole.WriteLine();

        // JWT Section with icons
        var jwtPanel = new Panel("[bold white]🔐 JWT Configuration[/]")
            .BorderStyle(new Style(Color.Blue))
            .RoundedBorder();
        AnsiConsole.Write(jwtPanel);
        
        int secretLength = AnsiConsole.Prompt(
            new TextPrompt<int>("[cyan]🔑 JWT secret length?[/] [grey](min 32, recommended 64)[/]")
                .DefaultValue(64)
                .Validate(len => len >= 32 ? ValidationResult.Success() : ValidationResult.Error("[red]Secret must be at least 32 characters[/]"))
        );
        
        string secret = GenerateSecret(secretLength);
        AnsiConsole.MarkupLine($"[green]✓[/] [grey]Generated secret:[/] [yellow]{secret[..8]}...[/]");
        
        string issuer = AnsiConsole.Prompt(
            new TextPrompt<string>("[cyan]🏷️  JWT issuer:[/]")
                .DefaultValue("your_issuer")
                .AllowEmpty()
        );
        if (string.IsNullOrWhiteSpace(issuer)) issuer = "your_issuer";
        
        string audience = AnsiConsole.Prompt(
            new TextPrompt<string>("[cyan]👥 JWT audience:[/]")
                .DefaultValue("your_audience")
                .AllowEmpty()
        );
        if (string.IsNullOrWhiteSpace(audience)) audience = "your_audience";
        
        int expiryMinutes = AnsiConsole.Prompt(
            new TextPrompt<int>("[cyan]⏰ JWT expiry (minutes):[/]")
                .DefaultValue(5)
        );

        AnsiConsole.WriteLine();

        // OAuth Section
        var oauthPanel = new Panel("[bold white]🎮 Discord OAuth Configuration[/]")
            .BorderStyle(new Style(Color.Purple))
            .RoundedBorder();
        AnsiConsole.Write(oauthPanel);
        
        string oauthId = AnsiConsole.Prompt(
        new TextPrompt<string>("[cyan]🆔 Discord OAuth client ID:[/]")
            .Validate(value => !string.IsNullOrWhiteSpace(value) 
                ? ValidationResult.Success() 
                : ValidationResult.Error("[red]Discord OAuth client ID is required[/]"))
        );
        if (string.IsNullOrWhiteSpace(oauthId)) oauthId = "your_discord_client_id";
        
        string oauthSecret = AnsiConsole.Prompt(
            new TextPrompt<string>("[cyan]🔒 Discord OAuth client secret:[/]")
                .Secret()
        );
        
        string apiEndpoint = AnsiConsole.Prompt(
            new TextPrompt<string>("[cyan]🌐 Discord API endpoint:[/]")
                .DefaultValue("https://discord.com/api/v10")
                .AllowEmpty()
        );
        if (string.IsNullOrWhiteSpace(apiEndpoint)) apiEndpoint = "https://discord.com/api/v10";

        AnsiConsole.WriteLine();

        // Bot Section
        var botPanel = new Panel("[bold white]🤖 Discord Bot Configuration[/]")
            .BorderStyle(new Style(Color.Magenta1))
            .RoundedBorder();
        AnsiConsole.Write(botPanel);
        
        string botToken = AnsiConsole.Prompt(
            new TextPrompt<string>("[cyan]🎭 Discord bot token:[/]")
                .Secret()
                .Validate(value => !string.IsNullOrWhiteSpace(value) 
                    ? ValidationResult.Success() 
                    : ValidationResult.Error("[red]Discord bot token is required[/]"))
        );

        AnsiConsole.WriteLine();

        // OpenAPI Section
        var openApiPanel = new Panel("[bold white]📚 OpenAPI/Swagger Configuration[/]")
            .BorderStyle(new Style(Color.Green))
            .RoundedBorder();
        AnsiConsole.Write(openApiPanel);
        
        bool openApiEnabled = AnsiConsole.Confirm("[cyan]📖 Enable OpenAPI/Swagger UI?[/]", true);

        AnsiConsole.WriteLine();

        // Server Section
        var serverPanel = new Panel("[bold white]🚀 Server Configuration[/]")
            .BorderStyle(new Style(Color.Orange3))
            .RoundedBorder();
        AnsiConsole.Write(serverPanel);
        
        int port = AnsiConsole.Prompt(
            new TextPrompt<int>("[cyan]🔌 Server port:[/]")
                .DefaultValue(5000)
        );

        // Generate config
        var configText = GenerateConfigText(secret, issuer, audience, expiryMinutes, oauthId, oauthSecret, apiEndpoint, botToken, openApiEnabled, port);

        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule("[bold green]✨ Generated Configuration ✨[/]").RuleStyle("green"));
        AnsiConsole.WriteLine();

        // Display config without border
        AnsiConsole.MarkupLine("[bold aqua]config.toml:[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Text(configText, new Style(Color.Grey)));

        // Copy to clipboard
        bool clipboardSuccess = TryCopyToClipboard(configText);
        
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule().RuleStyle("dim"));
        
        if (clipboardSuccess)
        {
            AnsiConsole.MarkupLine("[green]✓ Configuration copied to clipboard![/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]⚠️  Could not copy to clipboard. Please copy manually from above.[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]🎉 Setup complete! 🎉[/]");
    }

    static string GenerateConfigText(string secret, string issuer, string audience, int expiryMinutes, 
                                   string oauthId, string oauthSecret, string apiEndpoint, string botToken, bool openApiEnabled, int port)
    {
        return $@"[jwt]
secret = ""{secret}""
issuer = ""{issuer}""
audience = ""{audience}""
expiryMinutes = {expiryMinutes}

[oauth]
id = ""{oauthId}""
secret = ""{oauthSecret}""
apiEndpoint = ""{apiEndpoint}""

[bot]
token = ""{botToken}""

[openapi]
enabled = {openApiEnabled.ToString().ToLower()}

[server]
port = {port}";
    }

    static bool TryCopyToClipboard(string text)
    {
        try
        {
            ClipboardService.SetText(text);
            return true;
        }
        catch
        {
            return false;
        }
    }

    static string GenerateSecret(int length)
    {
        using (var rng = RandomNumberGenerator.Create())
        {
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }
}

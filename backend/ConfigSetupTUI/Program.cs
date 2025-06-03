using Spectre.Console;
using System.Security.Cryptography;
using TextCopy;

namespace ConfigSetupTUI;

class Program
{
    static void Main(string[] args)
    {
        AnsiConsole.Write(new FigletText("JWT Setup").Centered().Color(Color.Aqua));
        AnsiConsole.MarkupLine("[bold yellow]Welcome to the JWT Config Setup Wizard![/]");
        AnsiConsole.WriteLine();

        // Secret length
        int secretLength = AnsiConsole.Prompt(
            new TextPrompt<int>("[green]How long should the JWT secret be? (min 32, recommended 64)[/]")
                .DefaultValue(64)
                .Validate(len => len >= 32 ? ValidationResult.Success() : ValidationResult.Error("Secret must be at least 32 characters."))
        );
        string secret = GenerateSecret(secretLength);
        AnsiConsole.MarkupLine($"[grey]Generated secret:[/] [blue]{secret}[/]");
        AnsiConsole.WriteLine();

        // Issuer
        string issuer = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Enter the JWT issuer (e.g., your domain or app name):[/]")
                .DefaultValue("your_issuer")
                .AllowEmpty()
        );
        if (string.IsNullOrWhiteSpace(issuer)) issuer = "your_issuer";

        // Audience
        string audience = AnsiConsole.Prompt(
            new TextPrompt<string>("[green]Enter the JWT audience (e.g., your app audience):[/]")
                .DefaultValue("your_audience")
                .AllowEmpty()
        );
        if (string.IsNullOrWhiteSpace(audience)) audience = "your_audience";

        // Print config
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold green]Your config.toml JWT section:[/]");
        var configText = "[[jwt]]\nsecret = \"" + secret + "\"\nissuer = \"" + issuer + "\"\naudience = \"" + audience + "\"\n";
        bool clipboardSuccess = true;
        try
        {
            ClipboardService.SetText(configText);
        }
        catch (Exception)
        {
            clipboardSuccess = false;
        }
        AnsiConsole.Write(new Panel(configText).Border(BoxBorder.Double).Header("config.toml").BorderStyle(new Style(Color.Aqua)));
        if (clipboardSuccess)
            AnsiConsole.MarkupLine("[grey]This has been copied to your clipboard. Paste it into your config.toml file.[/]");
        else
            AnsiConsole.MarkupLine("[red]Could not copy to clipboard. Please copy it manually.[/]");
    }

    static string GenerateSecret(int length)
    {
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[length];
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes);
    }
}


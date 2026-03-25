# Randware Quota Backend Server

This is the backend server for the Randware Quota application, providing authentication and API services for the Discord quote bot.

## 🚀 Quick Start

1. Configure your server using our interactive setup tool:
   ```bash
   dotnet run --project ConfigSetupTUI
   ```
2. Copy the generated output to your `config.toml` file in the `Quota/backend` project directory.
   - Alternatively, you can manually create and edit `config.toml` in the `Main` project directory.

3. Start the server:
   ```bash
   dotnet run --project Main
   ```

## ⚙️ Configuration

The server uses a `config.toml` file for all its settings. You can either:

- Use our interactive setup tool (recommended)
- Manually edit `config.toml`

### Configuration Sections

```toml
[jwt]
secret = "your_jwt_secret"      # JWT signing secret
issuer = "your_issuer"          # JWT issuer
audience = "your_audience"       # JWT audience
expiryMinutes = 5              # JWT expiry time in minutes

[oauth]
id = "your_discord_client_id"   # Discord OAuth client ID
secret = "your_discord_secret"  # Discord OAuth client secret
apiEndpoint = "..."             # Discord API endpoint (default: v10)

[openapi]
enabled = true                  # Enable/disable Swagger UI

[server]
port = 5000                     # Server port (default: 5000)

[bot]
token = "your_bot_token"        # Discord bot token from Developer Portal
```

### Bot Configuration

The Discord bot requires a token to function. To get your bot token:

1. Go to https://discord.com/developers/applications
2. Select your application
3. Navigate to the "Bot" section
4. Click "Reset Token" or copy your existing token
5. Paste it in your config.toml under the [bot] section

⚠️ **Important**: Keep your bot token secret! Never commit it to version control.

Required Bot Permissions:

- View Channels
- Send Messages
- Send Messages in Threads
- Embed Links
- Use External Emojis
- Add Reactions
- View Guild Insights
- Manage Guild

You can use the `/bot/invite` endpoint to get an invite link with all required permissions pre-configured.

## 📖 API Documentation

When OpenAPI is enabled (`[openapi] enabled = true`), you can access the interactive API documentation at:

```
http://localhost:{port}/swagger
```

The Swagger UI provides:

- Detailed endpoint documentation
- Request/response schemas
- Interactive testing interface
- Authentication flow examples

## 🔒 Security

- All sensitive data should be properly configured, especially:
  - JWT secret (used for signing tokens)
  - Discord OAuth credentials
  - Bot token
- Never share your actual `config.toml` with others

## 🛠️ Development

### Prerequisites

- .NET 9.0 or higher
- A Discord application (for OAuth)
- A Discord bot token

### Local Development

1. Clone the repository
2. Run the config setup tool
3. Start the server
4. Access Swagger UI for API testing (if enabled)

## 📝 License

TODO: Add license information here.

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
```

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
- Never share your actual `config.toml` with others 

## 🛠️ Development

### Prerequisites
- .NET 9.0 or higher
- A Discord application (for OAuth)

### Local Development
1. Clone the repository
2. Run the config setup tool
3. Start the server
4. Access Swagger UI for API testing (if enabled)

## 📝 License

TODO: Add license information here.

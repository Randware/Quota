namespace Common.OAuth;
//TODO: Merge to the DB model class

///<summary>
/// Represents all sorts of prameters the application received during the OAuth2 process.
/// This includes Access & Refresh Token, Type of token, Scopes and expiry date
///</summary>
public class Token
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public string TokenType { get; set; }
    /// <summary>
    /// Original amount of seconds that indicate how many seconds the token expires in.
    /// To get the live amount of seconds call the <code>Remaining()</code>
    /// </summary>
    public long ExpiresIn { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public HashSet<string> Scope { get; set; } = new();
    /// <summary>
    /// True once ExpiresIn is 0.
    /// </summary>
    public bool IsExpired => Remaining() <= 0;

    public Token() { }
    public Token(string accessToken, string refreshToken, string tokenType, uint expiresIn, DateTime? createdAt = null, HashSet<string>? scope = null)
    {
        TokenType = tokenType ?? throw new ArgumentNullException(nameof(tokenType));
        AccessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
        RefreshToken = refreshToken ?? throw new ArgumentNullException(nameof(refreshToken));
        ExpiresIn = expiresIn;
        CreatedAt = createdAt?.ToUniversalTime() ?? DateTime.UtcNow;
        Scope = scope ?? new HashSet<string>();
    }

    /// <summary>
    /// Calculates the current amount of seconds the Access Token is valid.
    /// Returns 0 if the Access Token is no longer valid.
    /// </summary>
    public long Remaining()
    {
        var remaining = (long)(ExpiresIn - (DateTime.UtcNow - CreatedAt).TotalSeconds);
        return (long)Math.Max(0, remaining);
    }

    public override string ToString() => $"Token {{\n\tAccess Token: {AccessToken}\n\tRefresh Token: {RefreshToken}\n\tToken Type: {TokenType}\n\tExpires in: {Remaining()} seconds\n\tScope: {string.Join(" ", Scope)}\n}}";
}

/// <summary>
/// Represents a Discord User 
/// </summary>
public readonly struct User
{
    public string ID { get; init; }
    public string Username { get; init; }
    public string GlobalName { get; init; }
    public string Avatar { get; init; }
    public Nitro Premium { get; init; }
    public bool Mfa { get; init; }

    public enum Nitro : sbyte
    {
        None = 0,
        Classic = 1,
        Normal = 2,
        Basic = 3,
    }


    public User(string id, string username, string globalName, string avatar, Nitro premium, bool mfa)
    {
        ID = id ?? throw new ArgumentNullException(nameof(id));
        Username = username ?? throw new ArgumentNullException(nameof(username));
        GlobalName = globalName ?? throw new ArgumentNullException(nameof(globalName));
        Avatar = avatar;
        Premium = premium;
        Mfa = mfa;
    }


    public override string ToString() => $"User {{\n\tID: {ID}\n\tUsername: {Username}\n\tGlobal Name: {GlobalName}\n\tNitro: {Premium} \n\tMfa: {(Mfa ? "enabled" : "disabled")}\n}}";
}


/// <summary>
/// Represents a Discord Client 
/// </summary>
public class Client
{
    public string ID { get; init; }
    public string Secret { get; init; }
    public string ApiEndpoint { get; init; }
    
    public string BotToken { get; set; }

    public Client(string id, string secret, string botToken, string? apiEndpoint = null)
    {
        ID = id ?? throw new ArgumentNullException(nameof(id));
        Secret = secret ?? throw new ArgumentNullException(nameof(secret));
        BotToken = botToken ?? throw new ArgumentNullException(nameof(botToken));
        var endpoint = (apiEndpoint ?? "https://discord.com/api/v10");
        ApiEndpoint = endpoint.EndsWith("/") ? endpoint[..^1] : endpoint;
    }

    public override string ToString() => $"Client {{\n\tID: {ID}\n\tSecret: {Secret}\n\tBot Token: {BotToken}\n\tAPI Endpoint: {ApiEndpoint}\n}}";
}


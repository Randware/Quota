
namespace Global.OAuth;

public struct Token
{

    public string AccessToken { get; init; }
    public string RefreshToken { get; init; }
    public string TokenType { get; init; }
    /// <summary>
    /// Original amount of seconds that indicate how many seconds the token expires in.
    /// To get the live amount of seconds call the <code>Remeinig()</code>
    /// </summary>
    public long ExpiresIn { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    /// <summary>
    /// True once ExpiresIn is 0.
    /// </summary>
    public bool IsExpired => Remaining() <= 0;

    public Token(string accessToken, string refreshToken, string tokenType, uint expiresIn, DateTime? createdAt = null)
    {
        TokenType = tokenType ?? throw new ArgumentNullException(nameof(tokenType));
        AccessToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
        RefreshToken = refreshToken ?? throw new ArgumentNullException(nameof(refreshToken));
        ExpiresIn = expiresIn;
        CreatedAt = createdAt?.ToUniversalTime() ?? DateTime.UtcNow;
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

    public override string ToString() => $"Token {{\n\tAccess Token: {AccessToken}\n\tRefresh Token: {RefreshToken}\n\tToken Type: {TokenType}\n\tExpires in: {Remaining()} seconds\n}}";

}

public struct User
{
    public string Id { get; init; }
    public string Username { get; init; }
    public string GlobalName { get; init; }
    public string Avatar { get; init; }
    public sbyte Premium { get; init; }
    public bool Mfa { get; init; }


    public User(string id, string username, string globalName, string avatar, sbyte premium, bool mfa)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Username = username ?? throw new ArgumentNullException(nameof(username));
        GlobalName = globalName ?? throw new ArgumentNullException(nameof(globalName));
        Avatar = avatar;
        Premium = premium;
        Mfa = mfa;
    }
}

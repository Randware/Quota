namespace Database.Model;

public class Session
{
    public Guid ID { get; set; }
    public Guid TokenID { get; set; }
    public DiscordToken Token { get; set; }
    public string RefreshToken { get; set; }
    public bool Revoked { get; set; } = false;
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class DiscordToken
{
    public Guid ID { get; set; }
    public string DiscordID { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Scope { get; set; } // Store the scope as a space-separated string
    public ICollection<Session> Sessions { get; set; }
}

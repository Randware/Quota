namespace Model;

public class Session
{
    public Guid ID { get; set; }
    public string SessionToken { get; set; }
    public string DiscordID { get; set; }
    public string AuthToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime TokenExpires { get; set; }
    public DateTime SessionExpires { get; set; }
    public DateTime CreatedAt { get; set; }
}

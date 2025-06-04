namespace Database.Model;

public class User
{
    public Guid ID { get; set; }
    public string DiscordID { get; set; }

    public ICollection<Quote> QuotesSubmitted { get; set; } = new List<Quote>();
    public ICollection<Quotee> QuoteeProfiles { get; set; } = new List<Quotee>();
}


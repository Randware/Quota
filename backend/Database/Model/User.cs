namespace Database.Model;

public class User
{
    public Guid ID { get; set; }
    public string DiscordID { get; set; }

    public ICollection<Quote> QuotesSubmitted { get; set; }
    public ICollection<Quotee> QuoteeProfiles { get; set; }
}


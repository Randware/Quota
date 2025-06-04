namespace Database.Model;

public class Quote
{
    public Guid ID { get; set; }
    public string MessageID { get; set; }
    public string Content { get; set; }
    public string MediaUrls { get; set; } = ""; 
    public int Upvotes { get; set; }
    public int Downvotes { get; set; }
    public DateTime? CreatedAt { get; set; }

    public Guid GuildID { get; set; }
    public Guild Guild { get; set; }
    public Guid SubmittedByID { get; set; }
    public User SubmittedBy { get; set; }

    public ICollection<QuoteQuotee> QuoteQuotees { get; set; }
}

public class QuoteQuotee
{
    public Guid QuoteID { get; set; }
    public Quote Quote { get; set; }

    public Guid QuoteeID { get; set; }
    public Quotee Quotee { get; set; }
}

public class Quotee
{
    public Guid ID { get; set; }
    public string Name { get; set; }

    public Guid? UserID { get; set; }
    public User User { get; set; }

    public ICollection<QuoteQuotee> QuoteQuotees { get; set; }
}

public class QuoteVote
{
    public Guid QuoteID { get; set; }
    public Quote Quote { get; set; }

    public Guid UserID { get; set; }
    public User User { get; set; }

    public bool IsUpvote { get; set; }
}

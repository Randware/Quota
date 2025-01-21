namespace Store;
using Microsoft.Data.Sqlite;

public class Storage
{
    private const string DB_PATH = "database.db";
    
    private long id;
    
    private SqliteConnection connection;
    
    public Storage(long id)
    {
        this.id = id;
        
        connection = new SqliteConnection(DB_PATH);
    }

    public uint SaveQuote(Quote quote)
    {
        throw new NotImplementedException();
    }


    public IList<Quote> RandomQuote(uint amount = 1)
    {
        throw new NotImplementedException();
    }

    public IList<Quote> GetQuote(string quotee)
    {
        throw new NotImplementedException();
    }

    public Quote GetQuote(uint id)
    {
        throw new NotImplementedException();
    }
}
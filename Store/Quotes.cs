using Microsoft.Data.Sqlite;

namespace Store;

public class Quotes
{
    private const string quotesTable = "quotes";

    private readonly SqliteConnection _connection;

    public Quotes(SqliteConnection connection)
    {
        _connection = connection;

        Init();
    }

    private void Init()
    {
        using SqliteCommand createTable = _connection.CreateCommand();

        createTable.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {quotesTable} (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                text TEXT NOT NULL,
                quotee TEXT NOT NULL
            );
        ";

        createTable.ExecuteNonQuery();
    }

    /// <summary>
    /// Insert a new <see cref="Quote"/> into the Storage.
    /// </summary>
    /// 
    /// <param name="quote">
    /// The <see cref="Quote"/> which will be inserted.
    /// </param>
    /// 
    /// <returns>
    /// The id of the newly inserted quote.
    /// </returns>
    public uint SaveQuote(Quote quote)
    {
        using SqliteCommand insertCmd = _connection.CreateCommand();

        insertCmd.CommandText = $@"
            INSERT INTO {quotesTable} (text, quotee)
            VALUES (@text, @quotee);
        ";

        insertCmd.Parameters.AddWithValue("@text", quote.Text);
        insertCmd.Parameters.AddWithValue("@quotee", quote.Quotee);
        insertCmd.ExecuteNonQuery();

        using SqliteCommand idCommand = _connection.CreateCommand();

        idCommand.CommandText = $@"
            SELECT id FROM {quotesTable}
            WHERE rowid in (SELECT last_insert_rowid() LIMIT 1);
        ";

        using SqliteDataReader reader = idCommand.ExecuteReader();
        reader.Read();
        uint insertID = (uint)reader.GetInt64(0);

        return insertID;
    }

    /// <summary>
    /// Get a random <see cref="Quote"/> from the storage.
    /// </summary>
    /// 
    /// <returns>
    /// A random <see cref="Quote"/>, or null, if
    /// there are no quotes stored in the storage.
    /// </returns>
    public Quote? Random()
    {
        List<Quote>? quote = Random(1);

        if (quote.Count <= 0) return null;

        return quote.First();
    }

    /// <summary>
    /// Get a specific amount of random <see cref="Quote"/> objects from the storage.
    /// </summary>
    ///
    /// <param name="amount">
    /// The amount of random quotes to get. If there are not enough
    /// quotes in the storage, this will simply be all quotes.
    /// </param>
    /// 
    /// <returns>
    /// A <see cref="List{T}"/> containing random <see cref="Quote"/> objects.
    /// An empty <see cref="List{Quote}"/> if there are no quotes
    /// stored in the storage.
    /// </returns>
    public List<Quote> Random(uint amount)
    {
        List<Quote> quotes = new List<Quote>();

        using SqliteCommand randomCommand = _connection.CreateCommand();

        randomCommand.CommandText = $@"
            SELECT text, quotee FROM {quotesTable} ORDER BY RANDOM() LIMIT {amount};
        ";

        using SqliteDataReader reader = randomCommand.ExecuteReader();

        while (reader.Read())
        {
            quotes.Add(new Quote(
                reader.GetString(0),
                reader.GetString(1)
            ));
        }

        return quotes;
    }

    /// <summary>
    /// Get all <see cref="Quote"/> from a specific quotee.
    /// </summary>
    ///
    /// <param name="quotee">
    /// Name of the quotee to get the quotes from.
    /// </param>
    /// 
    /// <returns>
    /// A <see cref="List{T}"/> containing all <see cref="Quote"/>
    /// objects by the provided quotee. An empty <see cref="List{Quote}"/>
    /// if there are no quotes found.
    /// </returns>
    public List<Quote> ByQuotee(string quotee)
    {
        List<Quote> quotes = new List<Quote>();

        using SqliteCommand getCommand = _connection.CreateCommand();

        getCommand.CommandText = $@"
            SELECT text, quotee FROM {quotesTable} WHERE LOWER(quotee) = @quotee;
        ";

        getCommand.Parameters.AddWithValue("@quotee", quotee.ToLower());

        using SqliteDataReader reader = getCommand.ExecuteReader();

        while (reader.Read())
        {
            quotes.Add(new Quote(
                reader.GetString(0),
                reader.GetString(1)
            ));
        }

        return quotes;
    }

    /// <summary>
    /// Get a specific <see cref="Quote"/> by its ID.
    /// </summary>
    /// 
    /// <param name="id">
    /// The ID which to get the quote for 
    /// </param>
    /// 
    /// <returns>
    /// The <see cref="Quote"/> with the specified ID.
    /// </returns>
    public Quote? ByID(uint id)
    {
        using SqliteCommand getCommand = _connection.CreateCommand();

        getCommand.CommandText = $@"
            SELECT id, text, quotee FROM {quotesTable} WHERE id == @id LIMIT 1;
        ";

        getCommand.Parameters.AddWithValue("@id", id);

        using SqliteDataReader reader = getCommand.ExecuteReader();

        if (reader.Read())
        {
            return new Quote(
                reader.GetString(1),
                reader.GetString(2)
            );
        }

        return null;
    }
}
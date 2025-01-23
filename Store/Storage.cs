namespace Store;

using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;

public class Storage : IDisposable
{
    private const string DB_PATH = "Data Source=database.db;";

    private long id;
    private SqliteConnection connection;

    private string Table => $"quotes_{id}";

    public Storage(long id)
    {
        this.id = id;

        connection = new SqliteConnection(DB_PATH);
        connection.Open();

        InitTable();
    }

    private void InitTable()
    {
        using SqliteCommand createTable = connection.CreateCommand();

        createTable.CommandText = $@"
            CREATE TABLE IF NOT EXISTS {Table} (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                text TEXT NOT NULL,
                quotee TEXT NOT NULL
            );
        ";

        createTable.ExecuteNonQuery();
    }

    /// <summary>
    /// Delete all data associated with this storage instance and disconnect
    /// from the database. This effectively renders the storage useless.
    /// Trying to use it further, will result in errors.
    /// </summary>
    public void Delete()
    {
        using SqliteCommand deleteCommand = connection.CreateCommand();

        deleteCommand.CommandText = $@"
            DROP TABLE {Table};
        ";

        deleteCommand.ExecuteNonQuery();

        Dispose();
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
        using SqliteCommand insertCmd = connection.CreateCommand();

        insertCmd.CommandText = $@"
            INSERT INTO {Table} (text, quotee)
            VALUES (@text, @quotee);
        ";

        insertCmd.Parameters.AddWithValue("@text", quote.Text);
        insertCmd.Parameters.AddWithValue("@quotee", quote.Quotee);
        insertCmd.ExecuteNonQuery();

        using SqliteCommand idCommand = connection.CreateCommand();

        idCommand.CommandText = $@"
            SELECT id FROM {Table}
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
    public Quote? RandomQuote()
    {
        List<Quote>? quote = RandomQuote(1);

        if (quote == null) return null;

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
    public List<Quote> RandomQuote(uint amount)
    {
        List<Quote> quotes = new List<Quote>();

        using SqliteCommand randomCommand = connection.CreateCommand();

        randomCommand.CommandText = $@"
            SELECT text, quotee FROM {Table} ORDER BY RANDOM() LIMIT {amount};
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
    public List<Quote> GetQuotes(string quotee)
    {
        List<Quote> quotes = new List<Quote>();

        using SqliteCommand getCommand = connection.CreateCommand();

        getCommand.CommandText = $@"
            SELECT text, quotee FROM {Table} WHERE LOWER(quotee) = @quotee;
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
    public Quote? GetQuote(uint id)
    {
        using SqliteCommand getCommand = connection.CreateCommand();

        getCommand.CommandText = $@"
            SELECT id, text, quotee FROM {Table} WHERE id == @id LIMIT 1;
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

    public void Dispose()
    {
        if (connection != null)
        {
            connection.Close();
            connection.Dispose();
            connection = null;
        }
    }
}
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

    public void Delete()
    {
        using SqliteCommand deleteCommand = connection.CreateCommand();

        deleteCommand.CommandText = $@"
            DROP TABLE {Table};
        ";

        deleteCommand.ExecuteNonQuery();
    }

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

    public Quote RandomQuote()
    {
        throw new NotImplementedException();
    }
    
    public IList<Quote> RandomQuote(uint amount)
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
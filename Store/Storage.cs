namespace Store;

using Microsoft.Data.Sqlite;
using System;

public class Storage : IDisposable
{
    private readonly long _id;
    private SqliteConnection _connection;

    private string DatabasePath => $"{_id}.db";
    public Quotes Quotes { get; }


    public Storage(long id)
    {
        _id = id;

        _connection = new SqliteConnection($"Data Source={DatabasePath}");
        _connection.Open();

        Quotes = new Quotes(_connection);
    }

    public void Delete()
    {
        _connection.Close();
        _connection.Dispose();

        if (File.Exists(DatabasePath))
        {
            File.Delete(DatabasePath);
        }
    }

    public void Dispose()
    {
        if (_connection != null)
        {
            _connection.Close();
            _connection.Dispose();
            _connection = null;
        }
    }
}
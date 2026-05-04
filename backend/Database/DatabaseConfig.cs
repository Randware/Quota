using System;

namespace Database;

public static class DatabaseConfig
{
    public static string ConnectionString
    {
        get
        {
            var dbPath = Environment.GetEnvironmentVariable("QUOTA_DB_PATH");
            return string.IsNullOrWhiteSpace(dbPath)
                ? "Data Source=database.db"
                : $"Data Source={dbPath}";
        }
    }
}

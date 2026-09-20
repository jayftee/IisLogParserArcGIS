using Dapper;
using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Data.Connections;

/// <summary>
/// Opens connections to the output aggregate SQLite database file.
/// </summary>
public static class SqliteConnectionFactory
{
    static SqliteConnectionFactory()
    {
        SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
    }

    /// <summary>
    /// Opens a new, already-open connection to the SQLite database file at <paramref name="databasePath"/>,
    /// creating the file if it does not already exist. This is the sole entry point for obtaining a
    /// connection, which guarantees this assembly's Dapper <see cref="SqlMapper.TypeHandler{T}"/>
    /// registrations (e.g. for <see cref="DateOnly"/>) are in place before any query runs.
    /// </summary>
    /// <param name="databasePath">The path to the SQLite database file.</param>
    /// <returns>An open <see cref="SqliteConnection"/>.</returns>
    public static SqliteConnection Open(string databasePath)
    {
        ArgumentNullException.ThrowIfNull(databasePath);

        var connectionString = new SqliteConnectionStringBuilder { DataSource = databasePath }.ConnectionString;
        var connection = new SqliteConnection(connectionString);
        connection.Open();
        return connection;
    }
}

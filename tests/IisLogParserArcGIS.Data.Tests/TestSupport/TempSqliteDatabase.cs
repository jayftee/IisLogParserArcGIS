using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.Data.Tests.TestSupport;

/// <summary>
/// A SQLite database file at a unique temporary path, deleted on disposal. Shared by every test in this
/// project that needs a real (temporary-file) SQLite database, per the seams agreed in the spec. Disposal releases
/// only this database's pooled connections (via the same connection string the connection factory builds) so the
/// file can be deleted: <c>SqliteConnection.ClearAllPools()</c> is global, and with tests running in parallel it
/// closed pooled connections other tests still had in use (<see cref="ObjectDisposedException"/>).
/// </summary>
internal sealed class TempSqliteDatabase : IDisposable
{
    public TempSqliteDatabase()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");
    }

    public string Path { get; }

    public void Dispose()
    {
        using (var pooledConnection = new SqliteConnection(new SqliteConnectionStringBuilder { DataSource = Path }.ConnectionString))
        {
            SqliteConnection.ClearPool(pooledConnection);
        }

        if (File.Exists(Path))
        {
            File.Delete(Path);
        }
    }
}

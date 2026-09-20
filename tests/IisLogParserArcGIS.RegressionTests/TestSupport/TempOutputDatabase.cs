using Microsoft.Data.Sqlite;

namespace IisLogParserArcGIS.RegressionTests.TestSupport;

/// <summary>
/// A SQLite database file at a unique temporary path, deleted on disposal - the output database path passed to
/// the compiled executable for a single test run.
/// </summary>
internal sealed class TempOutputDatabase : IDisposable
{
    public TempOutputDatabase()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");
    }

    public string Path { get; }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();

        if (File.Exists(Path))
        {
            File.Delete(Path);
        }
    }
}

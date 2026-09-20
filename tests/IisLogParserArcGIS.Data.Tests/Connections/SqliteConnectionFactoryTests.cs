using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Tests.TestSupport;

namespace IisLogParserArcGIS.Data.Tests.Connections;

public class SqliteConnectionFactoryTests
{
    [Fact]
    public void Open_WithNonExistentFile_CreatesFileAndReturnsOpenConnection()
    {
        using var database = new TempSqliteDatabase();

        using var connection = SqliteConnectionFactory.Open(database.Path);

        Assert.Equal(System.Data.ConnectionState.Open, connection.State);
        Assert.True(File.Exists(database.Path));
    }

    [Fact]
    public void Open_WithNullDatabasePath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SqliteConnectionFactory.Open(null!));
    }
}

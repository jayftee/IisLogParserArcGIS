using System.Data;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Tests.TestSupport;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class AggregateQueryBaseTests
{
    [Fact]
    public void Constructor_ExposesTheGivenConnection()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);

        var query = new TestQuery(connection);

        Assert.Same(connection, query.ExposedConnection);
    }

    [Fact]
    public void Constructor_WithNullConnection_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new TestQuery(null!));
    }

    private sealed class TestQuery(IDbConnection connection) : AggregateQueryBase(connection)
    {
        public IDbConnection ExposedConnection => Connection;
    }
}

using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByArcGisServiceTopSuccessfulHitsQueryTests
{
    private static readonly DateOnly _localDate = new(2026, 5, 1);

    [Fact]
    public void GetTop50_OrdersDescendingBySuccessfulHits()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByArcGisServiceRepository().Insert(
            connection,
            [
                ServiceRow("low", successfulHits: 3),
                ServiceRow("high", successfulHits: 9),
                ServiceRow("mid", successfulHits: 6),
            ]);
        var query = new ByArcGisServiceTopSuccessfulHitsQuery(connection);

        var rows = query.GetTop50(_localDate, _localDate, "mimas").ToArray();

        Assert.Equal(["high", "mid", "low"], rows.Select(row => row.ServiceName));
        Assert.Equal([9, 6, 3], rows.Select(row => row.Hits));
    }

    [Fact]
    public void GetTop50_ReturnsOnlyTheGivenSite()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByArcGisServiceRepository().Insert(
            connection,
            [
                ServiceRow("firemap", successfulHits: 5, site: "mimas"),
                ServiceRow("firemap", successfulHits: 50, site: "proxy"),
            ]);
        var query = new ByArcGisServiceTopSuccessfulHitsQuery(connection);

        var rows = query.GetTop50(_localDate, _localDate, "mimas").ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(5, row.Hits);
    }

    [Fact]
    public void GetTop50_WithFewerThan50Services_ReturnsOnlyWhatExists()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByArcGisServiceRepository().Insert(connection, [ServiceRow("firemap", successfulHits: 5)]);
        var query = new ByArcGisServiceTopSuccessfulHitsQuery(connection);

        var rows = query.GetTop50(_localDate, _localDate, "mimas").ToArray();

        Assert.Single(rows);
    }

    [Fact]
    public void GetTop50_ExcludesServicesWithZeroSuccessfulHits()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByArcGisServiceRepository().Insert(
            connection,
            [
                new ByArcGisServiceAggregateRow
                {
                    LocalDate = _localDate,
                    Site = "mimas",
                    Folder = null,
                    ServiceName = "all-failed",
                    ServiceType = "MapServer",
                    SuccessfulTimeTakenSecond = 0,
                    FailedTimeTakenSecond = 5.0,
                    Hits = 1,
                    SuccessfulHits = 0,
                    FailedHits = 1,
                },
            ]);
        var query = new ByArcGisServiceTopSuccessfulHitsQuery(connection);

        var rows = query.GetTop50(_localDate, _localDate, "mimas");

        Assert.Empty(rows);
    }

    [Fact]
    public void GetTop50_WithNullSite_ThrowsArgumentNullException()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByArcGisServiceTopSuccessfulHitsQuery(connection);

        Assert.Throws<ArgumentNullException>(() => query.GetTop50(_localDate, _localDate, null!));
    }

    private static ByArcGisServiceAggregateRow ServiceRow(string serviceName, int successfulHits, string site = "mimas") => new()
    {
        LocalDate = _localDate,
        Site = site,
        Folder = null,
        ServiceName = serviceName,
        ServiceType = "MapServer",
        SuccessfulTimeTakenSecond = successfulHits,
        FailedTimeTakenSecond = 0,
        Hits = successfulHits,
        SuccessfulHits = successfulHits,
        FailedHits = 0,
    };
}

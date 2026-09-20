using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByArcGisServiceTopAverageSuccessfulTimeQueryTests
{
    private static readonly DateOnly _localDate = new(2026, 5, 1);

    [Fact]
    public void GetTop50_OrdersDescendingByAverageSuccessfulTime()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByArcGisServiceRepository().Insert(
            connection,
            [
                ServiceRow("fast", successfulTime: 1.0, successfulHits: 2),
                ServiceRow("slow", successfulTime: 10.0, successfulHits: 2),
            ]);
        var query = new ByArcGisServiceTopAverageSuccessfulTimeQuery(connection);

        var rows = query.GetTop50(_localDate, _localDate, "mimas").ToArray();

        Assert.Equal(["slow", "fast"], rows.Select(row => row.ServiceName));
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
        var query = new ByArcGisServiceTopAverageSuccessfulTimeQuery(connection);

        var rows = query.GetTop50(_localDate, _localDate, "mimas");

        Assert.Empty(rows);
    }

    [Fact]
    public void GetTop50_WithNullSite_ThrowsArgumentNullException()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByArcGisServiceTopAverageSuccessfulTimeQuery(connection);

        Assert.Throws<ArgumentNullException>(() => query.GetTop50(_localDate, _localDate, null!));
    }

    private static ByArcGisServiceAggregateRow ServiceRow(string serviceName, double successfulTime, int successfulHits) => new()
    {
        LocalDate = _localDate,
        Site = "mimas",
        Folder = null,
        ServiceName = serviceName,
        ServiceType = "MapServer",
        SuccessfulTimeTakenSecond = successfulTime,
        FailedTimeTakenSecond = 0,
        Hits = successfulHits,
        SuccessfulHits = successfulHits,
        FailedHits = 0,
    };
}

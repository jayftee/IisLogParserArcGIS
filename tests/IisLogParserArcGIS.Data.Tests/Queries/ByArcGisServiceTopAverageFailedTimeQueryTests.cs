using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByArcGisServiceTopAverageFailedTimeQueryTests
{
    private static readonly DateOnly _localDate = new(2026, 5, 1);

    [Fact]
    public void GetTop50_OrdersDescendingByAverageFailedTime()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByArcGisServiceRepository().Insert(
            connection,
            [
                ServiceRow("fast", failedTime: 1.0, failedHits: 2),
                ServiceRow("slow", failedTime: 10.0, failedHits: 2),
            ]);
        var query = new ByArcGisServiceTopAverageFailedTimeQuery(connection);

        var rows = query.GetTop50(_localDate, _localDate, "mimas").ToArray();

        Assert.Equal(["slow", "fast"], rows.Select(row => row.ServiceName));
    }

    [Fact]
    public void GetTop50_ExcludesServicesWithZeroFailedHits()
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
                    ServiceName = "all-successful",
                    ServiceType = "MapServer",
                    SuccessfulTimeTakenSecond = 5.0,
                    FailedTimeTakenSecond = 0,
                    Hits = 1,
                    SuccessfulHits = 1,
                    FailedHits = 0,
                },
            ]);
        var query = new ByArcGisServiceTopAverageFailedTimeQuery(connection);

        var rows = query.GetTop50(_localDate, _localDate, "mimas");

        Assert.Empty(rows);
    }

    [Fact]
    public void GetTop50_WithNullSite_ThrowsArgumentNullException()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByArcGisServiceTopAverageFailedTimeQuery(connection);

        Assert.Throws<ArgumentNullException>(() => query.GetTop50(_localDate, _localDate, null!));
    }

    private static ByArcGisServiceAggregateRow ServiceRow(string serviceName, double failedTime, int failedHits) => new()
    {
        LocalDate = _localDate,
        Site = "mimas",
        Folder = null,
        ServiceName = serviceName,
        ServiceType = "MapServer",
        SuccessfulTimeTakenSecond = 0,
        FailedTimeTakenSecond = failedTime,
        Hits = failedHits,
        SuccessfulHits = 0,
        FailedHits = failedHits,
    };
}

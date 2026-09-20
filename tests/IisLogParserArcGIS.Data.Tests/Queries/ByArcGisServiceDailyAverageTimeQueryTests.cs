using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByArcGisServiceDailyAverageTimeQueryTests
{
    [Fact]
    public void GetDailyAverageTimeTotals_SumsSuccessfulAndFailedTimeIndependently()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 5, 1);
        new ByArcGisServiceRepository().Insert(
            connection,
            [
                ServiceRow(localDate, "mimas", "firemap", successfulTime: 4.0, successfulHits: 2, failedTime: 1.0, failedHits: 1),
                ServiceRow(localDate, "mimas", "watermap", successfulTime: 2.0, successfulHits: 1, failedTime: 0.0, failedHits: 0),
            ]);
        var query = new ByArcGisServiceDailyAverageTimeQuery(connection);

        var rows = query.GetDailyAverageTimeTotals(localDate, localDate, "mimas").ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(6.0, row.SuccessfulTimeTakenSecond);
        Assert.Equal(3, row.SuccessfulHits);
        Assert.Equal(1.0, row.FailedTimeTakenSecond);
        Assert.Equal(1, row.FailedHits);
    }

    [Fact]
    public void GetDailyAverageTimeTotals_ReturnsOnlyTheGivenSite()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 5, 1);
        new ByArcGisServiceRepository().Insert(
            connection,
            [
                ServiceRow(localDate, "mimas", "firemap", successfulTime: 4.0, successfulHits: 2, failedTime: 0.0, failedHits: 0),
                ServiceRow(localDate, "proxy", "firemap", successfulTime: 99.0, successfulHits: 9, failedTime: 0.0, failedHits: 0),
            ]);
        var query = new ByArcGisServiceDailyAverageTimeQuery(connection);

        var rows = query.GetDailyAverageTimeTotals(localDate, localDate, "mimas").ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(4.0, row.SuccessfulTimeTakenSecond);
    }

    [Fact]
    public void GetDailyAverageTimeTotals_ReturnsOnlyRowsWithinTheDateRange()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByArcGisServiceRepository().Insert(
            connection,
            [
                ServiceRow(new DateOnly(2026, 4, 30), "mimas", "firemap", successfulTime: 99.0, successfulHits: 9, failedTime: 0.0, failedHits: 0),
                ServiceRow(new DateOnly(2026, 5, 1), "mimas", "firemap", successfulTime: 4.0, successfulHits: 2, failedTime: 0.0, failedHits: 0),
            ]);
        var query = new ByArcGisServiceDailyAverageTimeQuery(connection);

        var rows = query.GetDailyAverageTimeTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1), "mimas").ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(new DateOnly(2026, 5, 1), row.LocalDate);
        Assert.Equal(4.0, row.SuccessfulTimeTakenSecond);
    }

    [Fact]
    public void GetDailyAverageTimeTotals_WithNullSite_ThrowsArgumentNullException()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByArcGisServiceDailyAverageTimeQuery(connection);

        Assert.Throws<ArgumentNullException>(() => query.GetDailyAverageTimeTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1), null!));
    }

#pragma warning disable CC0042 // Seven independent test-fixture inputs; a parameter object would just repackage them without benefit.
    private static ByArcGisServiceAggregateRow ServiceRow(DateOnly localDate, string site, string serviceName, double successfulTime, int successfulHits, double failedTime, int failedHits) => new()
#pragma warning restore CC0042
    {
        LocalDate = localDate,
        Site = site,
        Folder = null,
        ServiceName = serviceName,
        ServiceType = "MapServer",
        SuccessfulTimeTakenSecond = successfulTime,
        FailedTimeTakenSecond = failedTime,
        Hits = successfulHits + failedHits,
        SuccessfulHits = successfulHits,
        FailedHits = failedHits,
    };
}

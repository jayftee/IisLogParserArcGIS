using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByArcGisServiceDailyHitTotalsQueryTests
{
    [Fact]
    public void GetDailyTotals_ReturnsOnlyRowsWithinTheDateRange()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByArcGisServiceRepository().Insert(
            connection,
            [
                ServiceRow(new DateOnly(2026, 4, 30), "mimas", "firemap", hits: 5, successfulHits: 5, failedHits: 0),
                ServiceRow(new DateOnly(2026, 5, 1), "mimas", "firemap", hits: 7, successfulHits: 6, failedHits: 1),
                ServiceRow(new DateOnly(2026, 5, 2), "mimas", "firemap", hits: 9, successfulHits: 9, failedHits: 0),
            ]);
        var query = new ByArcGisServiceDailyHitTotalsQuery(connection);

        var rows = query.GetDailyTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1), "mimas").ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(new DateOnly(2026, 5, 1), row.LocalDate);
        Assert.Equal(7, row.Hits);
        Assert.Equal(6, row.SuccessfulHits);
        Assert.Equal(1, row.FailedHits);
    }

    [Fact]
    public void GetDailyTotals_ReturnsOnlyTheGivenSite()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 5, 1);
        new ByArcGisServiceRepository().Insert(
            connection,
            [
                ServiceRow(localDate, "mimas", "firemap", hits: 5, successfulHits: 5, failedHits: 0),
                ServiceRow(localDate, "proxy", "firemap", hits: 9, successfulHits: 9, failedHits: 0),
            ]);
        var query = new ByArcGisServiceDailyHitTotalsQuery(connection);

        var rows = query.GetDailyTotals(localDate, localDate, "mimas").ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(5, row.Hits);
    }

    [Fact]
    public void GetDailyTotals_SumsAcrossEveryServiceOnTheSiteForOneDate()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 5, 1);
        new ByArcGisServiceRepository().Insert(
            connection,
            [
                ServiceRow(localDate, "mimas", "firemap", hits: 5, successfulHits: 4, failedHits: 1),
                ServiceRow(localDate, "mimas", "watermap", hits: 3, successfulHits: 3, failedHits: 0),
            ]);
        var query = new ByArcGisServiceDailyHitTotalsQuery(connection);

        var rows = query.GetDailyTotals(localDate, localDate, "mimas").ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(8, row.Hits);
        Assert.Equal(7, row.SuccessfulHits);
        Assert.Equal(1, row.FailedHits);
    }

    [Fact]
    public void GetDailyTotals_WithNoMatchingRows_ReturnsEmpty()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByArcGisServiceDailyHitTotalsQuery(connection);

        var rows = query.GetDailyTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1), "mimas");

        Assert.Empty(rows);
    }

    [Fact]
    public void GetDailyTotals_WithNullSite_ThrowsArgumentNullException()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByArcGisServiceDailyHitTotalsQuery(connection);

        Assert.Throws<ArgumentNullException>(() => query.GetDailyTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1), null!));
    }

#pragma warning disable CC0042 // Six independent test-fixture inputs; a parameter object would just repackage them without benefit.
    private static ByArcGisServiceAggregateRow ServiceRow(DateOnly localDate, string site, string serviceName, int hits, int successfulHits, int failedHits) => new()
#pragma warning restore CC0042
    {
        LocalDate = localDate,
        Site = site,
        Folder = null,
        ServiceName = serviceName,
        ServiceType = "MapServer",
        SuccessfulTimeTakenSecond = successfulHits,
        FailedTimeTakenSecond = failedHits,
        Hits = hits,
        SuccessfulHits = successfulHits,
        FailedHits = failedHits,
    };
}

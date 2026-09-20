using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByArcGisServiceDetailDailyHitTotalsQueryTests
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
                ServiceRow(new DateOnly(2026, 4, 30), "mimas", null, "firemap", hits: 5, successfulHits: 5, failedHits: 0),
                ServiceRow(new DateOnly(2026, 5, 1), "mimas", null, "firemap", hits: 7, successfulHits: 6, failedHits: 1),
                ServiceRow(new DateOnly(2026, 5, 2), "mimas", null, "firemap", hits: 9, successfulHits: 9, failedHits: 0),
            ]);
        var query = new ByArcGisServiceDetailDailyHitTotalsQuery(connection);

        var rows = query.GetDailyTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1), Service("mimas", null, "firemap")).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(new DateOnly(2026, 5, 1), row.LocalDate);
        Assert.Equal(7, row.Hits);
        Assert.Equal(6, row.SuccessfulHits);
        Assert.Equal(1, row.FailedHits);
    }

    [Fact]
    public void GetDailyTotals_ExcludesADifferentServiceOnTheSameSiteAndDate()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 5, 1);
        new ByArcGisServiceRepository().Insert(
            connection,
            [
                ServiceRow(localDate, "mimas", null, "firemap", hits: 5, successfulHits: 5, failedHits: 0),
                ServiceRow(localDate, "mimas", null, "watermap", hits: 9, successfulHits: 9, failedHits: 0),
            ]);
        var query = new ByArcGisServiceDetailDailyHitTotalsQuery(connection);

        var rows = query.GetDailyTotals(localDate, localDate, Service("mimas", null, "firemap")).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(5, row.Hits);
    }

    [Fact]
    public void GetDailyTotals_MatchesOnlyTheGivenFolder_NullSafely()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 5, 1);
        new ByArcGisServiceRepository().Insert(
            connection,
            [
                ServiceRow(localDate, "mimas", null, "firemap", hits: 5, successfulHits: 5, failedHits: 0),
                ServiceRow(localDate, "mimas", "utilities", "firemap", hits: 9, successfulHits: 9, failedHits: 0),
            ]);
        var query = new ByArcGisServiceDetailDailyHitTotalsQuery(connection);

        var folderlessRows = query.GetDailyTotals(localDate, localDate, Service("mimas", null, "firemap")).ToArray();
        var folderedRows = query.GetDailyTotals(localDate, localDate, Service("mimas", "utilities", "firemap")).ToArray();

        Assert.Equal(5, Assert.Single(folderlessRows).Hits);
        Assert.Equal(9, Assert.Single(folderedRows).Hits);
    }

    [Fact]
    public void GetDailyTotals_WithNoMatchingRows_ReturnsEmpty()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByArcGisServiceDetailDailyHitTotalsQuery(connection);

        var rows = query.GetDailyTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1), Service("mimas", null, "firemap"));

        Assert.Empty(rows);
    }

    [Fact]
    public void GetDailyTotals_WithNullService_ThrowsArgumentNullException()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByArcGisServiceDetailDailyHitTotalsQuery(connection);

        Assert.Throws<ArgumentNullException>(() => query.GetDailyTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1), null!));
    }

#pragma warning disable CC0042 // Six independent test-fixture inputs; a parameter object would just repackage them without benefit.
    private static ByArcGisServiceAggregateRow ServiceRow(DateOnly localDate, string site, string? folder, string serviceName, int hits, int successfulHits, int failedHits) => new()
#pragma warning restore CC0042
    {
        LocalDate = localDate,
        Site = site,
        Folder = folder,
        ServiceName = serviceName,
        ServiceType = "MapServer",
        SuccessfulTimeTakenSecond = successfulHits,
        FailedTimeTakenSecond = failedHits,
        Hits = hits,
        SuccessfulHits = successfulHits,
        FailedHits = failedHits,
    };

    private static ArcGisServiceIdentity Service(string site, string? folder, string serviceName) => new()
    {
        Site = site,
        Folder = folder,
        ServiceName = serviceName,
        ServiceType = "MapServer",
    };
}

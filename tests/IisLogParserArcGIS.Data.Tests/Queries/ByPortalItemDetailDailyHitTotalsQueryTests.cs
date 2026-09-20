using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByPortalItemDetailDailyHitTotalsQueryTests
{
    [Fact]
    public void GetDailyTotals_ReturnsOnlyRowsWithinTheDateRange()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByPortalItemRepository().Insert(
            connection,
            [
                ItemRow("item-1", new DateOnly(2026, 4, 30), hits: 5, successfulHits: 5, failedHits: 0),
                ItemRow("item-1", new DateOnly(2026, 5, 1), hits: 7, successfulHits: 6, failedHits: 1),
                ItemRow("item-1", new DateOnly(2026, 5, 2), hits: 9, successfulHits: 9, failedHits: 0),
            ]);
        var query = new ByPortalItemDetailDailyHitTotalsQuery(connection);

        var rows = query.GetDailyTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1), "item-1").ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(new DateOnly(2026, 5, 1), row.LocalDate);
        Assert.Equal(7, row.Hits);
        Assert.Equal(6, row.SuccessfulHits);
        Assert.Equal(1, row.FailedHits);
    }

    [Fact]
    public void GetDailyTotals_ExcludesADifferentPortalItemOnTheSameDate()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 5, 1);
        new ByPortalItemRepository().Insert(
            connection,
            [
                ItemRow("item-1", localDate, hits: 5, successfulHits: 5, failedHits: 0),
                ItemRow("item-2", localDate, hits: 9, successfulHits: 9, failedHits: 0),
            ]);
        var query = new ByPortalItemDetailDailyHitTotalsQuery(connection);

        var rows = query.GetDailyTotals(localDate, localDate, "item-1").ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(5, row.Hits);
    }

    [Fact]
    public void GetDailyTotals_WithNoMatchingRows_ReturnsEmpty()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByPortalItemDetailDailyHitTotalsQuery(connection);

        var rows = query.GetDailyTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1), "item-1");

        Assert.Empty(rows);
    }

    [Fact]
    public void GetDailyTotals_WithNullPortalItemId_ThrowsArgumentNullException()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByPortalItemDetailDailyHitTotalsQuery(connection);

        Assert.Throws<ArgumentNullException>(() => query.GetDailyTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1), null!));
    }

#pragma warning disable CC0042 // Five independent test-fixture inputs; a parameter object would just repackage them without benefit.
    private static ByPortalItemAggregateRow ItemRow(string portalItemId, DateOnly localDate, int hits, int successfulHits, int failedHits) => new()
#pragma warning restore CC0042
    {
        LocalDate = localDate,
        PortalItemId = portalItemId,
        TimeTakenSecond = hits,
        Hits = hits,
        SuccessfulHits = successfulHits,
        FailedHits = failedHits,
    };
}

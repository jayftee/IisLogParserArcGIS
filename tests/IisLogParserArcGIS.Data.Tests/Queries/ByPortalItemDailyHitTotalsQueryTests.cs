using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByPortalItemDailyHitTotalsQueryTests
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
                ItemRow("item-1", new DateOnly(2026, 4, 30), 5),
                ItemRow("item-1", new DateOnly(2026, 5, 1), 7),
                ItemRow("item-1", new DateOnly(2026, 5, 2), 9),
            ]);
        var query = new ByPortalItemDailyHitTotalsQuery(connection);

        var rows = query.GetDailyTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1)).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(new DateOnly(2026, 5, 1), row.LocalDate);
        Assert.Equal(7, row.Hits);
    }

    [Fact]
    public void GetDailyTotals_SumsAcrossEveryPortalItemOnTheSameDate()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 5, 1);
        new ByPortalItemRepository().Insert(
            connection,
            [
                new ByPortalItemAggregateRow { LocalDate = localDate, PortalItemId = "item-1", TimeTakenSecond = 5, Hits = 5, SuccessfulHits = 3, FailedHits = 2 },
                new ByPortalItemAggregateRow { LocalDate = localDate, PortalItemId = "item-2", TimeTakenSecond = 9, Hits = 9, SuccessfulHits = 6, FailedHits = 3 },
            ]);
        var query = new ByPortalItemDailyHitTotalsQuery(connection);

        var rows = query.GetDailyTotals(localDate, localDate).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(14, row.Hits);
        Assert.Equal(9, row.SuccessfulHits);
        Assert.Equal(5, row.FailedHits);
    }

    [Fact]
    public void GetDailyTotals_WithNoMatchingRows_ReturnsEmpty()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByPortalItemDailyHitTotalsQuery(connection);

        var rows = query.GetDailyTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1));

        Assert.Empty(rows);
    }

    private static ByPortalItemAggregateRow ItemRow(string portalItemId, DateOnly localDate, int hits) => new()
    {
        LocalDate = localDate,
        PortalItemId = portalItemId,
        TimeTakenSecond = hits,
        Hits = hits,
        SuccessfulHits = hits,
        FailedHits = 0,
    };
}

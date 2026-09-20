using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByPortalItemRangeTotalsQueryTests
{
    [Fact]
    public void GetRangeTotals_SumsAcrossTheDateRange_OneRowPerPortalItem()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByPortalItemRepository().Insert(
            connection,
            [
                new ByPortalItemAggregateRow { LocalDate = new DateOnly(2026, 5, 1), PortalItemId = "item-1", TimeTakenSecond = 1.5, Hits = 5, SuccessfulHits = 3, FailedHits = 2 },
                new ByPortalItemAggregateRow { LocalDate = new DateOnly(2026, 5, 2), PortalItemId = "item-1", TimeTakenSecond = 2.5, Hits = 4, SuccessfulHits = 4, FailedHits = 0 },
            ]);
        var query = new ByPortalItemRangeTotalsQuery(connection);

        var rows = query.GetRangeTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 2)).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal("item-1", row.PortalItemId);
        Assert.Equal(9, row.Hits);
        Assert.Equal(7, row.SuccessfulHits);
        Assert.Equal(2, row.FailedHits);
        Assert.Equal(4.0, row.TotalTimeTakenSecond);
    }

    [Fact]
    public void GetRangeTotals_ExcludesRowsOutsideTheDateRange()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByPortalItemRepository().Insert(
            connection,
            [
                new ByPortalItemAggregateRow { LocalDate = new DateOnly(2026, 4, 30), PortalItemId = "item-1", TimeTakenSecond = 1.0, Hits = 5, SuccessfulHits = 5, FailedHits = 0 },
                new ByPortalItemAggregateRow { LocalDate = new DateOnly(2026, 5, 1), PortalItemId = "item-1", TimeTakenSecond = 1.0, Hits = 7, SuccessfulHits = 7, FailedHits = 0 },
            ]);
        var query = new ByPortalItemRangeTotalsQuery(connection);

        var rows = query.GetRangeTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1)).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(7, row.Hits);
    }

    [Fact]
    public void GetRangeTotals_WithMultiplePortalItems_ReturnsOneRowEach()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 5, 1);
        new ByPortalItemRepository().Insert(
            connection,
            [
                new ByPortalItemAggregateRow { LocalDate = localDate, PortalItemId = "item-1", TimeTakenSecond = 1.0, Hits = 5, SuccessfulHits = 5, FailedHits = 0 },
                new ByPortalItemAggregateRow { LocalDate = localDate, PortalItemId = "item-2", TimeTakenSecond = 1.0, Hits = 9, SuccessfulHits = 9, FailedHits = 0 },
            ]);
        var query = new ByPortalItemRangeTotalsQuery(connection);

        var rows = query.GetRangeTotals(localDate, localDate).ToArray();

        Assert.Equal(2, rows.Length);
        Assert.Equal(["item-1", "item-2"], rows.Select(row => row.PortalItemId).Order(StringComparer.Ordinal));
    }

    [Fact]
    public void GetRangeTotals_WithNoMatchingRows_ReturnsEmpty()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByPortalItemRangeTotalsQuery(connection);

        var rows = query.GetRangeTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1));

        Assert.Empty(rows);
    }
}

using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByPortalItemTopSuccessfulHitsQueryTests
{
    private static readonly DateOnly _localDate = new(2026, 5, 1);

    [Fact]
    public void GetTop50_OrdersDescendingBySuccessfulHits()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByPortalItemRepository().Insert(
            connection,
            [
                ItemRow("low", successfulHits: 3),
                ItemRow("high", successfulHits: 9),
                ItemRow("mid", successfulHits: 6),
            ]);
        var query = new ByPortalItemTopSuccessfulHitsQuery(connection);

        var rows = query.GetTop50(_localDate, _localDate).ToArray();

        Assert.Equal(["high", "mid", "low"], rows.Select(row => row.PortalItemId));
        Assert.Equal([9, 6, 3], rows.Select(row => row.Hits));
    }

    [Fact]
    public void GetTop50_WithFewerThan50Items_ReturnsOnlyWhatExists()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByPortalItemRepository().Insert(connection, [ItemRow("item-1", successfulHits: 5)]);
        var query = new ByPortalItemTopSuccessfulHitsQuery(connection);

        var rows = query.GetTop50(_localDate, _localDate).ToArray();

        Assert.Single(rows);
    }

    [Fact]
    public void GetTop50_ExcludesItemsWithZeroSuccessfulHits()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByPortalItemRepository().Insert(
            connection,
            [
                new ByPortalItemAggregateRow
                {
                    LocalDate = _localDate,
                    PortalItemId = "all-failed",
                    TimeTakenSecond = 5.0,
                    Hits = 1,
                    SuccessfulHits = 0,
                    FailedHits = 1,
                },
            ]);
        var query = new ByPortalItemTopSuccessfulHitsQuery(connection);

        var rows = query.GetTop50(_localDate, _localDate);

        Assert.Empty(rows);
    }

    private static ByPortalItemAggregateRow ItemRow(string portalItemId, int successfulHits) => new()
    {
        LocalDate = _localDate,
        PortalItemId = portalItemId,
        TimeTakenSecond = successfulHits,
        Hits = successfulHits,
        SuccessfulHits = successfulHits,
        FailedHits = 0,
    };
}

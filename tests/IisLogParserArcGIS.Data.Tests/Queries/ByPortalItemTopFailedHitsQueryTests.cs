using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByPortalItemTopFailedHitsQueryTests
{
    private static readonly DateOnly _localDate = new(2026, 5, 1);

    [Fact]
    public void GetTop50_OrdersDescendingByFailedHits()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByPortalItemRepository().Insert(
            connection,
            [
                ItemRow("low", failedHits: 3),
                ItemRow("high", failedHits: 9),
                ItemRow("mid", failedHits: 6),
            ]);
        var query = new ByPortalItemTopFailedHitsQuery(connection);

        var rows = query.GetTop50(_localDate, _localDate).ToArray();

        Assert.Equal(["high", "mid", "low"], rows.Select(row => row.PortalItemId));
        Assert.Equal([9, 6, 3], rows.Select(row => row.Hits));
    }

    [Fact]
    public void GetTop50_ExcludesItemsWithZeroFailedHits()
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
                    PortalItemId = "all-successful",
                    TimeTakenSecond = 5.0,
                    Hits = 1,
                    SuccessfulHits = 1,
                    FailedHits = 0,
                },
            ]);
        var query = new ByPortalItemTopFailedHitsQuery(connection);

        var rows = query.GetTop50(_localDate, _localDate);

        Assert.Empty(rows);
    }

    private static ByPortalItemAggregateRow ItemRow(string portalItemId, int failedHits) => new()
    {
        LocalDate = _localDate,
        PortalItemId = portalItemId,
        TimeTakenSecond = failedHits,
        Hits = failedHits,
        SuccessfulHits = 0,
        FailedHits = failedHits,
    };
}

using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Repositories;

public class ByPortalItemRepositoryTests
{
    [Fact]
    public void Insert_ThenGetByLocalDate_RoundTripsRows()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new ByPortalItemRepository();
        var localDate = new DateOnly(2026, 5, 1);

        repository.Insert(
            connection,
            [
                new ByPortalItemAggregateRow
                {
                    LocalDate = localDate,
                    PortalItemId = "abc123",
                    TimeTakenSecond = 1.5,
                    Hits = 3,
                    SuccessfulHits = 2,
                    FailedHits = 1,
                },
                new ByPortalItemAggregateRow
                {
                    LocalDate = localDate,
                    PortalItemId = "def456",
                    TimeTakenSecond = 2.0,
                    Hits = 4,
                    SuccessfulHits = 4,
                    FailedHits = 0,
                },
            ]);

        var rows = repository.GetByLocalDate(connection, localDate).ToArray();

        Assert.Equal(2, rows.Length);
        var first = Assert.Single(rows, row => row.PortalItemId == "abc123");
        Assert.Equal(1.5, first.TimeTakenSecond);
        Assert.Equal(3, first.Hits);
        Assert.Equal(2, first.SuccessfulHits);
        Assert.Equal(1, first.FailedHits);
        var second = Assert.Single(rows, row => row.PortalItemId == "def456");
        Assert.Equal(4, second.Hits);
    }

    [Fact]
    public void DeleteByLocalDate_OnlyRemovesRowsForThatDate()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new ByPortalItemRepository();
        repository.Insert(
            connection,
            [
                new ByPortalItemAggregateRow
                {
                    LocalDate = new DateOnly(2026, 5, 1),
                    PortalItemId = "abc123",
                    TimeTakenSecond = 1.0,
                    Hits = 1,
                    SuccessfulHits = 1,
                    FailedHits = 0,
                },
                new ByPortalItemAggregateRow
                {
                    LocalDate = new DateOnly(2026, 5, 2),
                    PortalItemId = "abc123",
                    TimeTakenSecond = 1.0,
                    Hits = 1,
                    SuccessfulHits = 1,
                    FailedHits = 0,
                },
            ]);

        var deletedCount = repository.DeleteByLocalDate(connection, new DateOnly(2026, 5, 1));

        Assert.Equal(1, deletedCount);
        Assert.Empty(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 1)));
        Assert.Single(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 2)));
    }
}

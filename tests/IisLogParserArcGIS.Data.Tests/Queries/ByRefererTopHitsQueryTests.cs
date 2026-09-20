using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByRefererTopHitsQueryTests
{
    [Fact]
    public void GetTop500_SumsHitsAndTimeAcrossTheDateRange_GroupedByReferer()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByRefererRepository().Insert(
            connection,
            [
                new ByRefererAggregateRow { LocalDate = new DateOnly(2026, 1, 1), Referer = "https://example.com/", TimeTakenSecond = 2.0, Hits = 5 },
                new ByRefererAggregateRow { LocalDate = new DateOnly(2026, 1, 2), Referer = "https://example.com/", TimeTakenSecond = 3.0, Hits = 7 },
            ]);
        var query = new ByRefererTopHitsQuery(connection);

        var rows = query.GetTop500(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2)).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal("https://example.com/", row.Value);
        Assert.Equal(12, row.Hits);
        Assert.Equal(5.0, row.TotalTimeTakenSecond);
    }

    [Fact]
    public void GetTop500_OrdersDescendingByHits()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 1, 1);
        new ByRefererRepository().Insert(
            connection,
            [
                new ByRefererAggregateRow { LocalDate = localDate, Referer = "https://a.example.com/", TimeTakenSecond = 1.0, Hits = 3 },
                new ByRefererAggregateRow { LocalDate = localDate, Referer = "https://b.example.com/", TimeTakenSecond = 1.0, Hits = 9 },
                new ByRefererAggregateRow { LocalDate = localDate, Referer = "https://c.example.com/", TimeTakenSecond = 1.0, Hits = 6 },
            ]);
        var query = new ByRefererTopHitsQuery(connection);

        var rows = query.GetTop500(localDate, localDate).ToArray();

        Assert.Equal(["https://b.example.com/", "https://c.example.com/", "https://a.example.com/"], rows.Select(row => row.Value));
    }

    [Fact]
    public void GetTop500_ExcludesRowsOutsideTheDateRange()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByRefererRepository().Insert(
            connection,
            [
                new ByRefererAggregateRow { LocalDate = new DateOnly(2025, 12, 31), Referer = "https://example.com/", TimeTakenSecond = 1.0, Hits = 100 },
                new ByRefererAggregateRow { LocalDate = new DateOnly(2026, 1, 1), Referer = "https://example.com/", TimeTakenSecond = 1.0, Hits = 5 },
            ]);
        var query = new ByRefererTopHitsQuery(connection);

        var rows = query.GetTop500(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1)).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(5, row.Hits);
    }

    [Fact]
    public void GetTop500_WithNoMatchingRows_ReturnsEmpty()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByRefererTopHitsQuery(connection);

        var rows = query.GetTop500(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1));

        Assert.Empty(rows);
    }

    [Fact]
    public void GetTop500_ExcludesTheEmptyRefererPlaceholder()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 1, 1);
        new ByRefererRepository().Insert(
            connection,
            [
                new ByRefererAggregateRow { LocalDate = localDate, Referer = "-", TimeTakenSecond = 1.0, Hits = 1000 },
                new ByRefererAggregateRow { LocalDate = localDate, Referer = "https://example.com/", TimeTakenSecond = 1.0, Hits = 1 },
            ]);
        var query = new ByRefererTopHitsQuery(connection);

        var rows = query.GetTop500(localDate, localDate).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal("https://example.com/", row.Value);
    }

    [Fact]
    public void GetTop500_WithMoreThanFiveHundredDistinctReferers_ReturnsOnlyTheTop500()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 1, 1);
        var repository = new ByRefererRepository();
        const int DistinctRefererCount = 505;
        repository.Insert(
            connection,
            Enumerable.Range(0, DistinctRefererCount).Select(index => new ByRefererAggregateRow
            {
                LocalDate = localDate,
                Referer = $"https://referer-{index}.example.com/",
                TimeTakenSecond = 1.0,
                Hits = DistinctRefererCount - index,
            }));
        var query = new ByRefererTopHitsQuery(connection);

        var rows = query.GetTop500(localDate, localDate).ToArray();

        Assert.Equal(500, rows.Length);
        Assert.Equal(DistinctRefererCount, rows[0].Hits);
        Assert.Equal(DistinctRefererCount - 499, rows[^1].Hits);
    }
}

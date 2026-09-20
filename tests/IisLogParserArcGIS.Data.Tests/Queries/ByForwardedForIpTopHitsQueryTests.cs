using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByForwardedForIpTopHitsQueryTests
{
    [Fact]
    public void GetTop500_SumsHitsAndTimeAcrossTheDateRange_GroupedByIp()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByForwardedForIpRepository().Insert(
            connection,
            [
                new ByForwardedForIpAggregateRow { LocalDate = new DateOnly(2026, 1, 1), ForwardedForIp = "203.0.113.5", TimeTakenSecond = 2.0, Hits = 5 },
                new ByForwardedForIpAggregateRow { LocalDate = new DateOnly(2026, 1, 2), ForwardedForIp = "203.0.113.5", TimeTakenSecond = 3.0, Hits = 7 },
            ]);
        var query = new ByForwardedForIpTopHitsQuery(connection);

        var rows = query.GetTop500(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2)).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal("203.0.113.5", row.Value);
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
        new ByForwardedForIpRepository().Insert(
            connection,
            [
                new ByForwardedForIpAggregateRow { LocalDate = localDate, ForwardedForIp = "203.0.113.1", TimeTakenSecond = 1.0, Hits = 3 },
                new ByForwardedForIpAggregateRow { LocalDate = localDate, ForwardedForIp = "203.0.113.2", TimeTakenSecond = 1.0, Hits = 9 },
                new ByForwardedForIpAggregateRow { LocalDate = localDate, ForwardedForIp = "203.0.113.3", TimeTakenSecond = 1.0, Hits = 6 },
            ]);
        var query = new ByForwardedForIpTopHitsQuery(connection);

        var rows = query.GetTop500(localDate, localDate).ToArray();

        Assert.Equal(["203.0.113.2", "203.0.113.3", "203.0.113.1"], rows.Select(row => row.Value));
    }

    [Fact]
    public void GetTop500_ExcludesRowsOutsideTheDateRange()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByForwardedForIpRepository().Insert(
            connection,
            [
                new ByForwardedForIpAggregateRow { LocalDate = new DateOnly(2025, 12, 31), ForwardedForIp = "203.0.113.5", TimeTakenSecond = 1.0, Hits = 100 },
                new ByForwardedForIpAggregateRow { LocalDate = new DateOnly(2026, 1, 1), ForwardedForIp = "203.0.113.5", TimeTakenSecond = 1.0, Hits = 5 },
            ]);
        var query = new ByForwardedForIpTopHitsQuery(connection);

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
        var query = new ByForwardedForIpTopHitsQuery(connection);

        var rows = query.GetTop500(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1));

        Assert.Empty(rows);
    }

    [Fact]
    public void GetTop500_WithMoreThanFiveHundredDistinctIps_ReturnsOnlyTheTop500()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 1, 1);
        var repository = new ByForwardedForIpRepository();
        const int DistinctIpCount = 505;
        repository.Insert(
            connection,
            Enumerable.Range(0, DistinctIpCount).Select(index => new ByForwardedForIpAggregateRow
            {
                LocalDate = localDate,
                ForwardedForIp = $"10.0.{index / 256}.{index % 256}",
                TimeTakenSecond = 1.0,
                Hits = DistinctIpCount - index,
            }));
        var query = new ByForwardedForIpTopHitsQuery(connection);

        var rows = query.GetTop500(localDate, localDate).ToArray();

        Assert.Equal(500, rows.Length);
        Assert.Equal(DistinctIpCount, rows[0].Hits);
        Assert.Equal(DistinctIpCount - 499, rows[^1].Hits);
    }
}

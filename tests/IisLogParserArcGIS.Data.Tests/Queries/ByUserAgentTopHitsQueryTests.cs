using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByUserAgentTopHitsQueryTests
{
    [Fact]
    public void GetTop500_SumsHitsAndTimeAcrossTheDateRange_GroupedByUserAgent()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByUserAgentRepository().Insert(
            connection,
            [
                new ByUserAgentAggregateRow { LocalDate = new DateOnly(2026, 1, 1), UserAgent = "arcgis client", TimeTakenSecond = 2.0, Hits = 5 },
                new ByUserAgentAggregateRow { LocalDate = new DateOnly(2026, 1, 2), UserAgent = "arcgis client", TimeTakenSecond = 3.0, Hits = 7 },
            ]);
        var query = new ByUserAgentTopHitsQuery(connection);

        var rows = query.GetTop500(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2)).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal("arcgis client", row.Value);
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
        new ByUserAgentRepository().Insert(
            connection,
            [
                new ByUserAgentAggregateRow { LocalDate = localDate, UserAgent = "agent-a", TimeTakenSecond = 1.0, Hits = 3 },
                new ByUserAgentAggregateRow { LocalDate = localDate, UserAgent = "agent-b", TimeTakenSecond = 1.0, Hits = 9 },
                new ByUserAgentAggregateRow { LocalDate = localDate, UserAgent = "agent-c", TimeTakenSecond = 1.0, Hits = 6 },
            ]);
        var query = new ByUserAgentTopHitsQuery(connection);

        var rows = query.GetTop500(localDate, localDate).ToArray();

        Assert.Equal(["agent-b", "agent-c", "agent-a"], rows.Select(row => row.Value));
    }

    [Fact]
    public void GetTop500_ExcludesRowsOutsideTheDateRange()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByUserAgentRepository().Insert(
            connection,
            [
                new ByUserAgentAggregateRow { LocalDate = new DateOnly(2025, 12, 31), UserAgent = "agent-a", TimeTakenSecond = 1.0, Hits = 100 },
                new ByUserAgentAggregateRow { LocalDate = new DateOnly(2026, 1, 1), UserAgent = "agent-a", TimeTakenSecond = 1.0, Hits = 5 },
            ]);
        var query = new ByUserAgentTopHitsQuery(connection);

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
        var query = new ByUserAgentTopHitsQuery(connection);

        var rows = query.GetTop500(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1));

        Assert.Empty(rows);
    }

    [Fact]
    public void GetTop500_WithMoreThanFiveHundredDistinctUserAgents_ReturnsOnlyTheTop500()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 1, 1);
        var repository = new ByUserAgentRepository();
        const int DistinctUserAgentCount = 505;
        repository.Insert(
            connection,
            Enumerable.Range(0, DistinctUserAgentCount).Select(index => new ByUserAgentAggregateRow
            {
                LocalDate = localDate,
                UserAgent = $"agent-{index}",
                TimeTakenSecond = 1.0,
                Hits = DistinctUserAgentCount - index,
            }));
        var query = new ByUserAgentTopHitsQuery(connection);

        var rows = query.GetTop500(localDate, localDate).ToArray();

        Assert.Equal(500, rows.Length);
        Assert.Equal(DistinctUserAgentCount, rows[0].Hits);
        Assert.Equal(DistinctUserAgentCount - 499, rows[^1].Hits);
    }
}

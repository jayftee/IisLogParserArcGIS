using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByUriTopHitsQueryTests
{
    [Fact]
    public void GetTop500_SumsHitsAndTimeAcrossTheDateRange_GroupedByUri()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByUriRepository().Insert(
            connection,
            [
                new ByUriAggregateRow { LocalDate = new DateOnly(2026, 1, 1), UriStem = "/mimas/rest/services", TimeTakenSecond = 2.0, Hits = 5 },
                new ByUriAggregateRow { LocalDate = new DateOnly(2026, 1, 2), UriStem = "/mimas/rest/services", TimeTakenSecond = 3.0, Hits = 7 },
            ]);
        var query = new ByUriTopHitsQuery(connection);

        var rows = query.GetTop500(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2)).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal("/mimas/rest/services", row.Value);
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
        new ByUriRepository().Insert(
            connection,
            [
                new ByUriAggregateRow { LocalDate = localDate, UriStem = "/a", TimeTakenSecond = 1.0, Hits = 3 },
                new ByUriAggregateRow { LocalDate = localDate, UriStem = "/b", TimeTakenSecond = 1.0, Hits = 9 },
                new ByUriAggregateRow { LocalDate = localDate, UriStem = "/c", TimeTakenSecond = 1.0, Hits = 6 },
            ]);
        var query = new ByUriTopHitsQuery(connection);

        var rows = query.GetTop500(localDate, localDate).ToArray();

        Assert.Equal(["/b", "/c", "/a"], rows.Select(row => row.Value));
    }

    [Fact]
    public void GetTop500_ExcludesRowsOutsideTheDateRange()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByUriRepository().Insert(
            connection,
            [
                new ByUriAggregateRow { LocalDate = new DateOnly(2025, 12, 31), UriStem = "/a", TimeTakenSecond = 1.0, Hits = 100 },
                new ByUriAggregateRow { LocalDate = new DateOnly(2026, 1, 1), UriStem = "/a", TimeTakenSecond = 1.0, Hits = 5 },
            ]);
        var query = new ByUriTopHitsQuery(connection);

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
        var query = new ByUriTopHitsQuery(connection);

        var rows = query.GetTop500(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1));

        Assert.Empty(rows);
    }

    [Fact]
    public void GetTop500_WithMoreThanFiveHundredDistinctUris_ReturnsOnlyTheTop500()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 1, 1);
        var repository = new ByUriRepository();
        const int DistinctUriCount = 505;
        repository.Insert(
            connection,
            Enumerable.Range(0, DistinctUriCount).Select(index => new ByUriAggregateRow
            {
                LocalDate = localDate,
                UriStem = $"/uri-{index}",
                TimeTakenSecond = 1.0,
                Hits = DistinctUriCount - index,
            }));
        var query = new ByUriTopHitsQuery(connection);

        var rows = query.GetTop500(localDate, localDate).ToArray();

        Assert.Equal(500, rows.Length);
        Assert.Equal(DistinctUriCount, rows[0].Hits);
        Assert.Equal(DistinctUriCount - 499, rows[^1].Hits);
    }
}

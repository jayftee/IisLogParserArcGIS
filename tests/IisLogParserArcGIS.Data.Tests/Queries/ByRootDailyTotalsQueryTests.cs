using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByRootDailyTotalsQueryTests
{
    [Fact]
    public void GetDailyTotals_ReturnsOnlyRowsWithinTheDateRange()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByRootRepository().Insert(
            connection,
            [
                new ByRootAggregateRow { LocalDate = new DateOnly(2026, 4, 30), Root = "arcgis", TimeTakenSecond = 1.0, Hits = 5 },
                new ByRootAggregateRow { LocalDate = new DateOnly(2026, 5, 1), Root = "arcgis", TimeTakenSecond = 1.0, Hits = 7 },
                new ByRootAggregateRow { LocalDate = new DateOnly(2026, 5, 2), Root = "arcgis", TimeTakenSecond = 1.0, Hits = 9 },
            ]);
        var query = new ByRootDailyTotalsQuery(connection);

        var rows = query.GetDailyTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1), ["arcgis"]).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(new DateOnly(2026, 5, 1), row.LocalDate);
        Assert.Equal(7, row.Hits);
    }

    [Fact]
    public void GetDailyTotals_ReturnsOnlyIncludedRoots()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 5, 1);
        new ByRootRepository().Insert(
            connection,
            [
                new ByRootAggregateRow { LocalDate = localDate, Root = "arcgis", TimeTakenSecond = 1.0, Hits = 5 },
                new ByRootAggregateRow { LocalDate = localDate, Root = "proxy", TimeTakenSecond = 1.0, Hits = 9 },
            ]);
        var query = new ByRootDailyTotalsQuery(connection);

        var rows = query.GetDailyTotals(localDate, localDate, ["arcgis"]).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal("arcgis", row.Root);
    }

    [Fact]
    public void GetDailyTotals_SumsHitsWhenMultipleRowsShareTheSameDateAndRoot()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 5, 1);
        new ByRootRepository().Insert(
            connection,
            [
                new ByRootAggregateRow { LocalDate = localDate, Root = "arcgis", TimeTakenSecond = 1.0, Hits = 5 },
                new ByRootAggregateRow { LocalDate = localDate, Root = "arcgis", TimeTakenSecond = 1.0, Hits = 3 },
            ]);
        var query = new ByRootDailyTotalsQuery(connection);

        var rows = query.GetDailyTotals(localDate, localDate, ["arcgis"]).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(8, row.Hits);
    }

    [Fact]
    public void GetDailyTotals_WithNoMatchingRows_ReturnsEmpty()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByRootDailyTotalsQuery(connection);

        var rows = query.GetDailyTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1), ["arcgis"]);

        Assert.Empty(rows);
    }

    [Fact]
    public void GetDailyTotals_WithEmptyIncludedRoots_ReturnsEmpty()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 5, 1);
        new ByRootRepository().Insert(connection, [new ByRootAggregateRow { LocalDate = localDate, Root = "arcgis", TimeTakenSecond = 1.0, Hits = 5 }]);
        var query = new ByRootDailyTotalsQuery(connection);

        var rows = query.GetDailyTotals(localDate, localDate, []);

        Assert.Empty(rows);
    }

    [Fact]
    public void GetDailyTotals_WithNullIncludedRoots_ThrowsArgumentNullException()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByRootDailyTotalsQuery(connection);

        Assert.Throws<ArgumentNullException>(() => query.GetDailyTotals(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1), null!));
    }
}

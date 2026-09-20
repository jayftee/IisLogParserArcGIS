using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class ByRootRangeTotalsQueryTests
{
    [Fact]
    public void GetRangeTotals_SumsHitsAndTimeAcrossTheDateRange()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByRootRepository().Insert(
            connection,
            [
                new ByRootAggregateRow { LocalDate = new DateOnly(2026, 1, 1), Root = "arcgis", TimeTakenSecond = 2.0, Hits = 5 },
                new ByRootAggregateRow { LocalDate = new DateOnly(2026, 1, 2), Root = "arcgis", TimeTakenSecond = 3.0, Hits = 7 },
            ]);
        var query = new ByRootRangeTotalsQuery(connection);

        var rows = query.GetRangeTotals(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2), ["arcgis"]).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal("arcgis", row.Root);
        Assert.Equal(12, row.Hits);
        Assert.Equal(5.0, row.TotalTimeTakenSecond);
    }

    [Fact]
    public void GetRangeTotals_ReturnsOnlyIncludedRoots()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var localDate = new DateOnly(2026, 1, 1);
        new ByRootRepository().Insert(
            connection,
            [
                new ByRootAggregateRow { LocalDate = localDate, Root = "arcgis", TimeTakenSecond = 1.0, Hits = 5 },
                new ByRootAggregateRow { LocalDate = localDate, Root = "proxy", TimeTakenSecond = 1.0, Hits = 9 },
            ]);
        var query = new ByRootRangeTotalsQuery(connection);

        var rows = query.GetRangeTotals(localDate, localDate, ["arcgis"]).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal("arcgis", row.Root);
    }

    [Fact]
    public void GetRangeTotals_ExcludesRowsOutsideTheDateRange()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        new ByRootRepository().Insert(
            connection,
            [
                new ByRootAggregateRow { LocalDate = new DateOnly(2025, 12, 31), Root = "arcgis", TimeTakenSecond = 1.0, Hits = 100 },
                new ByRootAggregateRow { LocalDate = new DateOnly(2026, 1, 1), Root = "arcgis", TimeTakenSecond = 1.0, Hits = 5 },
            ]);
        var query = new ByRootRangeTotalsQuery(connection);

        var rows = query.GetRangeTotals(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), ["arcgis"]).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(5, row.Hits);
    }

    [Fact]
    public void GetRangeTotals_WithNoMatchingRows_ReturnsEmpty()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByRootRangeTotalsQuery(connection);

        var rows = query.GetRangeTotals(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), ["arcgis"]);

        Assert.Empty(rows);
    }

    [Fact]
    public void GetRangeTotals_WithNullIncludedRoots_ThrowsArgumentNullException()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new ByRootRangeTotalsQuery(connection);

        Assert.Throws<ArgumentNullException>(() => query.GetRangeTotals(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 1), null!));
    }
}

using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Repositories;

public class ByRootRepositoryTests
{
    [Fact]
    public void Insert_ThenGetByLocalDate_RoundTripsRows()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new ByRootRepository();
        var localDate = new DateOnly(2026, 5, 1);

        repository.Insert(
            connection,
            [
                new ByRootAggregateRow { LocalDate = localDate, Root = "arcgis", TimeTakenSecond = 1.5, Hits = 3 },
                new ByRootAggregateRow { LocalDate = localDate, Root = "portal", TimeTakenSecond = 2.0, Hits = 4 },
            ]);

        var rows = repository.GetByLocalDate(connection, localDate).ToArray();

        Assert.Equal(2, rows.Length);
        var rowForArcgis = Assert.Single(rows, row => row.Root == "arcgis");
        Assert.Equal(1.5, rowForArcgis.TimeTakenSecond);
        Assert.Equal(3, rowForArcgis.Hits);
    }

    [Fact]
    public void DeleteByLocalDate_OnlyRemovesRowsForThatDate()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new ByRootRepository();
        repository.Insert(
            connection,
            [
                new ByRootAggregateRow { LocalDate = new DateOnly(2026, 5, 1), Root = "arcgis", TimeTakenSecond = 1.0, Hits = 1 },
                new ByRootAggregateRow { LocalDate = new DateOnly(2026, 5, 2), Root = "arcgis", TimeTakenSecond = 1.0, Hits = 1 },
            ]);

        var deletedCount = repository.DeleteByLocalDate(connection, new DateOnly(2026, 5, 1));

        Assert.Equal(1, deletedCount);
        Assert.Empty(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 1)));
        Assert.Single(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 2)));
    }
}

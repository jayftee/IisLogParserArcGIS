using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Repositories;

public class ByUriRepositoryTests
{
    [Fact]
    public void Insert_ThenGetByLocalDate_RoundTripsRows()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new ByUriRepository();
        var localDate = new DateOnly(2026, 5, 1);

        repository.Insert(
            connection,
            [
                new ByUriAggregateRow { LocalDate = localDate, UriStem = "/a", TimeTakenSecond = 1.5, Hits = 3 },
                new ByUriAggregateRow { LocalDate = localDate, UriStem = "/b", TimeTakenSecond = 2.0, Hits = 4 },
            ]);

        var rows = repository.GetByLocalDate(connection, localDate).ToArray();

        Assert.Equal(2, rows.Length);
        var rowForA = Assert.Single(rows, row => row.UriStem == "/a");
        Assert.Equal(1.5, rowForA.TimeTakenSecond);
        Assert.Equal(3, rowForA.Hits);
    }

    [Fact]
    public void DeleteByLocalDate_OnlyRemovesRowsForThatDate()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new ByUriRepository();
        repository.Insert(
            connection,
            [
                new ByUriAggregateRow { LocalDate = new DateOnly(2026, 5, 1), UriStem = "/a", TimeTakenSecond = 1.0, Hits = 1 },
                new ByUriAggregateRow { LocalDate = new DateOnly(2026, 5, 2), UriStem = "/a", TimeTakenSecond = 1.0, Hits = 1 },
            ]);

        var deletedCount = repository.DeleteByLocalDate(connection, new DateOnly(2026, 5, 1));

        Assert.Equal(1, deletedCount);
        Assert.Empty(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 1)));
        Assert.Single(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 2)));
    }
}

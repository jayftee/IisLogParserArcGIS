using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Repositories;

public class ByRefererAndUriRepositoryTests
{
    [Fact]
    public void Insert_ThenGetByLocalDate_RoundTripsRows()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new ByRefererAndUriRepository();
        var localDate = new DateOnly(2026, 5, 1);

        repository.Insert(
            connection,
            [
                new ByRefererAndUriAggregateRow { LocalDate = localDate, Referer = "https://example.com/", UriStem = "/a", TimeTakenSecond = 1.5, Hits = 3 },
                new ByRefererAndUriAggregateRow { LocalDate = localDate, Referer = "-", UriStem = "/b", TimeTakenSecond = 2.0, Hits = 4 },
            ]);

        var rows = repository.GetByLocalDate(connection, localDate).ToArray();

        Assert.Equal(2, rows.Length);
        var rowForExample = Assert.Single(rows, row => row.Referer == "https://example.com/" && row.UriStem == "/a");
        Assert.Equal(1.5, rowForExample.TimeTakenSecond);
        Assert.Equal(3, rowForExample.Hits);
    }

    [Fact]
    public void DeleteByLocalDate_OnlyRemovesRowsForThatDate()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new ByRefererAndUriRepository();
        repository.Insert(
            connection,
            [
                new ByRefererAndUriAggregateRow { LocalDate = new DateOnly(2026, 5, 1), Referer = "https://example.com/", UriStem = "/a", TimeTakenSecond = 1.0, Hits = 1 },
                new ByRefererAndUriAggregateRow { LocalDate = new DateOnly(2026, 5, 2), Referer = "https://example.com/", UriStem = "/a", TimeTakenSecond = 1.0, Hits = 1 },
            ]);

        var deletedCount = repository.DeleteByLocalDate(connection, new DateOnly(2026, 5, 1));

        Assert.Equal(1, deletedCount);
        Assert.Empty(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 1)));
        Assert.Single(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 2)));
    }
}

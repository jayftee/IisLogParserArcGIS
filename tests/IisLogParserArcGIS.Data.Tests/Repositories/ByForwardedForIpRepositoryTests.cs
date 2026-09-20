using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Repositories;

public class ByForwardedForIpRepositoryTests
{
    [Fact]
    public void Insert_ThenGetByLocalDate_RoundTripsRows()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new ByForwardedForIpRepository();
        var localDate = new DateOnly(2026, 5, 1);

        repository.Insert(
            connection,
            [
                new ByForwardedForIpAggregateRow { LocalDate = localDate, ForwardedForIp = "203.0.113.1", TimeTakenSecond = 1.5, Hits = 3 },
                new ByForwardedForIpAggregateRow { LocalDate = localDate, ForwardedForIp = "127.0.0.1", TimeTakenSecond = 2.0, Hits = 4 },
            ]);

        var rows = repository.GetByLocalDate(connection, localDate).ToArray();

        Assert.Equal(2, rows.Length);
        var rowForFirst = Assert.Single(rows, row => row.ForwardedForIp == "203.0.113.1");
        Assert.Equal(1.5, rowForFirst.TimeTakenSecond);
        Assert.Equal(3, rowForFirst.Hits);
    }

    [Fact]
    public void DeleteByLocalDate_OnlyRemovesRowsForThatDate()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new ByForwardedForIpRepository();
        repository.Insert(
            connection,
            [
                new ByForwardedForIpAggregateRow { LocalDate = new DateOnly(2026, 5, 1), ForwardedForIp = "203.0.113.1", TimeTakenSecond = 1.0, Hits = 1 },
                new ByForwardedForIpAggregateRow { LocalDate = new DateOnly(2026, 5, 2), ForwardedForIp = "203.0.113.1", TimeTakenSecond = 1.0, Hits = 1 },
            ]);

        var deletedCount = repository.DeleteByLocalDate(connection, new DateOnly(2026, 5, 1));

        Assert.Equal(1, deletedCount);
        Assert.Empty(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 1)));
        Assert.Single(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 2)));
    }
}

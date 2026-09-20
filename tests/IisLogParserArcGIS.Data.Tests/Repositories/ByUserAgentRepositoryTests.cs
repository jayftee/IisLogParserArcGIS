using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Repositories;

public class ByUserAgentRepositoryTests
{
    [Fact]
    public void Insert_ThenGetByLocalDate_RoundTripsRows()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new ByUserAgentRepository();
        var localDate = new DateOnly(2026, 5, 1);

        repository.Insert(
            connection,
            [
                new ByUserAgentAggregateRow { LocalDate = localDate, UserAgent = "mozilla/5.0", TimeTakenSecond = 1.5, Hits = 3 },
                new ByUserAgentAggregateRow { LocalDate = localDate, UserAgent = "curl/8.0", TimeTakenSecond = 2.0, Hits = 4 },
            ]);

        var rows = repository.GetByLocalDate(connection, localDate).ToArray();

        Assert.Equal(2, rows.Length);
        var rowForMozilla = Assert.Single(rows, row => row.UserAgent == "mozilla/5.0");
        Assert.Equal(1.5, rowForMozilla.TimeTakenSecond);
        Assert.Equal(3, rowForMozilla.Hits);
    }

    [Fact]
    public void DeleteByLocalDate_OnlyRemovesRowsForThatDate()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new ByUserAgentRepository();
        repository.Insert(
            connection,
            [
                new ByUserAgentAggregateRow { LocalDate = new DateOnly(2026, 5, 1), UserAgent = "mozilla/5.0", TimeTakenSecond = 1.0, Hits = 1 },
                new ByUserAgentAggregateRow { LocalDate = new DateOnly(2026, 5, 2), UserAgent = "mozilla/5.0", TimeTakenSecond = 1.0, Hits = 1 },
            ]);

        var deletedCount = repository.DeleteByLocalDate(connection, new DateOnly(2026, 5, 1));

        Assert.Equal(1, deletedCount);
        Assert.Empty(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 1)));
        Assert.Single(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 2)));
    }
}

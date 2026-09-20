using Dapper;
using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Tests.TestSupport;

namespace IisLogParserArcGIS.Data.Tests.Repositories;

public class AggregateRepositoryBaseTests
{
    [Fact]
    public void Insert_ThenGetByLocalDate_RoundTripsRows()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        connection.Execute("CREATE TABLE test_rows (id INTEGER PRIMARY KEY, local_date TEXT NOT NULL, value TEXT NOT NULL)");
        var repository = new TestRepository();

        repository.Insert(
            connection,
            [
                new TestRow(new DateOnly(2026, 5, 1), "first"),
                new TestRow(new DateOnly(2026, 5, 1), "second"),
                new TestRow(new DateOnly(2026, 5, 2), "other day"),
            ]);

        var rows = repository.GetByLocalDate(connection, new DateOnly(2026, 5, 1)).ToArray();

        Assert.Equal(2, rows.Length);
        Assert.Contains(rows, row => row.Value == "first");
        Assert.Contains(rows, row => row.Value == "second");
        Assert.All(rows, row => Assert.Equal(new DateOnly(2026, 5, 1), row.LocalDate));
    }

    [Fact]
    public void DeleteByLocalDate_OnlyRemovesRowsForThatDate()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        connection.Execute("CREATE TABLE test_rows (id INTEGER PRIMARY KEY, local_date TEXT NOT NULL, value TEXT NOT NULL)");
        var repository = new TestRepository();
        repository.Insert(
            connection,
            [
                new TestRow(new DateOnly(2026, 5, 1), "first"),
                new TestRow(new DateOnly(2026, 5, 2), "other day"),
            ]);

        var deletedCount = repository.DeleteByLocalDate(connection, new DateOnly(2026, 5, 1));

        Assert.Equal(1, deletedCount);
        Assert.Empty(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 1)));
        Assert.Single(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 2)));
    }

    [Fact]
    public void Insert_WithNullConnection_ThrowsArgumentNullException()
    {
        var repository = new TestRepository();

        Assert.Throws<ArgumentNullException>(() => repository.Insert(null!, [new TestRow(new DateOnly(2026, 5, 1), "first")]));
    }

    private sealed record TestRow(DateOnly LocalDate, string Value);

    private sealed class TestRepository : AggregateRepositoryBase<TestRow>
    {
        protected override string InsertSql => "INSERT INTO test_rows (local_date, value) VALUES (@LocalDate, @Value)";

        protected override string DeleteByLocalDateSql => "DELETE FROM test_rows WHERE local_date = @LocalDate";

        protected override string SelectByLocalDateSql =>
            "SELECT local_date AS LocalDate, value AS Value FROM test_rows WHERE local_date = @LocalDate";
    }
}

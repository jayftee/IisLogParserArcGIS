using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Repositories;

public class BySurvey123DeviceRepositoryTests
{
    private const string DeviceA = "0123456789abcdef0123456789abcdef";
    private const string DeviceB = "fedcba9876543210fedcba9876543210";

    [Fact]
    public void Insert_ThenGetByLocalDate_RoundTripsAttributedAndUnattributedRows()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new BySurvey123DeviceRepository();
        var localDate = new DateOnly(2026, 5, 1);

        repository.Insert(connection, [Row(localDate, DeviceA, "user00364@somewhere") with { Hits = 3 }, Row(localDate, DeviceB, null) with { Hits = 4 }]);

        var rows = repository.GetByLocalDate(connection, localDate).ToArray();

        Assert.Equal(2, rows.Length);
        var attributed = Assert.Single(rows, row => row.DeviceId == DeviceA);
        Assert.Equal("user00364@somewhere", attributed.Username);
        Assert.Equal(3, attributed.Hits);
        Assert.Equal(1.5, attributed.TimeTakenSecond);
        var unattributed = Assert.Single(rows, row => row.DeviceId == DeviceB);
        Assert.Null(unattributed.Username);
        Assert.Equal(4, unattributed.Hits);
    }

    [Fact]
    public void DeleteByLocalDate_OnlyRemovesRowsForThatDate()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new BySurvey123DeviceRepository();
        repository.Insert(connection, [Row(new DateOnly(2026, 5, 1), DeviceA, "user00364@somewhere"), Row(new DateOnly(2026, 5, 2), DeviceA, null)]);

        var deletedCount = repository.DeleteByLocalDate(connection, new DateOnly(2026, 5, 1));

        Assert.Equal(1, deletedCount);
        Assert.Empty(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 1)));
        Assert.Single(repository.GetByLocalDate(connection, new DateOnly(2026, 5, 2)));
    }

    private static BySurvey123DeviceAggregateRow Row(DateOnly localDate, string deviceId, string? username) =>
        new()
        {
            LocalDate = localDate,
            DeviceId = deviceId,
            Username = username,
            TimeTakenSecond = 1.5,
            Hits = 1,
        };
}

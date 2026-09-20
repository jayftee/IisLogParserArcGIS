using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Repositories;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using IisLogParserArcGIS.Domain.Aggregation;

namespace IisLogParserArcGIS.Data.Tests.Repositories;

public class BySurvey123DeviceUsernameBackfillTests
{
    private const string DeviceA = "0123456789abcdef0123456789abcdef";
    private const string DeviceB = "fedcba9876543210fedcba9876543210";
    private static readonly DateOnly _may1 = new(2026, 5, 1);

    [Fact]
    public void BackfillUsernames_FillsNullRowsOnEarlierAndLaterDatesFromTheDevicesLogin()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new BySurvey123DeviceRepository();
        repository.Insert(
            connection,
            [Row(_may1.AddDays(-10), DeviceA, null), Row(_may1, DeviceA, "user00364@somewhere"), Row(_may1.AddDays(10), DeviceA, null)]);

        var updated = BySurvey123DeviceRepository.BackfillUsernames(connection);

        Assert.Equal(2, updated);
        Assert.Equal("user00364@somewhere", Assert.Single(repository.GetByLocalDate(connection, _may1.AddDays(-10))).Username);
        Assert.Equal("user00364@somewhere", Assert.Single(repository.GetByLocalDate(connection, _may1.AddDays(10))).Username);
    }

    [Fact]
    public void BackfillUsernames_WhenTheDeviceHasNoUsernameAnywhere_LeavesRowsNullAndUpdatesNothing()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new BySurvey123DeviceRepository();
        repository.Insert(connection, [Row(_may1, DeviceA, null), Row(_may1.AddDays(1), DeviceA, null)]);

        var updated = BySurvey123DeviceRepository.BackfillUsernames(connection);

        Assert.Equal(0, updated);
        Assert.Null(Assert.Single(repository.GetByLocalDate(connection, _may1)).Username);
        Assert.Null(Assert.Single(repository.GetByLocalDate(connection, _may1.AddDays(1))).Username);
    }

    [Fact]
    public void BackfillUsernames_DoesNotLetOneDevicesLoginAttributeAnotherDevice()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new BySurvey123DeviceRepository();
        repository.Insert(connection, [Row(_may1, DeviceA, "user00364@somewhere"), Row(_may1, DeviceB, null)]);

        var updated = BySurvey123DeviceRepository.BackfillUsernames(connection);

        Assert.Equal(0, updated);
        var rows = repository.GetByLocalDate(connection, _may1).ToArray();
        Assert.Equal("user00364@somewhere", Assert.Single(rows, row => row.DeviceId == DeviceA).Username);
        Assert.Null(Assert.Single(rows, row => row.DeviceId == DeviceB).Username);
    }

    [Fact]
    public void BackfillUsernames_NeverOverwritesAnExistingUsername()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new BySurvey123DeviceRepository();
        repository.Insert(connection, [Row(_may1, DeviceA, "first.user"), Row(_may1.AddDays(1), DeviceA, "second.user"), Row(_may1.AddDays(2), DeviceA, null)]);

        BySurvey123DeviceRepository.BackfillUsernames(connection);

        Assert.Equal("first.user", Assert.Single(repository.GetByLocalDate(connection, _may1)).Username);
        Assert.Equal("second.user", Assert.Single(repository.GetByLocalDate(connection, _may1.AddDays(1))).Username);
    }

    [Fact]
    public void BackfillUsernames_WithSeveralUsernamesForOneDevice_UsesTheEarliestDatesUsername()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new BySurvey123DeviceRepository();
        repository.Insert(
            connection,
            [Row(_may1.AddDays(5), DeviceA, "later.user"), Row(_may1.AddDays(2), DeviceA, "earlier.user"), Row(_may1.AddDays(9), DeviceA, null), Row(_may1.AddDays(-3), DeviceA, null)]);

        BySurvey123DeviceRepository.BackfillUsernames(connection);

        Assert.Equal("earlier.user", Assert.Single(repository.GetByLocalDate(connection, _may1.AddDays(9))).Username);
        Assert.Equal("earlier.user", Assert.Single(repository.GetByLocalDate(connection, _may1.AddDays(-3))).Username);
    }

    [Fact]
    public void BackfillUsernames_WithTwoUsernamesOnTheSameEarliestDate_BreaksTheTieAlphabetically()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new BySurvey123DeviceRepository();
        repository.Insert(connection, [Row(_may1, DeviceA, "zed"), Row(_may1, DeviceA, "amy"), Row(_may1.AddDays(1), DeviceA, null)]);

        BySurvey123DeviceRepository.BackfillUsernames(connection);

        Assert.Equal("amy", Assert.Single(repository.GetByLocalDate(connection, _may1.AddDays(1))).Username);
    }

    [Fact]
    public void BackfillUsernames_CalledASecondTime_ChangesNothing()
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new BySurvey123DeviceRepository();
        repository.Insert(connection, [Row(_may1, DeviceA, "user00364@somewhere"), Row(_may1.AddDays(1), DeviceA, null)]);
        BySurvey123DeviceRepository.BackfillUsernames(connection);

        var updatedOnSecondRun = BySurvey123DeviceRepository.BackfillUsernames(connection);

        Assert.Equal(0, updatedOnSecondRun);
        Assert.Equal("user00364@somewhere", Assert.Single(repository.GetByLocalDate(connection, _may1.AddDays(1))).Username);
    }

    [Fact]
    public void BackfillUsernames_ReachesTheSameFinalStateRegardlessOfTheOrderDatesWereInserted()
    {
        var loginFirst = BackfilledUsernamesFor([Row(_may1, DeviceA, "user00364@somewhere"), Row(_may1.AddDays(1), DeviceA, null), Row(_may1.AddDays(2), DeviceA, null)]);
        var loginLast = BackfilledUsernamesFor([Row(_may1.AddDays(2), DeviceA, null), Row(_may1.AddDays(1), DeviceA, null), Row(_may1, DeviceA, "user00364@somewhere")]);

        Assert.Equal(loginFirst, loginLast);
        Assert.All(loginFirst, username => Assert.Equal("user00364@somewhere", username));
    }

    private static List<string?> BackfilledUsernamesFor(BySurvey123DeviceAggregateRow[] rows)
    {
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var repository = new BySurvey123DeviceRepository();
        repository.Insert(connection, rows);

        BySurvey123DeviceRepository.BackfillUsernames(connection);

        return rows
            .Select(row => row.LocalDate)
            .Order()
            .Select(localDate => Assert.Single(repository.GetByLocalDate(connection, localDate)).Username)
            .ToList();
    }

    private static BySurvey123DeviceAggregateRow Row(DateOnly localDate, string deviceId, string? username) =>
        new()
        {
            LocalDate = localDate,
            DeviceId = deviceId,
            Username = username,
            TimeTakenSecond = 1.0,
            Hits = 1,
        };
}

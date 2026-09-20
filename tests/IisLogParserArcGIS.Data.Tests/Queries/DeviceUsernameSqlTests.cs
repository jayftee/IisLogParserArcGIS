using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using static IisLogParserArcGIS.Data.Tests.TestSupport.DeviceTableTestSupport;

namespace IisLogParserArcGIS.Data.Tests.Queries;

/// <summary>
/// Verifies the one username rule <c>DeviceUsernameSql</c> applies for every per-device query: among a
/// device's non-<c>NULL</c> rows, the earliest <c>local_date</c>, ties broken alphabetically, looked up across the
/// whole table rather than the queried range. <c>DeviceUsernameSql</c> is internal, so the rule is driven
/// through both public queries that embed it (<see cref="DeviceTopHitsQuery"/> and
/// <see cref="DeviceRangeTotalsQuery"/>), which must agree, for each by-device table.
/// </summary>
public class DeviceUsernameSqlTests
{
    private static readonly DateOnly _localDate = new(2026, 5, 1);

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void TheEarliestDateWins_EvenWhenALaterUsernameSortsFirstAlphabetically(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(connection, table, DeviceRow("lent", "zed", _localDate, 1), DeviceRow("lent", "amy", _localDate.AddDays(1), 1));

        AssertBothQueriesResolve(connection, table, _localDate, _localDate.AddDays(1), "lent", "zed");
    }

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void OnTheSameDate_TheAlphabeticallyFirstUsernameWins(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(connection, table, DeviceRow("tie", "bob", _localDate, 1), DeviceRow("tie", "amy", _localDate, 1));

        AssertBothQueriesResolve(connection, table, _localDate, _localDate, "tie", "amy");
    }

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void NullUsernameRowsAreIgnored_SoAnEarlierUnattributedRowDoesNotHideALaterUsername(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(connection, table, DeviceRow("late-login", null, _localDate, 1), DeviceRow("late-login", "amy", _localDate.AddDays(3), 1));

        AssertBothQueriesResolve(connection, table, _localDate, _localDate.AddDays(3), "late-login", "amy");
    }

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void ADeviceWithNoUsernameAnywhere_IsUnattributed(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(connection, table, DeviceRow("anon", null, _localDate, 1), DeviceRow("anon", null, _localDate.AddDays(1), 1));

        AssertBothQueriesResolve(connection, table, _localDate, _localDate.AddDays(1), "anon", null);
    }

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void TheUsernameIsLookedUpAcrossTheWholeTable_NotJustTheQueriedRange(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(connection, table, DeviceRow("lent", "zed", _localDate, 1), DeviceRow("lent", "amy", _localDate.AddDays(5), 1));

        AssertBothQueriesResolve(connection, table, _localDate.AddDays(5), _localDate.AddDays(5), "lent", "zed");
    }

#pragma warning disable CC0042 // Six independent inputs to one assertion helper shared by every test in this class; bundling them would just repackage them.
    private static void AssertBothQueriesResolve(System.Data.IDbConnection connection, DeviceAggregateTable table, DateOnly start, DateOnly end, string deviceId, string? expectedUsername)
#pragma warning restore CC0042
    {
        var top = new DeviceTopHitsQuery(connection, table).GetTop50(start, end).Single(row => row.DeviceId == deviceId);
        var totals = new DeviceRangeTotalsQuery(connection, table).GetRangeTotals(start, end).Single(row => row.DeviceId == deviceId);

        Assert.Equal(expectedUsername, top.Username);
        Assert.Equal(expectedUsername, totals.Username);
    }
}

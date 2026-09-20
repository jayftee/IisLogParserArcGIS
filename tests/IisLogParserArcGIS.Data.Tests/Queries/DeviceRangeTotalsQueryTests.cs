using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using static IisLogParserArcGIS.Data.Tests.TestSupport.DeviceTableTestSupport;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class DeviceRangeTotalsQueryTests
{
    private static readonly DateOnly _localDate = new(2026, 5, 1);

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void GetRangeTotals_SumsHitsAndTimeAcrossTheRange_AndReportsTheLastDateSeen(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(
            connection,
            table,
            DeviceRow("d1", "u1", _localDate, 5, timeTakenSecond: 1.5),
            DeviceRow("d1", "u1", _localDate.AddDays(3), 4, timeTakenSecond: 2.5));
        var query = new DeviceRangeTotalsQuery(connection, table);

        var row = Assert.Single(query.GetRangeTotals(_localDate, _localDate.AddDays(10)));

        Assert.Equal("d1", row.DeviceId);
        Assert.Equal("u1", row.Username);
        Assert.Equal(9, row.Hits);
        Assert.Equal(4.0, row.TotalTimeTakenSecond);
        Assert.Equal(_localDate.AddDays(3), row.LastSeen);
    }

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void GetRangeTotals_ExcludesRowsOutsideTheRange_IncludingFromLastSeen(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(connection, table, DeviceRow("d1", "u1", _localDate, 5), DeviceRow("d1", "u1", _localDate.AddDays(5), 7));
        var query = new DeviceRangeTotalsQuery(connection, table);

        var row = Assert.Single(query.GetRangeTotals(_localDate, _localDate.AddDays(1)));

        Assert.Equal(5, row.Hits);
        Assert.Equal(_localDate, row.LastSeen);
    }

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void GetRangeTotals_ReturnsOneRowPerDevice_AttributedOrNot(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(connection, table, DeviceRow("d2", null, _localDate, 2), DeviceRow("d1", "u1", _localDate, 3));
        var query = new DeviceRangeTotalsQuery(connection, table);

        var rows = query.GetRangeTotals(_localDate, _localDate).ToArray();

        Assert.Equal(["d1", "d2"], rows.Select(row => row.DeviceId));
        Assert.Null(rows[1].Username);
    }

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void GetRangeTotals_ForADeviceWithTwoUsernames_PicksTheSameOneAsTheTop50Query(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(
            connection,
            table,
            DeviceRow("lent", "zed", _localDate, 1),
            DeviceRow("lent", "amy", _localDate.AddDays(1), 1),
            DeviceRow("tie", "bob", _localDate, 1),
            DeviceRow("tie", "amy", _localDate, 1));

        var totals = new DeviceRangeTotalsQuery(connection, table)
            .GetRangeTotals(_localDate, _localDate.AddDays(1)).ToDictionary(row => row.DeviceId);
        var top = new DeviceTopHitsQuery(connection, table)
            .GetTop50(_localDate, _localDate.AddDays(1)).ToDictionary(row => row.DeviceId);

        Assert.Equal("zed", totals["lent"].Username);
        Assert.Equal("amy", totals["tie"].Username);
        Assert.Equal(top["lent"].Username, totals["lent"].Username);
        Assert.Equal(top["tie"].Username, totals["tie"].Username);
    }

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void GetRangeTotals_WithNoMatchingRows_ReturnsEmpty(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new DeviceRangeTotalsQuery(connection, table);

        Assert.Empty(query.GetRangeTotals(_localDate, _localDate));
    }

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void GetRangeTotals_IgnoresTheOtherTablesRows_EvenForTheSameDeviceId(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(connection, table, DeviceRow("shared-id", "own-user", _localDate, 7, timeTakenSecond: 14));
        InsertRows(connection, OtherTable(table), DeviceRow("shared-id", "other-user", _localDate.AddDays(2), 100), DeviceRow("other-only", "other-user", _localDate, 50));
        var query = new DeviceRangeTotalsQuery(connection, table);

        var row = Assert.Single(query.GetRangeTotals(_localDate, _localDate.AddDays(5)));

        Assert.Equal("shared-id", row.DeviceId);
        Assert.Equal("own-user", row.Username);
        Assert.Equal(7, row.Hits);
        Assert.Equal(14d, row.TotalTimeTakenSecond);
        Assert.Equal(_localDate, row.LastSeen);
    }
}

using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using static IisLogParserArcGIS.Data.Tests.TestSupport.DeviceTableTestSupport;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class DeviceTopHitsQueryTests
{
    private static readonly DateOnly _localDate = new(2026, 5, 1);

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void GetTop50_OrdersDescendingByHits(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(
            connection,
            table,
            DeviceRow("low", "u1", _localDate, 3),
            DeviceRow("high", "u2", _localDate, 9),
            DeviceRow("mid", "u3", _localDate, 6));
        var query = new DeviceTopHitsQuery(connection, table);

        var rows = query.GetTop50(_localDate, _localDate).ToArray();

        Assert.Equal(["high", "mid", "low"], rows.Select(row => row.DeviceId));
        Assert.Equal([9, 6, 3], rows.Select(row => row.Hits));
    }

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void GetTop50_SumsAcrossDatesInRange_AndExcludesRowsOutsideIt(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(
            connection,
            table,
            DeviceRow("d1", "u1", _localDate, 4),
            DeviceRow("d1", "u1", _localDate.AddDays(1), 5),
            DeviceRow("d1", "u1", _localDate.AddDays(2), 100));
        var query = new DeviceTopHitsQuery(connection, table);

        var rows = query.GetTop50(_localDate, _localDate.AddDays(1)).ToArray();

        var row = Assert.Single(rows);
        Assert.Equal(9, row.Hits);
    }

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void GetTop50_WithMoreThan50Devices_ReturnsExactly50(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(connection, table, [.. Enumerable.Range(1, 60).Select(index => DeviceRow($"device-{index:D2}", "u", _localDate, index))]);
        var query = new DeviceTopHitsQuery(connection, table);

        var rows = query.GetTop50(_localDate, _localDate).ToArray();

        Assert.Equal(50, rows.Length);
        Assert.Equal(60, rows[0].Hits);
        Assert.Equal(11, rows[^1].Hits);
    }

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void GetTop50_IncludesUnattributedDevices_WithANullUsername(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(connection, table, DeviceRow("anon", null, _localDate, 7));
        var query = new DeviceTopHitsQuery(connection, table);

        var row = Assert.Single(query.GetTop50(_localDate, _localDate));

        Assert.Equal("anon", row.DeviceId);
        Assert.Null(row.Username);
    }

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void GetTop50_WithNoMatchingRows_ReturnsEmpty(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        var query = new DeviceTopHitsQuery(connection, table);

        Assert.Empty(query.GetTop50(_localDate, _localDate));
    }

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void GetTop50_IgnoresTheOtherTablesRows_EvenForTheSameDeviceId(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(connection, table, DeviceRow("shared-id", "own-user", _localDate, 5));
        InsertRows(connection, OtherTable(table), DeviceRow("shared-id", "other-user", _localDate, 100), DeviceRow("other-only", "other-user", _localDate, 50));
        var query = new DeviceTopHitsQuery(connection, table);

        var row = Assert.Single(query.GetTop50(_localDate, _localDate));

        Assert.Equal("shared-id", row.DeviceId);
        Assert.Equal("own-user", row.Username);
        Assert.Equal(5, row.Hits);
    }
}

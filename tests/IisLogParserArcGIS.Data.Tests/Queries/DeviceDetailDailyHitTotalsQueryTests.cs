using IisLogParserArcGIS.Data.Connections;
using IisLogParserArcGIS.Data.Queries;
using IisLogParserArcGIS.Data.Schema;
using IisLogParserArcGIS.Data.Tests.TestSupport;
using static IisLogParserArcGIS.Data.Tests.TestSupport.DeviceTableTestSupport;

namespace IisLogParserArcGIS.Data.Tests.Queries;

public class DeviceDetailDailyHitTotalsQueryTests
{
    private static readonly DateOnly _localDate = new(2026, 5, 1);

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void GetDailyTotals_ReturnsOnlyThatDevicesDates_InDateOrder_WithNoZeroPadding(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(
            connection,
            table,
            DeviceRow("d1", null, _localDate.AddDays(20), 7),
            DeviceRow("d1", null, _localDate, 3),
            DeviceRow("d2", null, _localDate.AddDays(5), 99));
        var query = new DeviceDetailDailyHitTotalsQuery(connection, table);

        var rows = query.GetDailyTotals(_localDate, _localDate.AddDays(30), "d1").ToArray();

        Assert.Equal([_localDate, _localDate.AddDays(20)], rows.Select(row => row.LocalDate));
        Assert.Equal([3, 7], rows.Select(row => row.Hits));
    }

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void GetDailyTotals_ExcludesDatesOutsideTheRange(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(connection, table, DeviceRow("d1", null, _localDate, 3), DeviceRow("d1", null, _localDate.AddDays(9), 4));
        var query = new DeviceDetailDailyHitTotalsQuery(connection, table);

        var row = Assert.Single(query.GetDailyTotals(_localDate.AddDays(1), _localDate.AddDays(9), "d1"));

        Assert.Equal(4, row.Hits);
    }

    [Theory]
    [MemberData(nameof(TableNames), MemberType = typeof(DeviceTableTestSupport))]
    public void GetDailyTotals_IgnoresTheOtherTablesRows_EvenForTheSameDeviceId(string tableName)
    {
        var table = TableFor(tableName);
        using var database = new TempSqliteDatabase();
        using var connection = SqliteConnectionFactory.Open(database.Path);
        AggregateDatabaseSchema.EnsureCreated(connection);
        InsertRows(connection, table, DeviceRow("shared-id", "amy", _localDate, 4));
        InsertRows(connection, OtherTable(table), DeviceRow("shared-id", "amy", _localDate, 100), DeviceRow("shared-id", "amy", _localDate.AddDays(1), 100));
        var query = new DeviceDetailDailyHitTotalsQuery(connection, table);

        var row = Assert.Single(query.GetDailyTotals(_localDate, _localDate.AddDays(5), "shared-id"));

        Assert.Equal(_localDate, row.LocalDate);
        Assert.Equal(4, row.Hits);
    }
}

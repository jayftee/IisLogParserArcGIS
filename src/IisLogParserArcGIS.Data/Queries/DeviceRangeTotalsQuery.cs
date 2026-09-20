using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads per-device totals directly from one by-device aggregate table (Field Maps or Survey123), for a device
/// section's Complete View and as the driver set for its per-device Detail Pages. The table's (date, device)
/// grain already matches what this needs, so no SQL view is required (per ticket 03's resolution). Every
/// device is listed - attributed or not. The username of each device is chosen by
/// <see cref="DeviceUsernameSql"/>, shared with <see cref="DeviceTopHitsQuery"/>.
/// </summary>
public sealed class DeviceRangeTotalsQuery : AggregateQueryBase
{
    private readonly string _selectSql;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceRangeTotalsQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    /// <param name="table">The by-device table to read.</param>
    public DeviceRangeTotalsQuery(IDbConnection connection, DeviceAggregateTable table)
        : base(connection)
    {
        ArgumentNullException.ThrowIfNull(table);

        _selectSql =
            $"""
            SELECT {DeviceUsernameSql.DeviceAlias}.device_id AS DeviceId,
                {DeviceUsernameSql.Expression(table)} AS Username,
                SUM({DeviceUsernameSql.DeviceAlias}.hits) AS Hits,
                SUM({DeviceUsernameSql.DeviceAlias}.time_taken_second) AS TotalTimeTakenSecond,
                MAX({DeviceUsernameSql.DeviceAlias}.local_date) AS LastSeen
            FROM {table.Name} AS {DeviceUsernameSql.DeviceAlias}
            WHERE {DeviceUsernameSql.DeviceAlias}.local_date BETWEEN @StartDate AND @EndDate
            GROUP BY {DeviceUsernameSql.DeviceAlias}.device_id
            ORDER BY {DeviceUsernameSql.DeviceAlias}.device_id
            """;
    }

    /// <summary>
    /// Gets one row per device with at least one hit within [<paramref name="startDate"/>, <paramref name="endDate"/>].
    /// A device with no hits in range is simply absent, not a zero-valued row.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <returns>The matching range totals, ordered by device id.</returns>
    public IEnumerable<DeviceRangeTotalRow> GetRangeTotals(DateOnly startDate, DateOnly endDate)
    {
        return Connection.Query<DeviceRangeTotalRow>(
            _selectSql,
            new { StartDate = startDate, EndDate = endDate });
    }
}

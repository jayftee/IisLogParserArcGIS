using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads day-by-day hit totals for a single device directly from one by-device aggregate table (Field Maps or
/// Survey123), for that device's own Detail Page. No view needed: the (date, device) pair is already the base
/// table's own grain (per ticket 03's resolution).
/// </summary>
public sealed class DeviceDetailDailyHitTotalsQuery : AggregateQueryBase
{
    private readonly string _selectSql;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceDetailDailyHitTotalsQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    /// <param name="table">The by-device table to read.</param>
    public DeviceDetailDailyHitTotalsQuery(IDbConnection connection, DeviceAggregateTable table)
        : base(connection)
    {
        ArgumentNullException.ThrowIfNull(table);

        _selectSql =
            $"""
            SELECT local_date AS LocalDate, SUM(hits) AS Hits
            FROM {table.Name}
            WHERE local_date BETWEEN @StartDate AND @EndDate AND device_id = @DeviceId
            GROUP BY local_date
            ORDER BY local_date
            """;
    }

    /// <summary>
    /// Gets one row per local date within [<paramref name="startDate"/>, <paramref name="endDate"/>] on which
    /// <paramref name="deviceId"/> had at least one hit. A date with no hits is simply absent, not a
    /// zero-valued row - the caller must not zero-pad missing dates back in.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <param name="deviceId">The device to restrict results to.</param>
    /// <returns>The matching daily totals, ordered by date.</returns>
    public IEnumerable<DeviceDailyHitTotalRow> GetDailyTotals(DateOnly startDate, DateOnly endDate, string deviceId)
    {
        ArgumentNullException.ThrowIfNull(deviceId);

        return Connection.Query<DeviceDailyHitTotalRow>(
            _selectSql,
            new { StartDate = startDate, EndDate = endDate, DeviceId = deviceId });
    }
}

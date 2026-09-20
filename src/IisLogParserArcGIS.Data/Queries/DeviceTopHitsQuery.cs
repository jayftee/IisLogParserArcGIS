using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads the top 50 devices of one by-device aggregate table (Field Maps or Survey123), ranked by summed hits
/// over an arbitrary date range - a device section's Top-50 Leaderboard. One row per device, not per user, so a
/// user with two devices can appear twice. Unattributed devices (no username anywhere) are included. The
/// username of each ranked device is chosen by <see cref="DeviceUsernameSql"/>, after the ranking so it is
/// looked up for at most 50 devices rather than for every device in the table.
/// </summary>
public sealed class DeviceTopHitsQuery : AggregateQueryBase
{
    private const string LeaderboardSize = "50";

    private readonly string _selectSql;

    /// <summary>
    /// Initializes a new instance of the <see cref="DeviceTopHitsQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    /// <param name="table">The by-device table to rank.</param>
    public DeviceTopHitsQuery(IDbConnection connection, DeviceAggregateTable table)
        : base(connection)
    {
        ArgumentNullException.ThrowIfNull(table);

        _selectSql =
            $"""
            SELECT {DeviceUsernameSql.DeviceAlias}.device_id AS DeviceId,
                {DeviceUsernameSql.Expression(table)} AS Username,
                {DeviceUsernameSql.DeviceAlias}.hits AS Hits
            FROM (
                SELECT device_id, SUM(hits) AS hits
                FROM {table.Name}
                WHERE local_date BETWEEN @StartDate AND @EndDate
                GROUP BY device_id
                ORDER BY hits DESC
                LIMIT {LeaderboardSize}) AS {DeviceUsernameSql.DeviceAlias}
            ORDER BY {DeviceUsernameSql.DeviceAlias}.hits DESC
            """;
    }

    /// <summary>
    /// Gets up to the top 50 devices, ranked descending by summed hits within
    /// [<paramref name="startDate"/>, <paramref name="endDate"/>]. Fewer than 50 rows come back, with no
    /// placeholder, when fewer than 50 devices qualify - no secondary tie-break is applied among rows sharing
    /// the same hit count.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <returns>Up to 50 rows, ranked descending by hits.</returns>
    public IEnumerable<DeviceHitsLeaderboardRow> GetTop50(DateOnly startDate, DateOnly endDate)
    {
        return Connection.Query<DeviceHitsLeaderboardRow>(
            _selectSql,
            new { StartDate = startDate, EndDate = endDate });
    }
}

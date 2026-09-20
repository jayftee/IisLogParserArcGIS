using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads the top 500 forwarded-for IPs, ranked by summed hits over an arbitrary date range - one of the four
/// flat, standalone Leaderboards (ticket 17). The by-forwarded-for-IP aggregate's row grain already matches what
/// this needs, so no SQL view is required (per ticket 03's resolution).
/// </summary>
public sealed class ByForwardedForIpTopHitsQuery : AggregateQueryBase
{
    private const string LeaderboardSize = "500";

    private const string SelectSql =
        $"""
        SELECT forwarded_for_ip AS Value, SUM(hits) AS Hits, SUM(time_taken_second) AS TotalTimeTakenSecond
        FROM {AggregateTableNames.ByForwardedForIp}
        WHERE local_date BETWEEN @StartDate AND @EndDate
        GROUP BY forwarded_for_ip
        ORDER BY Hits DESC
        LIMIT {LeaderboardSize}
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByForwardedForIpTopHitsQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    public ByForwardedForIpTopHitsQuery(IDbConnection connection)
        : base(connection)
    {
    }

    /// <summary>
    /// Gets up to the top 500 forwarded-for IPs, ranked descending by summed hits within
    /// [<paramref name="startDate"/>, <paramref name="endDate"/>]. Fewer than 500 rows come back, with no
    /// placeholder, when fewer than 500 distinct IPs appear in range - no secondary tie-break is applied among
    /// rows sharing the same hit count.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <returns>Up to 500 rows, ranked descending by hits.</returns>
    public IEnumerable<FlatLeaderboardRow> GetTop500(DateOnly startDate, DateOnly endDate)
    {
        return Connection.Query<FlatLeaderboardRow>(SelectSql, new { StartDate = startDate, EndDate = endDate });
    }
}

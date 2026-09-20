using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads the top 500 user agents, ranked by summed hits over an arbitrary date range - one of the four flat,
/// standalone Leaderboards (ticket 17). The by-user-agent aggregate's row grain already matches what this needs,
/// so no SQL view is required (per ticket 03's resolution).
/// </summary>
public sealed class ByUserAgentTopHitsQuery : AggregateQueryBase
{
    private const string LeaderboardSize = "500";

    private const string SelectSql =
        $"""
        SELECT user_agent AS Value, SUM(hits) AS Hits, SUM(time_taken_second) AS TotalTimeTakenSecond
        FROM {AggregateTableNames.ByUserAgent}
        WHERE local_date BETWEEN @StartDate AND @EndDate
        GROUP BY user_agent
        ORDER BY Hits DESC
        LIMIT {LeaderboardSize}
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByUserAgentTopHitsQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    public ByUserAgentTopHitsQuery(IDbConnection connection)
        : base(connection)
    {
    }

    /// <summary>
    /// Gets up to the top 500 user agents, ranked descending by summed hits within [<paramref name="startDate"/>,
    /// <paramref name="endDate"/>]. Fewer than 500 rows come back, with no placeholder, when fewer than 500
    /// distinct user agents appear in range - no secondary tie-break is applied among rows sharing the same hit
    /// count.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <returns>Up to 500 rows, ranked descending by hits.</returns>
    public IEnumerable<FlatLeaderboardRow> GetTop500(DateOnly startDate, DateOnly endDate)
    {
        return Connection.Query<FlatLeaderboardRow>(SelectSql, new { StartDate = startDate, EndDate = endDate });
    }
}

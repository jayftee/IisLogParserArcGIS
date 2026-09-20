using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads the top 50 Portal items, ranked by summed failed hits over an arbitrary date range - the Portal
/// section's by-failed-hits Top-50 Leaderboard. Excludes any Portal item with zero failed hits in range, so an
/// item that never failed doesn't occupy a zero-value bar - matching
/// <see cref="ByPortalItemTopSuccessfulHitsQuery"/>'s own exclusion. No site/Root filter is applied - the
/// table's rows are already scoped to the configured Portal Web Adaptor at ingest time (per ticket 04's
/// resolution). A separate query class from <see cref="ByPortalItemTopSuccessfulHitsQuery"/>, per ticket 03's
/// "one query per Leaderboard variant" resolution, even though the two share the same row shape.
/// </summary>
public sealed class ByPortalItemTopFailedHitsQuery : AggregateQueryBase
{
    private const string LeaderboardSize = "50";

    private const string SelectSql =
        $"""
        SELECT portal_item_id AS PortalItemId, SUM(failed_hits) AS Hits
        FROM {AggregateTableNames.ByPortalItem}
        WHERE local_date BETWEEN @StartDate AND @EndDate
        GROUP BY portal_item_id
        HAVING SUM(failed_hits) > 0
        ORDER BY Hits DESC
        LIMIT {LeaderboardSize}
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByPortalItemTopFailedHitsQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    public ByPortalItemTopFailedHitsQuery(IDbConnection connection)
        : base(connection)
    {
    }

    /// <summary>
    /// Gets up to the top 50 Portal items, ranked descending by summed failed hits within
    /// [<paramref name="startDate"/>, <paramref name="endDate"/>]. Fewer than 50 rows come back, with no
    /// placeholder, when fewer than 50 Portal items qualify - no secondary tie-break is applied among rows
    /// sharing the same hit count.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <returns>Up to 50 rows, ranked descending by failed hits.</returns>
    public IEnumerable<PortalItemHitsLeaderboardRow> GetTop50(DateOnly startDate, DateOnly endDate)
    {
        return Connection.Query<PortalItemHitsLeaderboardRow>(
            SelectSql,
            new { StartDate = startDate, EndDate = endDate });
    }
}

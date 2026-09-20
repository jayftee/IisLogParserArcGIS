using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads the top 50 folder/service/type combinations for a single site, ranked by average time taken for failed
/// requests over an arbitrary date range - the ArcGIS Server section's by-average-time-failed Top-50 Leaderboard.
/// Excludes any folder/service/type with zero failed hits in range, since it has no defined average to rank by.
/// A separate query class from <see cref="ByArcGisServiceTopAverageSuccessfulTimeQuery"/>, per ticket 03's
/// "one query per Leaderboard variant" resolution, even though the two share the same row shape.
/// </summary>
public sealed class ByArcGisServiceTopAverageFailedTimeQuery : AggregateQueryBase
{
    private const string LeaderboardSize = "50";

    private const string SelectSql =
        $"""
        SELECT folder AS Folder, service_name AS ServiceName, service_type AS ServiceType,
            SUM(failed_time_taken_second) AS TotalTimeTakenSecond, SUM(failed_hits) AS Hits
        FROM {AggregateTableNames.ByArcGisService}
        WHERE local_date BETWEEN @StartDate AND @EndDate AND site = @Site
        GROUP BY folder, service_name, service_type
        HAVING SUM(failed_hits) > 0
        ORDER BY (SUM(failed_time_taken_second) * 1.0 / SUM(failed_hits)) DESC
        LIMIT {LeaderboardSize}
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByArcGisServiceTopAverageFailedTimeQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    public ByArcGisServiceTopAverageFailedTimeQuery(IDbConnection connection)
        : base(connection)
    {
    }

    /// <summary>
    /// Gets up to the top 50 folder/service/type combinations on <paramref name="site"/> with at least one
    /// failed hit, ranked descending by average failed-request time taken within
    /// [<paramref name="startDate"/>, <paramref name="endDate"/>]. Fewer than 50 rows come back, with no
    /// placeholder, when the site has fewer than 50 qualifying services - no secondary tie-break is applied
    /// among rows sharing the same average.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <param name="site">The site (Root) to restrict results to.</param>
    /// <returns>Up to 50 rows, ranked descending by average failed-request time taken.</returns>
    public IEnumerable<ArcGisServiceAverageTimeLeaderboardRow> GetTop50(DateOnly startDate, DateOnly endDate, string site)
    {
        ArgumentNullException.ThrowIfNull(site);

        return Connection.Query<ArcGisServiceAverageTimeLeaderboardRow>(
            SelectSql,
            new { StartDate = startDate, EndDate = endDate, Site = site });
    }
}

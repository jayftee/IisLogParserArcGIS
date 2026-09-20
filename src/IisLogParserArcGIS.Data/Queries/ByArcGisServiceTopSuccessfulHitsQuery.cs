using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads the top 50 folder/service/type combinations for a single site, ranked by summed successful hits over an
/// arbitrary date range - the ArcGIS Server section's by-successful-hits Top-50 Leaderboard. Excludes any
/// folder/service/type with zero successful hits in range, so a service that only ever failed doesn't occupy a
/// zero-value bar - matching how <see cref="ByArcGisServiceTopAverageSuccessfulTimeQuery"/> already excludes
/// the same case. A separate query class from <see cref="ByArcGisServiceTopFailedHitsQuery"/>, per ticket 03's
/// "one query per Leaderboard variant" resolution, even though the two share the same row shape.
/// </summary>
public sealed class ByArcGisServiceTopSuccessfulHitsQuery : AggregateQueryBase
{
    private const string LeaderboardSize = "50";

    private const string SelectSql =
        $"""
        SELECT folder AS Folder, service_name AS ServiceName, service_type AS ServiceType, SUM(successful_hits) AS Hits
        FROM {AggregateTableNames.ByArcGisService}
        WHERE local_date BETWEEN @StartDate AND @EndDate AND site = @Site
        GROUP BY folder, service_name, service_type
        HAVING SUM(successful_hits) > 0
        ORDER BY Hits DESC
        LIMIT {LeaderboardSize}
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByArcGisServiceTopSuccessfulHitsQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    public ByArcGisServiceTopSuccessfulHitsQuery(IDbConnection connection)
        : base(connection)
    {
    }

    /// <summary>
    /// Gets up to the top 50 folder/service/type combinations on <paramref name="site"/>, ranked descending by
    /// summed successful hits within [<paramref name="startDate"/>, <paramref name="endDate"/>]. Fewer than 50
    /// rows come back, with no placeholder, when the site has fewer than 50 services - no secondary tie-break is
    /// applied among rows sharing the same hit count.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <param name="site">The site (Root) to restrict results to.</param>
    /// <returns>Up to 50 rows, ranked descending by successful hits.</returns>
    public IEnumerable<ArcGisServiceHitsLeaderboardRow> GetTop50(DateOnly startDate, DateOnly endDate, string site)
    {
        ArgumentNullException.ThrowIfNull(site);

        return Connection.Query<ArcGisServiceHitsLeaderboardRow>(
            SelectSql,
            new { StartDate = startDate, EndDate = endDate, Site = site });
    }
}

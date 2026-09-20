using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads day-by-day hit totals for a single site directly from <see cref="AggregateTableNames.ByArcGisService"/>,
/// for the ArcGIS Server section's per-site successful/failed request evolution charts. The by-ArcGIS-Server-
/// service aggregate's row grain is finer than a site total (folder/service/type), so this collapses across
/// that dimension by day - still no view needed, since the collapse is a simple <c>GROUP BY local_date</c>
/// against a single site (per ticket 03's resolution, a view is reserved for genuinely cross-entity rollups).
/// </summary>
public sealed class ByArcGisServiceDailyHitTotalsQuery : AggregateQueryBase
{
    private const string SelectSql =
        $"""
        SELECT local_date AS LocalDate, SUM(hits) AS Hits, SUM(successful_hits) AS SuccessfulHits, SUM(failed_hits) AS FailedHits
        FROM {AggregateTableNames.ByArcGisService}
        WHERE local_date BETWEEN @StartDate AND @EndDate AND site = @Site
        GROUP BY local_date
        ORDER BY local_date
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByArcGisServiceDailyHitTotalsQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    public ByArcGisServiceDailyHitTotalsQuery(IDbConnection connection)
        : base(connection)
    {
    }

    /// <summary>
    /// Gets one row per local date within [<paramref name="startDate"/>, <paramref name="endDate"/>] on which
    /// <paramref name="site"/> had at least one hit. A date with no hits is simply absent, not a zero-valued row.
    /// </summary>
    /// <param name="startDate">The inclusive start of the date range.</param>
    /// <param name="endDate">The inclusive end of the date range.</param>
    /// <param name="site">The site (Root) to restrict results to.</param>
    /// <returns>The matching daily totals, ordered by date.</returns>
    public IEnumerable<ByArcGisServiceDailyHitTotalRow> GetDailyTotals(DateOnly startDate, DateOnly endDate, string site)
    {
        ArgumentNullException.ThrowIfNull(site);

        return Connection.Query<ByArcGisServiceDailyHitTotalRow>(
            SelectSql,
            new { StartDate = startDate, EndDate = endDate, Site = site });
    }
}

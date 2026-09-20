using System.Data;
using Dapper;
using IisLogParserArcGIS.Data.Schema;

namespace IisLogParserArcGIS.Data.Queries;

/// <summary>
/// Reads day-by-day successful/failed time-taken totals for a single site directly from
/// <see cref="AggregateTableNames.ByArcGisService"/>, for the ArcGIS Server section's per-site average-
/// processing-time evolution charts. Deliberately a separate query from
/// <see cref="ByArcGisServiceDailyHitTotalsQuery"/> - per ticket 11's acceptance criteria, an average-time
/// chart recomputes its own per-day sums rather than reusing the hit-count query's rows.
/// </summary>
public sealed class ByArcGisServiceDailyAverageTimeQuery : AggregateQueryBase
{
    private const string SelectSql =
        $"""
        SELECT local_date AS LocalDate,
            SUM(successful_time_taken_second) AS SuccessfulTimeTakenSecond, SUM(successful_hits) AS SuccessfulHits,
            SUM(failed_time_taken_second) AS FailedTimeTakenSecond, SUM(failed_hits) AS FailedHits
        FROM {AggregateTableNames.ByArcGisService}
        WHERE local_date BETWEEN @StartDate AND @EndDate AND site = @Site
        GROUP BY local_date
        ORDER BY local_date
        """;

    /// <summary>
    /// Initializes a new instance of the <see cref="ByArcGisServiceDailyAverageTimeQuery"/> class.
    /// </summary>
    /// <param name="connection">The database connection to query against.</param>
    public ByArcGisServiceDailyAverageTimeQuery(IDbConnection connection)
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
    public IEnumerable<ByArcGisServiceDailyAverageTimeRow> GetDailyAverageTimeTotals(DateOnly startDate, DateOnly endDate, string site)
    {
        ArgumentNullException.ThrowIfNull(site);

        return Connection.Query<ByArcGisServiceDailyAverageTimeRow>(
            SelectSql,
            new { StartDate = startDate, EndDate = endDate, Site = site });
    }
}
